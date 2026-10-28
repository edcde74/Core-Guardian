using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class CoreGuardianMaskAtlasTests
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    static Type Find(string name) =>
        AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).FirstOrDefault(t => t != null);

    [Test]
    public void DistinctSourceTexturesShareMaskDraw_WhileAlphaAndUnknownSourcesRemainSeparate()
    {
        var atlasType = Find("CoreGuardianMaskAtlas");
        var builder = Find("CoreGuardianMaskAtlasBuilder");
        Assert.That(atlasType, Is.Not.Null, "A mask-only source/page manifest is required");
        Assert.That(builder, Is.Not.Null);
        var nativeBefore = NativeCounts();
        string folder = "Assets/MaskAtlasTest_" + Guid.NewGuid().ToString("N");
        var textures = new Texture2D[4];
        var sprites = new Sprite[4];
        var root = new GameObject("Atlas test");
        var camRoot = new GameObject("Atlas camera");
        RenderTexture image = null;
        Texture2DArray replacement = null;
        ScriptableObject empty = null;
        try
        {
            AssetDatabase.CreateFolder("Assets", folder.Substring("Assets/".Length));
            for (int i = 0; i < 4; i++)
            {
                int width = i == 3 ? 16 : 8;
                textures[i] = new Texture2D(width, 8, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "Input" + i,
                };
                var pixels = Enumerable
                    .Range(0, width * 8)
                    .Select(v => new Color32(255, 255, 255, (byte)(v % 5 == i ? 0 : 255)))
                    .ToArray();
                textures[i].SetPixels32(pixels);
                textures[i].Apply();
                AssetDatabase.CreateAsset(textures[i], folder + "/Input" + i + ".asset");
                sprites[i] = Sprite.Create(textures[i], new Rect(1, 2, 5, 4), new Vector2(.3f, .4f), 8);
            }
            var manifest = builder
                .GetMethod("Build")
                .Invoke(null, new object[] { new[] { textures[0], textures[1], textures[3] }, folder + "/Atlas", 128 });
            Assert.That(manifest, Is.Not.Null);
            var entries = (Array)atlasType.GetField("entries", Hidden).GetValue(manifest);
            Assert.That(entries.Length, Is.EqualTo(3));
            foreach (var e in entries)
            {
                var et = e.GetType();
                var src = (Texture2D)et.GetField("source").GetValue(e);
                Assert.That(
                    et.GetField("page").GetValue(e),
                    Is.InstanceOf<Texture2DArray>(),
                    "Array pages preserve source UV and texture dimensions exactly"
                );
                var page = (Texture2DArray)et.GetField("page").GetValue(e);
                int layer = (int)et.GetField("layer").GetValue(e);
                var old = src.GetPixels32();
                var copy = (Color32[])builder.GetMethod("ReadLayer").Invoke(null, new object[] { page, layer });
                for (int y = 0; y < src.height; y++)
                {
                    for (int x = 0; x < src.width; x++)
                    {
                        Assert.That(copy[y * page.width + x].a, Is.EqualTo(old[y * src.width + x].a));
                    }
                }

                Assert.That(page.width, Is.EqualTo(src.width));
                Assert.That(page.height, Is.EqualTo(src.height));
                Assert.That(page.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(page.mipmapCount, Is.EqualTo(1));
            }
            root.transform.position = new Vector3(10000, 10000, 0);
            var art = new GameObject("2D Sprite");
            art.transform.SetParent(root.transform, false);
            var body = art.AddComponent<SpriteRenderer>();
            body.sprite = sprites[0];
            body.enabled = false;
            var a = new GameObject("A").AddComponent<SpriteRenderer>();
            a.transform.SetParent(art.transform, false);
            a.sprite = sprites[0];
            var b = new GameObject("B").AddComponent<SpriteRenderer>();
            b.transform.SetParent(art.transform, false);
            b.sprite = sprites[1];
            b.flipX = true;
            var c = new GameObject("C").AddComponent<SpriteRenderer>();
            c.transform.SetParent(art.transform, false);
            c.sprite = sprites[0];
            c.transform.localPosition = Vector3.right * .25f;
            var camera = camRoot.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 1;
            camera.transform.position = root.transform.position + Vector3.back * 10;
            image = new RenderTexture(128, 128, 0);
            image.Create();
            camera.targetTexture = image;
            var type = Find("CoreGuardianSelectionOutline");
            var owner = root.AddComponent(type);
            type.GetField("atlas", Hidden).SetValue(owner, manifest);
            empty = ScriptableObject.CreateInstance(atlasType);
            var reference = camRoot.AddComponent(type);
            type.GetField("atlas", Hidden).SetValue(reference, empty);
            int Draws()
            {
                type.GetMethod("Present").Invoke(owner, new object[] { root.transform, camera });
                type.GetMethod("Present").Invoke(reference, new object[] { root.transform, camera });
                Assert.That(
                    ReadMask(owner),
                    Is.EqualTo(ReadMask(reference)),
                    "Cropped/flip/alpha/fallback/shrink must preserve actual mask pixels"
                );
                return (int)type.GetField("lastMaskDrawCalls", Hidden).GetValue(owner);
            }
            Assert.That(Draws(), Is.EqualTo(1), "Different source textures sharing a page must combine");
            c.enabled = false;
            Assert.That(Draws(), Is.EqualTo(1), "Three-part to two-part combined mesh shrink must preserve pixels");
            b.color = new Color(1, 1, 1, .5f);
            Assert.That(Draws(), Is.EqualTo(2));
            b.color = Color.white;
            b.sprite = sprites[2];
            Assert.That(Draws(), Is.EqualTo(2));
            Assert.That((int)type.GetField("lastAtlasMisses", Hidden).GetValue(owner), Is.EqualTo(1));
            b.sprite = sprites[3];
            Assert.That(Draws(), Is.EqualTo(2), "Different source dimensions require different array pages");
            Assert.That((int)type.GetField("lastAtlasMisses", Hidden).GetValue(owner), Is.Zero);
            b.sprite = sprites[0];
            Assert.That(Draws(), Is.EqualTo(1));
            b.gameObject.SetActive(false);
            Assert.That(Draws(), Is.EqualTo(1));
            Assert.That(a.sprite, Is.SameAs(sprites[0]));
            b.gameObject.SetActive(true);
            b.sprite = sprites[1];
            object first = entries.GetValue(0);
            var entryType = first.GetType();
            replacement = UnityEngine.Object.Instantiate((Texture2DArray)entryType.GetField("page").GetValue(first));
            entryType.GetField("page").SetValue(first, replacement);
            entries.SetValue(first, 0);
            atlasType.GetMethod("Initialize").Invoke(manifest, new object[] { entries, "revision-test" });
            Assert.That(Draws(), Is.EqualTo(2), "Rebake must invalidate cached geometry and singleton meshes");
            type.GetMethod("Present").Invoke(owner, new object[] { null, camera });
            Assert.That((int)type.GetField("lastMaskDrawCalls", Hidden).GetValue(owner), Is.Zero);
        }
        finally
        {
            // EditMode-created disabled MonoBehaviours need explicit owner cleanup.
            foreach (
                var owner in root.GetComponentsInChildren<MonoBehaviour>(true)
                    .Concat(camRoot.GetComponentsInChildren<MonoBehaviour>(true))
            )
            {
                if (owner != null && owner.GetType().Name == "CoreGuardianSelectionOutline")
                {
                    owner.GetType().GetMethod("OnDestroy", Hidden).Invoke(owner, null);
                }
            }

            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(camRoot);
            if (image != null)
            {
                image.Release();
                UnityEngine.Object.DestroyImmediate(image);
            }
            foreach (var s in sprites)
            {
                if (s != null)
                {
                    UnityEngine.Object.DestroyImmediate(s);
                }
            }

            if (AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.DeleteAsset(folder);
            }

            if (replacement != null)
            {
                UnityEngine.Object.DestroyImmediate(replacement);
            }

            if (empty != null)
            {
                UnityEngine.Object.DestroyImmediate(empty);
            }

            Assert.That(
                NativeCounts(),
                Is.EqualTo(nativeBefore),
                "The test must release every mask, mesh and material it owns"
            );
        }
    }

    static (int masks, int meshes, int materials) NativeCounts() =>
        (
            Resources.FindObjectsOfTypeAll<RenderTexture>().Count(t => t.name == "Selected unit union mask"),
            Resources
                .FindObjectsOfTypeAll<Mesh>()
                .Count(m => m.name.StartsWith("Selection mask ") || m.name == "Selected unit outline quad"),
            Resources
                .FindObjectsOfTypeAll<Material>()
                .Count(m =>
                    m.shader != null
                    && (
                        m.shader.name == "CoreGuardian/SelectionMask"
                        || m.shader.name == "CoreGuardian/SelectionOutline"
                    )
                )
        );

    static Color32[] ReadMask(Component owner)
    {
        var mask = (RenderTexture)owner.GetType().GetField("mask", Hidden).GetValue(owner);
        var old = RenderTexture.active;
        var copy = new Texture2D(mask.width, mask.height, TextureFormat.RGBA32, false, true);
        try
        {
            RenderTexture.active = mask;
            copy.ReadPixels(new Rect(0, 0, mask.width, mask.height), 0, 0);
            copy.Apply();
            return copy.GetPixels32();
        }
        finally
        {
            RenderTexture.active = old;
            UnityEngine.Object.DestroyImmediate(copy);
        }
    }

    [Test]
    public void SourceReimportInvalidatesOldAlphaWithoutChangingTheBody()
    {
        string folder = "Assets/MaskAtlasStaleTest_" + Guid.NewGuid().ToString("N");
        var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false)
        {
            name = "Source",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        try
        {
            AssetDatabase.CreateFolder("Assets", folder.Substring(7));
            string path = folder + "/Source.asset";
            texture.SetPixels32(new Color32[64]);
            texture.Apply();
            AssetDatabase.CreateAsset(texture, path);
            var manifest = Find("CoreGuardianMaskAtlasBuilder")
                .GetMethod("Build")
                .Invoke(null, new object[] { new[] { texture }, folder + "/Atlas", 128 });
            var get = manifest.GetType().GetMethod("TryGet");
            var entry = ((Array)manifest.GetType().GetField("entries", Hidden).GetValue(manifest)).GetValue(0);
            var oldPage = (Texture2DArray)entry.GetType().GetField("page").GetValue(entry);
            var builder = Find("CoreGuardianMaskAtlasBuilder");
            var oldPixels = (Color32[])builder.GetMethod("ReadLayer").Invoke(null, new object[] { oldPage, 0 });
            bool Mapped() => (bool)get.Invoke(manifest, new object[] { texture, null });
            Assert.That(Mapped(), Is.True);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            EditorUtility.SetDirty(texture);
            AssetDatabase.SaveAssetIfDirty(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            Assert.That(Mapped(), Is.False, "Edited source must fall back until an explicit successful rebake");
            builder.GetMethod("Build").Invoke(null, new object[] { new[] { texture }, folder + "/Atlas", 128 });
            var updated = ((Array)manifest.GetType().GetField("entries", Hidden).GetValue(manifest)).GetValue(0);
            Assert.That(
                updated.GetType().GetField("page").GetValue(updated),
                Is.Not.SameAs(oldPage),
                "Publication must not overwrite pages used by the previous manifest"
            );
            Assert.That(
                builder.GetMethod("ReadLayer").Invoke(null, new object[] { oldPage, 0 }),
                Is.EqualTo(oldPixels)
            );
        }
        finally
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }
    }
}
