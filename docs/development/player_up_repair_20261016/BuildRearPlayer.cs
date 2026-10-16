using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

// Reassemble only the rear pose, from the original character pixels. No new colours or source edits.
public static class BuildRearPlayer
{
    const int W=36,H=48;
    const string Folder="docs/development/player_up_repair_20261016/";
    static Color32[] source;
    static Color32 At(int x,int top)=>source[(H-1-top)*W+x];
    static void Put(Color32[] a,int x,int top,Color32 c){if(x>=0&&x<W&&top>=0&&top<H&&c.a>0)a[(H-1-top)*W+x]=c;}
    public static object Main(bool apply=false)
    {
        const string sourcePath="Assets/Art/ScreenTests/20260930/Sprites/player.png";
        var originalBytes=File.ReadAllBytes(sourcePath);
        var texture=new Texture2D(2,2);texture.LoadImage(originalBytes);source=texture.GetPixels32();UnityEngine.Object.DestroyImmediate(texture);
        var body=new Color32[W*H];var arm=new Color32[W*H];var left=new Color32[W*H];var right=new Color32[W*H];var pack=new Color32[W*H];
        for(int top=0;top<H;top++)for(int x=0;x<W;x++)
        {
            var c=At(x,top);if(c.a==0)continue;
            bool tool=(top>=21&&top<=31&&x<=10)||(top>=26&&top<=32&&x<=4);
            // The old x>=26 mask also moved the player's bare forearm into the middle of the back.
            bool backpack=top<16?x>=23:top<=21?x>=28:top<=30&&x>=29;
            if(tool){Put(arm,35-x,top,c);continue;}
            if(top>=33){Put(x<=18?right:left,35-x,top,c);continue;}
            if(backpack){Put(pack,x-11,top,c);continue;}
            if(top<17&&x<=22)continue;
            Put(body,35-x,top,c);
        }
        for(int i=0;i<body.Length;i++)if(pack[i].a>0)body[i]=pack[i];
        // Preserve the original cap outline/crown; replace the front goggles with four original
        // blue crown pixels. The whole head is centred over the pack, without stretching its rows.
        for(int top=0;top<=13;top++)for(int x=6;x<=22;x++)
        {
            var c=At(x,top);if(c.a==0)continue;
            if(top<=9)
            {
                if(top>=3&&top<=7&&x>=8&&x<=17)c=At(x<13?18:19,top<5?2:3);
            }
            else
            {
                int first=top<13?9:11,last=top<13?19:18;
                if(x<first||x>last)continue;
                // Actual original hair patch, nearest-neighbour transfer, not front skin/neck.
                c=At(8+Mathf.Min(2,(x-first)*3/(last-first+1)),top<12?10:11);
            }
            Put(body,32-x,top,c);
        }
        int coloursOutsideSource=0;var palette=source.Where(c=>c.a>0).ToHashSet();
        var composite=new Color32[W*H];
        foreach(var p in new[]{("LeftLeg",left),("RightLeg",right),("Body",body),("Arm",arm)})
        {
            coloursOutsideSource+=p.Item2.Count(c=>c.a>0&&!palette.Contains(c));
            var t=new Texture2D(W,H,TextureFormat.RGBA32,false);t.SetPixels32(p.Item2);t.Apply();var bytes=t.EncodeToPNG();UnityEngine.Object.DestroyImmediate(t);
            File.WriteAllBytes(Folder+"D2_"+p.Item1+".png",bytes);
            if(apply)
            {
                var path="Assets/Resources/CoreGuardian/PlayerFacing/D2/D2_"+p.Item1+".png";File.WriteAllBytes(path,bytes);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            }
            for(int i=0;i<composite.Length;i++)if(p.Item2[i].a>0)composite[i]=p.Item2[i];
        }
        var comparison=new Texture2D(W*2,H,TextureFormat.RGBA32,false);comparison.SetPixels32(0,0,W,H,source);comparison.SetPixels32(W,0,W,H,composite);comparison.Apply();File.WriteAllBytes(Folder+"source_rear.png",comparison.EncodeToPNG());UnityEngine.Object.DestroyImmediate(comparison);
        return new{apply,coloursOutsideSource,originalPNGChanged=!originalBytes.SequenceEqual(File.ReadAllBytes(sourcePath))};
    }
}
