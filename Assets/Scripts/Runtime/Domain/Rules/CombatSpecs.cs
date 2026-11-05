using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CoreGuardian
{
    public sealed class EnemySpec
    {
        public EnemyKind Kind;
        public int Health,
            CoreDamage,
            Gold;
        public float Speed,
            Defense,
            BuffRadius,
            BuffSpeed;

        // Data table (2026-10-26 spec). Legacy rows leave these at their defaults.
        public int Id;
        public string Key;
        public float HealthMul,
            GoldMul,
            DefenseRefMul,
            HealInterval,
            HealRadius,
            HealPercent,
            RageAt,
            RageSpeed;
        public int DefenseRefId,
            SplitIntoId,
            SplitCount;
        public bool Flying;

        // Boss abilities: shield thresholds (% of max health), summons (enemy ID x count per cycle).
        public float[] ShieldAt = Array.Empty<float>();
        public float ShieldDefense,
            ShieldSeconds;
        public int SummonId,
            SummonCount;
        public float SummonInterval,
            SummonRageInterval;

        // 약점·저항 (2026-10-28): bit (1<<(int)TowerKind) per tower kind. Tower damage only.
        public int WeakTowers,
            ResistTowers;
        public float RagePercent => RageAt > 0 ? RageAt : 50;
        public float RageBoost => RageSpeed > 0 ? RageSpeed : 30;

        public EnemySpec Clone() => (EnemySpec)MemberwiseClone();
    }

    public sealed class TowerSpec
    {
        public TowerKind Kind;
        public int Cost;
        public float Damage,
            Rate,
            Range,
            ProjectileSpeed,
            BuffDamage;
        public int Id,
            MaxTargets;
        public string Key,
            Tag,
            AttackType,
            Requires;
        public float SlowPercent,
            SlowDuration,
            BurnPerStack,
            BurnDuration,
            PierceFalloff;

        // 포탑별 공격 순서 (towers.targetPriority/targetTiebreak). Null = no target choice (냉기·지원).
        public TargetOrder TargetOrder;
    }

    public sealed class UpgradeSpec
    {
        public string Target,
            Stat;
        public float Delta;
        public int Cost,
            CostIncrement;
        public int Id,
            TargetId,
            Group;
    }

    public sealed class WaveSpec
    {
        public int Wave,
            Count;
        public EnemyKind Kind;
        public float Interval,
            Start;
        public EnemySpec Enemy;
        public int Id,
            EnemyId;
    }

    public sealed class AugmentSpec
    {
        public int Id,
            Number;
        public string Name,
            Category,
            Personality,
            Effect,
            Text;
        public AugmentationGrade Grade;
        public string[] Subcategories;
        public float[] Values;
        public bool Repeatable;

        // 11-04 확정: false면 증강 뽑기(다시 뽑기 포함)에 나오지 않음. 카드 자체는 표에 남김.
        public bool Drawable = true;

        public float Value(int index) => index < Values.Length ? Values[index] : 0;
    }

    public sealed class EffectTagSpec
    {
        public int Id;
        public string Tag,
            Param,
            Unit;
        public float Value;
        public float? BossValue;
        public bool BossImmune;
    }

    public sealed class SkillSpec
    {
        public int Id,
            Owner;
        public string Key,
            Input,
            Phase;
        public float DamageMul,
            Radius,
            Cooldown;
        public bool ResetOnRoundStart,
            Android;
    }
}
