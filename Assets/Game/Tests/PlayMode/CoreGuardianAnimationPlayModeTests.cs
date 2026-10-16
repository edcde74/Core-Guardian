using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class CoreGuardianAnimationPlayModeTests
{
    static Type Find(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    [UnityTest]
    public IEnumerator NormalEnemyRoute_AnimatesThroughCombatAdapter_OnEveryWorldAxis()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var type=Find("CoreGuardianPrototype");var game=(MonoBehaviour)UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();
        var session=(CoreGuardian.Session)type.GetProperty("Session").GetValue(game);
        var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
        var views=(System.Collections.IDictionary)type.GetField("enemyViews",flags).GetValue(game);
        var samples=new int[4];var poses=new System.Collections.Generic.HashSet<Quaternion>[4];for(int d=0;d<4;d++)poses[d]=new System.Collections.Generic.HashSet<Quaternion>();
        game.enabled=false;session.StartCombat();int id=-1;Vector3 previous=Vector3.zero;
        for(int step=0;step<10000;step++)
        {
            // Normal wave/movement only. This isolated accelerated test is functional QA.
            session.Tick(.05f,new Vector2(-100,-100));type.GetMethod("SynchronizeViews",flags).Invoke(game,null);
            if(id<0&&session.Enemies.Count>0)id=session.Enemies[0].Id;
            if(id>=0&&views.Contains(id))
            {
                var view=(GameObject)views[id];var position=view.transform.position;var delta=position-previous;
                if(step>1&&delta.sqrMagnitude>.000001f)
                {
                    int d=Mathf.Abs(delta.x)>Mathf.Abs(delta.y)?(delta.x>0?0:1):(delta.y>0?2:3);
                    var art=view.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");var rig=art.transform.Find("OriginalWalk");
                    Assert.That(rig.gameObject.activeSelf,Is.True,"Actual route axis "+d);samples[d]++;poses[d].Add(rig.Find("LeftLower").localRotation);
                }
                previous=position;
            }
            if(samples.All(n=>n>=12)&&poses.All(p=>p.Count>=8))break;
            if(step%50==0)yield return null;
        }
        for(int d=0;d<4;d++){Assert.That(samples[d],Is.GreaterThanOrEqualTo(12),"Route axis "+d);Assert.That(poses[d].Count,Is.GreaterThanOrEqualTo(8),"Visible leg poses axis "+d);}
#else
        yield return null;
#endif
    }
    [UnityTest]
    public IEnumerator ActualLaunch_PreservesOriginalPlayerSprite_AndDoesNotUseRejectedMuzzle()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var type=Find("CoreGuardianPrototype");var game=(Component)UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();
        var session=(CoreGuardian.Session)type.GetProperty("Session").GetValue(game);
        var movement=(Component)type.GetField("player").GetValue(game);
        var art=movement.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="2D Sprite");
        var expected=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ScreenTests/20260930/Sprites/player.png");
        Assert.That(art.sprite,Is.SameAs(expected));
        var initialPosition=art.transform.localPosition;var initialScale=art.transform.localScale;
        var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
        var position=(Vector2)type.GetMethod("PlayerTile",flags).Invoke(game,null);
        float oldScale=Time.timeScale;
        try
        {
            Time.timeScale=3;session.Player.Rate=30;session.StartCombat();session.DebugSpawn(CoreGuardian.EnemyKind.Defense,77);
            int launches=0;session.ProjectileLaunched+=shot=>{if(shot.Source=="Player")launches++;};
            session.Tick(.15f,position);type.GetMethod("SynchronizeViews",flags).Invoke(game,null);
            Assert.That(launches,Is.GreaterThan(0));
            Assert.That(art.sprite,Is.SameAs(expected),"Attacks must not swap the rejected atlas back in");
            Assert.That(art.transform.localPosition,Is.EqualTo(initialPosition));Assert.That(art.transform.localScale,Is.EqualTo(initialScale));
            Assert.That(UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Any(r=>r.name.StartsWith("Muzzle_")),Is.False);
            var rig=art.transform.Find("OriginalMotion");
            Assert.That(rig,Is.Not.Null);Assert.That(rig.gameObject.activeSelf,Is.True,"A real launch must start the original cutout animation");
            var unit=movement.GetComponent(Find("CoreGuardianUnitAnimation"));
            Assert.That((int)unit.GetType().GetProperty("ShotCount").GetValue(unit),Is.GreaterThan(0));
            unit.GetType().GetMethod("Present").Invoke(unit,new object[]{1f});
            Assert.That(rig.gameObject.activeSelf,Is.False,"Recovery must end the attack pose");
            Assert.That(art.transform.Find("OriginalIdle").gameObject.activeSelf,Is.True,"Stationary player returns to original-pixel Idle");
        }
        finally{Time.timeScale=oldScale;}
