using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class CoreGuardianAttackBodyBatchTests
{
    static Type Batch => AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianAttackBodyBatch")).FirstOrDefault(t=>t!=null);
    static object Call(string name,params object[] args)=>Batch.GetMethod(name,BindingFlags.Public|BindingFlags.Static).Invoke(null,args);
    static int Count(string name)=>(int)Batch.GetProperty(name).GetValue(null);
    GameObject actor,cameraObject,foreign;
    Camera camera;
    SpriteRenderer[] parts;
    [SetUp] public void Setup()
    {
        Assert.That(Batch,Is.Not.Null,"Attack side-walk batch implementation is missing");
        Call("Clear");
        cameraObject=new GameObject("Attack batch test camera");camera=cameraObject.AddComponent<Camera>();
        camera.orthographic=true;camera.orthographicSize=8.5f;camera.transform.position=new Vector3(0,0,-10);
    }
    void Actor(string role="Attack")
    {
        actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Units/enemy_"+role.ToLowerInvariant()+".prefab"));
        var art=actor.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianOriginalWalk")).First(t=>t!=null);
        var walk=Activator.CreateInstance(type,new object[]{art,role});
        type.GetMethod("Move").Invoke(walk,new object[]{Vector2.right,.1f});type.GetMethod("Present").Invoke(walk,new object[]{.1f});
        parts=art.transform.Find("OriginalWalk").GetComponentsInChildren<SpriteRenderer>();
    }
    [TearDown] public void Cleanup()
    {
        if(Batch!=null)Call("Clear");
        if(actor!=null)UnityEngine.Object.DestroyImmediate(actor);
        if(cameraObject!=null)UnityEngine.Object.DestroyImmediate(cameraObject);
        if(foreign!=null)UnityEngine.Object.DestroyImmediate(foreign);
    }
    [Test] public void CandidateStartsOff_OriginalSourcesStayVisible()
    {
        Actor();Call("Flush",camera);
        Assert.That(Count("LastSources"),Is.Zero);Assert.That(parts.All(r=>!r.forceRenderingOff),Is.True);
    }
    [Test] public void FivePartsShareCarrierAndMaterial_KeepNativeRenderersAndRestoreInputs()
    {
        Actor();var materials=parts.Select(r=>r.sharedMaterial).ToArray();var sprites=parts.Select(r=>r.sprite).ToArray();var orders=parts.Select(r=>r.sortingOrder).ToArray();
        Call("SetEnabled",true);Call("Flush",camera);
        Assert.That(Count("LastSources"),Is.EqualTo(5));
        Assert.That(parts.All(r=>r.enabled&&!r.forceRenderingOff),Is.True,"Unity retains every original renderer and its sorting identity");
        CollectionAssert.AreEqual(sprites,parts.Select(r=>r.sprite).ToArray(),"Retain original Sprite identities as well as Renderer identities");
        var boundTextures=parts.Select(r=>{var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);return block.GetTexture("_MainTex");}).ToArray();
        Assert.That(boundTextures.All(t=>t!=null),Is.True);Assert.That(boundTextures.Distinct().Count(),Is.EqualTo(1));
        Assert.That(parts.Select(r=>r.sharedMaterial).Distinct().Count(),Is.EqualTo(1));
        CollectionAssert.AreEqual(orders,parts.Select(r=>r.sortingOrder).ToArray());
        Call("Restore");Assert.That(parts.All(r=>!r.forceRenderingOff),Is.True);
        CollectionAssert.AreEqual(materials,parts.Select(r=>r.sharedMaterial).ToArray());
        CollectionAssert.AreEqual(sprites,parts.Select(r=>r.sprite).ToArray());
    }
    [Test] public void ForeignRendererKeepsItsInputsAtSameSortKey()
    {
        Actor();foreign=new GameObject("Foreign transparent renderer");var renderer=foreign.AddComponent<SpriteRenderer>();
        renderer.sprite=parts[0].sprite;renderer.sharedMaterial=parts[0].sharedMaterial;
        renderer.transform.position=parts[0].bounds.center;renderer.transform.localScale=Vector3.one*10;
        renderer.sortingLayerID=parts[0].sortingLayerID;renderer.sortingOrder=parts[0].sortingOrder;
        var sprite=renderer.sprite;var material=renderer.sharedMaterial;int order=renderer.sortingOrder;
        Call("SetEnabled",true);Call("Flush",camera);
        Assert.That(renderer.forceRenderingOff,Is.False);Assert.That(renderer.sprite,Is.SameAs(sprite));
        Assert.That(renderer.sharedMaterial,Is.SameAs(material));Assert.That(renderer.sortingOrder,Is.EqualTo(order));
    }
    [Test] public void BuffWalkAndPropertyOverridesKeepOriginalRendering()
    {
        Actor("Buff");Call("SetEnabled",true);Call("Flush",camera);Assert.That(Count("LastSources"),Is.Zero);
        UnityEngine.Object.DestroyImmediate(actor);actor=null;Call("Clear");Actor();
        foreach(var part in parts){var properties=new MaterialPropertyBlock();properties.SetFloat("_ExternalOwner",1);part.SetPropertyBlock(properties);}
        Call("SetEnabled",true);Call("Flush",camera);Assert.That(Count("LastSources"),Is.Zero);
    }
    [TestCase(1f,1f,1f)]
    [TestCase(.8f,1f,.9f)]
    [TestCase(1.15f,1.15f,1.15f)]
    public void SideLegOverlapsMatchOriginalPixelsAtNativePartOrder(float red,float green,float blue)
    {
        Actor();foreach(var t in actor.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
        foreach(var part in parts)part.color=new Color(red,green,blue,1);
        camera.scene=actor.scene;camera.cullingMask=1<<31;camera.enabled=false;camera.orthographicSize=2;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.17f,.19f,.21f,1);
        var art=actor.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");
        camera.transform.position=new Vector3(art.bounds.center.x,art.bounds.center.y,-10);
        var previous=RenderTexture.active;var rt=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);rt.Create();
        var read=new Texture2D(256,256,TextureFormat.RGBA32,false);camera.targetTexture=rt;
        try
        {
            camera.Render();var original=Read();
            Assert.That(original.Count(c=>!c.Equals(original[0])),Is.GreaterThan(20),"Original render must contain visible art");
            Call("SetEnabled",true);Call("Flush",camera);Assert.That(Count("LastSources"),Is.EqualTo(5));
            camera.Render();var candidate=Read();
            Assert.That(original.Where((c,i)=>!c.Equals(candidate[i])).Count(),Is.Zero,"Combined part order must preserve overlapping leg pixels");
        }
        finally{Call("Restore");camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(read);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
        Color32[] Read(){RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,256,256),0,0);read.Apply();return read.GetPixels32();}
    }
}
