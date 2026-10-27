using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

// 2026-10-27 사용자 Play 피드백 1·2·3: 신규 포탑 공격 프레임 몸체 고정, 화염·레일 포신 조준.
public class CoreGuardianNewTowerAttackFrameTests
{
    const string Towers="Assets/Art/Characters/Towers/";
    static Type Find(string name)=>TypeCache.GetTypesDerivedFrom<object>().First(t=>t.Name==name);
    static Color32[] Load(string path,out int w,out int h)
    {
        var t=new Texture2D(2,2);ImageConversion.LoadImage(t,File.ReadAllBytes(path));w=t.width;h=t.height;
        var c=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);return c;
    }
    static int Matches(Color32[] idle,int iw,int ih,Color32[] frame,int fw,int fh,int dx,int dy)
    {
        int s=0;
        for(int y=0;y<ih;y++)for(int x=0;x<iw;x++)
        {
            var a=idle[y*iw+x];if(a.a<128)continue;int fx=x+dx,fy=y+dy;if(fx<0||fy<0||fx>=fw||fy>=fh)continue;
            var q=frame[fy*fw+fx];if(q.a>=128&&Math.Abs(a.r-q.r)+Math.Abs(a.g-q.g)+Math.Abs(a.b-q.b)<24)s++;
        }
        return s;
    }

    [TestCase("Frost","tower_frost","Pulse_5")]
    [TestCase("Flame","tower_flame","Fire_5")]
    [TestCase("Rail","tower_rail","Fire_0")]
    public void AttackFrameOffset_IsTheBestPixelMatchWithIdleSprite(string role,string idleName,string frameName)
    {
        var offset=(Vector2Int)Find("CoreGuardianOriginalMotion").GetMethod("AttackFrameOffset",BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public).Invoke(null,new object[]{role});
        var idle=Load(Towers+"Sprites/"+idleName+".png",out int iw,out int ih);
        var frame=Load(Towers+"Resources/CoreGuardian/OriginalMotion/"+role+"/"+frameName+".png",out int fw,out int fh);
        int opaque=idle.Count(c=>c.a>=128),best=-1;Vector2Int bestAt=default;
        for(int dx=-30;dx<=30;dx++)for(int dy=-10;dy<=10;dy++){int m=Matches(idle,iw,ih,frame,fw,fh,dx,dy);if(m>best){best=m;bestAt=new Vector2Int(dx,dy);}}
        Assert.That(offset,Is.EqualTo(bestAt),"Attack frame body must land where the idle body is");
        Assert.That(best,Is.GreaterThan(opaque*.98f));
    }

    [Test]
    public void FrostAttackFrame_KeepsBodyOnIdlePivot()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Units/Resources/CoreGuardian/Units/tower_frost.prefab");
        var root=UnityEngine.Object.Instantiate(prefab);
        try
        {
            var type=Find("CoreGuardianUnitAnimation");
            var body=root.GetComponentsInChildren<SpriteRenderer>().Single(r=>r.name=="2D Sprite");var idle=body.sprite;
            var unit=(Component)type.GetMethod("Attach").Invoke(null,new object[]{root,"Frost"});
            type.GetMethod("Fire").Invoke(unit,new object[]{Vector2.left,1f});
            type.GetMethod("Present").Invoke(unit,new object[]{0f});
            Assert.That(body.sprite,Is.Not.SameAs(idle));
            Assert.That(Vector2.Distance(body.sprite.pivot,idle.pivot),Is.LessThan(.01f),"Frame pixel matching the idle pivot sits on the transform");
            type.GetMethod("Present").Invoke(unit,new object[]{2f});
            Assert.That(body.sprite,Is.SameAs(idle));
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }

    [TestCase("Flame","tower_flame",22f,51f,1f,0f)] [TestCase("Flame","tower_flame",22f,51f,-1f,0f)]
    [TestCase("Flame","tower_flame",22f,51f,0f,1f)] [TestCase("Flame","tower_flame",22f,51f,1f,-1f)]
    [TestCase("Rail","tower_rail",26f,47f,1f,0f)] [TestCase("Rail","tower_rail",26f,47f,-1f,0f)]
    [TestCase("Rail","tower_rail",26f,47f,0f,-1f)] [TestCase("Rail","tower_rail",26f,47f,-1f,1f)]
    public void FlameAndRailBarrel_FollowTarget_ThroughAttackFrames(string role,string prefabName,float mountX,float mountY,float x,float y)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Units/Resources/CoreGuardian/Units/"+prefabName+".prefab");
        var root=UnityEngine.Object.Instantiate(prefab);
        try
        {
            var type=Find("CoreGuardianUnitAnimation");
            var body=root.GetComponentsInChildren<SpriteRenderer>().Single(r=>r.name=="2D Sprite");var idle=body.sprite;
            var unit=(Component)type.GetMethod("Attach").Invoke(null,new object[]{root,role});
            var direction=new Vector2(x,y).normalized;
            type.GetMethod("Aim").Invoke(unit,new object[]{direction,1f});
            type.GetMethod("Present").Invoke(unit,new object[]{0f});
            var rig=body.transform.Find("OriginalMotion");
            var barrel=rig.GetComponentsInChildren<SpriteRenderer>(true).Single(r=>r.name=="TurretBarrel");
            Assert.That(barrel.enabled&&barrel.gameObject.activeInHierarchy,Is.True);Assert.That(body.enabled,Is.False);
            var mount=body.transform.TransformPoint(new Vector3((mountX-26f)/32f,mountY/32f));
            Vector3 Muzzle()=>(Vector3)type.GetProperty("MuzzleWorld").GetValue(unit);
            Assert.That(Vector2.Dot(((Vector2)(Muzzle()-mount)).normalized,direction),Is.GreaterThan(.999f),"Barrel tip must face the target");
            var idleBarrel=barrel.sprite;var aimed=Muzzle();
            type.GetMethod("Fire").Invoke(unit,new object[]{direction,1f});
            type.GetMethod("Present").Invoke(unit,new object[]{0f});
            type.GetMethod("Present").Invoke(unit,new object[]{.08f});
            Assert.That(barrel.sprite,Is.Not.SameAs(idleBarrel),"Approved attack frame pixels are shown on the barrel");
            Assert.That(Vector2.Dot(((Vector2)(Muzzle()-mount)).normalized,direction),Is.GreaterThan(.995f),"Attack frames keep the aim");
            type.GetMethod("Present").Invoke(unit,new object[]{2f});
            Assert.That(barrel.sprite,Is.SameAs(idleBarrel));
            Assert.That(Vector3.Distance(Muzzle(),aimed),Is.LessThan(.001f),"Recovery keeps the aimed barrel");
            Assert.That(body.sprite,Is.SameAs(idle));
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
