using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Explicit bake for fixed Simple/Unlit sprites. Does not touch combat or mask production.
public static class CoreGuardianRouteMeshBuilder
{
    const string RootPath = "CoreGuardian_MapArt_B/CoreGuardian_Route01_VisualOnly";
    const string MeshFolder = "Assets/Art/Environment/Meshes/Routes";
    const string MaterialFolder = "Assets/Art/Rendering/Materials/Environment";

    [MenuItem("Tools/Core Guardian/Route Mesh/Combine Road Tiles")]
    static void BakeMenu()
    {
        var root = GameObject.Find(RootPath);
        if (root == null)
        {
            throw new InvalidOperationException("Open the combat scene; route root is missing");
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Combine road tiles");
        try
        {
            var outputs = CombineRoad(root);
            EnsureFolder(MeshFolder);
            EnsureFolder(MaterialFolder);
            // Asset names are unique; existing baked/user assets are never overwritten.
            var shared = new HashSet<Object>();
            foreach (var r in outputs)
            {
                var mesh = r.GetComponent<MeshFilter>().sharedMesh;
                if (shared.Add(mesh))
                {
                    AssetDatabase.CreateAsset(
                        mesh,
                        AssetDatabase.GenerateUniqueAssetPath(
                            MeshFolder + "/Route_Order_" + r.sortingOrder.ToString("00") + ".asset"
                        )
                    );
                }

                if (shared.Add(r.sharedMaterial))
                {
                    AssetDatabase.CreateAsset(
                        r.sharedMaterial,
                        AssetDatabase.GenerateUniqueAssetPath(MaterialFolder + "/Route_Material.mat")
                    );
                }
            }
            foreach (var asset in shared)
            {
                AssetDatabase.SaveAssetIfDirty(asset);
            }

            Debug.Log(
                "Combined "
                    + root.GetComponent<CoreGuardianRouteMeshBake>().sources.Length
                    + " road sprites into "
                    + outputs.Length
                    + " meshes. Scene remains unsaved. Restore via Tools/Core Guardian/Route Mesh/Restore Road Tiles."
            );
        }
        catch
        {
            Undo.RevertAllDownToGroup(group);
            throw;
        }
        finally
        {
            Undo.CollapseUndoOperations(group);
        }
    }

    [MenuItem("Tools/Core Guardian/Route Mesh/Restore Road Tiles")]
    static void RestoreMenu()
    {
        var root = GameObject.Find(RootPath);
        if (root == null)
            return;

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Restore road tiles");
        Restore(root);
        Undo.CollapseUndoOperations(group);
    }

    public static MeshRenderer[] CombineRoad(GameObject root)
    {
        // These two overlapping tiles differ near sampling boundaries in the approved views.
        // Preserve the original renderers rather than changing their UVs/artwork.
        var sprites = root.GetComponentsInChildren<SpriteRenderer>(true)
            .Where(r => r.enabled && r.gameObject.activeInHierarchy)
            .ToArray();
        var keep = new HashSet<SpriteRenderer>(
            sprites.Where(r =>
                r.name == "Low curb (25, 8) (0, -1)"
                || r.name == "Road stone (25, 8)"
                || r.name == "Road stone (15, 5)"
                || r.name == "Road stone (10, 10)"
            )
        );
        // Equal-order overlapping corners have an engine tie order. A merged renderer
        // cannot promise that order against the preserved sprites; keep both originals.
        for (int i = 0; i < sprites.Length; i++)
        {
            for (int j = i + 1; j < sprites.Length; j++)
            {
                var a = sprites[i];
                var b = sprites[j];
                if (a.sortingLayerID != b.sortingLayerID || a.sortingOrder != b.sortingOrder)
                    continue;

                var ab = a.bounds;
                var bb = b.bounds;
                if (
                    Mathf.Min(ab.max.x, bb.max.x) - Mathf.Max(ab.min.x, bb.min.x) > .00001f
                    && Mathf.Min(ab.max.y, bb.max.y) - Mathf.Max(ab.min.y, bb.min.y) > .00001f
                )
                {
                    keep.Add(a);
                    keep.Add(b);
                }
            }
        }

        return CombineKeeping(root, keep.ToArray());
    }

    public static MeshRenderer[] Combine(GameObject root) => CombineKeeping(root, Array.Empty<SpriteRenderer>());

    public static MeshRenderer[] CombineKeeping(GameObject root, SpriteRenderer[] keep)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            throw new InvalidOperationException("Wait until stopped and compiled");
        }

        if (root == null)
        {
            throw new ArgumentNullException(nameof(root));
        }

        if (root.GetComponent<CoreGuardianRouteMeshBake>() != null)
        {
            throw new InvalidOperationException("Restore road tiles before combining again");
        }

