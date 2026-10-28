using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

// CoreGuardianSharedOutline: batched/fallback mask recording and layout.
internal sealed partial class CoreGuardianSharedOutline
{
    [StructLayout(LayoutKind.Sequential)]
    struct PartItem
    {
        public Vector4 r0,
            r1,
            r2,
            r3,
            data;
    }

    sealed class SpriteGeometry
    {
        public Vector2[] vertices,
            uv;
        public ushort[] triangles;
    }

    void RecordFallback(int rank, ref int current)
    {
        var part = parts[rank];
        if (part.entity != current)
        {
            commands.SetGlobalFloat(MaskEntityId, part.entity);
            current = part.entity;
        }
        commands.SetGlobalFloat(MaskDepthId, Depth(rank));
        commands.DrawRenderer(part.renderer, maskMaterial, 0, 1);
        LastMaskDraws++;
    }

    SpriteGeometry Geometry(Sprite sprite)
    {
        if (geometries.TryGetValue(sprite, out var g))
            return g;

        g = new SpriteGeometry
        {
            vertices = sprite.vertices,
            uv = sprite.uv,
            triangles = sprite.triangles,
        };
        geometries.Add(sprite, g);
        return g;
    }

    bool Eligible(SpriteRenderer r, Sprite sprite) =>
        r.drawMode == SpriteDrawMode.Simple
        && sprite.texture != null
        && !(sprite.packed && sprite.packingMode == SpritePackingMode.Tight);

    void RecordBatched()
    {
        items.Clear();
        fallback.Clear();
        eligibleSequence.Clear();
        // parts are sorted by visual rank; sequence is the collection order.
        rankOfSequence.Clear();
        for (int i = 0; i < parts.Count; i++)
        {
            rankOfSequence.Add(0);
        }

        for (int rank = 0; rank < parts.Count; rank++)
        {
            rankOfSequence[parts[rank].sequence] = rank;
        }

        bool same = ReuseGeometry && layoutValid && batchMesh != null && layoutSprites.Count == parts.Count;
        bool patchable = same; // F25: same bodies, only some animation frames changed -> rewrite just those parts
        changedSequence.Clear();
        for (int seq = 0; seq < parts.Count; seq++)
        {
            int rank = rankOfSequence[seq];
            var r = parts[rank].renderer;
            var sprite = r.sprite;
            // Only plain sprites map 1:1 to Sprite.vertices; anything else keeps the renderer's own draw.
            if (!Eligible(r, sprite))
            {
                fallback.Add(rank);
                if (same && layoutSprites[seq] != null)
                {
                    same = false;
                    patchable = false;
                }
                continue;
            }
            int flip = (r.flipX ? 1 : 0) | (r.flipY ? 2 : 0);
            if (patchable && layoutSprites[seq] == null)
            {
                patchable = false; // a part joined the batch: the item list itself changes
            }
            if (same && (!ReferenceEquals(layoutSprites[seq], sprite) || layoutFlips[seq] != flip))
            {
                same = false;
                changedSequence.Add(seq);
            }
            else if (!same && patchable && (!ReferenceEquals(layoutSprites[seq], sprite) || layoutFlips[seq] != flip))
            {
                changedSequence.Add(seq);
            }

            eligibleSequence.Add(seq);
        }
        if (!same && !(patchable && PatchLayout()))
        {
            RebuildLayout();
        }

        foreach (int seq in eligibleSequence)
        {
            int rank = rankOfSequence[seq];
            var part = parts[rank];
            var m = part.transform.localToWorldMatrix;
            items.Add(
                new PartItem
                {
                    r0 = m.GetRow(0),
                    r1 = m.GetRow(1),
                    r2 = m.GetRow(2),
                    r3 = m.GetRow(3),
                    data = new Vector4(part.entity, Depth(rank), part.alpha, 0),
                }
            );
        }
        fallback.Sort();
        LastMaskDraws = 0;
        if (items.Count > 0)
        {
            if (partBuffer == null || partBuffer.count < items.Count)
            {
                partBuffer?.Release();
                partBuffer = new GraphicsBuffer(
                    GraphicsBuffer.Target.Structured,
                    Mathf.NextPowerOfTwo(Mathf.Max(64, items.Count)),
                    Marshal.SizeOf<PartItem>()
                );
            }
            partBuffer.SetData(items);
            commands.SetGlobalBuffer(PartsBufferId, partBuffer);
            for (int i = 0; i < groupTextures.Count; i++)
            {
                groupProperties.Clear();
                groupProperties.SetTexture(MainTexId, groupTextures[i]);
                commands.DrawMesh(batchMesh, Matrix4x4.identity, maskMaterial, i, 0, groupProperties);
                LastMaskDraws++;
            }
        }
        int current = -1;
        foreach (var rank in fallback)
        {
            RecordFallback(rank, ref current);
        }
    }

