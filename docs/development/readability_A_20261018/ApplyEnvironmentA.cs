using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ApplyEnvironmentA
{
    const string Dir="docs/development/readability_A_20261018/";
    const string MatDir="Assets/Art/Environment/AbandonedFortressB/";
    const string Original="Assets/Art/ScreenTests/20260930/ArtReview_Unlit.mat";
    static SpriteRenderer[] Targets()=>GameObject.Find("CoreGuardian_MapArt_B").GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.enabled&&(r.name=="Background_Ruins_VisualOnly"||r.transform.IsChildOf(GameObject.Find("CoreGuardian_MapArt_B/CoreGuardian_Route01_VisualOnly").transform))).ToArray();
    static string Protected()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var target=new HashSet<Component>(Targets());
        var rows=UnityEngine.Object.FindObjectsByType<Component>(FindObjectsInactive.Include).Where(c=>c.gameObject.scene==scene).Select(c=>{
            var json=Newtonsoft.Json.Linq.JObject.Parse(EditorJsonUtility.ToJson(c));
            if(target.Contains(c))((Newtonsoft.Json.Linq.JObject)json.First.First).Remove("m_Materials");
            return new {id=c.GetEntityId().ToString(),type=c.GetType().FullName,json=json.ToString(Newtonsoft.Json.Formatting.None)};
        }).OrderBy(x=>x.id).ToArray();
        return Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented);
    }
    static void Capture(string name)
    {
        var camera=Camera.main;var previous=camera.targetTexture;var active=RenderTexture.active;
        var rt=RenderTexture.GetTemporary(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);Texture2D tex=null;
        try {camera.targetTexture=rt;var d=camera.GetComponent<UniversalAdditionalCameraData>();VolumeManager.instance.Update(camera.transform,d.volumeLayerMask);camera.Render();camera.Render();RenderTexture.active=rt;tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();File.WriteAllBytes(Dir+name+".png",tex.EncodeToPNG());}
        finally {camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);if(tex!=null)UnityEngine.Object.DestroyImmediate(tex);}
    }
    static Material Make(string name,float contrast,float pivot,bool mask)
    {
        string path=MatDir+name+".mat";if(AssetDatabase.LoadAssetAtPath<Material>(path)!=null)throw new Exception("Material already exists");
        var mat=new Material(AssetDatabase.LoadAssetAtPath<Material>(Original));mat.name=name;mat.shader=Shader.Find("CoreGuardian/EnvironmentContrast");mat.SetFloat("_DetailContrast",contrast);mat.SetFloat("_LuminancePivot",pivot);mat.SetFloat("_InteriorOnly",mask?1:0);AssetDatabase.CreateAsset(mat,path);return mat;
    }
    public static object FinishPending()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Stopped editor required");
        var ids=new HashSet<string>(Targets().Select(r=>r.GetEntityId().ToString()));
        var rows=Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText(Dir+"protected_before.json"));
        foreach(var row in rows)if(ids.Contains((string)row["id"])){
            var json=Newtonsoft.Json.Linq.JObject.Parse((string)row["json"]);((Newtonsoft.Json.Linq.JObject)json.First.First).Remove("m_Materials");row["json"]=json.ToString(Newtonsoft.Json.Formatting.None);
        }
        var after=Newtonsoft.Json.Linq.JArray.Parse(Protected());
        if(!Newtonsoft.Json.Linq.JToken.DeepEquals(rows,after))throw new Exception("Protected component comparison still fails");
        File.WriteAllText(Dir+"protected_after.json",after.ToString());
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        var report=new{applied=true,protectedComponentsEqual=true,protectedCount=rows.Count,renderers=ids.Count,courtyardContrast=.62f,courtyardPivot=.14f,routeContrast=.70f,routePivot=.20f,outerRuinsMasked=true,sceneDirty=scene.isDirty};
        File.WriteAllText(Dir+"applied.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
    }
    public static object Main()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed||scene.isDirty||scene.path!="Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity")throw new Exception("Saved stopped combat scene required");
        var targets=Targets();if(targets.Length!=243||targets.Any(r=>AssetDatabase.GetAssetPath(r.sharedMaterial)!=Original))throw new Exception("Unexpected target renderer state");
        Capture("edit_before");var before=Protected();File.WriteAllText(Dir+"protected_before.json",before);
        var floor=Make("ReadabilityA_Courtyard",.62f,.14f,true);var road=Make("ReadabilityA_Route",.70f,.20f,false);
        Undo.RecordObjects(targets,"Readability A environment materials");
        foreach(var r in targets)r.sharedMaterial=r.name=="Background_Ruins_VisualOnly"?floor:road;
        Capture("edit_after");var after=Protected();File.WriteAllText(Dir+"protected_after.json",after);
        if(before!=after)throw new Exception("Protected scene components changed; inspect before save");
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        var result=new {applied=true,protectedComponentsEqual=true,renderers=targets.Length,courtyardContrast=.62f,courtyardPivot=.14f,routeContrast=.70f,routePivot=.20f,outerRuinsMasked=true,sceneDirty=scene.isDirty,sourcePngUnmodified=true};
        File.WriteAllText(Dir+"applied.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
    static void Screen(string name)
    {
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Pipeline.Editor.Commands.Capture.CaptureCommands")).First(t=>t!=null);
        var capture=type.GetMethod("CaptureGameView",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{1920,1080,null,null,false,0,"screen"});
        File.WriteAllBytes(Dir+name+".png",Convert.FromBase64String((string)capture.GetType().GetProperty("Base64").GetValue(capture)));
    }
    public static async Task<object> CombatComparison()
    {
        if(!EditorApplication.isPlaying||EditorApplication.isPaused)throw new Exception("Own running Play required");
        var g=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();if(!g.Ready||g.Session.Diagnostic||g.Session.Towers.Count!=0)throw new Exception("Fresh normal session required");
        var targets=Targets();var saved=targets.Select(r=>r.sharedMaterial).ToArray();var time=Time.timeScale;var delta=Time.captureDeltaTime;
        var cameraJson=EditorJsonUtility.ToJson(Camera.main);var cameraTransform=EditorJsonUtility.ToJson(Camera.main.transform);
        int money=g.Session.Crystals;
        if(!g.Place(CoreGuardian.TowerKind.Multi,new Vector2Int(4,8)))throw new Exception("Normal multi purchase failed");
        g.StartCombat();var deadline=DateTime.UtcNow.AddSeconds(18);
        while(g.Session.Enemies.Count(e=>e.Alive)<4&&DateTime.UtcNow<deadline)await Task.Delay(75);
        if(g.Session.Enemies.Count(e=>e.Alive)<2)throw new Exception("Natural enemies not present");
        try {
            Time.timeScale=0;await Task.Delay(150);Screen("combat_after");
            var original=AssetDatabase.LoadAssetAtPath<Material>(Original);foreach(var r in targets)r.sharedMaterial=original;
            await Task.Delay(150);Screen("combat_before");
            var result=new{normalPurchaseSpent=money-g.Session.Crystals,liveEnemies=g.Session.Enemies.Count(e=>e.Alive),diagnostic=g.Session.Diagnostic,sameFrozenSession=true,overlayIncluded=true,temporaryTimeScale=0,originalTimeScale=time,cameraUnchanged=cameraJson==EditorJsonUtility.ToJson(Camera.main)&&cameraTransform==EditorJsonUtility.ToJson(Camera.main.transform),method="Own Play, normal Session purchase/API StartCombat, natural spawn, temporary material swap. Not human play or balance measurement."};
            File.WriteAllText(Dir+"combat_qa.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        } finally {for(int i=0;i<targets.Length;i++)targets[i].sharedMaterial=saved[i];Time.timeScale=time;Time.captureDeltaTime=delta;}
    }
}
