using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

// Every opaque pixel is copied from player.png. No generated painting or new colours.
public static class BuildSourceFacings
{
    const int W=36,H=48;
    static Color32[] source;
    static Color32 Clear=>new Color32(0,0,0,0);
    static Color32 At(int x,int top)=>x<0||x>=W||top<0||top>=H?Clear:source[(H-1-top)*W+x];
    static Color32[] Empty()=>new Color32[W*H];
    static void Put(Color32[] dst,int x,int top,Color32 c){if(x>=0&&x<W&&top>=0&&top<H&&c.a>0)dst[(H-1-top)*W+x]=c;}
    static void Over(Color32[] dst,Color32[] src){for(int i=0;i<dst.Length;i++)if(src[i].a>0)dst[i]=src[i];}
    static Color32[] Mirror(Color32[] a){var b=Empty();for(int t=0;t<H;t++)for(int x=0;x<W;x++)b[(H-1-t)*W+W-1-x]=a[(H-1-t)*W+x];return b;}
    static void Save(string path,Color32[] a)
    {
        var t=new Texture2D(W,H,TextureFormat.RGBA32,false);t.SetPixels32(a);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.spritePixelsPerUnit=32;
        var s=new TextureImporterSettings();i.ReadTextureSettings(s);s.spriteAlignment=(int)SpriteAlignment.Custom;s.spritePivot=new Vector2(.5f,0);s.spriteMeshType=SpriteMeshType.FullRect;i.SetTextureSettings(s);
        i.filterMode=FilterMode.Point;i.mipmapEnabled=false;i.textureCompression=TextureImporterCompression.Uncompressed;i.alphaIsTransparency=true;i.SaveAndReimport();
    }
    public static object Main()
    {
        var t=new Texture2D(2,2);t.LoadImage(File.ReadAllBytes("Assets/Art/ScreenTests/20260930/Sprites/player.png"));source=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);
        string root="Assets/Resources/CoreGuardian/PlayerFacing";Directory.CreateDirectory(root);
        int addedColours=0;var palette=source.Where(c=>c.a>0).Select(c=>(uint)c.r<<24|(uint)c.g<<16|(uint)c.b<<8|c.a).ToHashSet();
        var composed=new Color32[8][];
        foreach(int d in new[]{5,4,3,2,6})
        {
            var body=Empty();var arm=Empty();var left=Empty();var right=Empty();var pack=Empty();
            float squeeze=d==4?.76f:d==3?.9f:1;
            for(int top=0;top<H;top++)for(int x=0;x<W;x++)
            {
                Color32 c=At(x,top);if(c.a==0)continue;
                bool tool=(top>=21&&top<=31&&x<=10)||(top>=26&&top<=32&&x<=4);
                bool backpack=(x>=26&&top<=30)||(x>=23&&top<21);
                int xx=Mathf.RoundToInt(18+(x-18)*squeeze);
                if(tool){Put(arm,xx,top,c);continue;}
                if(top>=33){Put(x<=18?left:right,xx,top,c);continue;}
                if(backpack){int shift=d==2?-10:d==3?-4:d==6?-5:0;Put(pack,xx+shift,top,c);continue;}
                Put(body,xx,top,c);
            }
            // Front view: reflect the visible half of the source head and shirt about its centre.
            // The backpack stays behind the torso. No face/cloth pixels are painted.
            if(d==6)
            {
                for(int top=1;top<21;top++)for(int x=8;x<=16;x++)Put(body,34-x,top,At(x,top));
                var combined=Empty();Over(combined,pack);Over(combined,body);body=combined;
            }
            else if(d==2||d==3)
            {
                // Back of the cap is the original crown, extended by nearest row reuse.
                // Original dark brown hair covers the hidden face; the original pack covers the back.
                for(int top=3;top<15;top++)for(int x=7;x<22;x++)
                {
                    int xx=Mathf.RoundToInt(18+(x-18)*squeeze);
                    if(body[(H-1-top)*W+xx].a==0)continue;
                    if(d==3&&x<=9&&top>=12)continue; // a narrow cheek remains on the three-quarter turn
                    Color32 rear=body[(H-1-top)*W+xx];
                    if(top<8&&x>=8&&x<=17)rear=At(18+(x-8)*3/10,2+(top-3)*3/5);
                    else if(top>=11)rear=At(8+Mathf.Clamp((x-7)*3/15,0,2),10+(top>=13?1:0));
                    if(rear.a>0)body[(H-1-top)*W+xx]=rear;
                }
                var head=(Color32[])body.Clone();Over(body,pack);
                for(int top=0;top<17;top++)for(int x=6;x<23;x++)
                {
                    int xx=Mathf.RoundToInt(18+(x-18)*squeeze);var c=head[(H-1-top)*W+xx];
                    if(c.a>0)body[(H-1-top)*W+xx]=c;
                }
                if(d==2){arm=Mirror(arm);}
            }
            else Over(body,pack);
            foreach(var pair in new[]{(5,7),(4,0),(3,1)})
            {
                if(d!=pair.Item1)continue;
                Export(pair.Item2,Mirror(body),Mirror(arm),Mirror(right),Mirror(left));
            }
            Export(d,body,arm,left,right);
        }
        void Export(int d,Color32[] body,Color32[] arm,Color32[] left,Color32[] right)
        {
            string folder=root+"/D"+d;Directory.CreateDirectory(folder);
            foreach(var p in new[]{("Body",body),("Arm",arm),("LeftLeg",left),("RightLeg",right)})
            {
                addedColours+=p.Item2.Count(c=>c.a>0&&!palette.Contains((uint)c.r<<24|(uint)c.g<<16|(uint)c.b<<8|c.a));
                Save(folder+"/D"+d+"_"+p.Item1+".png",p.Item2);
            }
            var all=Empty();Over(all,left);Over(all,right);Over(all,body);Over(all,arm);composed[d]=all;
        }
        // Native-pixel comparison: approved source at left, all eight derived poses at the same scale.
        var comparison=new Texture2D(W*9,H,TextureFormat.RGBA32,false);comparison.SetPixels32(new Color32[W*9*H]);
        comparison.SetPixels32(0,0,W,H,source);for(int d=0;d<8;d++)comparison.SetPixels32(W*(d+1),0,W,H,composed[d]);comparison.Apply();
        File.WriteAllBytes("docs/development/player_facing_20261016/native_comparison.png",comparison.EncodeToPNG());UnityEngine.Object.DestroyImmediate(comparison);
        int originalDifference=Enumerable.Range(0,source.Length).Count(i=>(source[i].a>0||composed[5][i].a>0)&&!source[i].Equals(composed[5][i]));
        AssetDatabase.Refresh();return new{directions=8,layers=32,addedColours,originalDirectionPixelDifference=originalDifference};
    }
}
