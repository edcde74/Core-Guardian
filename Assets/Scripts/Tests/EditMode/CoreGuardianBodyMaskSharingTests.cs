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
    static Type Cache=>Outline.Assembly.GetType("CoreGuardianBodyMaskCache");
    const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
    static object CacheCall(string method,params object[] args)=>Cache.GetMethod(method,Static).Invoke(null,args);
    static void Release(Component c)=>Outline.GetMethod("ReleasePresentation",Hidden).Invoke(c,null);
    static int Count(string name)=>(int)Cache.GetProperty(name,Static).GetValue(null);
    sealed class Fixture:IDisposable
    {
        public readonly List<GameObject> roots=new List<GameObject>();
        public readonly List<Component> owners=new List<Component>();
        public Camera camera;public Transform target;public SpriteRenderer body;public Texture2D texture;public Sprite sprite;public RenderTexture image;
        public Fixture():this(false){}
        public Fixture(bool native)
        {
            var root=Root("Sharing target");target=root.transform;
            texture=new Texture2D(8,8,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};texture.SetPixels(Enumerable.Repeat(Color.white,64).ToArray());texture.Apply();
            sprite=Sprite.Create(texture,new Rect(0,0,8,8),Vector2.one*.5f,16,0,SpriteMeshType.FullRect);
            body=new GameObject("2D Sprite").AddComponent<SpriteRenderer>();body.transform.SetParent(target,false);body.sprite=sprite;
            camera=Root("Sharing camera").AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=1;camera.aspect=1;camera.transform.position=target.position+Vector3.back*10;
            image=new RenderTexture(native?1920:128,native?1080:128,24);image.Create();camera.targetTexture=image;
            if(native){camera.orthographicSize=8.5f;camera.aspect=1920f/1080;}
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
    public void ContactPad4Output_UsesEmphasisCrop_AndEmphasisReusesIt()
    {
        using(var f=new Fixture(true))
        {
            var a=f.Owner(2);var b=f.Owner(1);var material=new Material(Resources.Load<Shader>("CoreGuardian/PhotoShadowContact"));
            Outline.GetMethod("PresentGroundContactWithBand").Invoke(a,new object[]{f.target,f.camera,material,16f});
            var first=Field<RenderTexture>(a,"mask");var second=f.Show(b);UnityEngine.Object.DestroyImmediate(material);
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
    [TestCase("alpha")][TestCase("flip")][TestCase("threshold")][TestCase("texture")][TestCase("camera")][TestCase("viewport")][TestCase("invalidate")][TestCase("target-size")][TestCase("rotation")][TestCase("sprite")]
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
                case "target-size":f.image.Release();f.image.width=96;f.image.Create();break;
                case "rotation":f.body.transform.localRotation=Quaternion.Euler(0,0,15);break;
                case "sprite":f.body.sprite=Sprite.Create(f.texture,new Rect(0,0,4,8),Vector2.one*.5f,16,0,SpriteMeshType.FullRect);break;
            }
            Assert.That(f.Show(b),Is.Not.SameAs(old));Assert.That(Field<int>(b,"lastMaskDrawCalls"),Is.EqualTo(1));
            if(change=="sprite")UnityEngine.Object.DestroyImmediate(f.body.sprite);
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
    [Test]
    public void DistantWorld_DifferentCropFallsBack_EqualCropStillShares()
    {
        using(var f=new Fixture())
        {
            f.target.position=new Vector3(10000,10000,0);f.camera.transform.position=f.target.position+Vector3.back*10;
            var a=f.Owner(2);var b=f.Owner(1);var c=f.Owner(2);var first=f.Show(a);
            Assert.That(f.Show(b),Is.Not.SameAs(first));Assert.That(f.Show(c),Is.SameAs(first));
        }
    }
    [Test]
    public void FractionalUpperViewportEdge_DifferentCropMustNotShare()
    {
        using(var f=new Fixture())
        {
            f.camera.pixelRect=new Rect(0,0,100.5f,100.5f);f.body.transform.localPosition=new Vector3(.85f,.85f,0);
            var a=f.Owner(2);var b=f.Owner(1);var first=f.Show(a);
            Assert.That(f.Show(b),Is.Not.SameAs(first),"Fractional actual crop must keep its own texel mapping");
        }
    }
    [Test]
    public void ContactJustOutsideViewport_EmptyCompactCropUsesOriginalValidCrop()
    {
        using(var f=new Fixture(true))
        {
            f.body.transform.localPosition=f.camera.ScreenToWorldPoint(new Vector3(1926,540,10))+Vector3.right*.25f;var a=f.Owner(2);
            var material=new Material(Resources.Load<Shader>("CoreGuardian/PhotoShadowContact"));
            try{Assert.DoesNotThrow(()=>Outline.GetMethod("PresentGroundContactWithBand").Invoke(a,new object[]{f.target,f.camera,material,16f}));}
            finally{UnityEngine.Object.DestroyImmediate(material);}
        }
    }
    [Test]
    public void LastRelease_DisposesOwnedRecords_AndRepeatedReleaseIsSafe()
    {
        int entries=Count("EntryCount"),bindings=Count("BindingCount");
        using(var f=new Fixture())
        {
            var a=f.Owner();var b=f.Owner();var rt=f.Show(a);f.Show(b);Release(a);Release(a);
            Assert.That(rt!=null&&rt.IsCreated(),Is.True);Assert.That(f.Show(b),Is.SameAs(rt));Release(b);
            Assert.That(rt==null,Is.True);Assert.That(Count("EntryCount"),Is.EqualTo(entries));Assert.That(Count("BindingCount"),Is.EqualTo(bindings));
        }
    }
    [Test]
    public void DisabledManualConsumer_CanPresentThenReleaseWhenHostDeactivated()
    {
        using(var f=new Fixture())
        {
            var a=(Behaviour)f.Owner();a.enabled=false;var rt=f.Show(a);Assert.That(rt!=null,Is.True);
            a.gameObject.SetActive(false);Assert.That(Field<RenderTexture>(a,"mask"),Is.Null);Assert.That(rt==null,Is.True);
        }
    }
    [Test]
    public void ReleasedHardware_RegeneratesBeforeHit()
    {
        using(var f=new Fixture())
        {
            var a=f.Owner();var b=f.Owner();var rt=f.Show(a);rt.Release();var next=f.Show(b);
            Assert.That(next!=null&&next.IsCreated(),Is.True);Assert.That(Field<int>(b,"lastMaskDrawCalls"),Is.EqualTo(1));
        }
    }
    [Test]
    public void UnpublishedRecord_IsNeverReturnedAsHit()
    {
        using(var f=new Fixture())
        {
            var a=f.Owner();var b=f.Owner();var rt=f.Show(a);
            var binding=Field<object>(a,"maskBinding");var record=binding.GetType().GetField("current",Hidden|BindingFlags.Public).GetValue(binding);
            record.GetType().GetField("valid",Hidden|BindingFlags.Public).SetValue(record,false);
            Assert.That(f.Show(b),Is.Not.SameAs(rt));Assert.That(Field<int>(b,"lastMaskDrawCalls"),Is.EqualTo(1));
        }
    }
    [Test]
    public void SoloOwnerNextGeneration_ReusesCapacity()
    {
        using(var f=new Fixture())
        {
            var a=f.Owner();var rt=f.Show(a);Outline.GetMethod("InvalidateSources").Invoke(a,null);
            Assert.That(f.Show(a),Is.SameAs(rt));Assert.That(Field<int>(a,"lastMaskDrawCalls"),Is.EqualTo(1));
        }
    }
    [Test]
    public void SoloOwnerNextFrame_ReusesCapacityButRegenerates()
    {
        using(var f=new Fixture())
        {
            var a=f.Owner();var rt=f.Show(a);var lease=Field<object>(a,"maskBinding");
            var record=lease.GetType().GetField("current",Hidden|BindingFlags.Public).GetValue(lease);
            var field=record.GetType().GetField("request",Hidden|BindingFlags.Public);var request=field.GetValue(record);
            request.GetType().GetField("frame",Hidden|BindingFlags.Public).SetValue(request,Time.frameCount-1);field.SetValue(record,request);
            Assert.That(f.Show(a),Is.SameAs(rt));Assert.That(Field<int>(a,"lastMaskDrawCalls"),Is.EqualTo(1));
        }
    }
    [Test]
    public void SameCameraNewTargetReference_ForcesMiss()
    {
        using(var f=new Fixture())
        {
            var a=f.Owner();var b=f.Owner();var rt=f.Show(a);var next=new RenderTexture(128,128,24);next.Create();
            try{f.camera.targetTexture=next;Assert.That(f.Show(b),Is.Not.SameAs(rt));Assert.That(Field<int>(b,"lastMaskDrawCalls"),Is.EqualTo(1));}
            finally{f.camera.targetTexture=f.image;next.Release();UnityEngine.Object.DestroyImmediate(next);}
        }
    }
    [Test]
    public void AtlasRevision_ForcesNewGeneration()
    {
        using(var f=new Fixture())
        {
            var atlasType=Outline.Assembly.GetType("CoreGuardianMaskAtlas");var atlas=ScriptableObject.CreateInstance(atlasType);
            try{
                var a=f.Owner();var b=f.Owner();Outline.GetField("atlas",Hidden).SetValue(a,atlas);Outline.GetField("atlas",Hidden).SetValue(b,atlas);
                var rt=f.Show(a);atlasType.GetMethod("Invalidate").Invoke(atlas,null);
                Assert.That(f.Show(b),Is.Not.SameAs(rt));Assert.That(Field<int>(b,"lastMaskDrawCalls"),Is.EqualTo(1));
            }finally{UnityEngine.Object.DestroyImmediate(atlas);}
        }
    }
    [Test]
    public void ExplicitGeometryInvalidation_RebuildsOtherConsumersMeshes()
    {
        using(var f=new Fixture())
        {
            var b=f.Owner(1);var a=f.Owner(2);f.Show(b);f.Show(a);
            var meshes=(System.Collections.IDictionary)Field<object>(b,"meshes");var old=(Mesh)meshes[f.sprite];Assert.That(old,Is.Not.Null);
            Outline.GetMethod("InvalidateSources").Invoke(a,null);f.Show(b);
            Assert.That(old==null,Is.True);Assert.That(meshes[f.sprite],Is.Not.SameAs(old));Assert.That(Field<int>(b,"lastMaskDrawCalls"),Is.EqualTo(1));
        }
    }
    [TestCase("target")][TestCase("width")][TestCase("color")][TestCase("hidden")]
    public void InvalidPresentation_ReleasesBinding(string reason)
    {
        using(var f=new Fixture())
        {
            var a=f.Owner();var rt=f.Show(a);
            if(reason=="width")Set(a,"widthPixels",0);if(reason=="color")Set(a,"color",Color.clear);if(reason=="hidden")f.body.enabled=false;
            Show(a,reason=="target"?null:f.target,f.camera);
            Assert.That(Field<RenderTexture>(a,"mask"),Is.Null);Assert.That(rt==null,Is.True);
        }
    }
    [Test]
    public void NullOverlayAndModeSwitch_PreserveFallback_AndResetHidesOutputs()
    {
        using(var f=new Fixture())
        {
            var a=f.Owner();Assert.That(Outline.GetMethod("PresentOverlay").Invoke(a,new object[]{f.target,f.camera,null}),Is.False);
            Assert.That((bool)Outline.GetProperty("IsVisible").GetValue(a),Is.True);
            var renderer=Field<MeshRenderer>(a,"output");Assert.That(renderer.sharedMaterial.shader.name,Does.Contain("Selection"));
            var rt=Field<RenderTexture>(a,"mask");CacheCall("ResetOwned");
            Assert.That(renderer.enabled,Is.False);Assert.That(Field<RenderTexture>(a,"mask"),Is.Null);Assert.That(rt==null,Is.True);
        }
    }
}
