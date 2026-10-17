using UnityEngine;

// Original cutout legs on X; separate source-based rear/front frames on Y.
public sealed class CoreGuardianOriginalWalk
{
    readonly SpriteRenderer original, body;
    readonly Transform rig;
    readonly SpriteRenderer[] layers;
    readonly float ppu, cyclePixels, lift;
    readonly Vector2 groundAnchor;
    readonly Vector3 groundInParent;
    readonly float healthTopPixels;
    readonly SpriteRenderer vertical;
    readonly Sprite[,] verticalFrames=new Sprite[2,4];
    readonly VerticalLegPair verticalLegs;
    readonly Leg left,right;
    float phase,weight;
    bool walking,mirror;
    int verticalFacing;
    Vector2 groundDirection=Vector2.left;
    sealed class Leg
    {
        public Vector2 hip,knee,ankle;
        public Transform upper,lower;
    }
    public float Phase => phase;
    public Vector3 HealthBarWorld => original.transform.parent.TransformPoint(
        groundInParent+Vector3.up*((healthTopPixels+4)*original.transform.localScale.y/ppu));
    public CoreGuardianOriginalWalk(SpriteRenderer artwork,string role)
    {
        original=artwork;ppu=original.sprite.pixelsPerUnit;
        // Maximum opaque height above the foot anchor across source and all Y frames.
        // Include raised equipment, so the bar never cuts across a spear or banner.
        // Stable per role: walking frames must not make the health bar bob.
        rig=new GameObject("OriginalWalk").transform;rig.SetParent(artwork.transform,false);
        body=Layer("Body",role);var lu=Layer("LeftUpper",role);var ll=Layer("LeftLower",role);var ru=Layer("RightUpper",role);var rl=Layer("RightLower",role);
        layers=new[]{body,lu,ll,ru,rl};
        if(role=="Attack") {left=Bone(lu,ll,18,43,19,49,17,61);right=Bone(ru,rl,30,43,31,51,35,60);cyclePixels=14;lift=1.8f;}
        else if(role=="Defense") {left=Bone(lu,ll,17,31,17,36,17,42);right=Bone(ru,rl,24,29,24,36,27,44);cyclePixels=10;lift=1.4f;}
        else if(role=="Buff") {left=Bone(lu,ll,13,61,12,67,9,74);right=Bone(ru,rl,22,60,24,69,24,77);cyclePixels=12;lift=1.6f;}
        else {left=Bone(lu,ll,32,51,32,59,28,67);right=Bone(ru,rl,47,51,49,60,53,70);cyclePixels=18;lift=2.2f;}
        // Midpoint of the two original boot soles, in source pixels relative to its pivot.
        // This is a ground anchor, not the center of the canvas/weapon/banner.
        groundAnchor=role=="Attack"?new Vector2(3.5f,0):role=="Defense"?new Vector2(5.5f,4):role=="Buff"?new Vector2(-1.75f,1.5f):new Vector2(-.75f,2);
        float directionalTop=role=="Attack"?65:role=="Defense"?46:role=="Buff"?84:65;
        healthTopPixels=Mathf.Max(directionalTop,original.sprite.rect.height-original.sprite.pivot.y-groundAnchor.y);
        var parent=artwork.transform.parent;
        groundInParent=parent.InverseTransformPoint(artwork.transform.TransformPoint(groundAnchor/ppu));
        foreach(var renderer in parent.GetComponentsInChildren<SpriteRenderer>(true))
            if(renderer.sprite!=null&&renderer.sprite.name.Contains("Shadow"))
            {groundInParent=parent.InverseTransformPoint(renderer.bounds.center);break;}
        // The prefab's flattened original soles are static. They look like extra feet
        // when the walking rig lifts or turns; retain the central ellipse instead.
        foreach(var renderer in parent.GetComponentsInChildren<SpriteRenderer>(true))
            if(renderer.name=="바닥 밀착선")renderer.enabled=false;
        AlignGround();
        rig.gameObject.SetActive(false);
        vertical=new GameObject("VerticalWalk").AddComponent<SpriteRenderer>();vertical.transform.SetParent(artwork.transform,false);
        vertical.sharedMaterial=artwork.sharedMaterial;vertical.transform.localPosition=groundAnchor/ppu;
        for(int d=0;d<2;d++)for(int f=0;f<4;f++)
        {
            string path="CoreGuardian/VerticalWalk/"+role+"/"+(d==0?"Up_":"Down_")+f;
            verticalFrames[d,f]=Resources.Load<Sprite>(path);
            if(verticalFrames[d,f]==null)throw new System.InvalidOperationException("Missing directional walk frame "+path);
        }
        vertical.gameObject.SetActive(false);
        verticalLegs=new VerticalLegPair(vertical,verticalFrames,role);
    }
    SpriteRenderer Layer(string name,string role)
    {
        var sprite=Resources.Load<Sprite>("CoreGuardian/OriginalWalk/"+role+"/"+name);
        if(sprite==null)throw new System.InvalidOperationException("Missing original walk layer "+role+"/"+name);
        var renderer=new GameObject(name).AddComponent<SpriteRenderer>();renderer.transform.SetParent(rig,false);renderer.sprite=sprite;renderer.sharedMaterial=original.sharedMaterial;return renderer;
    }
    Vector2 Point(float x,float top)=>(new Vector2(x,original.sprite.rect.height-top)-original.sprite.pivot);
    Leg Bone(SpriteRenderer upper,SpriteRenderer lower,float hx,float hy,float kx,float ky,float ax,float ay)=>new Leg{hip=Point(hx,hy),knee=Point(kx,ky),ankle=Point(ax,ay),upper=upper.transform,lower=lower.transform};
    public void Move(Vector2 worldVelocity,float delta)
    {
        walking=worldVelocity.sqrMagnitude>.000001f;
        if(!walking)return;
        Vector2 local=original.transform.InverseTransformVector(worldVelocity);
        // Select the presented direction from the actual waypoint displacement, on this turn.
        // A vertical turn must replace the side artwork, not just its leg trajectory.
        verticalFacing=Mathf.Abs(local.y)>Mathf.Abs(local.x)?(local.y>0?1:-1):0;
        if(verticalFacing!=0)mirror=false;
        else if(local.x>.0001f)mirror=true;else if(local.x<-.0001f)mirror=false;
        phase=Mathf.Repeat(phase+local.magnitude*Mathf.Max(0,delta)*ppu/cyclePixels,1);
        if(mirror)local.x=-local.x;
        groundDirection=local.normalized;
    }
    // Stance lasts exactly half a cycle for either leg. The other half is a lifted return.
    public static Vector2 FootOffset(float phase,float cyclePixels,float lift,Vector2 direction)
    {
        float p=Mathf.Repeat(phase,1),stride=cyclePixels*.5f;
        if(p<.5f)return direction*(stride*(.5f-p*2));
        float t=(p-.5f)*2,forward=t*t*(3-2*t);
        return direction*(stride*(forward-.5f))+Vector2.up*(lift*Mathf.Sin(t*Mathf.PI));
    }
    public void Present(float delta)
    {
        weight=Mathf.MoveTowards(weight,walking?1:0,Mathf.Max(0,delta)*8);
        original.flipX=mirror;
        AlignGround();
        vertical.gameObject.SetActive(verticalFacing!=0);
        if(verticalFacing!=0)
        {
            rig.gameObject.SetActive(false);original.enabled=false;
            vertical.sprite=verticalFrames[verticalFacing>0?0:1,walking?Mathf.Min(3,(int)(phase*4)):0];
            vertical.color=original.color;vertical.sortingLayerID=original.sortingLayerID;vertical.sortingOrder=original.sortingOrder;
            verticalLegs?.Present(vertical.sprite,verticalFacing>0?0:1,phase,walking);
            return;
        }
        bool active=weight>.0001f;rig.gameObject.SetActive(active);original.enabled=!active;
        if(!active)return;
        rig.localScale=new Vector3(mirror?-1:1,1,1);
        foreach(var layer in layers){layer.color=original.color;layer.sortingLayerID=original.sortingLayerID;layer.sortingOrder=original.sortingOrder;}
        body.sortingOrder=original.sortingOrder+1;
        // Two equal weight transfers per cycle, rather than one deep dip on a single leg.
        Vector2 pelvis=Vector2.up*((-.65f+.3f*Mathf.Cos(phase*Mathf.PI*4))*weight);
        body.transform.localPosition=pelvis/ppu;
        Pose(left,WalkingFootOffset(phase,cyclePixels,lift,groundDirection)*weight,pelvis);
        Pose(right,WalkingFootOffset(phase+.5f,cyclePixels,lift,groundDirection)*weight,pelvis);
    }
    void AlignGround()
    {
        Vector3 anchor=new Vector3((mirror?-1:1)*groundAnchor.x,groundAnchor.y,0)/ppu;
        var transform=original.transform;
        transform.localPosition=groundInParent-transform.localRotation*Vector3.Scale(transform.localScale,anchor);
    }
    public static Vector2 WalkingFootOffset(float phase,float cyclePixels,float lift,Vector2 direction)
    {
        // On the ground plane, depth travel is foreshortened; boot lift remains screen-up.
        // Using full vertical stride stretched a nearly straight leg against the IK limit,
        // concealing the return step, particularly when travelling toward +Y.
        float depth=Mathf.Abs(direction.y);
        return FootOffset(phase,cyclePixels,lift*(1+depth),new Vector2(direction.x,direction.y*.3f));
    }
    void Pose(Leg leg,Vector2 offset,Vector2 pelvis)
    {
        Vector2 hip=leg.hip+pelvis,goal=leg.ankle+offset;
        float upper=Vector2.Distance(leg.hip,leg.knee),lower=Vector2.Distance(leg.knee,leg.ankle);
        Vector2 difference=goal-hip;float distance=Mathf.Clamp(difference.magnitude,Mathf.Abs(upper-lower)+.001f,upper+lower-.001f);
        var direction=difference.normalized;float along=(upper*upper-lower*lower+distance*distance)/(2*distance);
        var rest=leg.ankle-leg.hip;var kneeRest=leg.knee-leg.hip;float side=Mathf.Sign(rest.x*kneeRest.y-rest.y*kneeRest.x);if(side==0)side=1;
        Vector2 knee=hip+direction*along+new Vector2(-direction.y,direction.x)*(side*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along)));
        Vector2 ankle=hip+direction*distance;
        Place(leg.upper,leg.hip,hip,Vector2.SignedAngle(leg.knee-leg.hip,knee-hip));
        Place(leg.lower,leg.knee,knee,Vector2.SignedAngle(leg.ankle-leg.knee,ankle-knee));
    }
    void Place(Transform part,Vector2 sourceJoint,Vector2 destination,float angle)
    {
        var rotation=Quaternion.Euler(0,0,angle);part.localRotation=rotation;
        part.localPosition=((Vector3)destination-rotation*(Vector3)sourceJoint)/ppu;
    }

    // The directional drawings held one knee up in every frame. Reuse their existing
    // boot pixels as two independent legs; the upper body and equipment keep their art.
    sealed class VerticalLegPair
    {
        static readonly System.Collections.Generic.Dictionary<Sprite,Sprite[]> bodies=new System.Collections.Generic.Dictionary<Sprite,Sprite[]>();
        static readonly System.Collections.Generic.Dictionary<Sprite,Sprite> boots=new System.Collections.Generic.Dictionary<Sprite,Sprite>();
        readonly SpriteRenderer body,leftFoot,rightFoot;
        readonly SpriteRenderer[] bodyParts;
        readonly Rect[][] bodyRects;
        readonly Sprite[] templates=new Sprite[2];
        readonly bool frontOnly;
        readonly float leftX,rightX;
        readonly Sprite frontRest;
        public VerticalLegPair(SpriteRenderer renderer,Sprite[,] frames,string role)
        {
            body=renderer;
            frontOnly=role!="Attack";
            frontRest=frames[1,0];
            // Cut only the fixed front legs. Keep shield/banner/hammer columns and
            // every upper-body frame in place; rear artwork remains unchanged.
            int top=19,cutLeft=42,cutRight=59,bootX=44,bootWidth=7,bootHeight=23;
            leftX=-3;rightX=5;
            if(role=="Defense"){top=22;cutLeft=43;cutRight=55;bootX=48;bootWidth=6;bootHeight=22;leftX=-4;rightX=3;}
            else if(role=="Buff"){top=22;cutLeft=43;cutRight=58;bootX=44;bootWidth=8;bootHeight=22;leftX=-1.5f;rightX=6.5f;}
            else if(role=="Boss"){top=25;cutLeft=40;cutRight=67;bootX=45;bootWidth=12;bootHeight=25;leftX=-1;rightX=12;}
            bodyRects=new[]{frontOnly?null:BodyRects(frames[0,0],19,39,59),frontOnly?FrontBodyRects(frontRest,top,cutLeft,cutRight):BodyRects(frontRest,top,cutLeft,cutRight)};
            bodyParts=frontOnly?new[]{FootRenderer("UpperBody"),FootRenderer("LeftEquipment"),FootRenderer("RightEquipment"),FootRenderer("HipCloth")}:new[]{FootRenderer("UpperBody"),FootRenderer("LeftEquipment"),FootRenderer("RightEquipment")};
            leftFoot=FootRenderer("LeftFoot");rightFoot=FootRenderer("RightFoot");rightFoot.flipX=true;
            for(int d=0;d<2;d++)
            {
                if(frontOnly&&d==0)continue;
                var source=frames[d,0];var key=source;
                if(!boots.TryGetValue(key,out var boot)||boot==null)
                {
                    // Rear and front drawings have different leg columns. The rear leg is
                    // x40..46; x44..50 retained only its thin edge plus part of the bent leg.
                    int height=d==0?23:bootHeight;
                    boot=Sprite.Create(source.texture,new Rect(source.rect.x+(d==0?40:bootX),source.rect.y,d==0?7:bootWidth,height),new Vector2(.5f,3f/height),source.pixelsPerUnit);
                    boot.name=source.name+"_Boot";boot.hideFlags=HideFlags.HideAndDontSave;boots[key]=boot;
                }
                templates[d]=boot;
            }
        }
        SpriteRenderer FootRenderer(string name)
        {
            var renderer=new GameObject(name).AddComponent<SpriteRenderer>();renderer.transform.SetParent(body.transform,false);renderer.sharedMaterial=body.sharedMaterial;return renderer;
        }
        static Rect[] BodyRects(Sprite source,int top,int left,int right)=>new[]{new Rect(0,top,source.rect.width,source.rect.height-top),new Rect(0,0,left,top),new Rect(right,0,source.rect.width-right,top)};
        static Rect[] FrontBodyRects(Sprite source,int top,int left,int right)=>new[]{new Rect(0,top+10,source.rect.width,source.rect.height-top-10),new Rect(0,0,left,top+10),new Rect(right,0,source.rect.width-right,top+10),new Rect(left,top,right-left,10)};
        static Sprite[] BodyWithoutFixedLegs(Sprite source,Rect[] rects,Sprite rest)
        {
            if(bodies.TryGetValue(source,out var found)&&found[0]!=null)return found;
            var sprites=new Sprite[rects.Length];
            for(int i=0;i<rects.Length;i++)
            {
                // A few source frames raise a fixed knee into the hem. Keep the
                // resting hip/cloth strip so it cannot appear as a third knee.
                var art=i==3?rest:source;
                var rect=rects[i];rect.position+=art.rect.position;
                sprites[i]=Sprite.Create(art.texture,rect,new Vector2(.5f,0),art.pixelsPerUnit);
                sprites[i].name=source.name+"_Body"+i;sprites[i].hideFlags=HideFlags.HideAndDontSave;
            }
            bodies[source]=sprites;return sprites;
        }
        public void Present(Sprite source,int direction,float phase,bool walking)
        {
            bool split=!frontOnly||direction==1;
            foreach(var part in bodyParts)part.gameObject.SetActive(split);
            leftFoot.gameObject.SetActive(split);rightFoot.gameObject.SetActive(split);
            if(!split){body.enabled=true;return;}
            body.enabled=false;
            var rects=bodyRects[direction];
            var sprites=BodyWithoutFixedLegs(source,rects,frontRest);
            for(int i=0;i<bodyParts.Length;i++)
            {
                var part=bodyParts[i];part.sprite=sprites[i];
                part.transform.localPosition=(new Vector3(rects[i].center.x,rects[i].y,0)-(Vector3)source.pivot)/source.pixelsPerUnit;
                part.color=body.color;part.sortingLayerID=body.sortingLayerID;part.sortingOrder=body.sortingOrder+1;
            }
            leftFoot.sprite=rightFoot.sprite=templates[direction];
            float Lift(float p)=>walking?Mathf.Max(0,Mathf.Sin(p*Mathf.PI*2))*3.5f:0;
            leftFoot.transform.localPosition=new Vector3(leftX,Lift(phase),0)/source.pixelsPerUnit;
            rightFoot.transform.localPosition=new Vector3(rightX,Lift(phase+.5f),0)/source.pixelsPerUnit;
            CopyPresentation(leftFoot);CopyPresentation(rightFoot);
        }
        void CopyPresentation(SpriteRenderer foot){foot.color=body.color;foot.sortingLayerID=body.sortingLayerID;foot.sortingOrder=body.sortingOrder;}
    }
}
