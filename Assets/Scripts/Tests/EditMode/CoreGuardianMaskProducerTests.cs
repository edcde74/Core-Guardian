using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

public class CoreGuardianMaskProducerTests
{
    static Type Page => typeof(CoreGuardianMaskPage);
    static MethodInfo Queue
    {
        get
        {
            var m = Page.GetMethod("Queue");
            Assert.That(m, Is.Not.Null, "Direct shared mask producer is missing");
            return m;
        }
    }

    static object Slot(Camera camera, int frame = 10)
    {
        var args = new object[] { camera, frame, 64, 64, null };
        Assert.That(Page.GetMethod("TryAcquire").Invoke(null, args), Is.EqualTo(true));
        return args[4];
    }

    static bool Add(object slot, Fixture f, float threshold, Matrix4x4 matrix) =>
        (bool)
            Queue.Invoke(
                null,
                new object[] { slot, f.mesh, f.material, matrix, f.vp, f.texture, 1f, threshold, new Vector2(64, 64) }
            );

    static void Flush(Camera camera) => Page.GetMethod("Flush").Invoke(null, new object[] { camera });

    sealed class Fixture : IDisposable
    {
        public readonly Camera camera;
        public readonly Texture2D texture;
        public readonly Material material;
        public readonly Mesh mesh;
        public readonly Matrix4x4 vp;

        public Fixture(float alpha = 1)
        {
            camera = new GameObject("B3 camera").AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 1;
            camera.aspect = 1;
            camera.transform.position = Vector3.back * 10;
            vp = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true) * camera.worldToCameraMatrix;
            texture = new Texture2D(8, 8, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels(
                Enumerable.Range(0, 64).Select(i => new Color(1, 1, 1, i % 8 >= 2 && i / 8 < 6 ? alpha : 0)).ToArray()
            );
            texture.Apply();
            mesh = new Mesh();
            mesh.vertices = new[]
            {
                new Vector3(-.5f, -.5f),
                new Vector3(.5f, -.5f),
                new Vector3(.5f, .5f),
                new Vector3(-.5f, .5f),
            };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            material = new Material(Resources.Load<Shader>("CoreGuardian/SelectionMask"));
        }

