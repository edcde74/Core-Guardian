using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

// Four bounded single-part comparisons distinguish sampling/transform from draw ordering.
public static class CGAttackPixelDiagnosis
{
    public static object Run()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling||UnityEditorInternal.ProfilerDriver.deepProfiling)throw new Exception("Idle, Deep OFF required");
        var scene=EditorSceneManager.NewPreviewScene();var previous=RenderTexture.active;var rows=new List<object>();
        var cameraRoot=new GameObject("Single-part QA camera");SceneManager.MoveGameObjectToScene(cameraRoot,scene);
        var camera=cameraRoot.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.orthographic=true;camera.orthographicSize=2;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.17f,.19f,.21f,1);camera.cullingMask=1<<31;
        var rt=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);rt.Create();camera.targetTexture=rt;var read=new Texture2D(256,256,TextureFormat.RGBA32,false);
        try
        {
            var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Units/enemy_attack.prefab"));SceneManager.MoveGameObjectToScene(root,scene);
            var animation=CoreGuardianUnitAnimation.Attach(root,"Attack");animation.Move(Vector2.left,.025f);animation.Present(.2f);
            foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
            var all=root.GetComponentsInChildren<SpriteRenderer>(true);var art=all.First(r=>r.name=="2D Sprite");camera.transform.position=new Vector3(art.bounds.center.x,art.bounds.center.y,-10);
            var parts=art.transform.Find("OriginalWalk").GetComponentsInChildren<SpriteRenderer>().Where(r=>r.name!="Body").ToArray();
            foreach(var part in parts)
            {
                CoreGuardianAttackBodyBatch.SetEnabled(false);foreach(var r in all)r.enabled=false;part.enabled=true;
                camera.Render();var original=Read();
                var dummy=new GameObject("Zero alpha batch companion").AddComponent<SpriteRenderer>();dummy.transform.SetParent(part.transform.parent,false);
                dummy.transform.localPosition=part.transform.localPosition;dummy.transform.localRotation=part.transform.localRotation;dummy.transform.localScale=part.transform.localScale;
                dummy.gameObject.layer=31;dummy.sprite=part.sprite;dummy.sharedMaterial=part.sharedMaterial;dummy.sortingLayerID=part.sortingLayerID;dummy.sortingOrder=part.sortingOrder;dummy.color=Color.clear;
                CoreGuardianAttackBodyBatch.Register("Attack",new[]{dummy,dummy,dummy,dummy,dummy});
                CoreGuardianAttackBodyBatch.SetEnabled(true);CoreGuardianAttackBodyBatch.Flush(camera);camera.Render();var candidate=Read();
                int differences=original.Where((c,i)=>!c.Equals(candidate[i])).Count();
                rows.Add(new{part=part.name,differences,sources=CoreGuardianAttackBodyBatch.LastSources,textureID=part.sprite.texture.GetEntityId().ToString(),rendererID=part.GetEntityId().ToString(),transformMatrix=part.transform.localToWorldMatrix.ToString(),rendererMatrix=part.localToWorldMatrix.ToString()});
                CoreGuardianAttackBodyBatch.Restore();UnityEngine.Object.DestroyImmediate(dummy.gameObject);
            }
        }
        finally{CoreGuardianAttackBodyBatch.Clear();camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(read);rt.Release();UnityEngine.Object.DestroyImmediate(rt);EditorSceneManager.ClosePreviewScene(scene);}
        File.WriteAllText("docs/development/attack_body_batch_20261023/single_part_diagnosis.json",JsonConvert.SerializeObject(rows,Formatting.Indented));return rows;
        Color32[] Read(){RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,256,256),0,0);read.Apply();return read.GetPixels32();}
    }
}
