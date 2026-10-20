using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Explicit Editor bake only. No build hooks or global SpriteAtlas binding.
public static class CoreGuardianMaskAtlasBuilder
{
    const int Padding=4;
    sealed class Page
    {
        public int x,y,rowHeight,width,height;
        public readonly List<(Texture2D source,RectInt rect)> entries=new List<(Texture2D,RectInt)>();
    }
    public static CoreGuardianMaskAtlas Build(Texture2D[] sources,string folder,int maxSize=2048)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Bake only outside Play mode");
        if(string.IsNullOrEmpty(folder)||!folder.StartsWith("Assets/",StringComparison.Ordinal)||folder.Contains(".."))throw new ArgumentException("Use a dedicated Assets subfolder");
        if(maxSize<16||maxSize>SystemInfo.maxTextureSize||!Mathf.IsPowerOfTwo(maxSize))throw new ArgumentOutOfRangeException(nameof(maxSize));
        var inputs=sources.Where(t=>t!=null).Distinct().OrderBy(t=>AssetDatabase.GetAssetPath(t),StringComparer.Ordinal).ThenBy(t=>t.name,StringComparer.Ordinal).ToArray();
        if(inputs.Length==0)throw new ArgumentException("No mask input textures");
        foreach(var t in inputs)
            if(t.width+Padding*2>maxSize||t.height+Padding*2>maxSize)throw new InvalidOperationException("Source exceeds page: "+t.name);
        var pages=new List<Page>();
        foreach(var t in inputs)
        {
            Page p=pages.Count>0?pages[pages.Count-1]:null;
            int w=t.width+Padding*2,h=t.height+Padding*2;
            if(p!=null&&p.x+w>maxSize){p.x=0;p.y+=p.rowHeight;p.rowHeight=0;}
            if(p==null||p.y+h>maxSize){p=new Page();pages.Add(p);}
            p.entries.Add((t,new RectInt(p.x+Padding,p.y+Padding,t.width,t.height)));
            p.x+=w;p.rowHeight=Mathf.Max(p.rowHeight,h);p.width=Mathf.Max(p.width,p.x);p.height=Mathf.Max(p.height,p.y+h);
        }
        EnsureFolder(folder);
        var records=new List<CoreGuardianMaskAtlas.Entry>();
        for(int i=0;i<pages.Count;i++)
        {
            var p=pages[i];int width=Mathf.NextPowerOfTwo(p.width),height=Mathf.NextPowerOfTwo(p.height);
            var data=new Color32[width*height];
            foreach(var e in p.entries)
            {
                var alpha=ReadPixels(e.source);
                for(int y=-Padding;y<e.source.height+Padding;y++)for(int x=-Padding;x<e.source.width+Padding;x++)
                {
                    byte a=alpha[Mathf.Clamp(y,0,e.source.height-1)*e.source.width+Mathf.Clamp(x,0,e.source.width-1)].a;
                    data[(e.rect.y+y)*width+e.rect.x+x]=new Color32(255,255,255,a);
                }
            }
            string path=folder+"/Page"+i+".png";
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false,true);
            try{texture.SetPixels32(data);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}
            finally{UnityEngine.Object.DestroyImmediate(texture);}
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.sRGBTexture=false;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=false;
            importer.mipmapEnabled=false;importer.isReadable=false;importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;
            importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=maxSize;importer.SaveAndReimport();
            var page=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var imported=ReadPixels(page);
            for(int pixel=0;pixel<data.Length;pixel++)if(imported[pixel].a!=data[pixel].a)throw new InvalidOperationException("Imported alpha differs: "+path+" pixel "+pixel);
            foreach(var e in p.entries)records.Add(new CoreGuardianMaskAtlas.Entry{source=e.source,page=page,rect=e.rect});
        }
        string manifestPath=folder+"/Manifest.asset";
        var manifest=AssetDatabase.LoadAssetAtPath<CoreGuardianMaskAtlas>(manifestPath);
        if(manifest==null){manifest=ScriptableObject.CreateInstance<CoreGuardianMaskAtlas>();AssetDatabase.CreateAsset(manifest,manifestPath);}
        manifest.Initialize(records.ToArray(),string.Join(";",inputs.Select(t=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(t))+":"+AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(t)))));
        EditorUtility.SetDirty(manifest);AssetDatabase.SaveAssetIfDirty(manifest);return manifest;
    }
    static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        string parent=path.Substring(0,path.LastIndexOf('/'));EnsureFolder(parent);AssetDatabase.CreateFolder(parent,path.Substring(parent.Length+1));
    }
    public static Color32[] ReadPixels(Texture2D source)
    {
        var old=RenderTexture.active;var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
        var copy=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false,true);
        try{Graphics.Blit(source,rt);RenderTexture.active=rt;copy.ReadPixels(new Rect(0,0,source.width,source.height),0,0);copy.Apply();return copy.GetPixels32();}
        finally{RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(copy);}
    }
}
