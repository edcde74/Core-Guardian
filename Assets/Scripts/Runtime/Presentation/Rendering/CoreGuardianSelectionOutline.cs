using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

// One binary union of visible original sprite parts. Never changes source materials or artwork.
[DisallowMultipleComponent]
[DefaultExecutionOrder(500)]
public sealed partial class CoreGuardianSelectionOutline : MonoBehaviour
{
    static readonly ProfilerMarker presentSample = new ProfilerMarker("CG.Mask.Present");
    static readonly ProfilerMarker resourcesSample = new ProfilerMarker("CG.Mask.Resources");
    static readonly ProfilerMarker partsSample = new ProfilerMarker("CG.Mask.Parts");
    static readonly ProfilerMarker uploadSample = new ProfilerMarker("CG.Mask.MeshUpload");
    static readonly ProfilerMarker executeSample = new ProfilerMarker("CG.Mask.Execute");
    static readonly ProfilerMarker outputSample = new ProfilerMarker("CG.Mask.Output");
    static Shader contactShader;
    static readonly int MainTexId = Shader.PropertyToID("_MainTex"),
        MainTexTexelSizeId = Shader.PropertyToID("_MainTex_TexelSize"),
        MaskOriginId = Shader.PropertyToID("_MaskOriginPx");
    static readonly int ColorId = Shader.PropertyToID("_Color"),
        WidthId = Shader.PropertyToID("_Width"),
        BandId = Shader.PropertyToID("_Band"),
        MaskAtlasId = Shader.PropertyToID("_MaskAtlas");
    static readonly int UseAtlasId = Shader.PropertyToID("_UseAtlas"),
        OpacityId = Shader.PropertyToID("_Opacity"),
        ThresholdId = Shader.PropertyToID("_Threshold"),
        MaskVPId = Shader.PropertyToID("_MaskVP"),
        UsePartMatricesId = Shader.PropertyToID("_UsePartMatrices");

    [Header("선택 대상 아웃라인 · 능력치/강화 패널이 열려 있을 때만 표시")]
    [Tooltip("외곽선 색상과 불투명도. 발광이나 깜빡임은 사용하지 않습니다.")]
    public Color color = new Color(.8f, .67f, .39f, 1);

    [Range(0, 8), Tooltip("게임 화면 기준 픽셀 두께. 0이면 숨깁니다.")]
    public int widthPixels = 2;

    [Range(.01f, 1), Tooltip("이 값 이상의 본체 알파만 실루엣에 포함합니다.")]
    public float alphaThreshold = .1f;
    public bool IsVisible => output != null && output.enabled;
    public Transform CurrentTarget { get; private set; }
    public Bounds TargetBounds { get; private set; }

    CoreGuardianHud hud;
    Material maskMaterial,
        outlineMaterial;
    CoreGuardianSharedMaskMaterial maskLease,
        outlineLease;
    MaterialPropertyBlock outputProperties;
    Mesh quad;
    MeshRenderer output;
    RenderTexture mask;
    CoreGuardianBodyMaskCache.Lease maskBinding;
    Vector2 maskOriginPx;
    long maskGeometryEpoch = -1;
    CommandBuffer commands;
    readonly Dictionary<Sprite, Mesh> meshes = new Dictionary<Sprite, Mesh>();
    readonly Dictionary<Sprite, MaskGeometry> geometry = new Dictionary<Sprite, MaskGeometry>();
    readonly List<MaskBatch> batches = new List<MaskBatch>();
    int lastMaskDrawCalls,
        lastAtlasMisses;
    CoreGuardianMaskAtlas atlas;
    CoreGuardianMaskAtlas cachedAtlas;
    int atlasRevision = -1;
    MaterialPropertyBlock properties;
    readonly List<SpriteRenderer> sources = new List<SpriteRenderer>();
    readonly CoreGuardianSpriteReferences bodySources = new CoreGuardianSpriteReferences();
    readonly Vector3[] quadVertices = new Vector3[4];
    readonly Vector2[] quadUV = new Vector2[4];
    Transform artwork,
        lastTarget;
    bool pageRasterSupported;

    [Tooltip("켜면 선택 대상 대신 마우스를 올린 대상을 그립니다(10-31, HUD가 실행 때 만듦).")]
    public bool hoverMode;

    void LateUpdate()
    {
        if (hud == null)
        {
            hud = GetComponentInParent<CoreGuardianHud>();
        }

        Transform target = null;
        if (hud != null)
        {
            target = hoverMode ? hud.HoverOutlineTarget : hud.InspectionTarget;
            // 10-31 확정: 대상 종류별 색(누름·올림 같은 색).
            if (target != null)
            {
                color = hoverMode ? hud.HoverOutlineColor : hud.InspectionOutlineColor;
            }
        }
        Present(target, Camera.main);
    }

    // Reuse the same animated-body union for a transient overlay without changing the sources.
    // A dedicated disabled instance is owned by the upgrade effect; the HUD outline stays separate.
    public bool PresentOverlay(Transform target, Camera camera, Material material)
    {
        PresentMask(target, camera, material == null);
        if (!IsVisible || material == null)
            return false;

        SetOutputMask();
        output.SetPropertyBlock(outputProperties);
        output.sharedMaterial = material;
        output.sortingOrder++;
        return true;
    }

    public void Present(Transform target, Camera camera)
    {
        PresentMask(target, camera, true);
        RegisterOutput(camera, false, 0);
    }

    void OnDisable()
    {
        ReleasePresentation();
    }

    void OnDestroy()
    {
        ReleaseResources();
    }
}
