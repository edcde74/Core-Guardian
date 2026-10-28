using UnityEngine;

// With the SRP Batcher off (project setting since 2026-10-24) the engine keeps every SpriteRenderer's sprite texture in
// a renderer property block, so Renderer.HasPropertyBlock() is true for any sprite even after SetPropertyBlock(null).
// This reports only overrides a script made: a different _MainTex or any per-renderer property this project sets.
public static class CoreGuardianSpriteBlocks
{
    static MaterialPropertyBlock probe;
    static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    static readonly int[] scriptProperties =
    {
        Shader.PropertyToID("_HitFlashAmount"),
        Shader.PropertyToID("_HitFlashTint"),
        Shader.PropertyToID("_RendererColor"),
        Shader.PropertyToID("unity_SpriteColor"),
        Shader.PropertyToID("unity_SpriteProps"),
        Shader.PropertyToID("_Color"),
        Shader.PropertyToID("_CGOutlineEntity"),
        Shader.PropertyToID("_Width"),
    };

    public static bool HasCustomBlock(Renderer renderer)
    {
        if (renderer == null || !renderer.HasPropertyBlock())
            return false;

        if (!(renderer is SpriteRenderer sprite) || sprite.sprite == null)
            return true;

        probe ??= new MaterialPropertyBlock();
        renderer.GetPropertyBlock(probe);
        if (probe.isEmpty)
            return false;

        if (probe.HasTexture(MainTexId) && probe.GetTexture(MainTexId) != sprite.sprite.texture)
            return true;

        foreach (var id in scriptProperties)
        {
            if (probe.HasProperty(id))
                return true;
        }

        return false;
    }
}
