using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreGuardian
{
    public enum AugmentationGrade
    {
        Silver,
        Gold,
        Platinum,
    }

    // One card face. Text and numbers come from augments.csv (증강 시스템, 2026-10-26 50장).
    public sealed class AugmentationCard
    {
        public readonly AugmentSpec Spec;
        public AugmentationGrade Grade => Spec.Grade;
        public int Number => Spec.Number;
        public string Id => Spec.Id.ToString();
        public string GradeName =>
            Grade == AugmentationGrade.Silver ? "실버"
            : Grade == AugmentationGrade.Gold ? "골드"
            : "플래티넘";
        public string Title => Spec.Name;
        public string Description => Spec.Text;

        public AugmentationCard(AugmentSpec spec)
        {
            Spec = spec ?? throw new ArgumentNullException(nameof(spec));
        }
    }

    public static class AugmentationDeck
    {
        public static AugmentationGrade RollGrade(double roll, float silver = 60, float gold = 30)
        {
            if (roll < 0 || roll >= 1 || double.IsNaN(roll))
            {
                throw new ArgumentOutOfRangeException(nameof(roll));
            }

            return roll < silver / 100.0 ? AugmentationGrade.Silver
                : roll < (silver + gold) / 100.0 ? AugmentationGrade.Gold
                : AugmentationGrade.Platinum;
        }

        // Left-to-right fill. Each slot rolls its grade, then picks a card of that grade by weight:
        // weight = product over subcategories of (1 + weight% x owned cards of that subcategory).
        public static AugmentationCard[] Draw(
            Random random,
            IReadOnlyList<AugmentSpec> cards,
            AugmentationEffects owned,
            int count,
            ICollection<int> previousScreen,
            bool firstPlatinum,
            float silver = 60,
            float gold = 30,
            float weightPercent = 25
        )
        {
            if (cards == null || cards.Count == 0)
                return Array.Empty<AugmentationCard>();

            var result = new List<AugmentationCard>(count);
            var weights = new List<double>();
            var pool = new List<AugmentSpec>();
            for (int slot = 0; slot < count; slot++)
            {
                var grade =
                    slot == 0 && firstPlatinum
                        ? AugmentationGrade.Platinum
                        : RollGrade(random.NextDouble(), silver, gold);
                pool.Clear();
                // 1) unseen this run, not on the previous screen; 2) allow previous picks that may repeat; 3) allow the previous screen.
                for (int pass = 0; pass < 3 && pool.Count == 0; pass++)
                {
                    foreach (var c in cards)
                    {
                        if (c.Grade != grade || result.Any(r => r.Spec.Id == c.Id))
                            continue;

                        bool picked = owned != null && owned.Count(c.Number) > 0;
                        if (picked && (pass == 0 || !c.Repeatable))
                            continue;

                        if (pass < 2 && previousScreen != null && previousScreen.Contains(c.Id))
                            continue;

                        pool.Add(c);
                    }
                }

                if (pool.Count == 0)
                    continue;

                weights.Clear();
                double total = 0;
                foreach (var c in pool)
                {
                    double w = 1;
                    foreach (var sub in c.Subcategories)
                    {
                        w *= 1 + weightPercent / 100.0 * (owned?.Subcategory(sub) ?? 0);
                    }
                    weights.Add(w);
                    total += w;
                }
                double roll = random.NextDouble() * total;
                int index = 0;
                for (; index < pool.Count - 1; index++)
                {
                    roll -= weights[index];
                    if (roll < 0)
                        break;
                }
                result.Add(new AugmentationCard(pool[index]));
            }
            return result.ToArray();
        }

        public static int BestIndex(IReadOnlyList<AugmentationCard> cards)
        {
            if (cards.Count == 0)
            {
                throw new ArgumentException("No cards");
            }

            int best = 0;
            for (int i = 1; i < cards.Count; i++)
            {
                if (cards[i].Grade > cards[best].Grade)
                {
                    best = i;
                }
            }

            return best;
        }
    }

    // Selected cards and their stacked numbers. Numeric cards add on repeat; rule cards appear once.
    public sealed class AugmentationEffects
    {
        readonly List<string> selected = new List<string>();
        readonly int[] picks = new int[51];
        readonly float[,] sums = new float[51, 3];
        readonly Dictionary<string, int> subcategories = new Dictionary<string, int>();
        public IReadOnlyList<string> Selected => selected.AsReadOnly();

        public int Count(int number) => number > 0 && number < picks.Length ? picks[number] : 0;

        public bool Has(int number) => Count(number) > 0;

        public float Value(int number, int index = 0) =>
            number > 0 && number < picks.Length && index < 3 ? sums[number, index] : 0;

        public int Subcategory(string name) => subcategories.TryGetValue(name, out int n) ? n : 0;

        // Run counters owned by the cards.
        public int Kills,
            Rounds12,
            Rounds18,
            Rounds32,
            Rounds34,
            CleanRounds;
        public bool PendingPlatinum;

        public void Apply(AugmentationCard card)
        {
            var spec = card.Spec;
            selected.Add(card.Id);
            picks[spec.Number]++;
            for (int i = 0; i < 3; i++)
            {
                sums[spec.Number, i] += spec.Value(i);
            }

            foreach (var sub in spec.Subcategories)
            {
                subcategories.TryGetValue(sub, out int n);
                subcategories[sub] = n + 1;
            }
            switch (spec.Number)
            {
                case 12:
                    Rounds12 += (int)spec.Value(0);
                    break;
                case 18:
                    Rounds18 += (int)spec.Value(1);
                    break;
                case 34:
                    Rounds34 += (int)spec.Value(0);
                    break;
                case 48:
                    PendingPlatinum = true;
                    break;
            }
        }

        // ---- Derived numbers (all from card values; no balance literals here).
        public float TowerDamagePercent => Value(1) + Value(19) + Value(36) * CleanRounds;
        public int TowerBuffPercent => (int)Math.Round(TowerDamagePercent);
        public float CritChance => Value(8);
        public float CritBonusPercent => Value(26);

        // 10 현상금 + 11 저금통 (per N kills, capped) - 18 빚내기 while its rounds remain.
        public float KillGoldPercent
        {
            get
            {
                float percent = Value(10);
                if (Has(11))
                {
                    float per = Value(11) / Count(11);
                    if (per > 0)
                    {
                        percent += Math.Min(Value(11, 2), (float)Math.Floor(Kills / per) * Value(11, 1));
                    }
                }
                if (Rounds18 > 0)
                {
                    percent -= Value(18, 2) / Count(18);
                }

                return percent;
            }
        }
        public int ChainCount => Count(6) + Count(24);
        public float PlayerRatePercent => Value(2, 1);
        public float ExtraRerolls => Value(17);
        public int Cards => Has(47) ? (int)(Value(47) / Count(47)) : 3;
    }
}
