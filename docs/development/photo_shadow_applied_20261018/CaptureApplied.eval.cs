if(UnityEditor.EditorApplication.isPlaying||UnityEditor.EditorApplication.isCompiling)throw new System.Exception("Stopped idle editor required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)throw new System.Exception("Clean scene required");
const string dir="docs/development/photo_shadow_applied_20261018/";
var game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();var source=Camera.main;
var core=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(r=>r.name=="2D Sprite"&&r.sprite!=null&&r.sprite.name=="core"&&r.gameObject.scene==scene);
var refs=new[]{game.enemyArt[0],game.enemyArt[1],game.enemyArt[2],game.player.gameObject,core.transform.parent.gameObject,game.towerArt[0],game.towerArt[1],game.towerArt[2]};
var roles=new[]{"Attack","Defense","Buff","Player","Core","Single","Multi","Support"};
var host=new GameObject("Temporary applied shadow verification"){hideFlags=HideFlags.HideAndDontSave};
var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();var previous=RenderTexture.active;
try
{
 var cameraRoot=new GameObject("Temporary applied shadow camera");cameraRoot.transform.SetParent(host.transform,false);
 var camera=cameraRoot.AddComponent<Camera>();camera.CopyFrom(source);camera.enabled=false;camera.targetTexture=rt;
 camera.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
 var data=cameraRoot.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.SetRenderer(-1);
 data.renderPostProcessing=source.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing;
 data.antialiasing=UnityEngine.Rendering.Universal.AntialiasingMode.None;
 for(int i=0;i<refs.Length;i++)
 {
  var root=UnityEngine.Object.Instantiate(refs[i],host.transform);root.name="Applied_"+roles[i];root.transform.position=game.World(new Vector2(3+4*i,13));
  foreach(var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))behaviour.enabled=false;
  var body=root.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");body.sortingOrder=900;
  if(roles[i]!="Core")CoreGuardianUnitAnimation.Attach(root,roles[i]).Present(.5f);
  else CoreGuardianPhotoShadow.Attach(root,"Core");
  root.GetComponent<CoreGuardianPhotoShadow>().Present(camera);
 }
 camera.Render();RenderTexture.active=rt;var t=new Texture2D(1920,1080,TextureFormat.RGB24,false);
 try{t.ReadPixels(new Rect(0,0,1920,1080),0,0);t.Apply();System.IO.File.WriteAllBytes(dir+"applied_idle_actual.png",t.EncodeToPNG());}
 finally{UnityEngine.Object.DestroyImmediate(t);}
}
finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(host);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
return new{scene=scene.path,dirty=scene.isDirty,play=UnityEditor.EditorApplication.isPlaying};
