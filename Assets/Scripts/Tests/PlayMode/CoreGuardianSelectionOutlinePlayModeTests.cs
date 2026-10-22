using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CoreGuardian;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class CoreGuardianSelectionOutlinePlayModeTests
{
    static Type Find(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).FirstOrDefault(t=>t!=null);
    static Component Game()=>(Component)UnityEngine.Object.FindObjectsByType(Find("CoreGuardianPrototype"),FindObjectsSortMode.None).Single();
    static void Call(Component g,string name,params object[] args)=>g.GetType().GetMethod(name).Invoke(g,args);
    static int Selected(Component g)=>(int)g.GetType().GetProperty("SelectedTower").GetValue(g);
    static void Click(Component g,string name)=>g.transform.Find("CombatHUD").GetComponentsInChildren<Button>(true).Single(b=>b.name==name).onClick.Invoke();
    static IEnumerator Load()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#endif
        yield return null;
    }
    [UnityTest]
    public IEnumerator FoldingOrFloorClickClearsSelection_WithoutASecondClick()
    {
        yield return Load();var g=Game();float previous=Time.timeScale;
        try
        {
            Time.timeScale=0;Call(g,"Place",TowerKind.Single,new Vector2Int(14,15));Call(g,"HandleWorldClick",new Vector2Int(14,15));
            Assert.That(Selected(g),Is.GreaterThanOrEqualTo(0));Click(g,"InspectorFold");
            Assert.That(Selected(g),Is.EqualTo(-1),"Folding must release the selected tower");
            Assert.That(g.transform.Find("CombatHUD/InspectorSummary"),Is.Null,"The obsolete folded tower summary must not be created");
            Call(g,"HandleWorldClick",new Vector2Int(14,15));Call(g,"HandleWorldClick",new Vector2Int(13,14));
            Assert.That(Selected(g),Is.EqualTo(-1),"One empty floor click folds and deselects");
        }
        finally{Time.timeScale=previous;}
    }
    [UnityTest]
    public IEnumerator PlayerPanelReplacesTowerTarget_AndInstallationRemovesOutline()
    {
        yield return Load();var g=Game();float previous=Time.timeScale;
        try
        {
            Time.timeScale=0;Call(g,"Place",TowerKind.Single,new Vector2Int(14,15));Call(g,"HandleWorldClick",new Vector2Int(14,15));
            Click(g,"PlayerSpecs");Assert.That(Selected(g),Is.EqualTo(-1),"Player inspection cannot retain a competing tower selection");
            var hud=g.GetComponent(Find("CoreGuardianHud"));var target=hud.GetType().GetProperty("InspectionTarget");
            Assert.That(target,Is.Not.Null);Assert.That(target.GetValue(hud),Is.Not.Null);
            Click(g,"PlayerBuyTab");Assert.That(target.GetValue(hud),Is.Not.Null);
            Click(g,"InspectorFold");Assert.That(target.GetValue(hud),Is.Null);
            Click(g,"PlayerSpecs");Click(g,"PlayerSpecs");Assert.That(target.GetValue(hud),Is.Null,"Toggling the same player button also releases inspection");
            Click(g,"PlayerGrowth");Click(g,"Installation");Assert.That(target.GetValue(hud),Is.Null);
        }
        finally{Time.timeScale=previous;}
    }
    [UnityTest]
    public IEnumerator WorldPlayerClickOpensCurrentAbilities_AndRepeatedClickKeepsItOpen()
    {
        yield return Load();var g=Game();float previous=Time.timeScale;
        try
        {
            Time.timeScale=0;var move=(Component)g.GetType().GetField("player").GetValue(g);
            var cell=(Vector3Int)move.GetType().GetProperty("CurrentCell").GetValue(move);var xy=new Vector2Int(cell.x,cell.y);
            Call(g,"Place",TowerKind.Single,new Vector2Int(14,15));Call(g,"HandleWorldClick",new Vector2Int(14,15));
            Call(g,"HandleWorldClick",xy);
            var root=g.transform.Find("CombatHUD");
            Assert.That(root.Find("PlayerGrowthPanel").gameObject.activeSelf,Is.True,"World player click opens current abilities");
            Assert.That(root.Find("TowerInformation").gameObject.activeSelf,Is.False);Assert.That(Selected(g),Is.EqualTo(-1));
            Assert.That(root.Find("PlayerGrowthPanel/damage").gameObject.activeSelf,Is.False,"Opening abilities does not open purchase controls");
            Call(g,"HandleWorldClick",xy);Assert.That(root.Find("PlayerGrowthPanel").gameObject.activeSelf,Is.True,"Repeated world selection must not toggle closed");
            Click(g,"PlayerBuyTab");Call(g,"HandleWorldClick",xy);
            Assert.That(root.Find("PlayerGrowthPanel/damage").gameObject.activeSelf,Is.False,"World click returns to the current ability tab");
            yield return null;
            var outline=g.GetComponent(Find("CoreGuardianSelectionOutline"));Assert.That(outline.GetType().GetProperty("CurrentTarget").GetValue(outline),Is.EqualTo(move.transform));
            Click(g,"InspectorFold");yield return null;Assert.That(outline.GetType().GetProperty("IsVisible").GetValue(outline),Is.EqualTo(false));
        }
        finally{Time.timeScale=previous;}
    }
    [UnityTest]
    public IEnumerator SceneHasPersistentTunableOutline_OnlyForOpenInspection()
    {
        yield return Load();var g=Game();var type=Find("CoreGuardianSelectionOutline");
        Assert.That(type,Is.Not.Null,"Selection renderer is not implemented yet");
        var component=g.GetComponent(type);Assert.That(component,Is.Not.Null,"Settings must exist in Edit mode on the scene controller");
        Assert.That(type.GetField("color"),Is.Not.Null);Assert.That(type.GetField("widthPixels"),Is.Not.Null);
        Click(g,"PlayerSpecs");yield return null;
        Assert.That(type.GetProperty("IsVisible").GetValue(component),Is.EqualTo(true));
        Click(g,"InspectorFold");yield return null;
        Assert.That(type.GetProperty("IsVisible").GetValue(component),Is.EqualTo(false));
    }
}
