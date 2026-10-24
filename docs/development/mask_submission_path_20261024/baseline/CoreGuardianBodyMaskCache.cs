using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Profiling;

// Only completed binary body masks are shared. Output geometry/material properties stay per consumer.
internal static class CoreGuardianBodyMaskCache
{
    static readonly ProfilerMarker lookupSample=new ProfilerMarker("CG.Mask.CacheLookup");
    static readonly ProfilerMarker flushSample=new ProfilerMarker("CG.Mask.CacheFlush");
    static readonly ProfilerMarker copySample=new ProfilerMarker("CG.Mask.CopyBack");
    internal static bool DiagnosticEnabled;
    internal static readonly Dictionary<string,long> DiagnosticCounts=new Dictionary<string,long>();
    internal static void ResetDiagnostics(){DiagnosticCounts.Clear();DiagnosticEnabled=false;}
    static void Count(string name){DiagnosticCounts.TryGetValue(name,out long n);DiagnosticCounts[name]=n+1;}
    sealed class ArtworkIdentity:IEqualityComparer<Transform>
    {
        public bool Equals(Transform a,Transform b)=>ReferenceEquals(a,b);
        public int GetHashCode(Transform value)=>System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
    }
    static readonly Dictionary<Transform,Entry> entries=new Dictionary<Transform,Entry>(new ArtworkIdentity());
    static readonly HashSet<Lease> active=new HashSet<Lease>();
    static readonly List<Lease> resetScratch=new List<Lease>();
    static long nextEpoch;
    static readonly List<Record> pending=new List<Record>();
    static UnityEngine.Rendering.CommandBuffer pendingCopies;
    internal static long Hits,Generations,Allocations,Releases;
    internal static int EntryCount=>entries.Count;
    internal static int BindingCount=>active.Count;
    internal static int AllocatedCount {get{int n=0;foreach(var e in entries.Values)n+=e.records.Count;return n;}}
    internal static long AllocatedBytes {get{long n=0;foreach(var e in entries.Values)foreach(var r in e.records)if(r.texture!=null)n+=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(r.texture);return n;}}

