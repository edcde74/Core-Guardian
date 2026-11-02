using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CoreGuardian
{
    // Session: tower/player attacks: payloads, pulses, volleys, target order.
    public sealed partial class Session
    {
        HitPayload LaunchPayload(TowerState tower, CombatStats stats, float buff, float crit)
        {
            var p = new HitPayload { Crit = crit };
            if (tower != null)
            {
                p.Tower = (int)tower.Kind + 1;
            }

            if (tower == null)
            {
                p.Player = true;
                if (Augments.Has(3))
                {
                    p.SlowPercent = Augments.Value(3) / Augments.Count(3);
                    p.SlowDuration = Augments.Value(3, 1) / Augments.Count(3);
                }
                if (Augments.Has(5))
                {
                    p.ExtraPierce = Augments.Count(5);
                    p.PierceFalloff = tags.PierceFalloff;
                    // 29 관통 치명 (10-30 결정): 기술병 관통 탄에도 적용.
                    p.CritNoFalloff = Augments.Has(29) && crit > 1;
                }
                if (Augments.Has(7))
                {
                    p.Execute = Augments.Value(7) / Augments.Count(7);
                }

                return p;
            }
            switch (tower.Kind)
            {
                case TowerKind.Flame:
                    p.BurnStacks = 1;
                    p.BurnPerStack = stats.BurnPerStack * (1 + buff / 100f);
                    p.BurnDuration = stats.BurnDuration;
                    break;
                case TowerKind.Single:
                    if (Augments.Has(4))
                    {
                        FlameBurn(out float per, out float duration);
                        p.BurnStacks = Augments.Count(4);
                        p.BurnPerStack = per * (1 + buff / 100f);
                        p.BurnDuration = duration;
                    }
                    break;
                case TowerKind.Multi:
                    p.Chain = Augments.ChainCount;
                    p.ChainFalloff = Augments.Has(24) ? 1 : tags.ChainFalloff;
                    break;
                case TowerKind.Rail:
                    p.Rail = true;
                    p.PierceFalloff = stats.PierceFalloff > 0 ? stats.PierceFalloff : tags.PierceFalloff;
                    p.PierceGrowth = Augments.Has(41);
                    p.CritNoFalloff = Augments.Has(29) && crit > 1;
                    break;
            }
            return p;
        }

        // 냉기 포탑: every 1/rate seconds, damage + slow to every enemy within the pulse radius. No target choice.
        void Pulse(TowerState t, float buff, float rateMul, float dt, EnemyRangeIndex rangeIndex)
        {
            var stats = t.Stats;
            float rate = stats.Rate * rateMul;
            var work = shotWorkPool.Count > 0 ? shotWorkPool.Pop() : new ShotWork();
            try
            {
                var available = work.Available;
                if (!rangeIndex.TryCollect(t.Position, stats.Range, Enemies, available, work.SpatialCandidates))
                {
                    foreach (var enemy in Enemies)
                    {
                        if (enemy.Alive && Vector2.Distance(enemy.Position, t.Position) <= stats.Range)
                        {
                            available.Add(enemy);
                        }
                    }
                }

                t.Cooldown -= dt;
                if (available.Count == 0 || rate <= 0)
                {
                    t.Cooldown = Mathf.Max(0, t.Cooldown);
                    return;
                }
                if (t.Cooldown > 0)
                    return;

                while (t.Cooldown <= .00001f)
                {
                    float crit = RollCrit(ref t.Attacks, ref t.CritBonus);
                    var payload = LaunchPayload(t, stats, buff, crit);
                    payload.SlowPercent = stats.SlowPercent;
                    payload.SlowDuration = stats.SlowDuration;
                    float damage = stats.Damage * (1 + buff / 100f) * crit;
                    RaiseCue("cold_pulse", t.Position);
                    if (crit > 1)
                    {
                        RaiseCue("crit", t.Position);
                    }

                    TowerPulsed?.Invoke(t.Id, t.Position, stats.Range);
                    foreach (var e in available)
                    {
                        if (e.Alive)
                        {
                            Strike(e, damage, t.AttackSource, payload);
                            RaiseCue("hit_cold", e.Position);
                        }
                    }
                    rangeIndex.Invalidate();
                    supportCache.Dirty = true;
                    var onAttack = AttackLaunched;
                    if (onAttack != null)
                    {
                        try
                        {
                            onAttack(t.AttackSource, 1);
                        }
                        finally
                        {
                            rangeIndex.Invalidate();
                            supportCache.Dirty = true;
                        }
                    }
                    if (disposed)
                        return;

                    float next = t.Cooldown + 1 / rate;
                    if (next <= t.Cooldown)
                    {
                        throw new InvalidOperationException("Attack rate exceeds floating-point timing precision");
                    }

                    t.Cooldown = next;
                    available.RemoveAll(e => !e.Alive);
                    if (available.Count == 0)
                    {
                        t.Cooldown = Mathf.Max(t.Cooldown, 0);
                        break;
                    }
                }
            }
            finally
            {
                work.Clear();
                if (shotWorkPool.Count < MaxRetainedWorkBuffers)
                {
                    shotWorkPool.Push(work);
                }
            }
        }

        void Shoot(
            Vector2 position,
            CombatStats stats,
            string source,
            float buff,
            ref float cooldown,
            ref EnemyHandle target,
            ref bool acquired,
            float dt,
            bool multi,
            EnemyRangeIndex rangeIndex,
            TowerState tower,
            float rateMul
        )
        {
            var work = shotWorkPool.Count > 0 ? shotWorkPool.Pop() : new ShotWork();
            try
            {
                var available = work.Available;
                if (!rangeIndex.TryCollect(position, stats.Range, Enemies, available, work.SpatialCandidates))
                {
                    foreach (var enemy in Enemies)
                    {
                        if (enemy.Alive && Vector2.Distance(enemy.Position, position) <= stats.Range)
                        {
                            available.Add(enemy);
                        }
                    }
                }
                // 공격 대상 선택(정본)은 플레이어·포탑 공통: 사거리가 비면 다음 진입 때 다시 '가장 먼저 들어온 적',
                // 보스는 일반 적보다 뒤, 보스 조준 중 일반 적이 들어오면 바꿈.
                // 포탑은 종류별 공격 순서(towers.targetPriority), 기술병은 공통 순서(player). 2026-10-28 사용자 확정.
                // 대상 유지: 살아 있고 사거리 안이면 바꾸지 않음. 보스 맨 뒤·보스 조준 중 교체는 기술병만.
                var order = tower != null ? TowerOrder(tower) : PlayerOrder;
                if (available.Count == 0)
                {
                    acquired = false;
                }

                if (target != default && (!target.Alive || Vector2.Distance(target.Position, position) > stats.Range))
                {
                    target = default;
                }

                if (tower == null && target != default && target.Kind == EnemyKind.Boss)
                {
                    foreach (var enemy in available)
                    {
                        if (enemy.Kind != EnemyKind.Boss)
                        {
                            target = default;
                            break;
                        }
                    }
                }

                if (target == default && available.Count > 0)
                {
                    if (acquired && order.Tiebreak == TargetOrder.MostPierced)
                    {
                        target = MostPiercedTarget(position, stats.Range, available, order);
                    }
                    else
                    {
                        foreach (var enemy in available)
                        {
                            if (target == default)
                            {
                                target = enemy;
                                continue;
                            }
                            // 사거리가 비었다 다시 들어오면 가장 먼저 들어온 적 (기술병은 보스를 뒤로).
                            int compare =
                                acquired ? order.Compare(enemy, target)
                                : tower == null ? CompareInitialTarget(enemy, target)
                                : enemy.Id.CompareTo(target.Id);
                            if (compare < 0)
                            {
                                target = enemy;
                            }
                        }
                    }

                    acquired = true;
                }
                float rate = stats.Rate * rateMul;
                cooldown -= dt;
                if (available.Count == 0 || rate <= 0)
                {
                    cooldown = Mathf.Max(0, cooldown);
                    return;
                }
                if (cooldown > 0)
                    return;

                var targets = work.Targets;
                int targetCount = 1;
                if (!multi)
                {
                    targets[0] = target;
                }
                else
                {
                    targetCount = 0;
                    // Stable insertion keeps the first three entries in the original full sort.
                    foreach (var enemy in available)
                    {
                        int index = 0;
                        while (index < targetCount && order.Compare(enemy, targets[index]) >= 0)
                        {
                            index++;
                        }

                        if (index >= 3)
                            continue;

                        for (int i = Math.Min(targetCount, 2); i > index; i--)
                        {
                            targets[i] = targets[i - 1];
                        }

                        targets[index] = enemy;
                        targetCount = Math.Min(targetCount + 1, 3);
                    }
                }
                while (cooldown <= .00001f)
                {
                    int launched = 0;
                    float crit =
                        tower != null
                            ? RollCrit(ref tower.Attacks, ref tower.CritBonus)
                            : RollCrit(ref playerAttacks, ref playerCritBonus);
                    var payload = LaunchPayload(tower, stats, buff, crit);
                    if (crit > 1)
                    {
                        RaiseCue("crit", position);
                    }

                    if (tower != null && tower.Kind == TowerKind.Flame)
                    {
                        RaiseCue("flame_shot", position);
                    }

                    if (tower != null && tower.Kind == TowerKind.Rail)
                    {
                        RaiseCue("rail_shot", position);
                    }

                    for (int i = 0; i < targetCount; i++)
                    {
                        var e = targets[i];
                        if (e == default)
                            continue;

                        // One projectile per target.
                        {
                            var projectile = RentProjectile(
                                e,
                                position,
                                stats.Damage * (1 + buff / 100f) * crit,
                                stats.ProjectileSpeed,
                                source
                            );
                            projectile.Slot.Payload = payload;
                            // 관통(정본): 대상을 따라가지 않고 사거리 끝까지 직진. 레일은 전부, 5 꿰뚫는 탄은 1+N명.
                            if (payload.Rail || payload.ExtraPierce > 0)
                            {
                                var slot = projectile.Slot;
                                var direction = e.Position - position;
                                slot.Line = true;
                                slot.Direction =
                                    direction.sqrMagnitude > .000001f ? direction.normalized : Vector2.right;
                                slot.Remaining = slot.Range = stats.Range;
                                slot.HitsLeft = payload.Rail ? -1 : 1 + payload.ExtraPierce;
                                slot.Target = default;
                            }
                            launched++;
                            var onProjectile = ProjectileLaunched;
                            if (onProjectile != null)
                            {
                                rangeIndex.Invalidate();
                                supportCache.Dirty = true;
                                try
                                {
                                    onProjectile(new ProjectileSnapshot(projectile));
                                }
                                finally
                                {
                                    rangeIndex.Invalidate();
                                    supportCache.Dirty = true;
                                }
                            }
                            if (disposed)
                                return;
                        }
                    }
                    var onAttack = AttackLaunched;
                    if (launched > 0 && onAttack != null)
                    {
                        rangeIndex.Invalidate();
                        supportCache.Dirty = true;
                        try
                        {
                            onAttack(source, launched);
                        }
                        finally
                        {
                            rangeIndex.Invalidate();
                            supportCache.Dirty = true;
                        }
                    }
                    if (disposed)
                        return;

                    float nextCooldown = cooldown + 1 / rate;
                    if (nextCooldown <= cooldown)
                    {
                        throw new InvalidOperationException("Attack rate exceeds floating-point timing precision");
                    }

                    cooldown = nextCooldown;
                }
            }
            finally
            {
                work.Clear();
                if (shotWorkPool.Count < MaxRetainedWorkBuffers)
                {
                    shotWorkPool.Push(work);
                }
            }
        }

        TargetOrder TowerOrder(TowerState tower) =>
            Data.Towers.TryGetValue(tower.Kind, out var spec) && spec.TargetOrder != null
                ? spec.TargetOrder
                : TargetOrder.Common;

        // 레일 '가장 많이 꿰뚫는 방향': 최고 순위 종류 중 그 적 방향 직선(사거리, 판정 폭 LineHitRadius)에 걸리는 적 수가 가장 많은 적.
        // 같으면 코어에 가까운 적 → ID (2026-10-28 코드 반영 때 정함).
        // O(available²): each candidate direction counts every other enemy on its line (F22).
        EnemyHandle MostPiercedTarget(Vector2 position, float range, List<EnemyHandle> available, TargetOrder order)
        {
            int bestRank = int.MaxValue;
            foreach (var e in available)
            {
                bestRank = Math.Min(bestRank, order.Rank(e.Kind));
            }

            EnemyHandle best = default;
            int bestCount = -1;
            foreach (var e in available)
            {
                if (order.Rank(e.Kind) != bestRank)
                    continue;

                int count = PiercedCount(position, e.Position - position, range);
                if (best == default || count > bestCount || count == bestCount && order.Compare(e, best) < 0)
                {
                    best = e;
                    bestCount = count;
                }
            }
            return best;
        }

        internal int PiercedCount(Vector2 from, Vector2 direction, float range)
        {
            if (direction.sqrMagnitude <= .000001f)
            {
                direction = Vector2.right;
            }

            var d = direction.normalized;
            int count = 0;
            foreach (var e in Enemies)
            {
                if (!e.Alive)
                    continue;

                var offset = e.Position - from;
                float t = Vector2.Dot(offset, d);
                if (t < 0 || t > range)
                    continue;

                if ((offset - d * t).magnitude <= LineHitRadius)
                {
                    count++;
                }
            }
            return count;
        }

        static int CompareInitialTarget(EnemyHandle a, EnemyHandle b)
        {
            int order = (a.Kind == EnemyKind.Boss ? 1 : 0).CompareTo(b.Kind == EnemyKind.Boss ? 1 : 0);
            if (order != 0)
                return order;

            return a.Id.CompareTo(b.Id);
        }

        public int ApplyDamage(EnemyHandle target, float damage, string source)
        {
            if (
                disposed
                || target == default
                || !ReferenceEquals(target.Owner, this)
                || !target.IsValid
                || !target.Alive
            )
            {
                return 0;
            }

            BeginModelOperation();
            try
            {
                var payload = source == "Player" ? LaunchPayload(null, Player, 0, 1) : default;
                int dealt = Strike(target, damage, source, payload);
                if (operationDepth == 1)
                {
                    FlushSpawns();
                }

                return dealt;
            }
            finally
            {
                EndModelOperation();
            }
        }
    }
}
