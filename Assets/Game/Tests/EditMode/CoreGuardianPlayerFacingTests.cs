using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

public class CoreGuardianPlayerFacingTests
{
    static Type Unit=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianUnitAnimation")).First(t=>t!=null);
    static object Call(object unit,string method,params object[] args)=>Unit.GetMethod(method).Invoke(unit,args);
    static GameObject Player()=>UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/ScreenTests/20261008_Depth/Prefabs/Player.prefab"));
    [Test]
    public void RearPoseKeepsTheFarElbowOutsideThePack_AndMirrorsBothOriginalLegsTogether()
    {
        var source=new Texture2D(2,2);var rear=new Texture2D(2,2);var normalLeg=new Texture2D(2,2);var rearLeg=new Texture2D(2,2);
        try
        {
            source.LoadImage(System.IO.File.ReadAllBytes("Assets/Art/ScreenTests/20260930/Sprites/player.png"));
            rear.LoadImage(System.IO.File.ReadAllBytes("Assets/Resources/CoreGuardian/PlayerFacing/D2/D2_Body.png"));
            Assert.That(rear.GetPixel(9,22),Is.EqualTo(source.GetPixel(26,22)),"The original far elbow belongs outside the pack, not in the middle of the back");
            foreach(var pair in new[]{("LeftLeg","RightLeg"),("RightLeg","LeftLeg")})
            {
                normalLeg.LoadImage(System.IO.File.ReadAllBytes("Assets/Resources/CoreGuardian/PlayerFacing/D5/D5_"+pair.Item2+".png"));
                rearLeg.LoadImage(System.IO.File.ReadAllBytes("Assets/Resources/CoreGuardian/PlayerFacing/D2/D2_"+pair.Item1+".png"));
                for(int y=0;y<48;y++)for(int x=0;x<36;x++)
                    if(normalLeg.GetPixel(x,y).a>.5f)Assert.That(rearLeg.GetPixel(35-x,y),Is.EqualTo(normalLeg.GetPixel(x,y)),"A rear pose must not mix an unmirrored lower body with mirrored arms");
            }
        }
        finally{foreach(var t in new[]{source,rear,normalLeg,rearLeg})UnityEngine.Object.DestroyImmediate(t);}
    }
    [Test]
    public void WalkingWithoutATargetKeepsTheToolRelaxed_AndStopsWithoutSnappingTheFeet()
    {
        var player=Player();
        try
        {
            var art=player.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="2D Sprite");var unit=Unit.GetMethod("Attach").Invoke(null,new object[]{player,"Player"});
            foreach(var step in new[]{(Vector2.right,0),(Vector2.up,2),(Vector2.left,4),(Vector2.down,6)})
            {
                Call(unit,"Move",step.Item1,1/60f);Call(unit,"Present",1/60f);
                Assert.That((int)Unit.GetProperty("Direction").GetValue(unit),Is.EqualTo(step.Item2),"A .12-second grid step must face its actual displacement on the first frame");
                var arm=art.transform.Find("PlayerWalk/Arm");
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(0,arm.localEulerAngles.z)),Is.LessThanOrEqualTo(10),"Walking alone must not point the wrench across the face or torso");
            }
            Call(unit,"Move",Vector2.right*(64*.6f/32),.25f);Call(unit,"Present",.01f);
            var walk=art.transform.Find("PlayerWalk");var leg=walk.Find("LeftLeg");var toe=new Vector3(-5,4,0)/32;var before=leg.TransformPoint(toe);
            Call(unit,"Move",Vector2.zero,1/60f);Call(unit,"Present",1/60f);
            Assert.That(walk.gameObject.activeSelf,Is.True,"A short step must settle rather than instantly swap to a different Idle pose");
            Assert.That(Vector3.Distance(before,leg.TransformPoint(toe))/(art.transform.lossyScale.x/32),Is.LessThan(1),"Stopping must not teleport the lifted foot");
        }
        finally{UnityEngine.Object.DestroyImmediate(player);}
    }
    [Test]
    public void FrontHeadReusesOneCompleteOriginalFace_WithoutDuplicatingGoggles()
    {
        var source=new Texture2D(2,2);var front=new Texture2D(2,2);
        try
        {
            source.LoadImage(System.IO.File.ReadAllBytes("Assets/Art/ScreenTests/20260930/Sprites/player.png"));
            front.LoadImage(System.IO.File.ReadAllBytes("Assets/Resources/CoreGuardian/PlayerFacing/D6/D6_Body.png"));
            for(int top=0;top<15;top++)for(int x=6;x<23;x++)
                if(source.GetPixel(x,47-top).a>.5f)Assert.That(front.GetPixel(x+2,47-top),Is.EqualTo(source.GetPixel(x,47-top)),"The whole source head must move together, not duplicate half of its face");
        }
        finally{UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(front);}
    }
    [Test]
    public void EightDirectionsChangeVisibleArtwork_WithoutRotatingTheSourceOrShadow()
    {
        var player=Player();
        try
        {
            var art=player.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="2D Sprite");var source=art.sprite;var position=art.transform.localPosition;var rotation=art.transform.localRotation;
            var shadows=player.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.name.Contains("그림자")).ToDictionary(r=>r,r=>r.transform.position);
            var unit=Unit.GetMethod("Attach").Invoke(null,new object[]{player,"Player"});var views=new System.Collections.Generic.HashSet<Sprite>();
            for(int d=0;d<8;d++)
            {
                var v=new Vector2(Mathf.Cos(d*Mathf.PI/4),Mathf.Sin(d*Mathf.PI/4));
                Call(unit,"Move",v,.3f);Call(unit,"Present",.1f);
                var root=art.transform.Find("PlayerWalk");Assert.That(root,Is.Not.Null,"The displayed character must have actual direction-specific walking art");
                var body=root.Find("Body").GetComponent<SpriteRenderer>();Assert.That(body.gameObject.activeInHierarchy&&body.enabled,Is.True);views.Add(body.sprite);
                Assert.That(body.sprite.name,Does.StartWith("D"+d+"_"));
                Assert.That(art.sprite,Is.SameAs(source));Assert.That(art.transform.localPosition,Is.EqualTo(position));Assert.That(art.transform.localRotation,Is.EqualTo(rotation));
                Assert.That(shadows.All(x=>x.Key.transform.position==x.Value),Is.True);
            }
            Assert.That(views.Count,Is.EqualTo(8));
        }
        finally{UnityEngine.Object.DestroyImmediate(player);}
    }
    [Test]
    public void WalkingBothLegsContinuesWhileAiming_AndRealFireImmediatelyUsesThatPose()
    {
        var player=Player();
        try
        {
            var art=player.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="2D Sprite");var unit=Unit.GetMethod("Attach").Invoke(null,new object[]{player,"Player"});
            var left=new System.Collections.Generic.List<float>();var right=new System.Collections.Generic.List<float>();
            Call(unit,"Aim",Vector2.right,1f);
            for(int i=0;i<48;i++)
            {
                Call(unit,"MoveWithoutTurning",Vector2.up*(64*.6f/32),1/48f);Call(unit,"Aim",Vector2.right,1f);Call(unit,"Present",1/48f);
                var root=art.transform.Find("PlayerWalk");Assert.That(root,Is.Not.Null);Assert.That(root.gameObject.activeSelf,Is.True);
                left.Add(root.Find("LeftLeg").localPosition.y);right.Add(root.Find("RightLeg").localPosition.y);
                Assert.That(root.Find("Body").GetComponent<SpriteRenderer>().sprite.name,Does.StartWith("D0_"),"Strafing must keep its target facing");
            }
            Assert.That(left.Max()-left.Min(),Is.GreaterThan(.05f));Assert.That(right.Max()-right.Min(),Is.GreaterThan(.05f));
            Call(unit,"Fire",Vector2.up,10f);
            var attack=art.transform.Find("OriginalMotion");Assert.That(attack.gameObject.activeSelf,Is.True);
            Assert.That(attack.Find("Fixed").GetComponent<SpriteRenderer>().sprite.name,Does.StartWith("D2_"),"Launch must snap to the actual target direction in the launch frame");
            var muzzle=(Vector3)Unit.GetProperty("MuzzleWorld").GetValue(unit);
            Assert.That(attack.Find("Moving").GetComponent<SpriteRenderer>().bounds.SqrDistance(muzzle),Is.LessThan(.001f),"The launch origin must stay on the animated tool/arm region");
            Call(unit,"Present",0f); // Present the launch frame before advancing recovery, as the combat adapter does.
            Call(unit,"Move",Vector2.zero,1f);Call(unit,"Present",1f);
            Assert.That(attack.gameObject.activeSelf,Is.False);Assert.That(art.transform.Find("OriginalIdle").gameObject.activeSelf,Is.True);
        }
        finally{UnityEngine.Object.DestroyImmediate(player);}
    }
}
