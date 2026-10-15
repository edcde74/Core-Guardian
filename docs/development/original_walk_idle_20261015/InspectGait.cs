using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class InspectGait
{
 const string Folder="docs/development/original_walk_idle_20261015/";
 static Texture2D Read(string path){var t=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(t,File.ReadAllBytes(path));return t;}
 public static object Main()
 {
  Directory.CreateDirectory(Folder);
  string[] roles={"Attack","Defense","Buff","Boss"};
  string[] paths={"Assets/Art/ScreenTests/20260930/Revision2/Sprites/enemy_attack_v2.png","Assets/Art/ScreenTests/20260930/Revision2/Sprites/enemy_defense_v2.png","Assets/Art/ScreenTests/20260930/Revision2/Sprites/enemy_buff_v2.png","Assets/Resources/CoreGuardian/Art/BossEye/BossEye.png"};
  var bg=new Color32(35,40,46,255);var board=new Texture2D(96*5*3,112*4*3,TextureFormat.RGBA32,false);board.SetPixels32(Enumerable.Repeat(bg,board.width*board.height).ToArray());
  var results=new System.Collections.Generic.List<object>();
  for(int row=0;row<4;row++)
  {
   var original=Read(paths[row]);var enlarged=new Texture2D(original.width*8,original.height*8,TextureFormat.RGBA32,false);
   for(int y=0;y<enlarged.height;y++)for(int x=0;x<enlarged.width;x++){var p=original.GetPixel(x/8,y/8);enlarged.SetPixel(x,y,p.a>.01f?p:bg);}
   enlarged.Apply();File.WriteAllBytes(Folder+roles[row]+"_original_8x.png",enlarged.EncodeToPNG());UnityEngine.Object.DestroyImmediate(enlarged);
   var atlas=Read("Assets/Resources/CoreGuardian/Animation/"+roles[row]+"/atlas.png");
   var metadata=JsonUtility.FromJson<CoreGuardianUnitAnimation.AtlasInfo>(File.ReadAllText("Assets/Resources/CoreGuardian/Animation/"+roles[row]+"/frames.json"));
   var frameMetrics=new System.Collections.Generic.List<object>();
   for(int col=0;col<5;col++)
   {
    var info=col==0?null:metadata.frames.First(f=>f.name==roles[row]+"_1_"+(col-1));
    int minY=112,maxY=-1,minX=96,maxX=-1;
    for(int y=0;y<112;y++)for(int x=0;x<96;x++)
    {
     int sx=x-48+original.width/2,sy=y-8;
     var p=col==0?(sx>=0&&sx<original.width&&sy>=0&&sy<original.height?original.GetPixel(sx,sy):Color.clear):atlas.GetPixel(info.x+x,info.y+y);
     if(p.a>.5f){minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);for(int iy=0;iy<3;iy++)for(int ix=0;ix<3;ix++)board.SetPixel((col*96+x)*3+ix,((3-row)*112+y)*3+iy,p);}
    }
    frameMetrics.Add(new{frame=col-1,minY,maxY,minX,maxX});
   }
   results.Add(new{role=roles[row],frameMetrics});UnityEngine.Object.DestroyImmediate(original);UnityEngine.Object.DestroyImmediate(atlas);
  }
  board.Apply();File.WriteAllBytes(Folder+"before_original_vs_walk.png",board.EncodeToPNG());UnityEngine.Object.DestroyImmediate(board);
  string json=Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented);File.WriteAllText(Folder+"before_metrics.json",json);return json;
 }
}
