using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json;
public static class MaskReuseContract
{
 public static object Run(string label){
  var root=new GameObject("Owned mask capacity contract");var c=root.AddComponent<CoreGuardianSelectionOutline>();c.enabled=false;
  var t=typeof(CoreGuardianSelectionOutline);var ensure=t.GetMethod("EnsureMask",BindingFlags.Instance|BindingFlags.NonPublic);var f=t.GetField("mask",BindingFlags.Instance|BindingFlags.NonPublic);
  try{ensure.Invoke(c,new object[]{100,100});var first=(RenderTexture)f.GetValue(c);ensure.Invoke(c,new object[]{90,95});var smaller=(RenderTexture)f.GetValue(c);bool retained=ReferenceEquals(first,smaller);
   ensure.Invoke(c,new object[]{120,90});var grown=(RenderTexture)f.GetValue(c);bool fits=grown.width>=120&&grown.height>=100;
   var result=new{label,retainedOnShrink=retained,growPreservesBothAxes=fits,passed=retained&&fits};File.WriteAllText("docs/development/mask_reuse_20261020/contract_"+label+".json",JsonConvert.SerializeObject(result,Formatting.Indented));return result;
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
}

