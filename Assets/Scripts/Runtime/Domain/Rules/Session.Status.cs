using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreGuardian
{
    // Effect tags, enemy abilities, area blast and augment combat hooks.
    // Rules: Notion 전투 피해 계산 규칙 · 적 유형 및 보스 설계 · 타워 시스템 · 증강 시스템 (2026-10-26/27).
    // Session: status effects: slow, burn, stun/freeze, shatter.
    public sealed partial class Session
    {
        // ================= Slow =================
        bool IsBoss(EnemyState e) => e.Kind == EnemyKind.Boss;

        internal float SlowFraction(EnemyState e)
        {
            if (e == null || e.Spec == null)
                return 0;

            bool boss = IsBoss(e);
            float scale = boss ? tags.SlowBossScale : 1,
                cap = (boss ? tags.SlowMaxBoss : tags.SlowMax) / 100f;
            float keep = 1;
            if (e.SlowFrostTime > 0)
            {
                keep *= 1 - e.SlowFrost * scale / 100f;
            }

            if (e.SlowRemaining > 0)
            {
                keep *= 1 - (e.SlowPlayer > 0 ? e.SlowPlayer : 20) * scale / 100f;
            }

            if (Augments.Has(15) && e.PathTravelled <= Augments.Value(15) / Augments.Count(15))
            {
                keep *= 1 - Augments.Value(15, 1) / Augments.Count(15) * scale / 100f;
            }

            return Mathf.Clamp(1 - keep, 0, cap);
        }

        // Bosses count as reaching any "slowed by X%" condition once they hit their slow cap.
        bool SlowedAtLeast(EnemyState e, float percent)
        {
            float slow = SlowFraction(e) * 100;
            if (slow <= 0)
                return false;

            if (IsBoss(e))
                return slow + .001f >= tags.SlowMaxBoss;

            return slow + .001f >= percent;
        }

        void ApplyFrostSlow(EnemyState e, float percent, float seconds)
        {
            percent = Mathf.Min(percent, tags.SlowMax);
            // Same kind of slow: strongest wins; a hit refreshes the duration.
            if (e.SlowFrostTime <= 0 || percent >= e.SlowFrost)
            {
                e.SlowFrost = percent;
            }

            e.SlowFrostTime = Mathf.Max(e.SlowFrostTime, seconds);
        }

        // ================= Burn =================
        void AddBurn(EnemyState e, float perStack, float duration, int stacks, string source = null)
        {
            if (stacks <= 0 || perStack <= 0 || duration <= 0 || e.Health <= 0)
                return;

            int max = tags.BurnMax;
            bool fresh = e.BurnStacks == 0;
            if (fresh)
            {
                e.BurnTick = 0;
                e.BurnCarry = 0;
            }
            e.BurnStacks = Mathf.Min(max, e.BurnStacks + stacks);
            e.BurnTime = duration;
            e.BurnPerStack = fresh ? perStack : Mathf.Max(e.BurnPerStack, perStack);
            if (source != null)
            {
                e.BurnSource = source;
            }
        }

        // Flame tower values with shared upgrades (불씨·불길 칸 use these).
        void FlameBurn(out float perStack, out float duration)
        {
            perStack = 0;
            duration = 0;
            if (!Data.Towers.TryGetValue(TowerKind.Flame, out var flame))
                return;

            perStack = flame.BurnPerStack;
            duration = flame.BurnDuration;
            foreach (var u in Data.Upgrades)
            {
                if (u.Target == "Flame")
                {
                    int n = TowerStep(TowerKind.Flame, UpgradeKey(u));
                    if (u.Stat == "burnPerStack")
                    {
                        perStack += u.Delta * n;
                    }
                    else if (u.Stat == "burnDuration")
                    {
                        duration += u.Delta * n;
                    }
                }
            }
        }

        void TickBurn(EnemyHandle h, float dt)
        {
            var e = h.Slot;
            if (e.BurnStacks <= 0)
                return;

            float tick = tags.BurnTick,
                ignore = tags.BurnIgnore / 100f;
            e.BurnTime -= dt;
            e.BurnTick += dt;
            while (e.BurnTick >= tick && e.BurnStacks > 0 && e.Health > 0)
            {
                e.BurnTick -= tick;
                // 화상 틱은 화염 포탑 피해로 판정 (증강 4·22·28·40 화상 포함, 2026-10-28).
                float raw =
                    e.BurnPerStack
                    * e.BurnStacks
                    * (1 + DamageIncreasePercent(e) / 100f)
                    * Affinity(e, (int)TowerKind.Flame + 1)
                    * Mathf.Max(0, 1 - EffectiveDefense(e) * (1 - ignore) / 100f);
                e.BurnCarry += raw;
                int whole = Mathf.FloorToInt(e.BurnCarry);
                e.BurnCarry -= whole;
                if (whole > 0)
                {
                    e.BurnKill = true;
                    float flame = Affinity(e, (int)TowerKind.Flame + 1);
                    hitAffinity =
                        flame > 1 ? 1
                        : flame < 1 ? -1
                        : 0;
                    try
                    {
                        DealDamage(h, whole, e.BurnSource ?? "Burn", raw, default);
                    }
                    finally
                    {
                        hitAffinity = 0;
                    }
                    if (h.Accessible)
                    {
                        e.BurnKill = false;
                    }
                }
            }
            if (e.BurnTime <= 0)
            {
                e.BurnStacks = 0;
                e.BurnCarry = 0;
            }
        }

        // ================= Stun / freeze =================
        // S19 기절음: 걸리는 순간 한 번. 빙결은 자기 소리(freeze)를 따로 낸다.
        bool ApplyStun(EnemyState e, float seconds, bool freeze = false)
        {
            if (e.StunTime > 0 || e.StunImmune > 0 || seconds <= 0)
                return false;

            e.StunTime = seconds * (IsBoss(e) ? tags.StunBossScale : 1);
            if (!freeze)
            {
                RaiseCue("stun", e.Position);
            }

            return true;
        }

        void TickStun(EnemyHandle h, float dt)
        {
            var e = h.Slot;
            if (e.StunImmune > 0)
            {
                e.StunImmune = Mathf.Max(0, e.StunImmune - dt);
            }

            if (e.StunTime <= 0)
                return;

            e.StunTime -= dt;
            if (e.StunTime <= 0)
            {
                e.StunTime = 0;
                e.StunImmune = tags.StunImmune;
                if (e.Frozen)
                {
                    Shatter(h);
                }
            }
        }

        void Shatter(EnemyHandle h)
        {
            var e = h.Slot;
            if (!e.Frozen)
                return;

            e.Frozen = false;
            float damage = e.FrozenDamage * .5f;
            e.FrozenDamage = 0;
            var at = e.Position;
            RaiseCue("shatter", at);
            if (damage <= 0)
                return;

            float radius = Augments.Value(39, 2) / Mathf.Max(1, Augments.Count(39));
            foreach (var other in Enemies)
            {
                if (other != h && other.Alive && Vector2.Distance(other.Position, at) <= radius)
                {
                    hitReplacesDeath = true;
                    try
                    {
                        Strike(other, damage, "Shatter", default);
                    }
                    finally
                    {
                        hitReplacesDeath = false;
                    }
                }
            }
        }
    }
}
