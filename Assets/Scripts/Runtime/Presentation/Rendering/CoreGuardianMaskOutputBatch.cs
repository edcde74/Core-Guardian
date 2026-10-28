using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Only independently ordered, disjoint output quads are combined. Unproven ordering keeps the original.
public static class CoreGuardianMaskOutputBatch
{
    public struct OutputParameters
    {
        public bool contact;
        public bool allowSingle;
        public bool prepared;
        public Color color;
        public int width;
        public float band;
        public Vector2 maskOriginPx,
            sourceSize;
    }

    sealed class Entry
    {
        internal CoreGuardianSelectionOutline owner;
        internal Camera camera;
        internal MeshRenderer source;
        internal Mesh quad;
        internal CoreGuardianMaskPage.Slot slot;
        internal OutputParameters parameters;
        internal RenderTexture mask;
        internal int frame;
        internal Rect screen;
        internal Material originalMaterial;
        internal readonly MaterialPropertyBlock savedProperties = new MaterialPropertyBlock();
    }

    sealed class Proxy
    {
        internal Mesh mesh;
        internal MeshRenderer renderer;
    }

    sealed class View
    {
        internal Camera camera;
        internal readonly List<Proxy> proxies = new List<Proxy>();
        internal readonly List<MeshRenderer> hidden = new List<MeshRenderer>();
        internal readonly List<Entry> single = new List<Entry>();
    }

    static readonly Dictionary<CoreGuardianSelectionOutline, Entry> entries =
        new Dictionary<CoreGuardianSelectionOutline, Entry>();
    static readonly Dictionary<Camera, View> views = new Dictionary<Camera, View>();
    static readonly Dictionary<(RenderTexture, int, int, int), List<Entry>> groups =
        new Dictionary<(RenderTexture, int, int, int), List<Entry>>();
    static readonly Stack<List<Entry>> groupPool = new Stack<List<Entry>>();
    static readonly Dictionary<Renderer, Entry> bySource = new Dictionary<Renderer, Entry>();
    static readonly List<Entry> safe = new List<Entry>();
    static readonly List<CoreGuardianSelectionOutline> stale = new List<CoreGuardianSelectionOutline>();
    static readonly List<Camera> staleViews = new List<Camera>();
    static readonly List<Vector3> positions = new List<Vector3>();
    static readonly List<Color> colors = new List<Color>();
    static readonly List<Vector4>[] uv =
    {
        new List<Vector4>(),
        new List<Vector4>(),
        new List<Vector4>(),
        new List<Vector4>(),
        new List<Vector4>(),
        new List<Vector4>(),
        new List<Vector4>(),
        new List<Vector4>(),
    };
    static readonly List<int> triangles = new List<int>();
    static readonly List<Vector3> quadPositions = new List<Vector3>();
    static readonly List<Vector2> quadUV = new List<Vector2>();
    static readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
    static readonly ProfilerMarker flushMarker = new ProfilerMarker("CG.Mask.OutputBatch");
    static Material material;
    public static int LastBatchedOutputs { get; private set; }
    public static int LastDraws { get; private set; }

    static CoreGuardianMaskOutputBatch()
    {
        RenderPipelineManager.beginCameraRendering += Begin;
        RenderPipelineManager.endCameraRendering += End;
        SceneManager.sceneUnloaded += scene => Prune();
        Application.quitting += Clear;
#if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Clear;
        UnityEditor.EditorApplication.playModeStateChanged += state =>
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                Clear();
            }
        };
