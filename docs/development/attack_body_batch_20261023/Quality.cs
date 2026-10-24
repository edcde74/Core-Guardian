using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

public static class CGAttackBatchQuality
{
    const string Root="docs/development/attack_body_batch_20261023/";
    public static object Run()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling||UnityEngine.Profiling.Profiler.enabled||UnityEditorInternal.ProfilerDriver.deepProfiling)
            throw new Exception("Stopped idle Editor, recording and Deep OFF required");
        var scene=EditorSceneManager.NewPreviewScene();var previous=RenderTexture.active;
        var cameraRoot=new GameObject("Attack body isolated QA camera");SceneManager.MoveGameObjectToScene(cameraRoot,scene);
        var camera=cameraRoot.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.orthographic=true;camera.orthographicSize=2;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.17f,.19f,.21f,1);camera.cullingMask=1<<31;
        var rt=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);rt.Create();camera.targetTexture=rt;
        var read=new Texture2D(256,256,TextureFormat.RGBA32,false);var rows=new List<object>();
        int failures=0;
        try
        {
            foreach(var direction in new[]{Vector2.left,Vector2.right})
            foreach(float phaseTime in new[]{.025f,.08f,.14f,.22f})
            foreach(bool overlap in new[]{false,true})
            {
                CoreGuardianAttackBodyBatch.Clear();var roots=new List<GameObject>();
                try
                {
                    for(int i=0;i<(overlap?2:1);i++)
                    {
                        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Units/enemy_attack.prefab"));roots.Add(root);
                        SceneManager.MoveGameObjectToScene(root,scene);root.transform.position=new Vector3(i*.12f,i*.025f,0);
                        var animation=CoreGuardianUnitAnimation.Attach(root,"Attack");animation.Move(direction,phaseTime+i*.013f);animation.Present(.2f);
                        foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                        // Vary the tint as well as pose to reveal ordering errors between overlapping actors.
                        if(i==1)foreach(var r in root.GetComponentsInChildren<SpriteRenderer>(true))r.color=new Color(.8f,1,.9f,1);
                    }
                    var art=roots[0].GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");
                    camera.transform.position=new Vector3(art.bounds.center.x,art.bounds.center.y,-10);
                    CoreGuardianAttackBodyBatch.SetEnabled(false);camera.Render();var before=Read();
                    if(before.Count(c=>!c.Equals(before[0]))<20)throw new Exception("Empty original render");
                    CoreGuardianAttackBodyBatch.SetEnabled(true);CoreGuardianAttackBodyBatch.Flush(camera);
                    int sources=CoreGuardianAttackBodyBatch.LastSources,draws=CoreGuardianAttackBodyBatch.LastDraws;
                    camera.Render();var after=Read();CoreGuardianAttackBodyBatch.Restore();
                    int differences=0,maxChannel=0;
                    for(int p=0;p<before.Length;p++)if(!before[p].Equals(after[p]))
                    {
                        differences++;maxChannel=Math.Max(maxChannel,Math.Max(Math.Abs(before[p].r-after[p].r),Math.Max(Math.Abs(before[p].g-after[p].g),Math.Abs(before[p].b-after[p].b))));
                    }
                    if(differences>0||sources<4)failures++;
                    rows.Add(new{direction=direction.x,phaseTime,overlap,sources,draws,differences,maxChannel});
                    if(failures>0)
                    {
                        read.SetPixels32(before);read.Apply();File.WriteAllBytes(Root+"quality_order_fix_failed_original.png",read.EncodeToPNG());
                        read.SetPixels32(after);read.Apply();File.WriteAllBytes(Root+"quality_order_fix_failed_candidate.png",read.EncodeToPNG());
                    }
                }
                finally{CoreGuardianAttackBodyBatch.Clear();foreach(var root in roots)UnityEngine.Object.DestroyImmediate(root);}
                if(failures>0)goto Completed;
            }
            Completed: ;
        }
        finally
        {
            CoreGuardianAttackBodyBatch.Clear();camera.targetTexture=null;RenderTexture.active=previous;
            UnityEngine.Object.DestroyImmediate(read);rt.Release();UnityEngine.Object.DestroyImmediate(rt);EditorSceneManager.ClosePreviewScene(scene);
        }
        var result=new{pairs=rows.Count,failures,width=256,height=256,ortho=2,scope="isolated Attack side phases/overlap; no HUD or whole-game approval",rows};
        File.WriteAllText(Root+"quality.json",JsonConvert.SerializeObject(result,Formatting.Indented));return new{pairs=rows.Count,failures};
        Color32[] Read(){RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,256,256),0,0);read.Apply();return read.GetPixels32();}
    }
}
