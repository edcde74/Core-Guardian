using System;
using System.Collections.Generic;
using CoreGuardian;
using MessagePipe;
using UnityEngine;

// Confirmed effect art for tags, abilities and skills (아트 제작 목록 2026-10-26/27). Pooled renderers,
// no per-frame allocation. Presentation only: reads Session state and cue events.
public sealed class CoreGuardianEffectsView : MonoBehaviour
{
    const float PixelScale = .6f;
    const int GroundOrder = 45,
        MarkerOrder = ArtDepthSorting.OverlayBase + 1750,
        PopOrder = ArtDepthSorting.OverlayBase + 1600,
        MaxPooled = 256;
    CoreGuardianPrototype game;

    sealed class Anim
    {
        public SpriteRenderer Renderer;
        public Sprite[] Frames;
        public float Age,
            Fps,
            Fade;
        public bool Loop;
    }

    readonly List<Anim> anims = new List<Anim>();
    readonly Stack<Anim> animPool = new Stack<Anim>();
    readonly Stack<SpriteRenderer> rendererPool = new Stack<SpriteRenderer>();
    readonly Dictionary<string, Sprite[]> sets = new Dictionary<string, Sprite[]>();

    // Persistent markers keyed by (owner id, slot).
    readonly Dictionary<long, SpriteRenderer> markers = new Dictionary<long, SpriteRenderer>();
    readonly HashSet<long> liveMarkers = new HashSet<long>();
    readonly List<long> markerWork = new List<long>();
    float clock;
    IDisposable subscriptions;

    // Session events arrive through the MessagePipe channel (CoreGuardianSessionEvents, 2026-10-28 pilot).
    public void Configure(CoreGuardianPrototype prototype)
    {
        game = prototype;
        var key = game.Session;
        var bag = DisposableBag.CreateBuilder();
        GlobalMessagePipe.GetSubscriber<Session, Session.CueEvent>().Subscribe(key, OnCue).AddTo(bag);
        GlobalMessagePipe.GetSubscriber<Session, TowerPulseMessage>().Subscribe(key, OnPulse).AddTo(bag);
        GlobalMessagePipe.GetSubscriber<Session, AreaBlastMessage>().Subscribe(key, OnBlast).AddTo(bag);
        GlobalMessagePipe.GetSubscriber<Session, DamageMessage>().Subscribe(key, OnDamage).AddTo(bag);
        subscriptions = bag.Build();
    }

    void OnDestroy()
    {
        subscriptions?.Dispose();
        subscriptions = null;
    }

    // Art lookup: Resources path frames (CoreGuardian/Art/...), else the sprite library (Fx/...).
    Sprite[] Set(string key)
    {
        if (sets.TryGetValue(key, out var frames))
            return frames;

        var list = new List<Sprite>();
        var single = Resources.Load<Sprite>("CoreGuardian/Art/" + key);
        if (single != null)
        {
            list.Add(single);
        }
        else
        {
            for (int i = 0; i < 32; i++)
            {
                var s = Resources.Load<Sprite>("CoreGuardian/Art/" + key + i);
                if (s == null)
                {
                    if (i == 0)
                        continue;
                    break;
                }
                list.Add(s);
            } // Burn_1~5 start at 1
        }

        if (list.Count == 0 && CoreGuardianSpriteLibrary.Instance != null)
        {
            var lib = CoreGuardianSpriteLibrary.Instance;
            var one = lib.Get("Fx/" + key);
            if (one != null)
            {
                list.Add(one);
            }
            else
            {
                list.AddRange(lib.Frames("Fx/" + key));
            }
        }
        frames = list.ToArray();
        sets[key] = frames;
        return frames;
    }

    // F26: status frames looked up once instead of a string-keyed search per enemy per frame (Set caches the same arrays).
    Sprite[] burnFrames,
        slowFrames,
        healFrames,
        stunFrames,
        freezeFrames;
    readonly Dictionary<string, string> fxNames = new Dictionary<string, string>();

    SpriteRenderer Rent(string name, int order)
    {
        SpriteRenderer r = null;
        while (rendererPool.Count > 0 && r == null)
        {
            r = rendererPool.Pop();
        }

        if (r == null)
        {
            r = new GameObject(name).AddComponent<SpriteRenderer>();
            r.transform.SetParent(transform, false);
        }
        r.name = name;
        r.sprite = null;
        r.sortingOrder = order;
        r.color = Color.white;
        r.flipX = false;
        r.transform.rotation = Quaternion.identity;
        r.transform.localScale = Vector3.one * PixelScale;
        r.gameObject.SetActive(true);
        return r;
    }

    void Return(SpriteRenderer r)
    {
        if (r == null)
            return;

        r.gameObject.SetActive(false);
        if (rendererPool.Count < MaxPooled)
        {
            rendererPool.Push(r);
        }
        else
        {
            Destroy(r.gameObject);
        }
    }

    Vector3 W(Vector2 tile) => game.World(tile);

    float TileWidth => Vector3.Distance(game.World(Vector2.zero), game.World(Vector2.right));

