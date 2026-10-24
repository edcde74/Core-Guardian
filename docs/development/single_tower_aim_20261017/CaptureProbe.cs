using UnityEngine;

// Cutout animation of the approved PNG. No replacement drawing, recolouring or scale changes.
public sealed class OriginalMotionProbe
{
    readonly SpriteRenderer original, fixedPart, movingPart;
    readonly SpriteRenderer[] housing;
    readonly System.Collections.Generic.List<Sprite> ownedSprites=new System.Collections.Generic.List<Sprite>();
    readonly Transform rig;
    readonly Transform singleProjection,singleYaw;
    readonly string role;
    readonly float ppu;
    readonly Vector2 sourcePivot;
    // Original 52x64 single-tower pixels: mount at (28,22 from top), muzzle at (3,4 from top).
    readonly Vector2 singleMount=new Vector2(26,47),barrelCenter=new Vector2(13.5f,53.5f);
    public OriginalMotionProbe(SpriteRenderer artwork,string role)
    {
        original=artwork;this.role=role;ppu=artwork.sprite.pixelsPerUnit;sourcePivot=artwork.sprite.pivot;
        var stationary=Resources.Load<Sprite>("CoreGuardian/OriginalMotion/"+role+"/Fixed");
        var moving=Resources.Load<Sprite>("CoreGuardian/OriginalMotion/"+role+"/Moving");
        if(stationary==null||moving==null)throw new System.InvalidOperationException("Missing original cutout layers: "+role);
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
        rig.gameObject.SetActive(false);
    }
    Sprite Cut(Sprite source,Rect part,string name)
    {
        var rect=source.textureRect;part.position+=rect.position;
        var sprite=Sprite.Create(source.texture,part,Vector2.one*.5f,ppu,0,SpriteMeshType.FullRect);
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
        bool active=age<duration;
        rig.gameObject.SetActive(active||role=="Single");original.enabled=!(active||role=="Single");
        if(!active&&role!="Single")return;
        fixedPart.color=movingPart.color=original.color;
        fixedPart.sortingLayerID=movingPart.sortingLayerID=original.sortingLayerID;
        fixedPart.sortingOrder=original.sortingOrder;movingPart.sortingOrder=original.sortingOrder+1;
        // The actual launch begins the movement; recovery always ends at the untouched source sprite.
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
    public Vector3 MuzzleWorld(Vector2 aim)
    {
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

public static class CaptureProbe
{
 public static object Main()
 {
  var preview=new UnityEditor.PreviewRenderUtility();var motions=new System.Collections.Generic.List<OriginalMotionProbe>();var glows=new System.Collections.Generic.List<CoreGuardianTowerGlow>();
  try
  {
   preview.camera.orthographic=true;preview.camera.orthographicSize=3.2f;preview.camera.transform.position=new Vector3(0,0,-10);preview.camera.transform.rotation=Quaternion.identity;preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color32(35,40,46,255);preview.camera.allowHDR=false;preview.camera.allowMSAA=false;
   var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/ScreenTests/20261008_Depth/Prefabs/tower_single.prefab");
   var dirs=new[]{Vector2.left,Vector2.up,Vector2.right,Vector2.down,new Vector2(-1,1),new Vector2(1,1),new Vector2(1,-1),new Vector2(-1,-1)};
   for(int i=0;i<8;i++)
   {
    var root=UnityEngine.Object.Instantiate(prefab);preview.AddSingleGO(root);root.transform.position=new Vector3(-3.6f+(i%4)*2.4f,i<4?.25f:-2.65f,0);
    var body=System.Linq.Enumerable.Single(root.GetComponentsInChildren<SpriteRenderer>(),r=>r.name=="2D Sprite");var motion=new OriginalMotionProbe(body,"Single");motions.Add(motion);motion.Present(100,.28f,dirs[i].normalized);var glow=new CoreGuardianTowerGlow(body,"Single");glows.Add(glow);glow.Present(100,.28f,0);
   }
            preview.BeginStaticPreview(new Rect(0,0,2048,1280));preview.Render(true,true);var tex=preview.EndStaticPreview();try{System.IO.File.WriteAllBytes("docs/development/single_tower_aim_20261017/eight_directions_refined.png",tex.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(tex);}
  }
  finally{foreach(var g in glows)g.Dispose();foreach(var m in motions)m.Dispose();preview.Cleanup();}
  return "Probe only: isolate pipe pixels, keep original collar fixed; 200px/unit; existing Play untouched";
 }
}
