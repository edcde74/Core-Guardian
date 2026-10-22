// Targeted integrity checks only; no Play, test runner, scene save or build.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
public static class CGCleanupVerification
{
    const string Root="docs/development/resource_cleanup_20261023/";
    public static object Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)throw new Exception("Busy");
        var rows=JArray.Parse(File.ReadAllText(Root+"manifest.json"));var problems=new List<string>();int kept=0,deleted=0,runtime=0,archived=0,edges=0;
        foreach(var row in rows)
        {
            string p=(string)row["path"];bool keep=(string)row["action"]=="keep";
            if(keep)
            {
                kept++;if(Hash(p)!=(string)row["sha256"]||Hash(p+".meta")!=(string)row["metaHash"])problems.Add("Kept asset changed: "+p);
                if(row["reasons"].Any(r=>((string)r).StartsWith("runtime Resources")))
                {
                    var resourcePath=p.Substring("Assets/Resources/".Length);resourcePath=resourcePath.Substring(0,resourcePath.LastIndexOf('.'));
                    runtime++;if(Resources.LoadAll<Sprite>(resourcePath).Length==0)problems.Add("Runtime sprite missing: "+resourcePath);
                }
            }
            else
            {
                deleted++;if(File.Exists(p)||File.Exists(p+".meta")||AssetDatabase.LoadMainAssetAtPath(p)!=null)problems.Add("Deletion incomplete: "+p);
                if((string)row["kind"]=="documentation-image")
                {archived++;if(Hash(Root+"imported_evidence/"+p.Substring("Assets/docs/".Length))!=(string)row["sha256"])problems.Add("Archived bytes differ: "+p);}
            }
        }
        var audit=JObject.Parse(File.ReadAllText(Root+"inventory.json"));
        foreach(var edge in audit["edges"])
        {
            var p=(string)edge["asset"];var current=new HashSet<string>(AssetDatabase.GetDependencies(p,false));
            foreach(string expected in edge["images"]){edges++;if(!current.Contains(expected))problems.Add("Lost dependency: "+p+" -> "+expected);}
        }
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var beforeLive=new HashSet<string>(audit["images"].Where(r=>(bool)r["live"]).Select(r=>(string)r["path"]));
        var currentLive=new HashSet<string>(EditorUtility.CollectDependencies(scene.GetRootGameObjects()).Select(AssetDatabase.GetAssetPath));
        foreach(var p in beforeLive)if(!currentLive.Contains(p))problems.Add("Live reference lost: "+p);
        var state=new{scene=scene.path,scene.isDirty,playing=EditorApplication.isPlaying,compiling=EditorApplication.isCompiling,updating=EditorApplication.isUpdating,profiler=UnityEngine.Profiling.Profiler.enabled,driver=UnityEditorInternal.ProfilerDriver.enabled,roots=scene.GetRootGameObjects().Select(g=>g.name).ToArray()};
        if(scene.isDirty)problems.Add("Unexpected dirty scene");
        var result=new{utc=DateTime.UtcNow,kept,deleted,runtimeSpritePathsVerified=runtime,archived,dependencyEdgesVerified=edges,liveImagesVerified=beforeLive.Count,problems,state};
        File.WriteAllText(Root+"verification.json",JsonConvert.SerializeObject(result,Formatting.Indented));
        if(problems.Count>0)throw new Exception(string.Join("; ",problems));return result;
    }
    static string Hash(string p){if(!File.Exists(p))return null;using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))).Replace("-","");}
}
