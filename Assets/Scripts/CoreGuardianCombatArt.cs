using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using UnityEngine;

// Approved static B sprites. This renderer never changes combat rules or original art prefabs.
public sealed class CoreGuardianCombatArt : MonoBehaviour
{
    public const string ResourcePath="CoreGuardian/Art/CombatB";
    public static readonly string[] Names={"EngineerChip","SingleBolt","MultiBolt","EnemyHit","EnemyDeath","EnemySpeedBuff","TowerDamageBuff","CoreHit","GoldGain"};
    readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
    readonly List<Effect> effects=new List<Effect>();
    CoreGuardianPrototype game;
    CoreGuardianHud hud;
    CoreGuardianCoreFeedback coreFeedback;
    CoreGuardianCoreAmbient coreAmbient;
    SpriteRenderer coreBody;
    sealed class Effect { public SpriteRenderer Renderer; public float Started,Duration,Width,Rise; public Vector3 Origin; }
    public bool Loaded => sprites.Count==Names.Length;

    public void Configure(CoreGuardianPrototype prototype,CoreGuardianHud combatHud)
    {
        game=prototype;hud=combatHud;
        foreach(var sprite in Resources.LoadAll<Sprite>(ResourcePath))sprites[sprite.name]=sprite;
        if(!Loaded||Names.Any(n=>!sprites.ContainsKey(n)))throw new InvalidOperationException("Approved combat sprites not installed");
        game.Session.DamageApplied+=OnDamage;
        // Resolve the visible core in this scene, never its contact shadow or tilemap.
        coreBody=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .FirstOrDefault(r=>r.gameObject.scene==game.gameObject.scene&&r.name=="2D Sprite"&&r.sprite!=null&&r.sprite.name=="core");
        coreFeedback=new CoreGuardianCoreFeedback(coreBody,hud.CoreDamageGraphics());
        if(coreBody!=null)coreAmbient=new CoreGuardianCoreAmbient(coreBody);
    }
    public Sprite Get(string name)=>sprites[name];
    public Sprite Projectile(ProjectileState projectile)
    {
        if(projectile.Source=="Player")return Get("EngineerChip");
        var tower=game.Session.Towers.FirstOrDefault(t=>projectile.Source=="Tower:"+t.Id);
        return Get(tower!=null&&tower.Kind==TowerKind.Multi?"MultiBolt":"SingleBolt");
    }
    public GameObject Marker(string name,Vector3 position,float width)
    {
        var r=NewRenderer(name,Get(name),position,1700);SetWidth(r,width);return r.gameObject;
    }
    SpriteRenderer NewRenderer(string name,Sprite sprite,Vector3 position,int order)
    {
        var g=new GameObject(name);g.transform.SetParent(transform,false);g.transform.position=position;
        var r=g.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=order;return r;
    }
    public static void SetWidth(SpriteRenderer renderer,float width)
    {
        renderer.transform.localScale=Vector3.one*(width/renderer.sprite.bounds.size.x);
    }
    void OnDamage(EnemyState enemy,int amount)
    {
        var position=game.World(enemy.Position);
        Show("EnemyHit",position+Vector3.up*.42f,.32f,.2f);
        if(enemy.Health>0)return;
        Show("EnemyDeath",position+Vector3.up*.3f,.48f,.55f);
        hud.ShowGoldNumber(position+Vector3.up*1.45f,enemy.Spec.Gold,Get("GoldGain"));
    }
    public void CoreDamaged(int amount)
    {
        var position=game.World(new Vector2(29,9));
        Show("CoreHit",position+Vector3.up*.5f,.7f,.35f);
        // User positioned the preview in Inspector; preserve its offset from the artwork.
        var numberPosition=coreBody!=null?new Vector3(coreBody.bounds.max.x-.3f,coreBody.bounds.max.y-.38f,coreBody.transform.position.z):position+new Vector3(.7f,1.1f,0);
        hud.ShowCoreNumber(numberPosition,amount);
        coreFeedback?.Trigger();
        coreAmbient?.NotifyCoreDamage();
    }
    void Show(string name,Vector3 position,float width,float duration,float rise=0)
    {
        var renderer=NewRenderer(name,Get(name),position,1750);SetWidth(renderer,width);
        effects.Add(new Effect{Renderer=renderer,Started=Time.time,Duration=duration,Width=width,Rise=rise,Origin=position});
    }
    void Update()
    {
        coreFeedback?.Advance(Time.deltaTime);
        coreAmbient?.Advance(Time.deltaTime);
        for(int i=effects.Count-1;i>=0;i--)
        {
            var effect=effects[i];float age=(Time.time-effect.Started)/effect.Duration;
            if(effect.Renderer==null||age>=1){if(effect.Renderer!=null)Destroy(effect.Renderer.gameObject);effects.RemoveAt(i);continue;}
            effect.Renderer.transform.position=effect.Origin+Vector3.up*(age*effect.Rise);
            effect.Renderer.color=new Color(1,1,1,1-Mathf.Clamp01((age-.3f)/.7f));
        }
    }
    public void ClearEffects(){foreach(var e in effects)if(e.Renderer!=null)Destroy(e.Renderer.gameObject);effects.Clear();coreFeedback?.Clear();}
    void OnEnable(){coreAmbient?.SetEnabled(true);}
    void OnDisable(){coreFeedback?.Clear();coreAmbient?.SetEnabled(false);}
    void OnDestroy(){if(game!=null&&game.Session!=null)game.Session.DamageApplied-=OnDamage;ClearEffects();coreFeedback?.Dispose();coreAmbient?.Dispose();}
}
