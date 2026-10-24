return CGB6.CGDraw.Run("srp_off_1",true,false);
}
}
namespace CGB6 {
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
public static class CGDraw
{
    const string Root="docs/development/drawcall_20261024/";
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
    static bool shared,sharedBefore;static Type sharedType;
    static void SetShared(bool on){sharedType??=typeof(CoreGuardianObjectEmphasis).Assembly.GetType("CoreGuardianSharedOutline");sharedType.GetMethod("SetEnabled",BindingFlags.Static|BindingFlags.Public).Invoke(null,new object[]{on});}
    static bool srp,srpBefore;static UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset urp;
    public static object Run(string label,bool useShared,bool useSrp){srp=useSrp;urp=(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;srpBefore=urp.useSRPBatcher;shared=useShared;sharedType??=typeof(CoreGuardianObjectEmphasis).Assembly.GetType("CoreGuardianSharedOutline");sharedBefore=(bool)sharedType.GetProperty("Enabled").GetValue(null);return Start(label);}
    static object Start(string runLabel)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||Profiler.enabled||ProfilerDriver.deepProfiling||UnityEngine.FrameDebugger.enabled
            ||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Stopped idle clean Editor required");
        label=runLabel;Directory.CreateDirectory(Root);
        if(true)
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
                var names=new[]{"CG.SharedOutline.Mask","CG.SharedOutline.Output","CG.SharedOutline.Collect","CG.SharedOutline.Sort","CG.SharedOutline.Record","CG.SharedOutline.Execute","Gfx.WaitForPresentOnGfxThread","Gfx.WaitForGfxCommandsFromMainThread","Gfx.WaitForRenderThread","RenderPipelineManager.DoRenderLoop_Internal()","Inl_UniversalRenderPipeline.RenderSingleCameraInternal","Update.ScriptRunBehaviourUpdate","PreLateUpdate.ScriptRunBehaviourLateUpdate","PostLateUpdate.FinishFrameRendering","Canvas.SendWillRenderCanvases","UGUI.Rendering.UpdateBatches","Canvas.BuildBatch","Physics2D.Simulate","FixedUpdate.PhysicsFixedUpdate","CPU Main Thread Frame Time","CPU Render Thread Frame Time","CPU Total Frame Time","GPU Frame Time","SetPass Calls Count","Draw Calls Count","Batches Count","Assembly-CSharp.dll!::CoreGuardianPrototype.LateUpdate() [Invoke]","CG.Mask.CacheFlush","CG.Mask.CopyBack","CG.Mask.Regenerate","CG.Mask.EligibleRegenerate","CG.Mask.SourceFilter","CG.Mask.BoundsCrop","CG.Mask.QuadUpload","CG.Mask.Resources","CG.Mask.CacheLookup","CG.Mask.Output","CG.Mask.Parts","CG.Mask.MeshUpload","CG.Mask.Execute","CG.Mask.PageProducer","CG.SpriteReferences.Read","Main Thread","PlayerLoop","EditorLoop","WaitForTargetFPS","WaitForLastPresentation","Semaphore.WaitForSignal","WaitForJobGroupID","Camera.Render","GC.Collect","GarbageCollector.CollectIncremental","Shader.EditorCompileVariant","Shader.DynamicLoadGPUProgram","Shader.CreateGPUPrograms","Shader.CompileGPUProgram","Texture.WaitForAsyncUpload","CG.Emphasis.Present","CG.Emphasis.Tint","CG.Mask.Present","CG.PhotoShadow.Present","Standard Draw Calls Count","SRP Batcher Draw Calls Count","GC Allocated In Frame","Assembly-CSharp.dll!::CoreGuardianPrototype.Update() [Invoke]","Assembly-CSharp.dll!::CoreGuardianHud.Update() [Invoke]","Assembly-CSharp.dll!::CoreGuardianCombatAudio.Update() [Invoke]","Assembly-CSharp.dll!::CoreGuardianCombatArt.Update() [Invoke]"};
                var selected=available.Select(ProfilerRecorderHandle.GetDescription).Where(d=>names.Contains(d.Name)).GroupBy(d=>d.Name).Select(g=>g.First()).ToArray();
                foreach(var d in selected)counters[d.Name]=ProfilerRecorder.StartNew(d.Category,d.Name,1,ProfilerRecorderOptions.StartImmediately|ProfilerRecorderOptions.SumAllSamplesInFrame|ProfilerRecorderOptions.WrapAroundWhenCapacityReached|ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
                File.WriteAllText(Root+label+"_benchmark_environment.json",JsonConvert.SerializeObject(new{unity=Application.unityVersion,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,ramMB=SystemInfo.systemMemorySize,Screen.width,Screen.height,QualitySettings.vSyncCount,Application.targetFrameRate,camera=Camera.main.orthographicSize,Time.captureDeltaTime,policy="fixed simulation dt1/60; wall frame intervals from realtimeSinceStartup; standard/SRP Rendering counters; 15 warm + 90 observed each; each run warm15/sample90; 3 runs per condition; current-thread marker sums; recording OFF",counters=selected.Select(d=>new{d.Name,category=d.Category.Name,unit=d.UnitType.ToString()})},Formatting.Indented));
                run=0;SetShared(shared);urp.useSRPBatcher=srp;Setup();return;
            }
            if(last==Time.frameCount)return;int gap=Time.frameCount-last;last=Time.frameCount;
            double now=Time.realtimeSinceStartupAsDouble,ms=(now-previous)*1000;previous=now;
            if(gap!=1)throw new Exception("Nonunit engine frame gap: "+gap);
            if(warm>0){warm--;previousCollection=GC.CollectionCount(0);return;}
            if(game.Session.Enemies.Count!=128||game.Session.Towers.Count!=15)throw new Exception("Load changed during sample");
            frames.Add(new{sharedOn=shared,srpOn=UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching,frame=last,gap,ms,collectionCount0=GC.CollectionCount(0),collectionsSincePrevious=GC.CollectionCount(0)-previousCollection,enemies=game.Session.Enemies.Count,towers=game.Session.Towers.Count,projectiles=game.Session.Projectiles.Count,sources=CoreGuardianAttackBodyBatch.LastSources,textureBytes=CoreGuardianAttackBodyBatch.TextureBytes,bufferBytes=CoreGuardianAttackBodyBatch.BufferBytes,draw=UnityStats.drawCalls,setpass=UnityStats.setPassCalls,counters=counters.ToDictionary(p=>p.Key,p=>new{valid=p.Value.Valid&&p.Value.Count>0,value=p.Value.Valid&&p.Value.Count>0?(long?)p.Value.LastValue:null})});
            previousCollection=GC.CollectionCount(0);
            if(frames.Count<90)return;
            CaptureBufferState();
            File.WriteAllText(Root+label+".json",JsonConvert.SerializeObject(new{label,frames},Formatting.Indented));runs.Add(new{label,samples=frames.Count});
            Finish(null);
        }
        catch(Exception e){Finish(e.ToString());}
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
        if(!running)return;running=false;error=failure;SetShared(sharedBefore);urp.useSRPBatcher=srpBefore;EditorApplication.update-=Poll;Application.logMessageReceived-=Log;
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
    }
}

}
public static class CGTailPad6{static object P(){
