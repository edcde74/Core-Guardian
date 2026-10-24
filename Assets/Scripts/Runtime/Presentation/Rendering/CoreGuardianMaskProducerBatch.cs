using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;

// Geometry keeps both original GPU matrix multiplies. Only the final viewport placement changes.
internal static class CoreGuardianMaskProducerBatch
{
    struct Draw
    {
        internal CoreGuardianMaskPage.Slot slot;
        internal Shape shape;
        internal Texture source;
        internal Material original;
        internal Matrix4x4 part,vp;
        internal float opacity,threshold;
        internal Vector2 viewport;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct Item
    {
        internal Vector4 p0,p1,p2,p3,v0,v1,v2,v3,region,alpha;
    }
    sealed class Group
    {
        internal RenderTexture page;
        internal Texture source;
        internal readonly List<Draw> draws=new List<Draw>();
        internal Mesh mesh;
        internal GraphicsBuffer buffer;
    }
    internal sealed class Shape
    {internal Vector3[] vertices;internal Vector2[] uv;internal int[] triangles;internal float layer;internal Mesh fallback;}
    static readonly List<Mesh> fallbackMeshes=new List<Mesh>();
    static readonly List<Draw> queued=new List<Draw>();
    static readonly List<Group> groups=new List<Group>();
    static readonly List<Item> items=new List<Item>();
    static readonly List<Vector3> vertices=new List<Vector3>();
    static readonly List<Vector2> uv=new List<Vector2>(),indices=new List<Vector2>();
    static readonly List<Vector4> sourceAtlas=new List<Vector4>();
    static readonly List<int> triangles=new List<int>();
    static readonly HashSet<RenderTexture> cleared=new HashSet<RenderTexture>();
    static readonly HashSet<long> validSlots=new HashSet<long>(),invalidSlots=new HashSet<long>();
    static readonly Dictionary<RenderTexture,long> clearedGenerations=new Dictionary<RenderTexture,long>();
    static readonly bool supported=SystemInfo.supportsComputeShaders&&(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Direct3D11||SystemInfo.graphicsDeviceType==GraphicsDeviceType.Direct3D12);
    static readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
    static readonly int ItemsId=Shader.PropertyToID("_Items"),PageSizeId=Shader.PropertyToID("_PageSize"),MaskAtlasId=Shader.PropertyToID("_MaskAtlas"),MainTexId=Shader.PropertyToID("_MainTex");
    static readonly int UseAtlasId=Shader.PropertyToID("_UseAtlas"),OpacityId=Shader.PropertyToID("_Opacity"),ThresholdId=Shader.PropertyToID("_Threshold"),MaskVPId=Shader.PropertyToID("_MaskVP"),UsePartMatricesId=Shader.PropertyToID("_UsePartMatrices");
    static readonly ProfilerMarker marker=new ProfilerMarker("CG.Mask.PageProducer");
    static Material material;
    static Shader sourceShader;
    static Material checkedSource;
    static CommandBuffer commands;
    internal static int LastDraws {get;private set;}
    internal static bool Supported=>supported;
    internal static bool Queue(in CoreGuardianMaskPage.Slot slot,Mesh mesh,Material original,Matrix4x4 part,Matrix4x4 vp,Texture source,float opacity,float threshold,Vector2 viewport)
    {
        if(mesh==null||!mesh.isReadable)return false;
        mesh.GetUVs(5,sourceAtlas);
        var shape=new Shape{vertices=mesh.vertices,uv=mesh.uv,triangles=mesh.triangles,layer=sourceAtlas.Count>0?sourceAtlas[0].x:0};
        return QueueShape(slot,shape,original,part,vp,source,opacity,threshold,viewport);
    }
    internal static bool QueueShape(in CoreGuardianMaskPage.Slot slot,Shape shape,Material original,Matrix4x4 part,Matrix4x4 vp,Texture source,float opacity,float threshold,Vector2 viewport,bool slotValidated=false)
    {
        if(sourceShader==null)sourceShader=Resources.Load<Shader>("CoreGuardian/SelectionMask");
        if((!slotValidated&&!CoreGuardianMaskPage.IsValid(slot))||shape==null||original==null||source==null
            ||(original!=checkedSource&&original.shader!=sourceShader)||!supported
            ||viewport.x<=0||viewport.y<=0||viewport.x>slot.Rect.width||viewport.y>slot.Rect.height)return false;
        if(material==null)
        {
            var shader=Resources.Load<Shader>("CoreGuardian/BatchedSelectionMask");if(shader==null||!shader.isSupported)return false;
            material=new Material(shader){name="Shared mask page producer",hideFlags=HideFlags.DontSave};
        }
        checkedSource=original;
        queued.Add(new Draw{slot=slot,shape=shape,original=original,part=part,vp=vp,source=source,opacity=opacity,threshold=threshold,viewport=viewport});return true;
    }
    internal static void Flush(Camera camera,bool discard=true)
    {
        using var scope=marker.Auto();LastDraws=0;if(camera==null)return;
        var id=camera.GetEntityId();int used=0;foreach(var g in groups)g.draws.Clear();validSlots.Clear();invalidSlots.Clear();
        foreach(var draw in queued)
        {
            if(draw.slot.CameraID!=id||draw.shape==null||draw.source==null)continue;
            if(!validSlots.Contains(draw.slot.lease))
            {
                if(invalidSlots.Contains(draw.slot.lease))continue;
                if(!CoreGuardianMaskPage.IsValid(draw.slot)){invalidSlots.Add(draw.slot.lease);continue;}
                validSlots.Add(draw.slot.lease);
            }
            int index=0;for(;index<used;index++)if(groups[index].page==draw.slot.Texture&&groups[index].source==draw.source)break;
            if(index==used){if(used==groups.Count)groups.Add(new Group());groups[index].page=draw.slot.Texture;groups[index].source=draw.source;used++;}
            groups[index].draws.Add(draw);
        }
        if(used==0){if(discard)Discard(camera);return;}
        if(commands==null)commands=new CommandBuffer{name="Core Guardian merged mask pages"};commands.Clear();cleared.Clear();
        for(int g=0;g<used;g++)
        {
            var group=groups[g];items.Clear();vertices.Clear();uv.Clear();indices.Clear();triangles.Clear();
            foreach(var draw in group.draws)
            {
                int item=items.Count,offset=vertices.Count;var r=draw.slot.Rect;
                items.Add(new Item{p0=draw.part.GetRow(0),p1=draw.part.GetRow(1),p2=draw.part.GetRow(2),p3=draw.part.GetRow(3),v0=draw.vp.GetRow(0),v1=draw.vp.GetRow(1),v2=draw.vp.GetRow(2),v3=draw.vp.GetRow(3),region=new Vector4(r.x,r.y,draw.viewport.x,draw.viewport.y),alpha=new Vector4(draw.opacity,draw.threshold,0,0)});
                var shape=draw.shape;
                for(int v=0;v<shape.vertices.Length;v++){vertices.Add(shape.vertices[v]);uv.Add(shape.uv[v]);indices.Add(new Vector2(item,shape.layer));}
                foreach(int triangle in shape.triangles)triangles.Add(offset+triangle);
            }
            if(group.mesh==null){group.mesh=new Mesh{name="Page mask geometry",hideFlags=HideFlags.DontSave,indexFormat=IndexFormat.UInt32};group.mesh.MarkDynamic();}
            group.mesh.Clear();group.mesh.SetVertices(vertices);group.mesh.SetUVs(0,uv);group.mesh.SetUVs(1,indices);group.mesh.SetTriangles(triangles,0,false);
            if(group.buffer==null||group.buffer.count<items.Count){group.buffer?.Dispose();group.buffer=new GraphicsBuffer(GraphicsBuffer.Target.Structured,Mathf.NextPowerOfTwo(items.Count),160);}
            group.buffer.SetData(items);
            commands.SetRenderTarget(group.page);
            if(cleared.Add(group.page)&&(!clearedGenerations.TryGetValue(group.page,out var generation)||generation!=group.draws[0].slot.Generation))commands.ClearRenderTarget(false,true,Color.clear);
            commands.SetViewport(new Rect(0,0,group.page.width,group.page.height));
            properties.Clear();properties.SetBuffer(ItemsId,group.buffer);properties.SetVector(PageSizeId,new Vector4(group.page.width,group.page.height,0,0));
            bool array=group.source is Texture2DArray;properties.SetTexture(array?MaskAtlasId:MainTexId,group.source);properties.SetFloat(UseAtlasId,array?1:0);
            commands.DrawMesh(group.mesh,Matrix4x4.identity,material,0,0,properties);LastDraws++;
        }
        Graphics.ExecuteCommandBuffer(commands);
        for(int g=0;g<used;g++)clearedGenerations[groups[g].page]=groups[g].draws[0].slot.Generation;
        if(discard)Discard(camera);
    }
    internal static void Discard(Camera camera)
    {
        // Same order-preserving filter as RemoveAll, without allocating a capturing predicate per Flush.
        var id=camera.GetEntityId();int write=0;
        for(int read=0;read<queued.Count;read++){var draw=queued[read];if(draw.slot.CameraID==id||!CoreGuardianMaskPage.IsValid(draw.slot))continue;queued[write++]=draw;}
        if(write<queued.Count)queued.RemoveRange(write,queued.Count-write);
    }
    internal static void ReleasePage(RenderTexture page)=>clearedGenerations.Remove(page);
    internal static void RenderOriginal(in CoreGuardianMaskPage.Slot slot,RenderTexture target)
    {
        if(commands==null)commands=new CommandBuffer{name="Core Guardian mask fallback"};commands.Clear();commands.SetRenderTarget(target);commands.ClearRenderTarget(false,true,Color.clear);
        foreach(var draw in queued)
        {
            if(draw.slot.lease!=slot.lease||draw.shape==null||draw.original==null||draw.source==null)continue;
            var shape=draw.shape;
            if(shape.fallback==null)
            {
                shape.fallback=new Mesh{name="Legacy queued mask fallback",hideFlags=HideFlags.DontSave,indexFormat=IndexFormat.UInt32};
                shape.fallback.vertices=shape.vertices;shape.fallback.uv=shape.uv;shape.fallback.triangles=shape.triangles;
                sourceAtlas.Clear();for(int i=0;i<shape.vertices.Length;i++)sourceAtlas.Add(new Vector4(shape.layer,0,0,0));shape.fallback.SetUVs(5,sourceAtlas);fallbackMeshes.Add(shape.fallback);
            }
            commands.SetViewport(new Rect(0,0,draw.viewport.x,draw.viewport.y));properties.Clear();
            bool array=draw.source is Texture2DArray;properties.SetTexture(array?MaskAtlasId:MainTexId,draw.source);properties.SetFloat(UseAtlasId,array?1:0);
            properties.SetFloat(OpacityId,draw.opacity);properties.SetFloat(ThresholdId,draw.threshold);properties.SetMatrix(MaskVPId,draw.vp);properties.SetFloat(UsePartMatricesId,0);
            commands.DrawMesh(shape.fallback,draw.part,draw.original,0,0,properties);
        }
        Graphics.ExecuteCommandBuffer(commands);
    }
    internal static void ReleaseAll()
    {queued.Clear();clearedGenerations.Clear();foreach(var mesh in fallbackMeshes)if(mesh!=null){if(Application.isPlaying)Object.Destroy(mesh);else Object.DestroyImmediate(mesh);}fallbackMeshes.Clear();foreach(var group in groups){group.buffer?.Dispose();if(group.mesh!=null){if(Application.isPlaying)Object.Destroy(group.mesh);else Object.DestroyImmediate(group.mesh);}}groups.Clear();commands?.Dispose();commands=null;if(material!=null){if(Application.isPlaying)Object.Destroy(material);else Object.DestroyImmediate(material);}material=null;checkedSource=null;}
}
