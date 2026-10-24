using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class CoreGuardianCoreAmbientPlayModeTests
{
    // Emphasis keeps a shared brightness twin assigned between frames; compare the material it was made from.
    static Material Source(Material material)
    {
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianObjectEmphasis")).First(t=>t!=null);
        return (Material)type.GetMethod("SourceOf",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{material});
    }
    [UnityTest]
    public IEnumerator CoreParticlesRiseAndExpire_WhileDamageTemporarilyHidesAmbient_WithoutCameraPostProcessing()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var ps=UnityEngine.Object.FindObjectsByType<ParticleSystem>().Single(p=>p.name=="CoreCrystalParticles");
        var body=ps.transform.parent.parent.GetComponent<SpriteRenderer>();var original=Source(body.sharedMaterial);
        var camera=Camera.main;bool hdr=camera.allowHDR;
        var data=camera.GetComponents<Component>().Single(c=>c.GetType().Name=="UniversalAdditionalCameraData");
        var post=data.GetType().GetProperty("renderPostProcessing");bool originalPost=(bool)post.GetValue(data);
        var first=new ParticleSystem.Particle[12];var second=new ParticleSystem.Particle[12];
        yield return new WaitForSeconds(.85f);int n=ps.GetParticles(first);Assert.That(n,Is.GreaterThan(0));
        yield return new WaitForSeconds(.25f);int m=ps.GetParticles(second);
        var pairs=first.Take(n).Join(second.Take(m),a=>a.randomSeed,b=>b.randomSeed,(a,b)=>new {a,b}).ToArray();
        Assert.That(pairs.Length,Is.GreaterThan(0));
        Assert.That(pairs.All(p=>p.b.position.y>p.a.position.y&&p.b.remainingLifetime<p.a.remainingLifetime),Is.True,"Particles must rise while their lifetime counts down");
        var artType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianCombatArt")).First(t=>t!=null);
        var art=UnityEngine.Object.FindFirstObjectByType(artType);artType.GetMethod("CoreDamaged").Invoke(art,new object[]{1});
        var overlay=ps.transform.parent.GetComponent<MeshRenderer>();
        Assert.That(overlay.enabled,Is.False);Assert.That(ps.GetComponent<ParticleSystemRenderer>().enabled,Is.False);
        Assert.That(Source(body.sharedMaterial),Is.Not.SameAs(original),"Approved body feedback still owns the damage flash");
        yield return new WaitForSeconds(.35f);
        Assert.That(overlay.enabled,Is.True);Assert.That(Source(body.sharedMaterial),Is.SameAs(original));
        Assert.That(camera.allowHDR,Is.EqualTo(hdr));Assert.That((bool)post.GetValue(data),Is.EqualTo(originalPost));
#else
        yield return null;
#endif
    }
}
