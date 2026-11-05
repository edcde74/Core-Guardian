using System.Linq;
using CoreGuardian;
using NUnit.Framework;
using UnityEngine;

// 적 종류별 코어 도착 수 (2026-10-30 사용자 요청, 밸런싱 기록용).
public class CoreGuardianCoreReachTests
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

    [Test]
    public void CountsEachEnemyKindThatReachesTheCore()
    {
        var s = Game();
        Assert.That(s.StartCombat(), Is.True);
        Assert.That(s.CoreReachesThisRound.Count, Is.Zero, "A new round starts with no reaches");

        var a = s.DebugSpawn(EnemyKind.Attack, 9.95f);
        var b = s.DebugSpawn(EnemyKind.Attack, 9.95f);
        var c = s.DebugSpawn(EnemyKind.Defense, 9.95f);
        bool Left(EnemyHandle h) => h.IsValid && !h.Exited;
        for (int i = 0; i < 20 && (Left(a) || Left(b) || Left(c)); i++)
        {
            s.Tick(.05f, Away);
        }

        Assert.That(Left(a) || Left(b) || Left(c), Is.False, "All three reached the core");
        Assert.That(s.CoreReachesThisRound[11001], Is.EqualTo(2));
        Assert.That(s.CoreReachesThisRound[11002], Is.EqualTo(1));
        Assert.That(s.CoreReachesThisRound.Values.Sum(), Is.EqualTo(3), "Wave spawns are still far from the core");
        Assert.That(s.RoundCombatSeconds, Is.GreaterThan(0));
    }
}
