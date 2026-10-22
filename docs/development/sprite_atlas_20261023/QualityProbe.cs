using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

public static class CGSpriteAtlasQuality
{
    public static object Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Idle Edit Mode required");
        var scene=EditorSceneManager.NewPreviewScene();var rows=new List<object>();var previous=RenderTexture.active;
        var cameraRoot=new GameObject("Atlas isolated QA camera");SceneManager.MoveGameObjectToScene(cameraRoot,scene);
        var camera=cameraRoot.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.orthographic=true;camera.orthographicSize=2;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.17f,.19f,.21f,1);camera.cullingMask=1<<31;
        var rt=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);rt.Create();camera.targetTexture=rt;
        var read=new Texture2D(256,256,TextureFormat.RGBA32,false);
        try
        {
            foreach(var role in new[]{"Attack","Defense","Buff","Single","Multi","Support"})
            foreach(var direction in new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right})
            {
                bool enemy=role=="Attack"||role=="Defense"||role=="Buff";
                string name=enemy?"enemy_"+role.ToLowerInvariant():"tower_"+role.ToLowerInvariant();
                var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Units/"+name+".prefab"));
                SceneManager.MoveGameObjectToScene(root,scene);root.transform.position=Vector3.zero;
                var temporary=new List<UnityEngine.Object>();
                try
                {
                    var animation=CoreGuardianUnitAnimation.Attach(root,role);
                    if(enemy)animation.Move(direction,.12f);else animation.Fire(direction,1);
                    animation.Present(.12f);
                    foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                    var body=root.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");
                    camera.transform.position=new Vector3(body.bounds.center.x,body.bounds.center.y,-10);
                    camera.Render();var packed=Read();
                    var background=packed[0];int visible=packed.Count(c=>!c.Equals(background));if(visible<20)throw new Exception("Empty render: "+role);
                    if(role=="Attack"&&direction==Vector2.up)File.WriteAllBytes("docs/development/sprite_atlas_20261023/quality_packed_example.png",read.EncodeToPNG());
                    // Replace only display coordinates/textures with equivalent original-source sprites.
                    foreach(var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        var s=renderer.sprite;if(s==null||!CoreGuardianSpriteAtlasSources.TryGetSource(s,out var source,out var uv)||source==s.texture)continue;
                        var rect=s.textureRect;
                        var originalRect=new Rect(Mathf.Round((rect.x/s.texture.width*uv.x+uv.z)*source.width),Mathf.Round((rect.y/s.texture.height*uv.y+uv.w)*source.height),rect.width,rect.height);
                        var replacement=Sprite.Create(source,originalRect,s.pivot/s.rect.size,s.pixelsPerUnit,0,SpriteMeshType.FullRect);temporary.Add(replacement);
                        foreach(var meshRenderer in renderer.GetComponentsInChildren<MeshRenderer>(true))
                        {
                            if(meshRenderer.transform.parent!=renderer.transform&&meshRenderer.name!="TowerBaseShade")continue;
                            var material=meshRenderer.sharedMaterial;if(material==null||material.mainTexture!=s.texture)continue;
                            var copy=new Material(material);temporary.Add(copy);copy.mainTexture=source;
                            if(copy.HasProperty("_UVRect")){var old=copy.GetVector("_UVRect");copy.SetVector("_UVRect",new Vector4(old.x*uv.x+uv.z,old.y*uv.y+uv.w,old.z*uv.x+uv.z,old.w*uv.y+uv.w));}
                            if(copy.HasProperty("_BottomUV"))copy.SetFloat("_BottomUV",copy.GetFloat("_BottomUV")*uv.y+uv.w);
                            if(copy.HasProperty("_TextureHeight"))copy.SetFloat("_TextureHeight",source.height);
                            meshRenderer.sharedMaterial=copy;
                            var filter=meshRenderer.GetComponent<MeshFilter>();var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);temporary.Add(mesh);
                            mesh.uv=mesh.uv.Select(p=>new Vector2(p.x*uv.x+uv.z,p.y*uv.y+uv.w)).ToArray();filter.sharedMesh=mesh;
                        }
                        renderer.sprite=replacement;
                    }
                    camera.Render();var original=Read();int changed=0,max=0;
                    for(int i=0;i<packed.Length;i++)if(!packed[i].Equals(original[i])){changed++;max=Math.Max(max,Math.Max(Math.Abs(packed[i].r-original[i].r),Math.Max(Math.Abs(packed[i].g-original[i].g),Math.Abs(packed[i].b-original[i].b))));}
                    rows.Add(new{role,direction=new[]{direction.x,direction.y},visible,changed,max});
                    if(changed>0){File.WriteAllBytes("docs/development/sprite_atlas_20261023/quality_"+role+"_"+direction.x+"_"+direction.y+"_reference.png",read.EncodeToPNG());}
                }
                finally{UnityEngine.Object.DestroyImmediate(root);foreach(var item in temporary)UnityEngine.Object.DestroyImmediate(item);}
            }
            File.WriteAllText("docs/development/sprite_atlas_20261023/quality.json",JsonConvert.SerializeObject(rows,Formatting.Indented));return rows;
        }
        finally{RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(read);rt.Release();UnityEngine.Object.DestroyImmediate(rt);EditorSceneManager.ClosePreviewScene(scene);}
        Color32[] Read(){RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,256,256),0,0);read.Apply();return read.GetPixels32();}
    }
}
