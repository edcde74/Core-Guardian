using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;

// One binary union of visible original sprite parts. Never changes source materials or artwork.
[DisallowMultipleComponent]
[DefaultExecutionOrder(500)]
public sealed class CoreGuardianSelectionOutline : MonoBehaviour
{
    static readonly ProfilerMarker presentSample=new ProfilerMarker("CG.Mask.Present");
    static readonly ProfilerMarker resourcesSample=new ProfilerMarker("CG.Mask.Resources");
    static readonly ProfilerMarker partsSample=new ProfilerMarker("CG.Mask.Parts");
    static readonly ProfilerMarker uploadSample=new ProfilerMarker("CG.Mask.MeshUpload");
    static readonly ProfilerMarker executeSample=new ProfilerMarker("CG.Mask.Execute");
    static readonly ProfilerMarker outputSample=new ProfilerMarker("CG.Mask.Output");
    static Shader contactShader;
    [Header("선택 대상 아웃라인 · 능력치/강화 패널이 열려 있을 때만 표시")]
    [Tooltip("외곽선 색상과 불투명도. 발광이나 깜빡임은 사용하지 않습니다.")]
    public Color color=new Color(.8f,.67f,.39f,1);
    [Range(0,8),Tooltip("게임 화면 기준 픽셀 두께. 0이면 숨깁니다.")]
    public int widthPixels=2;
    [Range(.01f,1),Tooltip("이 값 이상의 본체 알파만 실루엣에 포함합니다.")]
    public float alphaThreshold=.1f;
    public bool IsVisible=>output!=null&&output.enabled;
    public Transform CurrentTarget {get;private set;}
    public Bounds TargetBounds {get;private set;}

    CoreGuardianHud hud;
    Material maskMaterial,outlineMaterial;
    CoreGuardianSharedMaskMaterial maskLease,outlineLease;
    MaterialPropertyBlock outputProperties;
    Mesh quad;
    MeshRenderer output;
    RenderTexture mask;
    CoreGuardianBodyMaskCache.Lease maskBinding;
    Vector2 maskOriginPx;
    long maskGeometryEpoch=-1;
    CommandBuffer commands;
    readonly Dictionary<Sprite,Mesh> meshes=new Dictionary<Sprite,Mesh>();
    readonly Dictionary<Sprite,MaskGeometry> geometry=new Dictionary<Sprite,MaskGeometry>();
    readonly List<MaskBatch> batches=new List<MaskBatch>();
    int lastMaskDrawCalls,lastAtlasMisses;
    CoreGuardianMaskAtlas atlas;
    CoreGuardianMaskAtlas cachedAtlas;
    int atlasRevision=-1;
    MaterialPropertyBlock properties;
    readonly List<SpriteRenderer> sources=new List<SpriteRenderer>();
    readonly CoreGuardianSpriteReferences bodySources=new CoreGuardianSpriteReferences();
    readonly Vector3[] quadVertices=new Vector3[4];
    readonly Vector2[] quadUV=new Vector2[4];
    Transform artwork,lastTarget;
    bool pageRasterSupported;

