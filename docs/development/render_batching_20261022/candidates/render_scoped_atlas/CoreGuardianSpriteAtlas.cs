using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Render-scoped copies keep animation/cutout/mask consumers on their original
// sprites. Packing never rotates, trims, resizes, repaints, or repacks a source.
public static class CoreGuardianSpriteAtlas
{
    const int Size=2048,Padding=2;
    sealed class Page
    {
        internal Texture2D texture;
        internal int x=Padding,y=Padding,row;
        internal bool Place(int width,int height,out RectInt rect)
        {
            if(x+width+Padding>Size){x=Padding;y+=row+Padding*2;row=0;}
            if(y+height+Padding>Size){rect=default;return false;}
            rect=new RectInt(x,y,width,height);x+=width+Padding*2;row=Mathf.Max(row,height);return true;
        }
    }
    sealed class Entry {internal Page page;internal RectInt rect;internal uint version;}
    struct Swap {internal SpriteRenderer renderer;internal Sprite original,mapped;}
    static readonly List<Page> pages=new List<Page>();
    static readonly Dictionary<Texture2D,Entry> textures=new Dictionary<Texture2D,Entry>();
    static readonly Dictionary<Sprite,Sprite> sprites=new Dictionary<Sprite,Sprite>();
    static readonly HashSet<Sprite> unsupportedGeometry=new HashSet<Sprite>();
    static readonly Dictionary<Camera,List<Swap>> active=new Dictionary<Camera,List<Swap>>();
    public static int PageCount=>pages.Count;
    public static long RuntimeBytes
    {get{long bytes=0;foreach(var page in pages)if(page.texture!=null)bytes+=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(page.texture);return bytes;}}
    public static bool TryMap(Sprite source,out Sprite mapped)
    {
        mapped=null;
        if(source==null||source.packed||unsupportedGeometry.Contains(source)||(SystemInfo.copyTextureSupport&CopyTextureSupport.Basic)==0||SystemInfo.maxTextureSize<Size)return false;
        var texture=source.texture;
        if(texture==null||texture.width+Padding*2>Size||texture.height+Padding*2>Size||texture.filterMode!=FilterMode.Point||texture.mipmapCount!=1)return false;
        if(!SystemInfo.IsFormatSupported(texture.graphicsFormat,UnityEngine.Experimental.Rendering.GraphicsFormatUsage.Sample))return false;
        if(!textures.TryGetValue(texture,out var entry))
        {
            Page selected=null;RectInt region=default;
            foreach(var page in pages)if(page.texture.graphicsFormat==texture.graphicsFormat&&page.Place(texture.width,texture.height,out region)){selected=page;break;}
            if(selected==null)
            {
                var atlas=new Texture2D(Size,Size,texture.graphicsFormat,UnityEngine.Experimental.Rendering.TextureCreationFlags.None)
                {name="CoreGuardian sprite color page",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
                atlas.Apply(false,true);
                selected=new Page{texture=atlas};selected.Place(texture.width,texture.height,out region);pages.Add(selected);
            }
            entry=new Entry{page=selected,rect=region};Copy(texture,entry);textures.Add(texture,entry);
        }
        else if(entry.version!=texture.updateCount)Copy(texture,entry);
        if(sprites.TryGetValue(source,out mapped)&&mapped!=null)return true;
        var rect=source.rect;rect.position+=new Vector2(entry.rect.x,entry.rect.y);
        mapped=Sprite.Create(entry.page.texture,rect,new Vector2(source.pivot.x/source.rect.width,source.pivot.y/source.rect.height),source.pixelsPerUnit,0,SpriteMeshType.FullRect,source.border,false);
        var vertices=source.vertices;var defaultVertices=mapped.vertices;bool same=vertices.Length==defaultVertices.Length;
        if(same)for(int i=0;i<vertices.Length;i++)if(vertices[i]!=defaultVertices[i]){same=false;break;}
        if(!same)
        {
            // Render callbacks can run outside the player loop even during Play.
            // Unity forbids OverrideGeometry there. Retain arbitrary tight meshes
            // rather than expand geometry or mutate animation-owned sprites.
            unsupportedGeometry.Add(source);DestroyOwned(mapped);mapped=null;return false;
        }
        mapped.name=source.name+" (render atlas)";mapped.hideFlags=HideFlags.HideAndDontSave;sprites[source]=mapped;return true;
    }
    static void Copy(Texture2D source,Entry entry)
    {
        var target=entry.page.texture;var r=entry.rect;
        Graphics.CopyTexture(source,0,0,0,0,source.width,source.height,target,0,0,r.x,r.y);
        // Extrude only the original full texture's edge, preserving inner cutouts.
        for(int p=1;p<=Padding;p++)
        {
            Graphics.CopyTexture(source,0,0,0,0,1,source.height,target,0,0,r.x-p,r.y);
            Graphics.CopyTexture(source,0,0,source.width-1,0,1,source.height,target,0,0,r.xMax-1+p,r.y);
            Graphics.CopyTexture(source,0,0,0,0,source.width,1,target,0,0,r.x,r.y-p);
            Graphics.CopyTexture(source,0,0,0,source.height-1,source.width,1,target,0,0,r.x,r.yMax-1+p);
        }
        for(int x=1;x<=Padding;x++)for(int y=1;y<=Padding;y++)
        {
            Graphics.CopyTexture(source,0,0,0,0,1,1,target,0,0,r.x-x,r.y-y);
            Graphics.CopyTexture(source,0,0,source.width-1,0,1,1,target,0,0,r.xMax-1+x,r.y-y);
            Graphics.CopyTexture(source,0,0,0,source.height-1,1,1,target,0,0,r.x-x,r.yMax-1+y);
            Graphics.CopyTexture(source,0,0,source.width-1,source.height-1,1,1,target,0,0,r.xMax-1+x,r.yMax-1+y);
        }
        entry.version=source.updateCount;
    }
    internal static void Begin(Camera camera,List<SpriteRenderer> renderers)
    {
        End(camera);if(camera==null)return;
        if(!active.TryGetValue(camera,out var swaps)){swaps=new List<Swap>();active.Add(camera,swaps);}
        foreach(var renderer in renderers)
        {
            if(renderer==null||!renderer.enabled||!renderer.gameObject.activeInHierarchy||(camera.cullingMask&(1<<renderer.gameObject.layer))==0)continue;
            var original=renderer.sprite;if(!TryMap(original,out var mapped))continue;
            swaps.Add(new Swap{renderer=renderer,original=original,mapped=mapped});renderer.sprite=mapped;
        }
    }
    internal static void End(Camera camera)
    {
        if(camera==null||!active.TryGetValue(camera,out var swaps))return;
        foreach(var swap in swaps)if(swap.renderer!=null&&swap.renderer.sprite==swap.mapped)swap.renderer.sprite=swap.original;
        swaps.Clear();
    }
    public static void Release()
    {
        foreach(var swaps in active.Values)foreach(var swap in swaps)
            if(swap.renderer!=null&&swap.renderer.sprite==swap.mapped)swap.renderer.sprite=swap.original;
        active.Clear();
        foreach(var sprite in sprites.Values)DestroyOwned(sprite);sprites.Clear();unsupportedGeometry.Clear();
        foreach(var page in pages)DestroyOwned(page.texture);pages.Clear();textures.Clear();
    }
    static void DestroyOwned(Object value){if(value==null)return;if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);}
}
