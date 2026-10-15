using System.IO;
using UnityEngine;
public static class InspectOriginalPixels
{
    public static object Main()
    {
        string folder="docs/development/original_animation_rework_20261015/";Directory.CreateDirectory(folder);
        foreach(string name in new[]{"player","tower_single","tower_multi","tower_support"})
        {
            var input=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(input,File.ReadAllBytes("Assets/Art/ScreenTests/20260930/Sprites/"+name+".png"));
            var image=new Texture2D(input.width*12,input.height*12,TextureFormat.RGBA32,false);
            var pixels=input.GetPixels32();
            for(int y=0;y<image.height;y++)for(int x=0;x<image.width;x++){var p=pixels[(y/12)*input.width+x/12];image.SetPixel(x,y,p.a>127?p:new Color32(35,40,46,255));}
            image.Apply();File.WriteAllBytes(folder+name+"_original_12x.png",image.EncodeToPNG());
            Object.DestroyImmediate(input);Object.DestroyImmediate(image);
        }
        return "Original nearest12x inspection images; no asset edits";
    }
}
