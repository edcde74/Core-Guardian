using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using NUnit.Framework;
using UnityEngine;

public class CoreGuardianAnimationTimingTests
{
    static Session Game()
    {
        string Csv(string name) => Resources.Load<TextAsset>("CoreGuardian/" + name).text;
        var data = CombatData.Load(Csv("enemies"), Csv("towers"), Csv("stage"), Csv("upgrades"), Csv("waves"));
        data.Waves.Clear();
        var session = new Session(data);
        session.SetBoard(40, 25, new Vector2Int(30, 0), new[] { Vector2Int.zero, new Vector2Int(30, 0) });
        session.StartCombat();
        return session;
    }

    [TestCase(1f)]
    [TestCase(8f)]
    public void LaunchFeedbackOccursAtCreation_EvenWhenProjectileHitsWithinSameTick(float rate)
    {
        var session = Game();
        session.Player.Rate = rate;
        var enemy = session.DebugSpawn(EnemyKind.Defense);
        var launched = new List<int>();
        var feedback = typeof(Session).GetEvent("ProjectileLaunched");
        Assert.That(feedback, Is.Not.Null, "Animation must receive the actual shot, including same-tick hits");
        feedback.AddEventHandler(
            session,
            new Action<ProjectileSnapshot>(shot =>
            {
                Assert.That(session.Projectiles.Contains(shot.Handle), Is.True);
                Assert.That(shot.Target.Handle, Is.EqualTo(enemy));
                Assert.That(shot.Position, Is.EqualTo(new Vector2(0, .05f)));
                launched.Add(shot.Id);
            })
        );
        session.Tick(.05f, new Vector2(0, .05f));
        Assert.That(launched.Count, Is.EqualTo(1));
        Assert.That(session.Projectiles, Is.Empty, "This shot hits before view synchronization");
        Assert.That(enemy.Health, Is.EqualTo(40));
    }

    [Test]
    public void PlayerVolleyPublishesOneProjectile_WithoutChangingDamageOrCadence()
    {
        var session = Game();
        session.DebugSpawn(EnemyKind.Defense, 1);
        var shots = new List<ProjectileSnapshot>();
        var feedback = typeof(Session).GetEvent("ProjectileLaunched");
        Assert.That(feedback, Is.Not.Null);
        feedback.AddEventHandler(session, new Action<ProjectileSnapshot>(shots.Add));
        session.Tick(.01f, new Vector2(0, 1));
        Assert.That(shots.Count, Is.EqualTo(1));
        Assert.That(shots.All(p => p.Source == "Player" && p.Damage == 10), Is.True);
        session.Tick(.5f, new Vector2(0, 1));
        Assert.That(shots.Count, Is.EqualTo(1), "Presentation must not introduce another attack");
    }
}
