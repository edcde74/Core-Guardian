using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CoreGuardianTowerGlowTests
{
    [TestCase("Single","single")]
    [TestCase("Multi","multi")]
    [TestCase("Support","support")]
    public void RealFireLightsTheVisibleOriginalCutouts_ThenRecoversWithoutChangingArtwork(string role,string file)
    {
#if UNITY_EDITOR
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianUnitAnimation")).First(t=>t!=null);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/ScreenTests/20261008_Depth/Prefabs/tower_"+file+".prefab");
        var root=UnityEngine.Object.Instantiate(prefab);
        try
        {
            var body=root.GetComponentsInChildren<SpriteRenderer>().Single(r=>r.name=="2D Sprite");
            var material=body.sharedMaterial;var sprite=body.sprite;var color=body.color;
            var animation=(Component)type.GetMethod("Attach").Invoke(null,new object[]{root,role});
            var glows=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="TowerGlow").ToArray();
            Assert.That(glows.Length,Is.EqualTo(3),"Selected glow must follow the original body and both recoil cutouts");
            type.GetMethod("Fire").Invoke(animation,new object[]{Vector2.up,1f});
            foreach(var glow in glows)
            {
                var source=glow.transform.parent.GetComponent<SpriteRenderer>();
                Assert.That(glow.enabled,Is.EqualTo(source.enabled));
                Assert.That(glow.sharedMaterial.GetFloat("_Age"),Is.LessThan(.001f),"The actual Fire event must immediately start the glow");
            }
            type.GetMethod("Present").Invoke(animation,new object[]{0f});
            type.GetMethod("Present").Invoke(animation,new object[]{.5f});
            Assert.That(body.enabled,Is.True,"Original artwork returns after recoil");
            Assert.That(glows.Single(r=>r.transform.parent==body.transform).sharedMaterial.GetFloat("_Age"),Is.GreaterThan(.3f),"Shot highlight must recover instead of staying lit");
            type.GetMethod("Fire").Invoke(animation,new object[]{Vector2.up,20f});
            Assert.That(glows[0].sharedMaterial.GetFloat("_PulseScale"),Is.LessThan(.5f),"Fast attacks need a shorter pulse, not permanent brightness");
            type.GetMethod("OnDisable",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(animation,null);
            Assert.That(glows.All(r=>!r.enabled),Is.True);
            Assert.That(body.sprite,Is.SameAs(sprite));Assert.That(body.sharedMaterial,Is.SameAs(material));Assert.That(body.color,Is.EqualTo(color));
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
#endif
    }
}
