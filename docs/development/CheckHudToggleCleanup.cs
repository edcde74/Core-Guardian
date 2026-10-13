using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using CoreGuardian;
using Newtonsoft.Json;
public static class CheckHudToggleCleanup
{
    static CoreGuardianPrototype Game()=>UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
    static Button Button(string name)=>Game().GetComponentsInChildren<Button>(true).First(b=>b.name==name&&b.gameObject.activeInHierarchy);
    static GameObject Panel(string name)=>Game().GetComponentsInChildren<RectTransform>(true).First(r=>r.name==name).gameObject;
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Click(string name){var b=Button(name);Check(b.interactable,name+" disabled");b.onClick.Invoke();}
    public static string Verify()
    {
        Time.timeScale=0;
        var game=Game();
        Check(game.Session.Phase==SessionPhase.Preparation,"Needs fresh preparation");
        Check(!game.GetComponentsInChildren<Button>(true).Any(b=>b.name=="InstallFold"||b.name=="PlayerFold"||b.name=="접기"),"Duplicate controls remain");
        var install=Panel("TowerInstallPanel");var player=Panel("PlayerGrowthPanel");
        Check(!install.activeSelf&&!player.activeSelf,"Panels should start closed");
        Click("Installation");Check(install.activeSelf,"Install did not open");
        Click("공격 포탑");Click("Install_Single");
        Check(game.Installing==TowerKind.Single,"Tower not selected");
        Click("Installation");Check(!install.activeSelf&&game.Installing==TowerKind.Single,"Closing cancelled selected installation");
        Check(!Button("Installation").GetComponent<CoreGuardianHudReaction>().Selected(),"Closed install button remains selected");
        Click("Installation");Check(install.activeSelf&&game.Installing==TowerKind.Single,"Reopening lost selection");
        Check(Button("공격 포탑")!=null&&Button("지원 포탑")!=null,"Reopening did not reset categories");
        Click("공격 포탑");Click("분류로 돌아가기");Check(Button("지원 포탑")!=null,"Category return broken");
        Click("PlayerGrowth");Check(player.activeSelf,"Growth did not open");
        Click("PlayerGrowth");Check(!player.activeSelf,"Growth did not close");
        Check(!Button("PlayerGrowth").GetComponent<CoreGuardianHudReaction>().Selected(),"Closed growth button remains selected");
        Click("PlayerGrowth");Check(player.activeSelf,"Growth did not reopen");
        Click("CancelPlacement");Check(!game.Installing.HasValue&&!install.activeSelf,"Cancel no longer cancels placement");
        Click("Installation");Click("공격 포탑");
        var result=new {success=true,duplicateButtons=0,installToggle=true,growthToggle=true,selectionPreserved=true,categoryReturn=true,cancelSeparate=true,screenshotState="Preparation with attack installation and player growth panels open",screen=new[]{Screen.width,Screen.height}};
        Directory.CreateDirectory("docs/development/hud_toggle_cleanup_20261013");
        var json=JsonConvert.SerializeObject(result,Formatting.Indented).Replace("\r\n","\n");File.WriteAllText("docs/development/hud_toggle_cleanup_20261013/verification.json",json+"\n");return json;
    }
    public static string Finish(){Time.timeScale=1;return "Time scale restored";}
    public static string Import(){UnityEditor.AssetDatabase.Refresh();return "Import requested";}
    public static string PreserveCapture()
    {
        var source="Assets/docs/development/hud_toggle_cleanup_20261013/Hud.png";
        File.Copy(source,"docs/development/hud_toggle_cleanup_20261013/Hud.png",true);
        UnityEditor.AssetDatabase.DeleteAsset(source);
        UnityEditor.AssetDatabase.DeleteAsset("Assets/docs/development/hud_toggle_cleanup_20261013");
        UnityEditor.AssetDatabase.Refresh();return "Screenshot preserved outside Assets";
    }
}
