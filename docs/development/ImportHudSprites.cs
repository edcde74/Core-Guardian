using System;
using System.IO;
using UnityEngine;
using UnityEditor;
public static class ImportHudSprites
{
    public static string Main()
    {
        string folder="Assets/Resources/CoreGuardian/Hud";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        var source=Load("Assets/Art/Hud/20261013/HudAtlasSource.png");
        string[] names={"Panel","Button","Hover","Disabled","Core","Wave","Gold","Crystal","Install","Play","Upgrade","Cancel","Restart","Speed","Damage","Augment"};
        for(int row=0;row<4;row++)for(int col=0;col<4;col++){
            int x=col*source.width/4,y=(3-row)*source.height/4;int w=(col+1)*source.width/4-x,h=(4-row)*source.height/4-y;
            Export(source,new RectInt(x,y,w,h),folder+"/"+names[row*4+col]+".png",row==0?128:48,row==0?96:48,row==0?new Vector4(32,28,32,28):Vector4.zero);
        }
        var back=Load("Assets/Art/Hud/20261013/CardBackSource.png");Export(back,new RectInt(0,0,back.width,back.height),folder+"/CardBack.png",160,240,Vector4.zero);
        var tiers=Load("Assets/Art/Hud/20261013/CardBackTiersSource.png");string[] grades={"Silver","Gold","Platinum"};
        for(int i=0;i<3;i++){int x=i*tiers.width/3,w=(i+1)*tiers.width/3-x;Export(tiers,new RectInt(x,0,w,tiers.height),folder+"/CardBack"+grades[i]+".png",160,240,Vector4.zero);}
        AssetDatabase.SaveAssets();return "20 HUD sprites imported Point/uncompressed/no mipmaps; no scene changes";
    }
    static Texture2D Load(string path){AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.isReadable=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);}
    static void Export(Texture2D source,RectInt cell,string path,int width,int height,Vector4 border)
    {
        int left=cell.xMax,right=cell.x,lower=cell.yMax,upper=cell.y;
        for(int y=cell.y;y<cell.yMax;y++)for(int x=cell.x;x<cell.xMax;x++)if(source.GetPixel(x,y).a>.25f){left=Math.Min(left,x);right=Math.Max(right,x);lower=Math.Min(lower,y);upper=Math.Max(upper,y);}
        if(right<=left||upper<=lower)throw new Exception("Empty atlas cell "+path);
        var output=new Texture2D(width,height,TextureFormat.RGBA32,false);
        // Nearest-neighbor normalization, including original alpha; no invented bitmap redraw.
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)output.SetPixel(x,y,source.GetPixel(left+x*(right-left+1)/width,lower+y*(upper-lower+1)/height));
        output.Apply();File.WriteAllBytes(path,output.EncodeToPNG());UnityEngine.Object.DestroyImmediate(output);
        AssetDatabase.ImportAsset(path);var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.spritePixelsPerUnit=100;imp.spriteBorder=border;var settings=new TextureImporterSettings();imp.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;imp.SetTextureSettings(settings);imp.filterMode=FilterMode.Point;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.mipmapEnabled=false;imp.alphaIsTransparency=true;imp.SaveAndReimport();
    }
}
