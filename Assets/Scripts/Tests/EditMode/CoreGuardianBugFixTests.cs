using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoreGuardian;
using NUnit.Framework;
using UnityEngine;

// Regression tests for the refactor bug list (docs/development/refactor_review_20261028, B1·B2·B4·B6·B16·B18).
public class CoreGuardianBugFixTests
{
    static readonly Vector2 Away = new Vector2(100, 100);

    static string Csv(string key)
    {
        var asset = Resources.Load<TextAsset>("CoreGuardian/" + key);
        Assert.That(asset, Is.Not.Null, "Missing CSV " + key);
        return asset.text;
    }

    static Session Game()
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
        var s = new Session(d, 42);
        s.SetBoard(40, 25, new Vector2Int(10, 0), new[] { Vector2Int.zero, new Vector2Int(10, 0) });
        return s;
    }

    // B1: a lower core maximum (48·50) is not damage; only enemies reaching the core count.
    [Test]
    public void CoreDamageTakenIgnoresMaximumLossAndCountsHits()
    {
        var s = Game();
        int before = s.CoreEnergy;
        Assert.That(s.DebugGrantAugmentation(48), Is.True);
        Assert.That(s.DebugGrantAugmentation(50), Is.True);
        Assert.That(s.CoreEnergy, Is.LessThan(before), "48·50 lower the core here");
        Assert.That(s.CoreDamageTaken, Is.Zero);

        s.StartCombat();
        var e = s.DebugSpawn(EnemyKind.Attack, 9.9f);
        int core = s.CoreEnergy;
        for (int i = 0; i < 100 && e.IsValid && !e.Exited; i++)
        {
            s.Tick(.1f, Away);
        }

        Assert.That(s.CoreEnergy, Is.LessThan(core));
        Assert.That(s.CoreDamageTaken, Is.EqualTo(core - s.CoreEnergy));
    }

    // B2: the gold number on a kill matches what was paid; summons pay nothing.
    [Test]
    public void KillGoldIsZeroForSummonsAndMatchesPaidGold()
    {
        var s = Game();
        s.StartCombat();
        s.DebugSpawn(EnemyKind.Boss, 1, s.Data.EnemiesById[11103]);
        s.Tick(10.05f, Away);
        var minion = s.Enemies.First(e => e.Alive && e.Summoned);
        Assert.That(s.KillGold(new EnemySnapshot(minion)), Is.Zero);

        var normal = s.DebugSpawn(EnemyKind.Attack, 1);
        int expected = s.KillGold(new EnemySnapshot(normal));
        int gold = s.Gold;
        s.ApplyDamage(normal, 1e6f, "Test");
        Assert.That(s.Gold - gold, Is.EqualTo(expected));
        Assert.That(expected, Is.GreaterThan(0));
    }

    // B4: the lab stops selling slow/pierce once the cap is reached, and shows the capped value.
    [TestCase("slowPercent")]
    [TestCase("pierceFalloff")]
    public void LabUpgradeStopsAtTheCap(string stat)
    {
        var s = Game();
        var u = s.Data.Upgrades.First(x => x.Stat == stat);
        var kind = (TowerKind)Enum.Parse(typeof(TowerKind), u.Target);
        string r;
        if (kind == TowerKind.Support)
        {
            Assert.That(s.TryInstall(TowerKind.Single, new Vector2Int(2, 3), out r), Is.Not.Null, r);
        }
        s.DebugAddCrystals(1000000);
        Assert.That(s.TryInstall(kind, new Vector2Int(4, 3), out r), Is.Not.Null, r);
        int bought = 0;
        while (s.UpgradeTowerKind(kind, stat, out r) && bought < 500)
        {
            bought++;
        }

        Assert.That(bought, Is.GreaterThan(0));
        Assert.That(r, Is.EqualTo("최대치"));
        Assert.That(s.TowerKindStatMaxed(kind, stat), Is.True);
        int crystals = s.Crystals;
        Assert.That(s.UpgradeTowerKind(kind, stat, out r), Is.False);
        Assert.That(s.Crystals, Is.EqualTo(crystals), "No crystals spent at the cap");
        var st = s.TowerKindStats(kind);
        if (stat == "pierceFalloff")
        {
            Assert.That(st.PierceFalloff, Is.LessThanOrEqualTo(1));
        }
        else
        {
            Assert.That(st.SlowPercent, Is.LessThanOrEqualTo(s.Data.Tag("Slow", "maxPercent", false, 60)));
        }
    }

    // B6: the execute follow-up hit keeps the weakness and critical marks of the hit that triggered it.
    [Test]
    public void ExecuteHitKeepsAffinityAndCriticalMarks()
    {
        var s = Game();
        var spec = s.Data.EnemiesById.Values.First(e => e.Kind != EnemyKind.Boss && e.WeakTowers != 0);
        int tower = 0;
        while ((spec.WeakTowers & (1 << tower)) == 0)
        {
            tower++;
        }

        s.StartCombat();
        var target = s.DebugSpawn(spec.Kind, 1, spec);
        var marks = new List<(int affinity, bool critical)>();
        s.DamageApplied += (e, amount) => marks.Add((s.HitAffinity, s.HitCritical));
        var strike = typeof(Session).GetMethod("Strike", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(strike, Is.Not.Null);
        var payload = new HitPayload
        {
            Crit = 2,
            Execute = 100,
            Tower = tower + 1,
        };
        strike.Invoke(s, new object[] { target, 1f, "Test", payload });

        Assert.That(marks.Count, Is.GreaterThanOrEqualTo(2), "first hit plus the execute");
        Assert.That(marks.All(m => m.affinity == 1 && m.critical), Is.True, string.Join(", ", marks));
        Assert.That(s.HitAffinity, Is.Zero);
        Assert.That(s.HitCritical, Is.False);
    }

    // B16: choosing, previewing and installing share one rule and one set of messages.
    [Test]
    public void ChooseAndInstallShareOneRule()
    {
        var s = Game();
        string r;
        Assert.That(s.CanChooseTower(TowerKind.Support, out r), Is.False);
        Assert.That(r, Is.EqualTo("공격 포탑을 먼저 설치하세요."));
        Assert.That(s.CanInstall(TowerKind.Single, new Vector2Int(5, 0), out r), Is.False, "route cell");
        Assert.That(r, Is.EqualTo("이 칸에는 설치할 수 없습니다."));
    }

    // B18: the developer menu and the HUD read one enemy name table.
    [Test]
    public void DeveloperEnemyNamesUseTheSharedTable()
    {
        var s = Game();
        foreach (var id in s.DebugEnemyIds)
        {
            var spec = s.Data.EnemiesById[id];
            Assert.That(s.DebugEnemyName(id), Is.EqualTo(EnemyNames.Of(spec)), id.ToString());
            Assert.That(s.DebugEnemyName(id), Does.Not.Match("[A-Za-z]"), id + " shows a Korean name");
        }
        // 11-04 확정: 10라운드 보스 이름은 파성장군.
        Assert.That(EnemyNames.Of(s.Data.EnemiesById[11101]), Is.EqualTo("파성장군"));
    }
}
