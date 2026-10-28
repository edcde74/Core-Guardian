using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreGuardian
{
    // Effect tags, enemy abilities, area blast and augment combat hooks.
    // Rules: Notion 전투 피해 계산 규칙 · 적 유형 및 보스 설계 · 타워 시스템 · 증강 시스템 (2026-10-26/27).
    // Session: split children and summons, enemy abilities.
    public sealed partial class Session
    {
        // ================= Spawns (split children, summons) =================
        struct PendingSpawn
        {
            public EnemySpec Spec;
            public float Distance;
            public int Route,
                Summoner;
        }

        EnemySpec RoundAttack() => Data.EnemyForWave(Wave, EnemyKind.Attack);

        EnemySpec SplitChildSpec(EnemySpec parent)
        {
            if (roundSpecWave != Wave)
            {
                roundSpecs.Clear();
                roundSpecWave = Wave;
            }
            long key = ((long)parent.SplitIntoId << 20) ^ (parent.Id & 0xFFFFF);
            if (roundSpecs.TryGetValue(key, out var cached))
                return cached;

            var baseChild = Data.EnemiesById[parent.SplitIntoId];
            var attack = RoundAttack();
            var attackBase = Data.Enemies[EnemyKind.Attack];
            var child = baseChild.Clone();
            float growth = attackBase.Speed > 0 ? attack.Speed / attackBase.Speed : 1;
            if (baseChild.HealthMul > 0)
            {
                child.Health = Mathf.Max(1, Mathf.FloorToInt(attack.Health * baseChild.HealthMul + .5f));
            }

            if (baseChild.GoldMul > 0)
            {
                child.Gold = Mathf.FloorToInt(attack.Gold * baseChild.GoldMul + .5f);
            }

            child.Defense = parent.Defense;
            child.Speed = baseChild.Speed * growth;
            roundSpecs[key] = child;
            return child;
        }

        EnemySpec SummonSpec(EnemySpec lord)
        {
            if (roundSpecWave != Wave)
            {
                roundSpecs.Clear();
                roundSpecWave = Wave;
            }
            long key = ((long)lord.SummonId << 20) ^ 0xFFFFF;
            if (roundSpecs.TryGetValue(key, out var cached))
                return cached;

            var kind = Data.EnemiesById[lord.SummonId].Kind;
            var spec = Data.EnemyForWave(Wave, kind).Clone();
            spec.Gold = 0;
            roundSpecs[key] = spec;
            return spec;
        }

        void FlushSpawns()
        {
            if (pendingSpawns.Count == 0)
                return;

            // Same snapshot as ToArray, in a reused list (F15); a nested flush still gets its own copy.
            var list = spawnFlushing ? new List<PendingSpawn>(pendingSpawns) : spawnWork;
            if (!spawnFlushing)
            {
                spawnWork.Clear();
                spawnWork.AddRange(pendingSpawns);
            }
            pendingSpawns.Clear();
            bool outer = !spawnFlushing;
            spawnFlushing = true;
            try
            {
                foreach (var p in list)
                {
                    var h = RentEnemy(p.Spec.Kind, p.Spec, p.Distance, p.Route);
                    h.Slot.SummonerId = p.Summoner;
                }
            }
            finally
            {
                if (outer)
                {
                    spawnFlushing = false;
                    spawnWork.Clear();
                }
            }
        }

        readonly List<PendingSpawn> spawnWork = new List<PendingSpawn>();
        bool spawnFlushing;

        void TickAbilities(EnemyHandle h, float dt)
        {
            var e = h.Slot;
            var spec = e.Spec;
            if (e.ShieldTime > 0)
            {
                e.ShieldTime = Mathf.Max(0, e.ShieldTime - dt);
            }

            if (e.SlowFrostTime > 0)
            {
                e.SlowFrostTime = Mathf.Max(0, e.SlowFrostTime - dt);
            }

            if (e.HealMarkTime > 0)
            {
                e.HealMarkTime = Mathf.Max(0, e.HealMarkTime - dt);
            }

            if (spec.HealInterval > 0)
            {
                e.AbilityTimer += dt;
                if (e.AbilityTimer >= spec.HealInterval)
                {
                    e.AbilityTimer -= spec.HealInterval;
                    bool any = false;
                    foreach (var other in Enemies)
                    {
                        if (other == h || !other.Alive)
                            continue;

                        var o = other.Slot;
                        if (Vector2.Distance(o.Position, e.Position) > spec.HealRadius)
                            continue;
                        // Overlapping healers each heal (no per-target limit, 사용자 확정 10-27).
                        float percent = spec.HealPercent * (o.BurnStacks > 0 ? .5f : 1) * (IsBoss(o) ? .5f : 1);
                        int amount = Mathf.FloorToInt(o.Spec.Health * percent / 100f + .5f);
                        if (amount <= 0 || o.Health >= o.Spec.Health)
                            continue;

                        enemyRevision++;
                        o.Health = Mathf.Min(o.Spec.Health, o.Health + amount);
                        o.HealMarkTime = HealMarkSeconds;
                        any = true;
                    }
                    if (any)
                    {
                        RaiseCue("heal_pulse", e.Position);
                    }
                }
            }
            if (spec.SummonId != 0 && spec.SummonCount > 0)
            {
                e.AbilityTimer += dt;
                float interval = h.Enraged ? spec.SummonRageInterval : spec.SummonInterval;
                if (e.AbilityTimer >= interval)
                {
                    e.AbilityTimer -= interval;
                    // No cap on living summons (사용자 확정 10-27): every cycle summons the full count.
                    {
                        var summoned = SummonSpec(spec);
                        for (int i = 0; i < spec.SummonCount; i++)
                        {
                            pendingSpawns.Add(
                                new PendingSpawn
                                {
                                    Spec = summoned,
                                    Distance = e.PathTravelled + (i % 2 == 0 ? .3f : -.3f),
                                    Route = e.Route,
                                    Summoner = e.Id,
                                }
                            );
                        }

                        RaiseCue("summon", e.Position);
                    }
                }
            }
        }
    }
}
