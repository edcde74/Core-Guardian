using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CoreGuardian;
public static class VerifyHudSprites
{
    static CoreGuardianPrototype Game()=>UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
    static Button Button(string name)=>GameObject.Find(name).GetComponent<Button>();
    public static string OpenPanels(){Time.timeScale=0;Button("Installation").onClick.Invoke();Button("PlayerGrowth").onClick.Invoke();return Main();}
    public static string Main(){var canvas=GameObject.Find("CombatHUD");var images=canvas.GetComponentsInChildren<Image>(true);var missing=images.Where(i=>i.gameObject.name.StartsWith("Symbol_")&&i.sprite==null).Select(i=>i.name).ToArray();if(missing.Length>0)throw new Exception("Missing sprites: "+string.Join(",",missing));return "Sprites present, symbols="+images.Count(i=>i.name.StartsWith("Symbol_"))+", buttons="+canvas.GetComponentsInChildren<Button>(true).Length+", phase="+Game().Session.Phase;}
    public static string Augment(){Time.timeScale=0;var g=Game();g.Restart();return "Restart queued; call PrepareAugment next frame";}
    public static string PrepareAugment(){Time.timeScale=0;var g=Game();var s=g.Session;
        for(int wave=1;wave<=10;wave++){g.StartCombat();for(int i=0;i<1000&&s.Phase==SessionPhase.Combat;i++){s.Tick(.1f,new Vector2(100,100));foreach(var enemy in s.Enemies.ToArray())s.ApplyDamage(enemy,10000,"HUD flow verification");}}
        g.Notify(s.LastMessage);return "Forced combat completion for UI flow only, phase="+s.Phase;}
    public static string Reveal(){Button("AugmentationCard0").onClick.Invoke();return "First click; phase="+Game().Session.Phase;}
    public static string ShowTiers(){var s=Game().Session;if(s.Phase!=SessionPhase.Augmentation)throw new Exception("Not selecting");typeof(Session).GetField("choices",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(s,new[]{new AugmentationCard(AugmentationGrade.Silver,AugmentationFamily.Gold),new AugmentationCard(AugmentationGrade.Gold,AugmentationFamily.Core),new AugmentationCard(AugmentationGrade.Platinum,AugmentationFamily.Player)});Game().Notify("등급별 외형 검증용 런타임 카드 · 실제 추첨 아님");return "UI-only fixed tiers; no saved data changes";}
    public static string Choose(){Button("AugmentationCard0").onClick.Invoke();if(Game().Session.Phase!=SessionPhase.Preparation)throw new Exception("Second click did not select");return "Selected with other cards still closed, preparation="+Game().Session.PreparationRemaining;}
}
