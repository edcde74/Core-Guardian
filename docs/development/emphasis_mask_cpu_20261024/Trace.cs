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
public static class CGEmphasisMaskTrace
{
    const string Root="docs/development/emphasis_mask_cpu_20261024/";
    static bool running,oldBackground,oldDriver;
    static float oldCapture,oldScale;
    static double deadline,previous;
    static int run=-1,warm,last;
    static string error,label;
    static UnityEngine.Object gameView;static PropertyInfo sizeIndex;static int savedSizeIndex,settleFrames;
    static CoreGuardianPrototype game;
    static int previousCollection;
    static readonly List<object> frames=new List<object>();
    static readonly Dictionary<string,ProfilerRecorder> counters=new Dictionary<string,ProfilerRecorder>();
    static readonly List<object> runs=new List<object>();
    static readonly MethodInfo setDriver=typeof(ProfilerDriver).GetMethod("SetProfilingEnabled",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
    public static object Diagnose()=>Start("diagnostic");
    public static object Baseline(int repetition)=>Start("before_"+repetition);
    public static object Candidate(int repetition)=>Start("after_"+repetition);
    static object Start(string runLabel)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||Profiler.enabled||ProfilerDriver.deepProfiling||UnityEngine.FrameDebugger.enabled
            ||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Stopped idle clean Editor required");
        PreserveProfileBuffer();label=runLabel;
        if(label=="before_2"||label=="before_3"||(label.StartsWith("after_")||label=="diagnostic"))
        {
            var type=typeof(Editor).Assembly.GetType("UnityEditor.GameView");gameView=Resources.FindObjectsOfTypeAll(type).First();
            sizeIndex=type.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);savedSizeIndex=(int)sizeIndex.GetValue(gameView);
            File.WriteAllText(Root+label+"_game_view_before.json",JsonConvert.SerializeObject(new{savedSizeIndex,target=type.GetProperty("targetRenderSize",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(gameView).ToString()}));
            sizeIndex.SetValue(gameView,3);settleFrames=0;
        }
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
        previousCollection=GC.CollectionCount(0);warm=15;frames.Clear();last=Time.frameCount;previous=Time.realtimeSinceStartupAsDouble;
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
                if(Screen.width!=1920||Screen.height!=1080||Camera.main==null||Camera.main.orthographicSize!=8.5f)
                {
                    if(settleFrames++==0)File.WriteAllText(Root+label+"_initial_view.json",JsonConvert.SerializeObject(new{Screen.width,Screen.height,camera=Camera.main?.orthographicSize}));
                    if(gameView!=null&&settleFrames<15)return;
                    throw new Exception("Expected 1920x1080 / ortho8.5; actual "+Screen.width+"x"+Screen.height+" / "+Camera.main?.orthographicSize);
                }
                Time.timeScale=1;Time.captureDeltaTime=1/60f;
                var available=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(available);
                var names=new[]{"CG.Mask.CaptureSources","CG.Mask.ProjectionCrop","CG.Mask.RequestInput","CG.Mask.QueueInputs","CG.Mask.OutputGeometry","CG.Mask.OutputSorting","CG.Mask.OutputBinding","CG.Emphasis.Structure","CG.Emphasis.Restore","CG.Mask.SourceFilter","CG.Mask.BoundsCrop","CG.Mask.QuadUpload","CG.Mask.Resources","CG.Mask.CacheLookup","CG.Mask.Output","CG.Mask.Parts","CG.Mask.MeshUpload","CG.Mask.Execute","CG.Mask.PageProducer","CG.SpriteReferences.Read","Main Thread","PlayerLoop","EditorLoop","WaitForTargetFPS","WaitForLastPresentation","Semaphore.WaitForSignal","WaitForJobGroupID","Camera.Render","GC.Collect","GarbageCollector.CollectIncremental","Shader.EditorCompileVariant","Shader.DynamicLoadGPUProgram","Shader.CreateGPUPrograms","Shader.CompileGPUProgram","Texture.WaitForAsyncUpload","CG.Emphasis.Present","CG.Emphasis.Tint","CG.Mask.Present","CG.PhotoShadow.Present","Standard Draw Calls Count","SRP Batcher Draw Calls Count","GC Allocated In Frame","Assembly-CSharp.dll!::CoreGuardianPrototype.Update() [Invoke]","Assembly-CSharp.dll!::CoreGuardianHud.Update() [Invoke]","Assembly-CSharp.dll!::CoreGuardianCombatAudio.Update() [Invoke]","Assembly-CSharp.dll!::CoreGuardianCombatArt.Update() [Invoke]"};
                var selected=available.Select(ProfilerRecorderHandle.GetDescription).Where(d=>names.Contains(d.Name)).GroupBy(d=>d.Name).Select(g=>g.First()).ToArray();
                foreach(var d in selected)counters[d.Name]=ProfilerRecorder.StartNew(d.Category,d.Name,1,ProfilerRecorderOptions.StartImmediately|ProfilerRecorderOptions.SumAllSamplesInFrame|ProfilerRecorderOptions.WrapAroundWhenCapacityReached|ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
                File.WriteAllText(Root+label+"_benchmark_environment.json",JsonConvert.SerializeObject(new{unity=Application.unityVersion,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,ramMB=SystemInfo.systemMemorySize,Screen.width,Screen.height,QualitySettings.vSyncCount,Application.targetFrameRate,camera=Camera.main.orthographicSize,Time.captureDeltaTime,policy="fixed simulation dt1/60; wall frame intervals from realtimeSinceStartup; standard/SRP Rendering counters; 15 warm + 90 observed each; each run warm15/sample90; 3 runs per condition; current-thread marker sums; recording OFF",counters=selected.Select(d=>new{d.Name,category=d.Category.Name,unit=d.UnitType.ToString()})},Formatting.Indented));
                run=0;Setup();return;
            }
            if(last==Time.frameCount)return;int gap=Time.frameCount-last;last=Time.frameCount;
            double now=Time.realtimeSinceStartupAsDouble,ms=(now-previous)*1000;previous=now;
            if(gap!=1)throw new Exception("Nonunit engine frame gap: "+gap);
            if(warm>0){warm--;previousCollection=GC.CollectionCount(0);if(warm==0){ProfilerDriver.ClearAllFrames();setDriver.Invoke(null,new object[]{true});Profiler.enabled=true;}return;}
            if(game.Session.Enemies.Count!=128||game.Session.Towers.Count!=15)throw new Exception("Load changed during sample");
            frames.Add(new{frame=last,profilerFrame=ProfilerDriver.lastFrameIndex,gap,ms,collectionCount0=GC.CollectionCount(0),collectionsSincePrevious=GC.CollectionCount(0)-previousCollection,enemies=game.Session.Enemies.Count,towers=game.Session.Towers.Count,projectiles=game.Session.Projectiles.Count,sources=CoreGuardianAttackBodyBatch.LastSources,textureBytes=CoreGuardianAttackBodyBatch.TextureBytes,bufferBytes=CoreGuardianAttackBodyBatch.BufferBytes,draw=UnityStats.drawCalls,setpass=UnityStats.setPassCalls,counters=counters.ToDictionary(p=>p.Key,p=>new{valid=p.Value.Valid&&p.Value.Count>0,value=p.Value.Valid&&p.Value.Count>0?(long?)p.Value.LastValue:null})});
            previousCollection=GC.CollectionCount(0);
            if(frames.Count<90)return;Profiler.enabled=false;setDriver.Invoke(null,new object[]{false});CaptureBufferState();
            File.WriteAllText(Root+label+".json",JsonConvert.SerializeObject(new{label,frames},Formatting.Indented));runs.Add(new{label,samples=frames.Count});
            Finish(null);
        }
        catch(Exception e){Finish(e.ToString());}
    }
    static bool priorSaved;
    static int priorFirst,priorLast;
    static void PreserveProfileBuffer()
    {
        if(Profiler.enableAllocationCallstacks||ProfilerDriver.profileEditor)throw new Exception("Callstacks/profileEditor must already be OFF");
        priorFirst=ProfilerDriver.firstFrameIndex;priorLast=ProfilerDriver.lastFrameIndex;priorSaved=false;
        if(priorFirst>=0&&priorLast>=priorFirst)
        {
            string path=Path.GetFullPath(Root+"prior_profile.data");
            if(!ProfilerDriver.SaveProfile(path)||!File.Exists(path)||new FileInfo(path).Length<=4)throw new Exception("Cannot preserve prior Profiler buffer");
            priorSaved=true;
        }
        File.WriteAllText(Root+"prior_profile.json",JsonConvert.SerializeObject(new{priorFirst,priorLast,priorSaved},Formatting.Indented));
    }
    static void FinishTraceAndRestoreBuffer()
    {
        try
        {
            if(error==null)
            {
                bool saved=ProfilerDriver.SaveProfile(Path.GetFullPath(Root+"diagnostic_profile.data"));
                File.WriteAllText(Root+"diagnostic_profile_saved.json",JsonConvert.SerializeObject(new{saved,first=ProfilerDriver.firstFrameIndex,last=ProfilerDriver.lastFrameIndex}));
                TraceFixedFrames();
            }
        }
        catch(Exception e){File.WriteAllText(Root+"diagnostic_extract_error.json",JsonConvert.SerializeObject(new{error=e.ToString()},Formatting.Indented));}
        finally
        {
            ProfilerDriver.ClearAllFrames();bool restored=!priorSaved||ProfilerDriver.LoadProfile(Path.GetFullPath(Root+"prior_profile.data"),false);
            File.WriteAllText(Root+"profiler_buffer_restored.json",JsonConvert.SerializeObject(new{restored,priorFirst,priorLast,actualFirst=ProfilerDriver.firstFrameIndex,actualLast=ProfilerDriver.lastFrameIndex},Formatting.Indented));
        }
    }
    static void TraceFixedFrames()
    {
        var rows=Newtonsoft.Json.Linq.JArray.FromObject(frames);
        var selected=rows.OrderByDescending(f=>(long?)f["counters"]?["CG.Mask.Present"]?["value"]??0).Select(f=>(int)f["profilerFrame"]).Distinct().Take(2).ToArray();
        var output=new List<object>();
        foreach(int frame in selected)
        {
            using var raw=ProfilerDriver.GetRawFrameDataView(frame,0);
            if(!raw.valid||raw.threadName!="Main Thread")throw new Exception("Selected Main Thread unavailable: "+frame);
            if(raw.sampleCount>20000)throw new Exception("20000 sample cap: "+raw.sampleCount);
            var watch=System.Diagnostics.Stopwatch.StartNew();int count=raw.sampleCount;
            var names=new string[count];var times=new double[count];var children=new double[count];var parent=new int[count];var ends=new int[count];var stack=new List<int>();
            var bytes=new long[count];
            for(int i=0;i<count;i++)
            {
                if((i&255)==0&&watch.ElapsedMilliseconds>1000)throw new Exception("1000ms extraction bound");
                while(stack.Count>0&&i>=ends[stack[stack.Count-1]])stack.RemoveAt(stack.Count-1);
                parent[i]=stack.Count>0?stack[stack.Count-1]:-1;
                names[i]=raw.GetSampleName(i)??"<root>";times[i]=raw.GetSampleTimeMs(i);
                if(parent[i]>=0)children[parent[i]]+=times[i];
                if(names[i]=="GC.Alloc"&&raw.GetSampleMetadataCount(i)>0)bytes[i]=raw.GetSampleMetadataAsLong(i,0);
                ends[i]=i+raw.GetSampleChildrenCountRecursive(i)+1;if(ends[i]>i+1)stack.Add(i);
            }
            var groups=Enumerable.Range(0,count).GroupBy(i=>names[i]).Select(g=>new{name=g.Key,calls=g.Count(),inclusiveMs=g.Sum(i=>times[i]),selfMs=g.Sum(i=>Math.Max(0,times[i]-children[i])),gcBytes=g.Sum(i=>bytes[i])}).ToArray();
            var allocationParents=new Dictionary<string,long>();
            for(int i=0;i<count;i++)if(bytes[i]>0){string key=parent[i]>=0?names[parent[i]]:"<root>";allocationParents.TryGetValue(key,out long old);allocationParents[key]=old+bytes[i];}
            output.Add(new{frame,raw.threadName,raw.sampleCount,raw.frameTimeMs,elapsedMs=watch.ElapsedMilliseconds,targets=groups.Where(g=>g.name.StartsWith("CG.")).ToArray(),topSelf=groups.OrderByDescending(g=>g.selfMs).Take(25).ToArray(),allocationParents=allocationParents.OrderByDescending(p=>p.Value).Take(10).Select(p=>new{name=p.Key,bytes=p.Value}).ToArray()});
        }
        File.WriteAllText(Root+"diagnostic_frames.json",JsonConvert.SerializeObject(new{frameLimit=2,sampleLimitPerFrame=20000,extractionScanTimeLimitMs=1000,selection="top CG.Mask.Present observed counters; recording ON diagnostic only",output},Formatting.Indented));
    }

    static void CaptureBufferState()
    {
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        var field=typeof(CoreGuardianSelectionOutline).GetField("sourceFrame",flags);var rows=new List<object>();
        foreach(var owner in Resources.FindObjectsOfTypeAll<CoreGuardianSelectionOutline>())
        {
            if(owner==null||!owner.gameObject.scene.IsValid())continue;
            var frame=field?.GetValue(owner);if(frame==null)continue;
            var values=frame.GetType().GetField("Values",flags).GetValue(frame);var collection=(System.Collections.ICollection)values;
            int batchCount=0,batchCapacity=0;var batches=(System.Collections.IEnumerable)typeof(CoreGuardianSelectionOutline).GetField("batches",flags).GetValue(owner);
            foreach(var batch in batches){var parts=batch.GetType().GetField("parts",flags).GetValue(batch);batchCount+=((System.Collections.ICollection)parts).Count;batchCapacity+=(int)parts.GetType().GetProperty("Capacity").GetValue(parts);}
            rows.Add(new{owner=owner.name,active=owner.gameObject.activeInHierarchy,count=collection.Count,capacity=(int)values.GetType().GetProperty("Capacity").GetValue(values),batchCount,batchCapacity});
        }
        object stride=null;string strideError=null;
        try{var sampleType=typeof(CoreGuardianSelectionOutline).Assembly.GetType("CoreGuardianMaskSourceSample");var method=typeof(Unity.Collections.LowLevel.Unsafe.UnsafeUtility).GetMethod("SizeOf",BindingFlags.Public|BindingFlags.Static,null,new[]{typeof(Type)},null);if(method!=null&&sampleType!=null)stride=method.Invoke(null,new object[]{sampleType});else strideError="Type overload unavailable";}
        catch(Exception e){strideError=e.GetBaseException().Message;}
        File.WriteAllText(Root+label+"_buffers.json",JsonConvert.SerializeObject(new{stride,strideError,owners=rows,scope="logical live counts/capacities and sample size only; excludes object/list headers, other cache buffers and full heap/GPU peak"},Formatting.Indented));
    }
    static void Finish(string failure)
    {
        if(!running)return;running=false;error=failure;EditorApplication.update-=Poll;Application.logMessageReceived-=Log;
        CoreGuardianAttackBodyBatch.SetEnabled(false);foreach(var recorder in counters.Values)recorder.Dispose();counters.Clear();
        Time.captureDeltaTime=oldCapture;Time.timeScale=oldScale;Profiler.enabled=false;
        File.WriteAllText(Root+label+"_benchmark_completion.json",JsonConvert.SerializeObject(new{error,runs,stopRequested=EditorApplication.isPlaying},Formatting.Indented));
        if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;else RestoreEditor();
    }
    static void PlayState(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode&&running)Finish("Play stopped externally");if(state==PlayModeStateChange.EnteredEditMode)RestoreEditor();}
    static void RestoreEditor()
    {
        EditorApplication.playModeStateChanged-=PlayState;
        if(gameView!=null){sizeIndex.SetValue(gameView,savedSizeIndex);File.WriteAllText(Root+label+"_game_view_restored.json",JsonConvert.SerializeObject(new{savedSizeIndex,actualSizeIndex=(int)sizeIndex.GetValue(gameView)}));}
        Application.runInBackground=oldBackground;Time.captureDeltaTime=oldCapture;Time.timeScale=oldScale;
        setDriver.Invoke(null,new object[]{oldDriver});Profiler.enabled=false;
        File.WriteAllText(Root+label+"_benchmark_restored.json",JsonConvert.SerializeObject(new{playing=EditorApplication.isPlaying,compiling=EditorApplication.isCompiling,deep=ProfilerDriver.deepProfiling,recording=Profiler.enabled,driver=ProfilerDriver.enabled,Application.runInBackground,Time.captureDeltaTime,Time.timeScale,candidate=CoreGuardianAttackBodyBatch.Enabled,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,dirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty},Formatting.Indented));
        FinishTraceAndRestoreBuffer();
    }
}
