var folder="docs/development/rear_leg_width_20261016/";
var label=System.IO.File.ReadAllText(folder+"capture_label.txt").Trim();
if(UnityEditor.EditorApplication.isPlaying||UnityEditor.EditorApplication.isCompiling)throw new System.Exception("Stopped editor required");
var preview=new UnityEditor.PreviewRenderUtility();
try
{
 preview.camera.orthographic=true;preview.camera.orthographicSize=1.8f;preview.camera.transform.position=new UnityEngine.Vector3(0,.45f,-10);preview.camera.transform.rotation=UnityEngine.Quaternion.identity;
 preview.camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;preview.camera.backgroundColor=new UnityEngine.Color32(35,40,46,255);preview.camera.allowHDR=false;preview.camera.allowMSAA=false;
 var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Art/ScreenTests/20261008_Depth/Prefabs/enemy_attack.prefab");
 var units=new CoreGuardianUnitAnimation[2,4];
 for(int row=0;row<2;row++)for(int col=0;col<4;col++)
 {var clone=UnityEngine.Object.Instantiate(prefab);preview.AddSingleGO(clone);clone.transform.position=new UnityEngine.Vector3(-2.25f+col*1.5f,.7f-row*1.8f,0);var u=CoreGuardianUnitAnimation.Attach(clone,"Attack");units[row,col]=u;u.Move((row==0?UnityEngine.Vector2.down:UnityEngine.Vector2.up)*(14*.6f/32),1+col*.25f);u.Present(1);}
 System.IO.Directory.CreateDirectory(folder+label+"_frames");
 for(int frame=0;frame<48;frame++)
 {
  if(frame>0)for(int row=0;row<2;row++)for(int col=0;col<4;col++){units[row,col].Move((row==0?UnityEngine.Vector2.down:UnityEngine.Vector2.up)*(14*.6f/32),1/48f);units[row,col].Present(1/48f);}
  preview.BeginStaticPreview(new UnityEngine.Rect(0,0,960,576));preview.Render(true,true);var image=preview.EndStaticPreview();System.IO.File.WriteAllBytes(folder+label+"_frames/"+frame.ToString("000")+".png",image.EncodeToPNG());if(frame==0)System.IO.File.WriteAllBytes(folder+label+"_contact.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
 }
}
finally{preview.Cleanup();}
return label+" real Unity preview: rows Down/Up, columns quarter-cycle offsets, native3x";
