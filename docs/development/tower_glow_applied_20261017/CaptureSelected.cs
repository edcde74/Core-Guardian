using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CaptureSelected
{
 public static object Main()
 {
  if(EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating)throw new Exception("Stopped idle editor required");
  var game=UnityEngine.Object.FindFirstObjectByType<CoreGuardianPrototype>();var original=game.gameObject.scene;
  if(original.isDirty)throw new Exception("Clean source scene required");
  const string dir="docs/development/tower_glow_applied_20261017/frames/";Directory.CreateDirectory(dir);
  var source=Camera.main;float density=source.pixelHeight/(2*source.orthographicSize);
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
  var rt=new RenderTexture(720,256,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();var previous=RenderTexture.active;
  Texture2D tex=null;
  try
  {
   var cameraRoot=new GameObject("Temporary selected effects capture");SceneManager.MoveGameObjectToScene(cameraRoot,scene);
   var camera=cameraRoot.AddComponent<Camera>();camera.CopyFrom(source);camera.enabled=false;
   camera.transform.SetPositionAndRotation(new Vector3(16.5f,12.7f,source.transform.position.z),source.transform.rotation);
   camera.orthographicSize=128/density;camera.targetTexture=rt;camera.allowHDR=false;camera.allowMSAA=false;
   camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(original)|EditorSceneManager.GetSceneCullingMask(scene);
   var data=camera.GetUniversalAdditionalCameraData();data.SetRenderer(-1);data.renderPostProcessing=false;data.antialiasing=AntialiasingMode.None;
   var units=new CoreGuardianUnitAnimation[3];
   for(int i=0;i<3;i++)
   {
    var root=UnityEngine.Object.Instantiate(game.towerArt[i]);SceneManager.MoveGameObjectToScene(root,scene);
    root.transform.position=new Vector3(16.5f+(i-1)*240/density,12.2f,0);
    foreach(var depth in root.GetComponentsInChildren<ArtDepthSorting>(true))depth.enabled=false;
    root.GetComponentsInChildren<SpriteRenderer>(true).Single(r=>r.name=="2D Sprite").sortingOrder=900;
    units[i]=CoreGuardianUnitAnimation.Attach(root,new[]{"Single","Multi","Support"}[i]);
   }
   for(int frame=0;frame<48;frame++)
   {
    foreach(var unit in units){if(frame==10||frame==34)unit.Fire(Vector2.up,1);unit.Present(.05f);}
    camera.Render();if(frame==0){camera.Render();camera.Render();}RenderTexture.active=rt;
    tex=new Texture2D(720,256,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,720,256),0,0);tex.Apply();
    File.WriteAllBytes(dir+frame.ToString("D3")+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);tex=null;
   }
  }
  finally
  {
   RenderTexture.active=previous;if(tex!=null)UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
   EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(original);
  }
  return new{frames=48,fps=20,density,dirty=original.isDirty,scene=original.path,play=EditorApplication.isPlaying,HDR=source.allowHDR,post=source.GetUniversalAdditionalCameraData().renderPostProcessing,note="Applied production animation/shader on original prefabs, manual Fire clock, current scene background; not normal combat or Overlay HUD"};
 }
}
