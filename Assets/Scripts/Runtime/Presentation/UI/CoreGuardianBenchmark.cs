using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CoreGuardian;
using Unity.Profiling;
using UnityEngine;

// 실행 파일 성능 측정 (2026-10-28 사용자 요청). Only runs when the player is started with "-cgbench";
// normal play never creates it. Builds the same scene as the editor measurement (Logs/claude_perf_setup.cs):
// round 28, augments 1~11, 32 towers along the route, combat ×3, VSync off. Waits for ~110 living enemies,
// records 350 frames, writes PerfLogs/bench_*.txt next to the executable and quits.
// "-cgbench-heavy" (2026-10-28 사용자 요청): up to 120 towers within 2 cells of the route, more range towers,
// each attack kind's rate upgraded 10 times — many more projectiles.
public sealed class CoreGuardianBenchmark : MonoBehaviour
{
    const int Frames = 350,
        TargetEnemies = 110;
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StartIfRequested()
    {
        var args = Environment.GetCommandLineArgs();
        bool heavy = args.Contains("-cgbench-heavy");
        if (!Application.isEditor && (heavy || args.Contains("-cgbench")))
        {
            var host = new GameObject("CoreGuardianBenchmark");
            DontDestroyOnLoad(host);
            host.AddComponent<CoreGuardianBenchmark>().heavy = heavy;
        }
    }

    bool heavy;

