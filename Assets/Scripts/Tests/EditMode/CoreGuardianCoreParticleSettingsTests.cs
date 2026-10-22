using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class CoreGuardianCoreParticleSettingsTests
{
    [Test]
    public void InspectorSettingsChangeLiveParticleSizeAndCount_WithoutChangingLifetimeOrTint()
    {
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianCoreParticleSettings")).FirstOrDefault(t=>t!=null);
        Assert.That(type,Is.Not.Null,"Persistent Inspector particle controls are not implemented yet");
        var root=new GameObject("Particle settings test");
        try
        {
            var settings=root.AddComponent(type);var ps=root.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.startLifetime=2;main.startColor=Color.cyan;
            type.GetMethod("ApplyTo").Invoke(settings,new object[]{ps});
            ps.Play();
            var samples=new[]{new ParticleSystem.Particle{startSize=.025f,startLifetime=2,remainingLifetime=1,startColor=Color.cyan},new ParticleSystem.Particle{startSize=.045f,startLifetime=2,remainingLifetime=1,startColor=Color.cyan}};
            ps.SetParticles(samples,2);
            type.GetField("sizeMin").SetValue(settings,.05f);type.GetField("sizeMax").SetValue(settings,.09f);
            type.GetField("emissionPerSecond").SetValue(settings,7f);type.GetField("maxParticles").SetValue(settings,24);
            type.GetMethod("ApplyTo").Invoke(settings,new object[]{ps});
            Assert.That(ps.main.startSize.constantMin,Is.EqualTo(.05f).Within(.00001f));Assert.That(ps.main.startSize.constantMax,Is.EqualTo(.09f).Within(.00001f));
            Assert.That(ps.emission.rateOverTime.constant,Is.EqualTo(7));Assert.That(ps.main.maxParticles,Is.EqualTo(24));
            Assert.That(ps.GetParticles(samples),Is.EqualTo(2));Assert.That(samples[0].startSize,Is.EqualTo(.05f).Within(.00001f));Assert.That(samples[1].startSize,Is.EqualTo(.09f).Within(.00001f));
            Assert.That(samples.All(p=>p.remainingLifetime==1&&p.startColor.Equals((Color32)Color.cyan)),Is.True,"Live resize must not restart or recolor existing particles");
            Assert.That(ps.main.startLifetime.constant,Is.EqualTo(2));
            type.GetField("maxParticles").SetValue(settings,1);type.GetMethod("ApplyTo").Invoke(settings,new object[]{ps});Assert.That(ps.particleCount,Is.EqualTo(1));
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
