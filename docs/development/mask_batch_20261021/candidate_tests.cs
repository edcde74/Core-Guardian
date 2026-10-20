using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CoreGuardianSelectionOutlineTests
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    [Test]
    public void SharedTexturePartsReduceSubmissions_WithoutCrossingOpacityOrTextureBoundaries()
    {
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianSelectionOutline")).First(t=>t!=null);
        var root=new GameObject("Mask batch contract");var cameraRoot=new GameObject("Mask batch camera");
        var texture=new Texture2D(4,4,TextureFormat.RGBA32,false);texture.SetPixels(Enumerable.Repeat(Color.white,16).ToArray());texture.Apply();
        var other=new Texture2D(4,4,TextureFormat.RGBA32,false);other.SetPixels(Enumerable.Repeat(Color.white,16).ToArray());other.Apply();
        var sprite=Sprite.Create(texture,new Rect(0,0,4,4),Vector2.one*.5f,8);
        var secondSprite=Sprite.Create(other,new Rect(0,0,4,4),Vector2.one*.5f,8);
        try
        {
            root.transform.position=new Vector3(10000,10000,0);
            var art=new GameObject("2D Sprite").AddComponent<SpriteRenderer>();art.transform.SetParent(root.transform,false);art.enabled=false;
            var parts=new SpriteRenderer[3];for(int i=0;i<3;i++){parts[i]=new GameObject("Part"+i).AddComponent<SpriteRenderer>();parts[i].transform.SetParent(art.transform,false);parts[i].transform.localPosition=Vector3.right*(i-1)*.125f;parts[i].sprite=sprite;}
            var camera=cameraRoot.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=1;camera.transform.position=root.transform.position+Vector3.back*10;
            var image=new RenderTexture(128,128,0);image.Create();camera.targetTexture=image;
            try
            {
                var owner=root.AddComponent(type);var field=type.GetField("lastMaskDrawCalls",Hidden);
                Assert.That(field,Is.Not.Null,"The actual submitted mask draw count must be observable for the optimization contract");
                int Draws(){type.GetMethod("Present").Invoke(owner,new object[]{root.transform,camera});return (int)field.GetValue(owner);}
                Assert.That(Draws(),Is.EqualTo(1),"Three same-texture/same-opacity parts must submit one draw");
                parts[2].color=new Color(1,1,1,.5f);Assert.That(Draws(),Is.EqualTo(2),"Opacity is a mask input and cannot be merged across values");
                parts[1].sprite=secondSprite;Assert.That(Draws(),Is.EqualTo(3),"Different textures remain separate");
                parts[2].gameObject.SetActive(false);Assert.That(Draws(),Is.EqualTo(2));
                parts[1].sprite=sprite;Assert.That(Draws(),Is.EqualTo(1),"The reused group must not retain the old texture or inactive part");
                Assert.That(parts[0].sprite,Is.SameAs(sprite));Assert.That(parts[2].color.a,Is.EqualTo(.5f));
                type.GetMethod("Present").Invoke(owner,new object[]{null,camera});Assert.That((int)field.GetValue(owner),Is.Zero);
            }
            finally{camera.targetTexture=null;image.Release();UnityEngine.Object.DestroyImmediate(image);}
        }
        finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraRoot);UnityEngine.Object.DestroyImmediate(sprite);UnityEngine.Object.DestroyImmediate(secondSprite);UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(other);}
    }
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
        Texture2D read=null,maskRead=null;Material scan=null,contact=null;var old=RenderTexture.active;
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
            void AssertSamePixelsAfterGrowth(Material material)
            {
                void Present()
                {
                    if(material==null)type.GetMethod("Present").Invoke(component,new object[]{target.transform,camera});
                    else type.GetMethod("PresentOverlay").Invoke(component,new object[]{target.transform,camera,material});
                }
                Present();camera.Render();RenderTexture.active=image;read.ReadPixels(new Rect(0,0,128,128),0,0);read.Apply();
                var before=read.GetPixels32();
                target.transform.localScale=Vector3.one*1.4f;Present();
                var capacity=(RenderTexture)type.GetField("mask",Hidden).GetValue(component);
                target.transform.localScale=Vector3.one;Present();
                Assert.That(type.GetField("mask",Hidden).GetValue(component),Is.SameAs(capacity),"Smaller pose must reuse RT capacity");
                camera.Render();RenderTexture.active=image;read.ReadPixels(new Rect(0,0,128,128),0,0);read.Apply();
                Assert.That(read.GetPixels32(),Is.EqualTo(before),"Growth and reuse must preserve every output pixel");
            }
            AssertSamePixelsAfterGrowth(null);
            contact=new Material(Resources.Load<Shader>("CoreGuardian/PhotoShadowContact"));contact.SetFloat("_Band",16);
            AssertSamePixelsAfterGrowth(contact);
            scan=new Material(Resources.Load<Shader>("CoreGuardian/UpgradeScan"));scan.SetFloat("_BandY",target.transform.position.y);scan.SetFloat("_Opacity",1);
            AssertSamePixelsAfterGrowth(scan);
            Assert.That(type.GetMethod("PresentOverlay").Invoke(component,new object[]{target.transform,camera,scan}),Is.EqualTo(true));
            camera.Render();RenderTexture.active=image;read.ReadPixels(new Rect(0,0,128,128),0,0);read.Apply();
            int cyan=0;
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)if(read.GetPixel(x,y).a>.01f)
            {
                Assert.That(Inside(x,y),Is.True,"Upgrade scan cannot illuminate shadows, transparent margins, or outside the union");
                Assert.That(y,Is.InRange(59,68),"Scan is a short band, not a full-body flash");cyan++;
            }
            Assert.That(cyan,Is.GreaterThan(150),"The chosen B shader must actually render a band inside the body");
            type.GetField("widthPixels").SetValue(component,0);type.GetMethod("Present").Invoke(component,new object[]{target.transform,camera});
            Assert.That(type.GetProperty("IsVisible").GetValue(component),Is.EqualTo(false));
        }
        finally
        {
            RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraRoot);
            image.Release();UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(sprite);UnityEngine.Object.DestroyImmediate(texture);
            if(read!=null)UnityEngine.Object.DestroyImmediate(read);if(maskRead!=null)UnityEngine.Object.DestroyImmediate(maskRead);
            if(scan!=null)UnityEngine.Object.DestroyImmediate(scan);
            if(contact!=null)UnityEngine.Object.DestroyImmediate(contact);
        }
    }

    [Test]
    public void CachedPartsIncludeInactiveDirections_AndRefreshWhenHierarchyChanges()
    {
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianSelectionOutline")).First(t=>t!=null);
        var root=new GameObject("Cached parts test");var cameraRoot=new GameObject("Cache camera");
        var texture=new Texture2D(2,2);var sprite=Sprite.Create(texture,new Rect(0,0,2,2),Vector2.one*.5f,4);
        try
        {
            root.transform.position=new Vector3(10000,10000,0);
            var body=new GameObject("2D Sprite").AddComponent<SpriteRenderer>();body.transform.SetParent(root.transform,false);body.sprite=sprite;
            var part=new GameObject("Inactive direction").AddComponent<SpriteRenderer>();part.transform.SetParent(body.transform,false);part.sprite=sprite;part.gameObject.SetActive(false);
            var camera=cameraRoot.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.transform.position=root.transform.position+Vector3.back*10;
            var c=root.AddComponent(type);var present=type.GetMethod("Present");
            int Count(){present.Invoke(c,new object[]{root.transform,camera});return ((System.Collections.ICollection)type.GetField("sources",Hidden).GetValue(c)).Count;}
            Assert.That(Count(),Is.EqualTo(1));part.gameObject.SetActive(true);Assert.That(Count(),Is.EqualTo(2));
            part.enabled=false;Assert.That(Count(),Is.EqualTo(1));part.enabled=true;part.color=new Color(1,1,1,.01f);Assert.That(Count(),Is.EqualTo(1));
            part.color=Color.white;part.sprite=null;Assert.That(Count(),Is.EqualTo(1));part.sprite=sprite;
            var added=new GameObject("New rig part").AddComponent<SpriteRenderer>();added.transform.SetParent(part.transform,false);added.sprite=sprite;
            Assert.That(Count(),Is.EqualTo(3));added.transform.SetParent(cameraRoot.transform,false);Assert.That(Count(),Is.EqualTo(2));
            UnityEngine.Object.DestroyImmediate(part.gameObject);Assert.That(Count(),Is.EqualTo(1));
            // Adding only a renderer on an existing node uses the explicit invalidation contract.
            var node=new GameObject("Existing rig node");node.transform.SetParent(body.transform,false);Assert.That(Count(),Is.EqualTo(1));
            node.AddComponent<SpriteRenderer>().sprite=sprite;type.GetMethod("InvalidateSources").Invoke(c,null);Assert.That(Count(),Is.EqualTo(2));
        }
        finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraRoot);UnityEngine.Object.DestroyImmediate(sprite);UnityEngine.Object.DestroyImmediate(texture);}
    }
}