    void Play(string key, Vector3 world, float fps = 14, int order = PopOrder, float width = 0, float fade = 0)
    {
        var frames = Set(key);
        if (frames.Length == 0)
            return;

        var a = animPool.Count > 0 ? animPool.Pop() : new Anim();
        if (!fxNames.TryGetValue(key, out var fxName))
        {
            fxName = "Fx_" + key; // built once per effect kind (F7)
            fxNames.Add(key, fxName);
        }
        a.Renderer = Rent(fxName, order);
        a.Frames = frames;
        a.Age = 0;
        a.Fps = fps;
        a.Loop = false;
        a.Fade = fade;
        a.Renderer.sprite = frames[0];
        a.Renderer.transform.position = world;
        if (width > 0)
        {
            CoreGuardianCombatArt.SetWidth(a.Renderer, width);
        }

        anims.Add(a);
    }

    void OnCue(Session.CueEvent cue)
    {
        var at = W(cue.Position);
        switch (cue.Id)
        {
            case "execute":
                Play("Status/Execute_", at + Vector3.up * .5f, 10, PopOrder, TileWidth * 2.5f);
                break; // 피드백 4: 약 1.6배 크기·0.7초로 키움
            case "shatter":
                Play("Status/FreezeShatter_", at + Vector3.up * .3f, 14);
                break;
            case "burn_explode":
                Play("Augments/BurnExplosion", at + Vector3.up * .1f, 1, PopOrder, TileWidth * 3, .45f);
                break;
            case "chain":
                Play("Status/Chain_", at + Vector3.up * .3f, 18);
                break;
            case "heal_pulse":
                Play("Enemies/HealPulse", at, 1, GroundOrder, TileWidth * 4, .6f);
                break;
            case "shield_deploy":
                Play("IronKnightShield/ShieldStart_", at + Vector3.up * .2f, 12);
                break;
            case "summon":
                Play("CrimsonLordSummon/CrownWave_", at + Vector3.up * .6f, 12);
                Play("CrimsonLordSummon/SummonCircle_", at, 10, GroundOrder);
                break;
            case "hit_cold":
                Play("Frost/HitFrost_", at + Vector3.up * .3f, 20);
                break;
            case "hit_rail":
                Play("Rail/HitRail_", at + Vector3.up * .3f, 22);
                break;
            case "hit_flame":
                Play("Fire/HitFire_", at + Vector3.up * .3f, 20);
                break;
            case "star_merge":
                Play("Towers/MergeFlash_", at + Vector3.up * .3f, 14);
                break;
        }
    }

    // 장갑병 피격: 금속에 튕기는 강철 링 + 불똥(상태 효과 문서, 10-28 사용자 확정 B안). 5프레임 약 0.3초.
    void OnDamage(DamageMessage hit)
    {
        if (hit.Amount > 0 && hit.Enemy.Kind == EnemyKind.Armored)
        {
            Play("Enemies/ArmorDeflect_", W(hit.Enemy.Position) + Vector3.up * .42f, 16);
        }
    }

    void OnPulse(TowerPulseMessage pulse) =>
        Play("Towers/FrostPulse_", W(pulse.Position), 10, GroundOrder, TileWidth * pulse.Radius * 2, .25f);

    void OnBlast(AreaBlastMessage blast) =>
        Play("Skills/AreaBlast_", W(blast.Position), 10, GroundOrder, TileWidth * blast.Radius * 2, .2f);

    public void Teleport(Vector3 from, Vector3 to)
    {
        Play("Skills/Teleport_", from, 20);
        Play("Skills/Teleport_", to, 20);
    }

    SpriteRenderer Marker(long key, string name, int order)
    {
        liveMarkers.Add(key);
        if (markers.TryGetValue(key, out var r) && r != null)
            return r;

        r = Rent(name, order);
        markers[key] = r;
        return r;
    }

    static long Key(int owner, int slot) => ((long)owner << 4) | (uint)slot;

