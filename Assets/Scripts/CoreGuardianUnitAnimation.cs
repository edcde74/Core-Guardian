using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Presentation only. Movement, targeting, attack cadence and projectile physics stay in Session.
public sealed class CoreGuardianUnitAnimation : MonoBehaviour
{
    [Serializable] public sealed class FrameInfo { public string name; public int x,y; public float muzzleX, muzzleY; }
    [Serializable] public sealed class AtlasInfo { public FrameInfo[] frames; }
    readonly Dictionary<string,Sprite> sprites = new Dictionary<string,Sprite>();
    readonly Dictionary<string,FrameInfo> frames = new Dictionary<string,FrameInfo>();
    SpriteRenderer artwork;
    SpriteRenderer upper;
    CoreGuardianOriginalMotion originalMotion;
    Vector3 restPosition;
    string role;
    float walkPhase, shotAge=100, shotDuration=.25f, facingAngle=-90;
    Vector2 aim=Vector2.down;
    int direction=3;
    bool walking, support;
    bool justFired;
    int lastShotFrame=-1;
    public int Direction => direction;
    // Counts presented volleys (one per source/frame), never a combat/balance metric.
    public int ShotCount {get;private set;}
    public Vector3 MuzzleWorld {get;private set;}
    public bool Loaded => originalMotion!=null||sprites.Count>=4;
    // Rejected generated player/towers must not override the original scene/prefab artwork.
    public bool UsesOriginalArtwork => role=="Player"||role=="Single"||role=="Multi"||role=="Support";

    public static CoreGuardianUnitAnimation Attach(GameObject root,string role)
    {
        var animation=root.GetComponent<CoreGuardianUnitAnimation>();
        if(animation!=null)return animation;
        animation=root.AddComponent<CoreGuardianUnitAnimation>();
        animation.Configure(role);
        return animation;
    }
    void Configure(string value)
    {
        role=value;support=role=="Support";
        if(role=="Player")direction=6;
        var body=GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r=>r.name=="2D Sprite")?.transform;
        if(body==null)throw new InvalidOperationException("Missing approved artwork child: "+name);
        artwork=body.GetComponent<SpriteRenderer>();
        restPosition=body.localPosition;
        if(UsesOriginalArtwork){originalMotion=new CoreGuardianOriginalMotion(artwork,role);MuzzleWorld=body.position;return;}
        var atlas=Resources.Load<Texture2D>("CoreGuardian/Animation/"+role+"/atlas");
        var metadata=Resources.Load<TextAsset>("CoreGuardian/Animation/"+role+"/frames");
        if(metadata!=null&&atlas!=null)foreach(var frame in JsonUtility.FromJson<AtlasInfo>(metadata.text).frames)
        {
            frames[frame.name]=frame;
            var sprite=Sprite.Create(atlas,new Rect(frame.x,frame.y,96,112),new Vector2(.5f,8f/112),32);
            sprite.name=frame.name;sprites[frame.name]=sprite;
        }
        if(Loaded) {restPosition=Vector3.zero;body.localPosition=restPosition;artwork.flipX=false;}
        if(sprites.TryGetValue("Base",out var foundation))
        {
            artwork.sprite=foundation;
            upper=new GameObject("AnimatedUpper").AddComponent<SpriteRenderer>();
            upper.transform.SetParent(body,false);upper.sharedMaterial=artwork.sharedMaterial;
        }
        // Directional feet have their own contact poses. Keep only ground ellipse shadows.
        if(Loaded)foreach(var renderer in GetComponentsInChildren<SpriteRenderer>())
            if(renderer.name=="바닥 밀착선")renderer.enabled=false;
        ApplyFrame();
    }
    public void Move(Vector2 velocity,float delta)
    {
        TrackMotion(velocity,delta,true);
    }
    public void MoveWithoutTurning(Vector2 velocity,float delta)
    {
        TrackMotion(velocity,delta,false);
    }
    void TrackMotion(Vector2 velocity,float delta,bool turn)
    {
        walking=velocity.sqrMagnitude>.000001f;
        if(walking)
        {
            if(turn)Face(velocity,false,delta);
            // Distance-driven loop prevents moonwalking at slow/fast/buffed speeds.
            walkPhase+=velocity.magnitude*delta*1.7f;
        }
        else walkPhase=0;
    }
    public void Aim(Vector2 vector,float delta)
    {
        if(vector.sqrMagnitude>.000001f)Face(vector,false,delta);
    }
    void Face(Vector2 vector,bool snap,float delta)
    {
        aim=vector.normalized;
        float target=Mathf.Atan2(aim.y,aim.x)*Mathf.Rad2Deg;
        facingAngle=snap?target:Mathf.MoveTowardsAngle(facingAngle,target,900*delta);
        // Hysteresis avoids alternating views when a target sits at a diagonal boundary.
        float[] angles=role=="Player"?new float[]{0,45,90,135,180,-135,-90,-45}:new float[]{0,180,90,-90};
        int best=direction;
        for(int i=0;i<angles.Length;i++)if(Mathf.Abs(Mathf.DeltaAngle(facingAngle,angles[i]))+6<Mathf.Abs(Mathf.DeltaAngle(facingAngle,angles[best])))best=i;
        direction=best;
    }
    public void Fire(Vector2 vector,float attacksPerSecond)
    {
        Face(vector,true,0);
        // Multi-target and augmented projectiles in one volley share one trigger motion.
        if(lastShotFrame!=Time.frameCount){ShotCount++;lastShotFrame=Time.frameCount;}
        shotAge=0;shotDuration=Mathf.Clamp(.8f/Mathf.Max(.01f,attacksPerSecond),.045f,.28f);
        justFired=true;
        ApplyFrame();
    }
    public void Present(float delta)
    {
        // Delta belongs to the simulation interval preceding this launch; render its first pose.
        if(!justFired)shotAge+=delta;
        justFired=false;
        ApplyFrame();
    }
    void ApplyFrame()
    {
        if(artwork==null)return;
        if(UsesOriginalArtwork)
        {
            originalMotion.Present(shotAge,shotDuration);
            MuzzleWorld=originalMotion.MuzzleWorld(aim);
            return;
        }
        int frame=0;
        bool shooting=shotAge<shotDuration;
        if(role=="Player")frame=shooting?1+Mathf.Min(2,Mathf.FloorToInt(shotAge/shotDuration*3)):0;
        else if(role=="Attack"||role=="Defense"||role=="Buff"||role=="Boss")frame=walking?Mathf.FloorToInt(walkPhase*4)%4:0;
        else if(support)frame=Mathf.FloorToInt(Time.time*5)%4;
        string key=role+"_"+direction+"_"+frame;
        var animated=upper!=null?upper:artwork;
        if(sprites.TryGetValue(key,out var sprite))animated.sprite=sprite;
        float recoil=shooting?(1-Mathf.Clamp01(shotAge/shotDuration))*.035f:0;
        // Feet and shadow roots never rotate or recoil. This is a tiny upper-body kick.
        artwork.transform.localPosition=restPosition;
        if(upper!=null){upper.transform.localPosition=-(Vector3)aim*recoil;upper.sortingOrder=artwork.sortingOrder+1;}
        artwork.transform.localRotation=Quaternion.identity;
        var position=Vector2.zero;
        if(frames.TryGetValue(key,out var info))position=new Vector2(info.muzzleX,info.muzzleY)/32f;
        MuzzleWorld=animated.transform.TransformPoint(position);
    }
    void OnDestroy(){foreach(var sprite in sprites.Values)if(sprite!=null)Destroy(sprite);}
}
