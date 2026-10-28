using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

// CoreGuardianSelectionOutline: resource creation and release.
public sealed partial class CoreGuardianSelectionOutline
{
    void EnsureResources()
    {
        using var resourcesScope = resourcesSample.Auto();
        if (atlas == null && SystemInfo.supports2DArrayTextures)
        {
            atlas = Resources.Load<CoreGuardianMaskAtlas>("CoreGuardian/MaskAtlas/Manifest");
        }

        int revision = atlas != null ? atlas.Revision : -1;
        if (cachedAtlas != atlas || atlasRevision != revision)
        {
            foreach (var value in meshes.Values)
            {
                DisposeObject(value);
            }

            meshes.Clear();
            geometry.Clear();
            cachedAtlas = atlas;
            atlasRevision = revision;
        }
        if (output != null)
            return;

        properties = new MaterialPropertyBlock();
        maskBinding = new CoreGuardianBodyMaskCache.Lease(this);
        outputProperties = new MaterialPropertyBlock();
        maskLease = CoreGuardianSharedMaskMaterial.Acquire("CoreGuardian/SelectionMask");
        maskMaterial = maskLease.Material;
        outlineLease = CoreGuardianSharedMaskMaterial.Acquire("CoreGuardian/SelectionOutline");
        outlineMaterial = outlineLease.Material;
        commands = new CommandBuffer { name = "Selected unit sprite union" };
        var root = new GameObject("SelectionOutline") { hideFlags = HideFlags.DontSave };
        root.transform.SetParent(transform, false);
        root.AddComponent<CoreGuardianBodyMaskLifetime>().owner = this;
        quad = new Mesh { name = "Selected unit outline quad", hideFlags = HideFlags.DontSave };
        quad.vertices = new Vector3[4];
        quad.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        root.AddComponent<MeshFilter>().sharedMesh = quad;
        output = root.AddComponent<MeshRenderer>();
        output.sharedMaterial = outlineMaterial;
        output.shadowCastingMode = ShadowCastingMode.Off;
        output.receiveShadows = false;
        output.enabled = false;
    }

    // Component-only edits on an existing transform must explicitly invalidate this cache.
    // New/reparented/destroyed children are detected automatically, including inactive rigs.
    public void InvalidateSources()
    {
        CoreGuardianBodyMaskCache.Invalidate(artwork);
        bodySources.Invalidate();
        ClearGeometry();
        maskGeometryEpoch = -1;
        lastTarget = null;
        artwork = null;
    }

    void ClearGeometry()
    {
        foreach (var mesh in meshes.Values)
        {
            DisposeObject(mesh);
        }
        meshes.Clear();
        geometry.Clear();
    }

    internal void ReleasePresentation()
    {
        CoreGuardianMaskOutputBatch.Release(this);
        if (output != null)
        {
            output.enabled = false;
        }
        CurrentTarget = null;
        CoreGuardianBodyMaskCache.Release(maskBinding);
        mask = null;
        maskOriginPx = Vector2.zero;
    }

    internal void ReleaseResources()
    {
        ReleasePresentation();
        commands?.Dispose();
        commands = null;
        foreach (var mesh in meshes.Values)
        {
            DisposeObject(mesh);
        }

        meshes.Clear();
        foreach (var batch in batches)
        {
            DisposeObject(batch.mesh);
        }

        batches.Clear();
        geometry.Clear();
        DisposeObject(quad);
        if (output != null)
        {
            DisposeObject(output.gameObject);
        }

        maskLease?.Release();
        maskLease = null;
        outlineLease?.Release();
        outlineLease = null;
        maskMaterial = null;
        outlineMaterial = null;
    }

    static void DisposeObject(Object value)
    {
        if (value == null)
            return;

        if (Application.isPlaying)
        {
            Destroy(value);
        }
        else
        {
            DestroyImmediate(value);
        }
    }
}
