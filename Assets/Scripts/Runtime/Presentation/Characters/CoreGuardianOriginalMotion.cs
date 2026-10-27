using UnityEngine;

// Original-texture cutouts. Only the single barrel is mirrored/shortened by its aiming rig.
public sealed class CoreGuardianOriginalMotion
{
    readonly SpriteRenderer original, fixedPart, movingPart;
    readonly SpriteRenderer[] housing;
    readonly System.Collections.Generic.List<Sprite> ownedSprites=new System.Collections.Generic.List<Sprite>();
    readonly Transform rig;
    readonly Transform singleProjection,singleYaw;
    readonly string role;
    readonly float ppu;
    readonly Vector2 sourcePivot;
    // Original 52x64 pixels: barrel collar pivot (26,17 from top), muzzle (3,4).
    readonly Vector2 singleMount=new Vector2(26,47),barrelCenter=new Vector2(13.5f,53.5f);
    // New towers (냉기·화염·레일) ship finished attack frames; they replace the whole sprite during a shot.
    readonly Sprite[] attackFrames;readonly Sprite restSprite;const float AttackFrameSeconds=.075f;
    // 2026-10-27 사용자 피드백 1·2·3: attack frames are drawn on a different canvas than the idle sprite.
    // Offset = frame pixel minus idle pixel for the same body point (pixel-matched, see EditMode test).
    // Frost keeps whole frames re-pivoted; Flame·Rail cut each frame into body + barrel and turn the barrel like Single.
    sealed class TurretLayout {public Vector2Int Offset;public Rect Barrel;public Rect[] Body;public Vector2 Mount,Tip;}
    static TurretLayout Layout(string role)
    {
        switch(role)
        {
            case "Frost":return new TurretLayout{Offset=new Vector2Int(0,0)}; // 2026-10-27 A안: 52x68 프레임(위 4px 반짝임 여유). 파동 고리는 사거리에 맞춘 바닥 이펙트가 담당
            // 2026-10-27 A안(화로 정돈): 프레임은 대기 그림과 같은 52x64. 노즐 팔+불씨 구슬(x<22)만 돌고 화로·불꽃은 고정.
            case "Flame":return new TurretLayout{Offset=new Vector2Int(0,0),Barrel=new Rect(0,45,22,19),Body=new[]{new Rect(0,0,52,45),new Rect(22,45,30,19)},Mount=new Vector2(22,51),Tip=new Vector2(10,55.5f)};
            case "Rail":return new TurretLayout{Offset=new Vector2Int(3,1),Barrel=new Rect(0,43,27,21),Body=new[]{new Rect(0,0,52,43),new Rect(27,43,25,21)},Mount=new Vector2(26,47),Tip=new Vector2(3,60)};
            default:return null;
        }
    }
    internal static Vector2Int AttackFrameOffset(string role)=>Layout(role)?.Offset??Vector2Int.zero;
    sealed class TurretCut {public Sprite Barrel;public Vector2 BarrelCenter;public Sprite[] Body;public Vector2[] BodyCenter;}
    readonly TurretLayout layout;
    readonly Sprite[] alignedFrames;
    readonly TurretCut turretRest;readonly TurretCut[] turretFrames;
    readonly SpriteRenderer turretBarrel;readonly SpriteRenderer[] turretBody;
    readonly Transform turretMount,turretYaw;
    TurretCut turretShown;
    bool Turret=>turretYaw!=null;
    public Vector3 AimOriginWorld=>role=="Single"?singleProjection.position:Turret?turretMount.position:original.transform.position;
    public CoreGuardianOriginalMotion(SpriteRenderer artwork,string role)
    {
        original=artwork;this.role=role;ppu=artwork.sprite.pixelsPerUnit;sourcePivot=artwork.sprite.pivot;
        var stationary=Resources.Load<Sprite>("CoreGuardian/OriginalMotion/"+role+"/Fixed");
        var moving=Resources.Load<Sprite>("CoreGuardian/OriginalMotion/"+role+"/Moving");
        if(stationary==null||moving==null)throw new System.InvalidOperationException("Missing original cutout layers: "+role);
        restSprite=artwork.sprite;attackFrames=LoadFrames(role);
        rig=new GameObject("OriginalMotion").transform;rig.SetParent(artwork.transform,false);
        fixedPart=Layer("Fixed",stationary);
        if(role=="Single")
        {
            // Partition existing texture rectangles; never repaint or replace source texture pixels.
            movingPart=Layer("Moving",Cut(moving,new Rect(0,43,27,21),"SingleBarrel"));
            housing=new[]{Layer("HousingRight",Cut(moving,new Rect(27,0,25,64),"SingleHousingRight")),Layer("HousingLeft",Cut(moving,new Rect(0,0,27,43),"SingleHousingLeft"))};
            housing[0].transform.localPosition=(new Vector2(39.5f,32)-sourcePivot)/ppu;
            housing[1].transform.localPosition=(new Vector2(13.5f,21.5f)-sourcePivot)/ppu;
            singleProjection=new GameObject("SingleMount").transform;singleProjection.SetParent(rig,false);
            singleProjection.localPosition=(singleMount-sourcePivot)/ppu;singleProjection.localScale=Vector3.one;
            singleYaw=new GameObject("SingleYaw").transform;singleYaw.SetParent(singleProjection,false);
            movingPart.transform.SetParent(singleYaw,false);
        }
        else movingPart=Layer("Moving",moving);
        layout=attackFrames.Length>0?Layout(role):null;
        if(layout!=null&&layout.Body==null)
        {
            alignedFrames=new Sprite[attackFrames.Length];
            for(int i=0;i<attackFrames.Length;i++)alignedFrames[i]=Realign(attackFrames[i],layout.Offset);
        }
        else if(layout!=null)
        {
            fixedPart.gameObject.SetActive(false);movingPart.gameObject.SetActive(false);
            turretRest=CutTurret(restSprite,Vector2Int.zero,role+"Rest");
            turretFrames=new TurretCut[attackFrames.Length];
            for(int i=0;i<attackFrames.Length;i++)turretFrames[i]=CutTurret(attackFrames[i],layout.Offset,role+"Attack"+i);
            turretBody=new SpriteRenderer[layout.Body.Length];
            for(int i=0;i<turretBody.Length;i++)turretBody[i]=Layer("TurretBody"+i,turretRest.Body[i]);
            turretMount=new GameObject("TurretMount").transform;turretMount.SetParent(rig,false);
            turretMount.localPosition=(layout.Mount-sourcePivot)/ppu;
            turretYaw=new GameObject("TurretYaw").transform;turretYaw.SetParent(turretMount,false);
            turretBarrel=Layer("TurretBarrel",turretRest.Barrel);turretBarrel.transform.SetParent(turretYaw,false);
        }
        rig.gameObject.SetActive(false);
    }
    // Same frame, pivot moved to the frame pixel that matches the idle sprite pivot.
    Sprite Realign(Sprite frame,Vector2Int offset)
    {
        var size=frame.rect.size;var pivot=new Vector2((sourcePivot.x+offset.x)/size.x,(sourcePivot.y+offset.y)/size.y);
        var sprite=CoreGuardianSpriteAtlasSources.Cut(frame,new Rect(Vector2.zero,size),pivot,ppu);
        sprite.name=frame.name+"_Aligned";ownedSprites.Add(sprite);return sprite;
    }
    // Idle-space rectangles, shifted into the frame and clipped to its canvas.
    TurretCut CutTurret(Sprite frame,Vector2Int offset,string name)
    {
        var cut=new TurretCut{Body=new Sprite[layout.Body.Length],BodyCenter=new Vector2[layout.Body.Length]};
        cut.Barrel=CutPart(frame,layout.Barrel,offset,name+"Barrel",out cut.BarrelCenter);
        for(int i=0;i<layout.Body.Length;i++)cut.Body[i]=CutPart(frame,layout.Body[i],offset,name+"Body"+i,out cut.BodyCenter[i]);
        return cut;
    }
    Sprite CutPart(Sprite frame,Rect idleRect,Vector2Int offset,string name,out Vector2 idleCenter)
    {
        var size=frame.rect.size;
        float x0=Mathf.Max(0,idleRect.xMin+offset.x),y0=Mathf.Max(0,idleRect.yMin+offset.y);
        float x1=Mathf.Min(size.x,idleRect.xMax+offset.x),y1=Mathf.Min(size.y,idleRect.yMax+offset.y);
        var local=Rect.MinMaxRect(x0,y0,x1,y1);
        idleCenter=local.center-(Vector2)offset;
        var sprite=Cut(frame,local,name);return sprite;
    }
    static Sprite[] LoadFrames(string role)
    {
        var list=new System.Collections.Generic.List<Sprite>();
        foreach(var prefix in new[]{"Fire_","Pulse_"})
            for(int i=0;i<16;i++){var s=Resources.Load<Sprite>("CoreGuardian/OriginalMotion/"+role+"/"+prefix+i);if(s==null)break;list.Add(s);}
        return list.ToArray();
    }
    Sprite Cut(Sprite source,Rect part,string name)
    {
        var sprite=CoreGuardianSpriteAtlasSources.Cut(source,part,Vector2.one*.5f,ppu);
        sprite.name=name;ownedSprites.Add(sprite);return sprite;
    }
    SpriteRenderer Layer(string name,Sprite sprite)
    {
        var renderer=new GameObject(name).AddComponent<SpriteRenderer>();renderer.transform.SetParent(rig,false);
        renderer.sprite=sprite;renderer.sharedMaterial=original.sharedMaterial;
        return renderer;
    }
    public void Present(float age,float duration,Vector2 aim)
    {
        if(attackFrames.Length>0)
        {
            int index=(int)(age/AttackFrameSeconds);bool attacking=index>=0&&index<attackFrames.Length;
            if(Turret){PresentTurret(attacking?turretFrames[index]:turretRest,age,duration,aim);return;}
            rig.gameObject.SetActive(false);original.enabled=true;
            var frame=attacking?(alignedFrames!=null?alignedFrames[index]:attackFrames[index]):restSprite;
            if(original.sprite!=frame)original.sprite=frame;
            return;
        }
        bool active=age<duration;
        rig.gameObject.SetActive(active||role=="Single");original.enabled=!(active||role=="Single");
        if(!active&&role!="Single")return;
        fixedPart.color=movingPart.color=original.color;
        fixedPart.sortingLayerID=movingPart.sortingLayerID=original.sortingLayerID;
        fixedPart.sortingOrder=original.sortingOrder;movingPart.sortingOrder=original.sortingOrder+1;
        // Single retains its last aimed barrel; other roles recover to the original whole sprite.
        float t=Mathf.Clamp01(age/duration);
        float seconds=t*.48f;
        float kick=active?Mathf.Sin(Mathf.Min(1,seconds/.09f)*Mathf.PI*.5f)*Mathf.Exp(-Mathf.Max(0,seconds-.09f)*11):0;
        var part=movingPart.transform;part.localRotation=Quaternion.identity;part.localPosition=Vector3.zero;
        if(role=="Player")
        {
            var pivot=(new Vector2(10,original.sprite.rect.height-23)-sourcePivot)/ppu;
            var rotation=Quaternion.Euler(0,0,-.24f*Mathf.Rad2Deg*kick);
            part.localRotation=rotation;part.localPosition=(Vector3)pivot-rotation*(Vector3)pivot;
        }
        else if(role=="Single")
        {
            Vector2 localAim=original.transform.InverseTransformVector(aim);
            if(localAim.sqrMagnitude<.000001f)localAim=new Vector2(-25,18);
            localAim.Normalize();
            float mirror=localAim.x>0?-1:1;
            var native=new Vector2((3-singleMount.x)*mirror,(60-singleMount.y));
            var projectedAim=new Vector2(localAim.x,localAim.y);
            singleYaw.localRotation=Quaternion.Euler(0,0,Vector2.SignedAngle(native,projectedAim));
            float foreshorten=Mathf.Lerp(1,.72f,Mathf.Abs(localAim.y));part.localScale=new Vector3(mirror*foreshorten,foreshorten,1);
            part.localPosition=new Vector3((barrelCenter.x-singleMount.x)*mirror,(barrelCenter.y-singleMount.y))/ppu*foreshorten;
            part.localPosition-=(Vector3)native.normalized*(1.72f*kick/ppu);
            movingPart.sortingOrder=original.sortingOrder+(localAim.y<=0?2:0);
            foreach(var h in housing){h.color=original.color;h.sortingLayerID=original.sortingLayerID;h.sortingOrder=original.sortingOrder+1;}
        }
        else if(role=="Multi")part.localPosition=Vector3.down*(1.4f*kick/ppu);
        else part.localPosition=Vector3.up*(1.4f*Mathf.Sin(t*Mathf.PI)/ppu);
    }
    void PresentTurret(TurretCut cut,float age,float duration,Vector2 aim)
    {
        rig.gameObject.SetActive(true);original.enabled=false;turretShown=cut;
        float t=Mathf.Clamp01(age/duration),seconds=t*.48f;bool active=age<duration;
        float kick=active?Mathf.Sin(Mathf.Min(1,seconds/.09f)*Mathf.PI*.5f)*Mathf.Exp(-Mathf.Max(0,seconds-.09f)*11):0;
        for(int i=0;i<turretBody.Length;i++)
        {
            var body=turretBody[i];
            if(body.sprite!=cut.Body[i])body.sprite=cut.Body[i];
            body.transform.localPosition=(cut.BodyCenter[i]-sourcePivot)/ppu;
            body.color=original.color;body.sortingLayerID=original.sortingLayerID;body.sortingOrder=original.sortingOrder+1;
        }
        Vector2 localAim=original.transform.InverseTransformVector(aim);
        if(localAim.sqrMagnitude<.000001f)localAim=layout.Tip-layout.Mount;
        localAim.Normalize();
        float mirror=localAim.x>0?-1:1;
        var native=new Vector2((layout.Tip.x-layout.Mount.x)*mirror,layout.Tip.y-layout.Mount.y);
        turretYaw.localRotation=Quaternion.Euler(0,0,Vector2.SignedAngle(native,localAim));
        float foreshorten=Mathf.Lerp(1,.72f,Mathf.Abs(localAim.y));
        var barrel=turretBarrel.transform;
        if(turretBarrel.sprite!=cut.Barrel)turretBarrel.sprite=cut.Barrel;
        barrel.localScale=new Vector3(mirror*foreshorten,foreshorten,1);
        barrel.localPosition=new Vector3((cut.BarrelCenter.x-layout.Mount.x)*mirror,cut.BarrelCenter.y-layout.Mount.y)/ppu*foreshorten;
        barrel.localPosition-=(Vector3)native.normalized*(1.72f*kick/ppu);
        turretBarrel.color=original.color;turretBarrel.sortingLayerID=original.sortingLayerID;
        turretBarrel.sortingOrder=original.sortingOrder+(localAim.y<=0?2:0);
    }
    public Vector3 MuzzleWorld(Vector2 aim)
    {
        if(Turret)
        {
            var center=(turretShown??turretRest).BarrelCenter;
            return turretBarrel.transform.TransformPoint((layout.Tip-center)/ppu);
        }
        Vector2 point;
        if(role=="Player")point=new Vector2(2,original.sprite.rect.height-29);
        else if(role=="Single")point=new Vector2(3,original.sprite.rect.height-4);
        else if(role=="Multi")point=aim.y>Mathf.Abs(aim.x)?new Vector2(21,original.sprite.rect.height-3):aim.x>0?new Vector2(47,original.sprite.rect.height-13):new Vector2(4,original.sprite.rect.height-12);
        else point=new Vector2(20,original.sprite.rect.height-16);
        return movingPart.transform.TransformPoint((point-(role=="Single"?barrelCenter:sourcePivot))/ppu);
    }
    public void Dispose()
    {
        foreach(var sprite in ownedSprites)if(sprite!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(sprite);else UnityEngine.Object.DestroyImmediate(sprite);}
        ownedSprites.Clear();
    }
}
