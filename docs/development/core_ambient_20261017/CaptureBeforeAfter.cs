using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public static class CaptureBeforeAfter
{
    public static async Task<object> Main()
    {
        if(!Application.isPlaying)throw new Exception("Running Play required");
        const string dir="docs/development/core_ambient_20261017/before_after/";
        Directory.CreateDirectory(dir+"before");Directory.CreateDirectory(dir+"after");
        var cam=Camera.main;
        var core=UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Single(r=>r.sprite!=null&&r.sprite.name=="core"&&r.name=="2D Sprite");
        var ambient=core.transform.Find("CoreAmbient_Local");
        if(ambient==null)throw new Exception("Applied core ambient root missing");
        var renderers=ambient.GetComponentsInChildren<Renderer>();
        var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();
        var target=cam.targetTexture;var active=RenderTexture.active;
        var point=cam.WorldToViewportPoint(core.bounds.center+Vector3.up*.15f);
        int x=Mathf.Clamp(Mathf.RoundToInt(point.x*1920)-160,0,1600),y=Mathf.Clamp(Mathf.RoundToInt(point.y*1080)-160,0,760);
        var times=new System.Collections.Generic.List<float>();
        try
        {
            await Task.Delay(700);
            for(int i=0;i<32;i++)
            {
                await Task.Delay(80);
                if(!Application.isPlaying||core==null)throw new Exception("Play ended during capture");
                var enabled=renderers.Select(r=>r.enabled).ToArray();target=cam.targetTexture;active=RenderTexture.active;
                try
                {
                    cam.targetTexture=rt;
                    for(int side=0;side<2;side++)
                    {
                        for(int j=0;j<renderers.Length;j++)renderers[j].enabled=side==1&&enabled[j];
                        cam.Render();RenderTexture.active=rt;
                        var tex=new Texture2D(320,320,TextureFormat.RGB24,false);
                        try{tex.ReadPixels(new Rect(x,y,320,320),0,0);tex.Apply();File.WriteAllBytes(dir+(side==0?"before/":"after/")+i.ToString("D3")+".png",tex.EncodeToPNG());}
                        finally{UnityEngine.Object.Destroy(tex);}
                    }
                    times.Add(Time.time);
                }
                finally
                {
                    for(int j=0;j<renderers.Length;j++)if(renderers[j]!=null)renderers[j].enabled=enabled[j];
                    cam.targetTexture=target;RenderTexture.active=active;
                }
            }
        }
        finally{if(cam!=null)cam.targetTexture=target;RenderTexture.active=active;rt.Release();UnityEngine.Object.Destroy(rt);}
        var result=new{source="Same-frame off/on of new ambient renderers only; original core/floor glow/shadows preserved",cropX=x,cropY=y,width=320,height=320,sourceWidth=1920,sourceHeight=1080,temporaryZoom=false,timeScale=Time.timeScale,times};
        File.WriteAllText(dir+"capture.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
