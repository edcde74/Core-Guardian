using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CoreGuardian;
using UnityEngine;

// 라운드 기록 (2026-10-30 사용자 요청, 밸런싱용). When a round ends or the core falls, one line goes to
// <exe folder>/PerfLogs/rounds_*.csv (editor: <project>/PerfLogs): result, game seconds, core left and
// how many of each enemy reached the core. One file per play session; nothing is shown in normal play.
public sealed class CoreGuardianRoundLog : IDisposable
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    readonly Session session;
    readonly int[] enemyIds;
    readonly string path;
    bool started;

    public string LastSummary { get; private set; } = "직전 라운드 코어 도착: 기록 없음";

    public string FilePath => path;

    public CoreGuardianRoundLog(Session session)
    {
        this.session = session;
        enemyIds = session.Data.EnemiesById.Keys.OrderBy(id => id).ToArray();
        string dir = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "PerfLogs");
        path = Path.Combine(dir, "rounds_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", Inv) + ".csv");
        session.RoundCompleted += OnRoundCompleted;
        session.VictoryReached += OnVictory;
        session.DefeatReached += OnDefeat;
    }

    void OnRoundCompleted() => Record(session.CompletedWave, "완료");

    void OnVictory() => Record(session.CompletedWave, "완료");

    void OnDefeat() => Record(session.Wave, "패배");

    void Record(int round, string result)
    {
        var reaches = session.CoreReachesThisRound;
        int total = reaches.Values.Sum();
        float seconds = session.RoundCombatSeconds;
        var parts = new List<string>();
        foreach (int id in enemyIds)
        {
            if (reaches.TryGetValue(id, out int n) && n > 0)
            {
                parts.Add(EnemyNames.Of(session.Data.EnemiesById[id]) + " " + n);
            }
        }

        LastSummary =
            "직전 라운드 코어 도착("
            + round
            + "라운드 "
            + result
            + ", "
            + Clock(seconds)
            + "): "
            + (parts.Count == 0 ? "없음" : string.Join(", ", parts));

        var line = new StringBuilder();
        if (!started)
        {
            line.Append("기록 시각,라운드,결과,걸린 시간(초),코어 남음,코어 최대,도착 합계");
            foreach (int id in enemyIds)
            {
                line.Append(',').Append(EnemyNames.Of(session.Data.EnemiesById[id]));
            }
            line.Append('\n');
        }

        line.Append(DateTime.Now.ToString("HH:mm:ss", Inv))
            .Append(',')
            .Append(round)
            .Append(',')
            .Append(result)
            .Append(',')
            .Append(seconds.ToString("0.0", Inv))
            .Append(',')
            .Append(session.CoreEnergy)
            .Append(',')
            .Append(session.CoreMaximum)
            .Append(',')
            .Append(total);
        foreach (int id in enemyIds)
        {
            reaches.TryGetValue(id, out int n);
            line.Append(',').Append(n);
        }
        line.Append('\n');

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            File.AppendAllText(path, line.ToString(), new UTF8Encoding(true));
            started = true;
        }
        catch (Exception e)
        {
            // A failed log write must never stop the game.
            Debug.LogWarning("라운드 기록을 저장하지 못했습니다: " + e.Message);
        }
    }

    static string Clock(float seconds)
    {
        int s = Mathf.Max(0, Mathf.RoundToInt(seconds));
        return s >= 60 ? s / 60 + "분 " + s % 60 + "초" : s + "초";
    }

    public void Dispose()
    {
        session.RoundCompleted -= OnRoundCompleted;
        session.VictoryReached -= OnVictory;
        session.DefeatReached -= OnDefeat;
    }
}
