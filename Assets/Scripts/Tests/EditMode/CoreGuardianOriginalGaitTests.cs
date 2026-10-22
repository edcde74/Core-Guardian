using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

public class CoreGuardianOriginalGaitTests
{
    [Test]
    public void BossFrontTorsoAndHipsUseOneCoherentPose_WithoutAWaistFrameSeam()
    {
        var clone=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("CoreGuardian/Animation/Boss/BossAnimated"));
        try
        {
            var art=clone.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="2D Sprite");var walk=Activator.CreateInstance(Walk,new object[]{art,"Boss"});
            var rest=Resources.Load<Sprite>("CoreGuardian/VerticalWalk/Boss/Down_0");
            for(int f=0;f<16;f++)
            {
                Walk.GetMethod("Move").Invoke(walk,new object[]{Vector2.down*(18*art.transform.lossyScale.x/32),1/16f});Walk.GetMethod("Present").Invoke(walk,new object[]{1/16f});
                var vertical=art.transform.Find("VerticalWalk");
                foreach(var name in new[]{"UpperBody","LeftEquipment","RightEquipment","HipCloth"})
                    Assert.That(vertical.Find(name).GetComponent<SpriteRenderer>().sprite.texture,Is.SameAs(rest.texture),"Torso, equipment and hip cloth must share the same source pose, not stitch unrelated waist frames");
                float width=(vertical.Find("RightFoot").localPosition.x-vertical.Find("LeftFoot").localPosition.x)*32;
                Assert.That(width,Is.InRange(9.5f,10.5f),"Boots must sit under this pelvis rather than outside its hip sockets");
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(clone);}
    }
    [Test]
    public void RearDefenseHasSeparatedRearBoots_AndKeepsItsShield()
    {
        var clone=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Units/enemy_defense.prefab"));
        try
        {
            var art=clone.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="2D Sprite");var walk=Activator.CreateInstance(Walk,new object[]{art,"Defense"});
            Walk.GetMethod("Move").Invoke(walk,new object[]{Vector2.up,.1f});Walk.GetMethod("Present").Invoke(walk,new object[]{.1f});
            var vertical=art.transform.Find("VerticalWalk");
            var left=vertical.Find("LeftFoot");var right=vertical.Find("RightFoot");
            Assert.That(left.gameObject.activeInHierarchy&&right.gameObject.activeInHierarchy,Is.True,"Rear boots must be independently visible");
            Assert.That((right.localPosition.x-left.localPosition.x)*32,Is.InRange(8.5f,9.5f),"Widen the rear stance without moving the foot centre or root");
            Assert.That(left.GetComponent<SpriteRenderer>().sprite.texture,Is.SameAs(Resources.Load<Sprite>("CoreGuardian/VerticalWalk/Defense/Up_0").texture),"Use rear boot artwork, not mirrored front/side legs");
            Assert.That(vertical.Find("RightEquipment").GetComponent<SpriteRenderer>().sprite,Is.Not.Null,"Keep the original shield region");
        }
        finally{UnityEngine.Object.DestroyImmediate(clone);}
    }
    static Type Walk=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianOriginalWalk")).First(t=>t!=null);
    static Vector2 Foot(float phase,Vector2 direction)=>(Vector2)Walk.GetMethod("FootOffset").Invoke(null,new object[]{phase,14f,1.8f,direction});
    [Test]
    public void RearAttackBootsKeepTheCompleteSourceWidth_WithoutFixedLegFragments()
    {
        var clone=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Units/enemy_attack.prefab"));
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
        try
        {
            var art=clone.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="2D Sprite");var walk=Activator.CreateInstance(Walk,new object[]{art,"Attack"});
            Walk.GetMethod("Move").Invoke(walk,new object[]{Vector2.up,0f});Walk.GetMethod("Present").Invoke(walk,new object[]{.1f});
            var vertical=art.transform.Find("VerticalWalk");
            // A packed display texture is not a PNG asset. Read its actual GPU pixels.
            var display=vertical.GetComponent<SpriteRenderer>().sprite.texture;
            var previous=RenderTexture.active;var rt=RenderTexture.GetTemporary(display.width,display.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
            try{Graphics.Blit(display,rt);RenderTexture.active=rt;texture.Reinitialize(display.width,display.height);texture.ReadPixels(new Rect(0,0,display.width,display.height),0,0);texture.Apply();}
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
            foreach(var name in new[]{"LeftFoot","RightFoot"})
            {
                var boot=vertical.Find(name).GetComponent<SpriteRenderer>().sprite;var rect=boot.rect;
                for(int y=5;y<=9;y++)
                {
                    int opaque=0;for(int x=0;x<rect.width;x++)if(texture.GetPixel((int)rect.x+x,(int)rect.y+y).a>.5f)opaque++;
                    Assert.That(opaque,Is.GreaterThanOrEqualTo(6),name+" must retain the full source shin/boot, not a thin sliced edge");
                }
            }
            var equipment=vertical.Find("LeftEquipment").GetComponent<SpriteRenderer>().sprite.rect;
            for(int y=4;y<=10;y++)for(int x=0;x<equipment.width;x++)
                Assert.That(texture.GetPixel((int)equipment.x+x,(int)equipment.y+y).a,Is.LessThan(.5f),"No fixed source-leg stripe may remain beside the animated pair");
        }
        finally{UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(clone);}
    }
    [TestCase("Attack","enemy_attack",14f,1f)]
    [TestCase("Attack","enemy_attack",14f,-1f)]
    [TestCase("Defense","enemy_defense",10f,-1f)]
    [TestCase("Defense","enemy_defense",10f,1f)]
    [TestCase("Buff","enemy_buff",12f,-1f)]
    [TestCase("Boss","",18f,-1f)]
    public void VerticalWalkAlternatesBothVisibleBoots(string role,string name,float cycle,float direction)
    {
        var prefab=role=="Boss"?Resources.Load<GameObject>("CoreGuardian/Animation/Boss/BossAnimated"):AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Units/"+name+".prefab");
        var clone=UnityEngine.Object.Instantiate(prefab);
        try
        {
            var art=clone.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="2D Sprite");
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianUnitAnimation")).First(t=>t!=null);
            var unit=type.GetMethod("Attach").Invoke(null,new object[]{clone,role});
            var left=new System.Collections.Generic.List<float>();var right=new System.Collections.Generic.List<float>();
            var velocity=Vector2.up*(direction*cycle*art.transform.lossyScale.x/32);
            for(int frame=0;frame<48;frame++)
            {
                type.GetMethod("Move").Invoke(unit,new object[]{velocity,1/48f});type.GetMethod("Present").Invoke(unit,new object[]{1/48f});
                var vertical=art.transform.Find("VerticalWalk");
                var l=vertical.Find("LeftFoot");var r=vertical.Find("RightFoot");
                Assert.That(l,Is.Not.Null,"Y gait must move both visible boots, not only cycle different full-body images");Assert.That(r,Is.Not.Null);
                var lr=l.GetComponent<SpriteRenderer>();var rr=r.GetComponent<SpriteRenderer>();
                Assert.That(lr.enabled&&rr.enabled,Is.True);Assert.That(lr.sprite,Is.Not.Null);Assert.That(rr.sprite,Is.Not.Null);
                left.Add(lr.bounds.min.y);right.Add(rr.bounds.min.y);
            }
            float pixel=art.transform.lossyScale.x/32;
            float leftRange=left.Max()-left.Min(),rightRange=right.Max()-right.Min();
            Assert.That(leftRange/pixel,Is.GreaterThan(2),"Left visible sole must lift and return");
            Assert.That(rightRange/pixel,Is.GreaterThan(2),"Right visible sole must lift and return");
            Assert.That(Mathf.Abs(leftRange-rightRange)/pixel,Is.LessThan(.1f),"Both legs need equal swing amplitude");
            for(int i=0;i<24;i++)Assert.That(Mathf.Abs(left[i]-right[(i+24)%48])/pixel,Is.LessThan(.1f),"Boots must alternate half a cycle apart");
        }
        finally{UnityEngine.Object.DestroyImmediate(clone);}
    }
    [TestCase("Attack","enemy_attack")]
    [TestCase("Defense","enemy_defense")]
    [TestCase("Buff","enemy_buff")]
    [TestCase("Boss","")]
    public void WaypointTurnSelectsActualRearAndFrontArtwork_ThenRestoresOriginalHorizontalRig(string role,string name)
    {
        var prefab=role=="Boss"?Resources.Load<GameObject>("CoreGuardian/Animation/Boss/BossAnimated"):AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Units/"+name+".prefab");
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
                var path=AssetDatabase.GetAssetPath(sprite);var importer=(TextureImporter)AssetImporter.GetAtPath(path);Assert.That(importer.mipmapEnabled,Is.False);Assert.That(importer.textureCompression,Is.EqualTo(TextureImporterCompression.Uncompressed));
                images.Add(Convert.ToBase64String(System.IO.File.ReadAllBytes(path)));
            }
            Assert.That(images.Count,Is.EqualTo(4),role+"/"+direction+" actual image content");
        }
    }
    [Test]
    public void AttackSourceBootsShareTheGroundLine()
    {
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
        try
        {
            ImageConversion.LoadImage(texture,System.IO.File.ReadAllBytes("Assets/Art/Characters/Enemies/Sprites/enemy_attack_v2.png"));
            int Bottom(int first,int last)
            {
                for(int y=0;y<12;y++)for(int x=first;x<=last;x++)if(texture.GetPixel(x,y).a>.5f)return y;
                throw new Exception("Missing boot pixels");
            }
            Assert.That(Bottom(12,24),Is.EqualTo(Bottom(31,38)),"The mirrored screen-right boot must not hover above the other sole");
        }
        finally{UnityEngine.Object.DestroyImmediate(texture);}
    }
    [TestCase("Attack", "enemy_attack",3.5f,0f,14f)]
    [TestCase("Defense", "enemy_defense",5.5f,4f,10f)]
    [TestCase("Buff", "enemy_buff",-1.75f,1.5f,12f)]
    [TestCase("Boss", "",-.75f,2f,18f)]
    public void AllDirectionsAnimateBothLegs_AndMirroredStopKeepsGroundAnchor(string role,string name,float x,float y,float cycle)
    {
        var prefab=role=="Boss"?Resources.Load<GameObject>("CoreGuardian/Animation/Boss/BossAnimated"):AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Units/"+name+".prefab");
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
