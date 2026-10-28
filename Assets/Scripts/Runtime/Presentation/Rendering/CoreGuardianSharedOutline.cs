using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

// Default ON since 2026-10-24 (outline redesign plan, option A; see docs/development/shared_outline_20261024).
// All always-on emphasis bodies write their entity id into ONE screen-size mask, drawn in visual
// sorting order so each pixel keeps the front-most body. Each body keeps its own output quad in the
// original sorting slot (front part +3), so overlap order with other bodies/HUD/effects is unchanged.
// Selection (HUD) and upgrade overlays keep the original per-body CoreGuardianSelectionOutline path.
internal sealed partial class CoreGuardianSharedOutline
{
    static readonly ProfilerMarker maskSample = new ProfilerMarker("CG.SharedOutline.Mask");
    static readonly ProfilerMarker outputSample = new ProfilerMarker("CG.SharedOutline.Output");
    static readonly ProfilerMarker collectSample = new ProfilerMarker("CG.SharedOutline.Collect");
    static readonly ProfilerMarker sortSample = new ProfilerMarker("CG.SharedOutline.Sort");
    static readonly ProfilerMarker recordSample = new ProfilerMarker("CG.SharedOutline.Record");
    static readonly ProfilerMarker executeSample = new ProfilerMarker("CG.SharedOutline.Execute");
    static readonly int MaskTextureId = Shader.PropertyToID("_CGSharedOutlineMask"),
        LayoutId = Shader.PropertyToID("_CGSharedOutlineLayout"),
        ViewProjectionId = Shader.PropertyToID("_CGSharedOutlineVP"),
        ThresholdId = Shader.PropertyToID("_CGSharedOutlineThreshold"),
        MaskEntityId = Shader.PropertyToID("_CGMaskEntity"),
        OutputEntityId = Shader.PropertyToID("_CGOutlineEntity"),
        ColorId = Shader.PropertyToID("_Color"),
        WidthId = Shader.PropertyToID("_Width"),
        MaskDepthId = Shader.PropertyToID("_CGMaskDepth"),
        PartsBufferId = Shader.PropertyToID("_CGMaskParts"),
        MainTexId = Shader.PropertyToID("_MainTex");

    // Batched mask (2026-10-24 next optimisation #1): parts are drawn grouped by sprite texture in a few
    // DrawMesh calls; draw order no longer decides the front body, a per-part depth from the visual sort does.
    public static bool Batched { get; } = true;

    static readonly bool batchSupported =
        SystemInfo.supportsComputeShaders
        && (
            SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11
            || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12
        );

    // Adopted 2026-10-24 (user decision after step 2/3 comparison). SetEnabled(false) restores the original per-body path.
    public static bool Enabled { get; private set; } = true;

    public static void SetEnabled(bool value) => Enabled = value;

    struct Part
    {
        public SpriteRenderer renderer;
        public Transform transform;
        public int layer,
            order,
            entity,
            sequence;
        public float z,
            alpha;
    }

    struct Entry
    {
        public SpriteRenderer output;
        public Bounds bounds;
        public int frontLayerId,
            frontOrder;
    }

    sealed class PartOrder : IComparer<Part>
    {
        // Back to front like the 2D transparent queue: sorting layer, order, then farther z first.
        public int Compare(Part a, Part b)
        {
            if (a.layer != b.layer)
                return a.layer < b.layer ? -1 : 1;

            if (a.order != b.order)
                return a.order < b.order ? -1 : 1;

            if (a.z != b.z)
                return a.z > b.z ? -1 : 1;

            return a.sequence.CompareTo(b.sequence);
        }
    }

    static readonly PartOrder partOrder = new PartOrder();

    // List.Sort(IComparer) allocates a delegate per call; a cached Comparison runs the same sort without allocating.
    static readonly System.Comparison<Part> partCompare = partOrder.Compare;

    readonly List<Part> parts = new List<Part>();
    readonly List<Entry> entries = new List<Entry>();
    readonly Dictionary<Sprite, SpriteGeometry> geometries = new Dictionary<Sprite, SpriteGeometry>();
    readonly Dictionary<Texture, int> groupOf = new Dictionary<Texture, int>();
    readonly List<Texture> groupTextures = new List<Texture>();
    readonly List<List<int>> groupTriangles = new List<List<int>>();
    readonly List<Vector3> batchVertices = new List<Vector3>();
    readonly List<Vector2> batchUV = new List<Vector2>(),
        batchIndex = new List<Vector2>();
    readonly List<PartItem> items = new List<PartItem>();
    readonly List<int> fallback = new List<int>();
    readonly HashSet<SpriteRenderer> customOutputs = new HashSet<SpriteRenderer>();

