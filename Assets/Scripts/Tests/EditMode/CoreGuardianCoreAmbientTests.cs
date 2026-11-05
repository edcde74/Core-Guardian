using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class CoreGuardianCoreAmbientTests
{
    [Test]
    public void SceneUnloadCanDestroyTheCoreBeforeItsPresentationOwner()
    {
        var type = typeof(CoreGuardianCoreAmbient);
        var go = new GameObject("UnloadingCore");
        var body = go.AddComponent<SpriteRenderer>();
        var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
        body.sprite = sprite;
        var ambient = Activator.CreateInstance(type, new object[] { body });
        try
        {
            UnityEngine.Object.DestroyImmediate(go);
            Assert.DoesNotThrow(() => type.GetMethod("SetEnabled").Invoke(ambient, new object[] { false }));
            Assert.DoesNotThrow(() => type.GetMethod("NotifyCoreDamage").Invoke(ambient, null));
            Assert.DoesNotThrow(() => type.GetMethod("Advance").Invoke(ambient, new object[] { .1f }));
        }
        finally
        {
            ((IDisposable)ambient).Dispose();
            UnityEngine.Object.DestroyImmediate(sprite);
            if (go != null)
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }

    [Test]
    public void AmbientYieldsToDamage_ResumesAfterFeedback_AndPreservesSourcePresentation()
    {
        var type = typeof(CoreGuardianCoreAmbient);
        Assert.That(type, Is.Not.Null, "Core-only ambient presentation is not implemented");
        var go = new GameObject("CoreAmbientTest");
        object ambient = null;
        Sprite sprite = null;
        var sourceMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        var block = new MaterialPropertyBlock();
        block.SetFloat("_Preserved", .42f);
        try
        {
            var body = go.AddComponent<SpriteRenderer>();
            sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
            body.sprite = sprite;
            body.sharedMaterial = sourceMaterial;
            body.color = new Color(.8f, .9f, 1, .7f);
            body.SetPropertyBlock(block);
            var originalColor = body.color;
            var camera = Camera.main;
            bool hdr = camera != null && camera.allowHDR;
            ambient = Activator.CreateInstance(type, new object[] { body });
            var renderers = go.GetComponentsInChildren<Renderer>().Where(r => r != body).ToArray();
            Assert.That(
                renderers.Length,
                Is.GreaterThanOrEqualTo(2),
                "A localized glow and particle renderer must be present"
            );
            var advance = type.GetMethod("Advance");
            advance.Invoke(ambient, new object[] { .1f });
            Assert.That(renderers.All(r => r.enabled), Is.True);
            type.GetMethod("NotifyCoreDamage").Invoke(ambient, null);
            Assert.That(
                renderers.All(r => !r.enabled),
                Is.True,
                "Ambient must yield immediately to the approved damage flash"
            );
            advance.Invoke(ambient, new object[] { .2f });
            Assert.That(renderers.All(r => !r.enabled), Is.True);
            advance.Invoke(ambient, new object[] { .11f });
            Assert.That(renderers.All(r => r.enabled), Is.True, "Ambient returns after the 300ms feedback window");
            type.GetMethod("SetEnabled").Invoke(ambient, new object[] { false });
            Assert.That(renderers.All(r => !r.enabled), Is.True);
            type.GetMethod("SetEnabled").Invoke(ambient, new object[] { true });
            Assert.That(body.sharedMaterial, Is.SameAs(sourceMaterial));
            Assert.That(body.sprite, Is.SameAs(sprite));
            Assert.That(body.color, Is.EqualTo(originalColor));
            body.GetPropertyBlock(block);
            Assert.That(block.GetFloat("_Preserved"), Is.EqualTo(.42f));
            if (camera != null)
            {
                Assert.That(camera.allowHDR, Is.EqualTo(hdr), "Core-only glow must not enable HDR on the camera");
            }
            ((IDisposable)ambient).Dispose();
            ambient = null;
            Assert.That(go.transform.childCount, Is.Zero, "Owned presentation resources must be removed on dispose");
        }
        finally
        {
            if (ambient is IDisposable disposable)
            {
                disposable.Dispose();
            }

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(sprite);
            UnityEngine.Object.DestroyImmediate(sourceMaterial);
        }
    }
}
