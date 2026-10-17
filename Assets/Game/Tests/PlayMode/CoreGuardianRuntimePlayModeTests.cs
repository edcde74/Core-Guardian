using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class CoreGuardianRuntimePlayModeTests
{
    static UnityEngine.UI.Button HudButton(UnityEngine.Object runtime,string name)=>((Component)runtime).GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b=>b.name==name);
    [UnityTest]
    public IEnumerator ActualScene_ElevenRoundsKeepTowerAndHud_ThenRestartResetsSession()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianPrototype")).First(t=>t!=null);
        var runtime=UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();
        var s=(CoreGuardian.Session)type.GetProperty("Session").GetValue(runtime);
        Assert.That(type.GetMethod("Place").Invoke(runtime,new object[]{CoreGuardian.TowerKind.Single,new Vector2Int(14,15)}),Is.EqualTo(true));
        var tower=s.Towers.Single();float previousTimeScale=Time.timeScale;
        Time.timeScale=0;
        try
        {
            for(int wave=1;wave<=11;wave++)
            {
                type.GetMethod("StartCombat").Invoke(runtime,null);
                for(int i=0;i<1000&&s.Phase==CoreGuardian.SessionPhase.Combat;i++)
                {
                    s.Tick(.1f,new Vector2(100,100));
                    foreach(var enemy in s.Enemies.ToArray())s.ApplyDamage(enemy,10000,"ControlledIntegrationCheck");
                }
                Assert.That(s.CompletedWave,Is.EqualTo(wave));
                Assert.That(s.Towers.Single(),Is.SameAs(tower));
                type.GetMethod("Notify").Invoke(runtime,new object[]{s.LastMessage});
                yield return null;
                Assert.That(GameObject.Find("Tower_Single_1"),Is.Not.Null);
                Assert.That(GameObject.Find("Status").GetComponent<UnityEngine.UI.Text>().text,Does.Contain("라운드 "+s.Wave+" / 11"));
                Assert.That(s.Phase,Is.EqualTo(wave==11?CoreGuardian.SessionPhase.Victory:wave==10?CoreGuardian.SessionPhase.Augmentation:CoreGuardian.SessionPhase.Preparation));
                if(wave==10){Assert.That(GameObject.Find("AugmentationSelection"),Is.Not.Null);Assert.That(s.AugmentationChoices.Count,Is.EqualTo(3));Assert.That(GameObject.Find("ChoiceTimer").GetComponent<UnityEngine.UI.Text>().text,Does.Contain("30초"));Assert.That(HudButton(runtime,"RoleDiagnostic").interactable,Is.False);Assert.That(HudButton(runtime,"Reload").interactable,Is.False);int gold=s.Gold,crystals=s.Crystals;var card=s.AugmentationChoices[0];var cardButton=GameObject.Find("AugmentationCard0").GetComponent<UnityEngine.UI.Button>();Assert.That(cardButton.GetComponentsInChildren<UnityEngine.UI.Text>().Length,Is.Zero,"Card back must not reveal title, effect or tier text");cardButton.onClick.Invoke();Assert.That(s.Phase,Is.EqualTo(CoreGuardian.SessionPhase.Augmentation),"First click only reveals");yield return new WaitForSecondsRealtime(1.05f);Assert.That(s.Phase,Is.EqualTo(CoreGuardian.SessionPhase.Augmentation));cardButton.onClick.Invoke();Assert.That(s.Phase,Is.EqualTo(CoreGuardian.SessionPhase.Preparation));if(card.Family==CoreGuardian.AugmentationFamily.Gold)Assert.That(s.Gold-gold,Is.EqualTo(card.Value(100,300,600)));if(card.Family==CoreGuardian.AugmentationFamily.Crystals)Assert.That(s.Crystals-crystals,Is.EqualTo(card.Value(80,240,480)));Assert.That(s.PreparationRemaining,Is.EqualTo(50));Assert.That(HudButton(runtime,"Reload").interactable,Is.True);}
            }
            Assert.That(s.Gold,Is.GreaterThanOrEqualTo(752));Assert.That(s.Crystals,Is.GreaterThanOrEqualTo(270));Assert.That(s.CoreEnergy,Is.GreaterThanOrEqualTo(20));
            Assert.That(GameObject.Find("Preparation").GetComponent<UnityEngine.UI.Text>().text,Does.Contain("1~11라운드 검증 완료"));
            type.GetMethod("Restart").Invoke(runtime,null);
            yield return null;yield return null;
            runtime=UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();
            s=(CoreGuardian.Session)type.GetProperty("Session").GetValue(runtime);
            Assert.That(s.Wave,Is.EqualTo(1));Assert.That(s.Gold,Is.Zero);Assert.That(s.Crystals,Is.EqualTo(100));
            Assert.That(s.CoreEnergy,Is.EqualTo(20));Assert.That(s.Towers,Is.Empty);
            Assert.That(s.Phase,Is.EqualTo(CoreGuardian.SessionPhase.Preparation));
        }
        finally {Time.timeScale=previousTimeScale;}
