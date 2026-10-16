using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class VerifyShadowAxes
{
 const string Folder="docs/development/monster_shadow_axes_fix_20261016/";
 static Vector2 Sole(Sprite sprite)
 {
  var texture=new Texture2D(2,2);ImageConversion.LoadImage(texture,File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite.texture)));
  int bottom=texture.height,minX=texture.width,maxX=-1;
  for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(texture.GetPixel(x,y).a>.5f)bottom=Math.Min(bottom,y);
  for(int y=bottom;y<Math.Min(bottom+2,texture.height);y++)for(int x=0;x<texture.width;x++)if(texture.GetPixel(x,y).a>.5f){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);}
  UnityEngine.Object.DestroyImmediate(texture);return (new Vector2((minX+maxX+1)*.5f,bottom)-sprite.pivot)/sprite.pixelsPerUnit;
 }
 static Vector3 Foot(SpriteRenderer renderer,Vector2 sole)
 {if(renderer.flipX)sole.x=-sole.x;return renderer.transform.TransformPoint(sole);}
 public static object Main(string label)
 {
  var results=new System.Collections.Generic.List<object>();var failures=new System.Collections.Generic.List<string>();
  string[] roles={"Attack","Defense","Buff","Boss"};float[] cycles={14,10,12,18};var dirs=new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down};
  for(int r=0;r<4;r++)for(int d=0;d<4;d++)
  {
   var prefab=r==3?Resources.Load<GameObject>("CoreGuardian/Animation/Boss/BossAnimated"):AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/ScreenTests/20261008_Depth/Prefabs/"+new[]{"enemy_attack","enemy_defense","enemy_buff"}[r]+".prefab");
   var clone=UnityEngine.Object.Instantiate(prefab);clone.hideFlags=HideFlags.HideAndDontSave;
   try
   {
    var art=clone.GetComponentsInChildren<SpriteRenderer>(true).First(x=>x.name=="2D Sprite");var source=art.sprite;
    var shadow=clone.GetComponentsInChildren<SpriteRenderer>().Where(x=>x.sprite.name.Contains("Shadow")).OrderByDescending(x=>x.bounds.size.x).First();var shadowPosition=shadow.transform.position;var rootPosition=clone.transform.position;
    Vector2 ls=Sole(Resources.Load<Sprite>("CoreGuardian/OriginalWalk/"+roles[r]+"/LeftLower")),rs=Sole(Resources.Load<Sprite>("CoreGuardian/OriginalWalk/"+roles[r]+"/RightLower"));
    float pixel=art.transform.lossyScale.x/source.pixelsPerUnit;
    var unit=CoreGuardianUnitAnimation.Attach(clone,roles[r]);var velocity=dirs[d]*cycles[r]*pixel;
    unit.Move(velocity,1);unit.Present(1);
    var rig=art.transform.Find("OriginalWalk");var left=rig.Find("LeftLower").GetComponent<SpriteRenderer>();var right=rig.Find("RightLower").GetComponent<SpriteRenderer>();
    var centers=new System.Collections.Generic.List<Vector3>();var leftFeet=new System.Collections.Generic.List<Vector3>();var rightFeet=new System.Collections.Generic.List<Vector3>();
    for(int f=0;f<48;f++)
    {
     unit.Move(velocity,1/48f);unit.Present(1/48f);var l=Foot(left,ls);var rr=Foot(right,rs);centers.Add(((l+rr)*.5f-shadow.bounds.center)/pixel);leftFeet.Add(l/pixel);rightFeet.Add(rr/pixel);
    }
    unit.Move(Vector2.zero,1);unit.Present(1);var stopped=((Foot(art,ls)+Foot(art,rs))*.5f-shadow.bounds.center)/pixel;
    float x=centers.Average(v=>v.x),leftRange=leftFeet.Max(v=>v.y)-leftFeet.Min(v=>v.y),rightRange=rightFeet.Max(v=>v.y)-rightFeet.Min(v=>v.y);
    if(Mathf.Abs(stopped.x)>.05f||Mathf.Abs(stopped.y)>.05f)failures.Add(roles[r]+"/"+d+" stopped ground center "+stopped);
    if(Mathf.Abs(x)>1.5f)failures.Add(roles[r]+"/"+d+" mean horizontal offset "+x);
    if(d>=2&&(leftRange<3||rightRange<3))failures.Add(roles[r]+"/"+d+" vertical gait too small "+leftRange+","+rightRange);
    if(rootPosition!=clone.transform.position||shadowPosition!=shadow.transform.position||source!=art.sprite)failures.Add("Source/root/shadow modified");
    results.Add(new{role=roles[r],direction=d,solePixels=new[]{ls.x*32,ls.y*32,rs.x*32,rs.y*32},stopped=new[]{stopped.x,stopped.y},meanX=x,leftRangeY=leftRange,rightRangeY=rightRange,uniqueLeftPoses=leftFeet.Distinct().Count()});
   }
   finally{UnityEngine.Object.DestroyImmediate(clone);}
  }
  var output=new{label,passed=failures.Count==0,failures,results};File.WriteAllText(Folder+label+".json",Newtonsoft.Json.JsonConvert.SerializeObject(output,Newtonsoft.Json.Formatting.Indented));return output;
 }
}
