using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using Object=UnityEngine.Object;

// Reusable installation tint geometry. Owns neither the board nor the supplied sprite/material.
public sealed class CoreGuardianPlacementOverlay : IDisposable
{
    readonly Transform owner;
    readonly Sprite sprite;
    readonly GameObject view;
    readonly Material material;
    readonly Vector2[] spriteVertices,spriteUV;
    readonly ushort[] spriteTriangles;
    readonly List<Vector3> vertices=new List<Vector3>();
    readonly List<Vector2> uv=new List<Vector2>();
    readonly List<Color> colors=new List<Color>();
    readonly List<Vector3> normals=new List<Vector3>();
    readonly List<int> triangles=new List<int>();
    public Mesh Mesh {get;}
    public MeshRenderer Renderer {get;}
    public int CellCount {get;private set;}

    public CoreGuardianPlacementOverlay(Transform owner,Sprite sprite,Material sourceMaterial)
    {
        if(owner==null||sprite==null||sourceMaterial==null)throw new ArgumentException("Placement owner, sprite and material are required");
        this.owner=owner;this.sprite=sprite;spriteVertices=sprite.vertices;spriteUV=sprite.uv;spriteTriangles=sprite.triangles;
        view=new GameObject("InstallationOverlay");view.transform.SetParent(owner,false);view.SetActive(false);
        Mesh=new Mesh{name="Installation overlay mesh"};Mesh.MarkDynamic();
        view.AddComponent<MeshFilter>().sharedMesh=Mesh;
        Renderer=view.AddComponent<MeshRenderer>();Renderer.sortingOrder=3;
        Renderer.shadowCastingMode=ShadowCastingMode.Off;Renderer.receiveShadows=false;
        material=new Material(sourceMaterial){name="Installation overlay material"};material.mainTexture=sprite.texture;Renderer.sharedMaterial=material;
        var properties=new MaterialPropertyBlock();properties.SetVector("unity_SpriteProps",new Vector4(1,1,0,0));
        properties.SetColor("unity_SpriteColor",Color.white);properties.SetColor("_RendererColor",Color.white);Renderer.SetPropertyBlock(properties);
    }
    public void Show(Tilemap ground,Func<Vector2Int,bool> canInstall)
    {
        if(view==null)throw new ObjectDisposedException(nameof(CoreGuardianPlacementOverlay));
        if(ground==null||canInstall==null)throw new ArgumentNullException();
        vertices.Clear();uv.Clear();colors.Clear();normals.Clear();triangles.Clear();CellCount=0;
        var size=ground.layoutGrid.cellSize;var scale=new Vector3(size.x*(.55f/.6f),size.y*(.55f/.6f),1);
        var localRotation=Quaternion.Inverse(owner.rotation);
        var green=new Color(.1f,1f,.35f,.13f);var red=new Color(1f,.15f,.1f,.2f);
        if(QualitySettings.activeColorSpace==ColorSpace.Linear){green=green.linear;red=red.linear;}
        foreach(var cell in ground.cellBounds.allPositionsWithin)
        {
            if(!ground.HasTile(cell))continue;
            var center=owner.InverseTransformPoint(ground.GetCellCenterWorld(cell));
            var color=canInstall(new Vector2Int(cell.x,cell.y))?green:red;
            int first=vertices.Count;
            for(int i=0;i<spriteVertices.Length;i++)
            {
                vertices.Add(center+localRotation*Vector3.Scale(spriteVertices[i],scale));uv.Add(spriteUV[i]);colors.Add(color);normals.Add(Vector3.back);
            }
            foreach(var index in spriteTriangles)triangles.Add(first+index);
            CellCount++;
        }
        Mesh.Clear();Mesh.indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16;
        Mesh.SetVertices(vertices);Mesh.SetUVs(0,uv);Mesh.SetColors(colors);Mesh.SetNormals(normals);Mesh.SetTriangles(triangles,0);Mesh.RecalculateBounds();
        view.SetActive(CellCount>0);
    }
    public void Hide(){if(view!=null)view.SetActive(false);}
    public void Dispose(){Release(view);Release(Mesh);Release(material);}
    static void Release(Object value){if(value==null)return;if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);}
}
