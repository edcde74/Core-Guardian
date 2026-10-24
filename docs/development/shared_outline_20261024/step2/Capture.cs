return CGB2.CGSharedOutlineStep2.Start();
}
}
namespace CGB2 {
using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CoreGuardian;
using Newtonsoft.Json;

// Step 2 of the shared outline plan: one bounded own Play session.
// Fixed load (same as baseline: 128 enemies, 15 towers), simulation frozen (timeScale 0) for captures.
// Captures: original x2 (reproducibility), shared x2, original again, outline-hidden for both, zoom 4.25 set,
// variety set (all enemy kinds), then toggle-on regression (pool reuse / removal / zoom / toggles / restart).
public static class CGSharedOutlineStep2
{
    const string Root="docs/development/shared_outline_20261024/step2/";
    static readonly List<(int wait,string name,Action act)> steps=new List<(int,string,Action)>();
    static readonly List<object> log=new List<object>();
    static int index,waitFrames,lastFrame;static bool running,setupDone;static double deadline;static string error;
    static CoreGuardianPrototype game;static CoreGuardianObjectEmphasis emphasis;static Camera cam;
    static float oldCapture,oldScale,oldOrtho;static bool oldBackground;static int oldWidth;
    static UnityEngine.Object gameView;static PropertyInfo sizeIndex;static int savedSizeIndex,settle;
    static Type sharedType;
    public static object Start()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Stopped idle clean Editor required");
        Directory.CreateDirectory(Root);
        sharedType=typeof(CoreGuardianObjectEmphasis).Assembly.GetType("CoreGuardianSharedOutline");
        var type=typeof(Editor).Assembly.GetType("UnityEditor.GameView");gameView=Resources.FindObjectsOfTypeAll(type).First();
        sizeIndex=type.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);savedSizeIndex=(int)sizeIndex.GetValue(gameView);sizeIndex.SetValue(gameView,3);
        oldBackground=Application.runInBackground;oldCapture=Time.captureDeltaTime;oldScale=Time.timeScale;Application.runInBackground=true;
        steps.Clear();log.Clear();index=0;waitFrames=0;setupDone=false;error=null;settle=0;
        running=true;deadline=EditorApplication.timeSinceStartup+120;
        EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=PlayState;Application.logMessageReceived+=Log;
        EditorApplication.isPlaying=true;return new{started=true,savedSizeIndex};
    }
    static void Log(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert){if(error==null)error=m+"\n"+s;}}
    static void SetShared(bool on)=>sharedType.GetMethod("SetEnabled",BindingFlags.Static|BindingFlags.Public).Invoke(null,new object[]{on});
    static bool Shared=>(bool)sharedType.GetProperty("Enabled",BindingFlags.Static|BindingFlags.Public).GetValue(null);
    static void Note(string what,object data=null)=>log.Add(new{frame=Time.frameCount,what,shared=Shared,data});

    static void Capture(string name)
    {
        int w=cam.pixelWidth,h=cam.pixelHeight;
        var rt=RenderTexture.GetTemporary(w,h,24,RenderTextureFormat.ARGB32);var old=cam.targetTexture;
        cam.targetTexture=rt;cam.Render();cam.targetTexture=old;
        var prev=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(w,h,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,w,h),0,0);tex.Apply();RenderTexture.active=prev;
        RenderTexture.ReleaseTemporary(rt);
        File.WriteAllBytes(Root+name+".rgba",tex.GetRawTextureData());File.WriteAllBytes(Root+name+".png",tex.EncodeToPNG());
        UnityEngine.Object.Destroy(tex);
        Note("capture "+name,new{w,h,ortho=cam.orthographicSize,width=emphasis.outlineWidthPixels,targets=emphasis.ActiveTargetCount});
    }
    static void DumpMask(string name)
    {
        var mask=Shader.GetGlobalTexture("_CGSharedOutlineMask") as RenderTexture;
        if(mask==null){Note("mask missing "+name);return;}
        var prev=RenderTexture.active;RenderTexture.active=mask;var tex=new Texture2D(mask.width,mask.height,TextureFormat.RFloat,false);tex.ReadPixels(new Rect(0,0,mask.width,mask.height),0,0);tex.Apply();RenderTexture.active=prev;
        File.WriteAllBytes(Root+name+".r32",tex.GetRawTextureData());UnityEngine.Object.Destroy(tex);
        var layout=Shader.GetGlobalVector("_CGSharedOutlineLayout");
        Note("mask "+name,new{mask.width,mask.height,layout=new[]{layout.x,layout.y,layout.z,layout.w}});
    }
    static int EnabledSharedOutputs()=>Resources.FindObjectsOfTypeAll<MeshRenderer>().Count(r=>r!=null&&r.name=="SharedOutline"&&r.enabled&&r.gameObject.activeInHierarchy);
    static int EnabledOriginalOutputs()=>Resources.FindObjectsOfTypeAll<MeshRenderer>().Count(r=>r!=null&&r.name=="SelectionOutline"&&r.enabled&&r.gameObject.activeInHierarchy&&r.transform.parent!=null&&r.transform.parent.name.StartsWith("ObjectEmphasis_B2_"));
    static void Counts(string what)=>Note(what,new{targets=emphasis.ActiveTargetCount,sharedOutputs=EnabledSharedOutputs(),originalOutputs=EnabledOriginalOutputs(),enemies=game.Session.Enemies.Count});

    static void Spawn(EnemyKind kind,float progress)
    {
        var s=game.Session;var spec=s.Data.Enemies[kind];
        s.DebugSpawn(kind,progress,new EnemySpec{Kind=kind,Health=100000,Speed=spec.Speed,Defense=spec.Defense,Gold=0,CoreDamage=0,BuffRadius=spec.BuffRadius,BuffSpeed=spec.BuffSpeed});
    }
    static void RemoveEnemies(int count)
    {
        var s=game.Session;var handles=new List<EnemyHandle>();for(int i=0;i<Math.Min(count,s.Enemies.Count);i++)handles.Add(s.Enemies[i]);
        foreach(var h in handles)s.DebugRemoveEnemy(h);
    }
    static void Setup()
    {
        CoreGuardianAttackBodyBatch.SetEnabled(false);game.Diagnostic();var s=game.Session;
        for(int i=0;i<128;i++)Spawn(i%8==0?EnemyKind.Buff:EnemyKind.Attack,20+(i%12)*.3f);
        var p=s.Enemies[0].Position;var size=game.BoardSize;
        var cells=Enumerable.Range(0,size.x).SelectMany(x=>Enumerable.Range(0,size.y).Select(y=>new Vector2Int(x,y))).Where(s.IsBuildable).OrderBy(c=>((Vector2)c-p).sqrMagnitude).ToArray();
        for(int i=0;i<Math.Min(32,cells.Length);i++)game.Place(i%5==4?TowerKind.Support:TowerKind.Multi,cells[i]);
        game.StartCombat();typeof(Session).GetField("waveTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(s,.0001f);
        if(s.Enemies.Count!=128||s.Towers.Count!=15)throw new Exception("Unexpected load counts "+s.Enemies.Count+"/"+s.Towers.Count);
        oldOrtho=cam.orthographicSize;oldWidth=emphasis.outlineWidthPixels;
        Time.timeScale=1;Time.captureDeltaTime=1/60f;
    }
    static void BuildSteps()
    {
        void Add(int wait,string name,Action a)=>steps.Add((wait,name,a));
        Add(30,"freeze",()=>{Time.timeScale=0;Note("frozen");});
        // A. main load, ortho 8.5
        Add(3,"orig_a",()=>{Counts("before orig_a");Capture("orig_a");});
        Add(2,"orig_b",()=>Capture("orig_b"));
        Add(0,"orig hide",()=>emphasis.outlineWidthPixels=0);
        Add(2,"none_a",()=>Capture("none_a"));
        Add(0,"restore width",()=>emphasis.outlineWidthPixels=oldWidth);
        Add(2,"shared on",()=>SetShared(true));
        Add(3,"shared_a",()=>{Counts("shared_a");Capture("shared_a");DumpMask("mask_a");});
        Add(2,"shared_b",()=>Capture("shared_b"));
        Add(0,"shared hide",()=>emphasis.outlineWidthPixels=0);
        Add(2,"none_shared_a",()=>{Counts("shared width0");Capture("none_shared_a");});
        Add(0,"restore width2",()=>emphasis.outlineWidthPixels=oldWidth);
        Add(2,"shared off",()=>SetShared(false));
        Add(3,"orig_c",()=>{Counts("orig_c");Capture("orig_c");});
        // B. zoom 4.25
        Add(0,"zoom",()=>cam.orthographicSize=4.25f);
        Add(3,"zoom_orig_a",()=>Capture("zoom_orig_a"));
        Add(2,"zoom_orig_b",()=>Capture("zoom_orig_b"));
        Add(0,"zoom shared on",()=>SetShared(true));
        Add(3,"zoom_shared_a",()=>{Capture("zoom_shared_a");DumpMask("zoom_mask_a");});
        Add(0,"zoom shared off",()=>{SetShared(false);cam.orthographicSize=oldOrtho;});
        // C. variety: every enemy kind, several progress points
        Add(2,"variety setup",()=>{RemoveEnemies(game.Session.Enemies.Count);var kinds=(EnemyKind[])Enum.GetValues(typeof(EnemyKind));int k=0;foreach(var kind in kinds)for(int i=0;i<4;i++)Spawn(kind,14+(k++)*0.9f);Time.timeScale=1;Note("variety",kinds.Select(x=>x.ToString()).ToArray());});
        Add(40,"variety freeze",()=>Time.timeScale=0);
        Add(3,"var_orig_a",()=>{Counts("variety");Capture("var_orig_a");});
        Add(2,"var_orig_b",()=>Capture("var_orig_b"));
        Add(0,"var shared on",()=>SetShared(true));
        Add(3,"var_shared_a",()=>{Capture("var_shared_a");DumpMask("var_mask_a");});
        Add(0,"var shared off",()=>SetShared(false));
        // D. toggle-on regression with time running
        Add(2,"regress start",()=>{SetShared(true);Time.timeScale=1;Counts("regress start");});
        Add(30,"remove 10",()=>{RemoveEnemies(10);Counts("after remove 10 (same frame)");});
        Add(3,"after remove",()=>Counts("after remove 10"));
        Add(0,"respawn 10",()=>{for(int i=0;i<10;i++)Spawn(EnemyKind.Attack,16+i*.4f);});
        Add(5,"after respawn",()=>Counts("after respawn 10 (pool reuse)"));
        Add(0,"zoom live",()=>cam.orthographicSize=4.25f);
        Add(10,"zoom live check",()=>{Counts("zoom 4.25 live");cam.orthographicSize=oldOrtho;});
        Add(5,"toggle off live",()=>{SetShared(false);});
        Add(3,"toggle off check",()=>Counts("toggle off live"));
        Add(0,"toggle on live",()=>SetShared(true));
        Add(3,"toggle on check",()=>Counts("toggle on live"));
        Add(0,"restart",()=>{game.Restart();Note("restart called");});
        Add(30,"after restart",()=>{Counts("after restart");Note("restart leftovers",Resources.FindObjectsOfTypeAll<MeshRenderer>().Where(r=>r!=null&&r.name=="SharedOutline"&&r.enabled&&r.gameObject.activeInHierarchy).Select(r=>r.transform.parent.name+" / "+(r.transform.parent.parent!=null?r.transform.parent.parent.name:"-")).ToArray());});
        Add(0,"done",()=>{});
    }
    static void Poll()
    {
        if(!running)return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("120s deadline");
            if(error!=null)throw new Exception("Console error: "+error);
            if(!EditorApplication.isPlaying)return;
            if(EditorApplication.isPaused)throw new Exception("External pause");
            if(!setupDone)
            {
                game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();if(game==null||!game.Ready)return;
                emphasis=UnityEngine.Object.FindAnyObjectByType<CoreGuardianObjectEmphasis>();cam=Camera.main;
                if(Screen.width!=1920||Screen.height!=1080||cam==null||cam.orthographicSize!=8.5f){if(settle++<15)return;throw new Exception("Expected 1920x1080/ortho8.5, actual "+Screen.width+"x"+Screen.height+"/"+cam?.orthographicSize);}
                if(emphasis==null)throw new Exception("No emphasis");
                Setup();BuildSteps();setupDone=true;lastFrame=Time.frameCount;waitFrames=steps[0].wait;Note("setup",new{screenW=Screen.width,screenH=Screen.height,ortho=cam.orthographicSize,outline=emphasis.outlineWidthPixels});return;
            }
            if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
            if(waitFrames>0){waitFrames--;return;}
            steps[index].act();index++;
            if(index>=steps.Count){Finish(null);return;}
            waitFrames=steps[index].wait;
        }
        catch(Exception e){Finish(e.ToString());}
    }
    static void Finish(string failure)
    {
        if(!running)return;running=false;EditorApplication.update-=Poll;Application.logMessageReceived-=Log;
        try{SetShared(false);}catch{}
        try{if(cam!=null)cam.orthographicSize=oldOrtho==0?cam.orthographicSize:oldOrtho;if(emphasis!=null&&oldWidth>0)emphasis.outlineWidthPixels=oldWidth;}catch{}
        Time.captureDeltaTime=oldCapture;Time.timeScale=oldScale;
        File.WriteAllText(Root+"run.json",JsonConvert.SerializeObject(new{error=failure,completedSteps=index,totalSteps=steps.Count,log},Formatting.Indented));
        if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;else Restore();
    }
    static void PlayState(PlayModeStateChange s){if(s==PlayModeStateChange.ExitingPlayMode&&running)Finish("Play stopped externally");if(s==PlayModeStateChange.EnteredEditMode)Restore();}
    static void Restore()
    {
        EditorApplication.playModeStateChanged-=PlayState;
        sizeIndex.SetValue(gameView,savedSizeIndex);Application.runInBackground=oldBackground;Time.captureDeltaTime=oldCapture;Time.timeScale=oldScale;
        var leaks=Resources.FindObjectsOfTypeAll<RenderTexture>().Count(r=>r!=null&&r.name=="Shared emphasis outline mask");
        var mats=Resources.FindObjectsOfTypeAll<Material>().Count(m=>m!=null&&(m.name=="Shared outline mask"||m.name=="Shared outline output"));
        var outputs=Resources.FindObjectsOfTypeAll<MeshRenderer>().Count(r=>r!=null&&r.name=="SharedOutline");
        var sc=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        File.WriteAllText(Root+"restored.json",JsonConvert.SerializeObject(new{playing=EditorApplication.isPlaying,sharedEnabled=Shared,leakedMasks=leaks,leakedMaterials=mats,leakedOutputs=outputs,gameViewIndex=(int)sizeIndex.GetValue(gameView),Application.runInBackground,Time.captureDeltaTime,Time.timeScale,scene=sc.path,dirty=sc.isDirty,roots=sc.rootCount,native=CoreGuardianAttackBodyBatch.Enabled},Formatting.Indented));
    }
}
}
public static class CGTailPad2{static object P(){
