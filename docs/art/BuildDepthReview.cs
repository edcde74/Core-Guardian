using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class BuildDepthReview
{
    const string Source="Assets/Scenes/ArtReview_20261008_5x5.unity";
    const string Target="Assets/Scenes/ArtReview_20261008_Depth.unity";
    const string Folder="Assets/Art/ScreenTests/20261008_Depth";

    static Vector3 Foot(Sprite s,int rows)
    {
        var t=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(t,File.ReadAllBytes(AssetDatabase.GetAssetPath(s)));
        var p=t.GetPixels32();int l=t.width,r=-1,b=t.height,top=-1;
        for(int y=0;y<rows;y++)for(int x=0;x<t.width;x++)if(p[y*t.width+x].a>=128){l=Math.Min(l,x);r=Math.Max(r,x);b=Math.Min(b,y);top=Math.Max(top,y);}
        UnityEngine.Object.DestroyImmediate(t);
        if(r<l)throw new Exception("Missing footprint");
        return new Vector3(((l+r+1)*.5f-s.pivot.x)/s.pixelsPerUnit,((b+top+1)*.5f-s.pivot.y)/s.pixelsPerUnit,0);
    }
    static Sprite MakeEllipse()
    {
        var t=new Texture2D(48,24,TextureFormat.RGBA32,false);
        for(int y=0;y<24;y++)for(int x=0;x<48;x++){
            float dx=(x+0.5f-24)/24f,dy=(y+0.5f-12)/12f,d=dx*dx+dy*dy;
            byte a=(byte)(d<.55f?255:d<.78f?175:d<1f?80:0);
            t.SetPixel(x,y,new Color32(255,255,255,a));
        }
        t.Apply();string path=Folder+"/ShadowEllipse.png";File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spritePixelsPerUnit=48;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static SpriteRenderer Ellipse(Transform parent,string name,Sprite sprite,Material material,Vector2 size,Vector2 offset,Color color,int order)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=offset;go.transform.localScale=new Vector3(size.x,size.y*2,1);
        var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=material;r.color=color;r.sortingOrder=order;return r;
    }
    public static object Main()
    {
        var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.path!=Source||scene.isDirty)throw new Exception("Expected saved 5x5 scene");
        if(File.Exists(Target)||Directory.Exists(Folder))throw new Exception("Depth review already exists");
        if(!EditorSceneManager.SaveScene(scene,Target,true))throw new Exception("Scene copy failed");
        scene=EditorSceneManager.OpenScene(Target,OpenSceneMode.Single);
        Directory.CreateDirectory(Folder+"/Prefabs");AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var ellipse=MakeEllipse();
        var sprites=UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Where(r=>r.enabled&&r.sprite!=null&&(r.name=="PlayerVisual_Centered"||r.name=="core"||r.name.StartsWith("tower_")||r.name.StartsWith("enemy_"))).ToArray();
        foreach(var old in sprites){
            bool player=old.name=="PlayerVisual_Centered";string key=player?"Player":old.name;
            int rows=key=="core"?28:key=="tower_support"?16:key.StartsWith("tower_")?18:5;
            var foot=old.transform.TransformPoint(Foot(old.sprite,rows));var parent=old.transform.parent;
            string prefab=Folder+"/Prefabs/"+key+".prefab";
            if(!File.Exists(prefab)){
                var root=new GameObject(key);root.transform.position=foot;
                var visual=UnityEngine.Object.Instantiate(old.gameObject);visual.name="2D Sprite";visual.transform.position=old.transform.position;visual.transform.localScale=old.transform.lossyScale;visual.transform.SetParent(root.transform,true);
                var art=visual.GetComponent<SpriteRenderer>();
                var shadow=new GameObject("그림자");shadow.transform.SetParent(root.transform,false);
                float width=old.bounds.size.x*(key=="core"?.98f:key.StartsWith("tower_")?.95f:.85f);
                float height=key=="core"?.48f:key.StartsWith("tower_")?.29f:.19f;
                Ellipse(shadow.transform,"바깥 그림자",ellipse,old.sharedMaterial,new Vector2(width*1.22f,height*1.25f),new Vector2(.10f,-.05f),new Color(.07f,.08f,.11f,.28f),42);
                Ellipse(shadow.transform,"접촉 그림자",ellipse,old.sharedMaterial,new Vector2(width*.78f,height*.6f),Vector2.zero,new Color(.04f,.05f,.07f,.48f),43);
                if(key=="core")Ellipse(root.transform,"바닥 수정빛",ellipse,old.sharedMaterial,new Vector2(width*1.75f,.85f),Vector2.zero,new Color(.1f,.8f,1f,.16f),41);
                root.AddComponent<ArtDepthSorting>().SetArtwork(art);root.transform.position=Vector3.zero;
                PrefabUtility.SaveAsPrefabAsset(root,prefab);UnityEngine.Object.DestroyImmediate(root);
            }
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefab),scene);
            instance.transform.SetParent(parent,true);instance.transform.position=foot;
            if(player)instance.name="PlayerArt";
            UnityEngine.Object.DestroyImmediate(old.gameObject);
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");
        return Verify();
    }
    public static object Verify()
    {
        if(SceneManager.GetActiveScene().path!=Target)throw new Exception("Wrong scene");
        var roots=UnityEngine.Object.FindObjectsByType<ArtDepthSorting>();
        if(roots.Length!=14)throw new Exception("Expected 14 art prefab instances: "+roots.Length);
        foreach(var r in roots){
            if(!PrefabUtility.IsPartOfPrefabInstance(r))throw new Exception("Not prefab instance");
            var visual=r.transform.Find("2D Sprite");var shadow=r.transform.Find("그림자");
            if(visual==null||shadow==null||shadow.GetComponentsInChildren<SpriteRenderer>().Length<2)throw new Exception("Missing art/shadow structure");
            var art=visual.GetComponent<SpriteRenderer>();string key=r.name=="PlayerArt"?"Player":r.name.Replace("(Clone)","");
            int rows=key=="core"?28:key=="tower_support"?16:key.StartsWith("tower_")?18:5;
            if(Vector3.Distance(visual.TransformPoint(Foot(art.sprite,rows)),r.transform.position)>.001f)throw new Exception("Foot moved: "+key);
        }
        if(AssetDatabase.FindAssets("t:Prefab",new[]{Folder}).Length!=8)throw new Exception("Expected 8 reusable prefabs");
        return "PASS: 14 instances / 8 prefabs; Sprite + two-layer Shadow; foot alignment retained; core ground glow";
    }
    public static object RepairPlayerPrefab()
    {
        string path=Folder+"/Prefabs/Player.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try{
            var visual=root.transform.Find("2D Sprite");var r=visual.GetComponent<SpriteRenderer>();
            visual.position+=root.transform.position-visual.TransformPoint(Foot(r.sprite,5));
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return Verify();
    }
    public static object Live(string action)
    {
        if(!EditorApplication.isPlaying)throw new Exception("Expected Play mode");
        var m=UnityEngine.Object.FindObjectsByType<PrototypeGridMovement>().Single();
        if(action=="start"){Application.runInBackground=true;if(!m.TryStep(Vector2Int.right))throw new Exception("Move refused");return "Move started";}
        var art=m.transform.Find("PlayerArt");
        if(m.CurrentCell!=new Vector3Int(16,10,0)||Vector3.Distance(m.transform.position,new Vector3(9.9f,6.3f,0))>.001f||Vector3.Distance(art.position,m.transform.position)>.001f)throw new Exception("Art/shadow did not follow one tile move");
        var renderer=art.Find("2D Sprite").GetComponent<SpriteRenderer>();if(renderer.sortingOrder!=1000-Mathf.RoundToInt(art.position.y*20))throw new Exception("Depth order stale");
        return "PASS: artwork and shadows follow player; tile center and depth order correct";
    }
}
