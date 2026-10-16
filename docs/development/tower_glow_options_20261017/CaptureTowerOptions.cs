using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CaptureTowerOptions
{
 const string Dir="docs/development/tower_glow_options_20261017/";
 sealed class Glow {public SpriteRenderer source;public MeshRenderer renderer;public Material material;}
 sealed class View {public GameObject root;public SpriteRenderer body;public CoreGuardianOriginalMotion motion;public List<Glow> glows=new List<Glow>();public List<SpriteRenderer> bolts=new List<SpriteRenderer>();}
 static GameObject NewObject(string name,Scene scene){var o=new GameObject(name);o.hideFlags=HideFlags.HideAndDontSave;SceneManager.MoveGameObjectToScene(o,scene);return o;}
 static Glow MakeGlow(SpriteRenderer source,Shader shader,int role,int variant,List<UnityEngine.Object> owned)
 {
  var s=source.sprite;var r=s.textureRect;var b=s.bounds;float pad=3/s.pixelsPerUnit;
  var go=new GameObject("PreviewOnly_BlueMaskGlow");go.hideFlags=HideFlags.HideAndDontSave;go.transform.SetParent(source.transform,false);
  var mesh=new Mesh{name="Expanded original cutout quad"};owned.Add(mesh);
  mesh.vertices=new[]{new Vector3(b.min.x-pad,b.min.y-pad),new Vector3(b.max.x+pad,b.min.y-pad),new Vector3(b.max.x+pad,b.max.y+pad),new Vector3(b.min.x-pad,b.max.y+pad)};
  float w=s.texture.width,h=s.texture.height;
  mesh.uv=new[]{new Vector2((r.xMin-3)/w,(r.yMin-3)/h),new Vector2((r.xMax+3)/w,(r.yMin-3)/h),new Vector2((r.xMax+3)/w,(r.yMax+3)/h),new Vector2((r.xMin-3)/w,(r.yMax+3)/h)};
  mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();go.AddComponent<MeshFilter>().sharedMesh=mesh;
  var material=new Material(shader){name="PreviewOnly tower glow"};owned.Add(material);material.mainTexture=s.texture;
  material.SetVector("_UVRect",new Vector4(r.xMin/w,r.yMin/h,r.xMax/w,r.yMax/h));material.SetFloat("_Role",role);material.SetFloat("_Variant",variant);
  var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
  return new Glow{source=source,renderer=renderer,material=material};
 }
 public static object Main()
 {
  if(EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating)throw new Exception("Stopped idle editor required; user Play must not be interrupted");
  var game=UnityEngine.Object.FindFirstObjectByType<CoreGuardianPrototype>();var originalScene=game.gameObject.scene;
  if(originalScene.isDirty)throw new Exception("Clean source scene required");
  var source=Camera.main;float pixelsPerUnit=source.pixelHeight/(2*source.orthographicSize);
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
  var owned=new List<UnityEngine.Object>();var report=new List<object>();
  var rt=new RenderTexture(720,256,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();
  var active=RenderTexture.active;Texture2D tex=null;
  try
  {
   var camera=NewObject("PreviewOnly tower camera",scene).AddComponent<Camera>();camera.CopyFrom(source);camera.enabled=false;
   camera.transform.SetPositionAndRotation(new Vector3(16.5f,12.7f,source.transform.position.z),source.transform.rotation);
   camera.orthographicSize=128/pixelsPerUnit;camera.targetTexture=rt;camera.allowHDR=false;camera.allowMSAA=false;
   camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(originalScene)|EditorSceneManager.GetSceneCullingMask(scene);
   var data=camera.GetUniversalAdditionalCameraData();data.SetRenderer(-1);data.renderPostProcessing=false;data.antialiasing=AntialiasingMode.None;
   var shader=ShaderUtil.CreateShaderAsset(File.ReadAllText(Dir+"TowerGlowPreview.shader.txt"));owned.Add(shader);
   if(ShaderUtil.ShaderHasError(shader))throw new Exception("Preview shader has errors");
   var effects=Resources.LoadAll<Sprite>(CoreGuardianCombatArt.ResourcePath);
   for(int role=0;role<3;role++)
   {
    string roleName=new[]{"Single","Multi","Support"}[role];string output=Dir+roleName+"/";Directory.CreateDirectory(output);
    var views=new List<View>();
    for(int variant=0;variant<3;variant++)
    {
     var root=UnityEngine.Object.Instantiate(game.towerArt[role]);root.hideFlags=HideFlags.HideAndDontSave;SceneManager.MoveGameObjectToScene(root,scene);
     root.transform.position=new Vector3(16.5f+(variant-1)*240/pixelsPerUnit,12.2f,0);
     foreach(var depth in root.GetComponentsInChildren<ArtDepthSorting>(true))depth.enabled=false;
     var body=root.GetComponentsInChildren<SpriteRenderer>(true).Single(r=>r.name=="2D Sprite");body.sortingOrder=900;
     var view=new View{root=root,body=body,motion=new CoreGuardianOriginalMotion(body,roleName)};
     foreach(var r in root.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r==body||r.name=="Fixed"||r.name=="Moving").ToArray())view.glows.Add(MakeGlow(r,shader,role,variant,owned));
     if(role<2)
     {
      for(int n=0;n<(role==0?1:3);n++)
      {
       var bolt=NewObject("PreviewOnly existing approved bolt",scene).AddComponent<SpriteRenderer>();
       bolt.sprite=effects.Single(s=>s.name==(role==0?"SingleBolt":"MultiBolt"));bolt.sharedMaterial=body.sharedMaterial;bolt.sortingOrder=1500;CoreGuardianCombatArt.SetWidth(bolt,.275f);view.bolts.Add(bolt);
      }
     }
     views.Add(view);
    }
    for(int frame=0;frame<48;frame++)
    {
     float t=frame/20f;float cycle=t%1.2f;float age=cycle>=.5f?cycle-.5f:100;
     foreach(var view in views)
     {
      view.motion.Present(age,.28f);
      foreach(var glow in view.glows)
      {
       glow.renderer.enabled=glow.source.enabled;glow.renderer.sortingLayerID=glow.source.sortingLayerID;glow.renderer.sortingOrder=glow.source.sortingOrder+3;
       glow.material.SetFloat("_Clock",t);glow.material.SetFloat("_Age",age);
      }
      for(int n=0;n<view.bolts.Count;n++)
      {
       Vector2 direction=role==0?new Vector2(-1,.7f).normalized:new[]{new Vector2(-1,.65f).normalized,Vector2.up,new Vector2(1,.65f).normalized}[n];
       var bolt=view.bolts[n];bolt.enabled=age<.18f;
       if(bolt.enabled){bolt.transform.position=view.motion.MuzzleWorld(direction)+(Vector3)direction*(age*5);bolt.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);}
      }
     }
     // Repeat the first camera render so all newly created renderer resources are initialized.
     camera.Render();if(frame==0){camera.Render();camera.Render();}RenderTexture.active=rt;
     tex=new Texture2D(720,256,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,720,256),0,0);tex.Apply();
     File.WriteAllBytes(output+frame.ToString("D3")+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);tex=null;
    }
    report.Add(new{role=roleName,prefab=AssetDatabase.GetAssetPath(game.towerArt[role]),sourceSprite=AssetDatabase.GetAssetPath(views[0].body.sprite),filter=views[0].body.sprite.texture.filterMode.ToString(),variants=3,frames=48,fps=20,sourceBodyPixelsPerUnit=views[0].body.sprite.pixelsPerUnit,pixelScale=pixelsPerUnit,projectile=role==0?"SingleBolt":role==1?"MultiBolt (three distinct directions)":"None: reacts to allied firing",bodyWorldBounds=views[0].body.bounds.ToString()});
    foreach(var view in views){foreach(var b in view.bolts)UnityEngine.Object.DestroyImmediate(b.gameObject);UnityEngine.Object.DestroyImmediate(view.root);}
   }
  }
  finally
  {
   RenderTexture.active=active;if(tex!=null)UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
   EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(originalScene);
   foreach(var o in owned.AsEnumerable().Reverse())if(o!=null)UnityEngine.Object.DestroyImmediate(o);
  }
  var result=new{source="Original prefabs on current scene background, manual original recoil and diagnostic firing clock; not actual normal combat",sourceScene=originalScene.path,dirty=originalScene.isDirty,play=EditorApplication.isPlaying,sourceCameraHDR=source.allowHDR,sourceCameraPost=source.GetUniversalAdditionalCameraData().renderPostProcessing,sceneCount=SceneManager.sceneCount,report};
  File.WriteAllText(Dir+"capture.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
