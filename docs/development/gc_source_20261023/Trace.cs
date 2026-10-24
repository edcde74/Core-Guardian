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

// Bounded own Play: one fixed-dt session, 15 warm + 90 observed frames each.
// No Deep Profile, raw frame-tree extraction, recorder array copies, or profile archives.
public static class CGAllocationTrace
{
    const string Root="docs/development/gc_source_20261023/";
    static bool running,oldBackground,oldDriver;
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
    public static object Start()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||Profiler.enabled||ProfilerDriver.deepProfiling||UnityEngine.FrameDebugger.enabled
            ||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Stopped idle clean Editor required");
        if(Profiler.enableAllocationCallstacks)throw new Exception("Allocation callstacks must already be OFF");
        label="trace";
        oldBackground=Application.runInBackground;oldDriver=ProfilerDriver.enabled;oldCapture=Time.captureDeltaTime;oldScale=Time.timeScale;
        File.WriteAllText(Root+label+"_benchmark_before.json",JsonConvert.SerializeObject(new{oldBackground,oldDriver,oldCapture,oldScale,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,rootCount=UnityEngine.SceneManagement.SceneManager.GetActiveScene().rootCount}));
        setDriver.Invoke(null,new object[]{false});Profiler.enabled=false;Application.runInBackground=true;
        running=true;run=-1;deadline=EditorApplication.timeSinceStartup+30;EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=PlayState;Application.logMessageReceived+=Log;
        EditorApplication.isPlaying=true;return new{started=true,warm=15,samples=90,repetitions=1,deadlineSeconds=30};
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
                var names=new[]{"Main Thread","CG.Emphasis.Present","CG.Emphasis.Tint","CG.Mask.Present","CG.SpriteReferences.Read","GC.Collect","GarbageCollector.CollectIncremental","Standard Draw Calls Count","SRP Batcher Draw Calls Count","SetPass Calls Count","GC Allocated In Frame"};
                var selected=available.Select(ProfilerRecorderHandle.GetDescription).Where(d=>names.Contains(d.Name)).GroupBy(d=>d.Name).Select(g=>g.First()).ToArray();
                foreach(var d in selected)counters[d.Name]=ProfilerRecorder.StartNew(d.Category,d.Name,1,ProfilerRecorderOptions.StartImmediately|ProfilerRecorderOptions.SumAllSamplesInFrame|ProfilerRecorderOptions.WrapAroundWhenCapacityReached);
                File.WriteAllText(Root+label+"_benchmark_environment.json",JsonConvert.SerializeObject(new{unity=Application.unityVersion,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,ramMB=SystemInfo.systemMemorySize,Screen.width,Screen.height,QualitySettings.vSyncCount,Application.targetFrameRate,camera=Camera.main.orthographicSize,Time.captureDeltaTime,policy="fixed simulation dt1/60; wall frame intervals from realtimeSinceStartup; standard/SRP Rendering counters; 15 warm + 90 observed each; 90 standard Profiler frames for allocation provenance; not normal before-after FPS",counters=selected.Select(d=>new{d.Name,category=d.Category.Name,unit=d.UnitType.ToString()})},Formatting.Indented));
                run=0;Setup();return;
            }
            if(last==Time.frameCount)return;int gap=Time.frameCount-last;last=Time.frameCount;
            double now=Time.realtimeSinceStartupAsDouble,ms=(now-previous)*1000;previous=now;
            if(warm-->0){if(warm==0){oldCollections=GC.CollectionCount(0);setDriver.Invoke(null,new object[]{true});Profiler.enabled=true;}return;}
            long pollBefore=GC.GetAllocatedBytesForCurrentThread();
            if(game.Session.Enemies.Count!=128||game.Session.Towers.Count!=15)throw new Exception("Load changed during sample");
            frames.Add(new{frame=last,profilerFrame=ProfilerDriver.lastFrameIndex,collections0=GC.CollectionCount(0),collectionDelta0=GC.CollectionCount(0)-oldCollections,gap,ms,enemies=game.Session.Enemies.Count,towers=game.Session.Towers.Count,projectiles=game.Session.Projectiles.Count,sources=CoreGuardianAttackBodyBatch.LastSources,textureBytes=CoreGuardianAttackBodyBatch.TextureBytes,bufferBytes=CoreGuardianAttackBodyBatch.BufferBytes,draw=UnityStats.drawCalls,setpass=UnityStats.setPassCalls,counters=counters.ToDictionary(p=>p.Key,p=>new{valid=p.Value.Valid&&p.Value.Count>0,value=p.Value.Valid&&p.Value.Count>0?(long?)p.Value.LastValue:null})});
            diagnosticPollBytes+=GC.GetAllocatedBytesForCurrentThread()-pollBefore;oldCollections=GC.CollectionCount(0);
            if(frames.Count<90)return;
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
        if(error==null)try{TraceParents();}catch(Exception e){File.WriteAllText(Root+"trace_extract_error.json",JsonConvert.SerializeObject(new{error=e.ToString(),playing=EditorApplication.isPlaying},Formatting.Indented));}
    }
    static void TraceParents()
    {
        var rows=Newtonsoft.Json.Linq.JArray.FromObject(frames);
        var selected=rows.OrderByDescending(f=>(long?)f["counters"]?["GC Allocated In Frame"]?["value"]??0).Select(f=>(int)f["profilerFrame"]).Distinct().Take(2).ToArray();
        var output=new List<object>();
        foreach(int frame in selected)
        {
            using var raw=ProfilerDriver.GetRawFrameDataView(frame,0);
            if(!raw.valid||raw.threadName!="Main Thread"){output.Add(new{frame,skipped="Main Thread unavailable"});continue;}
            if(raw.sampleCount>20000){output.Add(new{frame,raw.sampleCount,skipped="20000 sample cap"});continue;}
            var watch=System.Diagnostics.Stopwatch.StartNew();
            var parents=new List<(int end,string name)>();var allocationRows=new List<object>();
            var totals=new Dictionary<string,long>();var counts=new Dictionary<string,int>();var collectRows=new List<object>();
            long bytes=0;int visited=0;bool truncated=false;
            for(int i=0;i<raw.sampleCount;i++)
            {
                if((i&255)==0&&watch.ElapsedMilliseconds>1000){truncated=true;break;}
                while(parents.Count>0&&i>=parents[parents.Count-1].end)parents.RemoveAt(parents.Count-1);
                string name=raw.GetSampleName(i)??"<root>";visited++;
                if(name=="GC.Alloc"&&raw.GetSampleMetadataCount(i)>0)
                {
                    long size=raw.GetSampleMetadataAsLong(i,0);bytes+=size;
                    string path=string.Join(" > ",parents.Skip(Math.Max(0,parents.Count-8)).Select(p=>p.name));
                    totals.TryGetValue(path,out long old);totals[path]=old+size;counts.TryGetValue(path,out int calls);counts[path]=calls+1;
                    allocationRows.Add(new{sample=i,size,path});
                }
                if(name=="GC.Collect"||name=="GarbageCollector.CollectIncremental")collectRows.Add(new{sample=i,name,ms=raw.GetSampleTimeMs(i)});
                int end=i+raw.GetSampleChildrenCountRecursive(i)+1;
                if(end>i+1)parents.Add((end,name));
            }
            output.Add(new{frame,raw.threadName,raw.threadGroupName,raw.sampleCount,raw.frameTimeMs,visited,truncated,elapsedMs=watch.ElapsedMilliseconds,gcBytes=bytes,allocationRows,collectRows,parentsByBytes=totals.OrderByDescending(p=>p.Value).Take(20).Select(p=>new{path=p.Key,bytes=p.Value,calls=counts[p.Key]})});
        }
        File.WriteAllText(Root+"trace_parents.json",JsonConvert.SerializeObject(new{frameLimit=2,sampleLimitPerFrame=20000,timeLimitPerFrameMs=1000,profileEditor=ProfilerDriver.profileEditor,deep=ProfilerDriver.deepProfiling,callstacks=Profiler.enableAllocationCallstacks,output},Formatting.Indented));
    }
}
