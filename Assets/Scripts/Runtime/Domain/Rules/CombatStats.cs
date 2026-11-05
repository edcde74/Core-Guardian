using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CoreGuardian
{
    public sealed class CombatStats
    {
        public float Damage,
            Rate,
            Range,
            ProjectileSpeed,
            BuffDamage;
        public float SlowPercent,
            SlowDuration,
            BurnPerStack,
            BurnDuration,
            PierceFalloff;
        public readonly Dictionary<string, int> UpgradeSteps = new Dictionary<string, int>();

        public CombatStats Clone()
        {
            return new CombatStats
            {
                Damage = Damage,
                Rate = Rate,
                Range = Range,
                ProjectileSpeed = ProjectileSpeed,
                BuffDamage = BuffDamage,
                SlowPercent = SlowPercent,
                SlowDuration = SlowDuration,
                BurnPerStack = BurnPerStack,
                BurnDuration = BurnDuration,
                PierceFalloff = PierceFalloff,
            };
        }

        public static bool IsStat(string stat)
        {
            switch (stat)
            {
                case "damage":
                case "rate":
                case "range":
                case "projectileSpeed":
                case "buffDamage":
                case "slowPercent":
                case "burnPerStack":
                case "burnDuration":
                case "pierceFalloff":
                    return true;
                default:
                    return false;
            }
        }

        public void Add(string stat, float value)
        {
            switch (stat)
            {
                case "damage":
                    Damage += value;
                    break;
                case "rate":
                    Rate += value;
                    break;
                case "range":
                    Range += value;
                    break;
                case "projectileSpeed":
                    ProjectileSpeed += value;
                    break;
                case "buffDamage":
                    BuffDamage += value;
                    break;
                case "slowPercent":
                    SlowPercent += value;
                    break;
                case "burnPerStack":
                    BurnPerStack += value;
                    break;
                case "burnDuration":
                    BurnDuration += value;
                    break;
                case "pierceFalloff":
                    PierceFalloff += value;
                    break;
                default:
                    throw new ArgumentException("Unknown stat: " + stat);
            }
        }
    }
}
