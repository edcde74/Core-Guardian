using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreGuardian
{
    // Effect tags, enemy abilities, area blast and augment combat hooks.
    // Rules: Notion 전투 피해 계산 규칙 · 적 유형 및 보스 설계 · 타워 시스템 · 증강 시스템 (2026-10-26/27).
    // Session: fire cells (22), area blast (32001), teleport blast (46), crits.
    public sealed partial class Session
    {
        public Dictionary<Vector2Int, float>.Enumerator FireCellEnumerator() => fireCells.GetEnumerator();

        public void NotifyPlayerStep(Vector2Int left)
        {
            if (!Augments.Has(22) || Phase != SessionPhase.Combat)
                return;

            float seconds = Augments.Value(22) / Augments.Count(22);
            foreach (var d in Sides) // same order: up, down, left, right (F15)
            {
                var c = left + d;
                if (pathCells.Contains(c))
                {
                    fireCells[c] = seconds;
                }
            }
        }

        void TickFire(float dt)
        {
            if (fireCells.Count == 0)
            {
                fireTick = 0;
                return;
            }
            fireWork.Clear();
            fireWork.AddRange(fireCells.Keys);
            foreach (var c in fireWork)
            {
                float t = fireCells[c] - dt;
                if (t <= 0)
                {
                    fireCells.Remove(c);
                }
                else
                {
                    fireCells[c] = t;
                }
            }
            fireTick += dt;
            if (fireTick < 1)
                return;

            fireTick -= 1;
            FlameBurn(out float perStack, out float duration);
            if (perStack <= 0)
                return;

            int stacks = (int)Mathf.Max(1, Augments.Value(22, 1) / Mathf.Max(1, Augments.Count(22)));
            foreach (var enemy in Enemies)
            {
                if (
                    enemy.Alive
                    && enemy.Slot.Route == 0
                    && fireCells.ContainsKey(Vector2Int.RoundToInt(enemy.Position))
                )
                {
                    AddBurn(enemy.Slot, perStack, duration, stacks);
                }
            }
        }

        public bool UseAreaBlast(Vector2 playerPosition)
        {
            var skill = AreaBlast;
            if (skill == null || Phase != SessionPhase.Combat || areaBlastCooldown > 0)
                return false;

            BeginModelOperation();
            try
            {
                areaBlastCooldown = skill.Cooldown;
                float damage = Player.Damage * skill.DamageMul;
                var payload = new HitPayload { Player = true };
                if (Augments.Has(43))
                {
                    payload.Execute = tags.ExecuteBase * Augments.Value(43) / Augments.Count(43);
                }

                blastWork.Clear();
                foreach (var e in Enemies)
                {
                    if (e.Alive && Vector2.Distance(e.Position, playerPosition) <= skill.Radius)
                    {
                        blastWork.Add(e);
                    }
                }

                // 44 확정 타격 (10-30 결정): 폭발 1번 = 공격 1회. 치명타는 한 번 굴려 맞은 적 모두에 같이 적용.
                float crit = RollCrit(ref playerAttacks, ref playerCritBonus);
                payload.Crit = crit;
                foreach (var e in blastWork)
                {
                    if (crit > 1)
                    {
                        RaiseCue("crit", e.Position);
                    }

                    Strike(e, damage * crit, "PlayerSkill", payload);
                }
                RaiseCue("area_blast", playerPosition);
                AreaBlastUsed?.Invoke(playerPosition, skill.Radius);
                FlushSpawns();
                RemoveDeadEnemies();
            }
            finally
            {
                EndModelOperation();
                FlushCues();
            }
            return true;
        }

        // 46 순간 이동 도착 폭발: 일반 범위 폭발과 같이 취급(반지름·43 처형·25 쿨감·기술병 누적), 피해 = value2 배율. 쿨다운은 새로 걸지 않음.
        public bool UseTeleportBlast(Vector2 position)
        {
            var skill = AreaBlast;
            if (skill == null || Phase != SessionPhase.Combat || !Augments.Has(46))
                return false;

            float scale = Augments.Value(46, 1) / Mathf.Max(1, Augments.Count(46));
            if (scale <= 0)
                return false;

            BeginModelOperation();
            try
            {
                float damage = Player.Damage * skill.DamageMul * scale;
                var payload = new HitPayload { Player = true };
                if (Augments.Has(43))
                {
                    payload.Execute = tags.ExecuteBase * Augments.Value(43) / Augments.Count(43);
                }

                blastWork.Clear();
                foreach (var e in Enemies)
                {
                    if (e.Alive && Vector2.Distance(e.Position, position) <= skill.Radius)
                    {
                        blastWork.Add(e);
                    }
                }

                // 44 확정 타격 (10-30 결정): 폭발 1번 = 공격 1회. 치명타는 한 번 굴려 맞은 적 모두에 같이 적용.
                float crit = RollCrit(ref playerAttacks, ref playerCritBonus);
                payload.Crit = crit;
                foreach (var e in blastWork)
                {
                    if (crit > 1)
                    {
                        RaiseCue("crit", e.Position);
                    }

                    Strike(e, damage * crit, "PlayerSkill", payload);
                }
                RaiseCue("area_blast", position);
                AreaBlastUsed?.Invoke(position, skill.Radius);
                FlushSpawns();
                RemoveDeadEnemies();
            }
            finally
            {
                EndModelOperation();
                FlushCues();
            }
            return true;
        }

        // Rolled once per attack at launch. Over-cap chance adds to the multiplier (130% → ×2.3).
        float RollCrit(ref int attacks, ref float bonus)
        {
            attacks++;
            float chance = tags.CritBase + Augments.CritChance + bonus;
            bonus = 0;
            bool forced = Augments.Has(44) && attacks % (int)Mathf.Max(1, Augments.Value(44) / Augments.Count(44)) == 0;
            if (chance <= 0 && !forced)
                return 1;

            bool crit = forced || combatRandom.NextDouble() * 100 < Mathf.Min(chance, tags.CritCap);
            if (!crit)
                return 1;

            if (Augments.Has(26))
            {
                bonus = Augments.Value(26, 1) / Augments.Count(26);
            }

            return tags.CritMul + Augments.CritBonusPercent / 100f + Mathf.Max(0, chance - 100) / 100f;
        }
    }
}
