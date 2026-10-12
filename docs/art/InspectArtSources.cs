using System;
using System.IO;
using System.Linq;
using UnityEngine;
public static class InspectArtSources {
 public static object Main() {
  return Directory.GetFiles("Assets/Art/ScreenTests/20260930/Source", "*.png").Select(path => {
   var t = new Texture2D(2,2);
   ImageConversion.LoadImage(t, File.ReadAllBytes(path));
   var p=t.GetPixels32();
   var result = new {path,t.width,t.height,transparent=p.Count(x=>x.a==0),solid=p.Count(x=>x.a>240),total=p.Length,corner=p[0].a};
   UnityEngine.Object.DestroyImmediate(t);
   return result;
  }).ToArray();
 }
}
