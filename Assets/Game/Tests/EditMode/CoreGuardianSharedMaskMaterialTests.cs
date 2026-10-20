using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

public class CoreGuardianSharedMaskMaterialTests
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static Type Type(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static T Field<T>(Component c,string name)=>(T)c.GetType().GetField(name,Hidden).GetValue(c);
    static void Set(Component c,string name,object value)=>c.GetType().GetField(name).SetValue(c,value);
    static void Present(Component c,Transform target,Camera camera)=>c.GetType().GetMethod("Present").Invoke(c,new object[]{target,camera});
    static void Release(Component c){if(c!=null)c.GetType().GetMethod("OnDestroy",Hidden)?.Invoke(c,null);}
    static int Native()=>Resources.FindObjectsOfTypeAll<Material>().Count(m=>(m.hideFlags&HideFlags.DontSave)!=0&&m.shader!=null&&new[]{"CoreGuardian/SelectionMask","CoreGuardian/SelectionOutline","CoreGuardian/PhotoShadowContact"}.Contains(m.shader.name));
    sealed class Fixture:IDisposable
    {
        public readonly List<GameObject> roots=new List<GameObject>();
        public readonly List<UnityEngine.Object> assets=new List<UnityEngine.Object>();
        public GameObject Root(string name){var r=new GameObject(name);roots.Add(r);return r;}
        public Camera Camera(){var c=Root("Shared material camera").AddComponent<Camera>();c.enabled=false;c.orthographic=true;c.orthographicSize=2;c.transform.position=new Vector3(10001,10000,-10);return c;}
        public Component Owner(float x)
        {
            var root=Root("Shared material owner");root.transform.position=new Vector3(x,10000,0);
            var texture=new Texture2D(8,8);texture.SetPixels(Enumerable.Repeat(Color.white,64).ToArray());texture.Apply();assets.Add(texture);
            var sprite=Sprite.Create(texture,new Rect(0,0,8,8),Vector2.one*.5f,8);assets.Add(sprite);
            var body=new GameObject("2D Sprite").AddComponent<SpriteRenderer>();body.transform.SetParent(root.transform,false);body.sprite=sprite;
            return root.AddComponent(Type("CoreGuardianSelectionOutline"));
        }
        public void Dispose()
        {
            foreach(var r in roots.Where(r=>r!=null))foreach(var c in r.GetComponentsInChildren<Component>(true))if(c!=null&&c.GetType().Name=="CoreGuardianSelectionOutline")Release(c);
            foreach(var r in roots.Where(r=>r!=null))foreach(var c in r.GetComponentsInChildren<Component>(true))if(c!=null&&c.GetType().Name=="CoreGuardianPhotoShadow")Release(c);
            foreach(var r in roots)if(r!=null)UnityEngine.Object.DestroyImmediate(r);
            foreach(var a in assets)if(a!=null)UnityEngine.Object.DestroyImmediate(a);
        }
    }
    [Test]
    public void OwnersShareMaterials_WithoutSharingMaskColorWidthOrLifetime()
    {
        int before=Native();
        using(var f=new Fixture())
        {
            var camera=f.Camera();var a=f.Owner(10000);var b=f.Owner(10002);
            Set(a,"color",Color.red);Set(a,"widthPixels",1);Set(b,"color",Color.green);Set(b,"widthPixels",3);
            Present(a,a.transform,camera);Present(b,b.transform,camera);
            var maskMaterial=Field<Material>(a,"maskMaterial");var outlineMaterial=Field<Material>(a,"outlineMaterial");
            Assert.That(Field<Material>(b,"maskMaterial"),Is.SameAs(maskMaterial),"Mask shader state must use a common material");
            Assert.That(Field<Material>(b,"outlineMaterial"),Is.SameAs(outlineMaterial),"Output material must be shared");
            var ap=new MaterialPropertyBlock();var bp=new MaterialPropertyBlock();
            Field<MeshRenderer>(a,"output").GetPropertyBlock(ap);Field<MeshRenderer>(b,"output").GetPropertyBlock(bp);
            Assert.That(ap.GetColor("_Color"),Is.EqualTo(Color.red));Assert.That(bp.GetColor("_Color"),Is.EqualTo(Color.green));
            Assert.That(ap.GetInt("_Width"),Is.EqualTo(1));Assert.That(bp.GetInt("_Width"),Is.EqualTo(3));
            Assert.That(ap.GetTexture("_MainTex"),Is.SameAs(Field<RenderTexture>(a,"mask")));
            Assert.That(bp.GetTexture("_MainTex"),Is.SameAs(Field<RenderTexture>(b,"mask")));
            Assert.That(ap.GetTexture("_MainTex"),Is.Not.SameAs(bp.GetTexture("_MainTex")));
            var defaultColor=outlineMaterial.GetColor("_Color");
            Set(a,"color",Color.blue);Present(a,a.transform,camera);
            Field<MeshRenderer>(b,"output").GetPropertyBlock(bp);Assert.That(bp.GetColor("_Color"),Is.EqualTo(Color.green));
            Assert.That(outlineMaterial.GetColor("_Color"),Is.EqualTo(defaultColor),"Shared material defaults must stay immutable");
            Release(a);UnityEngine.Object.DestroyImmediate(a.gameObject);
            Assert.That(maskMaterial!=null&&outlineMaterial!=null,Is.True,"A surviving owner must retain its shared resources");
            Present(b,b.transform,camera);Assert.That(Field<MeshRenderer>(b,"output").enabled,Is.True);
        }
        Assert.That(Native(),Is.EqualTo(before),"Last release must dispose our native materials");
    }
    [Test]
    public void PhotoContactsShareMaterial_ButRetainEachUnitsBandAndTexture()
    {
        int before=Native();
        using(var f=new Fixture())
        {
            var camera=f.Camera();var shadows=new Component[2];var contacts=new Component[2];
            for(int i=0;i<2;i++)
            {
                var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/ScreenTests/20261008_Depth/Prefabs/enemy_attack.prefab"));f.roots.Add(root);
                root.transform.position=new Vector3(10000+i*2,10000,0);root.transform.localScale*=i==0?1:1.4f;
                shadows[i]=(Component)Type("CoreGuardianPhotoShadow").GetMethod("Attach").Invoke(null,new object[]{root,"Attack"});
                shadows[i].GetType().GetMethod("Present").Invoke(shadows[i],new object[]{camera});
                contacts[i]=Field<Component>(shadows[i],"contact");
            }
            Assert.That(Field<Material>(shadows[0],"contactMaterial"),Is.SameAs(Field<Material>(shadows[1],"contactMaterial")));
            var blocks=new[]{new MaterialPropertyBlock(),new MaterialPropertyBlock()};
            for(int i=0;i<2;i++)
            {
                Field<MeshRenderer>(contacts[i],"output").GetPropertyBlock(blocks[i]);
                float expected=16*camera.pixelHeight/(2*camera.orthographicSize)/63.52941f*Field<float>(shadows[i],"size");
                Assert.That(blocks[i].GetFloat("_Band"),Is.EqualTo(expected).Within(.001));
                Assert.That(blocks[i].GetTexture("_MainTex"),Is.SameAs(Field<RenderTexture>(contacts[i],"mask")));
            }
            Assert.That(blocks[0].GetFloat("_Band"),Is.Not.EqualTo(blocks[1].GetFloat("_Band")));
        }
        Assert.That(Native(),Is.EqualTo(before));
    }
}