#else
        yield return null;
#endif
    }
    [UnityTest]
    public IEnumerator OriginalTowerSpritesAndTransformsRemainExact_WhileEnemyDirectionsStillWork()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var type=Find("CoreGuardianPrototype");var game=(Component)UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();
        var animationType=Find("CoreGuardianUnitAnimation");
        var towers=(GameObject[])type.GetField("towerArt").GetValue(game);
        for(int i=0;i<towers.Length;i++)
        {
            var clone=UnityEngine.Object.Instantiate(towers[i]);
            try
            {
                var art=clone.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");
                var original=art.sprite;var position=art.transform.localPosition;var scale=art.transform.localScale;var rotation=art.transform.localRotation;
                var shadows=clone.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r!=art).ToDictionary(r=>r,r=>r.enabled);
                var unit=animationType.GetMethod("Attach").Invoke(null,new object[]{clone,((CoreGuardian.TowerKind)i).ToString()});
                animationType.GetMethod("Fire").Invoke(unit,new object[]{Vector2.left,8f});animationType.GetMethod("Present").Invoke(unit,new object[]{.15f});
                Assert.That(art.sprite,Is.SameAs(original));Assert.That(art.transform.localPosition,Is.EqualTo(position));Assert.That(art.transform.localScale,Is.EqualTo(scale));Assert.That(art.transform.localRotation,Is.EqualTo(rotation));
                Assert.That(shadows.All(s=>s.Key.enabled==s.Value),Is.True);Assert.That(art.transform.Find("AnimatedUpper"),Is.Null);
            }
            finally{UnityEngine.Object.Destroy(clone);}
        }
        var enemyPrefabs=((GameObject[])type.GetField("enemyArt").GetValue(game)).Concat(new[]{Resources.Load<GameObject>("CoreGuardian/Animation/Boss/BossAnimated")}).ToArray();
        string[] roles={"Attack","Defense","Buff","Boss"};
        for(int i=0;i<4;i++)
        {
            var clone=UnityEngine.Object.Instantiate(enemyPrefabs[i]);
            try
            {
                var unit=animationType.GetMethod("Attach").Invoke(null,new object[]{clone,roles[i]});
                var art=clone.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="2D Sprite");
                var directions=new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down};
                var source=art.sprite;var rootPosition=clone.transform.position;var rootScale=clone.transform.localScale;
                for(int d=0;d<4;d++)
                {
                    animationType.GetMethod("Move").Invoke(unit,new object[]{directions[d],.3f});animationType.GetMethod("Present").Invoke(unit,new object[]{.3f});
                    Assert.That(art.sprite,Is.SameAs(source),"Movement must keep approved source, never generated frame art");
                    // The visual pivot compensates mirroring around the boot center.
                    // The logical unit root and original sprite/scale remain unchanged.
                    Assert.That(clone.transform.position,Is.EqualTo(rootPosition));
                    Assert.That(clone.transform.localScale,Is.EqualTo(rootScale));
                    Assert.That((int)animationType.GetProperty("Direction").GetValue(unit),Is.EqualTo(d));
                    Assert.That(art.transform.Find("OriginalWalk").gameObject.activeSelf,Is.True);
                }
                animationType.GetMethod("Move").Invoke(unit,new object[]{Vector2.zero,.3f});animationType.GetMethod("Present").Invoke(unit,new object[]{.3f});
                Assert.That(art.enabled,Is.True);Assert.That(art.transform.Find("OriginalWalk").gameObject.activeSelf,Is.False);
            }
            finally{UnityEngine.Object.Destroy(clone);}
        }
#else
        yield return null;
#endif
    }
}
