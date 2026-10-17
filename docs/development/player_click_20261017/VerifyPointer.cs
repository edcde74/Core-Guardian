using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CoreGuardian;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public static class VerifyPlayerClickPointer
{
    static void Need(bool ok,string message){if(!ok)throw new Exception(message);}
    public static async Task<object> Main()
    {
        var p=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();var hud=p.GetComponent<CoreGuardianHud>();var root=p.transform.Find("CombatHUD");
        Need(Application.isPlaying&&p.Ready,"Own Play required");
        float oldTime=Time.timeScale;var oldMouse=Mouse.current;var oldBehavior=InputSystem.settings.backgroundBehavior;
        var mouse=InputSystem.AddDevice<Mouse>();mouse.MakeCurrent();Time.timeScale=0;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        GameObject shield=null;
        Func<Vector2,bool,Task> state=async(pos,down)=>{InputSystem.QueueStateEvent(mouse,new MouseState{position=pos,buttons=(ushort)(down?1:0)});await Task.Delay(120);};
        Func<Vector2,Task> click=async pos=>{await state(pos,true);await state(pos,false);};
        Func<string,Vector2> button=name=>{var r=(RectTransform)root.GetComponentsInChildren<Button>(true).First(b=>b.name==name).transform;return RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));};
        try
        {
            await Task.Delay(150);
            var body=p.player.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Body"&&r.enabled&&r.gameObject.activeInHierarchy);
            var world=body.bounds.center;world.y=body.bounds.max.y-.07f;var point=(Vector2)Camera.main.WorldToScreenPoint(world);
            var headCell=p.ground.WorldToCell(world);var footCell=p.player.CurrentCell;
            Need(headCell!=footCell,"QA must click upper artwork outside the player's ground cell");
            await click(point);Need(root.Find("PlayerGrowthPanel").gameObject.activeSelf&&hud.InspectionTarget==p.player.transform,"Upper body pointer selects player and opens current abilities");
            await click(point);Need(root.Find("PlayerGrowthPanel").gameObject.activeSelf,"Repeated player pointer keeps panel open");
            await click(button("Installation"));await click(button("공격 포탑"));await click(button("Install_Single"));
            Need(p.Installing.HasValue,"QA must enter actual placement before testing its priority");
            await click(point);Need(hud.InspectionTarget==null&&!root.Find("PlayerGrowthPanel").gameObject.activeSelf,"Installation click cannot open player panel");p.CancelInstall();hud.CancelInstallation();
            if(p.Session.Towers.Count==0)Need(p.Place(TowerKind.Single,new Vector2Int(14,15)),"Install one normal starting-resource tower");
            p.HandleWorldClick(p.Session.Towers.Single().Position);Need(p.SelectedTower>=0,"QA must establish a selected tower");await click(point);
            Need(p.SelectedTower==-1&&hud.InspectionTarget==p.player.transform,"Player replaces selected tower");
            await click(button("InspectorFold"));Need(hud.InspectionTarget==null,"Real handle deselects");
            // A transparent UI raycast shield above the actual character must own the gesture.
            shield=new GameObject("PlayerPointerQaShield",typeof(RectTransform),typeof(Image));var rect=shield.GetComponent<RectTransform>();rect.SetParent(root,false);rect.sizeDelta=new Vector2(100,100);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)root,point,null,out var local);rect.anchoredPosition=local;shield.GetComponent<Image>().color=Color.clear;
            await click(point);Need(hud.InspectionTarget==null,"UI over player blocks world selection");UnityEngine.Object.Destroy(shield);shield=null;await Task.Delay(100);
            await state(button("PlayerSpecs"),true);hud.TryFoldDetails();await state(point,false);Need(hud.InspectionTarget==null,"UI down/disappear/release on player cannot leak selection");
            await click(point);Need(hud.InspectionTarget==p.player.transform,"Player can be selected after leaving placement");
            Canvas.ForceUpdateCanvases();string path=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"docs/development/player_click_20261017/player-click.png");ScreenCapture.CaptureScreenshot(path);await Task.Delay(200);
            return new {passed=true,headCell=headCell.ToString(),footCell=footCell.ToString(),resolution=Screen.width+"x"+Screen.height,outline=p.GetComponent<CoreGuardianSelectionOutline>().IsVisible,conditions="Temporary InputSystem mouse,TimeScale0,normal starting resources,one actual tower install,UI shield/callback setup; not human play or balance"};
        }
        finally
        {
            if(shield!=null)UnityEngine.Object.Destroy(shield);Time.timeScale=oldTime;InputSystem.settings.backgroundBehavior=oldBehavior;
            InputSystem.RemoveDevice(mouse);if(oldMouse!=null&&oldMouse.added)oldMouse.MakeCurrent();
        }
    }
}
