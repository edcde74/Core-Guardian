using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class BuildWalkIdleLayers
{
 const string Output="Assets/Resources/CoreGuardian/OriginalWalk/";
 static int Leg(int role,int x,int y)
 {
  if(role==0){if(y>=42&&x>=12&&x<=24)return 1;if((y>=43&&x>=26&&x<=32)||(y>=47&&x>=26&&x<=39))return 2;}
  if(role==1){if(y>=31&&x>=15&&x<=20)return 1;if((y>=29&&x>=21&&x<=26)||(y>=35&&x>=21&&x<=32))return 2;}
  if(role==2){if((y>=61&&x>=8&&x<=17)||(y>=68&&x>=5&&x<=17))return 1;if((y>=61&&x>=20&&x<=26)||(y>=65&&x>=20&&x<=31))return 2;}
  if(role==3){if((y>=50&&x>=23&&x<=34)||(y>=57&&x>=23&&x<=38))return 1;if((y>=51&&x>=45&&x<=54)||(y>=57&&x>=45&&x<=59))return 2;}
  return 0;
 }
 static void Save(string path,Color32[] pixels,int width,int height,Sprite source)
 {
  Directory.CreateDirectory(Path.GetDirectoryName(path));var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
  AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=source.pixelsPerUnit;
  var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=source.pivot/new Vector2(width,height);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
  importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=false;importer.SaveAndReimport();
 }
 public static object Main()
 {
  string[] roles={"Attack","Defense","Buff","Boss","Player"};
  string[] paths={"Assets/Art/ScreenTests/20260930/Revision2/Sprites/enemy_attack_v2.png","Assets/Art/ScreenTests/20260930/Revision2/Sprites/enemy_defense_v2.png","Assets/Art/ScreenTests/20260930/Revision2/Sprites/enemy_buff_v2.png","Assets/Resources/CoreGuardian/Art/BossEye/BossEye.png","Assets/Art/ScreenTests/20260930/Sprites/player.png"};
  int[,] knees={{49,51},{36,36},{67,69},{59,60}};
  var checks=new System.Collections.Generic.List<object>();
  for(int i=0;i<5;i++)
  {
   var source=i==3?AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/CoreGuardian/Animation/Boss/BossFeetCenteredV2.asset"):AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]);
   var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(texture,File.ReadAllBytes(paths[i]));var pixels=texture.GetPixels32();
   string[] layers=i<4?new[]{"Body","LeftUpper","LeftLower","RightUpper","RightLower"}:new[]{"Feet","Upper"};
   var split=layers.Select(_=>new Color32[pixels.Length]).ToArray();
   for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)
   {
    int top=texture.height-1-y,index=y*texture.width+x;
    if(i==4){split[top<=32?1:0][index]=pixels[index];if(top==32)split[0][index]=pixels[index];continue;}
    int leg=Leg(i,x,top);
    if(leg==0)split[0][index]=pixels[index];
    else
    {
     int knee=knees[i,leg-1];int upper=leg==1?1:3,lower=upper+1;
     split[top<knee?upper:lower][index]=pixels[index];
     // Source-pixel overlap covers rotating joint edges; the rest composite is unchanged.
     if(top>=knee-1&&top<=knee+1){split[upper][index]=pixels[index];split[lower][index]=pixels[index];}
     if(Leg(i,x,top-2)==0)split[0][index]=pixels[index];
    }
   }
   int difference=0;for(int p=0;p<pixels.Length;p++){var composed=new Color32();foreach(var layer in split)if(layer[p].a>0)composed=layer[p];if(pixels[p].a>0&&!composed.Equals(pixels[p]))difference++;}
   if(difference!=0)throw new Exception(roles[i]+" source pixel mismatch");
   for(int j=0;j<layers.Length;j++)Save((i==4?"Assets/Resources/CoreGuardian/OriginalIdle/Player/":Output+roles[i]+"/")+layers[j]+".png",split[j],texture.width,texture.height,source);
   checks.Add(new{role=roles[i],source=paths[i],difference,layers=layers.Select((name,n)=>new{name,pixels=split[n].Count(p=>p.a>0)}).ToArray()});UnityEngine.Object.DestroyImmediate(texture);
  }
  AssetDatabase.Refresh();string json=Newtonsoft.Json.JsonConvert.SerializeObject(checks,Newtonsoft.Json.Formatting.Indented);File.WriteAllText("docs/development/original_walk_idle_20261015/source_checks.json",json);return json;
 }
}
