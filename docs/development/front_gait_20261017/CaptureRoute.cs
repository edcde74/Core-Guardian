if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Own Play required");
var g=UnityEngine.Object.FindFirstObjectByType<CoreGuardianPrototype>();
if(g==null||!g.Ready)throw new System.Exception("Ready scene required");
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var path=(System.Collections.Generic.List<UnityEngine.Vector2>)typeof(CoreGuardian.Session).GetField("path",flags).GetValue(g.Session);
float distance=0;int segment=-1;
for(int i=0;i<path.Count-1;i++){var step=path[i+1]-path[i];if(step.y<-.5f&&UnityEngine.Mathf.Abs(step.y)>UnityEngine.Mathf.Abs(step.x)){segment=i;break;}distance+=step.magnitude;}
if(segment<0)throw new System.Exception("Down route missing");
var session=g.Session;var camera=UnityEngine.Camera.main;var target=camera.targetTexture;var active=UnityEngine.RenderTexture.active;bool enabled=g.enabled;
var rt=UnityEngine.RenderTexture.GetTemporary(1920,1080,24);var texture=new UnityEngine.Texture2D(1920,1080,UnityEngine.TextureFormat.RGB24,false);
var folder="docs/development/front_gait_20261017/route_frames/";System.IO.Directory.CreateDirectory(folder);
try
{
 g.enabled=false;session.StartCombat();
 var kinds=new[]{CoreGuardian.EnemyKind.Defense,CoreGuardian.EnemyKind.Buff,CoreGuardian.EnemyKind.Boss};
 var enemies=kinds.Select((k,i)=>session.DebugSpawn(k,distance+.5f+i*3f)).ToArray();
 g.GetType().GetMethod("SynchronizeViews",flags).Invoke(g,null);
 camera.targetTexture=rt;
 for(int i=0;i<48;i++)
 {
  session.Tick(.025f,new UnityEngine.Vector2(-100,-100));g.GetType().GetMethod("SynchronizeViews",flags).Invoke(g,null);
  camera.Render();UnityEngine.RenderTexture.active=rt;texture.ReadPixels(new UnityEngine.Rect(0,0,1920,1080),0,0);texture.Apply();
  System.IO.File.WriteAllBytes(folder+i.ToString("000")+".png",texture.EncodeToPNG());
 }
 var report=new {kind="Actual scene camera, diagnostic spawn on unchanged waypoint path; manually ticked 0.025s/frame; no Overlay HUD",segment,startDistance=distance,path=path.Select(p=>new{x=p.x,y=p.y}).ToArray(),units=enemies.Select(e=>{var p=camera.WorldToScreenPoint(g.World(e.Position));return new{kind=e.Kind.ToString(),screen=new{x=p.x,y=p.y,z=p.z}};}).ToArray(),orthographicSize=camera.orthographicSize,cameraPosition=new{x=camera.transform.position.x,y=camera.transform.position.y,z=camera.transform.position.z},timeScale=UnityEngine.Time.timeScale,captureDelta=UnityEngine.Time.captureDeltaTime};
 System.IO.File.WriteAllText("docs/development/front_gait_20261017/route_capture.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
}
finally{camera.targetTexture=target;UnityEngine.RenderTexture.active=active;g.enabled=enabled;UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(texture);}
return "48 actual game-camera route frames saved; no camera transform/zoom/time settings changed; own diagnostic Play must be stopped";
