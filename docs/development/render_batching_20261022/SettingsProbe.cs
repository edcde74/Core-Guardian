// Temporary in-memory diagnostic. Does not change or save the pipeline asset.
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditorInternal;
using Newtonsoft.Json;
public sealed class CGRenderSettingsProbe : MonoBehaviour
{
    static CGRenderSettingsProbe active;
    readonly List<object> rows=new List<object>();
    bool oldSrp,oldDriver,oldProfiler;
    int trial,step;
    const string root="docs/development/render_batching_20261022";
    public static object Start()
    {
        if(active!=null||!EditorApplication.isPlaying||Profiler.enabled||FrameDebugger.enabled)throw new Exception("Own idle Play required");
        active=new GameObject("Temporary rendering settings probe").AddComponent<CGRenderSettingsProbe>();
        active.oldSrp=GraphicsSettings.useScriptableRenderPipelineBatching;
        active.oldDriver=ProfilerDriver.enabled;active.oldProfiler=Profiler.enabled;
        active.Begin();return new{started=true,trials=6,warmup=60,samples=180};
    }
    void Begin()
    {
        step=0;rows.Clear();GraphicsSettings.useScriptableRenderPipelineBatching=trial%2==0;
    }
    void LateUpdate()
    {
        try{
            ++step;
            if(step==60){ProfilerDriver.ClearAllFrames();typeof(ProfilerDriver).GetMethod("SetProfilingEnabled",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{true});Profiler.enabled=true;}
            if(step>60)rows.Add(new{frame=Time.frameCount,dtMs=Time.unscaledDeltaTime*1000,draw=UnityStats.drawCalls,setPass=UnityStats.setPassCalls,triangles=UnityStats.triangles});
            if(step<240)return;
            Profiler.enabled=false;
            bool saved=ProfilerDriver.SaveProfile(Path.GetFullPath("Logs/CG_RenderBatching_20261022/settings_"+trial+".data"));
            File.WriteAllText(root+"/settings_"+trial+".json",JsonConvert.SerializeObject(new{trial,srp=trial%2==0,saved,first=ProfilerDriver.firstFrameIndex,last=ProfilerDriver.lastFrameIndex,rows},Formatting.Indented));
            if(++trial==6){Finish(null);return;}Begin();
        }catch(Exception ex){Finish(ex.ToString());}
    }
    public static object Status()=>active==null?(object)new{running=false}:new{running=true,active.trial,active.step};
    public static void Stop(){if(active!=null)active.Finish("Stopped by owner");}
    void Finish(string error)
    {
        Profiler.enabled=false;GraphicsSettings.useScriptableRenderPipelineBatching=oldSrp;
        ProfilerDriver.enabled=oldDriver;Profiler.enabled=oldProfiler;
        File.WriteAllText(root+"/settings_completion.json",JsonConvert.SerializeObject(new{error,trials=trial,restored=true}));
        active=null;Destroy(gameObject);
    }
}
