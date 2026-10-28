using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

// CoreGuardianSelectionOutline: outline output registration and mask pages.
public sealed partial class CoreGuardianSelectionOutline
{
    // Always-on readability sits below inspection (+4) and the success scan (+5).
    public void PresentAtOrder(Transform target, Camera camera, int offset)
    {
        PresentMask(target, camera, true);
        if (output != null && output.enabled)
        {
            output.sortingOrder += offset - 4;
        }

        RegisterOutput(camera, false, 0);
    }

    // Only the emphasis owner supplies references; standalone selection/upgrade consumers keep their own validation.
    internal void PresentAtOrderWithSources(
        Transform target,
        Camera camera,
        int offset,
        Transform sourceRoot,
        List<SpriteRenderer> sourceRenderers
    )
    {
        PresentMask(target, camera, true, sourceRoot: sourceRoot, sourceRenderers: sourceRenderers);
        if (output != null && output.enabled)
        {
            output.sortingOrder += offset - 4;
        }

        RegisterOutput(camera, false, 0);
    }

    // A separate owner uses the current body mask for floor contact, below all bodies.
    public void PresentGroundContact(Transform target, Camera camera, Material material)
    {
        PresentContact(target, camera, material, null);
    }

    public void PresentGroundContactWithBand(Transform target, Camera camera, Material material, float band)
    {
        PresentContact(target, camera, material, band);
    }

    void PresentContact(Transform target, Camera camera, Material material, float? band)
    {
        bool compact = false;
        if (material != null)
        {
            if (contactShader == null)
            {
                contactShader = Resources.Load<Shader>("CoreGuardian/PhotoShadowContact");
            }

            compact = material.shader == contactShader;
        }
        PresentMask(target, camera, material == null, compact);
        if (!IsVisible || material == null)
            return;

        SetOutputMask();
        if (band.HasValue)
        {
            outputProperties.SetFloat(BandId, band.Value);
        }

        output.SetPropertyBlock(outputProperties);
        output.sharedMaterial = material;
        output.sortingOrder = 43;
        if (compact)
        {
            RegisterOutput(camera, true, band ?? material.GetFloat("_Band"));
        }
    }

    void RegisterOutput(Camera camera, bool contact, float band)
    {
        // Runtime savings came from mask production, not output material substitution.
        // Keep every original output renderer/material/order, including foreign transparent ties.
        if (Application.isPlaying)
            return;

        if (output == null || !output.enabled || mask == null || camera == null)
            return;

        // Same native camera range already covered by body mask sharing pixel QA.
        if (!NativeQaCamera(camera))
        {
            return;
        }

        bool prepared = CoreGuardianBodyMaskCache.PreparedPage(maskBinding, out var slot);
        if (!prepared && !CoreGuardianMaskPage.TryAcquire(camera, Time.frameCount, mask.width, mask.height, out slot))
        {
            return;
        }

        var parameters = new CoreGuardianMaskOutputBatch.OutputParameters
        {
            contact = contact,
            allowSingle = true,
            prepared = prepared,
            color = color,
            width = Mathf.Clamp(widthPixels, 0, 8),
            band = band,
            maskOriginPx = maskOriginPx,
            sourceSize = new Vector2(mask.width, mask.height),
        };
        if (!CoreGuardianMaskOutputBatch.Register(this, camera, output, quad, slot, parameters) && !prepared)
        {
            CoreGuardianMaskPage.Release(slot);
        }
    }

    // F31: the one camera rule for the fast mask paths (native pixel QA covered exactly this view).
    // Any other window size silently uses the original path; the picture is the same, only slower.
    internal static bool NativeQaCamera(Camera camera)
    {
        var viewport = camera.pixelRect;
        return viewport == new Rect(0, 0, 1920, 1080)
            && camera.orthographic
            && !camera.stereoEnabled
            && !camera.allowDynamicResolution
            && (camera.orthographicSize == 8.5f || camera.orthographicSize == 4.25f)
            && DefaultProjection(camera)
            && camera.transform.rotation.Equals(Quaternion.identity)
            && Mathf.Abs(camera.transform.position.x) <= 32
            && Mathf.Abs(camera.transform.position.y) <= 32;
    }

    bool QueuePage(Camera camera, Matrix4x4 vp, int width, int height)
    {
        var viewport = camera.pixelRect;
        if (
            !Application.isPlaying
            || !pageRasterSupported
            || !CoreGuardianMaskProducerBatch.Supported
            || !NativeQaCamera(camera)
            || Mathf.Abs(artwork.position.x) > 32
            || Mathf.Abs(artwork.position.y) > 32
        )
        {
            return false;
        }

        if (!CoreGuardianMaskPage.TryAcquire(camera, Time.frameCount, mask.width, mask.height, out var slot))
        {
            return false;
        }

        foreach (var source in sources)
        {
            var shape = Geometry(source.sprite);
            if (
                CoreGuardianMaskPage.QueueShape(
                    slot,
                    shape.batch,
                    maskMaterial,
                    PartMatrix(source),
                    vp,
                    shape.texture,
                    source.color.a,
                    Mathf.Clamp(alphaThreshold, .01f, 1),
                    new Vector2(width, height)
                )
            )
            {
                continue;
            }

            CoreGuardianMaskPage.Release(slot);
            return false;
        }
        CoreGuardianBodyMaskCache.MarkPending(maskBinding, slot);
        return true;
    }

    void SetOutputMask()
    {
        outputProperties.Clear();
        outputProperties.SetTexture(MainTexId, mask);
        outputProperties.SetVector(
            MainTexTexelSizeId,
            new Vector4(1f / mask.width, 1f / mask.height, mask.width, mask.height)
        );
        outputProperties.SetVector(MaskOriginId, new Vector4(maskOriginPx.x, maskOriginPx.y, 0, 0));
    }
}
