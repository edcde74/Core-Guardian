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

public static class CGNormalObservation
{
    const string Root="docs/development/normal_probe_20261023/";
    static CoreGuardianPrototype game;
    static bool running;
    static int last,prepared,warm=30;
    static double started,nextMemory;
    static readonly List<object> frames=new List<object>(),memory=new List<object>(),actions=new List<object>(),errors=new List<object>();
    static readonly Dictionary<string,ProfilerRecorder> counters=new Dictionary<string,ProfilerRecorder>();
    public static object Start()
    {
        if(running||!EditorApplication.isPlaying||EditorApplication.isPaused||EditorApplication.isCompiling||Profiler.enabled)throw new Exception("Idle own Play required");
        game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
        if(game==null||!game.Ready||game.Session.Diagnostic||game.Session.Wave!=1||game.Session.Towers.Count!=0||game.Session.Phase!=SessionPhase.Preparation)throw new Exception("Fresh normal session required");
        if(Time.timeScale!=1||Time.captureDeltaTime!=0)throw new Exception("Normal 1x time required");
        var available=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(available);
        var names=new[]{"PlayerLoop","Main Thread","Render Thread","GPU Frame Time","GC Allocated In Frame","Draw Calls Count","SetPass Calls Count"};
        var selected=available.Select(ProfilerRecorderHandle.GetDescription).Where(d=>names.Contains(d.Name)||d.Name.StartsWith("CG.")).GroupBy(d=>d.Name).Select(g=>g.First()).ToArray();
        foreach(var d in selected)counters[d.Name]=ProfilerRecorder.StartNew(d.Category,d.Name,8192,ProfilerRecorderOptions.StartImmediately|ProfilerRecorderOptions.SumAllSamplesInFrame);
        Write("environment.json",new{utc=DateTime.UtcNow,unity=Application.unityVersion,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,ramMB=SystemInfo.systemMemorySize,api=SystemInfo.graphicsDeviceType.ToString(),SystemInfo.supportsGpuRecorder,Screen.width,Screen.height,QualitySettings.vSyncCount,Application.targetFrameRate,Time.timeScale,Time.captureDeltaTime,background=Application.runInBackground,profiler=Profiler.enabled,driver=UnityEditorInternal.ProfilerDriver.enabled,deep=UnityEditorInternal.ProfilerDriver.deepProfiling,camera=Camera.main.orthographicSize,policy="120s wall or 3 completed rounds or terminal; 1x normal Update/Tick; legal movement to (13,14); Multi at (12,13); spend earned gold on player damage; start preparation immediately; no injected enemies/currency/dt or resets; stationary afterwards; normal augmentation timeout",warmupFrames=30,repetitions=1,reason="Small observational current-state sample, no before/after claim",counters=selected.Select(d=>new{d.Name,unit=d.UnitType.ToString()})});
        started=EditorApplication.timeSinceStartup;nextMemory=started;last=Time.frameCount;running=true;
        Application.logMessageReceived+=OnLog;EditorApplication.update+=Poll;return Status();
    }
    static void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(new{message,stack,type=type.ToString()});}
    public static object ResourcesNow()
    {
        var rt=Resources.FindObjectsOfTypeAll<RenderTexture>().Where(t=>t.name=="Selected unit union mask"||t.name.IndexOf("mask",StringComparison.OrdinalIgnoreCase)>=0).ToArray();
        var meshes=Resources.FindObjectsOfTypeAll<Mesh>().Where(m=>m.name.StartsWith("Selection mask ")||m.name=="Selected unit outline quad").ToArray();
        var mats=Resources.FindObjectsOfTypeAll<Material>().Where(m=>m.shader!=null&&(m.shader.name=="CoreGuardian/SelectionMask"||m.shader.name=="CoreGuardian/SelectionOutline")).ToArray();
        return new{frame=Time.frameCount,elapsed=EditorApplication.timeSinceStartup-started,totalAllocated=Profiler.GetTotalAllocatedMemoryLong(),totalReserved=Profiler.GetTotalReservedMemoryLong(),monoUsed=Profiler.GetMonoUsedSizeLong(),renderTextures=rt.Select(t=>new{t.name,t.width,t.height,bytes=Profiler.GetRuntimeMemorySizeLong(t)}).ToArray(),meshes=meshes.Length,meshBytes=meshes.Sum(m=>Profiler.GetRuntimeMemorySizeLong(m)),materials=mats.Length,materialBytes=mats.Sum(m=>Profiler.GetRuntimeMemorySizeLong(m))};
    }
    static void Poll()
    {
        try{
            if(!EditorApplication.isPlaying||game==null){Finish("Play ended externally");return;}
            if(EditorApplication.isPaused){Finish("External pause");return;}
            if(last==Time.frameCount)return;int gap=Time.frameCount-last;last=Time.frameCount;
            var s=game.Session;double now=EditorApplication.timeSinceStartup;
            bool scan=now>=nextMemory;
            if(warm>0)warm--;else frames.Add(new{frame=last,gap,ms=Time.unscaledDeltaTime*1000d,wave=s.Wave,completed=s.CompletedWave,phase=s.Phase.ToString(),alive=s.Enemies.Count(e=>e.Alive),towers=s.Towers.Count,projectiles=s.Projectiles.Count,memoryScanThisPoll=scan});
            if(scan){memory.Add(ResourcesNow());nextMemory=now+5;}
            if(now-started>=120||s.CompletedWave>=3||s.Phase==SessionPhase.Defeat||s.Phase==SessionPhase.Victory){Finish("Bound reached: "+s.Phase);return;}
            if(s.Phase!=SessionPhase.Preparation||prepared==s.Wave)return;
            if(s.Wave==1&&game.player.CurrentCell.y<14){game.player.TryStep(Vector2Int.up);return;}
            if(s.Wave==1){bool ok=game.Place(TowerKind.Multi,new Vector2Int(12,13));actions.Add(new{wave=s.Wave,action="place Multi (12,13)",ok});if(!ok)throw new Exception("Legal placement failed");}
            while(s.Gold>=s.PlayerUpgradePrice("damage")){string reason;if(!s.UpgradePlayer("damage",out reason))throw new Exception("Upgrade failed: "+reason);actions.Add(new{wave=s.Wave,action="earned-gold player damage upgrade"});}
            prepared=s.Wave;game.StartCombat();actions.Add(new{wave=s.Wave,action="normal StartCombat",phase=s.Phase.ToString()});
        }catch(Exception e){errors.Add(e.ToString());Finish("Observer error");}
    }
    static void Finish(string reason)
    {
        if(!running)return;running=false;EditorApplication.update-=Poll;Application.logMessageReceived-=OnLog;
        memory.Add(ResourcesNow());
        var markerData=counters.Select(p=>new{name=p.Key,valid=p.Value.Valid,count=p.Value.Count,wrapped=p.Value.WrappedAround,samples=p.Value.ToArray().Select(s=>new{s.Value,s.Count}).ToArray()}).ToArray();
        foreach(var r in counters.Values)r.Dispose();counters.Clear();
        Write("result.json",new{reason,elapsed=EditorApplication.timeSinceStartup-started,engineFrame=Time.frameCount,wave=game.Session.Wave,completed=game.Session.CompletedWave,phase=game.Session.Phase.ToString(),frames,memory,actions,errors,markerData});
    }
    public static object Status()=>new{running,frames=frames.Count,wave=game==null?0:game.Session.Wave,completed=game==null?0:game.Session.CompletedWave,elapsed=EditorApplication.timeSinceStartup-started};
    static void Write(string file,object value){File.WriteAllText(Root+file,JsonConvert.SerializeObject(value,Formatting.Indented));}
}
