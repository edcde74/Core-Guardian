using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

public static class CGAttackOrderDiagnosis
{
    public static object Run()
    {
        if(EditorApplication.isPlaying||UnityEditorInternal.ProfilerDriver.deepProfiling)throw new Exception("Stopped/Deep OFF required");
        var scene=EditorSceneManager.NewPreviewScene();var previous=RenderTexture.active;var rows=new List<object>();
        var cameraRoot=new GameObject("Draw order QA camera");SceneManager.MoveGameObjectToScene(cameraRoot,scene);
        var camera=cameraRoot.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.orthographic=true;camera.orthographicSize=2;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.17f,.19f,.21f,1);camera.cullingMask=1<<31;
        var rt=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);rt.Create();camera.targetTexture=rt;var read=new Texture2D(256,256,TextureFormat.RGBA32,false);
        try
        {
            var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Units/enemy_attack.prefab"));SceneManager.MoveGameObjectToScene(root,scene);
            var animation=CoreGuardianUnitAnimation.Attach(root,"Attack");animation.Move(Vector2.left,.025f);animation.Present(.2f);
            foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
            var art=root.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");camera.transform.position=new Vector3(art.bounds.center.x,art.bounds.center.y,-10);
            camera.Render();var original=Read();
            var registrations=(List<SpriteRenderer>)typeof(CoreGuardianAttackBodyBatch).GetField("registered",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            var creation=registrations.ToArray();
            foreach(string mode in new[]{"creation","reverse_creation","texture_ascending","texture_descending","entity_ascending","entity_descending"})
            {
                CoreGuardianAttackBodyBatch.Restore();registrations.Clear();
                IEnumerable<SpriteRenderer> ordered=creation;
                if(mode=="reverse_creation")ordered=creation.Reverse();
                if(mode=="texture_ascending")ordered=creation.OrderBy(r=>r.sprite.texture.GetEntityId());
                if(mode=="texture_descending")ordered=creation.OrderByDescending(r=>r.sprite.texture.GetEntityId());
                if(mode=="entity_ascending")ordered=creation.OrderBy(r=>r.GetEntityId());
                if(mode=="entity_descending")ordered=creation.OrderByDescending(r=>r.GetEntityId());
                registrations.AddRange(ordered);CoreGuardianAttackBodyBatch.SetEnabled(true);CoreGuardianAttackBodyBatch.Flush(camera);
                camera.Render();var candidate=Read();rows.Add(new{mode,differences=original.Where((c,i)=>!c.Equals(candidate[i])).Count(),order=registrations.Select(r=>r.name+" "+r.GetEntityId()).ToArray()});
            }
        }
        finally{CoreGuardianAttackBodyBatch.Clear();camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(read);rt.Release();UnityEngine.Object.DestroyImmediate(rt);EditorSceneManager.ClosePreviewScene(scene);}
        File.WriteAllText("docs/development/attack_body_batch_20261023/order_diagnosis.json",JsonConvert.SerializeObject(rows,Formatting.Indented));return rows;
        Color32[] Read(){RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,256,256),0,0);read.Apply();return read.GetPixels32();}
    }
}
