using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CoreGuardian
{
    // 공격 대상 순서: 적 종류 순위(데이터의 적 ID 목록, 같은 종류는 같은 순위) → 같은 순위 안 기준.
    // 기준: LowestHealth(기술병) · HighestHealth(단일) · NearestCore(범위) · UnburnedNearestCore(화염) · MostPierced(레일, Session이 직선 관통 수를 셈).
    // 마지막은 코어까지 남은 경로 길이(PathRemaining) → ID.
    public sealed class TargetOrder
    {
        public const string LowestHealth = "LowestHealth",
            HighestHealth = "HighestHealth",
            NearestCore = "NearestCore",
            UnburnedNearestCore = "UnburnedNearestCore",
            MostPierced = "MostPierced";
        static readonly int KindCount = Enum.GetValues(typeof(EnemyKind)).Length;
        readonly int[] rank;
        public readonly string Tiebreak;
        public readonly int[] EnemyIds;

        TargetOrder(int[] rank, string tiebreak, int[] ids)
        {
            this.rank = rank;
            Tiebreak = tiebreak;
            EnemyIds = ids;
            // F23: the tiebreak name is resolved once; Compare runs for every pair of every target sort.
            mode =
                tiebreak == HighestHealth ? 1
                : tiebreak == UnburnedNearestCore ? 2
                : tiebreak == NearestCore || tiebreak == MostPierced ? 3
                : 0;
        }

        readonly int mode; // 0 = lowest health (default), 1 = highest health, 2 = unburned first, 3 = none

        public static readonly TargetOrder Common = FromKinds(
            new[]
            {
                EnemyKind.Healer,
                EnemyKind.Defense,
                EnemyKind.Buff,
                EnemyKind.Armored,
                EnemyKind.Flyer,
                EnemyKind.Splitter,
                EnemyKind.Attack,
                EnemyKind.Charger,
                EnemyKind.Boss,
            },
            LowestHealth,
            Array.Empty<int>()
        );

        static TargetOrder FromKinds(IList<EnemyKind> kinds, string tiebreak, int[] ids)
        {
            var r = new int[KindCount];
            for (int i = 0; i < r.Length; i++)
            {
                r[i] = -1;
            }

            int next = 0;
            // 분열 작은 개체는 분열체와 같은 순위 (정본): 목록에서 같은 종류로 취급.
            foreach (var listed in kinds)
            {
                var k = listed == EnemyKind.SplitChild ? EnemyKind.Splitter : listed;
                if (r[(int)k] < 0)
                {
                    r[(int)k] = next++;
                }
            }
            r[(int)EnemyKind.SplitChild] = r[(int)EnemyKind.Splitter];
            for (int i = 0; i < r.Length; i++)
            {
                if (r[i] < 0)
                {
                    r[i] = next;
                }
            }

            return new TargetOrder(r, tiebreak, ids);
        }

        public static TargetOrder Parse(
            string priority,
            string tiebreak,
            IReadOnlyDictionary<int, EnemySpec> enemies,
            string table
        )
        {
            switch (tiebreak)
            {
                case LowestHealth:
                case HighestHealth:
                case NearestCore:
                case UnburnedNearestCore:
                case MostPierced:
                    break;
                default:
                    throw new ArgumentException(table + ": unknown targetTiebreak " + tiebreak);
            }
            var ids = new List<int>();
            var kinds = new List<EnemyKind>();
            foreach (var part in priority.Split('|'))
            {
                if (
                    !int.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)
                    || !enemies.TryGetValue(id, out var e)
                )
                {
                    throw new ArgumentException(table + ": unknown enemy in targetPriority " + part);
                }

                ids.Add(id);
                kinds.Add(e.Kind);
            }
            return FromKinds(kinds, tiebreak, ids.ToArray());
        }

        public int Rank(EnemyKind kind) => rank[(int)kind];

        public int Compare(EnemyHandle a, EnemyHandle b)
        {
            int o = Rank(a.Kind).CompareTo(Rank(b.Kind));
            if (o != 0)
                return o;

            switch (mode)
            {
                case 1:
                    o = b.Health.CompareTo(a.Health);
                    break;
                case 2:
                    o = (a.BurnStacks > 0).CompareTo(b.BurnStacks > 0);
                    break;
                case 3:
                    o = 0;
                    break;
                default:
                    o = a.Health.CompareTo(b.Health);
                    break;
            }
            if (o != 0)
                return o;
            // PathRemaining: distance along the remaining route, never straight-line distance to core.
            o = a.PathRemaining.CompareTo(b.PathRemaining);
            return o != 0 ? o : a.Id.CompareTo(b.Id);
        }
    }
}
