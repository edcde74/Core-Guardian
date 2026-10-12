using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

public static class VerifyGoldFeedback
{
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static TextMeshProUGUI Gold()=>UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>().Single(t=>t.name=="GoldNumber");
    public static object CheckGroup()
    {
        Canvas.ForceUpdateCanvases();
        var text=Gold();
        var icon=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>().SingleOrDefault(i=>i.name=="GoldGain");
        Require(icon!=null,"Gold symbol is not grouped in the HUD with its number");
        Require(text.transform.parent==icon.transform.parent,"Symbol and number have different movement parents");
        Require(text.text=="+3","Kill reward value changed");
        Require(!text.raycastTarget&&!icon.raycastTarget,"Gold feedback blocks clicks");
        Require(icon.sprite.name=="GoldGain"&&text.font.name=="PixelifySansBoldDamage","Approved gold sprite/font changed");
        Require(icon.rectTransform.sizeDelta==new Vector2(36,36),"Gold icon size incorrect");
        float iconRight=icon.rectTransform.anchoredPosition.x+(1-icon.rectTransform.pivot.x)*icon.rectTransform.rect.width;
        float numberLeft=text.rectTransform.anchoredPosition.x-text.rectTransform.pivot.x*text.rectTransform.rect.width;
        Require(Mathf.Abs(numberLeft-iconRight-4)<.01f,"Number not immediately beside gold symbol");
        Require(Mathf.Abs(text.rectTransform.anchoredPosition.y-icon.rectTransform.anchoredPosition.y)<.01f,"Vertical alignment differs");
        Require(!UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Any(r=>r.name=="GoldGain"),"Duplicate world-space gold symbol remains");
        return new {success=true,text=text.text,iconPixels=36,gapPixels=numberLeft-iconRight,sharedParent=text.transform.parent.name};
    }
    public static object CheckMotionAndExpiry()
    {
        var text=Gold();var hud=UnityEngine.Object.FindAnyObjectByType<CoreGuardianHud>();
        var icon=text.transform.parent.GetComponentInChildren<UnityEngine.UI.Image>();
        var list=(System.Collections.IEnumerable)typeof(CoreGuardianHud).GetField("damageNumbers",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(hud);
        object entry=list.Cast<object>().Single(e=>(TextMeshProUGUI)e.GetType().GetField("Text").GetValue(e)==text);
        var started=entry.GetType().GetField("Started");
        var late=typeof(CoreGuardianHud).GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance);
        started.SetValue(entry,Time.time);late.Invoke(hud,null);
        var parent=(RectTransform)text.transform.parent;var initial=parent.anchoredPosition;
        started.SetValue(entry,Time.time-.6f);late.Invoke(hud,null);
        Require(Mathf.Abs(parent.anchoredPosition.y-initial.y-25.2f)<.02f,"Group rise incorrect");
        Require(Mathf.Abs(text.color.a-icon.color.a)<.001f,"Symbol and number fade separately");
        Require(Mathf.Abs(text.color.a-.5f)<.002f,"Fade timing incorrect");
        started.SetValue(entry,Time.time-.9f);late.Invoke(hud,null);
        return new {success=true,risePixels=25.2f,sharedAlpha=.5f,expiryRequested=true};
    }
    public static object CheckExpired()
    {
        Require(GameObject.Find("GoldFeedback")==null&&GameObject.Find("GoldGain")==null&&GameObject.Find("GoldNumber")==null,"Gold group survived expiry");
        return "Gold symbol and number expired together";
    }
    public static object CheckProjectileSizes()
    {
        var sprites=UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Where(r=>r.name.StartsWith("Projectile_")).ToArray();
        foreach(string name in new[]{"EngineerChip","SingleBolt","MultiBolt"})
        {
            var sprite=sprites.FirstOrDefault(r=>r.sprite.name==name);Require(sprite!=null,"Missing projectile "+name);
            float width=sprite.sprite.bounds.size.x*sprite.transform.localScale.x;
            Require(Mathf.Abs(width-(name=="EngineerChip"?.2f:.275f))<.0001f,"Projectile size incorrect: "+name);
        }
        return new {success=true,engineerWidth=.2f,towerWidth=.275f};
    }
    public static object CheckReset()
    {
        Require(GameObject.Find("GoldFeedback")==null&&GameObject.Find("GoldGain")==null&&GameObject.Find("GoldNumber")==null,"Reset retained gold group");
        return "Reset cleared grouped feedback";
    }
}

