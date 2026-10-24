using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class CoreGuardianObjectEmphasisTests
{
    [Test]
    public void SuppliedBodySourcesMatchIndependentOutlineAcrossVisibilityChanges()
    {
        var type=Find("CoreGuardianSelectionOutline");
        var shared=type.GetMethod("PresentAtOrderWithSources",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        Assert.That(shared,Is.Not.Null,"Emphasis must reuse its validated body references for the mask consumer");
        var actor=new GameObject("Shared body");var host=new GameObject("Shared outline");var legacy=new GameObject("Independent outline");var cameraGo=new GameObject("Camera");
        var texture=new Texture2D(2,2);texture.SetPixels(Enumerable.Repeat(Color.white,4).ToArray());texture.Apply();
        var sprite=Sprite.Create(texture,new Rect(0,0,2,2),Vector2.one*.5f,4);
        try
        {
            var body=new GameObject("2D Sprite").AddComponent<SpriteRenderer>();body.transform.SetParent(actor.transform,false);body.sprite=sprite;
            var child=new GameObject("Moving part").AddComponent<SpriteRenderer>();child.transform.SetParent(body.transform,false);child.transform.localPosition=Vector3.right;child.sprite=sprite;
            var camera=cameraGo.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.transform.position=Vector3.back*10;
            var supplied=host.AddComponent(type);var independent=legacy.AddComponent(type);
            var sources=new List<SpriteRenderer>{body,child};
            for(int state=0;state<4;state++)
            {
                body.enabled=state!=1;child.gameObject.SetActive(state!=2);
                if(state==3){child.transform.SetParent(actor.transform,false);sources.Remove(child);}
                shared.Invoke(supplied,new object[]{actor.transform,camera,3,body.transform,sources});
                type.GetMethod("PresentAtOrder").Invoke(independent,new object[]{actor.transform,camera,3});
                Assert.That(type.GetProperty("TargetBounds").GetValue(supplied),Is.EqualTo(type.GetProperty("TargetBounds").GetValue(independent)),"state "+state);
                var a=host.GetComponentInChildren<MeshRenderer>();var b=legacy.GetComponentInChildren<MeshRenderer>();
                Assert.That(a.enabled,Is.EqualTo(b.enabled));Assert.That(a.sortingOrder,Is.EqualTo(b.sortingOrder));
                CollectionAssert.AreEqual(a.GetComponent<MeshFilter>().sharedMesh.vertices,b.GetComponent<MeshFilter>().sharedMesh.vertices);
            }
        }
        finally{DestroyOwned(host);DestroyOwned(legacy);UnityEngine.Object.DestroyImmediate(actor);UnityEngine.Object.DestroyImmediate(cameraGo);UnityEngine.Object.DestroyImmediate(sprite);UnityEngine.Object.DestroyImmediate(texture);}
    }
    static Type Find(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).FirstOrDefault(t=>t!=null);
    static void DestroyOwned(GameObject root)
    {
        if(root==null)return;
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var emphasis=root.GetComponent(Find("CoreGuardianObjectEmphasis"));if(emphasis!=null)emphasis.GetType().GetMethod("OnDestroy",flags).Invoke(emphasis,null);
        foreach(var owner in root.GetComponentsInChildren<MonoBehaviour>(true))
            if(owner.GetType()==Find("CoreGuardianSelectionOutline"))owner.GetType().GetMethod("OnDestroy",flags).Invoke(owner,null);
        UnityEngine.Object.DestroyImmediate(root);
    }
    static void Begin(Component component,Camera camera)=>component.GetType().GetMethod("BeginCameraRendering",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(component,new object[]{default(UnityEngine.Rendering.ScriptableRenderContext),camera});
    [Test]
    public void RenderScopedBrightnessDoesNotFeedBackIntoIdleToAttackCutouts()
    {
        var type=Find("CoreGuardianObjectEmphasis");var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var begin=type.GetMethod("BeginCameraRendering",flags);var end=type.GetMethod("EndCameraRendering",flags);
        Assert.That(begin,Is.Not.Null,"Brightness must be scoped to rendering before animations can copy original tint");Assert.That(end,Is.Not.Null);
        var host=new GameObject("Transition emphasis");var target=new GameObject("Transition unit");var cameraGo=new GameObject("Transition camera");
        var texture=new Texture2D(2,2);texture.SetPixels(Enumerable.Repeat(Color.white,4).ToArray());texture.Apply();var sprite=Sprite.Create(texture,new Rect(0,0,2,2),Vector2.one*.5f,4);
        try {
            target.transform.position=new Vector3(10000,10000,0);var original=new GameObject("2D Sprite").AddComponent<SpriteRenderer>();original.transform.SetParent(target.transform,false);original.sprite=sprite;
            var cutout=new GameObject("Firing cutout").AddComponent<SpriteRenderer>();cutout.transform.SetParent(original.transform,false);cutout.sprite=sprite;cutout.enabled=false;
            var camera=cameraGo.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.transform.position=target.transform.position+Vector3.back*10;
            var component=host.AddComponent(type);var present=type.GetMethod("PresentTargets");var args=new object[]{default(UnityEngine.Rendering.ScriptableRenderContext),camera};
            present.Invoke(component,new object[]{new List<Transform>{target.transform},camera});begin.Invoke(component,args);Assert.That(original.color.r,Is.EqualTo(1.15f).Within(.00001f));end.Invoke(component,args);
            Assert.That(original.color,Is.EqualTo(Color.white),"Animation's source tint must already be restored between frames");
            cutout.color=original.color;original.enabled=false;cutout.enabled=true;
            present.Invoke(component,new object[]{new List<Transform>{target.transform},camera});begin.Invoke(component,args);Assert.That(cutout.color.r,Is.EqualTo(1.15f).Within(.00001f),"Copied cutout must not become 1.3225");end.Invoke(component,args);Assert.That(cutout.color,Is.EqualTo(Color.white));
            original.enabled=true;cutout.enabled=false;present.Invoke(component,new object[]{new List<Transform>{target.transform},camera});begin.Invoke(component,args);end.Invoke(component,args);Assert.That(original.color,Is.EqualTo(Color.white));
        } finally {DestroyOwned(host);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraGo);UnityEngine.Object.DestroyImmediate(sprite);UnityEngine.Object.DestroyImmediate(texture);}
    }
    [Test]
    public void BodyBoostDoesNotAccumulate_RespectsNewTint_AndRestoresOnRemoval()
    {
        var type=Find("CoreGuardianObjectEmphasis");Assert.That(type,Is.Not.Null,"Selected object emphasis is not implemented");
        var host=new GameObject("Emphasis test");var target=new GameObject("Test unit");var cameraGo=new GameObject("Emphasis camera");
        var texture=new Texture2D(2,2);texture.SetPixels(Enumerable.Repeat(Color.white,4).ToArray());texture.Apply();
        var sprite=Sprite.Create(texture,new Rect(0,0,2,2),Vector2.one*.5f,4);
        try {
            target.transform.position=new Vector3(10000,10000,0);
            var body=new GameObject("2D Sprite").AddComponent<SpriteRenderer>();body.transform.SetParent(target.transform,false);body.sprite=sprite;
            var shadow=new GameObject("접촉 그림자").AddComponent<SpriteRenderer>();shadow.transform.SetParent(target.transform,false);shadow.sprite=sprite;
            var baseline=new Color(.7f,.6f,.5f,.4f);body.color=baseline;shadow.color=new Color(.1f,.1f,.1f,.3f);var shadowBefore=shadow.color;
            var camera=cameraGo.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=1;camera.transform.position=target.transform.position+Vector3.back*10;
            var component=(Behaviour)host.AddComponent(type);var present=type.GetMethod("PresentTargets");var targets=new List<Transform>{target.transform};
            for(int i=0;i<5;i++){present.Invoke(component,new object[]{targets,camera});Begin(component,camera);}
            Assert.That(body.color.r,Is.EqualTo(baseline.r*1.15f).Within(.00001f),"Repeated presentation must not compound brightness");
            Assert.That(body.color.a,Is.EqualTo(baseline.a));Assert.That(shadow.color,Is.EqualTo(shadowBefore));
            var changed=new Color(.3f,.4f,.5f,.2f);body.color=changed;present.Invoke(component,new object[]{targets,camera});Begin(component,camera);
            Assert.That(body.color.r,Is.EqualTo(changed.r*1.15f).Within(.00001f),"A new animation tint must become the new baseline");
            type.GetField("bodyBrightness").SetValue(component,1.25f);present.Invoke(component,new object[]{targets,camera});Begin(component,camera);
            Assert.That(body.color.r,Is.EqualTo(changed.r*1.25f).Within(.00001f));
            present.Invoke(component,new object[]{new List<Transform>(),camera});Assert.That(body.color,Is.EqualTo(changed));
            present.Invoke(component,new object[]{targets,camera});Begin(component,camera);body.color=Color.red;
            present.Invoke(component,new object[]{new List<Transform>(),camera});Assert.That(body.color,Is.EqualTo(Color.red),"Removal must not overwrite another system's newer tint");Assert.That(shadow.color,Is.EqualTo(shadowBefore));
        } finally {DestroyOwned(host);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraGo);UnityEngine.Object.DestroyImmediate(sprite);UnityEngine.Object.DestroyImmediate(texture);}
    }
    [Test]
    public void InspectorOutlineChangesApplyLive_ZeroHidesOnlyEmphasis_SelectionRendersAboveIt()
    {
        var type=Find("CoreGuardianObjectEmphasis");Assert.That(type,Is.Not.Null,"Selected object emphasis is not implemented");
        var host=new GameObject("Emphasis settings test");var target=new GameObject("Settings unit");var cameraGo=new GameObject("Settings camera");var selectionHost=new GameObject("Independent selection");
        var texture=new Texture2D(2,2);texture.SetPixels(Enumerable.Repeat(Color.white,4).ToArray());texture.Apply();var sprite=Sprite.Create(texture,new Rect(0,0,2,2),Vector2.one*.5f,4);
        try {
            target.transform.position=new Vector3(10000,10000,0);var body=new GameObject("2D Sprite").AddComponent<SpriteRenderer>();body.transform.SetParent(target.transform,false);body.sprite=sprite;body.sortingOrder=10;
            var camera=cameraGo.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=1;camera.transform.position=target.transform.position+Vector3.back*10;
            var component=host.AddComponent(type);type.GetField("outlineWidthPixels").SetValue(component,3);type.GetField("outlineColor").SetValue(component,Color.magenta);
            var present=type.GetMethod("PresentTargets");var targets=new List<Transform>{target.transform};present.Invoke(component,new object[]{targets,camera});
            var output=host.GetComponentInChildren<MeshRenderer>();var properties=new MaterialPropertyBlock();output.GetPropertyBlock(properties);
            Assert.That(output.enabled,Is.True);Assert.That(properties.GetInt("_Width"),Is.EqualTo(3));Assert.That(properties.GetColor("_Color"),Is.EqualTo(Color.magenta));
            var selection=selectionHost.AddComponent(Find("CoreGuardianSelectionOutline"));selection.GetType().GetField("color").SetValue(selection,Color.green);selection.GetType().GetMethod("Present").Invoke(selection,new object[]{target.transform,camera});
            var green=selectionHost.GetComponentInChildren<MeshRenderer>();Assert.That(green.sortingOrder,Is.GreaterThan(output.sortingOrder),"Inspection must remain above always-on emphasis");
            type.GetField("outlineWidthPixels").SetValue(component,0);present.Invoke(component,new object[]{targets,camera});
            Assert.That(output.enabled,Is.False);Assert.That(green.enabled,Is.True);Begin(component,camera);Assert.That(body.color.r,Is.EqualTo(1.15f).Within(.00001f));
        } finally {DestroyOwned(host);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraGo);DestroyOwned(selectionHost);UnityEngine.Object.DestroyImmediate(sprite);UnityEngine.Object.DestroyImmediate(texture);}
    }
}
