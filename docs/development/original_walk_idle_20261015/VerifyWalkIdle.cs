using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class VerifyWalkIdle
{
 public static object Main()
 {
  var scene=EditorSceneManager.NewPreviewScene();var results=new System.Collections.Generic.List<object>();
  try
  {
   string[] roles={"Attack","Defense","Buff","Boss"};string[] names={"enemy_attack","enemy_defense","enemy_buff"};
   for(int i=0;i<4;i++)
   {
    var prefab=i==3?Resources.Load<GameObject>("CoreGuardian/Animation/Boss/BossAnimated"):AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/ScreenTests/20261008_Depth/Prefabs/"+names[i]+".prefab");
    var clone=UnityEngine.Object.Instantiate(prefab);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(clone,scene);
    var art=clone.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="2D Sprite");var source=art.sprite;var pos=art.transform.localPosition;var scale=art.transform.localScale;
    var unit=CoreGuardianUnitAnimation.Attach(clone,roles[i]);
    unit.Move(Vector2.right,.1f);unit.Present(.1f);
    if(art.sprite!=source)throw new Exception(roles[i]+": original sprite replaced by generated atlas");
    var rig=art.transform.Find("OriginalWalk");if(rig==null||!rig.gameObject.activeSelf)throw new Exception(roles[i]+": original skeletal walk missing");
    var left=rig.Find("LeftLower");var right=rig.Find("RightLower");if(left==null||right==null)throw new Exception("Two independent legs required");
    var first=left.localPosition;unit.Move(Vector2.right,.06f);unit.Present(.06f);if(left.localPosition==first)throw new Exception("Gait does not advance continuously with distance");
    foreach(var direction in new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down}){unit.Move(direction,.1f);unit.Present(.1f);if(art.sprite!=source||art.transform.localPosition!=pos||art.transform.localScale!=scale)throw new Exception("Source/foot root altered");}
    unit.Move(Vector2.zero,.3f);unit.Present(.3f);if(!art.enabled||rig.gameObject.activeSelf)throw new Exception("Stopped enemy must return to exact original");
    results.Add(new{role=roles[i],source=source.name,walk=true,continuous=true,rootPreserved=true,restored=true});UnityEngine.Object.DestroyImmediate(clone);
   }
   var player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/ScreenTests/20261008_Depth/Prefabs/Player.prefab"));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(player,scene);
   var playerArt=player.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="2D Sprite");var playerSprite=playerArt.sprite;var playerPos=playerArt.transform.localPosition;
   var animation=CoreGuardianUnitAnimation.Attach(player,"Player");animation.Present(.6f);
   var idle=playerArt.transform.Find("OriginalIdle");if(idle==null||!idle.gameObject.activeSelf)throw new Exception("Player original idle missing");
   if(idle.Find("Feet").localPosition!=Vector3.zero||playerArt.sprite!=playerSprite||playerArt.transform.localPosition!=playerPos)throw new Exception("Idle changes source or feet");
   var torso=idle.Find("Upper");var firstTorso=torso.localPosition;animation.Present(.4f);if(torso.localPosition==firstTorso)throw new Exception("Idle does not breathe");
   animation.Fire(Vector2.left,2);if(idle.gameObject.activeSelf||!playerArt.transform.Find("OriginalMotion").gameObject.activeSelf)throw new Exception("Attack must override Idle immediately");
   animation.Move(Vector2.right,.1f);animation.Present(1);if(idle.gameObject.activeSelf)throw new Exception("Idle must not play during movement");
   results.Add(new{role="Player",idle=true,feetFixed=true,attackOverrides=true,movementSuppresses=true});UnityEngine.Object.DestroyImmediate(player);
  }
  finally{EditorSceneManager.ClosePreviewScene(scene);}
  return Newtonsoft.Json.JsonConvert.SerializeObject(results);
 }
}
