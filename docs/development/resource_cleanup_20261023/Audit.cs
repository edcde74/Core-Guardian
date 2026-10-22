// Read-only asset audit, compiled in memory outside Assets.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
public static class CGResourceAudit
{
    const string Root="docs/development/resource_cleanup_20261023/";
    public static object Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)throw new Exception("Editor busy");
        var paths=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!AssetDatabase.IsValidFolder(p)).ToArray();
        var extensions=new HashSet<string>(new[]{".png",".jpg",".jpeg",".psd",".psb",".tga",".tif",".tiff",".bmp",".gif",".exr",".hdr",".webp"},StringComparer.OrdinalIgnoreCase);
        var images=paths.Where(p=>extensions.Contains(Path.GetExtension(p))).ToArray();var imageSet=new HashSet<string>(images);
        var links=images.ToDictionary(p=>p,p=>new List<string>());
        var edges=new List<object>();
        foreach(var p in paths.Where(p=>!imageSet.Contains(p)&&!p.EndsWith(".cs")&&!p.EndsWith(".shader")))
        {
            var dependencies=AssetDatabase.GetDependencies(p,false).Where(d=>imageSet.Contains(d)&&d!=p).Distinct().ToArray();
            if(dependencies.Length>0)edges.Add(new{asset=p,images=dependencies});
            foreach(var d in dependencies)links[d].Add(p);
        }
        var live=new HashSet<string>();
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            foreach(var obj in EditorUtility.CollectDependencies(scene.GetRootGameObjects())){var p=AssetDatabase.GetAssetPath(obj);if(imageSet.Contains(p))live.Add(p);}
        }
        var rows=images.Select(p=>{var importer=AssetImporter.GetAtPath(p) as TextureImporter;return new{path=p,guid=AssetDatabase.AssetPathToGUID(p),bytes=new FileInfo(p).Length,sha256=Hash(p),metaHash=Hash(p+".meta"),spriteMode=importer==null?-1:(int)importer.spriteImportMode,sprites=AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().Select(s=>s.name).ToArray(),live=live.Contains(p),referencedBy=links[p].ToArray()};}).ToArray();
        File.WriteAllText(Root+"inventory.json",JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,scenes=Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Select(i=>{var s=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);return new{s.path,s.isDirty};}),images=rows,edges},Formatting.Indented));
        return new{imageFiles=images.Length,spriteFiles=rows.Count(r=>r.sprites.Length>0),liveImages=live.Count,serializedLinked=rows.Count(r=>r.referencedBy.Length>0),withoutSerializedReference=rows.Count(r=>!r.live&&r.referencedBy.Length==0),bytes=rows.Sum(r=>r.bytes)};
    }
    static string Hash(string path){if(!File.Exists(path))return null;using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","");}
}
