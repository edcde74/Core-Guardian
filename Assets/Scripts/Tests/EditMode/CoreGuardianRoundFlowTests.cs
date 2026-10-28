using System;
using System.Linq;
using CoreGuardian;
using NUnit.Framework;
using UnityEngine;

public class CoreGuardianRoundFlowTests
{
    [Test]
    public void PrototypeIncludesThirtyRoundsAndAllBossMilestones()
    {
        var d = Data();
        Assert.That(d.Waves.Max(w => w.Wave), Is.EqualTo(30));
        EnemyKind boss;
        Assert.That(Enum.TryParse("Boss", out boss), Is.True);
        Assert.That(d.Enemies[boss].Health, Is.EqualTo(500));
        Assert.That(d.Enemies[boss].Gold, Is.EqualTo(120));
        Assert.That(d.Waves.Where(w => w.Wave == 10).Sum(w => w.Count), Is.EqualTo(38));
        Assert.That(d.Waves.Where(w => w.Wave == 11).Sum(w => w.Count), Is.EqualTo(45));
        Assert.That(d.RewardForWave(10), Is.EqualTo(90));
        Assert.That(d.RewardForWave(11), Is.EqualTo(70));
    }

    static readonly Vector2 Away = new Vector2(100, 100);

    static CombatData Data() =>
        CombatData.Load(
            CoreGuardianLegacyData.Text("enemies"),
            CoreGuardianLegacyData.Text("towers"),
            CoreGuardianLegacyData.Text("stage"),
            CoreGuardianLegacyData.Text("upgrades"),
            CoreGuardianLegacyData.Text("waves")
        );

    static Session Game(CombatData d)
    {
        var s = new Session(d);
        s.SetBoard(40, 25, new Vector2Int(10, 0), new[] { Vector2Int.zero, new Vector2Int(10, 0) });
        return s;
    }

    static void Finish(Session s)
    {
        for (int i = 0; i < 1000 && s.Phase == SessionPhase.Combat; i++)
        {
            s.Tick(.1f, Away);
            foreach (var e in s.Enemies.ToArray())
            {
                s.ApplyDamage(e, 10000, "Test");
            }
        }
        Assert.That(s.Phase, Is.Not.EqualTo(SessionPhase.Combat));
    }

    static CombatData Load(string stage) =>
        CombatData.Load(
            CoreGuardianLegacyData.Text("enemies"),
            CoreGuardianLegacyData.Text("towers"),
            stage,
            CoreGuardianLegacyData.Text("upgrades"),
            CoreGuardianLegacyData.Text("waves")
        );

    // 2026-10-24 rule: normal preparation 30s; 45s before a boss round and right after an augmentation choice.
    [Test]
    public void PreparationIsThirtySecondsAndLongLengthIsStageData()
    {
        var d = Data();
        Assert.That(d.PreparationSeconds, Is.EqualTo(30));
        Assert.That(d.PreparationSecondsLong, Is.EqualTo(45));
        Assert.That(Game(d).PreparationRemaining, Is.EqualTo(30), "Round 1 preparation is normal length");
        var stage = CoreGuardianLegacyData.Text("stage");
        Assert.That(
            Load(stage.Replace("preparationSecondsLong,45", "preparationSecondsLong,50")).PreparationSecondsLong,
            Is.EqualTo(50)
        );
        var legacy = Load(
            string.Join("\n", stage.Replace("\r", "").Split('\n').Where(l => !l.StartsWith("preparationSecondsLong,")))
        );
        Assert.That(
            legacy.PreparationSecondsLong,
            Is.EqualTo(legacy.PreparationSeconds),
            "Without the key every preparation keeps the normal length"
        );
    }

    [Test]
    public void BossRoundsComeFromWaveData()
    {
        var d = Data();
        foreach (int wave in new[] { 10, 20, 30 })
        {
            Assert.That(d.IsBossWave(wave), Is.True, "Boss round " + wave);
        }

        foreach (int wave in new[] { 1, 5, 9, 11, 15, 19, 21, 25, 29 })
        {
            Assert.That(d.IsBossWave(wave), Is.False, "Normal round " + wave);
        }

        Assert.That(d.PreparationFor(10, false), Is.EqualTo(45));
        Assert.That(d.PreparationFor(11, false), Is.EqualTo(30));
        Assert.That(d.PreparationFor(6, true), Is.EqualTo(45));
        Assert.That(d.PreparationFor(20, true), Is.EqualTo(45));
    }

