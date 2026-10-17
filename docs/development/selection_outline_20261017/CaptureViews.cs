using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CoreGuardian;
using UnityEngine;
using UnityEngine.UI;

public static class CaptureSelectionOutlineViews
{
    public static async Task<object> Main()
    {
        if(!Application.isPlaying)throw new Exception("Owned Play required");
        var p=UnityEngine.Object.FindFirstObjectByType<CoreGuardianPrototype>();var setting=p.GetComponent<CoreGuardianSelectionOutline>();
        bool previous=p.enabled;var color=setting.color;int width=setting.widthPixels;p.enabled=false;
        string dir=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"docs/development/selection_outline_20261017");
        var root=p.transform.Find("CombatHUD");
        Action<string> click=name=>root.GetComponentsInChildren<Button>(true).First(b=>b.name==name&&b.gameObject.activeInHierarchy).onClick.Invoke();
        Func<string,Task> capture=async name=>{await Task.Delay(120);Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(Path.Combine(dir,name+".png"));await Task.Delay(180);};
        try
        {
            // Diagnostic resources only for exhibiting all three tower silhouettes in one stationary QA.
            p.Diagnostic();p.Place(TowerKind.Single,new Vector2Int(14,15));p.Place(TowerKind.Multi,new Vector2Int(16,15));p.Place(TowerKind.Support,new Vector2Int(18,15));
            p.HandleWorldClick(new Vector2Int(14,15));await capture("single-2px");
            click("TowerBuyTab");await capture("single-upgrade");
            setting.widthPixels=0;await capture("single-off");setting.widthPixels=width;
            p.HandleWorldClick(new Vector2Int(16,15));await capture("multi-2px");
            p.HandleWorldClick(new Vector2Int(18,15));await capture("support-2px");
            click("PlayerSpecs");await capture("player-2px");
            click("PlayerBuyTab");setting.color=new Color(.2f,.9f,1,1);setting.widthPixels=4;await capture("player-custom-4px");
            setting.color=color;setting.widthPixels=width;click("InspectorFold");await capture("folded-deselected");
            if(p.SelectedTower!=-1||setting.IsVisible)throw new Exception("Fold must deselect and hide outline");
            return new {passed=true,resolution=Screen.width+"x"+Screen.height,selected=p.SelectedTower,outline=setting.IsVisible,conditions="Owned Play; diagnostic resources and actual placement; UI callbacks; adapter temporarily disabled; camera/time unchanged; Overlay HUD included; not human play/balance/performance"};
        }
        finally{setting.color=color;setting.widthPixels=width;p.enabled=previous;}
    }
}