    // Sorting layer values looked up once per presentation (layer order cannot change inside one frame).
    readonly Dictionary<int, int> layerValues = new Dictionary<int, int>();

    // Mask geometry reuse: the batch mesh is laid out in collection order and only rebuilt when the sequence of
    // (sprite, flip) changes. Per-frame data (matrix, entity, depth rank, alpha) lives in the part buffer, and the
    // depth test (not draw order) decides the front part, so the rasterized mask is identical.
    const bool ReuseGeometry = true;
    readonly List<Sprite> layoutSprites = new List<Sprite>();
    readonly List<int> layoutFlips = new List<int>();
    readonly List<int> rankOfSequence = new List<int>();
    readonly List<int> eligibleSequence = new List<int>();
    bool layoutValid;
    Mesh batchMesh;
    GraphicsBuffer partBuffer;
    MaterialPropertyBlock groupProperties;
    public int LastMaskDraws { get; private set; }
    MaterialPropertyBlock outputProperties;
    Material maskMaterial,
        outputMaterial;
    Texture2D outlineTexture;
    Sprite outlineSprite;

    // Ids 1..MaxBatchedEntity ride in the renderer alpha byte (shared material, sprite batching); larger ids use a property block.
    const int MaxBatchedEntity = 255;
    CommandBuffer commands;
    RenderTexture mask;
    Camera camera;
    Transform parent;
    Color color;
    int radius;
    float threshold;
    bool active;

    public int LastEntityCount { get; private set; }
    public int LastPartCount { get; private set; }

    // Only the gameplay camera setup that the quad placement below assumes.
    static bool? rfloatSupported;

