using System;
using System.Collections.Generic;
using UnityEngine;

// Selected Single 1 / Multi 3 / Support 1 presentation. Receives actual shot age, never fires attacks.
public sealed class CoreGuardianTowerGlow : IDisposable
{
    sealed class Layer
    {
        public SpriteRenderer source;
        public MeshRenderer renderer;
        public GameObject root;
        public Mesh mesh;
        public Material material;
    }

    static readonly int AgeId = Shader.PropertyToID("_Age"),
        ClockId = Shader.PropertyToID("_Clock"),
        PulseScaleId = Shader.PropertyToID("_PulseScale");
    readonly List<Layer> layers = new List<Layer>();
    float clock;
    bool enabled = true,
        disposed;

    public CoreGuardianTowerGlow(SpriteRenderer artwork, string role)
    {
        int index =
            role == "Single" ? 0
            : role == "Multi" ? 1
            : role == "Support" ? 2
            : -1;
        if (index < 0 || artwork == null)
        {
            throw new ArgumentException("Original tower artwork and role required");
        }

        var shader = Resources.Load<Shader>("CoreGuardian/TowerGlow");
        if (shader == null)
        {
            throw new InvalidOperationException("Missing selected tower glow shader");
        }

        foreach (var source in artwork.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (
                source != artwork
                && source.name != "Fixed"
                && source.name != "Moving"
                && !source.name.StartsWith("Housing")
            )
            {
                continue;
            }

            var sprite = source.sprite;
            var rect = CoreGuardianSpriteAtlasSources.OriginalRect(sprite, out var texture);
            var bounds = sprite.bounds;
            float pad = 3 / sprite.pixelsPerUnit;
            var root = new GameObject("TowerGlow");
            root.transform.SetParent(source.transform, false);
            var mesh = new Mesh { name = "Tower original blue-mask glow" };
            mesh.vertices = new[]
            {
                new Vector3(bounds.min.x - pad, bounds.min.y - pad),
                new Vector3(bounds.max.x + pad, bounds.min.y - pad),
                new Vector3(bounds.max.x + pad, bounds.max.y + pad),
                new Vector3(bounds.min.x - pad, bounds.max.y + pad),
            };
            float w = texture.width,
                h = texture.height;
            mesh.uv = new[]
            {
                new Vector2((rect.xMin - 3) / w, (rect.yMin - 3) / h),
                new Vector2((rect.xMax + 3) / w, (rect.yMin - 3) / h),
                new Vector2((rect.xMax + 3) / w, (rect.yMax + 3) / h),
                new Vector2((rect.xMin - 3) / w, (rect.yMax + 3) / h),
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            var material = new Material(shader) { name = "TowerGlow " + role };
            material.mainTexture = texture;
            material.SetVector("_UVRect", new Vector4(rect.xMin / w, rect.yMin / h, rect.xMax / w, rect.yMax / h));
            material.SetFloat("_Role", index);
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            layers.Add(
                new Layer
                {
                    source = source,
                    renderer = renderer,
                    root = root,
                    mesh = mesh,
                    material = material,
                }
            );
        }
        Present(100, .28f, 0);
    }

    public void Present(float shotAge, float shotDuration, float delta)
    {
        if (disposed)
            return;

        clock += Mathf.Max(0, delta);
        float pulseScale = Mathf.Clamp(shotDuration / .28f, .1f, 1);
        foreach (var layer in layers)
        {
            if (layer.renderer == null || layer.source == null || layer.material == null)
                continue;

            // F28: renderer state written only when it changes; the shader inputs below move every frame.
            bool show = enabled && layer.source.enabled;
            if (layer.renderer.enabled != show)
            {
                layer.renderer.enabled = show;
            }
            if (layer.renderer.sortingLayerID != layer.source.sortingLayerID)
            {
                layer.renderer.sortingLayerID = layer.source.sortingLayerID;
            }
            if (layer.renderer.sortingOrder != layer.source.sortingOrder + 3)
            {
                layer.renderer.sortingOrder = layer.source.sortingOrder + 3;
            }
            layer.material.SetFloat(AgeId, Mathf.Max(0, shotAge));
            layer.material.SetFloat(ClockId, clock);
            layer.material.SetFloat(PulseScaleId, pulseScale);
        }
    }

    public void Clear()
    {
        enabled = false;
        foreach (var layer in layers)
        {
            if (layer.renderer != null)
            {
                layer.renderer.enabled = false;
            }
        }
    }

    public void Enable()
    {
        enabled = true;
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        foreach (var layer in layers)
        {
            DestroyOwned(layer.root);
            DestroyOwned(layer.mesh);
            DestroyOwned(layer.material);
        }
        layers.Clear();
    }

    static void DestroyOwned(UnityEngine.Object value)
    {
        if (value == null)
            return;

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(value);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
