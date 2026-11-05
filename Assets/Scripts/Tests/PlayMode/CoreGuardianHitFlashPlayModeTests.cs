using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class CoreGuardianHitFlashPlayModeTests
{
    [UnityTest]
    public IEnumerator RealProjectileDamageFlashesTheSurvivingMonster_WithoutChangingExistingFeedback()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",
            new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single)
        );
        yield return null;
        var type = typeof(CoreGuardianPrototype);
        var game = (Component)UnityEngine.Object.FindObjectsByType(type, FindObjectsSortMode.None).Single();
        var session = (CoreGuardian.Session)type.GetProperty("Session").GetValue(game);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var position = (Vector2)type.GetMethod("PlayerTile", flags).Invoke(game, null);
        var synchronize = type.GetMethod("SynchronizeViews", flags);
        var oldScale = Time.timeScale;
        bool received = false,
            flashed = false;
        try
        {
            // Follow the current scene route instead of assuming a historical path offset.
            var path = (System.Collections.Generic.List<Vector2>)
                typeof(CoreGuardian.Session).GetField("path", flags).GetValue(session);
            float travelled = 0,
                spawnDistance = 0,
                nearest = float.PositiveInfinity;
            for (int i = 0; i < path.Count - 1; i++)
            {
                var segment = path[i + 1] - path[i];
                float length = segment.magnitude;
                if (length <= 0)
                    continue;

                float along = Mathf.Clamp(Vector2.Dot(position - path[i], segment) / length, 0, length);
                float distance = Vector2.Distance(position, path[i] + segment * (along / length));
                if (distance < nearest)
                {
                    nearest = distance;
                    spawnDistance = travelled + along;
                }
                travelled += length;
            }
            Assert.That(
                nearest,
                Is.LessThan(session.Player.Range),
                "The actual player must be in range of the scene route"
            );
            Time.timeScale = 0;
            session.StartCombat();
            var enemy = session.DebugSpawn(CoreGuardian.EnemyKind.Defense, spawnDistance);
            synchronize.Invoke(game, null);
            int health = enemy.Health;
            session.DamageApplied += (target, damage) =>
            {
                if (target.Handle == enemy && damage > 0)
                {
                    received = true;
                }
            };
            for (int step = 0; step < 200 && !received; step++)
            {
                session.Tick(.02f, position);
                synchronize.Invoke(game, null);
            }
            Assert.That(received, Is.True, "A real player projectile must hit the isolated target");
            Assert.That(enemy.Health, Is.LessThan(health));
            Assert.That(enemy.Alive, Is.True);
            var enemyView = GameObject.Find("Enemy_Defense_" + enemy.Id);
            Assert.That(enemyView, Is.Not.Null);
            var body = enemyView.GetComponentsInChildren<SpriteRenderer>(true).First(r => r.name == "2D Sprite");
            foreach (var renderer in body.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                if (renderer.enabled && renderer.gameObject.activeInHierarchy && block.GetFloat("_HitFlashAmount") > 0)
                {
                    flashed = true;
                }
            }
            Assert.That(flashed, Is.True, "DamageApplied must trigger the body shader in the combat view");
            Assert.That(
                UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Any(r => r.name == "EnemyHit"),
                Is.True,
                "Existing approved hit sprites stay in place"
            );
            Assert.That(GameObject.Find("HitDamage"), Is.Not.Null, "Existing damage numbers stay in place");
        }
        finally
        {
            Time.timeScale = oldScale;
        }
#else
        yield return null;
#endif
    }
}
