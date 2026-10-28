using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreGuardian
{
    // Effect tags, enemy abilities, area blast and augment combat hooks.
    // Rules: Notion 전투 피해 계산 규칙 · 적 유형 및 보스 설계 · 타워 시스템 · 증강 시스템 (2026-10-26/27).
    // Session: damage pipeline: defense, increase, affinity, strike, kills, chains.
    public sealed partial class Session
    {
        // ================= Damage pipeline =================
        float EffectiveDefense(EnemyState e) => e.Spec.Defense + (e.ShieldTime > 0 ? e.Spec.ShieldDefense : 0);

        // 3-3 피해 증가: 21 동상, 37 위기 대응, 38 보스 사냥꾼, 50 유리 대포 (cards add together).
        float DamageIncreasePercent(EnemyState e)
        {
            float percent = 0;
            if (Augments.Has(21) && SlowedAtLeast(e, Augments.Value(21) / Augments.Count(21)))
            {
                percent += Augments.Value(21, 1);
            }

            if (Augments.Has(37) && CoreEnergy <= CoreMaximum * Augments.Value(37) / Augments.Count(37) / 100f)
            {
                percent += Augments.Value(37, 1);
            }

            if (Augments.Has(38) && IsBoss(e))
            {
                percent += Augments.Value(38);
            }

            if (Augments.Has(50))
            {
                percent += Augments.Value(50, 1);
            }

            return percent;
        }

        // 3-4 약점·저항: tower damage only. 49 약점 공략 replaces the multipliers (×1.6 / ×0.85).
        float Affinity(EnemyState e, int tower)
        {
            if (tower <= 0 || e.Spec == null)
                return 1;

            int bit = 1 << (tower - 1);
            if ((e.Spec.WeakTowers & bit) != 0)
            {
                return Augments.Has(49) ? Augments.Value(49) / Augments.Count(49) : tags.Weak;
            }

            if ((e.Spec.ResistTowers & bit) != 0)
            {
                return Augments.Has(49) ? Augments.Value(49, 1) / Augments.Count(49) : tags.Resist;
            }

            return 1;
        }

        // Damage input after buff/crit/falloff; applies increase, weakness, defense, rounding, then on-hit effects.
        int Strike(EnemyHandle target, float damage, string source, HitPayload payload)
        {
            if (!target.Alive)
                return 0;

            var e = target.Slot;
            float affinity = Affinity(e, payload.Tower);
            float scaled = damage * (1 + DamageIncreasePercent(e) / 100f) * affinity;
            int affinityMark =
                affinity > 1 ? 1
                : affinity < 1 ? -1
                : 0;
            bool criticalMark = payload.Crit > 1;
            hitAffinity = affinityMark;
            hitCritical = criticalMark;
            int dealt;
            try
            {
                dealt = DealDamage(
                    target,
                    CombatRules.Damage(scaled, EffectiveDefense(e)),
                    source,
                    scaled * Mathf.Max(0, 1 - EffectiveDefense(e) / 100f),
                    payload
                );
            }
            finally
            {
                hitAffinity = 0;
                hitCritical = false;
            }
            if (!target.Alive)
                return dealt;

            if (payload.BurnStacks > 0)
            {
                AddBurn(e, payload.BurnPerStack, payload.BurnDuration, payload.BurnStacks, source);
                RaiseCue("hit_flame", e.Position);
            }
            if (payload.SlowPercent > 0)
            {
                if (payload.Player)
                {
                    e.SlowPlayer = Mathf.Max(e.SlowPlayer, payload.SlowPercent);
                    e.SlowRemaining = Mathf.Max(e.SlowRemaining, payload.SlowDuration);
                }
                else
                {
                    ApplyFrostSlow(e, payload.SlowPercent, payload.SlowDuration);
                }
            }
            // 7 처형 (기술병), 27 얼음 감옥 (둔화된 적), 43 사신 (범위 폭발). Highest threshold only. Bosses immune.
            float threshold = payload.Execute;
            if (Augments.Has(27) && SlowFraction(e) > 0)
            {
                threshold = threshold > 0 ? threshold + Augments.Value(27) : Augments.Value(27);
            }

            if (threshold > 0 && !IsBoss(e) && e.Health > 0 && e.Health <= e.Spec.Health * threshold / 100f)
            {
                RaiseCue("execute", e.Position);
                hitAffinity = affinityMark;
                hitCritical = criticalMark;
                try
                {
                    DealDamage(target, e.Health, source, e.Health, payload);
                }
                finally
                {
                    hitAffinity = 0;
                    hitCritical = false;
                }
                if (Augments.Has(25))
                {
                    areaBlastCooldown = Mathf.Max(0, areaBlastCooldown - Augments.Value(25));
                }
            }
            // 39 절대 영도
            if (
                target.Alive
                && Augments.Has(39)
                && !e.Frozen
                && SlowedAtLeast(e, Augments.Value(39) / Augments.Count(39))
            )
            {
                float seconds = Augments.Value(39, 1) / Augments.Count(39);
                if (ApplyStun(e, seconds))
                {
                    e.Frozen = true;
                    e.FrozenDamage = 0;
                    RaiseCue("freeze", e.Position);
                }
            }
            return dealt;
        }

        // Integer health change, totals, kill handling. "raw" is the pre-rounding value used for overkill.
        int DealDamage(EnemyHandle target, int amount, string source, float raw, HitPayload payload)
        {
            if (!target.Alive || amount <= 0)
                return 0;

            var e = target.Slot;
            int before = e.Health;
            int actual = Mathf.Min(before, amount);
            enemyRevision++;
            e.Health -= actual;
            // 피드백 4: 범위 폭발(PlayerSkill)도 누적 피해는 기술병 한 줄로 합산.
            var totalKey = source == "PlayerSkill" ? "Player" : source;
            DamageTotals.TryGetValue(totalKey, out int previous);
            DamageTotals[totalKey] = previous + actual;
            if (e.Frozen)
            {
                e.FrozenDamage += actual;
            }

            if (e.Health > 0)
            {
                CheckShield(e, before);
            }

            if (actual > 0)
            {
                DamageApplied?.Invoke(new EnemySnapshot(target), actual);
            }

            if (e.Health == 0 && !e.Exited)
            {
                OnKilled(target, source, raw - before, payload);
            }

            return actual;
        }

        void CheckShield(EnemyState e, int before)
        {
            var thresholds = e.Spec.ShieldAt;
            if (thresholds.Length == 0)
                return;

            bool deploy = false;
            for (int i = 0; i < thresholds.Length; i++)
            {
                if ((e.ShieldUsed & (1 << i)) != 0)
                    continue;

                float limit = e.Spec.Health * thresholds[i] / 100f;
                if (before > limit && e.Health <= limit)
                {
                    e.ShieldUsed |= 1 << i;
                    deploy = true;
                }
            }
            // Crossing two thresholds in one hit deploys once; both are consumed.
            if (deploy)
            {
                e.ShieldTime = e.Spec.ShieldSeconds;
                RaiseCue("shield_deploy", e.Position);
            }
        }

        int KillGold(int baseGold) =>
            Mathf.Max(0, Mathf.FloorToInt(baseGold * (1 + Augments.KillGoldPercent / 100f) + .5f));

        /// <summary>
        /// Gold a kill of this enemy pays at this moment (kill-gold augments 10·11·18 applied; summons pay nothing).
        /// Valid inside DamageApplied for the killing hit, before the kill counters advance.
        /// </summary>
        public int KillGold(EnemySnapshot enemy)
        {
            var slot = enemy.Handle.Slot;
            if (slot != null && slot.SummonerId != 0)
                return 0;
            return KillGold(enemy.Spec.Gold);
        }

        void OnKilled(EnemyHandle target, string source, float overkill, HitPayload payload)
        {
            var e = target.Slot;
            if (e.SummonerId == 0)
            {
                int gold = KillGold(e.Spec.Gold);
                Gold += gold;
                SetNumberMessage('g', gold);
            }
            Augments.Kills++;
            if (IsBoss(e))
            {
                BossKilled = true;
                if (Augments.Has(31))
                {
                    Player.Damage += Augments.Value(31);
                }
            }
            if (payload.Player || source == "Player")
            {
                if (Augments.Has(9))
                {
                    Player.Damage += Augments.Value(9);
                }
            }
            if (e.Frozen)
            {
                Shatter(target);
            }
            // Splitter: children spawn where it fell, 0.3 tiles ahead and behind on the same route.
            if (e.Spec.SplitIntoId != 0 && e.Spec.SplitCount > 0)
            {
                for (int i = 0; i < e.Spec.SplitCount; i++)
                {
                    pendingSpawns.Add(
                        new PendingSpawn
                        {
                            Spec = SplitChildSpec(e.Spec),
                            Distance = e.PathTravelled + (i % 2 == 0 ? .3f : -.3f),
                            Route = e.Route,
                        }
                    );
                }
            }

            if (e.Spec.SplitIntoId != 0)
            {
                RaiseCue("split", e.Position);
            }

            int max = tags.BurnMax;
            // 40 업화: max-stack burn victims explode.
            if (Augments.Has(40) && e.BurnStacks >= max)
            {
                RaiseCue("burn_explode", e.Position);
                float radius = Augments.Value(40) / Augments.Count(40);
                int stacks = (int)Augments.Value(40, 1);
                foreach (var other in Enemies)
                {
                    if (other != target && other.Alive && Vector2.Distance(other.Position, e.Position) <= radius)
                    {
                        AddBurn(other.Slot, e.BurnPerStack, Mathf.Max(e.BurnTime, 1), stacks);
                    }
                }
            }
            // 28 번지는 불: a burn death moves its stacks to the nearest enemy (fresh duration).
            if (Augments.Has(28) && e.BurnKill && e.BurnStacks > 0)
            {
                var near = Nearest(e.Position, float.PositiveInfinity, target, null);
                if (near != default)
                {
                    FlameBurn(out _, out float duration);
                    AddBurn(near.Slot, e.BurnPerStack, duration > 0 ? duration : 3, e.BurnStacks);
                }
            }
            // 42 연쇄 폭풍: leftover damage bounces once.
            if (Augments.Has(42) && !payload.NoBounce && overkill > 0)
            {
                var near = Nearest(e.Position, tags.ChainRadius, target, null);
                if (near != default)
                {
                    var bounce = payload;
                    bounce.NoBounce = true;
                    bounce.Chain = 0;
                    RaiseCue("chain", near.Position);
                    Strike(near, overkill, source, bounce);
                }
            }
        }

        EnemyHandle Nearest(Vector2 from, float radius, EnemyHandle except, List<int> exclude)
        {
            EnemyHandle best = default;
            float bestDistance = float.PositiveInfinity;
            foreach (var other in Enemies)
            {
                if (other == except || !other.Alive || exclude != null && exclude.Contains(other.Id))
                    continue;

                float d = Vector2.Distance(other.Position, from);
                if (d <= radius && (d < bestDistance || d == bestDistance && best != default && other.Id < best.Id))
                {
                    best = other;
                    bestDistance = d;
                }
            }
            return best;
        }

        // Hit plus instant chain bounces (반지름 2, ×0.7, 24번이면 ×1).
        int HitWithChain(EnemyHandle target, float damage, string source, HitPayload payload)
        {
            var at = target.Position;
            int id = target.Id;
            int dealt = Strike(target, damage, source, payload);
            if (payload.Chain <= 0)
                return dealt;

            chainHits.Clear();
            chainHits.Add(id);
            float radius = tags.ChainRadius,
                falloff = payload.ChainFalloff;
            var next = payload;
            next.Chain = 0;
            for (int i = 0; i < payload.Chain; i++)
            {
                var near = Nearest(at, radius, default, chainHits);
                if (near == default)
                    break;

                damage *= falloff;
                chainHits.Add(near.Id);
                at = near.Position;
                RaiseCue("chain", at);
                dealt += Strike(near, damage, source, next);
            }
            return dealt;
        }
    }
}
