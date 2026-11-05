using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CoreGuardian
{
    // Session: enemy range index and tower targeting for views.
    public sealed partial class Session
    {
        public EnemyHandle TowerTarget(int id)
        {
            var tower = FindTower(id);
            if (tower == null)
                return default;

            return TowerTarget(tower, null);
        }

        EnemyHandle TowerTarget(TowerState tower, EnemyRangeIndex index)
        {
            // Multi fires at its role-priority list, not the single-target acquisition cache.
            if (tower.Kind != TowerKind.Multi)
                return tower.Target;

            EnemyHandle best = default;
            var work = shotWorkPool.Count > 0 ? shotWorkPool.Pop() : new ShotWork();
            try
            {
                if (
                    index == null
                    || !index.TryCollect(
                        tower.Position,
                        tower.Stats.Range,
                        Enemies,
                        work.Available,
                        work.SpatialCandidates
                    )
                )
                {
                    foreach (var enemy in Enemies)
                    {
                        if (enemy.Alive && Vector2.Distance(enemy.Position, tower.Position) <= tower.Stats.Range)
                        {
                            work.Available.Add(enemy);
                        }
                    }
                }

                var order = TowerOrder(tower);
                foreach (var enemy in work.Available)
                {
                    if (best == default || order.Compare(enemy, best) < 0)
                    {
                        best = enemy;
                    }
                }

                return best;
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

        public void BeginTowerViews()
        {
            towerViewsPrepared = false;
            foreach (var tower in Towers)
            {
                if (tower.Kind == TowerKind.Multi)
                {
                    viewRangeIndex.Build(Enemies);
                    viewRevision = enemyRevision;
                    viewListVersion = Enemies.Version;
                    towerViewsPrepared = true;
                    break;
                }
            }
        }

        public void EndTowerViews()
        {
            towerViewsPrepared = false;
            viewRangeIndex.Clear();
        }

        public EnemyHandle TowerTargetForView(TowerState tower)
        {
            tower = FindTower(tower.Id);
            if (tower == null)
                return default;

            if (!towerViewsPrepared)
                return TowerTarget(tower, null);
            // Views can invoke external code between tower visits; re-validate only when an enemy write,
            // model operation or membership change happened since the index was last checked.
            if (viewRevision != enemyRevision || viewListVersion != Enemies.Version)
            {
                viewRangeIndex.Invalidate();
                viewRevision = enemyRevision;
                viewListVersion = Enemies.Version;
            }
            return TowerTarget(tower, viewRangeIndex);
        }

        struct RangeEntry
        {
            public EnemyHandle Enemy;
            public int Ordinal;
        }

        struct RangeStamp
        {
            public EnemyHandle Enemy;
            public Vector2 Position;
            public bool Alive;
        }

        sealed class EnemyRangeIndex
        {
            const float CellSize = 4,
                CoordinateLimit = 65536;
            const int MinimumEnemies = 32,
                MaximumQueryCells = 256,
                MaximumRetainedBuckets = 128;
            readonly Dictionary<Vector2Int, List<RangeEntry>> buckets = new Dictionary<Vector2Int, List<RangeEntry>>();
            readonly Stack<List<RangeEntry>> spareBuckets = new Stack<List<RangeEntry>>();
            readonly List<RangeStamp> stamps = new List<RangeStamp>();
            bool ready,
                needsValidation;
            int sourceCount;

            static bool Supported(Vector2 point) =>
                !float.IsNaN(point.x)
                && !float.IsNaN(point.y)
                && Mathf.Abs(point.x) <= CoordinateLimit
                && Mathf.Abs(point.y) <= CoordinateLimit;

            public void Clear()
            {
                ready = false;
                needsValidation = false;
                stamps.Clear();
                foreach (var bucket in buckets.Values)
                {
                    bucket.Clear();
                    if (spareBuckets.Count < MaximumRetainedBuckets)
                    {
                        spareBuckets.Push(bucket);
                    }
                }
                buckets.Clear();
                sourceCount = 0;
            }

            public void Invalidate()
            {
                needsValidation = true;
            }

            bool Matches(IReadOnlyList<EnemyHandle> enemies)
            {
                if (stamps.Count != enemies.Count)
                    return false;

                for (int i = 0; i < stamps.Count; i++)
                {
                    var stamp = stamps[i];
                    var enemy = enemies[i];
                    if (stamp.Enemy != enemy || stamp.Alive != enemy.Alive || !stamp.Position.Equals(enemy.Position))
                    {
                        return false;
                    }
                }
                return true;
            }

            public void Build(IReadOnlyList<EnemyHandle> enemies)
            {
                Clear();
                sourceCount = enemies.Count;
                if (sourceCount < MinimumEnemies)
                    return;

                for (int i = 0; i < enemies.Count; i++)
                {
                    var enemy = enemies[i];
                    stamps.Add(
                        new RangeStamp
                        {
                            Enemy = enemy,
                            Position = enemy.Position,
                            Alive = enemy.Alive,
                        }
                    );
                    if (!enemy.Alive)
                        continue;

                    if (!Supported(enemy.Position))
                        return;

                    var key = new Vector2Int(
                        Mathf.FloorToInt(enemy.Position.x / CellSize),
                        Mathf.FloorToInt(enemy.Position.y / CellSize)
                    );
                    if (!buckets.TryGetValue(key, out var bucket))
                    {
                        bucket = spareBuckets.Count > 0 ? spareBuckets.Pop() : new List<RangeEntry>();
                        buckets.Add(key, bucket);
                    }
                    bucket.Add(new RangeEntry { Enemy = enemy, Ordinal = i });
                }
                ready = true;
            }

            public bool TryCollect(
                Vector2 position,
                float range,
                IReadOnlyList<EnemyHandle> enemies,
                List<EnemyHandle> result,
                List<RangeEntry> candidates
            )
            {
                if (needsValidation)
                {
                    if (!Matches(enemies))
                    {
                        Build(enemies);
                    }
                    needsValidation = false;
                }
                if (!ready || !Supported(position) || float.IsNaN(range) || float.IsInfinity(range) || range < 0)
                {
                    return false;
                }

                double minX = Math.Floor(((double)position.x - range) / CellSize) - 1,
                    maxX = Math.Floor(((double)position.x + range) / CellSize) + 1;
                double minY = Math.Floor(((double)position.y - range) / CellSize) - 1,
                    maxY = Math.Floor(((double)position.y + range) / CellSize) + 1;
                double cells = (maxX - minX + 1) * (maxY - minY + 1);
                // One extra cell includes float-rounding/underflow boundary candidates. The
                // original float Distance remains the final predicate; unusual views fall back.
                if (
                    cells > MaximumQueryCells
                    || cells >= sourceCount
                    || minX < int.MinValue
                    || maxX >= int.MaxValue
                    || minY < int.MinValue
                    || maxY >= int.MaxValue
                )
                {
                    return false;
                }

                candidates.Clear();
                for (int y = (int)minY; y <= (int)maxY; y++)
                {
                    for (int x = (int)minX; x <= (int)maxX; x++)
                    {
                        if (buckets.TryGetValue(new Vector2Int(x, y), out var bucket))
                        {
                            foreach (var entry in bucket)
                            {
                                if (entry.Enemy.Alive && Vector2.Distance(entry.Enemy.Position, position) <= range)
                                {
                                    candidates.Add(entry);
                                }
                            }
                        }
                    }
                }

                candidates.Sort(rangeEntryOrder);
                foreach (var entry in candidates)
                {
                    result.Add(entry.Enemy);
                }

                return true;
            }
        }

        sealed class StepWork
        {
            public readonly List<WaveSpec> Rows = new List<WaveSpec>();
            public readonly ModelList<EnemyHandle> Enemies = new ModelList<EnemyHandle>();
            public readonly List<EnemyHandle> BuffSources = new List<EnemyHandle>();
            public readonly List<BuffAura> Auras = new List<BuffAura>();
            public readonly Dictionary<EnemyHandle, float> Boosts = new Dictionary<EnemyHandle, float>();
            public readonly List<TowerState> Towers = new List<TowerState>();
            public readonly ModelList<ProjectileHandle> Projectiles = new ModelList<ProjectileHandle>();
            public readonly Dictionary<ProjectileHandle, int> ProjectileRemovals =
                new Dictionary<ProjectileHandle, int>();
            public readonly EnemyRangeIndex RangeIndex = new EnemyRangeIndex();

            public void Clear()
            {
                Rows.Clear();
                Enemies.Clear();
                BuffSources.Clear();
                Auras.Clear();
                Boosts.Clear();
                Towers.Clear();
                Projectiles.Clear();
                ProjectileRemovals.Clear();
                RangeIndex.Clear();
            }
        }

        sealed class ShotWork
        {
            public readonly List<EnemyHandle> Available = new List<EnemyHandle>();
            public readonly EnemyHandle[] Targets = new EnemyHandle[3];
            public readonly List<RangeEntry> SpatialCandidates = new List<RangeEntry>();

            public void Clear()
            {
                Available.Clear();
                SpatialCandidates.Clear();
                Array.Clear(Targets, 0, Targets.Length);
            }
        }
    }
}
