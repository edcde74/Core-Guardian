using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CoreGuardianMaskCameraStateTests
{
    const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Find(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).FirstOrDefault(t=>t!=null);
    static object Get(object o,string n)=>o.GetType().GetField(n,All).GetValue(o);
    static object Capture(Camera c){var t=Find("CoreGuardianMaskCameraState");Assert.That(t,Is.Not.Null,"Need a call-scoped camera state, not a global frame cache");return t.GetMethod("Capture",All).Invoke(null,new object[]{c});}
    sealed class Fixture:IDisposable
    {
        readonly object inner=Activator.CreateInstance(typeof(CoreGuardianBodyMaskSharingTests).GetNestedType("Fixture",BindingFlags.NonPublic),new object[]{true});
        public Camera Camera=>(Camera)Get(inner,"camera");
        public Component Owner=>(Component)inner.GetType().GetMethod("Owner").Invoke(inner,new object[]{2});
        public void Show(Component owner)=>inner.GetType().GetMethod("Show").Invoke(inner,new object[]{owner});
        public void Dispose(){var page=Find("CoreGuardianMaskPage");page.GetMethod("ReleaseCamera",All).Invoke(null,new object[]{Camera});((IDisposable)inner).Dispose();}
    }
    static object Acquire(Camera camera,object state)
    {
        var method=Find("CoreGuardianMaskPage").GetMethods(All).Single(m=>m.Name=="TryAcquire"&&m.GetParameters().Length==6);
        var args=new object[]{camera,Time.frameCount,16,16,state,null};Assert.That(method.Invoke(null,args),Is.True);return args[5];
    }
    [Test]
    public void SameCallInternalChecks_UseCapturedValues_AndPublicChecksReadFresh()
    {
        using(var f=new Fixture())
        {
            var state=Capture(f.Camera);var slot=Acquire(f.Camera,state);f.Camera.transform.position+=Vector3.right*.1f;
            var methods=Find("CoreGuardianMaskPage").GetMethods(All).Where(m=>m.Name=="IsValid");
            Assert.That(methods.Single(m=>m.GetParameters().Length==2).Invoke(null,new[]{slot,state}),Is.True);
            Assert.That(methods.Single(m=>m.GetParameters().Length==1).Invoke(null,new[]{slot}),Is.False);
        }
    }
    [Test]
    public void NextPublicCallSeesChangedCamera_AndTwoCamerasDoNotShareState()
    {
        using(var f=new Fixture())using(var other=new Fixture())
        {
            var a=Capture(f.Camera);var b=Capture(other.Camera);Assert.That(Get(a,"camera"),Is.Not.SameAs(Get(b,"camera")));
            var old=Get(a,"projection");f.Camera.orthographicSize=4.25f;var fresh=Capture(f.Camera);
            Assert.That(Get(a,"projection"),Is.EqualTo(old));Assert.That(Get(fresh,"projection"),Is.EqualTo(f.Camera.projectionMatrix));Assert.That(Get(fresh,"projection"),Is.Not.EqualTo(old));
            var slot=Acquire(f.Camera,fresh);var method=Find("CoreGuardianMaskPage").GetMethods(All).Single(m=>m.Name=="IsValid"&&m.GetParameters().Length==2);
            Assert.That(method.Invoke(null,new[]{slot,b}),Is.False);
        }
    }
    [Test]
    public void ResourceCallbackCameraChange_IsCapturedAfterCallback()
    {
        using(var f=new Fixture())
        {
            Capture(f.Camera);var owner=f.Owner;var probe=owner.gameObject.AddComponent<CoreGuardianMaskCameraResourceProbe>();probe.camera=f.Camera;
            f.Show(owner);Assert.That(probe.calls,Is.EqualTo(1));
            var request=Get(Get(Get(owner,"maskBinding"),"current"),"request");
            Assert.That(Get(request,"projection"),Is.EqualTo(f.Camera.projectionMatrix));
            Assert.That(f.Camera.orthographicSize,Is.EqualTo(4.25f));
        }
    }
}

[ExecuteAlways]
public class CoreGuardianMaskCameraResourceProbe:MonoBehaviour
{
    public Camera camera;public int calls;
    void OnTransformChildrenChanged(){if(camera!=null&&calls==0&&transform.Find("SelectionOutline")!=null){calls++;camera.orthographicSize=4.25f;}}
}
