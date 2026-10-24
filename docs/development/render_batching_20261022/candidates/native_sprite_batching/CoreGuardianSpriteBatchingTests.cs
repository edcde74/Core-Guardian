using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CoreGuardianSpriteBatchingTests
{
    static Type Mapper
    {
        get
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianSpriteBatching")).FirstOrDefault(t=>t!=null);
            Assert.That(type,Is.Not.Null,"Selective sprite batching mapper is missing");return type;
        }
    }
    static Material Map(Material source)=>(Material)Mapper.GetMethod("Acquire").Invoke(null,new object[]{source});
    static IDisposable Apply(Transform root)=>(IDisposable)Mapper.GetMethod("Apply").Invoke(null,new object[]{root});
    static SpriteRenderer Renderer(GameObject root,Material material)
    {var renderer=root.AddComponent<SpriteRenderer>();renderer.sharedMaterial=material;return renderer;}

    [Test]
    public void SupportedUnlitUsesOneSharedMapping_PreservesSourceProperties()
    {
        var root=new GameObject("Batching shared mapping test");
        var material=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        material.SetColor("_Color",new Color(.2f,.4f,.6f,.8f));
        var renderer=Renderer(root,material);renderer.color=new Color(.3f,.5f,.7f,.9f);
        var sourceTint=material.GetColor("_Color");var rendererTint=renderer.color;
        var block=new MaterialPropertyBlock();block.SetFloat("_ExternalOwner",.73f);renderer.SetPropertyBlock(block);
        IDisposable lease=null;
        try
        {
            lease=Apply(root.transform);var mapped=Map(material);
            Assert.That(mapped,Is.Not.SameAs(material));Assert.That(Map(material),Is.SameAs(mapped));
            Assert.That(renderer.sharedMaterial,Is.SameAs(mapped));
            Assert.That(mapped.GetColor("_Color"),Is.EqualTo(sourceTint));
            Assert.That(renderer.color,Is.EqualTo(rendererTint));
            renderer.GetPropertyBlock(block);Assert.That(block.GetFloat("_ExternalOwner"),Is.EqualTo(.73f));
            Assert.That(material.shader.name,Is.EqualTo("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            lease.Dispose();lease=null;Assert.That(renderer.sharedMaterial,Is.SameAs(material));
        }
        finally{lease?.Dispose();UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(material);}
    }

    [Test]
    public void UnknownOrLitMaterialReturnsOriginal()
    {
        var root=new GameObject("Batching fallback test");IDisposable lease=null;
        var lit=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));
        var custom=new Material(Resources.Load<Shader>("CoreGuardian/BodyHitFlash"));
        try{lease=Apply(root.transform);Assert.That(Map(lit),Is.SameAs(lit));Assert.That(Map(custom),Is.SameAs(custom));Assert.That(Map(null),Is.Null);}
        finally{lease?.Dispose();UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(lit);UnityEngine.Object.DestroyImmediate(custom);}
    }

    [Test]
    public void HitFlashThenRestoreRetainsBatchingMaterialAndPropertyBlock()
    {
        var root=new GameObject("Batching hit restoration test");
        var material=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        var renderer=Renderer(root,material);IDisposable lease=null;
        try
        {
            lease=Apply(root.transform);var mapped=renderer.sharedMaterial;
            var block=new MaterialPropertyBlock();block.SetFloat("_ExternalOwner",.31f);renderer.SetPropertyBlock(block);
            var flashType=Mapper.Assembly.GetType("CoreGuardianHitFlash");
            var flash=Activator.CreateInstance(flashType,new object[]{renderer});
            Assert.That(flashType.GetMethod("Trigger").Invoke(flash,null),Is.EqualTo(true));
            Assert.That(renderer.sharedMaterial,Is.Not.SameAs(mapped));
            flashType.GetMethod("Advance").Invoke(flash,new object[]{1f});
            Assert.That(renderer.sharedMaterial,Is.SameAs(mapped));
            renderer.GetPropertyBlock(block);Assert.That(block.GetFloat("_ExternalOwner"),Is.EqualTo(.31f));
        }
        finally{lease?.Dispose();UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(material);}
    }

    [Test]
    public void ReleaseDoesNotOverwriteNewOwnerMaterial()
    {
        var root=new GameObject("Batching external owner test");
        var original=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        var replacement=new Material(Resources.Load<Shader>("CoreGuardian/BodyHitFlash"));
        var renderer=Renderer(root,original);IDisposable lease=null;
        try{lease=Apply(root.transform);renderer.sharedMaterial=replacement;lease.Dispose();lease=null;Assert.That(renderer.sharedMaterial,Is.SameAs(replacement));}
        finally{lease?.Dispose();UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(original);UnityEngine.Object.DestroyImmediate(replacement);}
    }
    [Test]
    public void MultipleSpritesKeepOriginalColorFlipAndCutoutWhenBatched()
    {
        var root=new GameObject("Multi-sprite batch pixels");var cameraRoot=new GameObject("Multi-sprite batch camera");
        var material=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        var texture=new Texture2D(16,16,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};
        var pixels=new Color32[256];for(int y=0;y<16;y++)for(int x=0;x<16;x++)pixels[y*16+x]=new Color32((byte)(x*15),(byte)(y*15),170,(byte)(x<3?0:255));texture.SetPixels32(pixels);texture.Apply();
        var whole=Sprite.Create(texture,new Rect(0,0,16,16),Vector2.one*.5f,16,0,SpriteMeshType.FullRect);
        var cut=Sprite.Create(texture,new Rect(3,2,8,10),new Vector2(.3f,.7f),16,0,SpriteMeshType.FullRect);
        var camera=cameraRoot.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=2;camera.transform.position=Vector3.back*10;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
        var target=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;var read=new Texture2D(128,128,TextureFormat.RGBA32,false);var previous=RenderTexture.active;IDisposable lease=null;
        try
        {
            for(int i=0;i<6;i++)
            {var child=new GameObject("Batch sprite "+i);child.transform.SetParent(root.transform,false);child.layer=31;child.transform.localPosition=new Vector3(i%3-1,i/3-.5f,0);var renderer=child.AddComponent<SpriteRenderer>();renderer.sharedMaterial=material;renderer.sprite=i%2==0?whole:cut;renderer.flipX=i%3==0;renderer.color=new Color(.25f+i*.1f,.8f,.9f-i*.1f,.7f);}
            Color32[] Capture(){camera.Render();RenderTexture.active=target;read.ReadPixels(new Rect(0,0,128,128),0,0);read.Apply();return read.GetPixels32();}
            var before=Capture();Assert.That(before.Any(p=>p.a>0),Is.True);lease=Apply(root.transform);
            Assert.That(Capture(),Is.EqualTo(before),"Multiple SpriteRenderers must retain their original GPU colors, UVs, flips and alpha when the engine batches them");
        }
        finally{lease?.Dispose();RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraRoot);target.Release();foreach(var obj in new UnityEngine.Object[]{target,read,material,whole,cut,texture})UnityEngine.Object.DestroyImmediate(obj);}
    }
}
