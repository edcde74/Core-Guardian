using System;
using System.IO;
using UnityEngine;
public static class PreviewAnimationAssets
{
    static Texture2D Read(string p){var t=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(t,File.ReadAllBytes(p));return t;}
    public static object Main()
    {
        string[] roles={"Attack","Defense","Buff","Boss"};
        string[] refs={"Assets/Art/ScreenTests/20260930/Revision2/Sprites/enemy_attack_v2.png","Assets/Art/ScreenTests/20260930/Revision2/Sprites/enemy_defense_v2.png","Assets/Art/ScreenTests/20260930/Revision2/Sprites/enemy_buff_v2.png","Assets/Resources/CoreGuardian/Art/BossEye/BossEye.png"};
        var board=new Texture2D(96*5*4,112*4*4,TextureFormat.RGBA32,false);
        for(int y=0;y<board.height;y++)for(int x=0;x<board.width;x++)board.SetPixel(x,y,new Color32(35,40,46,255));
        for(int row=0;row<4;row++)
        {
            var atlas=Read("Assets/Resources/CoreGuardian/Animation/"+roles[row]+"/atlas.png");
            var original=Read(refs[row]);
            int bottom=original.height;
            for(int oy=0;oy<original.height;oy++)for(int ox=0;ox<original.width;ox++)if(original.GetPixel(ox,oy).a>=.5f)bottom=Math.Min(bottom,oy);
            int left=original.width,right=0;
            for(int oy=bottom;oy<Math.Min(original.height,bottom+(row==3?8:Mathf.CeilToInt(original.height*.18f)));oy++)for(int ox=0;ox<original.width;ox++)if(original.GetPixel(ox,oy).a>=.5f){left=Math.Min(left,ox);right=Math.Max(right,ox);}
            int center=(left+right+1)/2;
            for(int col=0;col<5;col++)for(int y=0;y<112;y++)for(int x=0;x<96;x++)
            {
                Color pixel=Color.clear;
                if(col>0)pixel=atlas.GetPixel(x,(4-col)*112+y);
                else {int sx=x-48+center,sy=y-8+bottom;if(sx>=0&&sx<original.width&&sy>=0&&sy<original.height)pixel=original.GetPixel(sx,sy);}
                if(y==7&&pixel.a<.5f)pixel=new Color32(90,115,120,255);
                if(pixel.a<.5f)continue;
                for(int iy=0;iy<4;iy++)for(int ix=0;ix<4;ix++)board.SetPixel((col*96+x)*4+ix,((3-row)*112+y)*4+iy,pixel);
            }
            UnityEngine.Object.DestroyImmediate(atlas);UnityEngine.Object.DestroyImmediate(original);
        }
        board.Apply();File.WriteAllBytes("docs/art/20261015_Animation/comparison_4x.png",board.EncodeToPNG());UnityEngine.Object.DestroyImmediate(board);
        // Contact sheet / animation source images use the same unfiltered pixel scale.
        return "comparison_4x.png: rows Attack/Defense/Buff/Boss; columns original/right/left/up/down";
    }
}