    // Rewrites only the changed parts' vertices/uv/triangles when every changed sprite keeps its texture and its
    // vertex and triangle counts; the batch mesh then matches what RebuildLayout would have produced.
    bool PatchLayout()
    {
        if (eligibleSequence.Count != itemSequence.Count)
            return false;
        for (int i = 0; i < eligibleSequence.Count; i++)
        {
            if (eligibleSequence[i] != itemSequence[i])
                return false;
        }
        foreach (int seq in changedSequence)
        {
            int item = itemOfSequence[seq];
            var sprite = parts[rankOfSequence[seq]].renderer.sprite;
            var g = Geometry(sprite);
            if (
                item < 0
                || !ReferenceEquals(sprite.texture, groupTextures[itemGroup[item]])
                || g.vertices.Length != itemVertexCount[item]
                || g.triangles.Length != itemTriangleCount[item]
            )
            {
                return false;
            }
        }

        patchedGroups.Clear();
        foreach (int seq in changedSequence)
        {
            int item = itemOfSequence[seq];
            var r = parts[rankOfSequence[seq]].renderer;
            var sprite = r.sprite;
            var g = Geometry(sprite);
            int baseVertex = itemVertexStart[item];
            float fx = r.flipX ? -1 : 1,
                fy = r.flipY ? -1 : 1;
            for (int v = 0; v < g.vertices.Length; v++)
            {
                batchVertices[baseVertex + v] = new Vector3(g.vertices[v].x * fx, g.vertices[v].y * fy, 0);
                batchUV[baseVertex + v] = g.uv[v];
            }
            var tris = groupTriangles[itemGroup[item]];
            int start = itemTriangleStart[item];
            for (int t = 0; t < g.triangles.Length; t++)
            {
                tris[start + t] = baseVertex + g.triangles[t];
            }
            if (!patchedGroups.Contains(itemGroup[item]))
            {
                patchedGroups.Add(itemGroup[item]);
            }

            layoutSprites[seq] = sprite;
            layoutFlips[seq] = (r.flipX ? 1 : 0) | (r.flipY ? 2 : 0);
        }

        LayoutPatches++;
        batchMesh.SetVertices(batchVertices);
        batchMesh.SetUVs(0, batchUV);
        foreach (int group in patchedGroups)
        {
            batchMesh.SetTriangles(groupTriangles[group], group, false);
        }
        return true;
    }

    // Per batched item (F25 patching): where its vertices and triangles live in the batch mesh.
    readonly List<int> itemSequence = new List<int>(),
        itemOfSequence = new List<int>(),
        itemGroup = new List<int>(),
        itemVertexStart = new List<int>(),
        itemVertexCount = new List<int>(),
        itemTriangleStart = new List<int>(),
        itemTriangleCount = new List<int>(),
        changedSequence = new List<int>(),
        patchedGroups = new List<int>();

    public static int LayoutPatches { get; private set; }

    void RebuildLayout()
    {
        LayoutRebuilds++;
        itemSequence.Clear();
        itemOfSequence.Clear();
        itemGroup.Clear();
        itemVertexStart.Clear();
        itemVertexCount.Clear();
        itemTriangleStart.Clear();
        itemTriangleCount.Clear();
        groupOf.Clear();
        groupTextures.Clear();
        foreach (var list in groupTriangles)
        {
            list.Clear();
        }

        batchVertices.Clear();
        batchUV.Clear();
        batchIndex.Clear();
        layoutSprites.Clear();
        layoutFlips.Clear();
        for (int i = 0; i < parts.Count; i++)
        {
            layoutSprites.Add(null);
            layoutFlips.Add(0);
            itemOfSequence.Add(-1);
        }
        int item = 0;
        foreach (int seq in eligibleSequence)
        {
            var r = parts[rankOfSequence[seq]].renderer;
            var sprite = r.sprite;
            var texture = sprite.texture;
            if (!groupOf.TryGetValue(texture, out int group))
            {
                group = groupTextures.Count;
                groupOf.Add(texture, group);
                groupTextures.Add(texture);
                if (groupTriangles.Count <= group)
                {
                    groupTriangles.Add(new List<int>());
                }
            }
            var g = Geometry(sprite);
            int baseVertex = batchVertices.Count;
            float fx = r.flipX ? -1 : 1,
                fy = r.flipY ? -1 : 1;
            for (int v = 0; v < g.vertices.Length; v++)
            {
                // Same flip rule the sprite shader applies to object-space positions (exact sign change).
                batchVertices.Add(new Vector3(g.vertices[v].x * fx, g.vertices[v].y * fy, 0));
                batchUV.Add(g.uv[v]);
                batchIndex.Add(new Vector2(item, 0));
            }
            var tris = groupTriangles[group];
            itemSequence.Add(seq);
            itemOfSequence[seq] = item;
            itemGroup.Add(group);
            itemVertexStart.Add(baseVertex);
            itemVertexCount.Add(g.vertices.Length);
            itemTriangleStart.Add(tris.Count);
            itemTriangleCount.Add(g.triangles.Length);
            foreach (var t in g.triangles)
            {
                tris.Add(baseVertex + t);
            }

            layoutSprites[seq] = sprite;
            layoutFlips[seq] = (r.flipX ? 1 : 0) | (r.flipY ? 2 : 0);
            item++;
        }
        layoutValid = false;
        if (item == 0)
            return;

        if (batchMesh == null)
        {
            batchMesh = new Mesh { name = "Shared outline mask batch", hideFlags = HideFlags.DontSave };
            batchMesh.MarkDynamic();
        }
        batchMesh.Clear();
        batchMesh.indexFormat = batchVertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        batchMesh.SetVertices(batchVertices);
        batchMesh.SetUVs(0, batchUV);
        batchMesh.SetUVs(1, batchIndex);
        batchMesh.subMeshCount = groupTextures.Count;
        for (int i = 0; i < groupTextures.Count; i++)
        {
            batchMesh.SetTriangles(groupTriangles[i], i, false);
        }

        batchMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1e6f);
        layoutValid = true;
    }
}
