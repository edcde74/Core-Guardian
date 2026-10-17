using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public static class CaptureLive
{
 const string Dir="docs/development/single_tower_aim_20261017/";
 public static async Task<object> Main(string prefix="live")
 {
  if(!EditorApplication.isPlaying)throw new Exception("Own Play required");
  var g=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
  if(g==null||!g.Ready||g.Session.Phase!=CoreGuardian.SessionPhase.Preparation||g.Session.Towers.Count!=0)throw new Exception("Fresh own session required");
  if(!g.Place(CoreGuardian.TowerKind.Single,new Vector2Int(3,11)))throw new Exception(g.Feedback);
  g.StartCombat();
  var camera=Camera.main;var target=camera.targetTexture;var active=RenderTexture.active;
  var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();
  var scores=new List<float>();var angles=new List<float>();var times=new List<float>();
  var unit=UnityEngine.Object.FindObjectsByType<CoreGuardianUnitAnimation>().First(u=>u.name.StartsWith("Tower_Single"));
  var art=unit.GetComponentsInChildren<SpriteRenderer>(true).Single(r=>r.name=="2D Sprite");
  var mount=art.transform.Find("OriginalMotion/SingleMount");
  Action<CoreGuardian.ProjectileState> shot=p=>
  {
   if(!p.Source.StartsWith("Tower:"))return;
   Vector2 desired=g.World(p.Target.Position)+Vector3.up*.25f-mount.position;
   Vector2 actual=unit.MuzzleWorld-mount.position;
   scores.Add(Vector2.Dot(desired.normalized,actual.normalized));
  };
  g.Session.ProjectileLaunched+=shot;
  var center=camera.WorldToScreenPoint(g.World(new Vector2(3,11)));
  int cropX=Mathf.Max(0,Mathf.RoundToInt(center.x)-200),cropY=Mathf.Max(0,Mathf.RoundToInt(center.y)-80);
  Directory.CreateDirectory(Dir+prefix+"_frames");
  try
  {
   await Task.Delay(120);
   for(int i=0;i<80;i++)
   {
    camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
    var image=new Texture2D(440,260,TextureFormat.RGB24,false);
    try{image.ReadPixels(new Rect(cropX,cropY,440,260),0,0);image.Apply();File.WriteAllBytes(Dir+prefix+"_frames/"+i.ToString("D3")+".png",image.EncodeToPNG());}
    finally{UnityEngine.Object.DestroyImmediate(image);camera.targetTexture=target;RenderTexture.active=active;}
    times.Add(Time.time);angles.Add(unit.transform.GetComponentsInChildren<Transform>().First(t=>t.name=="SingleYaw").localEulerAngles.z);
    await Task.Delay(100);
   }
  }
  finally{g.Session.ProjectileLaunched-=shot;camera.targetTexture=target;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
  var result=new{frames=80,times,angles,launches=scores.Count,minimumAxisDot=scores.Count==0?0:scores.Min(),scores,diagnostic=g.Session.Diagnostic,crop=new[]{cropX,cropY,440,260},timeScale=Time.timeScale,hdr=camera.allowHDR,post=camera.GetUniversalAdditionalCameraData().renderPostProcessing,note="Actual camera render, normal purchase and round 1, existing API actions; crop only, no temporary zoom/time/input/economy injection; Overlay HUD not included"};
  File.WriteAllText(Dir+prefix+".json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
  return new{result.frames,result.launches,result.minimumAxisDot,result.diagnostic,seconds=times.Last()-times.First()};
 }
}
