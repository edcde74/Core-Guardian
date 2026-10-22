// Folder/file organization only. Compiled in memory, outside Assets.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
public static class CGDirectoryMove
{
    const string Root="docs/development/directory_reorganization_20261023/";
    public static object Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||UnityEngine.Profiling.Profiler.enabled)throw new Exception("Busy");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty||scene.path!="Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity")throw new Exception("Scene changed");
        var map=JArray.Parse(File.ReadAllText(Root+"move_map.json"));var prefixes=JArray.Parse(File.ReadAllText(Root+"prefix_map.json"));
        foreach(var row in map)
        {
            string from=(string)row["source"],to=(string)row["destination"];
            if(!from.StartsWith("Assets/")||!to.StartsWith("Assets/")||to.Contains("..")||AssetDatabase.AssetPathToGUID(from)!=(string)row["guid"]||Hash(from)!=(string)row["hash"]||Hash(from+".meta")!=(string)row["metaHash"])throw new Exception("Precondition changed: "+from);
            if(from!=to&&File.Exists(to))throw new Exception("Destination exists: "+to);
        }
        var done=new List<object>();var changedSources=new List<string>();
        // Register folders before pausing imports; a queued folder cannot be a MoveAsset parent.
        foreach(var folder in map.Select(r=>Path.GetDirectoryName((string)r["destination"]).Replace('\\','/')).Distinct().OrderBy(p=>p.Length))Folder(folder);
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach(var row in map)
            {
                string from=(string)row["source"],to=(string)row["destination"];if(from==to)continue;
                Folder(Path.GetDirectoryName(to).Replace('\\','/'));
                var error=AssetDatabase.ValidateMoveAsset(from,to);if(!string.IsNullOrEmpty(error))throw new Exception(error);
                error=AssetDatabase.MoveAsset(from,to);if(!string.IsNullOrEmpty(error))throw new Exception(error);
                done.Add(new{from,to,guid=(string)row["guid"]});
            }
            // Exact paths and provably consistent directory prefixes; never rewrite runtime Resources keys.
            foreach(var row in map.Where(r=>((string)r["destination"]).EndsWith(".cs")))
            {
                string path=(string)row["destination"],before=File.ReadAllText(path),after=before;
                foreach(var item in map.OrderByDescending(r=>((string)r["source"]).Length))after=after.Replace((string)item["source"],(string)item["destination"]);
                foreach(var item in prefixes.OrderByDescending(r=>((string)r["source"]).Length))after=after.Replace((string)item["source"],(string)item["destination"]);
                if(path.EndsWith("/RouteMeshBuilder.cs"))
                {
                    after=after.Replace("const string Folder=\"Assets/Art/Environment/AbandonedFortressB/CombinedRoute\";","const string MeshFolder=\"Assets/Art/Environment/Meshes/Routes\";\n    const string MaterialFolder=\"Assets/Art/Rendering/Materials/Environment\";");
                    after=after.Replace("EnsureFolder(Folder);","EnsureFolder(MeshFolder);EnsureFolder(MaterialFolder);");
                    after=after.Replace("Folder+\"/\"+mesh.name+\".asset\"","MeshFolder+\"/Route_Order_\"+r.sortingOrder.ToString(\"00\")+\".asset\"");
                    after=after.Replace("Folder+\"/RouteMaterial.mat\"","MaterialFolder+\"/Route_Material.mat\"");
                }
                if(after==before)continue;File.WriteAllText(path,after,new System.Text.UTF8Encoding(false));changedSources.Add(path);AssetDatabase.ImportAsset(path);
            }
            File.WriteAllText(Root+"move_result.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,moved=done,changedSources,error=(string)null},Formatting.Indented));
        }
        catch(Exception e){File.WriteAllText(Root+"move_result.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,moved=done,changedSources,error=e.ToString()},Formatting.Indented));throw;}
        finally{AssetDatabase.StopAssetEditing();}
        return new{moved=done.Count,changedSourcePaths=changedSources.Count,scene.isDirty};
    }
    static void Folder(string p){if(AssetDatabase.IsValidFolder(p))return;var parent=Path.GetDirectoryName(p).Replace('\\','/');Folder(parent);var guid=AssetDatabase.CreateFolder(parent,Path.GetFileName(p));if(string.IsNullOrEmpty(guid))throw new Exception("Cannot create folder: "+p);}
    static string Hash(string p){if(!File.Exists(p))return null;using(var sha=System.Security.Cryptography.SHA256.Create())using(var stream=File.OpenRead(p))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");}
}
