using System;
using System.IO;
using System.Linq;
using CoreGuardian;
using UnityEngine;
using TMPro;
using Newtonsoft.Json;

public static class VerifyCombatArt
{
    static CoreGuardianPrototype Game()=>UnityEngine.Object.FindObjectsByType<CoreGuardianPrototype>().Single();
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static TextMeshProUGUI[] Numbers(string name)=>UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>().Where(t=>t.name==name).ToArray();
    public static object Begin()
    {
        Require(Application.isPlaying,"Play required");Time.timeScale=0;
        var game=Game();game.Diagnostic();
        Require(game.GetComponent<CoreGuardianCombatArt>().Loaded,"Combat art not loaded");
        Require(game.Place(TowerKind.Single,new Vector2Int(14,15)),"Single placement failed");
        Require(game.Place(TowerKind.Multi,new Vector2Int(13,16)),"Multi placement failed");
        Require(game.Place(TowerKind.Support,new Vector2Int(14,16)),"Support placement failed");
        game.StartCombat();game.Session.DebugSpawn(EnemyKind.Attack,76);
        game.Session.Tick(.05f,new Vector2(15,10));
        return "Prepared actual three-source projectiles and both buff types";
    }
    public static object CheckProjectilesAndHit()
    {
        var sprites=UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Where(r=>r.name.StartsWith("Projectile_")).ToArray();
        Require(new[]{"EngineerChip","SingleBolt","MultiBolt"}.All(n=>sprites.Any(s=>s.sprite.name==n)),"Missing actual projectile art source");
        Require(GameObject.Find("EnemySpeedBuff")!=null,"Speed marker missing");
        Require(GameObject.Find("TowerDamageBuff")!=null,"Tower buff marker missing");
        Require(!UnityEngine.Object.FindObjectsByType<LineRenderer>().Any(l=>l.name=="EnemySpeedBuff"||l.name=="TowerDamageBuff"),"Old buff rings still active");
        var game=Game();var enemy=game.Session.Enemies.First(e=>e.Kind==EnemyKind.Defense);
        game.Session.ApplyDamage(enemy,12.5f,"Player");
        var killed=game.Session.Enemies.First(e=>e.Kind==EnemyKind.Attack&&e.PathTravelled<30);
        game.Session.ApplyDamage(killed,100,"Tower:1");
        return "Actual three projectile sprites + buff targets verified; applied hit and kill";
    }
    public static object CheckNumbersAndCore()
    {
        var damage=Numbers("HitDamage");var gold=Numbers("GoldNumber");
        Require(damage.Length==2&&damage.Select(t=>t.text).OrderBy(s=>s).SequenceEqual(new[]{"12","25"}),"Wrong HP-loss numbers");
        Require(gold.Length==1&&gold[0].text=="+3","Kill gold number missing");
        Require(damage.Concat(gold).All(t=>t.font.name=="PixelifySansBoldDamage"&&!t.raycastTarget),"TMP font or click blocking incorrect");
        Require(GameObject.Find("EnemyHit")!=null&&GameObject.Find("EnemyDeath")!=null&&GameObject.Find("GoldGain")!=null,"Missing hit/death/gold art");
        var game=Game();game.Session.DebugSpawn(EnemyKind.Attack,92);game.Session.Tick(.01f,new Vector2(15,10));
        return "Correct TMP damage and kill gold; advanced core arrival";
    }
    public static object CheckCoreAndReset()
    {
        Require(GameObject.Find("CoreHit")!=null,"Core-hit art missing");
        Require(Numbers("CoreDamageNumber").Single().text=="-2","Core loss number incorrect");
        Require(Numbers("GoldNumber").Length==1,"Core arrival incorrectly awarded gold feedback");
        Require(UnityEngine.Object.FindObjectsByType<Canvas>().Length==1,"Extra Canvas created");
        var report=new{success=true,projectileSources=new[]{"EngineerChip","SingleBolt","MultiBolt"},damage=new[]{12,25},killGold=3,coreDamage=2,coreArrivalGold=false,tmpFont="PixelifySansBoldDamage",clicksBlocked=false};
        File.WriteAllText("docs/development/combat_art_runtime_20261010.json",JsonConvert.SerializeObject(report,Formatting.Indented));
        Game().Diagnostic();return report;
    }
    public static object CheckReset()
    {
        Require(!UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>().Any(t=>new[]{"HitDamage","GoldNumber","CoreDamageNumber"}.Contains(t.name)),"Reset left numbers");
        Require(!UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Any(r=>new[]{"EnemyHit","EnemyDeath","GoldGain","CoreHit"}.Contains(r.name)),"Reset left transient effects");
        return "Reset cleared all effects and numbers";
    }
    public static object Capture()
    {
        var game=Game();game.Diagnostic();game.Place(TowerKind.Single,new Vector2Int(14,15));game.Place(TowerKind.Multi,new Vector2Int(13,16));game.Place(TowerKind.Support,new Vector2Int(14,16));game.StartCombat();
        game.Session.DebugSpawn(EnemyKind.Attack,76);game.Session.Tick(.05f,new Vector2(15,10));
        var enemy=game.Session.Enemies.First(e=>e.Kind==EnemyKind.Defense);game.Session.ApplyDamage(enemy,12.5f,"Player");
        var killed=game.Session.Enemies.First(e=>e.Kind==EnemyKind.Attack&&e.PathTravelled<30);game.Session.ApplyDamage(killed,100,"Tower:1");
        return "Actual scene: role diagnostic capture (not official Wave1)";
    }
    public static object Resume(){Time.timeScale=1;return "Time scale restored";}
}

