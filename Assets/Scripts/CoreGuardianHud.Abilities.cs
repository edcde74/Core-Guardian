using CoreGuardian;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class CoreGuardianHud
{
    RectTransform playerAbilities,towerAbilities;
    void StyleAbilityPurchase(Button button)
    {
        button.GetComponent<CoreGuardianHudReaction>().InsetAbilityCell=true;
        button.GetComponent<CoreGuardianHudReaction>().SetLabelLayout(new Vector2(62,-8),new Vector2(228,48));
        var label=button.transform.Find("Label").GetComponent<Text>();label.fontSize=16;
        var icon=button.GetComponentsInChildren<Image>().First(i=>i.transform!=button.transform);
        icon.rectTransform.anchoredPosition=new Vector2(16,-16);icon.rectTransform.sizeDelta=new Vector2(32,32);
    }
    RectTransform BuildAbilityPlate(RectTransform panel)
    {
        var plate=Box("AbilityPlate",panel,new Vector2(0,1),new Vector2(264,-52),new Vector2(612,128));
        plate.GetComponent<Image>().raycastTarget=false;
        // One recessed plate, with quiet rules instead of four competing card borders.
        foreach(bool vertical in new[]{true,false})
        {
            var line=Box(vertical?"ColumnRule":"RowRule",plate,new Vector2(0,1),vertical?new Vector2(306,-12):new Vector2(16,-64),vertical?new Vector2(1,104):new Vector2(580,1),false);
            var image=line.gameObject.AddComponent<Image>();image.color=new Color(.45f,.5f,.54f,.45f);image.raycastTarget=false;
        }
        string[] keys={"damage","rate","range","projectileSpeed"};
        for(int i=0;i<keys.Length;i++)
        {
            var cell=Box(keys[i],plate,new Vector2(0,1),new Vector2(i%2*306,-i/2*64),new Vector2(306,64),false);
            CoreGuardianHudSkin.Icon(cell,CoreGuardianHudSkin.Symbol(keys[i]),new Vector2(16,-16),new Vector2(32,32));
            var label=Text("Name",cell,new Vector2(62,-6),new Vector2(228,22),16);label.color=new Color(.78f,.82f,.85f);label.text=StatName(keys[i]);
            Text("Value",cell,new Vector2(62,-28),new Vector2(228,30),22);
        }
        return plate;
    }
    void RefreshAbilityPlate(RectTransform plate,CombatStats stats,bool support)
    {
        plate.gameObject.SetActive(true);
        string[] keys={"damage","rate","range","projectileSpeed"};
        string[] values={support?"+"+stats.BuffDamage.ToString("0.##")+"%":StatDisplay(stats.Damage,"damage"),stats.Rate.ToString("0.##")+"발/초",stats.Range.ToString("0.##")+"타일",stats.ProjectileSpeed.ToString("0.##")+"타일/초"};
        for(int i=0;i<keys.Length;i++)
        {
            var cell=(RectTransform)plate.Find(keys[i]);cell.gameObject.SetActive(!purchaseOpen&&(!support||i==0||i==2));
            cell.anchoredPosition=support&&i==2?new Vector2(306,0):new Vector2(i%2*306,-i/2*64);
            cell.Find("Name").GetComponent<Text>().text=support?(i==0?"공격력 증가":"지원 범위"):StatName(keys[i]);
            cell.Find("Value").GetComponent<Text>().text=values[i];
            cell.GetComponentInChildren<Image>().sprite=CoreGuardianHudSkin.Sprite(support&&i==0?"Augment":CoreGuardianHudSkin.Symbol(keys[i]));
        }
        plate.Find("RowRule").gameObject.SetActive(!support);
        plate.sizeDelta=new Vector2(612,support?72:128);
        ((RectTransform)plate.Find("ColumnRule")).sizeDelta=new Vector2(1,support?48:104);
    }
}
