using System;
using UnityEngine;

// Directional cutouts copied from the approved player.png. Never rotates the source or shadow.
public sealed class CoreGuardianPlayerMotion
{
    sealed class Pose
    {
        public Transform root;
        public SpriteRenderer body,arm,left,right;
    }
    readonly SpriteRenderer original;
    readonly Sprite[,] sprites=new Sprite[8,4];
    readonly Pose idle,walk,attack;
    readonly float ppu;
    float phase;
    bool moving;
    public Vector3 MuzzleWorld {get;private set;}
    public CoreGuardianPlayerMotion(SpriteRenderer artwork)
    {
        original=artwork;ppu=original.sprite.pixelsPerUnit;
        string[] names={"Body","Arm","LeftLeg","RightLeg"};
        for(int d=0;d<8;d++)for(int p=0;p<4;p++)
        {
            sprites[d,p]=Resources.Load<Sprite>("CoreGuardian/PlayerFacing/D"+d+"/D"+d+"_"+names[p]);
            if(sprites[d,p]==null)throw new InvalidOperationException("Missing original player cutout: "+d+"/"+names[p]);
        }
        idle=Create("OriginalIdle",false);walk=Create("PlayerWalk",false);attack=Create("OriginalMotion",true);
        Present(5,new Vector2(-8,-6).normalized,100,.25f,0);
    }
    Pose Create(string name,bool firing)
    {
        var p=new Pose{root=new GameObject(name).transform};p.root.SetParent(original.transform,false);
        p.body=Layer(p.root,firing?"Fixed":"Body");p.arm=Layer(p.root,firing?"Moving":"Arm");
        p.left=Layer(p.root,"LeftLeg");p.right=Layer(p.root,"RightLeg");return p;
    }
    SpriteRenderer Layer(Transform root,string name)
    {
        var r=new GameObject(name).AddComponent<SpriteRenderer>();r.transform.SetParent(root,false);r.sharedMaterial=original.sharedMaterial;return r;
    }
    public void Move(Vector2 velocity,float delta)
    {
        moving=velocity.sqrMagnitude>.000001f;
        // One tile (32 source pixels) is half a gait cycle: one alternating footstep per keypress.
        if(moving&&delta>0)phase=Mathf.Repeat(phase+velocity.magnitude*delta*ppu/Mathf.Max(.001f,Mathf.Abs(original.transform.lossyScale.x))/64,1);
    }
    static Vector2 NativePoint(int direction,Vector2 source)
    {
        float squeeze=direction==0||direction==4?.76f:direction==1||direction==3?.9f:1;
        source.x=Mathf.Round(18+(source.x-18)*squeeze);
        if(direction==0||direction==1||direction==2||direction==7)source.x=35-source.x;
        return source;
    }
    public void Present(int direction,Vector2 aim,float age,float duration,float idleTime)
    {
        bool firing=age<duration;var p=firing?attack:moving?walk:idle;
        idle.root.gameObject.SetActive(p==idle);walk.root.gameObject.SetActive(p==walk);attack.root.gameObject.SetActive(p==attack);original.enabled=false;
        var parts=new[]{p.body,p.arm,p.left,p.right};
        for(int i=0;i<4;i++)
        {
            var r=parts[i];r.sprite=sprites[direction,i];r.color=original.color;r.sortingLayerID=original.sortingLayerID;
            r.sortingOrder=original.sortingOrder+(i==1?2:i==0?1:0);r.transform.localPosition=Vector3.zero;r.transform.localRotation=Quaternion.identity;
        }
        float breath=!moving&&!firing?.55f*(1-Mathf.Cos(idleTime*Mathf.PI*2/2.8f))*.5f:0;
        p.body.transform.localPosition=Vector3.up*(breath/ppu);
        // Original trousers overlap under the shirt; only their own pixels move.
        if(moving)
        {
            p.left.transform.localPosition=Vector3.up*(3*Mathf.Max(0,Mathf.Sin(phase*Mathf.PI*2))/ppu);
            p.right.transform.localPosition=Vector3.up*(3*Mathf.Max(0,Mathf.Sin((phase+.5f)*Mathf.PI*2))/ppu);
        }
        Vector2 shoulder=NativePoint(direction,new Vector2(10,25));Vector2 tip=NativePoint(direction,new Vector2(2,19));
        var rest=tip-shoulder;float angle=Vector2.SignedAngle(rest,aim);
        float kick=firing?Mathf.Exp(-5*Mathf.Clamp01(age/duration)):0;
        var rotation=Quaternion.Euler(0,0,angle);var pivot=(shoulder-original.sprite.pivot)/ppu;
        p.arm.transform.localRotation=rotation;
        p.arm.transform.localPosition=(Vector3)pivot-rotation*(Vector3)pivot+Vector3.up*(breath/ppu)-(Vector3)aim*(1.25f*kick/ppu);
        MuzzleWorld=p.arm.transform.TransformPoint((tip-original.sprite.pivot)/ppu);
    }
}
