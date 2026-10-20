using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class CGAtlasCleanup
{
    public static object Run()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling||UnityEngine.Profiling.Profiler.enabled||UnityEngine.FrameDebugger.enabled)throw new Exception("Idle EditMode required");
        var held=new HashSet<Mesh>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        foreach(var owner in Resources.FindObjectsOfTypeAll<CoreGuardianSelectionOutline>())
            foreach(var batch in (IEnumerable)typeof(CoreGuardianSelectionOutline).GetField("batches",flags).GetValue(owner))
            {var mesh=(Mesh)batch.GetType().GetField("mesh").GetValue(batch);if(mesh!=null)held.Add(mesh);}
        var removed=new List<object>();
        foreach(var mesh in Resources.FindObjectsOfTypeAll<Mesh>())
        {
            // This exact batch name was introduced by this experiment; preserve every live owner.
            if(mesh.name!="Selection mask part batch"||held.Contains(mesh)||AssetDatabase.Contains(mesh)||(mesh.hideFlags&HideFlags.DontSave)==0)continue;
            removed.Add(new{id=mesh.GetEntityId().ToString(),bytes=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(mesh)});UnityEngine.Object.DestroyImmediate(mesh);
        }
        var result=new{removed,held=held.Count,remainingBatchMeshes=Resources.FindObjectsOfTypeAll<Mesh>().Count(m=>m.name=="Selection mask part batch"),globalMasks=Resources.FindObjectsOfTypeAll<RenderTexture>().Count(t=>t.name=="Selected unit union mask")};
        System.IO.File.WriteAllText("docs/development/mask_atlas_20261021/editor_cleanup.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
