from pathlib import Path

root=Path(__file__).resolve().parents[3]
old=root/'docs/development/shadow_only_preview_20261018'
new=Path(__file__).parent
text=(old/'CaptureShadowOnly.eval.cs').read_text(encoding='utf-8')
text=text.replace('shadow_only_preview_20261018/', 'shadow_blunt_preview_20261018/')
text=text.replace('var points=new System.Collections.Generic.List<object>();', 'var textures=new System.Collections.Generic.List<Texture2D>();\nvar points=new System.Collections.Generic.List<object>();')
start=text.index('  var projected=new Mesh')
end=text.index('  var shadowRoot=',start)
text=text[:start]+'''  float anchorX=root.transform.position.x;float anchorY=union.TargetBounds.min.y;
  float width=roles[i]=="Core"?1.20f:roles[i]=="Player"?.36f:i<3?.42f:.68f;
  float depth=roles[i]=="Core"?.45f:roles[i]=="Player"?.24f:i<3?.27f:.31f;
  // A deliberately blunt, filled mass. No weapon, banner, finger or leg silhouettes.
  var polygon=new[]{new Vector2(2,20),new Vector2(9,23),new Vector2(20,22),new Vector2(28,18),new Vector2(36,14),new Vector2(44,9),new Vector2(46,5),new Vector2(43,2),new Vector2(35,1),new Vector2(27,2),new Vector2(16,6),new Vector2(7,12),new Vector2(2,17)};
  var blob=new Texture2D(48,24,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};textures.Add(blob);
  var pixels=new Color[48*24];
  for(int y=0;y<24;y++)for(int x=0;x<48;x++)
  {
   bool inside=false;var p=new Vector2(x+.5f,y+.5f);
   for(int j=0,k=polygon.Length-1;j<polygon.Length;k=j++)
   {
    var a=polygon[j];var b=polygon[k];
    if((a.y>p.y)!=(b.y>p.y)&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)inside=!inside;
   }
   pixels[y*48+x]=inside?Color.white:Color.black;
  }
  blob.SetPixels(pixels);blob.Apply();
  float left=anchorX-.45f*width,right=anchorX+1.10f*width,bottom=anchorY-1.10f*depth,top=anchorY+.20f*depth;
  var projected=new Mesh{name="Blunt compact shadow mass"};
  projected.vertices=new[]{new Vector3(left,bottom),new Vector3(right,bottom),new Vector3(right,top),new Vector3(left,top)};
  projected.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};projected.triangles=new[]{0,1,2,0,2,3};projected.RecalculateBounds();meshes.Add(projected);
''' + text[end:]
text=text.replace('mainTexture=mask','mainTexture=blob')
text=text.replace('foreach(var mat in materials)UnityEngine.Object.DestroyImmediate(mat);','foreach(var mat in materials)UnityEngine.Object.DestroyImmediate(mat);foreach(var texture in textures)UnityEngine.Object.DestroyImmediate(texture);')
text=text.replace('projection=new{x=.50f,y=-.28f,nearOpacity=.38f,farOpacity=.22f}', 'shadow=new{shape="single blunt 13-point mass",opacity=.40f,details="No source silhouette projection"}')
text=text.replace('Only shadow renderers switched.', 'Only shadow renderers switched. Compact blunt polygon, no weapon/limb details.')
(new/'CaptureBluntShadow.eval.cs').write_text(text,encoding='utf-8',newline='\n')
shader=(old/'ShadowPreview.shader.txt').read_text(encoding='utf-8').replace('mask*lerp(.38,.22,saturate(i.uv.y))','mask*.40')
(new/'ShadowPreview.shader.txt').write_text(shader,encoding='utf-8',newline='\n')
compare=(old/'compare.py').read_text(encoding='utf-8').replace("'그림자만 변경'", "'뭉뚝한 그림자'").replace('실제 원본 렌더 + 별도 그림자','실제 원본 + 뭉뚝한 그림자').replace('(2320, 880)', '(2320, 920)').replace('row * 410','row * 435')
(new/'compare.py').write_text(compare,encoding='utf-8',newline='\n')
