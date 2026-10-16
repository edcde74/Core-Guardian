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
    [TestCase("Attack","enemy_attack")]
    [TestCase("Defense","enemy_defense")]
    [TestCase("Buff","enemy_buff")]
    [TestCase("Boss","")]
    public void WaypointTurnSelectsActualRearAndFrontArtwork_ThenRestoresOriginalHorizontalRig(string role,string name)
    {
        var prefab=role=="Boss"?Resources.Load<GameObject>("CoreGuardian/Animation/Boss/BossAnimated"):AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/ScreenTests/20261008_Depth/Prefabs/"+name+".prefab");
        var clone=UnityEngine.Object.Instantiate(prefab);
        try
        {
            var art=clone.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="2D Sprite");var source=art.sprite;
            var walk=Activator.CreateInstance(Walk,new object[]{art,role});
            void Step(Vector2 v){Walk.GetMethod("Move").Invoke(walk,new object[]{v,.1f});Walk.GetMethod("Present").Invoke(walk,new object[]{.1f});}
            Step(Vector2.right);
            var vertical=art.transform.Find("VerticalWalk");Assert.That(vertical,Is.Not.Null,"Direction must select actual directional art, not only alter boot offsets");
            Step(Vector2.up);Assert.That(vertical.gameObject.activeSelf,Is.True);Assert.That(vertical.GetComponent<SpriteRenderer>().sprite.name,Does.StartWith("Up_"));Assert.That(art.transform.Find("OriginalWalk").gameObject.activeSelf,Is.False);
            Step(Vector2.down);Assert.That(vertical.gameObject.activeSelf,Is.True);Assert.That(vertical.GetComponent<SpriteRenderer>().sprite.name,Does.StartWith("Down_"));
            Step(Vector2.zero);Assert.That(vertical.GetComponent<SpriteRenderer>().sprite.name,Is.EqualTo("Down_0"),"Stopping keeps its actual facing");
            Step(Vector2.left);Assert.That(vertical.gameObject.activeSelf,Is.False);Assert.That(art.transform.Find("OriginalWalk").gameObject.activeSelf,Is.True);Assert.That(art.sprite,Is.SameAs(source));
        }
        finally{UnityEngine.Object.DestroyImmediate(clone);}
    }
    [Test]
    public void VerticalDirectionsHaveFourDistinctFrames_WithFixedGroundPivotAndPixelImport()
    {
        foreach(var role in new[]{"Attack","Defense","Buff","Boss"})foreach(var direction in new[]{"Up","Down"})
        {
            var images=new System.Collections.Generic.HashSet<string>();
            for(int frame=0;frame<4;frame++)
            {
                var sprite=Resources.Load<Sprite>("CoreGuardian/VerticalWalk/"+role+"/"+direction+"_"+frame);Assert.That(sprite,Is.Not.Null);
                Assert.That(Vector2.Distance(sprite.pivot,new Vector2(48,3)),Is.LessThan(.0001f));Assert.That(sprite.pixelsPerUnit,Is.EqualTo(32));Assert.That(sprite.texture.filterMode,Is.EqualTo(FilterMode.Point));
                var path=AssetDatabase.GetAssetPath(sprite.texture);var importer=(TextureImporter)AssetImporter.GetAtPath(path);Assert.That(importer.mipmapEnabled,Is.False);Assert.That(importer.textureCompression,Is.EqualTo(TextureImporterCompression.Uncompressed));
                images.Add(Convert.ToBase64String(System.IO.File.ReadAllBytes(path)));
            }
            Assert.That(images.Count,Is.EqualTo(4),role+"/"+direction+" actual image content");
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
                var leftPoses=new System.Collections.Generic.HashSet<Quaternion>();var rightPoses=new System.Collections.Generic.HashSet<Quaternion>();var frames=new System.Collections.Generic.HashSet<Sprite>();
                for(int i=0;i<48;i++)
                {
                    unitType.GetMethod("Move").Invoke(unit,new object[]{direction*(cycle*art.transform.lossyScale.x/32),1/48f});unitType.GetMethod("Present").Invoke(unit,new object[]{1/48f});
                    if(direction.y!=0){var vertical=art.transform.Find("VerticalWalk");Assert.That(vertical.gameObject.activeSelf,Is.True);frames.Add(vertical.GetComponent<SpriteRenderer>().sprite);}
                    else {Assert.That(rig.gameObject.activeSelf,Is.True);leftPoses.Add(left.localRotation);rightPoses.Add(right.localRotation);}
                }
                if(direction.y!=0)Assert.That(frames.Count,Is.EqualTo(4),role+" needs the entire directional gait");
                else {Assert.That(leftPoses.Count,Is.GreaterThan(20),role+" left leg "+direction);Assert.That(rightPoses.Count,Is.GreaterThan(20),role+" right leg "+direction);}
                unitType.GetMethod("Move").Invoke(unit,new object[]{Vector2.zero,1f});unitType.GetMethod("Present").Invoke(unit,new object[]{1f});
                var anchor=new Vector3(art.flipX?-x:x,y,0)/32;
                Assert.That(Vector3.Distance(art.transform.TransformPoint(anchor),shadows.Keys.First().bounds.center),Is.LessThan(.00001f));
                Assert.That(art.enabled,Is.EqualTo(direction.y==0));Assert.That(art.sprite,Is.SameAs(sprite));
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
