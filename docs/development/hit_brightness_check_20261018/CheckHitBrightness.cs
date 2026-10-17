using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public static class CheckHitBrightness
{
    const string Dir="docs/development/hit_brightness_check_20261018/";
    static void Screen(string name)
    {
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Pipeline.Editor.Commands.Capture.CaptureCommands")).First(t=>t!=null);
        var capture=type.GetMethod("CaptureGameView",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{1920,1080,null,null,false,0,"screen"});
        File.WriteAllBytes(Dir+name+".png",Convert.FromBase64String((string)capture.GetType().GetProperty("Base64").GetValue(capture)));
    }
    static object Properties(SpriteRenderer[] parts)=>parts.Select(r=>{
        var b=new MaterialPropertyBlock();r.GetPropertyBlock(b);
        return new{r.name,r.enabled,active=r.gameObject.activeInHierarchy,color=new[]{r.color.r,r.color.g,r.color.b,r.color.a},shader=r.sharedMaterial.shader.name,hasOverride=r.HasPropertyBlock(),empty=b.isEmpty,hit=b.GetFloat("_HitFlashAmount"),props=new[]{"unity_SpriteColor","unity_SpriteRendererColorArray","_RendererColor","_MainTex"}.Select(n=>new{n,exists=b.HasProperty(n),value=n=="_MainTex"?null:(object)b.GetVector(n).ToString()}).ToArray()};
    }).ToArray();
    public static async Task<object> Main()
    {
        if(!EditorApplication.isPlaying||EditorApplication.isPaused)throw new Exception("Own fresh Play required");
        var g=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();var settings=g.GetComponent<CoreGuardianObjectEmphasis>();
        if(!g.Ready||g.Session.Enemies.Count>0)throw new Exception("Fresh session required");
        var time=Time.timeScale;var delta=Time.captureDeltaTime;var enabled=g.enabled;var emphasize=settings.enabled;
        try {
            g.StartCombat();var deadline=DateTime.UtcNow.AddSeconds(6);while(!g.Session.Enemies.Any(e=>e.Alive)&&DateTime.UtcNow<deadline)await Task.Delay(50);
            var enemy=g.Session.Enemies.First(e=>e.Alive);var root=GameObject.Find("Enemy_"+enemy.Kind+"_"+enemy.Id);
            g.enabled=false;Time.timeScale=0;await Task.Delay(150);
            var unit=root.GetComponent<CoreGuardianUnitAnimation>();var flash=(CoreGuardianHitFlash)typeof(CoreGuardianUnitAnimation).GetField("hitFlash",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(unit);
            var art=root.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");var parts=art.GetComponentsInChildren<SpriteRenderer>(true);
            var before=Properties(parts);Screen("00_before");
            settings.enabled=false;await Task.Delay(150);Screen("00_before_emphasis_off");settings.enabled=emphasize;await Task.Delay(150);
            flash.Trigger();await Task.Delay(150);Screen("01_during");var during=Properties(parts);
            flash.Advance(.12f);await Task.Delay(150);Screen("02_after");var after=Properties(parts);
            settings.enabled=false;await Task.Delay(150);Screen("02_after_emphasis_off");settings.enabled=emphasize;
            foreach(var r in parts)r.SetPropertyBlock(null);await Task.Delay(150);Screen("03_null_block");var cleared=Properties(parts);
            var visible=parts.Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();var bounds=visible[0].bounds;foreach(var r in visible)bounds.Encapsulate(r.bounds);
            var camera=Camera.main;var lo=camera.WorldToScreenPoint(bounds.min);var hi=camera.WorldToScreenPoint(bounds.max);
            var rect=new[]{Mathf.FloorToInt(lo.x)-4,1080-Mathf.CeilToInt(hi.y)-4,Mathf.CeilToInt(hi.x)+4,1080-Mathf.FloorToInt(lo.y)+4};
            var report=new{diagnosticOnly=true,actualNaturalEnemy=enemy.Id,healthUnchanged=enemy.Health,poseFrozen=true,before,during,after,cleared,crop=rect};
            File.WriteAllText(Dir+"check.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return new{report.diagnosticOnly,report.actualNaturalEnemy,report.healthUnchanged,report.poseFrozen,report.crop};
        } finally {g.enabled=enabled;settings.enabled=emphasize;Time.timeScale=time;Time.captureDeltaTime=delta;}
    }
}
