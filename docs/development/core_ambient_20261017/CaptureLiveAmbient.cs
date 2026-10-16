using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public static class CaptureLiveAmbient
{
    public static async Task<object> Main()
    {
        if(!Application.isPlaying)throw new Exception("Own running Play required");
        const string dir="docs/development/core_ambient_20261017/";Directory.CreateDirectory(dir+"frames");
        var cam=Camera.main;var core=UnityEngine.Object.FindObjectsByType<SpriteRenderer>().First(r=>r.sprite!=null&&r.sprite.name=="core"&&r.name=="2D Sprite");
        var particles=core.GetComponentsInChildren<ParticleSystem>().Single();
        var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();
        var samples=new System.Collections.Generic.List<object>();
        var previous=RenderTexture.active;var oldTarget=cam.targetTexture;Texture2D tex=null;
        var point=cam.WorldToViewportPoint(core.bounds.center+Vector3.up*.25f);
        int x=Mathf.Clamp(Mathf.RoundToInt(point.x*1920)-200,0,1520),y=Mathf.Clamp(Mathf.RoundToInt(point.y*1080)-200,0,680);
        try
        {
            for(int i=0;i<32;i++)
            {
                await Task.Delay(80);
                if(!Application.isPlaying||cam==null||core==null)throw new Exception("Play ended during capture");
                oldTarget=cam.targetTexture;previous=RenderTexture.active;
                try
                {
                    cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
                    tex=new Texture2D(400,400,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(x,y,400,400),0,0);tex.Apply();
                    File.WriteAllBytes(dir+"frames/"+i.ToString("D3")+".png",tex.EncodeToPNG());
                    UnityEngine.Object.Destroy(tex);tex=null;
                }
                finally{cam.targetTexture=oldTarget;RenderTexture.active=previous;}
                samples.Add(new{frame=i,time=Time.time,count=particles.particleCount});
            }
        }
        finally
        {
            if(cam!=null)cam.targetTexture=oldTarget;RenderTexture.active=previous;
            if(tex!=null)UnityEngine.Object.Destroy(tex);rt.Release();UnityEngine.Object.Destroy(rt);
        }
        var result=new{source="Actual running scene camera crop; no temporary zoom; no HUD in camera-only capture",width=400,height=400,cropX=x,cropY=y,sourceWidth=1920,sourceHeight=1080,timeScale=Time.timeScale,samples};
        File.WriteAllText(dir+"animation_capture.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
