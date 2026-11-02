using System.IO;
using System.Linq;
using CoreGuardian;
using NUnit.Framework;
using UnityEngine;

// 플레이 기록 (2026-10-30): four csv files with towers, damage by source × enemy, augments and upgrades.
public class CoreGuardianPlayLogTests
{
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

    [Test]
    public void WritesTowerDamageAugmentAndUpgradeFiles()
    {
        string dir = Path.Combine(Path.GetTempPath(), "cg_playlog_" + System.Guid.NewGuid().ToString("N"));
        var s = Game();
        var log = new CoreGuardianPlayLog(s, dir);
        try
        {
            s.DebugAddCrystals(100000);
            string r;
            var tower = s.TryInstall(TowerKind.Single, new Vector2Int(2, 3), out r);
            Assert.That(tower, Is.Not.Null, r);
            Assert.That(s.UpgradeTowerKind(TowerKind.Single, "damage", out r), Is.True, r);
            Assert.That(s.DebugGrantAugmentation(1), Is.True);
            s.StartCombat();

            var normal = s.DebugSpawn(EnemyKind.Attack, 1);
            int before = normal.Health;
            s.ApplyDamage(normal, 1e6f, tower.AttackSource);
            var boss = s.DebugSpawn(EnemyKind.Boss, 1, s.Data.EnemiesById.Values.First(e => e.Kind == EnemyKind.Boss));
            s.ApplyDamage(boss, 50, "PlayerSkill");

            log.Record(1, "완료");

            string towers = File.ReadAllText(Path.Combine(log.Folder, "포탑.csv"));
            string damage = File.ReadAllText(Path.Combine(log.Folder, "피해.csv"));
            string augments = File.ReadAllText(Path.Combine(log.Folder, "증강.csv"));
            string upgrades = File.ReadAllText(Path.Combine(log.Folder, "강화.csv"));

            var towerRow = towers.Split('\n')[1].Split(',');
            Assert.That(towerRow[4], Is.EqualTo("단일 고속"));
            Assert.That(towerRow[5] + "," + towerRow[6], Is.EqualTo("2,3"));
            Assert.That(towerRow[10], Does.Contain("공격력 1"));
            Assert.That(towerRow[12], Is.EqualTo(before.ToString()), "overkill is not counted");

            Assert.That(damage, Does.Contain("기술병 범위 폭발·순간 이동 폭발"));
            var skillRow = damage.Split('\n').First(l => l.Contains("기술병 범위 폭발")).Split(',');
            Assert.That(long.Parse(skillRow[9]), Is.GreaterThan(0), "boss damage column");
            Assert.That(augments.Split('\n').Length, Is.GreaterThanOrEqualTo(3), "header + one augment");
            Assert.That(upgrades, Does.Contain("기술병"));

            log.Record(2, "완료");
            var second = File.ReadAllText(Path.Combine(log.Folder, "포탑.csv")).Split('\n')[2].Split(',');
            Assert.That(second[11], Is.EqualTo("0"), "this-round damage resets");
            Assert.That(second[12], Is.EqualTo(before.ToString()), "total keeps adding up");
        }
        finally
        {
            log.Dispose();
            s.Dispose();
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