    [Test]
    public void ThirtyRoundsUseLongPreparationBeforeBossesAndAfterAugmentations()
    {
        var s = Game(Data());
        for (int wave = 1; wave < 30; wave++)
        {
            Assert.That(s.Wave, Is.EqualTo(wave));
            Assert.That(s.StartCombat(), Is.True);
            Finish(s);
            if (wave % 5 == 0)
            {
                Assert.That(s.Phase, Is.EqualTo(SessionPhase.Augmentation), "Augmentation after round " + wave);
                float before = s.PreparationRemaining;
                s.Tick(10, Away);
                Assert.That(s.Phase, Is.EqualTo(SessionPhase.Augmentation));
                Assert.That(s.PreparationRemaining, Is.EqualTo(before), "Augmentation time never consumes preparation");
                Assert.That(s.ChooseAugmentation(0), Is.True);
            }
            Assert.That(s.Phase, Is.EqualTo(SessionPhase.Preparation));
            Assert.That(s.Wave, Is.EqualTo(wave + 1));
            bool longer = wave % 5 == 0 || (wave + 1) % 10 == 0;
            float expected = longer ? 45 : 30;
            Assert.That(
                s.PreparationRemaining,
                Is.InRange(expected - .2f, expected),
                "Preparation before round " + (wave + 1) + " (the finishing tick may already spend a fraction)"
            );
        }
    }

    [Test]
    public void LongPreparationRunsItsFullLengthAndTimedOutChoiceAlsoGetsIt()
    {
        var s = Game(Data());
        for (int wave = 1; wave <= 5; wave++)
        {
            Assert.That(s.StartCombat(), Is.True);
            Finish(s);
        }
        Assert.That(s.Phase, Is.EqualTo(SessionPhase.Augmentation));
        s.Tick(30.01f, Away);
        Assert.That(s.Phase, Is.EqualTo(SessionPhase.Preparation), "Choice timeout picks a card");
        Assert.That(s.PreparationRemaining, Is.InRange(44.9f, 45));
        s.Tick(s.PreparationRemaining - .1f, Away);
        Assert.That(s.Phase, Is.EqualTo(SessionPhase.Preparation));
        s.Tick(.2f, Away);
        Assert.That(s.Phase, Is.EqualTo(SessionPhase.Combat));
        Assert.That(s.Wave, Is.EqualTo(6));
    }

    [Test]
    public void AllNineSchedulesAwardOnceAndEndAtNine()
    {
        var d = Data();
        d.AugmentationEvery = 10;
        d.Waves.RemoveAll(w => w.Wave > 9);
        var expectedGold = d.Waves.Sum(w => w.Count * (w.Enemy ?? d.Enemies[w.Kind]).Gold);
        var s = Game(d);
        int[] rewards = { 60, 60, 60, 60, 65, 65, 65, 65, 65 };
        for (int wave = 1; wave <= 9; wave++)
        {
            Assert.That(s.Wave, Is.EqualTo(wave));
            Assert.That(s.StartCombat(), Is.True);
            int before = s.Crystals;
            Finish(s);
            Assert.That(s.Crystals - before, Is.EqualTo(rewards[wave - 1]), "Reward for wave " + wave);
            Assert.That(s.Phase, Is.EqualTo(wave == 9 ? SessionPhase.Victory : SessionPhase.Preparation));
            int after = s.Crystals;
            s.Tick(1, Away);
            Assert.That(s.Crystals, Is.EqualTo(after));
        }
        Assert.That(s.Wave, Is.EqualTo(9));
        Assert.That(s.Gold, Is.EqualTo(expectedGold));
        Assert.That(s.Crystals, Is.EqualTo(665));
        Assert.That(s.StartCombat(), Is.False);
        s.Tick(100, Away);
        Assert.That(s.Phase, Is.EqualTo(SessionPhase.Victory));
    }

