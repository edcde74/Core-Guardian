var folder="docs/development/front_gait_20261017/";
var label=System.IO.File.ReadAllText(folder+"capture_label.txt").Trim();
if(UnityEditor.EditorApplication.isPlaying||UnityEditor.EditorApplication.isCompiling)throw new System.Exception("Stopped editor required");
var preview=new UnityEditor.PreviewRenderUtility();
try
{
 preview.camera.orthographic=true;preview.camera.orthographicSize=2.7f;preview.camera.transform.position=new UnityEngine.Vector3(0,.45f,-10);preview.camera.transform.rotation=UnityEngine.Quaternion.identity;
 preview.camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;preview.camera.backgroundColor=new UnityEngine.Color32(35,40,46,255);preview.camera.allowHDR=false;preview.camera.allowMSAA=false;
 string[] roles={"Defense","Buff","Boss"};float[] cycles={10,12,18};
 var units=new CoreGuardianUnitAnimation[3,4];var speeds=new float[3,4];
 for(int row=0;row<3;row++)for(int col=0;col<4;col++)
 {
  var prefab=row==2?UnityEngine.Resources.Load<UnityEngine.GameObject>("CoreGuardian/Animation/Boss/BossAnimated"):UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Art/ScreenTests/20261008_Depth/Prefabs/enemy_"+(row==0?"defense":"buff")+".prefab");
  var clone=UnityEngine.Object.Instantiate(prefab);preview.AddSingleGO(clone);clone.transform.position=new UnityEngine.Vector3(-2.25f+col*1.5f,1.8f-row*1.8f,0);
  var art=clone.GetComponentsInChildren<UnityEngine.SpriteRenderer>().First(r=>r.name=="2D Sprite");speeds[row,col]=cycles[row]*art.transform.lossyScale.y/32;
  var u=CoreGuardianUnitAnimation.Attach(clone,roles[row]);units[row,col]=u;u.Move(UnityEngine.Vector2.down*speeds[row,col],1+col*.25f);u.Present(1);
 }
 System.IO.Directory.CreateDirectory(folder+label+"_frames");
 for(int frame=0;frame<48;frame++)
 {
  if(frame>0)for(int row=0;row<3;row++)for(int col=0;col<4;col++){units[row,col].Move(UnityEngine.Vector2.down*speeds[row,col],1/48f);units[row,col].Present(1/48f);}
  preview.BeginStaticPreview(new UnityEngine.Rect(0,0,960,864));preview.Render(true,true);var image=preview.EndStaticPreview();System.IO.File.WriteAllBytes(folder+label+"_frames/"+frame.ToString("000")+".png",image.EncodeToPNG());if(frame==0)System.IO.File.WriteAllBytes(folder+label+"_contact.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
 }
}
finally{preview.Cleanup();}
return label+" isolated Unity preview: rows Defense/Buff/Boss, Down only, columns quarter-cycle offsets, native3x for scale .6; fixed positions, no game camera changes";
