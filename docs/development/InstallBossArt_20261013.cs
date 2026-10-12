using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

public static class InstallBossArt_20261013
{
    const string Folder="Assets/Resources/CoreGuardian/Art/BossEye";
    const string Image=Folder+"/BossEye.png";
    const string Prefab=Folder+"/BossEyePrefab.prefab";
    const string Reference="Assets/Art/ScreenTests/20261008_Depth/Prefabs/enemy_defense.prefab";
    public static object Main()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||EditorApplication.isCompiling||scene.isDirty)throw new Exception("Expected stopped, compiled, saved scene");
        if(File.Exists(Prefab))throw new Exception("BossEye already exists; do not overwrite");
        Directory.CreateDirectory(Folder);
        File.Copy("docs/art/20261013_BossReview/boss_enraged_v3_64x80.png",Image,false);
        AssetDatabase.ImportAsset(Image,ImportAssetOptions.ForceSynchronousImport);
        var tex=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(tex,File.ReadAllBytes(Image));
        var px=tex.GetPixels32();int bottom=tex.height,left=tex.width,right=-1,top=-1;
        for(int y=0;y<tex.height;y++)for(int x=0;x<tex.width;x++)if(px[y*tex.width+x].a>=128)bottom=Math.Min(bottom,y);
        for(int y=bottom;y<bottom+5;y++)for(int x=0;x<tex.width;x++)if(px[y*tex.width+x].a>=128){left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Max(top,y);}
        if(right<left)throw new Exception("No foot contact");
        var foot=new Vector2((left+right+1)*.5f,(bottom+top+1)*.5f);
        UnityEngine.Object.DestroyImmediate(tex);
        var imp=(TextureImporter)AssetImporter.GetAtPath(Image);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;
        imp.spritePixelsPerUnit=32;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.alphaIsTransparency=true;imp.npotScale=TextureImporterNPOTScale.None;
        var settings=new TextureImporterSettings();imp.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(foot.x/64f,foot.y/80f);imp.SetTextureSettings(settings);imp.SaveAndReimport();
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Image);if(sprite==null)throw new Exception("Missing imported sprite");
        var contact=Sprite.Create(sprite.texture,new Rect(left,bottom,right-left+1,top-bottom+1),new Vector2((foot.x-left)/(right-left+1),(foot.y-bottom)/(top-bottom+1)),32);
        contact.name="BossEyeContact";AssetDatabase.CreateAsset(contact,Folder+"/BossEyeContact.asset");
        var root=PrefabUtility.LoadPrefabContents(Reference);
        try{
            root.name="BossEye";root.transform.position=Vector3.zero;root.transform.localScale=Vector3.one;
            var art=root.transform.Find("2D Sprite").GetComponent<SpriteRenderer>();art.sprite=sprite;art.color=Color.white;art.transform.localPosition=Vector3.zero;art.transform.localScale=Vector3.one*.6f;
            var shadow=root.transform.Find("그림자");shadow.localScale*=1.4f;
            var line=shadow.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="바닥 밀착선");line.sprite=contact;line.transform.localPosition=Vector3.zero;line.transform.localScale=Vector3.one*(.6f/1.4f);
            root.GetComponent<ArtDepthSorting>().SetArtwork(art);
            if(PrefabUtility.SaveAsPrefabAsset(root,Prefab)==null)throw new Exception("Prefab save failed");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
        return JsonConvert.SerializeObject(new{success=true,image=Image,prefab=Prefab,footPixels=new[]{foot.x,foot.y},ppu=32,scale=.6f,sceneDirty=scene.isDirty});
    }
    public static object Verify()
    {
        var root=Resources.Load<GameObject>("CoreGuardian/Art/BossEye/BossEyePrefab");if(root==null)throw new Exception("Resources prefab missing");
        var art=root.transform.Find("2D Sprite").GetComponent<SpriteRenderer>();var imp=(TextureImporter)AssetImporter.GetAtPath(Image);
        if(art.sprite!=AssetDatabase.LoadAssetAtPath<Sprite>(Image)||art.color!=Color.white||art.transform.localPosition!=Vector3.zero||Mathf.Abs(art.transform.localScale.x-.6f)>.0001f)throw new Exception("Wrong boss artwork");
        if(imp.filterMode!=FilterMode.Point||imp.mipmapEnabled||imp.textureCompression!=TextureImporterCompression.Uncompressed||imp.spritePixelsPerUnit!=32)throw new Exception("Wrong import settings");
        return JsonConvert.SerializeObject(new{success=true,prefab=Prefab,pivot=new[]{art.sprite.pivot.x,art.sprite.pivot.y},rect=new[]{art.sprite.rect.width,art.sprite.rect.height},renderers=root.GetComponentsInChildren<SpriteRenderer>().Length});
    }
}
