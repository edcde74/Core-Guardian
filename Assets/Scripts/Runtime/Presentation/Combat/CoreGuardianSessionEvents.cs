using System;
using CoreGuardian;
using MessagePipe;
using UnityEngine;

// Session → presentation event channel (MessagePipe, 2026-10-28 pilot). The game rules keep their plain C# events;
// this bridge republishes some of them, keyed by the Session so two games (e.g. in tests) never mix. Views
// subscribe once and dispose one handle instead of pairing += / -= by hand. Pilot user: CoreGuardianEffectsView.
public readonly struct DamageMessage
{
    public readonly EnemySnapshot Enemy;
    public readonly int Amount;

    public DamageMessage(EnemySnapshot enemy, int amount)
    {
        Enemy = enemy;
        Amount = amount;
    }
}

public readonly struct TowerPulseMessage
{
    public readonly int Id;
    public readonly Vector2 Position;
    public readonly float Radius;

    public TowerPulseMessage(int id, Vector2 position, float radius)
    {
        Id = id;
        Position = position;
        Radius = radius;
    }
}

public readonly struct AreaBlastMessage
{
    public readonly Vector2 Position;
    public readonly float Radius;

    public AreaBlastMessage(Vector2 position, float radius)
    {
        Position = position;
        Radius = radius;
    }
}

public sealed class CoreGuardianSessionEvents : IDisposable
{
    static bool providerReady;

    // Domain Reload is off: build the channel again on every Play.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetProvider() => providerReady = false;

    public static void EnsureProvider()
    {
        if (providerReady)
            return;

        var builder = new BuiltinContainerBuilder();
        builder.AddMessagePipe();
        builder.AddMessageBroker<Session, Session.CueEvent>();
        builder.AddMessageBroker<Session, DamageMessage>();
        builder.AddMessageBroker<Session, TowerPulseMessage>();
        builder.AddMessageBroker<Session, AreaBlastMessage>();
        GlobalMessagePipe.SetProvider(builder.BuildServiceProvider());
        providerReady = true;
    }

    readonly Session session;
    readonly IPublisher<Session, Session.CueEvent> cue;
    readonly IPublisher<Session, DamageMessage> damage;
    readonly IPublisher<Session, TowerPulseMessage> pulse;
    readonly IPublisher<Session, AreaBlastMessage> blast;

    public CoreGuardianSessionEvents(Session session)
    {
        EnsureProvider();
        this.session = session;
        cue = GlobalMessagePipe.GetPublisher<Session, Session.CueEvent>();
        damage = GlobalMessagePipe.GetPublisher<Session, DamageMessage>();
        pulse = GlobalMessagePipe.GetPublisher<Session, TowerPulseMessage>();
        blast = GlobalMessagePipe.GetPublisher<Session, AreaBlastMessage>();
        session.Cue += OnCue;
        session.TowerPulsed += OnPulse;
        session.AreaBlastUsed += OnBlast;
        session.DamageApplied += OnDamage;
    }

    void OnCue(Session.CueEvent e) => cue.Publish(session, e);

    void OnDamage(EnemySnapshot enemy, int amount) => damage.Publish(session, new DamageMessage(enemy, amount));

    void OnPulse(int id, Vector2 position, float radius) =>
        pulse.Publish(session, new TowerPulseMessage(id, position, radius));

    void OnBlast(Vector2 position, float radius) => blast.Publish(session, new AreaBlastMessage(position, radius));

    public void Dispose()
    {
        session.Cue -= OnCue;
        session.TowerPulsed -= OnPulse;
        session.AreaBlastUsed -= OnBlast;
        session.DamageApplied -= OnDamage;
    }
}
