using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ConfigureSlabMovement
{
    public static object Main()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying || scene.isDirty || scene.path!="Assets/Scenes/ArtReview_20260930_B_Revision2.unity")
            throw new Exception("Expected saved B art scene in Edit mode");
        var movement=UnityEngine.Object.FindObjectsByType<PrototypeGridMovement>().Single(m=>m.gameObject.scene==scene);
        var serialized=new SerializedObject(movement);
        if(serialized.FindProperty("movementTileSize").vector2IntValue!=Vector2Int.one)
            throw new Exception("Slab movement already configured");
        var ground=(Tilemap)serialized.FindProperty("ground").objectReferenceValue;
        var occupied=(Tilemap)serialized.FindProperty("occupiedTowerCells").objectReferenceValue;
        if(occupied==null)throw new Exception("Missing tower occupancy map");
        var visual=movement.transform.Find("PlayerVisual_Centered");
        if(visual==null)throw new Exception("Missing centered player graphics");
        Undo.RecordObjects(new UnityEngine.Object[]{movement,movement.transform,visual,occupied},"Use large slab movement");
        serialized.FindProperty("movementTileSize").vector2IntValue=new Vector2Int(2,2);
        serialized.FindProperty("movementTileOrigin").vector2IntValue=Vector2Int.zero;
        serialized.FindProperty("spriteOffset").vector3Value=Vector3.zero;
        serialized.ApplyModifiedProperties();
        movement.transform.position=(ground.GetCellCenterWorld(new Vector3Int(6,4,0))+ground.GetCellCenterWorld(new Vector3Int(7,5,0)))*.5f;
        visual.localPosition-=new Vector3(.5f,-.5f,0);
        var marker=occupied.GetTilesBlock(occupied.cellBounds).FirstOrDefault(t=>t!=null)??ground.GetTile(Vector3Int.zero);
        if(marker==null)throw new Exception("No occupancy marker tile");
        // Static review towers have no combat component, but still block player movement.
        foreach(var origin in new[]{new Vector3Int(8,8,0),new Vector3Int(12,8,0),new Vector3Int(16,8,0)})
            for(int x=0;x<2;x++)for(int y=0;y<2;y++)occupied.SetTile(origin+new Vector3Int(x,y,0),marker);
        var renderer=occupied.GetComponent<TilemapRenderer>();
        if(renderer!=null){Undo.RecordObject(renderer,"Hide logical occupancy map");renderer.enabled=false;}
        EditorUtility.SetDirty(occupied);
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");
        return "Configured 2x2 movement tiles; start center=(7,5); three tower footprints block movement";
    }

    public static object LiveCheck(string action)
    {
        var scene=SceneManager.GetActiveScene();
        if(!EditorApplication.isPlaying)throw new Exception("Expected Play mode");
        var m=UnityEngine.Object.FindObjectsByType<PrototypeGridMovement>().Single(p=>p.gameObject.scene==scene);
        var visual=m.transform.Find("PlayerVisual_Centered");
        if(action=="start") {
            if(m.CurrentCell!=new Vector3Int(6,4,0) || Vector3.Distance(m.transform.position,new Vector3(7,5,0))>.001f)
                throw new Exception("Wrong start center: "+m.transform.position);
            if(m.TryStep(Vector2Int.right))throw new Exception("Crossed path to the right");
            if(!m.TryStep(Vector2Int.up))throw new Exception("Clear upward move refused");
            return "PASS: starts centered, blocks path, upward move accepted";
        }
        if(action=="return") {
            if(m.CurrentCell!=new Vector3Int(6,6,0) || Vector3.Distance(m.transform.position,new Vector3(7,7,0))>.001f)
                throw new Exception("Wrong next tile center: "+m.transform.position);
            if(!m.TryStep(Vector2Int.down))throw new Exception("Return move refused");
            return "PASS: completed one full slab to (7,7), return accepted";
        }
        if(m.CurrentCell!=new Vector3Int(6,4,0) || Vector3.Distance(m.transform.position,new Vector3(7,5,0))>.001f)
            throw new Exception("Return did not finish at starting center");
        return "PASS: returned to (7,5); visual local position="+visual.localPosition;
    }
}
