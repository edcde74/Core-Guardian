using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// CoreGuardianHud: enemy/tower kind, ability, status and order labels.
public sealed partial class CoreGuardianHud
{
    string OrderText(TowerSpec spec)
    {
        if (spec.TargetOrder == null)
            return spec.Kind == TowerKind.Frost ? "없음 (범위 안 전체 펄스)" : "직접 공격하지 않음";

        var names = new List<string>();
        var seen = new HashSet<EnemyKind>();
        foreach (var id in spec.TargetOrder.EnemyIds)
        {
            if (!game.Session.Data.EnemiesById.TryGetValue(id, out var e))
                continue;

            var k = e.Kind == EnemyKind.SplitChild ? EnemyKind.Splitter : e.Kind;
            if (seen.Add(k))
            {
                names.Add(KindName(k));
            }
        }
        string tie =
            spec.TargetOrder.Tiebreak == TargetOrder.HighestHealth ? "체력 많은 적"
            : spec.TargetOrder.Tiebreak == TargetOrder.NearestCore ? "코어 가까운 적"
            : spec.TargetOrder.Tiebreak == TargetOrder.UnburnedNearestCore ? "화상 없는 적 → 코어 가까운 적"
            : spec.TargetOrder.Tiebreak == TargetOrder.MostPierced ? "가장 많이 꿰뚫는 방향"
            : "체력 낮은 적";
        return string.Join(" > ", names) + "\n같은 순위: " + tie;
    }

    string TowerTagText(TowerKind kind, CombatStats st)
    {
        switch (kind)
        {
            case TowerKind.Frost:
                return "둔화 " + st.SlowPercent.ToString("0.##") + "% · " + st.SlowDuration.ToString("0.##") + "초";
            case TowerKind.Flame:
                return "화상 스택당 초당 "
                    + st.BurnPerStack.ToString("0.##")
                    + " · "
                    + st.BurnDuration.ToString("0.##")
                    + "초";
            case TowerKind.Rail:
                return "관통 · 통과마다 ×" + st.PierceFalloff.ToString("0.##");
            case TowerKind.Multi:
                return "최대 " + game.Session.Data.Towers[kind].MaxTargets + "명 동시 공격";
            default:
                return "";
        }
    }

    static string KindName(EnemyKind kind) => EnemyNames.Kind(kind);

    static string EnemyName(EnemyHandle e) =>
        e.Kind == EnemyKind.Boss ? BossName(e.Spec) : KindName(e.Kind) + (e.Summoned ? " (소환 졸개)" : "");

    static string EnemyFamily(EnemyHandle e)
    {
        switch (e.Kind)
        {
            case EnemyKind.Defense:
            case EnemyKind.Armored:
                return "수비형 계열";
            case EnemyKind.Buff:
            case EnemyKind.Healer:
                return "버프형 계열";
            case EnemyKind.Boss:
                return e.Spec.Key == "IronKnight" ? "수비형 보스"
                    : e.Spec.Key == "CrimsonLord" ? "버프형 보스"
                    : "보스";
            default:
                return "공격형 계열";
        }
    }

    static string AbilityText(EnemyHandle e)
    {
        var s = e.Spec;
        var parts = new List<string>();
        if (s.BuffRadius > 0 && s.BuffSpeed > 0)
        {
            parts.Add("주변 " + s.BuffRadius.ToString("0.##") + "타일 이동속도 +" + s.BuffSpeed.ToString("0.##") + "%");
        }

        if (s.HealInterval > 0)
        {
            parts.Add(
                s.HealInterval.ToString("0.##")
                    + "초마다 "
                    + s.HealRadius.ToString("0.##")
                    + "타일 회복 "
                    + s.HealPercent.ToString("0.##")
                    + "%"
            );
        }

        if (s.SplitIntoId != 0)
        {
            parts.Add("처치 시 " + s.SplitCount + "마리로 분열");
        }

        if (s.Flying)
        {
            parts.Add("비행 경로(지름길)");
        }

        if (e.Kind == EnemyKind.Boss)
        {
            parts.Add("체력 " + s.RagePercent.ToString("0") + "% 이하 격노 +" + s.RageBoost.ToString("0") + "%");
        }

        if (s.ShieldAt.Length > 0)
        {
            parts.Add(
                "체력 "
                    + string.Join("·", s.ShieldAt.Select(x => x.ToString("0") + "%"))
                    + "에서 "
                    + s.ShieldSeconds.ToString("0.#")
                    + "초 방어 +"
                    + s.ShieldDefense.ToString("0")
            );
        }

        if (s.SummonId != 0)
        {
            parts.Add(s.SummonInterval.ToString("0.#") + "초마다 졸개 " + s.SummonCount + "마리 소환");
        }

        if (parts.Count == 0)
        {
            parts.Add(
                e.Kind == EnemyKind.Charger ? "매우 빠름"
                : e.Kind == EnemyKind.Armored ? "높은 방어"
                : "없음"
            );
        }

        return string.Join(" · ", parts);
    }

    static string StatusText(EnemyHandle e)
    {
        var parts = new List<string>();
        float slow = e.SlowPercent;
        if (slow > 0)
        {
            parts.Add("둔화 " + slow.ToString("0") + "%");
        }

        if (e.BurnStacks > 0)
        {
            parts.Add("화상 " + e.BurnStacks + "스택");
        }

        if (e.Frozen)
        {
            parts.Add("얼어붙음");
        }
        else if (e.Stunned)
        {
            parts.Add("기절");
        }

        if (e.Shielded)
        {
            parts.Add("방패 전개");
        }

        if (e.Enraged)
        {
            parts.Add("격노");
        }

        if (e.Buffed)
        {
            parts.Add("버프 받음");
        }

        return parts.Count == 0 ? "없음" : string.Join(" · ", parts);
    }
}
