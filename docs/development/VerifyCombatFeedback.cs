using System;
using System.Linq;
using CoreGuardian;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;

// Run only against the current combat scene in Play Mode. No scene or prefab writes.
public static class VerifyCombatFeedback
{
    static CoreGuardianPrototype Game()=>UnityEngine.Object.FindObjectsByType<CoreGuardianPrototype>().Single();
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    public static object Begin()
    {
        Require(Application.isPlaying,"Requires Play Mode");Time.timeScale=0;
        var game=Game();game.Diagnostic();game.ChooseTower(TowerKind.Single);
        var screen=Camera.main.WorldToScreenPoint(game.World(new Vector2(14,15)));
        InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=(Vector2)screen+new Vector2(5,5)});
        return "Queued placement pointer";
    }
    public static object CheckTileAndMove()
    {
        var game=Game();var preview=GameObject.Find("TowerPlacementPreview");
        Require(preview!=null,"Placement preview missing");
        Require(Vector3.Distance(preview.transform.position,game.World(new Vector2(14,15)))<.001f,"Cursor not on tile center");
        Require(!Cursor.visible,"Free cursor must be hidden over board");
        var screen=Camera.main.WorldToScreenPoint(game.World(new Vector2(15,15)));
        InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=(Vector2)screen+new Vector2(-5,5)});
        return "First cell centered; queued adjacent cell";
    }
    public static object CheckAdjacentAndMoveOverUI()
    {
        var game=Game();var preview=GameObject.Find("TowerPlacementPreview");
        Require(Vector3.Distance(preview.transform.position,game.World(new Vector2(15,15)))<.001f,"Adjacent cell cursor not centered");
        var button=game.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Installation");
        var rect=button.GetComponent<RectTransform>();
        InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))});
        return "Adjacent cell centered; queued pointer over UI";
    }
    public static object CheckUIAndEscape()
    {
        Require(Cursor.visible,"Normal cursor was not restored over UI");
        Require(GameObject.Find("TowerPlacementPreview")==null,"Preview must be hidden over UI");
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.Escape));
        return "Normal UI cursor restored; queued Escape";
    }
    public static object CheckEscapeAndShowDamage()
    {
        var game=Game();
        Require(!game.Installing.HasValue,"Esc did not cancel placement");
        Require(GameObject.Find("TowerPlacementPreview")==null,"Esc left preview alive");
        Require(!UnityEngine.Object.FindObjectsByType<Transform>().Any(t=>t.name=="InstallationTile"),"Esc left tile highlights");
        Require(Cursor.visible,"Esc did not restore normal cursor");
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
        var e=game.Session.DebugSpawn(EnemyKind.Attack,24);
        game.Session.ApplyDamage(e,12.5f,"Player");game.Session.ApplyDamage(e,100,"Tower:1");
        var d=game.Session.DebugSpawn(EnemyKind.Defense,26);
        game.Session.ApplyDamage(d,12.5f,"Tower:2");
        return "Esc cleared mode, preview, highlights and cursor; applied three hits";
    }
    public static object CheckDamage()
    {
        var texts=UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>().Where(t=>t.name=="HitDamage").ToArray();
        Require(texts.Length==3,"Each hit must create a number, including lethal damage");
        Require(texts.Select(t=>t.text).OrderBy(x=>x).SequenceEqual(new[]{"12","12","13"}),"Incorrect damage numbers");
        Require(texts.All(t=>!t.raycastTarget),"Damage numbers must not block clicks");
        Require(UnityEngine.Object.FindObjectsByType<Canvas>().Length==1,"Existing HUD canvas must be reused");
        return JsonConvert.SerializeObject(new{success=true,damage=texts.Select(t=>t.text).ToArray(),raycastsBlocked=false,escCancelled=true,tileCursorCentered=true,timeScale=Time.timeScale});
    }
    public static object Resume()=>Time.timeScale=1;
    public static object CheckExpired()
    {
        Require(!UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>().Any(t=>t.name=="HitDamage"),"Damage numbers did not expire");
        return "Damage numbers expired";
    }
    public static object OpenMenu()
    {
        Game().GetComponentsInChildren<Button>(true).Single(b=>b.name=="Installation").onClick.Invoke();
        Require(GameObject.Find("TowerInstallPanel")!=null,"Install menu did not open");
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.Escape));return "Menu opened; queued Escape";
    }
    public static object CheckMenuClosed()
    {
        Require(GameObject.Find("TowerInstallPanel")==null,"Esc did not close selection menu");
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());return "Selection menu closed by Escape";
    }
    public static object CombatCapture()
    {
        var game=Game();game.Diagnostic();
        Require(game.Place(TowerKind.Single,new Vector2Int(14,15)),"Single placement failed");
        Require(game.Place(TowerKind.Multi,new Vector2Int(17,15)),"Multi placement failed");
        Require(game.Place(TowerKind.Support,new Vector2Int(14,16)),"Support placement failed");
        game.StartCombat();Time.timeScale=0;
        int hits=0;Action<EnemyState,int> handler=(enemy,damage)=>hits++;
        game.Session.DamageApplied+=handler;
        try{for(int i=0;i<200&&hits==0;i++)game.Session.Tick(.05f,new Vector2(15,10));}
        finally{game.Session.DamageApplied-=handler;}
        Require(hits>0,"Actual projectile hit did not show damage");
        return JsonConvert.SerializeObject(new{success=true,actualProjectileHits=hits,damageTotals=game.Session.DamageTotals});
    }
}