    void LateUpdate()
    {
        if(hud==null)hud=GetComponent<CoreGuardianHud>();
        Present(hud!=null?hud.InspectionTarget:null,Camera.main);
    }
    // Reuse the same animated-body union for a transient overlay without changing the sources.
    // A dedicated disabled instance is owned by the upgrade effect; the HUD outline stays separate.
    public bool PresentOverlay(Transform target,Camera camera,Material material)
    {
        PresentMask(target,camera,material==null);
        if(!IsVisible||material==null)return false;
        SetOutputMask();output.SetPropertyBlock(outputProperties);
        output.sharedMaterial=material;output.sortingOrder++;
        return true;
    }
    public void Present(Transform target,Camera camera)
    {
        PresentMask(target,camera,true);
        RegisterOutput(camera,false,0);
    }
    void PresentMask(Transform target,Camera camera,bool writeOutlineProperties,bool compactContact=false)
    {
        using var presentScope=presentSample.Auto();
        CoreGuardianMaskOutputBatch.Invalidate(this);
        lastMaskDrawCalls=0;lastAtlasMisses=0;
        CurrentTarget=target;
        if(output!=null)output.enabled=false;
        if(target==null||camera==null||widthPixels<=0||color.a<=0){ReleasePresentation();return;}
        if(target!=lastTarget||artwork==null)
        {
            lastTarget=target;
            var animation=target.GetComponent<CoreGuardianUnitAnimation>();
            // These two rigs still show a native-zoom pixel difference under atlas rasterization.
            // Keep their exact legacy mask production until separately validated.
            pageRasterSupported=animation==null||(animation.PresentationRole!="Defense"&&animation.PresentationRole!="Boss");
            artwork=target.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r=>r.name=="2D Sprite")?.transform;
        }
        if(artwork==null){ReleasePresentation();return;}
        sources.Clear();
        foreach(var r in bodySources.Read(artwork))
            if(r!=null&&r.enabled&&r.sprite!=null&&r.color.a>=alphaThreshold&&r.gameObject.activeInHierarchy)sources.Add(r);
        if(sources.Count==0){ReleasePresentation();return;}
        EnsureResources();
        var bounds=sources[0].bounds;foreach(var r in sources)bounds.Encapsulate(r.bounds);
        TargetBounds=bounds;
        var lo=new Vector2(float.PositiveInfinity,float.PositiveInfinity);var hi=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
        for(int i=0;i<8;i++)
        {
            var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
            var screen=camera.WorldToScreenPoint(corner);if(screen.z<=0){ReleasePresentation();return;}
            lo=Vector2.Min(lo,screen);hi=Vector2.Max(hi,screen);
        }
        int radius=Mathf.Clamp(widthPixels,0,8),pad=radius+2;
        var viewport=camera.pixelRect;
        var bodyLo=lo;var bodyHi=hi;
        lo=new Vector2(Mathf.Max(viewport.xMin-pad,Mathf.Floor(lo.x)-pad),Mathf.Max(viewport.yMin-pad,Mathf.Floor(lo.y)-pad));
        hi=new Vector2(Mathf.Min(viewport.xMax+pad,Mathf.Ceil(hi.x)+pad),Mathf.Min(viewport.yMax+pad,Mathf.Ceil(hi.y)+pad));
        int w=Mathf.CeilToInt(hi.x-lo.x),h=Mathf.CeilToInt(hi.y-lo.y);if(w<=0||h<=0){ReleasePresentation();return;}
        // The contact band can read the same pad3 crop as emphasis. Keep its original pad4 output quad.
        // Outside the clipped viewport, the original output padding remains outside the camera too.
        var maskLo=lo;var maskHi=hi;
        bool localContact=compactContact&&camera.orthographic&&!camera.stereoEnabled&&!camera.allowDynamicResolution
            // Enable the alternate contact crop only for the gameplay views covered by native pixel QA.
            &&viewport.width==1920&&viewport.height==1080&&(camera.orthographicSize==8.5f||camera.orthographicSize==4.25f)
            &&radius==2&&viewport.x==0&&viewport.y==0&&camera.transform.rotation.Equals(Quaternion.identity)&&DefaultProjection(camera)
            &&Mathf.Abs(camera.transform.position.x)<=32&&Mathf.Abs(camera.transform.position.y)<=32
            &&Mathf.Abs(artwork.position.x)<=32&&Mathf.Abs(artwork.position.y)<=32
            &&viewport.xMin==Mathf.Round(viewport.xMin)&&viewport.yMin==Mathf.Round(viewport.yMin)
            &&viewport.xMax==Mathf.Round(viewport.xMax)&&viewport.yMax==Mathf.Round(viewport.yMax);
        if(localContact&&radius>1)
        {
            maskLo=new Vector2(Mathf.Max(viewport.xMin-3,Mathf.Floor(bodyLo.x)-3),Mathf.Max(viewport.yMin-3,Mathf.Floor(bodyLo.y)-3));
            maskHi=new Vector2(Mathf.Min(viewport.xMax+3,Mathf.Ceil(bodyHi.x)+3),Mathf.Min(viewport.yMax+3,Mathf.Ceil(bodyHi.y)+3));
        }
        int maskWidth=Mathf.CeilToInt(maskHi.x-maskLo.x),maskHeight=Mathf.CeilToInt(maskHi.y-maskLo.y);
        if(maskWidth<=0||maskHeight<=0){maskLo=lo;maskHi=hi;maskWidth=w;maskHeight=h;}
        var request=new CoreGuardianBodyMaskCache.Request(artwork,camera,alphaThreshold,new Rect(maskLo.x,maskLo.y,maskWidth,maskHeight),atlas,sources);
        CoreGuardianBodyMaskCache.Sample sample;
        try
        {
            bool hit=CoreGuardianBodyMaskCache.Begin(maskBinding,request,out sample);mask=sample.Texture;
            if(!hit)
            {
                if(maskGeometryEpoch!=sample.GeometryEpoch){ClearGeometry();maskGeometryEpoch=sample.GeometryEpoch;}
                var a=(maskLo-viewport.position)/viewport.size*2-Vector2.one;
                var b=(maskHi-viewport.position)/viewport.size*2-Vector2.one;
                var crop=Matrix4x4.identity;crop.m00=2/(b.x-a.x);crop.m11=2/(b.y-a.y);crop.m03=-(b.x+a.x)/(b.x-a.x);crop.m13=-(b.y+a.y)/(b.y-a.y);
                var vp=GL.GetGPUProjectionMatrix(crop*camera.projectionMatrix,true)*camera.worldToCameraMatrix;
                if(!QueuePage(camera,vp,maskWidth,maskHeight))
                {
                    commands.Clear();commands.SetRenderTarget(mask);commands.ClearRenderTarget(false,true,Color.clear);
                    commands.SetViewport(new Rect(0,0,maskWidth,maskHeight));DrawMaskParts(vp);
                    using(executeSample.Auto())Graphics.ExecuteCommandBuffer(commands);
                    CoreGuardianBodyMaskCache.Publish(maskBinding);
                }
            }
        }
        catch{CoreGuardianBodyMaskCache.Abort(maskBinding);mask=null;throw;}
        maskOriginPx=lo-sample.ProducerRect.position;
        using var outputScope=outputSample.Auto();
        float depth=Vector3.Dot(bounds.center-camera.transform.position,camera.transform.forward);
        quadVertices[0]=Point(lo.x,lo.y);quadVertices[1]=Point(hi.x,lo.y);quadVertices[2]=Point(hi.x,hi.y);quadVertices[3]=Point(lo.x,hi.y);
        quad.vertices=quadVertices;quad.RecalculateBounds();
        // One texture texel remains one screen pixel, including the contact shader's lower band.
        float u=(float)w/mask.width,v=(float)h/mask.height;
        var origin=new Vector2(maskOriginPx.x/mask.width,maskOriginPx.y/mask.height);
        quadUV[0]=origin;quadUV[1]=origin+new Vector2(u,0);quadUV[2]=origin+new Vector2(u,v);quadUV[3]=origin+new Vector2(0,v);quad.uv=quadUV;
        Vector3 Point(float x,float y)=>output.transform.InverseTransformPoint(camera.ScreenToWorldPoint(new Vector3(x,y,depth)));
        var front=sources[0];int frontLayer=SortingLayer.GetLayerValueFromID(front.sortingLayerID);
        foreach(var r in sources)
        {
            int layer=SortingLayer.GetLayerValueFromID(r.sortingLayerID);
            // >= preserves the previous stable OrderBy(...).ThenBy(...).Last() tie behavior.
            if(layer>frontLayer||(layer==frontLayer&&r.sortingOrder>=front.sortingOrder)){front=r;frontLayer=layer;}
        }
        output.sortingLayerID=front.sortingLayerID;output.sortingOrder=front.sortingOrder+4;
        if(writeOutlineProperties)
        {
            SetOutputMask();outputProperties.SetColor("_Color",color);outputProperties.SetInt("_Width",radius);
            output.SetPropertyBlock(outputProperties);
        }
        output.sharedMaterial=outlineMaterial;
        output.enabled=true;
    }
    static bool DefaultProjection(Camera camera)
    {
        float h=camera.orthographicSize,w=h*camera.aspect;
        return camera.projectionMatrix.Equals(Matrix4x4.Ortho(-w,w,-h,h,camera.nearClipPlane,camera.farClipPlane));
    }
    // Always-on readability sits below inspection (+4) and the success scan (+5).
    public void PresentAtOrder(Transform target,Camera camera,int offset)
    {
        PresentMask(target,camera,true);
        if(output!=null&&output.enabled)output.sortingOrder+=offset-4;
        RegisterOutput(camera,false,0);
    }
    // A separate owner uses the current body mask for floor contact, below all bodies.
    public void PresentGroundContact(Transform target,Camera camera,Material material)
    {
        PresentContact(target,camera,material,null);
    }
    public void PresentGroundContactWithBand(Transform target,Camera camera,Material material,float band)
    {
        PresentContact(target,camera,material,band);
    }
    void PresentContact(Transform target,Camera camera,Material material,float? band)
    {
        bool compact=false;
        if(material!=null)
        {
            if(contactShader==null)contactShader=Resources.Load<Shader>("CoreGuardian/PhotoShadowContact");
            compact=material.shader==contactShader;
        }
        PresentMask(target,camera,material==null,compact);
        if(!IsVisible||material==null)return;
        SetOutputMask();if(band.HasValue)outputProperties.SetFloat("_Band",band.Value);
        output.SetPropertyBlock(outputProperties);output.sharedMaterial=material;output.sortingOrder=43;
        if(compact)RegisterOutput(camera,true,band??material.GetFloat("_Band"));
    }
    void RegisterOutput(Camera camera,bool contact,float band)
    {
        // Runtime savings came from mask production, not output material substitution.
        // Keep every original output renderer/material/order, including foreign transparent ties.
        if(Application.isPlaying)return;
        if(output==null||!output.enabled||mask==null||camera==null)return;
        var viewport=camera.pixelRect;
        // Same native camera range already covered by body mask sharing pixel QA.
        if(viewport!=new Rect(0,0,1920,1080)||!camera.orthographic||camera.stereoEnabled||camera.allowDynamicResolution
            ||(camera.orthographicSize!=8.5f&&camera.orthographicSize!=4.25f)||!DefaultProjection(camera)
            ||!camera.transform.rotation.Equals(Quaternion.identity)||Mathf.Abs(camera.transform.position.x)>32
            ||Mathf.Abs(camera.transform.position.y)>32)return;
        bool prepared=CoreGuardianBodyMaskCache.PreparedPage(maskBinding,out var slot);
        if(Application.isPlaying&&!prepared)return; // Unsupported producer keeps the original rendering path.
        if(!prepared&&!CoreGuardianMaskPage.TryAcquire(camera,Time.frameCount,mask.width,mask.height,out slot))return;
        var parameters=new CoreGuardianMaskOutputBatch.OutputParameters{contact=contact,allowSingle=true,prepared=prepared,color=color,width=Mathf.Clamp(widthPixels,0,8),band=band,maskOriginPx=maskOriginPx,sourceSize=new Vector2(mask.width,mask.height)};
        if(!CoreGuardianMaskOutputBatch.Register(this,camera,output,quad,slot,parameters)&&!prepared)CoreGuardianMaskPage.Release(slot);
    }
    bool QueuePage(Camera camera,Matrix4x4 vp,int width,int height)
    {
        var viewport=camera.pixelRect;
        if(!Application.isPlaying||!pageRasterSupported||!CoreGuardianMaskProducerBatch.Supported||viewport!=new Rect(0,0,1920,1080)||!camera.orthographic||camera.stereoEnabled||camera.allowDynamicResolution
            ||(camera.orthographicSize!=8.5f&&camera.orthographicSize!=4.25f)||!DefaultProjection(camera)
            ||!camera.transform.rotation.Equals(Quaternion.identity)||Mathf.Abs(camera.transform.position.x)>32||Mathf.Abs(camera.transform.position.y)>32
            ||Mathf.Abs(artwork.position.x)>32||Mathf.Abs(artwork.position.y)>32)return false;
        if(!CoreGuardianMaskPage.TryAcquire(camera,Time.frameCount,mask.width,mask.height,out var slot))return false;
        foreach(var source in sources)
        {
            var shape=Geometry(source.sprite);
            if(CoreGuardianMaskPage.QueueShape(slot,shape.batch,maskMaterial,PartMatrix(source),vp,shape.texture,source.color.a,Mathf.Clamp(alphaThreshold,.01f,1),new Vector2(width,height)))continue;
            CoreGuardianMaskPage.Release(slot);return false;
        }
        CoreGuardianBodyMaskCache.MarkPending(maskBinding,slot);return true;
    }
    void SetOutputMask()
    {
        outputProperties.Clear();outputProperties.SetTexture("_MainTex",mask);
        outputProperties.SetVector("_MainTex_TexelSize",new Vector4(1f/mask.width,1f/mask.height,mask.width,mask.height));
        outputProperties.SetVector("_MaskOriginPx",new Vector4(maskOriginPx.x,maskOriginPx.y,0,0));
    }
    Mesh SpriteMesh(Sprite sprite)
    {
        if(meshes.TryGetValue(sprite,out var value))return value;
        var shape=Geometry(sprite);
        value=new Mesh{name="Selection mask "+sprite.name,hideFlags=HideFlags.DontSave};
        value.vertices=shape.vertices;value.uv=shape.uv;
        value.SetUVs(5,Enumerable.Repeat(shape.atlasRect,shape.vertices.Length).ToList());
        value.triangles=shape.triangles;value.RecalculateBounds();meshes.Add(sprite,value);return value;
    }
    sealed class MaskGeometry
    {
        public CoreGuardianMaskProducerBatch.Shape batch;
        public Vector3[] vertices;
        public Vector2[] uv;
        public int[] triangles;
        public Texture texture;
        public bool mapped;
        public Vector4 atlasRect;
    }
    sealed class MaskBatch
    {
        public Texture texture;
        public float opacity;
        public readonly List<SpriteRenderer> parts=new List<SpriteRenderer>();
        public readonly List<Vector3> vertices=new List<Vector3>();
        public readonly List<Vector2> uv=new List<Vector2>();
        public readonly List<int> triangles=new List<int>();
        public readonly List<Vector4> row0=new List<Vector4>(),row1=new List<Vector4>(),row2=new List<Vector4>(),row3=new List<Vector4>();
        public readonly List<Vector4> atlasRects=new List<Vector4>();
        public Mesh mesh;
    }
    MaskGeometry Geometry(Sprite sprite)
    {
        if(geometry.TryGetValue(sprite,out var value))return value;
        value=new MaskGeometry{vertices=sprite.vertices.Select(v=>(Vector3)v).ToArray(),uv=sprite.uv,triangles=sprite.triangles.Select(v=>(int)v).ToArray(),texture=sprite.texture};
        if(atlas!=null&&atlas.TryGetSprite(sprite,out var entry,out var uv))
        {
            value.texture=entry.page;value.mapped=true;
            value.atlasRect=new Vector4(entry.layer,0,0,0);
            for(int i=0;i<value.uv.Length;i++)
            {
                var point=new Vector2(value.uv[i].x*uv.x+uv.z,value.uv[i].y*uv.y+uv.w);
                // FullRect packed parts have integer source-pixel corners. Avoid atlas UV cancellation drift.
                if(uv!=new Vector4(1,1,0,0))point=new Vector2(Mathf.Round(point.x*entry.page.width)/entry.page.width,Mathf.Round(point.y*entry.page.height)/entry.page.height);
                value.uv[i]=point;
            }
        }
        value.batch=new CoreGuardianMaskProducerBatch.Shape{vertices=value.vertices,uv=value.uv,triangles=value.triangles,layer=value.atlasRect.x};
        geometry.Add(sprite,value);return value;
    }
    static Matrix4x4 PartMatrix(SpriteRenderer part)=>part.transform.localToWorldMatrix*Matrix4x4.Scale(new Vector3(part.flipX?-1:1,part.flipY?-1:1,1));
    void MaskProperties(Texture texture,float opacity,Matrix4x4 vp,bool combined)
    {
        properties.Clear();bool array=texture is Texture2DArray;
        properties.SetTexture(array?"_MaskAtlas":"_MainTex",texture);properties.SetFloat("_UseAtlas",array?1:0);properties.SetFloat("_Opacity",opacity);
        properties.SetFloat("_Threshold",Mathf.Clamp(alphaThreshold,.01f,1));properties.SetMatrix("_MaskVP",vp);
        properties.SetFloat("_UsePartMatrices",combined?1:0);
    }
    void DrawMaskParts(Matrix4x4 vp)
    {
        using var partsScope=partsSample.Auto();
        lastAtlasMisses=0;
        foreach(var batch in batches)batch.parts.Clear();
        int used=0;
        foreach(var part in sources)
        {
            var shape=Geometry(part.sprite);var texture=shape.texture;if(!shape.mapped)lastAtlasMisses++;
            float opacity=part.color.a;int index=0;
            for(;index<used;index++)if(ReferenceEquals(batches[index].texture,texture)&&batches[index].opacity==opacity)break;
            if(index==used)
            {
                if(used==batches.Count)batches.Add(new MaskBatch());
                batches[index].texture=texture;batches[index].opacity=opacity;used++;
            }
            batches[index].parts.Add(part);
        }
        for(int i=0;i<used;i++)
        {
            var batch=batches[i];int vertices=0;
            foreach(var part in batch.parts)vertices+=Geometry(part.sprite).vertices.Length;
            // Preserve unrestricted source rendering when a group cannot use 16-bit indices.
            if(batch.parts.Count==1||vertices>65535)
            {
                MaskProperties(batch.texture,batch.opacity,vp,false);
                foreach(var part in batch.parts){commands.DrawMesh(SpriteMesh(part.sprite),PartMatrix(part),maskMaterial,0,0,properties);lastMaskDrawCalls++;}
                continue;
            }
            if(batch.mesh==null){batch.mesh=new Mesh{name="Selection mask part batch",hideFlags=HideFlags.DontSave};batch.mesh.MarkDynamic();}
            batch.vertices.Clear();batch.uv.Clear();batch.triangles.Clear();batch.row0.Clear();batch.row1.Clear();batch.row2.Clear();batch.row3.Clear();
            batch.atlasRects.Clear();
            foreach(var part in batch.parts)
            {
                var shape=Geometry(part.sprite);var matrix=PartMatrix(part);int offset=batch.vertices.Count;
                var a=matrix.GetRow(0);var b=matrix.GetRow(1);var c=matrix.GetRow(2);var d=matrix.GetRow(3);
                for(int v=0;v<shape.vertices.Length;v++)
                {
                    batch.vertices.Add(shape.vertices[v]);batch.uv.Add(shape.uv[v]);
                    // Keep the original GPU matrix multiply; CPU-baked world vertices lose precision.
                    batch.row0.Add(a);batch.row1.Add(b);batch.row2.Add(c);batch.row3.Add(d);
                    batch.atlasRects.Add(shape.atlasRect);
                }
                foreach(int triangle in shape.triangles)batch.triangles.Add(offset+triangle);
            }
            using(uploadSample.Auto())
            {
                if(batch.mesh.vertexCount>batch.vertices.Count)batch.mesh.SetTriangles(System.Array.Empty<int>(),0,false);
                batch.mesh.SetVertices(batch.vertices);batch.mesh.SetUVs(0,batch.uv);
                batch.mesh.SetUVs(1,batch.row0);batch.mesh.SetUVs(2,batch.row1);batch.mesh.SetUVs(3,batch.row2);batch.mesh.SetUVs(4,batch.row3);
                batch.mesh.SetUVs(5,batch.atlasRects);
                batch.mesh.SetTriangles(batch.triangles,0,false);
            }
            MaskProperties(batch.texture,batch.opacity,vp,true);
            commands.DrawMesh(batch.mesh,Matrix4x4.identity,maskMaterial,0,0,properties);lastMaskDrawCalls++;
        }
    }
    void EnsureResources()
    {
        using var resourcesScope=resourcesSample.Auto();
        if(atlas==null&&SystemInfo.supports2DArrayTextures)atlas=Resources.Load<CoreGuardianMaskAtlas>("CoreGuardian/MaskAtlas/Manifest");
        int revision=atlas!=null?atlas.Revision:-1;
        if(cachedAtlas!=atlas||atlasRevision!=revision)
        {
            foreach(var value in meshes.Values)DisposeObject(value);meshes.Clear();geometry.Clear();
            cachedAtlas=atlas;atlasRevision=revision;
        }
        if(output!=null)return;
        properties=new MaterialPropertyBlock();
        maskBinding=new CoreGuardianBodyMaskCache.Lease(this);
        outputProperties=new MaterialPropertyBlock();
        maskLease=CoreGuardianSharedMaskMaterial.Acquire("CoreGuardian/SelectionMask");maskMaterial=maskLease.Material;
        outlineLease=CoreGuardianSharedMaskMaterial.Acquire("CoreGuardian/SelectionOutline");outlineMaterial=outlineLease.Material;
        commands=new CommandBuffer{name="Selected unit sprite union"};
        var root=new GameObject("SelectionOutline"){hideFlags=HideFlags.DontSave};root.transform.SetParent(transform,false);
        root.AddComponent<CoreGuardianBodyMaskLifetime>().owner=this;
        quad=new Mesh{name="Selected unit outline quad",hideFlags=HideFlags.DontSave};quad.vertices=new Vector3[4];
        quad.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};quad.triangles=new[]{0,1,2,0,2,3};
        root.AddComponent<MeshFilter>().sharedMesh=quad;output=root.AddComponent<MeshRenderer>();output.sharedMaterial=outlineMaterial;
        output.shadowCastingMode=ShadowCastingMode.Off;output.receiveShadows=false;output.enabled=false;
    }
    // Component-only edits on an existing transform must explicitly invalidate this cache.
    // New/reparented/destroyed children are detected automatically, including inactive rigs.
    public void InvalidateSources(){CoreGuardianBodyMaskCache.Invalidate(artwork);bodySources.Invalidate();ClearGeometry();maskGeometryEpoch=-1;lastTarget=null;artwork=null;}
    void ClearGeometry(){foreach(var mesh in meshes.Values)DisposeObject(mesh);meshes.Clear();geometry.Clear();}
    internal void ReleasePresentation(){CoreGuardianMaskOutputBatch.Release(this);if(output!=null)output.enabled=false;CurrentTarget=null;CoreGuardianBodyMaskCache.Release(maskBinding);mask=null;maskOriginPx=Vector2.zero;}
    void OnDisable(){ReleasePresentation();}
    void OnDestroy(){ReleaseResources();}
    internal void ReleaseResources()
    {
        ReleasePresentation();commands?.Dispose();commands=null;
        foreach(var mesh in meshes.Values)DisposeObject(mesh);meshes.Clear();
        foreach(var batch in batches)DisposeObject(batch.mesh);batches.Clear();geometry.Clear();
        DisposeObject(quad);if(output!=null)DisposeObject(output.gameObject);
        maskLease?.Release();maskLease=null;outlineLease?.Release();outlineLease=null;
        maskMaterial=null;outlineMaterial=null;
    }
    static void DisposeObject(Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
}

