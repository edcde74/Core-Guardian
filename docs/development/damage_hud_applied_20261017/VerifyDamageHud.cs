using System;
using System.Linq;
using System.Reflection;
using CoreGuardian;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

// Focused UI QA in an owned fresh Play. Injected display totals are NOT balance measurements.
public static class VerifyDamageHud
{
    static void Need(bool condition,string message){if(!condition)throw new Exception(message);}
    static void NeedClickable(RectTransform toggle)
    {
        Canvas.ForceUpdateCanvases();var hits=new List<RaycastResult>();
        var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,toggle.TransformPoint(toggle.rect.center))};
        EventSystem.current.RaycastAll(pointer,hits);
        Need(hits.Count>0&&hits[0].gameObject==toggle.gameObject,"Handle center must be actual topmost UI pointer target");
    }
    public static object Main()
    {
        var p=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
        Need(Application.isPlaying&&p!=null&&p.Ready,"Owned fresh Play required");
        var s=p.Session;Need(s.Wave==1&&s.Phase==SessionPhase.Preparation,"Fresh preparation required");
        s.Towers.Add(new TowerState{Id=1001,Kind=TowerKind.Multi,Stats=new CombatStats()});
        s.Towers.Add(new TowerState{Id=1002,Kind=TowerKind.Single,Stats=new CombatStats()});
        s.DamageTotals["Tower:1001"]=425;s.DamageTotals["Tower:1002"]=210;s.DamageTotals["Player"]=7;
        var hud=p.GetComponent<CoreGuardianHud>();
        typeof(CoreGuardianHud).GetMethod("Refresh",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(hud,null);
        Canvas.ForceUpdateCanvases();
        var root=p.transform.Find("CombatHUD");var panel=root.Find("DamagePanel") as RectTransform;
        var content=panel.Find("Scroll/Viewport/Content");
        var rows=content.Cast<Transform>().Where(t=>t.Find("BarTrack")!=null).ToArray();
        Need(rows.Length==3,"Expected three framed BarTrack rows");
        foreach(var row in rows)
        {
            var track=row.Find("BarTrack") as RectTransform;var fill=track.Find("Bar") as RectTransform;
            Need(fill!=null&&fill.rect.width>0&&fill.rect.width<=track.rect.width-6,"Fill must stay inside fixed track with padding");
            Need(row.Find("RoleIcon")?.GetComponent<Image>()?.sprite!=null,"Role sprite missing");
        }
        Need(rows.Select(r=>r.Find("RoleIcon").GetComponent<Image>().sprite.name).Distinct().Count()==3,"Roles must use different symbols");
        var toggle=root.Find("DamageFold") as RectTransform;Need(toggle!=null,"Toggle missing");
        Need(toggle.GetComponentsInChildren<Text>().Length==0,"Handle must have no label box");
        Need(toggle.sizeDelta.x<=32&&toggle.sizeDelta.y>=80,"Thin handle geometry");
        Need(Mathf.Abs(toggle.anchoredPosition.y-(panel.anchoredPosition.y-panel.sizeDelta.y*.5f))<.01f,"Handle must align with left edge midpoint");
        Need(Mathf.Abs(toggle.anchoredPosition.x-(panel.anchoredPosition.x-panel.sizeDelta.x))<.01f,"Expanded handle must touch panel left edge");
        Need(Mathf.Abs(toggle.TransformPoint(toggle.rect.center).y-panel.TransformPoint(panel.rect.center).y)<.01f,"Handle visual center must match panel center");
        NeedClickable(toggle);
        var beforeGold=s.Gold;var beforeCore=s.CoreEnergy;var beforeWave=s.Wave;
        toggle.GetComponent<Button>().onClick.Invoke();
        Need(!panel.gameObject.activeSelf&&toggle.gameObject.activeInHierarchy,"Fold must leave usable handle");
        Need(toggle.anchoredPosition.x>=-32,"Folded handle must stay at screen right edge");
        NeedClickable(toggle);
        s.DamageTotals["Player"]=70;
        typeof(CoreGuardianHud).GetMethod("Refresh",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(hud,null);
        toggle.GetComponent<Button>().onClick.Invoke();
        Need(panel.gameObject.activeSelf,"Unfold must restore panel");
        Need(s.DamageTotals["Player"]==70&&s.Gold==beforeGold&&s.CoreEnergy==beforeCore&&s.Wave==beforeWave,"Visibility must preserve session");
        Need(content.GetComponentsInChildren<Text>(true).Any(t=>t.text.Contains("70")),"Hidden damage update must be visible on reopen");
        return new{passed=true,checks="framed inset fill,3 distinct roles,thin brass handle,no detached label,edge anchors,fold/reopen,preserved totals",method="Injected display-only UI QA, not balance or human play"};
    }
}
