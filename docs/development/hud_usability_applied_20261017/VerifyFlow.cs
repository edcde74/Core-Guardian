using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CoreGuardian;
using UnityEngine;
using UnityEngine.UI;

public static class VerifyHudFlow
{
    static void Need(bool v,string m){if(!v)throw new Exception(m);}
    public static async Task<object> Main()
    {
        var p=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();var h=p.GetComponent<CoreGuardianHud>();var s=p.Session;
        var root=p.transform.Find("CombatHUD");var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        Action refresh=()=>h.GetType().GetMethod("Refresh",flags).Invoke(h,null);
        Action<string> click=name=>root.GetComponentsInChildren<Button>(true).First(b=>b.name==name&&b.gameObject.activeInHierarchy).onClick.Invoke();
        Func<string,bool> shown=name=>root.Find(name).gameObject.activeSelf;
        bool enabled=p.enabled;p.enabled=false;
        try
        {
            // Isolated diagnostic resources/state; not a natural round or balance measurement.
            p.Diagnostic();refresh();
            click("PlayerSpecs");Need(shown("PlayerGrowthPanel"),"Player specs open");
            click("PlayerBuyTab");Need(root.Find("PlayerGrowthPanel/damage").gameObject.activeSelf,"Purchase grid active");
            int gold=s.Gold;float damage=s.Player.Damage;click("damage");Need(s.Gold<gold&&s.Player.Damage>damage,"Real Session purchase and currency");
            click("Installation");Need(!shown("PlayerGrowthPanel")&&shown("TowerInstallPanel"),"Install list replaces inspector");
            click("공격 포탑");click("Install_Single");Need(p.Installing.HasValue,"Placement entered");
            Need(!h.TryFoldDetails(),"Generic fold cannot cancel placement");
            Need(!p.Place(TowerKind.Single,new Vector2Int(0,12))&&p.Installing.HasValue,"Failed placement preserves mode");
            Need(p.Place(TowerKind.Single,new Vector2Int(14,15)),"Normal installation succeeds");
            int id=p.SelectedTower;Need(!shown("TowerInformation")&&shown("TowerInstallPanel"),"Success restores chooser, never auto expands spec");
            var floor=new Vector2Int(5,4);int crystals=s.Crystals,count=s.Towers.Count;
            p.HandleWorldClick(floor);Need(!shown("TowerInstallPanel")&&p.SelectedTower==id,"First floor closes chooser and retains selection");
            Need(s.Crystals==crystals&&s.Towers.Count==count,"Closing click cannot install");
            p.HandleWorldClick(floor);Need(p.SelectedTower==-1,"Second empty-floor click deselects");
            p.HandleWorldClick(new Vector2Int(14,15));Need(shown("TowerInformation"),"Tower selects current specs");
            click("InspectorFold");Need(!shown("TowerInformation")&&p.SelectedTower==id,"Handle folds without deselect");
            p.HandleWorldClick(new Vector2Int(14,15));Need(shown("TowerInformation"),"Same tower reopens specs");
            click("TowerBuyTab");p.StartCombat();refresh();Need(!shown("TowerInformation"),"Combat entry folds purchase once");
            Need(p.SelectedTower==id,"Combat fold preserves selected ID");
            p.HandleWorldClick(new Vector2Int(14,15));Need(shown("TowerInformation"),"Combat still permits current stats");
            Need(!root.Find("TowerInformation/TowerBuyTab").GetComponent<Button>().interactable,"No tower purchase during combat");
            click("PlayerGrowth");refresh();Need(shown("PlayerGrowthPanel"),"Combat permits player purchases");refresh();Need(shown("PlayerGrowthPanel"),"Periodic refresh cannot auto close reopened purchase");
            p.HandleWorldClick(new Vector2Int(0,12));Need(shown("PlayerGrowthPanel"),"Path is not empty floor");
            p.HandleWorldClick(floor);Need(!shown("PlayerGrowthPanel")&&p.SelectedTower==id,"Floor folds player details only");
            // Advance controlled functional sequence to the existing boss/augmentation, with forced clears.
            p.Diagnostic();typeof(Session).GetProperty("Diagnostic").SetValue(s,false);
            for(int guard=0;guard<5000&&s.Phase!=SessionPhase.Augmentation;guard++)
            {
                if(s.Phase==SessionPhase.Preparation)s.StartCombat();
                s.Tick(.2f,new Vector2(15,10));
                foreach(var e in s.Enemies.Where(e=>e.Alive).ToArray())s.ApplyDamage(e,100000,"Player");
            }
            refresh();Need(s.Phase==SessionPhase.Augmentation,"Reached augmentation via controlled forced clears");
            string choices=string.Join("|",s.AugmentationChoices.Select(c=>c.Id));
            click("AugmentationCard0");
            var view=root.GetComponentsInChildren<CoreGuardianAugmentationView>().First(v=>v.name=="AugmentationCard0");
            Need(view.Busy,"First card click starts reveal");
            click("AugmentFold");Need(root.Find("AugmentationSelection/AugmentTimerPlate/ChoiceTimer").gameObject.activeInHierarchy,"Fold preserves visible timer");
            var cards=root.Find("AugmentationSelection/Cards");Need(cards.gameObject.activeSelf&&cards.GetComponent<CanvasGroup>().alpha==0,"Cards hidden without cancelling reveal lifecycle");
            Need(!h.TryFoldDetails(),"Floor cannot dismiss augmentation");
            float remaining=s.AugmentationRemaining;s.Tick(1,new Vector2(15,10));refresh();
            Need(s.AugmentationRemaining<remaining,"Fold never pauses countdown");
            await Task.Delay(1100);Need(view.Revealed&&!view.Busy,"Reveal completes while card graphics are folded");
            click("AugmentFold");Need(string.Join("|",s.AugmentationChoices.Select(c=>c.Id))==choices,"Same candidates on reopen");
            Need(cards.GetComponent<CanvasGroup>().alpha==1,"Cards restored");
            s.Tick(31,new Vector2(15,10));refresh();Need(s.Phase==SessionPhase.Preparation,"Fold/reopen preserves automatic selection");
            foreach(var name in new[]{"TopFold","InspectorFold","InspectorReopen","DamageFold"})Need(root.GetComponentsInChildren<Button>(true).First(b=>b.name==name).interactable,"Handle must recover after augmentation: "+name);
            return new{passed=true,checks="real purchase,exclusive dock,placement fail/success,no auto-open,first fold/second deselect,same-tower reopen,combat entry/reopen,augmentation fold/time/automatic selection",kind="Controlled functional QA with diagnostic resources and forced clears; not human play/balance"};
        }
        finally{p.enabled=enabled;}
    }
}
