using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Profiling;
using CoreGuardian;
using Newtonsoft.Json;

public static class CGPlacementProbe
{
    const string Folder="docs/development/placement_overlay_20261023";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static CoreGuardianPrototype game;
    static CoreGuardianPlacementOverlay overlay;
    static GameObject legacy;
    static Func<Vector2Int,bool> canInstall;
    static bool oldEnabled;
    static int oldFrame,stage,warm,samples,run;
    static readonly List<object> frames=new List<object>();
    static ProfilerRecorder main,gc;
    static bool running;
    static Color32[] Capture(Camera camera,string name)
    {
        var target=camera.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(1920,1080,24,RenderTextureFormat.ARGB32);Texture2D image=null;
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image=new Texture2D(1920,1080,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(Folder+"/"+name+".png",image.EncodeToPNG());return image.GetPixels32();}
        finally{camera.targetTexture=target;RenderTexture.active=active;if(image!=null)UnityEngine.Object.DestroyImmediate(image);RenderTexture.ReleaseTemporary(rt);}
    }
    static object Diff(Color32[] a,Color32[] b)
    {int n=0,max=0;long sum=0;for(int i=0;i<a.Length;i++){int d=Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)+Math.Abs(a[i].a-b[i].a);if(d>0)n++;max=Math.Max(max,d);sum+=d;}return new{differentPixels=n,maxChannelSum=max,totalDelta=sum};}
    static void SetSide(bool before)
    {legacy.SetActive(before);if(before)overlay.Hide();else overlay.Show(game.ground,canInstall);}
    public static object Start()
    {
        if(!EditorApplication.isPlaying||UnityEngine.Profiling.Profiler.enabled||running)throw new Exception("Own idle Play required");
        game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();if(game==null||!game.Ready)throw new Exception("Wait for ready game");
        if(game.Session.Phase!=SessionPhase.Preparation)throw new Exception("Own diagnostic scene must be in preparation");
        oldEnabled=game.enabled;game.enabled=false;game.ChooseTower(TowerKind.Single);
        if(!game.Installing.HasValue){game.enabled=oldEnabled;throw new Exception("Placement did not start");}
        overlay=(CoreGuardianPlacementOverlay)typeof(CoreGuardianPrototype).GetField("placementOverlay",Private).GetValue(game);
        var square=(Sprite)typeof(CoreGuardianPrototype).GetField("square",Private).GetValue(game);
        canInstall=(Func<Vector2Int,bool>)Delegate.CreateDelegate(typeof(Func<Vector2Int,bool>),game,typeof(CoreGuardianPrototype).GetMethod("CanInstall",Private));
        legacy=new GameObject("LegacyInstallationGrid_Diagnostic");legacy.transform.SetParent(game.transform,false);var size=game.ground.layoutGrid.cellSize;int count=0;
        foreach(var p in game.ground.cellBounds.allPositionsWithin)
        {
            if(!game.ground.HasTile(p))continue;
            var h=new GameObject("LegacyInstallationTile");h.transform.SetParent(legacy.transform);h.transform.position=game.ground.GetCellCenterWorld(p);
            h.transform.localScale=new Vector3(size.x*(.55f/.6f),size.y*(.55f/.6f),1);
            var r=h.AddComponent<SpriteRenderer>();r.sprite=square;r.color=canInstall(new Vector2Int(p.x,p.y))?new Color(.1f,1f,.35f,.13f):new Color(1f,.15f,.1f,.2f);r.sortingOrder=3;count++;
        }
        var camera=Camera.main;float zoom=camera.orthographicSize;var pixels=new List<object>();
        try
        {
            foreach(var z in new[]{8.5f,4.25f})
            {
                camera.orthographicSize=z;string key=z.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture).Replace('.','_');
                SetSide(true);var a=Capture(camera,"before_"+key);var control=Capture(camera,"control_"+key);SetSide(false);var b=Capture(camera,"after_"+key);
                pixels.Add(new{zoom=z,control=Diff(a,control),comparison=Diff(a,b)});
            }
        }
        finally{camera.orthographicSize=zoom;}
        File.WriteAllText(Folder+"/pixels.json",JsonConvert.SerializeObject(new{legacyCount=count,overlayCount=overlay.CellCount,shader=overlay.Renderer.sharedMaterial.shader.name,overlayMaterialBytes=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(overlay.Renderer.sharedMaterial),overlayMeshBytes=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(overlay.Mesh),pixels},Formatting.Indented));
        main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",8);gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",8);
        frames.Clear();stage=0;run=0;samples=0;warm=10;SetSide(true);oldFrame=Time.frameCount;running=true;EditorApplication.update+=Poll;
        return new{started=true,legacyCount=count,overlayCount=overlay.CellCount,pixels,mainValid=main.Valid,gcValid=gc.Valid};
    }
    static void Poll()
    {
        try
        {
            if(!EditorApplication.isPlaying)throw new Exception("Own Play stopped during capture");
            if(Time.frameCount==oldFrame)return;oldFrame=Time.frameCount;
            if(warm>0){warm--;return;}
            frames.Add(new{side=stage==0?"legacy":"mesh",run,frame=oldFrame,deltaMs=Time.unscaledDeltaTime*1000d,mainNs=main.Valid?main.LastValue:-1,gcBytes=gc.Valid?gc.LastValue:-1,draw=UnityEditor.UnityStats.drawCalls,setpass=UnityEditor.UnityStats.setPassCalls});
            if(++samples<60)return;samples=0;
            if(++run<3){warm=10;return;}
            if(stage==0){stage=1;run=0;warm=10;SetSide(false);return;}
            Finish(null);
        }
        catch(Exception e){Finish(e.ToString());}
    }
    static void Finish(string error)
    {
        EditorApplication.update-=Poll;running=false;
        if(main.Valid)main.Dispose();if(gc.Valid)gc.Dispose();
        if(legacy!=null){legacy.SetActive(false);UnityEngine.Object.Destroy(legacy);}
        if(game!=null){game.CancelInstall();game.enabled=oldEnabled;}
        File.WriteAllText(Folder+"/frames.json",JsonConvert.SerializeObject(frames,Formatting.Indented));
        File.WriteAllText(Folder+"/completion.json",JsonConvert.SerializeObject(new{complete=error==null,error,frames=frames.Count,playing=EditorApplication.isPlaying},Formatting.Indented));
    }
}
