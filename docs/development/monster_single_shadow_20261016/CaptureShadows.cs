using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class CaptureShadows
{
 const string Folder="docs/development/monster_single_shadow_20261016/";
 public static object Main(string label)
 {
  if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new Exception("Editor must be stopped and ready");
  Directory.CreateDirectory(Folder+label);
  var preview=new PreviewRenderUtility();
  var units=new CoreGuardianUnitAnimation[4,4];
  string[] roles={"Attack","Defense","Buff","Boss"};
  var dirs=new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down};
  float[] cycles={14,10,12,18};
  try
  {
   preview.camera.orthographic=true;preview.camera.orthographicSize=3;preview.camera.transform.position=new Vector3(0,.6f,-10);preview.camera.transform.rotation=Quaternion.identity;
   preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color32(160,157,146,255);preview.camera.allowHDR=false;preview.camera.allowMSAA=false;
   for(int r=0;r<4;r++)for(int d=0;d<4;d++)
   {
    string path=r==3?"Assets/Resources/CoreGuardian/Animation/Boss/BossAnimated.prefab":"Assets/Art/ScreenTests/20261008_Depth/Prefabs/"+new[]{"enemy_attack","enemy_defense","enemy_buff"}[r]+".prefab";
    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
    var root=UnityEngine.Object.Instantiate(prefab);preview.AddSingleGO(root);root.transform.position=new Vector3(-2.7f+d*1.8f,2.1f-r*1.5f,0);
    var shadows=root.GetComponentsInChildren<SpriteRenderer>(true).Where(x=>x.sprite!=null&&x.sprite.name.Contains("Shadow")).ToArray();
    var old=shadows.Select(x=>new {x.enabled,x.transform.localPosition,x.transform.localScale,x.color,x.sprite}).ToArray();
    units[r,d]=CoreGuardianUnitAnimation.Attach(root,roles[r]);
    for(int i=0;i<shadows.Length;i++)if(shadows[i].enabled!=old[i].enabled||shadows[i].transform.localPosition!=old[i].localPosition||shadows[i].transform.localScale!=old[i].localScale||shadows[i].color!=old[i].color||shadows[i].sprite!=old[i].sprite)throw new Exception("Central shadow changed");
    var contact=root.GetComponentsInChildren<SpriteRenderer>(true).Single(x=>x.name=="바닥 밀착선");
    if(label=="after"&&contact.enabled)throw new Exception("Static foot marks still enabled");
   }
   for(int f=0;f<24;f++)
   {
    for(int r=0;r<4;r++)for(int d=0;d<4;d++){units[r,d].Move(dirs[d]*(cycles[r]*.6f/32),1/24f);units[r,d].Present(1/24f);}
    preview.BeginStaticPreview(new Rect(0,0,1152,960));preview.Render(true,true);var image=preview.EndStaticPreview();File.WriteAllBytes(Folder+label+"/"+f.ToString("000")+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
   }
  }
  finally{preview.Cleanup();}
  return new {label,frames=24,roles,directions=new[]{"X+","X-","Y+","Y-"},centralShadowsPreserved=true,pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.GetType().FullName??"Built-in",scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,dirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty};
 }
}
