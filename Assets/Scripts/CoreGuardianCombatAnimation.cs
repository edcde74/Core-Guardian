using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using UnityEngine;

// Receives every real launch, even a projectile that is removed before SynchronizeViews.
public sealed class CoreGuardianCombatAnimation : MonoBehaviour
{
    CoreGuardianPrototype game;
    CoreGuardianUnitAnimation player;
    readonly Dictionary<int,CoreGuardianUnitAnimation> enemies=new Dictionary<int,CoreGuardianUnitAnimation>();
    readonly HashSet<int> pendingHits=new HashSet<int>();
    readonly Dictionary<int,CoreGuardianUnitAnimation> towers=new Dictionary<int,CoreGuardianUnitAnimation>();
    readonly Dictionary<int,Vector2> previousEnemies=new Dictionary<int,Vector2>();
    readonly Dictionary<int,Vector3> launchOffsets=new Dictionary<int,Vector3>();
    readonly Dictionary<int,Vector2> launchOrigins=new Dictionary<int,Vector2>();
    Vector3 previousPlayer;
    sealed class Flash {public SpriteRenderer renderer; public float age;public int createdFrame;}
    readonly List<Flash> flashes=new List<Flash>();
    readonly Dictionary<string,int> volleyFrames=new Dictionary<string,int>();
    Sprite flashSprite;

