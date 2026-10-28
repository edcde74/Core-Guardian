using System;
using System.Collections;
using System.Linq;
using CoreGuardian;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

[Category("SharedTowersHud"), Category("ThirtyExperiment")]
public class CoreGuardianUpgradeFeedbackPlayModeTests
{
    static Type Find(string name) =>
        AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).FirstOrDefault(t => t != null);

    static void Call(Component g, string method, params object[] args) => g.GetType().GetMethod(method).Invoke(g, args);

    static void Click(Component g, string name) =>
        g
            .transform.Find("CombatHUD")
            .GetComponentsInChildren<Button>(true)
            .Single(b => b.name == name)
            .onClick.Invoke();

    [UnityTest]
    public IEnumerator InspectorDurationDefaultsToPointEight_AndControlsTheActualLifetime()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",
            new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single)
        );
#endif
        yield return null;
        var game = (Component)
            UnityEngine.Object.FindObjectsByType(Find("CoreGuardianPrototype"), FindObjectsSortMode.None).Single();
        var type = Find("CoreGuardianUpgradeFeedback");
        var feedback = game.GetComponent(type);
        var field = type.GetField("duration");
        Assert.That(field, Is.Not.Null, "Scan duration must be exposed in the Inspector");
        float previous = (float)field.GetValue(feedback),
            time = Time.timeScale;
        Assert.That(previous, Is.EqualTo(.8f).Within(.001f));
        Func<int> active = () => (int)type.GetProperty("ActiveCount").GetValue(feedback);
        try
        {
            Time.timeScale = 1;
            Call(game, "Diagnostic");
            field.SetValue(feedback, .15f);
            Call(game, "UpgradePlayer", "damage");
            yield return new WaitForSeconds(.25f);
            Assert.That(active(), Is.Zero, "Inspector short duration controls expiry");
            field.SetValue(feedback, 1.1f);
            Call(game, "UpgradePlayer", "rate");
            yield return new WaitForSeconds(.85f);
            Assert.That(active(), Is.EqualTo(1), "A long Inspector duration must remain active past 0.8 seconds");
            var visible = feedback
                .GetComponentsInChildren<MeshRenderer>()
                .Single(r => r.sharedMaterial != null && r.sharedMaterial.shader.name == "CoreGuardian/UpgradeScan");
            Assert.That(visible.enabled, Is.True);
            Assert.That(
                visible.sharedMaterial.GetFloat("_Opacity"),
                Is.GreaterThan(.1f),
                "The visible sweep must stretch too, rather than leaving an invisible delayed expiry"
            );
            yield return new WaitForSeconds(.35f);
            Assert.That(active(), Is.Zero);
        }
        finally
        {
            field.SetValue(feedback, previous);
            Time.timeScale = time;
        }
    }

    [UnityTest]
    public IEnumerator RealSuccessfulPlayerAndAllTowerPurchasesScan_WhileRejectedPurchasesDoNot()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",
            new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single)
        );
#endif
        yield return null;
        var game = (Component)
            UnityEngine.Object.FindObjectsByType(Find("CoreGuardianPrototype"), FindObjectsSortMode.None).Single();
        var type = Find("CoreGuardianUpgradeFeedback");
        Assert.That(type, Is.Not.Null, "Chosen B feedback is not connected yet");
        var feedback = game.GetComponent(type);
        Assert.That(feedback, Is.Not.Null);
        Func<int> triggers = () => (int)type.GetProperty("TriggerCount").GetValue(feedback);
        Func<int> active = () => (int)type.GetProperty("ActiveCount").GetValue(feedback);
        float old = Time.timeScale;
        try
        {
            Time.timeScale = 0;
            {
                var hud = game.GetComponent(Find("CoreGuardianHud"));
                hud.GetType().GetMethod("ToggleBench").Invoke(hud, null);
            }
            Click(game, "damage");
            Assert.That(triggers(), Is.Zero, "Initial zero gold cannot produce a success scan");
            Call(game, "Diagnostic");
            Click(game, "PlayerBuyTab");
            Click(game, "damage");
            Assert.That(triggers(), Is.EqualTo(1));
            var player = (Component)game.GetType().GetField("player").GetValue(game);
            Assert.That(
                type.GetMethod("IsScanning").Invoke(feedback, new object[] { player.transform }),
                Is.EqualTo(true)
            );
            Call(game, "UpgradePlayer", "rate");
            Assert.That(triggers(), Is.EqualTo(2));
            Assert.That(active(), Is.EqualTo(1), "Rapid repeat restarts the one scan without stacking copies");
            Call(game, "UpgradePlayer", "invalid-stat");
            Assert.That(triggers(), Is.EqualTo(2));
            var kinds = new[] { TowerKind.Single, TowerKind.Multi, TowerKind.Support };
            for (int i = 0; i < kinds.Length; i++)
            {
                if (i == 0)
                {
                    Call(game, "Place", TowerKind.Single, new Vector2Int(15, 15));
                }

                var cell = new Vector2Int(14 + i * 3, 15);
                Call(game, "Place", kinds[i], cell);
                Call(game, "HandleWorldClick", cell);
                Click(game, "LabKind_" + kinds[i]);
                Click(game, kinds[i] == TowerKind.Support ? "Tower_buffDamage" : "Tower_damage");
                Assert.That(triggers(), Is.EqualTo(4 + i), "A shared purchase scans every existing same-kind tower");
                int selected = (int)game.GetType().GetProperty("SelectedTower").GetValue(game);
                var target = (Transform)game.GetType().GetMethod("TowerView").Invoke(game, new object[] { selected });
                Assert.That(type.GetMethod("IsScanning").Invoke(feedback, new object[] { target }), Is.EqualTo(true));
            }
            Assert.That(active(), Is.EqualTo(5));
            Call(game, "StartCombat");
            Call(game, "UpgradeTower", "damage");
            Assert.That(triggers(), Is.EqualTo(6), "Rejected combat tower upgrade cannot trigger success");
            Time.timeScale = 1;
            yield return new WaitForSeconds(.65f);
            Assert.That(active(), Is.EqualTo(5), "Default 0.8 second scans remain active beyond the old duration");
            yield return new WaitForSeconds(.3f);
            Assert.That(active(), Is.Zero, "Scan expires without leaving tinted artwork");
        }
        finally
        {
            Time.timeScale = old;
        }
    }
}
