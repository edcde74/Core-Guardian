using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using Newtonsoft.Json;
public static class CGBShippingThreads
{
    public static object Run()
    {
        if(EditorApplication.isPlaying||UnityEngine.Profiling.Profiler.enabled)throw new Exception("Idle Editor required");
        var result=new List<object>();
        foreach(var side in new[]{"baseline_late","b_shipping"})for(int run=0;run<3;run++)
        {
            var path=Path.GetFullPath("Logs/CG_RenderBatching_20261022/b2_benchmark/"+side+"/run_"+run+".data");if(!ProfilerDriver.LoadProfile(path,false))throw new Exception(path);
            var rows=new List<object>();
            for(int frame=ProfilerDriver.firstFrameIndex;frame<=ProfilerDriver.lastFrameIndex;frame++)using(var iterator=new ProfilerFrameDataIterator())
            for(int thread=0;thread<iterator.GetThreadCount(frame);thread++)using(var raw=ProfilerDriver.GetRawFrameDataView(frame,thread))
            {
                if(!raw.valid||raw.sampleCount==0||(raw.threadName!="Main Thread"&&raw.threadName!="Render Thread"))continue;
                var markers=new Dictionary<string,double>();
                for(int sample=0;sample<raw.sampleCount;sample++)
                {
                    string name=raw.GetSampleName(sample);if(string.IsNullOrEmpty(name))continue;
                    if(name.StartsWith("CG.Mask.")||name=="Gfx.WaitForPresentOnGfxThread"||name=="Gfx.WaitForGfxCommandsFromMainThread"||name=="WaitForTargetFPS"||name=="Semaphore.WaitForSignal")
                    {markers.TryGetValue(name,out var value);markers[name]=value+raw.GetSampleTimeMs(sample);}
                }
                var counters=new Dictionary<string,long?>();foreach(var name in new[]{"Standard Draw Calls Count","SRP Batcher Draw Calls Count","SetPass Calls Count"}){int id=raw.GetMarkerId(name);counters[name]=id>=0&&raw.HasCounterValue(id)?(long?)raw.GetCounterValueAsLong(id):null;}
                rows.Add(new{frame,raw.threadName,raw.frameTimeMs,raw.frameGpuTimeMs,markers,counters});
            }
            File.WriteAllText("docs/development/render_batching_20261022/b2_benchmark/"+side+"/run_"+run+"_threads.json",JsonConvert.SerializeObject(rows,Formatting.Indented));result.Add(new{side,run,rows=rows.Count});
        }
        return result;
    }
}

