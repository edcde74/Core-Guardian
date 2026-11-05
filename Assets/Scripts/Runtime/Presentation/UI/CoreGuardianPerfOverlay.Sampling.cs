using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CoreGuardian;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;

// CoreGuardianPerfOverlay: frame sampling and profiler recorders.
public sealed partial class CoreGuardianPerfOverlay
{
    struct Window
    {
        public int Frames,
            CpuCount,
            RenderCount,
            GpuCount,
            GcCount;
        public double Sum,
            Cpu,
            Render,
            Gpu,
            Gc;
        public float Max;

        public void Clear()
        {
            this = default;
        }

        public void Add(float ms, double cpu, double render, double gpu, long gc)
        {
            Frames++;
            Sum += ms;
            if (ms > Max)
            {
                Max = ms;
            }

            if (cpu > 0)
            {
                Cpu += cpu;
                CpuCount++;
            }
            if (render > 0)
            {
                Render += render;
                RenderCount++;
            }
            if (gpu > 0)
            {
                Gpu += gpu;
                GpuCount++;
            }
            if (gc >= 0)
            {
                Gc += gc;
                GcCount++;
            }
        }

        public double Fps => Sum > 0 ? Frames * 1000.0 / Sum : 0;
        public double AvgMs => Frames > 0 ? Sum / Frames : 0;
        public double AvgCpu => CpuCount > 0 ? Cpu / CpuCount : -1;
        public double AvgRender => RenderCount > 0 ? Render / RenderCount : -1;
        public double AvgGpu => GpuCount > 0 ? Gpu / GpuCount : -1;
        public double GcKb => GcCount > 0 ? Gc / GcCount / 1024.0 : -1;
    }

    void Sample()
    {
        // The first frames after (re)starting carry time from before the start (loading, toggling).
        if (skipFrames > 0)
        {
            skipFrames--;
            lastThreadAlloc = 0;
            return;
        }
        float dt = Time.unscaledDeltaTime,
            ms = dt * 1000f;
        double cpu = -1,
            render = -1,
            gpu = -1;
        FrameTimingManager.CaptureFrameTimings();
        if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
        {
            var t = timings[0];
            // Main-thread work without the wait for Present/VSync.
            cpu = t.cpuMainThreadFrameTime - Math.Max(0, t.cpuMainThreadPresentWaitTime);
            render = t.cpuRenderThreadFrameTime;
            gpu = t.gpuFrameTime;
        }
        if (cpu <= 0 && mainThread.Valid && mainThread.Count > 0)
        {
            cpu = mainThread.LastValue * 1e-6;
        }

        long gc = -1;
        if (gcAlloc.Valid && gcAlloc.Count > 0)
        {
            gc = gcAlloc.LastValue;
        }
        else
        {
            long now = GC.GetAllocatedBytesForCurrentThread();
            if (now > 0 && lastThreadAlloc > 0 && now >= lastThreadAlloc)
            {
                gc = now - lastThreadAlloc;
            }

            lastThreadAlloc = now;
        }

        recent[recentIndex] = ms;
        recentIndex = (recentIndex + 1) % RecentFrames;
        if (recentCount < RecentFrames)
        {
            recentCount++;
        }

        display.Add(ms, cpu, render, gpu, gc);
        if (visible)
        {
            displayTimer += dt;
            if (displayTimer >= DisplayInterval)
            {
                RefreshText();
                display.Clear();
                displayTimer = 0;
            }
        }
        if (!recording)
            return;

        if (secondCount < SecondFrames)
        {
            second[secondCount++] = ms;
        }

        log.Add(ms, cpu, render, gpu, gc);
        all.Add(ms, cpu, render, gpu, gc);
        allFrames.Add(ms);
        if (
            ReadGame(out int wave, out var phase, out int enemies, out int towers, out int speed)
            && phase == SessionPhase.Combat
        )
        {
            if (!rounds.TryGetValue(wave, out var r))
            {
                // F13: round buffers are reused across recordings so the recorder does not dirty its own GC figure.
                r = roundPool.Count > 0 ? roundPool.Pop() : new RoundStat();
                rounds.Add(wave, r);
            }
            r.Frames.Add(ms);
            if (cpu > 0)
            {
                r.Cpu += cpu;
                r.CpuCount++;
            }
            if (gpu > 0)
            {
                r.Gpu += gpu;
                r.GpuCount++;
            }
            if (enemies > r.MaxEnemies)
            {
                r.MaxEnemies = enemies;
            }

            if (towers > r.MaxTowers)
            {
                r.MaxTowers = towers;
            }

            if (speed > r.MaxSpeed)
            {
                r.MaxSpeed = speed;
            }
        }
        logTimer += dt;
        if (logTimer >= LogInterval)
        {
            WriteRow();
            log.Clear();
            secondCount = 0;
            logTimer = 0;
        }
    }

    bool ReadGame(out int wave, out SessionPhase phase, out int enemies, out int towers, out int speed)
    {
        if (game == null && Time.unscaledTime >= nextFind)
        {
            nextFind = Time.unscaledTime + 1;
            game = FindAnyObjectByType<CoreGuardianPrototype>();
        }
        if (game != null && game.Ready)
        {
            var s = game.Session;
            wave = s.Wave;
            phase = s.Phase;
            enemies = s.Enemies.Count;
            towers = s.Towers.Count;
            speed = game.TestSpeed;
            return true;
        }
        wave = 0;
        phase = SessionPhase.Preparation;
        enemies = towers = 0;
        speed = 1;
        return false;
    }

    static string Ms(double v) => v > 0 ? v.ToString("0.0", Inv) : "-";

    double MemoryMb()
    {
        long bytes =
            usedMemory.Valid && usedMemory.Count > 0
                ? usedMemory.LastValue
                : UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        return bytes / 1048576.0;
    }

    // Average of the slowest `fraction` of frames, expressed as FPS.
    static float LowFps(float[] ascending, int n, float fraction)
    {
        if (n <= 0)
            return 0;

        int k = Mathf.Max(1, Mathf.CeilToInt(n * fraction));
        double sum = 0;
        for (int i = n - k; i < n; i++)
        {
            sum += ascending[i];
        }

        return sum > 0 ? (float)(1000.0 * k / sum) : 0;
    }

    // ---------- measurement mode ----------
    void SetUncapped(bool value)
    {
        if (value == uncapped)
            return;

        if (value)
        {
            savedVSync = QualitySettings.vSyncCount;
            savedTarget = Application.targetFrameRate;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
        }
        else
        {
            QualitySettings.vSyncCount = savedVSync;
            Application.targetFrameRate = savedTarget;
        }
        uncapped = value;
        if (visible)
        {
            RefreshText();
        }
    }

    void UpdateSampling()
    {
        bool need = visible || recording;
        if (need == sampling)
            return;

        sampling = need;
        if (need)
        {
            mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
            gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            usedMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory", 1);
            recentCount = recentIndex = 0;
            lastThreadAlloc = 0;
            skipFrames = 2;
        }
        else
        {
            DisposeRecorders();
        }
    }

    void DisposeRecorders()
    {
        mainThread.Dispose();
        gcAlloc.Dispose();
        usedMemory.Dispose();
    }
}
