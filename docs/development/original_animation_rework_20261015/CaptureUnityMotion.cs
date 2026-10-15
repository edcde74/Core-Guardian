using System;
using System.IO;
using UnityEngine;
using UnityEditor;
public static class CaptureUnityMotion
{
 public static object Main()
 {
  var preview=new PreviewRenderUtility();
  try
  {
   preview.camera.orthographic=true;preview.camera.orthographicSize=2.25f;
   preview.camera.transform.position=new Vector3(0,.25f,-10);preview.camera.transform.rotation=Quaternion.identity;
   preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color32(35,40,46,255);
   preview.camera.allowHDR=false;preview.camera.allowMSAA=false;
   string[] files={"Player","tower_single","tower_multi","tower_support"};string[] roles={"Player","Single","Multi","Support"};
   var animations=new System.Collections.Generic.List<CoreGuardianUnitAnimation>();
   for(int i=0;i<4;i++)for(int side=0;side<2;side++)
   {
    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/ScreenTests/20261008_Depth/Prefabs/"+files[i]+".prefab");
    var clone=UnityEngine.Object.Instantiate(prefab);preview.AddSingleGO(clone);
    clone.transform.position=new Vector3((i%2)*3.6f-2.6f+side*1.4f,1.05f-(i/2)*2.15f,0);
    if(side==1){var animation=CoreGuardianUnitAnimation.Attach(clone,roles[i]);animations.Add(animation);animation.Fire(Vector2.left,2);animation.Present(0);animation.Present(.05f);}
   }
   preview.BeginStaticPreview(new Rect(0,0,1040,720));preview.Render(true,true);var image=preview.EndStaticPreview();
   File.WriteAllBytes("docs/development/original_animation_rework_20261015/unity_attack.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
   string folder="docs/development/original_animation_rework_20261015/unity_frames/";Directory.CreateDirectory(folder);
   for(int frame=0;frame<40;frame++)
   {
    float age=frame*.04f-.6f;
    foreach(var animation in animations)
    {
     if(age>=0){animation.Fire(Vector2.left,2);animation.Present(0);animation.Present(age);}
     else animation.Present(1);
    }
    preview.BeginStaticPreview(new Rect(0,0,1040,720));preview.Render(true,true);var rendered=preview.EndStaticPreview();
    File.WriteAllBytes(folder+frame.ToString("000")+".png",rendered.EncodeToPNG());UnityEngine.Object.DestroyImmediate(rendered);
   }
  }
  finally{preview.Cleanup();}
  return "Actual Unity SpriteRenderer capture; left original/right source cutout attack; player,single,multi,support; active scene untouched";
 }
}
