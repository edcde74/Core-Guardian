using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using CoreGuardian;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;

// Disposable manually timed preview. Never subscribes to purchases or installs preview assets.
public static class CaptureUpgradeOptions
{
    static readonly BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
    sealed class Effect
    {
        public GameObject root;
        public Mesh mesh;
        public MeshRenderer renderer;
        public CoreGuardianSelectionOutline union;
        public Material scan;
        public Bounds bounds;
        public readonly List<Vector3> vertices=new List<Vector3>();
        public readonly List<Color> colors=new List<Color>();
        public readonly List<int> triangles=new List<int>();
        public readonly HashSet<Vector2Int> used=new HashSet<Vector2Int>();
    }
    static void Pixel(Effect e,Vector2 position,Color tint,float density,int width=1)
    {
        var pixel=new Vector2Int(Mathf.RoundToInt(position.x*density),Mathf.RoundToInt(position.y*density));
        if(!e.used.Add(pixel))return;
        float x=pixel.x/density,y=pixel.y/density,s=width*.5f/density;int start=e.vertices.Count;
        e.vertices.AddRange(new[]{new Vector3(x-s,y-s),new Vector3(x+s,y-s),new Vector3(x+s,y+s),new Vector3(x-s,y+s)});
        e.colors.AddRange(new[]{tint,tint,tint,tint});e.triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
    }
    static void Shape(Effect e,int option,float age,float density)
    {
        e.vertices.Clear();e.colors.Clear();e.triangles.Clear();e.used.Clear();
        float u=age/.6f;var b=e.bounds;
        if(u>=0&&u<1)
        {
            if(option==0)
            {
                for(int wave=0;wave<2;wave++)
                {
                    float t=(age-wave*.11f)/.43f;if(t<0||t>=1)continue;
                    float radius=Mathf.Lerp(.14f,.4f,t),height=.065f+t*.035f,y=b.min.y+.055f+t*.16f;
                    var gold=new Color(.96f,.78f,.3f,(1-t)*.95f);
                    for(int i=0;i<96;i++)
                    {
                        // Four short breaks keep the rising ellipse distinct from the selection outline.
                        if(i%24<3)continue;float a=i*Mathf.PI*2/96;
                        Pixel(e,new Vector2(b.center.x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*height),gold,density);
                    }
                }
            }
            if(option==2)
            {
                float t=Mathf.Clamp01(age/.48f),alpha=Mathf.Min(1,age/.07f)*Mathf.Clamp01((.6f-age)/.18f);
                for(int i=0;i<6;i++)
                {
                    float side=i%2==0?-1:1,spread=(.14f+.045f*(i/2))*(1-t*.45f);
                    var pos=new Vector2(b.center.x+side*spread,Mathf.Lerp(b.min.y+.14f+(i/2)*.08f,b.max.y+.15f,t));
                    Pixel(e,pos,new Color(1,.8f,.32f,alpha),density,2);
                    Pixel(e,pos+Vector2.down*(2/density),new Color(.8f,.6f,.22f,alpha*.45f),density);
                }
                if(age>.2f)
                {
                    float alpha2=Mathf.Clamp01((age-.2f)/.1f)*alpha;
                    for(int i=-3;i<=3;i++)Pixel(e,new Vector2(b.center.x+i/density,b.max.y+.19f+(3-Mathf.Abs(i))/density),new Color(1,.85f,.4f,alpha2),density);
                }
            }
        }
        e.mesh.Clear();e.mesh.SetVertices(e.vertices);e.mesh.SetColors(e.colors);e.mesh.SetTriangles(e.triangles,0);e.mesh.RecalculateBounds();
    }
    public static async Task<object> Main()
    {
        if(!Application.isPlaying)throw new Exception("Owned Play required");
        var game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();var source=Camera.main;
        bool gameEnabled=game.enabled,moveEnabled=game.player.enabled;var playerPosition=game.player.transform.position;
        game.enabled=false;game.player.enabled=false;
        string dir=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"docs/development/upgrade_feedback_options_20261017");
        var effects=new List<Effect>();GameObject cameraRoot=null;RenderTexture rt=null;Texture2D image=null;Shader shader=null;Material geometry=null;
        var previous=RenderTexture.active;
        try
        {
            game.Diagnostic();game.Place(TowerKind.Single,new Vector2Int(18,15));game.Place(TowerKind.Multi,new Vector2Int(23,15));game.Place(TowerKind.Support,new Vector2Int(28,15));
            game.player.transform.position=game.World(new Vector2(13,15))+Vector3.up*.11f;
            var targets=new[]{game.player.transform,game.TowerView(game.Session.Towers.Single(t=>t.Kind==TowerKind.Single).Id),game.TowerView(game.Session.Towers.Single(t=>t.Kind==TowerKind.Multi).Id),game.TowerView(game.Session.Towers.Single(t=>t.Kind==TowerKind.Support).Id)};
            float density=source.pixelHeight/(2*source.orthographicSize);
            cameraRoot=new GameObject("Temporary Upgrade Options Camera"){hideFlags=HideFlags.DontSave};var camera=cameraRoot.AddComponent<Camera>();camera.CopyFrom(source);camera.enabled=false;
            camera.transform.SetPositionAndRotation(new Vector3(game.World(new Vector2(20.5f,15)).x,game.World(new Vector2(20.5f,15)).y+.5f,source.transform.position.z),source.transform.rotation);
            camera.orthographicSize=115/density;camera.aspect=800/230f;camera.allowHDR=false;camera.allowMSAA=false;
            var data=camera.GetUniversalAdditionalCameraData();data.SetRenderer(-1);data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.None;
            data.volumeLayerMask=source.GetUniversalAdditionalCameraData().volumeLayerMask;
            rt=new RenderTexture(800,230,24,RenderTextureFormat.ARGB32);rt.Create();camera.targetTexture=rt;
            shader=ShaderUtil.CreateShaderAsset(File.ReadAllText(Path.Combine(dir,"Preview.shader")),true);shader.hideFlags=HideFlags.DontSave;
            if(ShaderUtil.ShaderHasError(shader))throw new Exception("Preview shader failed");geometry=new Material(shader){hideFlags=HideFlags.DontSave};
            foreach(var target in targets)
            {
                var art=target.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");
                var body=art.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled&&r.sprite!=null).ToArray();
                var bounds=body[0].bounds;foreach(var part in body)bounds.Encapsulate(part.bounds);
                var e=new Effect{root=new GameObject("Disposable Upgrade Feedback Preview"){hideFlags=HideFlags.DontSave},mesh=new Mesh(),bounds=bounds};
                e.root.AddComponent<MeshFilter>().sharedMesh=e.mesh;e.renderer=e.root.AddComponent<MeshRenderer>();e.renderer.sharedMaterial=geometry;e.renderer.sortingOrder=1850;
                e.union=e.root.AddComponent<CoreGuardianSelectionOutline>();e.union.enabled=false;e.union.Present(target,camera);
                var output=(MeshRenderer)typeof(CoreGuardianSelectionOutline).GetField("output",Hidden).GetValue(e.union);
                e.scan=new Material(shader){hideFlags=HideFlags.DontSave};e.scan.SetFloat("_Mode",1);e.scan.mainTexture=(RenderTexture)typeof(CoreGuardianSelectionOutline).GetField("mask",Hidden).GetValue(e.union);output.sharedMaterial=e.scan;output.sortingOrder=1851;output.enabled=false;
                effects.Add(e);
            }
            image=new Texture2D(800,230,TextureFormat.RGB24,false);
            for(int option=0;option<3;option++)
            {
                string folder=Path.Combine(dir,"frames_"+(option+1));Directory.CreateDirectory(folder);
                for(int frame=0;frame<48;frame++)
                {
                    float age=frame/30f-.25f;
                    for(int i=0;i<effects.Count;i++)
                    {
                        var e=effects[i];Shape(e,option,age,density);
                        var output=(MeshRenderer)typeof(CoreGuardianSelectionOutline).GetField("output",Hidden).GetValue(e.union);
                        output.enabled=option==1&&age>=0&&age<.55f;
                        e.scan.SetFloat("_BandY",Mathf.Lerp(e.bounds.min.y-.06f,e.bounds.max.y+.06f,Mathf.Clamp01(age/.48f)));
                        e.scan.SetFloat("_Opacity",Mathf.Clamp01(age/.06f)*Mathf.Clamp01((.55f-age)/.08f));
                    }
                    camera.Render();if(frame==0)camera.Render();RenderTexture.active=rt;
                    image.ReadPixels(new Rect(0,0,800,230),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,frame.ToString("D3")+".png"),image.EncodeToPNG());
                }
                await Task.Delay(10);
            }
            var centers=effects.Select(e=>camera.WorldToScreenPoint(e.bounds.center)).Select(v=>new {x=v.x,y=v.y}).ToArray();
            var metadata=new {frames=48,fps=30,density,centers,width=800,height=230,conditions="Actual live original player and three tower art on current scene floor; manually timed disposable upgrade effects; diagnostic resources,stationary relocated player,temporary crop camera with same pixels/world and current post; no Overlay HUD; not connected to purchases or normal gameplay"};
            File.WriteAllText(Path.Combine(dir,"capture.json"),Newtonsoft.Json.JsonConvert.SerializeObject(metadata,Newtonsoft.Json.Formatting.Indented));return metadata;
        }
        finally
        {
            RenderTexture.active=previous;
            foreach(var e in effects){UnityEngine.Object.DestroyImmediate(e.root);UnityEngine.Object.DestroyImmediate(e.mesh);UnityEngine.Object.DestroyImmediate(e.scan);}
            if(cameraRoot!=null)UnityEngine.Object.DestroyImmediate(cameraRoot);if(image!=null)UnityEngine.Object.DestroyImmediate(image);
            if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(geometry!=null)UnityEngine.Object.DestroyImmediate(geometry);if(shader!=null)UnityEngine.Object.DestroyImmediate(shader);
            game.player.transform.position=playerPosition;game.player.enabled=moveEnabled;game.enabled=gameEnabled;
        }
    }
}
