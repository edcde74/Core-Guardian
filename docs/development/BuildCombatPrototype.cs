using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using Newtonsoft.Json;

public static class BuildCombatPrototype
{
    public const string Source = "Assets/Scenes/ArtReview_20261008_PathContrast_A.unity";
    public const string Destination = "Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity";
    const string Prefabs = "Assets/Art/ScreenTests/20261008_Depth/Prefabs/";
    public static object Main()
    {
        if(EditorApplication.isPlaying) throw new Exception("Stop Play mode first");
        if(File.Exists(Destination)) throw new Exception("Gameplay scene already exists; use Verify instead of overwriting");
        var scene=EditorSceneManager.OpenScene(Source,OpenSceneMode.Single);
        if(!EditorSceneManager.SaveScene(scene,Destination,true)) throw new Exception("Scene copy failed");
        scene=EditorSceneManager.OpenScene(Destination,OpenSceneMode.Single);
        foreach(var sprite in UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Where(s=>s.name=="2D Sprite").ToArray())
        {
            var root=sprite.transform.parent;
            if(root.name.StartsWith("enemy_")||root.name.StartsWith("tower_")) UnityEngine.Object.DestroyImmediate(root.gameObject);
        }
        var maps=UnityEngine.Object.FindObjectsByType<Tilemap>();
        var ground=maps.Single(m=>m.name=="Ground_Prototype");
        var occupied=maps.Single(m=>m.name=="TowerOccupiedCells_Prototype");
        occupied.ClearAllTiles();
        var controller=new GameObject("CoreGuardian_Gameplay").AddComponent<CoreGuardianPrototype>();
        controller.ground=ground;
        controller.enemyPath=maps.Single(m=>m.name=="EnemyPath_Prototype");
        controller.coreMap=maps.Single(m=>m.name=="Core_Prototype");
        controller.occupied=occupied;
        controller.player=UnityEngine.Object.FindObjectsByType<PrototypeGridMovement>().Single();
        controller.enemyArt=new[]{"enemy_attack","enemy_defense","enemy_buff"}.Select(n=>AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+n+".prefab")).ToArray();
        controller.towerArt=new[]{"tower_single","tower_multi","tower_support"}.Select(n=>AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+n+".prefab")).ToArray();
        controller.enemiesCsv=Resources.Load<TextAsset>("CoreGuardian/enemies");
        controller.towersCsv=Resources.Load<TextAsset>("CoreGuardian/towers");
        controller.stageCsv=Resources.Load<TextAsset>("CoreGuardian/stage");
        controller.upgradesCsv=Resources.Load<TextAsset>("CoreGuardian/upgrades");
        controller.wavesCsv=Resources.Load<TextAsset>("CoreGuardian/waves");
        if(controller.enemyArt.Any(g=>g==null)||controller.towerArt.Any(g=>g==null))throw new Exception("Missing approved art prefab");
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");
        EditorSceneManager.OpenScene(Destination,OpenSceneMode.Single);
        return Verify();
    }
    public static object Verify()
    {
        var c=UnityEngine.Object.FindObjectsByType<CoreGuardianPrototype>().Single();
        if(c.gameObject.scene.path!=Destination)throw new Exception("Wrong scene");
        if(c.enemyArt.Length!=3||c.towerArt.Length!=3||c.player==null||c.ground==null||c.occupied==null)throw new Exception("Broken scene references");
        if(new[]{c.enemiesCsv,c.towersCsv,c.stageCsv,c.upgradesCsv,c.wavesCsv}.Any(t=>t==null))throw new Exception("Missing combat CSV");
        var maps=UnityEngine.Object.FindObjectsByType<Tilemap>();
        var path=maps.Single(m=>m.name=="PathArt_WideStraight").GetComponent<TilemapRenderer>();
        if(!AssetDatabase.GetAssetPath(path.sharedMaterial).EndsWith("PathContrast_A.mat"))throw new Exception("Approved A path material lost");
        var start=c.ground.WorldToCell(c.player.transform.position);
        return JsonConvert.SerializeObject(new{success=true,scene=Destination,approvedAPath=true,enemyTypes=3,towerTypes=3,playerStart=new[]{start.x,start.y},saved=!c.gameObject.scene.isDirty});
    }
    public static object RegisterGameplayScene()
    {
        var scenes=EditorBuildSettings.scenes.ToList();
        if(!scenes.Any(s=>s.path==Destination))scenes.Add(new EditorBuildSettingsScene(Destination,true));
        else scenes.Single(s=>s.path==Destination).enabled=true;
        EditorBuildSettings.scenes=scenes.ToArray();
        return JsonConvert.SerializeObject(new{success=true,scenes=scenes.Select(s=>new{s.path,s.enabled})});
    }
}
