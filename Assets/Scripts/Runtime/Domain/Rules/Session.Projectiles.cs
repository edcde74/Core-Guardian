using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CoreGuardian
{
    // Session: projectile movement and line (rail) hits.
    public sealed partial class Session
    {
        void MoveProjectiles(float dt, StepWork work)
        {
            var removals = work.ProjectileRemovals;
            try
            {
                foreach (var p in work.Projectiles)
                {
                    if (disposed)
                        return;

                    if (!p.Accessible)
                        continue;

                    if (p.Slot.Line)
                    {
                        if (MoveLine(p, dt))
                            continue;
                        removals.TryGetValue(p, out int finished);
                        removals[p] = finished + 1;
                        continue;
                    }
                    if (p.Target.Alive)
                    {
                        p.Slot.LastTarget = new EnemySnapshot(p.Target);
                        p.Slot.Position = Vector2.MoveTowards(p.Position, p.Target.Position, p.Speed * dt);
                        if (!(Vector2.Distance(p.Position, p.Target.Position) <= .00001f))
                            continue;

                        if (DamageApplied != null)
                        {
                            // Observers see prior removals, but still see the current shot.
                            // Remove after the callback using the original first-match rule:
                            // the observer may clear/reorder the list or re-enter Tick.
                            FlushProjectileRemovals(removals);
                            if (ProjectileHit(p))
                                continue;

                            RemoveProjectile(p);
                            continue;
                        }
                        if (ProjectileHit(p))
                            continue;
                    }
                    removals.TryGetValue(p, out int count);
                    removals[p] = count + 1;
                }
            }
            finally
            {
                FlushProjectileRemovals(removals);
            }
        }

        void FlushProjectileRemovals(Dictionary<ProjectileHandle, int> removals)
        {
            if (removals.Count == 0)
                return;

            int write = 0,
                count = Projectiles.Count;
            for (int read = 0; read < count; read++)
            {
                var projectile = Projectiles[read];
                if (projectile != default && removals.TryGetValue(projectile, out int remaining) && remaining > 0)
                {
                    removals[projectile] = remaining - 1;
                    ReleaseProjectileMembership(projectile);
                    continue;
                }
                if (write != read)
                {
                    Projectiles[write] = projectile;
                }

                write++;
            }
            if (write < count)
            {
                Projectiles.RemoveRange(write, count - write);
            }

            removals.Clear();
        }

        // Homing hit. Piercing shots (레일, 5 꿰뚫는 탄) fly as lines from launch and never home.
        bool ProjectileHit(ProjectileHandle p)
        {
            var slot = p.Slot;
            HitWithChain(p.Target, p.Damage, p.Source, slot.Payload);
            return false;
        }

        // Straight piercing flight (레일, 관통). Returns true while the shot is still flying.
        // O(enemies) per straight shot per step (F22).
        bool MoveLine(ProjectileHandle p, float dt)
        {
            var s = p.Slot;
            float step = Mathf.Min(s.Remaining, s.Speed * dt);
            Vector2 from = s.Position,
                to = from + s.Direction * step;
            lineHits.Clear();
            foreach (var e in Enemies)
            {
                if (!e.Alive || s.Hits.Contains(e.Id))
                    continue;

                Vector2 d = to - from;
                float t = d.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(e.Position - from, d) / d.sqrMagnitude) : 0;
                if (Vector2.Distance(from + d * t, e.Position) <= LineHitRadius)
                {
                    lineHits.Add(e);
                }
            }
            // Insertion sort by distance along the flight (few hits; no comparer allocation).
            for (int i = 1; i < lineHits.Count; i++)
            {
                var x = lineHits[i];
                float k = Vector2.Dot(x.Position - from, s.Direction);
                int j = i - 1;
                while (j >= 0 && Vector2.Dot(lineHits[j].Position - from, s.Direction) > k)
                {
                    lineHits[j + 1] = lineHits[j];
                    j--;
                }
                lineHits[j + 1] = x;
            }
            foreach (var e in lineHits)
            {
                if (s.HitsLeft == 0)
                    break;

                s.Hits.Add(e.Id);
                var payload = s.Payload;
                Strike(e, s.Damage, s.Source, payload);
                if (payload.Rail)
                {
                    RaiseCue("hit_rail", e.Position);
                }

                if (s.HitsLeft > 0)
                {
                    s.HitsLeft--;
                }
                // 관통: ×falloff per enemy passed; 29 crit → none.
                if (!payload.CritNoFalloff)
                {
                    s.Damage *= payload.PierceFalloff > 0 ? payload.PierceFalloff : 1;
                }

                if (disposed)
                    return false;
            }
            s.Position = to;
            s.Remaining -= step;
            if (s.HitsLeft == 0)
                return false;

            if (s.Remaining <= .00001f)
            {
                // 23 레일 반사: returns once along the same line; earlier victims can be hit again.
                if (s.Payload.Rail && Augments.Has(23) && !s.Reflected)
                {
                    s.Reflected = true;
                    s.Direction = -s.Direction;
                    s.Remaining = s.Range;
                    s.Hits.Clear();
                    return true;
                }
                return false;
            }
            return true;
        }
    }
}
