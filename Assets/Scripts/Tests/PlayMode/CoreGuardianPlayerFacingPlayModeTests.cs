using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class CoreGuardianPlayerFacingPlayModeTests
{
    static Type Find(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    [UnityTest]
    public IEnumerator NormalGridStepsDriveOriginalLegs_AndActualLaunchStartsAtTheAimedTool()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var type=Find("CoreGuardianPrototype");var game=(MonoBehaviour)UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();game.enabled=false;
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;var movement=(MonoBehaviour)type.GetField("player").GetValue(game);
        var movementType=movement.GetType();var unitType=Find("CoreGuardianUnitAnimation");var unit=movement.GetComponent(unitType);
        var combat=game.GetComponent(Find("CoreGuardianCombatAnimation"));var art=movement.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");
        var left=new System.Collections.Generic.List<float>();var right=new System.Collections.Generic.List<float>();int successful=0;
        // Legal grid input, actual movement Update and the actual presentation adapter. No forced combat results.
        foreach(var d in new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left})
        {
            if(!(bool)movementType.GetMethod("TryStep").Invoke(movement,new object[]{d}))continue;
            successful++;
            for(int frame=0;frame<12;frame++)
            {
                yield return null;combat.GetType().GetMethod("Player").Invoke(combat,null);
                var walk=art.transform.Find("PlayerWalk");
                if(walk.gameObject.activeSelf)
                {
                    left.Add(walk.Find("LeftLeg").localPosition.y);right.Add(walk.Find("RightLeg").localPosition.y);
                    int direction=d.x>0?0:d.y>0?2:d.x<0?4:6;
                    Assert.That((int)unitType.GetProperty("Direction").GetValue(unit),Is.EqualTo(direction),"Actual grid displacement must immediately choose the matching body facing");
                    Assert.That(Mathf.Abs(Mathf.DeltaAngle(0,walk.Find("Arm").localEulerAngles.z)),Is.LessThanOrEqualTo(10),"With no enemy target, normal movement must keep the tool relaxed");
                }
            }
            while((bool)movementType.GetField("isMoving",flags).GetValue(movement)){yield return null;combat.GetType().GetMethod("Player").Invoke(combat,null);}
        }
        Assert.That(successful,Is.GreaterThanOrEqualTo(2));Assert.That(left.Count,Is.GreaterThan(2));
        Assert.That(left.Max()-left.Min(),Is.GreaterThan(.02f));Assert.That(right.Max()-right.Min(),Is.GreaterThan(.02f));
        var session=(CoreGuardian.Session)type.GetProperty("Session").GetValue(game);var playerTile=(Vector2)type.GetMethod("PlayerTile",flags).Invoke(game,null);
        // Spawn on the route cell nearest the player so the shot is in range regardless of map length.
        var route=(System.Collections.Generic.IReadOnlyList<Vector2Int>)type.GetProperty("RouteCells").GetValue(game);float along=0,best=float.MaxValue,spawnAt=0;
        for(int i=0;i<route.Count;i++){if(i>0)along+=Vector2.Distance(route[i-1],route[i]);float gap=Vector2.Distance(route[i],playerTile);if(gap<best){best=gap;spawnAt=along;}}
        session.StartCombat();session.DebugSpawn(CoreGuardian.EnemyKind.Defense,spawnAt);session.Player.Rate=8;
        int launches=0;session.ProjectileLaunched+=shot=>
        {
            if(shot.Source!="Player")return;launches++;
            var muzzle=(Vector3)unitType.GetProperty("MuzzleWorld").GetValue(unit);
            var actual=(Vector3)combat.GetType().GetMethod("ProjectilePosition").Invoke(combat,new object[]{shot.Handle});
            Assert.That(Vector3.Distance(actual,muzzle),Is.LessThan(.0001f),"The actual projectile must start at this frame's aimed tool");
            Assert.That(art.transform.Find("OriginalMotion").gameObject.activeSelf,Is.True);
            var world=type.GetMethod("World");Vector2 aim=(Vector3)world.Invoke(game,new object[]{shot.Target.Position})-(Vector3)world.Invoke(game,new object[]{shot.Position});
            int direction=(Mathf.RoundToInt(Mathf.Atan2(aim.y,aim.x)/(Mathf.PI/4))+8)%8;
            Assert.That(art.transform.Find("OriginalMotion/Fixed").GetComponent<SpriteRenderer>().sprite.name,Does.StartWith("D"+direction+"_"));
        };
        for(int i=0;i<20&&launches==0;i++){session.Tick(.05f,playerTile);type.GetMethod("SynchronizeViews",flags).Invoke(game,null);yield return null;}
        Assert.That(launches,Is.GreaterThan(0),"phase "+session.Phase+" player "+playerTile+" range "+session.Player.Range+" enemies "+string.Join(";",session.Enemies.Select(e=>e.Kind+"@"+e.Position+" alive "+e.Alive)));
#else
        yield return null;
#endif
    }
}
