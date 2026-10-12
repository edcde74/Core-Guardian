using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Newtonsoft.Json;

public static class PathShadowContrastReview
{
    const string Source = "Assets/Scenes/ArtReview_20261008_Depth.unity";
    const string Folder = "Assets/Art/ScreenTests/20261008_PathContrast";
    const string Report = "docs/art/path_contrast_20261008_report.json";
    static string Hash(string p) { using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))); }
    static string Fingerprint()
    {
        var s=SceneManager.GetActiveScene();
        var sprites=UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Where(r=>r.gameObject.scene==s).OrderBy(r=>r.transform.GetHierarchyPath()).ThenBy(r=>r.transform.position.x).ThenBy(r=>r.transform.position.y).ThenBy(r=>r.transform.position.z).Select(r=>new {path=r.transform.GetHierarchyPath(),pos=V(r.transform.position),scale=V(r.transform.lossyScale),sprite=AssetDatabase.GetAssetPath(r.sprite),color=new[]{r.color.r,r.color.g,r.color.b,r.color.a},order=r.sortingOrder,material=AssetDatabase.GetAssetPath(r.sharedMaterial)}).ToArray();
        var maps=UnityEngine.Object.FindObjectsByType<Tilemap>().Where(m=>m.gameObject.scene==s).OrderBy(m=>m.name).Select(m=>new {m.name,cell=V(m.layoutGrid.cellSize),tiles=Cells(m.cellBounds).Where(p=>m.HasTile(p)).Select(p=>new {pos=new[]{p.x,p.y,p.z},tile=AssetDatabase.GetAssetPath(m.GetTile(p)),matrix=m.GetTransformMatrix(p).ToString("R")}).ToArray()}).ToArray();
        var camera=Camera.main;
        var movement=UnityEngine.Object.FindObjectsByType<PrototypeGridMovement>().Single();
        return JsonConvert.SerializeObject(new {sprites,maps,cameraPos=V(camera.transform.position),camera.orthographicSize,move=MovementState(movement)});
    }
    public static object Main()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.path!=Source||scene.isDirty) throw new Exception("Expected clean saved Depth scene in Edit mode");
        string[] keys={"A","B"};
        if(File.Exists(Report)) throw new Exception("Completed comparison exists; do not regenerate");
        // Reuse only this run's incomplete, dedicated comparison outputs.
        string sourceHash=Hash(Source), baseline=Fingerprint();
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var notes=new System.Collections.Generic.List<object>();
        foreach(var k in keys)
        {
            scene=EditorSceneManager.OpenScene(Source,OpenSceneMode.Single);
            if(!File.Exists(ScenePath(k)) && !EditorSceneManager.SaveScene(scene,ScenePath(k),true)) throw new Exception("Copy failed");
            scene=EditorSceneManager.OpenScene(ScenePath(k),OpenSceneMode.Single);
            var paths=UnityEngine.Object.FindObjectsByType<Tilemap>().Where(m=>m.name=="PathArt_WideStraight"||m.name=="PathArt_ConnectedCorners").ToArray();
            if(paths.Length!=2) throw new Exception("Expected two visible path maps");
            var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/PathContrast_"+k+".mat"); bool createMaterial=material==null; if(createMaterial) material=new Material(paths[0].GetComponent<TilemapRenderer>().sharedMaterial);
            material.name="PathContrast_"+k;
            // A preserves the original stone detail. B composites a paler, lower-contrast
            // path over the existing gray floor; source textures and all art stay intact.
            var tint=k=="A"?new Color(2.15f,2.18f,2.22f,1f):new Color(1.7f,1.75f,1.8f,.58f);
            material.SetColor("_Color",tint);
            if(createMaterial) AssetDatabase.CreateAsset(material,Folder+"/PathContrast_"+k+".mat"); else EditorUtility.SetDirty(material);
            foreach(var map in paths) map.GetComponent<TilemapRenderer>().sharedMaterial=material;
            if(Fingerprint()!=baseline) throw new Exception("Approved artwork/camera/layout/movement changed");
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Save failed");
            scene=EditorSceneManager.OpenScene(ScenePath(k),OpenSceneMode.Single);
            if(Fingerprint()!=baseline) throw new Exception("Reopened scene changed approved contents");
            var verify="Approved art/shadow fingerprint preserved";
            notes.Add(new {key=k,scene=ScenePath(k),tint=tint.ToString(),approvedArtworkLayoutCameraMovementUnchanged=true,shadowVerification=verify});
        }
        AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(Source,OpenSceneMode.Single);
        if(Hash(Source)!=sourceHash) throw new Exception("Original source scene changed");
        File.WriteAllText(Report,JsonConvert.SerializeObject(new {source=Source,sourceHash,originalUnchanged=true,variants=notes},Formatting.Indented));
        return JsonConvert.SerializeObject(new {success=true,originalUnchanged=true,variants=notes});
    }
    static System.Collections.Generic.IEnumerable<Vector3Int> Cells(BoundsInt b) { foreach(var p in b.allPositionsWithin) yield return p; }
    static string MovementState(PrototypeGridMovement m) { var s=new SerializedObject(m); return JsonConvert.SerializeObject(new { position=V(m.transform.position), size=s.FindProperty("movementTileSize").vector2IntValue.ToString(), origin=s.FindProperty("movementTileOrigin").vector2IntValue.ToString(), offset=V(s.FindProperty("spriteOffset").vector3Value), duration=s.FindProperty("stepDuration").floatValue, refs=new[]{"ground","enemyPath","core","occupiedTowerCells"}.Select(p=>s.FindProperty(p).objectReferenceValue.name).ToArray() }); }
    static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
    public static object Diagnose() { string current=Fingerprint(); EditorSceneManager.OpenScene(Source,OpenSceneMode.Single); string original=Fingerprint(); File.WriteAllText("docs/art/path_contrast_debug.json", "{\"current\":"+current+",\"original\":"+original+"}"); return "Wrote diagnostic fingerprints and reopened original"; }
    static string ScenePath(string k)=>"Assets/Scenes/ArtReview_20261008_PathContrast_"+k+".unity";
    static string GetHierarchyPath(this Transform t) => t.parent==null?t.name:GetHierarchyPath(t.parent)+"/"+t.name;
}




