using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CoreGuardian;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class CoreGuardianSingleTowerAimPlayModeTests
{
 [UnityTest]
 public IEnumerator ActualLaunchUsesMountToTargetRay_AndMuzzleOrigin()
 {
#if UNITY_EDITOR
  EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
  yield return null;
  var type=UnityEditor.TypeCache.GetTypesDerivedFrom<MonoBehaviour>().First(t=>t.Name=="CoreGuardianPrototype");
  var game=(MonoBehaviour)UnityEngine.Object.FindAnyObjectByType(type);
  var session=(Session)type.GetProperty("Session").GetValue(game);game.enabled=false;
  // Isolated board, diagnostic resources and high-HP target: functional QA, not balance play.
  session.StartDiagnosticEncounter();session.SetBoard(40,30,new Vector2Int(30,0),new[]{Vector2Int.zero,new Vector2Int(30,0)});
  string reason;var tower=session.TryInstall(TowerKind.Single,new Vector2Int(3,1),out reason);Assert.That(tower,Is.Not.Null,reason);
  var flags=BindingFlags.Instance|BindingFlags.NonPublic;type.GetMethod("SynchronizeViews",flags).Invoke(game,null);
  var views=(System.Collections.IDictionary)type.GetField("towerViews",flags).GetValue(game);
  var root=(GameObject)views[tower.Id];var art=root.GetComponentsInChildren<SpriteRenderer>(true).Single(r=>r.name=="2D Sprite");
  var mount=art.transform.Find("OriginalMotion/SingleMount");
  var unit=root.GetComponents<Component>().First(c=>c!=null&&c.GetType().Name=="CoreGuardianUnitAnimation");
  var adapter=game.GetComponents<Component>().First(c=>c!=null&&c.GetType().Name=="CoreGuardianCombatAnimation");
  int shots=0;
  session.ProjectileLaunched+=shot=>
  {
   if(shot.Source!="Tower:"+tower.Id)return;shots++;
   var world=(Vector3)type.GetMethod("World").Invoke(game,new object[]{shot.Target.Position});
   var muzzle=(Vector3)unit.GetType().GetProperty("MuzzleWorld").GetValue(unit);
   Assert.That(Vector2.Dot(((Vector2)(world+Vector3.up*.25f-mount.position)).normalized,((Vector2)(muzzle-mount.position)).normalized),Is.GreaterThan(.999f));
   var visual=(Vector3)adapter.GetType().GetMethod("ProjectilePosition").Invoke(adapter,new object[]{shot.Handle});
   Assert.That(Vector3.Distance(visual,muzzle),Is.LessThan(.0001f),"Actual projectile starts at the rotated/recoiling muzzle");
  };
  session.StartCombat();session.DebugSpawn(EnemyKind.Defense,1).Health=10000;
  session.Tick(.05f,new Vector2(-100,-100));Assert.That(shots,Is.GreaterThan(0));
#else
  yield return null;
#endif
 }
}
