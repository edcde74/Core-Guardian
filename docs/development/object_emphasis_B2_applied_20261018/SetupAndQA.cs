using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SetupAndQA
{
    const string Dir="docs/development/object_emphasis_B2_applied_20261018/";
    public static object Setup()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed||scene.isDirty)throw new Exception("Saved stopped scene required");
        var go=GameObject.Find("CoreGuardian_Gameplay");if(go==null||go.GetComponent<CoreGuardianObjectEmphasis>()!=null)throw new Exception("Unexpected scene controller/settings");
        var component=Undo.AddComponent<CoreGuardianObjectEmphasis>(go);
        component.outlineWidthPixels=1;component.outlineColor=new Color(.06f,.065f,.075f,.90f);component.bodyBrightness=1.15f;component.alphaThreshold=.1f;
        EditorUtility.SetDirty(component);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        var result=new{component=component.GetType().Name,gameObject=go.name,width=component.outlineWidthPixels,color=new[]{component.outlineColor.r,component.outlineColor.g,component.outlineColor.b,component.outlineColor.a},brightness=component.bodyBrightness,sceneDirty=scene.isDirty};
        File.WriteAllText(Dir+"setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
    static void Screen(string name)
    {
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Pipeline.Editor.Commands.Capture.CaptureCommands")).First(t=>t!=null);
        var capture=type.GetMethod("CaptureGameView",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{1920,1080,null,null,false,0,"screen"});
        File.WriteAllBytes(Dir+name+".png",Convert.FromBase64String((string)capture.GetType().GetProperty("Base64").GetValue(capture)));
    }
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static async Task Drive(System.Collections.IEnumerator iterator)
    {
        try {while(iterator.MoveNext()){
            if(iterator.Current is System.Collections.IEnumerator nested)await Drive(nested);
            else if(iterator.Current is WaitForSeconds wait){var f=typeof(WaitForSeconds).GetField("m_Seconds",BindingFlags.Instance|BindingFlags.NonPublic);await Task.Delay(Mathf.CeilToInt((float)f.GetValue(wait)*1000)+80);}
            else await Task.Delay(80);
        }} finally {(iterator as IDisposable)?.Dispose();}
    }
    public static async Task<object> BaselineDiagnostics()
    {
        Require(EditorApplication.isPlaying,"Own baseline Play required");
        var names=new[]{"CoreGuardianAnimationPlayModeTests.ActualLaunch_PreservesOriginalPlayerSprite_AndDoesNotUseRejectedMuzzle","CoreGuardianHitFlashPlayModeTests.RealProjectileDamageFlashesTheSurvivingMonster_WithoutChangingExistingFeedback","CoreGuardianPlacementHudPlayModeTests.FailedPlacementStaysFocused_AndSuccessfulPlacementRestoresHud","CoreGuardianPlayerFacingPlayModeTests.NormalGridStepsDriveOriginalLegs_AndActualLaunchStartsAtTheAimedTool","CoreGuardianRuntimePlayModeTests.ActualScene_WaveOneCombatAwardsGoldAndKeepsPrefabFeetAnchored","CoreGuardianRuntimePlayModeTests.PlayerCellTower_ManualDeparture_AndReentryBlocked"};
        var results=new List<object>();int failed=0;
        foreach(var name in names){var i=name.LastIndexOf('.');var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name.Substring(0,i))).First(t=>t!=null);
            Require(!UnityEngine.Object.FindAnyObjectByType<CoreGuardianObjectEmphasis>().enabled,"Feature must remain disabled for baseline");
            try {await Drive((System.Collections.IEnumerator)type.GetMethod(name.Substring(i+1)).Invoke(Activator.CreateInstance(type),null));results.Add(new{name,failed=false});}
            catch(Exception e){failed++;results.Add(new{name,failed=true,message=e.GetBaseException().Message});}
        }
        var report=new{manualCoroutineDiagnostics=true,notTestRunnerResult=true,featureEnabled=false,total=names.Length,failed,results};File.WriteAllText(Dir+"baseline_diagnostics.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
    }
    public static async Task<object> Combat()
    {
        Require(EditorApplication.isPlaying&&!EditorApplication.isPaused,"Own fresh running Play required");var game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();var settings=game.GetComponent<CoreGuardianObjectEmphasis>();
        Require(game.Ready&&settings!=null&&!game.Session.Diagnostic&&game.Session.Towers.Count==0,"Normal fresh session/settings required");
        var time=Time.timeScale;var captureDelta=Time.captureDeltaTime;var width=settings.outlineWidthPixels;var color=settings.outlineColor;var brightness=settings.bodyBrightness;
        var camera=EditorJsonUtility.ToJson(Camera.main);var selection=game.GetComponent<CoreGuardianSelectionOutline>();var selectionBefore=EditorJsonUtility.ToJson(selection);
        try {
            Require(game.Place(CoreGuardian.TowerKind.Multi,new Vector2Int(4,8)),"Normal 100 purchase failed");game.StartCombat();
            var deadline=DateTime.UtcNow.AddSeconds(18);while(game.Session.Enemies.Count(e=>e.Alive)<4&&DateTime.UtcNow<deadline)await Task.Delay(70);
            Require(game.Session.Enemies.Count(e=>e.Alive)>=2,"Natural enemies missing");Time.timeScale=0;await Task.Delay(160);
            var enemies=game.Session.Enemies.Count(e=>e.Alive);Require(settings.ActiveTargetCount==enemies+3,"Not all actual bodies registered");
            Screen("applied_combat");
            var playerRenderer=game.player.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Body"&&r.enabled&&r.gameObject.activeInHierarchy);
            // Async continuations can run after animation Update but before emphasis LateUpdate.
            // Compare disabling against the untouched original source tint, not that intermediate frame tint.
            var originalTint=game.player.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite").color;
            settings.enabled=false;await Task.Delay(160);
            File.WriteAllText(Dir+"disable_check.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{original=new[]{originalTint.r,originalTint.g,originalTint.b,originalTint.a},restored=new[]{playerRenderer.color.r,playerRenderer.color.g,playerRenderer.color.b,playerRenderer.color.a},brightness,settings.enabled,settings.ActiveTargetCount},Newtonsoft.Json.Formatting.Indented));
            Require(playerRenderer.color==originalTint,"Disabling did not restore original body tint");Screen("before_combat");
            settings.enabled=true;await Task.Delay(160);Require(settings.ActiveTargetCount==enemies+3,"Enable failed to rebuild");
            settings.outlineWidthPixels=3;settings.outlineColor=new Color(.65f,.15f,.65f,.85f);await Task.Delay(160);Screen("inspector_width3_color");
            var output=settings.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();
            Require(output.Length==enemies+3&&output.All(r=>r.sharedMaterial.GetInt("_Width")==3&&r.sharedMaterial.GetColor("_Color")==settings.outlineColor),"Live width/color did not apply to every body");
            settings.outlineWidthPixels=0;await Task.Delay(100);Require(settings.GetComponentsInChildren<MeshRenderer>().All(r=>!r.enabled),"Width0 did not hide only emphasis outline");
            settings.outlineWidthPixels=width;settings.outlineColor=color;
            // Open the actual player inspection panel through its normal HUD callback.
            var button=game.transform.Find("CombatHUD").GetComponentsInChildren<Button>(true).Single(b=>b.name=="PlayerSpecs");button.onClick.Invoke();await Task.Delay(160);
            Require(selection.IsVisible&&selection.CurrentTarget==game.player.transform,"Existing selection outline failed");
            var selectedOutput=selection.GetComponentsInChildren<MeshRenderer>().Single(r=>r.enabled&&r.name=="SelectionOutline"&&r.transform.parent==selection.transform);
            var playerBody=settings.transform.Find("ObjectEmphasis_B2_"+game.player.name);Require(playerBody!=null,"Player emphasis missing");var normalOutput=playerBody.GetComponentInChildren<MeshRenderer>();
            Require(selectedOutput.sortingOrder>normalOutput.sortingOrder,"Selection not above emphasis");Screen("selection_above_emphasis");
            game.transform.Find("CombatHUD").GetComponentsInChildren<Button>(true).Single(b=>b.name=="InspectorFold").onClick.Invoke();await Task.Delay(100);
            Require(!selection.IsVisible,"Folding no longer clears selection");
            Require(camera==EditorJsonUtility.ToJson(Camera.main),"Camera changed");
            Require(selectionBefore==EditorJsonUtility.ToJson(selection),"Selection settings changed");
            var result=new{normalPurchase=100,liveEnemies=enemies,targets=settings.ActiveTargetCount,liveWidthColorAllBodies=true,widthZeroHidesOutline=true,disableEnableRestores=true,selectionRendersAbove=true,foldClearsSelection=true,cameraUnchanged=true,diagnostic=game.Session.Diagnostic,temporaryTimeScale=0,method="Own actual-scene API purchase/start, natural spawns, actual HUD selection callback. No human input, balance measurement, injection, or performance claim."};
            File.WriteAllText(Dir+"runtime_qa.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        } finally {settings.outlineWidthPixels=width;settings.outlineColor=color;settings.bodyBrightness=brightness;settings.enabled=true;Time.timeScale=time;Time.captureDeltaTime=captureDelta;}
    }
}
