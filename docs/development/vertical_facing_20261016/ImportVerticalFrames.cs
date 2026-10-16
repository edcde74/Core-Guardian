using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;
public static class ImportVerticalFrames
{
 public static object Main()
 {
  int count=0;var factory=new SpriteDataProviderFactories();factory.Init();
  foreach(string role in new[]{"Attack","Defense","Buff","Boss"})foreach(string direction in new[]{"Up","Down"})for(int frame=0;frame<4;frame++)
  {
   string name=direction+"_"+frame;string path="Assets/Resources/CoreGuardian/VerticalWalk/"+role+"/"+name+".png";
   Directory.CreateDirectory(Path.GetDirectoryName(path));File.Copy("docs/development/vertical_facing_20261016/Frames/"+role+"/"+name+".png",path,true);AssetDatabase.ImportAsset(path);
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=false;importer.SaveAndReimport();
   var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();var capability=provider.GetDataProvider<ISpriteFrameEditCapability>();
   if(capability==null||!capability.GetEditCapability().HasCapability(EEditCapability.EditPivot))throw new Exception("Cannot edit sprite ground pivot: "+path);
   var rects=provider.GetSpriteRects();foreach(var rect in rects){rect.alignment=SpriteAlignment.Custom;rect.pivot=new Vector2(.5f,3f/112);}
   provider.SetSpriteRects(rects);provider.Apply();importer.SaveAndReimport();count++;
  }
  return new{imported=count,ppu=32,pivot="48,3 pixels",filter="Point",compression="None"};
 }
}
