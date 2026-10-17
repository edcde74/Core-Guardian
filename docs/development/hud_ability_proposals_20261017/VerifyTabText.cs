using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
public static class VerifyAbilityTabText
{
    static void Need(bool value,string message){if(!value)throw new Exception(message);}
    static void Check(Transform panel)
    {
        foreach(var button in panel.GetComponentsInChildren<Button>().Where(b=>b.name.EndsWith("Tab")))
        {
            var rect=(RectTransform)button.transform;var label=button.GetComponentInChildren<Text>().rectTransform;
            var corners=new Vector3[4];label.GetWorldCorners(corners);var local=corners.Select(c=>rect.InverseTransformPoint(c)).ToArray();
            Need(local.Min(c=>c.y)>=rect.rect.yMin+4,"Tab text needs bottom inset after reaction update: "+button.name);
            Need(local.Max(c=>c.y)<=rect.rect.yMax-4,"Tab text needs top inset: "+button.name);
        }
    }
    public static async Task<object> Main()
    {
        var p=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();Need(Application.isPlaying&&p!=null&&p.Ready,"Owned Play required");
        var root=p.transform.Find("CombatHUD");
        Action<string> click=n=>root.GetComponentsInChildren<Button>(true).First(b=>b.name==n).onClick.Invoke();
        click("PlayerSpecs");await Task.Delay(100);Check(root.Find("PlayerGrowthPanel"));
        click("PlayerBuyTab");await Task.Delay(100);Check(root.Find("PlayerGrowthPanel"));
        click("PlayerSpecTab");await Task.Delay(100);Check(root.Find("PlayerGrowthPanel"));
        p.Diagnostic();p.Place(CoreGuardian.TowerKind.Single,new Vector2Int(14,15));p.HandleWorldClick(new Vector2Int(14,15));
        await Task.Delay(100);Check(root.Find("TowerInformation"));click("TowerBuyTab");await Task.Delay(100);Check(root.Find("TowerInformation"));
        return new{passed=true,checks="Player/tower current/upgrade tabs retain inner padding after layout switches and reaction updates",kind="Actual HUD callbacks and diagnostic install; not human play"};
    }
}
