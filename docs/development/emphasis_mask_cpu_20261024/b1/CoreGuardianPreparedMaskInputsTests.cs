using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CoreGuardianPreparedMaskInputsTests
{
    const BindingFlags All=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Outline=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianSelectionOutline")).First(t=>t!=null);
    static object Get(object o,string n)=>o.GetType().GetField(n,All).GetValue(o);
    [Test]
    public void PreparedPartValuesStayStableAcrossSourceChanges_AndRefreshOnNextBegin()
    {
        var fixture=typeof(CoreGuardianBodyMaskSharingTests).GetNestedType("Fixture",BindingFlags.NonPublic);
        using(var f=(IDisposable)Activator.CreateInstance(fixture,true))
        {
            var owner=fixture.GetMethod("Owner").Invoke(f,new object[]{2});
            var body=(SpriteRenderer)Get(f,"body");
            body.color=new Color(1,1,1,.7f);body.flipX=true;
            fixture.GetMethod("Show").Invoke(f,new[]{owner});
            var lease=Get(owner,"maskBinding");var parts=(IList)Get(lease,"scratch");var old=parts[0];
            var sprite=old.GetType().GetProperty("Sprite",All);var alpha=old.GetType().GetProperty("Alpha",All);var matrix=old.GetType().GetProperty("DrawMatrix",All);
            Assert.That(sprite,Is.Not.Null,"Queue input must expose its prepared sprite");
            Assert.That(alpha,Is.Not.Null);Assert.That(matrix,Is.Not.Null);
            var expected=body.transform.localToWorldMatrix*Matrix4x4.Scale(new Vector3(-1,1,1));
            Assert.That(sprite.GetValue(old),Is.SameAs(body.sprite));Assert.That(alpha.GetValue(old),Is.EqualTo(.7f));Assert.That(matrix.GetValue(old),Is.EqualTo(expected));
            body.transform.localPosition=new Vector3(.1f,.2f,0);body.color=new Color(1,1,1,.4f);body.flipX=false;body.flipY=true;
            fixture.GetMethod("Show").Invoke(f,new[]{owner});
            Assert.That(alpha.GetValue(old),Is.EqualTo(.7f));Assert.That(matrix.GetValue(old),Is.EqualTo(expected));
            Assert.That(alpha.GetValue(parts[0]),Is.EqualTo(.4f));
            Assert.That(matrix.GetValue(parts[0]),Is.EqualTo(body.transform.localToWorldMatrix*Matrix4x4.Scale(new Vector3(1,-1,1))));
        }
    }
}
