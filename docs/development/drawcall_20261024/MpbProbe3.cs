return CGB19.MpbProbe.Run();
}
}
namespace CGB19 {
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
public static class MpbProbe
{
    static Color Px(Texture2D t,int x,int y)=>t.GetPixel(x,y);
    public static object Run()
    {
        var made=new List<UnityEngine.Object>();var results=new List<string>();
        bool srpBefore=GraphicsSettings.useScriptableRenderPipelineBatching;
        var urp=GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;bool urpBefore=urp.useSRPBatcher;
        try
        {
            var tex=new Texture2D(16,16){filterMode=FilterMode.Point,hideFlags=HideFlags.HideAndDontSave};var px=new Color[256];for(int i=0;i<256;i++)px[i]=new Color(.2f,.4f,.6f,1);tex.SetPixels(px);tex.Apply();made.Add(tex);
            var sprite=Sprite.Create(tex,new Rect(0,0,16,16),new Vector2(.5f,.5f),16);sprite.hideFlags=HideFlags.HideAndDontSave;made.Add(sprite);
            var hit=new Material(Resources.Load<Shader>("CoreGuardian/BodyHitFlash")){hideFlags=HideFlags.HideAndDontSave};made.Add(hit);
            var unlit=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")){hideFlags=HideFlags.HideAndDontSave};made.Add(unlit);
            int layer=31;
            SpriteRenderer Make(string n,float x,Material m,Color c,float amount)
            {
                var g=new GameObject(n){hideFlags=HideFlags.HideAndDontSave,layer=layer};made.Add(g);g.transform.position=new Vector3(x,1000,0);
                var r=g.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=m;r.color=c;
                if(amount>=0){var b=new MaterialPropertyBlock();b.SetFloat("_HitFlashAmount",amount);r.SetPropertyBlock(b);}
                return r;
            }
            Make("plainA",-3,unlit,Color.white,-1);Make("hitA",-3,hit,Color.white,1);Make("hitB",-1,hit,Color.white,1);Make("plainB",-1,unlit,Color.white,-1);Make("hdr",1,unlit,new Color(1.5f,1.5f,1.5f,1),-1);Make("plain",3,unlit,Color.white,-1);
            var cg=new GameObject("cam"){hideFlags=HideFlags.HideAndDontSave};made.Add(cg);cg.transform.position=new Vector3(0,1000,-10);
            var cam=cg.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=2;cam.cullingMask=1<<layer;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.black;cam.enabled=false;
            var rt=new RenderTexture(256,128,24,RenderTextureFormat.ARGB32){hideFlags=HideFlags.HideAndDontSave};made.Add(rt);cam.targetTexture=rt;
            foreach(bool srp in new[]{true,false})
            {
                urp.useSRPBatcher=srp;GraphicsSettings.useScriptableRenderPipelineBatching=srp;
                cam.Render();
                var prev=RenderTexture.active;RenderTexture.active=rt;var read=new Texture2D(256,128,TextureFormat.RGBA32,false);read.ReadPixels(new Rect(0,0,256,128),0,0);read.Apply();RenderTexture.active=prev;
                // world x -3,-1,1,3 at ortho 2 aspect 2 -> width 8 units => px = (x+4)*32
                results.Add("srp="+srp+" hitA="+Px(read,32,64)+" hitB="+Px(read,96,64)+" hdr="+Px(read,160,64)+" plain="+Px(read,224,64));
                UnityEngine.Object.DestroyImmediate(read);
            }
        }
        finally
        {
            urp.useSRPBatcher=urpBefore;GraphicsSettings.useScriptableRenderPipelineBatching=srpBefore;
            for(int i=made.Count-1;i>=0;i--)if(made[i]!=null)UnityEngine.Object.DestroyImmediate(made[i]);
        }
        return string.Join("\n",results);
    }
}
}
public static class CGTailPad19{static object P(){