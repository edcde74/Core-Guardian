using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CoreGuardian;

public static class VerifyWaveOrder
{
    public static object Main()
    {
        AssetDatabase.Refresh();
        Func<string,string> csv = name => Resources.Load<TextAsset>("CoreGuardian/" + name).text;
        var data = CombatData.Load(csv("enemies"),csv("towers"),csv("stage"),csv("upgrades"),csv("waves"));
        var manifest = JObject.Parse(File.ReadAllText("docs/development/wave_order_confirmed_20261010.json"));
        int checkedEvents = 0;
        foreach(var wave in manifest["waves"])
        {
            int number = (int)wave["wave"];
            var rows = data.Waves.Where(x=>x.Wave==number).OrderBy(x=>x.Start).ToArray();
            var events = wave["events"].ToArray();
            if(rows.Length!=events.Length) throw new Exception("Wave count mismatch: "+number);
            for(int i=0;i<rows.Length;i++)
            {
                if(rows[i].Count!=1 || rows[i].Kind.ToString()!=(string)events[i]["enemy"] ||
                   Math.Abs(rows[i].Start-(float)events[i]["start"])>0.0001f ||
                   Math.Abs(rows[i].Interval-(float)wave["interval"])>0.0001f)
                    throw new Exception("Wave event mismatch: "+number+" / "+(i+1));
                checkedEvents++;
            }
        }
        var first = data.Waves.Where(x=>x.Wave==1).Single();
        if(first.Kind!=EnemyKind.Attack || first.Count!=15 || first.Start!=0 || Math.Abs(first.Interval-1.8f)>0.0001f)
            throw new Exception("Wave 1 changed");
        if(data.Waves.Any(x=>x.Wave>9)) throw new Exception("Unexpected wave");
        var result = new {success=true, checkedEvents, wave1Preserved=true, unityLoaderPassed=true, verificationScope="spawn_data_only"};
        var json = JsonConvert.SerializeObject(result,Formatting.Indented);
        File.WriteAllText("docs/development/wave_order_unity_validation_20261010.json",json);
        return json;
    }
}