    IEnumerator Start()
    {
        CoreGuardianPrototype game = null;
        float waitUntil = Time.realtimeSinceStartup + 30;
        while ((game == null || !game.Ready) && Time.realtimeSinceStartup < waitUntil)
        {
            game = FindFirstObjectByType<CoreGuardianPrototype>();
            yield return null;
        }
        if (game == null || !game.Ready)
        {
            Finish("game not ready");
            yield break;
        }

        yield return new WaitForSecondsRealtime(1);
        string setup = Setup(game, heavy);
        var s = game.Session;
        waitUntil = Time.realtimeSinceStartup + 60;
        while (Living(s) < TargetEnemies && s.Phase == SessionPhase.Combat && Time.realtimeSinceStartup < waitUntil)
        {
            yield return null;
        }

        if (heavy)
        {
            // Let the crowd spread along the whole route so most towers are firing (about 15 combat seconds).
            float spread = Time.realtimeSinceStartup + 5;
            while (Time.realtimeSinceStartup < spread && s.Phase == SessionPhase.Combat)
            {
                yield return null;
            }
        }

        int enemiesAtStart = Living(s);
        int launched = 0,
            hits = 0;
        s.ProjectileLaunched += _ => launched++;
        s.DamageApplied += (_, _) => hits++;
        var frameMs = new float[Frames];
        var mainMs = new double[Frames];
        var gcBytes = new long[Frames];
        var projectiles = new int[Frames];
        using var main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
        using var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
        yield return null; // recorders deliver from the next completed frame
        for (int i = 0; i < Frames; i++)
        {
            yield return null;
            frameMs[i] = Time.unscaledDeltaTime * 1000;
            mainMs[i] = main.Valid && main.Count > 0 ? main.LastValue * 1e-6 : -1;
            gcBytes[i] = gc.Valid && gc.Count > 0 ? gc.LastValue : -1;
            projectiles[i] = s.Projectiles.Count;
        }

        var sorted = frameMs.OrderByDescending(x => x).ToArray();
        float onePercentWorst = sorted.Take(Mathf.Max(1, Frames / 100)).Average();
        double total = frameMs.Sum();
        var sb = new StringBuilder();
        sb.AppendLine("CoreGuardian benchmark " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", Inv));
        sb.AppendLine(setup);
        sb.AppendLine("screen " + Screen.width + "x" + Screen.height + " fullscreen " + Screen.fullScreenMode);
        sb.AppendLine(
            "enemies " + enemiesAtStart + " -> " + Living(s) + ", towers " + s.Towers.Count + ", phase " + s.Phase
        );
        sb.AppendLine(
            "shots launched "
                + launched
                + " ("
                + (launched * 1000 / total).ToString("0", Inv)
                + "/s), hits "
                + hits
                + " ("
                + (hits * 1000 / total).ToString("0", Inv)
                + "/s)"
        );
        sb.AppendLine(
            "projectiles in flight avg " + projectiles.Average().ToString("0", Inv) + " max " + projectiles.Max()
        );
        sb.AppendLine("FPS " + (Frames * 1000 / total).ToString("0.0", Inv));
        sb.AppendLine("1% low FPS " + (1000 / onePercentWorst).ToString("0.0", Inv));
        sb.AppendLine(
            "frame ms avg " + (total / Frames).ToString("0.00", Inv) + " max " + sorted[0].ToString("0.0", Inv)
        );
        sb.AppendLine(
            "main thread ms avg "
                + (mainMs[0] >= 0 ? mainMs.Average().ToString("0.00", Inv) : "n/a")
                + " max "
                + (mainMs[0] >= 0 ? mainMs.Max().ToString("0.0", Inv) : "n/a")
        );
        sb.AppendLine(
            "GC KB/frame avg "
                + (gcBytes[0] >= 0 ? (gcBytes.Average() / 1024).ToString("0.00", Inv) : "n/a")
                + " max "
                + (gcBytes[0] >= 0 ? (gcBytes.Max() / 1024.0).ToString("0.0", Inv) : "n/a")
                + ", frames over 1MB "
                + gcBytes.Count(b => b > 1024 * 1024)
        );
        Finish(sb.ToString());
    }

    // Same steps and order as Logs/claude_perf_setup.cs (editor measurement).
    static string Setup(CoreGuardianPrototype g, bool heavy)
    {
        var s = g.Session;
        s.DebugSetWave(28);
        s.DebugAddCrystals(1000000);
        s.DebugAddGold(1000000);
        for (int n = 1; n <= 11; n++)
        {
            s.DebugGrantAugmentation(n);
        }

        var kinds = heavy
            ? new[]
            {
                TowerKind.Single,
                TowerKind.Multi,
                TowerKind.Multi,
                TowerKind.Frost,
                TowerKind.Flame,
                TowerKind.Rail,
            }
            : new[] { TowerKind.Single, TowerKind.Multi, TowerKind.Frost, TowerKind.Flame, TowerKind.Rail };
        int limit = heavy ? 120 : 32,
            reach = heavy ? 2 : 1,
            placed = 0,
            k = 0;
        var size = g.BoardSize;
        for (int y = 0; y < size.y && placed < limit; y++)
        {
            for (int x = 0; x < size.x && placed < limit; x++)
            {
                var c = new Vector2Int(x, y);
                if (!s.IsBuildable(c))
                    continue;
                bool nearPath = false;
                for (int dx = -reach; dx <= reach; dx++)
                {
                    for (int dy = -reach; dy <= reach; dy++)
                    {
                        var n = c + new Vector2Int(dx, dy);
                        bool inside = n.x >= 0 && n.y >= 0 && n.x < size.x && n.y < size.y;
                        if (inside && !s.IsBuildable(n) && !s.Towers.Any(t => t.Position == n))
                        {
                            nearPath = true;
                        }
                    }
                }
                if (!nearPath)
                    continue;
                var kind =
                    heavy ? (placed % 15 == 14 ? TowerKind.Support : kinds[k++ % kinds.Length])
                    : placed >= 30 ? TowerKind.Support
                    : kinds[k++ % kinds.Length];
                if (g.Place(kind, c))
                {
                    placed++;
                }
            }
        }
        int upgrades = 0;
        if (heavy)
        {
            foreach (
                var kind in new[]
                {
                    TowerKind.Single,
                    TowerKind.Multi,
                    TowerKind.Frost,
                    TowerKind.Flame,
                    TowerKind.Rail,
                }
            )
            {
                for (int i = 0; i < 10 && s.UpgradeTowerKind(kind, "rate", out _); i++)
                {
                    upgrades++;
                }
            }
        }
        while (g.TestSpeed != 3)
        {
            g.CycleTestSpeed();
        }

        s.StartCombat();
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        return (heavy ? "HEAVY: " : "")
            + "wave "
            + s.Wave
            + ", towers "
            + s.Towers.Count
            + ", rate upgrades "
            + upgrades
            + ", speed x"
            + g.TestSpeed
            + ", vsync off";
    }

    static int Living(Session s)
    {
        int n = 0;
        foreach (var e in s.Enemies)
        {
            if (e.Alive)
            {
                n++;
            }
        }
        return n;
    }

    static void Finish(string text)
    {
        try
        {
            string dir = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "PerfLogs");
            Directory.CreateDirectory(dir);
            File.WriteAllText(
                Path.Combine(dir, "bench_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", Inv) + ".txt"),
                text,
                new UTF8Encoding(false)
            );
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Bench] " + e.Message);
        }
        Application.Quit();
    }
}