    internal sealed class Lease
    {
        internal readonly CoreGuardianSelectionOutline owner;
        internal Record current;
        internal bool diagnosticEligible;
        internal readonly List<Part> scratch=new List<Part>();
        internal Lease(CoreGuardianSelectionOutline owner){this.owner=owner;}
    }
    internal readonly struct Request
    {
        internal readonly Transform artwork;
        internal readonly Camera camera;
        internal readonly int frame;
        internal readonly float threshold;
        internal readonly Matrix4x4 view,projection;
        internal readonly Rect viewport,rect;
        internal readonly RenderTexture target;
        internal readonly int targetWidth,targetHeight,targetAA;
        internal readonly UnityEngine.Experimental.Rendering.GraphicsFormat targetFormat;
        internal readonly CoreGuardianMaskAtlas atlas;
        internal readonly int atlasRevision;
        internal readonly List<SpriteRenderer> sources;
        internal readonly bool shareable;
        internal Request(Transform artwork,Camera camera,float threshold,Rect rect,CoreGuardianMaskAtlas atlas,List<SpriteRenderer> sources)
        {
            this.artwork=artwork;this.camera=camera;this.threshold=threshold;this.rect=rect;this.atlas=atlas;this.sources=sources;
            frame=Time.frameCount;view=camera.worldToCameraMatrix;projection=camera.projectionMatrix;viewport=camera.pixelRect;
            target=camera.targetTexture;targetWidth=target!=null?target.width:Screen.width;targetHeight=target!=null?target.height:Screen.height;
            targetAA=target!=null?target.antiAliasing:QualitySettings.antiAliasing;targetFormat=target!=null?target.graphicsFormat:UnityEngine.Experimental.Rendering.GraphicsFormat.None;
            atlasRevision=atlas!=null?atlas.Revision:-1;
            shareable=camera.orthographic&&!camera.stereoEnabled&&!camera.allowDynamicResolution&&rect.x==Mathf.Round(rect.x)&&rect.y==Mathf.Round(rect.y);
            shareable=shareable&&viewport.xMin==Mathf.Round(viewport.xMin)&&viewport.yMin==Mathf.Round(viewport.yMin)
                &&viewport.xMax==Mathf.Round(viewport.xMax)&&viewport.yMax==Mathf.Round(viewport.yMax);
        }
    }
    internal readonly struct Sample
    {
        internal readonly RenderTexture Texture;
        internal readonly Rect ProducerRect;
        internal readonly long GeometryEpoch;
        internal Sample(Record record){Texture=record.texture;ProducerRect=record.request.rect;GeometryEpoch=record.epoch;}
    }
    internal sealed class Entry
    {
        internal Transform artwork;
        internal Scene scene;
        internal int users;
        internal long epoch;
        internal readonly List<Record> records=new List<Record>();
    }
    internal sealed class Record
    {
        internal Entry entry;
        internal Request request;
        internal long epoch;
        internal int users;
        internal bool valid;
        internal bool pending;
        internal CoreGuardianMaskPage.Slot page;
        internal RenderTexture texture;
        internal readonly List<Part> parts=new List<Part>();
    }
    internal struct Part:IEquatable<Part>
    {
        SpriteRenderer renderer;
        Sprite sprite;
        Texture source,mapped;
        uint sourceVersion,mappedVersion;
        int layer;
        float alpha;
        bool flipX,flipY;
        Matrix4x4 matrix;
        internal Part(SpriteRenderer r,CoreGuardianMaskAtlas atlas)
        {
            renderer=r;sprite=r.sprite;source=sprite.texture;sourceVersion=source.updateCount;
            mapped=source;layer=-1;
            if(atlas!=null&&atlas.TryGetSprite(sprite,out var item,out _)){mapped=item.page;layer=item.layer;}
            mappedVersion=mapped.updateCount;alpha=r.color.a;flipX=r.flipX;flipY=r.flipY;matrix=r.transform.localToWorldMatrix;
        }
        public bool Equals(Part b)=>ReferenceEquals(renderer,b.renderer)&&ReferenceEquals(sprite,b.sprite)&&ReferenceEquals(source,b.source)&&ReferenceEquals(mapped,b.mapped)&&sourceVersion==b.sourceVersion&&mappedVersion==b.mappedVersion&&layer==b.layer&&alpha.Equals(b.alpha)&&flipX==b.flipX&&flipY==b.flipY&&matrix.Equals(b.matrix);
        internal string Difference(Part b)
        {
            if(!ReferenceEquals(renderer,b.renderer)||!ReferenceEquals(sprite,b.sprite))return "sprite";
            if(!ReferenceEquals(source,b.source)||!ReferenceEquals(mapped,b.mapped)||sourceVersion!=b.sourceVersion||mappedVersion!=b.mappedVersion||layer!=b.layer)return "texture";
            if(!alpha.Equals(b.alpha))return "alpha";
            if(flipX!=b.flipX||flipY!=b.flipY||!matrix.Equals(b.matrix))return "pose";
            return "equal";
        }
    }