        var sources = root.GetComponentsInChildren<SpriteRenderer>(true)
            .Where(r => r.enabled && r.gameObject.activeInHierarchy && !keep.Contains(r))
            .ToArray();
        if (sources.Length == 0)
        {
            throw new InvalidOperationException("No enabled road sprites");
        }
        // Reject unsupported input before changing any source renderer.
        foreach (var r in sources)
        {
            if (
                r.sprite == null
                || r.sharedMaterial == null
                || r.sharedMaterial.shader.name != "Universal Render Pipeline/2D/Sprite-Unlit-Default"
                || r.drawMode != SpriteDrawMode.Simple
                || r.maskInteraction != SpriteMaskInteraction.None
                || CoreGuardianSpriteBlocks.HasCustomBlock(r)
                || r.GetComponentInParent<SortingGroup>() != null
                || r.GetComponentsInChildren<MonoBehaviour>(true).Length != 0
            )
            {
                throw new InvalidOperationException("Unsupported road sprite: " + r.name);
            }

            for (var t = r.transform; t != null; t = t.parent)
            {
                if (
                    t.GetComponent<Animator>() != null
                    || t.GetComponent<Animation>() != null
                    || t.GetComponents<MonoBehaviour>().Length != 0
                )
                {
                    throw new InvalidOperationException("Animated/scripted road hierarchy: " + t.name);
                }

                if (t == root.transform)
                    break;
            }
        }
        var outputs = new List<MeshRenderer>();
        var owned = new List<Object>();
        try
        {
            foreach (
                var group in sources.GroupBy(r =>
                    (r.sharedMaterial, r.sprite.texture, r.sortingLayerID, r.sortingOrder, r.gameObject.layer)
                )
            )
            {
                var input = new List<Mesh>();
                Mesh mesh = null;
                try
                {
                    var combines = new List<CombineInstance>();
                    // Equal-order sources in this fixed, planar route retain hierarchy order.
                    foreach (var r in group)
                    {
                        var sprite = r.sprite;
                        var vertices = sprite.vertices;
                        var v = new Vector3[vertices.Length];
                        var colors = new Color[v.Length];
                        var normals = new Vector3[v.Length];
                        // SpriteRenderer uploads its tint in the project's working color space.
                        // Mesh vertex colors do not receive that automatic conversion.
                        var tint = QualitySettings.activeColorSpace == ColorSpace.Linear ? r.color.linear : r.color;
                        for (int i = 0; i < v.Length; i++)
                        {
                            v[i] = new Vector3(
                                vertices[i].x * (r.flipX ? -1 : 1),
                                vertices[i].y * (r.flipY ? -1 : 1),
                                0
                            );
                            colors[i] = tint;
                            normals[i] = Vector3.back;
                        }
                        var part = new Mesh
                        {
                            vertices = v,
                            uv = sprite.uv,
                            colors = colors,
                            normals = normals,
                        };
                        input.Add(part);
                        part.triangles = Array.ConvertAll(sprite.triangles, x => (int)x);
                        combines.Add(
                            new CombineInstance
                            {
                                mesh = part,
                                transform = root.transform.worldToLocalMatrix * r.transform.localToWorldMatrix,
                            }
                        );
                    }
                    mesh = new Mesh { name = "Route_Order" + group.Key.sortingOrder, indexFormat = IndexFormat.UInt32 };
                    owned.Add(mesh);
                    mesh.CombineMeshes(combines.ToArray(), true, true, false);
                    mesh.RecalculateBounds();
                }
                finally
                {
                    foreach (var part in input)
                    {
                        Object.DestroyImmediate(part);
                    }
                }
                var go = new GameObject(mesh.name);
                Undo.RegisterCreatedObjectUndo(go, "Create combined road");
                Undo.SetTransformParent(go.transform, root.transform, "Parent combined road");
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                go.layer = group.Key.layer;
                var filter = Undo.AddComponent<MeshFilter>(go);
                filter.sharedMesh = mesh;
                var renderer = Undo.AddComponent<MeshRenderer>(go);
                renderer.sortingLayerID = group.Key.sortingLayerID;
                renderer.sortingOrder = group.Key.sortingOrder;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                var material = new Material(group.Key.sharedMaterial) { name = "RouteMaterial" };
                owned.Add(material);
                material.mainTexture = group.Key.texture;
                renderer.sharedMaterial = material;
                outputs.Add(renderer);
            }
            var state = Undo.AddComponent<CoreGuardianRouteMeshBake>(root);
            Undo.RecordObject(state, "Record road sources");
            state.sources = sources;
            state.outputs = outputs.ToArray();
            state.ConfigureOutputs();
            EditorUtility.SetDirty(state);
            Undo.RecordObjects(sources, "Hide source road sprites");
            foreach (var source in sources)
            {
                source.enabled = false;
            }

            return state.outputs;
        }
        catch
        {
            foreach (var output in outputs)
            {
                if (output != null)
                {
                    Object.DestroyImmediate(output.gameObject);
                }
            }

            foreach (var o in owned)
            {
                if (o != null && !EditorUtility.IsPersistent(o))
                {
                    Object.DestroyImmediate(o);
                }
            }

            var state = root.GetComponent<CoreGuardianRouteMeshBake>();
            if (state != null)
            {
                Object.DestroyImmediate(state);
            }

            foreach (var source in sources)
            {
                if (source != null)
                {
                    source.enabled = true;
                }
            }

            throw;
        }
    }

    public static void Restore(GameObject root)
    {
        if (root == null)
            return;

        var state = root.GetComponent<CoreGuardianRouteMeshBake>();
        if (state == null)
            return;

        Undo.RecordObjects(state.sources.Where(r => r != null).ToArray(), "Restore source road sprites");
        foreach (var source in state.sources)
        {
            if (source != null)
            {
                source.enabled = true;
            }
        }

        var owned = new HashSet<Object>();
        foreach (var output in state.outputs)
        {
            if (output != null)
            {
                var mesh = output.GetComponent<MeshFilter>().sharedMesh;
                if (mesh != null)
                {
                    owned.Add(mesh);
                }

                if (output.sharedMaterial != null)
                {
                    owned.Add(output.sharedMaterial);
                }

                Undo.DestroyObjectImmediate(output.gameObject);
            }
        }

        Undo.DestroyObjectImmediate(state);
        // Persistent assets are retained so scene Undo can reference them safely.
        foreach (var o in owned)
        {
            if (o != null && !EditorUtility.IsPersistent(o))
            {
                Undo.DestroyObjectImmediate(o);
            }
        }
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        int slash = folder.LastIndexOf('/');
        EnsureFolder(folder.Substring(0, slash));
        AssetDatabase.CreateFolder(folder.Substring(0, slash), folder.Substring(slash + 1));
    }
}
