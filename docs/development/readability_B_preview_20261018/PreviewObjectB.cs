using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public static class PreviewObjectB
{
    const string Dir="docs/development/readability_B_preview_20261018/";
    const string Original="Assets/Art/ScreenTests/20260930/ArtReview_Unlit.mat";
    public static object RollbackA()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||scene.isDirty||scene.path!="Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity")throw new Exception("Saved stopped combat scene required");
        var targets=GameObject.Find("CoreGuardian_MapArt_B").GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.sharedMaterial!=null&&r.sharedMaterial.shader.name=="CoreGuardian/EnvironmentContrast").ToArray();
        if(targets.Length!=243)throw new Exception("Unexpected A target count");
        var original=AssetDatabase.LoadAssetAtPath<Material>(Original);Undo.RecordObjects(targets,"Restore original environment after A feedback");
        foreach(var r in targets)r.sharedMaterial=original;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        var result=new{restoredRenderers=targets.Length,sceneDirty=scene.isDirty,bodyModified=false};File.WriteAllText(Dir+"rollback_A.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
    static void Screen(string name)
    {
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Pipeline.Editor.Commands.Capture.CaptureCommands")).First(t=>t!=null);
        var capture=type.GetMethod("CaptureGameView",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{1920,1080,null,null,false,0,"screen"});
        File.WriteAllBytes(Dir+name+".png",Convert.FromBase64String((string)capture.GetType().GetProperty("Base64").GetValue(capture)));
    }
    public static async Task<object> Main()
    {
        if(!EditorApplication.isPlaying||EditorApplication.isPaused)throw new Exception("Own Play required");
        var g=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
        if(!g.Ready||g.Session.Diagnostic||g.Session.Towers.Count!=0)throw new Exception("Fresh normal session required");
        if(!g.Place(CoreGuardian.TowerKind.Multi,new Vector2Int(4,8)))throw new Exception("Normal purchase failed");
        g.StartCombat();var deadline=DateTime.UtcNow.AddSeconds(18);
        while(g.Session.Enemies.Count(e=>e.Alive)<4&&DateTime.UtcNow<deadline)await Task.Delay(75);
        var time=Time.timeScale;var delta=Time.captureDeltaTime;
        var cameraJson=EditorJsonUtility.ToJson(Camera.main);var cameraTransform=EditorJsonUtility.ToJson(Camera.main.transform);
        var bodies=new Dictionary<SpriteRenderer,Color>();var hosts=new List<GameObject>();
        try {
            if(g.Session.Enemies.Count(e=>e.Alive)<2)throw new Exception("No natural enemies");
            Time.timeScale=0;await Task.Delay(150);Screen("00_original_environment");
            var targets=new List<Transform>{g.player.transform};targets.AddRange(g.Session.Towers.Select(t=>g.TowerView(t.Id)));
            foreach(var e in g.Session.Enemies.Where(e=>e.Alive)){var go=GameObject.Find("Enemy_"+e.Kind+"_"+e.Id);if(go!=null)targets.Add(go.transform);}
            var core=UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Single(r=>r.name=="2D Sprite"&&r.sprite!=null&&r.sprite.name=="core");targets.Add(core.transform.parent);
            foreach(var target in targets){
                var art=target.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r=>r.name=="2D Sprite")?.transform;
                if(art==null)throw new Exception("Body root missing: "+target.name);
                foreach(var r in art.GetComponentsInChildren<SpriteRenderer>())if(r.enabled&&r.sprite!=null){bodies[r]=r.color;var color=r.color;r.color=new Color(color.r*1.15f,color.g*1.15f,color.b*1.15f,color.a);}
            }
            await Task.Delay(150);Screen("01_body_only");
            foreach(var target in targets){
                var host=new GameObject("B_Preview_Only_"+target.name){hideFlags=HideFlags.DontSave};hosts.Add(host);
                var outline=host.AddComponent<CoreGuardianSelectionOutline>();outline.enabled=false;
                outline.widthPixels=1;outline.color=new Color(.06f,.065f,.075f,.90f);outline.alphaThreshold=.1f;
                outline.Present(target,Camera.main);
                if(!outline.IsVisible)throw new Exception("Union outline missing: "+target.name);
            }
            await Task.Delay(150);Screen("02_body_dark_edge");
            // Light rim is an alternate preview, not a selected-state outline or an emissive effect.
            foreach(var host in hosts){var outline=host.GetComponent<CoreGuardianSelectionOutline>();outline.color=new Color(.70f,.70f,.66f,.55f);outline.Present(outline.CurrentTarget,Camera.main);}
            await Task.Delay(150);Screen("03_body_light_edge");
            var report=new{previewOnly=true,bodyMultiplier=1.15,widthPixels=1,darkEdge=new[]{.06f,.065f,.075f,.90f},lightEdge=new[]{.70f,.70f,.66f,.55f},targets=targets.Count,sourceRenderers=bodies.Count,liveEnemies=g.Session.Enemies.Count(e=>e.Alive),sameFrozenSession=true,diagnostic=g.Session.Diagnostic,cameraUnchanged=cameraJson==EditorJsonUtility.ToJson(Camera.main)&&cameraTransform==EditorJsonUtility.ToJson(Camera.main.transform),overlayIncluded=true,temporaryTimeScale=0,restoresTimeScale=time,originalArtworkUnmodified=true};
            File.WriteAllText(Dir+"preview.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
        } finally {
            foreach(var pair in bodies)if(pair.Key!=null)pair.Key.color=pair.Value;
            foreach(var host in hosts)if(host!=null){host.SetActive(false);UnityEngine.Object.DestroyImmediate(host);}
            Time.timeScale=time;Time.captureDeltaTime=delta;
        }
    }
}
