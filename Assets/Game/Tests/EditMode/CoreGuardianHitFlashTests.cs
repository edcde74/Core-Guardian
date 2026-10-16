using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class CoreGuardianHitFlashTests
{
    static Type Find(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).FirstOrDefault(t=>t!=null);
    [TestCase("Attack","enemy_attack")]
    [TestCase("Defense","enemy_defense")]
    [TestCase("Buff","enemy_buff")]
    [TestCase("Boss","")]
    public void BodyFlashFollowsAllWalkingDirections_AndRestoresMaterialsWithoutLightingShadow(string role,string name)
    {
        var prefab=role=="Boss"?Resources.Load<GameObject>("CoreGuardian/Animation/Boss/BossAnimated"):AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/ScreenTests/20261008_Depth/Prefabs/"+name+".prefab");
        var clone=UnityEngine.Object.Instantiate(prefab);
        try
        {
            var unitType=Find("CoreGuardianUnitAnimation");var unit=unitType.GetMethod("Attach").Invoke(null,new object[]{clone,role});
            var receive=unitType.GetMethod("ReceiveHit");Assert.That(receive,Is.Not.Null,"Body hit feedback must be connected to the animated monster");
            var art=clone.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");
            var body=art.GetComponentsInChildren<SpriteRenderer>(true);var materials=body.ToDictionary(r=>r,r=>r.sharedMaterial);
            var shadows=clone.GetComponentsInChildren<SpriteRenderer>(true).Except(body).ToDictionary(r=>r,r=>r.sharedMaterial);
            var originalColor=art.color;var position=clone.transform.position;var scale=clone.transform.localScale;
            receive.Invoke(unit,null);
            foreach(var direction in new[]{Vector2.right,Vector2.up,Vector2.down,Vector2.left})
            {
                unitType.GetMethod("Move").Invoke(unit,new object[]{direction,.01f});unitType.GetMethod("Present").Invoke(unit,new object[]{0f});
                foreach(var renderer in body.Where(r=>r.enabled&&r.gameObject.activeInHierarchy))
                {
                    Assert.That(renderer.sharedMaterial.shader.name,Is.EqualTo("CoreGuardian/BodyHitFlash"));
                    var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);Assert.That(block.GetFloat("_HitFlashAmount"),Is.GreaterThan(0));
                }
            }
            unitType.GetMethod("Present").Invoke(unit,new object[]{.08f});
            foreach(var renderer in body)Assert.That(renderer.sharedMaterial,Is.SameAs(materials[renderer]));
            foreach(var pair in shadows)Assert.That(pair.Key.sharedMaterial,Is.SameAs(pair.Value),"Shadow/health renderers must not flash");
            Assert.That(art.color,Is.EqualTo(originalColor));Assert.That(clone.transform.position,Is.EqualTo(position));Assert.That(clone.transform.localScale,Is.EqualTo(scale));
        }
        finally{UnityEngine.Object.DestroyImmediate(clone);}
    }
    [Test]
    public void RepeatedHitsCannotKeepTheBodyLit_AndOriginalPropertyBlockReturns()
    {
        var type=Find("CoreGuardianHitFlash");Assert.That(type,Is.Not.Null,"Body hit feedback is not implemented");
        var go=new GameObject("FlashTest");var renderer=go.AddComponent<SpriteRenderer>();
        try
        {
            var original=renderer.sharedMaterial;var block=new MaterialPropertyBlock();block.SetFloat("_UnrelatedDiagnostic",.37f);renderer.SetPropertyBlock(block);
            var flash=Activator.CreateInstance(type,new object[]{renderer});
            Assert.That(type.GetMethod("Trigger").Invoke(flash,null),Is.EqualTo(true));
            for(int i=0;i<8;i++){type.GetMethod("Advance").Invoke(flash,new object[]{.01f});Assert.That(type.GetMethod("Trigger").Invoke(flash,null),Is.EqualTo(false));}
            Assert.That(renderer.sharedMaterial,Is.SameAs(original),"Frequent hits must not extend the 65ms flash");
            renderer.GetPropertyBlock(block);Assert.That(block.GetFloat("_UnrelatedDiagnostic"),Is.EqualTo(.37f));Assert.That(block.GetFloat("_HitFlashAmount"),Is.Zero);
            type.GetMethod("Advance").Invoke(flash,new object[]{.03f});Assert.That(type.GetMethod("Trigger").Invoke(flash,null),Is.EqualTo(true));
            type.GetMethod("Clear").Invoke(flash,null);Assert.That(renderer.sharedMaterial,Is.SameAs(original));
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
}
