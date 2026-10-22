// Read-only inventory, compiled outside Assets through the official Unity command.
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
public static class CGDirectoryInventory
{
    public static object Capture()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)throw new Exception("Busy");
        var rows=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!AssetDatabase.IsValidFolder(p)).Select(p=>{
            var script=AssetDatabase.LoadAssetAtPath<MonoScript>(p);var type=script==null?null:script.GetClass();
            int resource=p.LastIndexOf("/Resources/",StringComparison.Ordinal);string key=resource<0?null:p.Substring(resource+11);if(key!=null)key=key.Substring(0,key.Length-Path.GetExtension(key).Length);
            return new{path=p,guid=AssetDatabase.AssetPathToGUID(p),hash=Hash(p),metaHash=Hash(p+".meta"),bytes=new FileInfo(p).Length,resourceKey=key,className=type==null?null:type.FullName,assembly=type==null?null:type.Assembly.GetName().Name,compileAssembly=p.EndsWith(".cs")?UnityEditor.Compilation.CompilationPipeline.GetAssemblyNameFromScriptPath(p):null,dependencies=AssetDatabase.GetDependencies(p,false).Where(d=>d!=p).Select(AssetDatabase.AssetPathToGUID).Where(g=>!string.IsNullOrEmpty(g)).Distinct().OrderBy(g=>g).ToArray()};}).ToArray();
        File.WriteAllText("docs/development/directory_reorganization_20261023/inventory_before.json",JsonConvert.SerializeObject(rows,Formatting.Indented));return new{files=rows.Length,resources=rows.Count(r=>r.resourceKey!=null),scripts=rows.Count(r=>r.path.EndsWith(".cs")),dirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty};
    }
    static string Hash(string p){if(!File.Exists(p))return null;using(var sha=System.Security.Cryptography.SHA256.Create())using(var stream=File.OpenRead(p))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");}
}
