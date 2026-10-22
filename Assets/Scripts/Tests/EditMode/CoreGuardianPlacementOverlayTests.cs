using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CoreGuardianPlacementOverlayTests
{
    GameObject root;
    UnityEngine.SceneManagement.Scene preview;
    Tilemap ground;
    Tile tile;
    Texture2D texture;
    Sprite sprite;
    Material material;
    object overlay;
    Type OverlayType=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianPlacementOverlay")).FirstOrDefault(t=>t!=null);
    [SetUp] public void SetUp()
    {
        preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        root=new GameObject("Placement overlay tests");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);
        var grid=root.AddComponent<Grid>();grid.cellSize=new Vector3(.7f,.7f,1);
        var map=new GameObject("Ground");map.transform.SetParent(root.transform,false);ground=map.AddComponent<Tilemap>();
        tile=ScriptableObject.CreateInstance<Tile>();ground.SetTile(Vector3Int.zero,tile);ground.SetTile(new Vector3Int(2,1,0),tile);
        texture=new Texture2D(16,16);sprite=Sprite.Create(texture,new Rect(0,0,16,16),Vector2.one*.5f,16);
        material=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));
    }
    void Create(){Assert.That(OverlayType,Is.Not.Null,"Batched placement overlay is missing");overlay=Activator.CreateInstance(OverlayType,root.transform,sprite,material);}
    void Show(Func<Vector2Int,bool> predicate){OverlayType.GetMethod("Show").Invoke(overlay,new object[]{ground,predicate});}
    T Read<T>(string name)=>(T)OverlayType.GetProperty(name).GetValue(overlay);
    [TearDown] public void TearDown()
    {
        (overlay as IDisposable)?.Dispose();UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(material);
        UnityEngine.Object.DestroyImmediate(sprite);UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(tile);
        UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
    }
    [Test] public void SparseCellsKeepLegacyColorsGeometryAndSorting()
    {
        Create();Show(p=>p.x==0);var mesh=Read<Mesh>("Mesh");var renderer=Read<MeshRenderer>("Renderer");
        Assert.That(Read<int>("CellCount"),Is.EqualTo(2));Assert.That(mesh.vertexCount,Is.EqualTo(sprite.vertices.Length*2));
        Assert.That(renderer.sortingOrder,Is.EqualTo(3));Assert.That(renderer.sharedMaterial.mainTexture,Is.SameAs(texture));
        var green=new Color(.1f,1f,.35f,.13f);var red=new Color(1f,.15f,.1f,.2f);
        if(QualitySettings.activeColorSpace==ColorSpace.Linear){green=green.linear;red=red.linear;}
        Assert.That(mesh.colors.Count(c=>Mathf.Abs(c.a-.13f)<.000001f),Is.EqualTo(4));
        Assert.That(mesh.colors.Any(c=>Vector4.Distance(c,green)<.00001f)&&mesh.colors.Any(c=>Vector4.Distance(c,red)<.00001f),Is.True);
        Assert.That(renderer.gameObject.activeSelf,Is.True);
    }
    [Test] public void ReenterReusesResourcesAndRefreshesOccupiedCellColors()
    {
        Create();Show(p=>true);var mesh=Read<Mesh>("Mesh");var renderer=Read<MeshRenderer>("Renderer");var mat=renderer.sharedMaterial;
        OverlayType.GetMethod("Hide").Invoke(overlay,null);Assert.That(renderer.gameObject.activeSelf,Is.False);
        Show(p=>false);Assert.That(Read<Mesh>("Mesh"),Is.SameAs(mesh));Assert.That(Read<MeshRenderer>("Renderer"),Is.SameAs(renderer));
        Assert.That(renderer.sharedMaterial,Is.SameAs(mat));Assert.That(mesh.colors.All(c=>Mathf.Abs(c.a-.2f)<.000001f),Is.True);
    }
    [Test] public void DisposeReleasesOnlyOwnedMeshMaterialAndObject()
    {
        Create();Show(p=>true);var mesh=Read<Mesh>("Mesh");var renderer=Read<MeshRenderer>("Renderer");var mat=renderer.sharedMaterial;
        ((IDisposable)overlay).Dispose();overlay=null;
        Assert.That(mesh==null&&renderer==null&&mat==null,Is.True);Assert.That(sprite!=null&&texture!=null&&material!=null,Is.True);
    }
    [Test] public void WorldCornersMatchLegacyUnderRotatedScaledOwner()
    {
        root.transform.SetPositionAndRotation(new Vector3(3,2,0),Quaternion.Euler(0,0,18));root.transform.localScale=new Vector3(2,.75f,1);
        Create();Show(p=>true);var renderer=Read<MeshRenderer>("Renderer");var mesh=Read<Mesh>("Mesh");
        var legacy=new GameObject("Legacy transform");legacy.transform.SetParent(root.transform);legacy.transform.position=ground.GetCellCenterWorld(Vector3Int.zero);
        var size=ground.layoutGrid.cellSize;legacy.transform.localScale=new Vector3(size.x*(.55f/.6f),size.y*(.55f/.6f),1);
        foreach(var v in sprite.vertices){var world=legacy.transform.TransformPoint(v);Assert.That(mesh.vertices.Any(p=>Vector3.Distance(renderer.transform.TransformPoint(p),world)<.00001f),Is.True);}
    }
}
