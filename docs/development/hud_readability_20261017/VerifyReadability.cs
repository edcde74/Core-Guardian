using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
public static class VerifyHudReadability
{
    static void Need(bool value,string message){if(!value)throw new Exception(message);}
    public static object Main()
    {
        var p=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
        Need(Application.isPlaying&&p!=null&&p.Ready,"Owned fresh Play required");
        var root=p.transform.Find("CombatHUD");var h=p.GetComponent<CoreGuardianHud>();
        Action<string> click=name=>root.GetComponentsInChildren<Button>(true).First(b=>b.name==name).onClick.Invoke();
        click("PlayerSpecs");
        var panel=(RectTransform)root.Find("PlayerGrowthPanel");var stats=(RectTransform)panel.Find("Stats");
        Need(stats.anchoredPosition.x>240,"Ability text must use the empty right side, beside the navigation");
        Need(panel.GetComponentsInChildren<Text>().All(t=>!t.text.Contains("스펙")),"Use player-facing ability terms");
        click("InspectorFold");
        Need(!root.Find("InspectorSummary").gameObject.activeSelf,"No redundant collapsed player strip");
        p.StartCombat();
        var stage=root.Find("StagePlate");Need(stage.gameObject.activeSelf,"Combat details start expanded");
        var handle=(RectTransform)root.Find("TopFold");float expanded=handle.anchoredPosition.y;
        click("TopFold");Need(!stage.gameObject.activeSelf,"Top handle must actually hide round/combat/enemy details");
        Need(root.Find("TopStatus").gameObject.activeSelf,"Core and resource status must remain visible");
        Need(handle.gameObject.activeInHierarchy&&handle.anchoredPosition.y!=expanded,"Reopen handle must move next to remaining status");
        h.GetType().GetMethod("Refresh",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(h,null);
        Need(!stage.gameObject.activeSelf,"Refresh must preserve manual collapse");
        click("TopFold");Need(stage.gameObject.activeSelf,"Handle restores the round/enemy details");
        return new{passed=true,checks="right-side ability text,game terminology,no redundant player strip,actual combat details fold,resources retained,moving reopen handle,preserved collapse",kind="Actual HUD callback QA, not human play or balance"};
    }
}
