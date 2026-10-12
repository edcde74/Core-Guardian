using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine.SceneManagement;

public static class BuildArtRevision2
{
    const string Art="Assets/Art/ScreenTests/20260930/Revision2";
    static Texture2D Sample(string key,int height,int width=0,bool opaque=false)
    {
        var t=new Texture2D(2,2,TextureFormat.RGBA32,false);
        ImageConversion.LoadImage(t,File.ReadAllBytes(Art+"/Source/"+key+".png"));
        var p=t.GetPixels32();
        int l=0,b=0,r=t.width-1,top=t.height-1;
        if(!opaque)
        {
            if(p.Count(c=>c.a==0)<p.Length/10) throw new Exception("Missing alpha: "+key);
            l=t.width;b=t.height;r=-1;top=-1;
            for(int y=0;y<t.height;y++) for(int x=0;x<t.width;x++)
                if(p[y*t.width+x].a>=128) {l=Math.Min(l,x);b=Math.Min(b,y);r=Math.Max(r,x);top=Math.Max(top,y);}
        }
        if(width==0) width=Mathf.RoundToInt((float)(r-l+1)/(top-b+1)*height);
        var result=new Texture2D(width,height,TextureFormat.RGBA32,false);
        var colors=new Color32[width*height];
        for(int y=0;y<height;y++) for(int x=0;x<width;x++)
        {
            int sx=Math.Min(r,l+(int)((x+0.5f)*(r-l+1)/width));
            int sy=Math.Min(top,b+(int)((y+0.5f)*(top-b+1)/height));
            var c=p[sy*t.width+sx]; c.a=opaque?(byte)255:(c.a>=128?(byte)255:(byte)0);
            colors[y*width+x]=c;
        }
        result.SetPixels32(colors);result.Apply();
        UnityEngine.Object.DestroyImmediate(t);
        return result;
    }

    static string Import(string key,Texture2D texture,bool ground=false,bool multiple=false)
    {
        var path=Art+"/Sprites/"+key+".png";
        File.WriteAllBytes(path,ImageConversion.EncodeToPNG(texture));
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=multiple?SpriteImportMode.Multiple:SpriteImportMode.Single;
        importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;
        importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency=true;importer.maxTextureSize=256;
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
        settings.spriteMeshType=SpriteMeshType.FullRect;
        settings.spriteAlignment=(int)(ground?SpriteAlignment.Center:SpriteAlignment.BottomCenter);
        importer.SetTextureSettings(settings);importer.SaveAndReimport();
        UnityEngine.Object.DestroyImmediate(texture);
        return path;
    }

    static Sprite[] SliceFloor(string path)
    {
        var importer=AssetImporter.GetAtPath(path);
        var factory=new SpriteDataProviderFactories();factory.Init();
        var provider=factory.GetSpriteEditorDataProviderFromObject(importer);
        if(provider==null) throw new Exception("No sprite data provider; slicing aborted.");
        provider.InitSpriteEditorDataProvider();
        var edit=provider.GetDataProvider<ISpriteFrameEditCapability>();
        if(edit==null) throw new Exception("No edit capability; slicing aborted.");
        var capability=edit.GetEditCapability();
        foreach(var requirement in new[]{EEditCapability.CreateAndDeleteSprite,EEditCapability.EditSpriteRect,EEditCapability.EditSpriteName,EEditCapability.EditPivot})
            if(!capability.HasCapability(requirement)) throw new Exception("Unsupported "+requirement+"; slicing aborted.");
        var names=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if(names==null) throw new Exception("No name/file-ID provider; slicing aborted.");
        var rects=new List<SpriteRect>();var pairs=new List<SpriteNameFileIdPair>();
        for(int y=0;y<2;y++) for(int x=0;x<2;x++)
        {
            var id=GUID.Generate();var name="slab_"+x+"_"+y;
            rects.Add(new SpriteRect {name=name,spriteID=id,rect=new Rect(x*32,y*32,32,32),alignment=SpriteAlignment.Center,pivot=new Vector2(.5f,.5f)});
            pairs.Add(new SpriteNameFileIdPair(name,id));
        }
        provider.SetSpriteRects(rects.ToArray());names.SetNameFileIdPairs(pairs);provider.Apply();
        ((TextureImporter)importer).SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
    }

    static Tile TileAsset(string name,Sprite sprite)
    {
        var path=Art+"/Tiles/"+name+".asset";
        var tile=AssetDatabase.LoadAssetAtPath<Tile>(path);
        if(tile==null){tile=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(tile,path);}
        tile.sprite=sprite;tile.colliderType=Tile.ColliderType.None;EditorUtility.SetDirty(tile);
        return tile;
    }

