using System;
using System.Linq;
using System.Threading.Tasks;
using CoreGuardian;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public static class VerifyHudPointer
{
    static void Need(bool v,string m){if(!v)throw new Exception(m);}
    public static async Task<object> Main()
    {
        var p=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();var h=p.GetComponent<CoreGuardianHud>();
        Need(Application.isPlaying&&p.Ready,"Owned Play required");
        var root=p.transform.Find("CombatHUD");var previousMouse=Mouse.current;var mouse=InputSystem.AddDevice<Mouse>();
        float oldTime=Time.timeScale;Time.timeScale=0;
        Func<Vector2Int,Vector2> point=c=>(Vector2)Camera.main.WorldToScreenPoint(p.World(c));
        Func<string,Vector2> ui=name=>{var b=root.GetComponentsInChildren<Button>(true).First(x=>x.name==name);var r=(RectTransform)b.transform;return RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));};
        Func<Vector2,bool,Task> eventAt=async(pos,down)=>{InputSystem.QueueStateEvent(mouse,new MouseState{position=pos,buttons=(ushort)(down?1:0)});await Task.Delay(100);};
        Func<Vector2,Task> click=async(pos)=>{await eventAt(pos,true);await eventAt(pos,false);};
        try
        {
            p.Diagnostic();await Task.Delay(120);
            var floor=new Vector2Int(5,4);var tower=new Vector2Int(14,15);
            await click(ui("PlayerSpecs"));Need(root.Find("PlayerGrowthPanel").gameObject.activeSelf,"Actual UI click opens player spec");
            // Down on UI, disappear while held, release on board. It must not become any world action.
            int count=p.Session.Towers.Count;
            await eventAt(ui("PlayerSpecTab"),true);h.TryFoldDetails();await eventAt(point(floor),false);
            Need(p.Session.Towers.Count==count&&p.SelectedTower==-1,"UI gesture cannot fall through after panel disappears");
            await click(ui("Installation"));await click(ui("공격 포탑"));await click(ui("Install_Single"));
            Need(p.Installing.HasValue&&p.Session.Towers.Count==count,"Install button release cannot also place");
            await click(point(tower));Need(p.Session.Towers.Count==count+1&&!p.Installing.HasValue,"Actual board click installs exactly once");
            int id=p.SelectedTower;Need(!root.Find("TowerInformation").gameObject.activeSelf,"Installation never auto opens spec");
            await click(point(floor));Need(p.SelectedTower==id&&!root.Find("TowerInstallPanel").gameObject.activeSelf,"First floor click folds chooser and retains ID");
            await click(point(floor));Need(p.SelectedTower==-1,"Second floor click deselects");
            await click(point(tower));Need(p.SelectedTower==id&&root.Find("TowerInformation").gameObject.activeSelf,"Actual tower click opens current spec");
            await click(ui("InspectorFold"));Need(p.SelectedTower==id&&!root.Find("TowerInformation").gameObject.activeSelf,"Actual attached handle folds only");
            await click(point(tower));Need(root.Find("TowerInformation").gameObject.activeSelf,"Same tower click reopens");
            return new{passed=true,checks="Actual InputSystem mouse UI open,UI down-hide-release,chooser click no placement,one install,floor fold/deselect,same-tower reopen,handle hit",kind="Temporary mouse and diagnostic currency,TimeScale0 input QA; not human play/balance"};
        }
        finally{Time.timeScale=oldTime;InputSystem.RemoveDevice(mouse);if(previousMouse!=null&&previousMouse.added)previousMouse.MakeCurrent();}
    }
}
