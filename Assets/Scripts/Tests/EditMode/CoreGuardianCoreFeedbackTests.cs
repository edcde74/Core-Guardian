using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class CoreGuardianCoreFeedbackTests
{
    [Test]
    public void CoreFeedbackTurnsWhiteThenRed_RejectsOverlaps_AndRestoresPresentation()
    {
        var type = typeof(CoreGuardianCoreFeedback);
        Assert.That(type, Is.Not.Null, "Selected core feedback must exist");
        var go = new GameObject("CoreFeedbackTest");
        var ui = new GameObject("CoreLabel", typeof(RectTransform), typeof(Text));
        object flash = null;
        try
        {
            var body = go.AddComponent<SpriteRenderer>();
            var original = body.sharedMaterial;
            var color = new Color(.3f, .6f, .7f, .8f);
            body.color = color;
            var text = ui.GetComponent<Text>();
            text.color = new Color(.8f, .9f, 1, .6f);
            var uiColor = text.color;
            var saved = new MaterialPropertyBlock();
            saved.SetFloat("_Preserved", .42f);
            body.SetPropertyBlock(saved);
            flash = Activator.CreateInstance(type, new object[] { body, new Graphic[] { text } });
            Assert.That(type.GetMethod("Trigger").Invoke(flash, null), Is.EqualTo(true));
            var block = new MaterialPropertyBlock();
            body.GetPropertyBlock(block);
            Assert.That(block.GetColor("_HitFlashTint").g, Is.GreaterThan(.8f));
            Assert.That(block.GetFloat("_HitFlashAmount"), Is.GreaterThan(.4f));
            type.GetMethod("Advance").Invoke(flash, new object[] { .06f });
            body.GetPropertyBlock(block);
            Assert.That(block.GetColor("_HitFlashTint").r, Is.GreaterThan(block.GetColor("_HitFlashTint").g * 2));
            Assert.That(text.color.r, Is.GreaterThan(text.color.g));
            Assert.That(text.color.a, Is.EqualTo(uiColor.a));
            Assert.That(type.GetMethod("Trigger").Invoke(flash, null), Is.EqualTo(false));
            type.GetMethod("Advance").Invoke(flash, new object[] { .19f });
            Assert.That(body.sharedMaterial, Is.SameAs(original));
            Assert.That(body.color, Is.EqualTo(color));
            body.GetPropertyBlock(block);
            Assert.That(block.GetFloat("_Preserved"), Is.EqualTo(.42f));
            Assert.That(
                type.GetMethod("Trigger").Invoke(flash, null),
                Is.EqualTo(false),
                "Cooldown must last beyond body restoration"
            );
            type.GetMethod("Advance").Invoke(flash, new object[] { .06f });
            Assert.That(text.color, Is.EqualTo(uiColor));
            Assert.That(type.GetMethod("Trigger").Invoke(flash, null), Is.EqualTo(true));
            type.GetMethod("Clear").Invoke(flash, null);
            Assert.That(body.sharedMaterial, Is.SameAs(original));
            Assert.That(text.color, Is.EqualTo(uiColor));
        }
        finally
        {
            if (flash is IDisposable d)
            {
                d.Dispose();
            }
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(ui);
        }
    }
}
