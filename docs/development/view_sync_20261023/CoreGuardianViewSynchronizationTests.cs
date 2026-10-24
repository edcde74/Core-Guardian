using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoreGuardian;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEditor.SceneManagement;

// Catches stale-world motion, stale pooled history, and reusing coordinates across activation callbacks.
public class CoreGuardianViewSynchronizationTests
{
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
    static Type Find(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static object Get(object o,string name)=>o.GetType().GetField(name,Fields).GetValue(o);
    static void Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name).Invoke(o,args);
    static Vector3 World(Component game,Vector2 p)=>(Vector3)game.GetType().GetMethod("World").Invoke(game,new object[]{p});
    static void Sync(Component game)=>game.GetType().GetMethod("SynchronizeViews",Fields).Invoke(game,null);
    static void Load()=>EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));

    [UnityTest]
    public IEnumerator CachedWorldMatchesLegacyMotion_AcrossGridChangesZeroDtAndReleasedBodies()
    {
        Load();yield return null;
        var game=(MonoBehaviour)UnityEngine.Object.FindAnyObjectByType(Find("CoreGuardianPrototype"));game.enabled=false;
        var session=(Session)game.GetType().GetProperty("Session").GetValue(game);
        var animation=(Component)Get(game,"combatAnimation");
        var method=animation.GetType().GetMethod("Enemy",new[]{typeof(EnemyHandle),typeof(GameObject),typeof(Vector3)});
        Assert.That(method,Is.Not.Null,"Cached world entry must drive the actual motion consumer");
        var ground=(UnityEngine.Tilemaps.Tilemap)game.GetType().GetField("ground").GetValue(game);
        var views=(IDictionary)Get(game,"enemyViews");
        var originalScale=Time.timeScale;var grid=ground.transform.parent;var oldPosition=grid.position;var oldRotation=grid.rotation;var oldScale=grid.localScale;
        var clones=new List<GameObject>();
        try
        {
            foreach(var kind in new[]{EnemyKind.Attack,EnemyKind.Defense,EnemyKind.Buff,EnemyKind.Boss})
            {
                var enemy=session.DebugSpawn(kind,1);Sync(game);var view=(GameObject)views[enemy.Id];
                var prefab=kind==EnemyKind.Boss?(GameObject)Get(game,"bossArt"):((GameObject[])game.GetType().GetField("enemyArt").GetValue(game))[(int)kind];
                var reference=UnityEngine.Object.Instantiate(prefab);clones.Add(reference);
                // Independently configured source body consumes the legacy velocity formula.
                var referenceUnit=(Component)Find("CoreGuardianUnitAnimation").GetMethod("Attach").Invoke(null,new object[]{reference,kind.ToString()});
                Call(referenceUnit,"ResetEnemyForReuse");
                Call(view.GetComponent(Find("CoreGuardianUnitAnimation")),"ResetEnemyForReuse");
                Call(animation,"ReleaseEnemy",enemy.Id);
                Vector2 previous=enemy.Position;
                method.Invoke(animation,new object[]{enemy,view,World(game,enemy.Position)});
                Call(referenceUnit,"Move",Vector2.zero,Time.deltaTime);Call(referenceUnit,"Present",Time.deltaTime);
                for(int scenario=0;scenario<3;scenario++)
                {
                    grid.position=oldPosition+new Vector3(3*scenario,-2*scenario,0);
                    grid.rotation=oldRotation*Quaternion.Euler(0,0,scenario*37);
                    grid.localScale=Vector3.Scale(oldScale,new Vector3(1+scenario*.3f,1+scenario*.7f,1));
                    foreach(var direction in new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down})
                    {
                        enemy.Position+=direction*.2f;var current=World(game,enemy.Position);view.transform.position=reference.transform.position=current;
                        float dt=Time.deltaTime;
                        var velocity=dt>0?(Vector2)(current-World(game,previous))/dt:Vector2.zero;
                        Call(referenceUnit,"Move",velocity,dt);Call(referenceUnit,"Present",dt);
                        method.Invoke(animation,new object[]{enemy,view,current});
                        AssertPose(reference,view);
                        previous=enemy.Position;
                    }
                }
                Time.timeScale=0;
                // Test runner coroutine updates may precede the next player-loop time update.
                for(int wait=0;wait<4;wait++){yield return null;if(Time.deltaTime==0)break;}
                Assert.That(Time.deltaTime,Is.Zero);
                enemy.Position+=Vector2.right;
                Call(referenceUnit,"Move",Vector2.zero,0f);Call(referenceUnit,"Present",0f);
                method.Invoke(animation,new object[]{enemy,view,World(game,enemy.Position)});AssertPose(reference,view);
                Time.timeScale=originalScale;
                for(int wait=0;wait<4;wait++){yield return null;if(Time.deltaTime>0)break;}
                // Release and re-use the same ID with a distant model position; first pose must be stationary.
                Call(animation,"ReleaseEnemy",enemy.Id);Call(view.GetComponent(Find("CoreGuardianUnitAnimation")),"ResetEnemyForReuse");Call(referenceUnit,"ResetEnemyForReuse");
                enemy.Position+=new Vector2(8,-4);
                Call(referenceUnit,"Move",Vector2.zero,Time.deltaTime);Call(referenceUnit,"Present",Time.deltaTime);
                method.Invoke(animation,new object[]{enemy,view,World(game,enemy.Position)});AssertPose(reference,view);
                session.DebugRemoveEnemy(enemy);Sync(game);
            }
        }
        finally
        {
            Time.timeScale=originalScale;grid.position=oldPosition;grid.rotation=oldRotation;grid.localScale=oldScale;
            foreach(var clone in clones)UnityEngine.Object.DestroyImmediate(clone);
        }
    }

    static void AssertPose(GameObject expected,GameObject actual)
    {
        // The production view also owns its runtime health bar; compare the artwork rigs here.
        var left=expected.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite").GetComponentsInChildren<SpriteRenderer>(true);
        var right=actual.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite").GetComponentsInChildren<SpriteRenderer>(true);
        Assert.That(right.Length,Is.EqualTo(left.Length));
        for(int i=0;i<left.Length;i++)
        {
            Assert.That(right[i].sprite,Is.SameAs(left[i].sprite),left[i].name);
            Assert.That(Vector3.Distance(right[i].transform.localPosition,left[i].transform.localPosition),Is.LessThan(.00001f),left[i].name);
            Assert.That(Quaternion.Angle(right[i].transform.localRotation,left[i].transform.localRotation),Is.LessThan(.001f),left[i].name);
            Assert.That(right[i].color,Is.EqualTo(left[i].color));Assert.That(right[i].enabled,Is.EqualTo(left[i].enabled));
        }
    }

    [UnityTest]
    public IEnumerator ActualPoolReusesBodyWithoutHistory_ForNewGenerationWithSameId()
    {
        Load();yield return null;
        var game=(MonoBehaviour)UnityEngine.Object.FindAnyObjectByType(Find("CoreGuardianPrototype"));game.enabled=false;
        var session=(Session)game.GetType().GetProperty("Session").GetValue(game);
        var old=session.DebugSpawn(EnemyKind.Attack,1);Sync(game);
        var views=(IDictionary)Get(game,"enemyViews");var body=(GameObject)views[old.Id];
        old.Position+=Vector2.up;Sync(game);
        session.DebugRemoveEnemy(old);Sync(game);
        Assert.That(old.IsValid,Is.False);Assert.That(body.activeSelf,Is.False);Assert.That(views.Contains(old.Id),Is.False);
        typeof(Session).GetField("nextEnemy",Fields).SetValue(session,old.Id);
        var next=session.DebugSpawn(EnemyKind.Attack,8);Assert.That(next.Id,Is.EqualTo(old.Id));Assert.That(next,Is.Not.EqualTo(old));
        Sync(game);Assert.That(views[next.Id],Is.SameAs(body));Assert.That(body.activeSelf,Is.True);
        var unit=body.GetComponent(Find("CoreGuardianUnitAnimation"));
        Assert.That((bool)Get(unit,"walking"),Is.False,"A new generation's first pose must not consume retired history");
        Assert.That(Vector3.Distance(body.transform.position,World(game,next.Position)),Is.LessThan(.00001f));
        var animation=(Component)Get(game,"combatAnimation");
        Assert.That(((Dictionary<int,Vector2>)Get(animation,"previousEnemies"))[next.Id],Is.EqualTo(next.Position));
    }

    [UnityTest]
    public IEnumerator EnemyPresentCallbackRecomputesAnAlreadyActiveAuraPosition()
    {
        Load();yield return null;
        var game=(MonoBehaviour)UnityEngine.Object.FindAnyObjectByType(Find("CoreGuardianPrototype"));game.enabled=false;
        var session=(Session)game.GetType().GetProperty("Session").GetValue(game);
        var enemy=session.DebugSpawn(EnemyKind.Attack,1);enemy.Buffed=true;Sync(game);
        var view=(GameObject)((IDictionary)Get(game,"enemyViews"))[enemy.Id];
        var vertical=view.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="VerticalWalk").gameObject;
        Assert.That(vertical.activeSelf,Is.False);
        var callback=vertical.AddComponent<CoreGuardianViewSyncActivationProbe>();
        callback.target=((UnityEngine.Tilemaps.Tilemap)game.GetType().GetField("ground").GetValue(game)).transform.parent;
        enemy.Position+=Vector2.up;Sync(game);
        var aura=(GameObject)((IDictionary)Get(game,"auraViews"))[enemy.Id];
        Assert.That(callback.calls,Is.EqualTo(1));Assert.That(aura.activeSelf,Is.True);
        Assert.That(Vector3.Distance(aura.transform.position,World(game,enemy.Position)+Vector3.up*1.02f),Is.LessThan(.00001f));
    }

    [UnityTest]
    public IEnumerator AuraCreationRecomputesPositionAfterHierarchyCallback()
    {
        Load();yield return null;
        var game=(MonoBehaviour)UnityEngine.Object.FindAnyObjectByType(Find("CoreGuardianPrototype"));game.enabled=false;
        var session=(Session)game.GetType().GetProperty("Session").GetValue(game);
        var enemy=session.DebugSpawn(EnemyKind.Attack,1);Sync(game);
        var callback=((Component)Get(game,"combatArt")).gameObject.AddComponent<CoreGuardianViewSyncHierarchyProbe>();
        callback.target=((UnityEngine.Tilemaps.Tilemap)game.GetType().GetField("ground").GetValue(game)).transform.parent;
        enemy.Buffed=true;Sync(game);
        var aura=(GameObject)((IDictionary)Get(game,"auraViews"))[enemy.Id];
        Assert.That(callback.calls,Is.EqualTo(1));
        Assert.That(Vector3.Distance(aura.transform.position,World(game,enemy.Position)+Vector3.up*1.02f),Is.LessThan(.00001f));
    }

    [UnityTest]
    public IEnumerator AuraActivationRecomputesPositionAfterExternalGridMutation()
    {
        Load();yield return null;
        var game=(MonoBehaviour)UnityEngine.Object.FindAnyObjectByType(Find("CoreGuardianPrototype"));game.enabled=false;
        var session=(Session)game.GetType().GetProperty("Session").GetValue(game);
        var enemy=session.DebugSpawn(EnemyKind.Attack,1);enemy.Buffed=true;Sync(game);
        var aura=(GameObject)((IDictionary)Get(game,"auraViews"))[enemy.Id];
        enemy.Buffed=false;Sync(game);
        var callback=aura.AddComponent<CoreGuardianViewSyncActivationProbe>();
        var ground=(UnityEngine.Tilemaps.Tilemap)game.GetType().GetField("ground").GetValue(game);
        callback.target=ground.transform.parent;
        enemy.Buffed=true;Sync(game);
        Assert.That(callback.calls,Is.EqualTo(1));
        Assert.That(Vector3.Distance(aura.transform.position,World(game,enemy.Position)+Vector3.up*1.02f),Is.LessThan(.00001f));
    }
}

public class CoreGuardianViewSyncHierarchyProbe : MonoBehaviour
{
    public Transform target;public int calls;
    void OnTransformChildrenChanged(){if(target!=null&&calls==0&&transform.Find("EnemySpeedBuff")!=null){calls++;target.position+=new Vector3(2,-3,0);}}
}

public class CoreGuardianViewSyncActivationProbe : MonoBehaviour
{
    public Transform target;public int calls;
    void OnEnable(){if(target!=null){calls++;target.position+=new Vector3(2,-3,0);}}
}
