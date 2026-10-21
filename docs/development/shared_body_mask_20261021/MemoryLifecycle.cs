// Existing controlled-probe scenario, extended for finite-duration resource-lifecycle QA.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;
using UnityEditorInternal;
using CoreGuardian;
using Newtonsoft.Json;
public static class CGBodyMaskLifecycle
{
    const string Root="docs/development/shared_body_mask_20261021/lifecycle/";
    static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly FieldInfo MF=typeof(CoreGuardianSelectionOutline).GetField("mask",F);
    static CoreGuardianPrototype game;static Camera cam;static float size;
    static bool running,oldProfileEditor,oldBackground;static int run,frame,last,warm,tail;
    static double start,deadline;static long peak;static int replaced;
    static List<object> rows=new List<object>(),summaries=new List<object>();static List<float> delta=new List<float>();
    static Dictionary<CoreGuardianSelectionOutline,RenderTexture> known=new Dictionary<CoreGuardianSelectionOutline,RenderTexture>();
    static Dictionary<ProfilerArea,bool> areas=new Dictionary<ProfilerArea,bool>();
    public static object Start()
    {
        if(running||!EditorApplication.isPlaying||Profiler.enabled||UnityEngine.FrameDebugger.enabled)throw new Exception("Own idle Play required");
        game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();if(game==null||!game.Ready)throw new Exception("Game not ready");
        cam=Camera.main;size=cam.orthographicSize;oldBackground=Application.runInBackground;oldProfileEditor=ProfilerDriver.profileEditor;
        foreach(ProfilerArea a in Enum.GetValues(typeof(ProfilerArea)))areas[a]=ProfilerDriver.IsAreaEnabled(a);
        Application.runInBackground=true;ProfilerDriver.profileEditor=false;
        foreach(var a in new[]{ProfilerArea.CPU,ProfilerArea.Memory,ProfilerArea.Rendering})ProfilerDriver.SetAreaEnabled(a,true);
        System.IO.File.WriteAllText(Root+"memory_environment.json",JsonConvert.SerializeObject(new{unity=Application.unityVersion,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,ramMB=SystemInfo.systemMemorySize,screen=new{Screen.width,Screen.height},size,Time.timeScale,Time.captureDeltaTime,vsync=QualitySettings.vSyncCount,framesPerRun=600,warmup=30,resetTail=90,repeats=3,oldBackground,oldProfileEditor,areas=areas.ToDictionary(x=>x.Key.ToString(),x=>x.Value)},Formatting.Indented));
        running=true;run=0;Setup();EditorApplication.update+=Poll;return Status();
    }
    static void Setup()
    {
        cam.orthographicSize=size;game.Diagnostic();var s=game.Session;
        for(int i=0;i<128;i++){var kind=i%8==0?EnemyKind.Buff:EnemyKind.Attack;var b=s.Data.Enemies[kind];s.DebugSpawn(kind,20+(i%12)*.3f,new EnemySpec{Kind=kind,Health=100000,Speed=b.Speed,Defense=b.Defense,Gold=0,CoreDamage=0,BuffRadius=b.BuffRadius,BuffSpeed=b.BuffSpeed});}
        var point=s.Enemies[0].Position;var bs=game.BoardSize;
        var cells=Enumerable.Range(0,bs.x).SelectMany(x=>Enumerable.Range(0,bs.y).Select(y=>new Vector2Int(x,y))).Where(s.IsBuildable).OrderBy(p=>((Vector2)p-point).sqrMagnitude).ToArray();
        for(int i=0;i<Math.Min(32,cells.Length);i++)game.Place(i%5==4?TowerKind.Support:TowerKind.Multi,cells[i]);
        game.StartCombat();typeof(Session).GetField("waveTime",F).SetValue(s,.001f);
        frame=0;warm=30;tail=0;last=Time.frameCount;peak=0;replaced=0;known.Clear();delta.Clear();start=EditorApplication.timeSinceStartup;deadline=start+120;
    }
    static object Sample(string phase)
    {
        long bytes=0;int count=0;var distinct=new HashSet<RenderTexture>();foreach(var owner in Resources.FindObjectsOfTypeAll<CoreGuardianSelectionOutline>())
        {
            var rt=(RenderTexture)MF.GetValue(owner);if(rt==null)continue;count++;if(distinct.Add(rt))bytes+=Profiler.GetRuntimeMemorySizeLong(rt);
            if(known.TryGetValue(owner,out var previous)&&!ReferenceEquals(previous,rt))replaced++;known[owner]=rt;
        }
        var batchMeshes=Resources.FindObjectsOfTypeAll<Mesh>().Where(m=>m.name=="Selection mask part batch").ToArray();var pages=Resources.FindObjectsOfTypeAll<Texture2DArray>().Where(t=>AssetDatabase.GetAssetPath(t).StartsWith("Assets/Resources/CoreGuardian/MaskAtlas/")).ToArray();peak=Math.Max(peak,bytes);return new{batchMeshes=batchMeshes.Length,batchMeshBytes=batchMeshes.Sum(m=>Profiler.GetRuntimeMemorySizeLong(m)),pages=pages.Length,pageBytes=pages.Sum(t=>Profiler.GetRuntimeMemorySizeLong(t)),run,frame,engineFrame=Time.frameCount,phase,zoom=cam.orthographicSize,enemies=game.Session.Enemies.Count(e=>e.Alive),towers=game.Session.Towers.Count,ownerMasks=count,distinctBound=distinct.Count,cache=CacheTelemetry(),ownerBytes=bytes,globalMasks=Resources.FindObjectsOfTypeAll<RenderTexture>().Count(t=>t.name=="Selected unit union mask"),totalAllocated=Profiler.GetTotalAllocatedMemoryLong(),mono=Profiler.GetMonoUsedSizeLong(),replaced,materials=Materials()};
    }
    static void Poll()
    {
        try
        {
            if(!EditorApplication.isPlaying){Finish("Play ended externally");return;}
            if(EditorApplication.timeSinceStartup>deadline){Finish("120s run deadline");return;}
            if(last==Time.frameCount)return;last=Time.frameCount;
            if(warm>0){if(--warm==0)rows.Add(Sample("warm"));return;}
            if(tail>0)
            {
                if(--tail>0)return;rows.Add(Sample("after-reset"));
                if(++run<3){Setup();return;}Finish(null);return;
            }
            delta.Add(Time.unscaledDeltaTime*1000);frame++;
            if(frame==200){rows.Add(Sample("before-zoom"));cam.orthographicSize=size*.5f;}
            if(frame==400){rows.Add(Sample("before-restore-zoom"));cam.orthographicSize=size;}
            if(frame%30==0||frame==201||frame==401)rows.Add(Sample("combat"));
            if(frame==500){ProfilerDriver.ClearAllFrames();typeof(ProfilerDriver).GetMethod("SetProfilingEnabled",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,new object[]{true});Profiler.enabled=true;}
            if(frame==530)
            {
                Profiler.enabled=false;if(ProfilerDriver.firstFrameIndex<0)throw new Exception("No valid lifecycle Profiler frames");Export("memory_run"+run);bool saved=ProfilerDriver.SaveProfile(Path.GetFullPath(Root+"memory_run"+run+".data"));
                System.IO.File.WriteAllText(Root+"memory_capture"+run+".json",JsonConvert.SerializeObject(new{saved,first=ProfilerDriver.firstFrameIndex,last=ProfilerDriver.lastFrameIndex},Formatting.Indented));
            }
            if(frame<600)return;
            summaries.Add(new{run,frames=delta.Count,elapsed=EditorApplication.timeSinceStartup-start,minFps=1000/delta.Max(),maxFps=1000/delta.Min(),avgFps=delta.Count*1000/delta.Sum(),peakOwnerBytes=peak,replaced});
            game.Diagnostic();tail=90;
        }
        catch(Exception e){Finish(e.ToString());}
    }
    static void Finish(string error)
    {
        EditorApplication.update-=Poll;Profiler.enabled=false;typeof(ProfilerDriver).GetMethod("SetProfilingEnabled",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,new object[]{false});cam.orthographicSize=size;ProfilerDriver.profileEditor=oldProfileEditor;
        foreach(var p in areas)ProfilerDriver.SetAreaEnabled(p.Key,p.Value);Application.runInBackground=oldBackground;running=false;
        System.IO.File.WriteAllText(Root+"memory_lifecycle.json",JsonConvert.SerializeObject(new{error,running,runs=summaries,rows},Formatting.Indented));
    }
    sealed class Cost {public double totalMs,selfMs;public long calls,gcBytes;}
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
                            if(false&&stackSamples<3000){
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
        if(false)Write(label+"_allocation_stacks.json",new{sampleLimit=3000,stackSamples,stacks});
    }

    static object CacheTelemetry()
    {
        var t=typeof(CoreGuardianSelectionOutline).Assembly.GetType("CoreGuardianBodyMaskCache");var f=BindingFlags.Static|BindingFlags.NonPublic;
        return new{entries=t.GetProperty("EntryCount",f).GetValue(null),bindings=t.GetProperty("BindingCount",f).GetValue(null),allocated=t.GetProperty("AllocatedCount",f).GetValue(null),bytes=t.GetProperty("AllocatedBytes",f).GetValue(null),hits=t.GetField("Hits",f).GetValue(null),generations=t.GetField("Generations",f).GetValue(null),allocations=t.GetField("Allocations",f).GetValue(null),releases=t.GetField("Releases",f).GetValue(null)};
    }
    static void Write(string name,object data){File.WriteAllText(Root+name,JsonConvert.SerializeObject(data,Formatting.Indented));}
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
    public static object Status()=>new{running,run,frame,warm,tail};
}
