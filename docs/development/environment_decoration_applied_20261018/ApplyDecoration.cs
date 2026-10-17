using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;

// One-shot editor authoring. No runtime components, collision or game-rule changes.
public static class ApplyDecoration {
 const string Dir="docs/development/environment_decoration_applied_20261018/";
 const string Art="Assets/Art/Environment/AbandonedFortressB/DecorationHybrid_20261018/";
 const string Scene="Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity";
 const string Source=Dir+"HybridDecoration_Transparent.png";
 static void Require(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Ready(bool clean=true){var s=SceneManager.GetActiveScene();Require(!EditorApplication.isPlaying&&!EditorApplication.isPaused&&!EditorApplication.isCompiling&&!EditorUtility.scriptCompilationFailed,"Editor in use or compiling");Require(SceneManager.sceneCount==1&&s.path==Scene&&(!clean||!s.isDirty),"Expected single clean combat scene");}
 static string Hash(string p){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))).Replace("-","");}
 static Component[] Components()=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).Where(c=>c!=null).ToArray();
 static object Tiles(Tilemap t){var cells=new List<object>();foreach(var p in t.cellBounds.allPositionsWithin)if(t.HasTile(p))cells.Add(new{position=p.ToString(),asset=AssetDatabase.GetAssetPath(t.GetTile(p)),matrix=t.GetTransformMatrix(p).ToString(),color=t.GetColor(p).ToString()});return new{name=t.name,id=t.GetEntityId().ToString(),cells=cells.ToArray()};}
 static void Save(string name,object o)=>File.WriteAllText(Dir+name,JsonConvert.SerializeObject(o,Formatting.Indented));
 public static object Main(string mode){
 Ready();
 if(mode=="baseline"){
 var comps=Components();
 var assets=AssetDatabase.GetDependencies(Scene,true).Where(p=>p!=Scene&&File.Exists(p)).Select(p=>new{path=p,hash=Hash(p)}).ToArray();
 Save("baseline.json",new{scene=Scene,components=comps.Select(c=>new{id=c.GetEntityId().ToString(),type=c.GetType().FullName,name=c.name,json=EditorJsonUtility.ToJson(c)}).ToArray(),tilemaps=comps.OfType<Tilemap>().Select(Tiles).ToArray(),assets});
 Capture("before_edit.png");
 return new{components=comps.Length,assets=assets.Length,tilemaps=comps.OfType<Tilemap>().Count()};
 }
 if(mode=="apply"){
 Require(File.Exists(Dir+"baseline.json"),"Baseline missing");
 Require(GameObject.Find("EnvironmentDecoration_Hybrid_Selected")==null&&!AssetDatabase.IsValidFolder(Art.TrimEnd('/')),"Already applied");
 AssetDatabase.CreateFolder("Assets/Art/Environment/AbandonedFortressB","DecorationHybrid_20261018");
 File.Copy(Source,Art+"HybridDecoration_Transparent.png",false);
 AssetDatabase.ImportAsset(Art+"HybridDecoration_Transparent.png",ImportAssetOptions.ForceSynchronousImport);
 var imp=(TextureImporter)AssetImporter.GetAtPath(Art+"HybridDecoration_Transparent.png");
 imp.textureType=TextureImporterType.Default;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.npotScale=TextureImporterNPOTScale.None;imp.maxTextureSize=2048;imp.wrapMode=TextureWrapMode.Clamp;imp.alphaIsTransparency=true;imp.SaveAndReimport();
 var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"HybridDecoration_Transparent.png");
 Require(texture.width==1671&&texture.height==941,"Unexpected extraction canvas");
 var bg=GameObject.Find("Background_Ruins_VisualOnly").GetComponent<SpriteRenderer>();
 var mat=new Material(bg.sharedMaterial){name="HybridDecoration_Unlit"};mat.SetTexture("_MainTex",texture);AssetDatabase.CreateAsset(mat,Art+"HybridDecoration_Unlit.mat");
 var root=new GameObject("EnvironmentDecoration_Hybrid_Selected");root.transform.SetParent(GameObject.Find("CoreGuardian_MapArt_B").transform,false);Undo.RegisterCreatedObjectUndo(root,"Approved A interior and B perimeter decorations");
 var outer=new GameObject("OuterDecoration_OrderMinus10");outer.transform.SetParent(root.transform,false);
 var small=new GameObject("LowProps_OrderMinus5");small.transform.SetParent(root.transform,false);
 // Top-down source coordinates; disjoint UV rectangles retain source pixel layout.
 Make("LeftEdge",new Rect(0,0,125,941),outer.transform,-10,texture,mat);
 Make("RightEdge",new Rect(1540,0,131,941),outer.transform,-10,texture,mat);
 Make("TopEdge",new Rect(125,0,1415,115),outer.transform,-10,texture,mat);
 Make("BottomEdge",new Rect(125,865,1415,76),outer.transform,-10,texture,mat);
 Make("LowProps",new Rect(125,115,1415,750),small.transform,-5,texture,mat);
 AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
 Require(root.GetComponentsInChildren<Collider2D>().Length==0&&root.GetComponentsInChildren<Collider>().Length==0&&root.GetComponentsInChildren<MonoBehaviour>().Length==0,"Unexpected decoration behavior/collision");
 // Check all protected originals BEFORE saving the scene.
 var verification=Verify();
 EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
 Capture("applied_edit.png");
 Save("verification.json",verification);
 return verification;
 }
 if(mode=="verify"){var result=Verify();Save("final_verification.json",result);Capture("applied_edit.png");return result;}
 throw new Exception("Unknown mode");
 }
 static void Make(string name,Rect rect,Transform parent,int order,Texture2D tex,Material mat){
 var c=Camera.main;float h=c.orthographicSize*2,w=h*1920/1080;Vector2 min=(Vector2)c.transform.position-new Vector2(w,h)*.5f;
 float x0=min.x+rect.xMin/tex.width*w,x1=min.x+rect.xMax/tex.width*w,y0=min.y+(1-rect.yMax/tex.height)*h,y1=min.y+(1-rect.yMin/tex.height)*h;
 var mesh=new Mesh{name="Hybrid_"+name};mesh.vertices=new[]{new Vector3(x0,y0,0),new Vector3(x1,y0,0),new Vector3(x0,y1,0),new Vector3(x1,y1,0)};
 mesh.uv=new[]{new Vector2(rect.xMin/tex.width,1-rect.yMax/tex.height),new Vector2(rect.xMax/tex.width,1-rect.yMax/tex.height),new Vector2(rect.xMin/tex.width,1-rect.yMin/tex.height),new Vector2(rect.xMax/tex.width,1-rect.yMin/tex.height)};
 mesh.colors=Enumerable.Repeat(Color.white,4).ToArray();mesh.triangles=new[]{0,2,1,1,2,3};mesh.RecalculateBounds();mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,Art+name+".asset");
 var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.sortingOrder=order;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
 }
 static object Verify(){
 var old=JObject.Parse(File.ReadAllText(Dir+"baseline.json"));var comps=Components().ToDictionary(c=>c.GetEntityId().ToString());int checkedComponents=0;
 foreach(var entry in old["components"]){string id=(string)entry["id"];Require(comps.ContainsKey(id),"Original component missing "+entry["name"]);Require(EditorJsonUtility.ToJson(comps[id])==(string)entry["json"],"Original component changed "+entry["type"]+"/"+entry["name"]);checkedComponents++;}
 var now=JArray.FromObject(comps.Values.OfType<Tilemap>().Select(Tiles));Require(JToken.DeepEquals(old["tilemaps"],now),"Logical tiles changed");
 int assets=0;foreach(var a in old["assets"]){Require(Hash((string)a["path"])==(string)a["hash"],"Original resource changed "+a["path"]);assets++;}
 var root=GameObject.Find("EnvironmentDecoration_Hybrid_Selected");Require(root!=null,"Decorations missing");Require(root.GetComponentsInChildren<MonoBehaviour>().Length==0&&root.GetComponentsInChildren<Collider>().Length==0&&root.GetComponentsInChildren<Collider2D>().Length==0,"New behavior/collision");
 var renders=root.GetComponentsInChildren<MeshRenderer>();Require(renders.Length==5&&renders.Count(r=>r.sortingOrder==-10)==4&&renders.Count(r=>r.sortingOrder==-5)==1,"Decoration sorting mismatch");
 return new{passed=true,protectedComponents=checkedComponents,originalResourceHashes=assets,logicalTilemaps=now.Count,logicalCells=now.Sum(t=>t["cells"].Count()),newRenderers=renders.Length,newColliders=0,newMonoBehaviours=0,outerOrder=-10,lowPropOrder=-5,scene=SceneManager.GetActiveScene().path,camera=Camera.main.transform.position.ToString(),cameraSize=Camera.main.orthographicSize};
 }
 static void Capture(string name){
 var c=Camera.main;var target=c.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(1920,1080,24,RenderTextureFormat.ARGB32);var t=new Texture2D(1920,1080,TextureFormat.RGBA32,false);
 try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;t.ReadPixels(new Rect(0,0,1920,1080),0,0);t.Apply();File.WriteAllBytes(Dir+name,t.EncodeToPNG());}
 finally{c.targetTexture=target;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(t);RenderTexture.ReleaseTemporary(rt);}
 }
}