    public static object Main()
    {
        if(EditorApplication.isPlaying) throw new Exception("Stop play mode first.");
        var scene=SceneManager.GetActiveScene();
        if(scene.isDirty || !scene.path.Contains("ArtScaleReview_20260930_Medium")) throw new Exception("Expected saved medium comparison scene.");
        var pipeline=GraphicsSettings.currentRenderPipeline;
        if(pipeline==null || !pipeline.GetType().FullName.Contains("Universal")) throw new Exception("Expected current URP pipeline.");
        Directory.CreateDirectory(Art+"/Sprites");Directory.CreateDirectory(Art+"/Tiles");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var replacements=new Dictionary<string,Sprite>();
        string[] old={"enemy_attack","enemy_defense","enemy_buff"};int[] heights={64,48,80};
        for(int i=0;i<old.Length;i++)
        {
            var path=Import(old[i]+"_v2",Sample(old[i]+"_v2",heights[i]));
            replacements[old[i]]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        var slab=SliceFloor(Import("ground_slab_v2",Sample("ground_slab_v2",64,64,true),true,true));
        var floorTiles=slab.ToDictionary(s=>s.name,s=>TileAsset(s.name,s));

        // Native corridor cross-section is 48px, versus 32px in revision 1.
        var straight=Sample("path_straight_v2",48,32,true);
        var corner=Sample("path_corner_v2",48,48,true);
        // Match the open-edge bands to the straight tile cross-section.
        // The generated corner provides the interior L; exact edge bands provide the joins.
        var sp=straight.GetPixels32();var cp=corner.GetPixels32();
        for(int y=40;y<48;y++) for(int x=0;x<48;x++) cp[y*48+x]=sp[(47-x)*32+31];
        for(int x=40;x<48;x++) for(int y=0;y<48;y++) cp[y*48+x]=sp[y*32];
        corner.SetPixels32(cp);corner.Apply();
        var straightSprite=AssetDatabase.LoadAssetAtPath<Sprite>(Import("path_straight_v2",straight,true));
        var cornerSprite=AssetDatabase.LoadAssetAtPath<Sprite>(Import("path_corner_v2",corner,true));
        var straightTile=TileAsset("PathStraight_B",straightSprite);
        var cornerTile=TileAsset("PathCorner_NorthEast_B",cornerSprite);
        AssetDatabase.SaveAssets();
        string destination=AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/ArtReview_20260930_B_Revision2.unity");
        if(!EditorSceneManager.SaveScene(scene,destination,true)) throw new Exception("Could not copy comparison scene.");
        scene=EditorSceneManager.OpenScene(destination,OpenSceneMode.Single);
        floorTiles=slab.ToDictionary(s=>s.name,s=>AssetDatabase.LoadAssetAtPath<Tile>(Art+"/Tiles/"+s.name+".asset"));
        straightTile=AssetDatabase.LoadAssetAtPath<Tile>(Art+"/Tiles/PathStraight_B.asset");
        cornerTile=AssetDatabase.LoadAssetAtPath<Tile>(Art+"/Tiles/PathCorner_NorthEast_B.asset");

        foreach(var r in UnityEngine.Object.FindObjectsByType<SpriteRenderer>())
            if(r.sprite!=null && replacements.TryGetValue(r.sprite.name,out var replacement)) r.sprite=replacement;
        var maps=UnityEngine.Object.FindObjectsByType<Tilemap>();
        var ground=maps.First(t=>t.name=="Ground_Prototype");
        var pathMap=maps.First(t=>t.name=="EnemyPath_Prototype");
        var material=ground.GetComponent<TilemapRenderer>().sharedMaterial;
        foreach(var p in ground.cellBounds.allPositionsWithin)
            if(ground.HasTile(p)) ground.SetTile(p,floorTiles["slab_"+(p.x%2)+"_"+(p.y%2)]);
        pathMap.GetComponent<TilemapRenderer>().enabled=false;
        var pathVisual=new GameObject("PathArt_WideStraight").AddComponent<Tilemap>();
        pathVisual.transform.SetParent(pathMap.transform.parent,false);
        var pathRenderer=pathVisual.gameObject.AddComponent<TilemapRenderer>();
        pathRenderer.sharedMaterial=material;pathRenderer.sortingOrder=1;
        var cornerVisual=new GameObject("PathArt_ConnectedCorners").AddComponent<Tilemap>();
        cornerVisual.transform.SetParent(pathMap.transform.parent,false);
        var cornerRenderer=cornerVisual.gameObject.AddComponent<TilemapRenderer>();
        cornerRenderer.sharedMaterial=material;cornerRenderer.sortingOrder=2;
        int corners=0,straightCount=0;
        var cornerDetails=new List<object>();
        foreach(var p in pathMap.cellBounds.allPositionsWithin)
        {
            if(!pathMap.HasTile(p)) continue;
            bool n=pathMap.HasTile(p+Vector3Int.up),e=pathMap.HasTile(p+Vector3Int.right),s=pathMap.HasTile(p+Vector3Int.down),w=pathMap.HasTile(p+Vector3Int.left);
            int count=(n?1:0)+(e?1:0)+(s?1:0)+(w?1:0);
            bool turn=count==2 && !(n&&s) && !(e&&w);
            if(count>2) throw new Exception("Branch requires a separate intersection tile.");
            if(turn)
            {
                int angle=n&&e?0:n&&w?90:s&&w?180:270;
                cornerVisual.SetTile(p,cornerTile);cornerVisual.SetTileFlags(p,TileFlags.None);
                cornerVisual.SetTransformMatrix(p,Matrix4x4.Rotate(Quaternion.Euler(0,0,angle)));
                corners++;cornerDetails.Add(new {x=p.x,y=p.y,angle});
            }
            else
            {
                int angle=n||s?90:0;
                pathVisual.SetTile(p,straightTile);pathVisual.SetTileFlags(p,TileFlags.None);
                pathVisual.SetTransformMatrix(p,Matrix4x4.Rotate(Quaternion.Euler(0,0,angle)));
                straightCount++;
            }
        }
        if(corners!=7) throw new Exception("Expected seven route corners; got "+corners);
        // Sprite bases sit near the corridor centreline; no combat components are added.
        var originalPlayer=UnityEngine.Object.FindAnyObjectByType<PrototypeGridMovement>();
        if(originalPlayer!=null) originalPlayer.transform.position=new Vector3(6.5f,5.61f,0);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return new { scene=destination, corners, straightCount, cornerDetails, visualPathWidth=1.5f, floorBlockCells=2, enemyTypes=replacements.Count };
    }
}


