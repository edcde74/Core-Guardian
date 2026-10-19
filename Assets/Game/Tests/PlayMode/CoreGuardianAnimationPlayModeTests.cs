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
    public IEnumerator FrontGait_AllOtherEnemiesAlternateOnActualDownwardWaypoints()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var type=Find("CoreGuardianPrototype");var game=(MonoBehaviour)UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();
        var session=(CoreGuardian.Session)type.GetProperty("Session").GetValue(game);
        var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
        var path=(System.Collections.Generic.List<Vector2>)typeof(CoreGuardian.Session).GetField("path",flags).GetValue(session);
        float distance=0;int segment=-1;
        for(int i=0;i<path.Count-1;i++){var step=path[i+1]-path[i];if(step.y<-.5f&&Mathf.Abs(step.y)>Mathf.Abs(step.x)){segment=i;break;}distance+=step.magnitude;}
        Assert.That(segment,Is.GreaterThanOrEqualTo(0),"Use the actual scene's downward route, not a replacement board");
        var views=(System.Collections.IDictionary)type.GetField("enemyViews",flags).GetValue(game);
        game.enabled=false;session.StartCombat();
        var kinds=new[]{CoreGuardian.EnemyKind.Defense,CoreGuardian.EnemyKind.Buff,CoreGuardian.EnemyKind.Boss};
        var enemies=kinds.Select((k,i)=>session.DebugSpawn(k,distance+.1f+i*.7f)).ToArray();
        var heights=new System.Collections.Generic.List<float>[3,2];var poses=new System.Collections.Generic.HashSet<string>[3];
        for(int i=0;i<3;i++){poses[i]=new System.Collections.Generic.HashSet<string>();for(int b=0;b<2;b++)heights[i,b]=new System.Collections.Generic.List<float>();}
        type.GetMethod("SynchronizeViews",flags).Invoke(game,null);
        var previous=enemies.Select(e=>e.Position).ToArray();
        for(int step=0;step<100;step++)
        {
            session.Tick(.025f,new Vector2(-100,-100));type.GetMethod("SynchronizeViews",flags).Invoke(game,null);
            for(int i=0;i<3;i++)
            {
                var delta=enemies[i].Position-previous[i];previous[i]=enemies[i].Position;
                if(delta.y>=-.00001f||Mathf.Abs(delta.x)>Mathf.Abs(delta.y))continue;
                var view=(GameObject)views[enemies[i].Id];var art=view.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");
                var vertical=art.transform.Find("VerticalWalk");
                Assert.That(vertical.gameObject.activeInHierarchy,Is.True);Assert.That(vertical.GetComponent<SpriteRenderer>().enabled,Is.False,"Fixed front legs must not be drawn behind the moving pair");
                var sprite=vertical.GetComponent<SpriteRenderer>().sprite;Assert.That(sprite.name,Does.StartWith("Down_"));poses[i].Add(sprite.name);
                for(int b=0;b<2;b++){var foot=vertical.Find(b==0?"LeftFoot":"RightFoot");Assert.That(foot.gameObject.activeInHierarchy&&foot.GetComponent<SpriteRenderer>().enabled,Is.True);heights[i,b].Add(foot.localPosition.y*32);}
            }
            if(step%20==0)yield return null;
        }
        for(int i=0;i<3;i++)
        {
            Assert.That(poses[i].Count,Is.EqualTo(4),kinds[i]+" must present all four source upper-body frames on the route");
            for(int b=0;b<2;b++)Assert.That(heights[i,b].Max()-heights[i,b].Min(),Is.GreaterThan(3),kinds[i]+" both boots must lift and return on the real down segment");
            var unit=((GameObject)views[enemies[i].Id]).GetComponent(Find("CoreGuardianUnitAnimation"));
            unit.GetType().GetMethod("Move").Invoke(unit,new object[]{Vector2.zero,.1f});unit.GetType().GetMethod("Present").Invoke(unit,new object[]{.1f});
            var vertical=unit.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="VerticalWalk").transform;
            Assert.That(vertical.Find("LeftFoot").localPosition.y,Is.Zero);Assert.That(vertical.Find("RightFoot").localPosition.y,Is.Zero);
            unit.GetType().GetMethod("Move").Invoke(unit,new object[]{Vector2.up,.1f});unit.GetType().GetMethod("Present").Invoke(unit,new object[]{.1f});
            bool rearDefense=kinds[i]==CoreGuardian.EnemyKind.Defense;
            Assert.That(vertical.GetComponent<SpriteRenderer>().enabled,Is.EqualTo(!rearDefense));Assert.That(vertical.Find("LeftFoot").gameObject.activeSelf,Is.EqualTo(rearDefense));
        }
