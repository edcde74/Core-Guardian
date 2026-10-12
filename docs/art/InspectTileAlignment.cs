using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;

public static class InspectTileAlignment
{
    public static object Main()
    {
        var scene = SceneManager.GetActiveScene();
        return Newtonsoft.Json.JsonConvert.SerializeObject(new {
            scene = scene.path, dirty = scene.isDirty, playing = EditorApplication.isPlaying,
            pipeline = GraphicsSettings.currentRenderPipeline == null ? "Built-in" : GraphicsSettings.currentRenderPipeline.GetType().FullName,
            maps = UnityEngine.Object.FindObjectsByType<Tilemap>().Select(m => new {m.name, position=m.transform.position.ToString(), cellSize=m.layoutGrid.cellSize.ToString(), anchor=m.tileAnchor.ToString(), center=m.GetCellCenterWorld(Vector3Int.zero).ToString()}).ToArray(),
            sprites = UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Where(r=>r.gameObject.scene == scene && r.sprite != null).Select(r=>new { r.name, position=r.transform.position.ToString(), scale=r.transform.localScale.ToString(), sprite=r.sprite.name, pivot=r.sprite.pivot.ToString(), rect=r.sprite.rect.ToString(), bounds=r.bounds.ToString(), components=r.GetComponents<Component>().Select(c=>c.GetType().Name).ToArray(), offset=r.GetComponent<PrototypeGridMovement>() == null ? "" : new SerializedObject(r.GetComponent<PrototypeGridMovement>()).FindProperty("spriteOffset").vector3Value.ToString() }).ToArray()
        });
    }
}
