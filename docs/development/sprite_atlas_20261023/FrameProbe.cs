using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CoreGuardian;
using Newtonsoft.Json;

public static class CGSpriteAtlasProbe
{
    static CoreGuardianPrototype game;
    static string output;
    static int last,warm,count;
    static readonly List<object> frames=new List<object>();
    public static object Start(string path)
    {
        if(!EditorApplication.isPlaying||UnityEngine.Profiling.Profiler.enabled)throw new Exception("Own non-recording Play required");
        output=path;frames.Clear();count=0;warm=15;
        game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();game.Diagnostic();var s=game.Session;
        for(int i=0;i<128;i++){var kind=i%8==0?EnemyKind.Buff:EnemyKind.Attack;var spec=s.Data.Enemies[kind];s.DebugSpawn(kind,20+(i%12)*.3f,new EnemySpec{Kind=kind,Health=100000,Speed=spec.Speed,Defense=spec.Defense,Gold=0,CoreDamage=0,BuffRadius=spec.BuffRadius,BuffSpeed=spec.BuffSpeed});}
        var size=game.BoardSize;var p=s.Enemies[0].Position;
        var cells=Enumerable.Range(0,size.x).SelectMany(x=>Enumerable.Range(0,size.y).Select(y=>new Vector2Int(x,y))).Where(s.IsBuildable).OrderBy(c=>((Vector2)c-p).sqrMagnitude).ToArray();
        for(int i=0;i<Math.Min(32,cells.Length);i++)game.Place(i%5==4?TowerKind.Support:TowerKind.Multi,cells[i]);
        game.StartCombat();
        // Suppress only the diagnostic's three weak starter enemies in this transient Session.
        typeof(Session).GetField("waveTime",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(s,.0001f);
        last=Time.frameCount;EditorApplication.update+=Poll;
        return new{started=true,enemies=s.Enemies.Count,towers=s.Towers.Count};
    }
    static void Poll()
    {
        try
        {
            if(!EditorApplication.isPlaying)throw new Exception("Play stopped externally");
            if(Time.frameCount==last)return;last=Time.frameCount;if(warm-->0)return;
            frames.Add(new{frame=last,ms=Time.unscaledDeltaTime*1000d,enemies=game.Session.Enemies.Count,towers=game.Session.Towers.Count,draw=UnityEditor.UnityStats.drawCalls,setpass=UnityEditor.UnityStats.setPassCalls});
            if(++count<90)return;Finish(null);
        }
        catch(Exception e){Finish(e.ToString());}
    }
    static void Finish(string error)
    {
        EditorApplication.update-=Poll;
        var textures=Resources.FindObjectsOfTypeAll<Texture2D>();
        File.WriteAllText(output,JsonConvert.SerializeObject(new{error,count,textureBytes=textures.Sum(t=>UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t)),frames},Formatting.Indented));
    }
}
