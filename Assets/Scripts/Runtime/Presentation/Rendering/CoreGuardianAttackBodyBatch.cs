using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Unity.Profiling;
using Object=UnityEngine.Object;

// Native SpriteRenderers keep their identity, transforms, sorting and draw ordering.
// Only camera-scoped display sprites/materials change; animation and masks keep originals.
public static class CoreGuardianAttackBodyBatch
{
    struct Binding {public SpriteRenderer renderer;public Material material;public Color color;}
    static readonly List<SpriteRenderer> registered=new List<SpriteRenderer>();
    static readonly List<Binding> bindings=new List<Binding>();
    static readonly Dictionary<Texture2D,int> layers=new Dictionary<Texture2D,int>();
    static readonly Dictionary<Color,int> palette=new Dictionary<Color,int>();
    static readonly Color[] paletteColors=new Color[255];
    static readonly MaterialPropertyBlock sharedProperties=new MaterialPropertyBlock();
    static readonly ProfilerMarker marker=new ProfilerMarker("CG.AttackBodyBatch");
    static Texture2DArray texture;
    static Texture2D carrier;
    static Material material;
    static GraphicsBuffer colorBuffer;
    public static bool Enabled {get;private set;}
    public static int LastSources {get;private set;}
    // Native batching owns the draw count; read real Rendering counters instead.
    public static int LastDraws=>-1;
    public static long TextureBytes=>(texture==null?0:UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture))+(carrier==null?0:UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(carrier));
    public static long BufferBytes=>colorBuffer==null?0:colorBuffer.count*colorBuffer.stride;
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
        if(texture!=null&&material!=null&&colorBuffer!=null)return true;
        if(!SystemInfo.supports2DArrayTextures||SystemInfo.graphicsShaderLevel<45||SystemInfo.copyTextureSupport==CopyTextureSupport.None)return false;
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
        material=new Material(shader){name="Attack native shared display",hideFlags=HideFlags.DontSave};material.SetTexture("_BodyTextures",texture);
        colorBuffer=new GraphicsBuffer(GraphicsBuffer.Target.Structured,255,16);material.SetBuffer("_DisplayColors",colorBuffer);return true;
    }
    static bool Supported(SpriteRenderer r,Camera camera)
    {
        if(r==null||!r.enabled||r.forceRenderingOff||!r.gameObject.activeInHierarchy||r.sprite==null||r.sprite.packed
            ||r.drawMode!=SpriteDrawMode.Simple||r.maskInteraction!=SpriteMaskInteraction.None||r.HasPropertyBlock()||r.color.a!=1
            ||(camera.cullingMask&(1<<r.gameObject.layer))==0)return false;
        var m=r.sharedMaterial;
        return layers.ContainsKey(r.sprite.texture)&&m!=null&&m.shader!=null&&m.shader.name=="Universal Render Pipeline/2D/Sprite-Unlit-Default"
            &&m.renderQueue==3000&&(!m.HasProperty("_ZWrite")||m.GetFloat("_ZWrite")==0)&&(!m.HasProperty("_Color")||m.GetColor("_Color")==Color.white);
    }
    public static void Flush(Camera camera)
    {
        using var scope=marker.Auto();Restore();LastSources=0;
        if(!Enabled||camera==null||!camera.orthographic||camera.stereoEnabled||camera.allowDynamicResolution||camera.transform.rotation!=Quaternion.identity||!ResourcesReady())return;
        registered.RemoveAll(r=>r==null);palette.Clear();
        try
        {
            foreach(var r in registered)
            {
                if(!Supported(r,camera))continue;
                var originalColor=r.color;
                if(!palette.TryGetValue(originalColor,out int paletteIndex))
                {
                    if(palette.Count>=51)continue; // No truncation: additional tints keep their original path.
                    paletteIndex=palette.Count;palette.Add(originalColor,paletteIndex);
                    // Match SpriteRenderer's shader-side tint in the project's working color space.
                    var renderColor=QualitySettings.activeColorSpace==ColorSpace.Linear?originalColor.linear:originalColor;
                    for(int layer=0;layer<5;layer++)paletteColors[paletteIndex*5+layer]=renderColor;
                }
                bindings.Add(new Binding{renderer=r,material=r.sharedMaterial,color=r.color});
                // Keep original Sprite identity. The only property block is identical across all parts.
                sharedProperties.Clear();sharedProperties.SetTexture("_MainTex",carrier);r.SetPropertyBlock(sharedProperties);
                // Native batching quantizes vertex colors. Carry only an exact byte index there;
                // preserve the original floating-point/HDR tint in the common GPU buffer.
                r.color=new Color(1,1,1,(paletteIndex*5+layers[r.sprite.texture]+1)/255f);
                r.sharedMaterial=material;LastSources++;
            }
            if(palette.Count>0)colorBuffer.SetData(paletteColors,0,0,palette.Count*5);
        }
        catch{Restore();throw;}
    }
    public static void Restore()
    {
        foreach(var binding in bindings)
        {
            var r=binding.renderer;if(r==null)continue;
            if(r.sharedMaterial==material){r.sharedMaterial=binding.material;r.color=binding.color;r.SetPropertyBlock(null);}
        }
        bindings.Clear();
    }
    public static void Clear()
    {
        Restore();Enabled=false;LastSources=0;registered.Clear();
        layers.Clear();palette.Clear();colorBuffer?.Dispose();colorBuffer=null;Dispose(material);material=null;Dispose(texture);texture=null;Dispose(carrier);carrier=null;
    }
    static void Dispose(Object value){if(value==null)return;if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);}
}
