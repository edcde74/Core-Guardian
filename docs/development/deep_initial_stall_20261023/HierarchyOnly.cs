using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Profiling;
using Newtonsoft.Json;
public static class CGDeepHierarchyOnly
{
    public static object Read()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||UnityEngine.Profiling.Profiler.enabled||ProfilerDriver.deepProfiling)throw new Exception("Stopped restored Editor required");
        const int frame=3;var watch=System.Diagnostics.Stopwatch.StartNew();
        using var view=ProfilerDriver.GetHierarchyFrameDataView(frame,0,HierarchyFrameDataView.ViewModes.Default,HierarchyFrameDataView.columnTotalTime,false);
        long buildMs=watch.ElapsedMilliseconds;
        if(!view.valid)throw new Exception("Captured hierarchy unavailable");
        var queue=new List<(int id,int depth,string path,double weight)>();var rows=new List<object>();
        int root=view.GetRootItemID();queue.Add((root,0,"",view.GetItemColumnDataAsDouble(root,5)));
        var traverse=System.Diagnostics.Stopwatch.StartNew();
        while(queue.Count>0&&rows.Count<120&&traverse.ElapsedMilliseconds<1500)
        {
            queue.Sort((a,b)=>b.weight.CompareTo(a.weight));var item=queue[0];queue.RemoveAt(0);
            string name=view.GetItemName(item.id),path=item.path.Length==0?name:item.path+" > "+name;
            double total=view.GetItemColumnDataAsDouble(item.id,5),self=view.GetItemColumnDataAsDouble(item.id,6),calls=view.GetItemColumnDataAsDouble(item.id,3),gc=view.GetItemColumnDataAsDouble(item.id,4);
            rows.Add(new{path,name,depth=item.depth,totalMs=total,selfMs=self,calls,gcBytes=gc});
            if(item.depth>=24||total<.1)continue;
            var children=new List<int>();view.GetItemChildren(item.id,children);
            foreach(int child in children.Take(6))queue.Add((child,item.depth+1,path,view.GetItemColumnDataAsDouble(child,5)));
        }
        File.WriteAllText("docs/development/deep_initial_stall_20261023/hierarchy_frame3.json",JsonConvert.SerializeObject(new{frame,deepCapture=true,playing=false,buildMs,visited=rows.Count,traverseMs=traverse.ElapsedMilliseconds,unexpanded=queue.Count,limits="single native aggregated hierarchy, at most120 visited items, six children each, no raw sample traversal",rows},Formatting.Indented));
        return new{frame,buildMs,visited=rows.Count,traverseMs=traverse.ElapsedMilliseconds};
    }
}
