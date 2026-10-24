using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CoreGuardian;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class CoreGuardianDamageNumberPoolPlayModeTests
{
    [UnityTest]
    public IEnumerator PreparedHitNumbersStayHidden_ReuseWithoutCreating_AndKeepOverflowAndLifetime()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianPrototype")).First(t=>t!=null);
        var game=(Component)UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();
        var session=(Session)type.GetProperty("Session").GetValue(game);
        var hud=game.GetComponents<Component>().Single(c=>c.GetType().Name=="CoreGuardianHud");
        var hudType=hud.GetType();
        var feedback=game.transform.Find("CombatHUD/WorldFeedback");
        var prepared=feedback.Cast<Transform>().Where(t=>t.name=="HitDamage").ToArray();
        float oldScale=Time.timeScale;
        try
        {
            Time.timeScale=0;
            Assert.That(prepared.Length,Is.EqualTo(3),"First simultaneous three hits must be prepared during Configure");
            foreach(var root in prepared)
            {
                Assert.That(root.gameObject.activeSelf,Is.False,"Preparation must never display feedback");
                var text=root.GetComponents<Component>().Single(c=>c.GetType().Name=="TextMeshProUGUI");
                var textType=text.GetType();
                Assert.That(((UnityEngine.Mesh)textType.GetProperty("mesh").GetValue(text)).vertexCount,Is.GreaterThan(0),"Prepare actual glyph geometry before the first hit");
                Assert.That(((UnityEngine.Object)textType.GetProperty("font").GetValue(text)).name,Does.Contain("Pixelify"));
                Assert.That(textType.GetProperty("fontSharedMaterial").GetValue(text),Is.SameAs(Resources.Load<Material>("CoreGuardian/Fonts/PixelifySansBoldDamageOutline")));
            }
            var enemy=session.DebugSpawn(EnemyKind.Attack,20,new EnemySpec{Kind=EnemyKind.Attack,Health=100000,Speed=1});
            Action<int> show=amount=>session.ApplyDamage(enemy,amount,"Player");
            for(int i=0;i<3;i++)show(15);
            Assert.That(feedback.childCount,Is.EqualTo(3),"First three hits reuse the prepared objects");
            Assert.That(prepared.All(t=>t.gameObject.activeSelf),Is.True);
            var firstText=prepared[0].GetComponents<Component>().Single(c=>c.GetType().Name=="TextMeshProUGUI");
            Assert.That((float)firstText.GetType().GetProperty("fontSize").GetValue(firstText),Is.EqualTo(32));
            Assert.That((Color)firstText.GetType().GetProperty("color").GetValue(firstText),Is.EqualTo(new Color(1f,.94f,.68f)));
            show(15);
            Assert.That(feedback.childCount,Is.EqualTo(4),"Prewarm is not a display cap; overflow still creates");
            var fallbackText=feedback.GetChild(3).GetComponents<Component>().Single(c=>c.GetType().Name=="TextMeshProUGUI");
            foreach(var text in new[]{firstText,fallbackText})text.GetType().GetMethod("ForceMeshUpdate").Invoke(text,new object[]{false,false});
            var preparedMesh=(Mesh)firstText.GetType().GetProperty("mesh").GetValue(firstText);
            var fallbackMesh=(Mesh)fallbackText.GetType().GetProperty("mesh").GetValue(fallbackText);
            // Buffers can retain different capacities; only the two visible glyphs are drawn.
            CollectionAssert.AreEqual(fallbackMesh.vertices.Take(8),preparedMesh.vertices.Take(8));
            CollectionAssert.AreEqual(fallbackMesh.uv.Take(8),preparedMesh.uv.Take(8));
            CollectionAssert.AreEqual(fallbackMesh.colors32.Take(8),preparedMesh.colors32.Take(8));
            var active=(IList)hudType.GetField("damageNumbers",flags).GetValue(hud);
            foreach(var number in active)number.GetType().GetField("Started").SetValue(number,Time.time-.84f);
            hudType.GetMethod("LateUpdate",flags).Invoke(hud,null);
            Assert.That(active.Count,Is.EqualTo(4),"Existing .85 second lifetime remains");
            foreach(var number in active)number.GetType().GetField("Started").SetValue(number,Time.time-.86f);
            hudType.GetMethod("LateUpdate",flags).Invoke(hud,null);
            Assert.That(active.Count,Is.Zero);
            Assert.That(feedback.Cast<Transform>().All(t=>!t.gameObject.activeSelf),Is.True);
            show(16);
            Assert.That(feedback.childCount,Is.EqualTo(4),"Returned overflow also remains reusable");
            hudType.GetMethod("ClearDamageNumbers").Invoke(hud,null);
            Assert.That(active.Count,Is.Zero);
        }
        finally{Time.timeScale=oldScale;}
#else
        yield return null;
#endif
    }
}
