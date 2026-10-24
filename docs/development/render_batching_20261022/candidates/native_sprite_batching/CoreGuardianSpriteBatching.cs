using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Opt supported unlit sprites into Unity's sprite batching path, without changing
// the global SRP setting or touching another presentation owner's property block.
[DisallowMultipleComponent]
public sealed class CoreGuardianSpriteBatching : MonoBehaviour
{
    static readonly Dictionary<Material,Material> mapped=new Dictionary<Material,Material>();
    static Shader sourceShader,batchShader;
    static int owners;
    IDisposable lease;
    struct Binding {internal SpriteRenderer renderer;internal Material source,replacement;}
    sealed class Lease:IDisposable
    {
        List<Binding> bindings;
        internal Lease(List<Binding> value){bindings=value;owners++;}
        public void Dispose()
        {
            if(bindings==null)return;
            foreach(var binding in bindings)
                if(binding.renderer!=null&&binding.renderer.sharedMaterial==binding.replacement)
                    binding.renderer.sharedMaterial=binding.source;
            bindings=null;
            if(--owners==0)ClearMaterials();
        }
    }
    public static Material Acquire(Material source)
    {
        if(source==null)return null;
        if(sourceShader==null)sourceShader=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if(source.shader!=sourceShader)return source;
        if(mapped.TryGetValue(source,out var value)&&value!=null)return value;
        if(batchShader==null)batchShader=Resources.Load<Shader>("CoreGuardian/BatchedSpriteUnlit");
        if(batchShader==null||!batchShader.isSupported)return source;
        value=new Material(source){shader=batchShader,name="CoreGuardian sprite batch: "+source.name,hideFlags=HideFlags.HideAndDontSave};
        mapped[source]=value;return value;
    }
    public static IDisposable Apply(Transform root)
    {
        var bindings=new List<Binding>();
        if(root!=null)foreach(var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            var source=renderer.sharedMaterial;var replacement=Acquire(source);
            if(source==replacement)continue;
            bindings.Add(new Binding{renderer=renderer,source=source,replacement=replacement});
            renderer.sharedMaterial=replacement;
        }
        return new Lease(bindings);
    }
    public static void BindRoot(GameObject root)
    {
        if(root==null||root.GetComponent<CoreGuardianSpriteBatching>()!=null)return;
        var owner=root.AddComponent<CoreGuardianSpriteBatching>();owner.lease=Apply(root.transform);
    }
    public static void BindScene(Scene scene)
    {foreach(var root in scene.GetRootGameObjects())BindRoot(root);}
    void OnDestroy(){lease?.Dispose();lease=null;}
    static void ClearMaterials()
    {
        foreach(var value in mapped.Values)if(value!=null)
        {if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        mapped.Clear();
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCache(){ClearMaterials();owners=0;sourceShader=batchShader=null;}
}
