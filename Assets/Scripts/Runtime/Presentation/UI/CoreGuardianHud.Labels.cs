using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using Cysharp.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// CoreGuardianHud: enemy/tower kind, ability, status and order labels (appended to a ZString builder).
public sealed partial class CoreGuardianHud
{
    void AppendOrder(ref Utf16ValueStringBuilder sb, TowerSpec spec)
    {
        if (spec.TargetOrder == null)
        {
            sb.Append(spec.Kind == TowerKind.Frost ? "없음 (범위 안 전체 펄스)" : "직접 공격하지 않음");
            return;
        }

        int seen = 0;
        bool any = false;
        foreach (var id in spec.TargetOrder.EnemyIds)
        {
            if (!game.Session.Data.EnemiesById.TryGetValue(id, out var e))
                continue;

            var k = e.Kind == EnemyKind.SplitChild ? EnemyKind.Splitter : e.Kind;
            int bit = 1 << (int)k;
            if ((seen & bit) == 0)
            {
                seen |= bit;
                Separate(ref sb, ref any, " > ");
                sb.Append(KindName(k));
            }
        }
        sb.Append("\n같은 순위: ");
        sb.Append(
            spec.TargetOrder.Tiebreak == TargetOrder.HighestHealth ? "체력 많은 적"
            : spec.TargetOrder.Tiebreak == TargetOrder.NearestCore ? "코어 가까운 적"
            : spec.TargetOrder.Tiebreak == TargetOrder.UnburnedNearestCore ? "화상 없는 적 → 코어 가까운 적"
            : spec.TargetOrder.Tiebreak == TargetOrder.MostPierced ? "가장 많이 꿰뚫는 방향"
            : "체력 낮은 적"
        );
    }

    static bool HasTowerTag(TowerKind kind) =>
        kind == TowerKind.Frost || kind == TowerKind.Flame || kind == TowerKind.Rail || kind == TowerKind.Multi;

    void AppendTowerTag(ref Utf16ValueStringBuilder sb, TowerKind kind, CombatStats st)
    {
        switch (kind)
        {
            case TowerKind.Frost:
                sb.Append("둔화 ");
                sb.Append(st.SlowPercent, "0.##");
                sb.Append("% · ");
                sb.Append(st.SlowDuration, "0.##");
                sb.Append("초");
                break;
            case TowerKind.Flame:
                sb.Append("화상 스택당 초당 ");
                sb.Append(st.BurnPerStack, "0.##");
                sb.Append(" · ");
                sb.Append(st.BurnDuration, "0.##");
                sb.Append("초");
                break;
            case TowerKind.Rail:
                sb.Append("관통 · 통과마다 ×");
                sb.Append(st.PierceFalloff, "0.##");
                break;
            case TowerKind.Multi:
                sb.Append("최대 ");
                sb.Append(game.Session.Data.Towers[kind].MaxTargets);
                sb.Append("명 동시 공격");
                break;
        }
    }

    static string KindName(EnemyKind kind) => EnemyNames.Kind(kind);

    static void AppendEnemyName(ref Utf16ValueStringBuilder sb, EnemyHandle e)
    {
        if (e.Kind == EnemyKind.Boss)
        {
            sb.Append(BossName(e.Spec));
            return;
        }

        sb.Append(KindName(e.Kind));
        if (e.Summoned)
        {
            sb.Append(" (소환 졸개)");
        }
    }

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

    static void AppendAbility(ref Utf16ValueStringBuilder sb, EnemyHandle e)
    {
        var s = e.Spec;
        bool any = false;
        if (s.BuffRadius > 0 && s.BuffSpeed > 0)
        {
            Separate(ref sb, ref any, " · ");
            sb.Append("주변 ");
            sb.Append(s.BuffRadius, "0.##");
            sb.Append("타일 이동속도 +");
            sb.Append(s.BuffSpeed, "0.##");
            sb.Append("%");
        }

        if (s.HealInterval > 0)
        {
            Separate(ref sb, ref any, " · ");
            sb.Append(s.HealInterval, "0.##");
            sb.Append("초마다 ");
            sb.Append(s.HealRadius, "0.##");
            sb.Append("타일 회복 ");
            sb.Append(s.HealPercent, "0.##");
            sb.Append("%");
        }

        if (s.SplitIntoId != 0)
        {
            Separate(ref sb, ref any, " · ");
            sb.Append("처치 시 ");
            sb.Append(s.SplitCount);
            sb.Append("마리로 분열");
        }

        if (s.Flying)
        {
            Separate(ref sb, ref any, " · ");
            sb.Append("비행 경로(지름길)");
        }

        if (e.Kind == EnemyKind.Boss)
        {
            Separate(ref sb, ref any, " · ");
            sb.Append("체력 ");
            sb.Append(s.RagePercent, "0");
            sb.Append("% 이하 격노 +");
            sb.Append(s.RageBoost, "0");
            sb.Append("%");
        }

        if (s.ShieldAt.Length > 0)
        {
            Separate(ref sb, ref any, " · ");
            sb.Append("체력 ");
            for (int i = 0; i < s.ShieldAt.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append("·");
                }

                sb.Append(s.ShieldAt[i], "0");
                sb.Append("%");
            }
            sb.Append("에서 ");
            sb.Append(s.ShieldSeconds, "0.#");
            sb.Append("초 방어 +");
            sb.Append(s.ShieldDefense, "0");
        }

        if (s.SummonId != 0)
        {
            Separate(ref sb, ref any, " · ");
            sb.Append(s.SummonInterval, "0.#");
            sb.Append("초마다 졸개 ");
            sb.Append(s.SummonCount);
            sb.Append("마리 소환");
        }

        if (!any)
        {
            sb.Append(
                e.Kind == EnemyKind.Charger ? "매우 빠름"
                : e.Kind == EnemyKind.Armored ? "높은 방어"
                : "없음"
            );
        }
    }

    static void AppendStatus(ref Utf16ValueStringBuilder sb, EnemyHandle e)
    {
        bool any = false;
        float slow = e.SlowPercent;
        if (slow > 0)
        {
            Separate(ref sb, ref any, " · ");
            sb.Append("둔화 ");
            sb.Append(slow, "0");
            sb.Append("%");
        }

        if (e.BurnStacks > 0)
        {
            Separate(ref sb, ref any, " · ");
            sb.Append("화상 ");
            sb.Append(e.BurnStacks);
            sb.Append("스택");
        }

        if (e.Frozen)
        {
            Separate(ref sb, ref any, " · ");
            sb.Append("얼어붙음");
        }
        else if (e.Stunned)
        {
            Separate(ref sb, ref any, " · ");
            sb.Append("기절");
        }

        if (e.Shielded)
        {
            Separate(ref sb, ref any, " · ");
            sb.Append("방패 전개");
        }

        if (e.Enraged)
        {
            Separate(ref sb, ref any, " · ");
            sb.Append("격노");
        }

        if (e.Buffed)
        {
            Separate(ref sb, ref any, " · ");
            sb.Append("버프 받음");
        }

        if (!any)
        {
            sb.Append("없음");
        }
    }
}
