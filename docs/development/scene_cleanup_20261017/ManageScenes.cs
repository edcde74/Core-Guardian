using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ManageCoreGuardianScenes
{
    const string Keep="Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity";
    const string Folder="docs/development/scene_cleanup_20261017/";
    static string[] Scenes()=>AssetDatabase.FindAssets("t:Scene",new[]{"Assets"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.EndsWith(".unity",StringComparison.OrdinalIgnoreCase)).OrderBy(p=>p).ToArray();
    static string[] Templates(string[] deleted)=>AssetDatabase.FindAssets("t:SceneTemplateAsset",new[]{"Assets"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>AssetDatabase.GetDependencies(p,false).Any(d=>deleted.Contains(d))).OrderBy(p=>p).ToArray();
    public static object Audit()
    {
        Directory.CreateDirectory(Folder);
        var scenes=Scenes();if(!scenes.Contains(Keep))throw new Exception("Retained scene not found");
        var report=new {keep=Keep,delete=scenes.Where(p=>p!=Keep).Select(p=>new {path=p,guid=AssetDatabase.AssetPathToGUID(p)}).ToArray(),dependentTemplates=Templates(scenes.Where(p=>p!=Keep).ToArray()),buildScenes=EditorBuildSettings.scenes.Select(s=>new {s.path,s.enabled}).ToArray(),keepDependencies=AssetDatabase.GetDependencies(Keep,true).Where(p=>p.EndsWith(".unity")).ToArray()};
        File.WriteAllText(Folder+"before.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
    }
    public static object Delete()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new Exception("Stopped compiled editor required");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene; refusing deletion");
        var before=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Folder+"before.json"));
        var expected=before["delete"].Select(x=>(string)x["path"]).OrderBy(p=>p).ToArray();
        var targets=Scenes().Where(p=>p!=Keep).OrderBy(p=>p).ToArray();
        if(!targets.SequenceEqual(expected))throw new Exception("Scene inventory changed since checkpoint; refusing deletion");
        var templates=Templates(targets);var expectedTemplates=before["dependentTemplates"].Select(x=>(string)x).OrderBy(p=>p).ToArray();
        if(!templates.SequenceEqual(expectedTemplates))throw new Exception("Template dependencies changed since checkpoint");
        var root=Path.GetFullPath(Application.dataPath)+Path.DirectorySeparatorChar;
        foreach(var path in targets){var absolute=Path.GetFullPath(path);if(!absolute.StartsWith(root,StringComparison.OrdinalIgnoreCase)||path==Keep)throw new Exception("Deletion target escapes Assets");}
        if(EditorSceneManager.GetActiveScene().path!=Keep||UnityEngine.SceneManagement.SceneManager.sceneCount!=1)EditorSceneManager.OpenScene(Keep);
        foreach(var path in templates.Concat(targets))if(!AssetDatabase.DeleteAsset(path))throw new Exception("DeleteAsset failed: "+path);
        EditorBuildSettings.scenes=EditorBuildSettings.scenes.Where(s=>!targets.Contains(s.path)).ToArray();
        AssetDatabase.SaveAssets();
        var remaining=Scenes();if(remaining.Length!=1||remaining[0]!=Keep)throw new Exception("Unexpected remaining scene inventory");
        var result=new {deleted=targets,deletedTemplates=templates,remaining,active=EditorSceneManager.GetActiveScene().path,dirty=EditorSceneManager.GetActiveScene().isDirty,buildScenes=EditorBuildSettings.scenes.Select(s=>new {s.path,s.enabled}).ToArray()};
        File.WriteAllText(Folder+"after.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
