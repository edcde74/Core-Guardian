using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using UnityEngine;

// Approved static B sprites. This renderer never changes combat rules or original art prefabs.
public sealed class CoreGuardianCombatArt : MonoBehaviour
{
    public const string ResourcePath = "CoreGuardian/Art/CombatB";
    public static readonly string[] Names =
    {
        "EngineerChip",
        "SingleBolt",
        "MultiBolt",
        "EnemyHit",
        "EnemyDeath",
        "EnemySpeedBuff",
        "TowerDamageBuff",
        "CoreHit",
        "GoldGain",
    };
    readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    readonly List<Effect> effects = new List<Effect>();
    const int MaxRetainedEffects = 256;
    readonly Stack<Effect> effectPool = new Stack<Effect>();
    bool disposingEffects;
    CoreGuardianPrototype game;
    CoreGuardianHud hud;
    CoreGuardianCoreFeedback coreFeedback;
    CoreGuardianCoreAmbient coreAmbient;
    SpriteRenderer coreBody;

    sealed class Effect
    {
        public SpriteRenderer Renderer;
        public float Started,
            Duration,
            Width,
            Rise;
        public Vector3 Origin;
    }

    public bool Loaded => sprites.Count == Names.Length;

    public void Configure(CoreGuardianPrototype prototype, CoreGuardianHud combatHud)
    {
        game = prototype;
        hud = combatHud;
        foreach (var sprite in Resources.LoadAll<Sprite>(ResourcePath))
        {
            sprites[sprite.name] = sprite;
        }

        if (!Loaded || Names.Any(n => !sprites.ContainsKey(n)))
        {
            throw new InvalidOperationException("Approved combat sprites not installed");
        }

        game.Session.DamageApplied += OnDamage;
        // Resolve the visible core in this scene, never its contact shadow or tilemap.
        coreBody = UnityEngine
            .Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(r =>
                r.gameObject.scene == game.gameObject.scene
                && r.name == "2D Sprite"
                && r.sprite != null
                && r.sprite.name == "core"
            );
        coreFeedback = new CoreGuardianCoreFeedback(coreBody, hud.CoreDamageGraphics());
        if (coreBody != null)
        {
            coreAmbient = new CoreGuardianCoreAmbient(coreBody, game.GetComponent<CoreGuardianCoreParticleSettings>());
        }
    }

    Sprite fireball,
        railBolt;

    public Sprite Get(string name) => sprites[name];

    public Sprite Projectile(ProjectileHandle projectile)
    {
        if (projectile.Source == "Player")
            return Get("EngineerChip");
        // Session launches with TowerState.AttackSource ("Tower:"+Id). Match it without building a string per tower.
        TowerState tower = null;
        var source = projectile.Source;
        foreach (var t in game.Session.Towers)
        {
            if (t.AttackSource == source)
            {
                tower = t;
                break;
            }
        }

        if (tower != null && tower.Kind == TowerKind.Flame)
        {
            if (fireball == null)
                fireball = Resources.Load<Sprite>("CoreGuardian/Art/Towers/Fireball");
            if (fireball != null)
                return fireball;
        }
        if (tower != null && tower.Kind == TowerKind.Rail)
        {
            if (railBolt == null)
                railBolt = CoreGuardianSpriteLibrary.Instance.OrNull()?.Get("Fx/RailBolt");
            if (railBolt != null)
                return railBolt;
        }
        return Get(tower != null && tower.Kind == TowerKind.Multi ? "MultiBolt" : "SingleBolt");
    }

    public GameObject Marker(string name, Vector3 position, float width)
    {
        var r = NewRenderer(name, Get(name), position, ArtDepthSorting.OverlayBase + 1700);
        SetWidth(r, width);
        return r.gameObject;
    }

    SpriteRenderer NewRenderer(string name, Sprite sprite, Vector3 position, int order)
    {
        var g = new GameObject(name);
        g.transform.SetParent(transform, false);
        g.transform.position = position;
        var r = g.AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.sortingOrder = order;
        return r;
    }

    public static void SetWidth(SpriteRenderer renderer, float width)
    {
        renderer.transform.localScale = Vector3.one * (width / renderer.sprite.bounds.size.x);
    }

