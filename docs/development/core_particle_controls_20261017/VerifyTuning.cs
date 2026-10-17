using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

public static class VerifyCoreParticleTuning
{
    static void Need(bool value,string message){if(!value)throw new Exception(message);}
    public static async Task<object> Main()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();var settings=game.GetComponent<CoreGuardianCoreParticleSettings>();
        Need(Application.isPlaying&&settings!=null,"Owned Play with persistent Inspector settings required");
        var ps=UnityEngine.Object.FindObjectsByType<ParticleSystem>().Single(p=>p.name=="CoreCrystalParticles");
        float min=settings.sizeMin,max=settings.sizeMax,rate=settings.emissionPerSecond,time=Time.timeScale;int cap=settings.maxParticles;
        var dir=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"docs/development/core_particle_controls_20261017");
        try
        {
            await Task.Delay(850);Time.timeScale=0;await Task.Delay(100);
            var first=new ParticleSystem.Particle[64];int count=ps.GetParticles(first);Need(count>0,"Live particles required");
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"before.png"));await Task.Delay(150);
            // Use the same serialized path as the Inspector, not a separate debug control.
            var serialized=new SerializedObject(settings);serialized.FindProperty("sizeMin").floatValue=.055f;serialized.FindProperty("sizeMax").floatValue=.085f;
            serialized.FindProperty("emissionPerSecond").floatValue=8;serialized.FindProperty("maxParticles").intValue=24;serialized.ApplyModifiedProperties();
            await Task.Delay(120);
            Need(Mathf.Approximately(ps.main.startSize.constantMin,.055f)&&Mathf.Approximately(ps.main.startSize.constantMax,.085f),"Serialized Inspector size changes must reach the actual system");
            Need(Mathf.Approximately(ps.emission.rateOverTime.constant,8)&&ps.main.maxParticles==24,"Rate and cap must reach the actual system");
            var second=new ParticleSystem.Particle[64];int n=ps.GetParticles(second);Need(n==count,"Changing settings must preserve live particles");
            var pairs=first.Take(count).Join(second.Take(n),a=>a.randomSeed,b=>b.randomSeed,(a,b)=>new {a,b}).ToArray();
            Need(pairs.Length==count&&pairs.All(p=>p.b.startSize>p.a.startSize&&Mathf.Approximately(p.a.remainingLifetime,p.b.remainingLifetime)),"Existing particles immediately resize without lifetime restart: "+Newtonsoft.Json.JsonConvert.SerializeObject(pairs.Select(p=>new {before=p.a.startSize,after=p.b.startSize,lifeBefore=p.a.remainingLifetime,lifeAfter=p.b.remainingLifetime,seedBefore=p.a.randomSeed,seedAfter=p.b.randomSeed})));
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"larger-preview.png"));await Task.Delay(150);
            settings.enabled=false;await Task.Delay(80);Need(!ps.GetComponent<ParticleSystemRenderer>().enabled&&ps.transform.parent.GetComponent<MeshRenderer>().enabled,"Toggle affects particles only, not core glow");
            settings.enabled=true;await Task.Delay(80);
            var art=game.GetComponent<CoreGuardianCombatArt>();var ambient=(CoreGuardianCoreAmbient)typeof(CoreGuardianCombatArt).GetField("coreAmbient",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(art);
            ambient.NotifyCoreDamage();Need(!ps.emission.enabled&&!ps.GetComponent<ParticleSystemRenderer>().enabled,"Damage suppression still wins over inspector tuning");
            ambient.Advance(.31f);Need(ps.emission.enabled&&ps.GetComponent<ParticleSystemRenderer>().enabled,"Particles return after suppression");
            return new {passed=true,liveParticles=count,previewMin=.055f,previewMax=.085f,previewRate=8,previewCap=24,defaultsRestored=true,conditions="SerializedObject Inspector path,actual live particles;TimeScale0 for same lifetime before/after;temporary larger values restored;actual Overlay screenshots,no camera changes"};
        }
        finally
        {
            settings.enabled=true;settings.sizeMin=min;settings.sizeMax=max;settings.emissionPerSecond=rate;settings.maxParticles=cap;settings.ApplyTo(ps);Time.timeScale=time;
        }
    }
}
