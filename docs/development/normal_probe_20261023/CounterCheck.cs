// Diagnostic only: compiled in memory, never imported into Assets.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Profiling;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using CoreGuardian;
using Newtonsoft.Json;

public static class CGNormalCounterCheck
{
    const string Root="docs/development/normal_probe_20261023/counter_check/";
    static CoreGuardianPrototype game;
    static bool running;
    static int last,prepared,warm=30;
    static double started,nextMemory,nextWrite;
    static int rendered,lastRendered=-1;
    static readonly List<object> frames=new List<object>(),memory=new List<object>(),actions=new List<object>(),errors=new List<object>();
    static readonly Dictionary<string,ProfilerRecorder> counters=new Dictionary<string,ProfilerRecorder>();
    public static object Start()
    {
        if(running||!EditorApplication.isPlaying||EditorApplication.isPaused||EditorApplication.isCompiling||Profiler.enabled)throw new Exception("Idle own Play required");
        game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
        if(game==null||!game.Ready||game.Session.Diagnostic||game.Session.Wave!=1||game.Session.Towers.Count!=0||game.Session.Phase!=SessionPhase.Preparation)throw new Exception("Fresh normal session required");
        if(Time.timeScale!=1||Time.captureDeltaTime!=0)throw new Exception("Normal 1x time required");
        var available=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(available);
        // Failed attempt crashed inside native ToArray/CopyTo. Never copy recorder buffers here.
        // GPU counter discovery/support flags did not establish valid GPU data; omit that counter.
        var names=new[]{"PlayerLoop","Main Thread","GC Allocated In Frame","Draw Calls Count","SetPass Calls Count","CG.Mask.Present"};
        var selected=available.Select(ProfilerRecorderHandle.GetDescription).Where(d=>names.Contains(d.Name)).GroupBy(d=>d.Name).Select(g=>g.First()).ToArray();
        foreach(var d in selected)counters[d.Name]=ProfilerRecorder.StartNew(d.Category,d.Name,1,ProfilerRecorderOptions.StartImmediately|ProfilerRecorderOptions.SumAllSamplesInFrame|ProfilerRecorderOptions.WrapAroundWhenCapacityReached);
        Write("environment.json",new{utc=DateTime.UtcNow,unity=Application.unityVersion,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,ramMB=SystemInfo.systemMemorySize,api=SystemInfo.graphicsDeviceType.ToString(),SystemInfo.supportsGpuRecorder,Screen.width,Screen.height,QualitySettings.vSyncCount,Application.targetFrameRate,Time.timeScale,Time.captureDeltaTime,background=Application.runInBackground,profiler=Profiler.enabled,driver=UnityEditorInternal.ProfilerDriver.enabled,deep=UnityEditorInternal.ProfilerDriver.deepProfiling,camera=Camera.main.orthographicSize,policy="15s wall or 3 completed rounds or terminal; 1x normal Update/Tick; legal movement to (13,14); Multi at (12,13); spend earned gold on player damage; start preparation immediately; no injected enemies/currency/dt or resets; stationary afterwards; normal augmentation timeout",warmupFrames=30,repetitions=1,reason="Small observational current-state sample, no before/after claim",counters=selected.Select(d=>new{d.Name,unit=d.UnitType.ToString()})});
        started=EditorApplication.timeSinceStartup;nextMemory=started;nextWrite=started+10;last=Time.frameCount;running=true;
        Application.logMessageReceived+=OnLog;EditorApplication.update+=Poll;UnityEngine.Rendering.RenderPipelineManager.endContextRendering+=Rendered;return Status();
    }
    static void Rendered(UnityEngine.Rendering.ScriptableRenderContext context,List<Camera> cameras){if(lastRendered!=Time.frameCount&&cameras.Any(c=>c.cameraType==CameraType.Game)){lastRendered=Time.frameCount;rendered++;}}
    static void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(new{message,stack,type=type.ToString()});}
    public static object ResourcesNow()
    {
        var rt=Resources.FindObjectsOfTypeAll<RenderTexture>().Where(t=>t.name=="Selected unit union mask"||t.name.IndexOf("mask",StringComparison.OrdinalIgnoreCase)>=0).ToArray();
        var meshes=Resources.FindObjectsOfTypeAll<Mesh>().Where(m=>m.name.StartsWith("Selection mask ")||m.name=="Selected unit outline quad"||m.name=="Page mask geometry").ToArray();
        var mats=Resources.FindObjectsOfTypeAll<Material>().Where(m=>m.name=="Shared mask page producer"||(m.shader!=null&&(m.shader.name=="CoreGuardian/SelectionMask"||m.shader.name=="CoreGuardian/SelectionOutline"))).ToArray();
        return new{frame=Time.frameCount,elapsed=EditorApplication.timeSinceStartup-started,processWorkingSet=System.Diagnostics.Process.GetCurrentProcess().WorkingSet64,totalAllocated=Profiler.GetTotalAllocatedMemoryLong(),totalReserved=Profiler.GetTotalReservedMemoryLong(),monoUsed=Profiler.GetMonoUsedSizeLong(),renderTextures=rt.Select(t=>new{t.name,t.width,t.height,bytes=Profiler.GetRuntimeMemorySizeLong(t)}).ToArray(),meshes=meshes.Length,meshBytes=meshes.Sum(m=>Profiler.GetRuntimeMemorySizeLong(m)),materials=mats.Length,materialBytes=mats.Sum(m=>Profiler.GetRuntimeMemorySizeLong(m))};
    }
    static void Poll()
    {
        try{
            if(!EditorApplication.isPlaying||game==null){Finish("Play ended externally");return;}
            if(EditorApplication.isPaused){Finish("External pause");return;}
            if(last==Time.frameCount)return;int gap=Time.frameCount-last;last=Time.frameCount;
            var s=game.Session;double now=EditorApplication.timeSinceStartup;
            bool scan=now>=nextMemory;
            if(warm>0)warm--;else frames.Add(new{frame=last,gap,ms=Time.unscaledDeltaTime*1000d,wave=s.Wave,completed=s.CompletedWave,phase=s.Phase.ToString(),alive=s.Enemies.Count(e=>e.Alive),towers=s.Towers.Count,projectiles=s.Projectiles.Count,memoryScanThisPoll=scan,renderedFrames=rendered,counters=counters.ToDictionary(p=>p.Key,p=>new{valid=p.Value.Valid&&p.Value.Count>0,value=p.Value.Valid&&p.Value.Count>0?(long?)p.Value.LastValue:null})});
            if(scan){memory.Add(ResourcesNow());nextMemory=now+5;}
            if(now>=nextWrite){Save("running checkpoint");nextWrite=now+10;}
            if(now-started>=15||s.CompletedWave>=3||s.Phase==SessionPhase.Defeat||s.Phase==SessionPhase.Victory){Finish("Bound reached: "+s.Phase);return;}
            if(s.Phase!=SessionPhase.Preparation||prepared==s.Wave)return;
            if(s.Wave==1&&game.player.CurrentCell.y<14){game.player.TryStep(Vector2Int.up);return;}
            if(s.Wave==1){bool ok=game.Place(TowerKind.Multi,new Vector2Int(12,13));actions.Add(new{wave=s.Wave,action="place Multi (12,13)",ok});if(!ok)throw new Exception("Legal placement failed");}
            while(s.Gold>=s.PlayerUpgradePrice("damage")){string reason;if(!s.UpgradePlayer("damage",out reason))throw new Exception("Upgrade failed: "+reason);actions.Add(new{wave=s.Wave,action="earned-gold player damage upgrade"});}
            prepared=s.Wave;game.StartCombat();actions.Add(new{wave=s.Wave,action="normal StartCombat",phase=s.Phase.ToString()});
        }catch(Exception e){errors.Add(e.ToString());Finish("Observer error");}
    }
    static void Finish(string reason)
    {
        if(!running)return;running=false;EditorApplication.update-=Poll;Application.logMessageReceived-=OnLog;UnityEngine.Rendering.RenderPipelineManager.endContextRendering-=Rendered;
        memory.Add(ResourcesNow());
        foreach(var r in counters.Values)r.Dispose();counters.Clear();
        Save(reason);
    }
    static void Save(string reason)=>Write("result.json",new{running,reason,elapsed=EditorApplication.timeSinceStartup-started,engineFrame=Time.frameCount,wave=game.Session.Wave,completed=game.Session.CompletedWave,phase=game.Session.Phase.ToString(),renderedFrames=rendered,frames,memory,actions,errors});
    public static object Status()=>new{running,frames=frames.Count,wave=game==null?0:game.Session.Wave,completed=game==null?0:game.Session.CompletedWave,elapsed=EditorApplication.timeSinceStartup-started};
    static void Write(string file,object value){File.WriteAllText(Root+file,JsonConvert.SerializeObject(value,Formatting.Indented));}
}
