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

// Bounded own Play: one fixed-dt session, 15 warm + 12 observed frames each.
// One 12-frame Deep window; bounded hierarchy aggregation only; no raw frame-tree walks or recorder copies.
public static class CGDeepStallProbe
{
    const string Root="docs/development/deep_initial_stall_20261023/";
    static bool running,oldBackground,oldDriver;
    sealed class Saved {public bool oldBackground,oldDriver,oldDeep,oldProfileEditor;public float oldCapture,oldScale;}
    static Saved saved;
    static float oldCapture,oldScale;
    static double deadline,previous;
    static int run=-1,warm,last;
    static string error,label;
    static CoreGuardianPrototype game;
    static long diagnosticPollBytes;
    static int oldCollections;
    static readonly List<object> frames=new List<object>();
    static readonly Dictionary<string,ProfilerRecorder> counters=new Dictionary<string,ProfilerRecorder>();
    static readonly List<object> runs=new List<object>();
    static readonly MethodInfo setDriver=typeof(ProfilerDriver).GetMethod("SetProfilingEnabled",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
    public static object Prepare()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||Profiler.enabled||ProfilerDriver.deepProfiling||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Stopped clean Editor, recording/Deep OFF required");
        var state=new Saved{oldBackground=Application.runInBackground,oldDriver=ProfilerDriver.enabled,oldDeep=ProfilerDriver.deepProfiling,oldProfileEditor=ProfilerDriver.profileEditor,oldCapture=Time.captureDeltaTime,oldScale=Time.timeScale};
        File.WriteAllText(Root+"original_state.json",JsonConvert.SerializeObject(state,Formatting.Indented));
        setDriver.Invoke(null,new object[]{false});Profiler.enabled=false;ProfilerDriver.profileEditor=true;ProfilerDriver.deepProfiling=true;
        EditorUtility.RequestScriptReload();return new{deepEnabled=true,profileEditor=true,reloadRequested=true,recording=false};
    }
    public static object Start()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||Profiler.enabled||!ProfilerDriver.deepProfiling||UnityEngine.FrameDebugger.enabled
            ||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Stopped idle clean Editor required");
        if(Profiler.enableAllocationCallstacks)throw new Exception("Allocation callstacks must already be OFF");
        label="deep";
        saved=JsonConvert.DeserializeObject<Saved>(File.ReadAllText(Root+"original_state.json"));
        oldBackground=saved.oldBackground;oldDriver=saved.oldDriver;oldCapture=saved.oldCapture;oldScale=saved.oldScale;
        File.WriteAllText(Root+label+"_benchmark_before.json",JsonConvert.SerializeObject(new{oldBackground,oldDriver,oldCapture,oldScale,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,rootCount=UnityEngine.SceneManagement.SceneManager.GetActiveScene().rootCount}));
        setDriver.Invoke(null,new object[]{false});Profiler.enabled=false;Application.runInBackground=true;
        running=true;run=-1;deadline=EditorApplication.timeSinceStartup+30;EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=PlayState;Application.logMessageReceived+=Log;
        EditorApplication.isPlaying=true;return new{started=true,warm=15,samples=12,repetitions=1,deadlineSeconds=30};
    }
    static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)error=message;}
    static void Setup()
    {
        CoreGuardianAttackBodyBatch.SetEnabled(false);game.Diagnostic();var s=game.Session;
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
                var names=new[]{"Main Thread","PlayerLoop","EditorLoop","WaitForTargetFPS","Semaphore.WaitForSignal","GC.Collect","CG.Emphasis.Present","CG.Emphasis.Tint","CG.Mask.Present","CG.SpriteReferences.Read","GC.Collect","GarbageCollector.CollectIncremental","Standard Draw Calls Count","SRP Batcher Draw Calls Count","SetPass Calls Count","GC Allocated In Frame"};
                var selected=available.Select(ProfilerRecorderHandle.GetDescription).Where(d=>names.Contains(d.Name)).GroupBy(d=>d.Name).Select(g=>g.First()).ToArray();
                foreach(var d in selected)counters[d.Name]=ProfilerRecorder.StartNew(d.Category,d.Name,1,ProfilerRecorderOptions.StartImmediately|ProfilerRecorderOptions.SumAllSamplesInFrame|ProfilerRecorderOptions.WrapAroundWhenCapacityReached);
                File.WriteAllText(Root+label+"_benchmark_environment.json",JsonConvert.SerializeObject(new{unity=Application.unityVersion,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,ramMB=SystemInfo.systemMemorySize,Screen.width,Screen.height,QualitySettings.vSyncCount,Application.targetFrameRate,camera=Camera.main.orthographicSize,Time.captureDeltaTime,policy="fixed simulation dt1/60; wall frame intervals from realtimeSinceStartup; standard/SRP Rendering counters; 15 warm + 12 observed each; 12 Deep Profile frames for allocation provenance; not normal before-after FPS",counters=selected.Select(d=>new{d.Name,category=d.Category.Name,unit=d.UnitType.ToString()})},Formatting.Indented));
                run=0;Setup();return;
            }
            if(last==Time.frameCount)return;int gap=Time.frameCount-last;last=Time.frameCount;
            double now=Time.realtimeSinceStartupAsDouble,ms=(now-previous)*1000;previous=now;
            if(warm-->0){if(warm==0){oldCollections=GC.CollectionCount(0);setDriver.Invoke(null,new object[]{true});Profiler.enabled=true;}return;}
            long pollBefore=GC.GetAllocatedBytesForCurrentThread();
            if(game.Session.Enemies.Count!=128||game.Session.Towers.Count!=15)throw new Exception("Load changed during sample");
            frames.Add(new{frame=last,profilerFrame=ProfilerDriver.lastFrameIndex,collections0=GC.CollectionCount(0),collectionDelta0=GC.CollectionCount(0)-oldCollections,gap,ms,enemies=game.Session.Enemies.Count,towers=game.Session.Towers.Count,projectiles=game.Session.Projectiles.Count,sources=CoreGuardianAttackBodyBatch.LastSources,textureBytes=CoreGuardianAttackBodyBatch.TextureBytes,bufferBytes=CoreGuardianAttackBodyBatch.BufferBytes,draw=UnityStats.drawCalls,setpass=UnityStats.setPassCalls,counters=counters.ToDictionary(p=>p.Key,p=>new{valid=p.Value.Valid&&p.Value.Count>0,value=p.Value.Valid&&p.Value.Count>0?(long?)p.Value.LastValue:null})});
            diagnosticPollBytes+=GC.GetAllocatedBytesForCurrentThread()-pollBefore;oldCollections=GC.CollectionCount(0);
            if(frames.Count<12)return;
            Profiler.enabled=false;setDriver.Invoke(null,new object[]{false});
            File.WriteAllText(Root+label+".json",JsonConvert.SerializeObject(new{label,frames},Formatting.Indented));runs.Add(new{label,samples=frames.Count});
            Finish(null);
        }
        catch(Exception e){Finish(e.ToString());}
    }
    static void Finish(string failure)
    {
        if(!running)return;running=false;error=failure;EditorApplication.update-=Poll;Application.logMessageReceived-=Log;
        Profiler.enabled=false;setDriver.Invoke(null,new object[]{false});
        CoreGuardianAttackBodyBatch.SetEnabled(false);foreach(var recorder in counters.Values)recorder.Dispose();counters.Clear();
        Time.captureDeltaTime=oldCapture;Time.timeScale=oldScale;Profiler.enabled=false;
        File.WriteAllText(Root+label+"_benchmark_completion.json",JsonConvert.SerializeObject(new{error,runs,diagnosticPollBytes,stopRequested=EditorApplication.isPlaying},Formatting.Indented));
        if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;else RestoreEditor();
    }
    static void PlayState(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode&&running)Finish("Play stopped externally");if(state==PlayModeStateChange.EnteredEditMode)RestoreEditor();}
    static void RestoreEditor()
    {
        EditorApplication.playModeStateChanged-=PlayState;Application.runInBackground=oldBackground;Time.captureDeltaTime=oldCapture;Time.timeScale=oldScale;
        setDriver.Invoke(null,new object[]{oldDriver});Profiler.enabled=false;
        File.WriteAllText(Root+label+"_benchmark_restored.json",JsonConvert.SerializeObject(new{playing=EditorApplication.isPlaying,compiling=EditorApplication.isCompiling,deep=ProfilerDriver.deepProfiling,recording=Profiler.enabled,driver=ProfilerDriver.enabled,Application.runInBackground,Time.captureDeltaTime,Time.timeScale,candidate=CoreGuardianAttackBodyBatch.Enabled,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,dirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty},Formatting.Indented));
        try{if(error==null)Analyze();}
        catch(Exception e){File.WriteAllText(Root+"analysis_error.json",JsonConvert.SerializeObject(new{error=e.ToString()},Formatting.Indented));}
        finally
        {
            ProfilerDriver.profileEditor=saved.oldProfileEditor;ProfilerDriver.deepProfiling=saved.oldDeep;Profiler.enabled=false;
            File.WriteAllText(Root+"restore_reload_requested.json",JsonConvert.SerializeObject(new{deep=ProfilerDriver.deepProfiling,profileEditor=ProfilerDriver.profileEditor,playing=EditorApplication.isPlaying,recording=Profiler.enabled},Formatting.Indented));
            EditorUtility.RequestScriptReload();
        }
    }
    static void Analyze()
    {
        var framesJson=Newtonsoft.Json.Linq.JArray.FromObject(frames);
        var worst=framesJson.OrderByDescending(f=>(double)f["ms"]).First();
        var near=framesJson.OrderBy(f=>Math.Abs((int)f["frame"]-22)).First();
        var selected=new[]{worst,near}.Select(f=>(int)f["profilerFrame"]).Distinct().Take(2).ToArray();
        var output=new List<object>();
        foreach(int frame in selected)
        {
            int sampleCount;using(var raw=ProfilerDriver.GetRawFrameDataView(frame,0))sampleCount=raw.valid?raw.sampleCount:0;
            if(sampleCount==0||sampleCount>1000000){output.Add(new{frame,sampleCount,skipped="raw data unavailable or over 1000000 sample safety cap"});continue;}
            using var view=ProfilerDriver.GetHierarchyFrameDataView(frame,0,UnityEditor.Profiling.HierarchyFrameDataView.ViewModes.Default,UnityEditor.Profiling.HierarchyFrameDataView.columnTotalTime,false);
            if(!view.valid){output.Add(new{frame,sampleCount,skipped="invalid hierarchy"});continue;}
            var watch=System.Diagnostics.Stopwatch.StartNew();var queue=new List<(int id,int depth,string path,double weight)>();var rows=new List<object>();
            queue.Add((view.GetRootItemID(),0,"",view.GetItemColumnDataAsDouble(view.GetRootItemID(),5)));
            while(queue.Count>0&&rows.Count<120&&watch.ElapsedMilliseconds<1500)
            {
                queue.Sort((a,b)=>b.weight.CompareTo(a.weight));
                var item=queue[0];queue.RemoveAt(0);string name=view.GetItemName(item.id);string path=item.path.Length==0?name:item.path+" > "+name;
                double total=view.GetItemColumnDataAsDouble(item.id,5),self=view.GetItemColumnDataAsDouble(item.id,6),calls=view.GetItemColumnDataAsDouble(item.id,3),gc=view.GetItemColumnDataAsDouble(item.id,4);
                rows.Add(new{path,name,depth=item.depth,totalMs=total,selfMs=self,calls,gcBytes=gc});
                if(item.depth>=24||total<.1)continue;
                var children=new List<int>();view.GetItemChildren(item.id,children);
                foreach(int child in children.Take(6))queue.Add((child,item.depth+1,path,view.GetItemColumnDataAsDouble(child,5)));
            }
            output.Add(new{frame,sampleCount,visited=rows.Count,elapsedMs=watch.ElapsedMilliseconds,unexpanded=queue.Count,rows});
        }
        File.WriteAllText(Root+"hierarchy.json",JsonConvert.SerializeObject(new{deep=true,profileEditor=true,frameLimit=2,itemLimit=120,childrenPerItem=6,output},Formatting.Indented));
    }
}
