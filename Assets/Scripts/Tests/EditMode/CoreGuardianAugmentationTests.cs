using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using NUnit.Framework;
using UnityEngine;

public class CoreGuardianAugmentationTests
{
    static readonly Vector2 Away = new Vector2(100, 100);

    // Boss-gate fixtures use the legacy cadence (10 rounds + boss). Cards come from the live augments.csv.
    static CombatData Data()
    {
        var d = CombatData.Load(
            CoreGuardianLegacyData.Text("enemies"),
            CoreGuardianLegacyData.Text("towers"),
            CoreGuardianLegacyData.Text("stage"),
            CoreGuardianLegacyData.Text("upgrades"),
            CoreGuardianLegacyData.Text("waves")
        );
        d.AugmentationEvery = 10;
        d.AugmentationRequiresBoss = true;
        d.Waves.RemoveAll(w => w.Wave > 11);
        return d;
    }

    static Session Game(CombatData d)
    {
        var s = new Session(d, 42);
        s.SetBoard(40, 25, new Vector2Int(10, 0), new[] { Vector2Int.zero, new Vector2Int(10, 0) });
        return s;
    }

    static void Finish(Session s)
    {
        for (int i = 0; i < 1000 && s.Phase == SessionPhase.Combat; i++)
        {
            s.Tick(.05f, Away);
            foreach (var e in s.Enemies.ToArray())
            {
                s.ApplyDamage(e, 10000, "Test");
            }
        }
        Assert.That(s.Phase, Is.Not.EqualTo(SessionPhase.Combat));
    }

    static Session BeforeBoss(CombatData data = null)
    {
        var s = Game(data ?? Data());
        for (int i = 1; i <= 9; i++)
        {
            Assert.That(s.StartCombat(), Is.True);
            Finish(s);
        }
        Assert.That(s.Wave, Is.EqualTo(10));
        return s;
    }

    [Test]
    public void BossKillWaitsForAllSpawnsAwardsOnceAndBlocksPreparationUntilSelection()
    {
        var s = BeforeBoss();
        s.StartCombat();
        s.Tick(.8f, Away);
        var boss = s.Enemies.Single(e => e.Kind == EnemyKind.Boss);
        Assert.That(s.Enemies.Count(e => e.Kind == EnemyKind.Defense), Is.EqualTo(3));
        int g = s.Gold;
        s.ApplyDamage(boss, 10000, "Test");
        Assert.That(s.Gold - g, Is.EqualTo(120));
        s.Tick(.05f, Away);
        Assert.That(s.Phase, Is.EqualTo(SessionPhase.Combat));
        Finish(s);
        Assert.That(s.Phase, Is.EqualTo(SessionPhase.Augmentation));
        Assert.That(s.Wave, Is.EqualTo(11));
        Assert.That(s.Crystals, Is.EqualTo(755));
        s.Tick(10, Away);
        Assert.That(s.PreparationRemaining, Is.EqualTo(s.Data.PreparationSeconds));
        Assert.That(s.AugmentationRemaining, Is.EqualTo(20).Within(.01));
        string reason;
        Assert.That(s.StartCombat(), Is.False);
        Assert.That(s.UpgradePlayer("damage", out reason), Is.False);
        Assert.That(s.TryInstall(TowerKind.Single, new Vector2Int(20, 20), out reason), Is.Null);
        Assert.That(s.ChooseAugmentation(-1), Is.False);
        Assert.That(s.ChooseAugmentation(0), Is.True);
        Assert.That(s.ChooseAugmentation(0), Is.False);
        Assert.That(s.Phase, Is.EqualTo(SessionPhase.Preparation));
        Assert.That(s.PreparationRemaining, Is.EqualTo(s.Data.PreparationSecondsLong));
        s.StartCombat();
        Finish(s);
        Assert.That(s.Phase, Is.EqualTo(SessionPhase.Victory));
        Assert.That(s.CompletedWave, Is.EqualTo(11));
    }

