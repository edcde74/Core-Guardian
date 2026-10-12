using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CoreGuardian;

public class CoreGuardianRoundFlowTests
{
    [Test] public void PrototypeIncludesBossTenAndFullElevenSchedule()
    {
        var d=Data();
        Assert.That(d.Waves.Max(w=>w.Wave),Is.EqualTo(11));
        EnemyKind boss;
        Assert.That(Enum.TryParse("Boss",out boss),Is.True);
        Assert.That(d.Enemies[boss].Health,Is.EqualTo(300));
        Assert.That(d.Enemies[boss].Gold,Is.EqualTo(120));
        Assert.That(d.Waves.Where(w=>w.Wave==10).Sum(w=>w.Count),Is.EqualTo(15));
        Assert.That(d.Waves.Where(w=>w.Wave==11).Sum(w=>w.Count),Is.EqualTo(21));
        Assert.That(d.RewardForWave(10),Is.EqualTo(35));
        Assert.That(d.RewardForWave(11),Is.EqualTo(30));
    }
    static readonly Vector2 Away = new Vector2(100,100);
    static CombatData Data() => CombatData.Load(Resources.Load<TextAsset>("CoreGuardian/enemies").text,Resources.Load<TextAsset>("CoreGuardian/towers").text,Resources.Load<TextAsset>("CoreGuardian/stage").text,Resources.Load<TextAsset>("CoreGuardian/upgrades").text,Resources.Load<TextAsset>("CoreGuardian/waves").text);
    static Session Game(CombatData d)
    {
        var s=new Session(d);s.SetBoard(40,25,new Vector2Int(10,0),new[]{Vector2Int.zero,new Vector2Int(10,0)});return s;
    }
    static void Finish(Session s)
    {
        for(int i=0;i<1000&&s.Phase==SessionPhase.Combat;i++)
        {
            s.Tick(.1f,Away);
            foreach(var e in s.Enemies.ToArray())s.ApplyDamage(e,10000,"Test");
        }
        Assert.That(s.Phase,Is.Not.EqualTo(SessionPhase.Combat));
    }
    [Test] public void AllNineSchedulesAwardOnceAndEndAtNine()
    {
        var d=Data();d.Waves.RemoveAll(w=>w.Wave>9);var s=Game(d);int[] rewards={20,20,20,20,25,25,25,25,25};
        for(int wave=1;wave<=9;wave++)
        {
            Assert.That(s.Wave,Is.EqualTo(wave));Assert.That(s.StartCombat(),Is.True);
            int before=s.Crystals;Finish(s);
            Assert.That(s.Crystals-before,Is.EqualTo(rewards[wave-1]),"Reward for wave "+wave);
            Assert.That(s.Phase,Is.EqualTo(wave==9?SessionPhase.Victory:SessionPhase.Preparation));
            int after=s.Crystals;s.Tick(1,Away);Assert.That(s.Crystals,Is.EqualTo(after));
        }
        Assert.That(s.Wave,Is.EqualTo(9));Assert.That(s.Gold,Is.EqualTo(510));Assert.That(s.Crystals,Is.EqualTo(305));
        Assert.That(s.StartCombat(),Is.False);s.Tick(100,Away);Assert.That(s.Phase,Is.EqualTo(SessionPhase.Victory));
    }
    [Test] public void NextPreparationKeepsBuildAndGrowthAndCoreState()
    {
        var d=Data();d.StartGold=1000;d.StartCrystals=1000;d.Waves.Clear();
        d.Waves.Add(new WaveSpec{Wave=1,Kind=EnemyKind.Attack,Count=1,Interval=1});
        d.Waves.Add(new WaveSpec{Wave=2,Kind=EnemyKind.Defense,Count=1,Interval=1});
        var s=Game(d);string reason;var t=s.TryInstall(TowerKind.Single,new Vector2Int(30,20),out reason);
        Assert.That(s.UpgradeTower(t.Id,"damage",out reason),Is.True);Assert.That(s.UpgradePlayer("damage",out reason),Is.True);
        int crystals=s.Crystals,gold=s.Gold;s.StartCombat();s.Tick(11,Away);
        Assert.That(s.Phase,Is.EqualTo(SessionPhase.Preparation));Assert.That(s.Wave,Is.EqualTo(2));
        Assert.That(s.CoreEnergy,Is.EqualTo(18));Assert.That(s.Gold,Is.EqualTo(gold));Assert.That(s.Crystals,Is.EqualTo(crystals+20));
        Assert.That(s.Towers.Single(),Is.SameAs(t));Assert.That(t.Stats.UpgradeSteps["damage"],Is.EqualTo(1));
        Assert.That(s.Player.UpgradeSteps["damage"],Is.EqualTo(1));Assert.That(s.IsBuildable(t.Position),Is.False);
        Assert.That(s.PreparationRemaining,Is.InRange(48f,50f));
        Assert.That(s.UpgradeTower(t.Id,"damage",out reason),Is.True);
        Assert.That(s.StartCombat(),Is.True);Assert.That(s.StartCombat(),Is.False);
        Assert.That(s.UpgradeTower(t.Id,"damage",out reason),Is.False);
        Assert.That(s.TryInstall(TowerKind.Single,new Vector2Int(31,20),out reason),Is.Null);
        s.Tick(.05f,Away);Assert.That(s.Enemies.Single().Kind,Is.EqualTo(EnemyKind.Defense));
    }
    [Test] public void WaitingForFutureSpawnDoesNotFinishRound()
    {
        var d=Data();d.Waves.Clear();d.Waves.Add(new WaveSpec{Wave=1,Kind=EnemyKind.Attack,Count=1,Interval=1,Start=2});
        var s=Game(d);s.StartCombat();s.Tick(1,Away);Assert.That(s.Enemies,Is.Empty);Assert.That(s.Phase,Is.EqualTo(SessionPhase.Combat));
        s.Tick(1.1f,Away);Assert.That(s.Enemies.Count,Is.EqualTo(1));Assert.That(s.Phase,Is.EqualTo(SessionPhase.Combat));
    }
    [Test] public void NextPreparationTimeoutStartsWithoutTower()
    {
        var d=Data();d.Waves.Clear();foreach(int wave in new[]{1,2})d.Waves.Add(new WaveSpec{Wave=wave,Kind=EnemyKind.Attack,Count=1,Interval=1});
        var s=Game(d);s.StartCombat();Finish(s);Assert.That(s.Phase,Is.EqualTo(SessionPhase.Preparation));
        s.Tick(50.1f,Away);Assert.That(s.Wave,Is.EqualTo(2));Assert.That(s.Phase,Is.EqualTo(SessionPhase.Combat));
    }
    [Test] public void CoreDefeatHasNoCompletionRewardOrNextRound()
    {
        var d=Data();d.CoreEnergy=2;d.Waves.Clear();foreach(int wave in new[]{1,2})d.Waves.Add(new WaveSpec{Wave=wave,Kind=EnemyKind.Attack,Count=1,Interval=1});
        var s=Game(d);s.StartCombat();s.Tick(11,Away);Assert.That(s.Phase,Is.EqualTo(SessionPhase.Defeat));
        Assert.That(s.Wave,Is.EqualTo(1));Assert.That(s.Crystals,Is.EqualTo(100));s.Tick(100,Away);Assert.That(s.Crystals,Is.EqualTo(100));
    }
    [Test] public void DiagnosticIsSeparateAndCanResetAfterLaterRound()
    {
        var s=Game(Data());s.StartCombat();Finish(s);Assert.That(s.Wave,Is.EqualTo(2));
        s.StartDiagnosticEncounter();Assert.That(s.Wave,Is.EqualTo(1));s.StartCombat();Finish(s);
        Assert.That(s.Diagnostic,Is.True);Assert.That(s.Phase,Is.EqualTo(SessionPhase.Victory));
        Assert.That(s.Crystals,Is.EqualTo(1000));
    }
}
