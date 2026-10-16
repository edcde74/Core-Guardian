using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public static class RecordW
{
    public static object Main()
    {
        if(!EditorApplication.isPlaying||EditorApplication.isPaused)throw new InvalidOperationException("Own unpaused Play required");
        var screenshot=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Pipeline.Editor.Commands.ScreenshotCommand")).First(t=>t!=null);
        var render=screenshot.GetMethod("RenderToPng",BindingFlags.Static|BindingFlags.NonPublic);
        if(render==null)throw new InvalidOperationException("Pipeline screenshot helper unavailable");
        var game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();var player=game.player;var start=player.transform.position;var camera=Camera.main;
        var originalKeyboard=Keyboard.current;var keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
        float oldCapture=Time.captureDeltaTime;Time.captureDeltaTime=1/60f;
        const string folder="docs/development/player_up_repair_20261016/actual_w_frames/";Directory.CreateDirectory(folder);
        int count=0,lastFrame=-1;var samples=new List<object>();EditorApplication.CallbackFunction tick=null;
        void Clean(){EditorApplication.update-=tick;Time.captureDeltaTime=oldCapture;InputSystem.RemoveDevice(keyboard);if(originalKeyboard!=null&&originalKeyboard.added)originalKeyboard.MakeCurrent();}
        tick=()=>
        {
            if(!EditorApplication.isPlaying){Clean();return;}
            if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
            try
            {
                var bytes=(byte[])render.Invoke(null,new object[]{camera,1600,900});File.WriteAllBytes(folder+count.ToString("000")+".png",bytes);
                var unit=player.GetComponent<CoreGuardianUnitAnimation>();
                var parts=player.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&new[]{"Body","Arm","LeftLeg","RightLeg"}.Contains(r.name)).Select(r=>new{r.name,sprite=r.sprite.name}).ToArray();
                samples.Add(new {frame=count,x=player.transform.position.x,y=player.transform.position.y,direction=unit.Direction,parts});count++;
                if(count>=36)
                {
                    Clean();EditorApplication.isPaused=true;
                    File.WriteAllText("docs/development/player_up_repair_20261016/actual_w_sequence.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{input="W KeyboardState",stepSeconds=.12f,captureDelta=1/60f,start=new[]{start.x,start.y},end=new[]{player.transform.position.x,player.transform.position.y},samples},Newtonsoft.Json.Formatting.Indented));
                }
            }
            catch(Exception e){Clean();Debug.LogException(e);File.WriteAllText("docs/development/player_up_repair_20261016/actual_w_record_error.txt",e.ToString());}
        };
        EditorApplication.update+=tick;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));return new{recording=true,frames=36};
    }
}
