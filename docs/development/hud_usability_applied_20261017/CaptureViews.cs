using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CoreGuardian;
using UnityEngine;
using UnityEngine.UI;

public static class CaptureHudViews
{
    public static async Task<object> Main()
    {
        var p=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();bool previous=p.enabled;p.enabled=false;
        var root=p.transform.Find("CombatHUD");string dir=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"docs/development/hud_usability_applied_20261017");
        Action<string> click=name=>root.GetComponentsInChildren<Button>(true).First(b=>b.name==name&&b.gameObject.activeInHierarchy).onClick.Invoke();
        Func<string,Task> capture=async name=>{await Task.Delay(100);Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(Path.Combine(dir,name+".png"));await Task.Delay(150);};
        try
        {
            p.Diagnostic();p.Place(TowerKind.Single,new Vector2Int(14,15));p.HandleWorldClick(new Vector2Int(14,15));
            await capture("tower-spec");click("TowerBuyTab");await capture("tower-purchase");
            click("PlayerSpecs");await capture("player-spec");click("PlayerBuyTab");await capture("player-purchase");
            click("InspectorFold");await capture("folded");
            return new{captured=true,resolution=Screen.width+"x"+Screen.height,timeScale=Time.timeScale,captureDelta=Time.captureDeltaTime,conditions="Owned Play, diagnostic resources,real install,UI callbacks,adapter temporarily disabled,no camera/zoom/TimeScale change; functional presentation QA not human play/balance"};
        }
        finally{p.enabled=previous;}
    }
}
