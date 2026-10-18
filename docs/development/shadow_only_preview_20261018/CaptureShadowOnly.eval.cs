if(UnityEditor.EditorApplication.isPlaying||UnityEditor.EditorApplication.isCompiling)throw new System.Exception("Stopped idle editor required");
const string dir="docs/development/shadow_only_preview_20261018/";
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.isDirty)throw new System.Exception("Clean source scene required");
var game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();var source=Camera.main;
var core=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include).First(r=>r.name=="2D Sprite"&&r.sprite!=null&&r.sprite.name=="core"&&r.gameObject.scene==scene);
var refs=new[]{game.enemyArt[0],game.enemyArt[1],game.enemyArt[2],game.player.gameObject,core.transform.parent.gameObject,game.towerArt[0],game.towerArt[1],game.towerArt[2]};
var roles=new[]{"Attack","Defense","Buff","Player","Core","Single","Multi","Support"};
var names=new[]{"창병","방패병","군기병","플레이어","코어","단일","멀티","지원"};
var host=new GameObject("Temporary shadow-only composition"){hideFlags=HideFlags.HideAndDontSave};
var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();var previous=RenderTexture.active;
var meshes=new System.Collections.Generic.List<Mesh>();var materials=new System.Collections.Generic.List<Material>();
var switches=new System.Collections.Generic.List<System.Action>();var masks=new System.Collections.Generic.List<CoreGuardianSelectionOutline>();
var points=new System.Collections.Generic.List<object>();var originals=new System.Collections.Generic.List<SpriteRenderer>();
Shader shader=null;
try
{
 var cameraRoot=new GameObject("Temporary shadow-only camera");cameraRoot.transform.SetParent(host.transform,false);
 var camera=cameraRoot.AddComponent<Camera>();camera.CopyFrom(source);camera.enabled=false;camera.targetTexture=rt;
 camera.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
 var data=cameraRoot.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.SetRenderer(-1);
 data.renderPostProcessing=source.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing;
 data.antialiasing=UnityEngine.Rendering.Universal.AntialiasingMode.None;
 shader=UnityEditor.ShaderUtil.CreateShaderAsset(System.IO.File.ReadAllText(dir+"ShadowPreview.shader.txt"));shader.hideFlags=HideFlags.HideAndDontSave;
 if(UnityEditor.ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity.ToString()=="Error"))throw new System.Exception("Preview shader error");
 for(int i=0;i<refs.Length;i++)
 {
  var root=UnityEngine.Object.Instantiate(refs[i],host.transform);root.name="Preview_"+roles[i];root.transform.position=game.World(new Vector2(3+4*i,13));
  foreach(var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))behaviour.enabled=false;
  var body=root.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");body.sortingOrder=900;
  if(roles[i]!="Core")CoreGuardianUnitAnimation.Attach(root,roles[i]).Present(.5f);
  var maskRoot=new GameObject("Temporary actual-body union");maskRoot.transform.SetParent(host.transform,false);
  var union=maskRoot.AddComponent<CoreGuardianSelectionOutline>();union.enabled=false;union.Present(root.transform,camera);masks.Add(union);
  var outline=maskRoot.GetComponentInChildren<MeshRenderer>();outline.enabled=false;
  var mask=(RenderTexture)typeof(CoreGuardianSelectionOutline).GetField("mask",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(union);
  var sourceQuad=outline.GetComponent<MeshFilter>().sharedMesh;
  var projected=new Mesh{name="Actual body projected onto ground"};var vertices=sourceQuad.vertices;
  float anchorX=root.transform.position.x;float anchorY=union.TargetBounds.min.y;
  for(int j=0;j<vertices.Length;j++)
  {
   var p=outline.transform.TransformPoint(vertices[j]);float h=p.y-anchorY;
   vertices[j]=new Vector3(anchorX+(p.x-anchorX)+h*.50f,anchorY-h*.28f,p.z);
  }
  projected.vertices=vertices;projected.uv=sourceQuad.uv;projected.triangles=sourceQuad.triangles;projected.RecalculateBounds();meshes.Add(projected);
  var shadowRoot=new GameObject("Temporary separate cast shadow");shadowRoot.transform.SetParent(host.transform,false);
  shadowRoot.AddComponent<MeshFilter>().sharedMesh=projected;var shadow=shadowRoot.AddComponent<MeshRenderer>();
  var material=new Material(shader){mainTexture=mask};materials.Add(material);shadow.sharedMaterial=material;shadow.sortingOrder=40;shadow.enabled=false;
  var oldShadows=root.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="바깥 그림자"||r.name=="바닥 밀착선"||r.name=="짧은 투사 그림자").ToArray();
  switches.Add(()=>{foreach(var r in oldShadows)r.enabled=false;shadow.enabled=true;});
  var point=camera.WorldToScreenPoint(new Vector3(anchorX,anchorY,0));
  var maskCorner=camera.WorldToScreenPoint(outline.transform.TransformPoint(sourceQuad.vertices[0]));
  points.Add(new{role=roles[i],name=names[i],x=point.x,y=1080-point.y,maskX=Mathf.RoundToInt(maskCorner.x),maskY=1080-Mathf.RoundToInt(maskCorner.y)-mask.height,maskWidth=mask.width,maskHeight=mask.height});
  originals.AddRange(body.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy));
 }
 System.Action<string> save=path=>
 {
  camera.Render();RenderTexture.active=rt;var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);
  try{texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();System.IO.File.WriteAllBytes(dir+path,texture.EncodeToPNG());}
  finally{UnityEngine.Object.DestroyImmediate(texture);}
 };
 save("current_actual.png");foreach(var action in switches)action();save("shadow_only_actual.png");
 // Native union masks give independent proof that opaque source-body pixels were not repainted.
 for(int i=0;i<masks.Count;i++)
 {
  var union=masks[i];var mask=(RenderTexture)typeof(CoreGuardianSelectionOutline).GetField("mask",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(union);
  RenderTexture.active=mask;var t=new Texture2D(mask.width,mask.height,TextureFormat.RGBA32,false);
  t.ReadPixels(new Rect(0,0,mask.width,mask.height),0,0);t.Apply();System.IO.File.WriteAllBytes(dir+roles[i]+"_body_mask.png",t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);
 }
 System.IO.File.WriteAllText(dir+"capture.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{points,density=1080/(2*source.orthographicSize),projection=new{x=.50f,y=-.28f,nearOpacity=.38f,farOpacity=.22f},note="Actual approved original sprites + runtime idle cutouts, temporary clones in existing scene. Only shadow renderers switched. No image generator, repaint, Play, camera zoom or game asset modification. Camera capture excludes Overlay HUD."},Newtonsoft.Json.Formatting.Indented));
}
finally
{
 RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(host);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);foreach(var mat in materials)UnityEngine.Object.DestroyImmediate(mat);
 if(shader!=null)UnityEngine.Object.DestroyImmediate(shader);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
}
return new{scene=scene.path,dirty=scene.isDirty,scenes=UnityEngine.SceneManagement.SceneManager.sceneCount,play=UnityEditor.EditorApplication.isPlaying};