    [Test]
    public void BossArrivalHasNoGoldOrAugmentationAndWaitsForOtherEnemies()
    {
        var d = Data();
        d.Waves.RemoveAll(w => w.Wave == 10);
        d.Waves.Add(
            new WaveSpec
            {
                Wave = 10,
                Kind = EnemyKind.Boss,
                Count = 1,
                Interval = 1,
            }
        );
        d.Waves.Add(
            new WaveSpec
            {
                Wave = 10,
                Kind = EnemyKind.Attack,
                Count = 1,
                Interval = 1,
                Start = 30,
            }
        );
        var s = BeforeBoss(d);
        int gold = s.Gold;
        s.StartCombat();
        s.Tick(17, Away);
        Assert.That(s.CoreEnergy, Is.EqualTo(10));
        Assert.That(s.BossKilled, Is.False);
        Assert.That(s.Gold, Is.EqualTo(gold));
        Assert.That(s.Phase, Is.EqualTo(SessionPhase.Combat));
        Finish(s);
        Assert.That(s.Phase, Is.EqualTo(SessionPhase.Preparation));
        Assert.That(s.AugmentationChoices, Is.Empty);
    }

    [Test]
    public void LethalBossArrivalDoesNotAwardRoundReward()
    {
        var d = Data();
        d.CoreEnergy = 10;
        var s = BeforeBoss(d);
        int crystals = s.Crystals;
        s.StartCombat();
        s.Tick(17, Away);
        Assert.That(s.Phase, Is.EqualTo(SessionPhase.Defeat));
        Assert.That(s.Crystals, Is.EqualTo(crystals));
        Assert.That(s.AugmentationChoices, Is.Empty);
    }

    static Session Field(int crystals = 1000)
    {
        var d = Data();
        d.Waves.Clear();
        d.Waves.Add(
            new WaveSpec
            {
                Wave = 1,
                Kind = EnemyKind.Attack,
                Count = 1,
                Interval = 1,
                Start = 1000,
            }
        );
        d.StartCrystals = crystals;
        var s = Game(d);
        return s;
    }

    [Test]
    public void BossAcceleratesAtHalfAndBossSlowIsHalvedAndCapped()
    {
        var s = Field();
        s.StartCombat();
        var e = s.DebugSpawn(EnemyKind.Boss);
        Assert.That(e.MovementSpeed, Is.EqualTo(.6f));
        s.ApplyDamage(e, e.Spec.Health - e.Spec.Health / 2 - 1, "Test");
        Assert.That(e.Enraged, Is.False);
        s.ApplyDamage(e, 1, "Test");
        Assert.That(e.Enraged, Is.True);
        Assert.That(e.MovementSpeed, Is.EqualTo(.78f).Within(.0001));
        Assert.That(s.DebugGrantAugmentation(3), Is.True);
        s.ApplyDamage(e, 1, "Player");
        Assert.That(e.SlowPercent, Is.EqualTo(7.5f).Within(.001), "Boss takes half of the 15% player slow");
        Assert.That(e.MovementSpeed, Is.EqualTo(.78f * .925f).Within(.0001));
    }

    [Test]
    public void GradeThresholdsFollowStageChancesAndTimeoutPicksHighestLeftmost()
    {
        Assert.That(AugmentationDeck.RollGrade(.599999), Is.EqualTo(AugmentationGrade.Silver));
        Assert.That(AugmentationDeck.RollGrade(.6), Is.EqualTo(AugmentationGrade.Gold));
        Assert.That(AugmentationDeck.RollGrade(.9), Is.EqualTo(AugmentationGrade.Platinum));
        var s = BeforeBoss();
        s.StartCombat();
        Finish(s);
        var best = s.AugmentationChoices[AugmentationDeck.BestIndex(s.AugmentationChoices)];
        s.Tick(30.01f, Away);
        Assert.That(s.Augments.Selected.Single(), Is.EqualTo(best.Id));
        Assert.That(s.Phase, Is.EqualTo(SessionPhase.Preparation));
    }

