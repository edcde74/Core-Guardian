using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class BuildOriginalLayers
{
 const string Folder="Assets/Resources/CoreGuardian/OriginalMotion/";
 public static object Main()
 {
  string[] files={"player","tower_single","tower_multi","tower_support"};
  string[] roles={"Player","Single","Multi","Support"};
  var checks=new System.Collections.Generic.List<object>();
  for(int i=0;i<4;i++)
  {
   var path="Assets/Art/ScreenTests/20260930/Sprites/"+files[i]+".png";
   var source=AssetDatabase.LoadAssetAtPath<Sprite>(path);
   var t=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(t,File.ReadAllBytes(path));
   var pixels=t.GetPixels32();var fixedPixels=new Color32[pixels.Length];var movingPixels=new Color32[pixels.Length];
   for(int y=0;y<t.height;y++)for(int x=0;x<t.width;x++)
   {
    int top=t.height-1-y,index=y*t.width+x;
    bool moving=i==0?((top>=21&&top<=31&&x<=10)||(top>=26&&top<=32&&x<=4)):i==1||i==2?top<31:x>=16&&x<=24&&top>=7&&top<=25;
    if(moving)movingPixels[index]=pixels[index];else fixedPixels[index]=pixels[index];
   }
   int difference=0;
   for(int p=0;p<pixels.Length;p++)if(!(pixels[p].Equals(movingPixels[p].a!=0?movingPixels[p]:fixedPixels[p]))&&pixels[p].a!=0)difference++;
   if(difference!=0)throw new Exception("Source pixel mismatch: "+roles[i]);
   Directory.CreateDirectory(Folder+roles[i]);
   for(int j=0;j<2;j++)
   {
    string output=Folder+roles[i]+"/"+(j==0?"Fixed":"Moving")+".png";
    var layer=new Texture2D(t.width,t.height,TextureFormat.RGBA32,false);layer.SetPixels32(j==0?fixedPixels:movingPixels);layer.Apply();
    File.WriteAllBytes(output,layer.EncodeToPNG());UnityEngine.Object.DestroyImmediate(layer);
    AssetDatabase.ImportAsset(output);
    var importer=(TextureImporter)AssetImporter.GetAtPath(output);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
    importer.spritePixelsPerUnit=source.pixelsPerUnit;importer.spritePivot=source.pivot/new Vector2(t.width,t.height);
    var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=importer.spritePivot;settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
    importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=false;importer.SaveAndReimport();
   }
   checks.Add(new{role=roles[i],difference,source=path,width=t.width,height=t.height,ppu=source.pixelsPerUnit,pivot=source.pivot.ToString()});
   UnityEngine.Object.DestroyImmediate(t);
  }
  AssetDatabase.Refresh();return Newtonsoft.Json.JsonConvert.SerializeObject(checks);
 }
}
