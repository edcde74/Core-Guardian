using System;
using System.IO;
using System.Linq;
using CoreGuardian;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

// Exercises real UI transitions in the existing combat scene, with one frame between calls.
public static class VerifyInstallMenu
{
    static CoreGuardianPrototype Game()=>UnityEngine.Object.FindObjectsByType<CoreGuardianPrototype>().Single();
    static Button Control(string name)=>Game().GetComponentsInChildren<Button>().Single(b=>b.name==name);
    static string[] Labels()=>Game().GetComponentsInChildren<Button>().Where(b=>b.transform.parent.name=="TowerInstallPanel").Select(b=>b.name).OrderBy(n=>n).ToArray();
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    public static object OpenAttack()
    {
        Require(Application.isPlaying,"Play mode required");Time.timeScale=0;Game().Diagnostic();
        Control("Installation").onClick.Invoke();Control("공격 포탑").onClick.Invoke();return "Opened attack submenu";
    }
    public static object CloseAndReopen()
    {
        Require(Labels().Any(n=>n.StartsWith("단일 고속")),"Attack submenu did not open");
        Control("Installation").onClick.Invoke();Require(GameObject.Find("TowerInstallPanel")==null,"Panel did not close");
        Control("Installation").onClick.Invoke();return "Reopened using Installation button";
    }
    public static object Observe()=>new{labels=Labels(),startsAtCategories=Labels().Contains("공격 포탑")&&Labels().Contains("지원 포탑")};
    public static object ChooseAndFold()
    {
        Require(Labels().Contains("공격 포탑"),"Reopen kept old submenu");
        Control("공격 포탑").onClick.Invoke();return "Opened attack list for placement";
    }
    public static object PlaceChoiceAndFold()
    {
        Control("단일 고속\n100수정").onClick.Invoke();Require(Game().Installing==TowerKind.Single,"Tower choice failed");
        Control("InstallFold").onClick.Invoke();Require(GameObject.Find("TowerInstallPanel")==null,"Fold did not close");
        Require(Game().Installing==TowerKind.Single,"Fold should keep placement choice");
        Control("InstallFold").onClick.Invoke();return "Reopened using fold tab after choosing a tower";
    }
    public static object CheckFoldAndSupport()
    {
        Require(Labels().Contains("공격 포탑")&&Labels().Contains("지원 포탑"),"Fold reopen kept old submenu");
        Require(Game().Installing==TowerKind.Single,"Reopening changed placement choice");
        Control("지원 포탑").onClick.Invoke();return "Opened support submenu";
    }
    public static object CancelAndReopen()
    {
        Require(Labels().Any(n=>n.StartsWith("지원 포탑\n")),"Support submenu not shown");
        Require(Game().GetComponent<CoreGuardianHud>().CancelInstallation(),"Cancel failed");
        Require(!Game().Installing.HasValue,"Cancel left placement selected");
        Control("Installation").onClick.Invoke();return "Cancelled support submenu and reopened";
    }
    public static object Finish()
    {
        Require(Labels().Contains("공격 포탑")&&Labels().Contains("지원 포탑"),"Cancel reopen kept old submenu");
        Require(Labels().Length==3,"Duplicate old menu buttons remained");
        Control("접기").onClick.Invoke();Control("Installation").onClick.Invoke();return "Fold-button close reopened at categories";
    }
    public static object VerifyAndRestore()
    {
        Require(Labels().Contains("공격 포탑")&&Labels().Contains("지원 포탑")&&Labels().Length==3,"Category reopen failed");
        var result=new{success=true,mainButtonReopen=true,foldTabReopen=true,cancelReopen=true,categoryFoldReopen=true,foldKeepsPlacement=true,duplicateButtons=false};
        File.WriteAllText("docs/development/install_menu_verification_20261010.json",JsonConvert.SerializeObject(result,Formatting.Indented));
        Game().GetComponent<CoreGuardianHud>().CancelInstallation();Time.timeScale=1;return result;
    }
}
