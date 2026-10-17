using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
public static class CaptureAbilityA
{
    public static async Task<object> Main()
    {
        var p=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();if(!Application.isPlaying||p==null)throw new Exception("Owned Play required");
        var root=p.transform.Find("CombatHUD");Action<string> click=n=>root.GetComponentsInChildren<Button>(true).First(b=>b.name==n).onClick.Invoke();
        Func<string,Task> capture=async n=>{await Task.Delay(150);ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("docs/development/hud_ability_A_20261017/"+n+".png"));await Task.Delay(200);};
        click("PlayerSpecs");if(!root.Find("PlayerGrowthPanel").gameObject.activeSelf)click("PlayerSpecs");await capture("player_current");
        click("PlayerBuyTab");await capture("player_upgrade");
        p.HandleWorldClick(new Vector2Int(14,15));click("TowerSpecTab");await capture("tower_current");
        click("TowerBuyTab");await capture("tower_upgrade");
        p.Place(CoreGuardian.TowerKind.Support,new Vector2Int(15,15));p.HandleWorldClick(new Vector2Int(15,15));await capture("support_current");
        click("TowerBuyTab");await capture("support_upgrade");
        return new{captured=6,screen=new{Screen.width,Screen.height},kind="Actual Overlay HUD, diagnostic resources/install, no camera/time changes"};
    }
}
