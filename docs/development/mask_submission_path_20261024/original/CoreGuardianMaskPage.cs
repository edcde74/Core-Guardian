using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// GPU-only staging pages. A token never outlives its camera view/frame or its explicit lease.
public static class CoreGuardianMaskPage
{
    const int Size=2048,Padding=10;
    // Device capabilities stay fixed until the Editor/Player graphics device is recreated.
    // Unity's enum-validation path allocates when this API is called for every mask.
    static readonly bool supported=SystemInfo.maxTextureSize>=Size&&SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32);
    sealed class Page
    {
        internal RenderTexture texture;
        internal int x,y,row;
        internal void Reset(){x=y=row=0;}
        internal bool Pack(int width,int height,out RectInt rect)
        {
            int w=width+2*Padding,h=height+2*Padding;
            if(x+w>Size){x=0;y+=row;row=0;}
            if(y+h>Size){rect=default;return false;}
            rect=new RectInt(x+Padding,y+Padding,width,height);x+=w;row=Mathf.Max(row,h);return true;
        }
    }
    sealed class View
    {
        internal Camera camera;
        internal int frame,width,height;
        internal long generation;
        internal bool enabled;
        internal Rect viewport;
        internal Matrix4x4 view,projection;
        internal RenderTexture target;
        internal readonly List<Page> pages=new List<Page>();
        internal readonly HashSet<long> leases=new HashSet<long>();
        internal bool Matches(Camera c)=>c!=null&&viewport==c.pixelRect&&view.Equals(c.worldToCameraMatrix)
            &&projection.Equals(c.projectionMatrix)&&target==c.targetTexture&&enabled==c.enabled
            &&width==(target!=null?target.width:Screen.width)&&height==(target!=null?target.height:Screen.height);
        internal void Begin(Camera c,int f)
        {
            camera=c;frame=f;generation=++nextGeneration;leases.Clear();enabled=c.enabled;
            viewport=c.pixelRect;view=c.worldToCameraMatrix;projection=c.projectionMatrix;target=c.targetTexture;
            width=target!=null?target.width:Screen.width;height=target!=null?target.height:Screen.height;
            foreach(var p in pages)p.Reset();
        }
    }
    public readonly struct Slot
    {
        public RenderTexture Texture {get;}
        public RectInt Rect {get;}
        public EntityId CameraID {get;}
        public int Frame {get;}
        public long Generation {get;}
        internal readonly long lease;
        internal Slot(RenderTexture texture,RectInt rect,EntityId cameraID,int frame,long generation,long lease)
        {Texture=texture;Rect=rect;CameraID=cameraID;Frame=frame;Generation=generation;this.lease=lease;}
    }
    static readonly Dictionary<EntityId,View> views=new Dictionary<EntityId,View>();
    static readonly List<EntityId> stale=new List<EntityId>();
    static long nextGeneration,nextLease;
    public static int PageCount {get{int n=0;foreach(var v in views.Values)n+=v.pages.Count;return n;}}
    public static long RuntimeBytes {get{long n=0;foreach(var v in views.Values)foreach(var p in v.pages)if(p.texture!=null)n+=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(p.texture);return n;}}
    public static int LastProducerDraws=>CoreGuardianMaskProducerBatch.LastDraws;
    public static bool Queue(in Slot slot,Mesh mesh,Material material,Matrix4x4 partMatrix,Matrix4x4 maskVP,Texture source,float opacity,float threshold,Vector2 viewport)
        =>CoreGuardianMaskProducerBatch.Queue(slot,mesh,material,partMatrix,maskVP,source,opacity,threshold,viewport);
    public static void Flush(Camera camera)=>CoreGuardianMaskProducerBatch.Flush(camera);
    internal static bool QueueShape(in Slot slot,CoreGuardianMaskProducerBatch.Shape shape,Material material,Matrix4x4 partMatrix,Matrix4x4 maskVP,Texture source,float opacity,float threshold,Vector2 viewport)
        // Called only by QueuePage immediately after TryAcquire, within one synchronous loop.
        =>CoreGuardianMaskProducerBatch.QueueShape(slot,shape,material,partMatrix,maskVP,source,opacity,threshold,viewport,true);
    static CoreGuardianMaskPage()
    {
        SceneManager.sceneUnloaded+=Unload;
        RenderPipelineManager.beginFrameRendering+=Prune;
        Application.quitting+=ReleaseAll;
#if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload+=ReleaseAll;
        UnityEditor.EditorApplication.playModeStateChanged+=state=>{if(state==UnityEditor.PlayModeStateChange.ExitingPlayMode)ReleaseAll();};
#endif
    }
    public static bool TryAcquire(Camera camera,int frame,int width,int height,out Slot slot)
    {
        slot=default;
        if(camera==null||width<=0||height<=0||width>Size-2*Padding||height>Size-2*Padding
            ||!supported)return false;
        Prune(default,null);
        var id=camera.GetEntityId();
        if(!views.TryGetValue(id,out var v)){v=new View();views.Add(id,v);v.Begin(camera,frame);}
        else if(v.camera!=camera||v.frame!=frame||!v.Matches(camera))v.Begin(camera,frame);
        foreach(var page in v.pages)if(page.Pack(width,height,out var rect))
        {slot=Make(v,page,rect,id);return true;}
        var texture=new RenderTexture(Size,Size,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear)
        {name="Core Guardian shared mask page",hideFlags=HideFlags.DontSave,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,useMipMap=false,autoGenerateMips=false,antiAliasing=1};
        if(!texture.Create()){Dispose(texture);return false;}
        var added=new Page{texture=texture};v.pages.Add(added);added.Pack(width,height,out var region);
        slot=Make(v,added,region,id);return true;
    }
    static Slot Make(View v,Page p,RectInt rect,EntityId id)
    {long lease=++nextLease;v.leases.Add(lease);return new Slot(p.texture,rect,id,v.frame,v.generation,lease);}
    public static bool IsValid(in Slot slot)=>slot.Texture!=null&&slot.Texture.IsCreated()
        &&views.TryGetValue(slot.CameraID,out var v)&&v.generation==slot.Generation&&v.frame==slot.Frame
        &&v.leases.Contains(slot.lease)&&v.Matches(v.camera);
    public static void Release(in Slot slot)
    {if(views.TryGetValue(slot.CameraID,out var v)&&v.generation==slot.Generation)v.leases.Remove(slot.lease);}
    public static void ReleaseCamera(Camera camera)
    {if(!ReferenceEquals(camera,null))ReleaseID(camera.GetEntityId());}
    static void ReleaseID(EntityId id)
    {if(!views.TryGetValue(id,out var v))return;CoreGuardianBodyMaskCache.Flush(v.camera);foreach(var p in v.pages){CoreGuardianMaskProducerBatch.ReleasePage(p.texture);p.texture.Release();Dispose(p.texture);}views.Remove(id);if(views.Count==0)CoreGuardianMaskProducerBatch.ReleaseAll();}
    static void Unload(Scene scene)
    {stale.Clear();foreach(var pair in views)if(pair.Value.camera==null||pair.Value.camera.gameObject.scene==scene)stale.Add(pair.Key);foreach(var id in stale)ReleaseID(id);}
    static void Prune(ScriptableRenderContext context,Camera[] cameras)
    {stale.Clear();foreach(var pair in views)if(pair.Value.camera==null||(pair.Value.enabled&&!pair.Value.camera.enabled))stale.Add(pair.Key);foreach(var id in stale)ReleaseID(id);}
    static void ReleaseAll(){CoreGuardianMaskProducerBatch.ReleaseAll();foreach(var v in views.Values)foreach(var p in v.pages){p.texture.Release();Dispose(p.texture);}views.Clear();}
    static void Dispose(Object value){if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);}
}
