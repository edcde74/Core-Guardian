using System;
using System.Linq;
using UnityEngine;
using CoreGuardian;
using Newtonsoft.Json;
public static class VerifyBossArt_20261013
{
    static CoreGuardianPrototype Game()=>UnityEngine.Object.FindObjectsByType<CoreGuardianPrototype>().Single();
    static void Require(bool v,string m){if(!v)throw new Exception(m);}
    public static object Begin()
    {
        Require(Application.isPlaying,"Play required");Time.timeScale=0;
        var g=Game();var s=g.Session;Require(s!=null&&s.Wave==1,"Fresh session required");
        for(int wave=1;wave<=9;wave++){s.StartCombat();for(int i=0;i<1000&&s.Phase==SessionPhase.Combat;i++){s.Tick(.05f,new Vector2(100,100));foreach(var e in s.Enemies.ToArray())s.ApplyDamage(e,10000,"ArtDiagnostic");}Require(s.Phase==SessionPhase.Preparation,"Round failed");}
        s.StartCombat();s.Tick(.05f,new Vector2(100,100));var boss=s.Enemies.Single(e=>e.Kind==EnemyKind.Boss);
        boss.Position=new Vector2(8,12);g.Notify("보스 아트 검증 · 강제 라운드 이동, 밸런스 실측 아님");
        return JsonConvert.SerializeObject(new{success=true,boss.Health,boss.Enraged,wave=s.Wave});
    }
    public static object Check()
    {
        var g=Game();var boss=g.Session.Enemies.Single(e=>e.Kind==EnemyKind.Boss);
        var view=GameObject.Find("Enemy_Boss_"+boss.Id);Require(view!=null,"Boss view missing");
        var art=view.transform.Find("2D Sprite").GetComponent<SpriteRenderer>();
        var expected=Resources.Load<Sprite>("CoreGuardian/Art/BossEye/BossEye");
        Require(expected!=null&&art.sprite==expected,"Boss still uses soldier sprite instead of approved eye-glow sprite");
        Require(art.color==Color.white,"Approved boss colors are tinted");
        Require(Mathf.Abs(art.transform.lossyScale.x-.6f)<.0001f,"Boss sprite scaled twice");
        Require(Vector3.Distance(view.transform.position,g.World(boss.Position))<.001f,"Foot root differs from gameplay position");
        var bar=view.transform.Find("Health");Require(bar.position.y>art.bounds.max.y,"Health bar overlaps boss head");
        Require(view.GetComponentsInChildren<SpriteRenderer>().Count(r=>r.name.Contains("그림자")||r.name=="바닥 밀착선")>=3,"Boss shadows missing");
        return JsonConvert.SerializeObject(new{success=true,boss.Health,boss.Enraged,boss.MovementSpeed,art=art.sprite.name,color="white",scale=art.transform.lossyScale.x,barClear=true,footAligned=true});
    }
    public static object Enrage()
    {
        var g=Game();var boss=g.Session.Enemies.Single(e=>e.Kind==EnemyKind.Boss);g.Session.ApplyDamage(boss,150,"ArtDiagnostic");
        Require(boss.Enraged&&Mathf.Abs(boss.MovementSpeed-.78f)<.001f,"Enrage mechanics changed");return "Enraged at HP150; wait one editor frame then Check";
    }
    public static object Resume(){Time.timeScale=1;return "timeScale restored";}
}
