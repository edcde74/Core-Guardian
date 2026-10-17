using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
public static class VerifyAbilityA
{
    static void Need(bool v,string m){if(!v)throw new Exception(m);}
    public static async Task<object> Main()
    {
        var p=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();Need(Application.isPlaying&&p!=null&&p.Ready,"Owned Play required");
        var root=p.transform.Find("CombatHUD");Action<string> click=n=>root.GetComponentsInChildren<Button>(true).First(b=>b.name==n).onClick.Invoke();
        click("PlayerSpecs");await Task.Delay(100);
        var panel=root.Find("PlayerGrowthPanel");var plate=panel.Find("AbilityPlate");Need(plate!=null&&plate.gameObject.activeInHierarchy,"A inset ability panel must exist");
        foreach(string n in new[]{"damage","rate","range","projectileSpeed"}){var cell=plate.Find(n);Need(cell!=null&&cell.gameObject.activeSelf,"Ability cell: "+n);Need(cell.GetComponentInChildren<Image>().sprite!=null,"Ability symbol: "+n);Need(!string.IsNullOrEmpty(cell.Find("Value").GetComponent<Text>().text),"Live value: "+n);}
        Need(CoreGuardianHudSkin.Symbol("rate")!=CoreGuardianHudSkin.Symbol("projectileSpeed"),"Distinct attack/projectile speed symbols");
        Need(CoreGuardianHudSkin.Symbol("PlayerSpecTab")!=CoreGuardianHudSkin.Symbol("PlayerBuyTab"),"Distinct view/upgrade action");
        click("PlayerBuyTab");await Task.Delay(100);Need(!plate.gameObject.activeSelf,"Buy mode hides current plate");
        p.Diagnostic();float before=p.Session.Player.Damage;panel.Find("damage").GetComponent<Button>().onClick.Invoke();
        Need(p.Session.Player.Damage>before,"Actual upgrade changes Session value");
        click("PlayerSpecTab");await Task.Delay(100);Need(plate.gameObject.activeSelf,"Current mode restores plate");
        Need(plate.Find("damage/Value").GetComponent<Text>().text==Mathf.FloorToInt(p.Session.Player.Damage+.5f).ToString(),"Live stat reflects purchase with existing rounding");
        p.Diagnostic();p.Place(CoreGuardian.TowerKind.Single,new Vector2Int(14,15));p.HandleWorldClick(new Vector2Int(14,15));await Task.Delay(100);
        var tower=root.Find("TowerInformation");Need(tower.Find("AbilityPlate").gameObject.activeInHierarchy,"Tower inset plate");
        click("TowerBuyTab");await Task.Delay(100);Need(!tower.Find("AbilityPlate").gameObject.activeSelf,"Tower purchase hides stats");click("TowerSpecTab");await Task.Delay(100);
        Need(p.Place(CoreGuardian.TowerKind.Support,new Vector2Int(15,15)),"Support installation");p.HandleWorldClick(new Vector2Int(15,15));await Task.Delay(100);
        var support=tower.Find("AbilityPlate");Need(support.Find("damage/Name").GetComponent<Text>().text=="공격력 증가","Support buff label");
        Need(!support.Find("rate").gameObject.activeSelf&&!support.Find("projectileSpeed").gameObject.activeSelf&&support.Find("range").gameObject.activeSelf,"Support has only two relevant abilities");
        var hud=p.GetComponent<CoreGuardianHud>();Need(hud.TryFoldDetails(),"Fold consumes first click");Need(p.SelectedTower>=0,"Folding retains target");
        return new{passed=true,kind="HUD callbacks and diagnostic installation; not human play or balance",checks="player/tower inset plate, purchase refresh and rounding, distinct symbols, tab switching, support two abilities, folding retains target"};
    }
}
