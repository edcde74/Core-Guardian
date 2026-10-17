using System;
using System.Reflection;
using System.Threading.Tasks;
using CoreGuardian;
using UnityEngine;
public static class VerifyCoreHudFeedback
{
    public static async Task<object> Main()
    {
        var p=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
        if(!Application.isPlaying||p==null||!p.Ready)throw new Exception("Owned ready Play required");
        bool before=p.enabled;p.enabled=false;
        try
        {
            p.Diagnostic();p.StartCombat();await Task.Delay(3200);
            if(p.HasFeedback)throw new Exception("Setup: previous notification must expire");
            p.Session.DebugSpawn(EnemyKind.Attack,100000);p.Session.Tick(.01f,new Vector2(15,10));
            p.GetType().GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p,null);
            if(!p.HasFeedback)throw new Exception("Nonfatal core damage must renew the visible HUD notice");
            return new{passed=true,kind="Injected core arrival after notice expiry; functional QA only"};
        }
        finally{p.enabled=before;}
    }
}