    // 11-04 확정: 49번 약점 공략은 표에는 남기고 뽑기(다시 뽑기 포함)에서만 뺀다.
    [Test]
    public void CardFortyNineStaysInTableButIsNeverDrawn()
    {
        var cards = Data().Augments;
        Assert.That(cards.Count, Is.EqualTo(50));
        Assert.That(cards.Single(c => c.Number == 49).Drawable, Is.False);
        Assert.That(cards.Count(c => c.Drawable), Is.EqualTo(49));
        Assert.That(cards.Count(c => c.Drawable && c.Grade == AugmentationGrade.Platinum), Is.EqualTo(11));
        var random = new System.Random(11);
        var owned = new AugmentationEffects();
        for (int i = 0; i < 2000; i++)
        {
            Assert.That(AugmentationDeck.Draw(random, cards, owned, 3, null, i % 2 == 0).Any(c => c.Number == 49), Is.False);
            Assert.That(
                AugmentationDeck.Draw(random, cards, owned, 1, null, false, 60, 30, 25, AugmentationGrade.Platinum).Any(c => c.Number == 49),
                Is.False
            );
        }
    }

    [Test]
    public void DrawHasNoDuplicatesExcludesPicksAndPreviousScreenAndWeightsOwnedSubcategories()
    {
        var cards = Data().Augments;
        var random = new System.Random(7);
        var owned = new AugmentationEffects();
        for (int i = 0; i < 200; i++)
        {
            var draw = AugmentationDeck.Draw(random, cards, owned, 3, null, false);
            Assert.That(draw.Length, Is.EqualTo(3));
            Assert.That(draw.Select(c => c.Id).Distinct().Count(), Is.EqualTo(3));
        }
        var screen = AugmentationDeck.Draw(random, cards, owned, 3, null, false);
        var previous = new HashSet<int>(screen.Select(c => c.Spec.Id));
        for (int i = 0; i < 100; i++)
        {
            Assert.That(
                AugmentationDeck.Draw(random, cards, owned, 3, previous, false).Any(c => previous.Contains(c.Spec.Id)),
                Is.False,
                "Reroll excludes the shown cards"
            );
        }

        foreach (var rule in cards.Where(c => !c.Repeatable && c.Grade == AugmentationGrade.Platinum))
        {
            owned.Apply(new AugmentationCard(rule));
        }

        for (int i = 0; i < 100; i++)
        {
            Assert.That(
                AugmentationDeck.Draw(random, cards, owned, 1, null, true).All(c => c.Spec.Repeatable),
                Is.True,
                "Rule cards never return"
            );
        }

        Assert.That(
            AugmentationDeck.Draw(random, cards, new AugmentationEffects(), 3, null, true)[0].Grade,
            Is.EqualTo(AugmentationGrade.Platinum),
            "48 황금 거래 forces the first slot"
        );
        var burn = new AugmentationEffects();
        burn.Apply(new AugmentationCard(cards.Single(c => c.Number == 4)));
        burn.Apply(new AugmentationCard(cards.Single(c => c.Number == 22)));
        int burnSilver = 0,
            plainSilver = 0;
        var r = new System.Random(3);
        for (int i = 0; i < 4000; i++)
        {
            var c = AugmentationDeck.Draw(
                r,
                cards.Where(x => x.Grade == AugmentationGrade.Silver).ToList(),
                burn,
                1,
                null,
                false,
                100,
                0
            )[0];
            if (c.Number == 4)
            {
                burnSilver++;
            }
        }
        r = new System.Random(3);
        for (int i = 0; i < 4000; i++)
        {
            var c = AugmentationDeck.Draw(
                r,
                cards.Where(x => x.Grade == AugmentationGrade.Silver).ToList(),
                new AugmentationEffects(),
                1,
                null,
                false,
                100,
                0
            )[0];
            if (c.Number == 4)
            {
                plainSilver++;
            }
        }
        Assert.That(burnSilver, Is.EqualTo(0), "Picked numeric cards stay out while fresh cards remain");
        Assert.That(plainSilver, Is.GreaterThan(0));
    }

