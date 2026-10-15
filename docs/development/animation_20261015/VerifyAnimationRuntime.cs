using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using CoreGuardian;
using Newtonsoft.Json;

public static class VerifyAnimationRuntime
{
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    public static object Begin()
    {
        Require(Application.isPlaying,"Play required");Time.timeScale=0;
        var game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();Require(game!=null&&game.Ready,"Scene not ready");
        var s=game.Session;s.StartDiagnosticEncounter();game.player.transform.position=game.World(new Vector2(15,16));
        Require(game.Place(TowerKind.Single,new Vector2Int(14,16)),"Single install");
        Require(game.Place(TowerKind.Multi,new Vector2Int(17,16)),"Multi install");
        Require(game.Place(TowerKind.Support,new Vector2Int(14,15)),"Support install");
        s.StartCombat();
        s.DebugSpawn(EnemyKind.Attack,21);s.DebugSpawn(EnemyKind.Defense,22);s.DebugSpawn(EnemyKind.Buff,23);s.DebugSpawn(EnemyKind.Boss,24);
        int launches=0;s.ProjectileLaunched+=shot=>launches++;
        s.Tick(.05f,new Vector2(15,16));
        typeof(CoreGuardianPrototype).GetMethod("SynchronizeViews",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(game,null);
        Require(launches>=2,"Actual player/tower launch events missing");
        game.Notify("애니메이션 기능/화면 검사 · 진단 재화·적 생성 사용 · 밸런스 실측 아님");
        return JsonConvert.SerializeObject(new{success=true,launches,mode="Visual diagnostic only; injected encounter; not balance evidence",actors=UnityEngine.Object.FindObjectsByType<CoreGuardianUnitAnimation>().Select(a=>new{a.name,a.Loaded,a.Direction,a.ShotCount,muzzle=new[]{a.MuzzleWorld.x,a.MuzzleWorld.y}})});
    }
    public static object Check()
    {
        var actors=UnityEngine.Object.FindObjectsByType<CoreGuardianUnitAnimation>();
        var results=new List<object>();
        foreach(var actor in actors)
        {
            Require(actor.Loaded,"Atlas missing: "+actor.name);
            var art=actor.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="2D Sprite");
            bool player=actor.name.Contains("Player"),enemy=actor.name.StartsWith("Enemy_");
            int count=player?8:4;
            var frames=new List<string>();
            if(enemy)
            {
                var dirs=new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down};
                for(int d=0;d<4;d++)
                {
                    actor.Move(dirs[d],.3f);actor.Present(0);Require(actor.Direction==d,"Wrong enemy direction: "+actor.name);
                    Require(art.sprite.name.Contains("_"+d+"_"),"Wrong direction sprite");frames.Add(art.sprite.name);
                }
                var names=new HashSet<string>();
                for(int i=0;i<4;i++){actor.Move(Vector2.right,.15f);actor.Present(0);names.Add(art.sprite.name);}
                Require(names.Count==4,"Walk must contain four distinct phase sprites");
                Require(art.sprite.pivot==new Vector2(48,8),"Foot anchor changed");
            }
            if(player)
            {
                for(int d=0;d<8;d++)
                {
                    float angle=d*Mathf.PI/4;actor.Fire(new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)),1);
                    Require(actor.Direction==d,"Wrong 8-way attack direction");
                    Require(art.sprite.name=="Player_"+d+"_1","Attack frame not synchronized");frames.Add(art.sprite.name);
                }
                actor.Fire(Vector2.up,8);actor.Present(.15f);Require(art.sprite.name.EndsWith("_1"),"Launch pose skipped before first render");
                actor.Present(.11f);Require(art.sprite.name.EndsWith("_0"),"High-speed recoil did not finish before next shot");
            }
            Require(art.transform.localPosition==Vector3.zero,"Body foot offset");
            Require(art.transform.localRotation==Quaternion.identity,"Body must not rotate flat off the ground");
            Require(actor.transform.rotation==Quaternion.identity,"Root/shadows rotated");
            if(actor.name.StartsWith("Tower_"))Require(art.sprite.name=="Base"&&art.transform.Find("AnimatedUpper")!=null,"Stationary tower base and separate turret missing");
            results.Add(new{actor.name,directions=player||enemy?count:0,frames});
        }
        return JsonConvert.SerializeObject(new{success=true,results});
    }
    public static object Restore(){Time.timeScale=1;return "timeScale restored";}
}
