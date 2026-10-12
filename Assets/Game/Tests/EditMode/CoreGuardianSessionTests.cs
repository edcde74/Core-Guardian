using System;
using System.Linq;
using CoreGuardian;
using NUnit.Framework;
using UnityEngine;

public class CoreGuardianSessionTests
{
    static string Csv(string file) {var asset=Resources.Load<TextAsset>("CoreGuardian/"+file);Assert.That(asset,Is.Not.Null,"Missing CSV "+file);return asset.text;}
    static CombatData Data() {return CombatData.Load(Csv("enemies"),Csv("towers"),Csv("stage"),Csv("upgrades"),Csv("waves"));}
    static Session Game(bool noSpawns=true) {var d=Data();if(noSpawns)d.Waves.Clear();var s=new Session(d);s.SetBoard(40,25,new Vector2Int(10,0),new[]{new Vector2Int(0,0),new Vector2Int(10,0)});return s;}
    static readonly Vector2 Away=new Vector2(100,100);
    [Test] public void DamageFeedbackReportsEachActualHitIncludingLethalDamage()
    {
        var s=Game();var e=s.DebugSpawn(EnemyKind.Defense);
        var damageEvent=typeof(Session).GetEvent("DamageApplied");
        Assert.That(damageEvent,Is.Not.Null,"Actual hit feedback event is missing");
        var hits=new System.Collections.Generic.List<int>();
        damageEvent.AddEventHandler(s,new Action<EnemyState,int>((target,amount)=>{Assert.That(target,Is.SameAs(e));hits.Add(amount);}));
        s.ApplyDamage(e,12.5f,"Player"); // Defense then round: 12.
        s.ApplyDamage(e,100,"Tower:1"); // Remaining health, excluding overkill: 38.
        s.ApplyDamage(e,100,"Player"); // Dead target must not create another number.
        Assert.That(hits,Is.EqualTo(new[]{12,38}));
        Assert.That(s.DamageTotals.Values.Sum(),Is.EqualTo(50));
    }
    [Test] public void DamageFeedbackIgnoresZeroDamageMissingTargetsAndCoreArrival()
    {
        var s=Game();var e=s.DebugSpawn(EnemyKind.Attack,9.99f);
        var damageEvent=typeof(Session).GetEvent("DamageApplied");
        Assert.That(damageEvent,Is.Not.Null,"Actual hit feedback event is missing");
        int count=0;damageEvent.AddEventHandler(s,new Action<EnemyState,int>((target,amount)=>count++));
        s.ApplyDamage(e,0,"Player");s.ApplyDamage(null,10,"Player");
        s.StartCombat();s.Tick(.1f,Away);
        Assert.That(count,Is.Zero);Assert.That(s.CoreEnergy,Is.EqualTo(18));
    }
    [Test] public void CsvRejectsDuplicateNegativeAndMissingReference()
    {
        Assert.Throws<ArgumentException>(()=>CombatData.Load(Csv("enemies")+"\nAttack,25,1,2,3,0,0,0,provisional",Csv("towers"),Csv("stage"),Csv("upgrades"),Csv("waves")));
        Assert.Throws<ArgumentException>(()=>CombatData.Load(Csv("enemies").Replace("25,1,2","-25,1,2"),Csv("towers"),Csv("stage"),Csv("upgrades"),Csv("waves")));
        Assert.Throws<ArgumentException>(()=>CombatData.Load(Csv("enemies"),Csv("towers"),Csv("stage"),Csv("upgrades"),Csv("waves").Replace("Attack","Boss")));
    }
    [Test] public void UndefinedNumericKindsMissingWaveOneAndCaseDuplicateUpgradeAreRejected()
    {
        Assert.Throws<ArgumentException>(()=>CombatData.Load(Csv("enemies").Replace("Buff,30","9,30"),Csv("towers"),Csv("stage"),Csv("upgrades"),Csv("waves")));
        Assert.Throws<ArgumentException>(()=>CombatData.Load(Csv("enemies"),Csv("towers").Replace("Support,100","9,100"),Csv("stage"),Csv("upgrades"),Csv("waves")));
        Assert.Throws<ArgumentException>(()=>CombatData.Load(Csv("enemies"),Csv("towers"),Csv("stage"),Csv("upgrades").Replace(",Support,",",9,"),Csv("waves")));
        Assert.Throws<ArgumentException>(()=>CombatData.Load(Csv("enemies"),Csv("towers"),Csv("stage"),Csv("upgrades"),Csv("waves").Replace("wave1_attack,1,","wave1_attack,2,")));
        Assert.Throws<ArgumentException>(()=>CombatData.Load(Csv("enemies"),Csv("towers"),Csv("stage"),Csv("upgrades")+"\ncase_duplicate,single,damage,2.5,80,20",Csv("waves")));
    }
    [Test] public void AttackRateRetainsFractionalCooldownAndSupportsMultipleShotsPerStep()
    {
        string reason;var s=Game();var t=s.TryInstall(TowerKind.Single,new Vector2Int(2,1),out reason);t.Stats.ProjectileSpeed=.001f;s.Data.Enemies[EnemyKind.Attack].Speed=.001f;s.StartCombat();var e=s.DebugSpawn(EnemyKind.Attack,2);e.Health=10000;s.Tick(5.05f,Away);Assert.That(s.Projectiles.Count,Is.EqualTo(7));
        var fast=Game();t=fast.TryInstall(TowerKind.Single,new Vector2Int(2,1),out reason);t.Stats.Rate=100;t.Stats.ProjectileSpeed=.001f;fast.Data.Enemies[EnemyKind.Attack].Speed=.001f;fast.StartCombat();e=fast.DebugSpawn(EnemyKind.Attack,2);e.Health=10000;fast.Tick(.05f,Away);Assert.That(fast.Projectiles.Count,Is.EqualTo(6));
    }
    [Test] public void PlayerPricesHaveIncreasingIncrementAndIndependentStats()
    {
        var d=Data();Assert.That(Enumerable.Range(1,6).Select(d.PlayerUpgradePrice),Is.EqualTo(new[]{100,140,190,250,320,400}));
        var s=Game();s.StartDiagnosticEncounter();string reason;Assert.That(s.UpgradePlayer("damage",out reason),Is.True);Assert.That(s.PlayerUpgradePrice("damage"),Is.EqualTo(140));Assert.That(s.PlayerUpgradePrice("range"),Is.EqualTo(100));
    }
    [Test] public void TimeoutStartsCombatWithoutTowerAndCurrencyStartsCorrectly()
    {
        var s=Game();Assert.That(s.Gold,Is.Zero);Assert.That(s.Crystals,Is.EqualTo(100));s.Tick(50.1f,Away);Assert.That(s.Phase,Is.EqualTo(SessionPhase.Combat));
    }
    [Test] public void PlacementRejectsPathCoreDuplicateAndCombatButAllowsPlayerCell()
    {
        var s=Game();string r;Assert.That(s.TryInstall(TowerKind.Single,new Vector2Int(0,0),out r),Is.Null);Assert.That(s.Crystals,Is.EqualTo(100));Assert.That(s.TryInstall(TowerKind.Support,new Vector2Int(2,2),out r),Is.Null);
        var t=s.TryInstall(TowerKind.Single,new Vector2Int(2,2),out r);Assert.That(t,Is.Not.Null);Assert.That(s.IsBuildable(t.Position),Is.False);Assert.That(s.DestroyTower(t.Id),Is.True);Assert.That(s.DestroyTower(t.Id),Is.False);Assert.That(s.Crystals,Is.EqualTo(50));s.StartCombat();Assert.That(s.TryInstall(TowerKind.Single,new Vector2Int(3,3),out r),Is.Null);
    }
    [Test] public void RefundIncludesActualUpgradesOnce()
    {
        var s=Game();s.StartDiagnosticEncounter();string r;var t=s.TryInstall(TowerKind.Single,new Vector2Int(2,1),out r);Assert.That(s.UpgradeTower(t.Id,"damage",out r),Is.True);Assert.That(s.UpgradeTower(t.Id,"damage",out r),Is.True);Assert.That(t.Spent,Is.EqualTo(280));int before=s.Crystals;s.DestroyTower(t.Id);Assert.That(s.Crystals-before,Is.EqualTo(140));Assert.That(s.DestroyTower(t.Id),Is.False);
    }
    [Test] public void KillGoldAndActualDamageAreAccountedOnceWithoutOverkill()
    {
        var s=Game();var e=s.DebugSpawn(EnemyKind.Attack);Assert.That(s.ApplyDamage(e,100,"Player"),Is.EqualTo(25));Assert.That(s.ApplyDamage(e,100,"Player"),Is.Zero);Assert.That(s.Gold,Is.EqualTo(3));Assert.That(s.DamageTotals["Player"],Is.EqualTo(25));
    }
    [Test] public void CoreArrivalNeverRewardsAndNeverHitsTwice()
    {
        var s=Game();s.StartCombat();s.DebugSpawn(EnemyKind.Attack,9.99f);s.Tick(.1f,Away);Assert.That(s.CoreEnergy,Is.EqualTo(18));Assert.That(s.Gold,Is.Zero);s.Tick(1,Away);Assert.That(s.CoreEnergy,Is.EqualTo(18));
    }
    [Test] public void PriorityUsesRoleThenHealthThenRemainingPathDistance()
    {
        var s=Game();var a=s.DebugSpawn(EnemyKind.Attack,9);var b=s.DebugSpawn(EnemyKind.Buff,9);var d=s.DebugSpawn(EnemyKind.Defense,1);var d2=s.DebugSpawn(EnemyKind.Defense,2);Assert.That(CombatRules.Priority(s.Enemies),Is.SameAs(d2));d.Health=1;Assert.That(CombatRules.Priority(s.Enemies),Is.SameAs(d));
    }
    [Test] public void FirstTargetIsFirstArrivalThenRangeExitReacquiresPriority()
    {
        var s=Game();s.StartCombat();var attack=s.DebugSpawn(EnemyKind.Attack,0);var defense=s.DebugSpawn(EnemyKind.Defense,1);s.Tick(.01f,new Vector2(0,1));Assert.That(s.Projectiles.Single().Target,Is.SameAs(attack));
        attack.Position=new Vector2(-100,-100);attack.PathTravelled=9;s.Tick(1.05f,new Vector2(0,1));Assert.That(s.Projectiles.Any(p=>p.Target==defense),Is.True);
    }
    [Test] public void MultishotUsesThreeDistinctTargetsWithNoSplash()
    {
        var s=Game();string r;var tower=s.TryInstall(TowerKind.Multi,new Vector2Int(2,1),out r);s.StartCombat();var a=s.DebugSpawn(EnemyKind.Attack,0);var b=s.DebugSpawn(EnemyKind.Buff,1);var d=s.DebugSpawn(EnemyKind.Defense,2);var fourth=s.DebugSpawn(EnemyKind.Attack,0);
        s.Tick(.01f,Away);var p=s.Projectiles.Where(x=>x.Source=="Tower:"+tower.Id).ToArray();Assert.That(p.Length,Is.EqualTo(3));Assert.That(p.Select(x=>x.Target.Id).Distinct().Count(),Is.EqualTo(3));CollectionAssert.Contains(p.Select(x=>x.Target).ToArray(),d);CollectionAssert.Contains(p.Select(x=>x.Target).ToArray(),b);Assert.That(fourth.Health,Is.EqualTo(25));
    }
    [Test] public void SupportAddsDamagePercentOnlyToAttackTowers()
    {
        var s=Game();s.StartDiagnosticEncounter();string r;var t=s.TryInstall(TowerKind.Single,new Vector2Int(2,1),out r);var a=s.TryInstall(TowerKind.Support,new Vector2Int(2,2),out r);var b=s.TryInstall(TowerKind.Support,new Vector2Int(3,1),out r);s.UpgradeTower(b.Id,"buffDamage",out r);s.Data.Waves.Clear();s.StartCombat();s.Tick(.01f,new Vector2(0,1));
        var shot=s.Projectiles.First(x=>x.Source=="Tower:"+t.Id);Assert.That(shot.Damage,Is.EqualTo(12.5f*1.425f).Within(.0001));Assert.That(s.Projectiles.First(x=>x.Source=="Player").Damage,Is.EqualTo(10));Assert.That(s.Projectiles.Any(x=>x.Source=="Tower:"+a.Id||x.Source=="Tower:"+b.Id),Is.False);
    }
    [Test] public void EnemyBuffDoesNotStackAndSourceCannotBuffItself()
    {
        var s=Game();s.StartCombat();var a=s.DebugSpawn(EnemyKind.Attack,1);s.DebugSpawn(EnemyKind.Buff,0);s.DebugSpawn(EnemyKind.Buff,2);s.Tick(.05f,Away);Assert.That(a.PathTravelled,Is.EqualTo(1.06f).Within(.00001));
        var solo=Game();solo.StartCombat();var b=solo.DebugSpawn(EnemyKind.Buff,0);solo.Tick(.05f,Away);Assert.That(b.Buffed,Is.False);Assert.That(b.PathTravelled,Is.EqualTo(.05f).Within(.00001));
    }
    [Test] public void AuraAtBoundaryIsIndependentOfSourceInsertionOrder() { var s=Game();s.StartCombat();s.DebugSpawn(EnemyKind.Buff,3);var a=s.DebugSpawn(EnemyKind.Attack,1);s.Tick(.05f,Away);Assert.That(a.PathTravelled,Is.EqualTo(1.06f).Within(.00001)); }
    [Test] public void DeadTargetProjectilesVanishWithoutCollateralDamage()
    {
        var s=Game();s.StartCombat();var e=s.DebugSpawn(EnemyKind.Attack,0);var other=s.DebugSpawn(EnemyKind.Defense,1);s.Tick(.01f,new Vector2(0,1));Assert.That(s.Projectiles.Count,Is.EqualTo(1));s.ApplyDamage(e,100,"Test");s.Tick(.01f,Away);Assert.That(s.Projectiles.Count,Is.Zero);Assert.That(other.Health,Is.EqualTo(50));Assert.That(s.Gold,Is.EqualTo(3));
    }
}