    [Test]
    public void RerollIsFreeOncePerCardAndCardFourAddsChoices()
    {
        // 10-30: 다시 뽑기는 카드마다 1회(롤체 방식). 한 장만 바뀌고 나머지는 그대로.
        var s = BeforeBoss();
        s.StartCombat();
        Finish(s);
        Assert.That(s.RerollsLeft, Is.EqualTo(3));
        Assert.That(s.RerollsLeftFor(0), Is.EqualTo(1));
        var before = s.AugmentationChoices.Select(c => c.Id).ToArray();
        Assert.That(s.RerollAugmentation(0), Is.True);
        Assert.That(s.RerollAugmentation(0), Is.False);
        var after = s.AugmentationChoices.Select(c => c.Id).ToArray();
        Assert.That(before, Does.Not.Contain(after[0]));
        Assert.That(after.Skip(1), Is.EqualTo(before.Skip(1)));
        Assert.That(after.Distinct().Count(), Is.EqualTo(after.Length));
        Assert.That(s.RerollsLeftFor(1), Is.EqualTo(1));
        Assert.That(s.RerollAugmentation(1), Is.True);
        Assert.That(s.RerollsLeft, Is.EqualTo(1));
        Assert.That(s.AugmentationRemaining, Is.EqualTo(30).Within(.01));
        var t = BeforeBoss();
        t.DebugGrantAugmentation(17);
        t.DebugGrantAugmentation(47);
        t.StartCombat();
        Finish(t);
        Assert.That(t.AugmentationChoices.Count, Is.EqualTo(4));
        // 17번 '한 번 더': 카드마다 +1회.
        Assert.That(t.RerollsLeftFor(3), Is.EqualTo(2));
        Assert.That(t.RerollsLeft, Is.EqualTo(8));
    }

    [Test]
    public void NumericCardsStackAndOneTimeGrantsApply()
    {
        var s = Field();
        int gold = s.Gold;
        float damage = s.Player.Damage;
        int core = s.CoreMaximum;
        s.DebugGrantAugmentation(1);
        s.DebugGrantAugmentation(1);
        Assert.That(s.Augments.TowerDamagePercent, Is.EqualTo(16));
        s.DebugGrantAugmentation(19);
        Assert.That(s.Augments.TowerDamagePercent, Is.EqualTo(36));
        s.DebugGrantAugmentation(2);
        Assert.That(s.Player.Damage, Is.EqualTo(damage + 2));
        Assert.That(s.Augments.PlayerRatePercent, Is.EqualTo(10));
        s.DebugGrantAugmentation(18);
        Assert.That(s.Gold, Is.EqualTo(gold + 150));
        Assert.That(s.Augments.KillGoldPercent, Is.EqualTo(-20));
        s.DebugGrantAugmentation(14);
        Assert.That(s.CoreMaximum, Is.EqualTo(core + 5));
        Assert.That(s.CoreEnergy, Is.EqualTo(core + 5));
        s.DebugGrantAugmentation(50);
        Assert.That(s.CoreMaximum, Is.EqualTo(Mathf.CeilToInt((core + 5) / 2f)));
        Assert.That(s.CoreEnergy, Is.EqualTo(s.CoreMaximum));
        s.DebugGrantAugmentation(13);
        string reason;
        var t = s.TryInstall(TowerKind.Single, new Vector2Int(2, 1), out reason);
        int crystals = s.Crystals;
        Assert.That(s.DestroyTower(t.Id), Is.True);
        Assert.That(s.Crystals - crystals, Is.EqualTo(45), "재활용 75%");
    }

    [Test]
    public void ExchangeMovesTenPercentButAtMostFiftyCrystalsPerRound()
    {
        var s = Game(Data());
        s.DebugGrantAugmentation(33);
        typeof(Session).GetProperty("Gold").SetValue(s, 10000);
        int crystals = s.Crystals;
        Assert.That(s.StartCombat(), Is.True);
        Finish(s);
        Assert.That(s.Crystals - crystals - s.LastCompletionReward, Is.EqualTo(50), "11-02 확정: 라운드당 최대 50");
        typeof(Session).GetProperty("Gold").SetValue(s, 200);
        crystals = s.Crystals;
        Assert.That(s.StartCombat(), Is.True);
        Finish(s);
        int moved = s.Crystals - crystals - s.LastCompletionReward;
        Assert.That(moved, Is.GreaterThanOrEqualTo(20).And.LessThan(50), "상한 아래에선 남은 골드 10%");
    }

