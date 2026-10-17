using System;
using System.IO;
using UnityEngine;
using UnityEditor;
public static class CaptureAim
{
 public static object Main()
 {
  if(EditorApplication.isCompiling)throw new Exception("Compilation in progress");
  var preview=new PreviewRenderUtility();
  try
  {
   preview.camera.orthographic=true;preview.camera.orthographicSize=3.2f;
   preview.camera.transform.position=new Vector3(0,0,-10);preview.camera.transform.rotation=Quaternion.identity;
   preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color32(35,40,46,255);
   preview.camera.allowHDR=false;preview.camera.allowMSAA=false;
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/ScreenTests/20261008_Depth/Prefabs/tower_single.prefab");
   var directions=new[]{Vector2.left,Vector2.up,Vector2.right,Vector2.down,new Vector2(-1,1),new Vector2(1,1),new Vector2(1,-1),new Vector2(-1,-1)};
   for(int i=0;i<8;i++)
   {
    var root=UnityEngine.Object.Instantiate(prefab);preview.AddSingleGO(root);
    root.transform.position=new Vector3(-3.6f+(i%4)*2.4f,i<4?.25f:-2.65f,0);
    var unit=CoreGuardianUnitAnimation.Attach(root,"Single");unit.Aim(directions[i],1);unit.Present(0);
   }
   preview.BeginStaticPreview(new Rect(0,0,1024,640));preview.Render(true,true);var tex=preview.EndStaticPreview();
   try{File.WriteAllBytes("docs/development/single_tower_aim_20261017/eight_directions_after.png",tex.EncodeToPNG());}
   finally{UnityEngine.Object.DestroyImmediate(tex);}
  }
  finally{preview.Cleanup();}
  return new{source="Isolated PreviewRenderUtility; existing Play/camera/time untouched; no normal combat/HUD",rows="left/up/right/down; NW/NE/SE/SW",pixelsPerWorldUnit=100};
 }
}