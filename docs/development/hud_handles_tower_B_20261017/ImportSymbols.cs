using System;
using System.IO;
using UnityEngine;
using UnityEditor;
public static class ImportTowerBSymbols
{
    public static object Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Import only while stopped");
        var src=new Texture2D(2,2,TextureFormat.RGBA32,false);src.LoadImage(File.ReadAllBytes("docs/development/hud_handles_tower_B_20261017/symbols_source.png"));
        string[] names={"TowerAttackCategory","TowerSingle","TowerMulti","TowerSupport"};
        for(int i=0;i<4;i++)
        {
            int x0=i%2*src.width/2,x1=(i%2+1)*src.width/2,y0=(1-i/2)*src.height/2,y1=(2-i/2)*src.height/2;
            int l=x1,r=x0,b=y1,t=y0;
            for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(src.GetPixel(x,y).a>.2f){l=Math.Min(l,x);r=Math.Max(r,x);b=Math.Min(b,y);t=Math.Max(t,y);}
            if(r<l)throw new Exception("Empty icon "+names[i]);
            var dst=new Texture2D(48,48,TextureFormat.RGBA32,false);dst.SetPixels(new Color[2304]);
            float scale=44f/Math.Max(r-l+1,t-b+1);int w=Math.Max(1,Mathf.RoundToInt((r-l+1)*scale)),h=Math.Max(1,Mathf.RoundToInt((t-b+1)*scale));
            for(int y=0;y<h;y++)for(int x=0;x<w;x++){var c=src.GetPixel(l+Math.Min(r-l,(int)((x+.5f)/scale)),b+Math.Min(t-b,(int)((y+.5f)/scale)));if(c.a<.2f)c=Color.clear;dst.SetPixel((48-w)/2+x,(48-h)/2+y,c);}
            dst.Apply();string path="Assets/Resources/CoreGuardian/Hud/"+names[i]+".png";File.WriteAllBytes(path,dst.EncodeToPNG());UnityEngine.Object.DestroyImmediate(dst);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
        }
        UnityEngine.Object.DestroyImmediate(src);return new{imported=names,size="48x48",filter="Point",source="Generated atlas; alpha crop and nearest pixel normalization via Unity API"};
    }
}