#else
        yield return null;
#endif
    }
    [UnityTest]
    public IEnumerator ActualScene_WaveOneCombatAwardsGoldAndKeepsPrefabFeetAnchored()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity", new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var runtimeType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianPrototype")).First(t=>t!=null);
        var runtime=UnityEngine.Object.FindObjectsByType(runtimeType,FindObjectsSortMode.None).Single();
        var session=(CoreGuardian.Session)runtimeType.GetProperty("Session").GetValue(runtime);
        Assert.That(runtimeType.GetMethod("Place").Invoke(runtime,new object[]{CoreGuardian.TowerKind.Single,new Vector2Int(14,15)}),Is.EqualTo(true));
        runtimeType.GetMethod("StartCombat").Invoke(runtime,null);
        for(int i=0;i<1000&&session.Phase==CoreGuardian.SessionPhase.Combat;i++)session.Tick(.1f,new Vector2(15,10));
        yield return null;
        Assert.That(session.Phase,Is.EqualTo(CoreGuardian.SessionPhase.Preparation));
        Assert.That(session.Wave,Is.EqualTo(2));
        Assert.That(session.PreparationRemaining,Is.InRange(49f,50f));
        Assert.That(session.Gold,Is.EqualTo(45));
        Assert.That(session.Crystals,Is.EqualTo(20));
        Assert.That(session.CoreEnergy,Is.EqualTo(20));
        Assert.That(session.DamageTotals.Values.Sum(),Is.EqualTo(session.Data.Waves.Where(w=>w.Wave==1).Sum(w=>w.Count*session.Data.Enemies[w.Kind].Health)));
        Assert.That(GameObject.Find("Tower_Single_1").transform.position.x,Is.EqualTo(8.7f).Within(.001f));
        Assert.That(UnityEngine.Object.FindObjectsByType<Canvas>().Length,Is.EqualTo(1));
#else
        yield return null;
#endif
    }

    [UnityTest]
    public IEnumerator PlayerCellTower_ManualDeparture_AndReentryBlocked()
    {
        var runtimeType = AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianPrototype")).FirstOrDefault(t=>t!=null);
        Assert.That(runtimeType, Is.Not.Null, "Gameplay runtime bridge has not been implemented");
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity", new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var runtime = UnityEngine.Object.FindObjectsByType(runtimeType, FindObjectsSortMode.None).Single();
        var session = runtimeType.GetProperty("Session").GetValue(runtime);
        Assert.That(session, Is.Not.Null);
        Assert.That(runtimeType.GetMethod("PlaceAtPlayerForTest").Invoke(runtime, null), Is.EqualTo(true));
        var movementType = AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("PrototypeGridMovement")).First(t=>t!=null);
        var movement = UnityEngine.Object.FindObjectsByType(movementType, FindObjectsSortMode.None).Single();
        var start = ((Component)movement).transform.position;
        Assert.That(movementType.GetMethod("TryStep").Invoke(movement,new object[]{Vector2Int.right}), Is.EqualTo(true));
        yield return new WaitForSeconds(.16f);
        Assert.That(((Component)movement).transform.position.x, Is.EqualTo(start.x+.6f).Within(.001f));
        Assert.That(movementType.GetMethod("TryStep").Invoke(movement,new object[]{Vector2Int.left}), Is.EqualTo(false));
#else
        yield return null;
#endif
    }
}
