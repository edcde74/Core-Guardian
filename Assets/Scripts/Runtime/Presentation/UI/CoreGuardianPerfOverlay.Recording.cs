using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CoreGuardian;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;

// CoreGuardianPerfOverlay: CSV recording and round summary.
public sealed partial class CoreGuardianPerfOverlay
{
    sealed class RoundStat
    {
        public readonly List<float> Frames = new List<float>(4096);
        public double Cpu,
            Gpu;
        public int CpuCount,
            GpuCount,
            MaxEnemies,
            MaxTowers,
            MaxSpeed;
    }

    readonly Stack<RoundStat> roundPool = new Stack<RoundStat>();

    // ---------- recording ----------
    void StartRecording()
    {
        if (recording)
            return;

        try
        {
            string dir = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "PerfLogs");
            Directory.CreateDirectory(dir);
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", Inv);
            csvPath = Path.Combine(dir, "perf_" + stamp + ".csv");
            summaryPath = Path.Combine(dir, "perf_" + stamp + "_summary.txt");
            csv = new StreamWriter(csvPath, false, new UTF8Encoding(true));
            csv.WriteLine(
                "time_s,round,phase,speed,enemies,towers,uncapped,fps,fps_1pct_low,frame_ms_avg,frame_ms_max,cpu_main_ms,cpu_render_ms,gpu_ms,gc_kb_per_frame,mem_mb"
            );
            csv.Flush();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Perf] 기록 시작 실패: " + e.Message);
            csv = null;
            notice = "<color=#ff6a6a>기록 실패</color>";
            noticeUntil = Time.unscaledTime + 5;
            return;
        }
        recording = true;
        recordStart = DateTime.Now;
        recordStartTime = Time.realtimeSinceStartup;
        skipFrames = 2;
        allFrames.Clear();
        all.Clear();
        foreach (var round in rounds.Values)
        {
            round.Frames.Clear();
            round.Cpu = round.Gpu = 0;
            round.CpuCount = round.GpuCount = round.MaxEnemies = round.MaxTowers = round.MaxSpeed = 0;
            roundPool.Push(round);
        }
        rounds.Clear();
        log.Clear();
        logTimer = 0;
        secondCount = 0;
        UpdateSampling();
        Debug.Log("[Perf] 기록 시작: " + csvPath);
        if (visible)
        {
            RefreshText();
        }
    }

    void WriteRow()
    {
        var w = log;
        if (w.Frames == 0 || csv == null)
            return;

        Array.Sort(second, 0, secondCount);
        float low = LowFps(second, secondCount, .01f);
        ReadGame(out int wave, out var phase, out int enemies, out int towers, out int speed);
        sb.Clear();
        sb.Append((Time.realtimeSinceStartup - recordStartTime).ToString("0.0", Inv))
            .Append(',')
            .Append(wave)
            .Append(',')
            .Append(phase.ToString())
            .Append(',')
            .Append(speed)
            .Append(',')
            .Append(enemies)
            .Append(',')
            .Append(towers)
            .Append(',')
            .Append(uncapped ? 1 : 0)
            .Append(',')
            .Append(w.Fps.ToString("0.0", Inv))
            .Append(',')
            .Append(low.ToString("0.0", Inv))
            .Append(',')
            .Append(w.AvgMs.ToString("0.00", Inv))
            .Append(',')
            .Append(w.Max.ToString("0.00", Inv))
            .Append(',')
            .Append(Csv(w.AvgCpu))
            .Append(',')
            .Append(Csv(w.AvgRender))
            .Append(',')
            .Append(Csv(w.AvgGpu))
            .Append(',')
            .Append(w.GcKb >= 0 ? w.GcKb.ToString("0.00", Inv) : "")
            .Append(',')
            .Append(MemoryMb().ToString("0", Inv));
        try
        {
            csv.Write(sb); // F13: no intermediate row string
            csv.WriteLine();
            csv.Flush();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Perf] 기록 쓰기 실패: " + e.Message);
        }
    }

    static string Csv(double v) => v > 0 ? v.ToString("0.00", Inv) : "";

    void StopRecording()
    {
        if (!recording)
            return;

        if (log.Frames > 0)
        {
            WriteRow();
        }

        recording = false;
        try
        {
            csv?.Dispose();
        }
        catch (Exception)
        {
            // Closing a broken log file must not stop the game; the summary below still reports the round.
        }
        csv = null;
        try
        {
            File.WriteAllText(summaryPath, BuildSummary(), new UTF8Encoding(true));
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Perf] 요약 저장 실패: " + e.Message);
        }
        Debug.Log("[Perf] 기록 저장: " + csvPath);
        notice = "<color=#9fe89f>저장됨 PerfLogs</color>";
        noticeUntil = Time.unscaledTime + 5;
        UpdateSampling();
        if (visible)
        {
            RefreshText();
        }
    }

    string BuildSummary()
    {
        var s = new StringBuilder(2048);
        double seconds = Time.realtimeSinceStartup - recordStartTime;
        var frames = allFrames.ToArray();
        Array.Sort(frames);
        s.AppendLine("코어가디언 성능 기록 요약");
        s.AppendLine(
            "시작: "
                + recordStart.ToString("yyyy-MM-dd HH:mm:ss", Inv)
                + "   길이: "
                + seconds.ToString("0.0", Inv)
                + "초   프레임: "
                + frames.Length
        );
        s.AppendLine(
            "실행: "
                + (Application.isEditor ? "Editor" : "Player")
                + (Debug.isDebugBuild ? " (Development)" : "")
                + "   Unity "
                + Application.unityVersion
                + "   버전 "
                + Application.version
        );
        s.AppendLine(
            "화면: "
                + Screen.width
                + "x"
                + Screen.height
                + " "
                + Screen.fullScreenMode
                + "   주사율 "
                + Screen.currentResolution.refreshRateRatio.value.ToString("0.##", Inv)
                + "Hz"
        );
        s.AppendLine(
            "품질: "
                + QualitySettings.names[QualitySettings.GetQualityLevel()]
                + "   VSync "
                + QualitySettings.vSyncCount
                + "   targetFrameRate "
                + Application.targetFrameRate
                + "   제한없음모드 "
                + (uncapped ? "켬" : "끔")
        );
        s.AppendLine(
            "CPU: "
                + SystemInfo.processorType
                + " ("
                + SystemInfo.processorCount
                + " threads)   RAM "
                + SystemInfo.systemMemorySize
                + "MB"
        );
        s.AppendLine(
            "GPU: "
                + SystemInfo.graphicsDeviceName
                + "   "
                + SystemInfo.graphicsDeviceType
                + "   VRAM "
                + SystemInfo.graphicsMemorySize
                + "MB"
        );
        s.AppendLine();
        s.AppendLine("[전체]");
        s.AppendLine(
            "평균 FPS "
                + (seconds > 0 ? (frames.Length / seconds).ToString("0.0", Inv) : "-")
                + "   1% low "
                + LowFps(frames, frames.Length, .01f).ToString("0.0", Inv)
                + "   0.1% low "
                + LowFps(frames, frames.Length, .001f).ToString("0.0", Inv)
                + "   최악 프레임 "
                + (frames.Length > 0 ? frames[frames.Length - 1].ToString("0.0", Inv) : "-")
                + "ms"
        );
        s.AppendLine(
            "평균 프레임 "
                + all.AvgMs.ToString("0.00", Inv)
                + "ms   CPU 메인 "
                + Ms(all.AvgCpu)
                + "ms   렌더 스레드 "
                + Ms(all.AvgRender)
                + "ms   GPU "
                + Ms(all.AvgGpu)
                + "ms   GC "
                + (all.GcKb >= 0 ? all.GcKb.ToString("0.0", Inv) + "KB/프레임" : "-")
        );
        s.AppendLine();
        s.AppendLine("[라운드별 - 전투 구간만]");
        s.AppendLine(
            "round,seconds,frames,avg_fps,fps_1pct_low,worst_ms,cpu_main_ms,gpu_ms,max_enemies,max_towers,max_speed"
        );
        var keys = new List<int>(rounds.Keys);
        keys.Sort();
        foreach (int key in keys)
        {
            var r = rounds[key];
            var f = r.Frames.ToArray();
            double sum = 0;
            foreach (var x in f)
            {
                sum += x;
            }

            Array.Sort(f);
            s.Append(key)
                .Append(',')
                .Append((sum / 1000).ToString("0.0", Inv))
                .Append(',')
                .Append(f.Length)
                .Append(',')
                .Append(sum > 0 ? (f.Length * 1000 / sum).ToString("0.0", Inv) : "")
                .Append(',')
                .Append(LowFps(f, f.Length, .01f).ToString("0.0", Inv))
                .Append(',')
                .Append(f.Length > 0 ? f[f.Length - 1].ToString("0.0", Inv) : "")
                .Append(',')
                .Append(r.CpuCount > 0 ? (r.Cpu / r.CpuCount).ToString("0.00", Inv) : "")
                .Append(',')
                .Append(r.GpuCount > 0 ? (r.Gpu / r.GpuCount).ToString("0.00", Inv) : "")
                .Append(',')
                .Append(r.MaxEnemies)
                .Append(',')
                .Append(r.MaxTowers)
                .Append(',')
                .Append(r.MaxSpeed)
                .AppendLine();
        }
        s.AppendLine();
        s.AppendLine(
            "1% low = 가장 느린 1% 프레임의 평균 시간을 FPS로 바꾼 값. 프레임별 기록은 같은 이름의 .csv(1초 간격)."
        );
        return s.ToString();
    }
}