    [Test]
    public void NextPreparationKeepsBuildAndGrowthAndCoreState()
    {
        var d = Data();
        d.StartGold = 1000;
        d.StartCrystals = 1000;
        d.Waves.Clear();
        d.Waves.Add(
            new WaveSpec
            {
                Wave = 1,
                Kind = EnemyKind.Attack,
                Count = 1,
                Interval = 1,
            }
        );
        d.Waves.Add(
            new WaveSpec
            {
                Wave = 2,
                Kind = EnemyKind.Defense,
                Count = 1,
                Interval = 1,
            }
        );
        var s = Game(d);
        string reason;
        var t = s.TryInstall(TowerKind.Single, new Vector2Int(30, 20), out reason);
        Assert.That(s.UpgradeTower(t.Id, "damage", out reason), Is.True);
        Assert.That(s.UpgradePlayer("damage", out reason), Is.True);
        int crystals = s.Crystals,
            gold = s.Gold;
        s.StartCombat();
        s.Tick(11, Away);
        Assert.That(s.Phase, Is.EqualTo(SessionPhase.Preparation));
        Assert.That(s.Wave, Is.EqualTo(2));
        Assert.That(s.CoreEnergy, Is.EqualTo(18));
        Assert.That(s.Gold, Is.EqualTo(gold));
        Assert.That(s.Crystals, Is.EqualTo(crystals + d.RewardForWave(1)));
        Assert.That(s.Towers.Single(), Is.SameAs(t));
        Assert.That(t.Stats.UpgradeSteps["damage"], Is.EqualTo(1));
        Assert.That(s.Player.UpgradeSteps["damage"], Is.EqualTo(1));
        Assert.That(s.IsBuildable(t.Position), Is.False);
        Assert.That(s.PreparationRemaining, Is.InRange(d.PreparationSeconds - 2, d.PreparationSeconds));
        Assert.That(s.UpgradeTower(t.Id, "damage", out reason), Is.True);
        Assert.That(s.StartCombat(), Is.True);
        Assert.That(s.StartCombat(), Is.False);
        Assert.That(s.UpgradeTower(t.Id, "damage", out reason), Is.True, "전투 중 강화 허용 (10-28)");
        Assert.That(
            s.TryInstall(TowerKind.Single, new Vector2Int(31, 20), out reason),
            Is.Not.Null,
            "전투 중 설치 허용 (10-28)"
        );
        s.Tick(.05f, Away);
        Assert.That(s.Enemies.Single().Kind, Is.EqualTo(EnemyKind.Defense));
    }

    [Test]
    public void WaitingForFutureSpawnDoesNotFinishRound()
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
                Start = 2,
            }
        );
        var s = Game(d);
        s.StartCombat();
        s.Tick(1, Away);
        Assert.That(s.Enemies, Is.Empty);
        Assert.That(s.Phase, Is.EqualTo(SessionPhase.Combat));
        s.Tick(1.1f, Away);
        Assert.That(s.Enemies.Count, Is.EqualTo(1));
        Assert.That(s.Phase, Is.EqualTo(SessionPhase.Combat));
    }

    [Test]
    public void NextPreparationTimeoutStartsWithoutTower()
    {
        var d = Data();
        d.Waves.Clear();
        foreach (int wave in new[] { 1, 2 })
        {
            d.Waves.Add(
                new WaveSpec
                {
                    Wave = wave,
                    Kind = EnemyKind.Attack,
                    Count = 1,
                    Interval = 1,
                }
            );
        }

        var s = Game(d);
        s.StartCombat();
        Finish(s);
        Assert.That(s.Phase, Is.EqualTo(SessionPhase.Preparation));
        s.Tick(d.PreparationSeconds + .1f, Away);
        Assert.That(s.Wave, Is.EqualTo(2));
        Assert.That(s.Phase, Is.EqualTo(SessionPhase.Combat));
    }

    [Test]
    public void CoreDefeatHasNoCompletionRewardOrNextRound()
    {
        var d = Data();
        d.CoreEnergy = 2;
        d.Waves.Clear();
        foreach (int wave in new[] { 1, 2 })
        {
            d.Waves.Add(
                new WaveSpec
                {
                    Wave = wave,
                    Kind = EnemyKind.Attack,
                    Count = 1,
                    Interval = 1,
                }
            );
        }

        var s = Game(d);
        s.StartCombat();
        s.Tick(11, Away);
        Assert.That(s.Phase, Is.EqualTo(SessionPhase.Defeat));
        Assert.That(s.Wave, Is.EqualTo(1));
        Assert.That(s.Crystals, Is.EqualTo(100));
        s.Tick(100, Away);
        Assert.That(s.Crystals, Is.EqualTo(100));
    }

    [Test]
    public void DiagnosticIsSeparateAndCanResetAfterLaterRound()
    {
        var s = Game(Data());
        s.StartCombat();
        Finish(s);
        Assert.That(s.Wave, Is.EqualTo(2));
        s.StartDiagnosticEncounter();
        Assert.That(s.Wave, Is.EqualTo(1));
        s.StartCombat();
        Finish(s);
        Assert.That(s.Diagnostic, Is.True);
        Assert.That(s.Phase, Is.EqualTo(SessionPhase.Victory));
        Assert.That(s.Crystals, Is.EqualTo(1000));
    }
}
