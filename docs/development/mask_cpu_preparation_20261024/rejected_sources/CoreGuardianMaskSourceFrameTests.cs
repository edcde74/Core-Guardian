using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CoreGuardianMaskSourceFrameTests
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    const BindingFlags All=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Outline=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianSelectionOutline")).First(t=>t!=null);
    static object Get(object o,string name)=>o.GetType().GetField(name,All).GetValue(o);
    static object Sample(object frame)=>((IEnumerable)frame.GetType().GetProperty("Samples",All).GetValue(frame)).Cast<object>().Single();
    static void Capture(object frame,SpriteRenderer body)=>frame.GetType().GetMethod("Capture",All).Invoke(frame,new object[]{new List<SpriteRenderer>{body}});
    static object NewFrame()
    {
        var t=Outline.Assembly.GetType("CoreGuardianMaskSourceFrame");Assert.That(t,Is.Not.Null,"The call-local source collector must exist");return Activator.CreateInstance(t,true);
    }
    sealed class Fixture:IDisposable
    {
        readonly object inner;
        public Fixture(){inner=Activator.CreateInstance(typeof(CoreGuardianBodyMaskSharingTests).GetNestedType("Fixture",BindingFlags.NonPublic),true);}
        public SpriteRenderer Body=>(SpriteRenderer)Get(inner,"body");
        public Component Owner()=>(Component)inner.GetType().GetMethod("Owner").Invoke(inner,new object[]{2});
        public RenderTexture Show(Component c)=>(RenderTexture)inner.GetType().GetMethod("Show").Invoke(inner,new object[]{c});
        public void Dispose()=>((IDisposable)inner).Dispose();
    }
    static Color32[] Pixels(RenderTexture rt)
    {
        var old=RenderTexture.active;var read=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
        try{RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);read.Apply();return read.GetPixels32();}
        finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(read);}
    }
    [Test]
    public void CaptureReflectsSameFrameChanges_AndPriorValueStaysImmutable()
    {
        using(var f=new Fixture())
        {
            var frame=NewFrame();Capture(frame,f.Body);var old=Sample(frame);
            f.Body.flipX=true;f.Body.flipY=true;f.Body.color=new Color(.1f,.2f,.3f,.5f);f.Body.sortingOrder=42;
            f.Body.transform.localPosition=new Vector3(2,3,0);f.Body.transform.localRotation=Quaternion.Euler(0,0,37);f.Body.transform.localScale=new Vector3(2,3,1);
            var texture=f.Body.sprite.texture;texture.SetPixel(0,0,Color.clear);texture.Apply();
            var sprite=Sprite.Create(texture,new Rect(0,0,4,8),Vector2.one*.5f,16,0,SpriteMeshType.FullRect);
            try
            {
                f.Body.sprite=sprite;Capture(frame,f.Body);var current=Sample(frame);
                Assert.That(Get(current,"Sprite"),Is.SameAs(sprite));Assert.That(Get(current,"SourceVersion"),Is.EqualTo(texture.updateCount));
                Assert.That(Get(current,"FlipX"),Is.True);Assert.That(Get(current,"FlipY"),Is.True);Assert.That(Get(current,"Color"),Is.EqualTo(f.Body.color));
                Assert.That(Get(current,"SortingOrder"),Is.EqualTo(42));Assert.That(Get(current,"SortingLayerID"),Is.EqualTo(f.Body.sortingLayerID));
                Assert.That(Get(current,"LocalToWorld"),Is.EqualTo(f.Body.transform.localToWorldMatrix));Assert.That(Get(current,"Bounds"),Is.EqualTo(f.Body.bounds));
                Assert.That(Get(old,"Sprite"),Is.Not.SameAs(sprite));Assert.That(Get(old,"Color"),Is.EqualTo(Color.white));Assert.That(Get(old,"FlipX"),Is.False);
                frame.GetType().GetMethod("Clear",All).Invoke(frame,null);
                Assert.That(((IEnumerable)frame.GetType().GetProperty("Samples",All).GetValue(frame)).Cast<object>(),Is.Empty);
            }
            finally{UnityEngine.Object.DestroyImmediate(sprite);}
        }
    }
    [Test]
    public void ResourceCreationCallbackRunsBeforeCapture()
    {
        using(var f=new Fixture())
        {
            NewFrame();var owner=f.Owner();var callback=owner.gameObject.AddComponent<CoreGuardianMaskResourceProbe>();callback.body=f.Body;
            f.Show(owner);Assert.That(callback.calls,Is.EqualTo(1));
            var frame=Get(owner,"sourceFrame");var sample=Sample(frame);
            Assert.That(Get(sample,"LocalToWorld"),Is.EqualTo(f.Body.transform.localToWorldMatrix));
            Assert.That(Get(sample,"Color"),Is.EqualTo(new Color(1,1,1,.5f)));
            var bounds=(Bounds)Outline.GetProperty("TargetBounds").GetValue(owner);
            Assert.That(Vector3.Distance(bounds.center,f.Body.bounds.center),Is.LessThan(.000001f));
            Assert.That(Vector3.Distance(bounds.extents,f.Body.bounds.extents),Is.LessThan(.000001f));
        }
    }
    [Test]
    public void NextCaptureDoesNotMutateBoundRecordOrOldReaderPixels()
    {
        using(var f=new Fixture())
        {
            NewFrame();var a=f.Owner();var b=f.Owner();var c=f.Owner();var first=f.Show(a);var pixels=Pixels(first);
            f.Body.transform.localPosition=Vector3.right*.1f;var second=f.Show(b);Assert.That(second,Is.Not.SameAs(first));
            Capture(Get(a,"sourceFrame"),f.Body);
            Assert.That(Pixels(first),Is.EqualTo(pixels));
            f.Body.transform.localPosition=Vector3.zero;
            Assert.That(f.Show(c),Is.SameAs(first),"Cache must own Part values, not a mutable source-frame buffer");
            Assert.That(Pixels(first),Is.EqualTo(pixels));
        }
    }
}

[ExecuteAlways]
public class CoreGuardianMaskResourceProbe:MonoBehaviour
{
    public SpriteRenderer body;public int calls;
    void OnTransformChildrenChanged(){if(body!=null&&calls==0&&transform.Find("SelectionOutline")!=null){calls++;body.transform.localPosition=new Vector3(.1f,.2f,0);body.color=new Color(1,1,1,.5f);}}
}
