using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CoreGuardianSelectionOutlineTests
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    [TestCase(false)]
    [TestCase(true)]
    public void GpuMaskUnitesOverlappingParts_AndOutlineLeavesInteriorClear(bool asymmetric)
    {
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianSelectionOutline")).First(t=>t!=null);
        var root=new GameObject("Outline GPU test");var cameraRoot=new GameObject("Outline test camera");
        var texture=new Texture2D(8,8,TextureFormat.RGBA32,false);texture.SetPixels(Enumerable.Repeat(Color.white,64).ToArray());texture.Apply();
        var sprite=Sprite.Create(texture,new Rect(0,0,8,8),Vector2.one*.5f,16,0,SpriteMeshType.FullRect);
        var target=new GameObject("2D Sprite");target.transform.SetParent(root.transform,false);target.transform.position=new Vector3(10000,10000,0);
        var first=target.AddComponent<SpriteRenderer>();first.sprite=sprite;first.enabled=false;
        foreach(float offset in new[]{-.125f,.125f})
        {var p=new GameObject("Part").AddComponent<SpriteRenderer>();p.transform.SetParent(target.transform,false);p.transform.localPosition=new Vector3(offset,asymmetric&&offset>0?.125f:0,0);p.sprite=sprite;}
        var camera=cameraRoot.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=1;camera.aspect=1;
        camera.transform.position=target.transform.position+Vector3.back*10;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
        camera.cullingMask=1<<31;
        var image=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32);image.Create();camera.targetTexture=image;
        Texture2D read=null,maskRead=null;var old=RenderTexture.active;
        try
        {
            var component=root.AddComponent(type);type.GetField("color").SetValue(component,Color.magenta);type.GetField("widthPixels").SetValue(component,2);
            type.GetMethod("Present").Invoke(component,new object[]{target.transform,camera});
            var mask=(RenderTexture)type.GetField("mask",Hidden).GetValue(component);
            Assert.That(mask,Is.Not.Null);RenderTexture.active=mask;maskRead=new Texture2D(mask.width,mask.height,TextureFormat.RGBA32,false);
            maskRead.ReadPixels(new Rect(0,0,mask.width,mask.height),0,0);maskRead.Apply();
            int filled=maskRead.GetPixels().Count(c=>c.r>.5f);
            int expected=asymmetric?1664:1536;
            Assert.That(filled,Is.EqualTo(expected),"Two overlapping 32px squares must form one exact binary union");
            Assert.That(maskRead.GetPixel(mask.width/2,mask.height/2).r,Is.GreaterThan(.5f),"Union center must be solid");
            var output=(MeshRenderer)type.GetField("output",Hidden).GetValue(component);output.gameObject.layer=31;
            camera.Render();RenderTexture.active=image;read=new Texture2D(128,128,TextureFormat.RGBA32,false);read.ReadPixels(new Rect(0,0,128,128),0,0);read.Apply();
            Assert.That(read.GetPixels().Count(c=>c.r>.5f&&c.b>.5f),Is.GreaterThan(250),"Actual renderer must draw a visible ring");
            bool Inside(int x,int y)=>(x>=40&&x<72&&y>=48&&y<80)||(x>=56&&x<88&&y>=(asymmetric?56:48)&&y<(asymmetric?88:80));
            for(int y=30;y<105;y++)for(int x=30;x<105;x++)
            {
                bool edge=false;
                if(!Inside(x,y))for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)edge|=Inside(x+dx,y+dy);
                Assert.That(read.GetPixel(x,y).a>.5f,Is.EqualTo(edge),"Screen ring must match the exact union at "+x+","+y+"; no inversion or interior joins");
            }
            Assert.That(first.enabled,Is.False);Assert.That(first.sprite,Is.SameAs(sprite));
            type.GetField("widthPixels").SetValue(component,0);type.GetMethod("Present").Invoke(component,new object[]{target.transform,camera});
            Assert.That(type.GetProperty("IsVisible").GetValue(component),Is.EqualTo(false));
        }
        finally
        {
            RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraRoot);
            image.Release();UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(sprite);UnityEngine.Object.DestroyImmediate(texture);
            if(read!=null)UnityEngine.Object.DestroyImmediate(read);if(maskRead!=null)UnityEngine.Object.DestroyImmediate(maskRead);
        }
    }
}
