using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CoreGuardianOutlinePayloadTests
{
    const BindingFlags All=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Outline=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianSelectionOutline")).First(t=>t!=null);
    static object Get(object o,string n)=>o.GetType().GetField(n,All).GetValue(o);
    sealed class Fixture:IDisposable
    {
        readonly object inner=Activator.CreateInstance(typeof(CoreGuardianBodyMaskSharingTests).GetNestedType("Fixture",BindingFlags.NonPublic),true);
        public Component Owner;
        public Fixture(){Owner=(Component)inner.GetType().GetMethod("Owner").Invoke(inner,new object[]{2});}
        public void Show()=>inner.GetType().GetMethod("Show").Invoke(inner,new object[]{Owner});
        public Transform Target=>(Transform)Get(inner,"target");public Camera Camera=>(Camera)Get(inner,"camera");
        public bool Prepare(int radius){var method=Outline.GetMethod("PrepareOutlineProperties",All);Assert.That(method,Is.Not.Null,"Normal payload preparation must expose whether native block reconstruction was necessary");return (bool)method.Invoke(Owner,new object[]{radius});}
        public MaterialPropertyBlock Read(){var p=new MaterialPropertyBlock();((MeshRenderer)Get(Owner,"output")).GetPropertyBlock(p);return p;}
        public void Dispose()=>((IDisposable)inner).Dispose();
    }
    [Test]
    public void UnchangedPayloadReusesConstruction_ExactInputsInvalidate()
    {
        using(var f=new Fixture())
        {
            f.Show();Assert.That(f.Prepare(2),Is.False);
            var c=(Color)Outline.GetField("color").GetValue(f.Owner);c.r+=.000001f;Outline.GetField("color").SetValue(f.Owner,c);
            Assert.That(f.Prepare(2),Is.True);Assert.That(f.Prepare(2),Is.False);
            Assert.That(f.Prepare(3),Is.True);Assert.That(f.Prepare(3),Is.False);
            Outline.GetField("maskOriginPx",All).SetValue(f.Owner,new Vector2(.25f,.5f));Assert.That(f.Prepare(3),Is.True);
            var mask=(RenderTexture)Get(f.Owner,"mask");mask.Release();mask.width+=8;mask.Create();Assert.That(f.Prepare(3),Is.True);
        }
    }
    [Test]
    public void EveryPresentRestoresExternalRendererBlock_AndClampedWidth()
    {
        using(var f=new Fixture())
        {
            Outline.GetField("widthPixels").SetValue(f.Owner,9);f.Show();var expected=f.Read();
            var foreign=new MaterialPropertyBlock();foreign.SetColor("_Color",Color.magenta);foreign.SetInt("_Width",99);foreign.SetFloat("_Band",42);
            ((MeshRenderer)Get(f.Owner,"output")).SetPropertyBlock(foreign);f.Show();var actual=f.Read();
            Assert.That(actual.GetColor("_Color"),Is.EqualTo(expected.GetColor("_Color")));Assert.That(actual.GetInt("_Width"),Is.EqualTo(8));Assert.That(actual.GetFloat("_Band"),Is.Zero);
            Assert.That(f.Prepare(8),Is.False);Outline.GetField("widthPixels").SetValue(f.Owner,10);f.Show();Assert.That(f.Prepare(8),Is.False);
        }
    }
    [Test]
    public void ContactAndOverlayTransitionsClearForeignPayload_ReleaseAndInvalidationRefresh()
    {
        using(var f=new Fixture())
        {
            var material=new Material(Resources.Load<Shader>("CoreGuardian/PhotoShadowContact"));
            try
            {
                f.Show();Outline.GetMethod("PresentGroundContactWithBand").Invoke(f.Owner,new object[]{f.Target,f.Camera,material,16f});
                Assert.That(f.Read().GetFloat("_Band"),Is.EqualTo(16));f.Show();Assert.That(f.Read().GetFloat("_Band"),Is.Zero);Assert.That(f.Read().GetInt("_Width"),Is.EqualTo(2));
                Outline.GetMethod("PresentOverlay").Invoke(f.Owner,new object[]{f.Target,f.Camera,material});f.Show();Assert.That(f.Read().GetInt("_Width"),Is.EqualTo(2));
                Outline.GetMethod("InvalidateSources").Invoke(f.Owner,null);Assert.That(f.Prepare(2),Is.True);
                Outline.GetMethod("ReleasePresentation",All).Invoke(f.Owner,null);f.Show();Assert.That(f.Read().GetInt("_Width"),Is.EqualTo(2));Assert.That(f.Prepare(2),Is.False);
                f.Camera.orthographicSize*=.5f;f.Show();var mask=(RenderTexture)Get(f.Owner,"mask");Assert.That(f.Read().GetTexture("_MainTex"),Is.SameAs(mask));Assert.That(f.Read().GetVector("_MainTex_TexelSize").z,Is.EqualTo(mask.width));
            }
            finally{UnityEngine.Object.DestroyImmediate(material);}
        }
    }
}
