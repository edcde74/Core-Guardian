using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

public class CoreGuardianOriginalGaitTests
{
    static Type Walk=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianOriginalWalk")).First(t=>t!=null);
    static Vector2 Foot(float phase,Vector2 direction)=>(Vector2)Walk.GetMethod("FootOffset").Invoke(null,new object[]{phase,14f,1.8f,direction});
    [Test]
    public void VerticalStepsSeparateGroundDepthFromReturnLift()
    {
        foreach(var direction in new[]{Vector2.up,Vector2.down})
        {
            Vector2 Step(float p)=>(Vector2)Walk.GetMethod("WalkingFootOffset").Invoke(null,new object[]{p,14f,1.8f,direction});
            Assert.That(Vector2.Distance(Step(.75f),Vector2.up*3.6f),Is.LessThan(.0001f));
            Assert.That(Vector2.Distance(Step(.2f)-Step(.1f),-direction*.42f),Is.LessThan(.0001f));
        }
    }
    [TestCase("Attack", "enemy_attack",4f,2f,14f)]
    [TestCase("Defense", "enemy_defense",5.5f,4f,10f)]
    [TestCase("Buff", "enemy_buff",-1.75f,1.5f,12f)]
    [TestCase("Boss", "",-.75f,2f,18f)]
    public void AllDirectionsAnimateBothLegs_AndMirroredStopKeepsGroundAnchor(string role,string name,float x,float y,float cycle)
    {
        var prefab=role=="Boss"?Resources.Load<GameObject>("CoreGuardian/Animation/Boss/BossAnimated"):AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/ScreenTests/20261008_Depth/Prefabs/"+name+".prefab");
        var clone=UnityEngine.Object.Instantiate(prefab);
        try
        {
            clone.transform.position=new Vector3(5,7,0);clone.transform.localScale*=1.7f;
            var root=clone.transform.position;var scale=clone.transform.localScale;
            var art=clone.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="2D Sprite");var sprite=art.sprite;
            var shadows=clone.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.sprite.name.Contains("Shadow")).ToDictionary(r=>r,r=>r.transform.position);
            var unitType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianUnitAnimation")).First(t=>t!=null);
            var unit=unitType.GetMethod("Attach").Invoke(null,new object[]{clone,role});
            var rig=art.transform.Find("OriginalWalk");var left=rig.Find("LeftLower");var right=rig.Find("RightLower");
            foreach(var direction in new[]{Vector2.right,Vector2.up,Vector2.left,Vector2.down,Vector2.up})
            {
                var leftPoses=new System.Collections.Generic.HashSet<Quaternion>();var rightPoses=new System.Collections.Generic.HashSet<Quaternion>();
                for(int i=0;i<48;i++)
                {
                    unitType.GetMethod("Move").Invoke(unit,new object[]{direction*(cycle*art.transform.lossyScale.x/32),1/48f});unitType.GetMethod("Present").Invoke(unit,new object[]{1/48f});
                    Assert.That(rig.gameObject.activeSelf,Is.True);leftPoses.Add(left.localRotation);rightPoses.Add(right.localRotation);
                }
                Assert.That(leftPoses.Count,Is.GreaterThan(20),role+" left leg "+direction);Assert.That(rightPoses.Count,Is.GreaterThan(20),role+" right leg "+direction);
                unitType.GetMethod("Move").Invoke(unit,new object[]{Vector2.zero,1f});unitType.GetMethod("Present").Invoke(unit,new object[]{1f});
                var anchor=new Vector3(art.flipX?-x:x,y,0)/32;
                Assert.That(Vector3.Distance(art.transform.TransformPoint(anchor),shadows.Keys.First().bounds.center),Is.LessThan(.00001f));
                Assert.That(art.enabled,Is.True);Assert.That(art.sprite,Is.SameAs(sprite));
                Assert.That(clone.transform.position,Is.EqualTo(root));Assert.That(clone.transform.localScale,Is.EqualTo(scale));Assert.That(shadows.All(s=>s.Key.transform.position==s.Value),Is.True);
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(clone);}
    }
    [Test]
    public void BothFeetUseIdenticalTrajectories_HalfACycleApart_WithContinuousContact()
    {
        foreach(var direction in new[]{Vector2.left,Vector2.right,Vector2.up,Vector2.down})
        {
            for(int i=0;i<100;i++)
            {
                float phase=i/100f;
                Assert.That(Vector2.Distance(Foot(phase,direction),Foot(phase+1,direction)),Is.LessThan(.0001f));
                Assert.That(Vector2.Distance(Foot(phase+.5f,direction),Foot(phase-.5f,direction)),Is.LessThan(.0001f));
            }
            Assert.That(Vector2.Distance(Foot(.5f-.00001f,direction),Foot(.5f+.00001f,direction)),Is.LessThan(.002f));
            Assert.That(Vector2.Distance(Foot(1-.00001f,direction),Foot(.00001f,direction)),Is.LessThan(.002f));
        }
    }
    [Test]
    public void PlantedFootCancelsRootTravel_AndLiftOccursOnlyDuringSwing()
    {
        foreach(var direction in new[]{Vector2.left,Vector2.right,Vector2.up,Vector2.down})
        {
            var start=Foot(.1f,direction);
            var later=Foot(.2f,direction);
            Assert.That(Vector2.Distance(later-start,-direction*1.4f),Is.LessThan(.0001f),"A planted foot must move backward at exactly root travel speed");
            Assert.That(Vector2.Distance(Foot(.75f,direction),Vector2.up*1.8f),Is.LessThan(.0001f),"Either leg reaches the same swing lift");
        }
    }
}
