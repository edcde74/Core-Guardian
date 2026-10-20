using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEditor.Profiling;
using Newtonsoft.Json;
public static class CGReadAllocationStacks
{
    public static object Read()
    {
        if(UnityEngine.Profiling.Profiler.enabled||UnityEngine.FrameDebugger.enabled)throw new Exception("Other diagnostic active");
        string root="docs/development/profiler_investigation_20261020";
        if(!ProfilerDriver.LoadProfile(Path.GetFullPath(root+"/captures_valid/alloc_0_n128.data"),false))throw new Exception("Load failed");
        int a=ProfilerDriver.firstFrameIndex,b=ProfilerDriver.lastFrameIndex;
        var result=new List<object>();
        foreach(int frame in new[]{a+2,(a+b)/2,b-2}.Distinct())using(var raw=ProfilerDriver.GetRawFrameDataView(frame,0)){
            if(!raw.valid)continue;
            int allocs=0,sampled=0;long total=0;var examples=new List<object>();
            for(int i=0;i<raw.sampleCount;i++)if(raw.GetSampleName(i)=="GC.Alloc"&&raw.GetSampleMetadataCount(i)>0){
                long bytes=raw.GetSampleMetadataAsLong(i,0);total+=bytes;
                if(allocs++%31!=0)continue;
                var addresses=new List<ulong>();raw.GetSampleCallstack(i,addresses);
                var stack=addresses.Select(x=>raw.ResolveMethodInfo(x)).Where(x=>!string.IsNullOrEmpty(x.methodName)).Select(x=>new{x.methodName,x.sourceFileName,x.sourceFileLine}).ToArray();
                examples.Add(new{sample=i,bytes,stack});sampled++;
            }
            result.Add(new{frame,raw.threadName,totalGcBytes=total,allocs,sampled,examples});
        }
        File.WriteAllText(root+"/allocation_examples.json",JsonConvert.SerializeObject(new{sampling="every 31st GC.Alloc sample in three selected frames; byte totals include all main-thread allocations, examples are not an attribution distribution",result},Formatting.Indented));
        return new{frames=result.Count,saved=true};
    }
}
