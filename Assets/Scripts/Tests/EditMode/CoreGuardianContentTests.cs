using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using NUnit.Framework;
using UnityEngine;

// New towers, enemies, bosses, effect tags and the area blast (2026-10-26/27 Notion rules) on live data.
public class CoreGuardianContentTests
{
    static string Csv(string key)=>Resources.Load<TextAsset>("CoreGuardian/"+key).text;
    static CombatData Data(){var d=CombatData.Load(Csv("enemies"),Csv("towers"),Csv("stage"),Csv("upgrades"),Csv("waves"));d.Waves.RemoveAll(w=>w.Wave>1);foreach(var w in d.Waves)w.Start=10000;return d;}
    // Straight path along y=0 from (start,0) to the core at (end,0).
    static Session Game(CombatData d=null,int start=0,int end=30,int crystals=1000)
    {
        d=d??Data();d.StartCrystals=crystals;var s=new Session(d,11);s.SetBoard(Math.Max(end+2,40),25,new Vector2Int(end,0),new[]{new Vector2Int(start,0),new Vector2Int(end,0)});return s;
    }
    static EnemySpec Dummy(CombatData d,int health=1000,float defense=0,EnemyKind kind=EnemyKind.Attack){var e=d.Enemies[kind].Clone();e.Health=health;e.Defense=defense;e.Speed=.0001f;return e;}
    static readonly Vector2 Away=new Vector2(-50,20);

