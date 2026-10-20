using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using CoreGuardian;
public static class CGAtlasFrameProbe
{
    static string root="docs/development/mask_atlas_20261021/frame";
    static readonly BindingFlags flags=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    static Type utility=typeof(UnityEditorInternal.ProfilerDriver).Assembly.GetType("UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerUtility");
    static CoreGuardianPrototype game;
    static bool oldGame,oldBackground,oldPause;
    static EditorWindow window; static bool createdWindow;
    static double enabledAt;
    static int frame,warm;
    static EditorApplication.CallbackFunction poll;
    public static object Start()
    {
        if(!EditorApplication.isPlaying||UnityEngine.FrameDebugger.enabled||UnityEngine.Profiling.Profiler.enabled)throw new Exception("Own idle Play required");
        game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();oldGame=game.enabled;oldBackground=Application.runInBackground;oldPause=EditorApplication.isPaused;
        window=Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(w=>w.GetType().Name=="FrameDebuggerWindow");createdWindow=window==null;if(createdWindow)window=EditorWindow.GetWindow(typeof(UnityEditorInternal.ProfilerDriver).Assembly.GetTypes().First(t=>t.Name=="FrameDebuggerWindow"));
        Application.runInBackground=true;game.enabled=true;game.Diagnostic();var s=game.Session;
        for(int i=0;i<128;i++){var kind=i%8==0?EnemyKind.Buff:EnemyKind.Attack;var b=s.Data.Enemies[kind];s.DebugSpawn(kind,20+(i%12)*.3f,new EnemySpec{Kind=kind,Health=100000,Speed=b.Speed,Defense=b.Defense,Gold=0,CoreDamage=0,BuffRadius=b.BuffRadius,BuffSpeed=b.BuffSpeed});}
        var point=s.Enemies[0].Position;var size=game.BoardSize;
        var cells=Enumerable.Range(0,size.x).SelectMany(x=>Enumerable.Range(0,size.y).Select(y=>new Vector2Int(x,y))).Where(s.IsBuildable).OrderBy(p=>((Vector2)p-point).sqrMagnitude).ToArray();
        for(int i=0;i<32;i++)game.Place(i%5==4?TowerKind.Support:TowerKind.Multi,cells[i]);
        game.StartCombat();typeof(Session).GetField("waveTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(s,.001f);game.GetType().GetMethod("SynchronizeViews",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);
        frame=Time.frameCount;warm=60;poll=Poll;EditorApplication.update+=poll;return new{started=true,enemies=s.Enemies.Count,towers=s.Towers.Count};
    }
    static void Poll()
    {
        try{
            if(!EditorApplication.isPlaying)throw new Exception("Play ended");
            if(warm>0&&frame==Time.frameCount)return;frame=Time.frameCount;
            if(--warm>0)return;
            if(warm==0){game.enabled=false;window.GetType().GetMethod("EnableFrameDebugger",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,null);enabledAt=EditorApplication.timeSinceStartup;window.Repaint();return;}
            window.Repaint();
            if(EditorApplication.timeSinceStartup-enabledAt<3)return;
            var events=(Array)utility.GetMethod("GetFrameEvents",flags).Invoke(null,null);var result=new List<object>();
            if(events.Length==0&&EditorApplication.timeSinceStartup-enabledAt<10)return;
            for(int i=0;i<events.Length;i++){
                var e=events.GetValue(i);var t=e.GetType();var o=(UnityEngine.Object)t.GetField("m_Obj").GetValue(e);
                string info=(string)utility.GetMethod("GetFrameEventInfoName",flags).Invoke(null,new object[]{i});
                var dt=utility.Assembly.GetType("UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerEventData");var data=Activator.CreateInstance(dt,true);bool dataValid=(bool)utility.GetMethod("GetFrameEventData",flags).Invoke(null,new object[]{i,data});result.Add(new{index=i,type=t.GetField("m_Type").GetValue(e).ToString(),info,objectName=o==null?null:o.name,objectType=o==null?null:o.GetType().Name,dataValid,shader=dt.GetField("m_RealShaderName").GetValue(data),renderTarget=dt.GetField("m_RenderTargetName").GetValue(data),batchBreak=dt.GetField("m_BatchBreakCause").GetValue(data)});
            }
            Write("frame_debugger.json",new{kind="frozen simulation single-frame structural capture, not timing",frame,enemies=game.Session.Enemies.Count,towers=game.Session.Towers.Count,events=result});
            window.GetType().GetMethod("DisableFrameDebugger",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,null);
            var outlines=Resources.FindObjectsOfTypeAll<CoreGuardianSelectionOutline>().Where(x=>x.gameObject.scene.IsValid()).ToArray();
            var masks=Resources.FindObjectsOfTypeAll<RenderTexture>().Where(x=>x.name=="Selected unit union mask").ToArray();
            Write("resources.json",new{kind="runtime resource inventory, not Memory Profiler snapshot",frame,outlines=outlines.Length,visibleOutlines=outlines.Count(x=>x.IsVisible),emphasisHosts=outlines.Count(x=>x.name.StartsWith("ObjectEmphasis_B2_")),contactHosts=outlines.Count(x=>x.name=="PhotoDynamicContact"),maskCount=masks.Length,maskRuntimeBytes=masks.Sum(x=>UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(x)),maskSizes=masks.GroupBy(x=>x.width+"x"+x.height).Select(g=>new{size=g.Key,count=g.Count()}).ToArray(),materials=Resources.FindObjectsOfTypeAll<Material>().GroupBy(x=>x.shader==null?"<null>":x.shader.name).Select(g=>new{shader=g.Key,count=g.Count()}).OrderByDescending(x=>x.count).Take(20).ToArray()});
            Finish(null);
        }catch(Exception e){Finish(e.ToString());}
    }
    static void Finish(string error){EditorApplication.update-=poll;if(window!=null)window.GetType().GetMethod("DisableFrameDebugger",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,null);EditorApplication.isPaused=oldPause;if(game!=null)game.enabled=oldGame;Application.runInBackground=oldBackground;if(createdWindow&&window!=null)window.Close();Write("frame_debugger_completion.json",new{error,restored=true,enabled=UnityEngine.FrameDebugger.enabled});}
    static void Write(string file,object data){File.WriteAllText(Path.Combine(root,file),JsonConvert.SerializeObject(data,Formatting.Indented));}
}
