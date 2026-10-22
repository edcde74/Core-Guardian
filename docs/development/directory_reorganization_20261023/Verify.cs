// Post-move GUID/content/dependency/Resources/assembly checks. No gameplay execution.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
public static class CGDirectoryVerify
{
    const string Root="docs/development/directory_reorganization_20261023/";
    public static object Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)throw new Exception("Busy");
        var inventory=JArray.Parse(File.ReadAllText(Root+"inventory_before.json"));var map=JArray.Parse(File.ReadAllText(Root+"move_map.json"));
        var changes=new HashSet<string>(JObject.Parse(File.ReadAllText(Root+"move_result.json"))["changedSources"].Values<string>());
        var nameOnly=File.Exists(Root+"name_only_changes.json")?JArray.Parse(File.ReadAllText(Root+"name_only_changes.json")):new JArray();int verifiedNames=0;
        var problems=new List<string>();int guids=0,content=0,meta=0,keys=0,dependencies=0,assemblies=0;var hashMismatches=new List<object>();
        foreach(var row in map)
        {
            string path=(string)row["destination"],guid=(string)row["guid"];var before=inventory.First(r=>(string)r["guid"]==guid);
            if(AssetDatabase.GUIDToAssetPath(guid)!=path){problems.Add("GUID path: "+path);continue;}guids++;
            bool same=Hash(path)==(string)row["hash"];if(same)content++;else if(!changes.Contains(path)){
                var proof=nameOnly.FirstOrDefault(r=>(string)r["path"]==path&&(bool)r["onlySerializedNameChanged"]&&(string)r["hash"]==Hash(path));
                if(proof!=null)verifiedNames++;else{hashMismatches.Add(new{path,oldHash=(string)row["hash"],current=Hash(path)});problems.Add("Unexpected content change: "+path);}
            }
            if(Hash(path+".meta")== (string)row["metaHash"])meta++;else problems.Add("Meta changed: "+path);
            var currentDeps=new HashSet<string>(AssetDatabase.GetDependencies(path,false).Where(p=>p!=path).Select(AssetDatabase.AssetPathToGUID).Where(g=>!string.IsNullOrEmpty(g)));
            var oldDeps=new HashSet<string>(before["dependencies"].Values<string>());if(!oldDeps.SetEquals(currentDeps))problems.Add("Dependency set changed: "+path);else dependencies++;
            if(row["resourceKey"].Type!=JTokenType.Null)
            {
                var key=(string)row["resourceKey"];var loaded=Resources.Load(key);if(loaded==null||AssetDatabase.GetAssetPath(loaded)!=path)problems.Add("Resources key failed: "+key);else keys++;
            }
            if(path.EndsWith(".cs"))
            {
                string now=UnityEditor.Compilation.CompilationPipeline.GetAssemblyNameFromScriptPath(path),old=(string)before["compileAssembly"];
                if(now!=old)problems.Add("Assembly changed: "+path+" "+old+" -> "+now);else assemblies++;
            }
        }
        // A stale baked hash silently changes mask production to fallback even when pixels match.
        var manifestRow=map.First(r=>((string)r["source"]).EndsWith("CoreGuardian/MaskAtlas/Manifest.asset"));
        var manifest=AssetDatabase.LoadAssetAtPath<CoreGuardianMaskAtlas>((string)manifestRow["destination"]);var serialized=new SerializedObject(manifest);var entries=serialized.FindProperty("entries");int validMaskEntries=0;
        for(int i=0;i<entries.arraySize;i++){
            var entry=entries.GetArrayElementAtIndex(i);var source=entry.FindPropertyRelative("source").objectReferenceValue;string expected=entry.FindPropertyRelative("dependencyHash").stringValue;
            if(source==null||expected!=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(source)).ToString())problems.Add("Mask dependencyHash stale: "+(source==null?"null":AssetDatabase.GetAssetPath(source)));else validMaskEntries++;
        }
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)problems.Add("Unexpected dirty scene");
        var result=new{utc=DateTime.UtcNow,files=map.Count,guidsVerified=guids,byteIdenticalAssets=content,expectedChangedSourceFiles=changes.Count,verifiedNameOnlyChanges=verifiedNames,metaIdentical=meta,dependencySetsVerified=dependencies,resourceKeysLoaded=keys,scriptAssembliesVerified=assemblies,maskEntriesValid=validMaskEntries,maskEntries=entries.arraySize,problems,hashMismatches,state=new{scene=scene.path,scene.isDirty,playing=EditorApplication.isPlaying,compiling=EditorApplication.isCompiling,updating=EditorApplication.isUpdating,profiler=UnityEngine.Profiling.Profiler.enabled,driver=UnityEditorInternal.ProfilerDriver.enabled}};
        File.WriteAllText(Root+"verification.json",JsonConvert.SerializeObject(result,Formatting.Indented));
        return new{files=map.Count,guids,content,meta,dependencies,keys,assemblies,validMaskEntries,problems=problems.Take(12).ToArray(),problemCount=problems.Count};
    }
    public static object PruneEmptyFolders()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Busy");
        var all=AssetDatabase.GetAllAssetPaths();var used=new HashSet<string>(all.Where(p=>p.StartsWith("Assets/")&&!AssetDatabase.IsValidFolder(p)).SelectMany(p=>AssetDatabase.GetDependencies(p,false)).Select(AssetDatabase.AssetPathToGUID));
        var deleted=new List<string>();
        foreach(var folder in all.Where(p=>p.StartsWith("Assets/")&&AssetDatabase.IsValidFolder(p)).OrderByDescending(p=>p.Length))
        {
            if(!Path.GetFullPath(folder).StartsWith(Path.GetFullPath(Application.dataPath)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("Folder escaped Assets");
            if(used.Contains(AssetDatabase.AssetPathToGUID(folder))||!Directory.Exists(folder)||Directory.EnumerateFileSystemEntries(folder).Any())continue;
            if(!AssetDatabase.DeleteAsset(folder))throw new Exception("Empty folder delete failed: "+folder);deleted.Add(folder);
        }
        File.WriteAllText(Root+"empty_folders_removed.json",JsonConvert.SerializeObject(deleted,Formatting.Indented));return new{removed=deleted.Count,rootFolders=AssetDatabase.GetSubFolders("Assets")};
    }
    static string Hash(string p){if(!File.Exists(p))return null;using(var sha=System.Security.Cryptography.SHA256.Create())using(var stream=File.OpenRead(p))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");}
}
