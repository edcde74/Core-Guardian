using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CoreGuardian;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class CoreGuardianTowerGlowPlayModeTests
{
    [UnityTest]
    public IEnumerator ActualLaunchesLightBothTowers_AndOnlySupportInRange()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",
            new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single)
        );
        yield return null;
        var type = typeof(CoreGuardianPrototype);
        var game = (MonoBehaviour)UnityEngine.Object.FindObjectsByType(type, FindObjectsSortMode.None).Single();
        var session = (Session)type.GetProperty("Session").GetValue(game);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        // Injected functional QA: isolated board/resources/targets; never a balance measurement.
        game.enabled = false;
        session.StartDiagnosticEncounter();
        session.SetBoard(
            30,
            30,
            new Vector2Int(20, 20),
            new[] { Vector2Int.zero, new Vector2Int(20, 0), new Vector2Int(20, 20) }
        );
        string reason;
        var single = session.TryInstall(TowerKind.Single, new Vector2Int(1, 1), out reason);
        var multi = session.TryInstall(TowerKind.Multi, new Vector2Int(2, 1), out reason);
        var near = session.TryInstall(TowerKind.Support, new Vector2Int(1, 2), out reason);
        var far = session.TryInstall(TowerKind.Support, new Vector2Int(25, 10), out reason);
        Assert.That(new[] { single, multi, near, far }.All(t => t != null), Is.True, reason);
        single.Stats.Rate = 20;
        multi.Stats.Rate = 20;
        type.GetMethod("SynchronizeViews", flags).Invoke(game, null);
        var views = (System.Collections.IDictionary)type.GetField("towerViews", flags).GetValue(game);
        session.StartCombat();
        foreach (float distance in new[] { 0f, 1f, 2f })
        {
            session.DebugSpawn(EnemyKind.Defense, distance).Health = 10000;
        }

        int singleShots = 0,
            multiShots = 0,
            supportShots = 0;
        session.ProjectileLaunched += p =>
        {
            if (p.Source == "Tower:" + single.Id)
            {
                singleShots++;
            }
            if (p.Source == "Tower:" + multi.Id)
            {
                multiShots++;
            }
            if (p.Source == "Tower:" + near.Id || p.Source == "Tower:" + far.Id)
            {
                supportShots++;
            }
        };
        session.Tick(.05f, new Vector2(-100, -100));
        Assert.That(singleShots, Is.GreaterThan(0));
        Assert.That(multiShots, Is.GreaterThanOrEqualTo(3));
        Assert.That(supportShots, Is.Zero);
        foreach (var tower in new[] { single, multi, near, far })
        {
            var view = (GameObject)views[tower.Id];
            var glow = view.GetComponentsInChildren<MeshRenderer>(true).First(r => r.name == "TowerGlow");
            Assert.That(glow.sharedMaterial.GetFloat("_Age"), tower == far ? Is.GreaterThan(10) : Is.EqualTo(0));
            if (tower != far)
            {
                Assert.That(glow.sharedMaterial.GetFloat("_PulseScale"), Is.LessThan(.5f));
            }
        }
        // A later real frame must trigger again, rather than being swallowed by volley coalescing.
        yield return null;
        session.Tick(.1f, new Vector2(-100, -100));
        Assert.That(singleShots, Is.GreaterThan(1));
        Assert.That(multiShots, Is.GreaterThan(3));
        foreach (var tower in new[] { single, multi, near })
        {
            var view = (GameObject)views[tower.Id];
            var unit = view.GetComponents<Component>()
                .First(c => c != null && c.GetType().Name == "CoreGuardianUnitAnimation");
            unit.GetType().GetMethod("Present").Invoke(unit, new object[] { 0f });
            unit.GetType().GetMethod("Present").Invoke(unit, new object[] { .5f });
            Assert.That(
                view.GetComponentsInChildren<MeshRenderer>(true)
                    .First(r => r.name == "TowerGlow")
                    .sharedMaterial.GetFloat("_Age"),
                Is.GreaterThan(.3f)
            );
        }
#else
        yield return null;
#endif
    }
}
