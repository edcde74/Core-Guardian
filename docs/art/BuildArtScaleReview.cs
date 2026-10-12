using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class BuildArtScaleReview
{
    const string Art = "Assets/Art/ScreenTests/20260930";
    static readonly string[] Keys = { "player", "enemy_attack", "enemy_defense", "enemy_buff", "tower_single", "tower_multi", "tower_support", "core", "ground_tile", "path_tile" };
    static readonly int[] Heights = {48,64,48,96,64,56,64,96,32,32};
    static Dictionary<string,Sprite> Sprites = new Dictionary<string,Sprite>();
    static Material Material;

    // A first sprite preparation pass only: alpha crop, nearest-neighbour sampling.
    // Native test heights are provisional, not an approved production resolution.
    static void PrepareSprite(string key, int height)
    {
        string source = Art + "/Source/" + key + ".png";
        var sourceTexture = new Texture2D(2,2,TextureFormat.RGBA32,false);
        ImageConversion.LoadImage(sourceTexture, File.ReadAllBytes(source));
        var pixels = sourceTexture.GetPixels32();
        bool tile = key.EndsWith("_tile");
        int left=0,right=sourceTexture.width-1,bottom=0,top=sourceTexture.height-1;
        if (!tile)
        {
            left=sourceTexture.width; right=-1; bottom=sourceTexture.height; top=-1;
            for(int y=0;y<sourceTexture.height;y++)
            for(int x=0;x<sourceTexture.width;x++)
                if(pixels[y*sourceTexture.width+x].a >= 128)
                {left=Math.Min(left,x);right=Math.Max(right,x);bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
            if(right<left) throw new Exception("Empty sprite: "+key);
            if(pixels.Count(p=>p.a==0)<pixels.Length/10) throw new Exception("Missing transparent background: "+key);
        }
        int cropWidth=right-left+1,cropHeight=top-bottom+1;
        int width=tile?height:Math.Max(2,Mathf.RoundToInt((float)cropWidth/cropHeight*height));
        var target = new Texture2D(width,height,TextureFormat.RGBA32,false);
        var targetPixels = new Color32[width*height];
        for(int y=0;y<height;y++)
        for(int x=0;x<width;x++)
        {
            int sx=Math.Min(right,left+(int)((x+0.5f)*cropWidth/width));
            int sy=Math.Min(top,bottom+(int)((y+0.5f)*cropHeight/height));
            var color=pixels[sy*sourceTexture.width+sx];
            color.a=tile?(byte)255:(color.a>=128?(byte)255:(byte)0);
            targetPixels[y*width+x]=color;
        }
        target.SetPixels32(targetPixels); target.Apply();
        string path=Art+"/Sprites/"+key+".png";
        File.WriteAllBytes(path,ImageConversion.EncodeToPNG(target));
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(sourceTexture);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=32;
        
        importer.filterMode=FilterMode.Point;
        importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency=true;
        importer.maxTextureSize=256;
        var settings=new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType=SpriteMeshType.FullRect; settings.spriteAlignment=(int)(tile?SpriteAlignment.Center:SpriteAlignment.BottomCenter);
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        Sprites[key]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Tile MakeTile(string key)
    {
        var tile=ScriptableObject.CreateInstance<Tile>();
        tile.sprite=Sprites[key];
        tile.colliderType=Tile.ColliderType.None;
        AssetDatabase.CreateAsset(tile,Art+"/Tiles/"+key+".asset");
        return tile;
    }

    static SpriteRenderer Spawn(string key, float x,float y,float factor,Transform parent)
    {
        var go=new GameObject(key);
        go.transform.SetParent(parent);
        go.transform.position=new Vector3(x,y,0);
        go.transform.localScale=Vector3.one*factor;
        var renderer=go.AddComponent<SpriteRenderer>();
        renderer.sprite=Sprites[key];
        renderer.sharedMaterial=Material;
        renderer.sortingOrder=100-Mathf.RoundToInt(y*4);
        return renderer;
    }

    public static object Main()
    {
        if(EditorApplication.isPlaying) throw new Exception("Stop Play mode before building the comparison.");
        var original=SceneManager.GetActiveScene();
        if(original.path!="Assets/Scenes/SampleScene.unity" || original.isDirty)
            throw new Exception("Expected saved SampleScene. Original scene was not changed.");
        // Detect the pipeline before making any renderer or camera choices.
        var pipeline=GraphicsSettings.currentRenderPipeline;
        bool urp=pipeline!=null && pipeline.GetType().FullName.Contains("Universal");
        if(pipeline!=null && !urp) throw new Exception("This prototype supports Built-in or URP only.");
        foreach(var key in Keys)
            if(!File.Exists(Art+"/Source/"+key+".png")) throw new Exception("Missing source: "+key);
        Directory.CreateDirectory(Art+"/Sprites");
        Directory.CreateDirectory(Art+"/Tiles");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        for(int i=0;i<Keys.Length;i++) PrepareSprite(Keys[i],Heights[i]);
        Material=new Material(Shader.Find(urp?"Universal Render Pipeline/2D/Sprite-Unlit-Default":"Sprites/Default"));
        AssetDatabase.CreateAsset(Material,Art+"/ArtReview_Unlit.mat");
        var groundTile=MakeTile("ground_tile");
        var pathTile=MakeTile("path_tile");
        string[] sizes={"Small","Medium","Large"};
        float[] factors={2f/3f,1f,4f/3f};
        var scenes=new List<string>();
        for(int variant=0;variant<3;variant++)
        {
            string scenePath=AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/ArtScaleReview_20260930_"+sizes[variant]+".unity");
            if(!EditorSceneManager.SaveScene(original,scenePath,true)) throw new Exception("Could not copy scene.");
            var scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var roots=scene.GetRootGameObjects();
            var maps=roots.SelectMany(g=>g.GetComponentsInChildren<Tilemap>(true)).ToArray();
            foreach(var map in maps)
            {
                if(map.name=="Ground_Prototype" || map.name=="EnemyPath_Prototype")
                {
                    var cells=new List<Vector3Int>(); foreach(var position in map.cellBounds.allPositionsWithin) if(map.HasTile(position)) cells.Add(position);
                    foreach(var cell in cells)
                    {
                        bool path=map.name=="EnemyPath_Prototype";
                        bool vertical=path && (map.HasTile(cell+Vector3Int.up)||map.HasTile(cell+Vector3Int.down))
                            && !(map.HasTile(cell+Vector3Int.left)&&map.HasTile(cell+Vector3Int.right));
                        map.SetTile(cell,path?pathTile:groundTile);
                        map.SetTileFlags(cell,TileFlags.None);
                        map.SetTransformMatrix(cell,Matrix4x4.Rotate(Quaternion.Euler(0,0,vertical?90:0)));
                    }
                    map.GetComponent<TilemapRenderer>().sharedMaterial=Material;
                }
                if(map.name=="Core_Prototype") map.GetComponent<TilemapRenderer>().enabled=false;
            }
            var originalPlayer=roots.SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>(true))
                .FirstOrDefault(s=>s.sprite!=null && s.sprite.name=="Guardian_Temp_Idle");
            if(originalPlayer!=null)
            {
                originalPlayer.sprite=Sprites["player"];
                originalPlayer.sharedMaterial=Material;
                originalPlayer.transform.localScale=Vector3.one*factors[variant];
                originalPlayer.sortingOrder=70;
            }
            var group=new GameObject("NewArt_SizeReview_"+sizes[variant]).transform;
            Spawn("enemy_attack",2.5f,6.5f,factors[variant],group);
            Spawn("enemy_defense",5.5f,9f,factors[variant],group);
            Spawn("enemy_buff",10.5f,11.5f,factors[variant],group);
            Spawn("tower_single",9.5f,8.5f,factors[variant],group);
            Spawn("tower_multi",13.5f,8.5f,factors[variant],group);
            Spawn("tower_support",17.5f,8.5f,factors[variant],group);
            Spawn("core",15.5f,4.5f,factors[variant],group);
            // Crowd sample uses the same sprites and scale, no combat logic or stats.
            string[] crowd={"enemy_attack","enemy_defense","enemy_buff","enemy_attack","enemy_defense","enemy_attack"};
            for(int i=0;i<crowd.Length;i++) Spawn(crowd[i],11.5f+i*1.15f,2.5f,factors[variant],group);
            var camera=roots.SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First();
            camera.allowMSAA=false;
            camera.allowHDR=false;
            camera.allowDynamicResolution=false;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            scenes.Add(scenePath);
            EditorSceneManager.CloseScene(scene,true);
            SceneManager.SetActiveScene(original);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(scenes[1],OpenSceneMode.Single);
        return new { scenes, nativePPU=32, playerScreenHeightApprox1080p=new[]{64,95,127}, assets=Keys.Length, pipeline=urp?"URP":"Built-in" };
    }
}



