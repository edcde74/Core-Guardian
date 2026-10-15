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

public class CoreGuardianPlacementHudPlayModeTests
{
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static Type TypeOf(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static Component Game()=> (Component)UnityEngine.Object.FindObjectsByType(TypeOf("CoreGuardianPrototype"),FindObjectsSortMode.None).Single();
    static Session Session(Component game)=>(Session)game.GetType().GetProperty("Session").GetValue(game);
    static Transform Hud(Component game)=>game.transform.Find("CombatHUD");
    static void Call(Component game,string method,params object[] args)=>game.GetType().GetMethod(method).Invoke(game,args);
    static void Click(Transform hud,string name)=>hud.GetComponentsInChildren<Button>(true).First(b=>b.name==name).onClick.Invoke();
    static bool Visible(Transform node)=>node.gameObject.activeInHierarchy&&node.GetComponentsInParent<CanvasGroup>(true).All(g=>g.alpha>.001f);
    static bool Installing(Component game)=>game.GetType().GetProperty("Installing").GetValue(game)!=null;
    static IEnumerator Load()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#endif
        yield return null;
    }
    static void Begin(Transform hud)
    {
        Click(hud,"Installation");Click(hud,"공격 포탑");Click(hud,"Install_Single");
    }
    [UnityTest]
    public IEnumerator ManualRoundStartClosesOpenInstallationPanel()
    {
        yield return Load();var game=Game();var hud=Hud(game);
        Click(hud,"Installation");Assert.That(Visible(hud.Find("TowerInstallPanel")),Is.True);
        Click(hud,"Start");
        Assert.That(Session(game).Phase,Is.EqualTo(SessionPhase.Combat));
        Assert.That(hud.Find("TowerInstallPanel").gameObject.activeSelf,Is.False,"Round start must close the chooser, not just cancel the ghost");
        Assert.That(Installing(game),Is.False);
    }
    [UnityTest]
    public IEnumerator PlacementHidesOtherHud_AndCancelRestoresPanelAndCanvasGroupStates()
    {
        yield return Load();var game=Game();var hud=Hud(game);float old=Time.timeScale;
        try
        {
            Time.timeScale=0;Click(hud,"PlayerGrowth");Click(hud,"DamageFold");Click(hud,"TopFold");
            var speed=hud.Find("TestSpeed").gameObject.AddComponent<CanvasGroup>();speed.alpha=.6f;speed.blocksRaycasts=false;
            Begin(hud);Assert.That(Installing(game),Is.True);
            foreach(Transform root in hud)
            {
                bool keep=new[]{"TopStatus","StagePlate","BossPlate","CancelPlacement"}.Contains(root.name);
                if(keep)continue;
                Assert.That(Visible(root),Is.False,"Placement must hide "+root.name);
                var group=root.GetComponent<CanvasGroup>();Assert.That(group,Is.Not.Null);Assert.That(group.blocksRaycasts,Is.False);
            }
            Assert.That(Visible(hud.Find("TopStatus")),Is.True);Assert.That(Visible(hud.Find("StagePlate")),Is.True);Assert.That(Visible(hud.Find("CancelPlacement")),Is.True);
            Assert.That(hud.Find("BottomControls/Start").GetComponent<Button>().IsInteractable(),Is.False);
            game.GetComponent(TypeOf("CoreGuardianHud")).GetType().GetMethod("Refresh",Private).Invoke(game.GetComponent(TypeOf("CoreGuardianHud")),null);
            Assert.That(Visible(hud.Find("PlayerGrowthPanel")),Is.False,"Periodic refresh must not unhide focused HUD");
            Click(hud,"CancelPlacement");
            Assert.That(Installing(game),Is.False);Assert.That(Visible(hud.Find("PlayerGrowthPanel")),Is.True);
            Assert.That(hud.Find("DamagePanel").gameObject.activeSelf,Is.False,"Previously folded damage panel must stay folded");
            Assert.That(hud.Find("TopStatus").GetComponent<RectTransform>().sizeDelta.y,Is.EqualTo(52));
            Assert.That(Visible(hud.Find("BottomControls")),Is.True);Assert.That(hud.Find("TowerInstallPanel").gameObject.activeSelf,Is.False);
            Assert.That(speed.alpha,Is.EqualTo(.6f));Assert.That(speed.blocksRaycasts,Is.False);Assert.That(speed.interactable,Is.True);
            Assert.That(Cursor.visible,Is.True);
        }
        finally{Time.timeScale=old;}
    }
    [UnityTest]
    public IEnumerator FailedPlacementStaysFocused_AndSuccessfulPlacementRestoresHud()
    {
        yield return Load();var game=Game();var hud=Hud(game);float old=Time.timeScale;
        try
        {
            Time.timeScale=0;Click(hud,"PlayerGrowth");Begin(hud);
            Assert.That(game.GetType().GetMethod("Place").Invoke(game,new object[]{TowerKind.Single,new Vector2Int(0,12)}),Is.EqualTo(false));
            Assert.That(Installing(game),Is.True);Assert.That(Visible(hud.Find("BottomControls")),Is.False);
            Assert.That(game.GetType().GetMethod("Place").Invoke(game,new object[]{TowerKind.Single,new Vector2Int(14,15)}),Is.EqualTo(true));
            Assert.That(Installing(game),Is.False);Assert.That(Visible(hud.Find("BottomControls")),Is.True);
            Assert.That(Visible(hud.Find("PlayerGrowthPanel")),Is.True);Assert.That(Visible(hud.Find("TowerInstallPanel")),Is.True,"The pre-placement chooser returns after a successful placement");
            Call(game,"StartCombat");Assert.That(hud.Find("TowerInstallPanel").gameObject.activeSelf,Is.False);
        }
        finally{Time.timeScale=old;}
    }
    [UnityTest]
    public IEnumerator PreparationTimeoutCancelsPlacementAndClosesChooser()
    {
        yield return Load();var game=Game();var hud=Hud(game);float old=Time.timeScale;
        try
        {
            Time.timeScale=0;Begin(hud);var session=Session(game);
            session.Tick(session.PreparationRemaining-.01f,new Vector2(15,10));
            Time.timeScale=1;yield return new WaitForSecondsRealtime(.2f);
            Assert.That(session.Phase,Is.EqualTo(SessionPhase.Combat));Assert.That(Installing(game),Is.False);
            Assert.That(hud.Find("TowerInstallPanel").gameObject.activeSelf,Is.False);
            Assert.That(Visible(hud.Find("BottomControls")),Is.True);Assert.That(Cursor.visible,Is.True);
            yield return null;
            Assert.That(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t=>t.name=="TowerPlacementPreview"||t.name=="InstallationTile"),Is.False);
        }
        finally{Time.timeScale=old;}
    }
}
