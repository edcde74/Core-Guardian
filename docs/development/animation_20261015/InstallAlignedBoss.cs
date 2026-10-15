using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
public static class InstallAlignedBoss
{
    public static object Correct()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new Exception("Stop first");
        const string folder="Assets/Resources/CoreGuardian/Animation/Boss/";
        var original=Resources.Load<Sprite>("CoreGuardian/Art/BossEye/BossEye");
        var aligned=Sprite.Create(original.texture,original.rect,new Vector2(42f/64,6.5f/80),32);aligned.name="BossFeetCenteredV2";
        AssetDatabase.CreateAsset(aligned,folder+"BossFeetCenteredV2.asset");
        var root=PrefabUtility.LoadPrefabContents(folder+"BossAnimated.prefab");
        try{root.transform.Find("2D Sprite").GetComponent<SpriteRenderer>().sprite=aligned;PrefabUtility.SaveAsPrefabAsset(root,folder+"BossAnimated.prefab");}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.DeleteAsset(folder+"BossFeetCentered.asset");AssetDatabase.SaveAssets();
        return "Aligned both shoes at pivot(42,6.5), excluding axe pixels beginning at row15; original PNG and prefab preserved";
    }
    public static object Main()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Stopped compiled saved scene required");
        const string folder="Assets/Resources/CoreGuardian/Animation/Boss/";
        if(File.Exists(folder+"BossAnimated.prefab"))throw new Exception("Already installed");
        var original=Resources.Load<Sprite>("CoreGuardian/Art/BossEye/BossEye");
        var texture=new Texture2D(2,2);ImageConversion.LoadImage(texture,File.ReadAllBytes(AssetDatabase.GetAssetPath(original)));
        var pixels=texture.GetPixels32();int bottom=texture.height,top=-1;
        for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>=128){bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
        int left=texture.width,right=-1;
        // 8 lower rows include both shoes but exclude the axe head beginning at row15.
        for(int y=bottom;y<bottom+8;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>=128){left=Math.Min(left,x);right=Math.Max(right,x);}
        var pivot=new Vector2((left+right+1)*.5f,bottom+.5f);
        var aligned=Sprite.Create(original.texture,original.rect,new Vector2(pivot.x/original.rect.width,pivot.y/original.rect.height),32);
        aligned.name="BossFeetCentered";AssetDatabase.CreateAsset(aligned,folder+"BossFeetCentered.asset");
        var root=PrefabUtility.LoadPrefabContents("Assets/Resources/CoreGuardian/Art/BossEye/BossEyePrefab.prefab");
        try
        {
            root.name="BossAnimated";
            root.transform.Find("2D Sprite").GetComponent<SpriteRenderer>().sprite=aligned;
            root.transform.Find("2D Sprite").localPosition=Vector3.zero;
            foreach(var r in root.GetComponentsInChildren<SpriteRenderer>())if(r.name=="바닥 밀착선")r.enabled=false;
            if(PrefabUtility.SaveAsPrefabAsset(root,folder+"BossAnimated.prefab")==null)throw new Exception("Save failed");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);UnityEngine.Object.DestroyImmediate(texture);}
        AssetDatabase.SaveAssets();
        return JsonConvert.SerializeObject(new{success=true,oldPivot=new[]{original.pivot.x,original.pivot.y},newPivot=new[]{pivot.x,pivot.y},originalPreserved=true});
    }
}
