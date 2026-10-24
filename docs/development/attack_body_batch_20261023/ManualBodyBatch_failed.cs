using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Unity.Profiling;
using Object=UnityEngine.Object;

// Experimental display path only. Original renderers remain the animation/mask inputs.
public static class CoreGuardianAttackBodyBatch
{
    sealed class Shape {public Vector2[] vertices,uv;public ushort[] triangles;}
    sealed class NativeTieComparer:IComparer<SpriteRenderer>
    {
        public int Compare(SpriteRenderer a,SpriteRenderer b)=>a.GetEntityId().CompareTo(b.GetEntityId());
    }
    static readonly NativeTieComparer nativeTieComparer=new NativeTieComparer();
    sealed class Group
    {
        public readonly List<SpriteRenderer> sources=new List<SpriteRenderer>();
        public Mesh mesh;public MeshRenderer renderer;
    }
    static readonly List<SpriteRenderer> registered=new List<SpriteRenderer>();
    static readonly HashSet<SpriteRenderer> eligible=new HashSet<SpriteRenderer>();
    static readonly List<SpriteRenderer> hidden=new List<SpriteRenderer>();
    static readonly Dictionary<(int,int,int,int,Material),Group> groups=new Dictionary<(int,int,int,int,Material),Group>();
    static readonly Dictionary<Sprite,Shape> shapes=new Dictionary<Sprite,Shape>();
    static readonly Dictionary<Texture2D,int> layers=new Dictionary<Texture2D,int>();
    static readonly List<Vector3> vertices=new List<Vector3>();
    static readonly List<Color> colors=new List<Color>();
    static readonly List<int> triangles=new List<int>();
    static readonly List<Vector4>[] uv={new List<Vector4>(),new List<Vector4>(),new List<Vector4>(),new List<Vector4>(),new List<Vector4>(),new List<Vector4>()};
    static readonly ProfilerMarker marker=new ProfilerMarker("CG.AttackBodyBatch");
    static Texture2DArray texture;
    static Material material;
    public static bool Enabled {get;private set;}
    public static int LastSources {get;private set;}
    public static int LastDraws {get;private set;}
    public static long TextureBytes=>texture==null?0:UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);
    static CoreGuardianAttackBodyBatch()
    {
        SceneManager.sceneUnloaded+=scene=>Clear();
        Application.quitting+=Clear;
#if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload+=Clear;
        UnityEditor.EditorApplication.playModeStateChanged+=state=>{if(state==UnityEditor.PlayModeStateChange.ExitingPlayMode)Clear();};
#endif
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()=>Clear();
    public static void Register(string role,SpriteRenderer[] parts)
    {
        if(role!="Attack"||parts==null||parts.Length!=5)return;
        foreach(var part in parts)if(part!=null&&!registered.Contains(part))registered.Add(part);
    }
    public static void SetEnabled(bool value){Restore();Enabled=value;}
    static bool ResourcesReady()
    {
        if(texture!=null&&material!=null)return true;
        if(!SystemInfo.supports2DArrayTextures||SystemInfo.copyTextureSupport==CopyTextureSupport.None)return false;
        var shader=Resources.Load<Shader>("CoreGuardian/AttackBodyBatch");if(shader==null||!shader.isSupported)return false;
        var names=new[]{"Body","LeftUpper","LeftLower","RightUpper","RightLower"};
        var sources=new Texture2D[names.Length];
        for(int i=0;i<names.Length;i++)
        {
            var sprite=Resources.Load<Sprite>("CoreGuardian/OriginalWalk/Attack/"+names[i]);
            if(sprite==null||sprite.packed)return false;
            var source=sprite.texture;
            if(source.filterMode!=FilterMode.Point||source.mipmapCount!=1||source.wrapModeU!=TextureWrapMode.Clamp||source.wrapModeV!=TextureWrapMode.Clamp)return false;
            if(i>0&&(source.width!=sources[0].width||source.height!=sources[0].height||source.graphicsFormat!=sources[0].graphicsFormat))return false;
            sources[i]=source;
        }
        texture=new Texture2DArray(sources[0].width,sources[0].height,sources.Length,sources[0].graphicsFormat,UnityEngine.Experimental.Rendering.TextureCreationFlags.None)
            {name="Attack side original RGBA array",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontSave};
        for(int i=0;i<sources.Length;i++){Graphics.CopyTexture(sources[i],0,0,texture,i,0);layers[sources[i]]=i;}
        material=new Material(shader){name="Attack side shared display",hideFlags=HideFlags.DontSave};material.SetTexture("_BodyTextures",texture);
        return true;
    }
    static bool Supported(SpriteRenderer renderer,Camera camera)
    {
        if(renderer==null||!renderer.enabled||renderer.forceRenderingOff||!renderer.gameObject.activeInHierarchy||renderer.sprite==null
            ||renderer.sprite.packed||renderer.drawMode!=SpriteDrawMode.Simple||renderer.maskInteraction!=SpriteMaskInteraction.None
            ||renderer.HasPropertyBlock()||renderer.GetComponentInParent<SortingGroup>()!=null
            ||(camera.cullingMask&(1<<renderer.gameObject.layer))==0)return false;
        var m=renderer.sharedMaterial;
        return layers.ContainsKey(renderer.sprite.texture)&&m!=null&&m.shader!=null&&m.shader.name=="Universal Render Pipeline/2D/Sprite-Unlit-Default"
            &&m.renderQueue==3000&&(!m.HasProperty("_ZWrite")||m.GetFloat("_ZWrite")==0)&&(!m.HasProperty("_Color")||m.GetColor("_Color")==Color.white);
    }
    public static void Flush(Camera camera)
    {
        using var scope=marker.Auto();Restore();LastSources=LastDraws=0;
        if(!Enabled||camera==null||!camera.orthographic||camera.stereoEnabled||camera.allowDynamicResolution
            ||camera.transform.rotation!=Quaternion.identity||!ResourcesReady())return;
        eligible.Clear();foreach(var group in groups.Values)group.sources.Clear();
        registered.RemoveAll(r=>r==null);
        foreach(var renderer in registered)
        {
            if(!Supported(renderer,camera))continue;
            eligible.Add(renderer);var key=(renderer.sortingLayerID,renderer.sortingOrder,renderer.gameObject.layer,renderer.rendererPriority,renderer.sharedMaterial);
            if(!groups.TryGetValue(key,out var group))groups.Add(key,group=new Group());group.sources.Add(renderer);
        }
        if(eligible.Count==0)return;
        // Include DontSave outline outputs: FindObjectsByType omits these foreign renderers.
        var all=Resources.FindObjectsOfTypeAll<Renderer>();
        try
        {
            foreach(var pair in groups)
            {
                var group=pair.Value;if(group.sources.Count<2||!Safe(group,camera,all))continue;
                if(group.mesh==null)
                {
                    group.mesh=new Mesh{name="Attack side combined geometry",hideFlags=HideFlags.DontSave,indexFormat=IndexFormat.UInt32};group.mesh.MarkDynamic();
                    var root=new GameObject("Attack body batch"){hideFlags=HideFlags.DontSave};
                    SceneManager.MoveGameObjectToScene(root,group.sources[0].gameObject.scene);
                    root.AddComponent<MeshFilter>().sharedMesh=group.mesh;group.renderer=root.AddComponent<MeshRenderer>();
                    group.renderer.shadowCastingMode=ShadowCastingMode.Off;group.renderer.receiveShadows=false;
                }
                Upload(group);
                var first=group.sources[0];var output=group.renderer;output.gameObject.layer=first.gameObject.layer;
                output.sortingLayerID=first.sortingLayerID;output.sortingOrder=first.sortingOrder;output.rendererPriority=first.rendererPriority;
                output.sharedMaterial=material;output.enabled=true;
                foreach(var source in group.sources){source.forceRenderingOff=true;hidden.Add(source);}
                LastSources+=group.sources.Count;LastDraws++;
            }
        }
        catch{Restore();throw;}
    }
    static bool Safe(Group group,Camera camera,Renderer[] all)
    {
        var first=group.sources[0];float z=first.bounds.center.z;
        foreach(var source in group.sources)if(source.bounds.center.z!=z)return false;
        foreach(var other in all)
        {
            if(other==null||!other.enabled||other.forceRenderingOff||!other.gameObject.activeInHierarchy||!other.gameObject.scene.IsValid()
                ||(camera.cullingMask&(1<<other.gameObject.layer))==0)continue;
            if(other is SpriteRenderer sprite&&group.sources.Contains(sprite))continue;
            // A SortingGroup can replace the visible child's sort key; conservatively fall back.
            if(other.GetComponentInParent<SortingGroup>()==null&&(other.sortingLayerID!=first.sortingLayerID||other.sortingOrder!=first.sortingOrder))continue;
            var bounds=other.bounds;
            foreach(var source in group.sources)
            {
                var b=source.bounds;
                if(b.min.x<=bounds.max.x&&b.max.x>=bounds.min.x&&b.min.y<=bounds.max.y&&b.max.y>=bounds.min.y)return false;
            }
        }
        return true;
    }
    static void Upload(Group group)
    {
        vertices.Clear();colors.Clear();triangles.Clear();foreach(var channel in uv)channel.Clear();
        var bounds=group.sources[0].bounds;
        // Match the supported native equal-key renderer tie order, including reused EntityId slots.
        group.sources.Sort(nativeTieComparer);
        for(int sourceIndex=group.sources.Count-1;sourceIndex>=0;sourceIndex--)
        {
            var source=group.sources[sourceIndex];
            var sprite=source.sprite;
            if(!shapes.TryGetValue(sprite,out var shape))shapes.Add(sprite,shape=new Shape{vertices=sprite.vertices,uv=sprite.uv,triangles=sprite.triangles});
            int offset=vertices.Count;var matrix=source.localToWorldMatrix;float sx=source.flipX?-1:1,sy=source.flipY?-1:1;
            for(int i=0;i<shape.vertices.Length;i++)
            {
                var p=shape.vertices[i];vertices.Add(new Vector3(p.x*sx,p.y*sy,0));colors.Add(source.color);
                uv[0].Add(new Vector4(shape.uv[i].x,shape.uv[i].y,layers[sprite.texture],0));
                for(int row=0;row<4;row++)uv[row+1].Add(matrix.GetRow(row));
            }
            foreach(int index in shape.triangles)triangles.Add(offset+index);bounds.Encapsulate(source.bounds);
        }
        var mesh=group.mesh;mesh.Clear();mesh.SetVertices(vertices);mesh.SetColors(colors);
        for(int i=0;i<5;i++)mesh.SetUVs(i,uv[i]);mesh.SetTriangles(triangles,0,false);mesh.bounds=bounds;
    }
    public static void Restore()
    {
        foreach(var source in hidden)if(source!=null)source.forceRenderingOff=false;hidden.Clear();
        foreach(var group in groups.Values)if(group.renderer!=null)group.renderer.enabled=false;
    }
    public static void Clear()
    {
        Restore();Enabled=false;LastSources=LastDraws=0;
        foreach(var group in groups.Values){Dispose(group.mesh);if(group.renderer!=null)Dispose(group.renderer.gameObject);}
        groups.Clear();registered.Clear();eligible.Clear();shapes.Clear();layers.Clear();Dispose(material);material=null;Dispose(texture);texture=null;
    }
    static void Dispose(Object value){if(value==null)return;if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);}
}
