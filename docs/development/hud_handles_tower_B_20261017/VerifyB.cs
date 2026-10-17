using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public static class VerifyHudB
{
    static void Need(bool v,string m){if(!v)throw new Exception(m);}
    public static async Task<object> Main()
    {
        var p=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();Need(Application.isPlaying&&p!=null&&p.Ready,"Owned Play required");
        var root=p.transform.Find("CombatHUD");var data=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
        foreach(string n in new[]{"TopFold","DamageFold","InspectorFold","InspectorReopen","AugmentFold"})
        {
            var button=root.GetComponentsInChildren<Button>(true).First(b=>b.name==n);var edge=button.transform.Find("HandleArt/HoverEdge");Need(edge!=null,"B cyan hover edge required: "+n);
            var hidden=new System.Collections.Generic.List<GameObject>();for(var t=button.transform;t!=root;t=t.parent)if(!t.gameObject.activeSelf)hidden.Add(t.gameObject);
            foreach(var g in hidden.AsEnumerable().Reverse())g.SetActive(true);var group=edge.GetComponent<CanvasGroup>();
            try{
            ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerExitHandler);Need(group.alpha==0,"Idle edge hidden");
            ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerEnterHandler);Need(group.alpha>.9f,"Hover edge visible");
            ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerDownHandler);Need(group.alpha>0&&group.alpha<.9f,"Press dims cyan edge");
            ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerUpHandler);Need(group.alpha>.9f,"Release restores hover");
            ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerExitHandler);Need(group.alpha==0,"Exit clears hover");
            }finally{foreach(var g in hidden)g.SetActive(false);}
        }
        Need(CoreGuardianHudSkin.Symbol("Install_Single")!=CoreGuardianHudSkin.Symbol("Install_Multi")&&CoreGuardianHudSkin.Symbol("Install_Multi")!=CoreGuardianHudSkin.Symbol("Install_Support"),"Three distinct tower symbols");
        Action<string> click=n=>root.GetComponentsInChildren<Button>(true).First(b=>b.name==n&&b.gameObject.activeInHierarchy).onClick.Invoke();
        click("Installation");click("공격 포탑");await Task.Delay(100);
        foreach(string n in new[]{"Install_Single","Install_Multi"}){var b=root.GetComponentsInChildren<Button>().First(x=>x.name==n);Need(b.GetComponentsInChildren<Image>().Any(i=>i.sprite!=null&&i.sprite.name==CoreGuardianHudSkin.Symbol(n)),"B sprite on actual button: "+n);}
        click("분류로 돌아가기");click("지원 포탑");await Task.Delay(100);
        Need(root.GetComponentsInChildren<Button>().Any(x=>x.name=="Install_Support"),"Support installation remains available as a list");
        return new{passed=true,checks="Five handle hover/press/exit event paths, distinct installation symbols, category navigation",kind="EventSystem dispatch and actual HUD callbacks, not physical mouse or human play"};
    }
}
