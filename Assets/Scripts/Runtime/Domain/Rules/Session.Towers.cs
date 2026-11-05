using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CoreGuardian
{
    // Session: tower lookup, support cache, install/merge/stars, upgrades and prices.
    public sealed partial class Session
    {
        struct TowerStamp
        {
            public TowerState Tower;
            public int Id;
        }

        public TowerState FindTower(int id)
        {
            bool matches = towerStamps.Count == Towers.Count;
            for (int i = 0; matches && i < Towers.Count; i++)
            {
                matches = ReferenceEquals(towerStamps[i].Tower, Towers[i]) && towerStamps[i].Id == Towers[i].Id;
            }

            if (!matches)
            {
                towerStamps.Clear();
                towerLookup.Clear();
                foreach (var tower in Towers)
                {
                    towerStamps.Add(new TowerStamp { Tower = tower, Id = tower.Id });
                    if (!towerLookup.ContainsKey(tower.Id))
                    {
                        towerLookup.Add(tower.Id, tower);
                    }
                }
            }
            return towerLookup.TryGetValue(id, out var result) ? result : null;
        }

        public TowerState TowerFromSource(string source)
        {
            return TryTowerSourceId(source, out int id) ? FindTower(id) : null;
        }

        public bool TryTowerSourceId(string source, out int id)
        {
            id = 0;
            if (source == default || !source.StartsWith("Tower:"))
                return false;

            if (!sourceIds.TryGetValue(source, out id))
            {
                if (!int.TryParse(source.Substring(6), out id))
                    return false;
                // Only unsigned ASCII decimal spellings are culture independent.
                bool cacheable = source.Length > 6;
                for (int i = 6; cacheable && i < source.Length; i++)
                {
                    cacheable = source[i] >= '0' && source[i] <= '9';
                }

                if (cacheable)
                {
                    if (sourceIds.Count >= 512)
                    {
                        sourceIds.Clear();
                    }
                    sourceIds.Add(source, id);
                }
            }
            return true;
        }

        struct SupportStamp
        {
            public TowerState Tower;
            public TowerKind Kind;
            public Vector2Int Position;
            public float Range,
                Damage;
        }

        struct SupportResult
        {
            public float Damage;
            public bool InRange;
        }

        // Rebuilt only when towers change; a rebuild is O(towers²) (≤32 towers) (F22).
        sealed class SupportCache
        {
            readonly List<SupportStamp> stamps = new List<SupportStamp>();
            readonly Dictionary<Vector2Int, SupportResult> results = new Dictionary<Vector2Int, SupportResult>();
            public bool Dirty = true;

            public void Refresh(List<TowerState> towers)
            {
                bool matches = stamps.Count == towers.Count;
                for (int i = 0; matches && i < towers.Count; i++)
                {
                    var tower = towers[i];
                    var stamp = stamps[i];
                    matches = ReferenceEquals(stamp.Tower, tower) && stamp.Kind == tower.Kind;
                    if (matches && tower.Kind == TowerKind.Support)
                    {
                        matches =
                            stamp.Position == tower.Position
                            && stamp.Range.Equals(tower.Stats.Range)
                            && stamp.Damage.Equals(tower.Stats.BuffDamage);
                    }
                }
                if (!matches)
                {
                    stamps.Clear();
                    results.Clear();
                    foreach (var tower in towers)
                    {
                        var stamp = new SupportStamp { Tower = tower, Kind = tower.Kind };
                        if (tower.Kind == TowerKind.Support)
                        {
                            stamp.Position = tower.Position;
                            stamp.Range = tower.Stats.Range;
                            stamp.Damage = tower.Stats.BuffDamage;
                        }
                        stamps.Add(stamp);
                    }
                }
                Dirty = false;
            }

            public SupportResult Get(List<TowerState> towers, Vector2Int position)
            {
                if (Dirty)
                {
                    Refresh(towers);
                }

                if (results.TryGetValue(position, out var cached))
                    return cached;

                double damage = 0;
                bool inRange = false;
                foreach (var stamp in stamps)
                {
                    if (stamp.Kind == TowerKind.Support && Vector2.Distance(stamp.Position, position) <= stamp.Range)
                    {
                        damage += stamp.Damage;
                        inRange = true;
                    }
                }

                var result = new SupportResult { Damage = (float)damage, InRange = inRange };
                if (results.Count >= 256)
                {
                    results.Clear();
                }

                results.Add(position, result);
                return result;
            }
        }

        public bool HasTowerSupport(TowerState tower)
        {
            supportCache.Refresh(Towers);
            return supportCache.Get(Towers, tower.Position).InRange;
        }

        int TowerStep(TowerKind kind, string stat) =>
            towerUpgradeSteps.TryGetValue(kind, out var steps) && steps.TryGetValue(stat, out var n) ? n : 0;

        /// <summary>Lab upgrade steps bought for this tower kind and stat (shared by every tower of the kind).</summary>
        public int TowerUpgradeStep(TowerKind kind, string stat) => TowerStep(kind, stat);

        public int InstallCost(TowerKind kind)
        {
            float cost = Data.Towers[kind].Cost;
            if (Augments.Has(34))
            {
                cost *=
                    Augments.Rounds34 > 0 ? 1 - Augments.Value(34, 1) / Augments.Count(34) / 100f
                    : Augments.Penalty34 > 0 ? 1 + Augments.Value(34, 2) / Augments.Count(34) / 100f
                    : 1;
            }
            return Mathf.Max(0, Mathf.FloorToInt(cost + .5f));
        }

        // The one placement rule (B16): choosing a tower, the green/red preview and the final install all ask here.
        public bool CanChooseTower(TowerKind kind, out string reason)
        {
            reason = null;
            if (!CanBuildNow)
            {
                reason = "지금은 설치할 수 없습니다.";
            }
            else if (Crystals < InstallCost(kind))
            {
                reason = "수정 부족 · 필요 " + InstallCost(kind) + "수정";
            }
            else if (kind == TowerKind.Support && !Towers.Any(t => t.Kind != TowerKind.Support))
            {
                reason = "공격 포탑을 먼저 설치하세요.";
            }

            return reason == null;
        }

        public bool CanInstall(TowerKind kind, Vector2Int cell, out string reason)
        {
            if (!CanChooseTower(kind, out reason))
                return false;

            if (!IsBuildable(cell))
            {
                reason = "이 칸에는 설치할 수 없습니다.";
            }

            return reason == null;
        }

        public TowerState TryInstall(TowerKind kind, Vector2Int cell, out string reason)
        {
            if (!CanInstall(kind, cell, out reason))
                return null;

            var s = Data.Towers[kind];
            int cost = InstallCost(kind);
            var t = new TowerState
            {
                Id = nextTower++,
                Kind = kind,
                Position = cell,
                Spent = cost,
                Stats = new CombatStats
                {
                    Damage = s.Damage,
                    Rate = s.Rate,
                    Range = s.Range,
                    ProjectileSpeed = s.ProjectileSpeed,
                    BuffDamage = s.BuffDamage,
                    SlowPercent = s.SlowPercent,
                    SlowDuration = s.SlowDuration,
                    BurnPerStack = s.BurnPerStack,
                    BurnDuration = s.BurnDuration,
                    PierceFalloff = s.PierceFalloff,
                },
            };
            foreach (var u in Data.Upgrades.Where(u => u.Target == kind.ToString()))
            {
                int step = TowerStep(kind, UpgradeKey(u));
                if (step == 0)
                    continue;

                t.Stats.Add(u.Stat, u.Delta * step);
                t.Stats.UpgradeSteps[u.Stat] = step;
            }
            ClampTowerStats(t);
            Crystals -= cost;
            Towers.Add(t);
            LastMessage = "포탑 설치";
            if (Augments.Has(45))
            {
                t = TryMerge(t);
            }

            return t;
        }

        public bool DestroyTower(int id)
        {
            if (Phase == SessionPhase.Augmentation)
                return false;
            var t = Towers.FirstOrDefault(x => x.Id == id);
            if (t == null)
                return false;
            Towers.Remove(t);
            int refund = t.Spent * RefundPercent / 100;
            Crystals += refund;
            LastMessage = "파괴 +" + refund + " 수정";
            return true;
        }

        TowerState TryMerge(TowerState placed)
        {
            var current = placed;
            for (int guard = 0; guard < 3; guard++)
            {
                if (current.Stars >= 3)
                    break;

                TowerState partner = null;
                foreach (var d in Sides)
                {
                    var other = Towers.FirstOrDefault(x =>
                        x != current
                        && x.Kind == current.Kind
                        && x.Stars == current.Stars
                        && x.Position == current.Position + d
                    );
                    if (other != null)
                    {
                        partner = other;
                        break;
                    }
                }
                if (partner == null)
                    break;

                var keep = partner.Id < current.Id ? partner : current;
                var vacate = keep == partner ? current : partner;
                Towers.Remove(vacate);
                keep.Spent += vacate.Spent;
                SetStars(keep, keep.Stars + 1);
                RaiseCue("star_merge", keep.Position);
                LastMessage = "포탑 합체 ★" + keep.Stars;
                current = keep;
            }
            return current;
        }

        float StarDamage(int stars) =>
            stars >= 3 ? Data.Setting("starDamage3", 3.2f)
            : stars == 2 ? Data.Setting("starDamage2", 1.8f)
            : 1;

        float StarRange(int stars) =>
            stars >= 3 ? Data.Setting("starRange3", 1)
            : stars == 2 ? Data.Setting("starRange2", .5f)
            : 0;

        void SetStars(TowerState t, int stars)
        {
            float ratio = StarDamage(stars) / StarDamage(t.Stars);
            float range = StarRange(stars) - StarRange(t.Stars);
            t.Stats.Damage *= ratio;
            t.Stats.BuffDamage *= ratio;
            t.Stats.BurnPerStack *= ratio;
            t.Stats.Range += range;
            t.Stars = stars;
        }

        void ClampTowerStats(TowerState t)
        {
            t.Stats.PierceFalloff = Mathf.Min(1, t.Stats.PierceFalloff);
            if (t.Stats.SlowPercent > 0)
            {
                t.Stats.SlowPercent = Mathf.Min(t.Stats.SlowPercent, tags.SlowMax);
            }
        }

        // Grouped upgrades (21041 + 21046) share the step of their group leader's stat.
        string UpgradeKey(UpgradeSpec u)
        {
            if (u.Group == 0 || u.Group == u.Id)
                return u.Stat;
            foreach (var g in Data.Upgrades)
            {
                if (g.Id == u.Group)
                    return g.Stat;
            }
            return u.Stat;
        }

        UpgradeSpec FindUpgrade(string target, string stat)
        {
            foreach (var u in Data.Upgrades)
            {
                if (u.Target == target && u.Stat == stat)
                    return u;
            }
            return null;
        }

        public IEnumerable<string> UpgradeStats(TowerKind kind)
        {
            foreach (var u in Data.Upgrades)
            {
                if (u.Target == kind.ToString() && UpgradeKey(u) == u.Stat)
                {
                    yield return u.Stat;
                }
            }
        }

        static int Step(CombatStats s, string stat)
        {
            int n;
            return s.UpgradeSteps.TryGetValue(stat, out n) ? n : 0;
        }

        public int PlayerUpgradePrice(string stat)
        {
            return FindUpgrade("Player", stat) == null ? -1 : Data.PlayerUpgradePrice(Step(Player, stat) + 1);
        }

        public int TowerUpgradePrice(int id, string stat)
        {
            TowerState t = null;
            foreach (var x in Towers)
            {
                if (x.Id == id)
                {
                    t = x;
                    break;
                }
            }
            if (t == null)
                return -1;
            return TowerKindUpgradePrice(t.Kind, stat);
        }

        // 포탑 연구소: 종류별 공유 강화는 그 종류 포탑이 없어도 살 수 있음(이후 설치분에 적용).
        public int TowerKindUpgradePrice(TowerKind kind, string stat)
        {
            var u = FindUpgrade(kind.ToString(), stat);
            int step = TowerStep(kind, stat);
            return u == null ? -1 : u.Cost + u.CostIncrement * step - (step == 0 ? Data.TowerFirstUpgradeDiscount : 0);
        }

        /// <summary>True when buying this kind-wide upgrade would change nothing (둔화 상한·관통 감쇠 1).</summary>
        public bool TowerKindStatMaxed(TowerKind kind, string stat)
        {
            var st = TowerKindStats(kind);
            switch (stat)
            {
                case "slowPercent":
                    return tags.SlowMax > 0 && st.SlowPercent >= tags.SlowMax;
                case "pierceFalloff":
                    return st.PierceFalloff >= 1;
                default:
                    return false;
            }
        }

        // Base + shared upgrades for one kind (no star scaling, no support buff).
        public CombatStats TowerKindStats(TowerKind kind)
        {
            var s = Data.Towers[kind];
            var st = new CombatStats
            {
                Damage = s.Damage,
                Rate = s.Rate,
                Range = s.Range,
                ProjectileSpeed = s.ProjectileSpeed,
                BuffDamage = s.BuffDamage,
                SlowPercent = s.SlowPercent,
                SlowDuration = s.SlowDuration,
                BurnPerStack = s.BurnPerStack,
                BurnDuration = s.BurnDuration,
                PierceFalloff = s.PierceFalloff,
            };
            foreach (var u in Data.Upgrades)
            {
                if (u.Target != kind.ToString())
                    continue;
                int step = TowerStep(kind, UpgradeKey(u));
                if (step == 0)
                    continue;
                st.Add(u.Stat, u.Delta * step);
                st.UpgradeSteps[u.Stat] = step;
            }
            // Same caps as the towers themselves (ClampTowerStats), so the lab shows the value in effect.
            st.PierceFalloff = Mathf.Min(1, st.PierceFalloff);
            if (st.SlowPercent > 0 && tags.SlowMax > 0)
            {
                st.SlowPercent = Mathf.Min(st.SlowPercent, tags.SlowMax);
            }
            return st;
        }

        public bool UpgradePlayer(string stat, out string reason)
        {
            var u = FindUpgrade("Player", stat);
            reason = null;
            if (u == null)
            {
                reason = "강화 불가";
            }
            else if (Phase != SessionPhase.Preparation && Phase != SessionPhase.Combat)
            {
                reason = "현재 단계에서 강화 불가";
            }
            else if (Gold < PlayerUpgradePrice(stat))
            {
                reason = "골드 부족";
            }
            if (reason != null)
                return false;
            Gold -= PlayerUpgradePrice(stat);
            Player.Add(stat, u.Delta);
            Player.UpgradeSteps[stat] = Step(Player, stat) + 1;
            LastMessage = "기술병 강화";
            return true;
        }

        public bool UpgradeTower(int id, string stat, out string reason)
        {
            var t = Towers.FirstOrDefault(x => x.Id == id);
            if (t == null)
            {
                reason = !CanBuildNow ? "지금은 강화 불가" : "강화 불가";
                return false;
            }
            return UpgradeTowerKind(t.Kind, stat, out reason);
        }

        // 포탑 연구소: 그 종류 포탑이 1기 이상 있어야 구매 (10-28 사용자 확정).
        public bool HasTowerKind(TowerKind kind)
        {
            foreach (var t in Towers)
            {
                if (t.Kind == kind)
                    return true;
            }
            return false;
        }

        public bool UpgradeTowerKind(TowerKind kind, string stat, out string reason)
        {
            var u = Data.Towers.ContainsKey(kind) ? FindUpgrade(kind.ToString(), stat) : null;
            reason = null;
            if (!CanBuildNow)
            {
                reason = "지금은 강화 불가";
            }
            else if (u == null)
            {
                reason = "강화 불가";
            }
            else if (!HasTowerKind(kind))
            {
                reason = "설치된 포탑 없음";
            }
            else if (TowerKindStatMaxed(kind, stat))
            {
                reason = "최대치";
            }
            else if (Crystals < TowerKindUpgradePrice(kind, stat))
            {
                reason = "수정 부족";
            }

            if (reason != null)
                return false;

            Crystals -= TowerKindUpgradePrice(kind, stat);
            int step = TowerStep(kind, stat) + 1;
            if (!towerUpgradeSteps.TryGetValue(kind, out var steps))
            {
                towerUpgradeSteps[kind] = steps = new Dictionary<string, int>();
            }

            steps[stat] = step;
            foreach (var member in Data.Upgrades)
            {
                if (member.Target != u.Target || member != u && UpgradeKey(member) != stat)
                    continue;

                foreach (var tower in Towers.Where(x => x.Kind == kind))
                {
                    bool scaled =
                        member.Stat == "damage" || member.Stat == "buffDamage" || member.Stat == "burnPerStack";
                    tower.Stats.Add(member.Stat, member.Delta * (scaled ? StarDamage(tower.Stars) : 1));
                    tower.Stats.UpgradeSteps[member.Stat] = step;
                    ClampTowerStats(tower);
                }
            }
            LastMessage = "같은 종류 포탑 전체 강화 · 이후 설치 포함";
            return true;
        }

        // 16 촘촘한 진지 (adjacent tower), 30 화력 지원 (player within N tiles).
        /// <summary>Damage buff % this tower fires with now: augments plus support towers in range (as in combat).</summary>
        public float TowerDamageBuffPercent(TowerState t) =>
            Augments.TowerDamagePercent + supportCache.Get(Towers, t.Position).Damage;

        /// <summary>Shots per second this tower fires now, including 16 촘촘한 진지 and 30 화력 지원.</summary>
        public float TowerFireRate(TowerState t, Vector2 playerPosition) =>
            t.Stats.Rate * (1 + TowerRatePercent(t, playerPosition) / 100f);

        // 16 촘촘한 진지 scans all towers per side: O(towers) per tower, O(towers²) per step (F22).
        float TowerRatePercent(TowerState t, Vector2 player)
        {
            float percent = 0;
            if (Augments.Has(16))
            {
                foreach (var d in Sides)
                {
                    bool found = false;
                    foreach (var o in Towers)
                    {
                        if (o != t && o.Position == t.Position + d)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found)
                    {
                        percent += Augments.Value(16);
                        break;
                    }
                }
            }

            if (Augments.Has(30) && Vector2.Distance(player, t.Position) <= Augments.Value(30) / Augments.Count(30))
            {
                percent += Augments.Value(30, 1);
            }

            return percent;
        }
    }
}
