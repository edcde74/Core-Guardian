using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CoreGuardian;

public class CoreGuardianSpriteAtlasTests
{
    static Type Atlas
    {get{var t=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianSpriteAtlas")).FirstOrDefault(x=>x!=null);Assert.That(t,Is.Not.Null,"Render-scoped sprite atlas is missing");return t;}}
    static bool Map(Sprite source,out Sprite mapped)
    {var args=new object[]{source,null};bool ok=(bool)Atlas.GetMethod("TryMap").Invoke(null,args);mapped=(Sprite)args[1];return ok;}
    static void Release(){var t=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianSpriteAtlas")).FirstOrDefault(x=>x!=null);t?.GetMethod("Release").Invoke(null,null);}
    sealed class View:IDisposable
    {
        public readonly GameObject root=new GameObject("Atlas pixel fixture"),cameraRoot=new GameObject("Atlas pixel camera");
        public readonly SpriteRenderer renderer;readonly Camera camera;readonly RenderTexture target;readonly Texture2D read;readonly Material material;readonly RenderTexture old;
        public View()
        {
            old=RenderTexture.active;root.layer=31;root.transform.position=new Vector3(10000,10000,0);renderer=root.AddComponent<SpriteRenderer>();
            material=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));renderer.sharedMaterial=material;
            camera=cameraRoot.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=1;camera.transform.position=new Vector3(10000,10000,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<31;
            target=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;read=new Texture2D(128,128,TextureFormat.RGBA32,false);
        }
        public Color32[] Capture(Sprite sprite){renderer.sprite=sprite;camera.Render();RenderTexture.active=target;read.ReadPixels(new Rect(0,0,128,128),0,0);read.Apply();return read.GetPixels32();}
        public void Same(Sprite original,Sprite mapped)
        {var before=Capture(original);Assert.That(before.Any(p=>p.a>0),Is.True,"Fixture must render visible pixels");Assert.That(Capture(mapped),Is.EqualTo(before),"Atlas must preserve every RGBA pixel: "+original.name+" -> "+mapped.rect);}
        public void Dispose(){renderer.sprite=null;Release();RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraRoot);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(read);UnityEngine.Object.DestroyImmediate(material);}
    }
    static Texture2D Texture(Color32 color)
    {var t=new Texture2D(16,16,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};t.SetPixels32(Enumerable.Repeat(color,256).ToArray());t.Apply(false,true);return t;}

    [Test]
    public void CutoutRectAndFlipRenderExactly()
    {
        var texture=Texture(new Color32(31,137,223,177));var sprite=Sprite.Create(texture,new Rect(2,3,6,8),new Vector2(.3f,.2f),16,0,SpriteMeshType.FullRect);
        try{using(var view=new View()){Assert.That(Map(sprite,out var mapped),Is.True);Assert.That(mapped.pixelsPerUnit,Is.EqualTo(16));Assert.That(mapped.pivot,Is.EqualTo(sprite.pivot));foreach(bool flip in new[]{false,true}){view.renderer.flipX=flip;view.Same(sprite,mapped);}}}
        finally{Release();UnityEngine.Object.DestroyImmediate(sprite);UnityEngine.Object.DestroyImmediate(texture);}
    }
    [Test]
    public void AtlasPointSamplingHasNoNeighbourBleed()
    {
        var a=Texture(new Color32(255,0,71,255));var b=Texture(new Color32(0,255,0,255));
        var first=Sprite.Create(a,new Rect(0,0,16,16),Vector2.one*.5f,16);var second=Sprite.Create(b,new Rect(0,0,16,16),Vector2.one*.5f,16);
        try{using(var view=new View()){Assert.That(Map(first,out var mapped),Is.True);Assert.That(Map(second,out var neighbour),Is.True);Assert.That(neighbour.texture,Is.SameAs(mapped.texture));Assert.That(mapped.texture.filterMode,Is.EqualTo(FilterMode.Point));view.Same(first,mapped);view.Same(second,neighbour);}}
        finally{Release();foreach(var obj in new UnityEngine.Object[]{first,second,a,b})UnityEngine.Object.DestroyImmediate(obj);}
    }
    [Test]
    public void UnmappedSpriteRetainsOriginal()
    {
        var texture=new Texture2D(2050,1,TextureFormat.RGBA32,false);var sprite=Sprite.Create(texture,new Rect(0,0,2050,1),Vector2.one*.5f,16);
        try{Assert.That(Map(null,out var empty),Is.False);Assert.That(empty,Is.Null);Assert.That(Map(sprite,out var mapped),Is.False);Assert.That(mapped,Is.Null);Assert.That(sprite.texture,Is.SameAs(texture));}
        finally{Release();UnityEngine.Object.DestroyImmediate(sprite);UnityEngine.Object.DestroyImmediate(texture);}
    }
    [Test]
    public void SharedHealthStatesKeepAllFillPixels()
    {
        using(var view=new View())
        {
            var health=view.root.AddComponent<SegmentedEnemyHealthBar>();health.Configure(false);
            for(int filled=0;filled<=8;filled++)
            {health.SetHealth(filled,8);var original=view.renderer.sprite;Assert.That(Map(original,out var mapped),Is.True);view.Same(original,mapped);view.renderer.sprite=original;}
        }
    }
    [Test]
    public void UnsupportedTextureFormatFallsBackWithoutBreakingCameraRendering()
    {
        var texture=new Texture2D(16,16,TextureFormat.RGB24,false){filterMode=FilterMode.Point};
        texture.SetPixels(Enumerable.Repeat(Color.white,256).ToArray());texture.Apply();
        var sprite=Sprite.Create(texture,new Rect(0,0,16,16),Vector2.one*.5f,16,0,SpriteMeshType.FullRect);
        try{Assert.That(Map(sprite,out var mapped),Is.False);Assert.That(mapped,Is.Null);}
        finally{Release();UnityEngine.Object.DestroyImmediate(sprite);UnityEngine.Object.DestroyImmediate(texture);}
    }
}
