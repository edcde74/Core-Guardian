using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;

public static class CompareOriginalArt
{
    const string Output="docs/development/original_art_restore_20261015/";
    static Texture2D Read(string path){var t=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(t,File.ReadAllBytes(path));return t;}
    public static object Compare()
    {
        Directory.CreateDirectory(Output);
        string[] names={"player","tower_single","tower_multi","tower_support"};
        string[] roles={"Player","Single","Multi","Support"};
        var board=new Texture2D(96*2*5,112*4*5,TextureFormat.RGBA32,false);
        var bg=new Color32(35,40,46,255);board.SetPixels32(Enumerable.Repeat(bg,board.width*board.height).ToArray());
        for(int row=0;row<4;row++)
        {
            var original=Read("Assets/Art/ScreenTests/20260930/Sprites/"+names[row]+".png");
            var atlas=Read("Assets/Art/RejectedAnimations/20261015/"+roles[row]+"/atlas.png");
            for(int y=0;y<112;y++)for(int x=0;x<96;x++)
            {
                int sx=x-48+original.width/2,sy=y-8;
                var a=sx>=0&&sx<original.width&&sy>=0&&sy<original.height?original.GetPixel(sx,sy):Color.clear;
                // Closest matching facing: player southwest, gun towers northwest; support front.
                var b=atlas.GetPixel(x,row<3?224+y:y);
                if(row>0){var foundation=atlas.GetPixel(x,448+y);if(foundation.a>.5f)b=foundation;}
                for(int iy=0;iy<5;iy++)for(int ix=0;ix<5;ix++)
                {
                    int py=((3-row)*112+y)*5+iy;
                    if(a.a>.5f)board.SetPixel(x*5+ix,py,a);
                    if(b.a>.5f)board.SetPixel((96+x)*5+ix,py,b);
                }
            }
            UnityEngine.Object.DestroyImmediate(original);UnityEngine.Object.DestroyImmediate(atlas);
        }
        board.Apply();File.WriteAllBytes(Output+"original_vs_replaced_5x.png",board.EncodeToPNG());UnityEngine.Object.DestroyImmediate(board);
        return "left=existing original, right=rejected replacement; rows player/single/multi/support; same nearest5x pixel scale";
    }
    public static object Verify()
    {
        if(!Application.isPlaying)throw new Exception("Play required");
        var game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
        var player=game.player.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");
        var expected=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ScreenTests/20260930/Sprites/player.png");
        if(player.sprite!=expected)throw new Exception("Player original replaced: "+player.sprite.name);
        var checks=new System.Collections.Generic.List<object>();
        for(int i=0;i<game.towerArt.Length;i++)
        {
            var clone=UnityEngine.Object.Instantiate(game.towerArt[i]);
            try
            {
                var art=clone.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");
                var sprite=art.sprite;var pos=art.transform.localPosition;var rotation=art.transform.localRotation;var scale=art.transform.localScale;
                var shadows=clone.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name!="2D Sprite").ToArray();var enabled=shadows.Select(r=>r.enabled).ToArray();
                var animation=CoreGuardianUnitAnimation.Attach(clone,((CoreGuardian.TowerKind)i).ToString());
                animation.Aim(Vector2.right,1);animation.Fire(Vector2.left,8);animation.Present(.15f);
                if(art.sprite!=sprite||art.transform.localPosition!=pos||art.transform.localRotation!=rotation||art.transform.localScale!=scale||shadows.Where((r,j)=>r.enabled!=enabled[j]).Any()||clone.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="AnimatedUpper"))throw new Exception("Tower original altered: "+clone.name);
                checks.Add(new{clone.name,sprite=sprite.name,originalPreserved=true});
            }
            finally{UnityEngine.Object.Destroy(clone);}
        }
        return JsonConvert.SerializeObject(new{success=true,player=AssetDatabase.GetAssetPath(player.sprite),towers=checks});
    }
    public static object Showcase()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
        if(!Application.isPlaying||!g.Ready)throw new Exception("Ready Play required");
        Time.timeScale=0;g.Diagnostic();
        g.player.transform.position=g.World(new Vector2(12,15));
        if(!g.Place(CoreGuardian.TowerKind.Single,new Vector2Int(14,15))||!g.Place(CoreGuardian.TowerKind.Multi,new Vector2Int(17,15))||!g.Place(CoreGuardian.TowerKind.Support,new Vector2Int(20,15)))throw new Exception("Install failed");
        var camera=Camera.main;camera.orthographicSize=1.8f;camera.transform.position=g.World(new Vector2(16,15))+Vector3.up*.5f+Vector3.back*10;
        return Verify();
    }
}
