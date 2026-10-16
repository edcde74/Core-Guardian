using System;
using System.IO;
using UnityEngine;
using UnityEditor;
public static class CapturePlayer
{
    public static object Main()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Capture requires a stopped, compiled Editor");
        const string folder="docs/development/player_facing_20261016/frames/";Directory.CreateDirectory(folder);
        var preview=new PreviewRenderUtility();
        try
        {
            var cam=preview.camera;cam.orthographic=true;cam.orthographicSize=1.125f;cam.transform.position=new Vector3(0,-.075f,-10);cam.transform.rotation=Quaternion.identity;
            cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color32(40,44,49,255);cam.allowHDR=false;cam.allowMSAA=false;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/ScreenTests/20261008_Depth/Prefabs/Player.prefab");var units=new CoreGuardianUnitAnimation[2,8];
            for(int row=0;row<2;row++)for(int col=0;col<9;col++)
            {
                var clone=UnityEngine.Object.Instantiate(prefab);preview.AddSingleGO(clone);clone.transform.position=new Vector3(col-4,-row*1.05f,0);
                if(col==0)continue;
                var unit=CoreGuardianUnitAnimation.Attach(clone,"Player");units[row,col-1]=unit;
                var v=new Vector2(Mathf.Cos((col-1)*Mathf.PI/4),Mathf.Sin((col-1)*Mathf.PI/4));unit.Aim(v,1);
            }
            for(int frame=0;frame<48;frame++)
            {
                for(int row=0;row<2;row++)for(int d=0;d<8;d++)
                {
                    var u=units[row,d];var v=new Vector2(Mathf.Cos(d*Mathf.PI/4),Mathf.Sin(d*Mathf.PI/4));
                    u.Move(v*(64*.6f/32),1/48f);if(row==1&&frame%12==0)u.Fire(v,4);u.Present(1/48f);
                }
                preview.BeginStaticPreview(new Rect(0,0,1920,480));preview.Render(true,true);var image=preview.EndStaticPreview();File.WriteAllBytes(folder+frame.ToString("000")+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
            }
        }
        finally{preview.Cleanup();}
        return "Actual Unity render, 48 frames. Columns: original/E/NE/N/NW/W/SW/S/SE. Rows: walking/walking+real Fire presentation. Same native4x scale.";
    }
}
