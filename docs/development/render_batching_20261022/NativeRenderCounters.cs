using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using Newtonsoft.Json;
public static class CGNativeRenderCounters
{
    public static object Run()
    {
        if(EditorApplication.isPlaying||UnityEngine.Profiling.Profiler.enabled)throw new Exception("Idle Editor required");
        var report=new List<object>();
        foreach(string side in new[]{"baseline","a1","a2"})for(int run=0;run<3;run++)
        {
            string file=Path.GetFullPath("Logs/CG_RenderBatching_20261022/a1_benchmark/"+side+"/run_"+run+".data");
            if(!ProfilerDriver.LoadProfile(file,false))throw new Exception("Cannot load "+file);
            var rows=new List<object>();var allocations=new Dictionary<string,long>();
            for(int frame=ProfilerDriver.firstFrameIndex;frame<=ProfilerDriver.lastFrameIndex;frame++)
            using(var raw=ProfilerDriver.GetRawFrameDataView(frame,0))
            {
                if(!raw.valid||raw.threadName!="Main Thread")continue;
                var values=new Dictionary<string,long?>();
                foreach(string name in new[]{"Standard Draw Calls Count","SRP Batcher Draw Calls Count","SetPass Calls Count","Dynamic Batches Count","Standard Instanced Draw Calls Count","BRG Draw Calls Count"})
                {int id=raw.GetMarkerId(name);values[name]=id>=0&&raw.HasCounterValue(id)?(long?)raw.GetCounterValueAsLong(id):null;}
                var ends=new List<int>();var parents=new List<string>();
                for(int i=0;i<raw.sampleCount;i++)
                {
                    while(ends.Count>0&&ends[ends.Count-1]<i){ends.RemoveAt(ends.Count-1);parents.RemoveAt(parents.Count-1);}
                    string name=raw.GetSampleName(i);
                    if(name=="GC.Alloc"&&raw.GetSampleMetadataCount(i)>0)
                    {string parent=parents.Count==0?"<root>":parents[parents.Count-1];long old;allocations.TryGetValue(parent,out old);allocations[parent]=old+raw.GetSampleMetadataAsLong(i,0);}
                    int children=raw.GetSampleChildrenCountRecursive(i);if(children>0){ends.Add(i+children);parents.Add(name??"<null>");}
                }
                rows.Add(new{frame,raw.frameTimeMs,values});
            }
            string output="docs/development/render_batching_20261022/a1_benchmark/"+side+"/run_"+run+"_native_render.json";
            File.WriteAllText(output,JsonConvert.SerializeObject(new{side,run,rows,allocations},Formatting.Indented));report.Add(new{side,run,frames=rows.Count});
        }
        return report;
    }
}
