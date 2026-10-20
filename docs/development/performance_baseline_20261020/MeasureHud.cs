var game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();var hud=game.GetComponent<CoreGuardianHud>();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var method=hud.GetType().GetMethod("Refresh",flags);
var content=(RectTransform)hud.GetType().GetField("damageContent",flags).GetValue(hud);
var saved=new Dictionary<string,int>(game.Session.DamageTotals);var samples=new List<object>();
try{
 game.Session.DamageTotals.Clear();game.Session.DamageTotals["Player"]=100;
 foreach(var tower in game.Session.Towers)game.Session.DamageTotals["Tower:"+tower.Id]=100;
 method.Invoke(hud,null);
 for(int i=0;i<6;i++){
  var before=new HashSet<Transform>(Enumerable.Range(0,content.childCount).Select(n=>content.GetChild(n)));
  game.Session.DamageTotals["Player"]++;
  var watch=System.Diagnostics.Stopwatch.StartNew();method.Invoke(hud,null);watch.Stop();
  int newRows=Enumerable.Range(0,content.childCount).Count(n=>!before.Contains(content.GetChild(n)));
  samples.Add(new{ms=watch.Elapsed.TotalMilliseconds,newRows,damageSources=game.Session.DamageTotals.Count});
 }
}finally{game.Session.DamageTotals.Clear();foreach(var pair in saved)game.Session.DamageTotals[pair.Key]=pair.Value;method.Invoke(hud,null);}
var result=new{kind="synchronous forced damage value refresh; existing scene HUD; synthetic totals, no frame interpretation; deferred destroys may overlap within this single eval",samples};
System.IO.File.WriteAllText("docs/development/performance_baseline_20261020/hud.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
