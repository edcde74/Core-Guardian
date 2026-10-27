using System;
using System.Linq;
using CoreGuardian;
using NUnit.Framework;
using UnityEngine;

public class CoreGuardianThirtyRoundTests
{
    static string Csv(string key)=>CoreGuardianLegacyData.Text(key);
    static CombatData Data()=>CombatData.Load(Csv("enemies"),Csv("towers"),Csv("stage"),Csv("upgrades"),Csv("waves"));
    static Session Game(CombatData d){var s=new Session(d,42);s.SetBoard(40,25,new Vector2Int(10,0),new[]{Vector2Int.zero,new Vector2Int(10,0)});return s;}
    [Test] public void LoaderKeepsSixColumnCompatibilityAndRejectsInvalidWaveOverrides()
    {
        var lines=Csv("waves").Replace("\r","").Split('\n').Where(l=>!string.IsNullOrWhiteSpace(l)).ToArray();
        var legacy=string.Join("\n",lines.Select(l=>string.Join(",",l.Split(',').Take(6))));
        var d=CombatData.Load(Csv("enemies"),Csv("towers"),Csv("stage"),Csv("upgrades"),legacy);
        Assert.That(d.Waves.All(w=>w.Enemy==null),Is.True);Assert.That(d.EnemyForWave(30,EnemyKind.Attack),Is.SameAs(d.Enemies[EnemyKind.Attack]));
        var fields=lines[1].Split(',');
        foreach(var pair in new[]{(6,"0"),(7,"100"),(8,"0"),(9,"-1")})
        {
            var bad=(string[])fields.Clone();bad[pair.Item1]=pair.Item2;
            Assert.Throws<ArgumentException>(()=>CombatData.Load(Csv("enemies"),Csv("towers"),Csv("stage"),Csv("upgrades"),string.Join("\n",new[]{lines[0],string.Join(",",bad)})));
        }
    }
    [Test] public void ThirtyRoundDataHasFrequentGrowthAndThreeBosses()
    {
        var d=Data();Assert.That(d.Waves.Select(w=>w.Wave).Distinct().OrderBy(w=>w),Is.EqualTo(Enumerable.Range(1,30)));
        Assert.That(d.Waves.Where(w=>w.Kind==EnemyKind.Boss).Select(w=>w.Wave),Is.EqualTo(new[]{10,20,30}));
        Assert.That(d.Towers[TowerKind.Single].Cost,Is.EqualTo(60));Assert.That(d.Towers[TowerKind.Multi].Cost,Is.EqualTo(60));Assert.That(d.Towers[TowerKind.Support].Cost,Is.EqualTo(75));
        Assert.That(Enumerable.Range(1,4).Select(d.PlayerUpgradePrice),Is.EqualTo(new[]{40,65,85,110}));
        Assert.That(d.RewardForWave(1),Is.EqualTo(60));Assert.That(d.RewardForWave(30),Is.EqualTo(130));
        var s=Game(d);string reason;var t=s.TryInstall(TowerKind.Single,new Vector2Int(2,1),out reason);Assert.That(s.Crystals,Is.EqualTo(40));Assert.That(s.TowerUpgradePrice(t.Id,"damage"),Is.EqualTo(60));
    }
    [Test] public void EveryFifthRoundOpensAugmentationAndFinalThirtyEndsWithoutAnotherChoice()
    {
        var s=Game(Data());int choices=0;
        for(int wave=1;wave<=30;wave++)
        {
            Assert.That(s.StartCombat(),Is.True);
            // Forced lethal hits only verify progression; never difficulty or measured clear time.
            for(int i=0;i<1600&&s.Phase==SessionPhase.Combat;i++){s.Tick(.05f,new Vector2(100,100));foreach(var e in s.Enemies.ToArray())s.ApplyDamage(e,100000,"ProgressionQA");}
            Assert.That(s.CompletedWave,Is.EqualTo(wave));
            Assert.That(s.Phase,Is.EqualTo(wave==30?SessionPhase.Victory:wave%5==0?SessionPhase.Augmentation:SessionPhase.Preparation));
            if(s.Phase==SessionPhase.Augmentation){choices++;Assert.That(s.AugmentationChoices.Count,Is.EqualTo(3));Assert.That(s.ChooseAugmentation(0),Is.True);}
        }
        Assert.That(choices,Is.EqualTo(5));Assert.That(s.Augments.Selected.Count,Is.EqualTo(5));Assert.That(s.AugmentationChoices,Is.Empty);
    }
    [Test] public void WaveStatsArePerSpawnAndDoNotMutateTheBaseEnemyOrPreviouslySpawnedEnemy()
    {
        var d=Data();var s=Game(d);var first=s.DebugSpawn(EnemyKind.Attack);
        typeof(Session).GetProperty("Wave").SetValue(s,30);var late=s.DebugSpawn(EnemyKind.Attack);
        Assert.That(late.Spec.Health,Is.GreaterThan(first.Spec.Health));Assert.That(late.Spec.Defense,Is.GreaterThan(first.Spec.Defense));
        Assert.That(first.Spec.Health,Is.EqualTo(40));Assert.That(d.Enemies[EnemyKind.Attack].Health,Is.EqualTo(40));
        foreach(var pair in new[]{(10,500),(20,1600),(30,4500)}){typeof(Session).GetProperty("Wave").SetValue(s,pair.Item1);Assert.That(s.DebugSpawn(EnemyKind.Boss).Health,Is.EqualTo(pair.Item2));}
    }
    [Test] public void FiveRoundAugmentationDoesNotRequireTheBossToHaveBeenKilled()
    {
        var d=Data();d.Waves.Clear();d.Waves.Add(new WaveSpec{Wave=10,Kind=EnemyKind.Boss,Count=1,Interval=1});d.Waves.Add(new WaveSpec{Wave=11,Kind=EnemyKind.Attack,Count=1,Interval=1});
        var s=Game(d);typeof(Session).GetProperty("Wave").SetValue(s,10);Assert.That(s.StartCombat(),Is.True);s.Tick(17,new Vector2(100,100));
        Assert.That(s.BossKilled,Is.False);Assert.That(s.Gold,Is.Zero);Assert.That(s.CoreEnergy,Is.EqualTo(10));Assert.That(s.Phase,Is.EqualTo(SessionPhase.Augmentation));Assert.That(s.AugmentationChoices.Count,Is.EqualTo(3));
    }
}
