using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
public static class CaptureHud
{
    public static async Task<object> Main(string name)
    {
        if(!EditorApplication.isPlaying)throw new Exception("Owned Play required");
        var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.Focus();view.Repaint();
        string path=Path.GetFullPath("docs/development/damage_hud_applied_20261017/"+name+".png");
        await Task.Delay(300);ScreenCapture.CaptureScreenshot(path);await Task.Delay(800);
        if(!File.Exists(path))throw new Exception("Capture pending: "+path);
        return new{path,method="ScreenCapture.CaptureScreenshot after GameView focus; overlay HUD included; injected UI display totals"};
    }
}
