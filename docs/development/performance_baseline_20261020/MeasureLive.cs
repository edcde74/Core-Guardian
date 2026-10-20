if(!UnityEditor.EditorApplication.isPlaying)throw new InvalidOperationException("Own Play required");
var game=UnityEngine.Object.FindFirstObjectByType<CoreGuardianPrototype>();
bool previousBackground=Application.runInBackground;Application.runInBackground=true;
var gc=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,"GC Allocated In Frame",1);
var cpu=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Internal,"Main Thread",1);
var render=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Internal,"Render Thread",1);
var modes=new[]{32,64,128};int mode=-1,warm=0,lastFrame=-1;double deadline=0;
var samples=new List<object>();var all=new List<object>();
UnityEditor.EditorApplication.CallbackFunction poll=null;
Action complete=()=>{
    UnityEditor.EditorApplication.update-=poll;gc.Dispose();cpu.Dispose();render.Dispose();Application.runInBackground=previousBackground;
    System.IO.File.WriteAllText("docs/development/performance_baseline_20261020/live.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{kind="actual scene Editor render stress; injected HP/resources/enemies, no balance interpretation; Editor frame costs, not standalone FPS",all},Newtonsoft.Json.Formatting.Indented));
};
Action setup=()=>{
    game.Diagnostic();var s=game.Session;
    for(int i=0;i<modes[mode];i++){
        var kind=i%8==0?CoreGuardian.EnemyKind.Buff:CoreGuardian.EnemyKind.Attack;var b=s.Data.Enemies[kind];
        s.DebugSpawn(kind,20+(i%12)*.3f,new CoreGuardian.EnemySpec{Kind=kind,Health=100000,Speed=b.Speed,Defense=b.Defense,Gold=0,CoreDamage=0,BuffRadius=b.BuffRadius,BuffSpeed=b.BuffSpeed});
    }
    var point=s.Enemies[0].Position;
    var size=game.BoardSize;
    var cells=Enumerable.Range(0,size.x).SelectMany(x=>Enumerable.Range(0,size.y).Select(y=>new Vector2Int(x,y))).Where(s.IsBuildable).OrderBy(p=>((Vector2)p-point).sqrMagnitude).ToArray();
    for(int i=0;i<Math.Min(32,cells.Length);i++)game.Place(i%5==4?CoreGuardian.TowerKind.Support:CoreGuardian.TowerKind.Multi,cells[i]);
    game.StartCombat();samples.Clear();warm=25;lastFrame=Time.frameCount;deadline=UnityEditor.EditorApplication.timeSinceStartup+20;
};
poll=()=>{
    try{
        if(!UnityEditor.EditorApplication.isPlaying){complete();return;}
        if(mode<0){mode=0;setup();return;}
        if(Time.frameCount==lastFrame){if(UnityEditor.EditorApplication.timeSinceStartup>deadline){all.Add(new{error="frame not advancing",mode});complete();}return;}
        lastFrame=Time.frameCount;
        if(warm-->0)return;
        samples.Add(new{frame=Time.frameCount,unscaledMs=Time.unscaledDeltaTime*1000,cpuMs=cpu.Valid?cpu.LastValue/1000000.0:-1,renderMs=render.Valid?render.LastValue/1000000.0:-1,gcBytes=gc.Valid?gc.LastValue:-1,enemies=game.Session.Enemies.Count,towers=game.Session.Towers.Count,projectiles=game.Session.Projectiles.Count,drawCalls=UnityEditor.UnityStats.drawCalls,setPass=UnityEditor.UnityStats.setPassCalls,triangles=UnityEditor.UnityStats.triangles,vertices=UnityEditor.UnityStats.vertices,monoUsed=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()});
        if(samples.Count<90&&UnityEditor.EditorApplication.timeSinceStartup<deadline)return;
        all.Add(new{requestedEnemies=modes[mode],gcValid=gc.Valid,cpuValid=cpu.Valid,renderValid=render.Valid,samples=samples.ToArray()});
        mode++;if(mode>=modes.Length){complete();return;}setup();
    }catch(Exception e){all.Add(new{error=e.ToString(),mode});complete();}
};
UnityEditor.EditorApplication.update+=poll;
return new{started=true,conditions=modes,framesPerCondition=90,warmup=25};
