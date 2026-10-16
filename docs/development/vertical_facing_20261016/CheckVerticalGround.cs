using System;using System.IO;using System.Linq;using UnityEngine;
public static class CheckVerticalGround
{
 public static object Main()
 {
  var output=new System.Collections.Generic.List<object>();
  foreach(var role in new[]{"Attack","Defense","Buff","Boss"})foreach(var dir in new[]{"Up","Down"})for(int f=0;f<4;f++)
  {
   var t=new Texture2D(2,2);ImageConversion.LoadImage(t,File.ReadAllBytes("docs/development/vertical_facing_20261016/Frames/"+role+"/"+dir+"_"+f+".png"));
   int bottom=t.height;for(int y=0;y<t.height;y++)for(int x=0;x<t.width;x++)if(t.GetPixel(x,y).a>.7f)bottom=Math.Min(bottom,y);
   int minX=t.width,maxX=-1;for(int y=bottom;y<Math.Min(bottom+5,t.height);y++)for(int x=0;x<t.width;x++)if(t.GetPixel(x,y).a>.7f){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);}
   output.Add(new{role,dir,f,bottom,footX=(minX+maxX+1)*.5f-48,minX,maxX});UnityEngine.Object.DestroyImmediate(t);
  }
  File.WriteAllText("docs/development/vertical_facing_20261016/feet.json",Newtonsoft.Json.JsonConvert.SerializeObject(output,Newtonsoft.Json.Formatting.Indented));return output;
 }
}
