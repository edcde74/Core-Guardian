using System.Linq;
using CoreGuardian;
using NUnit.Framework;
using UnityEngine;

// 10-31 확정: 증강 리롤은 바꾸는 카드와 같은 등급 안에서 다시 뽑는다(실버는 실버끼리).
public class CoreGuardianDecision1031Tests
{
    static string Csv(string key)
    {
        var asset = Resources.Load<TextAsset>("CoreGuardian/" + key);
        Assert.That(asset, Is.Not.Null, "Missing CSV " + key);
        return asset.text;
    }

    static Session Game(int seed)
    {
        var d = CombatData.Load(
            Csv("enemies"),
            Csv("towers"),
            Csv("stage"),
            Csv("upgrades"),
            Csv("waves"),
            Csv("player"),
            Csv("augments"),
            Csv("effectTags"),
            Csv("skills")
        );
        var s = new Session(d, seed);
        s.SetBoard(40, 25, new Vector2Int(10, 0), new[] { Vector2Int.zero, new Vector2Int(10, 0) });
        return s;
    }

    [Test]
    public void Reroll_KeepsTheCardGrade()
    {
        int checkedCards = 0;
        for (int seed = 1; seed <= 40; seed++)
        {
            var s = Game(seed);
            try
            {
                Assert.That(s.DebugOpenAugmentation(), Is.True);
                for (int i = 0; i < s.AugmentationChoices.Count; i++)
                {
                    var grade = s.AugmentationChoices[i].Grade;
                    if (s.RerollAugmentation(i))
                    {
                        Assert.That(s.AugmentationChoices[i].Grade, Is.EqualTo(grade), "seed " + seed + " card " + i);
                        checkedCards++;
                    }
                }
                var ids = s.AugmentationChoices.Select(c => c.Id).ToArray();
                Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Length));
            }
            finally
            {
                s.Dispose();
            }
        }
        Assert.That(checkedCards, Is.GreaterThan(100));
    }

    [Test]
    public void Draw_OnlyGrade_NeverRollsAnotherGrade()
    {
        var s = Game(7);
        try
        {
            var random = new System.Random(3);
            foreach (
                var grade in new[] { AugmentationGrade.Silver, AugmentationGrade.Gold, AugmentationGrade.Platinum }
            )
            {
                for (int i = 0; i < 200; i++)
                {
                    var one = AugmentationDeck.Draw(
                        random,
                        s.Data.Augments,
                        s.Augments,
                        1,
                        null,
                        false,
                        onlyGrade: grade
                    );
                    Assert.That(one.Length, Is.EqualTo(1));
                    Assert.That(one[0].Grade, Is.EqualTo(grade));
                }
            }
        }
        finally
        {
            s.Dispose();
        }
    }

    [Test]
    public void FixedEconomyCards_ScaleByTheTenRoundBlockWhenPicked()
    {
        var s = Game(3);
        try
        {
            var completed = typeof(Session).GetProperty("CompletedWave");
            int gold = s.Gold;
            Assert.That(s.DebugGrantAugmentation(18), Is.True);
            Assert.That(s.Gold - gold, Is.EqualTo(150), "1~10라운드: 1배");
            completed.SetValue(s, 12);
            gold = s.Gold;
            s.DebugGrantAugmentation(18);
            Assert.That(s.Gold - gold, Is.EqualTo(300), "11~20라운드: 2배");
            completed.SetValue(s, 21);
            s.DebugGrantAugmentation(32);
            Assert.That(s.Augments.MineIncome32, Is.EqualTo(30), "21~30라운드: 3배");
        }
        finally
        {
            s.Dispose();
        }
    }

    [Test]
    public void PenaltyCards_OnlyHurtForFiveRoundsAfterTheBonus()
    {
        var s = Game(4);
        try
        {
            int baseCost = s.InstallCost(TowerKind.Single);
            s.DebugGrantAugmentation(34);
            Assert.That(s.Augments.Rounds34, Is.EqualTo(2));
            Assert.That(s.Augments.Penalty34, Is.EqualTo(5));
            Assert.That(s.InstallCost(TowerKind.Single), Is.LessThan(baseCost));
            s.Augments.Rounds34 = 0;
            Assert.That(s.InstallCost(TowerKind.Single), Is.GreaterThan(baseCost));
            s.Augments.Penalty34 = 0;
            Assert.That(s.InstallCost(TowerKind.Single), Is.EqualTo(baseCost), "손해 기간이 끝나면 원래 값");
            s.DebugGrantAugmentation(12);
            Assert.That(s.Augments.Penalty12, Is.EqualTo(5));
        }
        finally
        {
            s.Dispose();
        }
    }
}
