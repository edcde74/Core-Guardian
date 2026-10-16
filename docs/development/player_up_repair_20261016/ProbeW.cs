using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public static class ProbeW
{
    public static object Main(string label)
    {
        if(!EditorApplication.isPlaying||EditorApplication.isPaused)throw new InvalidOperationException("Own unpaused Play session required");
        var game=UnityEngine.Object.FindFirstObjectByType<CoreGuardianPrototype>();var player=game.player;var start=player.transform.position;
        var originalKeyboard=Keyboard.current;var keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
        var started=EditorApplication.timeSinceStartup;EditorApplication.CallbackFunction tick=null;
        void Clean(){EditorApplication.update-=tick;InputSystem.RemoveDevice(keyboard);if(originalKeyboard!=null&&originalKeyboard.added)originalKeyboard.MakeCurrent();}
        tick=()=>
        {
            if(!EditorApplication.isPlaying){Clean();return;}
            var delta=player.transform.position-start;
            if(delta.y>.01f)
            {
                EditorApplication.isPaused=true;Clean();
                var unit=player.GetComponent<CoreGuardianUnitAnimation>();
                var data=new {input="W through InputSystem.KeyboardState",start=new[]{start.x,start.y},position=new[]{player.transform.position.x,player.transform.position.y},direction=unit.Direction,
                    parts=player.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).Select(r=>new {r.name,sprite=r.sprite==null?null:r.sprite.name}).ToArray()};
                File.WriteAllText("docs/development/player_up_repair_20261016/"+label+"_w_state.json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));
            }
            else if(EditorApplication.timeSinceStartup-started>8){Clean();File.WriteAllText("docs/development/player_up_repair_20261016/"+label+"_w_failed.txt","W did not move up from the initial cell");}
        };
        EditorApplication.update+=tick;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));
        return new{started=true,label,initialCell=player.CurrentCell};
    }
}
