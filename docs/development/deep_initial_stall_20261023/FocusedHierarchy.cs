using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Profiling;
using Newtonsoft.Json;
public static class CGDeepFocusedHierarchy
{
    public static object Read()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||UnityEngine.Profiling.Profiler.enabled||ProfilerDriver.deepProfiling)throw new Exception("Stopped restored Editor required");
        var output=new List<object>();
        foreach(int frame in new[]{4,5})
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();
            using var view=ProfilerDriver.GetHierarchyFrameDataView(frame,0,HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,HierarchyFrameDataView.columnTotalTime,false);
            long buildMs=watch.ElapsedMilliseconds;if(!view.valid)throw new Exception("Captured frame unavailable");
            var queue=new List<(int id,int depth,string path,double weight)>();var seeds=new List<(int id,int depth,string path,double weight)>();var trunk=new List<object>();var rows=new List<object>();
            int root=view.GetRootItemID();queue.Add((root,0,"",view.GetItemColumnDataAsDouble(root,5)));
            var traverse=System.Diagnostics.Stopwatch.StartNew();
            while(queue.Count>0&&trunk.Count<80&&traverse.ElapsedMilliseconds<1500)
            {
                queue.Sort((a,b)=>b.weight.CompareTo(a.weight));var item=queue[0];queue.RemoveAt(0);string name=view.GetItemName(item.id);
                trunk.Add(new{name,totalMs=item.weight});
                if(name.Contains("CoreGuardianObjectEmphasis.PresentTargets")||name.Contains("CoreGuardianPrototype.SynchronizeViews")||name.Contains("Session.StepTick")){seeds.Add((item.id,0,"",item.weight));continue;}
                if(name.Contains("GUIView.RepaintAll")||name.Contains("Profiler.IntegrateSessionData")||item.depth>18||item.weight<.1)continue;
                var children=new List<int>();view.GetItemChildren(item.id,children);foreach(int child in children.Take(8))queue.Add((child,item.depth+1,name,view.GetItemColumnDataAsDouble(child,5)));
                if(seeds.Count>=3)break;
            }
            queue=seeds;
            while(queue.Count>0&&rows.Count<180&&traverse.ElapsedMilliseconds<1500)
            {
                queue.Sort((a,b)=>b.weight.CompareTo(a.weight));var item=queue[0];queue.RemoveAt(0);string name=view.GetItemName(item.id),path=item.path.Length==0?name:item.path+" > "+name;
                double total=view.GetItemColumnDataAsDouble(item.id,5),self=view.GetItemColumnDataAsDouble(item.id,6),calls=view.GetItemColumnDataAsDouble(item.id,3),gc=view.GetItemColumnDataAsDouble(item.id,4);
                rows.Add(new{path,name,depth=item.depth,totalMs=total,selfMs=self,calls,gcBytes=gc});
                if(item.depth>=18||total<.1)continue;var children=new List<int>();view.GetItemChildren(item.id,children);foreach(int child in children.Take(8))queue.Add((child,item.depth+1,path,view.GetItemColumnDataAsDouble(child,5)));
            }
            output.Add(new{frame,buildMs,traverseMs=traverse.ElapsedMilliseconds,visited=rows.Count,unexpanded=queue.Count,trunk,rows});
        }
        File.WriteAllText("docs/development/deep_initial_stall_20261023/focused_hierarchy.json",JsonConvert.SerializeObject(new{mode="native merged hierarchy, focused game branches, no raw sample traversal",output},Formatting.Indented));return new{frames=output.Count,saved=true};
    }
}
