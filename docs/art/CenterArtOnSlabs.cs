using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

// Placement-only editor operation for the B art review. Large slabs cover 2x2 grid cells.
public static class CenterArtOnSlabs
{
    const string ScenePath = "Assets/Scenes/ArtReview_20260930_B_Revision2.unity";

    static Vector3 Footprint(Sprite sprite, int rows)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        ImageConversion.LoadImage(texture, File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
        var pixels = texture.GetPixels32();
        int minX=texture.width, maxX=-1, minY=texture.height, maxY=-1;
        for (int y=0; y<rows; y++) for (int x=0; x<texture.width; x++)
            if (pixels[y*texture.width+x].a >= 128) {
                minX=Math.Min(minX,x); maxX=Math.Max(maxX,x);
                minY=Math.Min(minY,y); maxY=Math.Max(maxY,y);
            }
        UnityEngine.Object.DestroyImmediate(texture);
        if (maxX < minX) throw new Exception("No footprint: " + sprite.name);
        return new Vector3(((minX+maxX+1)*0.5f-sprite.pivot.x)/sprite.pixelsPerUnit,
            ((minY+maxY+1)*0.5f-sprite.pivot.y)/sprite.pixelsPerUnit, 0);
    }

    public static object Main()
    {
        var scene=SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != ScenePath || scene.isDirty)
            throw new Exception("Expected saved B Revision2 scene in Edit mode.");
        var renderers=UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Where(r=>r.gameObject.scene==scene).ToArray();
        var ground=UnityEngine.Object.FindObjectsByType<Tilemap>().Single(m=>m.name=="Ground_Prototype");
        var targets = new Dictionary<string, Vector3Int> {
            {"tower_single", new Vector3Int(8,8,0)}, {"tower_multi", new Vector3Int(12,8,0)},
            {"tower_support", new Vector3Int(16,8,0)}, {"core", new Vector3Int(14,4,0)}
        };
        var rows = new Dictionary<string,int> {{"tower_single",18},{"tower_multi",18},{"tower_support",16},{"core",28}};
        var report=new List<object>();
        int undo=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Center B art on stone slabs");
        foreach(var pair in targets) {
            var r=renderers.Single(s=>s.name==pair.Key);
            var footprint=Footprint(r.sprite, rows[pair.Key]);
            var center=(ground.GetCellCenterWorld(pair.Value)+ground.GetCellCenterWorld(pair.Value+new Vector3Int(1,1,0)))*0.5f;
            var old=r.transform.position;
            Undo.RecordObject(r.transform,"Center base on slab");
            r.transform.position=center-r.transform.TransformVector(footprint);
            report.Add(new {name=r.name,before=old.ToString(),after=r.transform.position.ToString(),baseCenter=r.transform.TransformPoint(footprint).ToString(),target=center.ToString()});
        }
        var movement=UnityEngine.Object.FindObjectsByType<PrototypeGridMovement>().Single(m=>m.gameObject.scene==scene);
        var player=movement.GetComponent<SpriteRenderer>();
        if (player==null) throw new Exception("Expected original player renderer");
        var playerFootprint=Footprint(player.sprite,5);
        var visual=new GameObject("PlayerVisual_Centered");
        Undo.RegisterCreatedObjectUndo(visual,"Separate player graphics from movement anchor");
        visual.transform.SetParent(movement.transform,false);
        var graphic=Undo.AddComponent<SpriteRenderer>(visual);
        EditorUtility.CopySerialized(player,graphic);
        Undo.RecordObject(player,"Use centered child graphics"); player.enabled=false;
        Undo.RecordObject(movement.transform,"Center logical start cell");
        var logicalCell=new Vector3Int(6,5,0);
        movement.transform.position=ground.GetCellCenterWorld(logicalCell);
        var desired=(ground.GetCellCenterWorld(new Vector3Int(6,4,0))+ground.GetCellCenterWorld(new Vector3Int(7,5,0)))*0.5f;
        visual.transform.position=desired-visual.transform.TransformVector(playerFootprint);
        var serialized=new SerializedObject(movement);
        serialized.FindProperty("spriteOffset").vector3Value=Vector3.zero;
        serialized.ApplyModifiedProperties();
        report.Add(new {name="player",logicalCell=logicalCell.ToString(),position=movement.transform.position.ToString(),feet=visual.transform.TransformPoint(playerFootprint).ToString(),target=desired.ToString()});
        Undo.CollapseUndoOperations(undo);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Scene save failed");
        File.WriteAllText("docs/art/tile_center_alignment_20261008_report.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        return Newtonsoft.Json.JsonConvert.SerializeObject(report);
    }

    public static object Verify()
    {
        var scene=SceneManager.GetActiveScene();
        var ground=UnityEngine.Object.FindObjectsByType<Tilemap>().Single(m=>m.name=="Ground_Prototype");
        var movement=UnityEngine.Object.FindObjectsByType<PrototypeGridMovement>().Single(m=>m.gameObject.scene==scene);
        var graphic=movement.GetComponentsInChildren<SpriteRenderer>().Single(r=>r.enabled);
        var expected=new Vector3(7,5,0);
        var feet=graphic.transform.TransformPoint(Footprint(graphic.sprite,5));
        if ((feet-expected).sqrMagnitude>0.00001f) throw new Exception("Player feet lost alignment: "+feet);
        var settings=new SerializedObject(movement);
        var large=settings.FindProperty("movementTileSize").vector2IntValue==new Vector2Int(2,2);
        var expectedAnchor=large ? new Vector3(7,5,0) : ground.GetCellCenterWorld(new Vector3Int(6,5,0));
        if ((movement.transform.position-expectedAnchor).sqrMagnitude>0.00001f)
            throw new Exception("Player logical anchor changed");
        var offsets=new Dictionary<string,int>{{"tower_single",18},{"tower_multi",18},{"tower_support",16},{"core",28}};
        foreach(var r in UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Where(r=>offsets.ContainsKey(r.name))) {
            var p=r.transform.TransformPoint(Footprint(r.sprite,offsets[r.name]));
            if (Mathf.Abs(p.x-Mathf.Round(p.x))>0.0001f || Mathf.Abs(p.y-Mathf.Round(p.y))>0.0001f ||
                Mathf.RoundToInt(p.x)%2!=1 || Mathf.RoundToInt(p.y)%2!=1) throw new Exception("Base is not at a slab center: "+r.name);
        }
        return "PASS: four bases and player feet centered; logical player start preserved; play="+EditorApplication.isPlaying;
    }
}
