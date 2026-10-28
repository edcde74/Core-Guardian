using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CoreGuardian;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class CoreGuardianSelectionOutlinePlayModeTests
{
    static Type Find(string name) =>
        AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).FirstOrDefault(t => t != null);

    static Component Game() =>
        (Component)
            UnityEngine.Object.FindObjectsByType(Find("CoreGuardianPrototype"), FindObjectsSortMode.None).Single();

    static void Call(Component g, string name, params object[] args) => g.GetType().GetMethod(name).Invoke(g, args);

    static int Selected(Component g) => (int)g.GetType().GetProperty("SelectedTower").GetValue(g);

    static void Click(Component g, string name) =>
        g
            .transform.Find("CombatHUD")
            .GetComponentsInChildren<Button>(true)
            .Single(b => b.name == name)
            .onClick.Invoke();

    static IEnumerator Load()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",
            new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single)
        );
#endif
        yield return null;
    }

    static Component Hud(Component g) => g.GetComponent(Find("CoreGuardianHud"));

    static void HudCall(Component g, string name, params object[] args)
    {
        var h = Hud(g);
        h.GetType().GetMethod(name).Invoke(h, args);
    }

    static bool Active(Component g, string path) => g.transform.Find("CombatHUD/" + path).gameObject.activeSelf;

    // HUD 개편 B안: 선택 정보는 왼쪽 아래 고정 정보창(SelectionInfo), 닫기는 InfoClose 또는 빈 바닥 클릭.
    [UnityTest]
    public IEnumerator InfoCloseOrFloorClickClearsSelection_WithoutASecondClick()
    {
        yield return Load();
        var g = Game();
        float previous = Time.timeScale;
        try
        {
            Time.timeScale = 0;
            Call(g, "Place", TowerKind.Single, new Vector2Int(14, 15));
            Assert.That(Active(g, "SelectionInfo"), Is.False, "Placement must not auto open info");
            Call(g, "HandleWorldClick", new Vector2Int(14, 15));
            Assert.That(Selected(g), Is.GreaterThanOrEqualTo(0));
            Assert.That(Active(g, "SelectionInfo"), Is.True);
            Assert.That(Active(g, "SelectionInfo/Destroy"), Is.True);
            Click(g, "InfoClose");
            Assert.That(Selected(g), Is.EqualTo(-1), "Closing must release the selected tower");
            Assert.That(Active(g, "SelectionInfo"), Is.False);
            Call(g, "HandleWorldClick", new Vector2Int(14, 15));
            Call(g, "HandleWorldClick", new Vector2Int(13, 14));
            Assert.That(Selected(g), Is.EqualTo(-1), "One empty floor click closes and deselects");
            Assert.That(Active(g, "SelectionInfo"), Is.False);
        }
        finally
        {
            Time.timeScale = previous;
        }
    }

    [UnityTest]
    public IEnumerator PlayerInfoReplacesTowerTarget_AndInstallationRemovesOutline()
    {
        yield return Load();
        var g = Game();
        float previous = Time.timeScale;
        try
        {
            Time.timeScale = 0;
            Call(g, "Place", TowerKind.Single, new Vector2Int(14, 15));
            Call(g, "HandleWorldClick", new Vector2Int(14, 15));
            HudCall(g, "OpenPlayerSpecs");
            Assert.That(Selected(g), Is.EqualTo(-1), "Player inspection cannot retain a competing tower selection");
            var hud = Hud(g);
            var target = hud.GetType().GetProperty("InspectionTarget");
            Assert.That(target, Is.Not.Null);
            Assert.That(target.GetValue(hud), Is.Not.Null);
            HudCall(g, "ToggleBench");
            Assert.That(target.GetValue(hud), Is.Not.Null, "Opening the bench keeps the info selection");
            Click(g, "InfoClose");
            Assert.That(target.GetValue(hud), Is.Null);
            HudCall(g, "OpenPlayerSpecs");
            Click(g, "Installation");
            Assert.That(target.GetValue(hud), Is.Null);
        }
        finally
        {
            Time.timeScale = previous;
        }
    }

    [UnityTest]
    public IEnumerator WorldPlayerClickOpensInfo_AndRepeatedClickKeepsItOpen()
    {
        yield return Load();
        var g = Game();
        float previous = Time.timeScale;
        try
        {
            Time.timeScale = 0;
            var move = (Component)g.GetType().GetField("player").GetValue(g);
            var cell = (Vector3Int)move.GetType().GetProperty("CurrentCell").GetValue(move);
            var xy = new Vector2Int(cell.x, cell.y);
            Call(g, "Place", TowerKind.Single, new Vector2Int(14, 15));
            Call(g, "HandleWorldClick", new Vector2Int(14, 15));
            Call(g, "HandleWorldClick", xy);
            Assert.That(Active(g, "SelectionInfo"), Is.True, "World player click opens the info panel");
            Assert.That(Selected(g), Is.EqualTo(-1));
            Assert.That(Active(g, "PlayerGrowthPanel"), Is.False, "Player click does not open the bench");
            Assert.That(
                g.transform.Find("CombatHUD/SelectionInfo/Title").GetComponent<Text>().text,
                Does.Contain("기술병")
            );
            Call(g, "HandleWorldClick", xy);
            Assert.That(Active(g, "SelectionInfo"), Is.True, "Repeated world selection must not toggle closed");
            yield return null;
            var outline = g.GetComponent(Find("CoreGuardianSelectionOutline"));
            Assert.That(outline.GetType().GetProperty("CurrentTarget").GetValue(outline), Is.EqualTo(move.transform));
            Click(g, "InfoClose");
            yield return null;
            Assert.That(outline.GetType().GetProperty("IsVisible").GetValue(outline), Is.EqualTo(false));
        }
        finally
        {
            Time.timeScale = previous;
        }
    }

    [UnityTest]
    public IEnumerator BuildingsToggleOneBigPanel_AndBlockPlacement()
    {
        yield return Load();
        var g = Game();
        float previous = Time.timeScale;
        try
        {
            Time.timeScale = 0;
            var session = (Session)g.GetType().GetProperty("Session").GetValue(g);
            var lab = new Vector2Int(
                Mathf.RoundToInt(session.Data.Setting("buildingLabX", -1)),
                Mathf.RoundToInt(session.Data.Setting("buildingLabY", -1))
            );
            var bench = new Vector2Int(
                Mathf.RoundToInt(session.Data.Setting("buildingBenchX", -1)),
                Mathf.RoundToInt(session.Data.Setting("buildingBenchY", -1))
            );
            foreach (var c in new[] { lab, lab + Vector2Int.one, bench, bench + Vector2Int.one })
            {
                Assert.That(session.IsBuildable(c), Is.False, "building cell " + c);
            }

            Assert.That(
                g.GetType().GetMethod("Place").Invoke(g, new object[] { TowerKind.Single, lab }),
                Is.EqualTo(false)
            );
            Assert.That(GameObject.Find("Building_TurretLab"), Is.Not.Null);
            Assert.That(GameObject.Find("Building_EngineerBench"), Is.Not.Null);
            Call(g, "HandleWorldClick", lab + Vector2Int.one);
            Assert.That(Active(g, "TowerLabPanel"), Is.True);
            Assert.That(Active(g, "PlayerGrowthPanel"), Is.False);
            Call(g, "HandleWorldClick", bench);
            Assert.That(Active(g, "PlayerGrowthPanel"), Is.True);
            Assert.That(Active(g, "TowerLabPanel"), Is.False, "Only one big panel");
            Call(g, "HandleWorldClick", bench);
            Assert.That(Active(g, "PlayerGrowthPanel"), Is.False, "Second click closes");
        }
        finally
        {
            Time.timeScale = previous;
        }
    }

    [UnityTest]
    public IEnumerator EnemyClickShowsEnemyInfo()
    {
        yield return Load();
        var g = Game();
        float previous = Time.timeScale;
        try
        {
            Time.timeScale = 0;
            var session = (Session)g.GetType().GetProperty("Session").GetValue(g);
            session.StartCombat();
            var e = session.DebugSpawn(EnemyKind.Attack, 3);
            yield return null;
            Call(g, "HandleWorldClick", Vector2Int.RoundToInt(e.Position));
            Assert.That(Active(g, "SelectionInfo"), Is.True);
            var title = g.transform.Find("CombatHUD/SelectionInfo/Title").GetComponent<Text>().text;
            Assert.That(title, Does.Contain("공격형"));
            var rows = g
                .transform.Find("CombatHUD/SelectionInfo")
                .GetComponentsInChildren<Text>()
                .Select(t => t.text)
                .ToArray();
            Assert.That(rows.Any(t => t.StartsWith("체력 ")), Is.True);
            Assert.That(rows.Any(t => t.StartsWith("약점")), Is.True);
            session.ApplyDamage(e, 100000, "QA");
            yield return null;
            var hud = Hud(g);
            hud.GetType().GetMethod("Refresh", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(hud, null);
            Assert.That(Active(g, "SelectionInfo"), Is.False, "Dead enemy closes the info panel");
        }
        finally
        {
            Time.timeScale = previous;
        }
    }

    [UnityTest]
    public IEnumerator SceneHasPersistentTunableOutline_OnlyForOpenInspection()
    {
        yield return Load();
        var g = Game();
        var type = Find("CoreGuardianSelectionOutline");
        Assert.That(type, Is.Not.Null, "Selection renderer is not implemented yet");
        var component = g.GetComponent(type);
        Assert.That(component, Is.Not.Null, "Settings must exist in Edit mode on the scene controller");
        Assert.That(type.GetField("color"), Is.Not.Null);
        Assert.That(type.GetField("widthPixels"), Is.Not.Null);
        HudCall(g, "OpenPlayerSpecs");
        yield return null;
        Assert.That(type.GetProperty("IsVisible").GetValue(component), Is.EqualTo(true));
        Click(g, "InfoClose");
        yield return null;
        Assert.That(type.GetProperty("IsVisible").GetValue(component), Is.EqualTo(false));
    }
}
