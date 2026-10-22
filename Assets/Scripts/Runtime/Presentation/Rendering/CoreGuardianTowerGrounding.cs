using System;
using UnityEngine;

// Presentation only: contact follows the approved base, never the logical tile center.
public sealed class CoreGuardianTowerGrounding : IDisposable
{
    readonly SpriteRenderer body;
    readonly MeshRenderer shade;
    readonly Mesh mesh;
    readonly Material material;
    readonly GameObject castRoot;

    public CoreGuardianTowerGrounding(SpriteRenderer artwork,string role)
    {
        body=artwork;
        var sprite=body.sprite;
        float pixel=body.transform.localScale.y/sprite.pixelsPerUnit;
        float basePixels=role=="Single"?39:role=="Multi"?38:40;
        float frontCenter=role=="Single"?33.5f:role=="Multi"?25.5f:23f;
        var center=body.transform.localPosition+new Vector3((frontCenter-sprite.pivot.x)*body.transform.localScale.x/sprite.pixelsPerUnit,-sprite.pivot.y*pixel);
        float width=basePixels*body.transform.localScale.x/sprite.pixelsPerUnit;
        var root=body.transform.parent.Find("그림자");
        if(root!=null)
        {
            SetShadow(root.Find("바깥 그림자")?.GetComponent<SpriteRenderer>(),center+Vector3.up*(3*pixel),width+4*pixel,11*pixel,new Color(.10f,.085f,.07f,.20f));
            var contact=root.Find("접촉 그림자")?.GetComponent<SpriteRenderer>();
            SetShadow(contact,center+Vector3.up*pixel,width*.82f,4*pixel,new Color(.025f,.022f,.018f,.82f));
            var old=root.Find("바닥 밀착선");if(old!=null)old.gameObject.SetActive(false);
            if(contact!=null)
            {
                castRoot=new GameObject("짧은 투사 그림자");castRoot.transform.SetParent(root,false);
                var cast=castRoot.AddComponent<SpriteRenderer>();cast.sprite=contact.sprite;cast.sharedMaterial=contact.sharedMaterial;
                cast.sortingLayerID=contact.sortingLayerID;cast.sortingOrder=contact.sortingOrder-2;
                SetShadow(cast,center+new Vector3(4*pixel,pixel),width,7*pixel,new Color(.09f,.075f,.06f,.30f));
            }
        }
        // A separate alpha-masked overlay keeps original PNGs, cutouts and source materials intact.
        var shader=Resources.Load<Shader>("CoreGuardian/TowerBaseShade");
        if(shader==null)throw new InvalidOperationException("Missing tower base shade shader");
        var sourceRect=CoreGuardianSpriteAtlasSources.OriginalRect(sprite,out var sourceTexture);
        material=new Material(shader){name="Tower base shade "+role};material.mainTexture=sourceTexture;
        material.SetFloat("_BottomUV",sourceRect.yMin/sourceTexture.height);
        material.SetFloat("_TextureHeight",sourceTexture.height);
        var vertices=sprite.vertices;var v=new Vector3[vertices.Length];for(int i=0;i<v.Length;i++)v[i]=vertices[i];
        var indices=sprite.triangles;var triangles=new int[indices.Length];for(int i=0;i<triangles.Length;i++)triangles[i]=indices[i];
        var uv=sprite.uv;
        if(sourceTexture!=sprite.texture&&CoreGuardianSpriteAtlasSources.TryGetSource(sprite,out _,out var transform))
            for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(Mathf.Round((uv[i].x*transform.x+transform.z)*sourceTexture.width)/sourceTexture.width,Mathf.Round((uv[i].y*transform.y+transform.w)*sourceTexture.height)/sourceTexture.height);
        mesh=new Mesh{name="Original base shade mask"};mesh.vertices=v;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateBounds();
        var go=new GameObject("TowerBaseShade");go.transform.SetParent(body.transform.parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
        shade=go.AddComponent<MeshRenderer>();shade.sharedMaterial=material;
        shade.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;shade.receiveShadows=false;
        Present();
    }
    static void SetShadow(SpriteRenderer renderer,Vector3 position,float width,float height,Color color)
    {
        if(renderer==null||renderer.sprite==null)return;
        renderer.transform.localPosition=position;
        renderer.transform.localScale=new Vector3(width/renderer.sprite.bounds.size.x,height/renderer.sprite.bounds.size.y,1);
        renderer.color=color;
    }
    public void Present()
    {
        if(body==null||shade==null)return;
        shade.transform.localPosition=body.transform.localPosition;shade.transform.localRotation=body.transform.localRotation;shade.transform.localScale=body.transform.localScale;
        shade.sortingLayerID=body.sortingLayerID;shade.sortingOrder=body.sortingOrder+2;
    }
    public void Dispose(){Destroy(shade!=null?shade.gameObject:null);Destroy(castRoot);Destroy(mesh);Destroy(material);}
    static void Destroy(UnityEngine.Object value){if(value==null)return;if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
}
