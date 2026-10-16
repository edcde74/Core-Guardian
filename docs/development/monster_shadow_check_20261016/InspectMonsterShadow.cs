using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class InspectMonsterShadow
{
 const string Folder="docs/development/monster_shadow_check_20261016/";
 static Vector2 Sole(Sprite sprite)
 {
  var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(texture,File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite.texture)));
  int bottom=texture.height,minX=texture.width,maxX=-1;
  for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(texture.GetPixel(x,y).a>.5f)bottom=Math.Min(bottom,y);
  for(int y=bottom;y<Math.Min(bottom+2,texture.height);y++)for(int x=0;x<texture.width;x++)if(texture.GetPixel(x,y).a>.5f){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);}
  UnityEngine.Object.DestroyImmediate(texture);return (new Vector2((minX+maxX+1)*.5f,bottom)-sprite.pivot)/sprite.pixelsPerUnit;
 }
 static Vector3 WorldSole(SpriteRenderer source,SpriteRenderer actual,Sprite foot)
 {
  var point=Sole(foot);if(actual==source&&source.flipX)point.x=-point.x;return actual.transform.TransformPoint(point);
 }
 static object Vector(Vector3 v)=>new[]{v.x,v.y,v.z};
 public static object Main()
 {
  Directory.CreateDirectory(Folder);var preview=new PreviewRenderUtility();var results=new System.Collections.Generic.List<object>();
  try
  {
   preview.camera.orthographic=true;preview.camera.orthographicSize=4.2f;preview.camera.transform.position=new Vector3(0,0,-10);preview.camera.transform.rotation=Quaternion.identity;
   preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color32(35,40,46,255);preview.camera.allowHDR=false;preview.camera.allowMSAA=false;
   string[] roles={"Attack","Defense","Buff","Boss"};string[] names={"enemy_attack","enemy_defense","enemy_buff"};float[] cycles={14,10,12,18};
   for(int row=0;row<4;row++)
   {
    var prefab=row==3?Resources.Load<GameObject>("CoreGuardian/Animation/Boss/BossAnimated"):AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/ScreenTests/20261008_Depth/Prefabs/"+names[row]+".prefab");
    var roleResults=new System.Collections.Generic.List<object>();
    for(int col=0;col<3;col++)
    {
     var clone=UnityEngine.Object.Instantiate(prefab);preview.AddSingleGO(clone);clone.transform.position=new Vector3(-1.8f+col*1.8f,2.4f-row*2.1f,0);
     var art=clone.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");
     var renderers=clone.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r!=art).Select(r=>new{r.name,sprite=r.sprite?.name,position=Vector(r.transform.position-clone.transform.position),localPosition=Vector(r.transform.localPosition),scale=Vector(r.transform.lossyScale),center=Vector(r.bounds.center-clone.transform.position),r.enabled}).ToArray();
     var shadows=clone.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.sprite!=null&&r.sprite.name.Contains("Shadow")).ToArray();
     var shadow=shadows.OrderByDescending(r=>r.bounds.size.x).First();
     var leftSprite=Resources.Load<Sprite>("CoreGuardian/OriginalWalk/"+roles[row]+"/LeftLower");var rightSprite=Resources.Load<Sprite>("CoreGuardian/OriginalWalk/"+roles[row]+"/RightLower");
     var restCenter=(WorldSole(art,art,leftSprite)+WorldSole(art,art,rightSprite))*.5f;
     var animation=CoreGuardianUnitAnimation.Attach(clone,roles[row]);
     Vector2 velocity=col==0?Vector2.zero:(col==1?Vector2.left:Vector2.right)*(cycles[row]*.6f/32);
     var samples=new System.Collections.Generic.List<Vector3>();
     for(int frame=0;frame<48;frame++)
     {
      animation.Move(velocity,1/48f);animation.Present(1/48f);
      var rig=art.transform.Find("OriginalWalk");var left=rig.Find("LeftLower").GetComponent<SpriteRenderer>();var right=rig.Find("RightLower").GetComponent<SpriteRenderer>();
      var feet=col==0?(WorldSole(art,art,leftSprite)+WorldSole(art,art,rightSprite))*.5f:(WorldSole(art,left,leftSprite)+WorldSole(art,right,rightSprite))*.5f;
      samples.Add(feet-shadow.bounds.center);
     }
     var mean=new Vector3(samples.Average(v=>v.x),samples.Average(v=>v.y),0);
     float sourcePixelWorld=art.transform.lossyScale.x/art.sprite.pixelsPerUnit;
     float[] stoppedPixels=null;
     if(col==2)
     {
      animation.Move(Vector2.zero,.3f);animation.Present(.3f);
      var stopped=(WorldSole(art,art,leftSprite)+WorldSole(art,art,rightSprite))*.5f-shadow.bounds.center;
      stoppedPixels=new[]{stopped.x/sourcePixelWorld,stopped.y/sourcePixelWorld};
      animation.Move(velocity,1);animation.Present(1);
     }
     roleResults.Add(new{facing=col==0?"rest":col==1?"left":"right",source=art.sprite.name,pivot=new[]{art.sprite.pivot.x,art.sprite.pivot.y},bodyPosition=Vector(art.transform.position-clone.transform.position),restFeetCenter=Vector(restCenter-clone.transform.position),shadowCenter=Vector(shadow.bounds.center-clone.transform.position),meanFeetMinusShadow=Vector(mean),meanSourcePixels=new[]{mean.x/sourcePixelWorld,mean.y/sourcePixelWorld},stoppedSourcePixels=stoppedPixels,minXSourcePixels=samples.Min(v=>v.x)/sourcePixelWorld,maxXSourcePixels=samples.Max(v=>v.x)/sourcePixelWorld,renderers});
    }
    results.Add(new{role=roles[row],states=roleResults});
   }
   preview.BeginStaticPreview(new Rect(0,0,864,1344));preview.Render(true,true);var capture=preview.EndStaticPreview();File.WriteAllBytes(Folder+"rest_left_right.png",capture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(capture);
  }
  finally{preview.Cleanup();}
  string json=Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented);File.WriteAllText(Folder+"measurements.json",json);return json;
 }
}
