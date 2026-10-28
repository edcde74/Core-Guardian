using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

// Selected B2: original body tint plus a separate union silhouette, below inspection and upgrades.
[DisallowMultipleComponent]
[DefaultExecutionOrder(450)]
public sealed class CoreGuardianObjectEmphasis : MonoBehaviour
{
    static readonly ProfilerMarker presentSample = new ProfilerMarker("CG.Emphasis.Present");
    static readonly ProfilerMarker tintSample = new ProfilerMarker("CG.Emphasis.Tint");

    [Header("개체 강조 B2 · 상시 윤곽")]
    [Tooltip("상시 윤곽의 색과 불투명도. 초록색 선택 아웃라인과 별도입니다.")]
    public Color outlineColor = new Color(.06f, .065f, .075f, .90f);

    [Range(0, 8), Tooltip("게임 화면 기준 픽셀 두께. 0이면 윤곽만 숨깁니다. Edit/Play에서 조절 가능합니다.")]
    public int outlineWidthPixels = 1;

    [
        Range(1, 1.5f),
        Tooltip(
            "원본 본체 RGB 배율. 1이면 원래 밝기, 선택 시안은 1.15입니다. 그림자·체력바·별도 발광에는 적용하지 않습니다."
        )
    ]
    public float bodyBrightness = 1.15f;

    [Range(.01f, 1), Tooltip("윤곽 마스크에 포함할 본체 알파 기준입니다.")]
    public float alphaThreshold = .1f;
    public int ActiveTargetCount => states.Count;

    sealed class Tint
    {
        // Brightness goes into a shared twin material's _Color: dynamic sprite batching (SRP Batcher off) bakes
        // renderer.color into 8-bit vertex colours and would clamp RGB above 1. renderer.color is never changed.
        // The twin stays assigned while the part is emphasized (one material read per frame instead of a swap and a
        // restore around every render). Any material another system assigns (hit flash, feedback) becomes the new
        // source on the next frame; twins copied onto other renderers are mapped back to their source, never stacked.
        public Material original,
            twin;
        public float brightness = -1;
        public bool applied;
        public int seen;

        public void Apply(SpriteRenderer renderer, float value)
        {
            var current = renderer.sharedMaterial;
            if (applied && twin != null && brightness == value && ReferenceEquals(current, twin))
                return;

            var source = SourceOf(current);
            if (!ReferenceEquals(source, original) || brightness != value)
            {
                original = source;
                twin = Brighter(source, value);
                brightness = value;
            }
            // No twin (brightness 1 or no _Color): show the source itself, never a stale twin.
            var target = twin != null ? twin : source;
            if (!ReferenceEquals(current, target))
            {
                renderer.sharedMaterial = target;
            }

            applied = true;
        }

        public void Restore(SpriteRenderer renderer)
        {
            // Another presentation system may have supplied a newer material; never overwrite it.
            if (renderer != null && applied && twin != null && renderer.sharedMaterial == twin)
            {
                renderer.sharedMaterial = original;
            }

            applied = false;
        }
    }

    // Twins are shared by every emphasis owner and live until Play mode ends, so a pooled or copied renderer that
    // still references one never shows a destroyed material. Keyed by (source, brightness).
    static readonly Dictionary<(Material, float), Material> twinCache = new Dictionary<(Material, float), Material>();
    static readonly Dictionary<Material, (Material source, float brightness)> twinInfo =
        new Dictionary<Material, (Material, float)>();
    static readonly Dictionary<Material, int> twinSynced = new Dictionary<Material, int>();
    static int tintFrame;

    static Material SourceOf(Material material) =>
        material != null && twinInfo.TryGetValue(material, out var info) ? info.source : material;

    static Material Brighter(Material source, float brightness)
    {
        if (source == null || brightness <= 1)
            return null;

        if (!twinCache.TryGetValue((source, brightness), out var twin))
        {
            twin = source.HasProperty(ColorId)
                ? new Material(source) { name = source.name + " (CG emphasis)", hideFlags = HideFlags.HideAndDontSave }
                : null;
            twinCache[(source, brightness)] = twin;
            if (twin != null)
            {
                twinInfo[twin] = (source, brightness);
                twinSynced[twin] = -1;
            }
        }
        if (twin != null && twinSynced[twin] != tintFrame)
        {
            Sync(twin);
        }

        return twin;
    }