    public static bool Supports(Camera camera, Transform parent)
    {
        if (camera == null || !camera.orthographic || camera.stereoEnabled || camera.allowDynamicResolution)
        {
            return false;
        }

        if (!camera.transform.rotation.Equals(Quaternion.identity))
            return false;

        var viewport = camera.pixelRect;
        if (viewport.x != 0 || viewport.y != 0 || viewport.width <= 0 || viewport.height <= 0)
            return false;

        if (parent != null && !parent.rotation.Equals(Quaternion.identity))
            return false;
        // SupportsRenderTextureFormat validates the enum through reflection (allocates on every call); the answer is fixed per device.
        rfloatSupported ??= SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat);
        return rfloatSupported.Value;
    }

    public void Begin(Camera camera, Transform parent, int radius, Color color, float threshold)
    {
        this.camera = camera;
        this.parent = parent;
        this.radius = Mathf.Clamp(radius, 0, 8);
        this.color = color;
        this.threshold = Mathf.Clamp(threshold, .01f, 1);
        parts.Clear();
        entries.Clear();
        layerValues.Clear();
        active = camera != null && this.radius > 0 && color.a > 0;
    }

    // Returns false when the body has no visible source part; its output is hidden.
    public bool Add(GameObject host, ref SpriteRenderer output, List<SpriteRenderer> sources)
    {
        if (!active || host == null || sources == null)
        {
            Hide(output);
            return false;
        }
        using var collectScope = collectSample.Auto();
        int entity = entries.Count + 1,
            first = parts.Count;
        SpriteRenderer front = null;
        int frontLayer = 0;
        Bounds bounds = default;
        foreach (var r in sources)
        {
            // Same source filter as CoreGuardianSelectionOutline.PresentMask. The only caller (ObjectEmphasis) passes
            // sources it filtered by activeInHierarchy this same frame, so that native check is not repeated here.
            if (r == null || !r.enabled || r.sprite == null)
                continue;

            float alpha = r.color.a;
            if (alpha < threshold)
                continue;

            int layerId = r.sortingLayerID;
            if (!layerValues.TryGetValue(layerId, out int layer))
            {
                layer = SortingLayer.GetLayerValueFromID(layerId);
                layerValues.Add(layerId, layer);
            }
            if (front == null)
            {
                front = r;
                frontLayer = layer;
                bounds = r.bounds;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
                // >= keeps the original stable OrderBy(...).ThenBy(...).Last() tie behaviour.
                if (layer > frontLayer || (layer == frontLayer && r.sortingOrder >= front.sortingOrder))
                {
                    front = r;
                    frontLayer = layer;
                }
            }
            var t = r.transform;
            parts.Add(
                new Part
                {
                    renderer = r,
                    transform = t,
                    layer = layer,
                    order = r.sortingOrder,
                    entity = entity,
                    sequence = parts.Count,
                    z = t.position.z,
                    alpha = alpha,
                }
            );
        }
        if (front == null)
        {
            Hide(output);
            return false;
        }
        EnsureResources();
        if (output == null)
        {
            // Own child: never touches the host or the original SelectionOutline output transform.
            var child = new GameObject("SharedOutline") { hideFlags = HideFlags.DontSave };
            child.transform.SetParent(host.transform, false);
            output = child.AddComponent<SpriteRenderer>();
            output.sprite = outlineSprite;
            output.sharedMaterial = outputMaterial;
        }
        entries.Add(
            new Entry
            {
                output = output,
                bounds = bounds,
                frontLayerId = front.sortingLayerID,
                frontOrder = front.sortingOrder,
            }
        );
        return true;
    }

    public static void Hide(SpriteRenderer output)
    {
        if (output != null)
        {
            output.enabled = false;
        }
    }

    public void End()
    {
        LastEntityCount = entries.Count;
        LastPartCount = parts.Count;
        if (!active || entries.Count == 0)
            return;

        var viewport = camera.pixelRect;
        int width = (int)viewport.width,
            height = (int)viewport.height,
            pad = radius + 1;
        using (maskSample.Auto())
        {
            using (sortSample.Auto())
            {
                parts.Sort(partCompare);
            }

            EnsureMask(width + pad * 2, height + pad * 2);
            // Expand the camera projection by `pad` pixels on every side so off-screen neighbours stay in the mask.
            float ax = -pad / (float)width * 2 - 1,
                bx = (width + pad) / (float)width * 2 - 1,
                ay = -pad / (float)height * 2 - 1,
                by = (height + pad) / (float)height * 2 - 1;
            var crop = Matrix4x4.identity;
            crop.m00 = 2 / (bx - ax);
            crop.m11 = 2 / (by - ay);
            crop.m03 = -(bx + ax) / (bx - ax);
            crop.m13 = -(by + ay) / (by - ay);
            var vp = GL.GetGPUProjectionMatrix(crop * camera.projectionMatrix, true) * camera.worldToCameraMatrix;
            commands.Clear();
            commands.SetRenderTarget(mask);
            // Colour 0 = no body; depth 0 = farthest. Each part writes depth (rank+1)/(N+1) and ZTest Greater keeps
            // the visually front-most part, exactly like the previous sorted overwrite.
            commands.ClearRenderTarget(true, true, Color.clear, 0f);
            commands.SetGlobalMatrix(ViewProjectionId, vp);
            commands.SetGlobalFloat(ThresholdId, threshold);
            using (recordSample.Auto())
            {
                if (Batched && batchSupported)
                {
                    RecordBatched();
                }
                else
                {
                    LastMaskDraws = 0;
                    int current = -1;
                    for (int i = 0; i < parts.Count; i++)
                    {
                        RecordFallback(i, ref current);
                    }
                }
            }
            using (executeSample.Auto())
            {
                Graphics.ExecuteCommandBuffer(commands);
            }

            Shader.SetGlobalTexture(MaskTextureId, mask);
            Shader.SetGlobalVector(LayoutId, new Vector4(pad, pad, width, height));
        }
        using (outputSample.Auto())
        {
            outputMaterial.SetColor(ColorId, color);
            outputMaterial.SetInt(WidthId, radius);
            float pixel = camera.orthographicSize * 2 / height;
            float margin = (radius + 2) * pixel * 2;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var output = entry.output;
                var t = output.transform;
                var size = entry.bounds.size;
                var parentScale = t.parent != null ? t.parent.lossyScale : Vector3.one;
                t.position = entry.bounds.center;
                t.localScale = new Vector3((size.x + margin) / parentScale.x, (size.y + margin) / parentScale.y, 1);
                output.sortingLayerID = entry.frontLayerId;
                output.sortingOrder = entry.frontOrder + 3;
                int entity = i + 1;
                if (entity <= MaxBatchedEntity)
                {
                    output.color = new Color(1, 1, 1, entity / 255f);
                    if (customOutputs.Remove(output))
                    {
                        output.SetPropertyBlock(null);
                    }
                }
                else
                {
                    output.color = Color.white;
                    outputProperties.Clear();
                    outputProperties.SetFloat(OutputEntityId, entity);
                    output.SetPropertyBlock(outputProperties);
                    customOutputs.Add(output);
                }
                if (output.sprite != outlineSprite)
                {
                    output.sprite = outlineSprite;
                }

                output.enabled = true;
            }
        }
    }

    float Depth(int rank) => (rank + 1) / (float)(parts.Count + 1);

    public static int LayoutRebuilds { get; private set; }
}
