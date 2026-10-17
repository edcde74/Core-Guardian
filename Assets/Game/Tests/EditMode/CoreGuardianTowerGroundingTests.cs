using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

public class CoreGuardianTowerGroundingTests
{
    [TestCase("Single","single")]
    [TestCase("Multi","multi")]
    [TestCase("Support","support")]
    public void AllTowerBasesStayGroundedThroughFiring_WithoutReplacingOriginals(string role,string file)
    {
        var type=TypeCache.GetTypesDerivedFrom<MonoBehaviour>().First(t=>t.Name=="CoreGuardianUnitAnimation");
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/ScreenTests/20261008_Depth/Prefabs/tower_"+file+".prefab"));
        try
        {
            var body=root.GetComponentsInChildren<SpriteRenderer>().Single(r=>r.name=="2D Sprite");
            var sprite=body.sprite;var material=body.sharedMaterial;var position=body.transform.localPosition;
            var animation=type.GetMethod("Attach").Invoke(null,new object[]{root,role});
            var shade=root.GetComponentsInChildren<MeshRenderer>(true).SingleOrDefault(r=>r.name=="TowerBaseShade");
            Assert.That(shade,Is.Not.Null,"Approved base shading must be attached for every installed tower kind");
            var contact=root.GetComponentsInChildren<SpriteRenderer>().Single(r=>r.name=="접촉 그림자");
            float pixel=body.transform.lossyScale.y/sprite.pixelsPerUnit;
            Assert.That(contact.bounds.min.y,Is.InRange(body.bounds.min.y-2*pixel,body.bounds.min.y),"Shadow must touch actual sprite bottom, not logical tile origin");
            Assert.That(contact.color.a,Is.GreaterThan(.7f));
            Assert.That(root.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.name=="짧은 투사 그림자"),Is.True);
            Assert.That(root.transform.Find("그림자/바닥 밀착선").gameObject.activeSelf,Is.False,"Do not stack the old displaced source-foot duplicate");
            Assert.That(shade.transform.IsChildOf(body.transform),Is.False,"Ground shade must be excluded from body union, outline and upgrade masks");
            var before=shade.transform.position;
            type.GetMethod("Fire").Invoke(animation,new object[]{Vector2.up,20f});
            type.GetMethod("Present").Invoke(animation,new object[]{.5f});
            Assert.That(shade.transform.position,Is.EqualTo(before),"Base shade stays fixed while barrel or crystals recoil");
            Assert.That(shade.sortingOrder,Is.EqualTo(body.sortingOrder+2));
            Assert.That(body.sprite,Is.SameAs(sprite));Assert.That(body.sharedMaterial,Is.SameAs(material));
            Assert.That(body.transform.localPosition,Is.EqualTo(position));
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
