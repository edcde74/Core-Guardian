// [UNITY-SKILL:SPRITEATLAS]
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

// Shared by explicit Editor preview and future user-requested builds. Never starts a build.
public sealed class CoreGuardianSpriteAtlasPipeline : IPreprocessBuildWithReport
{
    const string Output="Assets/Art/Atlases/Sprites/";
    const string Manifest="Assets/Data/Presentation/Resources/CoreGuardian/SpriteAtlases/Sources.asset";
    public int callbackOrder=>-100;
    static readonly string[][] Groups={
        new[]{"CG_Enemies_Common_01","Assets/Art/Characters/Enemies/Sprites","Assets/Art/Characters/Enemies/Resources/CoreGuardian/VerticalWalk","Assets/Art/Characters/Enemies/Boss","Assets/Art/Characters/Enemies/Resources/CoreGuardian/OriginalWalk"},
        new[]{"CG_Towers_Common_01","Assets/Art/Characters/Towers"},
        new[]{"CG_CombatFX_Common","Assets/Art/Effects/Combat"}
    };
    public void OnPreprocessBuild(BuildReport report)
    {
        var existing=Groups.Select(g=>Output+g[0]+".spriteatlasv2").Where(System.IO.File.Exists).ToArray();
        bool enabled=existing.Length>0&&existing.All(p=>((SpriteAtlasImporter)AssetImporter.GetAtPath(p)).includeInBuild);
        Synchronize(false,enabled);
    }
    internal static bool RequiresFullRect(string path)=>Groups.Take(2).SelectMany(g=>g.Skip(1)).Any(folder=>path.StartsWith(folder+"/",StringComparison.Ordinal));
    public static string[] Synchronize(bool preview,bool enablePackedSprites=false)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Synchronize only in idle Edit Mode");
        if(EditorSettings.spritePackerMode!=SpritePackerMode.SpriteAtlasV2)throw new InvalidOperationException("Enable SpriteAtlasV2 explicitly before synchronizing");
        var sources=new List<CoreGuardianSpriteAtlasSources.Entry>();var atlases=new List<SpriteAtlas>();var paths=new List<string>();
        var allAtlases=AssetDatabase.FindAssets("t:SpriteAtlas",new[]{"Assets"}).Select(g=>AssetDatabase.GUIDToAssetPath(g)).ToArray();
        foreach(var group in Groups)
        {
            var folders=group.Skip(1).ToArray();
            var texturePaths=AssetDatabase.FindAssets("t:Texture2D",folders).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p=>(AssetImporter.GetAtPath(p) as TextureImporter)?.textureType==TextureImporterType.Sprite).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
            string path=Output+group[0]+".spriteatlasv2";
            // CombatB already shares one texture. Add its atlas only when content actually grows.
            if(group[0]=="CG_CombatFX_Common"&&texturePaths.Length<2&&!System.IO.File.Exists(path))continue;
            foreach(var otherPath in allAtlases.Where(p=>p!=path))
            {
                var other=AssetDatabase.LoadAssetAtPath<SpriteAtlas>(otherPath);
                foreach(var packable in other.GetPackables())
                {
                    string p=AssetDatabase.GetAssetPath(packable);
                    if(texturePaths.Any(t=>t==p||(AssetDatabase.IsValidFolder(p)&&t.StartsWith(p+"/",StringComparison.Ordinal))))
                        throw new InvalidOperationException("Duplicate sprite ownership: "+otherPath+" / "+p);
                }
            }
            foreach(string p in texturePaths)
            {
                // Rectangular packing alone still trims a Tight source mesh. Parts need the full source rectangle.
                var input=(TextureImporter)AssetImporter.GetAtPath(p);var settings=new TextureImporterSettings();input.ReadTextureSettings(settings);
                if(settings.spriteMeshType!=SpriteMeshType.FullRect){settings.spriteMeshType=SpriteMeshType.FullRect;input.SetTextureSettings(settings);input.SaveAndReimport();}
                foreach(var sprite in AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>())
                    sources.Add(new CoreGuardianSpriteAtlasSources.Entry{sprite=sprite,texture=AssetDatabase.LoadAssetAtPath<Texture2D>(p),rect=sprite.rect});
            }
            var asset=System.IO.File.Exists(path)?SpriteAtlasAsset.Load(path):new SpriteAtlasAsset();
            if(System.IO.File.Exists(path))asset.Remove(AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path).GetPackables());
            if(enablePackedSprites)asset.Add(folders.Select(AssetDatabase.LoadAssetAtPath<DefaultAsset>).Cast<UnityEngine.Object>().ToArray());
            SpriteAtlasAsset.Save(asset,path);AssetDatabase.ImportAsset(path);
            var importer=(SpriteAtlasImporter)AssetImporter.GetAtPath(path);
            importer.includeInBuild=enablePackedSprites;
            var packing=importer.packingSettings;packing.enableRotation=false;packing.enableTightPacking=false;packing.padding=4;importer.packingSettings=packing;
            var texture=importer.textureSettings;texture.filterMode=FilterMode.Point;texture.generateMipMaps=false;texture.readable=false;texture.sRGB=true;importer.textureSettings=texture;
            var platform=importer.GetPlatformSettings("DefaultTexturePlatform");platform.maxTextureSize=2048;platform.textureCompression=TextureImporterCompression.Uncompressed;platform.crunchedCompression=false;importer.SetPlatformSettings(platform);
            importer.SaveAndReimport();paths.Add(path);atlases.Add(AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path));
        }
        EnsureFolder(System.IO.Path.GetDirectoryName(Manifest).Replace('\\','/'));
        var manifest=AssetDatabase.LoadAssetAtPath<CoreGuardianSpriteAtlasSources>(Manifest);
        if(manifest==null){manifest=ScriptableObject.CreateInstance<CoreGuardianSpriteAtlasSources>();AssetDatabase.CreateAsset(manifest,Manifest);}
        manifest.Initialize(sources.ToArray());EditorUtility.SetDirty(manifest);AssetDatabase.SaveAssetIfDirty(manifest);
        if(!enablePackedSprites)
        {
            // Include in Build OFF alone leaves sprites waiting for late binding. Remove our packing ownership too.
            foreach(var sourcePath in sources.Select(s=>AssetDatabase.GetAssetPath(s.texture)).Distinct())AssetDatabase.ImportAsset(sourcePath,ImportAssetOptions.ForceUpdate);
        }
        RefreshMaskHashes();
        if(preview&&enablePackedSprites)SpriteAtlasUtility.PackAtlases(atlases.ToArray(),EditorUserBuildSettings.activeBuildTarget);
        return paths.ToArray();
    }
    static void RefreshMaskHashes()
    {
        var mask=Resources.Load<CoreGuardianMaskAtlas>("CoreGuardian/MaskAtlas/Manifest");if(mask==null)return;
        var serialized=new SerializedObject(mask);var values=serialized.FindProperty("entries");bool changed=false;
        for(int i=0;i<values.arraySize;i++)
        {
            var value=values.GetArrayElementAtIndex(i);var texture=(Texture2D)value.FindPropertyRelative("source").objectReferenceValue;
            if(texture==null)continue;
            var hash=value.FindPropertyRelative("dependencyHash");var current=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(texture)).ToString();if(hash.stringValue==current)continue;
            var page=(Texture2DArray)value.FindPropertyRelative("page").objectReferenceValue;int layer=value.FindPropertyRelative("layer").intValue;
            var original=CoreGuardianMaskAtlasBuilder.ReadPixels(texture);var baked=CoreGuardianMaskAtlasBuilder.ReadLayer(page,layer);
            if(original.Length!=baked.Length||original.Where((p,n)=>p.a!=baked[n].a).Any())throw new InvalidOperationException("Source alpha changed; rebake B explicitly: "+texture.name);
            hash.stringValue=current;changed=true;
        }
        if(changed)
        {
            var signature=new List<Texture2D>();for(int i=0;i<values.arraySize;i++)signature.Add((Texture2D)values.GetArrayElementAtIndex(i).FindPropertyRelative("source").objectReferenceValue);
            serialized.FindProperty("inputHash").stringValue="array-v1;"+string.Join(";",signature.Distinct().OrderBy(t=>AssetDatabase.GetAssetPath(t),StringComparer.Ordinal).Select(t=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(t))+":"+AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(t))));
            serialized.ApplyModifiedPropertiesWithoutUndo();mask.Invalidate();AssetDatabase.SaveAssetIfDirty(mask);
        }
    }
    static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        var split=path.LastIndexOf('/');EnsureFolder(path.Substring(0,split));AssetDatabase.CreateFolder(path.Substring(0,split),path.Substring(split+1));
    }
}

// Apply only the coordinate contract at import; atlas generation stays in the explicit/prebuild pipeline.
sealed class CoreGuardianSpriteAtlasInputImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!CoreGuardianSpriteAtlasPipeline.RequiresFullRect(assetPath))return;
        var importer=(TextureImporter)assetImporter;var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
        if(settings.spriteMeshType==SpriteMeshType.FullRect)return;
        settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
    }
}
