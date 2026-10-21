using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using Newtonsoft.Json;
public static class CGExtractRenderSettings
{
    public static object Run()
    {
        if(EditorApplication.isPlaying||UnityEngine.Profiling.Profiler.enabled)throw new Exception("Idle EditMode required");
        var reports=new List<object>();
        for(int trial=0;trial<6;trial++)
        {
            string path=Path.GetFullPath("Logs/CG_RenderBatching_20261022/settings_"+trial+".data");
            if(!ProfilerDriver.LoadProfile(path,false))throw new Exception("Cannot load "+path);
            var frames=new List<object>();
            int first=ProfilerDriver.firstFrameIndex,last=ProfilerDriver.lastFrameIndex;
            for(int f=first;f<=last;f++)using(var iterator=new ProfilerFrameDataIterator())
            {
                var threads=new List<object>();double frameMs=0;long gc=0;
                for(int t=0;t<iterator.GetThreadCount(f);t++)using(var raw=ProfilerDriver.GetRawFrameDataView(f,t))
                {
                    if(!raw.valid||raw.sampleCount==0)continue;
                    if(raw.threadName!="Main Thread"&&!raw.threadName.Contains("Render"))continue;
                    var markers=new Dictionary<string,double>();
                    for(int i=0;i<raw.sampleCount;i++)
                    {
                        string name=raw.GetSampleName(i);if(string.IsNullOrEmpty(name))continue;
                        if(raw.threadName=="Main Thread"&&name=="GC.Alloc"&&raw.GetSampleMetadataCount(i)>0)gc+=raw.GetSampleMetadataAsLong(i,0);
                        if(name.Contains("DrawSRPBatcher")||name.Contains("DrawRenderers")||name.Contains("Gfx.")||name.Contains("RenderLoop.Draw")||name=="Camera.Render")
                        {double old;markers.TryGetValue(name,out old);markers[name]=old+raw.GetSampleTimeMs(i);}
                    }
                    if(raw.threadName=="Main Thread")frameMs=raw.frameTimeMs;
                    threads.Add(new{raw.threadName,raw.frameTimeMs,raw.frameGpuTimeMs,markers});
                }
                frames.Add(new{frame=f,frameMs,gc,threads});
            }
            File.WriteAllText("docs/development/render_batching_20261022/native_"+trial+".json",JsonConvert.SerializeObject(frames,Formatting.Indented));
            reports.Add(new{trial,first,last,count=frames.Count});
        }
        File.WriteAllText("docs/development/render_batching_20261022/native_extraction.json",JsonConvert.SerializeObject(reports,Formatting.Indented));
        return reports;
    }
}
