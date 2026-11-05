using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CoreGuardian
{
    public sealed partial class CombatData
    {
        public readonly Dictionary<EnemyKind, EnemySpec> Enemies = new Dictionary<EnemyKind, EnemySpec>();
        public readonly Dictionary<TowerKind, TowerSpec> Towers = new Dictionary<TowerKind, TowerSpec>();
        public readonly List<UpgradeSpec> Upgrades = new List<UpgradeSpec>();
        public readonly List<WaveSpec> Waves = new List<WaveSpec>();
        public readonly Dictionary<int, int> WaveRewards = new Dictionary<int, int>();

        // Numeric-ID tables (empty for legacy string-ID data).
        public readonly Dictionary<int, EnemySpec> EnemiesById = new Dictionary<int, EnemySpec>();
        public readonly Dictionary<int, TowerSpec> TowersById = new Dictionary<int, TowerSpec>();
        public readonly List<AugmentSpec> Augments = new List<AugmentSpec>();
        public readonly Dictionary<int, EffectTagSpec> EffectTags = new Dictionary<int, EffectTagSpec>();
        public readonly List<SkillSpec> Skills = new List<SkillSpec>();
        public readonly Dictionary<string, float> Settings = new Dictionary<string, float>();
        public bool NumericIds { get; private set; }

        public int RewardForWave(int wave) => WaveRewards.TryGetValue(wave, out var reward) ? reward : WaveReward;

        public int CoreEnergy,
            StartGold,
            StartCrystals,
            WaveReward,
            PlayerFirstCost,
            PlayerSecondIncrease,
            PlayerIncreaseStep,
            DiagnosticGold,
            DiagnosticCrystals;
        public int PlayerFirstUpgradeDiscount,
            TowerFirstUpgradeDiscount;
        public int AugmentationEvery = 10;
        public bool AugmentationRequiresBoss = true;

        public EnemySpec EnemyForWave(int wave, EnemyKind kind) =>
            Waves.FirstOrDefault(w => w.Wave == wave && w.Kind == kind)?.Enemy ?? Enemies[kind];

        public float PreparationSeconds,
            DiagnosticStartDistance,
            DiagnosticSpacing;

        // Long preparation: before a boss round and right after an augmentation choice. Missing key keeps the normal length.
        public float PreparationSecondsLong;

        public bool IsBossWave(int wave)
        {
            foreach (var w in Waves)
            {
                if (w.Wave == wave && w.Kind == EnemyKind.Boss)
                    return true;
            }
            return false;
        }

        public float PreparationFor(int wave, bool afterAugmentation) =>
            afterAugmentation || IsBossWave(wave) ? PreparationSecondsLong : PreparationSeconds;

        public float Setting(string key, float fallback) => Settings.TryGetValue(key, out var value) ? value : fallback;

        public CombatStats Player;

        // 기술병 공통 공격 순서 (player.targetPriority). Legacy data uses TargetOrder.Common.
        public TargetOrder PlayerTargetOrder = TargetOrder.Common;

        // Effect tag lookup with a fallback when the sheet lacks the row; NaN marks boss immunity.
        public float Tag(string tag, string param, bool boss, float fallback)
        {
            foreach (var t in EffectTags.Values)
            {
                if (t.Tag == tag && t.Param == param)
                {
                    return boss ? (t.BossImmune ? float.NaN : t.BossValue ?? t.Value) : t.Value;
                }
            }

            return fallback;
        }

        // Effect tag lookup; returns null when the boss is immune.
        public float? TagValue(string tag, string param, bool boss)
        {
            foreach (var t in EffectTags.Values)
            {
                if (t.Tag == tag && t.Param == param)
                {
                    return boss ? (t.BossImmune ? (float?)null : t.BossValue ?? t.Value) : t.Value;
                }
            }

            throw new KeyNotFoundException("Missing effect tag " + tag + "." + param);
        }

        static EnemySpec WaveEnemy(EnemySpec b, string health, string defense, string speed, string gold)
        {
            var e = b.Clone();
            e.Health = I(health);
            e.Defense = F(defense);
            e.Speed = F(speed);
            e.Gold = I(gold);
            if (e.Health <= 0 || e.Speed <= 0 || e.Defense >= 100)
            {
                throw new ArgumentException("Invalid wave enemy stats");
            }

            return e;
        }

        public int PlayerUpgradePrice(int step)
        {
            if (step < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(step));
            }
            return checked(
                PlayerFirstCost
                + (step - 1) * PlayerSecondIncrease
                + PlayerIncreaseStep * (step - 1) * (step - 2) / 2
                - (step == 1 ? PlayerFirstUpgradeDiscount : 0)
            );
        }
    }
}
