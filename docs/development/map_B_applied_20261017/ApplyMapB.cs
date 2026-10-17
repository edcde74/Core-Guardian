using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

// One-shot authoring tool, outside Assets. No runtime code or rules are added.
public static class ApplyMapB
{
    const string Art = "Assets/Art/Environment/AbandonedFortressB/";
    const string Record = "docs/development/map_B_applied_20261017/";
    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    static TextureImporter Import(string name, int maxSize, float ppu, SpriteImportMode mode)
    {
        string path = Art + name;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = mode;
        imp.spritePixelsPerUnit = ppu;
        imp.spritePivot = new Vector2(.5f, .5f);
        var settings = new TextureImporterSettings(); imp.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; imp.SetTextureSettings(settings);
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.npotScale = TextureImporterNPOTScale.None;
        imp.maxTextureSize = maxSize;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = false;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.SaveAndReimport();
        return imp;
    }
    public static object Main()
    {
        Require(!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorUtility.scriptCompilationFailed, "Editor busy");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        Require(scene.path == "Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity" && !scene.isDirty, "Expected clean combat scene");
        Require(GameObject.Find("CoreGuardian_MapArt_B") == null, "B already applied; inspect instead of rebuilding");
        var game = UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
        var ground = game.ground;
        Require(ground.cellBounds.size == new Vector3Int(45,25,1), "Unexpected ground");
        var originalFloor = ground.GetTile(new Vector3Int(0,0,0));
        Require(originalFloor != null && game.coreMap.GetTile(new Vector3Int(29,9,0)) == originalFloor, "Expected shared floor reference");

        var imp = Import("StoneSlabs_B_2x2.png", 2048, 100, SpriteImportMode.Multiple);
        imp.GetSourceTextureWidthAndHeight(out int width, out int height);
        Require(width == height && width % 2 == 0, "Expected even square atlas");
        float half = width / 2f;
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(imp);
        Require(provider != null, "Sprite editor provider missing");
        provider.InitSpriteEditorDataProvider();
        var edit = provider.GetDataProvider<ISpriteFrameEditCapability>();
        Require(edit != null && edit.GetEditCapability().HasCapability(EEditCapability.CreateAndDeleteSprite), "Sprite slicing capability unavailable");
        var rects = new SpriteRect[4];
        for (int i=0;i<4;i++) rects[i] = new SpriteRect {
            name="Stone_B_"+i, rect=new Rect((i%2)*half,(i/2)*half,half,half),
            alignment=SpriteAlignment.Center, pivot=new Vector2(.5f,.5f), spriteID=GUID.Generate()
        };
        provider.SetSpriteRects(rects);
        var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        Require(names != null, "Sprite ID provider unavailable");
        names.SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)).ToArray());
        provider.Apply();
        // Imported atlas 64x64 => four effective 32px slabs, each still exactly 0.6 world units.
        imp.spritePixelsPerUnit = half/.6f;
        imp.maxTextureSize = 64;
        imp.SaveAndReimport();
        var sprites=AssetDatabase.LoadAllAssetsAtPath(Art+"StoneSlabs_B_2x2.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        Require(sprites.Length==4 && sprites.All(s=>Mathf.Abs(s.bounds.size.x-.6f)<.0001f), "Floor sprite bounds mismatch");
        var tiles=new Tile[4];
        for(int i=0;i<4;i++) {
            string path=Art+"Stone_B_"+i+".asset";
            Require(AssetDatabase.LoadMainAssetAtPath(path)==null,"Do not overwrite previous tile");
            var tile=ScriptableObject.CreateInstance<Tile>(); tile.name="Stone_B_"+i;
            tile.sprite=sprites[i]; tile.colliderType=Tile.ColliderType.None;
            tile.flags=TileFlags.LockColor; tile.color=Color.white;
            AssetDatabase.CreateAsset(tile,path); tiles[i]=tile;
        }
        // Atlas order: lower-left moss, lower-right crack, upper-left clean, upper-right chip.
        var positions=new System.Collections.Generic.List<Vector3Int>();
        foreach(var p in ground.cellBounds.allPositionsWithin) if(ground.HasTile(p))positions.Add(p);
        Undo.RegisterCompleteObjectUndo(ground,"B stone visual tiles");
        foreach(var p in positions) {
            int hash=(p.x*73856093 ^ p.y*19349663)&0x7fffffff;
            int roll=hash%100;
            int variant=roll<3?0:roll<8?1:roll<78?2:3;
            ground.SetTile(p,tiles[variant]);
        }
        Undo.RegisterCompleteObjectUndo(game.coreMap,"Matching core floor");
        game.coreMap.SetTile(new Vector3Int(29,9,0), tiles[2]);

        var bgImp=Import("CourtyardBackground_B.png",2048,100,SpriteImportMode.Single);
        var background=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"CourtyardBackground_B.png");
        Require(background!=null,"Background sprite missing");
        var root=new GameObject("CoreGuardian_MapArt_B");
        Undo.RegisterCreatedObjectUndo(root,"B environment background");
        var bg=new GameObject("Background_Ruins_VisualOnly"); bg.transform.SetParent(root.transform,false);
        var renderer=bg.AddComponent<SpriteRenderer>(); renderer.sprite=background;
        renderer.sharedMaterial=ground.GetComponent<TilemapRenderer>().sharedMaterial;
        renderer.sortingLayerID=ground.GetComponent<TilemapRenderer>().sortingLayerID;
        renderer.sortingOrder=-20;
        // Slight art-only overscan leaves stone apron outside the playable tile rectangle.
        var camera=Camera.main;
        float h=camera.orthographicSize*2, w=h*(1920f/1080f);
        bg.transform.position=new Vector3(camera.transform.position.x,camera.transform.position.y,1);
        bg.transform.localScale=new Vector3(w*1.08f/background.bounds.size.x,h*1.08f/background.bounds.size.y,1);
        root.transform.position=Vector3.zero;
        var floorMaterial=new Material(ground.GetComponent<TilemapRenderer>().sharedMaterial);
        floorMaterial.name="StoneFloor_B";
        floorMaterial.SetColor("_Color",new Color(1.05f,1.045f,1.035f,1));
        AssetDatabase.CreateAsset(floorMaterial,Art+"StoneFloor_B.mat");
        ground.GetComponent<TilemapRenderer>().sharedMaterial=floorMaterial;
        game.coreMap.GetComponent<TilemapRenderer>().sharedMaterial=floorMaterial;
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        var result=new { sourceAtlas=new[]{width,height}, importedAtlas=new[]{64,64}, tiles=positions.Count,
            variants=positions.GroupBy(p=>ground.GetTile(p).name).Select(g=>new{g.Key,count=g.Count()}).ToArray(),
            spriteBounds=sprites.Select(s=>new{s.name,size=s.bounds.size.ToString(),s.pixelsPerUnit}).ToArray(),
            background=renderer.bounds.ToString(), colliders=root.GetComponentsInChildren<Collider2D>().Length,
            sceneSaved=false, gameplayCodeChanged=false };
        File.WriteAllText(Record+"applied_setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
