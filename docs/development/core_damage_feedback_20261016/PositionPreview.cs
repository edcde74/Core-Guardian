using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;

// Ephemeral editor tool, compiled in memory by run_script; never imported into Assets.
public static class CoreNumberPositionPreview
{
    public static object Main()
    {
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Requires running, compiled combat scene");
        const string Name="CoreDamageNumber_PositionPreview";
        var existing=GameObject.Find(Name);
        if(existing!=null){Selection.activeGameObject=existing;return new {existing=true,position=existing.transform.position.ToString()};}
        var game=UnityEngine.Object.FindFirstObjectByType<CoreGuardianPrototype>();
        if(game==null||!game.Ready)throw new InvalidOperationException("Combat scene not ready");
        var body=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).First(r=>r.name=="2D Sprite"&&r.sprite!=null&&r.sprite.name=="core");
        var hud=game.GetComponent<CoreGuardianHud>();
        var anchor=new GameObject(Name);anchor.hideFlags=HideFlags.DontSave;
        anchor.transform.position=new Vector3(body.bounds.max.x+.2f,body.bounds.max.y+.12f,body.transform.position.z);
        var previousScale=Time.timeScale;Time.timeScale=0;
        hud.ShowCoreNumber(anchor.transform.position,123);
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var list=(IList)typeof(CoreGuardianHud).GetField("damageNumbers",flags).GetValue(hud);
        var number=list[list.Count-1];var type=number.GetType();
        var root=(RectTransform)type.GetField("Root").GetValue(number);
        root.name="CoreDamageNumber_Preview_123";
        EditorApplication.CallbackFunction tick=null;
        tick=()=>
        {
            if(!EditorApplication.isPlaying||anchor==null||root==null)
            {
                EditorApplication.update-=tick;Time.timeScale=previousScale;
                if(root!=null)UnityEngine.Object.DestroyImmediate(root.gameObject);
                if(anchor!=null)UnityEngine.Object.DestroyImmediate(anchor);
                return;
            }
            type.GetField("World").SetValue(number,anchor.transform.position);
            type.GetField("Started").SetValue(number,Time.time);
            typeof(CoreGuardianHud).GetMethod("LateUpdate",flags).Invoke(hud,null);
            EditorApplication.QueuePlayerLoopUpdate();
        };
        EditorApplication.update+=tick;
        Selection.activeGameObject=anchor;EditorGUIUtility.PingObject(anchor);tick();
        return new {anchor=Name,position=anchor.transform.position.ToString("F4"),coreBounds=body.bounds.ToString(),sample="-123",timeScale=Time.timeScale,previousScale};
    }
}
