using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class BuildFiveByFiveReview
{
    const string Source="Assets/Scenes/ArtReview_20260930_B_Revision2.unity";
    const string Destination="Assets/Scenes/ArtReview_20261008_5x5.unity";
    const string Art="Assets/Art/ScreenTests/20261008_5x5";
    const float Cell=.6f, Scale=.6f;
    const int Width=45, Height=25;
    static readonly Vector3Int[] Turns={new Vector3Int(0,12,0),new Vector3Int(11,12,0),new Vector3Int(11,18,0),new Vector3Int(35,18,0),new Vector3Int(35,6,0),new Vector3Int(17,6,0),new Vector3Int(17,12,0),new Vector3Int(29,12,0),new Vector3Int(29,9,0)};
    static readonly Vector3Int[] Zones={new Vector3Int(12,13,0),new Vector3Int(21,13,0),new Vector3Int(30,13,0),new Vector3Int(12,7,0),new Vector3Int(18,7,0),new Vector3Int(30,7,0)};

    static Tile MakeTile(string name,Sprite source,float ppu,bool fullTexture=false)
    {
        var rect=fullTexture?new Rect(0,0,source.texture.width,source.texture.height):source.rect;
        var sprite=Sprite.Create(source.texture,rect,new Vector2(.5f,.5f),ppu,0,SpriteMeshType.FullRect);
        sprite.name=name+"_sprite";
        var tile=ScriptableObject.CreateInstance<Tile>();tile.name=name;tile.sprite=sprite;tile.colliderType=Tile.ColliderType.None;
        AssetDatabase.CreateAsset(tile,Art+"/"+name+".asset");AssetDatabase.AddObjectToAsset(sprite,tile);
        return tile;
    }
    static Vector3 Foot(Sprite sprite,int rows)
    {
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
        ImageConversion.LoadImage(texture,File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
        var pixels=texture.GetPixels32();int l=texture.width,r=-1,b=texture.height,t=-1;
        for(int y=0;y<rows;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>=128){l=Math.Min(l,x);r=Math.Max(r,x);b=Math.Min(b,y);t=Math.Max(t,y);}
        UnityEngine.Object.DestroyImmediate(texture);
        if(r<l)throw new Exception("No lower footprint: "+sprite.name);
        return new Vector3(((l+r+1)*.5f-sprite.pivot.x)/sprite.pixelsPerUnit,((b+t+1)*.5f-sprite.pivot.y)/sprite.pixelsPerUnit,0);
    }
    static void Place(SpriteRenderer r,Tilemap ground,Vector3Int cell,int rows)
    {
        r.transform.localScale=Vector3.one*Scale;
        var target=ground.GetCellCenterWorld(cell);
        r.transform.position=target-r.transform.TransformVector(Foot(r.sprite,rows));
        r.sortingOrder=100-Mathf.RoundToInt(target.y*4);
    }
    static HashSet<Vector3Int> Route()
    {
        var cells=new HashSet<Vector3Int>();
        for(int i=0;i<Turns.Length-1;i++){
            var p=Turns[i];var delta=Turns[i+1]-p;var step=new Vector3Int(Math.Sign(delta.x),Math.Sign(delta.y),0);
            cells.Add(p);while(p!=Turns[i+1]){p+=step;cells.Add(p);}
        }
        return cells;
    }
    public static object Main()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying || scene.path!=Source || scene.isDirty)throw new Exception("Expected saved B scene in Edit mode");
        if(File.Exists(Destination) || Directory.Exists(Art))throw new Exception("5x5 review already exists; do not overwrite");
        var maps=UnityEngine.Object.FindObjectsByType<Tilemap>();
        var ground=maps.Single(m=>m.name=="Ground_Prototype");
        var oldFloor=(Tile)ground.GetTile(Vector3Int.zero);
        var straightSource=AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/ScreenTests/20260930/Revision2/Tiles/PathStraight_B.asset");
        var cornerSource=AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/ScreenTests/20260930/Revision2/Tiles/PathCorner_NorthEast_B.asset");
        if(oldFloor==null || straightSource==null || cornerSource==null)throw new Exception("Missing review tile assets");
        Directory.CreateDirectory(Art);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var floor=MakeTile("Floor_Small",oldFloor.sprite,oldFloor.sprite.texture.width/Cell,true);
        var straight=MakeTile("Path_Straight_Small",straightSource.sprite,32f/Cell);
        var corner=MakeTile("Path_Corner_Small",cornerSource.sprite,32f/Cell);
        AssetDatabase.SaveAssets();
        if(!EditorSceneManager.SaveScene(scene,Destination,true))throw new Exception("Scene copy failed");
        scene=EditorSceneManager.OpenScene(Destination,OpenSceneMode.Single);
        maps=UnityEngine.Object.FindObjectsByType<Tilemap>();ground=maps.Single(m=>m.name=="Ground_Prototype");
        ground.layoutGrid.cellSize=new Vector3(Cell,Cell,1);
        foreach(var map in maps)map.ClearAllTiles();
        for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)ground.SetTile(new Vector3Int(x,y,0),floor);
        var route=maps.Single(m=>m.name=="EnemyPath_Prototype");
        var lines=maps.Single(m=>m.name=="PathArt_WideStraight");
        var corners=maps.Single(m=>m.name=="PathArt_ConnectedCorners");
        var cells=Route();
        foreach(var p in cells){
            route.SetTile(p,straight);
            bool n=cells.Contains(p+Vector3Int.up),e=cells.Contains(p+Vector3Int.right),s=cells.Contains(p+Vector3Int.down),w=cells.Contains(p+Vector3Int.left);
            bool turn=(n?1:0)+(e?1:0)+(s?1:0)+(w?1:0)==2 && !(n&&s) && !(e&&w);
            int angle=turn?(n&&e?0:n&&w?90:s&&w?180:270):(n||s?90:0);
            var map=turn?corners:lines;map.SetTile(p,turn?corner:straight);map.SetTileFlags(p,TileFlags.None);map.SetTransformMatrix(p,Matrix4x4.Rotate(Quaternion.Euler(0,0,angle)));
        }
        var renderers=UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Where(r=>r.gameObject.scene==scene).ToArray();
        var towerCells=new Dictionary<string,Vector3Int>{{"tower_single",new Vector3Int(16,17,0)},{"tower_multi",new Vector3Int(25,17,0)},{"tower_support",new Vector3Int(35,17,0)}};
        var occupied=maps.Single(m=>m.name=="TowerOccupiedCells_Prototype");
        foreach(var pair in towerCells){Place(renderers.Single(r=>r.name==pair.Key),ground,pair.Value,pair.Key=="tower_support"?16:18);occupied.SetTile(pair.Value,floor);}
        Place(renderers.Single(r=>r.name=="core"),ground,new Vector3Int(31,9,0),28);
        maps.Single(m=>m.name=="Core_Prototype").SetTile(new Vector3Int(31,9,0),floor);
        var enemies=renderers.Where(r=>r.name.StartsWith("enemy_")).OrderByDescending(r=>r.transform.position.y).ThenBy(r=>r.transform.position.x).ToArray();
        var enemyCells=new[]{new Vector3Int(24,20,0),new Vector3Int(11,16,0),new Vector3Int(5,12,0),new Vector3Int(23,4,0),new Vector3Int(25,4,0),new Vector3Int(27,4,0),new Vector3Int(29,4,0),new Vector3Int(32,4,0),new Vector3Int(35,4,0)};
        if(enemies.Length!=enemyCells.Length)throw new Exception("Expected 9 review enemies");
        for(int i=0;i<enemies.Length;i++)Place(enemies[i],ground,enemyCells[i],5);
        var movement=UnityEngine.Object.FindObjectsByType<PrototypeGridMovement>().Single(m=>m.gameObject.scene==scene);
        var settings=new SerializedObject(movement);settings.FindProperty("movementTileSize").vector2IntValue=Vector2Int.one;settings.FindProperty("movementTileOrigin").vector2IntValue=Vector2Int.zero;settings.FindProperty("spriteOffset").vector3Value=Vector3.zero;settings.ApplyModifiedProperties();
        movement.transform.localScale=Vector3.one;movement.transform.position=ground.GetCellCenterWorld(new Vector3Int(15,10,0));
        var graphic=movement.transform.Find("PlayerVisual_Centered").GetComponent<SpriteRenderer>();
        Place(graphic,ground,new Vector3Int(15,10,0),5);
        var camera=UnityEngine.Object.FindObjectsByType<Camera>().Single(c=>c.gameObject.scene==scene && c.CompareTag("MainCamera"));
        camera.transform.position=new Vector3(Width*Cell*.5f,Height*Cell*.5f,camera.transform.position.z);
        foreach(var map in maps){map.RefreshAllTiles();EditorUtility.SetDirty(map);}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        // Reload Sprite subassets before final persistence; transient handles can be lost on scene copy.
        RepairTiles();
        var report=new {scene=Destination,width=Width,height=Height,cellWorldSize=Cell,unitScale=Scale,routeCells=cells.Count,zones=Zones.Select(z=>new{x=z.x,y=z.y,width=5,height=5}).ToArray(),start=new{x=15,y=10}};
        File.WriteAllText("docs/art/5x5_review_20261008_layout.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        return Newtonsoft.Json.JsonConvert.SerializeObject(report);
    }
    public static object Verify()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!=Destination)throw new Exception("Wrong scene");
        var maps=UnityEngine.Object.FindObjectsByType<Tilemap>();var ground=maps.Single(m=>m.name=="Ground_Prototype");var path=maps.Single(m=>m.name=="EnemyPath_Prototype");
        var core=maps.Single(m=>m.name=="Core_Prototype");var towers=maps.Single(m=>m.name=="TowerOccupiedCells_Prototype");
        Func<Vector3Int,bool> clear=p=>ground.HasTile(p)&&!path.HasTile(p)&&!core.HasTile(p)&&!towers.HasTile(p);
        var reachable=new HashSet<Vector3Int>();var queue=new Queue<Vector3Int>();var start=new Vector3Int(15,10,0);queue.Enqueue(start);reachable.Add(start);
        while(queue.Count>0){var p=queue.Dequeue();foreach(var d in new[]{Vector3Int.up,Vector3Int.down,Vector3Int.left,Vector3Int.right}){var q=p+d;if(clear(q)&&reachable.Add(q))queue.Enqueue(q);}}
        foreach(var origin in Zones){int space=0;for(int x=0;x<5;x++)for(int y=0;y<5;y++){var p=origin+new Vector3Int(x,y,0);if(!ground.HasTile(p)||path.HasTile(p)||core.HasTile(p))throw new Exception("5x5 zone cut by path/core: "+p);if(reachable.Contains(p))space++;}if(space<24)throw new Exception("Zone unreachable: "+origin+" cells="+space);}
        var movement=UnityEngine.Object.FindObjectsByType<PrototypeGridMovement>().Single(m=>m.gameObject.scene==scene);
        if(new SerializedObject(movement).FindProperty("movementTileSize").vector2IntValue!=Vector2Int.one)throw new Exception("Wrong movement unit");
        var graphic=movement.transform.Find("PlayerVisual_Centered").GetComponent<SpriteRenderer>();
        if(Vector3.Distance(graphic.transform.TransformPoint(Foot(graphic.sprite,5)),movement.transform.position)>.001f)throw new Exception("Feet not centered");
        if(Mathf.Abs(ground.layoutGrid.cellSize.x-Cell)>.0001f)throw new Exception("Wrong tile size");
        return "PASS: 45x25 small tiles; six 5x5 regions reachable; player feet centered; one visible tile per move; reachable="+reachable.Count;
    }
    public static object RepairTiles()
    {
        if(EditorApplication.isPlaying || SceneManager.GetActiveScene().path!=Destination)throw new Exception("Expected 5x5 scene in Edit mode");
        AssetDatabase.SaveAssets();
        var tiles=new Dictionary<string,Tile>();
        foreach(var key in new[]{"Floor_Small","Path_Straight_Small","Path_Corner_Small"}) {
            var asset=Art+"/"+key+".asset";
            AssetDatabase.ImportAsset(asset,ImportAssetOptions.ForceSynchronousImport);
            var tile=AssetDatabase.LoadAllAssetsAtPath(asset).OfType<Tile>().Single();
            var sprite=AssetDatabase.LoadAllAssetsAtPath(asset).OfType<Sprite>().Single();
            tile.sprite=sprite;EditorUtility.SetDirty(tile);tiles[key]=tile;
        }
        AssetDatabase.SaveAssets();
        var maps=UnityEngine.Object.FindObjectsByType<Tilemap>();
        foreach(var map in maps)map.ClearAllTiles();
        var ground=maps.Single(m=>m.name=="Ground_Prototype");
        for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)ground.SetTile(new Vector3Int(x,y,0),tiles["Floor_Small"]);
        var cells=Route();var route=maps.Single(m=>m.name=="EnemyPath_Prototype");
        foreach(var p in cells){
            route.SetTile(p,tiles["Path_Straight_Small"]);
            bool n=cells.Contains(p+Vector3Int.up),e=cells.Contains(p+Vector3Int.right),s=cells.Contains(p+Vector3Int.down),w=cells.Contains(p+Vector3Int.left);
            bool turn=(n?1:0)+(e?1:0)+(s?1:0)+(w?1:0)==2 && !(n&&s) && !(e&&w);
            int angle=turn?(n&&e?0:n&&w?90:s&&w?180:270):(n||s?90:0);
            var map=maps.Single(m=>m.name==(turn?"PathArt_ConnectedCorners":"PathArt_WideStraight"));
            map.SetTile(p,tiles[turn?"Path_Corner_Small":"Path_Straight_Small"]);map.SetTileFlags(p,TileFlags.None);map.SetTransformMatrix(p,Matrix4x4.Rotate(Quaternion.Euler(0,0,angle))*Matrix4x4.Scale(turn?new Vector3(2f/3f,2f/3f,1):new Vector3(1,2f/3f,1)));
        }
        var towers=maps.Single(m=>m.name=="TowerOccupiedCells_Prototype");
        foreach(var p in new[]{new Vector3Int(14,15,0),new Vector3Int(23,15,0),new Vector3Int(32,15,0)})towers.SetTile(p,tiles["Floor_Small"]);
        maps.Single(m=>m.name=="Core_Prototype").SetTile(new Vector3Int(29,9,0),tiles["Floor_Small"]);
        foreach(var map in maps){map.RefreshAllTiles();EditorUtility.SetDirty(map);}
        var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Restored persistent tile references";
    }
    public static object CorrectActualFiveCells()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying || scene.path!=Destination || scene.isDirty)throw new Exception("Expected saved review scene in Edit mode");
        var asset=Art+"/Floor_Small.asset";
        var tile=AssetDatabase.LoadAssetAtPath<Tile>(asset);
        var old=tile.sprite;
        var sprite=Sprite.Create(old.texture,new Rect(0,0,32,32),new Vector2(.5f,.5f),32f/Cell,0,SpriteMeshType.FullRect);
        sprite.name="Floor_Small_sprite";
        AssetDatabase.AddObjectToAsset(sprite,tile);tile.sprite=sprite;
        UnityEngine.Object.DestroyImmediate(old,true);EditorUtility.SetDirty(tile);AssetDatabase.SaveAssets();
        RepairTiles();
        var ground=UnityEngine.Object.FindObjectsByType<Tilemap>().Single(m=>m.name=="Ground_Prototype");
        var renderers=UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Where(r=>r.gameObject.scene==scene).ToArray();
        Place(renderers.Single(r=>r.name=="tower_single"),ground,new Vector3Int(14,15,0),18);
        Place(renderers.Single(r=>r.name=="tower_multi"),ground,new Vector3Int(23,15,0),18);
        Place(renderers.Single(r=>r.name=="tower_support"),ground,new Vector3Int(32,15,0),16);
        Place(renderers.Single(r=>r.name=="core"),ground,new Vector3Int(29,9,0),28);
        var enemies=renderers.Where(r=>r.name.StartsWith("enemy_")).OrderByDescending(r=>r.transform.position.y).ThenBy(r=>r.transform.position.x).ToArray();
        var cells=new[]{new Vector3Int(24,18,0),new Vector3Int(11,15,0),new Vector3Int(5,12,0),new Vector3Int(20,6,0),new Vector3Int(22,6,0),new Vector3Int(24,6,0),new Vector3Int(26,6,0),new Vector3Int(29,6,0),new Vector3Int(32,6,0)};
        for(int i=0;i<enemies.Length;i++)Place(enemies[i],ground,cells[i],5);
        var movement=UnityEngine.Object.FindObjectsByType<PrototypeGridMovement>().Single();
        movement.transform.position=ground.GetCellCenterWorld(new Vector3Int(15,10,0));
        Place(movement.transform.Find("PlayerVisual_Centered").GetComponent<SpriteRenderer>(),ground,new Vector3Int(15,10,0),5);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return VerifyActualWidths();
    }
    public static object VerifyActualWidths()
    {
        Verify();
        var ground=UnityEngine.Object.FindObjectsByType<Tilemap>().Single(m=>m.name=="Ground_Prototype");
        var path=UnityEngine.Object.FindObjectsByType<Tilemap>().Single(m=>m.name=="EnemyPath_Prototype");
        var sprite=((Tile)ground.GetTile(Vector3Int.zero)).sprite;
        if(sprite.rect.width!=32 || sprite.rect.height!=32 || Mathf.Abs(sprite.bounds.size.x-Cell)>.001f)throw new Exception("Floor is not one visible slab per cell");
        foreach(var line in new[]{new[]{new Vector3Int(11,10,0),new Vector3Int(17,10,0)},new[]{new Vector3Int(24,12,0),new Vector3Int(24,18,0)},new[]{new Vector3Int(24,6,0),new Vector3Int(24,12,0)},new[]{new Vector3Int(29,10,0),new Vector3Int(35,10,0)}}){
            // Left boundary extends downward only visually as open connector: count against the aligned path column.
            var a=line[0];var b=line[1];var step=new Vector3Int(Math.Sign(b.x-a.x),Math.Sign(b.y-a.y),0);int count=0;
            for(var p=a+step;p!=b;p+=step){if(!ground.HasTile(p)||path.HasTile(p))throw new Exception("Corridor cut: "+p);count++;}
            if(count!=5)throw new Exception("Actual corridor width is "+count);
        }
        return "PASS: left/top/bottom/right clear widths = 5/5/5/5 visible slabs; one slab = one movement cell";
    }
    public static object CleanCountableFloor()
    {
        if(EditorApplication.isPlaying || SceneManager.GetActiveScene().path!=Destination)throw new Exception("Expected review in Edit mode");
        var texture=new Texture2D(32,32,TextureFormat.RGBA32,false);
        for(int y=0;y<32;y++)for(int x=0;x<32;x++){
            var c=new Color32(169,166,157,255);
            if(x==0||y==0)c=new Color32(126,125,120,255);
            else if(x==31||y==31)c=new Color32(188,185,174,255);
            else if(x==1||y==1)c=new Color32(155,153,145,255);
            texture.SetPixel(x,y,c);
        }
        texture.Apply();var png=Art+"/Ground_Countable.png";File.WriteAllBytes(png,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(png,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(png);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=32f/Cell;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var asset=Art+"/Floor_Small.asset";var tile=AssetDatabase.LoadAssetAtPath<Tile>(asset);
        var sprite=AssetDatabase.LoadAllAssetsAtPath(asset).OfType<Sprite>().Single();UnityEngine.Object.DestroyImmediate(sprite,true);
        var source=AssetDatabase.LoadAssetAtPath<Sprite>(png);
        var sub=Sprite.Create(source.texture,source.rect,new Vector2(.5f,.5f),32f/Cell,0,SpriteMeshType.FullRect);sub.name="Floor_Small_sprite";AssetDatabase.AddObjectToAsset(sub,tile);tile.sprite=sub;EditorUtility.SetDirty(tile);AssetDatabase.SaveAssets();RepairTiles();return VerifyActualWidths();
    }
    public static object LiveStep(string action)
    {
        if(!EditorApplication.isPlaying)throw new Exception("Expected Play mode");
        if(action=="background"){Application.runInBackground=true;return "Enabled background frames for CLI play verification";}
        var m=UnityEngine.Object.FindObjectsByType<PrototypeGridMovement>().Single();
        var ground=UnityEngine.Object.FindObjectsByType<Tilemap>().Single(t=>t.name=="Ground_Prototype");
        if(action=="start"){if(!m.TryStep(Vector2Int.right))throw new Exception("Open corridor move refused");return "Started one small tile move";}
        if(m.CurrentCell!=new Vector3Int(16,10,0)||Vector3.Distance(m.transform.position,ground.GetCellCenterWorld(m.CurrentCell))>.001f)throw new Exception("Move failed to finish at next center: cell="+m.CurrentCell+" position="+m.transform.position+" expected="+ground.GetCellCenterWorld(m.CurrentCell)+" paused="+EditorApplication.isPaused+" timeScale="+Time.timeScale+" enabled="+m.enabled);
        return "PASS: small tile move ended at "+m.transform.position;
    }
}
