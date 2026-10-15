using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;

// Technical nearest-neighbor normalization/import only. Artwork is created by imagegen.
public static class PrepareAnimationAssets
{
    const string Root="Assets/Resources/CoreGuardian/Animation/";
    const string Art="docs/art/20261015_Animation/";
    const int W=96,H=112;
    class Frame {public string name;public int x,y;public float muzzleX,muzzleY;}
    static readonly List<string> imported=new List<string>();
    static readonly List<object> results=new List<object>();
    static Texture2D Read(string path){var t=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!ImageConversion.LoadImage(t,File.ReadAllBytes(path)))throw new Exception(path);return t;}
    static RectInt Bounds(Color32[] p,int width,int height)
    {
        int l=width,r=-1,b=height,t=-1;
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)if(p[y*width+x].a>=160){l=Math.Min(l,x);r=Math.Max(r,x);b=Math.Min(b,y);t=Math.Max(t,y);}
        if(r<l)throw new Exception("Empty cell");return new RectInt(l,b,r-l+1,t-b+1);
    }
    static Color32[] Palette(Texture2D reference)
    {
        return reference.GetPixels32().Where(p=>p.a>=200).GroupBy(p=>(p.r/12)*65536+(p.g/12)*256+p.b/12).OrderByDescending(g=>g.Count()).Take(56).Select(g=>g.First()).ToArray();
    }
    static Color32 Nearest(Color32 value,Color32[] palette)
    {
        if(value.a<160)return new Color32(0,0,0,0);
        int best=int.MaxValue;Color32 chosen=palette[0];
        foreach(var p in palette){int dr=p.r-value.r,dg=p.g-value.g,db=p.b-value.b;int distance=dr*dr+dg*dg+db*db;if(distance<best){best=distance;chosen=p;}}
        chosen.a=255;return chosen;
    }
    static void RemoveSpecks(Color32[] pixels,int width,int height)
    {
        var seen=new bool[pixels.Length];var components=new List<List<int>>();
        for(int i=0;i<pixels.Length;i++)
        {
            if(seen[i]||pixels[i].a<160)continue;
            var component=new List<int>();var pending=new Queue<int>();pending.Enqueue(i);seen[i]=true;
            while(pending.Count>0){int p=pending.Dequeue();component.Add(p);int x=p%width,y=p/width;int[] neighbors={x>0?p-1:-1,x<width-1?p+1:-1,y>0?p-width:-1,y<height-1?p+width:-1};foreach(int n in neighbors)if(n>=0&&!seen[n]&&pixels[n].a>=160){seen[n]=true;pending.Enqueue(n);}}
            components.Add(component);
        }
        int largest=components.Max(c=>c.Count);
        foreach(var component in components.Where(c=>c.Count<largest*.02f))foreach(int p in component)pixels[p]=new Color32();
    }
    static void Unit(string role,string reference,int rawRow=-1,bool player8=false)
    {
        bool tower=rawRow>=0;
        var source=Read(Art+(player8?"Player8":tower?"Towers":role)+"_raw.png");
        var original=Read(reference);var palette=Palette(original);
        int target=Bounds(original.GetPixels32(),original.width,original.height).height;
        int directions=player8?8:4;
        int cw=source.width/(player8?8:4),ch=source.height/(tower?3:4);
        var cells=new Color32[directions*4][];var bounds=new RectInt[directions*4];
        for(int d=0;d<directions;d++)for(int f=0;f<4;f++)
        {
            int col=tower?(role=="Support"?f:d):f,row=tower?rawRow:d;
            if(player8){col=d;row=f;}
            // Generated cannon first view faces left; mirror its upper assembly for right.
            if(role=="Multi"&&d<2)col=1-d;
            var cell=source.GetPixels(col*cw,source.height-(row+1)*ch,cw,ch).Select(c=>(Color32)c).ToArray();
            RemoveSpecks(cell,cw,ch);
            if(role=="Single"&&d==0)for(int y=0;y<ch;y++)Array.Reverse(cell,y*cw,cw);
            cells[d*4+f]=cell;bounds[d*4+f]=Bounds(cell,cw,ch);
        }
        int rows=tower?5:directions;var atlas=new Texture2D(W*4,H*rows,TextureFormat.RGBA32,false);
        atlas.SetPixels32(new Color32[atlas.width*atlas.height]);var frames=new List<Frame>();
        Color32[] foundation=null;
        for(int d=0;d<directions;d++)
        {
            float scale=target/(float)bounds.Skip(d*4).Take(4).Average(b=>b.height);
            for(int f=0;f<4;f++)
            {
                var b=bounds[d*4+f];var cell=cells[d*4+f];int left=cw,right=-1;
                // Both feet, rather than just the single lowest shoe, determine ground center.
                int feetHeight=Mathf.Max(1,Mathf.RoundToInt(b.height*(tower?.10f:.18f)));
                for(int y=b.y;y<b.y+feetHeight;y++)for(int x=b.x;x<b.xMax;x++)if(cell[y*cw+x].a>=160){left=Math.Min(left,x);right=Math.Max(right,x);}
                float center=(left+right+1)*.5f;
                var pixels=new Color32[W*H];
                for(int y=0;y<H;y++)for(int x=0;x<W;x++)
                {
                    int sx=Mathf.FloorToInt(center+(x-48+.5f)/scale),sy=Mathf.FloorToInt(b.y+(y-8+.5f)/scale);
                    if(sx>=b.x&&sx<b.xMax&&sy>=b.y&&sy<b.yMax)pixels[y*W+x]=Nearest(cell[sy*cw+sx],palette);
                }
                // Muzzle is measured from the actual displayed weapon silhouette.
                int mx=48,my=8+target/2;float best=-99999;
                if(role=="Player"||tower&&! (role=="Support"))
                {
                    Vector2[] axes=player8?Enumerable.Range(0,8).Select(i=>new Vector2(Mathf.Cos(i*Mathf.PI/4),Mathf.Sin(i*Mathf.PI/4))).ToArray():new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down};
                    for(int y=8+target/3;y<8+target;y++)for(int x=0;x<W;x++)
                    {
                        var p=pixels[y*W+x];if(p.a==0)continue;
                        if(tower&&!(p.b>p.r*1.2f&&p.b>p.g*.9f))continue;
                        float score=Vector2.Dot(new Vector2(x-48,y-(8+target*.62f)),axes[d]);
                        if(score>best){best=score;mx=x;my=y;}
                    }
                }
                if(tower)
                {
                    if(foundation==null){foundation=new Color32[W*H];for(int y=0;y<8+target*.37f;y++)Array.Copy(pixels,y*W,foundation,y*W,W);}
                    for(int y=0;y<8+target*.37f;y++)Array.Clear(pixels,y*W,W);
                }
                int ax=f*W,ay=(directions-1-d)*H;atlas.SetPixels32(ax,ay,W,H,pixels);
                frames.Add(new Frame{name=role+"_"+d+"_"+f,x=ax,y=ay,muzzleX=mx-48+.5f,muzzleY=my-8+.5f});
            }
        }
        if(tower){atlas.SetPixels32(0,H*4,W,H,foundation);frames.Add(new Frame{name="Base",x=0,y=H*4});}
        string dir=Root+role;Directory.CreateDirectory(dir);atlas.Apply();
        File.WriteAllBytes(dir+"/atlas.png",atlas.EncodeToPNG());
        File.WriteAllText(dir+"/frames.json",JsonConvert.SerializeObject(new{frames},Formatting.Indented));
        imported.Add(dir+"/atlas.png");
        results.Add(new{role,targetHeight=target,frames=frames.Count,reference,palette=palette.Length});
        UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(original);UnityEngine.Object.DestroyImmediate(atlas);
    }
    public static object Main()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Stopped compiled saved scene required");
        if(Directory.Exists(Root))throw new Exception("Refusing to overwrite existing animation assets");
        Unit("Attack","Assets/Art/ScreenTests/20260930/Revision2/Sprites/enemy_attack_v2.png");
        Unit("Defense","Assets/Art/ScreenTests/20260930/Revision2/Sprites/enemy_defense_v2.png");
        Unit("Buff","Assets/Art/ScreenTests/20260930/Revision2/Sprites/enemy_buff_v2.png");
        Unit("Boss","Assets/Resources/CoreGuardian/Art/BossEye/BossEye.png");
        Unit("Player","Assets/Art/ScreenTests/20260930/Sprites/player.png");
        Unit("Single","Assets/Art/ScreenTests/20260930/Sprites/tower_single.png",0);
        Unit("Multi","Assets/Art/ScreenTests/20260930/Sprites/tower_multi.png",1);
        Unit("Support","Assets/Art/ScreenTests/20260930/Sprites/tower_support.png",2);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach(string path in imported)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;
            importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.SaveAndReimport();
        }
        return JsonConvert.SerializeObject(new{success=true,results});
    }
    public static object Refine()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new Exception("Stop first");
        imported.Clear();results.Clear();
        Unit("Attack","Assets/Art/ScreenTests/20260930/Revision2/Sprites/enemy_attack_v2.png");
        Unit("Defense","Assets/Art/ScreenTests/20260930/Revision2/Sprites/enemy_defense_v2.png");
        Unit("Buff","Assets/Art/ScreenTests/20260930/Revision2/Sprites/enemy_buff_v2.png");
        Unit("Boss","Assets/Resources/CoreGuardian/Art/BossEye/BossEye.png");
        Unit("Player","Assets/Art/ScreenTests/20260930/Sprites/player.png");
        Unit("Single","Assets/Art/ScreenTests/20260930/Sprites/tower_single.png",0);
        Unit("Multi","Assets/Art/ScreenTests/20260930/Sprites/tower_multi.png",1);
        Unit("Support","Assets/Art/ScreenTests/20260930/Sprites/tower_support.png",2);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        return JsonConvert.SerializeObject(new{success=true,results,change="Remove isolated source-cell edge artifacts; existing import settings preserved"});
    }
    public static object Player8()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new Exception("Stop first");
        Unit("Player","Assets/Art/ScreenTests/20260930/Sprites/player.png",-1,true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        return JsonConvert.SerializeObject(new{success=true,results});
    }
}