    [Test] public void FrostPulseHitsEveryoneInRadiusAndBossTakesHalfSlow()
    {
        var s=Game();string reason;Assert.That(s.TryInstall(TowerKind.Frost,new Vector2Int(5,1),out reason),Is.Not.Null,reason);
        s.StartCombat();
        var a=s.DebugSpawn(EnemyKind.Attack,5,Dummy(s.Data));var b=s.DebugSpawn(EnemyKind.Attack,6,Dummy(s.Data));var far=s.DebugSpawn(EnemyKind.Attack,9,Dummy(s.Data));
        var boss=s.DebugSpawn(EnemyKind.Boss,4,Dummy(s.Data,5000,0,EnemyKind.Boss));
        int pulses=0;s.TowerPulsed+=(id,p,r)=>pulses++;
        s.Tick(.05f,Away);
        Assert.That(pulses,Is.EqualTo(1));
        Assert.That(new[]{a.Health,b.Health,boss.Health},Is.EqualTo(new[]{995,995,4995}),"5 damage to everyone within radius 2");
        Assert.That(far.Health,Is.EqualTo(1000));
        Assert.That(a.SlowPercent,Is.EqualTo(30).Within(.01));Assert.That(boss.SlowPercent,Is.EqualTo(15).Within(.01));
        s.Tick(1.9f,Away);Assert.That(pulses,Is.EqualTo(1),"0.5 pulses per second");s.Tick(.1f,Away);Assert.That(pulses,Is.EqualTo(2));
    }
    [Test] public void FlameBurnStacksTickEverySecondAndIgnoreHalfDefense()
    {
        var s=Game();string reason;s.TryInstall(TowerKind.Flame,new Vector2Int(5,1),out reason);s.StartCombat();
        var e=s.DebugSpawn(EnemyKind.Attack,5,Dummy(s.Data,1000,50));
        s.Tick(.3f,Away);Assert.That(e.BurnStacks,Is.EqualTo(1));Assert.That(e.Health,Is.EqualTo(997),"hit 5 × (1-0.5) = 2.5 → 3");
        s.Tick(.94f,Away);Assert.That(e.Health,Is.EqualTo(995),"tick 3 × (1-0.25) = 2.25 → 2, 0.25 carried");
    }
    [Test] public void PiercingRoundFliesStraightToRangeEndAndHitsTwo()
    {
        // 5 꿰뚫는 탄: 관통 규칙대로 사거리 끝까지 직진, 1명 관통(총 2명), 통과마다 ×0.8.
        var s=Game(null,2,30);s.DebugGrantAugmentation(5);s.StartCombat();
        var first=s.DebugSpawn(EnemyKind.Attack,1,Dummy(s.Data));var second=s.DebugSpawn(EnemyKind.Attack,1.5f,Dummy(s.Data));var third=s.DebugSpawn(EnemyKind.Attack,2,Dummy(s.Data));
        s.Tick(.6f,new Vector2(1,0));
        Assert.That(new[]{first.Health,second.Health,third.Health},Is.EqualTo(new[]{990,992,1000}),"10 → 8, then stops after two");
        Assert.That(s.Projectiles,Is.Empty);
    }
    [Test] public void RailPiercesInALineWithFalloff()
    {
        var s=Game(null,2,30);string reason;Assert.That(s.TryInstall(TowerKind.Rail,new Vector2Int(1,0),out reason),Is.Not.Null,reason);s.StartCombat();
        var e1=s.DebugSpawn(EnemyKind.Attack,1,Dummy(s.Data));var e2=s.DebugSpawn(EnemyKind.Attack,2,Dummy(s.Data));var e3=s.DebugSpawn(EnemyKind.Attack,3,Dummy(s.Data));
        s.Tick(.6f,Away);
        Assert.That(new[]{e1.Health,e2.Health,e3.Health},Is.EqualTo(new[]{965,972,978}),"35 → 28 → 22.4");
        Assert.That(s.Projectiles,Is.Empty);
    }
    [Test] public void SplitterLeavesTwoChildrenAheadAndBehind()
    {
        var s=Game();s.StartCombat();var splitter=s.DebugSpawn(EnemyKind.Splitter,10);
        Assert.That(splitter.Spec.Health,Is.EqualTo(60));
        s.ApplyDamage(splitter,1000,"Test");
        var kids=s.Enemies.Where(e=>e.Alive&&e.Kind==EnemyKind.SplitChild).ToList();
        Assert.That(kids.Count,Is.EqualTo(2));Assert.That(kids.Select(k=>k.PathTravelled).OrderBy(x=>x),Is.EqualTo(new[]{9.7f,10.3f}).Within(.001f));
        Assert.That(kids.All(k=>k.Spec.Health==16&&k.Spec.Gold==2&&Mathf.Approximately(k.Spec.Speed,1.3f)),Is.True);
        s.ApplyDamage(kids[0],1000,"Test");Assert.That(s.Enemies.Count(e=>e.Alive&&e.Kind==EnemyKind.SplitChild),Is.EqualTo(1),"Children never split again");
    }
    [Test] public void OverlappingHealersEachHeal()
    {
        var s=Game(null,0,200);s.StartCombat();
        var healer=s.DebugSpawn(EnemyKind.Healer,5);var other=s.DebugSpawn(EnemyKind.Healer,5);var hurt=s.DebugSpawn(EnemyKind.Attack,5.5f);
        s.ApplyDamage(hurt,20,"Test");Assert.That(hurt.Health,Is.EqualTo(20));
        s.Tick(3.05f,Away);Assert.That(hurt.Health,Is.EqualTo(26),"8% of 40 from each healer, no overlap limit");
    }
    [Test] public void FlyersTakeTheShortcutRoute()
    {
        var d=Data();d.StartCrystals=0;var s=new Session(d,1);
        s.SetBoard(39,22,new Vector2Int(25,8),new[]{new Vector2Int(0,10),new Vector2Int(10,10),new Vector2Int(10,16),new Vector2Int(30,16),new Vector2Int(30,5),new Vector2Int(15,5),new Vector2Int(15,10),new Vector2Int(25,10),new Vector2Int(25,8)});
        s.SetFlyingRoute(new[]{new Vector2(0,10),new Vector2(10,10),new Vector2(10,16),new Vector2(30,16),new Vector2(15,10),new Vector2(25,10),new Vector2(25,8)});
        s.StartCombat();var flyer=s.DebugSpawn(EnemyKind.Flyer);var walker=s.DebugSpawn(EnemyKind.Attack);
        Assert.That(flyer.Flying,Is.True);Assert.That(walker.Flying,Is.False);
        Assert.That(flyer.PathRemaining,Is.EqualTo(10+6+20+Mathf.Sqrt(15*15+6*6)+10+2).Within(.01f),"약 64칸");
        Assert.That(walker.PathRemaining,Is.EqualTo(79).Within(.01f));
        flyer.PathTravelled=38;s.Tick(.01f,Away);Assert.That(flyer.Position.y,Is.LessThan(16).And.GreaterThan(10),"Cuts diagonally across the inner loop");
    }
    [Test] public void IronKnightShieldDeploysOnceWhenTwoThresholdsFallTogether()
    {
        var s=Game(null,0,200);s.StartCombat();var spec=s.Data.EnemiesById[11102];
        var k=s.DebugSpawn(EnemyKind.Boss,1,spec);
        s.ApplyDamage(k,452,"Test");Assert.That(k.Health,Is.EqualTo(1202),"Base defense 12");Assert.That(k.Shielded,Is.False);
        s.ApplyDamage(k,3,"Test");Assert.That(k.Shielded,Is.True,"Below 75%");
        int before=k.Health;s.ApplyDamage(k,100,"Test");Assert.That(before-k.Health,Is.EqualTo(38),"Defense 12 + 50 while shielded");
        s.Tick(5.05f,Away);Assert.That(k.Shielded,Is.False);
        var fresh=s.DebugSpawn(EnemyKind.Boss,1,spec);s.ApplyDamage(fresh,1400,"Test");Assert.That(fresh.Health,Is.LessThan(640));Assert.That(fresh.Shielded,Is.True);
        s.Tick(5.05f,Away);s.ApplyDamage(fresh,50,"Test");Assert.That(fresh.Shielded,Is.False,"Both thresholds were consumed by the single hit");
    }
    [Test] public void CrimsonLordSummonsTwoChargersEveryTenSecondsWithoutCapOrGold()
    {
        var s=Game(null,0,200);s.StartCombat();var lord=s.DebugSpawn(EnemyKind.Boss,1,s.Data.EnemiesById[11103]);
        s.Tick(10.05f,Away);
        var minions=s.Enemies.Where(e=>e.Alive&&e.Summoned).ToList();Assert.That(minions.Count,Is.EqualTo(2));Assert.That(minions.All(m=>m.Kind==EnemyKind.Charger),Is.True);
        int gold=s.Gold;s.ApplyDamage(minions[0],1000,"Test");Assert.That(s.Gold,Is.EqualTo(gold),"졸개 처치 골드 0");
        s.Tick(20f,Away);Assert.That(s.Enemies.Count(e=>e.Alive&&e.Summoned),Is.EqualTo(5));s.Tick(10f,Away);Assert.That(s.Enemies.Count(e=>e.Alive&&e.Summoned),Is.EqualTo(7),"No cap on living summons");s.Tick(10f,Away);Assert.That(s.Enemies.Count(e=>e.Alive&&e.Summoned),Is.EqualTo(9));
        s.ApplyDamage(lord,100000,"Test");int alive=s.Enemies.Count(e=>e.Alive&&e.Summoned);s.Tick(12f,Away);Assert.That(s.Enemies.Count(e=>e.Alive&&e.Summoned),Is.EqualTo(alive),"Summoning stops with the lord");
    }
    [Test] public void AreaBlastHitsRadiusOnlyAndHasCooldown()
    {
        var s=Game();s.StartCombat();var near=s.DebugSpawn(EnemyKind.Attack,1,Dummy(s.Data));var far=s.DebugSpawn(EnemyKind.Attack,6,Dummy(s.Data));
        Assert.That(s.AreaBlastReady,Is.True);Assert.That(s.UseAreaBlast(new Vector2(1,1)),Is.True);
        Assert.That(near.Health,Is.EqualTo(960),"Player 10 × 4");Assert.That(far.Health,Is.EqualTo(1000));
        Assert.That(s.DamageTotals.ContainsKey("PlayerSkill"),Is.False,"피드백 4: 범위 폭발은 기술병 누적 피해에 합산");Assert.That(s.DamageTotals["Player"],Is.EqualTo(40));
        Assert.That(s.UseAreaBlast(new Vector2(1,1)),Is.False);Assert.That(s.AreaBlastCooldown,Is.EqualTo(12).Within(.001));
        s.Tick(12.01f,Away);Assert.That(s.AreaBlastReady,Is.True);
    }
    [Test] public void TargetPriorityPutsHealersFirstAndBossesLastForTowers()
    {
        var s=Game();s.StartCombat();var order=new[]{EnemyKind.Charger,EnemyKind.Boss,EnemyKind.Attack,EnemyKind.Splitter,EnemyKind.Flyer,EnemyKind.Armored,EnemyKind.Buff,EnemyKind.Defense,EnemyKind.Healer};
        var spawned=order.Select((k,i)=>s.DebugSpawn(k,i+1,Dummy(s.Data,100,0,k))).ToList();
        Assert.That(CombatRules.TowerPriorityOrder(spawned).Select(e=>e.Kind),Is.EqualTo(new[]{EnemyKind.Healer,EnemyKind.Defense,EnemyKind.Buff,EnemyKind.Armored,EnemyKind.Flyer,EnemyKind.Splitter,EnemyKind.Attack,EnemyKind.Charger,EnemyKind.Boss}));
    }
    [Test] public void BuildCardsChangeWhenAndForHowMuchTowersCanBeBuilt()
    {
        var s=Game();string reason;s.StartCombat();
        Assert.That(s.TryInstall(TowerKind.Single,new Vector2Int(3,2),out reason),Is.Null);
        s.DebugGrantAugmentation(49);int crystals=s.Crystals;
        Assert.That(s.TryInstall(TowerKind.Single,new Vector2Int(3,2),out reason),Is.Not.Null,"49 전선 건설");Assert.That(crystals-s.Crystals,Is.EqualTo(75),"60 × 1.25");
        var t=Game();t.DebugGrantAugmentation(34);Assert.That(t.InstallCost(TowerKind.Single),Is.EqualTo(36),"−40% for two rounds");
    }
}
