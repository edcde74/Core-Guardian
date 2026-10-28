using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

// CoreGuardianSelectionOutline: mask production from sprite parts.
public sealed partial class CoreGuardianSelectionOutline
{
    void PresentMask(
        Transform target,
        Camera camera,
        bool writeOutlineProperties,
        bool compactContact = false,
        Transform sourceRoot = null,
        List<SpriteRenderer> sourceRenderers = null
    )
    {
        using var presentScope = presentSample.Auto();
        CoreGuardianMaskOutputBatch.Invalidate(this);
        lastMaskDrawCalls = 0;
        lastAtlasMisses = 0;
        CurrentTarget = target;
        if (output != null)
        {
            output.enabled = false;
        }

        if (target == null || camera == null || widthPixels <= 0 || color.a <= 0)
        {
            ReleasePresentation();
            return;
        }
        if (target != lastTarget || artwork == null)
        {
            lastTarget = target;
            var animation = target.GetComponent<CoreGuardianUnitAnimation>();
            // These two rigs still show a native-zoom pixel difference under atlas rasterization.
            // Keep their exact legacy mask production until separately validated.
            pageRasterSupported =
                animation == null || (animation.PresentationRole != "Defense" && animation.PresentationRole != "Boss");
            artwork =
                sourceRoot != null
                    ? sourceRoot
                    : target
                        .GetComponentsInChildren<SpriteRenderer>(true)
                        .FirstOrDefault(r => r.name == "2D Sprite")
                        .OrNull()
                        ?.transform;
        }
        if (artwork == null)
        {
            ReleasePresentation();
            return;
        }
        sources.Clear();
        foreach (var r in sourceRenderers ?? bodySources.Read(artwork))
        {
            if (
                r != null
                && r.enabled
                && r.sprite != null
                && r.color.a >= alphaThreshold
                && r.gameObject.activeInHierarchy
            )
            {
                sources.Add(r);
            }
        }

        if (sources.Count == 0)
        {
            ReleasePresentation();
            return;
        }
        EnsureResources();
        var bounds = sources[0].bounds;
        foreach (var r in sources)
        {
            bounds.Encapsulate(r.bounds);
        }

        TargetBounds = bounds;
        var lo = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        var hi = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        // With zero depth extent, corners i and i^4 are bit-identical; project each distinct corner once.
        int cornerCount = bounds.extents.z == 0 ? 4 : 8;
        for (int i = 0; i < cornerCount; i++)
        {
            var corner =
                bounds.center
                + Vector3.Scale(
                    bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)
                );
            var screen = camera.WorldToScreenPoint(corner);
            if (screen.z <= 0)
            {
                ReleasePresentation();
                return;
            }
            lo = Vector2.Min(lo, screen);
            hi = Vector2.Max(hi, screen);
        }
        int radius = Mathf.Clamp(widthPixels, 0, 8),
            pad = radius + 2;
        var viewport = camera.pixelRect;
        var bodyLo = lo;
        var bodyHi = hi;
        lo = new Vector2(
            Mathf.Max(viewport.xMin - pad, Mathf.Floor(lo.x) - pad),
            Mathf.Max(viewport.yMin - pad, Mathf.Floor(lo.y) - pad)
        );
        hi = new Vector2(
            Mathf.Min(viewport.xMax + pad, Mathf.Ceil(hi.x) + pad),
            Mathf.Min(viewport.yMax + pad, Mathf.Ceil(hi.y) + pad)
        );
        int w = Mathf.CeilToInt(hi.x - lo.x),
            h = Mathf.CeilToInt(hi.y - lo.y);
        if (w <= 0 || h <= 0)
        {
            ReleasePresentation();
            return;
        }
        // The contact band can read the same pad3 crop as emphasis. Keep its original pad4 output quad.
        // Outside the clipped viewport, the original output padding remains outside the camera too.
        var maskLo = lo;
        var maskHi = hi;
        bool localContact =
            compactContact
            && camera.orthographic
            && !camera.stereoEnabled
            && !camera.allowDynamicResolution
            // Enable the alternate contact crop only for the gameplay views covered by native pixel QA.
            && viewport.width == 1920
            && viewport.height == 1080
            && (camera.orthographicSize == 8.5f || camera.orthographicSize == 4.25f)
            && radius == 2
            && viewport.x == 0
            && viewport.y == 0
            && camera.transform.rotation.Equals(Quaternion.identity)
            && DefaultProjection(camera)
            && Mathf.Abs(camera.transform.position.x) <= 32
            && Mathf.Abs(camera.transform.position.y) <= 32
            && Mathf.Abs(artwork.position.x) <= 32
            && Mathf.Abs(artwork.position.y) <= 32
            && viewport.xMin == Mathf.Round(viewport.xMin)
            && viewport.yMin == Mathf.Round(viewport.yMin)
            && viewport.xMax == Mathf.Round(viewport.xMax)
            && viewport.yMax == Mathf.Round(viewport.yMax);
        if (localContact && radius > 1)
        {
            maskLo = new Vector2(
                Mathf.Max(viewport.xMin - 3, Mathf.Floor(bodyLo.x) - 3),
                Mathf.Max(viewport.yMin - 3, Mathf.Floor(bodyLo.y) - 3)
            );
            maskHi = new Vector2(
                Mathf.Min(viewport.xMax + 3, Mathf.Ceil(bodyHi.x) + 3),
                Mathf.Min(viewport.yMax + 3, Mathf.Ceil(bodyHi.y) + 3)
            );
        }
        int maskWidth = Mathf.CeilToInt(maskHi.x - maskLo.x),
            maskHeight = Mathf.CeilToInt(maskHi.y - maskLo.y);
        if (maskWidth <= 0 || maskHeight <= 0)
        {
            maskLo = lo;
            maskHi = hi;
            maskWidth = w;
            maskHeight = h;
        }
        var request = new CoreGuardianBodyMaskCache.Request(
            artwork,
            camera,
            alphaThreshold,
            new Rect(maskLo.x, maskLo.y, maskWidth, maskHeight),
            atlas,
            sources
        );
        CoreGuardianBodyMaskCache.Sample sample;
        try
        {
            bool hit = CoreGuardianBodyMaskCache.Begin(maskBinding, request, out sample);
            mask = sample.Texture;
            if (!hit)
            {
                if (maskGeometryEpoch != sample.GeometryEpoch)
                {
                    ClearGeometry();
                    maskGeometryEpoch = sample.GeometryEpoch;
                }
                var a = (maskLo - viewport.position) / viewport.size * 2 - Vector2.one;
                var b = (maskHi - viewport.position) / viewport.size * 2 - Vector2.one;
                var crop = Matrix4x4.identity;
                crop.m00 = 2 / (b.x - a.x);
                crop.m11 = 2 / (b.y - a.y);
                crop.m03 = -(b.x + a.x) / (b.x - a.x);
                crop.m13 = -(b.y + a.y) / (b.y - a.y);
                var vp = GL.GetGPUProjectionMatrix(crop * camera.projectionMatrix, true) * camera.worldToCameraMatrix;
                if (!QueuePage(camera, vp, maskWidth, maskHeight))
                {
                    commands.Clear();
                    commands.SetRenderTarget(mask);
                    commands.ClearRenderTarget(false, true, Color.clear);
                    commands.SetViewport(new Rect(0, 0, maskWidth, maskHeight));
                    DrawMaskParts(vp);
                    using (executeSample.Auto())
                    {
                        Graphics.ExecuteCommandBuffer(commands);
                    }

                    CoreGuardianBodyMaskCache.Publish(maskBinding);
                }
            }
        }
        catch
        {
            CoreGuardianBodyMaskCache.Abort(maskBinding);
            mask = null;
            throw;
        }
        maskOriginPx = lo - sample.ProducerRect.position;
        using var outputScope = outputSample.Auto();
        float depth = Vector3.Dot(bounds.center - camera.transform.position, camera.transform.forward);
        quadVertices[0] = Point(lo.x, lo.y);
        quadVertices[1] = Point(hi.x, lo.y);
        quadVertices[2] = Point(hi.x, hi.y);
        quadVertices[3] = Point(lo.x, hi.y);
        quad.vertices = quadVertices;
        quad.RecalculateBounds();
        // One texture texel remains one screen pixel, including the contact shader's lower band.
        float u = (float)w / mask.width,
            v = (float)h / mask.height;
        var origin = new Vector2(maskOriginPx.x / mask.width, maskOriginPx.y / mask.height);
        quadUV[0] = origin;
        quadUV[1] = origin + new Vector2(u, 0);
        quadUV[2] = origin + new Vector2(u, v);
        quadUV[3] = origin + new Vector2(0, v);
        quad.uv = quadUV;
        Vector3 Point(float x, float y) =>
            output.transform.InverseTransformPoint(camera.ScreenToWorldPoint(new Vector3(x, y, depth)));
        var front = sources[0];
        int frontLayer = SortingLayer.GetLayerValueFromID(front.sortingLayerID);
        foreach (var r in sources)
        {
            int layer = SortingLayer.GetLayerValueFromID(r.sortingLayerID);
            // >= preserves the previous stable OrderBy(...).ThenBy(...).Last() tie behavior.
            if (layer > frontLayer || (layer == frontLayer && r.sortingOrder >= front.sortingOrder))
            {
                front = r;
                frontLayer = layer;
            }
        }
        output.sortingLayerID = front.sortingLayerID;
        output.sortingOrder = front.sortingOrder + 4;
        if (writeOutlineProperties)
        {
            SetOutputMask();
            outputProperties.SetColor(ColorId, color);
            outputProperties.SetInt(WidthId, radius);
            output.SetPropertyBlock(outputProperties);
        }
        output.sharedMaterial = outlineMaterial;
        output.enabled = true;
    }

    static bool DefaultProjection(Camera camera)
    {
        float h = camera.orthographicSize,
            w = h * camera.aspect;
        return camera.projectionMatrix.Equals(Matrix4x4.Ortho(-w, w, -h, h, camera.nearClipPlane, camera.farClipPlane));
    }

    Mesh SpriteMesh(Sprite sprite)
    {
        if (meshes.TryGetValue(sprite, out var value))
            return value;

        var shape = Geometry(sprite);
        value = new Mesh { name = "Selection mask " + sprite.name, hideFlags = HideFlags.DontSave };
        value.vertices = shape.vertices;
        value.uv = shape.uv;
        value.SetUVs(5, Enumerable.Repeat(shape.atlasRect, shape.vertices.Length).ToList());
        value.triangles = shape.triangles;
        value.RecalculateBounds();
        meshes.Add(sprite, value);
        return value;
    }

    sealed class MaskGeometry
    {
        public CoreGuardianMaskProducerBatch.Shape batch;
        public Vector3[] vertices;
        public Vector2[] uv;
        public int[] triangles;
        public Texture texture;
        public bool mapped;
        public Vector4 atlasRect;
    }

    sealed class MaskBatch
    {
        public Texture texture;
        public float opacity;
        public readonly List<SpriteRenderer> parts = new List<SpriteRenderer>();
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<int> triangles = new List<int>();
        public readonly List<Vector4> row0 = new List<Vector4>(),
            row1 = new List<Vector4>(),
            row2 = new List<Vector4>(),
            row3 = new List<Vector4>();
        public readonly List<Vector4> atlasRects = new List<Vector4>();
        public Mesh mesh;
    }

    MaskGeometry Geometry(Sprite sprite)
    {
        if (geometry.TryGetValue(sprite, out var value))
            return value;

        value = new MaskGeometry
        {
            vertices = sprite.vertices.Select(v => (Vector3)v).ToArray(),
            uv = sprite.uv,
            triangles = sprite.triangles.Select(v => (int)v).ToArray(),
            texture = sprite.texture,
        };
        if (atlas != null && atlas.TryGetSprite(sprite, out var entry, out var uv))
        {
            value.texture = entry.page;
            value.mapped = true;
            value.atlasRect = new Vector4(entry.layer, 0, 0, 0);
            for (int i = 0; i < value.uv.Length; i++)
            {
                var point = new Vector2(value.uv[i].x * uv.x + uv.z, value.uv[i].y * uv.y + uv.w);
                // FullRect packed parts have integer source-pixel corners. Avoid atlas UV cancellation drift.
                if (uv != new Vector4(1, 1, 0, 0))
                {
                    point = new Vector2(
                        Mathf.Round(point.x * entry.page.width) / entry.page.width,
                        Mathf.Round(point.y * entry.page.height) / entry.page.height
                    );
                }

                value.uv[i] = point;
            }
        }
        value.batch = new CoreGuardianMaskProducerBatch.Shape
        {
            vertices = value.vertices,
            uv = value.uv,
            triangles = value.triangles,
            layer = value.atlasRect.x,
        };
        geometry.Add(sprite, value);
        return value;
    }

    static Matrix4x4 PartMatrix(SpriteRenderer part) =>
        part.transform.localToWorldMatrix * Matrix4x4.Scale(new Vector3(part.flipX ? -1 : 1, part.flipY ? -1 : 1, 1));

    void MaskProperties(Texture texture, float opacity, Matrix4x4 vp, bool combined)
    {
        properties.Clear();
        bool array = texture is Texture2DArray;
        properties.SetTexture(array ? MaskAtlasId : MainTexId, texture);
        properties.SetFloat(UseAtlasId, array ? 1 : 0);
        properties.SetFloat(OpacityId, opacity);
        properties.SetFloat(ThresholdId, Mathf.Clamp(alphaThreshold, .01f, 1));
        properties.SetMatrix(MaskVPId, vp);
        properties.SetFloat(UsePartMatricesId, combined ? 1 : 0);
    }

    void DrawMaskParts(Matrix4x4 vp)
    {
        using var partsScope = partsSample.Auto();
        lastAtlasMisses = 0;
        foreach (var batch in batches)
        {
            batch.parts.Clear();
        }

        int used = 0;
        foreach (var part in sources)
        {
            var shape = Geometry(part.sprite);
            var texture = shape.texture;
            if (!shape.mapped)
            {
                lastAtlasMisses++;
            }

            float opacity = part.color.a;
            int index = 0;
            for (; index < used; index++)
            {
                if (ReferenceEquals(batches[index].texture, texture) && batches[index].opacity == opacity)
                    break;
            }

            if (index == used)
            {
                if (used == batches.Count)
                {
                    batches.Add(new MaskBatch());
                }

                batches[index].texture = texture;
                batches[index].opacity = opacity;
                used++;
            }
            batches[index].parts.Add(part);
        }
        for (int i = 0; i < used; i++)
        {
            var batch = batches[i];
            int vertices = 0;
            foreach (var part in batch.parts)
            {
                vertices += Geometry(part.sprite).vertices.Length;
            }
            // Preserve unrestricted source rendering when a group cannot use 16-bit indices.
            if (batch.parts.Count == 1 || vertices > 65535)
            {
                MaskProperties(batch.texture, batch.opacity, vp, false);
                foreach (var part in batch.parts)
                {
                    commands.DrawMesh(SpriteMesh(part.sprite), PartMatrix(part), maskMaterial, 0, 0, properties);
                    lastMaskDrawCalls++;
                }
                continue;
            }
            if (batch.mesh == null)
            {
                batch.mesh = new Mesh { name = "Selection mask part batch", hideFlags = HideFlags.DontSave };
                batch.mesh.MarkDynamic();
            }
            batch.vertices.Clear();
            batch.uv.Clear();
            batch.triangles.Clear();
            batch.row0.Clear();
            batch.row1.Clear();
            batch.row2.Clear();
            batch.row3.Clear();
            batch.atlasRects.Clear();
            foreach (var part in batch.parts)
            {
                var shape = Geometry(part.sprite);
                var matrix = PartMatrix(part);
                int offset = batch.vertices.Count;
                var a = matrix.GetRow(0);
                var b = matrix.GetRow(1);
                var c = matrix.GetRow(2);
                var d = matrix.GetRow(3);
                for (int v = 0; v < shape.vertices.Length; v++)
                {
                    batch.vertices.Add(shape.vertices[v]);
                    batch.uv.Add(shape.uv[v]);
                    // Keep the original GPU matrix multiply; CPU-baked world vertices lose precision.
                    batch.row0.Add(a);
                    batch.row1.Add(b);
                    batch.row2.Add(c);
                    batch.row3.Add(d);
                    batch.atlasRects.Add(shape.atlasRect);
                }
                foreach (int triangle in shape.triangles)
                {
                    batch.triangles.Add(offset + triangle);
                }
            }
            using (uploadSample.Auto())
            {
                if (batch.mesh.vertexCount > batch.vertices.Count)
                {
                    batch.mesh.SetTriangles(System.Array.Empty<int>(), 0, false);
                }

                batch.mesh.SetVertices(batch.vertices);
                batch.mesh.SetUVs(0, batch.uv);
                batch.mesh.SetUVs(1, batch.row0);
                batch.mesh.SetUVs(2, batch.row1);
                batch.mesh.SetUVs(3, batch.row2);
                batch.mesh.SetUVs(4, batch.row3);
                batch.mesh.SetUVs(5, batch.atlasRects);
                batch.mesh.SetTriangles(batch.triangles, 0, false);
            }
            MaskProperties(batch.texture, batch.opacity, vp, true);
            commands.DrawMesh(batch.mesh, Matrix4x4.identity, maskMaterial, 0, 0, properties);
            lastMaskDrawCalls++;
        }
    }
}
