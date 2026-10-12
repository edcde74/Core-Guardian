using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using Newtonsoft.Json;

public static class InstallCombatArt
{
    const string Path="Assets/Resources/CoreGuardian/Art/CombatB.png";
    public static object Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play before import");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(Path);
        if(importer==null)throw new Exception("Combat source missing");
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency=true;importer.isReadable=true;importer.npotScale=TextureImporterNPOTScale.None;
        importer.maxTextureSize=2048;importer.spritePixelsPerUnit=128;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Path);
        importer.GetSourceTextureWidthAndHeight(out var width,out var height);
        if(texture.width!=width||texture.height!=height)throw new Exception("Source/import sizes differ");
        var factory=new SpriteDataProviderFactories();factory.Init();
        var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
        var capability=provider.GetDataProvider<ISpriteFrameEditCapability>();
        if(capability==null||!capability.GetEditCapability().HasCapability(EEditCapability.CreateAndDeleteSprite))throw new Exception("Sprite slicing not supported");
        var old=provider.GetSpriteRects();var rects=new SpriteRect[9];var pixels=texture.GetPixels32();
        for(int i=0;i<9;i++)
        {
            int col=i%3,row=2-i/3,x0=col*width/3,x1=(col+1)*width/3,y0=row*height/3,y1=(row+1)*height/3;
            int minX=x1,minY=y1,maxX=x0,maxY=y0,count=0;
            for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(pixels[y*width+x].a>=64){minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);count++;}
            if(count==0)throw new Exception("Empty cell "+i);
            minX=Math.Max(x0,minX-2);minY=Math.Max(y0,minY-2);maxX=Math.Min(x1-1,maxX+2);maxY=Math.Min(y1-1,maxY+2);
            var name=CoreGuardianCombatArt.Names[i];var previous=old.FirstOrDefault(r=>r.name==name);
            rects[i]=new SpriteRect{name=name,rect=new Rect(minX,minY,maxX-minX+1,maxY-minY+1),pivot=new Vector2(.5f,.5f),alignment=SpriteAlignment.Center,spriteID=previous==null?GUID.Generate():previous.spriteID};
        }
        provider.SetSpriteRects(rects);
        var ids=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if(ids==null)throw new Exception("Sprite file ID provider unavailable");
        ids.SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
        provider.Apply();importer.SaveAndReimport();
        importer.isReadable=false;importer.SaveAndReimport();AssetDatabase.SaveAssets();
        return Verify();
    }
    public static object Verify()
    {
        var sprites=AssetDatabase.LoadAllAssetsAtPath(Path).OfType<Sprite>().ToArray();
        if(sprites.Length!=9||CoreGuardianCombatArt.Names.Any(n=>!sprites.Any(s=>s.name==n)))throw new Exception("Missing sprites");
        var importer=(TextureImporter)AssetImporter.GetAtPath(Path);
        if(importer.filterMode!=FilterMode.Point||importer.mipmapEnabled||importer.textureCompression!=TextureImporterCompression.Uncompressed)throw new Exception("Invalid pixel sprite import");
        var report=new{success=true,path=Path,sprites=sprites.Select(s=>new{name=s.name,rect=new[]{s.rect.x,s.rect.y,s.rect.width,s.rect.height},pivot=new[]{s.pivot.x,s.pivot.y}}).ToArray(),ppu=importer.spritePixelsPerUnit,filter=importer.filterMode.ToString(),compressed=false,mipmaps=false};
        File.WriteAllText("docs/development/combat_art_import_20261010.json",JsonConvert.SerializeObject(report,Formatting.Indented));return report;
    }
}