    // Twins follow later edits of their source material once per presentation.
    static void Sync(Material twin)
    {
        var (source, brightness) = twinInfo[twin];
        if (source == null)
            return;

        twin.CopyPropertiesFromMaterial(source);
        var c = source.GetColor(ColorId);
        twin.SetColor(ColorId, new Color(c.r * brightness, c.g * brightness, c.b * brightness, c.a));
        twinSynced[twin] = tintFrame;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetTwins()
    {
        ReleaseTwins();
        Application.quitting -= ReleaseTwins;
        Application.quitting += ReleaseTwins;
    }

    static void ReleaseTwins()
    {
        foreach (var twin in twinInfo.Keys)
        {
            if (twin != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(twin);
                }
                else
                {
                    DestroyImmediate(twin);
                }
            }
        }

        twinCache.Clear();
        twinInfo.Clear();
        twinSynced.Clear();
    }

    sealed class Body
    {
        public GameObject host;
        public CoreGuardianSelectionOutline union;
        public Transform art;
        public SpriteRenderer sharedOutput;
        public bool unionPresented;
        public readonly Dictionary<SpriteRenderer, Tint> tints = new Dictionary<SpriteRenderer, Tint>();
        public readonly List<SpriteRenderer> sources = new List<SpriteRenderer>();
        public readonly CoreGuardianSpriteReferences references = new CoreGuardianSpriteReferences();
    }

    readonly Dictionary<Transform, Body> states = new Dictionary<Transform, Body>();
    readonly HashSet<Transform> live = new HashSet<Transform>();
    readonly List<Transform> targets = new List<Transform>(),
        removed = new List<Transform>();
    readonly List<KeyValuePair<SpriteRenderer, Tint>> appliedTints = new List<KeyValuePair<SpriteRenderer, Tint>>();
    static readonly int ColorId = Shader.PropertyToID("_Color");
    readonly List<Material> syncTwins = new List<Material>();
    int tintPass;

    // Always-on outline uses one shared screen mask (CoreGuardianSharedOutline, default ON). SetEnabled(false) = original path.
    // Created on first use: its MaterialPropertyBlock may not be built during MonoBehaviour construction.
    CoreGuardianSharedOutline sharedOutline;
    CoreGuardianPrototype game;
    Transform core;
    float nextCoreSearch;
    Camera presentedCamera;

