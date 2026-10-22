using UnityEngine;

// Serialized ownership only: no game-loop updates, no source object deletion.
[ExecuteAlways, DisallowMultipleComponent]
public sealed class CoreGuardianRouteMeshBake : MonoBehaviour
{
    public SpriteRenderer[] sources = new SpriteRenderer[0];
    public MeshRenderer[] outputs = new MeshRenderer[0];
    void OnEnable() => ConfigureOutputs();
    public void ConfigureOutputs()
    {
        // The stock sprite shader normally receives these from SpriteRenderer.
        var properties=new MaterialPropertyBlock();
        properties.SetVector("unity_SpriteProps",new Vector4(1,1,0,0));
        properties.SetColor("unity_SpriteColor",Color.white);
        properties.SetColor("_RendererColor",Color.white);
        foreach(var output in outputs)if(output!=null)output.SetPropertyBlock(properties);
    }
}
