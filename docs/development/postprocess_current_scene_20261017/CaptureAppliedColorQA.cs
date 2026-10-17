using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public static class CaptureAppliedColorQA
{
    const string Dir = "docs/development/postprocess_current_scene_20261017/";
    static void Screen(string filename)
    {
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Unity.Pipeline.Editor.Commands.Capture.CaptureCommands")).First(t => t != null);
        var result = type.GetMethod("CaptureGameView", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] {1920,1080,null,null,false,0,"screen"});
        File.WriteAllBytes(Dir+filename, Convert.FromBase64String((string)result.GetType().GetProperty("Base64").GetValue(result)));
    }
    public static async Task<object> Main(string prefix = "applied", bool comparePrevious = false)
    {
        if (!EditorApplication.isPlaying) throw new Exception("Own visual QA Play required");
        UnityEngine.Object.FindObjectsByType<Button>().Single(b => b.name == "Reload").onClick.Invoke();
        await Task.Delay(500);
        var game = UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
        if (game == null || !game.Ready || !game.Place(CoreGuardian.TowerKind.Single,new Vector2Int(3,11))) throw new Exception("Normal purchase failed");
        UnityEngine.Object.FindObjectsByType<Button>().Single(b => b.name == "TowerFold").onClick.Invoke();
        UnityEngine.Object.FindObjectsByType<Button>().Single(b => b.name == "Start").onClick.Invoke();
        var deadline = DateTime.UtcNow.AddSeconds(12);
        while (!UnityEngine.Object.FindObjectsByType<TMPro.TextMeshProUGUI>().Any(t => t.name == "HitDamage"))
        {
            if (!EditorApplication.isPlaying || DateTime.UtcNow > deadline) throw new Exception("No natural hit captured");
            await Task.Delay(80);
        }
        var volume = UnityEngine.Object.FindAnyObjectByType<Volume>();
        float originalTime = Time.timeScale, originalWeight = volume.weight;
        var originalProfile = volume.sharedProfile;
        VolumeProfile previous = null;
        try
        {
            Time.timeScale = 0;
            await Task.Delay(200);
            Screen(prefix+"_hit_after.png");
            if (comparePrevious)
            {
                previous = UnityEngine.Object.Instantiate(originalProfile);
                previous.components = originalProfile.components.Select(c => UnityEngine.Object.Instantiate(c)).ToList();
                previous.TryGet<UnityEngine.Rendering.Universal.ColorAdjustments>(out var c);
                previous.TryGet<UnityEngine.Rendering.Universal.SplitToning>(out var s);
                c.contrast.Override(10);c.saturation.Override(6);c.postExposure.value=0;c.postExposure.overrideState=false;
                s.shadows.Override(new Color(.47f,.49f,.53f,1));s.highlights.Override(new Color(.53f,.51f,.47f,1));s.balance.Override(0);
                volume.sharedProfile=previous;
            }
            else volume.weight = 0;
            await Task.Delay(200);
            Screen(prefix+"_hit_before.png");
        }
        finally
        {
            if (volume != null) { volume.sharedProfile=originalProfile;volume.weight=originalWeight; }
            if(previous!=null){foreach(var c in previous.components)UnityEngine.Object.DestroyImmediate(c);UnityEngine.Object.DestroyImmediate(previous);}
            Time.timeScale=originalTime;
        }
        var result = new { source="Composited GameView including ScreenSpaceOverlay HUD", size="1920x1080",
            method="Existing HUD callbacks and normal crystal purchase, natural round 1 hit; timeScale temporarily 0 for matching frames; no damage/economy injection", comparePrevious,
            diagnostic=game.Session.Diagnostic, damage=game.Session.DamageTotals.ToArray(), restoredTimeScale=Time.timeScale, restoredWeight=volume.weight };
        File.WriteAllText(Dir+prefix+"_hit_qa.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
