using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class VerifyNatural01
{
    const string Dir="docs/development/map_natural_01_applied_20261017/";
    static void Require(bool value,string reason){if(!value)throw new Exception(reason);}
    public static object Main(string filename="edit_invariants.json")
    {
        Require(!EditorApplication.isPlaying&&!EditorApplication.isCompiling,"Edit verification only");
        var before=JObject.Parse(File.ReadAllText("docs/development/map_B_applied_20261017/before.json"));
        var game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        Require(scene.path==(string)before["scene"]&&UnityEngine.SceneManagement.SceneManager.sceneCount==1,"Scene changed");
        var results=new List<object>(); int checkedCells=0;
        foreach(var old in before["tilemaps"]){
            string name=(string)old["name"];
            var map=UnityEngine.Object.FindObjectsByType<Tilemap>().Single(t=>t.name==name);
            var oldCells=old["cells"].ToArray();int currentCount=0;
            foreach(var p in map.cellBounds.allPositionsWithin)if(map.HasTile(p))currentCount++;
            Require(currentCount==oldCells.Length,"Cell count changed: "+name);
            bool floor=map==game.ground||map==game.coreMap;
            foreach(var cell in oldCells){
                var p=new Vector3Int((int)cell["x"],(int)cell["y"],(int)cell["z"]);
                Require(map.HasTile(p),"Missing logical tile "+name+p);
                Require(map.GetTransformMatrix(p).ToString()==(string)cell["matrix"],"Tile matrix changed "+name+p);
                Require(map.GetColor(p).ToString()==(string)cell["color"],"Tile color changed "+name+p);
                if(!floor)Require(AssetDatabase.GetAssetPath(map.GetTile(p))==(string)cell["tile"],"Nonfloor asset changed "+name+p);
                else Require(map.GetTile<Tile>(p).colliderType==Tile.ColliderType.None,"New floor collision");
                checkedCells++;
            }
            var oldRender=JObject.Parse((string)old["renderer"]);
            var newRender=JObject.Parse(EditorJsonUtility.ToJson(map.GetComponent<TilemapRenderer>()));
            if(floor){
                foreach(var o in oldRender.Descendants().OfType<JProperty>().Where(p=>p.Name=="m_Materials"||p.Name=="m_Enabled").ToArray())o.Remove();
                foreach(var o in newRender.Descendants().OfType<JProperty>().Where(p=>p.Name=="m_Materials"||p.Name=="m_Enabled").ToArray())o.Remove();}
            Require(JToken.DeepEquals(oldRender,newRender),"Renderer changed beyond floor material: "+name);
            results.Add(new{name,cells=currentCount,logicalCoordinatesMatricesColorsUnchanged=true,nonfloorAssetsUnchanged=!floor});
        }
        Require(EditorJsonUtility.ToJson(Camera.main)==(string)before["camera"],"Camera changed");
        Require(EditorJsonUtility.ToJson(Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>())==(string)before["cameraData"],"Camera pipeline changed");
        foreach(var old in before["roots"]){var root=scene.GetRootGameObjects().Single(r=>r.name==(string)old["name"]);
            Require(EditorJsonUtility.ToJson(root.transform)==(string)old["json"],"Original root moved: "+root.name);}
        foreach(var old in before["gameplay"]){var component=game.GetComponents<Component>().Single(c=>c.GetType().FullName==(string)old["type"]);
            Require(EditorJsonUtility.ToJson(component)==(string)old["json"],"Gameplay/feedback component changed: "+component.GetType().Name);}
        Require(EditorJsonUtility.ToJson(game.player)==(string)before["movement"][0],"Movement settings changed");
        Require(game.ground.cellBounds.size==new Vector3Int(45,25,1),"Board dimensions changed");
        Require(game.ground.layoutGrid.cellSize==new Vector3(.6f,.6f,1),"Cell size changed");
        var added=GameObject.Find("CoreGuardian_MapArt_B");
        Require(added!=null&&added.GetComponentsInChildren<Collider2D>().Length==0&&added.GetComponentsInChildren<Collider>().Length==0,"Art collision found");
        Require(added.GetComponentsInChildren<MonoBehaviour>().Length==0,"Unexpected art gameplay behavior");
        var bg=added.GetComponentInChildren<SpriteRenderer>();
        // Scale/transform serialization can differ by subpixel float rounding at exact frustum edges.
        Require(bg.sortingOrder<0&&bg.bounds.min.x<=-1.61f+.001f&&bg.bounds.min.y<=-1+.001f&&bg.bounds.max.x>=28.61f-.001f&&bg.bounds.max.y>=16-.001f,"Background coverage/sort");
        var textures=AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Environment/AbandonedFortressB/StoneSlabs_B_2x2.png").OfType<Sprite>().ToArray();
        Require(textures.Length==4&&textures.All(s=>s.rect.width==32&&Mathf.Abs(s.bounds.size.x-.6f)<.0001f&&s.texture.filterMode==FilterMode.Point),"Effective 32px floor size");
        Require(!game.ground.GetComponent<TilemapRenderer>().enabled&&!game.coreMap.GetComponent<TilemapRenderer>().enabled,"Repeated floor art still visible"); Require(bg.sprite.name=="NaturalCourtyard_01_ClearBorder","Wrong selected plate"); var result=new{passed=true,naturalFloorIsSingleSprite=true,logicalFloorRenderersHidden=true,checkedCells,maps=results,board="45x25 / 0.6",originalRootTransformsUnchanged=true,
            cameraAndPipelineUnchanged=true,gameplayFeedbackAndMovementUnchanged=true,backgroundSprite=bg.sprite.name,
            backgroundBounds=bg.bounds.ToString(),addedColliders=0,addedRuntimeBehaviours=0,scene.isDirty};
        File.WriteAllText(Dir+filename,Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
