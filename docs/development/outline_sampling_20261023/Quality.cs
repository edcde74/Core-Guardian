using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

public static class CGOutlineQuality
{
    const string Root="docs/development/outline_sampling_20261023/";
    public static object Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||UnityEngine.Profiling.Profiler.enabled||UnityEditorInternal.ProfilerDriver.deepProfiling||CoreGuardianAttackBodyBatch.Enabled)
            throw new Exception("Stopped idle Editor and held native batch required");
        string phase=File.ReadAllText(Root+"phase.txt").Trim();
        var scene=EditorSceneManager.NewPreviewScene();var old=RenderTexture.active;
        var cameraRoot=new GameObject("Outline QA camera");SceneManager.MoveGameObjectToScene(cameraRoot,scene);
        var camera=cameraRoot.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.orthographic=true;camera.orthographicSize=2;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.17f,.19f,.21f,1);camera.cullingMask=1<<31;
        var rt=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);rt.Create();camera.targetTexture=rt;
        var read=new Texture2D(256,256,TextureFormat.RGBA32,false);
        var reference=new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Scripts/Tests/EditMode/Fixtures/SelectionOutlineReference.shader"));
        var rows=new List<object>();int failures=0;
        try
        {
            foreach(string role in new[]{"Attack","Buff"})
            foreach(var direction in new[]{Vector2.left,Vector2.right,Vector2.up,Vector2.down})
            foreach(float step in new[]{.025f,.08f,.14f,.22f})
            foreach(bool overlap in new[]{false,true})
            {
                var roots=new List<GameObject>();GameObject host=null;
                try
                {
                    for(int i=0;i<(overlap?2:1);i++)
                    {
                        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Units/enemy_"+role.ToLowerInvariant()+".prefab"));roots.Add(root);SceneManager.MoveGameObjectToScene(root,scene);
                        root.transform.position=new Vector3(i*.12f,i*.025f,0);
                        var animation=CoreGuardianUnitAnimation.Attach(root,role);animation.Move(direction,step+i*.013f);animation.Present(.2f);
                        foreach(var node in root.GetComponentsInChildren<Transform>(true))node.gameObject.layer=31;
                    }
                    var art=roots[0].GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");camera.transform.position=new Vector3(art.bounds.center.x,art.bounds.center.y,-10);
                    host=new GameObject("Outline QA consumers");SceneManager.MoveGameObjectToScene(host,scene);
                    var emphasis=host.AddComponent<CoreGuardianObjectEmphasis>();emphasis.PresentTargets(roots.Select(r=>r.transform),camera);
                    var selection=host.AddComponent<CoreGuardianSelectionOutline>();selection.enabled=false;selection.Present(roots[0].transform,camera);
                    foreach(var consumer in host.GetComponentsInChildren<CoreGuardianSelectionOutline>(true))
                    {
                        var mask=(RenderTexture)typeof(CoreGuardianSelectionOutline).GetField("mask",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(consumer);
                        if(mask!=null&&(mask.useMipMap||mask.filterMode!=FilterMode.Point||mask.wrapMode!=TextureWrapMode.Clamp))throw new Exception("Explicit LOD0 mask contract failed");
                    }
                    foreach(var node in host.GetComponentsInChildren<Transform>(true))node.gameObject.layer=31;
                    var outputs=host.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.sharedMaterial!=null&&r.sharedMaterial.shader.name=="CoreGuardian/SelectionOutline").ToArray();
                    var materials=outputs.Select(r=>r.sharedMaterial).ToArray();Color32[] before;
                    try{foreach(var output in outputs)output.sharedMaterial=reference;before=Read();}
                    finally{for(int i=0;i<outputs.Length;i++)outputs[i].sharedMaterial=materials[i];}
                    var after=Read();
                    if(before.Count(c=>!c.Equals(before[0]))<20)throw new Exception("Empty actor comparison");
                    int differences=before.Where((p,i)=>!p.Equals(after[i])).Count();
                    rows.Add(new{role,directionX=direction.x,directionY=direction.y,step,overlap,outputs=outputs.Length,differences});
                    if(rows.Count==1||differences>0)
                    {
                        read.SetPixels32(before);read.Apply();File.WriteAllBytes(Root+phase+"_actor_before.png",read.EncodeToPNG());
                        read.SetPixels32(after);read.Apply();File.WriteAllBytes(Root+phase+"_actor_after.png",read.EncodeToPNG());
                    }
                    if(differences>0){failures++;goto Completed;}
                }
                finally
                {
                    if(host!=null)
                    {
                        var emphasis=host.GetComponent<CoreGuardianObjectEmphasis>();
                        typeof(CoreGuardianObjectEmphasis).GetMethod("OnDestroy",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(emphasis,null);
                        foreach(var outline in host.GetComponents<CoreGuardianSelectionOutline>())typeof(CoreGuardianSelectionOutline).GetMethod("ReleaseResources",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(outline,null);
                        UnityEngine.Object.DestroyImmediate(host);
                    }
                    foreach(var root in roots)UnityEngine.Object.DestroyImmediate(root);
                }
            }
            Completed:;
        }
        finally{camera.targetTexture=null;RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(read);UnityEngine.Object.DestroyImmediate(reference);EditorSceneManager.ClosePreviewScene(scene);}
        File.WriteAllText(Root+phase+"_actor_quality.json",JsonConvert.SerializeObject(new{phase,pairs=rows.Count,failures,width=256,height=256,scope="same original Attack/Buff, four directions/four poses, single/overlap, emphasis width1 plus inspection width2; not full HUD/native zoom QA",rows},Formatting.Indented));
        return new{phase,pairs=rows.Count,failures};
        Color32[] Read(){camera.Render();RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,256,256),0,0);read.Apply();return read.GetPixels32();}
    }
}
