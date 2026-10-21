using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CoreGuardianBodyMaskSharingTests
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static Type Outline=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianSelectionOutline")).First(t=>t!=null);
    static T Field<T>(Component c,string name)=>(T)Outline.GetField(name,Hidden).GetValue(c);
    static void Set(Component c,string name,object value)=>Outline.GetField(name).SetValue(c,value);
    static void Show(Component c,Transform target,Camera camera)=>Outline.GetMethod("Present").Invoke(c,new object[]{target,camera});
    static void Clear(Component c){if(c!=null)Outline.GetMethod("OnDestroy",Hidden).Invoke(c,null);}
    sealed class Fixture:IDisposable
    {
        public readonly List<GameObject> roots=new List<GameObject>();
        public readonly List<Component> owners=new List<Component>();
        public Camera camera;public Transform target;public SpriteRenderer body;public Texture2D texture;public Sprite sprite;public RenderTexture image;
        public Fixture()
        {
            var root=Root("Sharing target");root.transform.position=new Vector3(10000,10000,0);target=root.transform;
            texture=new Texture2D(8,8,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};texture.SetPixels(Enumerable.Repeat(Color.white,64).ToArray());texture.Apply();
            sprite=Sprite.Create(texture,new Rect(0,0,8,8),Vector2.one*.5f,16,0,SpriteMeshType.FullRect);
            body=new GameObject("2D Sprite").AddComponent<SpriteRenderer>();body.transform.SetParent(target,false);body.sprite=sprite;
            camera=Root("Sharing camera").AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=1;camera.aspect=1;camera.transform.position=target.position+Vector3.back*10;
            image=new RenderTexture(128,128,24);image.Create();camera.targetTexture=image;
        }
        public GameObject Root(string name){var r=new GameObject(name);roots.Add(r);return r;}
        public Component Owner(int width=2){var c=Root("Mask consumer").AddComponent(Outline);owners.Add(c);Set(c,"widthPixels",width);return c;}
        public RenderTexture Show(Component c){CoreGuardianBodyMaskSharingTests.Show(c,target,camera);return Field<RenderTexture>(c,"mask");}
        public void Dispose()
        {
            foreach(var c in owners)Clear(c);
            camera.targetTexture=null;foreach(var r in roots)if(r!=null)UnityEngine.Object.DestroyImmediate(r);
            image.Release();UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(sprite);UnityEngine.Object.DestroyImmediate(texture);
        }
    }
    static Color32[] Pixels(RenderTexture rt)
    {
        var old=RenderTexture.active;var read=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
        try{RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);read.Apply();return read.GetPixels32();}
        finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(read);}
    }
    [Test]
    public void SameBodySameFrame_ReusesOneMask_AndPad4To3UsesInnerUV()
    {
        using(var f=new Fixture())
        {
            var a=f.Owner(2);var b=f.Owner(1);var first=f.Show(a);var second=f.Show(b);
            Assert.That(second,Is.SameAs(first),"Same body must reuse its covering frame mask");
            Assert.That(Field<int>(a,"lastMaskDrawCalls"),Is.EqualTo(1));Assert.That(Field<int>(b,"lastMaskDrawCalls"),Is.Zero);
            var auv=Field<Mesh>(a,"quad").uv;var buv=Field<Mesh>(b,"quad").uv;
            Assert.That(buv[0].x-auv[0].x,Is.EqualTo(1f/first.width).Within(.000001));
            Assert.That(buv[0].y-auv[0].y,Is.EqualTo(1f/first.height).Within(.000001));
            var aVertices=Field<Mesh>(a,"quad").vertices;var bVertices=Field<Mesh>(b,"quad").vertices;
            Assert.That(aVertices[1].x-aVertices[0].x,Is.GreaterThan(bVertices[1].x-bVertices[0].x));
        }
    }
    [Test]
    public void ChangedPose_PreservesOldReader_ThenNewReaderSharesNewGeneration()
    {
        using(var f=new Fixture())
        {
            var a=f.Owner();var b=f.Owner();var c=f.Owner();var old=f.Show(a);var pixels=Pixels(old);
            f.body.transform.localPosition+=Vector3.right*.1f;var fresh=f.Show(b);
            Assert.That(fresh,Is.Not.SameAs(old));Assert.That(Pixels(old),Is.EqualTo(pixels));
            Assert.That(f.Show(c),Is.SameAs(fresh));Assert.That(Field<int>(c,"lastMaskDrawCalls"),Is.Zero);
            Clear(b);UnityEngine.Object.DestroyImmediate(b.gameObject);
            Assert.That(fresh!=null&&fresh.IsCreated(),Is.True,"Borrower must survive producer destruction");
        }
    }
    [TestCase("alpha")][TestCase("flip")][TestCase("threshold")][TestCase("texture")][TestCase("camera")][TestCase("viewport")][TestCase("invalidate")]
    public void ChangedInputs_AreNotStaleHits(string change)
    {
        using(var f=new Fixture())
        {
            var a=f.Owner();var b=f.Owner();var old=f.Show(a);
            switch(change)
            {
                case "alpha":f.body.color=new Color(1,1,1,.5f);break;
                case "flip":f.body.flipX=true;break;
                case "threshold":Set(b,"alphaThreshold",.3f);break;
                case "texture":f.texture.SetPixel(0,0,Color.clear);f.texture.Apply();break;
                case "camera":f.camera.transform.position+=Vector3.right*.1f;break;
                case "viewport":f.camera.pixelRect=new Rect(0,0,96,128);break;
                case "invalidate":Outline.GetMethod("InvalidateSources").Invoke(a,null);break;
            }
            Assert.That(f.Show(b),Is.Not.SameAs(old));Assert.That(Field<int>(b,"lastMaskDrawCalls"),Is.EqualTo(1));
        }
    }
    [Test]
    public void RGBOnly_IsShared_AndDifferentBodyIsSeparate()
    {
        using(var f=new Fixture())
        {
            var a=f.Owner();var b=f.Owner();var c=f.Owner();var first=f.Show(a);f.body.color=Color.red;
            Assert.That(f.Show(b),Is.SameAs(first));
            var other=f.Root("Other body");other.transform.position=f.target.position;
            var sprite=new GameObject("2D Sprite").AddComponent<SpriteRenderer>();sprite.transform.SetParent(other.transform,false);sprite.sprite=f.sprite;
            Show(c,other.transform,f.camera);Assert.That(Field<RenderTexture>(c,"mask"),Is.Not.SameAs(first));
        }
    }
    [Test]
    public void SmallerFirstThenLarger_DoesNotOverwriteSmallerReader()
    {
        using(var f=new Fixture())
        {
            var a=f.Owner(1);var b=f.Owner(2);var first=f.Show(a);var pixels=Pixels(first);
            Assert.That(f.Show(b),Is.Not.SameAs(first));Assert.That(Pixels(first),Is.EqualTo(pixels));
        }
    }
}
