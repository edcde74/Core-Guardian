using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CoreGuardianMaskPageTests
{
    static Type Page
    {get{var t=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianMaskPage")).FirstOrDefault(x=>x!=null);Assert.That(t,Is.Not.Null,"Shared GPU mask page allocator is missing");return t;}}
    static object Slot(Camera camera,int frame,int width=100,int height=100)
    {var args=new object[]{camera,frame,width,height,null};Assert.That(Page.GetMethod("TryAcquire").Invoke(null,args),Is.EqualTo(true));return args[4];}
    static bool Valid(object slot)=>(bool)Page.GetMethod("IsValid").Invoke(null,new[]{slot});
    static T Read<T>(object slot,string name)=>(T)slot.GetType().GetProperty(name).GetValue(slot);
    static void Release(Camera camera){var t=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianMaskPage")).FirstOrDefault(x=>x!=null);t?.GetMethod("ReleaseCamera").Invoke(null,new object[]{camera});}
    sealed class View:IDisposable
    {
        public readonly GameObject root=new GameObject("Mask page camera");public readonly Camera camera;
        public View(){camera=root.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=8.5f;camera.pixelRect=new Rect(0,0,1920,1080);}
        public void Dispose(){Release(camera);UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test]
    public void RegionsNeverOverlapAndOverflowUsesAnotherPage()
    {
        using(var view=new View())
        {
            var first=Slot(view.camera,10);var second=Slot(view.camera,10);var large=Slot(view.camera,10,2000,2000);
            Assert.That(Read<RenderTexture>(first,"Texture"),Is.SameAs(Read<RenderTexture>(second,"Texture")));
            Assert.That(Read<RectInt>(first,"Rect").Overlaps(Read<RectInt>(second,"Rect")),Is.False);
            Assert.That(Read<RenderTexture>(large,"Texture"),Is.Not.SameAs(Read<RenderTexture>(first,"Texture")));
            Assert.That(Valid(first)&&Valid(second)&&Valid(large),Is.True);
            var args=new object[]{view.camera,10,2049,1,null};Assert.That(Page.GetMethod("TryAcquire").Invoke(null,args),Is.EqualTo(false));
        }
    }
    [Test]
    public void ReusedSlotRejectsOldGeneration()
    {
        using(var view=new View())
        {
            var old=Slot(view.camera,10);Page.GetMethod("Release").Invoke(null,new[]{old});Assert.That(Valid(old),Is.False);
            var next=Slot(view.camera,11);Assert.That(Valid(next),Is.True);Assert.That(Valid(old),Is.False);
            Assert.That(Read<RectInt>(next,"Rect"),Is.EqualTo(Read<RectInt>(old,"Rect")),"Frame reuse should retain the same packing area without making the old token valid");
        }
    }
    [Test]
    public void CameraFrameAndViewportNeverShareStaleSlot()
    {
        using(var a=new View())using(var b=new View())
        {
            var first=Slot(a.camera,10);var other=Slot(b.camera,10);
            Assert.That(Read<RenderTexture>(first,"Texture"),Is.Not.SameAs(Read<RenderTexture>(other,"Texture")));
            a.camera.rect=new Rect(.1f,0,.9f,1);Assert.That(Valid(first),Is.False);Assert.That(Valid(other),Is.True);
            var next=Slot(a.camera,11);Assert.That(Valid(next),Is.True);Assert.That(Valid(first),Is.False);
        }
    }
    [Test]
    public void TinyCameraMatrixChangeInvalidatesSlot()
    {
        using(var view=new View()){var slot=Slot(view.camera,10);var matrix=view.camera.worldToCameraMatrix;matrix.m03+=.0000001f;view.camera.worldToCameraMatrix=matrix;Assert.That(Valid(slot),Is.False);}
    }
    [Test]
    public void DisableDestroyUnloadReleaseOnlyOwnedResources()
    {
        using(var view=new View())
        {
            var external=new RenderTexture(32,32,0);external.Create();view.camera.targetTexture=external;
            try
            {
                var slot=Slot(view.camera,10);var owned=Read<RenderTexture>(slot,"Texture");Release(view.camera);
                Assert.That(Valid(slot),Is.False);Assert.That(owned==null||!owned.IsCreated(),Is.True);
                Assert.That(external.IsCreated(),Is.True,"Camera target belongs to the caller, never to the mask page allocator");
            }
            finally{view.camera.targetTexture=null;external.Release();UnityEngine.Object.DestroyImmediate(external);}
        }
    }
}
