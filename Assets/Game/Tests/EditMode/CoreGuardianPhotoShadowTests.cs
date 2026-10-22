using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class CoreGuardianPhotoShadowTests
{
    static void DestroyOwned(GameObject root)
    {
        // EditMode-created, non-ExecuteAlways components need explicit native cleanup.
        foreach(var c in root.GetComponentsInChildren<MonoBehaviour>(true))
            if(c!=null&&(c.GetType().Name=="CoreGuardianSelectionOutline"||c.GetType().Name=="CoreGuardianPhotoShadow"))
                c.GetType().GetMethod("OnDestroy",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.Invoke(c,null);
        UnityEngine.Object.DestroyImmediate(root);
    }
    static Type Type(string name)=>TypeCache.GetTypesDerivedFrom<MonoBehaviour>().FirstOrDefault(t=>t.Name==name);
    [TestCase("Attack","enemy_attack")][TestCase("Defense","enemy_defense")][TestCase("Buff","enemy_buff")]
    [TestCase("Single","tower_single")][TestCase("Multi","tower_multi")][TestCase("Support","tower_support")]
    public void RealCreationAttachesSeparatePhotoLayerAndKeepsBody(string role,string prefab)
    {
        var type=Type("CoreGuardianPhotoShadow");Assert.That(type,Is.Not.Null,"Approved photo shadow is not connected");
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/ScreenTests/20261008_Depth/Prefabs/"+prefab+".prefab"));
        try {
            var body=root.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");
            var original=body.sprite;var position=body.transform.localPosition;var material=body.sharedMaterial;
            var unit=Type("CoreGuardianUnitAnimation").GetMethod("Attach").Invoke(null,new object[]{root,role});
            // Existing walking setup aligns the original source to its approved ground anchor.
            position=body.transform.localPosition;
            var shadow=root.GetComponent(type);Assert.That(shadow,Is.Not.Null,"Every real spawn/install must attach the approved shadow");
            type.GetMethod("Present").Invoke(shadow,new object[]{null});
            var cast=root.GetComponentsInChildren<SpriteRenderer>(true).Single(r=>r.name=="PhotoCastShadow");
            Assert.That(root.transform.Find("PhotoDynamicContact"),Is.Null,"Approved removal must skip contact-mask creation entirely");
            Assert.That(cast.sprite,Is.Not.Null);Assert.That(cast.transform.IsChildOf(body.transform),Is.False);
            var offset=cast.transform.position-root.transform.position;root.transform.position+=new Vector3(1,2);
            Type("CoreGuardianUnitAnimation").GetMethod("Move").Invoke(unit,new object[]{Vector2.up,.1f});
            Type("CoreGuardianUnitAnimation").GetMethod("Present").Invoke(unit,new object[]{.1f});
            type.GetMethod("Present").Invoke(shadow,new object[]{null});
            Assert.That(Vector3.Distance(cast.transform.position-root.transform.position,offset),Is.LessThan(.0001f),"Cast anchor must follow ground root without rotating into another direction");
            Assert.That(cast.transform.rotation,Is.EqualTo(Quaternion.identity));
            Assert.That(body.sprite,Is.SameAs(original));Assert.That(body.sharedMaterial,Is.SameAs(material));Assert.That(body.transform.localPosition,Is.EqualTo(position));
            if(role=="Defense"||role=="Multi"||role=="Support")
                Assert.That(root.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="바깥 그림자"||r.name=="접촉 그림자"||r.name=="짧은 투사 그림자").All(r=>!r.enabled),Is.True,"Do not stack replaced old shadow ellipses");
        } finally {DestroyOwned(root);}
    }
    [Test]
    public void AllEightPhotoMasksAreDistinctPointFilteredResources()
    {
        foreach(var role in new[]{"Attack","Defense","Buff","Player","Core","Single","Multi","Support"})
        {
            var sprite=Resources.Load<Sprite>("CoreGuardian/PhotoShadows/"+role);Assert.That(sprite,Is.Not.Null,role);
            Assert.That(sprite.texture.filterMode,Is.EqualTo(FilterMode.Point));Assert.That(sprite.texture.mipmapCount,Is.EqualTo(1));
        }
    }
    [TestCase("Player","Assets/Art/ScreenTests/20261008_Depth/Prefabs/Player.prefab")]
    [TestCase("Core","Assets/Art/ScreenTests/20261008_Depth/Prefabs/core.prefab")]
    [TestCase("Boss","Assets/Resources/CoreGuardian/Animation/Boss/BossAnimated.prefab")]
    public void PlayerCoreAndBossKeepTheirOwnOriginalAndUsePhotoShadows(string role,string path)
    {
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        try {
            var type=Type("CoreGuardianPhotoShadow");var art=root.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");var sprite=art.sprite;
            if(role=="Core")type.GetMethod("Attach").Invoke(null,new object[]{root,role});
            else Type("CoreGuardianUnitAnimation").GetMethod("Attach").Invoke(null,new object[]{root,role});
            Assert.That(root.GetComponent(type),Is.Not.Null);Assert.That(art.sprite,Is.SameAs(sprite));
            Assert.That(root.GetComponentsInChildren<SpriteRenderer>(true).Single(r=>r.name=="PhotoCastShadow").sprite,Is.Not.Null);
            if(role=="Core")Assert.That(root.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="바닥 수정빛").All(r=>!r.enabled),Is.True);
            Assert.That(root.transform.Find("PhotoDynamicContact"),Is.Null,"Approved removal keeps only the broad photo shadow");
        } finally {DestroyOwned(root);}
    }
}
