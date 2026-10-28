using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

public class CoreGuardianMaskOutputBatchTests
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static Type Batch
    {
        get
        {
            var t = AppDomain
                .CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("CoreGuardianMaskOutputBatch"))
                .FirstOrDefault(x => x != null);
            Assert.That(t, Is.Not.Null, "Mask output batching is missing");
            return t;
        }
    }
    static Type Page => typeof(CoreGuardianMaskPage);
    static Type Outline => typeof(CoreGuardianSelectionOutline);

    static T Field<T>(Component owner, string name) => (T)Outline.GetField(name, Hidden).GetValue(owner);

    static object Call(string name, params object[] args) => Batch.GetMethod(name).Invoke(null, args);

    sealed class Fixture : IDisposable
    {
        public readonly GameObject root = new GameObject("B2 output test");
        public readonly Camera camera;
        public readonly Component[] owners = new Component[2];
        public readonly MeshRenderer[] sources = new MeshRenderer[2];
        public readonly RenderTexture image;
        readonly Texture2D texture;
        readonly Sprite sprite;

        public Fixture(bool contact = false, bool overlap = false)
        {
            camera = new GameObject("B2 camera").AddComponent<Camera>();
            camera.transform.SetParent(root.transform);
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 1;
            camera.aspect = 1;
            camera.transform.position = Vector3.back * 10;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.13f, .24f, .37f, 1);
            camera.cullingMask = 1 << 30;
            image = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32);
            image.Create();
            camera.targetTexture = image;
            texture = new Texture2D(8, 8, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels(
                Enumerable.Range(0, 64).Select(i => i % 8 < 6 && i / 8 < 6 ? Color.white : Color.clear).ToArray()
            );
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), Vector2.one * .5f, 16, 0, SpriteMeshType.FullRect);
            for (int i = 0; i < 2; i++)
            {
                var host = new GameObject("B2 owner " + i);
                host.transform.SetParent(root.transform);
                host.transform.position = new Vector3(i == 0 ? -.45f : (overlap ? -.25f : .45f), 0, 0);
                var body = new GameObject("2D Sprite").AddComponent<SpriteRenderer>();
                body.transform.SetParent(host.transform, false);
                body.sprite = sprite;
                body.sortingOrder = 39;
                owners[i] = host.AddComponent(Outline);
                Outline.GetField("color").SetValue(owners[i], i == 0 ? Color.magenta : Color.cyan);
                Outline.GetMethod("Present").Invoke(owners[i], new object[] { host.transform, camera });
                sources[i] = Field<MeshRenderer>(owners[i], "output");
                sources[i].gameObject.layer = 30;
            }
        }

        public void Register(int index, bool contact = false, bool single = false)
        {
            var mask = Field<RenderTexture>(owners[index], "mask");
            var args = new object[] { camera, Time.frameCount, mask.width, mask.height, null };
            Assert.That(Page.GetMethod("TryAcquire").Invoke(null, args), Is.EqualTo(true));
            var parameters = Activator.CreateInstance(Batch.GetNestedType("OutputParameters"));
            void Set(string name, object value) => parameters.GetType().GetField(name).SetValue(parameters, value);
            Set("contact", contact);
            Set("color", (Color)Outline.GetField("color").GetValue(owners[index]));
            Set("width", 2);
            Set("band", 16f);
            if (single)
            {
                Set("allowSingle", true);
            }

            Set("maskOriginPx", Field<Vector2>(owners[index], "maskOriginPx"));
            Set("sourceSize", new Vector2(mask.width, mask.height));
            Assert.That(
                Call(
                    "Register",
                    owners[index],
                    camera,
                    sources[index],
                    Field<Mesh>(owners[index], "quad"),
                    args[4],
                    parameters
                ),
                Is.EqualTo(true)
            );
        }

        public Color32[] Pixels()
        {
            camera.Render();
            var old = RenderTexture.active;
            var read = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = image;
                read.ReadPixels(new Rect(0, 0, 128, 128), 0, 0);
                read.Apply();
                return read.GetPixels32();
            }
            finally
            {
                RenderTexture.active = old;
                UnityEngine.Object.DestroyImmediate(read);
            }
        }

        public void Dispose()
        {
            var t = AppDomain
                .CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("CoreGuardianMaskOutputBatch"))
                .FirstOrDefault(x => x != null);
            if (t != null)
            {
                foreach (var owner in owners)
                {
                    t.GetMethod("Release").Invoke(null, new object[] { owner });
                }
                t.GetMethod("Restore").Invoke(null, new object[] { camera });
            }
            Page.GetMethod("ReleaseCamera").Invoke(null, new object[] { camera });
            if (camera != null)
            {
                camera.targetTexture = null;
            }

            image.Release();
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(sprite);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    static int Batched => (int)Batch.GetProperty("LastBatchedOutputs").GetValue(null);

    [Test]
    public void DestroyedCameraRestoresOriginalRendererMaterial()
    {
        using (var f = new Fixture(overlap: true))
        {
            var originals = f.sources.Select(s => s.sharedMaterial).ToArray();
            f.Register(0, false, true);
            f.Register(1, false, true);
            Call("Flush", f.camera);
            UnityEngine.Object.DestroyImmediate(f.camera.gameObject);
            Call("Restore", f.camera);
            for (int i = 0; i < 2; i++)
            {
                Assert.That(f.sources[i].sharedMaterial, Is.SameAs(originals[i]));
            }
        }
    }

    [Test]
    public void SharedPageKeepsOverlappingRendererIdentityAndContactPixels()
    {
        var type = Batch;
        using (var f = new Fixture(overlap: true))
        {
            var material = new Material(Resources.Load<Shader>("CoreGuardian/PhotoShadowContact"));
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    Outline
                        .GetMethod("PresentGroundContactWithBand")
                        .Invoke(f.owners[i], new object[] { f.owners[i].transform, f.camera, material, 16f });
                }

                var before = f.Pixels();
                f.Register(0, true, true);
                f.Register(1, true, true);
                Assert.That(f.Pixels(), Is.EqualTo(before));
                Assert.That(Batched, Is.EqualTo(2));
                Assert.That(
                    f.sources.All(r => r.enabled && !r.forceRenderingOff && r.sharedMaterial == material),
                    Is.True,
                    "End camera must restore each original renderer and material"
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }
    }

    [Test]
    public void NativePresentRegistersWithoutChangingLogicalVisibility()
    {
        var type = Batch;
        using (var f = new Fixture())
        {
            f.camera.targetTexture = null;
            f.image.Release();
            f.image.width = 1920;
            f.image.height = 1080;
            f.image.Create();
            f.camera.targetTexture = f.image;
            f.camera.aspect = 1920f / 1080;
            f.camera.orthographicSize = 8.5f;
            for (int i = 0; i < 2; i++)
            {
                Outline.GetMethod("Present").Invoke(f.owners[i], new object[] { f.owners[i].transform, f.camera });
            }

            var registrations = (System.Collections.IDictionary)
                Batch.GetField("entries", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            foreach (var owner in f.owners)
            {
                Assert.That(registrations.Contains(owner), Is.True, "Native Present should register its output");
            }

            Call("Flush", f.camera);
            Assert.That(Batched, Is.EqualTo(2));
            foreach (var owner in f.owners)
            {
                Assert.That((bool)Outline.GetProperty("IsVisible").GetValue(owner), Is.True);
            }

            Call("Restore", f.camera);
            Assert.That(f.sources.All(r => !r.forceRenderingOff), Is.True);
        }
    }

    [Test]
    public void TwoDistinctMasksKeepIndividualInteriorAndOutline()
    {
        var type = Batch;
        using (var f = new Fixture())
        {
            var before = f.Pixels();
            f.Register(0);
            f.Register(1);
            var after = f.Pixels();
            Assert.That(Batched, Is.EqualTo(2));
            Assert.That(after, Is.EqualTo(before));
            Assert.That(after.Distinct().Count(), Is.GreaterThan(2), "Both colored outlines must render");
        }
    }

    [Test]
    public void AtlasEdgesMatchOriginalClamp()
    {
        var type = Batch;
        using (var f = new Fixture())
        {
            // Different masks on adjacent atlas rectangles must not leak through the outline kernel.
            var mask = Field<RenderTexture>(f.owners[0], "mask");
            var cb = new CommandBuffer();
            cb.SetRenderTarget(mask);
            cb.ClearRenderTarget(false, true, Color.white);
            Graphics.ExecuteCommandBuffer(cb);
            cb.Dispose();
            var before = f.Pixels();
            f.Register(0);
            f.Register(1);
            Assert.That(f.Pixels(), Is.EqualTo(before));
            Assert.That(Batched, Is.EqualTo(2));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void InterleavedBodyBreaksBatch(bool disjoint)
    {
        var type = Batch;
        using (var f = new Fixture())
        {
            var foreign = new GameObject("Foreign renderer").AddComponent<SpriteRenderer>();
            foreign.transform.SetParent(f.root.transform);
            foreign.gameObject.layer = 30;
            foreign.sortingOrder = 43;
            foreign.sprite = f.root.GetComponentInChildren<SpriteRenderer>().sprite;
            foreign.transform.position = disjoint ? Vector3.up * 10 : Vector3.left * .45f;
            var before = f.Pixels();
            f.Register(0);
            f.Register(1);
            Assert.That(f.Pixels(), Is.EqualTo(before));
            Assert.That(Batched, Is.EqualTo(disjoint ? 2 : 0));
        }
    }

    [Test]
    public void SortingGroupOrForeignCameraFallsBack()
    {
        var type = Batch;
        using (var f = new Fixture())
        {
            f.root.AddComponent<SortingGroup>();
            f.Register(0);
            f.Register(1);
            f.Pixels();
            Assert.That(Batched, Is.Zero);
            UnityEngine.Object.DestroyImmediate(f.root.GetComponent<SortingGroup>());
            f.Register(0);
            f.Register(1);
            var foreign = new GameObject("Foreign camera").AddComponent<Camera>();
            foreign.enabled = false;
            Call("Flush", foreign);
            Assert.That(f.sources.All(r => !r.forceRenderingOff), Is.True);
            UnityEngine.Object.DestroyImmediate(foreign.gameObject);
        }
    }

    [Test]
    public void ContactBandAndAlphaOverlapMatchOriginal()
    {
        var type = Batch;
        using (var f = new Fixture(overlap: true))
        {
            var material = new Material(Resources.Load<Shader>("CoreGuardian/PhotoShadowContact"));
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    Outline
                        .GetMethod("PresentGroundContactWithBand")
                        .Invoke(f.owners[i], new object[] { f.owners[i].transform, f.camera, material, 16f });
                }

                var before = f.Pixels();
                f.Register(0, true);
                f.Register(1, true);
                Assert.That(f.Pixels(), Is.EqualTo(before));
                var map = (System.Collections.IDictionary)
                    Batch.GetField("entries", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                var rectangles = string.Join(
                    ";",
                    map.Values.Cast<object>().Select(e => e.GetType().GetField("screen", Hidden).GetValue(e).ToString())
                );
                Assert.That(Batched, Is.Zero, "Unproven overlapping transparent order must fall back: " + rectangles);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }
    }

    [Test]
    public void DestroyOrHideRemovesOnlyItsOutput()
    {
        var type = Batch;
        using (var f = new Fixture())
        {
            f.Register(0);
            f.Register(1);
            f.Pixels();
            Assert.That(Batched, Is.EqualTo(2));
            Call("Release", f.owners[0]);
            f.sources[0].enabled = false;
            f.Pixels();
            Assert.That(Batched, Is.Zero);
            Assert.That(f.sources[1].enabled && !f.sources[1].forceRenderingOff, Is.True);
        }
    }
}
