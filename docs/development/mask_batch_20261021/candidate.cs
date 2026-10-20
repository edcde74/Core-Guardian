using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// One binary union of visible original sprite parts. Never changes source materials or artwork.
[DisallowMultipleComponent]
[DefaultExecutionOrder(500)]
public sealed class CoreGuardianSelectionOutline : MonoBehaviour
{
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
    Mesh quad;
    MeshRenderer output;
    RenderTexture mask;
    CommandBuffer commands;
    readonly Dictionary<Sprite,Mesh> meshes=new Dictionary<Sprite,Mesh>();
    readonly Dictionary<Sprite,MaskGeometry> geometry=new Dictionary<Sprite,MaskGeometry>();
    readonly List<MaskBatch> batches=new List<MaskBatch>();
    int lastMaskDrawCalls;
    MaterialPropertyBlock properties;
    readonly List<SpriteRenderer> sources=new List<SpriteRenderer>();
    readonly CoreGuardianSpriteReferences bodySources=new CoreGuardianSpriteReferences();
    readonly Vector3[] quadVertices=new Vector3[4];
    readonly Vector2[] quadUV=new Vector2[4];
    Transform artwork,lastTarget;

    void LateUpdate()
    {
        if(hud==null)hud=GetComponent<CoreGuardianHud>();
        Present(hud!=null?hud.InspectionTarget:null,Camera.main);
    }
    // Reuse the same animated-body union for a transient overlay without changing the sources.
    // A dedicated disabled instance is owned by the upgrade effect; the HUD outline stays separate.
    public bool PresentOverlay(Transform target,Camera camera,Material material)
    {
        Present(target,camera);
        if(!IsVisible||material==null)return false;
        material.mainTexture=mask;output.sharedMaterial=material;output.sortingOrder++;
        return true;
    }
    public void Present(Transform target,Camera camera)
    {
        lastMaskDrawCalls=0;
        CurrentTarget=target;
        if(output!=null)output.enabled=false;
        if(target==null||camera==null||widthPixels<=0||color.a<=0)return;
        if(target!=lastTarget||artwork==null)
        {
            lastTarget=target;
            artwork=target.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r=>r.name=="2D Sprite")?.transform;
        }
        if(artwork==null)return;
        sources.Clear();
        foreach(var r in bodySources.Read(artwork))
            if(r!=null&&r.enabled&&r.sprite!=null&&r.color.a>=alphaThreshold&&r.gameObject.activeInHierarchy)sources.Add(r);
        if(sources.Count==0)return;
        EnsureResources();
        var bounds=sources[0].bounds;foreach(var r in sources)bounds.Encapsulate(r.bounds);
        TargetBounds=bounds;
        var lo=new Vector2(float.PositiveInfinity,float.PositiveInfinity);var hi=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
        for(int i=0;i<8;i++)
        {
            var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
            var screen=camera.WorldToScreenPoint(corner);if(screen.z<=0)return;
            lo=Vector2.Min(lo,screen);hi=Vector2.Max(hi,screen);
        }
        int radius=Mathf.Clamp(widthPixels,0,8),pad=radius+2;
        var viewport=camera.pixelRect;
        lo=new Vector2(Mathf.Max(viewport.xMin-pad,Mathf.Floor(lo.x)-pad),Mathf.Max(viewport.yMin-pad,Mathf.Floor(lo.y)-pad));
        hi=new Vector2(Mathf.Min(viewport.xMax+pad,Mathf.Ceil(hi.x)+pad),Mathf.Min(viewport.yMax+pad,Mathf.Ceil(hi.y)+pad));
        int w=Mathf.CeilToInt(hi.x-lo.x),h=Mathf.CeilToInt(hi.y-lo.y);if(w<=0||h<=0)return;
        EnsureMask(w,h);
        var a=(lo-viewport.position)/viewport.size*2-Vector2.one;
        var b=(hi-viewport.position)/viewport.size*2-Vector2.one;
        var crop=Matrix4x4.identity;crop.m00=2/(b.x-a.x);crop.m11=2/(b.y-a.y);crop.m03=-(b.x+a.x)/(b.x-a.x);crop.m13=-(b.y+a.y)/(b.y-a.y);
        var vp=GL.GetGPUProjectionMatrix(crop*camera.projectionMatrix,true)*camera.worldToCameraMatrix;
        commands.Clear();commands.SetRenderTarget(mask);commands.ClearRenderTarget(false,true,Color.clear);
        // Clear the entire capacity, then rasterize at the original screen-pixel resolution.
        commands.SetViewport(new Rect(0,0,w,h));
        DrawMaskParts(vp);
        Graphics.ExecuteCommandBuffer(commands);
        float depth=Vector3.Dot(bounds.center-camera.transform.position,camera.transform.forward);
        quadVertices[0]=Point(lo.x,lo.y);quadVertices[1]=Point(hi.x,lo.y);quadVertices[2]=Point(hi.x,hi.y);quadVertices[3]=Point(lo.x,hi.y);
        quad.vertices=quadVertices;quad.RecalculateBounds();
        // One texture texel remains one screen pixel, including the contact shader's lower band.
        float u=(float)w/mask.width,v=(float)h/mask.height;
        quadUV[0]=Vector2.zero;quadUV[1]=new Vector2(u,0);quadUV[2]=new Vector2(u,v);quadUV[3]=new Vector2(0,v);quad.uv=quadUV;
        Vector3 Point(float x,float y)=>output.transform.InverseTransformPoint(camera.ScreenToWorldPoint(new Vector3(x,y,depth)));
        var front=sources[0];int frontLayer=SortingLayer.GetLayerValueFromID(front.sortingLayerID);
        foreach(var r in sources)
        {
            int layer=SortingLayer.GetLayerValueFromID(r.sortingLayerID);
            // >= preserves the previous stable OrderBy(...).ThenBy(...).Last() tie behavior.
            if(layer>frontLayer||(layer==frontLayer&&r.sortingOrder>=front.sortingOrder)){front=r;frontLayer=layer;}
        }
        output.sortingLayerID=front.sortingLayerID;output.sortingOrder=front.sortingOrder+4;
        outlineMaterial.mainTexture=mask;outlineMaterial.SetColor("_Color",color);outlineMaterial.SetInt("_Width",radius);
        output.sharedMaterial=outlineMaterial;
        output.enabled=true;
    }
    // Always-on readability sits below inspection (+4) and the success scan (+5).
    public void PresentAtOrder(Transform target,Camera camera,int offset)
    {
        Present(target,camera);
        if(output!=null&&output.enabled)output.sortingOrder+=offset-4;
    }
    // A separate owner uses the current body mask for floor contact, below all bodies.
    public void PresentGroundContact(Transform target,Camera camera,Material material)
    {
        Present(target,camera);
        if(output==null||!output.enabled||material==null)return;
        material.mainTexture=mask;output.sharedMaterial=material;output.sortingOrder=43;
    }
    Mesh SpriteMesh(Sprite sprite)
    {
        if(meshes.TryGetValue(sprite,out var value))return value;
        var shape=Geometry(sprite);
        value=new Mesh{name="Selection mask "+sprite.name,hideFlags=HideFlags.DontSave};
        value.vertices=shape.vertices;value.uv=shape.uv;
        value.triangles=shape.triangles;value.RecalculateBounds();meshes.Add(sprite,value);return value;
    }
    sealed class MaskGeometry
    {
        public Vector3[] vertices;
        public Vector2[] uv;
        public int[] triangles;
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
        public Mesh mesh;
    }
    MaskGeometry Geometry(Sprite sprite)
    {
        if(geometry.TryGetValue(sprite,out var value))return value;
        value=new MaskGeometry{vertices=sprite.vertices.Select(v=>(Vector3)v).ToArray(),uv=sprite.uv,triangles=sprite.triangles.Select(v=>(int)v).ToArray()};
        geometry.Add(sprite,value);return value;
    }
    static Matrix4x4 PartMatrix(SpriteRenderer part)=>part.transform.localToWorldMatrix*Matrix4x4.Scale(new Vector3(part.flipX?-1:1,part.flipY?-1:1,1));
    void MaskProperties(Texture texture,float opacity,Matrix4x4 vp,bool combined)
    {
        properties.Clear();properties.SetTexture("_MainTex",texture);properties.SetFloat("_Opacity",opacity);
        properties.SetFloat("_Threshold",Mathf.Clamp(alphaThreshold,.01f,1));properties.SetMatrix("_MaskVP",vp);
        properties.SetFloat("_UsePartMatrices",combined?1:0);
    }
    void DrawMaskParts(Matrix4x4 vp)
    {
        foreach(var batch in batches)batch.parts.Clear();
        int used=0;
        foreach(var part in sources)
        {
            var texture=part.sprite.texture;float opacity=part.color.a;int index=0;
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
            foreach(var part in batch.parts)
            {
                var shape=Geometry(part.sprite);var matrix=PartMatrix(part);int offset=batch.vertices.Count;
                var a=matrix.GetRow(0);var b=matrix.GetRow(1);var c=matrix.GetRow(2);var d=matrix.GetRow(3);
                for(int v=0;v<shape.vertices.Length;v++)
                {
                    batch.vertices.Add(shape.vertices[v]);batch.uv.Add(shape.uv[v]);
                    // Keep the original GPU matrix multiply; CPU-baked world vertices lose precision.
                    batch.row0.Add(a);batch.row1.Add(b);batch.row2.Add(c);batch.row3.Add(d);
                }
                foreach(int triangle in shape.triangles)batch.triangles.Add(offset+triangle);
            }
            if(batch.mesh.vertexCount>batch.vertices.Count)batch.mesh.SetTriangles(System.Array.Empty<int>(),0,false);
            batch.mesh.SetVertices(batch.vertices);batch.mesh.SetUVs(0,batch.uv);
            batch.mesh.SetUVs(1,batch.row0);batch.mesh.SetUVs(2,batch.row1);batch.mesh.SetUVs(3,batch.row2);batch.mesh.SetUVs(4,batch.row3);
            batch.mesh.SetTriangles(batch.triangles,0,false);
            MaskProperties(batch.texture,batch.opacity,vp,true);
            commands.DrawMesh(batch.mesh,Matrix4x4.identity,maskMaterial,0,0,properties);lastMaskDrawCalls++;
        }
    }
    void EnsureResources()
    {
        if(output!=null)return;
        properties=new MaterialPropertyBlock();
        maskMaterial=new Material(Resources.Load<Shader>("CoreGuardian/SelectionMask")){hideFlags=HideFlags.DontSave};
        outlineMaterial=new Material(Resources.Load<Shader>("CoreGuardian/SelectionOutline")){hideFlags=HideFlags.DontSave};
        commands=new CommandBuffer{name="Selected unit sprite union"};
        var root=new GameObject("SelectionOutline"){hideFlags=HideFlags.DontSave};root.transform.SetParent(transform,false);
        quad=new Mesh{name="Selected unit outline quad",hideFlags=HideFlags.DontSave};quad.vertices=new Vector3[4];
        quad.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};quad.triangles=new[]{0,1,2,0,2,3};
        root.AddComponent<MeshFilter>().sharedMesh=quad;output=root.AddComponent<MeshRenderer>();output.sharedMaterial=outlineMaterial;
        output.shadowCastingMode=ShadowCastingMode.Off;output.receiveShadows=false;output.enabled=false;
    }
    void EnsureMask(int width,int height)
    {
        if(mask!=null&&mask.width>=width&&mask.height>=height){if(!mask.IsCreated())mask.Create();return;}
        if(mask!=null){width=Mathf.Max(width,mask.width);height=Mathf.Max(height,mask.height);}
        if(mask!=null){mask.Release();DisposeObject(mask);}
        mask=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear)
            {name="Selected unit union mask",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontSave};mask.Create();
    }
    // Component-only edits on an existing transform must explicitly invalidate this cache.
    // New/reparented/destroyed children are detected automatically, including inactive rigs.
    public void InvalidateSources(){bodySources.Invalidate();lastTarget=null;artwork=null;}
    void OnDisable(){if(output!=null)output.enabled=false;CurrentTarget=null;}
    void OnDestroy()
    {
        commands?.Dispose();if(mask!=null){mask.Release();DisposeObject(mask);}
        foreach(var mesh in meshes.Values)DisposeObject(mesh);meshes.Clear();
        foreach(var batch in batches)DisposeObject(batch.mesh);batches.Clear();geometry.Clear();
        DisposeObject(quad);DisposeObject(maskMaterial);DisposeObject(outlineMaterial);if(output!=null)DisposeObject(output.gameObject);
    }
    static void DisposeObject(Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
}

// Per-consumer references only; this does not share masks or alter presentation order.
// Hierarchy checks do not use Transform.hasChanged: walking moves transforms every frame.
internal sealed class CoreGuardianSpriteReferences
{
    Transform root;
    bool dirty=true;
    readonly List<SpriteRenderer> renderers=new List<SpriteRenderer>();
    readonly List<Transform> nodes=new List<Transform>();
    readonly List<int> childCounts=new List<int>();
    readonly List<Transform> parents=new List<Transform>();
    public void Invalidate()=>dirty=true;
    public List<SpriteRenderer> Read(Transform target)
    {
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
