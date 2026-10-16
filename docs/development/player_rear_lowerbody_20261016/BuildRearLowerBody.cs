using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

// Rear-facing trousers, straight boot shafts and heels assembled from original pixels.
// Run AFTER BuildSourceFacings and BuildRearPlayer when regenerating all directions.
public static class BuildRearLowerBody
{
 const string Folder="docs/development/player_rear_lowerbody_20261016/";
 public static object Main(bool apply=false)
 {
  if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new Exception("Editor must be stopped and ready");
  var source=new Texture2D(2,2);source.LoadImage(File.ReadAllBytes("Assets/Art/ScreenTests/20260930/Sprites/player.png"));
  var pixels=source.GetPixels32();UnityEngine.Object.DestroyImmediate(source);
  var palette=pixels.Where(c=>c.a>0).ToHashSet();int extraColours=0;
  foreach(var leg in new[]{("LeftLeg",13),("RightLeg",23)})
  {
   var result=new Color32[36*48];
   for(int top=33;top<48;top++)
   {
    // The original long leg supplies the complete trousers and shaft. Re-centre
    // the knee/ankle rows instead of retaining the original outward splay.
    int row=top<=40?top:top<=42?42:top==43?45:top<=46?46:47;
    int first=row<=37?20:row<=40?21:row==42?22:21;
    for(int dx=0;dx<7;dx++)
    {
     var c=pixels[(47-row)*36+first+dx];if(c.a==0)continue;
     result[(47-top)*36+leg.Item2-3+dx]=c;
     if(!palette.Contains(c))extraColours++;
    }
   }
   var texture=new Texture2D(36,48,TextureFormat.RGBA32,false);texture.SetPixels32(result);texture.Apply();var bytes=texture.EncodeToPNG();UnityEngine.Object.DestroyImmediate(texture);
   string name="D2_"+leg.Item1+".png";File.WriteAllBytes(Folder+name,bytes);
   if(apply){string path="Assets/Resources/CoreGuardian/PlayerFacing/D2/"+name;File.WriteAllBytes(path,bytes);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);}
  }
  return new{apply,extraColours,centres=new[]{13,23},source="player.png original long trouser leg / boot shaft / dark heel / sole",upperBodyModified=false};
 }
}
