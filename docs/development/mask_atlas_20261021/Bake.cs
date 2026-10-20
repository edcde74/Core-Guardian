using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Profiling;
public static class CGAtlasBake
{
    const string Folder="Assets/Resources/CoreGuardian/MaskAtlas";
    public static object Attack()=>Build(AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/CoreGuardian/OriginalWalk/Attack"}).Select(AssetDatabase.GUIDToAssetPath).ToArray(),"attack_bake");
    public static object Expand(){var observed=Directory.GetFiles("docs/development/mask_atlas_20261021/pixels","*_sources.txt").SelectMany(File.ReadAllLines);var dirs=new[]{"Assets/Resources/CoreGuardian/PlayerFacing","Assets/Resources/CoreGuardian/VerticalWalk/Attack","Assets/Resources/CoreGuardian/VerticalWalk/Defense","Assets/Resources/CoreGuardian/VerticalWalk/Buff","Assets/Resources/CoreGuardian/VerticalWalk/Boss"};var states=AssetDatabase.FindAssets("t:Texture2D",dirs).Select(AssetDatabase.GUIDToAssetPath);return Build(observed.Concat(states).Distinct().OrderBy(x=>x).ToArray(),"expanded_bake");}
    static object Build(string[] paths,string label)
    {
        var inputs=paths.Select(AssetDatabase.LoadAssetAtPath<Texture2D>).ToArray();
        if(inputs.Any(x=>x==null))throw new Exception("Unknown source asset");
        var start=EditorApplication.timeSinceStartup;
        var manifest=CoreGuardianMaskAtlasBuilder.Build(inputs,Folder,2048);
        var entries=(CoreGuardianMaskAtlas.Entry[])typeof(CoreGuardianMaskAtlas).GetField("entries",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(manifest);var pages=entries.Select(e=>e.page).Distinct().ToArray();
        var result=new{sources=paths,sourceInfo=inputs.Select(t=>new{path=AssetDatabase.GetAssetPath(t),t.width,t.height,format=t.format.ToString(),filter=t.filterMode.ToString(),wrap=t.wrapMode.ToString(),mips=t.mipmapCount}).ToArray(),seconds=EditorApplication.timeSinceStartup-start,pages=pages.Select(p=>new{p.name,p.width,p.height,p.depth,bytes=Profiler.GetRuntimeMemorySizeLong(p)}).ToArray()};
        File.WriteAllText("docs/development/mask_atlas_20261021/"+label+".json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
