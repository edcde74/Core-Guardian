// Executes only the reviewed manifest after a verified Git checkpoint. Outside Assets.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
public static class CGUnusedImageCleanup
{
    const string Root="docs/development/resource_cleanup_20261023/";
    sealed class Row {public string path,sha256,metaHash,action,kind;}
    public static object Execute()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||UnityEngine.Profiling.Profiler.enabled)throw new Exception("Editor busy");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity"||scene.isDirty)throw new Exception("Scene changed/unsaved");
        var rows=JsonConvert.DeserializeObject<Row[]>(File.ReadAllText(Root+"manifest.json"));
        var checkpoint=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Root+"checkpoint.json"));
        if((string)checkpoint["checkpoint"]!="5ae39a1"||(int)checkpoint["verifiedImageAndMetaBlobs"]!=rows.Count(r=>r.action=="delete")*2)throw new Exception("Checkpoint not verified");
        foreach(var row in rows)
        {
            var full=Path.GetFullPath(row.path);var assetRoot=Path.GetFullPath(Application.dataPath)+Path.DirectorySeparatorChar;
            if(!full.StartsWith(assetRoot,StringComparison.OrdinalIgnoreCase)||!File.Exists(full)||Hash(full)!=row.sha256||Hash(full+".meta")!=row.metaHash)throw new Exception("Path/hash changed: "+row.path);
        }
        var doomed=new HashSet<string>(rows.Where(r=>r.action=="delete").Select(r=>r.path));
        foreach(var p in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!AssetDatabase.IsValidFolder(p)&&!doomed.Contains(p)&&!p.EndsWith(".cs")&&!p.EndsWith(".shader")))
            foreach(var d in AssetDatabase.GetDependencies(p,false))if(doomed.Contains(d))throw new Exception("New serialized reference: "+p+" -> "+d);
        foreach(var obj in EditorUtility.CollectDependencies(scene.GetRootGameObjects()))if(doomed.Contains(AssetDatabase.GetAssetPath(obj)))throw new Exception("New live reference");
        var archived=new List<string>();
        foreach(var row in rows.Where(r=>r.action=="delete"&&r.kind=="documentation-image"))
        {
            var dest=Root+"imported_evidence/"+row.path.Substring("Assets/docs/".Length);Directory.CreateDirectory(Path.GetDirectoryName(dest));
            if(File.Exists(dest)&&Hash(dest)!=row.sha256)throw new Exception("Archive exists with different bytes: "+dest);
            if(!File.Exists(dest))File.Copy(row.path,dest);
            if(Hash(dest)!=row.sha256)throw new Exception("Archive hash mismatch: "+dest);archived.Add(dest);
        }
        var deleted=new List<string>();var failed=new List<string>();
        AssetDatabase.StartAssetEditing();
        try{foreach(var path in doomed){if(AssetDatabase.DeleteAsset(path))deleted.Add(path);else{failed.Add(path);break;}}}
        finally{AssetDatabase.StopAssetEditing();}
        File.WriteAllText(Root+"deletion_result.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,checkpoint="5ae39a1",deleted,failed,archived,scene=scene.path,scene.isDirty},Formatting.Indented));
        if(failed.Count>0)throw new Exception("Partial deletion; inspect deletion_result.json");
        return new{deleted=deleted.Count,documentationArchived=archived.Count,failed=failed.Count,scene.isDirty};
    }
    static string Hash(string path){if(!File.Exists(path))return null;using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","");}
}
