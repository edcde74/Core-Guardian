using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// Temporary visual layout comparison. No logical board, session or saved scene changes.
public static class PreviewGrid15
{
    const string Dir="docs/development/grid15_comparison_20261017/";
    static readonly Vector2Int[] Old={new(0,12),new(11,12),new(11,18),new(35,18),new(35,6),new(17,6),new(17,12),new(29,12),new(29,9)};
    static readonly Vector2Int[] Small={new(0,10),new(10,10),new(10,16),new(30,16),new(30,5),new(15,5),new(15,10),new(25,10),new(25,8)};
    static List<UnityEngine.Object> trash=new();
    static GameObject Root;
    static Material mat;
    static Sprite stone,curb,white;
    static T Temp<T>(T o) where T:UnityEngine.Object {o.hideFlags=HideFlags.HideAndDontSave;trash.Add(o);return o;}
    static void Piece(Sprite s,Vector2 p,Vector2 size,int order,Color color,float angle=0)
    {
        var go=Temp(new GameObject("Preview visual"));go.transform.SetParent(Root.transform);
        go.transform.position=new Vector3(p.x,p.y,0);go.transform.rotation=Quaternion.Euler(0,0,angle);
        go.transform.localScale=new Vector3(size.x/s.bounds.size.x,size.y/s.bounds.size.y,1);
        var r=go.AddComponent<SpriteRenderer>();r.sprite=s;r.sharedMaterial=mat;r.sortingOrder=order;r.color=color;
    }
    static HashSet<Vector2Int> Cells(Vector2Int[] route)
    {
        var cells=new HashSet<Vector2Int>();
        for(int i=1;i<route.Length;i++){
            var p=route[i-1];var delta=route[i]-p;var step=new Vector2Int(Math.Sign(delta.x),Math.Sign(delta.y));
            cells.Add(p);while(p!=route[i]){p+=step;cells.Add(p);}
        }return cells;
    }
    static void Capture(Camera camera,string name)
    {
        var rt=RenderTexture.GetTemporary(1920,1080,24);var previous=RenderTexture.active;
        var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();File.WriteAllBytes(Dir+name+".png",tex.EncodeToPNG());}
        finally{camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
    public static object Main()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||EditorApplication.isCompiling||scene.isDirty)throw new Exception("Clean stopped editor required");
        var renderers=UnityEngine.Object.FindObjectsByType<Renderer>();
        var states=renderers.ToDictionary(r=>r,r=>r.enabled);
        var transforms=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToDictionary(t=>t,t=>EditorJsonUtility.ToJson(t));
        var game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
        var bg=GameObject.Find("Background_Ruins_VisualOnly").GetComponent<SpriteRenderer>();
        var core=GameObject.Find("NewArt_SizeReview_Medium").transform.Find("core");
        var original=Camera.main;var cameraJson=EditorJsonUtility.ToJson(original);
        var results=new List<object>();
        try{
            var texture=Temp(new Texture2D(2,2));texture.LoadImage(File.ReadAllBytes("docs/development/route_art_proposals_20261017/01_fortress_flagstone.png"));texture.filterMode=FilterMode.Point;
            stone=Temp(Sprite.Create(texture,new Rect(575,texture.height-294,44,32),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect));
            curb=Temp(Sprite.Create(texture,new Rect(575,texture.height-260,44,11),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect));
            var pixel=Temp(new Texture2D(1,1));pixel.SetPixel(0,0,Color.white);pixel.Apply();white=Temp(Sprite.Create(pixel,new Rect(0,0,1,1),new Vector2(.5f,.5f),1));
            mat=bg.sharedMaterial;
            foreach(var r in renderers)if(r is TilemapRenderer||r is SpriteRenderer)r.enabled=false;
            for(int mode=1;mode<=2;mode++){
                Root=Temp(new GameObject("Grid15 comparison TEMP"));
                var camGO=Temp(new GameObject("Comparison camera TEMP"));var cam=camGO.AddComponent<Camera>();cam.CopyFrom(original);
                var data=original.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                var cloneData=camGO.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(data),cloneData);
                float cell=.69f;int width=mode==1?39:45,height=mode==1?22:25;
                Vector2 origin=mode==1?new Vector2(.045f,-.09f):Vector2.zero;
                cam.orthographicSize=mode==1?8.5f:9.775f;cam.transform.position=mode==1?original.transform.position:new Vector3(15.525f,8.625f,-10);
                var bgGO=Temp(new GameObject("Preview background"));bgGO.transform.SetParent(Root.transform);
                var b=bgGO.AddComponent<SpriteRenderer>();b.sprite=bg.sprite;b.sharedMaterial=mat;b.sortingOrder=-20;
                b.transform.position=mode==1?bg.transform.position:new Vector3(15.525f,8.625f,1);b.transform.localScale=bg.transform.lossyScale*(mode==1?1:1.15f);
                var route=mode==1?Small:Old;var cells=Cells(route);
                Func<Vector2Int,Vector2> center=p=>origin+new Vector2((p.x+.5f)*cell,(p.y+.5f)*cell);
                foreach(var p in cells){
                    var c=center(p);Piece(stone,c,Vector2.one*cell,1,new Color(1.3f,1.3f,1.3f,1));
                    foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right})if(!cells.Contains(p+d)){
                        bool horizontal=d.y!=0;Piece(curb,c+(Vector2)d*(cell*.5f-.045f),new Vector2(cell,.095f),2,new Color(1.25f,1.25f,1.25f,1),horizontal?0:90);
                    }
                }
                foreach(var pair in new[]{(root:game.player.transform,destination:center(mode==1?new Vector2Int(13,9):new Vector2Int(15,10))), (root:core,destination:center(route.Last()))}){
                    foreach(var src in pair.root.GetComponentsInChildren<SpriteRenderer>(true).Where(s=>states.ContainsKey(s)&&states[s])){
                        var go=Temp(new GameObject("Preview "+src.name));go.transform.SetParent(Root.transform);var dst=go.AddComponent<SpriteRenderer>();
                        EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(src),dst);dst.enabled=true;
                        go.transform.position=src.transform.position+(Vector3)(pair.destination-(Vector2)pair.root.position);go.transform.rotation=src.transform.rotation;go.transform.localScale=src.transform.lossyScale;
                    }
                }
                Capture(cam,"option_"+mode+"_clean");
                var gridRoot=Temp(new GameObject("Installation cells TEMP"));gridRoot.transform.SetParent(Root.transform);
                var roadCore=route.Last();
                for(int y=0;y<height;y++)for(int x=0;x<width;x++){
                    var p=new Vector2Int(x,y);bool blocked=cells.Contains(p)||p==roadCore;
                    Piece(white,center(p),Vector2.one*.6325f,3,blocked?new Color(1,.15f,.1f,.2f):new Color(.1f,1,.35f,.13f));
                }
                Capture(cam,"option_"+mode+"_grid");
                results.Add(new{mode,board=$"{width}x{height}",cell,worldWidth=width*cell,worldHeight=height*cell,cameraSize=cam.orthographicSize,roadCells=cells.Count,installationCells=width*height,route=route.Select(p=>new[]{p.x,p.y}).ToArray()});
                UnityEngine.Object.DestroyImmediate(Root);UnityEngine.Object.DestroyImmediate(camGO);
            }
        }
        finally{
            foreach(var pair in states)if(pair.Key)pair.Key.enabled=pair.Value;
            foreach(var o in trash.AsEnumerable().Reverse())if(o)UnityEngine.Object.DestroyImmediate(o);trash.Clear();
        }
        if(transforms.Any(p=>EditorJsonUtility.ToJson(p.Key)!=p.Value)||EditorJsonUtility.ToJson(original)!=cameraJson)throw new Exception("Original transform/camera changed");
        if(scene.isDirty)throw new Exception("Unexpected scene dirtiness; do not save");
        var report=new{visualComparisonOnly=true,logicalBoardUnchanged=true,savedSceneUnchanged=true,originalRenderersRestored=states.All(p=>p.Key.enabled==p.Value),playStarted=false,results};
        File.WriteAllText(Dir+"preview_report.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
    }
}
