using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using Cysharp.Text;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// CoreGuardianHud: stat and name text formatting.
public sealed partial class CoreGuardianHud
{
    static string BossName(EnemySpec spec) => EnemyNames.Boss(spec);

    string StatName(string stat)
    {
        switch (stat)
        {
            case "damage":
                return "공격력";
            case "rate":
                return "공격 속도";
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
            default:
                return "투사체 속도";
        }
    }

    float StatValue(CombatStats stats, string stat) =>
        stat == "damage" ? stats.Damage
        : stat == "rate" ? stats.Rate
        : stat == "range" ? stats.Range
        : stat == "buffDamage" ? stats.BuffDamage
        : stat == "slowPercent" ? stats.SlowPercent
        : stat == "burnPerStack" ? stats.BurnPerStack
        : stat == "burnDuration" ? stats.BurnDuration
        : stat == "pierceFalloff" ? stats.PierceFalloff
        : stats.ProjectileSpeed;

    string UpgradeLabel(CombatStats stats, string target, string stat, int cost, string currency, bool maxed = false)
    {
        var sb = ZString.CreateStringBuilder(true);
        try
        {
            sb.Append(StatName(stat));
            sb.Append(" ");
            if (maxed)
            {
                AppendStat(ref sb, StatValue(stats, stat), stat);
                sb.Append("\n최대치");
                return sb.ToString();
            }

            UpgradeSpec spec = null;
            foreach (var u in game.Session.Data.Upgrades)
            {
                if (u.Target == target && u.Stat == stat)
                {
                    spec = u;
                    break;
                }
            }

            float value = StatValue(stats, stat),
                delta = spec == null ? 0 : spec.Delta;
            bool affordable = currency == "골드" ? game.Session.Gold >= cost : game.Session.Crystals >= cost;
            AppendStat(ref sb, value, stat);
            sb.Append(" → ");
            AppendStat(ref sb, value + delta, stat);
            sb.Append("\n+");
            if (stat == "pierceFalloff")
            {
                sb.Append(Mathf.RoundToInt(delta * 100));
                sb.Append("%");
            }
            else
            {
                sb.Append(delta, "0.##");
            }
            // Grouped upgrades (화염 공격력 + 화상 피해) list their partner stat too.
            if (spec != null)
            {
                foreach (var u in game.Session.Data.Upgrades)
                {
                    if (u != spec && u.Group == spec.Id && u.Target == target)
                    {
                        sb.Append(" · ");
                        sb.Append(StatName(u.Stat));
                        sb.Append(" +");
                        sb.Append(u.Delta, "0.##");
                    }
                }
            }

            sb.Append(" · ");
            sb.Append(cost);
            sb.Append(currency);
            if (!affordable)
            {
                sb.Append(" · 부족");
            }

            return sb.ToString();
        }
        finally
        {
            sb.Dispose();
        }
    }

    string TowerName(TowerKind kind)
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
}