#else
        yield return null;
#endif
    }
    [UnityTest]
    public IEnumerator DefenseRearWiderStanceIsVisibleOnActualUpwardWaypoint()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var type=Find("CoreGuardianPrototype");var game=(MonoBehaviour)UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var session=(CoreGuardian.Session)type.GetProperty("Session").GetValue(game);
        var path=(System.Collections.Generic.List<Vector2>)typeof(CoreGuardian.Session).GetField("path",flags).GetValue(session);
        float distance=0;bool found=false;
        for(int i=0;i<path.Count-1;i++){var step=path[i+1]-path[i];if(step.y>.5f&&Mathf.Abs(step.y)>Mathf.Abs(step.x)){found=true;break;}distance+=step.magnitude;}
        Assert.That(found,Is.True);game.enabled=false;session.StartCombat();var enemy=session.DebugSpawn(CoreGuardian.EnemyKind.Defense,distance+.5f);
        var views=(System.Collections.IDictionary)type.GetField("enemyViews",flags).GetValue(game);type.GetMethod("SynchronizeViews",flags).Invoke(game,null);
        var heights=new[]{new System.Collections.Generic.List<float>(),new System.Collections.Generic.List<float>()};
        for(int i=0;i<100;i++)
        {
            session.Tick(.025f,new Vector2(-100,-100));type.GetMethod("SynchronizeViews",flags).Invoke(game,null);
            var art=((GameObject)views[enemy.Id]).GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");var v=art.transform.Find("VerticalWalk");
            Assert.That(v.gameObject.activeInHierarchy,Is.True);Assert.That(v.GetComponent<SpriteRenderer>().sprite.name,Does.StartWith("Up_"));
            var left=v.Find("LeftFoot");var right=v.Find("RightFoot");
            Assert.That(left.gameObject.activeInHierarchy&&right.gameObject.activeInHierarchy,Is.True);
            Assert.That((right.localPosition.x-left.localPosition.x)*32,Is.EqualTo(9).Within(.01f));
            heights[0].Add(left.localPosition.y*32);heights[1].Add(right.localPosition.y*32);
            if(i%20==0)yield return null;
        }
        foreach(var h in heights)Assert.That(h.Max()-h.Min(),Is.GreaterThan(3));
#else
        yield return null;
#endif
    }
    [UnityTest]
    [Category("SharedTowersHud")]
    public IEnumerator EnemyHealthBarsClearEachSprite_AndPlayerHasNoStaticSoleMarks()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var type=Find("CoreGuardianPrototype");var game=(MonoBehaviour)UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();
        var session=(CoreGuardian.Session)type.GetProperty("Session").GetValue(game);
        var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
        var views=(System.Collections.IDictionary)type.GetField("enemyViews",flags).GetValue(game);
        game.enabled=false;session.StartCombat();
        foreach(var kind in new[]{CoreGuardian.EnemyKind.Attack,CoreGuardian.EnemyKind.Defense,CoreGuardian.EnemyKind.Buff,CoreGuardian.EnemyKind.Boss})session.DebugSpawn(kind,77);
        type.GetMethod("SynchronizeViews",flags).Invoke(game,null);
        foreach(var enemy in session.Enemies)
        {
            var view=(GameObject)views[enemy.Id];var bar=view.transform.Find("Health");
            var art=view.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");
            Assert.That(bar.GetComponent<SpriteRenderer>().bounds.min.y,Is.GreaterThan(art.bounds.max.y+.025f),enemy.Kind+" health must clear the source sprite");
            var unit=view.GetComponent(Find("CoreGuardianUnitAnimation"));
            Assert.That(bar.position,Is.EqualTo((Vector3)unit.GetType().GetProperty("HealthBarWorld").GetValue(unit)));
            Assert.That(bar.localScale,Is.EqualTo(Vector3.one));
            var cells=bar.GetComponent<CoreGuardian.SegmentedEnemyHealthBar>();
            Assert.That(cells.SegmentCount,Is.EqualTo(enemy.Kind==CoreGuardian.EnemyKind.Boss?12:8));
            Assert.That(cells.FilledSegments,Is.EqualTo(cells.SegmentCount));
            var sprite=bar.GetComponent<SpriteRenderer>().sprite;float framedWidth=sprite.bounds.size.x;
            enemy.Health=enemy.Spec.Health/2;type.GetMethod("SynchronizeViews",flags).Invoke(game,null);
            Assert.That(cells.FilledSegments,Is.EqualTo(cells.SegmentCount/2));
            Assert.That(bar.GetComponent<SpriteRenderer>().sprite.bounds.size.x,Is.EqualTo(framedWidth));
        }
        var player=(Component)type.GetField("player").GetValue(game);
        Assert.That(player.GetComponentsInChildren<SpriteRenderer>(true).Single(r=>r.name=="바닥 밀착선").enabled,Is.False);
        Assert.That(player.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.sprite!=null&&r.sprite.name.Contains("Shadow")).All(r=>r.enabled),Is.True);
