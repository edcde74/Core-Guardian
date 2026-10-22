// Diagnostic source compiled in memory through the official Unity run_script command.
// Never imported into Assets. Identical for baseline/current; injected workload, not balance QA.
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

public sealed class CGB2RuntimeProbe : MonoBehaviour
{
    static CGB2RuntimeProbe active;
    CoreGuardianPrototype game;
    string root,side;
    int run,step,lastFrame=-1;
    bool pending,oldDriver,oldProfiler,oldEditor,oldDeep,oldStacks,oldBackground,oldGame;
    float oldCapture,oldScale;
    double deadline;
    Vector2 player;
    readonly Dictionary<ProfilerArea,bool> areas=new Dictionary<ProfilerArea,bool>();
    readonly List<object> counts=new List<object>();
    readonly List<string> events=new List<string>();
    readonly List<KeyValuePair<EventInfo,Delegate>> subscriptions=new List<KeyValuePair<EventInfo,Delegate>>();
    readonly FieldInfo waveTime=typeof(Session).GetField("waveTime",BindingFlags.Instance|BindingFlags.NonPublic);
    readonly MethodInfo sync=typeof(CoreGuardianPrototype).GetMethod("SynchronizeViews",BindingFlags.Instance|BindingFlags.NonPublic);
    public static object Start(string label)
    {
        if(active!=null||!EditorApplication.isPlaying||Profiler.enabled)throw new Exception("Idle profiler and own Play required");
        var host=new GameObject("CG Runtime Validation (temporary)");active=host.AddComponent<CGB2RuntimeProbe>();
        active.Begin(label);return Status();
    }
    public static object Status()=>active==null?(object)new{running=false}:new{running=true,active.side,active.run,active.step,active.pending,frame=Time.frameCount};
    void Begin(string label)
    {
        side=label;root="docs/development/render_batching_20261022/b2_benchmark/"+side;Directory.CreateDirectory(root);
        game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();if(game==null||!game.Ready)throw new Exception("Game not ready");
        oldGame=game.enabled;oldDriver=ProfilerDriver.enabled;oldProfiler=Profiler.enabled;oldEditor=ProfilerDriver.profileEditor;oldDeep=ProfilerDriver.deepProfiling;oldStacks=Profiler.enableAllocationCallstacks;oldBackground=Application.runInBackground;oldCapture=Time.captureDeltaTime;oldScale=Time.timeScale;
        foreach(ProfilerArea a in Enum.GetValues(typeof(ProfilerArea)))areas[a]=ProfilerDriver.IsAreaEnabled(a);
        Write("environment.json",new{utc=DateTime.UtcNow,unity=Application.unityVersion,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,Screen.width,Screen.height,QualitySettings.vSyncCount,Application.targetFrameRate,oldDriver,oldProfiler,oldEditor,oldDeep,oldStacks,oldBackground,oldCapture,oldScale,warmup=60,sampledSimulationSteps=180,dt=1f/60,enemyCount=128,requestedTowers=32,repetitions=3});
        game.enabled=false;Application.runInBackground=true;Time.timeScale=1;Time.captureDeltaTime=1f/60;
        ProfilerDriver.profileEditor=false;ProfilerDriver.deepProfiling=false;Profiler.enableAllocationCallstacks=false;
        foreach(var a in new[]{ProfilerArea.CPU,ProfilerArea.Rendering,ProfilerArea.Memory,ProfilerArea.UI,ProfilerArea.Audio})ProfilerDriver.SetAreaEnabled(a,true);
        game.Session.DamageApplied+=(e,d)=>events.Add("D|"+e.Id+"|"+d);
        game.Session.ProjectileLaunched+=p=>events.Add("P|"+p.Id+"|"+p.Target.Id+"|"+p.Source);
        foreach(var name in new[]{"DamageApplied","ProjectileLaunched"}){
            var field=typeof(Session).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);
            subscriptions.Add(new KeyValuePair<EventInfo,Delegate>(typeof(Session).GetEvent(name),((Delegate)field.GetValue(game.Session)).GetInvocationList().Last()));
        }
        EditorApplication.update+=Poll;Setup();
    }
    void Setup()
    {
        Profiler.enabled=false;game.Diagnostic();var s=game.Session;events.Clear();counts.Clear();
        for(int i=0;i<128;i++)Spawn(i);
        var point=s.Enemies[0].Position;var size=game.BoardSize;
        var cells=Enumerable.Range(0,size.x).SelectMany(x=>Enumerable.Range(0,size.y).Select(y=>new Vector2Int(x,y))).Where(s.IsBuildable).OrderBy(p=>((Vector2)p-point).sqrMagnitude).ToArray();
        for(int i=0;i<Math.Min(32,cells.Length);i++)game.Place(i%5==4?TowerKind.Support:TowerKind.Multi,cells[i]);
        game.StartCombat();waveTime.SetValue(s,.001f);
        player=(Vector2)typeof(CoreGuardianPrototype).GetMethod("PlayerTile",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);
        sync.Invoke(game,null);step=0;pending=false;lastFrame=Time.frameCount;deadline=EditorApplication.timeSinceStartup+100;
    }
    void Spawn(int i)
    {
        var s=game.Session;var kind=i%8==0?EnemyKind.Buff:EnemyKind.Attack;var b=s.Data.Enemies[kind];
        s.DebugSpawn(kind,20+(i%12)*.3f,new EnemySpec{Kind=kind,Health=100000,Speed=b.Speed,Defense=b.Defense,Gold=0,CoreDamage=0,BuffRadius=b.BuffRadius,BuffSpeed=b.BuffSpeed});
    }
    void LateUpdate()
    {
        if(pending||Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
        try{
            var s=game.Session;
            bool replace=step>0&&step%30==0;
            if(replace){var dying=s.Enemies.Take(8).ToArray();foreach(var e in dying)s.ApplyDamage(e,1000000000,"Validation");}
            Profiler.BeginSample("CG.Validation.Tick");try{s.Tick(1f/60,player);}finally{Profiler.EndSample();}
            if(replace)for(int i=0;i<8;i++)Spawn(i);
            Profiler.BeginSample("CG.Validation.SynchronizeViews");try{sync.Invoke(game,null);}finally{Profiler.EndSample();}
            step++;
            if(step==60){ProfilerDriver.ClearAllFrames();typeof(ProfilerDriver).GetMethod("SetProfilingEnabled",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{true});Profiler.enabled=true;}
            if(step>60)counts.Add(new{step,engineFrame=Time.frameCount,enemies=s.Enemies.Count,towers=s.Towers.Count,projectiles=s.Projectiles.Count,drawCalls=UnityStats.drawCalls,setPass=UnityStats.setPassCalls,monoUsed=Profiler.GetMonoUsedSizeLong(),totalAllocated=Profiler.GetTotalAllocatedMemoryLong()});
            if(step>=240)pending=true;
        }catch(Exception ex){Finish(ex.ToString());}
    }
    void Poll()
    {
        if(!EditorApplication.isPlaying){Finish("Play ended externally");return;}
        if(EditorApplication.timeSinceStartup>deadline){Finish("Capture deadline exceeded");return;}
        if(!pending||Time.frameCount==lastFrame)return;
        try{
            Profiler.enabled=false;int first=ProfilerDriver.firstFrameIndex,last=ProfilerDriver.lastFrameIndex;
            if(first<0||last-first<175)throw new Exception("Insufficient native engine frames: "+first+".."+last);
            string nativeRoot="Logs/CG_RenderBatching_20261022/b2_benchmark/"+side;Directory.CreateDirectory(nativeRoot);
            bool saved=ProfilerDriver.SaveProfile(Path.GetFullPath(nativeRoot+"/run_"+run+".data"));
            var frames=new List<object>();
            for(int f=first;f<=last;f++)using(var iterator=new ProfilerFrameDataIterator())
            for(int t=0;t<iterator.GetThreadCount(f);t++)using(var raw=ProfilerDriver.GetRawFrameDataView(f,t)){
                if(!raw.valid||raw.sampleCount==0||raw.threadName!="Main Thread")continue;
                double tick=0,view=0;long gc=0,tickGC=0,viewGC=0;int tickEnd=-1,viewEnd=-1;
                for(int i=0;i<raw.sampleCount;i++){
                    var name=raw.GetSampleName(i);
                    if(name=="CG.Validation.Tick"){tick+=raw.GetSampleTimeMs(i);tickEnd=i+raw.GetSampleChildrenCountRecursive(i);}
                    if(name=="CG.Validation.SynchronizeViews"){view+=raw.GetSampleTimeMs(i);viewEnd=i+raw.GetSampleChildrenCountRecursive(i);}
                    if(name=="GC.Alloc"&&raw.GetSampleMetadataCount(i)>0){long b=raw.GetSampleMetadataAsLong(i,0);gc+=b;if(i<=tickEnd)tickGC+=b;if(i<=viewEnd)viewGC+=b;}
                }
                frames.Add(new{frame=f,raw.frameTimeMs,tickMs=tick,viewMs=view,gcBytes=gc,tickGC,viewGC,raw.sampleCount});
            }
            Write("run_"+run+"_frames.json",frames);Write("run_"+run+"_counts.json",counts);
            var s=game.Session;
            var enemyPool=typeof(Session).GetMethod("EnemyPool");
            Write("run_"+run+"_outcome.json",new{saved,first,last,events=events.ToArray(),s.CoreEnergy,s.Gold,s.Crystals,phase=s.Phase.ToString(),enemies=s.Enemies.Select(e=>new{e.Id,kind=e.Kind.ToString(),e.Health,x=e.Position.x,y=e.Position.y,e.PathTravelled,e.PathRemaining}).ToArray(),projectiles=s.Projectiles.Select(p=>new{p.Id,p.Source,target=p.Target.Id,x=p.Position.x,y=p.Position.y}).ToArray(),damage=s.DamageTotals.OrderBy(p=>p.Key).ToArray(),poolProperties=typeof(Session).GetProperties().Where(p=>p.Name.Contains("Pool")).ToDictionary(p=>p.Name,p=>p.GetValue(s)),enemyPools=enemyPool==null?null:Enum.GetValues(typeof(EnemyKind)).Cast<EnemyKind>().ToDictionary(k=>k.ToString(),k=>enemyPool.Invoke(s,new object[]{k}))});
            if(++run==3){Finish(null);return;}Setup();
        }catch(Exception ex){Finish(ex.ToString());}
    }
    void Finish(string error)
    {
        Profiler.enabled=false;EditorApplication.update-=Poll;
        if(game!=null)foreach(var subscription in subscriptions)subscription.Key.RemoveEventHandler(game.Session,subscription.Value);
        subscriptions.Clear();
        foreach(var p in areas)ProfilerDriver.SetAreaEnabled(p.Key,p.Value);
        ProfilerDriver.profileEditor=oldEditor;ProfilerDriver.deepProfiling=oldDeep;Profiler.enableAllocationCallstacks=oldStacks;ProfilerDriver.enabled=oldDriver;Profiler.enabled=oldProfiler;
        Application.runInBackground=oldBackground;Time.captureDeltaTime=oldCapture;Time.timeScale=oldScale;if(game!=null)game.enabled=oldGame;
        Write("completion.json",new{utc=DateTime.UtcNow,error,completedRuns=run,step,restored=true});active=null;Destroy(gameObject);
    }
    void Write(string name,object data)=>File.WriteAllText(Path.Combine(root,name),JsonConvert.SerializeObject(data,Formatting.Indented));
}