    static CoreGuardianBodyMaskCache()
    {
        SceneManager.sceneUnloaded+=SceneUnloaded;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged+=state=>
        {
            if(state==UnityEditor.PlayModeStateChange.ExitingEditMode||state==UnityEditor.PlayModeStateChange.ExitingPlayMode)ResetOwned();
        };
#endif
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void NewSession(){ResetOwned();}
    static void SceneUnloaded(Scene scene){Reset(scene);}
    internal static void ResetOwned(){Reset(null);}
    static void Reset(Scene? scene)
    {
        resetScratch.Clear();foreach(var lease in active)if(!scene.HasValue||lease.current.entry.scene==scene.Value)resetScratch.Add(lease);
        foreach(var lease in resetScratch){if(lease.owner!=null)lease.owner.ReleasePresentation();else Release(lease);}
        resetScratch.Clear();
    }
    static bool Compatible(Record record,in Request request,List<Part> parts)
    {
        var a=record.request;
        if((!record.valid&&!(record.pending&&CoreGuardianMaskPage.IsValid(record.page)))||record.epoch!=record.entry.epoch||record.texture==null||!record.texture.IsCreated()||!request.shareable||!a.shareable)return false;
        if(a.frame!=request.frame||!ReferenceEquals(a.camera,request.camera)||!a.threshold.Equals(request.threshold)||!a.view.Equals(request.view)||!a.projection.Equals(request.projection)||!a.viewport.Equals(request.viewport))return false;
        if(!ReferenceEquals(a.target,request.target)||a.targetWidth!=request.targetWidth||a.targetHeight!=request.targetHeight||a.targetAA!=request.targetAA||a.targetFormat!=request.targetFormat||!ReferenceEquals(a.atlas,request.atlas)||a.atlasRevision!=request.atlasRevision)return false;
        // Different crop matrices can rasterize point-sampled sprite edges differently, even near the origin.
        if(!a.rect.Equals(request.rect)||record.parts.Count!=parts.Count)return false;
        for(int i=0;i<parts.Count;i++)if(!record.parts[i].Equals(parts[i]))return false;
        return true;
    }
    internal static string CompletedReuseReason(Record record,in Request request,List<Part> parts)
    {
        if(record==null||!record.valid||record.pending||record.epoch!=record.entry.epoch||record.texture==null||!record.texture.IsCreated()||!request.shareable||!record.request.shareable)return "invalid";
        var a=record.request;
        if(!ReferenceEquals(a.camera,request.camera)||!a.view.Equals(request.view)||!a.projection.Equals(request.projection)||!a.viewport.Equals(request.viewport)
            ||!ReferenceEquals(a.target,request.target)||a.targetWidth!=request.targetWidth||a.targetHeight!=request.targetHeight||a.targetAA!=request.targetAA||a.targetFormat!=request.targetFormat)return "camera";
        if(!a.threshold.Equals(request.threshold))return "threshold";
        if(!ReferenceEquals(a.atlas,request.atlas)||a.atlasRevision!=request.atlasRevision)return "texture";
        if(!a.rect.Equals(request.rect))return "crop";
        if(record.parts.Count!=parts.Count)return "sprite";
        for(int i=0;i<parts.Count;i++){var reason=record.parts[i].Difference(parts[i]);if(reason!="equal")return reason;}
        return "equal";
    }
    internal static bool Begin(Lease lease,in Request request,out Sample sample)
    {
        using var scope=lookupSample.Auto();
        lease.scratch.Clear();foreach(var source in request.sources)lease.scratch.Add(new Part(source,request.atlas));
        lease.diagnosticEligible=false;
        if(DiagnosticEnabled)
        {
            var animation=request.artwork.GetComponentInParent<CoreGuardianUnitAnimation>();
            string role=animation!=null?animation.PresentationRole:request.artwork.parent!=null?request.artwork.parent.name:"unknown";
            string reason=CompletedReuseReason(lease.current,request,lease.scratch);lease.diagnosticEligible=reason=="equal";
            Count(role+":"+reason);Count("requests");
        }
        if(!entries.TryGetValue(request.artwork,out var entry))
        {
            entry=new Entry{artwork=request.artwork,scene=request.artwork.gameObject.scene,epoch=++nextEpoch};entries.Add(request.artwork,entry);
        }
        foreach(var candidate in entry.records)
            if(Compatible(candidate,request,lease.scratch)){Bind(lease,candidate);Hits++;if(DiagnosticEnabled)Count("sameFrameHits");sample=new Sample(candidate);return true;}
        var record=lease.current;
        if(record==null||record.entry!=entry||record.users!=1)record=null;
        if(record==null)foreach(var candidate in entry.records)if(candidate.users==0){record=candidate;break;}
        if(record==null){record=new Record{entry=entry};entry.records.Add(record);}
        Bind(lease,record);CoreGuardianMaskPage.Release(record.page);record.page=default;record.pending=false;record.valid=false;record.request=request;record.epoch=entry.epoch;
        record.parts.Clear();record.parts.AddRange(lease.scratch);
        int width=Mathf.CeilToInt(request.rect.width),height=Mathf.CeilToInt(request.rect.height);
        if(record.texture!=null&&(record.texture.width<width||record.texture.height<height))
        {
            width=Mathf.Max(width,record.texture.width);height=Mathf.Max(height,record.texture.height);DestroyTexture(record);
        }
        if(record.texture==null)
        {
            record.texture=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear)
            {name="Selected unit union mask",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontSave};Allocations++;
        }
        if(!record.texture.IsCreated()&&!record.texture.Create()){Abort(lease);throw new InvalidOperationException("Body mask RT creation failed");}
        if(DiagnosticEnabled)Count("generations");sample=new Sample(record);return false;
    }
    static void Bind(Lease lease,Record record)
    {
        if(ReferenceEquals(lease.current,record))return;
        var old=lease.current;record.users++;record.entry.users++;lease.current=record;active.Add(lease);
        if(old!=null)Return(old);
    }
    internal static void Publish(Lease lease){if(lease.current==null)return;lease.current.valid=true;Generations++;}
    internal static void MarkPending(Lease lease,in CoreGuardianMaskPage.Slot slot)
    {var record=lease.current;record.valid=false;record.pending=true;record.page=slot;}
    internal static bool PreparedPage(Lease lease,out CoreGuardianMaskPage.Slot slot)
    {slot=lease?.current?.page??default;return CoreGuardianMaskPage.IsValid(slot);}
    internal static bool HasPage(Camera camera)
    {foreach(var entry in entries.Values)foreach(var record in entry.records)if(record.users>0&&record.request.camera==camera&&CoreGuardianMaskPage.IsValid(record.page))return true;return false;}
    internal static void Flush(Camera camera)
    {
        using var flushScope=flushSample.Auto();
        pending.Clear();foreach(var entry in entries.Values)foreach(var record in entry.records)if(record.pending&&record.users>0&&record.request.camera==camera)pending.Add(record);
        if(pending.Count==0)return;
        try
        {
            foreach(var record in pending)if(!CoreGuardianMaskPage.IsValid(record.page)){CoreGuardianMaskProducerBatch.RenderOriginal(record.page,record.texture);record.pending=false;record.valid=true;Generations++;}
            CoreGuardianMaskProducerBatch.Flush(camera,false);
            using(copySample.Auto())
            {
            if(pendingCopies==null)pendingCopies=new UnityEngine.Rendering.CommandBuffer{name="Completed shared masks to legacy consumers"};pendingCopies.Clear();
            foreach(var record in pending)if(record.pending)
            {var r=record.page.Rect;pendingCopies.CopyTexture(record.page.Texture,0,0,r.x,r.y,r.width,r.height,record.texture,0,0,0,0);if(DiagnosticEnabled)Count("copies");}
            Graphics.ExecuteCommandBuffer(pendingCopies);
            }
            foreach(var record in pending)if(record.pending){record.pending=false;record.valid=true;Generations++;}
        }
        catch(System.Exception e)
        {
            foreach(var record in pending)if(record.pending)
            {CoreGuardianMaskProducerBatch.RenderOriginal(record.page,record.texture);CoreGuardianMaskPage.Release(record.page);record.page=default;record.pending=false;record.valid=true;Generations++;}
            Debug.LogWarning("Shared mask producer restored original rendering: "+e.Message);
        }
        finally{CoreGuardianMaskProducerBatch.Discard(camera);pending.Clear();}
    }
    internal static void Abort(Lease lease){if(lease.current!=null)lease.current.valid=false;Release(lease);}
    internal static void Release(Lease lease)
    {
        if(lease==null||lease.current==null)return;
        var record=lease.current;lease.current=null;active.Remove(lease);Return(record);
    }
    static void Return(Record record)
    {
        var entry=record.entry;record.users--;entry.users--;
        if(record.users==0){CoreGuardianMaskPage.Release(record.page);record.page=default;record.pending=false;}
        if(entry.users==0)
        {
            foreach(var value in entry.records)DestroyTexture(value);entry.records.Clear();entries.Remove(entry.artwork);
            if(entries.Count==0){CoreGuardianMaskPage.ReleaseCamera(record.request.camera);pendingCopies?.Dispose();pendingCopies=null;}return;
        }
        bool spare=false;
        for(int i=entry.records.Count-1;i>=0;i--)
        {
            var value=entry.records[i];if(value.users!=0)continue;
            if(!spare){spare=true;continue;}DestroyTexture(value);entry.records.RemoveAt(i);
        }
    }
    internal static void Invalidate(Transform artwork)
    {
        if(artwork!=null&&entries.TryGetValue(artwork,out var entry)){entry.epoch=++nextEpoch;foreach(var r in entry.records)r.valid=false;}
    }
    static void DestroyTexture(Record record)
    {
        CoreGuardianMaskPage.Release(record.page);record.page=default;record.pending=false;
        if(record.texture==null)return;record.texture.Release();
        if(Application.isPlaying)UnityEngine.Object.Destroy(record.texture);else UnityEngine.Object.DestroyImmediate(record.texture);
        record.texture=null;Releases++;
    }
}

// Manual Present also works on a disabled consumer. Its active output child observes host deactivation.
[ExecuteAlways]
internal sealed class CoreGuardianBodyMaskLifetime:MonoBehaviour
{
    internal CoreGuardianSelectionOutline owner;
    void OnDisable(){if(owner!=null)owner.ReleasePresentation();}
}
