using System;
using System.Linq;
using System.Reflection;
using CoreGuardian;
using UnityEngine;
using UnityEngine.UI;

public static class VerifyHudUsability
{
    const BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static void Need(bool value,string message){if(!value)throw new Exception(message);}
    public static object Main()
    {
        var p=UnityEngine.Object.FindFirstObjectByType<CoreGuardianPrototype>();
        Need(Application.isPlaying&&p!=null&&p.Ready,"Fresh owned Play required");
        var h=p.GetComponent<CoreGuardianHud>();var root=p.transform.Find("CombatHUD");
        var spec=root.GetComponentsInChildren<Button>(true).FirstOrDefault(b=>b.name=="PlayerSpecs");
        Need(spec!=null,"Missing separate player current-spec entry in bottom dock");
        spec.onClick.Invoke();
        var player=root.Find("PlayerGrowthPanel");var tower=root.Find("TowerInformation");
        Need(player.gameObject.activeSelf&&!tower.gameObject.activeSelf,"Only one inspector visible");
        var r=(RectTransform)player;Need(r.anchorMin==new Vector2(.5f,0)&&r.anchoredPosition.x==0,"Inspector must share bottom center dock");
        Need(!player.Find("damage").gameObject.activeSelf,"Opening current specs must not show purchase rows");
        var method=h.GetType().GetMethod("TryFoldDetails");Need(method!=null,"Missing explicit fold-only input contract");
        int gold=p.Session.Gold,crystals=p.Session.Crystals,count=p.Session.Towers.Count;
        Need((bool)method.Invoke(h,null),"First floor click folds inspector");
        Need(!player.gameObject.activeSelf,"Floor fold hides inspector");
        Need(p.Session.Gold==gold&&p.Session.Crystals==crystals&&p.Session.Towers.Count==count,"Fold cannot buy/place/destroy");
        Need(!(bool)method.Invoke(h,null),"Already folded cannot consume a second floor click as another fold");
        return new{passed=true,checks="Separate spec entry,shared dock,exclusive inspector,read-only default,fold consumption",kind="Focused actual HUD functional QA; not human play/balance"};
    }
}
