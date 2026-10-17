using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
public static class InspectDecoration {
 public static object Main(string filename="preflight.json"){
 var s=SceneManager.GetActiveScene();
 var roots=s.GetRootGameObjects();
 var rs=roots.SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).ToArray();
 var state=new {play=EditorApplication.isPlaying,paused=EditorApplication.isPaused,compiling=EditorApplication.isCompiling,failed=EditorUtility.scriptCompilationFailed,scenes=SceneManager.sceneCount,scene=s.path,dirty=s.isDirty,timeScale=Time.timeScale,captureDelta=Time.captureDeltaTime,pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.GetType().FullName,camera=Camera.main.transform.position.ToString(),cameraSize=Camera.main.orthographicSize,
 renderGroups=rs.GroupBy(r=>new{r.sortingLayerName,r.sortingOrder}).Select(g=>new{g.Key,count=g.Count(),examples=g.Take(5).Select(r=>r.name).ToArray()}).ToArray(),
 background=rs.Where(r=>r.name=="Background_Ruins_VisualOnly").Select(r=>new{bounds=r.bounds.ToString(),json=EditorJsonUtility.ToJson(r),transform=EditorJsonUtility.ToJson(r.transform)}).ToArray()};
 File.WriteAllText("docs/development/environment_decoration_applied_20261018/"+filename,Newtonsoft.Json.JsonConvert.SerializeObject(state,Newtonsoft.Json.Formatting.Indented));
 return state;
 }
}