// Immutable shader state only. Each owner keeps its changing texture and values in a property block.
internal sealed class CoreGuardianSharedMaskMaterial
{
    static readonly Dictionary<string,CoreGuardianSharedMaskMaterial> entries=new Dictionary<string,CoreGuardianSharedMaskMaterial>();
    readonly string path;
    int users;
    public Material Material {get;private set;}
    CoreGuardianSharedMaskMaterial(string path)
    {
        this.path=path;
        Material=new Material(Resources.Load<Shader>(path)){name="Shared "+path,hideFlags=HideFlags.DontSave};
    }
    public static CoreGuardianSharedMaskMaterial Acquire(string path)
    {
        if(!entries.TryGetValue(path,out var entry)){entry=new CoreGuardianSharedMaskMaterial(path);entries.Add(path,entry);}
        entry.users++;return entry;
    }
    public void Release()
    {
        if(users<=0)return;
        if(--users!=0)return;
        entries.Remove(path);
        if(Material!=null){if(Application.isPlaying)Object.Destroy(Material);else Object.DestroyImmediate(Material);}
        Material=null;
    }
}

// Per-consumer references only; this does not share masks or alter presentation order.
// Hierarchy checks do not use Transform.hasChanged: walking moves transforms every frame.
internal sealed class CoreGuardianSpriteReferences
{
    static readonly ProfilerMarker readSample=new ProfilerMarker("CG.SpriteReferences.Read");
    Transform root;
    bool dirty=true;
    readonly List<SpriteRenderer> renderers=new List<SpriteRenderer>();
    readonly List<Transform> nodes=new List<Transform>();
    readonly List<int> childCounts=new List<int>();
    readonly List<Transform> parents=new List<Transform>();
    public void Invalidate()=>dirty=true;
    public List<SpriteRenderer> Read(Transform target)
    {
        using var readScope=readSample.Auto();
        bool refresh=dirty||root!=target;
        if(!refresh)
        {
            for(int i=0;i<nodes.Count;i++)
                if(nodes[i]==null||nodes[i].childCount!=childCounts[i]||nodes[i].parent!=parents[i]){refresh=true;break;}
            if(!refresh)for(int i=0;i<renderers.Count;i++)if(renderers[i]==null){refresh=true;break;}
        }
        if(refresh)
        {
            root=target;dirty=false;renderers.Clear();nodes.Clear();childCounts.Clear();parents.Clear();
            if(root!=null)
            {
                root.GetComponentsInChildren(true,renderers);root.GetComponentsInChildren(true,nodes);
                foreach(var node in nodes){childCounts.Add(node.childCount);parents.Add(node.parent);}
            }
        }
        return renderers;
    }
}