    [Test]
    public void CritCardsChangeDamageAndEveryFourthAttackCrits()
    {
        var s = Field();
        s.DebugGrantAugmentation(44);
        s.StartCombat();
        var e = s.DebugSpawn(EnemyKind.Boss, 0);
        var hits = new List<int>();
        typeof(Session)
            .GetEvent("DamageApplied")
            .AddEventHandler(s, new Action<EnemySnapshot, int>((x, n) => hits.Add(n)));
        for (int i = 0; i < 4; i++)
        {
            s.Tick(1.0f, new Vector2(0, 1));
        }
        Assert.That(hits.Count, Is.GreaterThanOrEqualTo(4));
        Assert.That(
            hits.Take(4).ToArray(),
            Is.EqualTo(new[] { 10, 10, 10, 20 }),
            "Player 10 damage, boss defense 0, ×2 on the fourth attack"
        );
    }

    [Test]
    public void MergingAdjacentSameTowersBuildsStars()
    {
        var s = Field(10000);
        s.DebugGrantAugmentation(45);
        string reason;
        var a = s.TryInstall(TowerKind.Single, new Vector2Int(2, 2), out reason);
        var b = s.TryInstall(TowerKind.Single, new Vector2Int(3, 2), out reason);
        Assert.That(b, Is.SameAs(a));
        Assert.That(a.Stars, Is.EqualTo(2));
        Assert.That(s.Towers.Count, Is.EqualTo(1));
        Assert.That(a.Stats.Damage, Is.EqualTo(12.5f * 1.8f).Within(.001));
        Assert.That(a.Stats.Range, Is.EqualTo(4f).Within(.001));
        var c = s.TryInstall(TowerKind.Single, new Vector2Int(2, 4), out reason);
        var d = s.TryInstall(TowerKind.Single, new Vector2Int(2, 5), out reason);
        Assert.That(d.Stars, Is.EqualTo(2));
        // ★2 at (2,2); a new ★2 forms at (3,2) and joins it → ★3 keeps the oldest cell.
        var t = Field(10000);
        t.DebugGrantAugmentation(45);
        t.TryInstall(TowerKind.Multi, new Vector2Int(2, 2), out reason);
        t.TryInstall(TowerKind.Multi, new Vector2Int(2, 3), out reason);
        t.TryInstall(TowerKind.Multi, new Vector2Int(3, 2), out reason);
        var star3 = t.TryInstall(TowerKind.Multi, new Vector2Int(4, 2), out reason);
        Assert.That(t.Towers.Count(x => x.Kind == TowerKind.Multi), Is.EqualTo(1));
        Assert.That(star3.Stars, Is.EqualTo(3));
        Assert.That(star3.Stats.Damage, Is.EqualTo(11f * 3.2f).Within(.001));
        Assert.That(star3.Spent, Is.EqualTo(240));
    }

    // 치명타 숫자 표시(10-28): DamageApplied 순간에만 HitCritical이 켜짐.
    [Test]
    public void HitCriticalMarksOnlyTheCriticalHit()
    {
        var s = Field();
        s.DebugGrantAugmentation(44);
        s.StartCombat();
        var e = s.DebugSpawn(EnemyKind.Boss, 0);
        var crits = new List<bool>();
        typeof(Session)
            .GetEvent("DamageApplied")
            .AddEventHandler(s, new Action<EnemySnapshot, int>((x, n) => crits.Add(s.HitCritical)));
        for (int i = 0; i < 4; i++)
        {
            s.Tick(1.0f, new Vector2(0, 1));
        }
        Assert.That(crits.Take(4).ToArray(), Is.EqualTo(new[] { false, false, false, true }));
        Assert.That(s.HitCritical, Is.False);
    }
}
