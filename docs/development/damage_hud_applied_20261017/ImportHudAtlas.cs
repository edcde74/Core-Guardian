using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class ImportHudAtlas
{
    public static object Main()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new Exception("Idle Editor required");
        const string path="Assets/Resources/CoreGuardian/Hud/DamageRoles.png";
        if(File.Exists(path))throw new Exception("Refusing to overwrite existing art");
        var raw=new Texture2D(2,2);raw.LoadImage(File.ReadAllBytes("docs/development/damage_hud_applied_20261017/source_atlas.png"));
        var names=new[]{"DamageRange","DamageSingle","DamageEngineer","DamageHandle"};
        var rects=new SpriteRect[4];
        try
        {
            for(int i=0;i<4;i++)
            {
                int x0=(i%2)*raw.width/2,y0=(i<2?1:0)*raw.height/2;
                int minX=raw.width,minY=raw.height,maxX=-1,maxY=-1;
                for(int y=y0;y<y0+raw.height/2;y++)for(int x=x0;x<x0+raw.width/2;x++)
                    if(raw.GetPixel(x,y).a>.15f){minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);}
                if(maxX<minX)throw new Exception("Empty quadrant "+i);
                rects[i]=new SpriteRect{name=names[i],rect=new Rect(minX,minY,maxX-minX+1,maxY-minY+1),pivot=new Vector2(.5f,.5f),alignment=SpriteAlignment.Center,spriteID=GUID.Generate()};
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(raw);}
        File.Copy("docs/development/damage_hud_applied_20261017/source_atlas.png",path);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.maxTextureSize=512;
        importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
        var factories=new SpriteDataProviderFactories();factories.Init();
        var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
        var capability=provider.GetDataProvider<ISpriteFrameEditCapability>();
        if(capability==null||!capability.GetEditCapability().HasCapability(EEditCapability.CreateAndDeleteSprite))throw new Exception("Sprite slicing capability unavailable");
        var ids=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if(ids==null)throw new Exception("Sprite name ID provider unavailable");
        provider.SetSpriteRects(rects);ids.SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
        provider.Apply();importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Select(s=>new{name=s.name,rect=s.rect.ToString()}).ToArray();
    }
}
