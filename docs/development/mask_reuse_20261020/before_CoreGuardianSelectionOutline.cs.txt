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
    MaterialPropertyBlock properties;
    readonly List<SpriteRenderer> sources=new List<SpriteRenderer>();
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
        CurrentTarget=target;
        if(output!=null)output.enabled=false;
        if(target==null||camera==null||widthPixels<=0||color.a<=0)return;
        if(target!=lastTarget||artwork==null)
        {
            lastTarget=target;
            artwork=target.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r=>r.name=="2D Sprite")?.transform;
        }
        if(artwork==null)return;
        sources.Clear();artwork.GetComponentsInChildren(false,sources);
        sources.RemoveAll(r=>!r.enabled||r.sprite==null||r.color.a<alphaThreshold||!r.gameObject.activeInHierarchy);
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
        foreach(var r in sources)
        {
            properties.Clear();properties.SetTexture("_MainTex",r.sprite.texture);properties.SetFloat("_Opacity",r.color.a);
            properties.SetFloat("_Threshold",Mathf.Clamp(alphaThreshold,.01f,1));properties.SetMatrix("_MaskVP",vp);
            var matrix=r.transform.localToWorldMatrix*Matrix4x4.Scale(new Vector3(r.flipX?-1:1,r.flipY?-1:1,1));
            commands.DrawMesh(SpriteMesh(r.sprite),matrix,maskMaterial,0,0,properties);
        }
        Graphics.ExecuteCommandBuffer(commands);
        float depth=Vector3.Dot(bounds.center-camera.transform.position,camera.transform.forward);
        quad.vertices=new[]{Point(lo.x,lo.y),Point(hi.x,lo.y),Point(hi.x,hi.y),Point(lo.x,hi.y)};quad.RecalculateBounds();
        Vector3 Point(float x,float y)=>output.transform.InverseTransformPoint(camera.ScreenToWorldPoint(new Vector3(x,y,depth)));
        var front=sources.OrderBy(r=>SortingLayer.GetLayerValueFromID(r.sortingLayerID)).ThenBy(r=>r.sortingOrder).Last();
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
        value=new Mesh{name="Selection mask "+sprite.name,hideFlags=HideFlags.DontSave};
        value.vertices=sprite.vertices.Select(v=>(Vector3)v).ToArray();value.uv=sprite.uv;
        value.triangles=sprite.triangles.Select(v=>(int)v).ToArray();value.RecalculateBounds();meshes.Add(sprite,value);return value;
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
        if(mask!=null&&mask.width==width&&mask.height==height)return;
        if(mask!=null){mask.Release();DisposeObject(mask);}
        mask=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear)
            {name="Selected unit union mask",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontSave};mask.Create();
    }
    void OnDisable(){if(output!=null)output.enabled=false;CurrentTarget=null;}
    void OnDestroy()
    {
        commands?.Dispose();if(mask!=null){mask.Release();DisposeObject(mask);}
        foreach(var mesh in meshes.Values)DisposeObject(mesh);meshes.Clear();
        DisposeObject(quad);DisposeObject(maskMaterial);DisposeObject(outlineMaterial);if(output!=null)DisposeObject(output.gameObject);
    }
    static void DisposeObject(Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
}
