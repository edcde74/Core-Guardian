using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CaptureVisualOptions
{
 const string Dir="docs/development/visual_options_20261017/";
 static GameObject NewObject(string name,Scene scene)
 {
  var o=new GameObject(name);o.hideFlags=HideFlags.HideAndDontSave;
  SceneManager.MoveGameObjectToScene(o,scene);return o;
 }
 public static object Main()
 {
  if(EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating)throw new Exception("Idle stopped editor required");
  var source=Camera.main;var sourceScene=source.gameObject.scene;
  if(sourceScene.isDirty)throw new Exception("Clean source scene required");
  var originalRenderers=UnityEngine.Object.FindObjectsByType<Renderer>();
  var mats=originalRenderers.Select(r=>r.sharedMaterials).ToArray();
  var global=UnityEngine.Object.FindObjectsByType<Light2D>().Single(l=>l.lightType==Light2D.LightType.Global);
  float oldIntensity=global.intensity;Color oldColor=global.color;
  var core=UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Single(r=>r.name=="2D Sprite"&&r.transform.position.x>15);
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
  var owned=new List<UnityEngine.Object>();
  var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();
  var prev=RenderTexture.active;Texture2D tex=null;
  var report=new List<object>();
  try
  {
   var cam=NewObject("Preview camera",scene).AddComponent<Camera>();cam.CopyFrom(source);cam.enabled=false;
   cam.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
   cam.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(sourceScene)|EditorSceneManager.GetSceneCullingMask(scene);
   cam.targetTexture=rt;cam.allowHDR=true;
   var data=cam.GetUniversalAdditionalCameraData();data.SetRenderer(-1);data.volumeLayerMask=1<<31;data.antialiasing=AntialiasingMode.None;data.renderPostProcessing=true;
   var volumeGo=NewObject("Preview volume",scene);volumeGo.layer=31;
   var volume=volumeGo.AddComponent<Volume>();volume.isGlobal=true;volume.priority=10000;
   var profile=ScriptableObject.CreateInstance<VolumeProfile>();owned.Add(profile);volume.sharedProfile=profile;
   var color=profile.Add<ColorAdjustments>(true);owned.Add(color);
   var white=profile.Add<WhiteBalance>(true);owned.Add(white);
   var bloom=profile.Add<Bloom>(true);owned.Add(bloom);bloom.threshold.value=1.1f;bloom.scatter.value=.3f;bloom.highQualityFiltering.value=true;
   foreach(var r in originalRenderers)
    if(r is UnityEngine.Tilemaps.TilemapRenderer)
    {
     var lit=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));owned.Add(lit);
     if(r.sharedMaterial.HasProperty("_Color"))lit.SetColor("_Color",r.sharedMaterial.GetColor("_Color"));
     r.sharedMaterial=lit;
    }
   var lampGo=NewObject("Core actual Light2D",scene);lampGo.transform.position=new Vector3(17.7f,6.5f,0);
   var lamp=lampGo.AddComponent<Light2D>();lamp.lightType=Light2D.LightType.Point;lamp.color=new Color(.18f,.8f,1);lamp.pointLightInnerRadius=.6f;lamp.pointLightOuterRadius=5;lamp.falloffIntensity=.7f;
   var warmGo=NewObject("Warm upper fill Light2D",scene);warmGo.transform.position=new Vector3(9,12,0);
   var warm=warmGo.AddComponent<Light2D>();warm.lightType=Light2D.LightType.Point;warm.color=new Color(1,.72f,.4f);warm.pointLightInnerRadius=2;warm.pointLightOuterRadius=12;warm.falloffIntensity=.6f;
   var sortingField=typeof(Light2D).GetField("m_ApplyToSortingLayers",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
   var sortingIds=SortingLayer.layers.Select(l=>l.id).ToArray();sortingField.SetValue(lamp,sortingIds);sortingField.SetValue(warm,sortingIds);
   var lateUpdate=typeof(Light2D).GetMethod("LateUpdate",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
   lateUpdate.Invoke(lamp,null);lateUpdate.Invoke(warm,null);
   var shader=ShaderUtil.CreateShaderAsset(File.ReadAllText(Dir+"CrystalEmission.shader.txt"));owned.Add(shader);
   if(ShaderUtil.ShaderHasError(shader))throw new Exception("Preview emission shader failed");
   var emission=new Material(shader);owned.Add(emission);
   var glowGo=NewObject("Source crystal masked emission",scene);glowGo.transform.SetPositionAndRotation(core.transform.position,core.transform.rotation);glowGo.transform.localScale=core.transform.lossyScale;
   var glow=glowGo.AddComponent<SpriteRenderer>();glow.sprite=core.sprite;glow.flipX=core.flipX;glow.flipY=core.flipY;glow.sortingLayerID=core.sortingLayerID;glow.sortingOrder=core.sortingOrder+1;glow.sharedMaterial=emission;
   var particleGo=NewObject("Preview pixel particles frozen at sample",scene);particleGo.transform.position=new Vector3(17.7f,6.4f,0);
   var ps=particleGo.AddComponent<ParticleSystem>();var main=ps.main;main.playOnAwake=false;main.loop=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startSpeed=0;main.startLifetime=10;main.maxParticles=64;
   var emit=ps.emission;emit.enabled=false;var shape=ps.shape;shape.enabled=false;ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=false;ps.randomSeed=17;
   var pr=ps.GetComponent<ParticleSystemRenderer>();pr.sortingOrder=1000;pr.renderMode=ParticleSystemRenderMode.Billboard;
   var particleMaterial=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));owned.Add(particleMaterial);particleMaterial.SetFloat("_Surface",1);particleMaterial.SetFloat("_Blend",0);particleMaterial.SetFloat("_ZWrite",0);particleMaterial.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);particleMaterial.SetFloat("_DstBlend",(float)BlendMode.One);particleMaterial.renderQueue=3000;particleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");pr.sharedMaterial=particleMaterial;
   // Warm-up before the first retained image; a newly created camera can return only its clear color.
   VolumeManager.instance.Update(cam.transform,data.volumeLayerMask);cam.Render();cam.Render();cam.Render();
   for(int k=0;k<4;k++)
   {
    string id=new[]{"A_clear_fortress","B_cool_depth","C_crystal_energy","D_warm_machinery"}[k];
    global.intensity=new[]{.9f,.68f,.72f,.8f}[k];
    global.color=k==3?new Color(1,.94f,.84f):k==1?new Color(.78f,.87f,1):new Color(.93f,.96f,1);
    lamp.intensity=new[]{.38f,.85f,1.05f,.65f}[k];warm.intensity=k==3?.38f:0;
    glow.enabled=k>=2;emission.SetFloat("_Strength",k==2?3.3f:2.2f);
    color.contrast.value=new[]{5,10,8,8}[k];color.saturation.value=new[]{2,-4,3,1}[k];white.temperature.value=k==3?6:k==1?-5:0;
    bloom.intensity.value=k>=2?.22f:.04f;
    ps.Simulate(.1f,true,true,true);ps.Clear(true);
    int count=k>=2?(k==2?22:12):0;
    var random=new System.Random(1717);
    for(int n=0;n<count;n++)
    {
     var ep=new ParticleSystem.EmitParams();ep.position=new Vector3(17.7f+(float)(random.NextDouble()-.5)*2.8f,6+(float)random.NextDouble()*2.8f,0);
     ep.velocity=Vector3.zero;ep.startLifetime=10;ep.startSize=n%4==0?.065f:.035f;
     ep.startColor=n%3==0&&k==3?new Color(1.8f,1.1f,.35f,.8f):new Color(.25f,1.7f,2.3f,.8f);ps.Emit(ep,1);
    }
    ps.Simulate(.01f,true,false,true);ps.Pause(true);
    VolumeManager.instance.Update(cam.transform,data.volumeLayerMask);cam.Render();cam.Render();cam.Render();RenderTexture.active=rt;
    tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();File.WriteAllBytes(Dir+id+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);tex=null;
    report.Add(new{id,globalIntensity=global.intensity,globalColor=global.color.ToString(),coreLight=lamp.intensity,warmFill=warm.intensity,emission=k>=2,emissionStrength=k>=2?emission.GetFloat("_Strength"):0,particles=count,actualParticles=ps.particleCount,particleBounds=pr.bounds.ToString(),bloom=bloom.intensity.value,bloomThreshold=bloom.threshold.value,contrast=color.contrast.value,saturation=color.saturation.value});
   }
  }
  finally
  {
   for(int i=0;i<originalRenderers.Length;i++)if(originalRenderers[i])originalRenderers[i].sharedMaterials=mats[i];
   global.intensity=oldIntensity;global.color=oldColor;
   RenderTexture.active=prev;if(tex)UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
   EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(sourceScene);
   foreach(var o in owned.AsEnumerable().Reverse())if(o)UnityEngine.Object.DestroyImmediate(o);
  }
  var result=new {sourceScene=sourceScene.path,dirty=sourceScene.isDirty,play=EditorApplication.isPlaying,sourceHDR=source.allowHDR,sourcePost=source.GetUniversalAdditionalCameraData().renderPostProcessing,sceneCount=SceneManager.sceneCount,sourceGlobalIntensity=global.intensity,sourceGlobalColor=global.color.ToString(),report};
  File.WriteAllText(Dir+"capture.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
