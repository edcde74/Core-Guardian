using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

// CoreGuardianSharedOutline: resource creation and release.
internal sealed partial class CoreGuardianSharedOutline
{
    void EnsureResources()
    {
        if (maskMaterial == null)
        {
            maskMaterial = new Material(Resources.Load<Shader>("CoreGuardian/SharedOutlineMask"))
            {
                name = "Shared outline mask",
                hideFlags = HideFlags.DontSave,
            };
        }

        if (outputMaterial == null)
        {
            outputMaterial = new Material(Resources.Load<Shader>("CoreGuardian/SharedOutlineOutput"))
            {
                name = "Shared outline output",
                hideFlags = HideFlags.DontSave,
            };
        }

        if (outputProperties == null)
        {
            outputProperties = new MaterialPropertyBlock();
        }

        if (groupProperties == null)
        {
            groupProperties = new MaterialPropertyBlock();
        }

        if (commands == null)
        {
            commands = new CommandBuffer { name = "Shared emphasis outline mask" };
        }

        if (outlineSprite == null)
        {
            // One world unit, centred: the same footprint as the former quad mesh.
            outlineTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "Shared outline output",
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.DontSave,
            };
            outlineTexture.SetPixel(0, 0, Color.white);
            outlineTexture.Apply(false, true);
            outlineSprite = Sprite.Create(
                outlineTexture,
                new Rect(0, 0, 1, 1),
                new Vector2(.5f, .5f),
                1,
                0,
                SpriteMeshType.FullRect
            );
            outlineSprite.name = "Shared outline output";
            outlineSprite.hideFlags = HideFlags.DontSave;
        }
    }

    void EnsureMask(int width, int height)
    {
        if (mask != null && mask.width == width && mask.height == height)
            return;

        ReleaseMask();
        mask = new RenderTexture(width, height, 24, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
        {
            name = "Shared emphasis outline mask",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false,
            hideFlags = HideFlags.DontSave,
        };
        mask.Create();
    }

    void ReleaseMask()
    {
        if (mask == null)
            return;
        mask.Release();
        Dispose(mask);
        mask = null;
    }

    public void Release()
    {
        parts.Clear();
        entries.Clear();
        customOutputs.Clear();
        active = false;
        ReleaseMask();
        commands?.Dispose();
        commands = null;
        partBuffer?.Release();
        partBuffer = null;
        Dispose(batchMesh);
        batchMesh = null;
        layoutValid = false;
        geometries.Clear();
        groupOf.Clear();
        groupTextures.Clear();
        Dispose(maskMaterial);
        maskMaterial = null;
        Dispose(outputMaterial);
        outputMaterial = null;
        Dispose(outlineSprite);
        outlineSprite = null;
        Dispose(outlineTexture);
        outlineTexture = null;
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
