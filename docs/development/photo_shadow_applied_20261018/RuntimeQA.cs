using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreGuardian;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class PhotoShadowRuntimeQA
{
    static void Need(bool ok,string why){if(!ok)throw new Exception(why);}
    public static async Task<object> Main()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
        Need(Application.isPlaying&&game!=null&&game.Ready,"Owned running combat scene required");
        const string dir="docs/development/photo_shadow_applied_20261018/";
        var oldKeyboard=Keyboard.current;var behavior=InputSystem.settings.backgroundBehavior;
        float time=Time.timeScale,capture=Time.captureDeltaTime;var keyboard=InputSystem.AddDevice<Keyboard>();
        var checks=new List<object>();
        try
        {
            keyboard.MakeCurrent();InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            var root=game.player.transform;var shadow=root.GetComponent<CoreGuardianPhotoShadow>();Need(shadow!=null,"Live player shadow missing");
            var cast=root.GetComponentsInChildren<SpriteRenderer>().Single(r=>r.name=="PhotoCastShadow");
            var offset=cast.transform.position-root.position;var start=root.position;
            foreach(var key in new[]{Key.D,Key.A,Key.W,Key.S})
            {
                var before=root.position;InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));
                await Task.Delay(75);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Task.Delay(220);
                Need((root.position-before).sqrMagnitude>.1f,"Normal key did not move: "+key);
                Need(Vector3.Distance(cast.transform.position-root.position,offset)<.0001f,"Shadow drifted from ground root");
                Need(root.GetComponent<CoreGuardianUnitAnimation>().Direction==(key==Key.D?0:key==Key.A?4:key==Key.W?2:6),"Wrong live facing "+key);
                checks.Add(new{key=key.ToString(),from=before.ToString(),to=root.position.ToString(),direction=root.GetComponent<CoreGuardianUnitAnimation>().Direction});
                ScreenCapture.CaptureScreenshot(dir+"key_"+key+".png");await Task.Delay(120);
            }
            Need(Vector3.Distance(start,root.position)<.001f,"Four normal key movements did not return to start");
            // Actual install API; diagnostic currency is clearly separated from normal purchases.
            game.Diagnostic();
            Need(game.Place(TowerKind.Single,new Vector2Int(14,15)),"Single install failed");
            Need(game.Place(TowerKind.Multi,new Vector2Int(16,15)),"Multi install failed");
            Need(game.Place(TowerKind.Support,new Vector2Int(18,15)),"Support install failed");
            await Task.Delay(150);
            foreach(var tower in game.Session.Towers)
                Need(game.TowerView(tower.Id).GetComponent<CoreGuardianPhotoShadow>()!=null,"New installed tower has no approved shadow");
            game.StartCombat();await Task.Delay(400);
            Need(game.Session.Enemies.Any(),"Natural enemy spawn missing");
            // Supplementary diagnostic spawns check every kind's actual creation path.
            foreach(var kind in new[]{EnemyKind.Defense,EnemyKind.Buff,EnemyKind.Boss})game.Session.DebugSpawn(kind);
            await Task.Delay(200);
            var targets=new List<Transform>();game.GetCombatBodyTargets(targets);
            Need(targets.All(t=>t.GetComponent<CoreGuardianPhotoShadow>()!=null),"New real combat view missing shadow");
            var core=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).First(r=>r.name=="2D Sprite"&&r.sprite!=null&&r.sprite.name=="core");
            Need(core.transform.parent.GetComponent<CoreGuardianPhotoShadow>()!=null,"Core was not connected at scene start");
            Need(core.transform.parent.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="바닥 수정빛").All(r=>!r.enabled),"Replaced core floor glow stacked");
            ScreenCapture.CaptureScreenshot(dir+"live_hud_combat.png");await Task.Delay(180);
            var result=new{passed=true,normalInput=checks,liveTargets=targets.Count,installedTowers=game.Session.Towers.Count,coreConnected=true,note="Own Play; real keyboard input via temporary test device; normal Update; diagnostic currency and additional species spawns for presentation QA; actual HUD screenshot; no balance/human-play/performance conclusion"};
            File.WriteAllText(dir+"runtime_qa.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented).Replace("\r\n","\n"));
            return result;
        }
        finally
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.RemoveDevice(keyboard);
            if(oldKeyboard!=null)oldKeyboard.MakeCurrent();InputSystem.settings.backgroundBehavior=behavior;
            Time.timeScale=time;Time.captureDeltaTime=capture;
        }
    }
}
