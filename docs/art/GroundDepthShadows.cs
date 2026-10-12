using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class GroundDepthShadows
{
    const string Folder="Assets/Art/ScreenTests/20261008_Depth";
    static int Rows(string key)=>key=="core"?28:key=="tower_support"?16:key.StartsWith("tower_")?18:5;
    static Rect Footprint(Sprite sprite,int rows)
    {
        var t=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(t,File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
        var p=t.GetPixels32();int l=t.width,r=-1,b=t.height,top=-1;
        for(int y=0;y<rows;y++)for(int x=0;x<t.width;x++)if(p[y*t.width+x].a>=128){l=Math.Min(l,x);r=Math.Max(r,x);b=Math.Min(b,y);top=Math.Max(top,y);}
        UnityEngine.Object.DestroyImmediate(t);if(r<l)throw new Exception("Empty footprint");return new Rect(l,b,r-l+1,top-b+1);
    }
    static void SetEllipse(Transform t,float width,float height,Color color,int order)
    {
        t.localPosition=Vector3.zero;t.localScale=new Vector3(width,height*2,1);
        var r=t.GetComponent<SpriteRenderer>();r.color=color;r.sortingOrder=order;
    }
    public static object Main()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!="Assets/Scenes/ArtReview_20261008_Depth.unity")throw new Exception("Expected depth review Edit mode");
        Directory.CreateDirectory(Folder+"/ContactShapes");AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var reports=new List<object>();
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{Folder+"/Prefabs"})){
            string path=AssetDatabase.GUIDToAssetPath(guid);var root=PrefabUtility.LoadPrefabContents(path);
            try{
                string key=Path.GetFileNameWithoutExtension(path);var art=root.transform.Find("2D Sprite").GetComponent<SpriteRenderer>();var rect=Footprint(art.sprite,Rows(key));
                float width=rect.width/art.sprite.pixelsPerUnit*art.transform.localScale.x;
                bool equipment=key=="core"||key.StartsWith("tower_");
                float height=equipment?Mathf.Clamp(rect.height/art.sprite.pixelsPerUnit*.6f,.16f,.35f):Mathf.Clamp(width*.32f,.10f,.18f);
                var shadow=root.transform.Find("그림자");shadow.localPosition=Vector3.zero;
                SetEllipse(shadow.Find("바깥 그림자"),width*1.18f,height*1.10f,new Color(.05f,.06f,.08f,.30f),42);
                SetEllipse(shadow.Find("접촉 그림자"),width*1.04f,height*.68f,new Color(.02f,.025f,.03f,.58f),43);
                string shapePath=Folder+"/ContactShapes/"+key+".asset";
                var shape=AssetDatabase.LoadAssetAtPath<Sprite>(shapePath);
                if(shape==null){shape=Sprite.Create(art.sprite.texture,rect,new Vector2(.5f,.5f),art.sprite.pixelsPerUnit,0,SpriteMeshType.FullRect);shape.name=key+"_ContactShape";AssetDatabase.CreateAsset(shape,shapePath);}
                var old=shadow.Find("바닥 밀착선");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
                var seam=new GameObject("바닥 밀착선");seam.transform.SetParent(shadow,false);
                seam.transform.localPosition=new Vector3(0,-.025f,0);seam.transform.localScale=new Vector3(.6f*1.08f,equipment?.6f*.35f:.6f*.65f,1);
                var seamRenderer=seam.AddComponent<SpriteRenderer>();seamRenderer.sprite=shape;seamRenderer.sharedMaterial=art.sharedMaterial;seamRenderer.color=new Color(0,0,0,.65f);seamRenderer.sortingOrder=44;
                PrefabUtility.SaveAsPrefabAsset(root,path);reports.Add(new{key,footWidth=width,shadowWidth=width*1.18f,shadowHeight=height*1.10f});
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.WriteAllText("docs/art/grounded_shadows_20261008.json",Newtonsoft.Json.JsonConvert.SerializeObject(reports,Newtonsoft.Json.Formatting.Indented));
        return Verify();
    }
    public static object Verify()
    {
        var roots=UnityEngine.Object.FindObjectsByType<ArtDepthSorting>();if(roots.Length!=14)throw new Exception("Expected 14 roots");
        foreach(var root in roots){
            var art=root.transform.Find("2D Sprite").GetComponent<SpriteRenderer>();string key=root.name=="PlayerArt"?"Player":root.name;
            var rect=Footprint(art.sprite,Rows(key));float width=rect.width/art.sprite.pixelsPerUnit*art.transform.lossyScale.x;
            var foot=art.transform.TransformPoint(new Vector3((rect.center.x-art.sprite.pivot.x)/art.sprite.pixelsPerUnit,(rect.center.y-art.sprite.pivot.y)/art.sprite.pixelsPerUnit,0));
            var shadow=root.transform.Find("그림자");
            foreach(var name in new[]{"바깥 그림자","접촉 그림자"})if(Vector3.Distance(shadow.Find(name).position,foot)>.001f)throw new Exception("Offset shadow: "+key);
            var outer=shadow.Find("바깥 그림자").GetComponent<SpriteRenderer>();if(Mathf.Abs(outer.bounds.size.x-width*1.18f)>.001f)throw new Exception("Wrong footprint scale: "+key);
            if(shadow.Find("바닥 밀착선")==null)throw new Exception("Missing contact seam");
        }
        return "PASS: 14 instances; shadow centers match contact footprints; type-specific widths; contact seam present";
    }
}