    public void Configure(CoreGuardianPrototype prototype)
    {
        game=prototype;
        player=CoreGuardianUnitAnimation.Attach(game.player.gameObject,"Player");
        var core=FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include)
            .FirstOrDefault(r=>r.gameObject.scene==game.gameObject.scene&&r.name=="2D Sprite"&&r.sprite!=null&&r.sprite.name=="core");
        if(core!=null)CoreGuardianPhotoShadow.Attach(core.transform.parent.gameObject,"Core");
        previousPlayer=game.player.transform.position;
        flashSprite=Resources.LoadAll<Sprite>(CoreGuardianCombatArt.ResourcePath).First(s=>s.name=="SingleBolt");
        game.Session.ProjectileLaunched+=Launched;
        game.Session.DamageApplied+=Damaged;
    }
    public void Enemy(EnemyState enemy,GameObject view)
    {
        if(!enemies.TryGetValue(enemy.Id,out var animation))
        {
            animation=CoreGuardianUnitAnimation.Attach(view,enemy.Kind.ToString());enemies.Add(enemy.Id,animation);
            previousEnemies[enemy.Id]=enemy.Position;
        }
        if(pendingHits.Remove(enemy.Id))animation.ReceiveHit();
        float dt=Time.deltaTime;
        var previous=previousEnemies[enemy.Id];
        animation.Move(dt>0?(Vector2)(game.World(enemy.Position)-game.World(previous))/dt:Vector2.zero,dt);
        previousEnemies[enemy.Id]=enemy.Position;
        animation.Present(dt);
    }
    public void Tower(TowerState tower,GameObject view)
    {
        if(!towers.TryGetValue(tower.Id,out var animation))
        {animation=CoreGuardianUnitAnimation.Attach(view,tower.Kind.ToString());towers.Add(tower.Id,animation);}
        var target=game.Session.TowerTarget(tower.Id);
        if(target!=null&&target.Alive)
            animation.Aim(tower.Kind==TowerKind.Single?(Vector2)(game.World(target.Position)+Vector3.up*.25f-animation.AimOriginWorld):target.Position-tower.Position,Time.deltaTime);
        animation.Present(Time.deltaTime);
    }
    void Damaged(EnemyState enemy,int amount)
    {
        if(amount<=0||!enemy.Alive)return;
        if(enemies.TryGetValue(enemy.Id,out var animation)&&animation!=null)animation.ReceiveHit();
        else pendingHits.Add(enemy.Id);
    }
    public void Player()
    {
        float dt=Time.deltaTime;
        Vector2 velocity=dt>0?(Vector2)(game.player.transform.position-previousPlayer)/dt:Vector2.zero;
        var target=game.Session.PlayerTarget;
        if(target!=null&&target.Alive)player.MoveWithoutTurning(velocity,dt);else player.Move(velocity,dt);
        previousPlayer=game.player.transform.position;
        if(target!=null&&target.Alive)player.Aim((Vector2)(game.World(target.Position)-game.player.transform.position),dt);
        player.Present(dt);
        foreach(int id in enemies.Where(x=>x.Value==null).Select(x=>x.Key).ToArray()){enemies.Remove(id);previousEnemies.Remove(id);}
        if(pendingHits.Count>0)pendingHits.RemoveWhere(id=>!game.Session.Enemies.Any(e=>e.Id==id&&e.Alive));
        foreach(int id in towers.Where(x=>x.Value==null).Select(x=>x.Key).ToArray())towers.Remove(id);
        var active=new HashSet<int>(game.Session.Projectiles.Select(p=>p.Id));
        foreach(int id in launchOffsets.Keys.Where(id=>!active.Contains(id)).ToArray()){launchOffsets.Remove(id);launchOrigins.Remove(id);}
        for(int i=flashes.Count-1;i>=0;i--)
        {
            var f=flashes[i];if(f.createdFrame!=Time.frameCount)f.age+=dt;
            if(f.age>=.07f||f.renderer==null){if(f.renderer!=null)Destroy(f.renderer.gameObject);flashes.RemoveAt(i);continue;}
            f.renderer.color=new Color(1,.85f,.4f,1-f.age/.07f);
        }
    }
    void Launched(ProjectileState shot)
    {
        CoreGuardianUnitAnimation source=null;float rate=1;bool single=false;
        if(shot.Source=="Player"){source=player;rate=game.Session.Player.Rate;}
        else if(shot.Source.StartsWith("Tower:")&&int.TryParse(shot.Source.Substring(6),out int id))
        {
            towers.TryGetValue(id,out source);
            var tower=game.Session.Towers.FirstOrDefault(t=>t.Id==id);if(tower!=null){rate=tower.Stats.Rate;single=tower.Kind==TowerKind.Single;}
        }
        if(source==null)return;
        var direction=game.World(shot.Target.Position)-game.World(shot.Position);
        if(single)direction=game.World(shot.Target.Position)+Vector3.up*.25f-source.AimOriginWorld;
        if(!volleyFrames.TryGetValue(shot.Source,out int frame)||frame!=Time.frameCount)
        {source.Fire(direction,rate);volleyFrames[shot.Source]=Time.frameCount;}
        if(shot.Source.StartsWith("Tower:"))
        {
            // A support crystal responds only when a tower inside its actual buff radius fires.
            foreach(var support in game.Session.Towers.Where(t=>t.Kind==TowerKind.Support&&Vector2.Distance(t.Position,shot.Position)<=t.Stats.Range))
            {
                string key="Support:"+support.Id;
                if(towers.TryGetValue(support.Id,out var aura)&&(!volleyFrames.TryGetValue(key,out int last)||last!=Time.frameCount))
                {aura.Fire(direction,rate);volleyFrames[key]=Time.frameCount;}
            }
        }
        var muzzle=source.MuzzleWorld;
        launchOffsets[shot.Id]=muzzle-(game.World(shot.Position)+Vector3.up*.25f);
        launchOrigins[shot.Id]=shot.Position;
        // Original cutouts keep the approved projectile effect; only its real launch origin moves.
        if(source.UsesOriginalArtwork)return;
        var flash=new GameObject("Muzzle_"+shot.Id).AddComponent<SpriteRenderer>();
        flash.transform.SetParent(transform,false);flash.transform.position=muzzle;
        flash.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
        flash.sprite=flashSprite;flash.sortingOrder=1501;CoreGuardianCombatArt.SetWidth(flash,.12f);
        flashes.Add(new Flash{renderer=flash,createdFrame=Time.frameCount});
    }
    public Vector3 ProjectilePosition(ProjectileState shot)
    {
        var position=game.World(shot.Position)+Vector3.up*.25f;
        if(launchOffsets.TryGetValue(shot.Id,out var offset))
            position+=offset*Mathf.Clamp01(1-Vector2.Distance(shot.Position,launchOrigins[shot.Id])/Mathf.Max(.001f,Vector2.Distance(shot.Target.Position,launchOrigins[shot.Id])));
        return position;
    }
    void OnDestroy()
    {
        if(game!=null&&game.Session!=null)game.Session.ProjectileLaunched-=Launched;
        if(game!=null&&game.Session!=null)game.Session.DamageApplied-=Damaged;
        foreach(var flash in flashes)if(flash.renderer!=null)Destroy(flash.renderer.gameObject);
    }
}
