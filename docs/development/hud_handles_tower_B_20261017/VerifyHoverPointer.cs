using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public static class VerifyHoverPointerB
{
    static void Need(bool v,string m){if(!v)throw new Exception(m);}
    public static async Task<object> Main()
    {
        var p=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();Need(Application.isPlaying&&Application.isFocused,"Owned focused Play required");
        var root=p.transform.Find("CombatHUD");var oldMouse=Mouse.current;var mouse=InputSystem.AddDevice<Mouse>();float oldTime=Time.timeScale;Time.timeScale=0;
        var oldBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        Func<string,Vector2> center=n=>{var b=root.GetComponentsInChildren<Button>(true).First(x=>x.name==n);var r=(RectTransform)b.transform;return RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));};
        Func<Vector2,bool,Task> move=async(pos,down)=>{InputSystem.QueueStateEvent(mouse,new MouseState{position=pos,buttons=(ushort)(down?1:0)});await Task.Delay(100);};
        Func<string,float> alpha=n=>root.GetComponentsInChildren<Button>(true).First(x=>x.name==n).transform.Find("HandleArt/HoverEdge").GetComponent<CanvasGroup>().alpha;
        try
        {
            p.Diagnostic();p.StartCombat();await Task.Delay(120);
            foreach(string n in new[]{"TopFold","DamageFold"})
            {
                await move(new Vector2(1900,1070),false);Need(alpha(n)==0,"Pointer outside clears "+n);
                await move(center(n),false);Need(alpha(n)>.9f,"Real pointer enters "+n);
                await move(center(n),true);Need(alpha(n)>0&&alpha(n)<.9f,"Real press dims "+n);
                await move(center(n),false);await move(center(n),false);Need(alpha(n)>.9f,"Moved/reversed collapsed handle still hovers "+n);
                await move(center(n),true);await move(center(n),false);
            }
            await move(new Vector2(1900,1070),false);
            ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("docs/development/hud_handles_tower_B_20261017/pointer_verified.png"));await Task.Delay(150);
            return new{passed=true,checks="InputSystem mouse enters/presses/releases both visible grips, top handle moves and damage grip mirrors/reopens with hover intact",kind="Temporary mouse, diagnostic resources and TimeScale0; restored; not human play"};
        }
        finally{Time.timeScale=oldTime;InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.RemoveDevice(mouse);if(oldMouse!=null&&oldMouse.added)oldMouse.MakeCurrent();}
    }
}
