using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class NormalizeVertical
{
 const string Folder="docs/development/vertical_facing_20261016/";
 static Texture2D Read(string path){var t=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(t,File.ReadAllBytes(path));return t;}
 static RectInt Bounds(Texture2D t,RectInt cell)
 {
  int minX=cell.xMax,minY=cell.yMax,maxX=cell.xMin,maxY=cell.yMin;
  for(int y=cell.yMin;y<cell.yMax;y++)for(int x=cell.xMin;x<cell.xMax;x++)if(t.GetPixel(x,y).a>.7f){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
  return new RectInt(minX,minY,maxX-minX+1,maxY-minY+1);
 }
 static Color32 Closest(Color c,Color32[] palette)
 {
  float best=float.MaxValue;Color32 chosen=new Color32();foreach(var p in palette){Color pc=p;float d=(c.r-pc.r)*(c.r-pc.r)*.3f+(c.g-pc.g)*(c.g-pc.g)*.59f+(c.b-pc.b)*(c.b-pc.b)*.11f;if(d<best){best=d;chosen=p;}}
  chosen.a=255;return chosen;
 }
 static float HelmetTop(Texture2D texture,float center,float halfWidth,RectInt cell)
 {
  for(int y=cell.yMax-1;y>=cell.yMin;y--)for(int x=Math.Max(cell.xMin,(int)(center-halfWidth));x<Math.Min(cell.xMax,(int)(center+halfWidth));x++)
  {var c=texture.GetPixel(x,y);if(c.a>.7f&&c.r>.25f&&Mathf.Abs(c.r-c.g)<.09f&&Mathf.Abs(c.g-c.b)<.1f)return y+1;}
  throw new Exception("Helmet not found");
 }
 static void RemoveSpecks(Texture2D texture,int cw,int ch)
 {
  var pixels=texture.GetPixels32();var seen=new bool[pixels.Length];var parts=new System.Collections.Generic.List<System.Collections.Generic.List<int>>();
  for(int row=0;row<2;row++)for(int col=0;col<4;col++)
  {
   parts.Clear();
   for(int y=row*ch;y<(row+1)*ch;y++)for(int x=col*cw;x<(col+1)*cw;x++)
   {
    int first=y*texture.width+x;if(seen[first]||pixels[first].a<179)continue;
    var part=new System.Collections.Generic.List<int>();var queue=new System.Collections.Generic.Queue<int>();queue.Enqueue(first);seen[first]=true;
    while(queue.Count>0)
    {
     int index=queue.Dequeue();part.Add(index);int px=index%texture.width,py=index/texture.width;
     foreach(int next in new[]{index-1,index+1,index-texture.width,index+texture.width})
     {
      if(next<0||next>=pixels.Length)continue;int nx=next%texture.width,ny=next/texture.width;
      if(nx<col*cw||nx>=(col+1)*cw||ny<row*ch||ny>=(row+1)*ch||Math.Abs(nx-px)+Math.Abs(ny-py)!=1||seen[next]||pixels[next].a<179)continue;
      seen[next]=true;queue.Enqueue(next);
     }
    }
    parts.Add(part);
   }
   float threshold=parts.Max(p=>p.Count)*.003f;foreach(var part in parts.Where(p=>p.Count<threshold))foreach(int index in part)pixels[index]=new Color32();
  }
  texture.SetPixels32(pixels);texture.Apply();
 }
 public static object Main()
 {
  string[] roles={"Attack","Defense","Buff","Boss"};string[] sources={"Assets/Art/ScreenTests/20260930/Revision2/Sprites/enemy_attack_v2.png","Assets/Art/ScreenTests/20260930/Revision2/Sprites/enemy_defense_v2.png","Assets/Art/ScreenTests/20260930/Revision2/Sprites/enemy_buff_v2.png","Assets/Resources/CoreGuardian/Art/BossEye/BossEye.png"};
  var report=new System.Collections.Generic.List<object>();var compare=new Texture2D(96*9,112*4,TextureFormat.RGBA32,false);compare.SetPixels32(new Color32[compare.width*compare.height]);
  for(int r=0;r<4;r++)
  {
   var source=Read(sources[r]);var palette=source.GetPixels32().Where(c=>c.a>200).Distinct().ToArray();var sourceBounds=Bounds(source,new RectInt(0,0,source.width,source.height));
   var raw=Read(Folder+"Source/"+roles[r]+".png");int cw=raw.width/4,ch=raw.height/2;RemoveSpecks(raw,cw,ch);
   var bounds=new RectInt[8];for(int row=0;row<2;row++)for(int col=0;col<4;col++)bounds[row*4+col]=Bounds(raw,new RectInt(col*cw,(1-row)*ch,cw,ch));
   float[] sourceCenters={25,22,15.75f,41.25f};
   float bodyHeight=HelmetTop(source,sourceCenters[r],source.width*.15f,new RectInt(0,0,source.width,source.height))-sourceBounds.yMin;
   float[] heights=new float[8];
   for(int i=0;i<8;i++){var b=bounds[i];heights[i]=HelmetTop(raw,b.center.x,cw*.08f,b)-b.yMin;}
   float scale=bodyHeight/heights.Max();string output=Folder+"Frames/"+roles[r]+"/";Directory.CreateDirectory(output);
   for(int row=0;row<2;row++)
   {
    float bottom=(float)bounds.Skip(row*4).Take(4).Average(b=>b.yMin);float anchor=0;
    for(int col=0;col<4;col++)
    {
     var b=bounds[row*4+col];int minX=b.xMax,maxX=b.xMin;
     for(int y=b.yMin;y<b.yMin+Math.Max(1,b.height/10);y++)for(int x=b.xMin;x<b.xMax;x++)if(raw.GetPixel(x,y).a>.7f){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);}
     anchor+=(minX+maxX+1)*.5f-col*cw;
    }
    anchor/=4;
    for(int col=0;col<4;col++)
    {
     var frame=new Texture2D(96,112,TextureFormat.RGBA32,false);var pixels=new Color32[96*112];
     for(int y=0;y<112;y++)for(int x=0;x<96;x++)
     {
      int sx=Mathf.FloorToInt((x-48+.5f)/scale+anchor+col*cw),sy=Mathf.FloorToInt((y-3+.5f)/scale+bottom);
      if(sx<col*cw||sx>=(col+1)*cw||sy<(1-row)*ch||sy>=(2-row)*ch)continue;
      var color=raw.GetPixel(sx,sy);if(color.a>.7f)pixels[y*96+x]=Closest(color,palette);
     }
     frame.SetPixels32(pixels);frame.Apply();string name=(row==0?"Up_":"Down_")+col;File.WriteAllBytes(output+name+".png",frame.EncodeToPNG());
     for(int y=0;y<112;y++)for(int x=0;x<96;x++)compare.SetPixel((1+row*4+col)*96+x,(3-r)*112+y,pixels[y*96+x]);
     report.Add(new{role=roles[r],name,scale,bounds=new[]{bounds[row*4+col].x,bounds[row*4+col].y,bounds[row*4+col].width,bounds[row*4+col].height},paletteColors=palette.Length});UnityEngine.Object.DestroyImmediate(frame);
    }
   }
   for(int y=0;y<source.height;y++)for(int x=0;x<source.width;x++)compare.SetPixel(48-source.width/2+x,(3-r)*112+3+y,source.GetPixel(x,y));
   UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(raw);
  }
  compare.Apply();var enlarged=new Texture2D(compare.width*3,compare.height*3,TextureFormat.RGBA32,false);var bg=new Color32(35,40,46,255);
  for(int y=0;y<enlarged.height;y++)for(int x=0;x<enlarged.width;x++){var c=compare.GetPixel(x/3,y/3);enlarged.SetPixel(x,y,c.a>.5f?c:bg);}enlarged.Apply();File.WriteAllBytes(Folder+"native3x_comparison.png",enlarged.EncodeToPNG());UnityEngine.Object.DestroyImmediate(compare);UnityEngine.Object.DestroyImmediate(enlarged);
  File.WriteAllText(Folder+"normalization.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return new{frames=32,palette="exact source per-role",scale="shared across both directions and all four phases"};
 }
}
