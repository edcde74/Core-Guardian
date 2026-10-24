using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CoreGuardianCompletedMaskEligibilityTests
{
    const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,All).GetValue(o);
    [TestCase("equal","equal")][TestCase("pose","pose")][TestCase("pending","invalid")]
    public void CompletedReuseEligibility_UsesActualInputAndCompletion(string change,string expected)
    {
        var t=typeof(CoreGuardianBodyMaskSharingTests).GetNestedType("Fixture",BindingFlags.NonPublic);
        using(var f=(IDisposable)Activator.CreateInstance(t,true))
        {
            var owner=t.GetMethod("Owner").Invoke(f,new object[]{2});t.GetMethod("Show").Invoke(f,new[]{owner});
            var lease=Get(owner,"maskBinding");var record=Get(lease,"current");var request=Get(record,"request");
            request.GetType().GetField("frame",All).SetValue(request,Time.frameCount+1);
            var parts=(IList)Get(lease,"scratch");
            if(change=="pose")
            {
                var body=(SpriteRenderer)Get(f,"body");body.transform.localPosition+=Vector3.right*.1f;
                var part=Activator.CreateInstance(parts[0].GetType(),BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{body,null},null);parts.Clear();parts.Add(part);
            }
            if(change=="pending"){record.GetType().GetField("valid",All).SetValue(record,false);record.GetType().GetField("pending",All).SetValue(record,true);}
            var method=record.GetType().DeclaringType.GetMethod("CompletedReuseReason",All);
            Assert.That(method,Is.Not.Null,"Need completion-aware exact-input classifier before changing reuse behavior");
            Assert.That(method.Invoke(null,new[]{record,request,parts}),Is.EqualTo(expected));
        }
    }
}
