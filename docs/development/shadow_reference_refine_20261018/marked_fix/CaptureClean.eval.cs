if(UnityEditor.EditorApplication.isPlaying||UnityEditor.EditorApplication.isCompiling)throw new System.Exception("Stopped idle editor required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.isDirty)throw new System.Exception("Clean scene required");
const string dir="docs/development/shadow_reference_refine_20261018/marked_fix/";
var game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();var source=Camera.main;
var core=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(r=>r.name=="2D Sprite"&&r.sprite!=null&&r.sprite.name=="core"&&r.gameObject.scene==scene);
var refs=new[]{game.enemyArt[0],game.enemyArt[1],game.enemyArt[2],game.player.gameObject,core.transform.parent.gameObject,game.towerArt[0],game.towerArt[1],game.towerArt[2]};
var roles=new[]{"Attack","Defense","Buff","Player","Core","Single","Multi","Support"};
var host=new GameObject("Temporary marked-area shadow review"){hideFlags=HideFlags.HideAndDontSave};
var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();var previous=RenderTexture.active;
var shadows=new System.Collections.Generic.List<SpriteRenderer>();
try
{
 var cameraRoot=new GameObject("Temporary same-scale camera");cameraRoot.transform.SetParent(host.transform,false);
 var camera=cameraRoot.AddComponent<Camera>();camera.CopyFrom(source);camera.enabled=false;camera.targetTexture=rt;
 camera.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
 var data=cameraRoot.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.SetRenderer(-1);
 data.renderPostProcessing=source.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing;
 data.antialiasing=UnityEngine.Rendering.Universal.AntialiasingMode.None;
 for(int i=0;i<refs.Length;i++)
 {
  var root=UnityEngine.Object.Instantiate(refs[i],host.transform);root.name="Review_"+roles[i];root.transform.position=game.World(new Vector2(3+4*i,13));
  foreach(var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))behaviour.enabled=false;
  var body=root.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");body.sortingOrder=900;
  if(roles[i]!="Core")CoreGuardianUnitAnimation.Attach(root,roles[i]).Present(.5f);
  if(roles[i]=="Defense"||roles[i]=="Core"||roles[i]=="Multi"||roles[i]=="Support")
   shadows.AddRange(root.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="바깥 그림자"||r.name=="접촉 그림자"||r.name=="바닥 밀착선"||r.name=="짧은 투사 그림자"||r.name=="바닥 수정빛"));
 }
 System.Action<string> save=path=>
 {
  camera.Render();RenderTexture.active=rt;var t=new Texture2D(1920,1080,TextureFormat.RGB24,false);
  try{t.ReadPixels(new Rect(0,0,1920,1080),0,0);t.Apply();System.IO.File.WriteAllBytes(dir+path,t.EncodeToPNG());}
  finally{UnityEngine.Object.DestroyImmediate(t);}
 };
 save("fresh_existing_ground.png");foreach(var r in shadows)r.enabled=false;save("clean_ground_four.png");
 System.IO.File.WriteAllText(dir+"capture_state.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{sourceScene=scene.path,play=false,dirty=scene.isDirty,width=1920,height=1080,density=1080/(2*source.orthographicSize),disabledCloneRenderers=shadows.Select(r=>new{r.name,parent=r.transform.parent.name}).ToArray(),note="Temporary clones only; same idle Present(.5); core floor glow isolated independently of body; no source assets or original renderers modified"},Newtonsoft.Json.Formatting.Indented));
}
finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(host);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
return new{scene=scene.path,dirty=scene.isDirty,scenes=UnityEngine.SceneManagement.SceneManager.sceneCount,play=UnityEditor.EditorApplication.isPlaying};
