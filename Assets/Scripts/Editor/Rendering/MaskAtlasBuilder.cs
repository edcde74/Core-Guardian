using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Explicit Editor bake. Array layers keep source dimensions and UVs unchanged.
public static class CoreGuardianMaskAtlasBuilder
{
    public static CoreGuardianMaskAtlas Build(Texture2D[] sources,string folder,int maxSize=2048)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Bake only outside Play mode");
        if(string.IsNullOrEmpty(folder)||!folder.StartsWith("Assets/",StringComparison.Ordinal)||folder.Contains(".."))throw new ArgumentException("Use a dedicated Assets subfolder");
        if(maxSize<1||maxSize>SystemInfo.maxTextureSize)throw new ArgumentOutOfRangeException(nameof(maxSize));
        if(!SystemInfo.supports2DArrayTextures)throw new NotSupportedException("Texture arrays are unavailable");
        var inputs=sources.Where(t=>t!=null).Distinct().OrderBy(t=>AssetDatabase.GetAssetPath(t),StringComparer.Ordinal).ThenBy(t=>t.name,StringComparer.Ordinal).ToArray();
        if(inputs.Length==0)throw new ArgumentException("No mask input textures");
        foreach(var t in inputs)
        {
            if(t.width>maxSize||t.height>maxSize)throw new InvalidOperationException("Source exceeds page: "+t.name);
            if(t.filterMode!=FilterMode.Point||t.wrapModeU!=TextureWrapMode.Clamp||t.wrapModeV!=TextureWrapMode.Clamp||t.mipmapCount!=1)
                throw new InvalidOperationException("Only unmipped Point/Clamp input is supported: "+t.name);
        }
        EnsureFolder(folder);
        string signature="array-v1;"+string.Join(";",inputs.Select(t=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(t))+":"+AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(t))));
        string version=Hash128.Compute(signature).ToString();
        string manifestPath=folder+"/Manifest.asset";
        var manifest=AssetDatabase.LoadAssetAtPath<CoreGuardianMaskAtlas>(manifestPath);
        var snapshot=manifest!=null?UnityEngine.Object.Instantiate(manifest):null;
        var createdPaths=new List<string>();
        try
        {
        var records=new List<CoreGuardianMaskAtlas.Entry>();int index=0;
        foreach(var group in inputs.GroupBy(t=>(t.width,t.height)))
        {
            var textures=group.ToArray();
            for(int first=0;first<textures.Length;first+=256)
            {
                int count=Math.Min(256,textures.Length-first);
                var page=new Texture2DArray(group.Key.width,group.Key.height,count,TextureFormat.RGBA32,false,true)
                    {name="Page"+index,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
                try
                {
                    for(int layer=0;layer<count;layer++)
                    {
                        var pixels=ReadPixels(textures[first+layer]);
                        for(int p=0;p<pixels.Length;p++){byte a=pixels[p].a;pixels[p]=new Color32(255,255,255,a);}
                        page.SetPixels32(pixels,layer);
                    }
                    page.Apply(false,true);
                    for(int layer=0;layer<count;layer++)
                    {
                        var expected=ReadPixels(textures[first+layer]);var actual=ReadLayer(page,layer);
                        for(int p=0;p<expected.Length;p++)if(expected[p].a!=actual[p].a)throw new InvalidOperationException("Array alpha differs: "+textures[first+layer].name);
                    }
                    // Immutable content-addressed pages: a later failure cannot change old layers.
                    string path=folder+"/Page_"+version+"_"+index+".asset";
                    var saved=AssetDatabase.LoadAssetAtPath<Texture2DArray>(path);
                    if(saved==null){createdPaths.Add(path);AssetDatabase.CreateAsset(page,path);saved=page;page=null;}
                    else
                    {
                        if(saved.width!=page.width||saved.height!=page.height||saved.depth!=page.depth)throw new InvalidOperationException("Existing generated page has changed: "+path);
                        for(int layer=0;layer<count;layer++)
                        {
                            var expected=ReadLayer(page,layer);var actual=ReadLayer(saved,layer);
                            for(int p=0;p<expected.Length;p++)if(expected[p].a!=actual[p].a)throw new InvalidOperationException("Existing generated page alpha has changed: "+path);
                        }
                    }
                    for(int layer=0;layer<count;layer++)records.Add(new CoreGuardianMaskAtlas.Entry{source=textures[first+layer],page=saved,layer=layer,dependencyHash=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(textures[first+layer])).ToString()});
                    index++;
                }
                finally{if(page!=null)UnityEngine.Object.DestroyImmediate(page);}
            }
        }
        // Publish only after every layer has been validated. Roll back only this bake's new assets.
        if(manifest==null){manifest=ScriptableObject.CreateInstance<CoreGuardianMaskAtlas>();createdPaths.Add(manifestPath);AssetDatabase.CreateAsset(manifest,manifestPath);}
        manifest.Initialize(records.ToArray(),signature);
        EditorUtility.SetDirty(manifest);AssetDatabase.SaveAssetIfDirty(manifest);return manifest;
        }
        catch
        {
            if(snapshot!=null&&manifest!=null){EditorUtility.CopySerialized(snapshot,manifest);manifest.Invalidate();}
            foreach(string path in createdPaths)AssetDatabase.DeleteAsset(path);
            throw;
        }
        finally{if(snapshot!=null)UnityEngine.Object.DestroyImmediate(snapshot);}
    }
    static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        string parent=path.Substring(0,path.LastIndexOf('/'));EnsureFolder(parent);AssetDatabase.CreateFolder(parent,path.Substring(parent.Length+1));
    }
    public static Color32[] ReadPixels(Texture2D source)=>Read(source,null,0);
    public static Color32[] ReadLayer(Texture2DArray source,int layer)=>Read(source,source,layer);
    static Color32[] Read(Texture source,Texture2DArray array,int layer)
    {
        var old=RenderTexture.active;var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
        var copy=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false,true);
        try
        {
            if(array==null)Graphics.Blit(source,rt);else Graphics.CopyTexture(array,layer,0,rt,0,0);
            RenderTexture.active=rt;copy.ReadPixels(new Rect(0,0,source.width,source.height),0,0);copy.Apply();return copy.GetPixels32();
        }
        finally{RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(copy);}
    }
}

// Only invalidate transient caches; never generate assets or change source importers here.
sealed class CoreGuardianMaskAtlasSourceInvalidation : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] movedFrom)
    {
        if(imported.Length+deleted.Length+moved.Length+movedFrom.Length==0)return;
        foreach(var atlas in Resources.FindObjectsOfTypeAll<CoreGuardianMaskAtlas>())atlas.Invalidate();
    }
}
