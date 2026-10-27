using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine.U2D;

public class CoreGuardianSpriteAtlasCompatibilityTests
{
    static Type Implementation=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianSpriteAtlasSources")).FirstOrDefault(t=>t!=null);

    [Test]
    public void SessionResetDisposesCachedWalkCutsBeforeSourceMappingsAreReset()
    {
        var walk=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianOriginalWalk")).First(t=>t!=null);
        var pair=walk.GetNestedType("VerticalLegPair",BindingFlags.NonPublic);
        var boots=(System.Collections.IDictionary)pair.GetField("boots",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        var source=Resources.Load<Sprite>("CoreGuardian/VerticalWalk/Attack/Up_0");Sprite part=null;
        try
        {
            part=(Sprite)Implementation.GetMethod("Cut").Invoke(null,new object[]{source,new Rect(40,0,7,23),new Vector2(.5f,3/23f),source.pixelsPerUnit});
            boots[source]=part;
            Implementation.GetMethod("Reset",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
            Assert.That(part==null,Is.True,"Domain reload is disabled: retained cuts must not outlive their source mappings");
            Assert.That(boots.Count,Is.EqualTo(0));
        }
        finally{boots.Remove(source);if(part!=null)UnityEngine.Object.DestroyImmediate(part);}
    }

    [Test]
    public void NewManagedCharacterTexturesCannotSilentlyReintroduceTightMeshTrimming()
    {
        string path="Assets/Art/Characters/Enemies/Sprites/AtlasImportTest_"+Guid.NewGuid().ToString("N")+".png";
        var texture=new Texture2D(40,40);
        try
        {
            texture.SetPixels32(Enumerable.Repeat(new Color32(255,255,255,255),1600).ToArray());texture.Apply();
            System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.textureType=TextureImporterType.Sprite;settings.spriteMeshType=SpriteMeshType.Tight;importer.SetTextureSettings(settings);importer.SaveAndReimport();
            importer.ReadTextureSettings(settings);Assert.That(settings.spriteMeshType,Is.EqualTo(SpriteMeshType.FullRect),"New character imports must preserve the part-cropping rectangle before Play or build");
        }
        finally{AssetDatabase.DeleteAsset(path);UnityEngine.Object.DestroyImmediate(texture);}
    }

    [Test]
    public void ManagedAtlasesKeepFullRectPixelsAndOriginalBMaskLookup()
    {
        var pipeline=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianSpriteAtlasPipeline")).First(t=>t!=null);
        // Packing is the default since 2026-10-27 (최적화 1). Restore whatever state the project had before the test.
        var enemyAtlas=(SpriteAtlasImporter)AssetImporter.GetAtPath("Assets/Art/Atlases/Sprites/CG_Enemies_Common_01.spriteatlasv2");
        bool wasEnabled=enemyAtlas!=null&&enemyAtlas.includeInBuild;
        try
        {
        pipeline.GetMethod("Synchronize").Invoke(null,new object[]{true,true});
        var paths=new[]{"Assets/Art/Atlases/Sprites/CG_Enemies_Common_01.spriteatlasv2","Assets/Art/Atlases/Sprites/CG_Towers_Common_01.spriteatlasv2"};
        var atlases=paths.Select(AssetDatabase.LoadAssetAtPath<SpriteAtlas>).ToArray();
        Assert.That(atlases.All(a=>a!=null),Is.True);
        SpriteAtlasUtility.PackAtlases(atlases,EditorUserBuildSettings.activeBuildTarget);
        var builder=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianMaskAtlasBuilder")).First(t=>t!=null);
        var maskType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianMaskAtlas")).First(t=>t!=null);
        var mask=Resources.Load("CoreGuardian/MaskAtlas/Manifest",maskType);
        var lookup=maskType.GetMethod("TryGet",new[]{typeof(Texture2D),maskType.GetNestedType("Entry").MakeByRefType()});
        int tested=0,mapped=0;
        foreach(string path in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Art/Characters/Enemies","Assets/Art/Characters/Towers"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var original=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            foreach(var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
            {
                Assert.That(sprite.packed,Is.True,path);Assert.That(sprite.packingRotation,Is.EqualTo(SpritePackingRotation.None));
                Assert.That(sprite.textureRect.size,Is.EqualTo(sprite.rect.size),"Transparent margins are part of the body-cut coordinate contract: "+path);
                var args=new object[]{sprite,null,null};Assert.That(Implementation.GetMethod("TryGetSource").Invoke(null,args),Is.EqualTo(true));
                Assert.That(args[1],Is.EqualTo(original));var transform=(Vector4)args[2];
                var a=(Color32[])builder.GetMethod("ReadPixels").Invoke(null,new object[]{original});
                var b=(Color32[])builder.GetMethod("ReadPixels").Invoke(null,new object[]{sprite.texture});
                var rect=sprite.textureRect;var src=sprite.rect;
                for(int y=0;y<(int)src.height;y++)for(int x=0;x<(int)src.width;x++)
                {
                    var expected=a[((int)src.y+y)*original.width+(int)src.x+x];var actual=b[((int)rect.y+y)*sprite.texture.width+(int)rect.x+x];
                    Assert.That(actual.a,Is.EqualTo(expected.a),path+" alpha at "+x+","+y);
                    if(expected.a>0)Assert.That(actual,Is.EqualTo(expected),path+" visible pixel at "+x+","+y);
                }
                var pixel=new Vector2(rect.x+.5f,rect.y+.5f)/new Vector2(sprite.texture.width,sprite.texture.height);
                Assert.That(pixel.x*transform.x+transform.z,Is.EqualTo((src.x+.5f)/original.width).Within(.00001f));
                Assert.That(pixel.y*transform.y+transform.w,Is.EqualTo((src.y+.5f)/original.height).Within(.00001f));
                var entryArgs=new object[]{args[1],null};bool originalMapped=(bool)lookup.Invoke(mask,entryArgs);
                var spriteLookup=maskType.GetMethod("TryGetSprite",new[]{typeof(Sprite),maskType.GetNestedType("Entry").MakeByRefType(),typeof(Vector4).MakeByRefType()});
                Assert.That(spriteLookup.Invoke(mask,new object[]{sprite,null,null}),Is.EqualTo(originalMapped),"Packing must not add fallback or expand B eligibility: "+path);
                if(originalMapped)mapped++;tested++;
            }
        }
        Assert.That(tested,Is.GreaterThan(20));Assert.That(mapped,Is.GreaterThan(20),"Existing B coverage must remain active; originally unsupported inputs keep fallback");
        }
        finally{pipeline.GetMethod("Synchronize").Invoke(null,new object[]{wasEnabled,wasEnabled});}
    }

    [Test]
    public void CroppingAnOffsetSpriteKeepsTheRequestedPixelsAndPivot()
    {
        Assert.That(Implementation,Is.Not.Null,"Atlas-safe part cropping is required before packing animated artwork");
        var texture=new Texture2D(64,64);var source=Sprite.Create(texture,new Rect(11,17,20,24),Vector2.one*.5f,32,0,SpriteMeshType.FullRect);
        Sprite part=null;
        try
        {
            part=(Sprite)Implementation.GetMethod("Cut").Invoke(null,new object[]{source,new Rect(3,4,7,8),new Vector2(.5f,0),32f});
            Assert.That(part.texture,Is.SameAs(texture));
            Assert.That(part.textureRect,Is.EqualTo(new Rect(14,21,7,8)));
            Assert.That(part.pivot,Is.EqualTo(new Vector2(3.5f,0)));
        }
        finally{if(part!=null)UnityEngine.Object.DestroyImmediate(part);UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(texture);}
    }

    [Test]
    public void RemappedCutKeepsOriginalMaskCoordinatesInsteadOfLookingUpThePackedPage()
    {
        Assert.That(Implementation,Is.Not.Null,"B mask lookup must retain source identity and UVs");
        var original=new Texture2D(32,32);var packed=new Texture2D(128,128);
        var source=Sprite.Create(packed,new Rect(48,64,20,24),Vector2.one*.5f,32,0,SpriteMeshType.FullRect);Sprite part=null;
        try
        {
            Implementation.GetMethod("Register",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{source,original,new Rect(2,3,20,24)});
            part=(Sprite)Implementation.GetMethod("Cut").Invoke(null,new object[]{source,new Rect(3,4,7,8),new Vector2(.5f,0),32f});
            var args=new object[]{part,null,null};
            Assert.That(Implementation.GetMethod("TryGetSource").Invoke(null,args),Is.EqualTo(true));
            Assert.That(args[1],Is.SameAs(original));
            var transform=(Vector4)args[2];
            foreach(var uv in part.uv)
            {
                var mapped=new Vector2(uv.x*transform.x+transform.z,uv.y*transform.y+transform.w);
                Assert.That(mapped.x,Is.InRange(5/32f,12/32f));
                Assert.That(mapped.y,Is.InRange(7/32f,15/32f));
            }
        }
        finally{if(part!=null)UnityEngine.Object.DestroyImmediate(part);UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(packed);UnityEngine.Object.DestroyImmediate(original);}
    }
}
