using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class FinishDecorationSetup {
 public static object Main(){
 var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
 if(EditorApplication.isPlaying||EditorApplication.isCompiling||s.isDirty)throw new Exception("Expected stopped clean scene");
 var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/AbandonedFortressB/DecorationHybrid_20261018/HybridDecoration_Unlit.mat");
 mat.SetColor("_Color",new Color(.78f,.82f,.85f,1));EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();
 const string accidental="Assets/docs/development/environment_decoration_applied_20261018";
 if(AssetDatabase.IsValidFolder(accidental)){
 var files=Directory.GetFiles(accidental,"*",SearchOption.AllDirectories);
 foreach(var f in files)if(!f.EndsWith("before_edit.png")&&!f.EndsWith("before_edit.png.meta"))throw new Exception("Unexpected file in temporary capture directory: "+f);
 AssetDatabase.DeleteAsset(accidental);
 }
 return new{decorationTint=mat.GetColor("_Color").ToString(),accidentalCaptureFolderRemoved=!AssetDatabase.IsValidFolder(accidental),sceneUnchanged=!s.isDirty};
 }
}