        public Color32[] Original(float threshold, params Matrix4x4[] parts)
        {
            var image = new RenderTexture(64, 64, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            image.Create();
            var cb = new CommandBuffer();
            var p = new MaterialPropertyBlock();
            try
            {
                cb.SetRenderTarget(image);
                cb.ClearRenderTarget(false, true, Color.clear);
                cb.SetViewport(new Rect(0, 0, 64, 64));
                p.SetTexture("_MainTex", texture);
                p.SetFloat("_Opacity", 1);
                p.SetFloat("_Threshold", threshold);
                p.SetMatrix("_MaskVP", vp);
                foreach (var part in parts)
                {
                    cb.DrawMesh(mesh, part, material, 0, 0, p);
                }

                Graphics.ExecuteCommandBuffer(cb);
                return Read(image);
            }
            finally
            {
                cb.Dispose();
                image.Release();
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        public void Dispose()
        {
            Page.GetMethod("ReleaseCamera").Invoke(null, new object[] { camera });
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(mesh);
            UnityEngine.Object.DestroyImmediate(material);
        }
    }

    static Color32[] Read(RenderTexture image)
    {
        var old = RenderTexture.active;
        var read = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
        try
        {
            RenderTexture.active = image;
            read.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0);
            read.Apply();
            return read.GetPixels32();
        }
        finally
        {
            RenderTexture.active = old;
            UnityEngine.Object.DestroyImmediate(read);
        }
    }

    static Color32[] Pixels(object slot)
    {
        var texture = (RenderTexture)slot.GetType().GetProperty("Texture").GetValue(slot);
        var rect = (RectInt)slot.GetType().GetProperty("Rect").GetValue(slot);
        var copy = new RenderTexture(
            rect.width,
            rect.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear
        );
        copy.Create();
        try
        {
            Graphics.CopyTexture(texture, 0, 0, rect.x, rect.y, rect.width, rect.height, copy, 0, 0, 0, 0);
            return Read(copy);
        }
        finally
        {
            copy.Release();
            UnityEngine.Object.DestroyImmediate(copy);
        }
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void PendingMaskIsNotPublishedUntilProducerFlush(int action)
    {
        var cache = Page.Assembly.GetType("CoreGuardianBodyMaskCache");
        var pending = cache.GetMethod("MarkPending", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(
            pending,
            Is.Not.Null,
            "Deferred production must have an explicit pending state instead of publishing unfinished pixels"
        );
        using (var f = new Fixture())
        {
            var type = Page.Assembly.GetType("CoreGuardianSelectionOutline");
            var root = new GameObject("Pending body");
            var body = new GameObject("2D Sprite").AddComponent<SpriteRenderer>();
            body.transform.SetParent(root.transform);
            var sprite = Sprite.Create(
                f.texture,
                new Rect(0, 0, 8, 8),
                Vector2.one * .5f,
                8,
                0,
                SpriteMeshType.FullRect
            );
            body.sprite = sprite;
            try
            {
                var owner = root.AddComponent(type);
                type.GetMethod("Present").Invoke(owner, new object[] { root.transform, f.camera });
                var binding = type.GetField("maskBinding", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(owner);
                var record = binding
                    .GetType()
                    .GetField("current", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(binding);
                var texture = (RenderTexture)
                    record
                        .GetType()
                        .GetField("texture", BindingFlags.Instance | BindingFlags.NonPublic)
                        .GetValue(record);
                var clear = new CommandBuffer();
                clear.SetRenderTarget(texture);
                clear.ClearRenderTarget(false, true, Color.clear);
                Graphics.ExecuteCommandBuffer(clear);
                clear.Dispose();
                var args = new object[] { f.camera, Time.frameCount, texture.width, texture.height, null };
                Page.GetMethod("TryAcquire").Invoke(null, args);
                var slot = args[4];
                var qArgs = new object[]
                {
                    slot,
                    f.mesh,
                    f.material,
                    Matrix4x4.identity,
                    f.vp,
                    f.texture,
                    1f,
                    .1f,
                    new Vector2(texture.width, texture.height),
                };
                Queue.Invoke(null, qArgs);
                pending.Invoke(null, new object[] { binding, slot });
                Assert.That(
                    (bool)
                        record
                            .GetType()
                            .GetField("valid", BindingFlags.Instance | BindingFlags.NonPublic)
                            .GetValue(record),
                    Is.False
                );
                if (action == 1)
                {
                    var otherRoot = new GameObject("Other mask threshold");
                    otherRoot.transform.SetParent(root.transform);
                    var other = otherRoot.AddComponent(type);
                    type.GetField("alphaThreshold").SetValue(other, .9f);
                    type.GetMethod("Present").Invoke(other, new object[] { root.transform, f.camera });
                    cache
                        .GetMethod("Release", BindingFlags.Static | BindingFlags.NonPublic)
                        .Invoke(null, new[] { binding });
                    Assert.That(
                        (bool)Page.GetMethod("IsValid").Invoke(null, new[] { slot }),
                        Is.False,
                        "Returned spare records must not retain queued production leases"
                    );
                    return;
                }
                if (action == 2)
                {
                    Page.GetMethod("ReleaseCamera").Invoke(null, new object[] { f.camera });
                }

                Assert.That(
                    action != 2
                        || (bool)
                            record
                                .GetType()
                                .GetField("pending", BindingFlags.Instance | BindingFlags.NonPublic)
                                .GetValue(record) == false,
                    Is.True,
                    "Camera release must complete the legacy fallback before dropping queued geometry"
                );
                cache
                    .GetMethod("Flush", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { f.camera });
                Assert.That(
                    (bool)
                        record
                            .GetType()
                            .GetField("valid", BindingFlags.Instance | BindingFlags.NonPublic)
                            .GetValue(record),
                    Is.True
                );
                Assert.That(Read(texture).Count(p => p.r > 128), Is.GreaterThan(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(sprite);
            }
        }
    }

    [Test]
    public void CombinedProducerMatchesEveryIndependentMaskPixel()
    {
        var queue = Queue;
        using (var f = new Fixture())
        {
            var a = Slot(f.camera);
            var b = Slot(f.camera);
            var first = Matrix4x4.Translate(new Vector3(-.15f, .1f, 0));
            var second = Matrix4x4.TRS(new Vector3(.2f, -.1f, 0), Quaternion.identity, new Vector3(-1, 1, 1));
            Add(a, f, .1f, first);
            Add(a, f, .1f, second);
            Add(b, f, .1f, second);
            Flush(f.camera);
            Assert.That(Pixels(a), Is.EqualTo(f.Original(.1f, first, second)));
            Assert.That(Pixels(b), Is.EqualTo(f.Original(.1f, second)));
            Assert.That(Pixels(a).Count(p => p.r > 128), Is.GreaterThan(100));
            Assert.That(
                (int)Page.GetProperty("LastProducerDraws").GetValue(null),
                Is.EqualTo(1),
                "One source texture and one page should submit one merged draw"
            );
        }
    }

    [Test]
    public void SecondFlushInSameFramePreservesCompletedMask()
    {
        using (var f = new Fixture())
        {
            var a = Slot(f.camera);
            Add(a, f, .1f, Matrix4x4.identity);
            Flush(f.camera);
            var before = Pixels(a);
            var b = Slot(f.camera);
            Add(b, f, .1f, Matrix4x4.Translate(Vector3.right * .2f));
            Flush(f.camera);
            Assert.That(Pixels(a), Is.EqualTo(before), "A completed valid slot must survive a later same-frame flush");
        }
    }

    [Test]
    public void QueuedGeometrySurvivesProducerMeshDestruction()
    {
        using (var f = new Fixture())
        {
            var expected = f.Original(.1f, Matrix4x4.identity);
            var slot = Slot(f.camera);
            Add(slot, f, .1f, Matrix4x4.identity);
            UnityEngine.Object.DestroyImmediate(f.mesh);
            Flush(f.camera);
            Assert.That(
                Pixels(slot),
                Is.EqualTo(expected),
                "A queued mask owns its geometry until consumers can sample it"
            );
        }
    }

    [Test]
    public void DifferentAlphaThresholdsStayIndependent()
    {
        var queue = Queue;
        using (var f = new Fixture(.4f))
        {
            var a = Slot(f.camera);
            var b = Slot(f.camera);
            Add(a, f, .1f, Matrix4x4.identity);
            Add(b, f, .75f, Matrix4x4.identity);
            Flush(f.camera);
            Assert.That(Pixels(a), Is.EqualTo(f.Original(.1f, Matrix4x4.identity)));
            Assert.That(Pixels(b), Is.EqualTo(f.Original(.75f, Matrix4x4.identity)));
        }
    }

    [Test]
    public void CameraChangesFlushOnlyItsCommands()
    {
        var queue = Queue;
        using (var a = new Fixture())
        using (var b = new Fixture())
        {
            var first = Slot(a.camera);
            var second = Slot(b.camera);
            Add(first, a, .1f, Matrix4x4.identity);
            Add(second, b, .1f, Matrix4x4.identity);
            a.camera.orthographicSize = 2;
            Flush(a.camera);
            Assert.That((int)Page.GetProperty("LastProducerDraws").GetValue(null), Is.Zero);
            Flush(b.camera);
            Assert.That(Pixels(second), Is.EqualTo(b.Original(.1f, Matrix4x4.identity)));
        }
    }

    [Test]
    public void SameFrameReturnAndReuseNeverShowsOldMask()
    {
        var queue = Queue;
        using (var f = new Fixture(.4f))
        {
            var old = Slot(f.camera);
            Add(old, f, .1f, Matrix4x4.identity);
            Page.GetMethod("Release").Invoke(null, new[] { old });
            var next = Slot(f.camera);
            Add(next, f, .9f, Matrix4x4.identity);
            Flush(f.camera);
            Assert.That(Pixels(next), Is.EqualTo(f.Original(.9f, Matrix4x4.identity)));
        }
    }
}
