using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

public static class CGEmphasisQuality
{
    const string Root="docs/development/emphasis_cpu_20261023/";
    public static object Run(string variant)
    {
        if(variant!="baseline"&&variant!="candidate")throw new Exception("Unknown variant");
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||UnityEngine.Profiling.Profiler.enabled||UnityEditorInternal.ProfilerDriver.deepProfiling)
            throw new Exception("Stopped idle Editor required");
        var scene=EditorSceneManager.NewPreviewScene();var previous=RenderTexture.active;
        var cameraRoot=new GameObject("Emphasis QA camera");SceneManager.MoveGameObjectToScene(cameraRoot,scene);
        var camera=cameraRoot.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.orthographic=true;camera.orthographicSize=2;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.17f,.19f,.21f,1);camera.cullingMask=1<<31;
        var rt=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);rt.Create();camera.targetTexture=rt;
        var read=new Texture2D(256,256,TextureFormat.RGBA32,false);var rows=new List<object>();int failures=0;
        try
        {
            foreach(var direction in new[]{Vector2.left,Vector2.right})
            foreach(float phase in new[]{.025f,.08f,.14f,.22f})
            foreach(bool overlap in new[]{false,true})
            {
                var roots=new List<GameObject>();var host=new GameObject("Emphasis QA");SceneManager.MoveGameObjectToScene(host,scene);
                var emphasis=host.AddComponent<CoreGuardianObjectEmphasis>();
                try
                {
                    for(int i=0;i<(overlap?2:1);i++)
                    {
                        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Units/enemy_attack.prefab"));roots.Add(root);
                        SceneManager.MoveGameObjectToScene(root,scene);root.transform.position=new Vector3(i*.12f,i*.025f,0);
                        var animation=CoreGuardianUnitAnimation.Attach(root,"Attack");animation.Move(direction,phase+i*.013f);animation.Present(.2f);
                        foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                        if(i==1)foreach(var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))renderer.color=new Color(.8f,1,.9f,1);
                    }
                    var art=roots[0].GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");
                    camera.transform.position=new Vector3(art.bounds.center.x,art.bounds.center.y,-10);
                    emphasis.PresentTargets(roots.Select(r=>r.transform),camera);
                    foreach(var t in host.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                    camera.Render();RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,256,256),0,0);read.Apply();
                    var pixels=read.GetPixels32();if(pixels.Count(c=>!c.Equals(pixels[0]))<20)throw new Exception("Empty render");
                    string id=direction.x+"_"+phase.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"_"+overlap;
                    var bytes=pixels.SelectMany(c=>new[]{c.r,c.g,c.b,c.a}).ToArray();int differences=0;
                    if(variant=="baseline")File.WriteAllBytes(Root+id+".rgba",bytes);
                    else
                    {
                        var before=File.ReadAllBytes(Root+id+".rgba");
                        for(int p=0;p<bytes.Length;p+=4)if(before[p]!=bytes[p]||before[p+1]!=bytes[p+1]||before[p+2]!=bytes[p+2]||before[p+3]!=bytes[p+3])differences++;
                    }
                    File.WriteAllBytes(Root+variant+"_"+id+".png",read.EncodeToPNG());
                    if(differences>0)failures++;rows.Add(new{direction=direction.x,phase,overlap,differences});
                }
                finally
                {
                    typeof(CoreGuardianObjectEmphasis).GetMethod("OnDestroy",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(emphasis,null);
                    UnityEngine.Object.DestroyImmediate(host);foreach(var root in roots)UnityEngine.Object.DestroyImmediate(root);
                }
                if(failures>0)goto Completed;
            }
            Completed: ;
        }
        finally
        {
            camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(read);rt.Release();UnityEngine.Object.DestroyImmediate(rt);EditorSceneManager.ClosePreviewScene(scene);
        }
        File.WriteAllText(Root+variant+"_quality.json",JsonConvert.SerializeObject(new{pairs=rows.Count,failures,width=256,height=256,ortho=2,rows},Formatting.Indented));
        return new{pairs=rows.Count,failures};
    }
}
