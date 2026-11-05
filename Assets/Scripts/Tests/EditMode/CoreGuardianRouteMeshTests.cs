using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CoreGuardianRouteMeshTests
{
    GameObject root;
    Texture2D texture;
    Sprite sprite;
    Material material;
    UnityEngine.SceneManagement.Scene preview;
    int undoGroup;
    static Type Builder =>
        AppDomain
            .CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("CoreGuardianRouteMeshBuilder"))
            .First(t => t != null);

    object Call(string name, params object[] args)
    {
        Assert.That(Builder, Is.Not.Null, "Route mesh combiner is missing");
        try
        {
            return Builder.GetMethod(name).Invoke(null, args);
        }
        catch (TargetInvocationException e)
        {
            throw e.InnerException;
        }
    }

    [SetUp]
    public void SetUp()
    {
        UnityEditor.Undo.IncrementCurrentGroup();
        undoGroup = UnityEditor.Undo.GetCurrentGroup();
        preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        root = new GameObject("Route mesh test");
        texture = new Texture2D(8, 8);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview);
        sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), new Vector2(.5f, .5f), 8);
        material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
    }

    SpriteRenderer Add(string name, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.sharedMaterial = material;
        r.sortingOrder = order;
        return r;
    }

    [TearDown]
    public void TearDown()
    {
        if (Builder != null && root != null)
        {
            Call("Restore", root);
        }

        UnityEditor.Undo.RevertAllDownToGroup(undoGroup);
        if (root != null)
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        if (preview.IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
        }

        UnityEngine.Object.DestroyImmediate(material);
        UnityEngine.Object.DestroyImmediate(sprite);
        UnityEngine.Object.DestroyImmediate(texture);
    }

    [Test]
    public void SeparatesOrdersAndSharesTextureWithoutChangingSources()
    {
        var a = Add("A", 1);
        var b = Add("B", 1);
        var c = Add("C", 2);
        var outputs = (MeshRenderer[])Call("Combine", root);
        Assert.That(outputs.Length, Is.EqualTo(2));
        CollectionAssert.AreEquivalent(new[] { 1, 2 }, outputs.Select(r => r.sortingOrder));
        Assert.That(outputs.All(r => r.sharedMaterial.mainTexture == texture), Is.True);
        Assert.That(a.enabled || b.enabled || c.enabled, Is.False);
        Assert.That(a.gameObject.activeSelf && b.gameObject.activeSelf && c.gameObject.activeSelf, Is.True);
        Assert.That(
            outputs.Sum(r => r.GetComponent<MeshFilter>().sharedMesh.vertexCount),
            Is.EqualTo(sprite.vertices.Length * 3)
        );
    }

    [Test]
    public void KeepsHdrTintFlipAndWorldVerticesUnderTransformedParent()
    {
        root.transform.SetPositionAndRotation(new Vector3(2, 3, 0), Quaternion.Euler(0, 0, 25));
        root.transform.localScale = new Vector3(2, .7f, 1);
        var a = Add("A", 1);
        a.transform.localPosition = new Vector3(1, -2, 0);
        a.flipX = true;
        a.flipY = true;
        a.color = new Color(1.3f, 1.25f, .8f, .7f);
        var output = ((MeshRenderer[])Call("Combine", root)).Single();
        var mesh = output.GetComponent<MeshFilter>().sharedMesh;
        var tint = QualitySettings.activeColorSpace == ColorSpace.Linear ? a.color.linear : a.color;
        Assert.That(
            mesh.colors.All(c => Mathf.Abs(c.r - tint.r) < .00001f && Mathf.Abs(c.a - .7f) < .00001f),
            Is.True,
            "HDR tint must keep working-space precision, not Color32"
        );
        foreach (var v in sprite.vertices)
        {
            var expected = a.transform.TransformPoint(new Vector3(-v.x, -v.y, 0));
            Assert.That(
                mesh.vertices.Any(p => Vector3.Distance(output.transform.TransformPoint(p), expected) < .00001f),
                Is.True
            );
        }
    }

    [Test]
    public void RestorePreservesInitiallyDisabledRenderersAndOriginalObjects()
    {
        var a = Add("A", 1);
        var b = Add("Disabled", 2);
        b.enabled = false;
        Call("Combine", root);
        Call("Restore", root);
        Assert.That(a.enabled, Is.True);
        Assert.That(b.enabled, Is.False);
        Assert.That(root.GetComponentsInChildren<MeshRenderer>(true), Is.Empty);
        Assert.That(root.transform.childCount, Is.EqualTo(2));
    }

    [Test]
    public void UnsupportedMaskFailsBeforeAnySourceIsDisabled()
    {
        var a = Add("A", 1);
        var b = Add("Masked", 1);
        b.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        Assert.Throws<InvalidOperationException>(() => Call("Combine", root));
        Assert.That(a.enabled && b.enabled, Is.True);
        Assert.That(root.GetComponentsInChildren<MeshRenderer>(true), Is.Empty);
    }

    [Test]
    public void RejectsSecondCombineWithoutLeakingOrDisablingOutputs()
    {
        Add("A", 1);
        Call("Combine", root);
        Assert.Throws<InvalidOperationException>(() => Call("Combine", root));
        Assert.That(root.GetComponentsInChildren<MeshRenderer>(true).Count(r => r.enabled), Is.EqualTo(1));
    }

    [Test]
    public void ExplicitFallbackStaysOriginalAcrossCombineAndRestore()
    {
        var a = Add("Merge", 1);
        var b = Add("Keep original", 1);
        var outputs = (MeshRenderer[])Call("CombineKeeping", root, new[] { b });
        Assert.That(a.enabled, Is.False);
        Assert.That(b.enabled, Is.True);
        Assert.That(
            outputs.Single().GetComponent<MeshFilter>().sharedMesh.vertexCount,
            Is.EqualTo(sprite.vertices.Length)
        );
        Call("Restore", root);
        Assert.That(a.enabled && b.enabled, Is.True);
    }

    [Test]
    public void RoadBakeKeepsOverlappingEqualOrderCornersOriginal()
    {
        var a = Add("Corner A", 2);
        var b = Add("Corner B", 2);
        var c = Add("Separate stone", 1);
        c.transform.localPosition = new Vector3(3, 0, 0);
        Call("CombineRoad", root);
        Assert.That(a.enabled && b.enabled, Is.True);
        Assert.That(c.enabled, Is.False);
    }
}
