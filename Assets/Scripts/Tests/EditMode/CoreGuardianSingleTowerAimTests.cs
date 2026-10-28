using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class CoreGuardianSingleTowerAimTests
{
    [TestCase(1f, 0f)]
    [TestCase(-1f, 0f)]
    [TestCase(0f, 1f)]
    [TestCase(0f, -1f)]
    [TestCase(1f, 1f)]
    [TestCase(-1f, 1f)]
    [TestCase(1f, -1f)]
    [TestCase(-1f, -1f)]
    public void BarrelAndMuzzleFollowTarget_WhileBaseAndHousingStayFixed(float x, float y)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Units/tower_single.prefab");
        var root = UnityEngine.Object.Instantiate(prefab);
        try
        {
            var type = TypeCache.GetTypesDerivedFrom<MonoBehaviour>().First(t => t.Name == "CoreGuardianUnitAnimation");
            var body = root.GetComponentsInChildren<SpriteRenderer>().Single(r => r.name == "2D Sprite");
            var source = body.sprite;
            var rotation = body.transform.rotation;
            var unit = (Component)type.GetMethod("Attach").Invoke(null, new object[] { root, "Single" });
            var direction = new Vector2(x, y).normalized;
            type.GetMethod("Aim").Invoke(unit, new object[] { direction, 1f });
            type.GetMethod("Present").Invoke(unit, new object[] { 0f });
            var rig = body.transform.Find("OriginalMotion");
            var basePart = rig.Find("Fixed");
            var basePosition = basePart.position;
            var baseRotation = basePart.rotation;
            var mount = body.transform.TransformPoint(new Vector3(0, 47f / 32f));
            var muzzle = (Vector3)type.GetProperty("MuzzleWorld").GetValue(unit);
            Assert.That(
                Vector2.Dot(((Vector2)(muzzle - mount)).normalized, direction),
                Is.GreaterThan(.999f),
                "Visible barrel axis and physical muzzle must face the selected target"
            );
            Assert.That(rig.GetComponentsInChildren<SpriteRenderer>().Single(r => r.name == "Moving").enabled, Is.True);
            var housings = rig.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(r => r.name.StartsWith("Housing"))
                .ToArray();
            Assert.That(
                housings.Length,
                Is.EqualTo(2),
                "Original turret housing must stay separate from rotating barrel"
            );
            var positions = housings.Select(r => r.transform.position).ToArray();
            type.GetMethod("Fire").Invoke(unit, new object[] { direction, 1f });
            type.GetMethod("Present").Invoke(unit, new object[] { 0f });
            type.GetMethod("Present").Invoke(unit, new object[] { .08f });
            var recoilMuzzle = (Vector3)type.GetProperty("MuzzleWorld").GetValue(unit);
            Assert.That(
                Vector2.Dot(recoilMuzzle - muzzle, direction),
                Is.LessThan(-.001f),
                "Recoil retracts along the aimed barrel, not the original diagonal"
            );
            Assert.That(basePart.position, Is.EqualTo(basePosition));
            Assert.That(basePart.rotation, Is.EqualTo(baseRotation));
            for (int i = 0; i < housings.Length; i++)
            {
                Assert.That(housings[i].transform.position, Is.EqualTo(positions[i]));
            }

            type.GetMethod("Present").Invoke(unit, new object[] { 1f });
            muzzle = (Vector3)type.GetProperty("MuzzleWorld").GetValue(unit);
            Assert.That(
                Vector2.Dot(((Vector2)(muzzle - mount)).normalized, direction),
                Is.GreaterThan(.999f),
                "Recovery must retain aim instead of popping back to the source direction"
            );
            Assert.That(body.sprite, Is.SameAs(source));
            Assert.That(body.transform.rotation, Is.EqualTo(rotation));
            var moving = rig.GetComponentsInChildren<SpriteRenderer>().Single(r => r.name == "Moving").transform;
            var glow = moving.Find("TowerGlow");
            Assert.That(glow, Is.Not.Null);
            Assert.That(
                glow.localRotation,
                Is.EqualTo(Quaternion.identity),
                "Selected local glow follows the rotating barrel"
            );
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
