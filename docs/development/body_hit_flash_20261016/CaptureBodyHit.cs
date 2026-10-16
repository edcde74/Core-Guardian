using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

public static class CaptureBodyHit
{
    const string Folder="docs/development/body_hit_flash_20261016/";
    public static object Main()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Capture requires stopped, compiled Editor");
        Directory.CreateDirectory(Folder);var preview=new PreviewRenderUtility();
        var units=new CoreGuardianUnitAnimation[4,3];string[] roles={"Attack","Defense","Buff","Boss"};
        try
        {
            preview.camera.orthographic=true;preview.camera.orthographicSize=3.4f;preview.camera.transform.position=new Vector3(0,.3f,-10);preview.camera.transform.rotation=Quaternion.identity;
            preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color32(35,40,46,255);preview.camera.allowHDR=false;preview.camera.allowMSAA=false;
            for(int row=0;row<4;row++)for(int col=0;col<3;col++)
            {
                var prefab=row==3?Resources.Load<GameObject>("CoreGuardian/Animation/Boss/BossAnimated"):AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/ScreenTests/20261008_Depth/Prefabs/"+new[]{"enemy_attack","enemy_defense","enemy_buff"}[row]+".prefab");
                var clone=UnityEngine.Object.Instantiate(prefab);preview.AddSingleGO(clone);clone.transform.position=new Vector3(-1.8f+col*1.8f,2.1f-row*1.7f,0);
                var unit=CoreGuardianUnitAnimation.Attach(clone,roles[row]);units[row,col]=unit;
                unit.Move(row%2==0?Vector2.right:Vector2.up,.1f);unit.Present(.1f);
                if(col>0){unit.ReceiveHit();unit.Present(0);if(col==2)unit.Present(.12f);}
            }
            preview.BeginStaticPreview(new Rect(0,0,864,1088));preview.Render(true,true);var image=preview.EndStaticPreview();
            File.WriteAllBytes(Folder+"unity_body_flash.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
            var shader=Resources.Load<Shader>("CoreGuardian/BodyHitFlash");var messages=ShaderUtil.GetShaderMessages(shader);
            return new {capture=Folder+"unity_body_flash.png",columns=new[]{"normal","hit peak","restored"},rows=roles,shaderSupported=shader.isSupported,shaderMessages=messages.Select(m=>m.message).ToArray()};
        }
        finally{preview.Cleanup();}
    }
}
