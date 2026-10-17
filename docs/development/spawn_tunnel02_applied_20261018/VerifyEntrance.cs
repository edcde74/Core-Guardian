using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class VerifyEntrance
{
    static void Require(bool v,string m){if(!v)throw new Exception(m);}
    public static object Main()
    {
        var b=JObject.Parse(File.ReadAllText("docs/development/spawn_tunnel02_applied_20261018/before.json"));
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var g=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
        Require(!EditorApplication.isPlaying&&!EditorApplication.isCompiling&&!EditorUtility.scriptCompilationFailed&&!scene.isDirty,"Clean stopped scene required");
        Require(scene.path==(string)b["scene"]&&UnityEngine.SceneManagement.SceneManager.sceneCount==1,"Scene changed");
        Require(EditorJsonUtility.ToJson(Camera.main)==(string)b["camera"]&&EditorJsonUtility.ToJson(Camera.main.transform)==(string)b["cameraTransform"],"Camera changed");
        foreach(var old in b["gameplay"]){var c=g.GetComponents<Component>().Single(c=>c.GetType().FullName==(string)old["type"]);Require(EditorJsonUtility.ToJson(c)==(string)old["json"],"Gameplay/feedback changed "+c.GetType().Name);}
        foreach(var old in b["transforms"]){GlobalObjectId.TryParse((string)old["path"],out var id);var transform=(Transform)GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);Require(transform!=null&&EditorJsonUtility.ToJson(transform)==(string)old["json"],"Original object moved");}
        foreach(var old in b["tilemaps"]){var map=UnityEngine.Object.FindObjectsByType<UnityEngine.Tilemaps.Tilemap>().Single(t=>t.name==(string)old["name"]);Require(EditorJsonUtility.ToJson(map)==(string)old["json"],"Logical tilemap changed "+map.name);}
        var entrance=GameObject.Find("SpawnTunnel02_Selected");Require(entrance!=null,"Entrance missing");
        Require(entrance.GetComponentsInChildren<Collider>().Length==0&&entrance.GetComponentsInChildren<Collider2D>().Length==0&&entrance.GetComponentsInChildren<MonoBehaviour>().Length==0,"Entrance added gameplay behavior");
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Environment/AbandonedFortressB/SpawnTunnel02_Approved.png");
        Require(texture.filterMode==FilterMode.Point&&texture.width==1672&&texture.height==941,"Reference import changed");
        Require(File.ReadAllBytes("Assets/Art/Environment/AbandonedFortressB/SpawnTunnel02_Approved.png").SequenceEqual(File.ReadAllBytes("docs/development/spawn_entrance_options_20261018/02_approved_tunnel.png")),"Selected source was regenerated/edited");
        Require(Time.timeScale==1&&Time.captureDeltaTime==0,"Capture time not restored");
        var result=new{passed=true,sourceExactByteCopy=true,originalTransformsChecked=b["transforms"].Count(),tilemapsUnchanged=b["tilemaps"].Count(),gameplayAndEffectsUnchanged=true,cameraUnchanged=true,newColliders=0,newRuntimeBehaviours=0,scene.isDirty,Time.timeScale,Time.captureDeltaTime};
        File.WriteAllText("docs/development/spawn_tunnel02_applied_20261018/verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