    void OnDamage(EnemySnapshot enemy, int amount)
    {
        var position = game.World(enemy.Position);
        // 장갑병은 공통 피격 대신 금속 튕김(B안 강철 링, 10-28 확정)을 EffectsView가 그림.
        if (enemy.Kind != EnemyKind.Armored)
        {
            Show("EnemyHit", position + Vector3.up * .42f, .32f, .2f);
        }

        if (enemy.Health > 0)
            return;

        Show("EnemyDeath", position + Vector3.up * .3f, .48f, .55f);
        int gold = game.Session.KillGold(enemy);
        if (gold > 0)
        {
            hud.ShowGoldNumber(position + Vector3.up * 1.45f, gold, CoreGuardianHudSkin.Sprite("Gold"));
        }
    }

    public void CoreDamaged(int amount)
    {
        var position = game.World(game.CoreCell);
        Show("CoreHit", position + Vector3.up * .5f, .7f, .7f);
        // User positioned the preview in Inspector; preserve its offset from the artwork.
        var numberPosition =
            coreBody != null
                ? new Vector3(coreBody.bounds.max.x - .3f, coreBody.bounds.max.y - .38f, coreBody.transform.position.z)
                : position + new Vector3(.7f, 1.1f, 0);
        hud.ShowCoreNumber(numberPosition, amount);
        coreFeedback?.Trigger();
        coreAmbient?.NotifyCoreDamage();
    }

    void Show(string name, Vector3 position, float width, float duration, float rise = 0)
    {
        Effect effect = null;
        while (effectPool.Count > 0 && effect == null)
        {
            var candidate = effectPool.Pop();
            if (candidate.Renderer != null)
            {
                effect = candidate;
            }
        }
        if (effect == null)
        {
            effect = new Effect
            {
                Renderer = NewRenderer(name, Get(name), position, ArtDepthSorting.OverlayBase + 1750),
            };
        }

        var renderer = effect.Renderer;
        renderer.name = name;
        renderer.sprite = Get(name);
        renderer.color = Color.white;
        renderer.flipX = renderer.flipY = false;
        renderer.sortingOrder = ArtDepthSorting.OverlayBase + 1750;
        renderer.transform.position = position;
        renderer.transform.localRotation = Quaternion.identity;
        SetWidth(renderer, width);
        effect.Started = Time.time;
        effect.Duration = duration;
        effect.Width = width;
        effect.Rise = rise;
        effect.Origin = position;
        renderer.transform.SetAsLastSibling();
        renderer.gameObject.SetActive(true);
        effects.Add(effect);
    }

    void ReturnEffect(Effect effect)
    {
        if (effect.Renderer == null)
            return;

        effect.Renderer.gameObject.SetActive(false);
        if (!disposingEffects && effectPool.Count < MaxRetainedEffects)
        {
            effectPool.Push(effect);
        }
        else
        {
            Destroy(effect.Renderer.gameObject);
        }
    }

    void Update()
    {
        coreFeedback?.Advance(Time.deltaTime);
        coreAmbient?.Advance(Time.deltaTime);
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            var effect = effects[i];
            float age = (Time.time - effect.Started) / effect.Duration;
            if (effect.Renderer == null || age >= 1)
            {
                ReturnEffect(effect);
                effects.RemoveAt(i);
                continue;
            }
            effect.Renderer.transform.position = effect.Origin + Vector3.up * (age * effect.Rise);
            effect.Renderer.color = new Color(1, 1, 1, 1 - Mathf.Clamp01((age - .3f) / .7f));
        }
    }

    public void ClearEffects()
    {
        foreach (var e in effects)
        {
            ReturnEffect(e);
        }
        effects.Clear();
        coreFeedback?.Clear();
    }

    void OnEnable()
    {
        coreAmbient?.SetEnabled(true);
    }

    void OnDisable()
    {
        coreFeedback?.Clear();
        coreAmbient?.SetEnabled(false);
    }

    void OnDestroy()
    {
        if (game != null && game.Session != null)
        {
            game.Session.DamageApplied -= OnDamage;
        }

        disposingEffects = true;
        ClearEffects();
        while (effectPool.Count > 0)
        {
            var effect = effectPool.Pop();
            if (effect.Renderer != null)
            {
                Destroy(effect.Renderer.gameObject);
            }
        }
        coreFeedback?.Dispose();
        coreAmbient?.Dispose();
    }
}
