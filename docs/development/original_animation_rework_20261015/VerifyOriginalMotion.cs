using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class VerifyOriginalMotion
{
 public static object Main()
 {
  var scene=EditorSceneManager.NewPreviewScene();
  var results=new System.Collections.Generic.List<object>();
  try
  {
   string[] roles={"Player","Single","Multi","Support"};
   string[] files={"Player","tower_single","tower_multi","tower_support"};
   for(int i=0;i<4;i++)
   {
    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/ScreenTests/20261008_Depth/Prefabs/"+files[i]+".prefab");
    var root=UnityEngine.Object.Instantiate(prefab);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
    var art=root.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");
    var sprite=art.sprite;var pos=art.transform.localPosition;var rotation=art.transform.localRotation;var scale=art.transform.localScale;
    var animation=CoreGuardianUnitAnimation.Attach(root,roles[i]);
    animation.Fire(new Vector2(-1,1),2);animation.Present(.02f);animation.Present(.07f);
    var rig=art.transform.Find("OriginalMotion");
    if(rig==null)throw new Exception(roles[i]+": original cutout motion missing after Fire");
    var moving=rig.Find("Moving");var fixedPart=rig.Find("Fixed");
    if(moving==null||fixedPart==null||!rig.gameObject.activeSelf||art.enabled)throw new Exception(roles[i]+": attack layers not visible");
    if(moving.localPosition==Vector3.zero&&moving.localRotation==Quaternion.identity)throw new Exception(roles[i]+": no attack motion");
    if(fixedPart.localPosition!=Vector3.zero||fixedPart.localRotation!=Quaternion.identity)throw new Exception(roles[i]+": foundation/feet moved");
    animation.Present(1);
    if(!art.enabled||rig.gameObject.activeSelf||art.sprite!=sprite||art.transform.localPosition!=pos||art.transform.localRotation!=rotation||art.transform.localScale!=scale)throw new Exception(roles[i]+": original pose not restored exactly");
    results.Add(new {role=roles[i],source=sprite.name,attackMoves=true,feetOrBaseFixed=true,originalRestored=true});
    UnityEngine.Object.DestroyImmediate(root);
   }
  }
  finally{EditorSceneManager.ClosePreviewScene(scene);}
  return Newtonsoft.Json.JsonConvert.SerializeObject(results);
 }
}
