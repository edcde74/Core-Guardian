using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CoreGuardian;
using UnityEngine;

// 플레이 기록 (2026-10-30 사용자 요청, 밸런싱용). At each round end (and on defeat/victory) rows are appended to
// four csv files in <exe folder>/PerfLogs/play_<시각>/ (editor: <project>/PerfLogs/play_<시각>/):
//   포탑.csv  — every tower: kind, cell, star level, lab upgrade steps, damage this round / total / to bosses
//   피해.csv  — every damage source (tower, 기술병 attack, 기술병 skill, …) × enemy type, cumulative
//   증강.csv  — augment cards picked, in order
//   강화.csv  — 기술병 and per-tower-kind upgrade steps, gold and crystals left
// Damage is the health actually removed (overkill excluded, after defense and weakness). Excel opens the files
// directly (UTF-8 with BOM). One folder per play session; nothing is shown in normal play.
public sealed class CoreGuardianPlayLog : IDisposable
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static readonly TowerKind[] Kinds = (TowerKind[])Enum.GetValues(typeof(TowerKind));
    const string Other = "기타";

    sealed class SourceRow
    {
        public string Key;
        public int TowerId;
        public long[] Total;
        public long Round,
            All,
            Boss;
    }

    sealed class TowerInfo
    {
        public int Id,
            Stars,
            Spent;
        public TowerKind Kind;
        public Vector2Int Cell;
        public bool Gone;
    }

    readonly Session session;
    readonly string folder;
    readonly List<string> enemyColumns = new List<string>();
    readonly Dictionary<string, int> enemyIndex = new Dictionary<string, int>();
    readonly Dictionary<string, SourceRow> sources = new Dictionary<string, SourceRow>();
    readonly List<SourceRow> sourceOrder = new List<SourceRow>();
    readonly Dictionary<int, TowerInfo> towers = new Dictionary<int, TowerInfo>();
    readonly HashSet<string> headerWritten = new HashSet<string>();
    int augmentsWritten;

    public string Folder => folder;

    public CoreGuardianPlayLog(Session session, string directory = null)
    {
        this.session = session;
        string root = directory ?? Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "PerfLogs");
        folder = Path.Combine(root, "play_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", Inv));

        foreach (var spec in session.Data.EnemiesById.OrderBy(p => p.Key).Select(p => p.Value))
        {
            AddColumn(EnemyNames.Of(spec));
        }
        AddColumn(EnemyNames.Kind(EnemyKind.SplitChild));
        AddColumn(Other);

        session.DamageDealt += OnDamage;
        session.CombatStarted += OnCombatStarted;
        session.RoundCompleted += OnRoundCompleted;
        session.VictoryReached += OnVictory;
        session.DefeatReached += OnDefeat;
    }

    void AddColumn(string name)
    {
        if (!enemyIndex.ContainsKey(name))
        {
            enemyIndex[name] = enemyColumns.Count;
            enemyColumns.Add(name);
        }
    }

    void OnDamage(EnemySpec spec, string source, int amount)
    {
        if (amount <= 0)
            return;

        string key = source ?? "?";
        if (!sources.TryGetValue(key, out var row))
        {
            row = AddSource(key);
        }

        if (!enemyIndex.TryGetValue(EnemyNames.Of(spec), out int column))
        {
            column = enemyIndex[Other];
        }

        row.Total[column] += amount;
        row.Round += amount;
        row.All += amount;
        if (spec != null && spec.Kind == EnemyKind.Boss)
        {
            row.Boss += amount;
        }
    }

    SourceRow AddSource(string key)
    {
        var row = new SourceRow
        {
            Key = key,
            TowerId = TowerIdOf(key),
            Total = new long[enemyColumns.Count],
        };
        sources[key] = row;
        sourceOrder.Add(row);
        if (row.TowerId != 0)
        {
            foreach (var t in session.Towers)
            {
                if (t.Id == row.TowerId)
                {
                    Remember(t);
                    break;
                }
            }
        }
        return row;
    }

    static int TowerIdOf(string key)
    {
        int colon = key.IndexOf(':');
        if (colon < 0)
            return 0;

        string head = key.Substring(0, colon);
        if (head != "Tower" && head != "Support")
            return 0;

        return int.TryParse(key.Substring(colon + 1), NumberStyles.Integer, Inv, out int id) ? id : 0;
    }

    void Remember(TowerState t)
    {
        if (!towers.TryGetValue(t.Id, out var info))
        {
            towers[t.Id] = info = new TowerInfo { Id = t.Id };
        }
        info.Kind = t.Kind;
        info.Cell = t.Position;
        info.Stars = t.Stars;
        info.Spent = t.Spent;
        info.Gone = false;
    }

    void SyncTowers()
    {
        foreach (var info in towers.Values)
        {
            info.Gone = true;
        }
        foreach (var t in session.Towers)
        {
            Remember(t);
        }
    }

    void OnCombatStarted()
    {
        SyncTowers();
        WriteNewAugments(session.Wave);
    }

    void OnRoundCompleted() => Record(session.CompletedWave, "완료");

    void OnVictory() => Record(session.CompletedWave, "완료");

    void OnDefeat() => Record(session.Wave, "패배");

    /// <summary>Appends this moment's rows to all four files (round ends call it; tests may too).</summary>
    public void Record(int round, string result)
    {
        SyncTowers();
        WriteNewAugments(session.Wave);
        string time = DateTime.Now.ToString("HH:mm:ss", Inv);
        WriteTowers(time, round, result);
        WriteDamage(time, round, result);
        WriteUpgrades(time, round, result);
        foreach (var row in sourceOrder)
        {
            row.Round = 0;
        }
    }

    void WriteTowers(string time, int round, string result)
    {
        var body = new StringBuilder();
        foreach (var info in towers.Values.OrderBy(t => t.Id))
        {
            long now = 0,
                all = 0,
                boss = 0;
            foreach (var row in sourceOrder)
            {
                if (row.TowerId == info.Id)
                {
                    now += row.Round;
                    all += row.All;
                    boss += row.Boss;
                }
            }

            body.Append(time)
                .Append(',')
                .Append(round)
                .Append(',')
                .Append(result)
                .Append(',')
                .Append(info.Id)
                .Append(',')
                .Append(TowerName(info.Kind))
                .Append(',')
                .Append(info.Cell.x)
                .Append(',')
                .Append(info.Cell.y)
                .Append(',')
                .Append(info.Stars)
                .Append(',')
                .Append(info.Spent)
                .Append(',')
                .Append(info.Gone ? "없음(합체·철거)" : "설치됨")
                .Append(',')
                .Append(TowerSteps(info.Kind, out _))
                .Append(',')
                .Append(now)
                .Append(',')
                .Append(all)
                .Append(',')
                .Append(boss)
                .Append('\n');
        }

        Append(
            "포탑.csv",
            "기록 시각,라운드,결과,포탑 번호,종류,칸 X,칸 Y,합체 별,설치 비용,상태,강화(같은 종류 공통),이번 라운드 피해,누적 피해,누적 보스 피해",
            body
        );
    }

    void WriteDamage(string time, int round, string result)
    {
        var body = new StringBuilder();
        foreach (var row in sourceOrder)
        {
            towers.TryGetValue(row.TowerId, out var info);
            body.Append(time)
                .Append(',')
                .Append(round)
                .Append(',')
                .Append(result)
                .Append(',')
                .Append(SourceName(row))
                .Append(',')
                .Append(info != null ? TowerName(info.Kind) : "")
                .Append(',')
                .Append(info != null ? info.Cell.x.ToString(Inv) : "")
                .Append(',')
                .Append(info != null ? info.Cell.y.ToString(Inv) : "")
                .Append(',')
                .Append(row.Round)
                .Append(',')
                .Append(row.All)
                .Append(',')
                .Append(row.Boss);
            foreach (long n in row.Total)
            {
                body.Append(',').Append(n);
            }
            body.Append('\n');
        }

        var header = new StringBuilder(
            "기록 시각,라운드,결과,출처,포탑 종류,칸 X,칸 Y,이번 라운드 피해,누적 피해,누적 보스 피해"
        );
        foreach (string name in enemyColumns)
        {
            header.Append(',').Append(name).Append(" 누적");
        }
        Append("피해.csv", header.ToString(), body);
    }

    void WriteUpgrades(string time, int round, string result)
    {
        var body = new StringBuilder();
        int playerSteps = 0;
        var parts = new List<string>();
        foreach (var pair in session.Player.UpgradeSteps.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (pair.Value > 0)
            {
                playerSteps += pair.Value;
                parts.Add(StatName(pair.Key) + " " + pair.Value);
            }
        }
        UpgradeRow(
            body,
            time,
            round,
            result,
            "기술병",
            playerSteps,
            parts.Count == 0 ? "없음" : string.Join(" · ", parts)
        );

        foreach (var kind in Kinds)
        {
            string text = TowerSteps(kind, out int steps);
            UpgradeRow(body, time, round, result, TowerName(kind), steps, text);
        }

        Append("강화.csv", "기록 시각,라운드,결과,대상,강화 횟수 합계,내용,남은 골드,남은 수정", body);
    }

    void UpgradeRow(StringBuilder body, string time, int round, string result, string target, int steps, string text)
    {
        body.Append(time)
            .Append(',')
            .Append(round)
            .Append(',')
            .Append(result)
            .Append(',')
            .Append(target)
            .Append(',')
            .Append(steps)
            .Append(',')
            .Append(text)
            .Append(',')
            .Append(session.Gold)
            .Append(',')
            .Append(session.Crystals)
            .Append('\n');
    }

    void WriteNewAugments(int round)
    {
        var picked = session.Augments.Selected;
        if (picked.Count <= augmentsWritten)
            return;

        var body = new StringBuilder();
        string time = DateTime.Now.ToString("HH:mm:ss", Inv);
        for (int i = augmentsWritten; i < picked.Count; i++)
        {
            var spec = session.Data.Augments.FirstOrDefault(a => a.Id.ToString(Inv) == picked[i]);
            body.Append(time)
                .Append(',')
                .Append(round)
                .Append(',')
                .Append(i + 1)
                .Append(',')
                .Append(spec != null ? spec.Number.ToString(Inv) : picked[i])
                .Append(',')
                .Append(spec != null ? new AugmentationCard(spec).GradeName : "")
                .Append(',')
                .Append(spec != null ? Clean(spec.Name) : "")
                .Append('\n');
        }
        augmentsWritten = picked.Count;
        Append("증강.csv", "기록 시각,받은 시점(이 라운드 시작 전),순서,번호,등급,이름", body);
    }

    string TowerSteps(TowerKind kind, out int total)
    {
        total = 0;
        var parts = new List<string>();
        foreach (string stat in session.UpgradeStats(kind))
        {
            int n = session.TowerUpgradeStep(kind, stat);
            if (n > 0)
            {
                total += n;
                parts.Add(StatName(stat) + " " + n);
            }
        }
        return parts.Count == 0 ? "없음" : string.Join(" · ", parts);
    }

    string SourceName(SourceRow row)
    {
        if (row.TowerId != 0)
            return "포탑 " + row.TowerId + (row.Key.StartsWith("Support:", StringComparison.Ordinal) ? " 지원" : "");

        switch (row.Key)
        {
            case "Player":
                return "기술병 기본 공격";
            case "PlayerSkill":
                return "기술병 범위 폭발·순간 이동 폭발";
            case "Shatter":
                return "얼음 깨짐(증강)";
            case "Burn":
                return "화상(출처 모름)";
            default:
                return Clean(row.Key);
        }
    }

    static string Clean(string text) => (text ?? "").Replace(',', ' ').Replace('\n', ' ');

    static string TowerName(TowerKind kind)
    {
        switch (kind)
        {
            case TowerKind.Single:
                return "단일 고속";
            case TowerKind.Multi:
                return "범위 공격";
            case TowerKind.Frost:
                return "냉기 포탑";
            case TowerKind.Flame:
                return "화염 포탑";
            case TowerKind.Rail:
                return "레일 포탑";
            default:
                return "지원 포탑";
        }
    }

    static string StatName(string stat)
    {
        switch (stat)
        {
            case "damage":
                return "공격력";
            case "rate":
                return "공격속도";
            case "range":
                return "사거리";
            case "buffDamage":
                return "공격력 증가";
            case "slowPercent":
                return "둔화율";
            case "burnPerStack":
                return "화상 피해";
            case "burnDuration":
                return "화상 지속";
            case "pierceFalloff":
                return "관통 피해";
            case "projectileSpeed":
                return "탄환 속도";
            default:
                return Clean(stat);
        }
    }

    void Append(string file, string header, StringBuilder body)
    {
        bool first = !headerWritten.Contains(file);
        if (body.Length == 0 && !first)
            return;

        try
        {
            Directory.CreateDirectory(folder);
            string text = (first ? header + "\n" : "") + body;
            File.AppendAllText(Path.Combine(folder, file), text, new UTF8Encoding(true));
            headerWritten.Add(file);
        }
        catch (Exception e)
        {
            // A failed log write must never stop the game.
            Debug.LogWarning("플레이 기록을 저장하지 못했습니다: " + e.Message);
        }
    }

    public void Dispose()
    {
        session.DamageDealt -= OnDamage;
        session.CombatStarted -= OnCombatStarted;
        session.RoundCompleted -= OnRoundCompleted;
        session.VictoryReached -= OnVictory;
        session.DefeatReached -= OnDefeat;
    }
}
