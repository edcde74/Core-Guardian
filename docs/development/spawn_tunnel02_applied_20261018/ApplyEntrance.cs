using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Uses the exact selected composite's local entrance pixels, not a regenerated entrance.
public static class ApplyEntrance
{
    const string Art="Assets/Art/Environment/AbandonedFortressB/";
    public static object Main()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed||scene.isDirty)throw new Exception("Clean stopped scene required");
        if(scene.path!="Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity"||GameObject.Find("SpawnTunnel02_Selected")!=null)throw new Exception("Unexpected scene or entrance already exists");
        AssetDatabase.ImportAsset(Art+"SpawnTunnel02_Approved.png",ImportAssetOptions.ForceSynchronousImport);
        var imp=(TextureImporter)AssetImporter.GetAtPath(Art+"SpawnTunnel02_Approved.png");
        imp.textureType=TextureImporterType.Default;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;
        imp.npotScale=TextureImporterNPOTScale.None;imp.maxTextureSize=2048;imp.wrapMode=TextureWrapMode.Clamp;imp.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"SpawnTunnel02_Approved.png");
        if(texture.width!=1672||texture.height!=941)throw new Exception("Selected reference size changed");
        var camera=Camera.main;float height=camera.orthographicSize*2,width=height*1920/1080;
        var min=(Vector2)camera.transform.position-new Vector2(width,height)*.5f;
        // A feathered native mesh blends only the tiny entrance patch with the unchanged ground.
        float[] xs={14,32,170,194},ys={380,400,552,572};
        var vertices=new Vector3[16];var uv=new Vector2[16];var colors=new Color[16];
        for(int y=0;y<4;y++)for(int x=0;x<4;x++){
            int i=y*4+x;vertices[i]=new Vector3(min.x+xs[x]*width/texture.width,min.y+(texture.height-ys[y])*height/texture.height,0);
            uv[i]=new Vector2(xs[x]/texture.width,1-ys[y]/texture.height);float alpha=x==0||x==3||y==0||y==3?0:1;
            colors[i]=new Color(1.3f,1.3f,1.3f,alpha);
        }
        var triangles=new System.Collections.Generic.List<int>();
        for(int y=0;y<3;y++)for(int x=0;x<3;x++){int i=y*4+x;triangles.AddRange(new[]{i,i+4,i+1,i+1,i+4,i+5});}
        var mesh=new Mesh{name="SpawnTunnel02_ExactSelectedPatch"};mesh.vertices=vertices;mesh.uv=uv;mesh.colors=colors;mesh.triangles=triangles.ToArray();mesh.RecalculateBounds();mesh.RecalculateNormals();
        AssetDatabase.CreateAsset(mesh,Art+"SpawnTunnel02_Patch.asset");
        var bg=GameObject.Find("Background_Ruins_VisualOnly").GetComponent<SpriteRenderer>();
        var material=new Material(bg.sharedMaterial){name="SpawnTunnel02_Selected"};material.SetTexture("_MainTex",texture);AssetDatabase.CreateAsset(material,Art+"SpawnTunnel02_Selected.mat");
        var root=new GameObject("SpawnTunnel02_Selected");root.transform.SetParent(GameObject.Find("CoreGuardian_MapArt_B").transform,false);
        Undo.RegisterCreatedObjectUndo(root,"Selected underground entrance");root.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=4;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
        var result=new{source=Art+"SpawnTunnel02_Approved.png",exactSelectedPixels=true,patchPixels=new[]{14,380,194,572},newColliders=0,newRuntimeBehaviours=0,spawnRouteChanged=false};
        File.WriteAllText("docs/development/spawn_tunnel02_applied_20261018/setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
