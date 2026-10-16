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
    float phase,gaitWeight;
    bool moving;
    Vector2 travel=Vector2.left;
    public Vector3 MuzzleWorld {get;private set;}
    public CoreGuardianPlayerMotion(SpriteRenderer artwork)
    {
        original=artwork;ppu=original.sprite.pixelsPerUnit;
        // A flattened copy of the idle soles cannot follow animated feet.
        foreach(var renderer in artwork.transform.parent.GetComponentsInChildren<SpriteRenderer>(true))
            if(renderer.name=="바닥 밀착선")renderer.enabled=false;
        string[] names={"Body","Arm","LeftLeg","RightLeg"};
        for(int d=0;d<8;d++)for(int p=0;p<4;p++)
        {
            sprites[d,p]=Resources.Load<Sprite>("CoreGuardian/PlayerFacing/D"+d+"/D"+d+"_"+names[p]);
            if(sprites[d,p]==null)throw new InvalidOperationException("Missing original player cutout: "+d+"/"+names[p]);
        }
        idle=Create("OriginalIdle",false);walk=Create("PlayerWalk",false);attack=Create("OriginalMotion",true);
        Present(5,new Vector2(-8,-6).normalized,100,.25f,0,false);
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
        delta=Mathf.Max(0,delta);gaitWeight=Mathf.MoveTowards(gaitWeight,moving?1:0,delta*10);
        if(moving&&delta>0)
        {
            travel=velocity.normalized;
            float cycles=velocity.magnitude*ppu/Mathf.Max(.001f,Mathf.Abs(original.transform.lossyScale.x))/64;
            phase=Mathf.Repeat(phase+Mathf.Min(3,cycles)*delta,1);
        }
        else if(gaitWeight>0)
        {
            // Finish the current half-step while blending out instead of teleporting a lifted boot.
            float plant=Mathf.Ceil(phase*2)/2;phase=Mathf.MoveTowards(phase,plant,delta*3);
        }
    }
    static Vector2 NativePoint(int direction,Vector2 source)
    {
        if(direction==0||direction==1||direction==2||direction==7)source.x=35-source.x;
        return source;
    }
    public void Present(int direction,Vector2 aim,float age,float duration,float idleTime,bool aiming)
    {
        bool firing=age<duration;var p=firing?attack:gaitWeight>0?walk:idle;
        idle.root.gameObject.SetActive(p==idle);walk.root.gameObject.SetActive(p==walk);attack.root.gameObject.SetActive(p==attack);original.enabled=false;
        var parts=new[]{p.body,p.arm,p.left,p.right};
        for(int i=0;i<4;i++)
        {
            var r=parts[i];r.sprite=sprites[direction,i];r.color=original.color;r.sortingLayerID=original.sortingLayerID;
            r.sortingOrder=original.sortingOrder+(i==1?2:i==0?1:0);r.transform.localPosition=Vector3.zero;r.transform.localRotation=Quaternion.identity;
        }
        float breath=p==idle?.55f*(1-Mathf.Cos(idleTime*Mathf.PI*2/2.8f))*.5f:0;
        p.body.transform.localPosition=Vector3.up*(breath/ppu);
        // Original trousers overlap under the shirt; only their own pixels move.
        if(gaitWeight>0)
        {
            bool mirror=direction==0||direction==1||direction==2||direction==7;
            var leftHip=mirror?new Vector2(11,14):new Vector2(14,16);
            var rightHip=mirror?new Vector2(21,16):new Vector2(24,14);
            if(direction==2){leftHip=new Vector2(13,14);rightHip=new Vector2(23,14);}
            float legSwing=direction==2?1.5f:6;
            Step(p.left.transform,leftHip,phase,legSwing);Step(p.right.transform,rightHip,phase+.5f,legSwing);
        }
        Vector2 shoulder=NativePoint(direction,new Vector2(10,25));Vector2 tip=NativePoint(direction,new Vector2(2,19));
        var rest=tip-shoulder;
        float angle=aiming||firing?Vector2.SignedAngle(rest,aim):5*Mathf.Sin(phase*Mathf.PI*2)*gaitWeight;
        float kick=firing?Mathf.Exp(-5*Mathf.Clamp01(age/duration)):0;
        var rotation=Quaternion.Euler(0,0,angle);var pivot=(shoulder-original.sprite.pivot)/ppu;
        p.arm.transform.localRotation=rotation;
        p.arm.transform.localPosition=(Vector3)pivot-rotation*(Vector3)pivot+Vector3.up*(breath/ppu)-(Vector3)aim*(1.25f*kick/ppu);
        MuzzleWorld=p.arm.transform.TransformPoint((tip-original.sprite.pivot)/ppu);
    }
    void Step(Transform leg,Vector2 hip,float p,float legSwing)
    {
        float angle=p*Mathf.PI*2,swing=Mathf.Sin(angle),stride=-Mathf.Cos(angle)*2.2f;
        var offset=new Vector2(travel.x*stride,travel.y*stride*.25f)+Vector2.up*(2.2f*Mathf.Max(0,swing));
        var pivot=(hip-original.sprite.pivot)/ppu;var rotation=Quaternion.Euler(0,0,legSwing*Mathf.Cos(angle)*gaitWeight);
        leg.localRotation=rotation;leg.localPosition=(Vector3)pivot-rotation*(Vector3)pivot+(Vector3)offset*(gaitWeight/ppu);
    }
}
