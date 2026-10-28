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
public sealed partial class CoreGuardianPerfOverlay : MonoBehaviour
{
    const int RecentFrames = 1024,
        SecondFrames = 4096;
    const float DisplayInterval = .5f,
        LogInterval = 1f;
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static CoreGuardianPerfOverlay instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (instance != null)
            return;

        var go = new GameObject("CoreGuardianPerfOverlay");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<CoreGuardianPerfOverlay>();
    }

    bool visible,
        recording,
        uncapped,
        sampling;
    int savedVSync,
        savedTarget = -1;
    readonly FrameTiming[] timings = new FrameTiming[1];
    ProfilerRecorder mainThread,
        gcAlloc,
        usedMemory;
    long lastThreadAlloc;
    readonly float[] recent = new float[RecentFrames],
        recentSorted = new float[RecentFrames],
        second = new float[SecondFrames];
    int recentCount,
        recentIndex,
        secondCount,
        skipFrames;
    Window display,
        log;
    float displayTimer,
        logTimer;
    CoreGuardianPrototype game;
    float nextFind;

    StreamWriter csv;
    string csvPath,
        summaryPath,
        notice;
    float noticeUntil;
    DateTime recordStart;
    float recordStartTime;
    readonly List<float> allFrames = new List<float>(1 << 16);
    Window all;
    readonly Dictionary<int, RoundStat> rounds = new Dictionary<int, RoundStat>();

    GameObject canvasRoot;
    UnityEngine.UI.Text label;
    readonly StringBuilder sb = new StringBuilder(512);

    void Awake()
    {
        foreach (var arg in Environment.GetCommandLineArgs())
        {
            if (string.Equals(arg, "-perf", StringComparison.OrdinalIgnoreCase))
            {
                SetUncapped(true);
            }
            else if (string.Equals(arg, "-perfhud", StringComparison.OrdinalIgnoreCase))
            {
                SetVisible(true);
            }
            else if (string.Equals(arg, "-perflog", StringComparison.OrdinalIgnoreCase))
            {
                StartRecording();
            }
        }
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.f3Key.wasPressedThisFrame)
            {
                SetVisible(!visible);
            }

            if (kb.f4Key.wasPressedThisFrame)
            {
                if (recording)
                {
                    StopRecording();
                }
                else
                {
                    StartRecording();
                }
            }
            if (kb.f5Key.wasPressedThisFrame)
            {
                SetUncapped(!uncapped);
            }
        }
        if (!sampling)
            return;

        Sample();
    }

    void OnApplicationQuit()
    {
        StopRecording();
    }

    void OnDestroy()
    {
        StopRecording();
        if (uncapped)
        {
            SetUncapped(false);
        }

        if (sampling)
        {
            sampling = false;
            DisposeRecorders();
        }
        if (instance == this)
        {
            instance = null;
        }
    }
}
