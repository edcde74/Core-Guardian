using System;
using System.Linq;
using CoreGuardian;
using UnityEngine;
using Newtonsoft.Json;

public static class VerifyBossAugmentation
{
    static CoreGuardianPrototype Game()=>UnityEngine.Object.FindObjectsByType<CoreGuardianPrototype>().Single();
    static readonly Vector2 Away=new Vector2(100,100);
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    public static object Restart(){Game().Restart();return "Restarted flow diagnostic";}
    public static object ClearCaptures(){UnityEditor.AssetDatabase.DeleteAsset("Assets/docs/development/boss_enraged_screen_20261013.png");UnityEditor.AssetDatabase.DeleteAsset("Assets/docs/development/augmentation_selection_screen_20261013.png");return "Removed own transient Assets captures after copying into docs";}
    static void Finish(Session s){for(int i=0;i<1000&&s.Phase==SessionPhase.Combat;i++){s.Tick(.05f,Away);foreach(var e in s.Enemies.ToArray())s.ApplyDamage(e,10000,"FlowDiagnostic");}Require(s.Phase!=SessionPhase.Combat,"Round failed to finish");}
    public static object Boss()
    {
        Require(Application.isPlaying,"Play required");Time.timeScale=0;var game=Game();var s=game.Session;
        Require(s.Wave==1&&s.Phase==SessionPhase.Preparation,"Fresh session required");
        for(int wave=1;wave<=9;wave++){Require(s.StartCombat(),"Cannot start");Finish(s);}
        Require(s.Wave==10,"Not boss round");s.StartCombat();s.Tick(.05f,Away);
        var boss=s.Enemies.Single(e=>e.Kind==EnemyKind.Boss);s.ApplyDamage(boss,150,"FlowDiagnostic");
        game.Notify("흐름 진단: 강제 처치로 보스 구간 이동 · 난이도 실측 아님");
        return JsonConvert.SerializeObject(new{success=true,wave=s.Wave,boss.Health,boss.Enraged,boss.MovementSpeed});
    }
    public static object Choices()
    {
        var game=Game();var s=game.Session;Finish(s);
        Require(s.Phase==SessionPhase.Augmentation,"Selection not opened");
        game.Notify("흐름 진단: 보스 격파 후 증강 선택");
        return JsonConvert.SerializeObject(new{success=true,phase=s.Phase.ToString(),s.PreparationRemaining,s.AugmentationRemaining,cards=s.AugmentationChoices.Select(c=>new{c.Id,c.Description})});
    }
    public static object CheckAndChoose()
    {
        var game=Game();var s=game.Session;
        var modal=GameObject.Find("AugmentationSelection");Require(modal!=null,"Modal missing");
        var buttons=modal.GetComponentsInChildren<UnityEngine.UI.Button>();Require(buttons.Length==3,"Wrong card count");
        Require(!game.player.enabled,"Movement not blocked");
        foreach(var b in buttons){Require(b.interactable,"Card not selectable");var text=b.GetComponentInChildren<UnityEngine.UI.Text>();Require(text.text.Contains("실버")||text.text.Contains("골드")||text.text.Contains("플래티넘"),"Grade missing");Require(text.preferredHeight<=text.rectTransform.rect.height,"Card text overflow");}
        buttons[1].onClick.Invoke();Require(s.Phase==SessionPhase.Preparation,"UI selection failed");
        Require(s.PreparationRemaining==50,"Preparation time consumed");Require(!modal.activeSelf,"Modal not dismissed");
        return JsonConvert.SerializeObject(new{success=true,selected=s.Augments.Selected,preparation=s.PreparationRemaining});
    }
    public static object FinishEleven()
    {
        var game=Game();var s=game.Session;Require(game.player.enabled,"Movement not restored");Require(s.StartCombat(),"11 cannot start");Finish(s);
        Require(s.Phase==SessionPhase.Victory&&s.CompletedWave==11,"11 not completed");game.Notify(s.LastMessage);Time.timeScale=1;
        return JsonConvert.SerializeObject(new{success=true,s.CompletedWave,s.CoreEnergy,s.Gold,s.Crystals});
    }
}
