using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// One-shot scene authoring only. The logical floor Tilemaps remain intact.
public static class ApplyNatural01
{
    public static object Main()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed||scene.isDirty)
            throw new Exception("Expected clean stopped editor");
        if(scene.path!="Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity")throw new Exception("Wrong scene");
        const string path="Assets/Art/Environment/AbandonedFortressB/NaturalCourtyard_01_ClearBorder.png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=100;importer.spritePivot=new Vector2(.5f,.5f);
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
        settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
        importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;
        importer.mipmapEnabled=false;importer.alphaIsTransparency=false;importer.SaveAndReimport();
        var game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
        var bg=GameObject.Find("Background_Ruins_VisualOnly").GetComponent<SpriteRenderer>();
        Undo.RecordObject(bg,"Selected natural stone floor");Undo.RecordObject(bg.transform,"Continuous courtyard frame");
        Undo.RecordObject(game.ground.GetComponent<TilemapRenderer>(),"Hide rejected repeated floor art");
        Undo.RecordObject(game.coreMap.GetComponent<TilemapRenderer>(),"Keep core floor continuous");
        bg.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        // Same fixed camera/world footprint; no art-only 8% crop from the rejected first application.
        bg.transform.localScale=new Vector3((17f*1920/1080)/bg.sprite.bounds.size.x,17f/bg.sprite.bounds.size.y,1);
        game.ground.GetComponent<TilemapRenderer>().enabled=false;
        game.coreMap.GetComponent<TilemapRenderer>().enabled=false;
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
        return new{floorRenderer=false,coreFloorRenderer=false,sprite=path,logicalBoard=game.ground.cellBounds.size.ToString(),cameraChanged=false};
    }
}
