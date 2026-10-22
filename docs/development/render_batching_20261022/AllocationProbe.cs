using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;
using UnityEditorInternal;
using Newtonsoft.Json;
public sealed class CGBAllocationProbe:MonoBehaviour
{
    int frames;bool oldStacks,oldProfiler,oldDriver,oldBackground;
    public static object Start()
    {
        if(!EditorApplication.isPlaying||Profiler.enabled||FrameDebugger.enabled)throw new Exception("Own idle Play required");
        var host=new GameObject("Temporary B allocation callstack diagnostic");host.AddComponent<CGBAllocationProbe>();return new{started=true,frames=20,timingComparable=false};
    }
    void Awake()
    {
        oldStacks=Profiler.enableAllocationCallstacks;oldProfiler=Profiler.enabled;oldDriver=ProfilerDriver.enabled;oldBackground=Application.runInBackground;
        Application.runInBackground=true;Profiler.enableAllocationCallstacks=true;ProfilerDriver.ClearAllFrames();
        typeof(ProfilerDriver).GetMethod("SetProfilingEnabled",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,new object[]{true});Profiler.enabled=true;
    }
    void LateUpdate(){if(++frames<20)return;enabled=false;Profiler.enabled=false;EditorApplication.delayCall+=Read;}
    void Read()
    {
        var report=new Dictionary<string,long>();var methods=new Dictionary<ulong,string>();var stack=new List<ulong>();
        try
        {
            for(int f=ProfilerDriver.firstFrameIndex;f<=ProfilerDriver.lastFrameIndex;f++)using(var raw=ProfilerDriver.GetRawFrameDataView(f,0))
            {
                if(!raw.valid)continue;int end=-1;
                for(int i=0;i<raw.sampleCount;i++)
                {
                    string name=raw.GetSampleName(i);if(name=="CG.Mask.Present")end=i+raw.GetSampleChildrenCountRecursive(i);
                    if(i>end||name!="GC.Alloc"||raw.GetSampleMetadataCount(i)==0)continue;
                    stack.Clear();raw.GetSampleCallstack(i,stack);var names=new List<string>();
                    foreach(ulong address in stack){if(!methods.TryGetValue(address,out var method)){var info=raw.ResolveMethodInfo(address);method=info.methodName+" "+info.sourceFileName+":"+info.sourceFileLine;methods[address]=method;}names.Add(method);}
                    string key=string.Join(" -> ",names);report.TryGetValue(key,out long value);report[key]=value+raw.GetSampleMetadataAsLong(i,0);
                }
            }
            File.WriteAllText("docs/development/render_batching_20261022/allocation_callstacks.json",JsonConvert.SerializeObject(report.OrderByDescending(p=>p.Value).Take(15),Formatting.Indented));
        }
        finally{Profiler.enableAllocationCallstacks=oldStacks;Profiler.enabled=oldProfiler;ProfilerDriver.enabled=oldDriver;Application.runInBackground=oldBackground;Destroy(gameObject);}
    }
}