#else
        yield return null;
#endif
    }
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
        var samples=new int[4];var poses=new System.Collections.Generic.HashSet<string>[4];for(int d=0;d<4;d++)poses[d]=new System.Collections.Generic.HashSet<string>();
        var bootHeights=new System.Collections.Generic.List<float>[2,2];for(int d=0;d<2;d++)for(int b=0;b<2;b++)bootHeights[d,b]=new System.Collections.Generic.List<float>();
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
                    if(d>=2)
                    {
                        var vertical=art.transform.Find("VerticalWalk");Assert.That(vertical.gameObject.activeSelf,Is.True,"Actual Y waypoint must switch artwork");
                        var sprite=vertical.GetComponent<SpriteRenderer>().sprite;Assert.That(sprite.name,Does.StartWith(d==2?"Up_":"Down_"));Assert.That(rig.gameObject.activeSelf,Is.False);poses[d].Add(sprite.name);
                        for(int boot=0;boot<2;boot++)
                        {
                            var foot=vertical.Find(boot==0?"LeftFoot":"RightFoot");Assert.That(foot,Is.Not.Null,"Normal route must show two independently moving boots");
                            Assert.That(foot.gameObject.activeInHierarchy&&foot.GetComponent<SpriteRenderer>().enabled,Is.True);
                            bootHeights[d-2,boot].Add(foot.localPosition.y*32);
                        }
                    }
                    else {Assert.That(rig.gameObject.activeSelf,Is.True,"Actual route axis "+d);poses[d].Add(rig.Find("LeftLower").localRotation.ToString("F5"));}
                    samples[d]++;
                }
                previous=position;
            }
            if(samples.All(n=>n>=12)&&poses[0].Count>=8&&poses[1].Count>=8&&poses[2].Count==4&&poses[3].Count==4)break;
            if(step%50==0)yield return null;
        }
        for(int d=0;d<4;d++){Assert.That(samples[d],Is.GreaterThanOrEqualTo(12),"Route axis "+d);Assert.That(poses[d].Count,Is.GreaterThanOrEqualTo(d<2?8:4),"Actual displayed poses axis "+d);}
        for(int d=0;d<2;d++)for(int boot=0;boot<2;boot++)Assert.That(bootHeights[d,boot].Max()-bootHeights[d,boot].Min(),Is.GreaterThan(2),"Both visible boots must lift on actual vertical waypoint segments");
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
                    Assert.That(art.transform.Find(d<2?"OriginalWalk":"VerticalWalk").gameObject.activeSelf,Is.True);
                }
                animationType.GetMethod("Move").Invoke(unit,new object[]{Vector2.zero,.3f});animationType.GetMethod("Present").Invoke(unit,new object[]{.3f});
                Assert.That(art.enabled,Is.False);Assert.That(art.transform.Find("OriginalWalk").gameObject.activeSelf,Is.False);Assert.That(art.transform.Find("VerticalWalk").GetComponent<SpriteRenderer>().sprite.name,Is.EqualTo("Down_0"));
            }
            finally{UnityEngine.Object.Destroy(clone);}
        }
#else
        yield return null;
#endif
    }
}
