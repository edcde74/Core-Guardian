using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public static class CapturePlacementHud
{
 static CoreGuardianPrototype Game()=>UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
 static Transform Hud()=>Game().transform.Find("CombatHUD");
 static void Click(string name)=>Hud().GetComponentsInChildren<Button>(true).First(b=>b.name==name).onClick.Invoke();
 public static object Before()
 {
  if(!Application.isPlaying||!Game().Ready)throw new Exception("Own ready Play session required");
  Time.timeScale=0;Click("PlayerGrowth");Click("Installation");Click("공격 포탑");return State();
 }
 public static object Focus(){Click("Install_Single");return State();}
 public static object Cancel(){Click("CancelPlacement");return State();}
 public static object Install()
 {
  Click("Installation");Click("공격 포탑");Click("Install_Single");
  if(!Game().Place(CoreGuardian.TowerKind.Single,new Vector2Int(14,15)))throw new Exception("Normal placement failed");return State();
 }
 public static object Combat(){Game().StartCombat();return State();}
 public static object State()
 {
  return Newtonsoft.Json.JsonConvert.SerializeObject(new{phase=Game().Session.Phase.ToString(),installing=Game().Installing?.ToString(),cursor=Cursor.visible,roots=Hud().Cast<Transform>().Select(t=>new{name=t.name,active=t.gameObject.activeSelf,alpha=t.GetComponent<CanvasGroup>()?.alpha,raycasts=t.GetComponent<CanvasGroup>()?.blocksRaycasts}).ToArray()});
 }
}
