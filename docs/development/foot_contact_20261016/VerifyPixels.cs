var folder="docs/development/foot_contact_20261016/";
var before=new UnityEngine.Texture2D(2,2);var after=new UnityEngine.Texture2D(2,2);
try
{
 UnityEngine.ImageConversion.LoadImage(before,System.IO.File.ReadAllBytes(folder+"backup/enemy_attack_v2.png"));
 UnityEngine.ImageConversion.LoadImage(after,System.IO.File.ReadAllBytes("Assets/Art/ScreenTests/20260930/Revision2/Sprites/enemy_attack_v2.png"));
 System.Func<UnityEngine.Texture2D,int,int,int> bottom=(t,a,b)=>{for(int y=0;y<t.height;y++)for(int x=a;x<=b;x++)if(t.GetPixel(x,y).a>.5f)return y;throw new System.Exception("No boot");};
 var oldPixels=before.GetPixels32();var pixels=after.GetPixels32();
 var palette=new System.Collections.Generic.HashSet<UnityEngine.Color32>(oldPixels.Where(c=>c.a>0));
 int outside=0,newColors=0;for(int y=0;y<64;y++)for(int x=0;x<42;x++)
 {int i=y*42+x,top=63-y;if((top<55||x<12||x>24)&&!oldPixels[i].Equals(pixels[i]))outside++;if(pixels[i].a>0&&!palette.Contains(pixels[i]))newColors++;}
 var left=UnityEngine.Resources.Load<UnityEngine.Sprite>("CoreGuardian/OriginalWalk/Attack/LeftLower");
 var result=new {beforeLeft=bottom(before,12,24),beforeRight=bottom(before,31,38),afterLeft=bottom(after,12,24),afterRight=bottom(after,31,38),outsideRectangle=outside,newColors,sourceSize=new[]{after.width,after.height},layerPPU=left.pixelsPerUnit};
 if(outside!=0||newColors!=0||bottom(after,12,24)!=bottom(after,31,38))throw new System.Exception("Pixel preservation / sole contact failed");
 System.IO.File.WriteAllText(folder+"pixel_verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
}
finally{UnityEngine.Object.DestroyImmediate(before);UnityEngine.Object.DestroyImmediate(after);}
