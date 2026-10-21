// Diagnostic only. Compiled in memory through Unity run_script; never imported into Assets.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Profiling;
using CoreGuardian;
using Newtonsoft.Json;

public static class CGBodyMaskProbe
{
    static string root="docs/development/mask_reuse_20261020/after";
    static bool running,oldEnabled,oldEditor,oldDeep,oldStacks,oldBackground;
    static readonly Dictionary<ProfilerArea,bool> areas=new Dictionary<ProfilerArea,bool>();
    static CoreGuardianPrototype game;
    static int run,warm,lastFrame,observed,targetFrames;
    static string mode;
    static double deadline;
    static List<object> frames=new List<object>();
    static List<object> progress=new List<object>();
    static UnityEditor.EditorApplication.CallbackFunction poll;
    static int[] populations;
    static int replacements;
    static readonly Dictionary<CoreGuardianSelectionOutline,RenderTexture> knownMasks=new Dictionary<CoreGuardianSelectionOutline,RenderTexture>();
    static readonly FieldInfo maskField=typeof(CoreGuardianSelectionOutline).GetField("mask",BindingFlags.Instance|BindingFlags.NonPublic);
    static void MaskSample(bool count){foreach(var c in Resources.FindObjectsOfTypeAll<CoreGuardianSelectionOutline>()){var current=(RenderTexture)maskField.GetValue(c);if(current==null)continue;if(knownMasks.TryGetValue(c,out var previous)&&!ReferenceEquals(previous,current)&&count)replacements++;knownMasks[c]=current;}}
    static CoreGuardianSelectionOutline[] owners;
    static void SampleKnownMasks(){foreach(var c in owners){if(c==null)continue;var current=(RenderTexture)maskField.GetValue(c);if(!ReferenceEquals(knownMasks[c],current))replacements++;knownMasks[c]=current;}}
    sealed class Cost {public double totalMs,selfMs; public long calls,gcBytes;}
    public static object Start(string requestedMode,string side)
    {
        if(running)throw new Exception("Probe already running");
        if(!EditorApplication.isPlaying)throw new Exception("Own Play required");
        if(Profiler.enabled)throw new Exception("Another Profiler recording is active");
        root="docs/development/shared_body_mask_20261021/"+side;mode=requestedMode;if(mode=="instrumented")root="docs/development/profiler_investigation_20261020/captures_instrumented";
        game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
        if(game==null||!game.Ready)throw new Exception("Game not ready");
        oldEnabled=Profiler.enabled;oldEditor=ProfilerDriver.profileEditor;oldDeep=ProfilerDriver.deepProfiling;
        oldStacks=Profiler.enableAllocationCallstacks;oldBackground=Application.runInBackground;
        areas.Clear();foreach(ProfilerArea a in Enum.GetValues(typeof(ProfilerArea)))areas[a]=ProfilerDriver.IsAreaEnabled(a);
        Directory.CreateDirectory(root);
        if(mode=="normal"){populations=new[]{128,128,128};targetFrames=100;}
        else {populations=new[]{128};targetFrames=mode=="instrumented"?20:mode=="deep"?30:60;}
        Write(mode+"_environment.json",new{utc=DateTime.UtcNow,unity=Application.unityVersion,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,ramMB=SystemInfo.systemMemorySize,os=SystemInfo.operatingSystem,screen=new{Screen.width,Screen.height},vsync=QualitySettings.vSyncCount,targetFrameRate=Application.targetFrameRate,Time.timeScale,Time.captureDeltaTime,storedVolume=PlayerPrefs.GetFloat("CoreGuardian.SoundVolume",-1),audioListener=AudioListener.volume,original=new{oldEnabled,oldEditor,oldDeep,oldStacks,oldBackground},areas=areas.ToDictionary(p=>p.Key.ToString(),p=>p.Value),mode,targetFrames,warmup=30,populations});
        Application.runInBackground=true;ProfilerDriver.profileEditor=false;ProfilerDriver.deepProfiling=mode=="deep"||mode=="instrumented";
        Profiler.enableAllocationCallstacks=mode=="alloc";
        foreach(var a in new[]{ProfilerArea.CPU,ProfilerArea.Rendering,ProfilerArea.Memory,ProfilerArea.UI,ProfilerArea.Audio})ProfilerDriver.SetAreaEnabled(a,true);
        run=0;running=true;poll=Poll;EditorApplication.update+=poll;
        Setup();return Status();
    }
    static void Setup()
    {
        Profiler.enabled=false;game.Diagnostic();var s=game.Session;
        for(int i=0;i<populations[run];i++){
            var kind=i%8==0?EnemyKind.Buff:EnemyKind.Attack;var b=s.Data.Enemies[kind];
            s.DebugSpawn(kind,20+(i%12)*.3f,new EnemySpec{Kind=kind,Health=100000,Speed=b.Speed,Defense=b.Defense,Gold=0,CoreDamage=0,BuffRadius=b.BuffRadius,BuffSpeed=b.BuffSpeed});
        }
        var point=s.Enemies[0].Position;var size=game.BoardSize;
        var cells=Enumerable.Range(0,size.x).SelectMany(x=>Enumerable.Range(0,size.y).Select(y=>new Vector2Int(x,y))).Where(s.IsBuildable).OrderBy(p=>((Vector2)p-point).sqrMagnitude).ToArray();
        for(int i=0;i<Math.Min(32,cells.Length);i++)game.Place(i%5==4?TowerKind.Support:TowerKind.Multi,cells[i]);
        game.StartCombat();typeof(Session).GetField("waveTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(s,.001f);replacements=0;knownMasks.Clear();warm=30;observed=0;lastFrame=Time.frameCount;frames.Clear();
        deadline=EditorApplication.timeSinceStartup+65;
    }
    static void Poll()
    {
        try{
            if(!EditorApplication.isPlaying){Finish("Play ended externally");return;}
            if(EditorApplication.timeSinceStartup>deadline){Finish("Capture deadline exceeded");return;}
            if(lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
            if(warm>0){warm--;if(warm==0){MaskSample(false);owners=knownMasks.Keys.ToArray();ProfilerDriver.ClearAllFrames();typeof(ProfilerDriver).GetMethod("SetProfilingEnabled",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});Profiler.enabled=true;}return;}
            SampleKnownMasks();frames.Add(new{engineFrame=Time.frameCount,profilerFrame=ProfilerDriver.lastFrameIndex,deltaMs=Time.unscaledDeltaTime*1000,enemies=game.Session.Enemies.Count(e=>e.Alive),towers=game.Session.Towers.Count,projectiles=game.Session.Projectiles.Count,damageSources=game.Session.DamageTotals.Count,drawCalls=UnityStats.drawCalls,setPass=UnityStats.setPassCalls,monoUsed=Profiler.GetMonoUsedSizeLong(),totalAllocated=Profiler.GetTotalAllocatedMemoryLong()});
            if(++observed<targetFrames)return;
            Profiler.enabled=false;
            var label=mode+"_"+run+"_n"+populations[run];
            if(ProfilerDriver.firstFrameIndex<0||ProfilerDriver.lastFrameIndex<0)throw new Exception("No actual Profiler frames received; capture invalid");
            bool saved=ProfilerDriver.SaveProfile(Path.GetFullPath(root+"/"+label+".data"));
            Write(label+"_frames.json",frames);
            var aliases=knownMasks.Values.Where(r=>r!=null).ToArray();var rt=aliases.Distinct().ToArray();Write(label+"_resources.json",new{replacements,owners=owners.Length,boundAliases=aliases.Length,cache=CacheTelemetry(),maskCount=rt.Length,bytes=rt.Sum(r=>Profiler.GetRuntimeMemorySizeLong(r)),sizes=rt.Select(r=>new{r.width,r.height}).ToArray(),materials=Materials()});
            Export(label);
            progress.Add(new{label,saved,frames=frames.Count,first=ProfilerDriver.firstFrameIndex,last=ProfilerDriver.lastFrameIndex});Write(mode+"_progress.json",progress);
            run++;if(run>=populations.Length){Finish(null);return;}Setup();
        }catch(Exception ex){Finish(ex.ToString());}
    }
    public static object ExportSaved(string directory,string label)
    {
        if(EditorApplication.isPlaying||Profiler.enabled)throw new Exception("Idle own capture analysis required");
        root=directory;mode="lifecycle";ProfilerDriver.LoadProfile(Path.GetFullPath(root+"/"+label+".data"),false);
        return new{first=ProfilerDriver.firstFrameIndex,last=ProfilerDriver.lastFrameIndex};
    }
    public static object ExportLoaded(string directory,string label){root=directory;mode="lifecycle";if(ProfilerDriver.firstFrameIndex<0)throw new Exception("Profile not loaded");Export(label);return new{first=ProfilerDriver.firstFrameIndex,last=ProfilerDriver.lastFrameIndex};}
    static void Export(string label)
    {
        var output=new List<object>();var stacks=new Dictionary<string,long>();int stackSamples=0;
        for(int frame=ProfilerDriver.firstFrameIndex;frame<=ProfilerDriver.lastFrameIndex;frame++){
            using(var it=new ProfilerFrameDataIterator()){
                int count=it.GetThreadCount(frame);
                for(int thread=0;thread<count;thread++)using(var raw=ProfilerDriver.GetRawFrameDataView(frame,thread)){
                    if(!raw.valid||raw.sampleCount==0)continue;
                    if(raw.threadName!="Main Thread"&&!raw.threadName.Contains("Render")&&!raw.threadName.Contains("GfxDevice"))continue;
                    long cacheInclusiveGC=0;var costs=new Dictionary<string,Cost>();var parent=new Stack<int>();var ends=new Stack<int>();
                    for(int i=0;i<raw.sampleCount;i++){
                        while(ends.Count>0&&i>=ends.Peek()){ends.Pop();parent.Pop();}
                        string name=raw.GetSampleName(i)??"<unnamed-root>";double total=raw.GetSampleTimeMs(i);int end=i+raw.GetSampleChildrenCountRecursive(i)+1;
                        double childMs=0;for(int c=i+1;c<end;c+=raw.GetSampleChildrenCountRecursive(c)+1)childMs+=raw.GetSampleTimeMs(c);
                        Cost cost;if(!costs.TryGetValue(name,out cost))costs[name]=cost=new Cost();cost.calls++;cost.totalMs+=total;cost.selfMs+=Math.Max(0,total-childMs);
                        if(name=="GC.Alloc"&&raw.GetSampleMetadataCount(i)>0){
                            long bytes=raw.GetSampleMetadataAsLong(i,0);cost.gcBytes+=bytes;if(parent.Any(index=>raw.GetSampleName(index)=="CG.Mask.CacheLookup"))cacheInclusiveGC+=bytes;
                            if(parent.Count>0){var owner=raw.GetSampleName(parent.Peek())??"<unnamed-root>";if(!costs.ContainsKey(owner))costs[owner]=new Cost();costs[owner].gcBytes+=bytes;}
                            if(mode=="alloc"&&stackSamples<3000){
                                var addresses=new List<ulong>();raw.GetSampleCallstack(i,addresses);
                                string key=string.Join(" <- ",addresses.Select(a=>{var m=raw.ResolveMethodInfo(a);return m.methodName+" @ "+m.sourceFileName+":"+m.sourceFileLine;}).ToArray());
                                if(string.IsNullOrEmpty(key))key="<unresolved>";
                                if(!stacks.ContainsKey(key))stacks[key]=0;stacks[key]+=bytes;stackSamples++;
                            }
                        }
                        if(end>i+1){parent.Push(i);ends.Push(end);}
                    }
                    output.Add(new{frame,thread,raw.threadName,raw.threadGroupName,raw.frameTimeMs,raw.frameGpuTimeMs,raw.sampleCount,cacheInclusiveGC,costs});
                }
            }
        }
        Write(label+"_markers.json",output);
        if(mode=="alloc")Write(label+"_allocation_stacks.json",new{sampleLimit=3000,stackSamples,stacks});
    }
    static object CacheTelemetry()
    {
        var t=typeof(CoreGuardianSelectionOutline).Assembly.GetType("CoreGuardianBodyMaskCache");var f=BindingFlags.Static|BindingFlags.NonPublic;
        return new{entries=t.GetProperty("EntryCount",f).GetValue(null),bindings=t.GetProperty("BindingCount",f).GetValue(null),allocated=t.GetProperty("AllocatedCount",f).GetValue(null),bytes=t.GetProperty("AllocatedBytes",f).GetValue(null),hits=t.GetField("Hits",f).GetValue(null),generations=t.GetField("Generations",f).GetValue(null),allocations=t.GetField("Allocations",f).GetValue(null),releases=t.GetField("Releases",f).GetValue(null)};
    }
    static object Materials()
    {
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var owner=Resources.FindObjectsOfTypeAll<CoreGuardianSelectionOutline>().Where(c=>c.gameObject.scene.IsValid()).ToArray();
        var contacts=Resources.FindObjectsOfTypeAll<CoreGuardianPhotoShadow>().Where(c=>c.gameObject.scene.IsValid()).ToArray();
        var mask=owner.Select(c=>(Material)typeof(CoreGuardianSelectionOutline).GetField("maskMaterial",flags).GetValue(c)).Where(m=>m!=null).Distinct().ToArray();
        var outline=owner.Select(c=>(Material)typeof(CoreGuardianSelectionOutline).GetField("outlineMaterial",flags).GetValue(c)).Where(m=>m!=null).Distinct().ToArray();
        var contact=contacts.Select(c=>(Material)typeof(CoreGuardianPhotoShadow).GetField("contactMaterial",flags).GetValue(c)).Where(m=>m!=null).Distinct().ToArray();
        var all=mask.Concat(outline).Concat(contact).Distinct().ToArray();
        return new{owners=owner.Length,mask=mask.Length,outline=outline.Length,contact=contact.Length,total=all.Length,bytes=all.Sum(m=>Profiler.GetRuntimeMemorySizeLong(m))};
    }
    public static object Status(){return new{running,mode,run,warm,observed,targetFrames,frame=Time.frameCount,progress};}
    public static object Stop(){Finish("Stopped by diagnostic owner");return Status();}
    static void Finish(string error)
    {
        Profiler.enabled=false;if(poll!=null)EditorApplication.update-=poll;
        ProfilerDriver.deepProfiling=oldDeep;Profiler.enableAllocationCallstacks=oldStacks;ProfilerDriver.profileEditor=oldEditor;
        foreach(var pair in areas)ProfilerDriver.SetAreaEnabled(pair.Key,pair.Value);
        Profiler.enabled=oldEnabled;Application.runInBackground=oldBackground;running=false;
        Write(mode+"_completion.json",new{utc=DateTime.UtcNow,error,run,observed,restored=true,Profiler.enabled,ProfilerDriver.deepProfiling,Profiler.enableAllocationCallstacks,Application.runInBackground});
    }
    static void Write(string file,object value){File.WriteAllText(Path.Combine(root,file),JsonConvert.SerializeObject(value,Formatting.Indented));}
}
