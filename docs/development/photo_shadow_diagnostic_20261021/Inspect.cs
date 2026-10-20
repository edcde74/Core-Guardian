// Offline native Profiler traversal. Run only after our capture finishes.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Profiling;
using UnityEditorInternal;
using Newtonsoft.Json;
public static class CGShadowInspect
{
    public static object Run(string side)
    {
        if(Profiler.enabled)throw new Exception("Recording active; do not load capture");
        string root="docs/development/photo_shadow_diagnostic_20261021/";
        var rows=new List<object>();
        foreach(var file in Directory.GetFiles(root+side,"*.data").OrderBy(p=>p))
        {
            ProfilerDriver.LoadProfile(Path.GetFullPath(file),false);
            int valid=0;
            for(int frame=ProfilerDriver.firstFrameIndex;frame<=ProfilerDriver.lastFrameIndex;frame++)
            using(var it=new ProfilerFrameDataIterator())
            for(int thread=0;thread<it.GetThreadCount(frame);thread++)
            using(var raw=ProfilerDriver.GetRawFrameDataView(frame,thread))
            {
                if(!raw.valid||raw.threadName!="Main Thread")continue;
                valid++;double photo=0,self=0,max=0;int calls=0;object largest=null;
                var stages=new Dictionary<string,double>();
                for(int i=0;i<raw.sampleCount;i++)
                {
                    if(!(raw.GetSampleName(i)??"").Contains("CoreGuardianPhotoShadow.LateUpdate"))continue;
                    int end=i+raw.GetSampleChildrenCountRecursive(i)+1;
                    double ms=raw.GetSampleTimeMs(i),children=0;
                    for(int j=i+1;j<end;j+=raw.GetSampleChildrenCountRecursive(j)+1)children+=raw.GetSampleTimeMs(j);
                    photo+=ms;self+=Math.Max(0,ms-children);calls++;
                    var subtree=new List<object>();
                    for(int j=i+1;j<end;j++)
                    {
                        string name=raw.GetSampleName(j)??"<unnamed>";double time=raw.GetSampleTimeMs(j);
                        if(name.StartsWith("CG.")){if(!stages.ContainsKey(name))stages[name]=0;stages[name]+=time;}
                        if(ms>max&&time>.02)subtree.Add(new {name,time});
                    }
                    if(ms>max){max=ms;largest=new {index=i,ms,self=Math.Max(0,ms-children),subtree};}
                    i=end-1;
                }
                rows.Add(new {capture=Path.GetFileName(file),frame,raw.frameTimeMs,calls,photoMs=photo,photoSelfMs=self,maxPhotoCallMs=max,stages,largest});
            }
            if(valid==0)throw new Exception("No native main frames: "+file);
        }
        File.WriteAllText(root+side+"/photo_paths.json",JsonConvert.SerializeObject(rows,Formatting.Indented));
        return new {side,frames=rows.Count};
    }
}
