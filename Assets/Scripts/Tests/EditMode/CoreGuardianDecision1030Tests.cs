using System.Linq;
using System.Reflection;
using CoreGuardian;
using NUnit.Framework;
using UnityEngine;

// 10-30 기획서 대조 결정: #4 관통 치명은 기술병 관통 탄에도, #6 확정 타격은 폭발 1번 = 공격 1회.
public class CoreGuardianDecision1030Tests
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
    public void PlayerPierceShot_CritKeepsFullDamage_With29()
    {
        var s = Game();
        try
        {
            Assert.That(s.DebugGrantAugmentation(5), Is.True);
            Assert.That(s.DebugGrantAugmentation(29), Is.True);
            var launch = typeof(Session).GetMethod("LaunchPayload", BindingFlags.Instance | BindingFlags.NonPublic);
            var crit = launch.Invoke(s, new object[] { null, null, 0f, 2f });
            var normal = launch.Invoke(s, new object[] { null, null, 0f, 1f });
            var flag = crit.GetType().GetField("CritNoFalloff");
            Assert.That((bool)flag.GetValue(crit), Is.True, "crit pierce shot keeps damage");
            Assert.That((bool)flag.GetValue(normal), Is.False, "non-crit shot still falls off");
        }
        finally
        {
            s.Dispose();
        }
    }

    [Test]
    public void AreaBlast_CountsAsOneAttack_ForGuaranteedCrit()
    {
        var s = Game();
        try
        {
            s.StartCombat();
            var a = s.DebugSpawn(EnemyKind.Attack, 1);
            s.DebugSpawn(EnemyKind.Attack, 1);
            s.DebugSpawn(EnemyKind.Attack, 1);
            var attacks = typeof(Session).GetField("playerAttacks", BindingFlags.Instance | BindingFlags.NonPublic);
            int before = (int)attacks.GetValue(s);
            Assert.That(s.UseAreaBlast(a.Position), Is.True);
            Assert.That((int)attacks.GetValue(s) - before, Is.EqualTo(1), "one blast = one attack");
        }
        finally
        {
            s.Dispose();
        }
    }
}