    void LateUpdate()
    {
        if (game == null || !game.Ready)
            return;

        float dt = Time.deltaTime;
        clock += dt;
        for (int i = anims.Count - 1; i >= 0; i--)
        {
            var a = anims[i];
            a.Age += dt;
            int frame = (int)(a.Age * a.Fps);
            bool done = a.Fade > 0 ? a.Age >= Mathf.Max(a.Fade, a.Frames.Length / a.Fps) : frame >= a.Frames.Length;
            if (done || a.Renderer == null)
            {
                Return(a.Renderer);
                a.Renderer = null;
                anims.RemoveAt(i);
                animPool.Push(a);
                continue;
            }
            a.Renderer.sprite = a.Frames[Mathf.Min(frame, a.Frames.Length - 1)];
            if (a.Fade > 0)
            {
                var c = a.Renderer.color;
                c.a = 1 - Mathf.Clamp01(a.Age / Mathf.Max(a.Fade, a.Frames.Length / a.Fps));
                a.Renderer.color = c;
            }
        }
        liveMarkers.Clear();
        var s = game.Session;
        foreach (var e in s.Enemies)
        {
            if (!e.Alive)
                continue;

            var head = game.HealthView(e.Id);
            if (head == null)
                continue;

            var top = head.position + Vector3.up * .16f;
            var foot = W(e.Position);
            int slot = 0;
            if (e.BurnStacks > 0)
            {
                var r = Marker(Key(e.Id, 0), "BurnMark", MarkerOrder);
                var f = burnFrames ??= Set("Status/Burn_");
                if (f.Length > 0)
                {
                    r.sprite = f[Mathf.Clamp(e.BurnStacks, 1, f.Length) - 1];
                }
                r.transform.position = top + Vector3.right * (.14f * slot++);
            }
            if (e.SlowPercent > 0 && !e.Frozen)
            {
                var r = Marker(Key(e.Id, 1), "SlowMark", MarkerOrder);
                var f = slowFrames ??= Set("Status/Slow");
                if (f.Length > 0)
                {
                    r.sprite = f[0];
                }
                r.transform.position = top + Vector3.right * (.14f * slot++);
            }
            if (e.HealMarked && slot < 3)
            {
                var r = Marker(Key(e.Id, 7), "HealMark", MarkerOrder);
                var f = healFrames ??= Set("Status/HealMark");
                if (f.Length > 0)
                {
                    r.sprite = f[0];
                }
                r.transform.position = top + Vector3.right * (.14f * slot++);
            }
            if (e.Stunned && !e.Frozen)
            {
                var r = Marker(Key(e.Id, 2), "StunMark", MarkerOrder);
                var f = stunFrames ??= Set("Status/Stun_");
                if (f.Length > 0)
                {
                    r.sprite = f[(int)(clock * 16) % f.Length];
                }
                r.transform.position = top + Vector3.up * .1f;
            }
            if (e.Frozen)
            {
                var r = Marker(Key(e.Id, 3), "FreezeIce", GroundOrder + 1);
                var f = freezeFrames ??= Set("Status/FreezeFootIce");
                if (f.Length > 0)
                {
                    r.sprite = f[0];
                }
                r.transform.position = foot;
            }
            if (e.Shielded)
            {
                var r = Marker(Key(e.Id, 4), "ShieldLoop", MarkerOrder - 10);
                var f = Set("IronKnightShield/ShieldLoop_");
                if (f.Length > 0)
                {
                    r.sprite = f[(int)(clock * 10) % f.Length];
                }
                r.transform.position = foot + Vector3.up * .35f;
            }
            if (e.Kind == EnemyKind.Boss && e.Enraged)
            {
                var r = Marker(Key(e.Id, 5), "RageLines", MarkerOrder - 20);
                var f = Set("Enemies/BossRageSpeedLines");
                if (f.Length > 0)
                {
                    r.sprite = f[0];
                }
                r.transform.position = foot + Vector3.up * .5f + Vector3.right * .35f;
            }
        }
        foreach (var t in s.Towers)
        {
            if (t.Stars < 2)
                continue;

            var view = game.TowerView(t.Id);
            if (view == null)
                continue;

            var r = Marker(Key(-t.Id - 1, 6), "StarRank", MarkerOrder - 30);
            var f = Set(t.Stars >= 3 ? "Towers/StarRank3" : "Towers/StarRank2");
            if (f.Length > 0)
            {
                r.sprite = f[0];
            }

            r.transform.position = view.position + Vector3.down * .18f;
        }
        int cell = 0;
        var fires = s.FireCellEnumerator();
        while (fires.MoveNext())
        {
            var fire = fires.Current;
            var r = Marker(Key(-100000 - cell, 7), "FireTile", GroundOrder);
            var f = Set("Augments/FireTile_");
            if (f.Length > 0)
            {
                r.sprite = f[(int)(clock * 10 + cell) % f.Length];
            }

            r.transform.position = W(fire.Key);
            cell++;
        }
        if (s.Augments.Has(15) && game.RouteCells.Count > 1)
        {
            var r = Marker(Key(-200000, 8), "StickyFloor", GroundOrder - 1);
            var f = Set("Augments/StickyFloor");
            if (f.Length > 0)
            {
                r.sprite = f[0];
            }

            float cells = s.Augments.Value(15) / Mathf.Max(1, s.Augments.Count(15));
            var start = (Vector2)game.RouteCells[0];
            var dir = ((Vector2)game.RouteCells[1] - start).normalized;
            r.transform.position = W(start + dir * ((cells - 1) * .5f));
        }
        markerWork.Clear();
        foreach (var key in markers.Keys)
        {
            if (!liveMarkers.Contains(key))
            {
                markerWork.Add(key);
            }
        }

        foreach (var key in markerWork)
        {
            Return(markers[key]);
            markers.Remove(key);
        }
    }

    public void Clear()
    {
        foreach (var a in anims)
        {
            Return(a.Renderer);
        }

        anims.Clear();
        foreach (var r in markers.Values)
        {
            Return(r);
        }

        markers.Clear();
    }
}