    // F9: released emphasis hosts (outline component + shared output child) wait here for the next body.
    const int MaxPooledBodies = 64;
    readonly Stack<Body> freeBodies = new Stack<Body>();
    readonly List<SpriteRenderer> artSearch = new List<SpriteRenderer>();

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
    }

    void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (!isActiveAndEnabled || camera != presentedCamera)
            return;

        using var tintScope = tintSample.Auto();
        ApplyBodyTints();
    }

    void ApplyBodyTints()
    {
        float brightness = Mathf.Clamp(bodyBrightness, 1, 1.5f);
        int pass = ++tintPass;
        if (tintFrame != Time.frameCount || !Application.isPlaying)
        {
            tintFrame = Application.isPlaying ? Time.frameCount : tintFrame + 1;
            syncTwins.Clear();
            syncTwins.AddRange(twinInfo.Keys);
            foreach (var twin in syncTwins)
            {
                if (twin != null)
                {
                    Sync(twin);
                }
            }
        }
        foreach (var state in states.Values)
        {
            foreach (var renderer in state.sources)
            {
                if (
                    renderer != null
                    && renderer.enabled
                    && renderer.gameObject.activeInHierarchy
                    && renderer.sprite != null
                )
                {
                    if (!state.tints.TryGetValue(renderer, out var tint))
                    {
                        tint = new Tint();
                        state.tints.Add(renderer, tint);
                    }
                    if (!tint.applied)
                    {
                        appliedTints.Add(new KeyValuePair<SpriteRenderer, Tint>(renderer, tint));
                    }

                    tint.Apply(renderer, brightness);
                    tint.seen = pass;
                }
            }
        }
        // Parts that were hidden, left the body or were released this frame get their source material back.
        int write = 0;
        for (int i = 0; i < appliedTints.Count; i++)
        {
            var pair = appliedTints[i];
            if (pair.Key != null && pair.Value.applied && pair.Value.seen == pass)
            {
                appliedTints[write++] = pair;
                continue;
            }
            pair.Value.Restore(pair.Key);
        }
        appliedTints.RemoveRange(write, appliedTints.Count - write);
    }

    void RestoreBodyTints()
    {
        foreach (var pair in appliedTints)
        {
            pair.Value.Restore(pair.Key);
        }
        appliedTints.Clear();
    }

    // A released body may still hold a twin that another system copied or put back (e.g. hit flash restore).
    static void RestoreTwinCopies(Body state)
    {
        if (state.art == null)
            return;

        foreach (var r in state.art.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (
                r != null
                && r.sharedMaterial != null
                && twinInfo.TryGetValue(r.sharedMaterial, out var info)
                && info.source != null
            )
            {
                r.sharedMaterial = info.source;
            }
        }
    }

    void LateUpdate()
    {
        if (game == null)
        {
            game = GetComponent<CoreGuardianPrototype>();
        }

        if (game == null || !game.Ready)
            return;

        targets.Clear();
        game.GetCombatBodyTargets(targets);
        // F12: the scene-wide search runs at most once a second while the core is missing (it is found at once today).
        if (core == null && Time.unscaledTime >= nextCoreSearch)
        {
            nextCoreSearch = Time.unscaledTime + 1;
            foreach (var r in FindObjectsByType<SpriteRenderer>())
            {
                if (
                    r.gameObject.scene == gameObject.scene
                    && r.name == "2D Sprite"
                    && r.sprite != null
                    && r.sprite.name == "core"
                )
                {
                    core = r.transform.parent;
                    break;
                }
            }
        }

        if (core != null && core.gameObject.activeInHierarchy)
        {
            targets.Add(core);
        }

        PresentTargets(targets, Camera.main);
    }

    public void PresentTargets(IEnumerable<Transform> bodyTargets, Camera camera)
    {
        using var presentScope = presentSample.Auto();
        // renderer.color is never changed by emphasis, so animation inputs stay unmodified between renders.
        presentedCamera = camera;
        live.Clear();
        bool shared = CoreGuardianSharedOutline.Enabled && CoreGuardianSharedOutline.Supports(camera, transform);
        if (shared)
        {
            sharedOutline ??= new CoreGuardianSharedOutline();
            sharedOutline.Begin(camera, transform, outlineWidthPixels, outlineColor, alphaThreshold);
        }
        if (camera != null && bodyTargets != null)
        {
            // F2: a List is walked by index (the game's per-frame call); other sequences are copied once.
            var list = bodyTargets as List<Transform>;
            if (list == null)
            {
                otherTargets.Clear();
                otherTargets.AddRange(bodyTargets);
                list = otherTargets;
            }
            for (int index = 0; index < list.Count; index++)
            {
                var target = list[index];
                if (target == null || !target.gameObject.activeInHierarchy || !live.Add(target))
                    continue;

                if (!states.TryGetValue(target, out var state))
                {
                    Transform art = null;
                    target.GetComponentsInChildren(true, artSearch);
                    foreach (var r in artSearch)
                    {
                        if (r.name == "2D Sprite")
                        {
                            art = r.transform;
                            break;
                        }
                    }
                    artSearch.Clear();

                    if (art == null)
                        continue;

                    state = RentBody(target, art);
                    states.Add(target, state);
                }
                if (state.art == null)
                    continue;
                // Both consumers validate visibility when they use this same, structurally checked list.
                state.sources.Clear();
                var read = state.references.Read(state.art);
                var objects = state.references.GameObjects;
                for (int i = 0; i < read.Count; i++)
                {
                    var renderer = read[i];
                    if (renderer != null && objects[i].activeInHierarchy)
                    {
                        state.sources.Add(renderer);
                    }
                }
                if (shared)
                {
                    if (state.unionPresented)
                    {
                        state.union.ReleasePresentation();
                        state.unionPresented = false;
                    }
                    sharedOutline.Add(state.host, ref state.sharedOutput, state.sources);
                    continue;
                }
                CoreGuardianSharedOutline.Hide(state.sharedOutput);
                state.union.widthPixels = Mathf.Clamp(outlineWidthPixels, 0, 8);
                state.union.color = outlineColor;
                state.union.alphaThreshold = Mathf.Clamp(alphaThreshold, .01f, 1);
                state.union.PresentAtOrderWithSources(target, camera, 3, state.art, state.sources);
                state.unionPresented = true;
            }
        }

        if (shared)
        {
            sharedOutline.End();
        }

        removed.Clear();
        foreach (var pair in states)
        {
            if (pair.Key == null || !live.Contains(pair.Key))
            {
                removed.Add(pair.Key);
            }
        }

        foreach (var target in removed)
        {
            Recycle(states[target]);
            states.Remove(target);
        }
    }

    readonly List<Transform> otherTargets = new List<Transform>();

    Body RentBody(Transform target, Transform art)
    {
        while (freeBodies.Count > 0)
        {
            var pooled = freeBodies.Pop();
            if (pooled.host == null || pooled.union == null)
                continue;

            pooled.art = art;
            pooled.host.SetActive(true);
            return pooled;
        }

        var host = new GameObject("ObjectEmphasis_B2_" + target.name) { hideFlags = HideFlags.DontSave };
        host.transform.SetParent(transform, false);
        var union = host.AddComponent<CoreGuardianSelectionOutline>();
        union.enabled = false;
        return new Body
        {
            host = host,
            union = union,
            art = art,
        };
    }

    // Play mode keeps the host for the next body; everything that belongs to the old body is let go first.
    void Recycle(Body state)
    {
        if (!Application.isPlaying || state.host == null || state.union == null || freeBodies.Count >= MaxPooledBodies)
        {
            Release(state);
            return;
        }

        foreach (var pair in state.tints)
        {
            pair.Value.Restore(pair.Key);
        }

        RestoreTwinCopies(state);
        state.union.ReleasePresentation();
        state.union.InvalidateSources();
        CoreGuardianSharedOutline.Hide(state.sharedOutput);
        state.unionPresented = false;
        state.tints.Clear();
        state.sources.Clear();
        state.references.Invalidate();
        state.art = null;
        state.host.SetActive(false);
        freeBodies.Push(state);
    }

    static void Release(Body state)
    {
        foreach (var pair in state.tints)
        {
            pair.Value.Restore(pair.Key);
        }

        RestoreTwinCopies(state);
        if (state.host == null)
            return;

        if (state.union != null)
        {
            state.union.ReleasePresentation();
        }
        // Non-ExecuteAlways EditMode consumers may never receive OnDestroy.
        if (!Application.isPlaying && state.union != null)
        {
            state.union.ReleaseResources();
        }

        state.host.SetActive(false);
        if (Application.isPlaying)
        {
            Destroy(state.host);
        }
        else
        {
            DestroyImmediate(state.host);
        }
    }

    public void InvalidateSources()
    {
        foreach (var state in states.Values)
        {
            state.references.Invalidate();
            state.union.InvalidateSources();
        }
    }

    public void ReleaseTarget(Transform target)
    {
        if (target == null || !states.TryGetValue(target, out var state))
            return;

        Recycle(state);
        states.Remove(target);
        live.Remove(target);
    }

    void Clear()
    {
        RestoreBodyTints();
        foreach (var state in states.Values)
        {
            Release(state);
        }
        states.Clear();
        while (freeBodies.Count > 0)
        {
            Release(freeBodies.Pop());
        }
        presentedCamera = null;
        sharedOutline?.Release();
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
        Clear();
    }

    void OnDestroy()
    {
        Clear();
    }

    void OnValidate()
    {
        outlineWidthPixels = Mathf.Clamp(outlineWidthPixels, 0, 8);
        bodyBrightness = Mathf.Clamp(bodyBrightness, 1, 1.5f);
        alphaThreshold = Mathf.Clamp(alphaThreshold, .01f, 1);
    }
}
