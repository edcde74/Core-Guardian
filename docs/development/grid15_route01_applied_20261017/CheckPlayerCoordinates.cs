using System.IO;
using System.Reflection;
using UnityEngine;

public static class CheckPlayerCoordinates
{
    public static object Main(string phase)
    {
        var g=Object.FindAnyObjectByType<CoreGuardianPrototype>();
        var p=(Vector2)g.GetType().GetMethod("PlayerTile",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g,null);
        var expected=new Vector2(13,9);float error=Vector2.Distance(p,expected);
        var result=new{phase,passed=error<.00001f,expected=expected.ToString(),actual=p.ToString(),error};
        File.WriteAllText("docs/development/grid15_route01_applied_20261017/"+phase+"_coordinates.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
