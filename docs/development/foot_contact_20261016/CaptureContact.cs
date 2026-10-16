if(UnityEditor.EditorApplication.isPlaying||UnityEditor.EditorApplication.isCompiling)throw new System.Exception("Stopped editor required");
var folder="docs/development/foot_contact_20261016/";
var oldTexture=new UnityEngine.Texture2D(2,2,UnityEngine.TextureFormat.RGBA32,false);
UnityEngine.ImageConversion.LoadImage(oldTexture,System.IO.File.ReadAllBytes(folder+"backup/enemy_attack_v2.png"));
oldTexture.filterMode=UnityEngine.FilterMode.Point;
var oldSprite=UnityEngine.Sprite.Create(oldTexture,new UnityEngine.Rect(0,0,42,64),new UnityEngine.Vector2(.5f,0),32);
var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Art/ScreenTests/20261008_Depth/Prefabs/enemy_attack.prefab");
var preview=new UnityEditor.PreviewRenderUtility();
var footResults=new System.Collections.Generic.List<object>();
try
{
 preview.camera.orthographic=true;preview.camera.orthographicSize=1.8f;
 preview.camera.transform.position=new UnityEngine.Vector3(0,.45f,-10);preview.camera.transform.rotation=UnityEngine.Quaternion.identity;
 preview.camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;preview.camera.backgroundColor=new UnityEngine.Color32(75,77,76,255);
 preview.camera.allowHDR=false;preview.camera.allowMSAA=false;
 for(int row=0;row<2;row++)for(int col=0;col<6;col++)
 {
  var clone=UnityEngine.Object.Instantiate(prefab);preview.AddSingleGO(clone);clone.transform.position=new UnityEngine.Vector3(-3.75f+col*1.5f,.7f-row*1.8f,0);
  var art=clone.GetComponentsInChildren<UnityEngine.SpriteRenderer>(true).First(x=>x.name=="2D Sprite");
  var shadow=clone.GetComponentsInChildren<UnityEngine.SpriteRenderer>(true).Where(x=>x.sprite!=null&&x.sprite.name.Contains("Shadow")).OrderByDescending(x=>x.bounds.size.x).First();
  if(col==0)
  {
   art.sprite=oldSprite;art.flipX=row==0;
   var parent=art.transform.parent;var ground=parent.InverseTransformPoint(shadow.bounds.center);
   var anchor=new UnityEngine.Vector3(row==0?-4:4,2,0)/32;
   art.transform.localPosition=ground-art.transform.localRotation*UnityEngine.Vector3.Scale(art.transform.localScale,anchor);
  }
  else
  {
   var unit=CoreGuardianUnitAnimation.Attach(clone,"Attack");
   var v=(row==0?UnityEngine.Vector2.right:UnityEngine.Vector2.left)*(14*.6f/32);
   if(col==1){unit.Move(v,1);unit.Present(1);unit.Move(UnityEngine.Vector2.zero,1);unit.Present(1);}
   else {unit.Move(v,1+(col-2)*.25f);unit.Present(1);}
   footResults.Add(new{row,col,phase=col<2?-1:(col-2)*.25f,root=clone.transform.position.ToString(),shadow=shadow.bounds.center.ToString()});
  }
 }
 preview.BeginStaticPreview(new UnityEngine.Rect(0,0,1440,576));preview.Render(true,true);
 var image=preview.EndStaticPreview();System.IO.File.WriteAllBytes(folder+"unity_contact_native3x.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
}
finally{preview.Cleanup();UnityEngine.Object.DestroyImmediate(oldSprite);UnityEngine.Object.DestroyImmediate(oldTexture);}
var result=new {rows="rightward mirrored / leftward original",columns="before static / after static / after phase0 / .25 / .5 / .75",nativeScale=3,footResults};
System.IO.File.WriteAllText(folder+"capture.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
return "Saved actual Unity renderer contact comparison at native3x";
