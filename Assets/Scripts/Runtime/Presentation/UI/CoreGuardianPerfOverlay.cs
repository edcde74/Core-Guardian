using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CoreGuardian;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;

// Performance overlay + CSV recorder that ships in normal (non-development) player builds.
//   F3  overlay on/off (top-right, above the damage panel)
//   F4  start/stop recording -> <exe folder>/PerfLogs/perf_*.csv (+ _summary.txt)
//   F5  uncapped measurement mode (VSync off, no frame cap); press again to restore
// Command line: -perfhud (overlay on), -perflog (record from launch), -perf (uncapped).
// While hidden and not recording the only per-frame work is one keyboard poll.
// Created at runtime (no scene changes); survives Restart() scene reloads.
public sealed class CoreGuardianPerfOverlay : MonoBehaviour
{
    const int RecentFrames=1024,SecondFrames=4096;
    const float DisplayInterval=.5f,LogInterval=1f;
    static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
    static CoreGuardianPerfOverlay instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if(instance!=null)return;
        var go=new GameObject("CoreGuardianPerfOverlay");
        DontDestroyOnLoad(go);
        instance=go.AddComponent<CoreGuardianPerfOverlay>();
    }

    struct Window
    {
        public int Frames,CpuCount,RenderCount,GpuCount,GcCount;
        public double Sum,Cpu,Render,Gpu,Gc;
        public float Max;
        public void Clear(){this=default;}
        public void Add(float ms,double cpu,double render,double gpu,long gc)
        {
            Frames++;Sum+=ms;if(ms>Max)Max=ms;
            if(cpu>0){Cpu+=cpu;CpuCount++;}
            if(render>0){Render+=render;RenderCount++;}
            if(gpu>0){Gpu+=gpu;GpuCount++;}
            if(gc>=0){Gc+=gc;GcCount++;}
        }
        public double Fps=>Sum>0?Frames*1000.0/Sum:0;
        public double AvgMs=>Frames>0?Sum/Frames:0;
        public double AvgCpu=>CpuCount>0?Cpu/CpuCount:-1;
        public double AvgRender=>RenderCount>0?Render/RenderCount:-1;
        public double AvgGpu=>GpuCount>0?Gpu/GpuCount:-1;
        public double GcKb=>GcCount>0?Gc/GcCount/1024.0:-1;
    }
    sealed class RoundStat
    {
        public readonly List<float> Frames=new List<float>(4096);
        public double Cpu,Gpu;public int CpuCount,GpuCount,MaxEnemies,MaxTowers,MaxSpeed;
    }

    bool visible,recording,uncapped,sampling;
    int savedVSync,savedTarget=-1;
    readonly FrameTiming[] timings=new FrameTiming[1];
    ProfilerRecorder mainThread,gcAlloc,usedMemory;
    long lastThreadAlloc;
    readonly float[] recent=new float[RecentFrames],recentSorted=new float[RecentFrames],second=new float[SecondFrames];
    int recentCount,recentIndex,secondCount,skipFrames;
    Window display,log;
    float displayTimer,logTimer;
    CoreGuardianPrototype game;float nextFind;

    StreamWriter csv;string csvPath,summaryPath,notice;float noticeUntil;
    DateTime recordStart;float recordStartTime;
    readonly List<float> allFrames=new List<float>(1<<16);
    Window all;
    readonly Dictionary<int,RoundStat> rounds=new Dictionary<int,RoundStat>();

    GameObject canvasRoot;UnityEngine.UI.Text label;
    readonly StringBuilder sb=new StringBuilder(512);

    void Awake()
    {
        foreach(var arg in Environment.GetCommandLineArgs())
        {
            if(string.Equals(arg,"-perf",StringComparison.OrdinalIgnoreCase))SetUncapped(true);
            else if(string.Equals(arg,"-perfhud",StringComparison.OrdinalIgnoreCase))SetVisible(true);
            else if(string.Equals(arg,"-perflog",StringComparison.OrdinalIgnoreCase))StartRecording();
        }
    }

    void Update()
    {
        var kb=Keyboard.current;
        if(kb!=null)
        {
            if(kb.f3Key.wasPressedThisFrame)SetVisible(!visible);
            if(kb.f4Key.wasPressedThisFrame){if(recording)StopRecording();else StartRecording();}
            if(kb.f5Key.wasPressedThisFrame)SetUncapped(!uncapped);
        }
        if(!sampling)return;
        Sample();
    }

    void Sample()
    {
        // The first frames after (re)starting carry time from before the start (loading, toggling).
        if(skipFrames>0){skipFrames--;lastThreadAlloc=0;return;}
        float dt=Time.unscaledDeltaTime,ms=dt*1000f;
        double cpu=-1,render=-1,gpu=-1;
        FrameTimingManager.CaptureFrameTimings();
        if(FrameTimingManager.GetLatestTimings(1,timings)>0)
        {
            var t=timings[0];
            // Main-thread work without the wait for Present/VSync.
            cpu=t.cpuMainThreadFrameTime-Math.Max(0,t.cpuMainThreadPresentWaitTime);
            render=t.cpuRenderThreadFrameTime;gpu=t.gpuFrameTime;
        }
        if(cpu<=0&&mainThread.Valid&&mainThread.Count>0)cpu=mainThread.LastValue*1e-6;
        long gc=-1;
        if(gcAlloc.Valid&&gcAlloc.Count>0)gc=gcAlloc.LastValue;
        else
        {
            long now=GC.GetAllocatedBytesForCurrentThread();
            if(now>0&&lastThreadAlloc>0&&now>=lastThreadAlloc)gc=now-lastThreadAlloc;
            lastThreadAlloc=now;
        }

        recent[recentIndex]=ms;recentIndex=(recentIndex+1)%RecentFrames;if(recentCount<RecentFrames)recentCount++;
        display.Add(ms,cpu,render,gpu,gc);
        if(visible)
        {
            displayTimer+=dt;
            if(displayTimer>=DisplayInterval){RefreshText();display.Clear();displayTimer=0;}
        }
        if(!recording)return;

        if(secondCount<SecondFrames)second[secondCount++]=ms;
        log.Add(ms,cpu,render,gpu,gc);all.Add(ms,cpu,render,gpu,gc);allFrames.Add(ms);
        if(ReadGame(out int wave,out var phase,out int enemies,out int towers,out int speed)&&phase==SessionPhase.Combat)
        {
            if(!rounds.TryGetValue(wave,out var r)){r=new RoundStat();rounds.Add(wave,r);}
            r.Frames.Add(ms);
            if(cpu>0){r.Cpu+=cpu;r.CpuCount++;}
            if(gpu>0){r.Gpu+=gpu;r.GpuCount++;}
            if(enemies>r.MaxEnemies)r.MaxEnemies=enemies;
            if(towers>r.MaxTowers)r.MaxTowers=towers;
            if(speed>r.MaxSpeed)r.MaxSpeed=speed;
        }
        logTimer+=dt;
        if(logTimer>=LogInterval){WriteRow();log.Clear();secondCount=0;logTimer=0;}
    }

    bool ReadGame(out int wave,out SessionPhase phase,out int enemies,out int towers,out int speed)
    {
        if(game==null&&Time.unscaledTime>=nextFind){nextFind=Time.unscaledTime+1;game=FindAnyObjectByType<CoreGuardianPrototype>();}
        if(game!=null&&game.Ready)
        {
            var s=game.Session;wave=s.Wave;phase=s.Phase;enemies=s.Enemies.Count;towers=s.Towers.Count;speed=game.TestSpeed;
            return true;
        }
        wave=0;phase=SessionPhase.Preparation;enemies=towers=0;speed=1;return false;
    }

    // ---------- overlay ----------
    void SetVisible(bool value)
    {
        visible=value;
        if(visible&&canvasRoot==null)BuildUi();
        if(canvasRoot!=null)canvasRoot.SetActive(visible);
        display.Clear();displayTimer=0;skipFrames=2;
        UpdateSampling();
        if(visible)RefreshText();
    }

    void BuildUi()
    {
        canvasRoot=new GameObject("PerfOverlayCanvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));
        canvasRoot.transform.SetParent(transform,false);
        var canvas=canvasRoot.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=5000;
        var scale=canvasRoot.GetComponent<UnityEngine.UI.CanvasScaler>();
        scale.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1920,1080);scale.matchWidthOrHeight=.5f;
        // No GraphicRaycaster: the overlay never blocks clicks.
        var panel=new GameObject("Panel",typeof(RectTransform),typeof(UnityEngine.UI.Image));
        var rect=panel.GetComponent<RectTransform>();rect.SetParent(canvasRoot.transform,false);
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,1);rect.anchoredPosition=new Vector2(-16,-8);rect.sizeDelta=new Vector2(320,130);
        // Same frame as the HUD panels (a plain translucent Image was nearly invisible here).
        var image=panel.GetComponent<UnityEngine.UI.Image>();CoreGuardianHudSkin.Frame(image,"Panel");image.raycastTarget=false;
        var text=new GameObject("Text",typeof(RectTransform),typeof(UnityEngine.UI.Text));
        var textRect=text.GetComponent<RectTransform>();textRect.SetParent(rect,false);
        textRect.anchorMin=Vector2.zero;textRect.anchorMax=Vector2.one;textRect.offsetMin=new Vector2(14,8);textRect.offsetMax=new Vector2(-12,-9);
        label=text.GetComponent<UnityEngine.UI.Text>();
        var font=Resources.Load<Font>("CoreGuardian/Fonts/Galmuri11");
        label.font=font!=null?font:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize=14;label.lineSpacing=1.05f;label.color=new Color(.92f,1f,.9f);label.alignment=TextAnchor.UpperLeft;
        label.raycastTarget=false;label.supportRichText=true;
        label.horizontalOverflow=HorizontalWrapMode.Overflow;label.verticalOverflow=VerticalWrapMode.Overflow;
    }

    void RefreshText()
    {
        if(label==null)return;
        int n=recentCount;Array.Copy(recent,recentSorted,n);Array.Sort(recentSorted,0,n);
        float low=LowFps(recentSorted,n,.01f);
        var w=display;
        sb.Clear();
        sb.Append("FPS ").Append(w.Frames>0?w.Fps.ToString("0",Inv):"-").Append("   1% low ").Append(low>0?low.ToString("0",Inv):"-").Append('\n');
        sb.Append("프레임 ").Append(w.AvgMs.ToString("0.0",Inv)).Append("ms   최대 ").Append(w.Max.ToString("0.0",Inv)).Append("ms\n");
        sb.Append("CPU ").Append(Ms(w.AvgCpu)).Append("  렌더 ").Append(Ms(w.AvgRender)).Append("  GPU ").Append(Ms(w.AvgGpu)).Append(" ms\n");
        sb.Append("GC ").Append(w.GcKb>=0?w.GcKb.ToString("0.0",Inv)+"KB/프레임":"-").Append("   메모리 ").Append(MemoryMb().ToString("0",Inv)).Append("MB\n");
        if(ReadGame(out int wave,out var phase,out int enemies,out int towers,out int speed))
            sb.Append("적 ").Append(enemies).Append("  포탑 ").Append(towers).Append("  라운드 ").Append(wave).Append("  ×").Append(speed).Append('\n');
        else sb.Append("전투 정보 없음\n");
        sb.Append(QualitySettings.vSyncCount>0?"VSync 켬":"<color=#7fd0ff>제한 없음</color>").Append("(F5)  ");
        if(recording)
        {
            int sec=(int)(Time.realtimeSinceStartup-recordStartTime);
            sb.Append("<color=#ff6a6a>REC ").Append((sec/60).ToString("00",Inv)).Append(':').Append((sec%60).ToString("00",Inv)).Append("</color>(F4)");
        }
        else if(notice!=null&&Time.unscaledTime<noticeUntil)sb.Append(notice);
        else sb.Append("기록 F4");
        label.text=sb.ToString();
    }

    static string Ms(double v)=>v>0?v.ToString("0.0",Inv):"-";

    double MemoryMb()
    {
        long bytes=usedMemory.Valid&&usedMemory.Count>0?usedMemory.LastValue:UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        return bytes/1048576.0;
    }

    // Average of the slowest `fraction` of frames, expressed as FPS.
    static float LowFps(float[] ascending,int n,float fraction)
    {
        if(n<=0)return 0;
        int k=Mathf.Max(1,Mathf.CeilToInt(n*fraction));
        double sum=0;for(int i=n-k;i<n;i++)sum+=ascending[i];
        return sum>0?(float)(1000.0*k/sum):0;
    }

    // ---------- measurement mode ----------
    void SetUncapped(bool value)
    {
        if(value==uncapped)return;
        if(value){savedVSync=QualitySettings.vSyncCount;savedTarget=Application.targetFrameRate;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;}
        else{QualitySettings.vSyncCount=savedVSync;Application.targetFrameRate=savedTarget;}
        uncapped=value;
        if(visible)RefreshText();
    }

    void UpdateSampling()
    {
        bool need=visible||recording;
        if(need==sampling)return;
        sampling=need;
        if(need)
        {
            mainThread=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1);
            gcAlloc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
            usedMemory=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"Total Used Memory",1);
            recentCount=recentIndex=0;lastThreadAlloc=0;skipFrames=2;
        }
        else DisposeRecorders();
    }

    void DisposeRecorders(){mainThread.Dispose();gcAlloc.Dispose();usedMemory.Dispose();}

    // ---------- recording ----------
    void StartRecording()
    {
        if(recording)return;
        try
        {
            string dir=Path.Combine(Path.GetDirectoryName(Application.dataPath)??".","PerfLogs");
            Directory.CreateDirectory(dir);
            string stamp=DateTime.Now.ToString("yyyyMMdd_HHmmss",Inv);
            csvPath=Path.Combine(dir,"perf_"+stamp+".csv");summaryPath=Path.Combine(dir,"perf_"+stamp+"_summary.txt");
            csv=new StreamWriter(csvPath,false,new UTF8Encoding(true));
            csv.WriteLine("time_s,round,phase,speed,enemies,towers,uncapped,fps,fps_1pct_low,frame_ms_avg,frame_ms_max,cpu_main_ms,cpu_render_ms,gpu_ms,gc_kb_per_frame,mem_mb");
            csv.Flush();
        }
        catch(Exception e)
        {
            Debug.LogWarning("[Perf] 기록 시작 실패: "+e.Message);
            csv=null;notice="<color=#ff6a6a>기록 실패</color>";noticeUntil=Time.unscaledTime+5;return;
        }
        recording=true;recordStart=DateTime.Now;recordStartTime=Time.realtimeSinceStartup;skipFrames=2;
        allFrames.Clear();all.Clear();rounds.Clear();log.Clear();logTimer=0;secondCount=0;
        UpdateSampling();
        Debug.Log("[Perf] 기록 시작: "+csvPath);
        if(visible)RefreshText();
    }

    void WriteRow()
    {
        var w=log;if(w.Frames==0||csv==null)return;
        Array.Sort(second,0,secondCount);
        float low=LowFps(second,secondCount,.01f);
        ReadGame(out int wave,out var phase,out int enemies,out int towers,out int speed);
        sb.Clear();
        sb.Append((Time.realtimeSinceStartup-recordStartTime).ToString("0.0",Inv)).Append(',')
          .Append(wave).Append(',').Append(phase.ToString()).Append(',').Append(speed).Append(',')
          .Append(enemies).Append(',').Append(towers).Append(',').Append(uncapped?1:0).Append(',')
          .Append(w.Fps.ToString("0.0",Inv)).Append(',').Append(low.ToString("0.0",Inv)).Append(',')
          .Append(w.AvgMs.ToString("0.00",Inv)).Append(',').Append(w.Max.ToString("0.00",Inv)).Append(',')
          .Append(Csv(w.AvgCpu)).Append(',').Append(Csv(w.AvgRender)).Append(',').Append(Csv(w.AvgGpu)).Append(',')
          .Append(w.GcKb>=0?w.GcKb.ToString("0.00",Inv):"").Append(',').Append(MemoryMb().ToString("0",Inv));
        try{csv.WriteLine(sb.ToString());csv.Flush();}
        catch(Exception e){Debug.LogWarning("[Perf] 기록 쓰기 실패: "+e.Message);}
    }

    static string Csv(double v)=>v>0?v.ToString("0.00",Inv):"";

    void StopRecording()
    {
        if(!recording)return;
        if(log.Frames>0)WriteRow();
        recording=false;
        try{csv?.Dispose();}catch{}
        csv=null;
        try{File.WriteAllText(summaryPath,BuildSummary(),new UTF8Encoding(true));}
        catch(Exception e){Debug.LogWarning("[Perf] 요약 저장 실패: "+e.Message);}
        Debug.Log("[Perf] 기록 저장: "+csvPath);
        notice="<color=#9fe89f>저장됨 PerfLogs</color>";noticeUntil=Time.unscaledTime+5;
        UpdateSampling();
        if(visible)RefreshText();
    }

    string BuildSummary()
    {
        var s=new StringBuilder(2048);
        double seconds=Time.realtimeSinceStartup-recordStartTime;
        var frames=allFrames.ToArray();Array.Sort(frames);
        s.AppendLine("코어가디언 성능 기록 요약");
        s.AppendLine("시작: "+recordStart.ToString("yyyy-MM-dd HH:mm:ss",Inv)+"   길이: "+seconds.ToString("0.0",Inv)+"초   프레임: "+frames.Length);
        s.AppendLine("실행: "+(Application.isEditor?"Editor":"Player")+(Debug.isDebugBuild?" (Development)":"")+"   Unity "+Application.unityVersion+"   버전 "+Application.version);
        s.AppendLine("화면: "+Screen.width+"x"+Screen.height+" "+Screen.fullScreenMode+"   주사율 "+Screen.currentResolution.refreshRateRatio.value.ToString("0.##",Inv)+"Hz");
        s.AppendLine("품질: "+QualitySettings.names[QualitySettings.GetQualityLevel()]+"   VSync "+QualitySettings.vSyncCount+"   targetFrameRate "+Application.targetFrameRate+"   제한없음모드 "+(uncapped?"켬":"끔"));
        s.AppendLine("CPU: "+SystemInfo.processorType+" ("+SystemInfo.processorCount+" threads)   RAM "+SystemInfo.systemMemorySize+"MB");
        s.AppendLine("GPU: "+SystemInfo.graphicsDeviceName+"   "+SystemInfo.graphicsDeviceType+"   VRAM "+SystemInfo.graphicsMemorySize+"MB");
        s.AppendLine();
        s.AppendLine("[전체]");
        s.AppendLine("평균 FPS "+(seconds>0?(frames.Length/seconds).ToString("0.0",Inv):"-")
            +"   1% low "+LowFps(frames,frames.Length,.01f).ToString("0.0",Inv)
            +"   0.1% low "+LowFps(frames,frames.Length,.001f).ToString("0.0",Inv)
            +"   최악 프레임 "+(frames.Length>0?frames[frames.Length-1].ToString("0.0",Inv):"-")+"ms");
        s.AppendLine("평균 프레임 "+all.AvgMs.ToString("0.00",Inv)+"ms   CPU 메인 "+Ms(all.AvgCpu)+"ms   렌더 스레드 "+Ms(all.AvgRender)+"ms   GPU "+Ms(all.AvgGpu)+"ms   GC "+(all.GcKb>=0?all.GcKb.ToString("0.0",Inv)+"KB/프레임":"-"));
        s.AppendLine();
        s.AppendLine("[라운드별 - 전투 구간만]");
        s.AppendLine("round,seconds,frames,avg_fps,fps_1pct_low,worst_ms,cpu_main_ms,gpu_ms,max_enemies,max_towers,max_speed");
        var keys=new List<int>(rounds.Keys);keys.Sort();
        foreach(int key in keys)
        {
            var r=rounds[key];var f=r.Frames.ToArray();
            double sum=0;foreach(var x in f)sum+=x;
            Array.Sort(f);
            s.Append(key).Append(',').Append((sum/1000).ToString("0.0",Inv)).Append(',').Append(f.Length).Append(',')
             .Append(sum>0?(f.Length*1000/sum).ToString("0.0",Inv):"").Append(',')
             .Append(LowFps(f,f.Length,.01f).ToString("0.0",Inv)).Append(',')
             .Append(f.Length>0?f[f.Length-1].ToString("0.0",Inv):"").Append(',')
             .Append(r.CpuCount>0?(r.Cpu/r.CpuCount).ToString("0.00",Inv):"").Append(',')
             .Append(r.GpuCount>0?(r.Gpu/r.GpuCount).ToString("0.00",Inv):"").Append(',')
             .Append(r.MaxEnemies).Append(',').Append(r.MaxTowers).Append(',').Append(r.MaxSpeed).AppendLine();
        }
        s.AppendLine();
        s.AppendLine("1% low = 가장 느린 1% 프레임의 평균 시간을 FPS로 바꾼 값. 프레임별 기록은 같은 이름의 .csv(1초 간격).");
        return s.ToString();
    }

    void OnApplicationQuit(){StopRecording();}
    void OnDestroy()
    {
        StopRecording();
        if(uncapped)SetUncapped(false);
        if(sampling){sampling=false;DisposeRecorders();}
        if(instance==this)instance=null;
    }
}
