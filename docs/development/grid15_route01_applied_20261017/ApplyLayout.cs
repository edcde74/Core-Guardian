using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class ApplyLayout
{
    const string Source="Assets/Art/Environment/AbandonedFortressB/Route01_FlagstoneSource.png";
    static readonly Vector2Int[] Route={new(0,10),new(10,10),new(10,16),new(30,16),new(30,5),new(15,5),new(15,10),new(25,10),new(25,8)};
    static void Require(bool v,string m){if(!v)throw new Exception(m);}
    static Sprite[] Import()
    {
        AssetDatabase.ImportAsset(Source,ImportAssetOptions.ForceSynchronousImport);
        var imp=(TextureImporter)AssetImporter.GetAtPath(Source);
        imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Multiple;
        imp.spritePixelsPerUnit=100;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;
        imp.textureCompression=TextureImporterCompression.Uncompressed;imp.npotScale=TextureImporterNPOTScale.None;
        imp.maxTextureSize=2048;imp.alphaIsTransparency=false;imp.wrapMode=TextureWrapMode.Clamp;
        var settings=new TextureImporterSettings();imp.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;imp.SetTextureSettings(settings);
        imp.SaveAndReimport();imp.GetSourceTextureWidthAndHeight(out int w,out int h);
        Require(w==1672&&h==941,"Unexpected approved reference dimensions");
        var factories=new SpriteDataProviderFactories();factories.Init();var provider=factories.GetSpriteEditorDataProviderFromObject(imp);provider.InitSpriteEditorDataProvider();
        var capability=provider.GetDataProvider<ISpriteFrameEditCapability>();
        Require(capability!=null&&capability.GetEditCapability().HasCapability(EEditCapability.CreateAndDeleteSprite),"Sprite slicing unavailable");
        var rects=new[]{
            new SpriteRect{name="Route01_Stone0",rect=new Rect(530,h-294,44,32),alignment=SpriteAlignment.Center,pivot=new Vector2(.5f,.5f),spriteID=GUID.Generate()},
            new SpriteRect{name="Route01_Stone1",rect=new Rect(575,h-294,44,32),alignment=SpriteAlignment.Center,pivot=new Vector2(.5f,.5f),spriteID=GUID.Generate()},
            new SpriteRect{name="Route01_Stone2",rect=new Rect(620,h-294,44,32),alignment=SpriteAlignment.Center,pivot=new Vector2(.5f,.5f),spriteID=GUID.Generate()},
            new SpriteRect{name="Route01_Curb",rect=new Rect(575,h-305,44,11),alignment=SpriteAlignment.Center,pivot=new Vector2(.5f,.5f),spriteID=GUID.Generate()}};
        provider.SetSpriteRects(rects);var names=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();Require(names!=null,"Missing sprite ID provider");
        names.SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));provider.Apply();imp.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(Source).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
    }
    static void Piece(Transform root,Sprite sprite,Material material,Vector3 pos,Vector2 size,string name,int order,Color tint,float angle=0)
    {
        var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=pos;
        go.transform.rotation=Quaternion.Euler(0,0,angle);go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);
        var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=material;r.sortingOrder=order;r.color=tint;
    }
    public static object Main()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        Require(!EditorApplication.isPlaying&&!EditorApplication.isCompiling&&!EditorUtility.scriptCompilationFailed&&!scene.isDirty,"Clean stopped editor required");
        Require(scene.path=="Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity","Wrong scene");
        Require(GameObject.Find("CoreGuardian_Route01_VisualOnly")==null,"Already applied: inspect instead of rebuilding");
        var g=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
        var grid=(Grid)g.ground.layoutGrid;var floor=g.ground.GetTile(Vector3Int.zero);
        var pathTile=g.enemyPath.GetTile(new Vector3Int(0,12,0));var coreTile=g.coreMap.GetTile(new Vector3Int(29,9,0));
        Require(floor!=null&&pathTile!=null&&coreTile!=null,"Missing source logical tiles");
        var sprites=Import();var stones=sprites.Where(s=>s.name.StartsWith("Route01_Stone")).ToArray();var curb=sprites.Single(s=>s.name=="Route01_Curb");
        Undo.RegisterCompleteObjectUndo(grid,"15 percent larger actual cells");Undo.RecordObject(grid.transform,"Compact board centered in approved frame");
        grid.cellSize=new Vector3(.69f,.69f,1);grid.transform.position=new Vector3(.045f,-.09f,0);
        foreach(var map in new[]{g.ground,g.enemyPath,g.coreMap,g.occupied}){Undo.RegisterCompleteObjectUndo(map,"Approved compact layout");map.ClearAllTiles();}
        for(int y=0;y<22;y++)for(int x=0;x<39;x++)g.ground.SetTile(new Vector3Int(x,y,0),floor);
        var cells=new HashSet<Vector2Int>();for(int i=1;i<Route.Length;i++){
            var p=Route[i-1];var delta=Route[i]-p;var step=new Vector2Int(Math.Sign(delta.x),Math.Sign(delta.y));cells.Add(p);while(p!=Route[i]){p+=step;cells.Add(p);}}
        foreach(var p in cells)g.enemyPath.SetTile(new Vector3Int(p.x,p.y,0),pathTile);
        g.coreMap.SetTile(new Vector3Int(25,8,0),coreTile);
        foreach(var map in new[]{g.ground,g.enemyPath,g.coreMap,g.occupied}){map.CompressBounds();map.GetComponent<TilemapRenderer>().enabled=false;EditorUtility.SetDirty(map);}
        foreach(var name in new[]{"PathArt_WideStraight","PathArt_ConnectedCorners"}){
            var map=GameObject.Find(name).GetComponent<Tilemap>();Undo.RegisterCompleteObjectUndo(map,"Retire old path artwork");map.ClearAllTiles();map.CompressBounds();map.GetComponent<TilemapRenderer>().enabled=false;}
        var so=new SerializedObject(g);so.FindProperty("boardSize").vector2IntValue=new Vector2Int(39,22);so.FindProperty("coreCell").vector2IntValue=new Vector2Int(25,8);
        var route=so.FindProperty("routeCells");route.arraySize=Route.Length;for(int i=0;i<Route.Length;i++)route.GetArrayElementAtIndex(i).vector2IntValue=Route[i];so.ApplyModifiedProperties();
        Undo.RecordObject(g.player.transform,"Approved compact player start");g.player.transform.position=g.ground.GetCellCenterWorld(new Vector3Int(13,9,0));
        var core=GameObject.Find("NewArt_SizeReview_Medium").transform.Find("core");Undo.RecordObject(core,"Core follows logical cell");core.position=g.ground.GetCellCenterWorld(new Vector3Int(25,8,0));
        var markers=GameObject.Find("EnemyRoute_Waypoints_Prototype").transform.Cast<Transform>().ToArray();Require(markers.Length==Route.Length,"Unexpected route marker count");
        for(int i=0;i<Route.Length;i++){Undo.RecordObject(markers[i],"Route marker follows runtime route");markers[i].position=g.ground.GetCellCenterWorld(new Vector3Int(Route[i].x,Route[i].y,0));}
        var visual=new GameObject("CoreGuardian_Route01_VisualOnly");visual.transform.SetParent(GameObject.Find("CoreGuardian_MapArt_B").transform,false);Undo.RegisterCreatedObjectUndo(visual,"Approved stone road");
        var mat=GameObject.Find("Background_Ruins_VisualOnly").GetComponent<SpriteRenderer>().sharedMaterial;
        foreach(var p in cells){
            Vector3 center=g.ground.GetCellCenterWorld(new Vector3Int(p.x,p.y,0));int hash=(p.x*73856093^p.y*19349663)&0x7fffffff;
            Piece(visual.transform,stones[hash%stones.Length],mat,center,Vector2.one*.69f,"Road stone "+p,1,new Color(1.3f,1.3f,1.3f,1));
            foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right})if(!cells.Contains(p+d)){
                // Curbs are low decoration inside the blocked road cell, not added collision or build exclusion.
                Piece(visual.transform,curb,mat,center+(Vector3)(Vector2)d*(.345f-.05f),new Vector2(.69f,.1f),"Low curb "+p+" "+d,2,new Color(1.25f,1.25f,1.25f,1),d.y!=0?0:90);
            }
        }
        Require(visual.GetComponentsInChildren<Collider2D>().Length==0&&visual.GetComponentsInChildren<MonoBehaviour>().Length==0,"Road visual must have no gameplay behavior");
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
        var result=new{board="39x22",cell=.69,origin=grid.transform.position.ToString(),floor=858,road=cells.Count,coreCell="25,8",playerCell="13,9",sprites=sprites.Select(s=>s.name).ToArray(),roadRenderers=visual.GetComponentsInChildren<SpriteRenderer>().Length,cameraChanged=false,sceneSaved=false};
        File.WriteAllText("docs/development/grid15_route01_applied_20261017/setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
