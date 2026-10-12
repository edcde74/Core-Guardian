using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class RepairArtRevision2 {
 public static object Main(){
 if(EditorApplication.isPlaying) throw new Exception("Stop play first");
 string root="Assets/Art/ScreenTests/20260930/Revision2/Tiles/";
 AssetDatabase.SaveAssets();
 var maps=UnityEngine.Object.FindObjectsByType<Tilemap>();
 var ground=maps.First(t=>t.name=="Ground_Prototype");
 for(int y=0;y<15;y++)for(int x=0;x<26;x++) ground.SetTile(new Vector3Int(x,y,0),AssetDatabase.LoadAssetAtPath<Tile>(root+"slab_"+(x%2)+"_"+(y%2)+".asset"));
 var route=maps.First(t=>t.name=="EnemyPath_Prototype");
 var straight=maps.First(t=>t.name=="PathArt_WideStraight");
 var corner=maps.First(t=>t.name=="PathArt_ConnectedCorners");
 straight.ClearAllTiles();corner.ClearAllTiles();
 var st=AssetDatabase.LoadAssetAtPath<Tile>(root+"PathStraight_B.asset");
 var ct=AssetDatabase.LoadAssetAtPath<Tile>(root+"PathCorner_NorthEast_B.asset");
 if(st==null||ct==null||st.sprite==null||ct.sprite==null)throw new Exception("Missing saved tile assets");
 int turns=0,lines=0;
 foreach(var p in route.cellBounds.allPositionsWithin){
 if(!route.HasTile(p))continue;
 bool n=route.HasTile(p+Vector3Int.up),e=route.HasTile(p+Vector3Int.right),s=route.HasTile(p+Vector3Int.down),w=route.HasTile(p+Vector3Int.left);
 bool turn=((n?1:0)+(e?1:0)+(s?1:0)+(w?1:0))==2&&!(n&&s)&&!(e&&w);
 var map=turn?corner:straight;
 int angle=turn?(n&&e?0:n&&w?90:s&&w?180:270):(n||s?90:0);
 map.SetTile(p,turn?ct:st);map.SetTileFlags(p,TileFlags.None);map.SetTransformMatrix(p,Matrix4x4.Rotate(Quaternion.Euler(0,0,angle)));
 if(turn)turns++;else lines++;
 }
 ground.RefreshAllTiles();straight.RefreshAllTiles();corner.RefreshAllTiles();
 // Flush Tilemap serialization before saving, then verify by reopening.
 EditorUtility.SetDirty(ground);EditorUtility.SetDirty(straight);EditorUtility.SetDirty(corner);
 var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 string path=scene.path;EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
 return new{path,turns,lines,maps=UnityEngine.Object.FindObjectsByType<Tilemap>().Select(t=>new{t.name,used=t.GetUsedTilesCount(),origin=t.HasTile(Vector3Int.zero)}).ToArray()};
 }
}
