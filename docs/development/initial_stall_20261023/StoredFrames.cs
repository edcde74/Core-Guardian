using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using Newtonsoft.Json;
public static class CGStallStoredFrames
{
    sealed class Node {public string name;public int end,parent;public double ms,child;}
    sealed class Cost {public int calls;public double inclusive,self;public long gc;}
    public static object Read()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||UnityEngine.Profiling.Profiler.enabled)throw new Exception("Stopped idle recording OFF required");
        var output=new List<object>();
        foreach(int frame in new[]{4538,4541})
        {
            using var raw=ProfilerDriver.GetRawFrameDataView(frame,0);
            if(!raw.valid||raw.threadName!="Main Thread"||raw.sampleCount>20000){output.Add(new{frame,skipped=true,count=raw.sampleCount});continue;}
            var watch=System.Diagnostics.Stopwatch.StartNew();var nodes=new List<Node>();var stack=new List<int>();var costs=new Dictionary<string,Cost>();bool truncated=false;
            for(int i=0;i<raw.sampleCount;i++)
            {
                if((i&255)==0&&watch.ElapsedMilliseconds>1000){truncated=true;break;}
                while(stack.Count>0&&i>=nodes[stack[stack.Count-1]].end)stack.RemoveAt(stack.Count-1);
                string name=raw.GetSampleName(i)??"<root>";double ms=raw.GetSampleTimeMs(i);
                int parent=stack.Count>0?stack[stack.Count-1]:-1;if(parent>=0)nodes[parent].child+=ms;
                var node=new Node{name=name,ms=ms,end=i+raw.GetSampleChildrenCountRecursive(i)+1,parent=parent};nodes.Add(node);
                if(node.end>i+1)stack.Add(i);
                if(!costs.TryGetValue(name,out var cost))costs[name]=cost=new Cost();cost.calls++;cost.inclusive+=ms;
                if(name=="GC.Alloc"&&raw.GetSampleMetadataCount(i)>0)cost.gc+=raw.GetSampleMetadataAsLong(i,0);
            }
            foreach(var node in nodes)costs[node.name].self+=Math.Max(0,node.ms-node.child);
            string Path(int i){var names=new List<string>();for(int n=0;i>=0&&n<10;n++,i=nodes[i].parent)names.Add(nodes[i].name);names.Reverse();return string.Join(" > ",names);}
            output.Add(new{frame,raw.threadName,raw.sampleCount,raw.frameTimeMs,visited=nodes.Count,truncated,elapsedMs=watch.ElapsedMilliseconds,bySelf=costs.OrderByDescending(p=>p.Value.self).Take(25).Select(p=>new{name=p.Key,p.Value.calls,p.Value.inclusive,p.Value.self,p.Value.gc}),byInclusive=costs.OrderByDescending(p=>p.Value.inclusive).Take(30).Select(p=>new{name=p.Key,p.Value.calls,p.Value.inclusive,p.Value.self}),waits=costs.Where(p=>p.Key.IndexOf("wait",StringComparison.OrdinalIgnoreCase)>=0||p.Key.IndexOf("present",StringComparison.OrdinalIgnoreCase)>=0).OrderByDescending(p=>p.Value.inclusive).Take(15).Select(p=>new{name=p.Key,p.Value.inclusive,p.Value.self}),largestSamples=nodes.Select((n,i)=>new{path=Path(i),n.ms,self=Math.Max(0,n.ms-n.child)}).OrderByDescending(n=>n.self).Take(15)});
        }
        File.WriteAllText("docs/development/initial_stall_20261023/stored_cpu_frames.json",JsonConvert.SerializeObject(output,Formatting.Indented));
        return new{frames=output.Count,saved=true};
    }
}
