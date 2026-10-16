// Surgical normalization of the image edit: retain source pixels outside the lower boot.
if(UnityEditor.EditorApplication.isPlaying || UnityEditor.EditorApplication.isCompiling)
    throw new System.InvalidOperationException("Stopped, compiled editor required");
var folder="docs/development/foot_contact_20261016/";
var path="Assets/Art/ScreenTests/20260930/Revision2/Sprites/enemy_attack_v2.png";
System.IO.Directory.CreateDirectory(folder+"backup");
if(System.IO.File.Exists(folder+"backup/enemy_attack_v2.png"))
    throw new System.InvalidOperationException("Refusing to overwrite backup / repeat foot displacement");
System.IO.File.Copy(path,folder+"backup/enemy_attack_v2.png");
var texture=new UnityEngine.Texture2D(2,2,UnityEngine.TextureFormat.RGBA32,false);
UnityEngine.ImageConversion.LoadImage(texture,System.IO.File.ReadAllBytes(path));
var original=texture.GetPixels32();var changed=(UnityEngine.Color32[])original.Clone();
int w=texture.width,h=texture.height;
// The source-left boot becomes screen-right during rightward movement.
// Move the entire boot/cuff four native pixels down, preserving the spear at top54.
for(int top=55;top<=63;top++)for(int x=12;x<=24;x++)changed[(h-1-top)*w+x]=new UnityEngine.Color32();
for(int top=55;top<=59;top++)for(int x=12;x<=24;x++)changed[(h-1-(top+4))*w+x]=original[(h-1-top)*w+x];
// Continue the existing greave, using the original row54, with no new palette entries.
for(int top=55;top<=58;top++)for(int x=16;x<=21;x++)changed[(h-1-top)*w+x]=original[(h-1-54)*w+x];
int outside=0,total=0;
for(int top=0;top<h;top++)for(int x=0;x<w;x++)
{int i=(h-1-top)*w+x;if(!changed[i].Equals(original[i])){total++;if(top<55||x<12||x>24)outside++;}}
if(outside!=0)throw new System.Exception("Pixels outside authorized lower-boot rectangle changed");
texture.SetPixels32(changed);texture.Apply();System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());
UnityEditor.AssetDatabase.ImportAsset(path,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
// Update only the existing Attack split layers. Keep their GUIDs and import settings.
var names=new[]{"Body","LeftUpper","LeftLower","RightUpper","RightLower"};
var split=names.Select(_=>new UnityEngine.Color32[changed.Length]).ToArray();
System.Func<int,int,int> leg=(x,top)=>top>=42&&x>=12&&x<=24?1:((top>=43&&x>=26&&x<=32)||(top>=47&&x>=26&&x<=39)?2:0);
for(int top=0;top<h;top++)for(int x=0;x<w;x++)
{
 int index=(h-1-top)*w+x,l=leg(x,top);
 if(l==0)split[0][index]=changed[index];
 else {int knee=l==1?49:51,upper=l==1?1:3,lower=upper+1;
 split[top<knee?upper:lower][index]=changed[index];
 if(top>=knee-1&&top<=knee+1){split[upper][index]=changed[index];split[lower][index]=changed[index];}
 if(leg(x,top-2)==0)split[0][index]=changed[index];}
}
var files=new System.Collections.Generic.List<string>();
for(int l=0;l<names.Length;l++)
{
 var layerPath="Assets/Resources/CoreGuardian/OriginalWalk/Attack/"+names[l]+".png";
 System.IO.File.Copy(layerPath,folder+"backup/"+names[l]+".png");
 texture.SetPixels32(split[l]);texture.Apply();var bytes=texture.EncodeToPNG();
 if(!System.IO.File.ReadAllBytes(layerPath).SequenceEqual(bytes))
 {System.IO.File.WriteAllBytes(layerPath,bytes);UnityEditor.AssetDatabase.ImportAsset(layerPath,UnityEditor.ImportAssetOptions.ForceSynchronousImport);files.Add(layerPath);}
}
UnityEngine.Object.DestroyImmediate(texture);
var result=new {source=path,changedPixels=total,outsideRectangle=outside,bootShiftNativePixels=4,files};
System.IO.File.WriteAllText(folder+"source_edit.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
return result;
