using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;
using UnityEditorInternal;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using CoreGuardian;
using Newtonsoft.Json;

// Bounded own Play: two matched fixed-dt sessions, 15 warm + 90 observed frames each.
// No Deep Profile, raw frame-tree extraction, recorder array copies, or profile archives.
public static class CGAttackNativeBenchmark
{
    const string Root="docs/development/attack_body_batch_20261023/";
    static bool running,oldBackground,oldDriver;
    static float oldCapture,oldScale;
    static double deadline,previous;
    static int run=-1,warm,last;
    static string error;
    static CoreGuardianPrototype game;
    static readonly List<object> frames=new List<object>();
    static readonly Dictionary<string,ProfilerRecorder> counters=new Dictionary<string,ProfilerRecorder>();
    static readonly List<object> runs=new List<object>();
    static readonly MethodInfo setDriver=typeof(ProfilerDriver).GetMethod("SetProfilingEnabled",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
    public static object Start()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||Profiler.enabled||ProfilerDriver.deepProfiling||UnityEngine.FrameDebugger.enabled
            ||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Stopped idle clean Editor required");
        oldBackground=Application.runInBackground;oldDriver=ProfilerDriver.enabled;oldCapture=Time.captureDeltaTime;oldScale=Time.timeScale;
        File.WriteAllText(Root+"benchmark_before.json",JsonConvert.SerializeObject(new{oldBackground,oldDriver,oldCapture,oldScale,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,rootCount=UnityEngine.SceneManagement.SceneManager.GetActiveScene().rootCount}));
        setDriver.Invoke(null,new object[]{false});Profiler.enabled=false;Application.runInBackground=true;
        running=true;run=-1;deadline=EditorApplication.timeSinceStartup+30;EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=PlayState;Application.logMessageReceived+=Log;
        EditorApplication.isPlaying=true;return new{started=true,warm=15,samples=90,repetitions=1,deadlineSeconds=30};
    }
    static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)error=message;}
    static void Setup()
    {
        CoreGuardianAttackBodyBatch.SetEnabled(run==1);game.Diagnostic();var s=game.Session;
        for(int i=0;i<128;i++)
        {
            var kind=i%8==0?EnemyKind.Buff:EnemyKind.Attack;var spec=s.Data.Enemies[kind];
            s.DebugSpawn(kind,20+(i%12)*.3f,new EnemySpec{Kind=kind,Health=100000,Speed=spec.Speed,Defense=spec.Defense,Gold=0,CoreDamage=0,BuffRadius=spec.BuffRadius,BuffSpeed=spec.BuffSpeed});
        }
        var p=s.Enemies[0].Position;var size=game.BoardSize;
        var cells=Enumerable.Range(0,size.x).SelectMany(x=>Enumerable.Range(0,size.y).Select(y=>new Vector2Int(x,y))).Where(s.IsBuildable).OrderBy(c=>((Vector2)c-p).sqrMagnitude).ToArray();
        for(int i=0;i<Math.Min(32,cells.Length);i++)game.Place(i%5==4?TowerKind.Support:TowerKind.Multi,cells[i]);
        game.StartCombat();typeof(Session).GetField("waveTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(s,.0001f);
        if(s.Enemies.Count!=128||s.Towers.Count!=15)throw new Exception("Unexpected load counts");
        warm=15;frames.Clear();last=Time.frameCount;previous=Time.realtimeSinceStartupAsDouble;
    }
    static void Poll()
    {
        if(!running)return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("30s diagnostic deadline");
            if(error!=null)throw new Exception(error);
            if(!EditorApplication.isPlaying)return;
            if(EditorApplication.isPaused)throw new Exception("External pause");
            if(run<0)
            {
                game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();if(game==null||!game.Ready)return;
                if(Screen.width!=1920||Screen.height!=1080||Camera.main.orthographicSize!=8.5f)throw new Exception("Expected 1920x1080 / ortho8.5");
                Time.timeScale=1;Time.captureDeltaTime=1/60f;
                var available=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(available);
                var names=new[]{"Main Thread","CG.AttackBodyBatch","Standard Draw Calls Count","SRP Batcher Draw Calls Count","SetPass Calls Count","GC Allocated In Frame"};
                var selected=available.Select(ProfilerRecorderHandle.GetDescription).Where(d=>names.Contains(d.Name)).GroupBy(d=>d.Name).Select(g=>g.First()).ToArray();
                foreach(var d in selected)counters[d.Name]=ProfilerRecorder.StartNew(d.Category,d.Name,1,ProfilerRecorderOptions.StartImmediately|ProfilerRecorderOptions.SumAllSamplesInFrame|ProfilerRecorderOptions.WrapAroundWhenCapacityReached);
                File.WriteAllText(Root+"benchmark_environment.json",JsonConvert.SerializeObject(new{unity=Application.unityVersion,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,ramMB=SystemInfo.systemMemorySize,Screen.width,Screen.height,QualitySettings.vSyncCount,Application.targetFrameRate,camera=Camera.main.orthographicSize,Time.captureDeltaTime,policy="fixed simulation dt1/60; wall frame intervals from realtimeSinceStartup; standard/SRP Rendering counters; 15 warm + 90 observed each; one matched diagnostic pair",counters=selected.Select(d=>new{d.Name,category=d.Category.Name,unit=d.UnitType.ToString()})},Formatting.Indented));
                run=0;Setup();return;
            }
            if(last==Time.frameCount)return;int gap=Time.frameCount-last;last=Time.frameCount;
            double now=Time.realtimeSinceStartupAsDouble,ms=(now-previous)*1000;previous=now;
            if(warm-->0)return;
            if(game.Session.Enemies.Count!=128||game.Session.Towers.Count!=15)throw new Exception("Load changed during sample");
            frames.Add(new{frame=last,gap,ms,enemies=game.Session.Enemies.Count,towers=game.Session.Towers.Count,projectiles=game.Session.Projectiles.Count,sources=CoreGuardianAttackBodyBatch.LastSources,textureBytes=CoreGuardianAttackBodyBatch.TextureBytes,bufferBytes=CoreGuardianAttackBodyBatch.BufferBytes,draw=UnityStats.drawCalls,setpass=UnityStats.setPassCalls,counters=counters.ToDictionary(p=>p.Key,p=>new{valid=p.Value.Valid&&p.Value.Count>0,value=p.Value.Valid&&p.Value.Count>0?(long?)p.Value.LastValue:null})});
            if(frames.Count<90)return;
            string label=run==0?"native_baseline":"native_candidate";
            File.WriteAllText(Root+label+".json",JsonConvert.SerializeObject(new{label,frames},Formatting.Indented));runs.Add(new{label,samples=frames.Count});
            if(run==0){run=1;Setup();}else Finish(null);
        }
        catch(Exception e){Finish(e.ToString());}
    }
    static void Finish(string failure)
    {
        if(!running)return;running=false;error=failure;EditorApplication.update-=Poll;Application.logMessageReceived-=Log;
        CoreGuardianAttackBodyBatch.SetEnabled(false);foreach(var recorder in counters.Values)recorder.Dispose();counters.Clear();
        Time.captureDeltaTime=oldCapture;Time.timeScale=oldScale;Profiler.enabled=false;
        File.WriteAllText(Root+"benchmark_completion.json",JsonConvert.SerializeObject(new{error,runs,stopRequested=EditorApplication.isPlaying},Formatting.Indented));
        if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;else RestoreEditor();
    }
    static void PlayState(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode&&running)Finish("Play stopped externally");if(state==PlayModeStateChange.EnteredEditMode)RestoreEditor();}
    static void RestoreEditor()
    {
        EditorApplication.playModeStateChanged-=PlayState;Application.runInBackground=oldBackground;Time.captureDeltaTime=oldCapture;Time.timeScale=oldScale;
        setDriver.Invoke(null,new object[]{oldDriver});Profiler.enabled=false;
        File.WriteAllText(Root+"benchmark_restored.json",JsonConvert.SerializeObject(new{playing=EditorApplication.isPlaying,compiling=EditorApplication.isCompiling,deep=ProfilerDriver.deepProfiling,recording=Profiler.enabled,driver=ProfilerDriver.enabled,Application.runInBackground,Time.captureDeltaTime,Time.timeScale,candidate=CoreGuardianAttackBodyBatch.Enabled,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,dirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty},Formatting.Indented));
    }
}
