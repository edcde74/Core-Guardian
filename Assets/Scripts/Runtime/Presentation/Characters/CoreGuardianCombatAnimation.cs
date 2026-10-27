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
    readonly List<int> expiredIds=new List<int>();
    readonly HashSet<int> activeProjectileIds=new HashSet<int>();
    readonly HashSet<int> livingEnemyIds=new HashSet<int>();
    Vector3 previousPlayer;
    sealed class Flash {public SpriteRenderer renderer; public float age;public int createdFrame;}
    readonly List<Flash> flashes=new List<Flash>();
    const int MaxRetainedFlashes=128;
    readonly Stack<Flash> flashPool=new Stack<Flash>();
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
        game.Session.TowerPulsed+=Pulsed;
    }
    // 냉기 포탑 pulse has no projectile; play the tower's attack frames directly.
    void Pulsed(int id,Vector2 position,float radius)
    {
        if(!towers.TryGetValue(id,out var source)||source==null)return;
        var tower=game.Session.FindTower(id);source.Fire(Vector2.down,tower!=null?tower.Stats.Rate:1);
    }
    public void Enemy(EnemyHandle enemy,GameObject view)
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
    public void ReleaseEnemy(int id)
    {
        enemies.Remove(id);previousEnemies.Remove(id);pendingHits.Remove(id);
    }
    public void Tower(TowerState tower,GameObject view)
    {
        if(!towers.TryGetValue(tower.Id,out var animation))
        {animation=CoreGuardianUnitAnimation.Attach(view,tower.Kind.ToString());towers.Add(tower.Id,animation);}
        var target=game.Session.TowerTargetForView(tower);
        if(target.Alive)
            animation.Aim(tower.Kind==TowerKind.Single||tower.Kind==TowerKind.Flame||tower.Kind==TowerKind.Rail?(Vector2)(game.World(target.Position)+Vector3.up*.25f-animation.AimOriginWorld):target.Position-tower.Position,Time.deltaTime);
        animation.Present(Time.deltaTime);
    }
    void Damaged(EnemySnapshot enemy,int amount)
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
        if(target.Alive)player.MoveWithoutTurning(velocity,dt);else player.Move(velocity,dt);
        previousPlayer=game.player.transform.position;
        if(target.Alive)player.Aim((Vector2)(game.World(target.Position)-game.player.transform.position),dt);
        player.Present(dt);
        expiredIds.Clear();foreach(var entry in enemies)if(entry.Value==null)expiredIds.Add(entry.Key);
        foreach(int id in expiredIds){enemies.Remove(id);previousEnemies.Remove(id);}
        if(pendingHits.Count>0)
        {
            livingEnemyIds.Clear();foreach(var enemy in game.Session.Enemies)if(enemy.Alive)livingEnemyIds.Add(enemy.Id);
            expiredIds.Clear();foreach(int id in pendingHits)if(!livingEnemyIds.Contains(id))expiredIds.Add(id);
            foreach(int id in expiredIds)pendingHits.Remove(id);
        }
        expiredIds.Clear();foreach(var entry in towers)if(entry.Value==null)expiredIds.Add(entry.Key);
        foreach(int id in expiredIds)towers.Remove(id);
        activeProjectileIds.Clear();foreach(var projectile in game.Session.Projectiles)activeProjectileIds.Add(projectile.Id);
        expiredIds.Clear();foreach(int id in launchOffsets.Keys)if(!activeProjectileIds.Contains(id))expiredIds.Add(id);
        foreach(int id in expiredIds){launchOffsets.Remove(id);launchOrigins.Remove(id);}
        expiredIds.Clear();
        for(int i=flashes.Count-1;i>=0;i--)
        {
            var f=flashes[i];if(f.createdFrame!=Time.frameCount)f.age+=dt;
            if(f.age>=.07f||f.renderer==null)
            {
                if(f.renderer!=null)
                {
                    f.renderer.gameObject.SetActive(false);
                    if(flashPool.Count<MaxRetainedFlashes)flashPool.Push(f);else Destroy(f.renderer.gameObject);
                }
                flashes.RemoveAt(i);continue;
            }
            f.renderer.color=new Color(1,.85f,.4f,1-f.age/.07f);
        }
    }
    void Launched(ProjectileSnapshot shot)
    {
        CoreGuardianUnitAnimation source=null;float rate=1;bool single=false;
        if(shot.Source=="Player"){source=player;rate=game.Session.Player.Rate;}
        else if(game.Session.TryTowerSourceId(shot.Source,out int id))
        {
            towers.TryGetValue(id,out source);
            var tower=game.Session.FindTower(id);if(tower!=null){rate=tower.Stats.Rate;single=tower.Kind==TowerKind.Single;}
        }
        if(source==null)return;
        var aimAt=shot.Line?shot.Position+shot.Direction:shot.Target.Position;
        var direction=game.World(aimAt)-game.World(shot.Position);
        if(single)direction=game.World(aimAt)+Vector3.up*.25f-source.AimOriginWorld;
        if(!volleyFrames.TryGetValue(shot.Source,out int frame)||frame!=Time.frameCount)
        {source.Fire(direction,rate);volleyFrames[shot.Source]=Time.frameCount;}
        if(shot.Source.StartsWith("Tower:"))
        {
            // A support crystal responds only when a tower inside its actual buff radius fires.
            foreach(var support in game.Session.Towers)
            {
                if(support.Kind!=TowerKind.Support||!(Vector2.Distance(support.Position,shot.Position)<=support.Stats.Range))continue;
                string key=support.SupportSource;
                if(towers.TryGetValue(support.Id,out var aura)&&(!volleyFrames.TryGetValue(key,out int last)||last!=Time.frameCount))
                {aura.Fire(direction,rate);volleyFrames[key]=Time.frameCount;}
            }
        }
        var muzzle=source.MuzzleWorld;
        launchOffsets[shot.Id]=muzzle-(game.World(shot.Position)+Vector3.up*.25f);
        launchOrigins[shot.Id]=shot.Position;
        // Original cutouts keep the approved projectile effect; only its real launch origin moves.
        if(source.UsesOriginalArtwork)return;
        Flash entry=null;
        while(flashPool.Count>0&&entry==null){var candidate=flashPool.Pop();if(candidate.renderer!=null)entry=candidate;}
        if(entry==null)entry=new Flash{renderer=new GameObject("Muzzle_"+shot.Id).AddComponent<SpriteRenderer>()};
        var flash=entry.renderer;flash.name="Muzzle_"+shot.Id;
        flash.transform.SetParent(transform,false);flash.transform.position=muzzle;
        flash.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
        flash.sprite=flashSprite;flash.color=Color.white;flash.flipX=flash.flipY=false;flash.sortingOrder=ArtDepthSorting.OverlayBase+1501;CoreGuardianCombatArt.SetWidth(flash,.12f);
        entry.age=0;entry.createdFrame=Time.frameCount;
        flash.transform.SetAsLastSibling();flash.gameObject.SetActive(true);flashes.Add(entry);
    }
    public Vector3 ProjectilePosition(ProjectileHandle shot)
    {
        var position=game.World(shot.Position)+Vector3.up*.25f;
        if(launchOffsets.TryGetValue(shot.Id,out var offset))
            position+=offset*Mathf.Clamp01(1-Vector2.Distance(shot.Position,launchOrigins[shot.Id])/Mathf.Max(.001f,Vector2.Distance(shot.TargetPosition,launchOrigins[shot.Id])));
        return position;
    }
    void OnDestroy()
    {
        if(game!=null&&game.Session!=null)game.Session.ProjectileLaunched-=Launched;
        if(game!=null&&game.Session!=null)game.Session.DamageApplied-=Damaged;
        if(game!=null&&game.Session!=null)game.Session.TowerPulsed-=Pulsed;
        foreach(var flash in flashes)if(flash.renderer!=null)Destroy(flash.renderer.gameObject);
        while(flashPool.Count>0){var flash=flashPool.Pop();if(flash.renderer!=null)Destroy(flash.renderer.gameObject);}
    }
}
