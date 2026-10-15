using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class CoreGuardianAnimationPlayModeTests
{
    [UnityTest]
    public IEnumerator ActualLaunchThenViewUpdate_PreservesHighRateFiringPoseAndMuzzleAtThreeTimesSpeed()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianPrototype")).First(t=>t!=null);
        var game=(Component)UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();
        var session=(CoreGuardian.Session)type.GetProperty("Session").GetValue(game);
        var movement=(Component)type.GetField("player").GetValue(game);
        var art=movement.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="2D Sprite");
        var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
        var position=(Vector2)type.GetMethod("PlayerTile",flags).Invoke(game,null);
        float previousScale=Time.timeScale;
        try
        {
            // Controlled presentation regression only: deliberately extreme rate, not balance evidence.
            Time.timeScale=3;session.Player.Rate=30;
            session.StartCombat();session.DebugSpawn(CoreGuardian.EnemyKind.Defense,77);
            int launches=0;session.ProjectileLaunched+=shot=>{if(shot.Source=="Player")launches++;};
            session.Tick(.15f,position);
            Assert.That(launches,Is.GreaterThan(0));
            type.GetMethod("SynchronizeViews",flags).Invoke(game,null);
            Assert.That(art.sprite.name,Does.EndWith("_1"),"Tick→launch→presentation must not skip the launch pose");
            Assert.That(UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Any(r=>r.name.StartsWith("Muzzle_")&&r.color.a>0),Is.True,"A launch flash must survive through its first rendered frame");
        }
        finally{Time.timeScale=previousScale;}
#else
        yield return null;
#endif
    }
    [UnityTest]
    public IEnumerator ActualScene_LoadsDirectionalArt_KeepsFeetAnchored_AndShootsInEightDirections()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianPrototype")).First(t=>t!=null);
        var game=(Component)UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();
        Assert.That(game.GetComponent(type).GetType().GetProperty("Ready").GetValue(game),Is.EqualTo(true));
        var animationType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianUnitAnimation")).First(t=>t!=null);
        var movement=(Component)type.GetField("player").GetValue(game);
        var animation=movement.GetComponent(animationType);
        Assert.That(animation,Is.Not.Null);
        Assert.That(animationType.GetProperty("Loaded").GetValue(animation),Is.EqualTo(true));
        var art=movement.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="2D Sprite");
        for(int d=0;d<8;d++)
        {
            var axis=new Vector2(Mathf.Cos(d*Mathf.PI/4),Mathf.Sin(d*Mathf.PI/4));
            animationType.GetMethod("Fire").Invoke(animation,new object[]{axis,1f});
            Assert.That(art.sprite.name,Is.EqualTo("Player_"+d+"_1"));
            animationType.GetMethod("Present").Invoke(animation,new object[]{.15f});
            Assert.That(art.sprite.name,Is.EqualTo("Player_"+d+"_1"),"A shot must render once even at high speed or a long frame");
            Assert.That(art.sprite.pivot,Is.EqualTo(new Vector2(48,8)));
            Assert.That(art.transform.localPosition,Is.EqualTo(Vector3.zero));
            Assert.That(movement.transform.rotation,Is.EqualTo(Quaternion.identity));
        }
        animationType.GetMethod("MoveWithoutTurning").Invoke(animation,new object[]{Vector2.right,.15f});
        animationType.GetMethod("Aim").Invoke(animation,new object[]{Vector2.left,.15f});
        Assert.That(animationType.GetProperty("Direction").GetValue(animation),Is.EqualTo(4),"Movement must not cancel aiming toward the current target");
        var enemyPrefabs=((GameObject[])type.GetField("enemyArt").GetValue(game)).Concat(new[]{Resources.Load<GameObject>("CoreGuardian/Animation/Boss/BossAnimated")}).ToArray();
        string[] roles={"Attack","Defense","Buff","Boss"};
        for(int i=0;i<4;i++)
        {
            var clone=UnityEngine.Object.Instantiate(enemyPrefabs[i]);
            try
            {
                var unit=animationType.GetMethod("Attach").Invoke(null,new object[]{clone,roles[i]});
                var body=clone.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="2D Sprite");
                var directions=new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down};
                for(int d=0;d<4;d++)
                {
                    animationType.GetMethod("Move").Invoke(unit,new object[]{directions[d],.3f});
                    animationType.GetMethod("Present").Invoke(unit,new object[]{0f});
                    Assert.That(body.sprite.name,Does.StartWith(roles[i]+"_"+d+"_"));
                    Assert.That(body.sprite.pivot,Is.EqualTo(new Vector2(48,8)));
                    Assert.That(body.transform.localPosition,Is.EqualTo(Vector3.zero));
                }
            }
            finally{UnityEngine.Object.Destroy(clone);}
        }
#else
        yield return null;
#endif
    }
}
