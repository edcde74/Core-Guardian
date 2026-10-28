using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Shared sprite skin; texts stay live and decorative graphics never intercept input.
public static class CoreGuardianHudSkin
{
    static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
    public static Sprite Sprite(string name){if(!sprites.TryGetValue(name,out var sprite)||sprite==null){sprite=Resources.Load<Sprite>("CoreGuardian/Hud/"+name);sprites[name]=sprite;}return sprite;}
    public static void Frame(Image image, string name="Panel")
    {
        image.sprite=Sprite(name); image.type=Image.Type.Sliced; image.color=Color.white;
        // Keep the ornamental border out of the text area, including short buttons.
        image.pixelsPerUnitMultiplier=2.5f;
    }
    public static Image Icon(Transform parent,string name,Vector2 position,Vector2 size)
    {
        var obj=new GameObject("Symbol_"+name,typeof(RectTransform),typeof(Image));
        var rt=obj.GetComponent<RectTransform>();rt.SetParent(parent,false);
        rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);rt.anchoredPosition=position;rt.sizeDelta=size;
        var image=obj.GetComponent<Image>();image.sprite=Sprite(name);image.preserveAspect=true;image.raycastTarget=false;return image;
    }
    public static string Symbol(string name)
    {
        if(name.StartsWith("LabKind_"))name="Install_"+name.Substring(8); // 연구소 종류 탭 = 설치 아이콘
        if(name=="InfoClose")return "Cancel";
        if(name=="Installation"||name=="공격 포탑")return "TowerAttackCategory";
        if(name=="지원 포탑"||name=="Install_Support")return "TowerSupport";
        if(name=="Install_Single")return "TowerSingle";
        if(name=="Install_Multi")return "TowerMulti";
        if(name=="Install_Frost"||name=="Tower_slowPercent")return "TowerFrost";
        if(name=="Install_Flame"||name=="Tower_burnDuration")return "TowerFlame";
        if(name=="Install_Rail"||name=="Tower_pierceFalloff")return "TowerRail";
        if(name=="AugmentReroll")return "Reroll";
        if(name=="AreaBlast")return "AreaBlast";
        if(name=="PlayerSpecs"||name.EndsWith("SpecTab"))return "AbilityList";
        if(name=="PlayerGrowth"||name.EndsWith("BuyTab"))return "AbilityUpgrade";
        if(name=="damage"||name=="Tower_damage")return "StatDamage";
        if(name=="rate"||name=="Tower_rate")return "StatRate";
        if(name=="range"||name=="Tower_range")return "StatRange";
        if(name=="projectileSpeed"||name=="Tower_projectileSpeed")return "StatProjectile";
        if(name=="Tower_buffDamage")return "Augment";
        if(name.Contains("Fold")||name=="接기"||name=="접기"||name.Contains("돌아가기"))return "Play";
        if(name.Contains("Reload"))return "Restart";
        if(name.Contains("Speed")||name=="rate"||name=="Tower_rate"||name.Contains("projectileSpeed"))return "Speed";
        if(name.Contains("Cancel")||name=="Destroy")return "Cancel";
        if(name=="Start")return "Play";
        if(name=="PlayerSpecs"||name.Contains("Growth")||name.Contains("range")||name.Contains("buffDamage"))return "Upgrade";
        if(name.Contains("damage")||name.Contains("Diagnostic")||name.Contains("공격"))return "Damage";
        return "Install";
    }
}

public sealed class CoreGuardianHudReaction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    Button button;Image image;RectTransform label;Vector2 origin;bool hover,pressed;float flash;
    string spriteKey;Sprite keyedSprite;
    public Func<bool> Selected;
    public bool InsetAbilityCell;
    void Awake(){button=GetComponent<Button>();image=GetComponent<Image>();button.transition=Selectable.Transition.None;}
    void Start(){var text=GetComponentInChildren<Text>();if(text!=null){label=text.rectTransform;origin=label.anchoredPosition;}}
    // Layout changes must also move the resting point used by the press animation.
    public void SetLabelLayout(Vector2 position,Vector2 size)
    {
        if(label==null){var text=GetComponentInChildren<Text>();if(text!=null)label=text.rectTransform;}
        origin=position;
        if(label!=null){label.sizeDelta=size;label.anchoredPosition=origin+(pressed&&button!=null&&button.interactable?new Vector2(0,-3):Vector2.zero);}
    }
    public void Confirm(){flash=Time.unscaledTime+.18f;}
    void Update()
    {
        bool usable=button!=null&&button.interactable;
        string key=!usable?"Disabled":hover||Time.unscaledTime<flash||Selected!=null&&Selected()?"Hover":"Button";
        // Same skin lookup result; skip the string-keyed dictionary while the key and sprite are unchanged.
        if(!ReferenceEquals(key,spriteKey)||keyedSprite==null){keyedSprite=CoreGuardianHudSkin.Sprite(key);spriteKey=key;}
        image.sprite=keyedSprite;
        var color=!usable?new Color(.7f,.72f,.75f):pressed?new Color(.7f,.8f,.8f):Color.white;
        if(InsetAbilityCell)
        {
            // Preserve the shared recessed plate; only the hovered/confirmed cell gets a frame.
            bool highlight=usable&&(hover||Time.unscaledTime<flash);
            color=highlight?(pressed?new Color(.7f,.8f,.8f,.75f):new Color(1,1,1,.75f)):Color.clear;
        }
        // One final assignment: the former interim color dirtied inset cells' vertices every frame.
        image.type=Image.Type.Sliced;image.color=color;
        if(label!=null)label.anchoredPosition=origin+(pressed&&usable?new Vector2(0,-3):Vector2.zero);
    }
    public void OnPointerEnter(PointerEventData e){hover=true;}
    public void OnPointerExit(PointerEventData e){hover=false;pressed=false;}
    public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)pressed=true;}
    public void OnPointerUp(PointerEventData e){pressed=false;}
    public void OnSelect(BaseEventData e){hover=true;}
    public void OnDeselect(BaseEventData e){hover=false;pressed=false;}
    void OnDisable(){hover=pressed=false;}
}
