using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class CaptureShadowAxes
{
 const string Folder="docs/development/monster_shadow_axes_fix_20261016/";
 static GameObject Prefab(int role)=>role==3?Resources.Load<GameObject>("CoreGuardian/Animation/Boss/BossAnimated"):AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/ScreenTests/20261008_Depth/Prefabs/"+new[]{"enemy_attack","enemy_defense","enemy_buff"}[role]+".prefab");
 static void Camera(PreviewRenderUtility preview,float size,float y)
 {
  preview.camera.orthographic=true;preview.camera.orthographicSize=size;preview.camera.transform.position=new Vector3(0,y,-10);preview.camera.transform.rotation=Quaternion.identity;preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color32(35,40,46,255);preview.camera.allowHDR=false;preview.camera.allowMSAA=false;
 }
 static void Save(PreviewRenderUtility preview,int width,int height,string path)
 {
  preview.BeginStaticPreview(new Rect(0,0,width,height));preview.Render(true,true);var image=preview.EndStaticPreview();File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
 }
 public static object Main()
 {
  Directory.CreateDirectory(Folder+"walk_frames");Directory.CreateDirectory(Folder+"idle_frames");
  var preview=new PreviewRenderUtility();
  try
  {
   Camera(preview,4.2f,0);string[] roles={"Attack","Defense","Buff","Boss"};var animations=new CoreGuardianUnitAnimation[4,4];
   for(int role=0;role<4;role++)for(int col=0;col<5;col++)
   {
    var clone=UnityEngine.Object.Instantiate(Prefab(role));preview.AddSingleGO(clone);clone.transform.position=new Vector3(-3.6f+col*1.8f,2.4f-role*2.1f,0);
    if(col>0)animations[role,col-1]=CoreGuardianUnitAnimation.Attach(clone,roles[role]);
   }
   var directions=new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down};float[] cycle={14,10,12,18};
   for(int role=0;role<4;role++)for(int direction=0;direction<4;direction++)
   {animations[role,direction].Move(directions[direction]*(cycle[role]*.6f/32),1);animations[role,direction].Present(1);}
   for(int frame=0;frame<48;frame++)
   {
    for(int role=0;role<4;role++)for(int direction=0;direction<4;direction++)
    {
     var animation=animations[role,direction];animation.Move(directions[direction]*(cycle[role]*.6f/32),1/48f);animation.Present(1/48f);
    }
    Save(preview,1440,1344,Folder+"walk_frames/"+frame.ToString("000")+".png");
   }
  }
  finally{preview.Cleanup();}
  preview=new PreviewRenderUtility();
  try
  {
   Camera(preview,.5625f,.45f);CoreGuardianUnitAnimation idle=null;
   for(int col=0;col<2;col++)
   {
    var clone=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/ScreenTests/20261008_Depth/Prefabs/Player.prefab"));preview.AddSingleGO(clone);clone.transform.position=new Vector3(col==0?-.55f:.55f,0,0);if(col==1)idle=CoreGuardianUnitAnimation.Attach(clone,"Player");
   }
   for(int frame=0;frame<70;frame++){idle.Present(.04f);Save(preview,720,360,Folder+"idle_frames/"+frame.ToString("000")+".png");}
  }
  finally{preview.Cleanup();}
  return "Actual Unity captures: walk rows Attack/Defense/Buff/Boss, columns source/right/left/up/down at native3x; player source/Idle at native6x";
 }
}
