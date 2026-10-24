using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CoreGuardianMaskCameraFlushPlayModeTests
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
    static Type Find(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    [UnityTest]
    public IEnumerator CameraChangeBeforeFlush_RendersQueuedOriginalToOwnedTexture()
    {
        yield return null;
        var type=Find("CoreGuardianSelectionOutline");var cache=Find("CoreGuardianBodyMaskCache");var page=Find("CoreGuardianMaskPage");
        var root=new GameObject("Captured production fixture");var host=new GameObject("Captured production owner");var cameraRoot=new GameObject("Captured production camera");
        Texture2D texture=null;Sprite sprite=null;RenderTexture image=null;Component owner=null;Camera camera=null;
        try
        {
            texture=new Texture2D(8,8,TextureFormat.RGBA32,false);texture.SetPixels(Enumerable.Repeat(Color.white,64).ToArray());texture.Apply();
            sprite=Sprite.Create(texture,new Rect(0,0,8,8),Vector2.one*.5f,16,0,SpriteMeshType.FullRect);
            var body=new GameObject("2D Sprite").AddComponent<SpriteRenderer>();body.transform.SetParent(root.transform,false);body.sprite=sprite;
            camera=cameraRoot.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=8.5f;camera.aspect=1920f/1080;camera.transform.position=Vector3.back*10;
            image=new RenderTexture(1920,1080,24);image.Create();camera.targetTexture=image;
            owner=host.AddComponent(type);((Behaviour)owner).enabled=false;type.GetField("widthPixels").SetValue(owner,1);
            type.GetMethod("Present").Invoke(owner,new object[]{root.transform,camera});
            var binding=type.GetField("maskBinding",Hidden).GetValue(owner);var record=binding.GetType().GetField("current",Hidden).GetValue(binding);
            Assert.That((bool)record.GetType().GetField("pending",Hidden).GetValue(record),Is.True,"Must exercise the real runtime QueuePage path, not direct fallback");
            var original=(RenderTexture)record.GetType().GetField("texture",Hidden).GetValue(record);
            camera.transform.position+=Vector3.right*.1f;
            cache.GetMethod("Flush",Static).Invoke(null,new object[]{camera});
            Assert.That((bool)record.GetType().GetField("pending",Hidden).GetValue(record),Is.False);
            var previous=RenderTexture.active;var read=new Texture2D(original.width,original.height,TextureFormat.RGBA32,false);
            try
            {
                RenderTexture.active=original;read.ReadPixels(new Rect(0,0,original.width,original.height),0,0);read.Apply();
                Assert.That(read.GetPixels32().Count(p=>p.r>128),Is.GreaterThan(0),"Camera mutation must invalidate the page and preserve queued original rendering");
            }
            finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(read);}
            type.GetMethod("ReleasePresentation",Hidden).Invoke(owner,null);
        }
        finally
        {
            if(owner!=null)type.GetMethod("ReleaseResources",Hidden).Invoke(owner,null);
            if(camera!=null){page.GetMethod("ReleaseCamera").Invoke(null,new object[]{camera});camera.targetTexture=null;}
            UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraRoot);
            if(image!=null){image.Release();UnityEngine.Object.DestroyImmediate(image);}if(sprite!=null)UnityEngine.Object.DestroyImmediate(sprite);if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
