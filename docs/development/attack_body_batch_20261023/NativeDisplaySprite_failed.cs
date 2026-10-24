using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Unity.Profiling;
using Object=UnityEngine.Object;

// Native SpriteRenderers keep their identity, transforms, sorting and draw ordering.
// Only camera-scoped display sprites/materials change; animation and masks keep originals.
public static class CoreGuardianAttackBodyBatch
{
    struct Binding {public SpriteRenderer renderer;public Sprite original,display;public Material material;}
    static readonly List<SpriteRenderer> registered=new List<SpriteRenderer>();
    static readonly List<Binding> bindings=new List<Binding>();
    static readonly Dictionary<Sprite,Sprite> displays=new Dictionary<Sprite,Sprite>();
    static readonly Dictionary<Texture2D,int> layers=new Dictionary<Texture2D,int>();
    static readonly ProfilerMarker marker=new ProfilerMarker("CG.AttackBodyBatch");
    static Texture2DArray texture;
    static Texture2D carrier;
    static Material material;
    public static bool Enabled {get;private set;}
    public static int LastSources {get;private set;}
    // Native batching owns the draw count; read real Rendering counters instead.
    public static int LastDraws=>-1;
    public static long TextureBytes=>texture==null?0:UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);
    static CoreGuardianAttackBodyBatch()
    {
        SceneManager.sceneUnloaded+=scene=>Clear();Application.quitting+=Clear;
#if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload+=Clear;
        UnityEditor.EditorApplication.playModeStateChanged+=state=>{if(state==UnityEditor.PlayModeStateChange.ExitingPlayMode)Clear();};
#endif
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset()=>Clear();
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
        var names=new[]{"Body","LeftUpper","LeftLower","RightUpper","RightLower"};var sources=new Texture2D[names.Length];
        for(int i=0;i<names.Length;i++)
        {
            var sprite=Resources.Load<Sprite>("CoreGuardian/OriginalWalk/Attack/"+names[i]);if(sprite==null||sprite.packed)return false;
            var source=sprite.texture;
            if(source.filterMode!=FilterMode.Point||source.mipmapCount!=1||source.wrapModeU!=TextureWrapMode.Clamp||source.wrapModeV!=TextureWrapMode.Clamp)return false;
            if(i>0&&(source.width!=sources[0].width||source.height!=sources[0].height||source.graphicsFormat!=sources[0].graphicsFormat))return false;
            sources[i]=source;
        }
        // Runtime sprite geometry requires a readable carrier; never change source import settings.
        carrier=new Texture2D(sources[0].width,sources[0].height,sources[0].graphicsFormat,UnityEngine.Experimental.Rendering.TextureCreationFlags.None)
            {name="Attack native readable carrier",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontSave};
        var opaque=new Color32[carrier.width*carrier.height];for(int i=0;i<opaque.Length;i++)opaque[i]=new Color32(255,255,255,255);
        carrier.SetPixels32(opaque);carrier.Apply(false,false);
        texture=new Texture2DArray(carrier.width,carrier.height,sources.Length,carrier.graphicsFormat,UnityEngine.Experimental.Rendering.TextureCreationFlags.None)
            {name="Attack original RGBA array",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontSave};
        for(int i=0;i<sources.Length;i++){Graphics.CopyTexture(sources[i],0,0,texture,i,0);layers[sources[i]]=i;}
        material=new Material(shader){name="Attack native shared display",hideFlags=HideFlags.DontSave};material.SetTexture("_BodyTextures",texture);return true;
    }
    static bool Supported(SpriteRenderer r,Camera camera)
    {
        if(r==null||!r.enabled||r.forceRenderingOff||!r.gameObject.activeInHierarchy||r.sprite==null||r.sprite.packed
            ||r.drawMode!=SpriteDrawMode.Simple||r.maskInteraction!=SpriteMaskInteraction.None||r.HasPropertyBlock()
            ||(camera.cullingMask&(1<<r.gameObject.layer))==0)return false;
        var m=r.sharedMaterial;
        return layers.ContainsKey(r.sprite.texture)&&m!=null&&m.shader!=null&&m.shader.name=="Universal Render Pipeline/2D/Sprite-Unlit-Default"
            &&m.renderQueue==3000&&(!m.HasProperty("_ZWrite")||m.GetFloat("_ZWrite")==0)&&(!m.HasProperty("_Color")||m.GetColor("_Color")==Color.white);
    }
    static Sprite Display(Sprite source)
    {
        if(displays.TryGetValue(source,out var display))return display;
        display=Sprite.Create(carrier,source.rect,source.pivot/source.rect.size,source.pixelsPerUnit,0,SpriteMeshType.FullRect);
        display.name="Native Attack display "+source.name;display.hideFlags=HideFlags.DontSave;
        // These approved inputs are FullRect. Do not call the unsupported OverrideGeometry API.
        if(!source.vertices.SequenceEqual(display.vertices)||!source.triangles.SequenceEqual(display.triangles))
        {Dispose(display);return null;}
        using(var originalUV=new NativeArray<Vector2>(source.uv,Allocator.Temp))display.SetVertexAttribute(VertexAttribute.TexCoord0,originalUV);
        var indices=new NativeArray<Vector2>(source.vertices.Length,Allocator.Temp);
        try
        {
            for(int i=0;i<indices.Length;i++)indices[i]=new Vector2(layers[source.texture],0);
            display.SetVertexAttribute(VertexAttribute.TexCoord1,indices);
        }
        finally{indices.Dispose();}
        displays.Add(source,display);return display;
    }
    public static void Flush(Camera camera)
    {
        using var scope=marker.Auto();Restore();LastSources=0;
        if(!Enabled||camera==null||!camera.orthographic||camera.stereoEnabled||camera.allowDynamicResolution||camera.transform.rotation!=Quaternion.identity||!ResourcesReady())return;
        registered.RemoveAll(r=>r==null);
        try
        {
            foreach(var r in registered)
            {
                if(!Supported(r,camera))continue;
                var display=Display(r.sprite);if(display==null)continue;
                bindings.Add(new Binding{renderer=r,original=r.sprite,display=display,material=r.sharedMaterial});
                r.sprite=display;r.sharedMaterial=material;LastSources++;
            }
        }
        catch{Restore();throw;}
    }
    public static void Restore()
    {
        foreach(var binding in bindings)
        {
            var r=binding.renderer;if(r==null)continue;
            if(r.sprite==binding.display)r.sprite=binding.original;
            if(r.sharedMaterial==material)r.sharedMaterial=binding.material;
        }
        bindings.Clear();
    }
    public static void Clear()
    {
        Restore();Enabled=false;LastSources=0;registered.Clear();
        foreach(var display in displays.Values)Dispose(display);displays.Clear();layers.Clear();Dispose(material);material=null;Dispose(texture);texture=null;Dispose(carrier);carrier=null;
    }
    static void Dispose(Object value){if(value==null)return;if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);}
}
