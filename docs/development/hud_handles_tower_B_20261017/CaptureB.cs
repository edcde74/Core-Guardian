using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public static class CaptureHudB
{
    public static async Task<object> Main()
    {
        var p=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();if(!Application.isPlaying||p==null)throw new Exception("Owned Play required");p.Diagnostic();
        var root=p.transform.Find("CombatHUD");Action<string> click=n=>root.GetComponentsInChildren<Button>(true).First(b=>b.name==n&&b.gameObject.activeInHierarchy).onClick.Invoke();
        Func<string,Task> capture=async n=>{await Task.Delay(120);ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("docs/development/hud_handles_tower_B_20261017/"+n+".png"));await Task.Delay(150);};
        click("Installation");if(!root.Find("TowerInstallPanel").gameObject.activeSelf)click("Installation");await capture("categories");
        click("공격 포탑");await capture("attack_list");click("분류로 돌아가기");click("지원 포탑");await capture("support_list");
        p.GetComponent<CoreGuardianHud>().TryFoldDetails();p.StartCombat();
        var data=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
        var top=root.Find("TopFold").gameObject;var damage=root.Find("DamageFold").gameObject;
        await capture("handles_idle");
        ExecuteEvents.Execute(top,data,ExecuteEvents.pointerEnterHandler);ExecuteEvents.Execute(damage,data,ExecuteEvents.pointerEnterHandler);await capture("handles_hover");
        ExecuteEvents.Execute(top,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(damage,data,ExecuteEvents.pointerDownHandler);await capture("handles_pressed");
        ExecuteEvents.Execute(top,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(top,data,ExecuteEvents.pointerExitHandler);ExecuteEvents.Execute(damage,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(damage,data,ExecuteEvents.pointerExitHandler);
        return new{captured=6,screen=new{Screen.width,Screen.height},kind="Actual Overlay with diagnostic session, callbacks and dispatched pointer states; no camera/time changes"};
    }
}
