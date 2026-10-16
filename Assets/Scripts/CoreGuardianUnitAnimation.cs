using System;
using System.Linq;
using UnityEngine;

// Presentation only. Keeps source artwork and its directional extensions; Session owns combat and movement.
public sealed class CoreGuardianUnitAnimation : MonoBehaviour
{
    // Retained for reading archived animation metadata in diagnostic tools.
    [Serializable] public sealed class FrameInfo {public string name;public int x,y;public float muzzleX,muzzleY;}
    [Serializable] public sealed class AtlasInfo {public FrameInfo[] frames;}
    static readonly float[] PlayerAngles={0,45,90,135,180,-135,-90,-45};
    static readonly float[] CardinalAngles={0,180,90,-90};
    SpriteRenderer artwork;
    CoreGuardianOriginalMotion originalMotion;
    CoreGuardianOriginalWalk originalWalk;
    CoreGuardianPlayerMotion playerMotion;
    CoreGuardianHitFlash hitFlash;
    bool justHit;
    string role;
    float shotAge=100,shotDuration=.25f,idleTime,facingAngle=-90;
    Vector2 aim=Vector2.down;
    int direction=3,lastShotFrame=-1;
    bool walking,justFired,aiming;
    public int Direction=>direction;
    // Presented volleys, never a combat or balance metric.
    public int ShotCount {get;private set;}
    public Vector3 MuzzleWorld {get;private set;}
    public Vector3 HealthBarWorld => originalWalk!=null?originalWalk.HealthBarWorld:transform.position;
    public bool Loaded=>originalMotion!=null||originalWalk!=null||playerMotion!=null;
    public bool UsesOriginalArtwork=>role=="Player"||role=="Single"||role=="Multi"||role=="Support";
    public bool UsesOriginalWalking=>role=="Attack"||role=="Defense"||role=="Buff"||role=="Boss";
    public static CoreGuardianUnitAnimation Attach(GameObject root,string role)
    {
        var animation=root.GetComponent<CoreGuardianUnitAnimation>();if(animation!=null)return animation;
        animation=root.AddComponent<CoreGuardianUnitAnimation>();animation.Configure(role);return animation;
    }
    void Configure(string value)
    {
        role=value;if(role=="Player"){direction=5;facingAngle=-135;aim=new Vector2(-8,-6).normalized;}
        artwork=GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r=>r.name=="2D Sprite");
        if(artwork==null)throw new InvalidOperationException("Missing approved artwork child: "+name);
        if(role=="Player")playerMotion=new CoreGuardianPlayerMotion(artwork);
        else if(UsesOriginalArtwork)
        {
            originalMotion=new CoreGuardianOriginalMotion(artwork,role);
        }
        else if(UsesOriginalWalking){originalWalk=new CoreGuardianOriginalWalk(artwork,role);hitFlash=new CoreGuardianHitFlash(artwork);}
        else throw new InvalidOperationException("Unsupported original animation role: "+role);
        MuzzleWorld=artwork.transform.position;
    }
    public void Move(Vector2 worldVelocity,float delta)=>TrackMotion(worldVelocity,delta,true);
    public void MoveWithoutTurning(Vector2 worldVelocity,float delta)=>TrackMotion(worldVelocity,delta,false);
    void TrackMotion(Vector2 velocity,float delta,bool turn)
    {
        originalWalk?.Move(velocity,delta);playerMotion?.Move(velocity,delta);walking=velocity.sqrMagnitude>.000001f;
        if(role=="Player")aiming=!turn;
        if(walking&&turn)Face(velocity,role=="Player",delta);
    }
    public void Aim(Vector2 vector,float delta){if(vector.sqrMagnitude>.000001f){aiming=true;Face(vector,false,delta);}}
    void Face(Vector2 vector,bool snap,float delta)
    {
        aim=vector.normalized;
        float target=Mathf.Atan2(aim.y,aim.x)*Mathf.Rad2Deg;
        facingAngle=snap?target:Mathf.MoveTowardsAngle(facingAngle,target,900*delta);
        var angles=role=="Player"?PlayerAngles:CardinalAngles;int best=direction;
        for(int i=0;i<angles.Length;i++)if(Mathf.Abs(Mathf.DeltaAngle(facingAngle,angles[i]))+6<Mathf.Abs(Mathf.DeltaAngle(facingAngle,angles[best])))best=i;
        direction=best;
    }
    public void Fire(Vector2 vector,float attacksPerSecond)
    {
        Face(vector,true,0);
        if(lastShotFrame!=Time.frameCount){ShotCount++;lastShotFrame=Time.frameCount;}
        shotAge=0;shotDuration=Mathf.Clamp(.8f/Mathf.Max(.01f,attacksPerSecond),.045f,.28f);
        justFired=true;ApplyFrame(0);
    }
    public void Present(float delta)
    {
        if(!walking&&shotAge>=shotDuration)idleTime+=delta;else idleTime=0;
        if(!justFired)shotAge+=delta;justFired=false;ApplyFrame(delta);
        hitFlash?.Advance(justHit?0:delta);justHit=false;
    }
    public void ReceiveHit(){if(hitFlash!=null&&hitFlash.Trigger())justHit=true;}
    void OnDisable(){hitFlash?.Clear();}
    void ApplyFrame(float delta)
    {
        if(artwork==null)return;
        if(playerMotion!=null){playerMotion.Present(direction,aim,shotAge,shotDuration,idleTime,aiming);MuzzleWorld=playerMotion.MuzzleWorld;return;}
        if(originalWalk!=null){originalWalk.Present(delta);return;}
        originalMotion.Present(shotAge,shotDuration);
        MuzzleWorld=originalMotion.MuzzleWorld(aim);
    }
}
