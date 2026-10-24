using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Keeps scene art identical when sprites are drawn through native dynamic batching (SRP Batcher off):
// - Batching bakes SpriteRenderer.color into 8-bit vertex colours, so authored RGB above 1 would clamp.
//   Such static sprites get a shared material twin carrying that RGB in _Color (shader: input.color*_Color*unity_SpriteColor).
// - MeshRenderers using a stock sprite shader do not receive the per-renderer sprite constants a SpriteRenderer supplies,
//   so they get the same neutral constants CoreGuardianRouteMeshBake uses.
// Runs once per loaded scene in Play mode only; scene files and assets are never modified.
public static class CoreGuardianSpriteBatchCompat
{
    static readonly int ColorId=Shader.PropertyToID("_Color");
    static readonly Dictionary<(Material,Color),Material> twins=new Dictionary<(Material,Color),Material>();
    // Totals since entering Play mode.
    public static int LastColorTwins {get;private set;}
    public static int LastMeshes {get;private set;}

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        SceneManager.sceneLoaded-=OnSceneLoaded;SceneManager.sceneLoaded+=OnSceneLoaded;
        Application.quitting-=Release;Application.quitting+=Release;
        LastColorTwins=0;LastMeshes=0;
        for(int i=0;i<SceneManager.sceneCount;i++)Apply(SceneManager.GetSceneAt(i));
    }
    static void OnSceneLoaded(Scene scene,LoadSceneMode mode)=>Apply(scene);
    // Application.quitting also fires when the Editor leaves Play mode.
    static void Release(){foreach(var twin in twins.Values)if(twin!=null)Object.Destroy(twin);twins.Clear();}

    public static void Apply(Scene scene)
    {
        if(!scene.IsValid()||!scene.isLoaded)return;
        var properties=new MaterialPropertyBlock();
        properties.SetVector("unity_SpriteProps",new Vector4(1,1,0,0));
        properties.SetColor("unity_SpriteColor",Color.white);
        properties.SetColor("_RendererColor",Color.white);
        foreach(var root in scene.GetRootGameObjects())
        {
            foreach(var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var c=renderer.color;var material=renderer.sharedMaterial;
                // Only plain static art: objects driven by a script keep their own colour handling.
                // (With the SRP Batcher off every SpriteRenderer carries an engine _MainTex block, so blocks are not a filter here.)
                if(c.r<=1&&c.g<=1&&c.b<=1||material==null||!material.HasProperty(ColorId)
                    ||renderer.GetComponent<MonoBehaviour>()!=null)continue;
                var rgb=new Color(c.r,c.g,c.b,1);
                if(!twins.TryGetValue((material,rgb),out var twin)||twin==null)
                {
                    twin=new Material(material){name=material.name+" (CG colour)",hideFlags=HideFlags.HideAndDontSave};
                    var m=material.GetColor(ColorId);twin.SetColor(ColorId,new Color(m.r*rgb.r,m.g*rgb.g,m.b*rgb.b,m.a));
                    twins[(material,rgb)]=twin;
                }
                renderer.sharedMaterial=twin;renderer.color=new Color(1,1,1,c.a);LastColorTwins++;
            }
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var material=renderer.sharedMaterial;
                if(material==null||material.shader==null||renderer.HasPropertyBlock()
                    ||!material.shader.name.StartsWith("Universal Render Pipeline/2D/Sprite-",System.StringComparison.Ordinal))continue;
                renderer.SetPropertyBlock(properties);LastMeshes++;
            }
        }
    }
}
