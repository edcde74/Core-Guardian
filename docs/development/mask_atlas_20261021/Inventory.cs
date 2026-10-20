using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
public static class CGAtlasInventory
{
 public static object Run(){
  var f=BindingFlags.Instance|BindingFlags.NonPublic;
  var game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
  var owners=Resources.FindObjectsOfTypeAll<CoreGuardianSelectionOutline>().Where(o=>o.CurrentTarget!=null&&o.IsVisible).ToArray();
  var rows=owners.Select(o=>{var s=(System.Collections.Generic.List<SpriteRenderer>)typeof(CoreGuardianSelectionOutline).GetField("sources",f).GetValue(o);return new{target=o.CurrentTarget.name,owner=o.name,parts=s.Count,draws=(int)typeof(CoreGuardianSelectionOutline).GetField("lastMaskDrawCalls",f).GetValue(o),misses=(int)typeof(CoreGuardianSelectionOutline).GetField("lastAtlasMisses",f).GetValue(o),texturePaths=s.Select(r=>UnityEditor.AssetDatabase.GetAssetPath(r.sprite.texture)).ToArray(),textures=s.Select(r=>r.sprite.texture.name).ToArray(),spriteNames=s.Select(r=>r.sprite.name).ToArray()};}).ToArray();
  var result=new{enemies=game.Session.Enemies.Count,owners=rows.Length,parts=rows.Sum(r=>r.parts),draws=rows.Sum(r=>r.draws),mergedOwners=rows.Count(r=>r.parts>r.draws),rows};
  System.IO.File.WriteAllText("docs/development/mask_atlas_20261021/source_inventory.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{result.enemies,result.owners,result.parts,result.draws,result.mergedOwners,examples=rows.Where(r=>r.parts>1).Take(5).ToArray()};
 }
}