#endif
    }

    public static bool Register(
        CoreGuardianSelectionOutline owner,
        Camera camera,
        MeshRenderer source,
        Mesh quad,
        in CoreGuardianMaskPage.Slot slot,
        in OutputParameters parameters
    )
    {
        if (owner == null || camera == null || source == null || quad == null || !CoreGuardianMaskPage.IsValid(slot))
        {
            return false;
        }

        if (!entries.TryGetValue(owner, out var entry))
        {
            entry = new Entry();
            entries.Add(owner, entry);
        }
        else if (!entry.parameters.prepared && entry.slot.lease != slot.lease)
        {
            CoreGuardianMaskPage.Release(entry.slot);
        }

        source.GetPropertyBlock(properties);
        var mask = properties.GetTexture("_MainTex") as RenderTexture;
        if (mask == null)
            return false;

        entry.owner = owner;
        entry.camera = camera;
        entry.source = source;
        entry.quad = quad;
        entry.slot = slot;
        entry.parameters = parameters;
        entry.mask = mask;
        entry.frame = Time.frameCount;
        bySource[source] = entry;
        if (!views.ContainsKey(camera))
        {
            views.Add(camera, new View { camera = camera });
        }

        return true;
    }

    static void Begin(ScriptableRenderContext context, Camera camera)
    {
        try
        {
            Flush(camera);
        }
        catch (System.Exception e)
        {
            Restore(camera);
            Debug.LogException(e);
        }
    }

    static void End(ScriptableRenderContext context, Camera camera) => Restore(camera);

    public static void Flush(Camera camera)
    {
        using var scope = flushMarker.Auto();
        LastBatchedOutputs = LastDraws = 0;
        Restore(camera);
        CoreGuardianBodyMaskCache.Flush(camera);
        Prune();
        if (camera == null || !views.TryGetValue(camera, out var view) || !camera.orthographic || camera.stereoEnabled)
        {
            return;
        }

        foreach (var list in groups.Values)
        {
            list.Clear();
            groupPool.Push(list);
        }
        groups.Clear();
        foreach (var entry in entries.Values)
        {
            var source = entry.source;
            if (
                entry.camera != camera
                || entry.frame != Time.frameCount
                || source == null
                || !source.enabled
                || source.forceRenderingOff
                || !source.gameObject.activeInHierarchy
                || (camera.cullingMask & (1 << source.gameObject.layer)) == 0
                || source.GetComponentInParent<SortingGroup>() != null
                || !CoreGuardianMaskPage.IsValid(entry.slot)
                || source.sharedMaterial == null
                || source.sharedMaterial.renderQueue != 3000
            )
            {
                continue;
            }

            entry.screen = ScreenRect(camera, source.bounds);
            if (entry.parameters.contact)
            {
                entry.screen = new Rect(
                    entry.screen.x,
                    entry.screen.y,
                    entry.screen.width,
                    Mathf.Min(entry.screen.height, Mathf.Max(0, entry.parameters.band) + 1)
                );
            }

            var key = (entry.slot.Texture, source.sortingLayerID, source.sortingOrder, source.gameObject.layer);
            if (!groups.TryGetValue(key, out var list))
            {
                list = groupPool.Count > 0 ? groupPool.Pop() : new List<Entry>();
                groups.Add(key, list);
            }
            list.Add(entry);
        }
        if (groups.Count == 0)
            return;
        // FindObjectsByType omits DontSave presentation renderers, including our original outputs.
        bool merge = false;
        foreach (var list in groups.Values)
        {
            foreach (var entry in list)
            {
                if (!entry.parameters.allowSingle)
                {
                    merge = true;
                    break;
                }
            }
        }

        var all = merge ? Resources.FindObjectsOfTypeAll<Renderer>() : System.Array.Empty<Renderer>();
        // Nested SortingGroup keys can override child renderer keys. Keep their original path.
        if (merge && Object.FindObjectsByType<SortingGroup>(FindObjectsInactive.Exclude).Length != 0)
            return;

        int used = 0;
        foreach (var candidates in groups.Values)
        {
            if (!merge)
                break;

            safe.Clear();
            foreach (var candidate in candidates)
            {
                if (Safe(camera, candidate, all))
                {
                    safe.Add(candidate);
                }
            }

            var group = safe;
            if (group.Count < 2)
                continue;

            if (material == null)
            {
                var shader = Resources.Load<Shader>("CoreGuardian/BatchedMaskOutput");
                if (shader == null || !shader.isSupported)
                    return;

                material = new Material(shader) { name = "Shared atlas mask output", hideFlags = HideFlags.DontSave };
            }
            foreach (var entry in group)
            {
                var r = entry.slot.Rect;
                if (!entry.parameters.prepared)
                {
                    Graphics.CopyTexture(
                        entry.mask,
                        0,
                        0,
                        0,
                        0,
                        entry.mask.width,
                        entry.mask.height,
                        entry.slot.Texture,
                        0,
                        0,
                        r.x,
                        r.y
                    );
                }
            }
            if (used == view.proxies.Count)
            {
                view.proxies.Add(Create());
            }

            var proxy = view.proxies[used++];
            Upload(proxy.mesh, group);
            var first = group[0];
            proxy.renderer.gameObject.layer = first.source.gameObject.layer;
            proxy.renderer.sortingLayerID = first.source.sortingLayerID;
            proxy.renderer.sortingOrder = first.source.sortingOrder;
            proxy.renderer.sharedMaterial = material;
            properties.Clear();
            properties.SetTexture("_MainTex", first.slot.Texture);
            proxy.renderer.SetPropertyBlock(properties);
            proxy.renderer.enabled = true;
            foreach (var entry in group)
            {
                entry.source.forceRenderingOff = true;
                view.hidden.Add(entry.source);
            }
            LastBatchedOutputs += group.Count;
            LastDraws++;
        }
        foreach (var candidates in groups.Values)
        {
            foreach (var entry in candidates)
            {
                if (!entry.parameters.allowSingle || entry.source.forceRenderingOff)
                    continue;

                if (material == null)
                {
                    var shader = Resources.Load<Shader>("CoreGuardian/BatchedMaskOutput");
                    if (shader == null || !shader.isSupported)
                        return;

                    material = new Material(shader)
                    {
                        name = "Shared atlas mask output",
                        hideFlags = HideFlags.DontSave,
                    };
                }
                var r = entry.slot.Rect;
                var p = entry.parameters;
                if (!p.prepared)
                {
                    Graphics.CopyTexture(
                        entry.mask,
                        0,
                        0,
                        0,
                        0,
                        entry.mask.width,
                        entry.mask.height,
                        entry.slot.Texture,
                        0,
                        0,
                        r.x,
                        r.y
                    );
                }

                entry.originalMaterial = entry.source.sharedMaterial;
                entry.source.GetPropertyBlock(entry.savedProperties);
                properties.Clear();
                properties.SetTexture("_MainTex", entry.slot.Texture);
                properties.SetFloat("_Single", 1);
                properties.SetVector("_Region", new Vector4(r.x, r.y, p.sourceSize.x, p.sourceSize.y));
                properties.SetVector("_Parameters", new Vector4(p.width, p.band, p.maskOriginPx.y, p.contact ? 1 : 0));
                properties.SetColor("_Color", p.color);
                entry.source.sharedMaterial = material;
                entry.source.SetPropertyBlock(properties);
                view.single.Add(entry);
                LastBatchedOutputs++;
                LastDraws++;
            }
        }
    }

    static bool Safe(Camera camera, Entry entry, Renderer[] all)
    {
        var first = entry.source;
        foreach (var renderer in all)
        {
            if (
                renderer == null
                || !renderer.enabled
                || renderer.forceRenderingOff
                || !renderer.gameObject.activeInHierarchy
                || !renderer.gameObject.scene.IsValid()
                || (camera.cullingMask & (1 << renderer.gameObject.layer)) == 0
            )
            {
                continue;
            }

            if (renderer.sortingLayerID != first.sortingLayerID || renderer.sortingOrder != first.sortingOrder)
            {
                continue;
            }

            if (renderer == first)
                continue;

            var rect =
                bySource.TryGetValue(renderer, out var other)
                && other.camera == camera
                && other.frame == Time.frameCount
                    ? other.screen
                    : ScreenRect(camera, renderer.bounds);
            if (entry.screen.Overlaps(rect))
                return false;
        }
        return true;
    }

    static Rect ScreenRect(Camera camera, Bounds bounds)
    {
        var lo = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        var hi = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < 8; i++)
        {
            var p = camera.WorldToScreenPoint(
                bounds.center
                    + Vector3.Scale(
                        bounds.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)
                    )
            );
            lo = Vector2.Min(lo, p);
            hi = Vector2.Max(hi, p);
        }
        return Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y);
    }

    static Proxy Create()
    {
        var root = new GameObject("Shared mask output batch") { hideFlags = HideFlags.DontSave };
        var mesh = new Mesh { name = "Shared mask output geometry", hideFlags = HideFlags.DontSave };
        mesh.MarkDynamic();
        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = root.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.enabled = false;
        return new Proxy { mesh = mesh, renderer = renderer };
    }

    static void Upload(Mesh mesh, List<Entry> group)
    {
        positions.Clear();
        colors.Clear();
        triangles.Clear();
        foreach (var channel in uv)
        {
            channel.Clear();
        }

        var bounds = group[0].source.bounds;
        foreach (var entry in group)
        {
            entry.quad.GetVertices(quadPositions);
            entry.quad.GetUVs(0, quadUV);
            int offset = positions.Count;
            var p = entry.parameters;
            var rect = entry.slot.Rect;
            var matrix = entry.source.localToWorldMatrix;
            for (int i = 0; i < quadPositions.Count; i++)
            {
                positions.Add(quadPositions[i]);
                colors.Add(p.color);
                uv[0].Add(quadUV[i]);
                uv[1].Add(new Vector4(rect.x, rect.y, p.sourceSize.x, p.sourceSize.y));
                uv[2].Add(new Vector4(p.width, p.band, p.maskOriginPx.y, p.contact ? 1 : 0));
                uv[3].Add(Vector4.zero);
                for (int row = 0; row < 4; row++)
                {
                    uv[4 + row].Add(matrix.GetRow(row));
                }
            }
            triangles.Add(offset);
            triangles.Add(offset + 1);
            triangles.Add(offset + 2);
            triangles.Add(offset);
            triangles.Add(offset + 2);
            triangles.Add(offset + 3);
            bounds.Encapsulate(entry.source.bounds);
        }
        mesh.Clear();
        mesh.SetVertices(positions);
        mesh.SetColors(colors);
        for (int i = 0; i < 8; i++)
        {
            mesh.SetUVs(i, uv[i]);
        }

        mesh.SetTriangles(triangles, 0, false);
        mesh.bounds = bounds;
    }

    public static void Restore(Camera camera)
    {
        if (ReferenceEquals(camera, null) || !views.TryGetValue(camera, out var view))
            return;

        foreach (var source in view.hidden)
        {
            if (source != null)
            {
                source.forceRenderingOff = false;
            }
        }

        view.hidden.Clear();
        foreach (var entry in view.single)
        {
            if (entry.source != null && entry.source.sharedMaterial == material)
            {
                entry.source.sharedMaterial = entry.originalMaterial;
                entry.source.SetPropertyBlock(entry.savedProperties);
            }
        }

        view.single.Clear();
        foreach (var proxy in view.proxies)
        {
            if (proxy.renderer != null)
            {
                proxy.renderer.enabled = false;
            }
        }
    }

    public static void Release(CoreGuardianSelectionOutline owner)
    {
        if (ReferenceEquals(owner, null) || !entries.TryGetValue(owner, out var entry))
            return;

        if (!entry.parameters.prepared)
        {
            CoreGuardianMaskPage.Release(entry.slot);
        }

        bySource.Remove(entry.source);
        entries.Remove(owner);
        if (entries.Count == 0)
        {
            Prune();
        }
    }

    internal static void Invalidate(CoreGuardianSelectionOutline owner)
    {
        if (entries.TryGetValue(owner, out var entry))
        {
            if (!entry.parameters.prepared)
            {
                CoreGuardianMaskPage.Release(entry.slot);
            }
            entry.frame = -1;
        }
    }

    static void Prune()
    {
        stale.Clear();
        foreach (var pair in entries)
        {
            if (pair.Key == null || pair.Value.camera == null || pair.Value.source == null)
            {
                stale.Add(pair.Key);
            }
        }

        foreach (var owner in stale)
        {
            var entry = entries[owner];
            CoreGuardianMaskPage.Release(entry.slot);
            bySource.Remove(entry.source);
            entries.Remove(owner);
        }
        staleViews.Clear();
        foreach (var view in views.Values)
        {
            bool used = false;
            foreach (var entry in entries.Values)
            {
                if (entry.camera == view.camera)
                {
                    used = true;
                    break;
                }
            }

            if (!used)
            {
                staleViews.Add(view.camera);
            }
        }
        foreach (var camera in staleViews)
        {
            var view = views[camera];
            Restore(camera);
            foreach (var proxy in view.proxies)
            {
                Dispose(proxy.mesh);
                if (proxy.renderer != null)
                {
                    Dispose(proxy.renderer.gameObject);
                }
            }
            if (!CoreGuardianBodyMaskCache.HasPage(camera))
            {
                CoreGuardianMaskPage.ReleaseCamera(camera);
            }
            views.Remove(camera);
        }
        if (views.Count == 0)
        {
            Dispose(material);
            material = null;
        }
    }

    static void Clear()
    {
        foreach (var view in views.Values)
        {
            Restore(view.camera);
            foreach (var p in view.proxies)
            {
                Dispose(p.mesh);
                if (p.renderer != null)
                {
                    Dispose(p.renderer.gameObject);
                }
            }
        }
        views.Clear();
        entries.Clear();
        bySource.Clear();
        Dispose(material);
        material = null;
    }

    static void Dispose(Object value)
    {
        if (value == null)
            return;

        if (Application.isPlaying)
        {
            Object.Destroy(value);
        }
        else
        {
            Object.DestroyImmediate(value);
        }
    }
}
