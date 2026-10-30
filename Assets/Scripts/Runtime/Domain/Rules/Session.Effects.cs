using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreGuardian
{
    // Effect tags, enemy abilities, area blast and augment combat hooks.
    // Rules: Notion 전투 피해 계산 규칙 · 적 유형 및 보스 설계 · 타워 시스템 · 증강 시스템 (2026-10-26/27).
    public sealed partial class Session
    {
        public readonly struct CueEvent
        {
            public readonly string Id;
            public readonly Vector2 Position;

            public CueEvent(string id, Vector2 position)
            {
                Id = id;
                Position = position;
            }
        }

        // Presentation cues (sound/VFX): crit, execute, freeze, shatter, burn_explode, chain, heal_pulse, shield_deploy,
        // summon, split, area_blast, cold_pulse, flame_shot, rail_shot, hit_cold, hit_flame, hit_rail, star_merge, reroll.
        public event Action<CueEvent> Cue;
        public event Action<int, Vector2, float> TowerPulsed;
        public event Action<Vector2, float> AreaBlastUsed;
        readonly List<CueEvent> pendingCues = new List<CueEvent>();
        readonly List<CueEvent> cueWork = new List<CueEvent>();

        const int MaxPendingCues = 256;
        bool cueOverflowWarned;

        /// <summary>Cues dropped because more than MaxPendingCues were raised before one flush.</summary>
        public int DroppedCues { get; private set; }

        void RaiseCue(string id, Vector2 position)
        {
            if (Cue == null)
                return;

            if (pendingCues.Count < MaxPendingCues)
            {
                pendingCues.Add(new CueEvent(id, position));
                return;
            }

            DroppedCues++;
            if (!cueOverflowWarned)
            {
                cueOverflowWarned = true;
                Debug.LogWarning(
                    "[CoreGuardian] More than " + MaxPendingCues + " effect/sound cues in one step; extras dropped."
                );
            }
        }

        void FlushCues()
        {
            if (pendingCues.Count == 0)
                return;

            cueWork.Clear();
            cueWork.AddRange(pendingCues);
            pendingCues.Clear();
            var cue = Cue;
            if (cue == null)
                return;

            foreach (var c in cueWork)
            {
                cue(c);
            }

            cueWork.Clear();
        }

        // Effect-tag numbers (effectTags.csv) read once; hot paths never search the table.
        struct TagCache
        {
            public float SlowMax,
                SlowMaxBoss,
                SlowBossScale,
                BurnTick,
                BurnIgnore,
                ChainRadius,
                ChainFalloff,
                PierceFalloff,
                ExecuteBase,
                CritBase,
                CritMul,
                CritCap,
                StunBossScale,
                StunImmune,
                Weak,
                Resist;
            public int BurnMax;

            public static TagCache From(CombatData d) =>
                new TagCache
                {
                    SlowMax = d.Tag("Slow", "maxPercent", false, 60),
                    SlowMaxBoss = d.Tag("Slow", "maxPercent", true, 30),
                    SlowBossScale = d.Tag("Slow", "effectScale", true, .5f),
                    BurnMax = (int)d.Tag("Burn", "maxStacks", false, 5),
                    BurnTick = d.Tag("Burn", "tickSeconds", false, 1),
                    BurnIgnore = d.Tag("Burn", "defenseIgnore", false, 50),
                    ChainRadius = d.Tag("Chain", "radius", false, 2),
                    ChainFalloff = d.Tag("Chain", "falloff", false, .7f),
                    PierceFalloff = d.Tag("Pierce", "falloff", false, .8f),
                    ExecuteBase = d.Tag("Execute", "baseThreshold", false, 8),
                    CritBase = d.Tag("Crit", "baseChance", false, 0),
                    CritMul = d.Tag("Crit", "baseMultiplier", false, 2),
                    CritCap = d.Tag("Crit", "chanceCap", false, 100),
                    StunBossScale = d.Tag("Stun", "durationScale", true, .25f),
                    StunImmune = d.Tag("Stun", "immuneSeconds", false, 3),
                    Weak = d.Tag("Affinity", "weakMultiplier", false, 1.3f),
                    Resist = d.Tag("Affinity", "resistMultiplier", false, .7f),
                };
        }

        TagCache tags;

        // ---- random for crits (separate stream so card draws stay reproducible per seed)
        readonly System.Random combatRandom;

        // ---- flying route (generic polyline; diagonal segments allowed)
        readonly List<Vector2> flyPath = new List<Vector2>();
        readonly List<float> flyEnds = new List<float>();
        float flyLength;
        public bool HasFlyingRoute => flyPath.Count >= 2;
        public IReadOnlyList<Vector2> FlyingRoute => flyPath;

        public void SetFlyingRoute(IEnumerable<Vector2> route)
        {
            flyPath.Clear();
            flyEnds.Clear();
            flyLength = 0;
            foreach (var p in route)
            {
                flyPath.Add(p);
            }

            if (flyPath.Count < 2)
            {
                flyPath.Clear();
                return;
            }
            if (flyPath[flyPath.Count - 1] != (Vector2)core)
            {
                throw new ArgumentException("Flying route must terminate at core");
            }

            for (int i = 1; i < flyPath.Count; i++)
            {
                float len = Vector2.Distance(flyPath[i - 1], flyPath[i]);
                if (len <= 0)
                {
                    throw new ArgumentException("Zero length segment");
                }
                flyLength += len;
                flyEnds.Add(flyLength);
            }
        }

        void PositionFlying(EnemyState e)
        {
            float d = Mathf.Clamp(e.PathTravelled, 0, flyLength);
            for (int i = 0; i < flyEnds.Count; i++)
            {
                if (d <= flyEnds[i])
                {
                    float start = i == 0 ? 0 : flyEnds[i - 1];
                    float len = flyEnds[i] - start;
                    e.Position = Vector2.Lerp(flyPath[i], flyPath[i + 1], len <= 0 ? 1 : (d - start) / len);
                    e.PathRemaining = Mathf.Max(0, flyLength - e.PathTravelled);
                    return;
                }
            }
            e.Position = flyPath[flyPath.Count - 1];
            e.PathRemaining = 0;
        }

        float RouteLength(EnemyState e) => e.Route == 1 ? flyLength : pathLength;

        // Presentation only: +1 약점 / -1 저항 for the hit being reported through DamageApplied (피해 숫자 표시).
        int hitAffinity;
        public int HitAffinity => hitAffinity;

        // Presentation only: the hit being reported through DamageApplied was a critical hit (치명타 숫자 표시).
        bool hitCritical;
        public bool HitCritical => hitCritical;

        readonly List<int> chainHits = new List<int>(8);

        readonly List<PendingSpawn> pendingSpawns = new List<PendingSpawn>();
        readonly Dictionary<long, EnemySpec> roundSpecs = new Dictionary<long, EnemySpec>();
        int roundSpecWave = -1;

        // ================= Enemy abilities =================
        // 회복받는 적 위 + 표식을 보여 주는 시간(표시 전용, 회복 파동 페이드와 같게).
        public const float HealMarkSeconds = .6f;

        // ================= 22 불길 발자국 =================
        readonly Dictionary<Vector2Int, float> fireCells = new Dictionary<Vector2Int, float>();
        readonly List<Vector2Int> fireWork = new List<Vector2Int>();
        float fireTick;

        // ================= Area blast (32001) =================
        float areaBlastCooldown;
        public SkillSpec AreaBlast => Data.Skills.Count > 0 ? Data.Skills[0] : null;
        public float AreaBlastCooldown => areaBlastCooldown;
        public bool AreaBlastReady => AreaBlast != null && Phase == SessionPhase.Combat && areaBlastCooldown <= 0;

        // 46 순간 이동(10-30 사용자 결정): Shift로 따로 쓰고 대기 시간도 따로(augments value3, 범위 폭발과 같은 12초).
        float teleportCooldown;
        public float TeleportCooldown => teleportCooldown;
        public float TeleportCooldownTotal =>
            Augments.Has(46) ? Augments.Value(46, 2) / Mathf.Max(1, Augments.Count(46)) : 0;
        public bool TeleportReady => Augments.Has(46) && Phase == SessionPhase.Combat && teleportCooldown <= 0;

        public bool StartTeleportCooldown()
        {
            if (!TeleportReady)
                return false;

            teleportCooldown = TeleportCooldownTotal;
            return true;
        }

        readonly List<EnemyHandle> blastWork = new List<EnemyHandle>();

        // ================= Crit =================
        int playerAttacks;
        float playerCritBonus;
    }
}
