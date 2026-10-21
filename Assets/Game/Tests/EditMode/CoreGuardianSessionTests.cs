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
    static float Stat(CombatStats s,string key)=>key=="damage"?s.Damage:key=="rate"?s.Rate:key=="range"?s.Range:key=="projectileSpeed"?s.ProjectileSpeed:s.BuffDamage;
    [Test] public void SegmentedHealthBarUsesDiscreteCellsAndStableFramedWidth()
    {
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardian.SegmentedEnemyHealthBar")).FirstOrDefault(t=>t!=null);
        Assert.That(type,Is.Not.Null,"Framed cell health display is missing");
        var go=new GameObject("SegmentHealthQA",typeof(SpriteRenderer));
        try
        {
            var bar=go.AddComponent(type);var renderer=go.GetComponent<SpriteRenderer>();
            type.GetMethod("Configure").Invoke(bar,new object[]{false});
            type.GetMethod("SetHealth").Invoke(bar,new object[]{20,40});
            Assert.That(type.GetProperty("SegmentCount").GetValue(bar),Is.EqualTo(8));Assert.That(type.GetProperty("FilledSegments").GetValue(bar),Is.EqualTo(4));
            var half=renderer.sprite;float width=half.bounds.size.x;
            type.GetMethod("SetHealth").Invoke(bar,new object[]{19,40});Assert.That(renderer.sprite,Is.SameAs(half),"HP within one cell must not smoothly shrink the display");
            type.GetMethod("SetHealth").Invoke(bar,new object[]{15,40});Assert.That(type.GetProperty("FilledSegments").GetValue(bar),Is.EqualTo(3));
            Assert.That(renderer.sprite.bounds.size.x,Is.EqualTo(width));Assert.That(renderer.sprite.texture.filterMode,Is.EqualTo(FilterMode.Point));
            type.GetMethod("Configure").Invoke(bar,new object[]{true});type.GetMethod("SetHealth").Invoke(bar,new object[]{250,500});
            Assert.That(type.GetProperty("SegmentCount").GetValue(bar),Is.EqualTo(12));Assert.That(type.GetProperty("FilledSegments").GetValue(bar),Is.EqualTo(6));
            type.GetMethod("SetHealth").Invoke(bar,new object[]{0,500});Assert.That(type.GetProperty("FilledSegments").GetValue(bar),Is.Zero);
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
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
        Assert.Throws<ArgumentException>(()=>CombatData.Load(Csv("enemies").Replace("Attack,"+Data().Enemies[EnemyKind.Attack].Health+",","Attack,-1,"),Csv("towers"),Csv("stage"),Csv("upgrades"),Csv("waves")));
        Assert.Throws<ArgumentException>(()=>CombatData.Load(Csv("enemies").Replace("Boss,"+Data().Enemies[EnemyKind.Boss].Health+",0.6,10,120,0,0,0,provisional",""),Csv("towers"),Csv("stage"),Csv("upgrades"),Csv("waves")));
    }
    [Test] public void UndefinedNumericKindsMissingWaveOneAndCaseDuplicateUpgradeAreRejected()
    {
        Assert.Throws<ArgumentException>(()=>CombatData.Load(Csv("enemies").Replace("Buff,30","9,30"),Csv("towers"),Csv("stage"),Csv("upgrades"),Csv("waves")));
        Assert.Throws<ArgumentException>(()=>CombatData.Load(Csv("enemies"),Csv("towers").Replace("Support,75","9,75"),Csv("stage"),Csv("upgrades"),Csv("waves")));
        Assert.Throws<ArgumentException>(()=>CombatData.Load(Csv("enemies"),Csv("towers"),Csv("stage"),Csv("upgrades").Replace(",Support,",",9,"),Csv("waves")));
        Assert.Throws<ArgumentException>(()=>CombatData.Load(Csv("enemies"),Csv("towers"),Csv("stage"),Csv("upgrades"),Csv("waves").Replace(",1,Attack,",",2,Attack,")));
        Assert.Throws<ArgumentException>(()=>CombatData.Load(Csv("enemies"),Csv("towers"),Csv("stage"),Csv("upgrades")+"\ncase_duplicate,single,damage,2.5,80,20",Csv("waves")));
    }
    [Test] public void AttackRateRetainsFractionalCooldownAndSupportsMultipleShotsPerStep()
    {
        string reason;var s=Game();var t=s.TryInstall(TowerKind.Single,new Vector2Int(2,1),out reason);t.Stats.ProjectileSpeed=.001f;s.Data.Enemies[EnemyKind.Attack].Speed=.001f;s.StartCombat();var e=s.DebugSpawn(EnemyKind.Attack,2);e.Health=10000;s.Tick(5.05f,Away);Assert.That(s.Projectiles.Count,Is.EqualTo(7));
        var fast=Game();t=fast.TryInstall(TowerKind.Single,new Vector2Int(2,1),out reason);t.Stats.Rate=100;t.Stats.ProjectileSpeed=.001f;fast.Data.Enemies[EnemyKind.Attack].Speed=.001f;fast.StartCombat();e=fast.DebugSpawn(EnemyKind.Attack,2);e.Health=10000;fast.Tick(.05f,Away);Assert.That(fast.Projectiles.Count,Is.EqualTo(6));
    }
    [Test] public void PlayerPricesHaveIncreasingIncrementAndIndependentStats()
    {
        var d=Data();Assert.That(Enumerable.Range(1,6).Select(d.PlayerUpgradePrice),Is.EqualTo(new[]{40,65,85,110,140,175}));
        var s=Game();s.StartDiagnosticEncounter();string reason;Assert.That(s.UpgradePlayer("damage",out reason),Is.True);Assert.That(s.PlayerUpgradePrice("damage"),Is.EqualTo(65));Assert.That(s.PlayerUpgradePrice("range"),Is.EqualTo(40));
    }
    [Test] public void FirstGrowthBudgetUnlocksPlayerAndTowerAfterTwoRounds()
    {
        // Functional affordability fixture, not a simulated or measured combat run.
        var d=Data();d.StartGold=d.Waves.Where(w=>w.Wave<=2).Sum(w=>w.Count*d.Enemies[w.Kind].Gold);
        d.StartCrystals=d.Towers[TowerKind.Single].Cost+d.RewardForWave(1)+d.RewardForWave(2);
        var s=new Session(d);s.SetBoard(40,25,new Vector2Int(10,0),new[]{new Vector2Int(0,0),new Vector2Int(10,0)});
        string reason;var t=s.TryInstall(TowerKind.Single,new Vector2Int(2,1),out reason);
        Assert.That(s.Gold,Is.EqualTo(93));Assert.That(s.Crystals,Is.EqualTo(120));
        Assert.That(s.UpgradePlayer("damage",out reason),Is.True,reason);Assert.That(s.Gold,Is.EqualTo(53));
        Assert.That(s.UpgradeTower(t.Id,"damage",out reason),Is.True,reason);Assert.That(s.Crystals,Is.EqualTo(60));
        Assert.That(s.TowerUpgradePrice(t.Id,"damage"),Is.EqualTo(90));
        Assert.That(s.TowerUpgradePrice(t.Id,"range"),Is.EqualTo(60));
        Assert.That(s.UpgradeTower(t.Id,"damage",out reason),Is.False,"Insufficient crystals must still reject a purchase");
    }
    [Test] public void FirstGrowthDiscountsAreOptionalAndCannotMakeUpgradesFree()
    {
        var oldStage=string.Join("\n",Csv("stage").Replace("\r","").Split('\n').Where(l=>!l.StartsWith("playerFirstUpgradeDiscount,")&&!l.StartsWith("towerFirstUpgradeDiscount,")));
        var legacy=CombatData.Load(Csv("enemies"),Csv("towers"),oldStage,Csv("upgrades"),Csv("waves"));
        Assert.That(legacy.PlayerUpgradePrice(1),Is.EqualTo(50));
        var s=new Session(legacy);s.SetBoard(40,25,new Vector2Int(10,0),new[]{new Vector2Int(0,0),new Vector2Int(10,0)});
        string reason;var t=s.TryInstall(TowerKind.Single,new Vector2Int(2,1),out reason);Assert.That(s.TowerUpgradePrice(t.Id,"damage"),Is.EqualTo(60));
        Assert.Throws<ArgumentException>(()=>CombatData.Load(Csv("enemies"),Csv("towers"),oldStage+"\nplayerFirstUpgradeDiscount,100",Csv("upgrades"),Csv("waves")));
        Assert.Throws<ArgumentException>(()=>CombatData.Load(Csv("enemies"),Csv("towers"),oldStage+"\ntowerFirstUpgradeDiscount,80",Csv("upgrades"),Csv("waves")));
    }
    [Test] public void TimeoutStartsCombatWithoutTowerAndCurrencyStartsCorrectly()
    {
        var s=Game();Assert.That(s.Gold,Is.Zero);Assert.That(s.Crystals,Is.EqualTo(100));s.Tick(s.Data.PreparationSeconds+.1f,Away);Assert.That(s.Phase,Is.EqualTo(SessionPhase.Combat));
    }
    [Test] public void PlacementRejectsPathCoreDuplicateAndCombatButAllowsPlayerCell()
    {
        var s=Game();string r;Assert.That(s.TryInstall(TowerKind.Single,new Vector2Int(0,0),out r),Is.Null);Assert.That(s.Crystals,Is.EqualTo(100));Assert.That(s.TryInstall(TowerKind.Support,new Vector2Int(2,2),out r),Is.Null);
        var t=s.TryInstall(TowerKind.Single,new Vector2Int(2,2),out r);Assert.That(t,Is.Not.Null);Assert.That(s.IsBuildable(t.Position),Is.False);Assert.That(s.DestroyTower(t.Id),Is.True);Assert.That(s.DestroyTower(t.Id),Is.False);Assert.That(s.Crystals,Is.EqualTo(70));s.StartCombat();Assert.That(s.TryInstall(TowerKind.Single,new Vector2Int(3,3),out r),Is.Null);
    }
    [TestCase(TowerKind.Single,"damage")][TestCase(TowerKind.Multi,"damage")][TestCase(TowerKind.Support,"buffDamage")]
    [TestCase(TowerKind.Single,"rate")][TestCase(TowerKind.Single,"range")][TestCase(TowerKind.Single,"projectileSpeed")]
    [TestCase(TowerKind.Multi,"rate")][TestCase(TowerKind.Multi,"range")][TestCase(TowerKind.Multi,"projectileSpeed")][TestCase(TowerKind.Support,"range")]
    public void SharedTowerUpgradeAppliesToExistingAndFutureOfSameKind(TowerKind kind,string stat)
    {
        var s=Game();s.StartDiagnosticEncounter();string r;
        if(kind==TowerKind.Support)s.TryInstall(TowerKind.Single,new Vector2Int(1,1),out r);
        var a=s.TryInstall(kind,new Vector2Int(2,1),out r);var b=s.TryInstall(kind,new Vector2Int(3,1),out r);
        var other=s.TryInstall(kind==TowerKind.Single?TowerKind.Multi:TowerKind.Single,new Vector2Int(4,1),out r);
        float before=Stat(a.Stats,stat),otherBefore=other.Stats.Damage;int crystals=s.Crystals;
        float delta=s.Data.Upgrades.Single(u=>u.Target==kind.ToString()&&u.Stat==stat).Delta;
        Assert.That(s.UpgradeTower(a.Id,stat,out r),Is.True,r);
        Assert.That(crystals-s.Crystals,Is.EqualTo(60));Assert.That(Stat(b.Stats,stat),Is.EqualTo(before+delta).Within(.001));
        Assert.That(other.Stats.Damage,Is.EqualTo(otherBefore));Assert.That(s.TowerUpgradePrice(b.Id,stat),Is.EqualTo(90));
        var future=s.TryInstall(kind,new Vector2Int(5,1),out r);
        Assert.That(Stat(future.Stats,stat),Is.EqualTo(before+delta).Within(.001));Assert.That(future.Stats.UpgradeSteps[stat],Is.EqualTo(1));
        foreach(var t in s.Towers.Where(t=>t.Kind==kind).ToArray())s.DestroyTower(t.Id);
        var replacement=s.TryInstall(kind,new Vector2Int(2,1),out r);
        Assert.That(Stat(replacement.Stats,stat),Is.EqualTo(before+delta).Within(.001));Assert.That(s.TowerUpgradePrice(replacement.Id,stat),Is.EqualTo(90));
        s.StartDiagnosticEncounter();if(kind==TowerKind.Support)s.TryInstall(TowerKind.Single,new Vector2Int(1,1),out r);
        var reset=s.TryInstall(kind,new Vector2Int(2,1),out r);Assert.That(Stat(reset.Stats,stat),Is.EqualTo(before));Assert.That(s.TowerUpgradePrice(reset.Id,stat),Is.EqualTo(60));
    }
    [Test] public void RefundReturnsInstallationOnlyAndSharedInvestmentRemains()
    {
        var s=Game();s.StartDiagnosticEncounter();string r;var t=s.TryInstall(TowerKind.Single,new Vector2Int(2,1),out r);Assert.That(s.UpgradeTower(t.Id,"damage",out r),Is.True);Assert.That(s.UpgradeTower(t.Id,"damage",out r),Is.True);Assert.That(t.Spent,Is.EqualTo(60));int before=s.Crystals;s.DestroyTower(t.Id);Assert.That(s.Crystals-before,Is.EqualTo(30));Assert.That(s.DestroyTower(t.Id),Is.False);
    }
    [Test] public void RejectedSharedUpgradeCannotSpendOrChangeCurrentAndFutureTowers()
    {
        var s=Game();string r;var a=s.TryInstall(TowerKind.Single,new Vector2Int(2,1),out r);
        Assert.That(s.UpgradeTower(a.Id,"damage",out r),Is.False);Assert.That(r,Is.EqualTo("수정 부족"));
        Assert.That(s.Crystals,Is.EqualTo(40));Assert.That(a.Stats.Damage,Is.EqualTo(s.Data.Towers[TowerKind.Single].Damage));
        Assert.That(s.TowerUpgradePrice(a.Id,"damage"),Is.EqualTo(60));
        s.StartDiagnosticEncounter();a=s.TryInstall(TowerKind.Single,new Vector2Int(2,1),out r);int crystals=s.Crystals;
        Assert.That(s.UpgradeTower(a.Id,"invalid",out r),Is.False);Assert.That(s.UpgradeTower(-1,"damage",out r),Is.False);
        s.StartCombat();Assert.That(s.UpgradeTower(a.Id,"damage",out r),Is.False);Assert.That(s.Crystals,Is.EqualTo(crystals));
        Assert.That(s.TowerUpgradePrice(a.Id,"damage"),Is.EqualTo(60));Assert.That(a.Stats.UpgradeSteps,Is.Empty);
    }
    [Test] public void KillGoldAndActualDamageAreAccountedOnceWithoutOverkill()
    {
        var s=Game();var e=s.DebugSpawn(EnemyKind.Attack);int initialHealth=e.Health;Assert.That(s.ApplyDamage(e,100,"Player"),Is.EqualTo(initialHealth));Assert.That(s.ApplyDamage(e,100,"Player"),Is.Zero);Assert.That(s.Gold,Is.EqualTo(3));Assert.That(s.DamageTotals["Player"],Is.EqualTo(initialHealth));
    }
    [Test] public void CoreArrivalNeverRewardsAndNeverHitsTwice()
    {
        var s=Game();s.StartCombat();s.DebugSpawn(EnemyKind.Attack,9.99f);s.Tick(.1f,Away);Assert.That(s.CoreEnergy,Is.EqualTo(18));Assert.That(s.Gold,Is.Zero);s.Tick(1,Away);Assert.That(s.CoreEnergy,Is.EqualTo(18));
    }
    [Test] public void HordeAfterTwoUsesThirtyRoundCountsAndPreservesIntro()
    {
        var d=Data();Assert.That(d.Waves.Where(w=>w.Wave==1).Max(w=>w.Start+(w.Count-1)*w.Interval),Is.EqualTo(22.68f).Within(.01f));
        Assert.That(d.Waves.Where(w=>w.Wave==2).Max(w=>w.Start+(w.Count-1)*w.Interval),Is.EqualTo(22.95f).Within(.01f));
        int[] counts={23,26,31,35,38,42,46};int[] gold={72,82,102,116,126,142,156};
        for(int wave=3;wave<=9;wave++)
        {
            var rows=d.Waves.Where(w=>w.Wave==wave).OrderBy(w=>w.Start).ToArray();
            Assert.That(rows.Sum(w=>w.Count),Is.EqualTo(counts[wave-3]));Assert.That(rows.Sum(w=>w.Count*d.Enemies[w.Kind].Gold),Is.EqualTo(gold[wave-3]));
            Assert.That(rows.Last().Start,Is.LessThanOrEqualTo(16f),"Spawn pressure must be substantially denser after round 2");
        }
        Assert.That(d.Waves.First(w=>w.Wave==11&&w.Kind==EnemyKind.Attack).Interval,Is.EqualTo(.34f));
        Assert.That(d.Waves.Single(w=>w.Wave==10&&w.Kind==EnemyKind.Boss).Start,Is.EqualTo(.75f));
    }
    [Test] public void BossScreenWaveTenSpawnsThreeQuickShieldsBeforeBoss()
    {
        var d=Data();var schedule=d.Waves.Where(w=>w.Wave==10).ToArray();
        Assert.That(schedule.Sum(w=>w.Count),Is.EqualTo(38));
        d.Waves.Clear();foreach(var w in schedule)d.Waves.Add(new WaveSpec{Wave=1,Kind=w.Kind,Count=w.Count,Interval=w.Interval,Start=w.Start});
        var s=new Session(d);s.SetBoard(40,25,new Vector2Int(10,0),new[]{new Vector2Int(0,0),new Vector2Int(10,0)});s.StartCombat();
        s.Tick(.01f,Away);Assert.That(s.Enemies.Select(e=>e.Kind),Is.EqualTo(new[]{EnemyKind.Defense}));
        s.Tick(.25f,Away);Assert.That(s.Enemies.Count,Is.EqualTo(2));Assert.That(s.Enemies.All(e=>e.Kind==EnemyKind.Defense),Is.True);
        s.Tick(.25f,Away);Assert.That(s.Enemies.Count,Is.EqualTo(3));Assert.That(s.Enemies.All(e=>e.Kind==EnemyKind.Defense),Is.True);
        s.Tick(.25f,Away);Assert.That(s.Enemies.Select(e=>e.Kind),Is.EqualTo(new[]{EnemyKind.Defense,EnemyKind.Defense,EnemyKind.Defense,EnemyKind.Boss}));
    }
    [TestCase(TowerKind.Single)][TestCase(TowerKind.Multi)]
    public void BossScreenTowersPutEvenLowHealthFirstArrivalBossLast(TowerKind kind)
    {
        var s=Game();string reason;var t=s.TryInstall(kind,new Vector2Int(2,1),out reason);t.Stats.ProjectileSpeed=.001f;s.StartCombat();
        var boss=s.DebugSpawn(EnemyKind.Boss,0);boss.Health=1;
        s.DebugSpawn(EnemyKind.Attack,.2f);s.DebugSpawn(EnemyKind.Buff,.4f);s.DebugSpawn(EnemyKind.Defense,.6f);
        s.Tick(.01f,Away);var shots=s.Projectiles.Where(p=>p.Source=="Tower:"+t.Id).ToArray();
        Assert.That(shots,Is.Not.Empty);Assert.That(shots.All(p=>p.Target!=boss),Is.True);
        Assert.That(s.TowerTarget(t.Id),Is.Not.SameAs(boss));
    }
    [TestCase(TowerKind.Single)][TestCase(TowerKind.Multi)]
    public void BossScreenTowersYieldBossLockWhenOrdinaryEnemyEntersRange(TowerKind kind)
    {
        var s=Game();string reason;var t=s.TryInstall(kind,new Vector2Int(2,1),out reason);t.Stats.ProjectileSpeed=.001f;s.StartCombat();
        var boss=s.DebugSpawn(EnemyKind.Boss,0);s.Tick(.01f,Away);Assert.That(s.TowerTarget(t.Id),Is.SameAs(boss));
        s.Projectiles.Clear();var attack=s.DebugSpawn(EnemyKind.Attack,0);s.Tick(2f,Away);
        Assert.That(s.TowerTarget(t.Id),Is.SameAs(attack));
        Assert.That(s.Projectiles.First(p=>p.Source=="Tower:"+t.Id).Target,Is.SameAs(attack));
    }
    [Test] public void BossScreenDoesNotChangePlayerExistingBossLock()
    {
        var s=Game();s.Player.ProjectileSpeed=.001f;s.StartCombat();var boss=s.DebugSpawn(EnemyKind.Boss,0);
        s.Tick(.01f,new Vector2(0,1));Assert.That(s.PlayerTarget,Is.SameAs(boss));
        s.DebugSpawn(EnemyKind.Attack,0);s.Tick(1.05f,new Vector2(0,1));Assert.That(s.PlayerTarget,Is.SameAs(boss));
    }
    [Test] public void PriorityUsesRoleThenHealthThenRemainingPathDistance()
    {
        var s=Game();var a=s.DebugSpawn(EnemyKind.Attack,9);var b=s.DebugSpawn(EnemyKind.Buff,9);var d=s.DebugSpawn(EnemyKind.Defense,1);var d2=s.DebugSpawn(EnemyKind.Defense,2);Assert.That(CombatRules.Priority(s.Enemies),Is.SameAs(d2));d.Health=1;Assert.That(CombatRules.Priority(s.Enemies),Is.SameAs(d));
    }
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
    public void EqualRoleAndHealthPriorityUsesRemainingRouteAcrossAllTravelDirections(int turns)
    {
        Vector2Int Rotate(Vector2Int point)
        {
            for(int i=0;i<turns;i++)point=new Vector2Int(-point.y,point.x);
            return point+new Vector2Int(12,12);
        }
        var route=new[]{new Vector2Int(0,0),new Vector2Int(10,0),new Vector2Int(10,10),new Vector2Int(0,10),new Vector2Int(0,1),new Vector2Int(8,1)}.Select(Rotate).ToArray();
        var session=new Session(Data(),17);var core=route[route.Length-1];session.SetBoard(30,30,core,route);
        var straightNear=session.DebugSpawn(EnemyKind.Attack,8);
        var routeNear=session.DebugSpawn(EnemyKind.Attack,40);
        Assert.That(routeNear.Health,Is.EqualTo(straightNear.Health));
        Assert.That(Vector2.Distance(straightNear.Position,core),Is.LessThan(Vector2.Distance(routeNear.Position,core)));
        Assert.That(routeNear.PathRemaining,Is.LessThan(straightNear.PathRemaining));
        Assert.That(CombatRules.Priority(session.Enemies),Is.SameAs(routeNear));
        Assert.That(CombatRules.CompareTargetPriority(routeNear,straightNear,false),Is.LessThan(0));
        Assert.That(CombatRules.TowerPriorityOrder(session.Enemies).First(),Is.SameAs(routeNear));
    }
    [Test] public void FirstTargetIsFirstArrivalThenRangeExitReacquiresPriority()
    {
        var s=Game();s.StartCombat();var attack=s.DebugSpawn(EnemyKind.Attack,0);var defense=s.DebugSpawn(EnemyKind.Defense,1);s.Tick(.01f,new Vector2(0,1));Assert.That(s.Projectiles.Single().Target,Is.SameAs(attack));
        attack.Position=new Vector2(-100,-100);attack.PathTravelled=9;s.Tick(1.05f,new Vector2(0,1));Assert.That(s.Projectiles.Any(p=>p.Target==defense),Is.True);
    }
    [Test] public void MultishotUsesThreeDistinctTargetsWithNoSplash()
    {
        var s=Game();string r;var tower=s.TryInstall(TowerKind.Multi,new Vector2Int(2,1),out r);s.StartCombat();var a=s.DebugSpawn(EnemyKind.Attack,0);var b=s.DebugSpawn(EnemyKind.Buff,1);var d=s.DebugSpawn(EnemyKind.Defense,2);var fourth=s.DebugSpawn(EnemyKind.Attack,0);
        s.Tick(.01f,Away);var p=s.Projectiles.Where(x=>x.Source=="Tower:"+tower.Id).ToArray();Assert.That(p.Length,Is.EqualTo(3));Assert.That(p.Select(x=>x.Target.Id).Distinct().Count(),Is.EqualTo(3));CollectionAssert.Contains(p.Select(x=>x.Target).ToArray(),d);CollectionAssert.Contains(p.Select(x=>x.Target).ToArray(),b);Assert.That(fourth.Health,Is.EqualTo(s.Data.Enemies[EnemyKind.Attack].Health));
    }
    [Test] public void SupportAddsDamagePercentOnlyToAttackTowers()
    {
        var s=Game();s.StartDiagnosticEncounter();string r;var t=s.TryInstall(TowerKind.Single,new Vector2Int(2,1),out r);var a=s.TryInstall(TowerKind.Support,new Vector2Int(2,2),out r);var b=s.TryInstall(TowerKind.Support,new Vector2Int(3,1),out r);s.UpgradeTower(b.Id,"buffDamage",out r);s.Data.Waves.Clear();s.StartCombat();s.Tick(.01f,new Vector2(0,1));
        var shot=s.Projectiles.First(x=>x.Source=="Tower:"+t.Id);Assert.That(shot.Damage,Is.EqualTo(12.5f*1.45f).Within(.0001));Assert.That(s.Projectiles.First(x=>x.Source=="Player").Damage,Is.EqualTo(10));Assert.That(s.Projectiles.Any(x=>x.Source=="Tower:"+a.Id||x.Source=="Tower:"+b.Id),Is.False);
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
    static ProjectileState Shot(EnemyState target,Vector2 position,float damage=1)=>new ProjectileState{Target=target,Position=position,Damage=damage,Speed=0,Source="Test"};
    static Session ProjectileGame(out EnemyState target)
    {
        var s=Game();s.StartCombat();s.Player.Rate=0;target=s.DebugSpawn(EnemyKind.Attack,1);target.Spec.Speed=0;target.Health=1000;return s;
    }
    [Test] public void ProjectileBatchPreservesSurvivorOrderAndDuplicateRemovalCounts()
    {
        var s=ProjectileGame(out var live);var dead=s.DebugSpawn(EnemyKind.Defense,2);dead.Health=0;
        var a=Shot(live,new Vector2(20,20));var b=Shot(live,new Vector2(21,20));var removed=Shot(dead,Vector2.zero);var hit=Shot(live,live.Position);
        s.Projectiles.AddRange(new[]{removed,a,removed,hit,b});s.Tick(.01f,Away);
        CollectionAssert.AreEqual(new[]{a,b},s.Projectiles);Assert.That(live.Health,Is.EqualTo(999));
    }
    [Test] public void ProjectileObserverSeesPriorRemovalsAndMayReplaceList()
    {
        var s=ProjectileGame(out var live);var dead=s.DebugSpawn(EnemyKind.Defense,2);dead.Health=0;
        var removed=Shot(dead,Vector2.zero);var hit=Shot(live,live.Position);var later=Shot(live,live.Position);var added=Shot(live,new Vector2(20,20));int calls=0;
        s.Projectiles.AddRange(new[]{removed,hit,later});
        s.DamageApplied+=(target,damage)=>
        {
            if(calls++==0){CollectionAssert.AreEqual(new[]{hit,later},s.Projectiles);s.Projectiles.Clear();s.Projectiles.Add(added);}
            else CollectionAssert.AreEqual(new[]{added},s.Projectiles);
        };
        s.Tick(.01f,Away);Assert.That(calls,Is.EqualTo(2));Assert.That(live.Health,Is.EqualTo(998));CollectionAssert.AreEqual(new[]{added},s.Projectiles);
    }
    [Test] public void ProjectileObserverCanReenterWithIndependentRemovalBuffers()
    {
        var s=ProjectileGame(out var live);var dead=s.DebugSpawn(EnemyKind.Defense,2);dead.Health=0;
        var hit=Shot(live,live.Position);s.Projectiles.AddRange(new[]{Shot(dead,Vector2.zero),hit});int calls=0;
        Action<EnemyState,int> observer=null;
        observer=(target,damage)=>{calls++;CollectionAssert.AreEqual(new[]{hit},s.Projectiles);s.DamageApplied-=observer;s.Tick(.01f,Away);};
        s.DamageApplied+=observer;s.Tick(.01f,Away);
        Assert.That(calls,Is.EqualTo(1));Assert.That(live.Health,Is.EqualTo(998));Assert.That(s.Projectiles,Is.Empty);
    }
    [Test] public void ProjectileObserverExceptionKeepsCurrentShotAfterPriorRemovals()
    {
        var s=ProjectileGame(out var live);var dead=s.DebugSpawn(EnemyKind.Defense,2);dead.Health=0;
        var hit=Shot(live,live.Position);s.Projectiles.AddRange(new[]{Shot(dead,Vector2.zero),hit});
        s.DamageApplied+=(target,damage)=>throw new InvalidOperationException("Expected observer failure");
        Assert.Throws<InvalidOperationException>(()=>s.Tick(.01f,Away));
        CollectionAssert.AreEqual(new[]{hit},s.Projectiles);Assert.That(live.Health,Is.EqualTo(999));
    }
    [Test] public void ProjectileBatchFlushesEarlierRemovalsOnProcessingException()
    {
        var s=ProjectileGame(out var live);var dead=s.DebugSpawn(EnemyKind.Defense,2);dead.Health=0;
        var invalid=Shot(null,Vector2.zero);s.Projectiles.AddRange(new[]{Shot(dead,Vector2.zero),invalid});
        Assert.Throws<NullReferenceException>(()=>s.Tick(.01f,Away));CollectionAssert.AreEqual(new[]{invalid},s.Projectiles);
    }
    [Test] public void ProjectileWithNonFinitePositionDoesNotBecomeAHit()
    {
        var s=ProjectileGame(out var live);var shot=Shot(live,new Vector2(float.NaN,0));s.Projectiles.Add(shot);
        s.Tick(.01f,Away);CollectionAssert.AreEqual(new[]{shot},s.Projectiles);Assert.That(live.Health,Is.EqualTo(1000));
    }
    [Test] public void TowerSourcesAreReusedAndTrackPublicIdChanges()
    {
        var tower=new TowerState{Id=12};string source=tower.AttackSource,support=tower.SupportSource;
        Assert.That(tower.AttackSource,Is.SameAs(source));Assert.That(tower.SupportSource,Is.SameAs(support));
        tower.Id=19;Assert.That(tower.AttackSource,Is.EqualTo("Tower:19"));Assert.That(tower.SupportSource,Is.EqualTo("Support:19"));
    }
    [Test] public void CachedTowerLookupTracksReorderingDuplicateIdsReplacementAndRemoval()
    {
        var s=Game();var a=new TowerState{Id=7};var b=new TowerState{Id=7};s.Towers.AddRange(new[]{a,b});
        Assert.That(s.TowerFromSource("Tower:7"),Is.SameAs(a));s.Towers.Reverse();Assert.That(s.FindTower(7),Is.SameAs(b));
        b.Id=8;Assert.That(s.TowerFromSource("Tower:7"),Is.SameAs(a));
        var replacement=new TowerState{Id=7};s.Towers[1]=replacement;Assert.That(s.TowerFromSource("Tower:7"),Is.SameAs(replacement));
        s.Towers.Clear();Assert.That(s.TowerFromSource("Tower:7"),Is.Null);Assert.That(s.TowerFromSource("Player"),Is.Null);Assert.That(s.TowerFromSource("Tower:bad"),Is.Null);
    }
    [Test] public void NegativeTowerSourceFormattingAndParsingTrackCultureSignChanges()
    {
        var s=Game();var tower=new TowerState{Id=-12};s.Towers.Add(tower);
        var original=System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            var culture=(System.Globalization.CultureInfo)System.Globalization.CultureInfo.InvariantCulture.Clone();
            System.Globalization.CultureInfo.CurrentCulture=culture;
            Assert.That(s.TowerFromSource(tower.AttackSource),Is.SameAs(tower));
            culture.NumberFormat.NegativeSign="~";
            Assert.That(tower.AttackSource,Is.EqualTo("Tower:~12"));Assert.That(tower.SupportSource,Is.EqualTo("Support:~12"));
            Assert.That(s.TowerFromSource(tower.AttackSource),Is.SameAs(tower));Assert.That(s.TowerFromSource("Tower:-12"),Is.Null);
        }
        finally{System.Globalization.CultureInfo.CurrentCulture=original;}
    }
    [Test] public void TowerSupportRangeIncludesZeroBuffAndTracksDirectChanges()
    {
        var s=Game();var attack=new TowerState{Id=1,Kind=TowerKind.Single,Position=new Vector2Int(2,1)};
        var support=new TowerState{Id=2,Kind=TowerKind.Support,Position=new Vector2Int(2,2),Stats=new CombatStats{Range=1,BuffDamage=0}};
        s.Towers.AddRange(new[]{attack,support});Assert.That(s.HasTowerSupport(attack),Is.True);
        support.Stats.Range=.5f;Assert.That(s.HasTowerSupport(attack),Is.False);
        support.Position=attack.Position;Assert.That(s.HasTowerSupport(attack),Is.True);
        support.Kind=TowerKind.Single;Assert.That(s.HasTowerSupport(attack),Is.False);
        support.Kind=TowerKind.Support;s.Towers.Remove(support);Assert.That(s.HasTowerSupport(attack),Is.False);
    }
    [Test] public void SupportCacheRefreshesBetweenLaunchCallbacksAndOnTheNextTick()
    {
        var s=Game();s.StartDiagnosticEncounter();string reason;
        var a=s.TryInstall(TowerKind.Single,new Vector2Int(2,1),out reason);var b=s.TryInstall(TowerKind.Single,new Vector2Int(3,1),out reason);
        var support=s.TryInstall(TowerKind.Support,new Vector2Int(2,2),out reason);
        a.Stats.ProjectileSpeed=b.Stats.ProjectileSpeed=.001f;
        s.StartCombat();s.Player.Rate=0;var enemy=s.DebugSpawn(EnemyKind.Attack,2);enemy.Spec.Speed=0;enemy.Health=10000;
        support.Stats.Range=10;support.Stats.BuffDamage=0;
        s.ProjectileLaunched+=shot=>{if(shot.Source==a.AttackSource)support.Stats.BuffDamage=100;};
        s.Tick(.01f,Away);
        Assert.That(s.Projectiles.First(p=>p.Source==a.AttackSource).Damage,Is.EqualTo(a.Stats.Damage));
        Assert.That(s.Projectiles.First(p=>p.Source==b.AttackSource).Damage,Is.EqualTo(b.Stats.Damage*2));
        support.Stats.BuffDamage=200;Assert.That(s.HasTowerSupport(b),Is.True);
        // A zero-rate first tower prevents its observer from replacing the next-tick edit.
        a.Stats.Rate=0;b.Stats.Rate=100;s.Projectiles.Clear();s.Tick(.05f,Away);
        // Existing cooldown can delay b; advance enough for the next real volley.
        s.Tick(2,Away);Assert.That(s.Projectiles.Any(p=>p.Source==b.AttackSource&&p.Damage==b.Stats.Damage*3),Is.True);
    }
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
    public void IndexedPathPositionMatchesSequentialSubtractionAtTurnsAndFractions(int turns)
    {
        Vector2Int Rotate(Vector2Int p){for(int i=0;i<turns;i++)p=new Vector2Int(-p.y,p.x);return p+new Vector2Int(12,12);}
        var route=new[]{new Vector2Int(0,0),new Vector2Int(10,0),new Vector2Int(10,10),new Vector2Int(0,10),new Vector2Int(0,1),new Vector2Int(8,1)}.Select(Rotate).ToArray();
        var s=new Session(Data());s.SetBoard(30,30,route.Last(),route);
        foreach(float travelled in new[]{0,.00001f,9.999999f,10,10.000001f,19.999998f,20,30,39,40.125f,47,float.NaN})
        {
            float d=travelled;Vector2 expected=route.Last();
            for(int i=0;i<route.Length-1;i++){float len=Vector2.Distance(route[i],route[i+1]);if(d<=len){expected=Vector2.Lerp(route[i],route[i+1],d/len);break;}d-=len;}
            var enemy=s.DebugSpawn(EnemyKind.Attack,travelled);Assert.That(enemy.Position.Equals(expected),Is.True,"distance "+travelled);
            Assert.That(enemy.PathRemaining,float.IsNaN(travelled)?Is.EqualTo(0):Is.EqualTo(Mathf.Max(0,47-travelled)));
        }
    }
    [Test] public void ViewTargetIndexTracksPositionHealthRevivalAndMembershipChanges()
    {
        var s=Game();var tower=new TowerState{Id=1,Kind=TowerKind.Multi,Position=new Vector2Int(2,1),Stats=new CombatStats{Range=2}};s.Towers.Add(tower);
        for(int i=0;i<96;i++)s.DebugSpawn(EnemyKind.Attack,1+i%8);
        s.BeginTowerViews();
        try
        {
            Assert.That(s.TowerTargetForView(tower),Is.SameAs(s.TowerTarget(tower.Id)));
            var enemy=s.Enemies[50];enemy.Position=tower.Position;enemy.Health=1;
            Assert.That(s.TowerTargetForView(tower),Is.SameAs(enemy));
            enemy.Health=0;Assert.That(s.TowerTargetForView(tower),Is.SameAs(s.TowerTarget(tower.Id)));
            enemy.Health=1;s.Enemies.Reverse();Assert.That(s.TowerTargetForView(tower),Is.SameAs(enemy));
            s.Enemies.Remove(enemy);Assert.That(s.TowerTargetForView(tower),Is.SameAs(s.TowerTarget(tower.Id)));
            tower.Stats.Range=float.PositiveInfinity;Assert.That(s.TowerTargetForView(tower),Is.SameAs(s.TowerTarget(tower.Id)));
        }
        finally{s.EndTowerViews();}
        Assert.That(s.TowerTargetForView(tower),Is.SameAs(s.TowerTarget(tower.Id)));
    }
    [TestCase(false)] [TestCase(true)]
    public void AttackIndexHandlesUnchangedAndMutatingLaunchObservers(bool mutate)
    {
        var s=Game();s.StartDiagnosticEncounter();string reason;var tower=s.TryInstall(TowerKind.Single,new Vector2Int(30,1),out reason);
        tower.Stats.Range=2;s.StartCombat();var first=s.DebugSpawn(EnemyKind.Attack,1);first.Spec.Speed=0;first.Health=10000;
        for(int i=0;i<95;i++)s.DebugSpawn(EnemyKind.Attack,1+i%8);
        bool observed=false;
        s.ProjectileLaunched+=shot=>{if(!observed&&shot.Source=="Player"){observed=true;if(mutate)first.Position=new Vector2(30,0);}};
        s.Tick(.01f,new Vector2(1,1));Assert.That(observed,Is.True);
        Assert.That(s.Projectiles.Any(p=>p.Source==tower.AttackSource&&ReferenceEquals(p.Target,first)),Is.EqualTo(mutate));
    }
}



