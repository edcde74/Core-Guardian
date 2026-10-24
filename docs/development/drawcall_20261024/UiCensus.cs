return CGB29.CGUiCensus.Start();
}
}
namespace CGB29 {
using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CoreGuardian;
using Newtonsoft.Json;
using UnityEngine.UI;

// Step 2 of the shared outline plan: one bounded own Play session.
// Fixed load (same as baseline: 128 enemies, 15 towers), simulation frozen (timeScale 0) for captures.
// Captures: original x2 (reproducibility), shared x2, original again, outline-hidden for both, zoom 4.25 set,
// variety set (all enemy kinds), then toggle-on regression (pool reuse / removal / zoom / toggles / restart).
public static class CGUiCensus
{
    const string Root="docs/development/drawcall_20261024/final/";
    static readonly List<(int wait,string name,Action act)> steps=new List<(int,string,Action)>();
    static readonly List<object> log=new List<object>();
    static int index,waitFrames,lastFrame;static bool running,setupDone;static double deadline;static string error;
    static CoreGuardianPrototype game;static CoreGuardianObjectEmphasis emphasis;static Camera cam;
    static float oldCapture,oldScale,oldOrtho;static bool oldBackground;static int oldWidth;
    static UnityEngine.Object gameView;static PropertyInfo sizeIndex;static int savedSizeIndex,settle;
    static Type sharedType;
    public static object Start()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Stopped idle clean Editor required");
        Directory.CreateDirectory(Root);
        sharedType=typeof(CoreGuardianObjectEmphasis).Assembly.GetType("CoreGuardianSharedOutline");
        var type=typeof(Editor).Assembly.GetType("UnityEditor.GameView");gameView=Resources.FindObjectsOfTypeAll(type).First();
        sizeIndex=type.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);savedSizeIndex=(int)sizeIndex.GetValue(gameView);sizeIndex.SetValue(gameView,3);
        oldBackground=Application.runInBackground;oldCapture=Time.captureDeltaTime;oldScale=Time.timeScale;Application.runInBackground=true;
        steps.Clear();log.Clear();index=0;waitFrames=0;setupDone=false;error=null;settle=0;
        running=true;deadline=EditorApplication.timeSinceStartup+120;
        EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=PlayState;Application.logMessageReceived+=Log;
        EditorApplication.isPlaying=true;return new{started=true,savedSizeIndex};
    }
    static void Log(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert){if(error==null)error=m+"\n"+s;}}
    static void SetShared(bool on)=>sharedType.GetMethod("SetEnabled",BindingFlags.Static|BindingFlags.Public).Invoke(null,new object[]{on});
    static bool Shared=>(bool)sharedType.GetProperty("Enabled",BindingFlags.Static|BindingFlags.Public).GetValue(null);
    static void Note(string what,object data=null)=>log.Add(new{frame=Time.frameCount,what,shared=Shared,data});

    static Material hitMat;static Color hitTintOld;
    static void HitInfo(string tag)
    {
        var mp=new MaterialPropertyBlock();var list=new List<string>();
        foreach(var r in UnityEngine.Object.FindObjectsByType<SpriteRenderer>())
        {
            if(r.sharedMaterial==null||r.sharedMaterial.name!="CoreGuardian Body Hit (shared)")continue;hitMat=r.sharedMaterial;
            r.GetPropertyBlock(mp);if(list.Count<6)list.Add(r.name+" en="+r.enabled+" vis="+r.isVisible+" amt="+mp.GetFloat("_HitFlashAmount")+" col="+r.color+" order="+r.sortingOrder+" layer="+r.sortingLayerName+" pos="+r.transform.position);
        }
        Note("hit "+tag,list.ToArray());
    }
    static readonly List<SpriteRenderer> hidden=new List<SpriteRenderer>();
    static void HideHit(bool hide)
    {
        if(hide){foreach(var r in UnityEngine.Object.FindObjectsByType<SpriteRenderer>())if(r.enabled&&r.sharedMaterial!=null&&r.sharedMaterial.name=="CoreGuardian Body Hit (shared)"){r.enabled=false;hidden.Add(r);}Note("hidden",hidden.Count);}
        else{foreach(var r in hidden)if(r!=null)r.enabled=true;hidden.Clear();}
    }
    static readonly List<Behaviour> offB=new List<Behaviour>();static readonly List<Renderer> offR=new List<Renderer>();
    static void Hide(string tag,Func<Renderer,bool> pick){int n=0;foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>())if(r.enabled&&pick(r)){r.enabled=false;offR.Add(r);n++;}Note("hide "+tag,n);}
    static void HideCanvases(){int n=0;foreach(var c in UnityEngine.Object.FindObjectsByType<Canvas>())if(c.enabled&&c.isRootCanvas){c.enabled=false;offB.Add(c);n++;}Note("hide canvases",n);}
    static void Unhide(){foreach(var r in offR)if(r!=null)r.enabled=true;foreach(var b in offB)if(b!=null)b.enabled=true;offR.Clear();offB.Clear();}
    static readonly List<CanvasRenderer> culled=new List<CanvasRenderer>();
    static string Path(Transform t,Transform stop){var p=t.name;while(t.parent!=null&&t.parent!=stop){t=t.parent;p=t.name+"/"+p;}return p;}
    static Canvas root;static List<Transform> groups=new List<Transform>();
    static void UiDump()
    {
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>();
        Note("canvases",canvases.Select(c=>c.name+" root="+c.isRootCanvas+" mode="+c.renderMode+" order="+c.sortingOrder+" override="+c.overrideSorting+" enabled="+c.enabled+" graphics="+c.GetComponentsInChildren<Graphic>().Length).ToArray());
        root=canvases.Where(c=>c.isRootCanvas&&c.enabled).OrderByDescending(c=>c.GetComponentsInChildren<Graphic>().Length).First();
        var rows=new List<string>();
        foreach(var g in root.GetComponentsInChildren<Graphic>())
        {
            var cr=g.canvasRenderer;bool vis=g.isActiveAndEnabled&&!cr.cull&&cr.GetAlpha()>0&&g.color.a>0;
            var tex=g.mainTexture;var mat=g.materialForRendering;
            rows.Add((vis?"V":"-")+"|"+cr.absoluteDepth+"|"+g.GetType().Name+"|"+(mat?mat.name:"null")+"|"+(tex?tex.name:"null")+"|"+Path(g.transform,root.transform));
        }
        File.WriteAllLines(Root+"ui_graphics.txt",rows);
        groups.Clear();foreach(Transform c in root.transform)if(c.gameObject.activeInHierarchy)groups.Add(c);
        Note("groups",groups.Select(t=>t.name+" graphics="+t.GetComponentsInChildren<Graphic>().Count(g=>g.isActiveAndEnabled)).ToArray());
    }
    static void CullGroup(int i,bool on)
    {
        if(i>=groups.Count)return;
        if(on){foreach(var g in groups[i].GetComponentsInChildren<Graphic>())if(!g.canvasRenderer.cull){g.canvasRenderer.cull=true;culled.Add(g.canvasRenderer);}}
        else{foreach(var c in culled)if(c!=null)c.cull=false;culled.Clear();}
    }
    static void Unify(Func<SpriteRenderer,bool> pick)
    {
        Sprite target=null;int n=0;
        foreach(var r in UnityEngine.Object.FindObjectsByType<SpriteRenderer>()){if(!r.enabled||!pick(r))continue;if(target==null)target=r.sprite;r.sprite=target;n++;}
        Note("unified",n);
    }
    static void CensusOff(string tag)
    {
        var rows=new List<(int layer,int order,string mat,string tex,string name)>();
        foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>())
        {
            if(!r.enabled||!r.gameObject.activeInHierarchy||!r.isVisible)continue;
            string tex="-";if(r is SpriteRenderer sr&&sr.sprite!=null)tex=sr.sprite.texture.name;
            else if(r.sharedMaterial!=null&&r.sharedMaterial.HasProperty("_MainTex")&&r.sharedMaterial.mainTexture!=null)tex=r.sharedMaterial.mainTexture.name;
            rows.Add((SortingLayer.GetLayerValueFromID(r.sortingLayerID),r.sortingOrder,r.sharedMaterial?r.sharedMaterial.name:"null",tex,r.GetType().Name+":"+r.name));
        }
        var groups=rows.GroupBy(x=>(x.layer,x.order)).OrderBy(g=>g.Key.layer).ThenBy(g=>g.Key.order)
            .Select(g=>new{g.Key.layer,g.Key.order,count=g.Count(),keys=g.Select(x=>x.mat+"|"+x.tex).Distinct().Count(),
                top=g.GroupBy(x=>x.mat+"|"+x.tex+"|"+x.name).OrderByDescending(h=>h.Count()).Take(6).Select(h=>h.Count()+" "+h.Key).ToArray()}).ToArray();
        Note("census "+tag,new{renderers=rows.Count,orders=groups.Length,sumKeys=groups.Sum(g=>g.keys),groups});
    }
    static void Magenta(bool on){if(hitMat==null)return;if(on){hitTintOld=hitMat.GetColor("_HitFlashTint");hitMat.SetColor("_HitFlashTint",new Color(1,0,1,1));}else hitMat.SetColor("_HitFlashTint",hitTintOld);}
    static float oldBright=-1;
    static RenderTexture frameRT;
    static void BeginFrameCapture(){frameRT=RenderTexture.GetTemporary(cam.pixelWidth,cam.pixelHeight,24,RenderTextureFormat.ARGB32);cam.targetTexture=frameRT;}
    static void EndFrameCapture(string name)
    {
        cam.targetTexture=null;int w=frameRT.width,h=frameRT.height;
        var prev=RenderTexture.active;RenderTexture.active=frameRT;var tex=new Texture2D(w,h,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,w,h),0,0);tex.Apply();RenderTexture.active=prev;
        RenderTexture.ReleaseTemporary(frameRT);frameRT=null;
        File.WriteAllBytes(Root+name+".rgba",tex.GetRawTextureData());File.WriteAllBytes(Root+name+".png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);
        Note("frame capture "+name,new{w,h,draw=UnityEditor.UnityStats.drawCalls,setpass=UnityEditor.UnityStats.setPassCalls});
    }
    static void Capture(string name)
    {
        int w=cam.pixelWidth,h=cam.pixelHeight;
        var rt=RenderTexture.GetTemporary(w,h,24,RenderTextureFormat.ARGB32);var old=cam.targetTexture;
        cam.targetTexture=rt;cam.Render();cam.targetTexture=old;
        var prev=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(w,h,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,w,h),0,0);tex.Apply();RenderTexture.active=prev;
        RenderTexture.ReleaseTemporary(rt);
        File.WriteAllBytes(Root+name+".rgba",tex.GetRawTextureData());File.WriteAllBytes(Root+name+".png",tex.EncodeToPNG());
        UnityEngine.Object.Destroy(tex);
        Note("capture "+name,new{w,h,ortho=cam.orthographicSize,width=emphasis.outlineWidthPixels,targets=emphasis.ActiveTargetCount});
    }
    static void DumpMask(string name)
    {
        var mask=Shader.GetGlobalTexture("_CGSharedOutlineMask") as RenderTexture;
        if(mask==null){Note("mask missing "+name);return;}
        var prev=RenderTexture.active;RenderTexture.active=mask;var tex=new Texture2D(mask.width,mask.height,TextureFormat.RFloat,false);tex.ReadPixels(new Rect(0,0,mask.width,mask.height),0,0);tex.Apply();RenderTexture.active=prev;
        File.WriteAllBytes(Root+name+".r32",tex.GetRawTextureData());UnityEngine.Object.Destroy(tex);
        var layout=Shader.GetGlobalVector("_CGSharedOutlineLayout");
        Note("mask "+name,new{mask.width,mask.height,layout=new[]{layout.x,layout.y,layout.z,layout.w}});
    }
    static int EnabledSharedOutputs()=>Resources.FindObjectsOfTypeAll<MeshRenderer>().Count(r=>r!=null&&r.name=="SharedOutline"&&r.enabled&&r.gameObject.activeInHierarchy);
    static int EnabledOriginalOutputs()=>Resources.FindObjectsOfTypeAll<MeshRenderer>().Count(r=>r!=null&&r.name=="SelectionOutline"&&r.enabled&&r.gameObject.activeInHierarchy&&r.transform.parent!=null&&r.transform.parent.name.StartsWith("ObjectEmphasis_B2_"));
    static void Counts(string what)=>Note(what,new{targets=emphasis.ActiveTargetCount,sharedOutputs=EnabledSharedOutputs(),originalOutputs=EnabledOriginalOutputs(),enemies=game.Session.Enemies.Count});

    static void Spawn(EnemyKind kind,float progress)
    {
        var s=game.Session;var spec=s.Data.Enemies[kind];
        s.DebugSpawn(kind,progress,new EnemySpec{Kind=kind,Health=100000,Speed=spec.Speed,Defense=spec.Defense,Gold=0,CoreDamage=0,BuffRadius=spec.BuffRadius,BuffSpeed=spec.BuffSpeed});
    }
    static void RemoveEnemies(int count)
    {
        var s=game.Session;var handles=new List<EnemyHandle>();for(int i=0;i<Math.Min(count,s.Enemies.Count);i++)handles.Add(s.Enemies[i]);
        foreach(var h in handles)s.DebugRemoveEnemy(h);
    }
    static void Setup()
    {
        CoreGuardianAttackBodyBatch.SetEnabled(false);game.Diagnostic();var s=game.Session;
        for(int i=0;i<128;i++)Spawn(i%8==0?EnemyKind.Buff:EnemyKind.Attack,20+(i%12)*.3f);
        var p=s.Enemies[0].Position;var size=game.BoardSize;
        var cells=Enumerable.Range(0,size.x).SelectMany(x=>Enumerable.Range(0,size.y).Select(y=>new Vector2Int(x,y))).Where(s.IsBuildable).OrderBy(c=>((Vector2)c-p).sqrMagnitude).ToArray();
        for(int i=0;i<Math.Min(32,cells.Length);i++)game.Place(i%5==4?TowerKind.Support:TowerKind.Multi,cells[i]);
        game.StartCombat();typeof(Session).GetField("waveTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(s,.0001f);
        if(s.Enemies.Count!=128||s.Towers.Count!=15)throw new Exception("Unexpected load counts "+s.Enemies.Count+"/"+s.Towers.Count);
        oldOrtho=cam.orthographicSize;oldWidth=emphasis.outlineWidthPixels;
        Time.timeScale=1;Time.captureDeltaTime=1/60f;
    }
    static UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset urp;static bool srpBefore;
    static readonly List<(SpriteRenderer r,Material m)> swapped=new List<(SpriteRenderer,Material)>();
    static void Srp(bool on){UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching=on;}
    static void Stats(string what)=>Note(what,new{draw=UnityEditor.UnityStats.drawCalls,setpass=UnityEditor.UnityStats.setPassCalls,srp=UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching});
    static void SwapLit()
    {
        Material unlit=null;var lits=new Dictionary<string,int>();
        foreach(var r in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
            if(r.sharedMaterial!=null&&r.sharedMaterial.shader.name=="Universal Render Pipeline/2D/Sprite-Lit-Default"){lits[r.name]=(lits.TryGetValue(r.name,out var c)?c:0)+1;}
        foreach(var r in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
            if(r.sharedMaterial!=null&&r.sharedMaterial.name.StartsWith("ArtReview_Unlit")){unlit=r.sharedMaterial;break;}
        foreach(var r in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
            if(r.sharedMaterial!=null&&r.sharedMaterial.shader.name=="Universal Render Pipeline/2D/Sprite-Lit-Default"){swapped.Add((r,r.sharedMaterial));r.sharedMaterial=unlit;}
        Note("lit renderers by name",lits.OrderByDescending(k=>k.Value).Select(k=>k.Value+" "+k.Key).ToArray());
        Note("swapped to",unlit!=null?unlit.name+"|"+unlit.shader.name:"none");
    }
    static void UnswapLit(){foreach(var (r,m) in swapped)if(r!=null)r.sharedMaterial=m;swapped.Clear();}
    static void FixDeco()
    {
        var p=new MaterialPropertyBlock();p.SetVector("unity_SpriteProps",new Vector4(1,1,0,0));p.SetColor("unity_SpriteColor",Color.white);p.SetColor("_RendererColor",Color.white);
        var fixedNames=new List<string>();
        foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
            if(r.sharedMaterial!=null&&r.sharedMaterial.shader.name.StartsWith("Universal Render Pipeline/2D/Sprite-")&&!r.HasPropertyBlock()){r.SetPropertyBlock(p);fixedNames.Add(r.name);}
        Note("deco mpb applied",fixedNames.ToArray());
    }
    static void BuildSteps()
    {
        void Add(int wait,string name,Action a)=>steps.Add((wait,name,a));
        urp=(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;srpBefore=UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching;
        Add(30,"freeze",()=>{Time.timeScale=0;UiDump();});
        Add(3,"b base",()=>BeginFrameCapture());Add(1,"e base",()=>EndFrameCapture("ui_base"));
        Add(0,"cull0",()=>{CullGroup(0,true);Note("cull group",0<groups.Count?groups[0].name:"-");});
        Add(3,"b g0",()=>BeginFrameCapture());Add(1,"e g0",()=>{EndFrameCapture("ui_g0");CullGroup(0,false);});
        Add(0,"cull1",()=>{CullGroup(1,true);Note("cull group",1<groups.Count?groups[1].name:"-");});
        Add(3,"b g1",()=>BeginFrameCapture());Add(1,"e g1",()=>{EndFrameCapture("ui_g1");CullGroup(1,false);});
        Add(0,"cull2",()=>{CullGroup(2,true);Note("cull group",2<groups.Count?groups[2].name:"-");});
        Add(3,"b g2",()=>BeginFrameCapture());Add(1,"e g2",()=>{EndFrameCapture("ui_g2");CullGroup(2,false);});
        Add(0,"cull3",()=>{CullGroup(3,true);Note("cull group",3<groups.Count?groups[3].name:"-");});
        Add(3,"b g3",()=>BeginFrameCapture());Add(1,"e g3",()=>{EndFrameCapture("ui_g3");CullGroup(3,false);});
        Add(0,"cull4",()=>{CullGroup(4,true);Note("cull group",4<groups.Count?groups[4].name:"-");});
        Add(3,"b g4",()=>BeginFrameCapture());Add(1,"e g4",()=>{EndFrameCapture("ui_g4");CullGroup(4,false);});
        Add(0,"cull5",()=>{CullGroup(5,true);Note("cull group",5<groups.Count?groups[5].name:"-");});
        Add(3,"b g5",()=>BeginFrameCapture());Add(1,"e g5",()=>{EndFrameCapture("ui_g5");CullGroup(5,false);});
        Add(0,"cull6",()=>{CullGroup(6,true);Note("cull group",6<groups.Count?groups[6].name:"-");});
        Add(3,"b g6",()=>BeginFrameCapture());Add(1,"e g6",()=>{EndFrameCapture("ui_g6");CullGroup(6,false);});
        Add(0,"cull7",()=>{CullGroup(7,true);Note("cull group",7<groups.Count?groups[7].name:"-");});
        Add(3,"b g7",()=>BeginFrameCapture());Add(1,"e g7",()=>{EndFrameCapture("ui_g7");CullGroup(7,false);});
        Add(0,"cull8",()=>{CullGroup(8,true);Note("cull group",8<groups.Count?groups[8].name:"-");});
        Add(3,"b g8",()=>BeginFrameCapture());Add(1,"e g8",()=>{EndFrameCapture("ui_g8");CullGroup(8,false);});
        Add(0,"cull9",()=>{CullGroup(9,true);Note("cull group",9<groups.Count?groups[9].name:"-");});
        Add(3,"b g9",()=>BeginFrameCapture());Add(1,"e g9",()=>{EndFrameCapture("ui_g9");CullGroup(9,false);});
        Add(0,"cull10",()=>{CullGroup(10,true);Note("cull group",10<groups.Count?groups[10].name:"-");});
        Add(3,"b g10",()=>BeginFrameCapture());Add(1,"e g10",()=>{EndFrameCapture("ui_g10");CullGroup(10,false);});
        Add(0,"cull11",()=>{CullGroup(11,true);Note("cull group",11<groups.Count?groups[11].name:"-");});
        Add(3,"b g11",()=>BeginFrameCapture());Add(1,"e g11",()=>{EndFrameCapture("ui_g11");CullGroup(11,false);});
        Add(0,"cull12",()=>{CullGroup(12,true);Note("cull group",12<groups.Count?groups[12].name:"-");});
        Add(3,"b g12",()=>BeginFrameCapture());Add(1,"e g12",()=>{EndFrameCapture("ui_g12");CullGroup(12,false);});
        Add(0,"cull13",()=>{CullGroup(13,true);Note("cull group",13<groups.Count?groups[13].name:"-");});
        Add(3,"b g13",()=>BeginFrameCapture());Add(1,"e g13",()=>{EndFrameCapture("ui_g13");CullGroup(13,false);});
        Add(0,"cull14",()=>{CullGroup(14,true);Note("cull group",14<groups.Count?groups[14].name:"-");});
        Add(3,"b g14",()=>BeginFrameCapture());Add(1,"e g14",()=>{EndFrameCapture("ui_g14");CullGroup(14,false);});
        Add(0,"cull15",()=>{CullGroup(15,true);Note("cull group",15<groups.Count?groups[15].name:"-");});
        Add(3,"b g15",()=>BeginFrameCapture());Add(1,"e g15",()=>{EndFrameCapture("ui_g15");CullGroup(15,false);});
        Add(0,"restore",()=>{CullGroup(0,false);});
        Add(0,"done",()=>{});
    }
    static void Poll()
    {
        if(!running)return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("120s deadline");
            if(error!=null)throw new Exception("Console error: "+error);
            if(!EditorApplication.isPlaying)return;
            if(EditorApplication.isPaused)throw new Exception("External pause");
            if(!setupDone)
            {
                game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();if(game==null||!game.Ready)return;
                emphasis=UnityEngine.Object.FindAnyObjectByType<CoreGuardianObjectEmphasis>();cam=Camera.main;
                if(Screen.width!=1920||Screen.height!=1080||cam==null||cam.orthographicSize!=8.5f){if(settle++<15)return;throw new Exception("Expected 1920x1080/ortho8.5, actual "+Screen.width+"x"+Screen.height+"/"+cam?.orthographicSize);}
                if(emphasis==null)throw new Exception("No emphasis");
                Setup();BuildSteps();setupDone=true;lastFrame=Time.frameCount;waitFrames=steps[0].wait;Note("setup",new{screenW=Screen.width,screenH=Screen.height,ortho=cam.orthographicSize,outline=emphasis.outlineWidthPixels});return;
            }
            if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
            if(waitFrames>0){waitFrames--;return;}
            steps[index].act();index++;
            if(index>=steps.Count){Finish(null);return;}
            waitFrames=steps[index].wait;
        }
        catch(Exception e){Finish(e.ToString());}
    }
    static void Finish(string failure)
    {
        if(!running)return;running=false;EditorApplication.update-=Poll;Application.logMessageReceived-=Log;
        
        try{if(cam!=null)cam.orthographicSize=oldOrtho==0?cam.orthographicSize:oldOrtho;if(emphasis!=null&&oldWidth>0)emphasis.outlineWidthPixels=oldWidth;}catch{}
        try{if(cam!=null)cam.targetTexture=null;if(oldBright>0&&emphasis!=null)emphasis.bodyBrightness=oldBright;UnswapLit();UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching=srpBefore;}catch{}
        Time.captureDeltaTime=oldCapture;Time.timeScale=oldScale;
        File.WriteAllText(Root+"ui_run.json",JsonConvert.SerializeObject(new{error=failure,completedSteps=index,totalSteps=steps.Count,log},Formatting.Indented));
        if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;else Restore();
    }
    static void PlayState(PlayModeStateChange s){if(s==PlayModeStateChange.ExitingPlayMode&&running)Finish("Play stopped externally");if(s==PlayModeStateChange.EnteredEditMode)Restore();}
    static void Restore()
    {
        EditorApplication.playModeStateChanged-=PlayState;
        sizeIndex.SetValue(gameView,savedSizeIndex);Application.runInBackground=oldBackground;Time.captureDeltaTime=oldCapture;Time.timeScale=oldScale;
        var leaks=Resources.FindObjectsOfTypeAll<RenderTexture>().Count(r=>r!=null&&r.name=="Shared emphasis outline mask");
        var mats=Resources.FindObjectsOfTypeAll<Material>().Count(m=>m!=null&&(m.name=="Shared outline mask"||m.name=="Shared outline output"));
        var outputs=Resources.FindObjectsOfTypeAll<MeshRenderer>().Count(r=>r!=null&&r.name=="SharedOutline");
        var sc=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        File.WriteAllText(Root+"ui_restored.json",JsonConvert.SerializeObject(new{playing=EditorApplication.isPlaying,sharedEnabled=Shared,leakedMasks=leaks,leakedMaterials=mats,leakedOutputs=outputs,gameViewIndex=(int)sizeIndex.GetValue(gameView),Application.runInBackground,Time.captureDeltaTime,Time.timeScale,scene=sc.path,dirty=sc.isDirty,roots=sc.rootCount,native=CoreGuardianAttackBodyBatch.Enabled},Formatting.Indented));
    }
}
}
public static class CGTailPad29{static object P(){
