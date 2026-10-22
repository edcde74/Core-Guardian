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
    static Vector3[] Corners(Transform t){var v=new Vector3[4];((RectTransform)t).GetWorldCorners(v);return v;}
    [UnityTest,Category("ThirtyExperiment")]
    public IEnumerator ThirtyRoundActualSceneLoadsStatsShowsFiveAugmentationsAndEndsAtThirty()
    {
#if UNITY_EDITOR
        float previous=Time.timeScale;
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianPrototype")).First(t=>t!=null);
        var game=(MonoBehaviour)UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();
        var s=(CoreGuardian.Session)type.GetProperty("Session").GetValue(game);int choices=0;
        try
        {
            game.enabled=false;Time.timeScale=0;Assert.That(s.LastWave,Is.EqualTo(30));
            Assert.That(type.GetMethod("Place").Invoke(game,new object[]{CoreGuardian.TowerKind.Multi,new Vector2Int(12,13)}),Is.EqualTo(true));
            Assert.That(s.Crystals,Is.EqualTo(40));
            for(int wave=1;wave<=30;wave++)
            {
                type.GetMethod("StartCombat").Invoke(game,null);bool bossSeen=false;
                for(int tick=0;tick<1600&&s.Phase==CoreGuardian.SessionPhase.Combat;tick++)
                {
                    s.Tick(.05f,new Vector2(100,100));
                    var boss=s.Enemies.FirstOrDefault(e=>e.Kind==CoreGuardian.EnemyKind.Boss);
                    if(boss.IsValid&&!bossSeen)
                    {
                        bossSeen=true;Assert.That(boss.Spec.Health,Is.EqualTo(wave==10?500:wave==20?1600:4500));
                        type.GetMethod("SynchronizeViews",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(game,null);
                        type.GetMethod("Notify").Invoke(game,new object[]{s.LastMessage});
                        Assert.That(game.GetComponentsInChildren<UnityEngine.UI.Text>(true).Any(t=>t.text.Contains(" / "+boss.Spec.Health)),Is.True,"Boss HUD must use its current wave maximum");
                    }
                    // Injected functional progression QA; these times and kills are not balance results.
                    foreach(var enemy in s.Enemies.ToArray())s.ApplyDamage(enemy,100000,"ThirtyRoundSceneQA");
                }
                Assert.That(s.CompletedWave,Is.EqualTo(wave));Assert.That(bossSeen,Is.EqualTo(wave%10==0));
                type.GetMethod("Notify").Invoke(game,new object[]{s.LastMessage});yield return null;
                Assert.That(game.GetComponentsInChildren<UnityEngine.UI.Text>(true).Single(t=>t.name=="Status_Wave").text,Does.Contain("/30"));
                if(s.Phase==CoreGuardian.SessionPhase.Augmentation){choices++;Assert.That(game.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="AugmentationSelection").gameObject.activeSelf,Is.True);Assert.That(s.ChooseAugmentation(0),Is.True);}
            }
            Assert.That(choices,Is.EqualTo(5));Assert.That(s.Phase,Is.EqualTo(CoreGuardian.SessionPhase.Victory));Assert.That(s.AugmentationChoices,Is.Empty);
        }
        finally{Time.timeScale=previous;}
#else
        yield return null;
#endif
    }
    [UnityTest,Category("SharedTowersHud"),Category("ThirtyExperiment")] public IEnumerator SharedUpgradeHudKeepsPanelTabsAndStatCellsFixed()
    {
#if UNITY_EDITOR
        float previous=Time.timeScale;
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianPrototype")).First(t=>t!=null);
        var runtime=(Component)UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();
        var hud=runtime.GetComponent(AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianHud")).First(t=>t!=null));
        var s=(CoreGuardian.Session)type.GetProperty("Session").GetValue(runtime);
        try
        {
            Assert.That(UnityEngine.EventSystems.EventSystem.current.sendNavigationEvents,Is.False,"WASD movement must not navigate clicked HUD buttons");
            Time.timeScale=0;type.GetMethod("Diagnostic").Invoke(runtime,null);
            foreach(var kind in new[]{CoreGuardian.TowerKind.Single,CoreGuardian.TowerKind.Multi,CoreGuardian.TowerKind.Support})
                Assert.That(type.GetMethod("Place").Invoke(runtime,new object[]{kind,new Vector2Int(12+(int)kind,13)}),Is.EqualTo(true));
            for(int subject=0;subject<4;subject++)
            {
                bool player=subject==0,support=subject==3;string prefix=player?"Player":"Tower";
                if(player)hud.GetType().GetMethod("OpenPlayerSpecs").Invoke(hud,null);
                else{type.GetProperty("SelectedTower").SetValue(runtime,s.Towers[subject-1].Id);hud.GetType().GetMethod("OpenSelectedTowerSpecs").Invoke(hud,null);}
                yield return null;Canvas.ForceUpdateCanvases();
                var panel=HudButton(runtime,prefix+"SpecTab").transform.parent;var bounds=Corners(panel);
                var specTab=Corners(panel.Find(prefix+"SpecTab"));var buyTab=Corners(panel.Find(prefix+"BuyTab"));
                string[] stats=support?new[]{"buffDamage","range"}:new[]{"damage","rate","range","projectileSpeed"};
                var cells=stats.Select(k=>Corners(panel.Find("AbilityPlate/"+(k=="buffDamage"?"damage":k)))).ToArray();
                HudButton(runtime,prefix+"BuyTab").onClick.Invoke();yield return null;Canvas.ForceUpdateCanvases();
                Assert.That(Corners(panel),Is.EqualTo(bounds),prefix+" panel moved/resized");
                Assert.That(Corners(panel.Find(prefix+"SpecTab")),Is.EqualTo(specTab));Assert.That(Corners(panel.Find(prefix+"BuyTab")),Is.EqualTo(buyTab));
                for(int i=0;i<stats.Length;i++)Assert.That(Corners(panel.Find((player?"":"Tower_")+stats[i])),Is.EqualTo(cells[i]),stats[i]+" moved on tab switch");
                var purchase=HudButton(runtime,(player?"":"Tower_")+stats[0]);Assert.That(purchase.interactable,Is.True);purchase.onClick.Invoke();yield return null;
                Assert.That(player?s.Player.UpgradeSteps[stats[0]]:s.Towers[subject-1].Stats.UpgradeSteps[stats[0]],Is.EqualTo(1));
                HudButton(runtime,prefix+"SpecTab").onClick.Invoke();yield return null;Assert.That(Corners(panel),Is.EqualTo(bounds));
            }
        }
        finally{Time.timeScale=previous;}
#else
        yield return null;
#endif
    }
    [UnityTest,Category("ThirtyExperiment")]
    public IEnumerator CombatOnlySpeedRemembersSelectionButUsesOneInPreparationAugmentationAndResults()
    {
#if UNITY_EDITOR
        float previous=Time.timeScale;
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianPrototype")).First(t=>t!=null);
        var runtime=(Component)UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();
        var s=(CoreGuardian.Session)type.GetProperty("Session").GetValue(runtime);
        try
        {
            type.GetMethod("CycleTestSpeed").Invoke(runtime,null);
            Assert.That((int)type.GetProperty("TestSpeed").GetValue(runtime),Is.EqualTo(2));
            Assert.That(Time.timeScale,Is.EqualTo(1),"Selecting combat speed must not accelerate preparation");
            for(int wave=1;wave<=5;wave++)
            {
                type.GetMethod("StartCombat").Invoke(runtime,null);Assert.That(Time.timeScale,Is.EqualTo(2));
                // Injected functional phase QA, not difficulty measurement.
                for(int i=0;i<1000&&s.Phase==CoreGuardian.SessionPhase.Combat;i++)
                {s.Tick(.05f,new Vector2(100,100));foreach(var e in s.Enemies.ToArray())s.ApplyDamage(e,10000,"SpeedPhaseQA");}
                yield return null;Assert.That(Time.timeScale,Is.EqualTo(1));
            }
            Assert.That(s.Phase,Is.EqualTo(CoreGuardian.SessionPhase.Augmentation));
            type.GetMethod("CycleTestSpeed").Invoke(runtime,null);Assert.That(Time.timeScale,Is.EqualTo(1));
            float remaining=s.AugmentationRemaining;yield return new WaitForSecondsRealtime(.3f);
            Assert.That(remaining-s.AugmentationRemaining,Is.InRange(.2f,.55f),"Augmentation timer must use normal real-time pacing");
            Assert.That(s.ChooseAugmentation(0),Is.True);yield return null;Assert.That(Time.timeScale,Is.EqualTo(1));
            for(int wave=6;wave<=s.LastWave;wave++)
            {
                if(s.Phase==CoreGuardian.SessionPhase.Augmentation)s.ChooseAugmentation(0);
                type.GetMethod("StartCombat").Invoke(runtime,null);Assert.That(Time.timeScale,Is.EqualTo(3));
                for(int i=0;i<1000&&s.Phase==CoreGuardian.SessionPhase.Combat;i++)
                {s.Tick(.05f,new Vector2(100,100));foreach(var e in s.Enemies.ToArray())s.ApplyDamage(e,10000,"SpeedPhaseQA");}
                yield return null;Assert.That(Time.timeScale,Is.EqualTo(1));
            }
            yield return null;Assert.That(s.Phase,Is.EqualTo(CoreGuardian.SessionPhase.Victory));Assert.That(Time.timeScale,Is.EqualTo(1));
        }
        finally{Time.timeScale=previous;}
#else
        yield return null;
#endif
    }
    [UnityTest,Category("ThirtyExperiment")]
    public IEnumerator CombatOnlySpeedDefeatResetsAndDefaultExternalPauseIsPreserved()
    {
#if UNITY_EDITOR
        float previous=Time.timeScale;
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianPrototype")).First(t=>t!=null);
        var runtime=(Component)UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();
        var s=(CoreGuardian.Session)type.GetProperty("Session").GetValue(runtime);
        try
        {
            Time.timeScale=0;yield return null;Assert.That(Time.timeScale,Is.Zero,"The default speed must not override an external QA pause");
            type.GetMethod("CycleTestSpeed").Invoke(runtime,null);Assert.That(Time.timeScale,Is.EqualTo(1));
            type.GetMethod("StartCombat").Invoke(runtime,null);Assert.That(Time.timeScale,Is.EqualTo(2));
            // Injected terminal-state QA, never a balance result.
            s.DebugSpawn(CoreGuardian.EnemyKind.Boss,1000);s.DebugSpawn(CoreGuardian.EnemyKind.Boss,1000);s.Tick(.05f,new Vector2(100,100));
            Assert.That(s.Phase,Is.EqualTo(CoreGuardian.SessionPhase.Defeat));yield return null;Assert.That(Time.timeScale,Is.EqualTo(1));
        }
        finally{Time.timeScale=previous;}
#else
        yield return null;
#endif
    }
    [UnityTest,Category("ThirtyExperiment")]
    public IEnumerator ActualScene_ThirtyRoundsKeepTowerAndHud_ThenRestartResetsSession()
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
            for(int wave=1;wave<=s.LastWave;wave++)
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
                Assert.That(GameObject.Find("Status").GetComponent<UnityEngine.UI.Text>().text,Does.Contain("라운드 "+s.Wave+" / 30"));
                Assert.That(s.Phase,Is.EqualTo(wave==30?CoreGuardian.SessionPhase.Victory:wave%5==0?CoreGuardian.SessionPhase.Augmentation:CoreGuardian.SessionPhase.Preparation));
                if(wave%5==0&&wave<s.LastWave){Assert.That(GameObject.Find("AugmentationSelection"),Is.Not.Null);Assert.That(s.AugmentationChoices.Count,Is.EqualTo(3));Assert.That(GameObject.Find("ChoiceTimer").GetComponent<UnityEngine.UI.Text>().text,Does.Contain("30초"));Assert.That(HudButton(runtime,"RoleDiagnostic").interactable,Is.False);Assert.That(HudButton(runtime,"Reload").interactable,Is.False);int gold=s.Gold,crystals=s.Crystals;var card=s.AugmentationChoices[0];var cardButton=GameObject.Find("AugmentationCard0").GetComponent<UnityEngine.UI.Button>();Assert.That(cardButton.GetComponentsInChildren<UnityEngine.UI.Text>().Length,Is.Zero,"Card back must not reveal title, effect or tier text");cardButton.onClick.Invoke();Assert.That(s.Phase,Is.EqualTo(CoreGuardian.SessionPhase.Augmentation),"First click only reveals");yield return new WaitForSecondsRealtime(1.05f);Assert.That(s.Phase,Is.EqualTo(CoreGuardian.SessionPhase.Augmentation));cardButton.onClick.Invoke();Assert.That(s.Phase,Is.EqualTo(CoreGuardian.SessionPhase.Preparation));if(card.Family==CoreGuardian.AugmentationFamily.Gold)Assert.That(s.Gold-gold,Is.EqualTo(card.Value(100,300,600)));if(card.Family==CoreGuardian.AugmentationFamily.Crystals)Assert.That(s.Crystals-crystals,Is.EqualTo(card.Value(80,240,480)));Assert.That(s.PreparationRemaining,Is.EqualTo(s.Data.PreparationSeconds));Assert.That(HudButton(runtime,"Reload").interactable,Is.True);}
            }
            Assert.That(s.Gold,Is.GreaterThanOrEqualTo(752));Assert.That(s.Crystals,Is.GreaterThanOrEqualTo(270));Assert.That(s.CoreEnergy,Is.GreaterThanOrEqualTo(20));
            Assert.That(GameObject.Find("Preparation").GetComponent<UnityEngine.UI.Text>().text,Does.Contain("1~30라운드 완료"));
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
        Assert.That(session.PreparationRemaining,Is.InRange(session.Data.PreparationSeconds-1,session.Data.PreparationSeconds));
        Assert.That(session.Gold,Is.EqualTo(45));
        Assert.That(session.Crystals,Is.EqualTo(session.Data.StartCrystals-session.Data.Towers[CoreGuardian.TowerKind.Single].Cost+session.Data.RewardForWave(1)));
        Assert.That(session.CoreEnergy,Is.EqualTo(20));
        Assert.That(session.DamageTotals.Values.Sum(),Is.EqualTo(session.Data.Waves.Where(w=>w.Wave==1).Sum(w=>w.Count*session.Data.Enemies[w.Kind].Health)));
        var expectedTowerPosition=(Vector3)runtimeType.GetMethod("World").Invoke(runtime,new object[]{new Vector2(14,15)});
        Assert.That(Vector3.Distance(GameObject.Find("Tower_Single_1").transform.position,expectedTowerPosition),Is.LessThan(.001f));
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
        var ground=(UnityEngine.Tilemaps.Tilemap)runtimeType.GetField("ground").GetValue(runtime);
        var startCell=(Vector3Int)movementType.GetProperty("CurrentCell").GetValue(movement);
        float expectedX=start.x+ground.GetCellCenterWorld(startCell+Vector3Int.right).x-ground.GetCellCenterWorld(startCell).x;
        Assert.That(movementType.GetMethod("TryStep").Invoke(movement,new object[]{Vector2Int.right}), Is.EqualTo(true));
        yield return new WaitForSeconds(.16f);
        Assert.That(((Component)movement).transform.position.x, Is.EqualTo(expectedX).Within(.001f));
        Assert.That(movementType.GetMethod("TryStep").Invoke(movement,new object[]{Vector2Int.left}), Is.EqualTo(false));
#else
        yield return null;
#endif
    }
}
