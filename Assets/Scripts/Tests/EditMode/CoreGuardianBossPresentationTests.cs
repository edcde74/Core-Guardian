using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// 2026-10-27 사용자 피드백 1·2: 보스 그림자 폭, 철벽 기사 옆걸음 들썩임.
public class CoreGuardianBossPresentationTests
{
    static System.Type Unit =>
        TypeCache.GetTypesDerivedFrom<MonoBehaviour>().First(x => x.Name == "CoreGuardianUnitAnimation");

    static Component Attach(GameObject root, string role) =>
        (Component)Unit.GetMethod("Attach").Invoke(null, new object[] { root, role });

    static void Step(Component u, Vector2 v, float dt)
    {
        Unit.GetMethod("Move").Invoke(u, new object[] { v, dt });
        Unit.GetMethod("Present").Invoke(u, new object[] { dt });
    }

    static string Prefab(string role) =>
        "Assets/Prefabs/Characters/Enemies/Resources/CoreGuardian/Animation/Boss/" + role + "Animated.prefab";

    [TestCase("IronKnight")]
    [TestCase("CrimsonLord")]
    public void BossShadowIsAboutBodyWidth(string role)
    {
        var path = Prefab(role);
        Assume.That(path, Is.Not.Null, "boss prefab");
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        try
        {
            Attach(root, role);
            var art = root.GetComponentsInChildren<SpriteRenderer>(true).Single(r => r.name == "2D Sprite");
            var cast = root.GetComponentsInChildren<SpriteRenderer>(true).Single(r => r.name == "PhotoCastShadow");
            float ratio = cast.bounds.size.x / art.bounds.size.x;
            Assert.That(
                ratio,
                Is.InRange(.8f, 1.05f),
                "Boss shadow keeps normal-enemy proportion instead of 1.6x spread"
            );
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void IronKnightSideWalk_BobsWholePixels_AndVerticalWalkDoesNot()
    {
        var path = Prefab("IronKnight");
        Assume.That(path, Is.Not.Null, "boss prefab");
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        try
        {
            var unit = Attach(root, "IronKnight");
            var art = root.GetComponentsInChildren<SpriteRenderer>(true).Single(r => r.name == "2D Sprite");
            float px = art.sprite.pixelsPerUnit / art.transform.localScale.y;
            var rest = art.transform.localPosition.y;
            var side = new System.Collections.Generic.HashSet<float>();
            for (int i = 0; i < 40; i++)
            {
                Step(unit, Vector2.left, .03f);
                float bob = Mathf.Round((art.transform.localPosition.y - rest) * px * 100) / 100;
                side.Add(bob);
                int frame = art.sprite.name[art.sprite.name.Length - 1] - '0';
                Assert.That(
                    bob,
                    Is.EqualTo(frame % 2 == 0 ? -2f : 0f),
                    "stride frames (0,2) sit low, passing frames (1,3) high: " + art.sprite.name
                );
            }
            Assert.That(side.Min(), Is.EqualTo(-2f), "dips 2 source pixels");
            Assert.That(side.Max(), Is.EqualTo(0f));
            Assert.That(side.All(v => Mathf.Approximately(v, Mathf.Round(v))), Is.True, "whole pixels only");
            for (int i = 0; i < 40; i++)
            {
                Step(unit, Vector2.up, .03f);
                Assert.That(
                    art.transform.localPosition.y,
                    Is.EqualTo(rest).Within(1e-5f),
                    "up/down frames already carry their own motion"
                );
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
