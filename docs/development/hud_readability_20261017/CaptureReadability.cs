using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
public static class CaptureHudReadability
{
    public static async Task<object> Main()
    {
        var p=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
        if(!Application.isPlaying||p==null||!p.Ready)throw new Exception("Owned Play required");
        bool before=p.enabled;p.enabled=false;
        var root=p.transform.Find("CombatHUD");
        Action<string> click=name=>root.GetComponentsInChildren<Button>(true).First(b=>b.name==name).onClick.Invoke();
        string dir=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"docs/development/hud_readability_20261017");
        Func<string,Task> capture=async name=>{await Task.Delay(120);Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(Path.Combine(dir,name+".png"));await Task.Delay(150);};
        try
        {
            p.Diagnostic();click("PlayerSpecs");await capture("abilities");
            click("PlayerBuyTab");await capture("upgrades");
            click("InspectorFold");await capture("player-closed");
            p.StartCombat();p.Session.Tick(.01f,new Vector2(15,10));
            p.GetType().GetMethod("SynchronizeViews",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p,null);p.Notify(p.Session.LastMessage);
            await capture("combat-expanded");click("TopFold");await capture("combat-collapsed");
            click("TopFold");
            return new{captured=true,resolution=Screen.width+"x"+Screen.height,timeScale=Time.timeScale,captureDelta=Time.captureDeltaTime,kind="Overlay HUD,diagnostic resources and role encounter,adapter temporarily disabled,no camera/zoom/time changes; presentation QA,not human play/balance"};
        }
        finally{p.enabled=before;}
    }
}
