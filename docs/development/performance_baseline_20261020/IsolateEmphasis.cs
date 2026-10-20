if(!UnityEditor.EditorApplication.isPlaying)throw new InvalidOperationException("Own Play required");
var game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
var emphasis=game.GetComponent<CoreGuardianObjectEmphasis>();
bool oldGame=game.enabled,oldEmphasis=emphasis.enabled,oldBackground=Application.runInBackground;
game.enabled=false;Application.runInBackground=true;
game.Diagnostic();
var session=game.Session;
for(int i=0;i<128;i++){
 var kind=i%8==0?CoreGuardian.EnemyKind.Buff:CoreGuardian.EnemyKind.Attack;var b=session.Data.Enemies[kind];
 session.DebugSpawn(kind,20+(i%12)*.3f,new CoreGuardian.EnemySpec{Kind=kind,Health=100000,Speed=b.Speed,Defense=b.Defense,Gold=0,CoreDamage=0,BuffRadius=b.BuffRadius,BuffSpeed=b.BuffSpeed});
}
var point=session.Enemies[0].Position;var size=game.BoardSize;
var cells=Enumerable.Range(0,size.x).SelectMany(x=>Enumerable.Range(0,size.y).Select(y=>new Vector2Int(x,y))).Where(session.IsBuildable).OrderBy(p=>((Vector2)p-point).sqrMagnitude).ToArray();
for(int i=0;i<32;i++)game.Place(i%5==4?CoreGuardian.TowerKind.Support:CoreGuardian.TowerKind.Multi,cells[i]);
game.StartCombat();
game.GetType().GetMethod("SynchronizeViews",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(game,null);
var cpu=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Internal,"Main Thread",1);
var gc=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,"GC Allocated In Frame",1);
var modes=new[]{true,false,true};int mode=0,warm=25,lastFrame=Time.frameCount;
emphasis.enabled=modes[0];var samples=new List<object>();var all=new List<object>();
double deadline=UnityEditor.EditorApplication.timeSinceStartup+25;
UnityEditor.EditorApplication.CallbackFunction poll=null;
Action complete=()=>{
 UnityEditor.EditorApplication.update-=poll;cpu.Dispose();gc.Dispose();emphasis.enabled=oldEmphasis;game.enabled=oldGame;Application.runInBackground=oldBackground;
 System.IO.File.WriteAllText("docs/development/performance_baseline_20261020/emphasis_frozen128.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{kind="fixed scene paired on/off/on diagnostic; simulation/view sync paused, original presentation restored; NOT an approved removal",all},Newtonsoft.Json.Formatting.Indented));
};
poll=()=>{try{
 if(!UnityEditor.EditorApplication.isPlaying){complete();return;}
 if(Time.frameCount==lastFrame){if(UnityEditor.EditorApplication.timeSinceStartup>deadline){all.Add(new{error="frames not advancing"});complete();}return;}lastFrame=Time.frameCount;
 if(warm-->0)return;
 samples.Add(new{frame=Time.frameCount,cpuMs=cpu.Valid?cpu.LastValue/1000000.0:-1,unscaledMs=Time.unscaledDeltaTime*1000,gcBytes=gc.Valid?gc.LastValue:-1,drawCalls=UnityEditor.UnityStats.drawCalls,setPass=UnityEditor.UnityStats.setPassCalls,enemies=game.Session.Enemies.Count,towers=game.Session.Towers.Count});
 if(samples.Count<75&&UnityEditor.EditorApplication.timeSinceStartup<deadline)return;
 all.Add(new{enabled=modes[mode],samples=samples.ToArray()});mode++;if(mode==modes.Length){complete();return;}
 emphasis.enabled=modes[mode];samples.Clear();warm=25;deadline=UnityEditor.EditorApplication.timeSinceStartup+25;
 }catch(Exception e){all.Add(new{error=e.ToString()});complete();}};
UnityEditor.EditorApplication.update+=poll;return new{started=true,modes};
