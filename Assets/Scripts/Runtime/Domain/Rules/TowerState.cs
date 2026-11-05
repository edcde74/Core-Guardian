using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CoreGuardian
{
    public sealed class TowerState
    {
        public int Id,
            Spent;
        public TowerKind Kind;
        public Vector2Int Position;
        public CombatStats Stats;
        internal float Cooldown;
        internal EnemyHandle Target;
        internal bool AcquiredOnce;

        // 45 포탑 합체 star level (1~3); per-attacker crit counters (26·44).
        public int Stars = 1;
        internal int Attacks;
        internal float CritBonus;
        int sourceId;
        string attackSource,
            supportSource;

        void RefreshSources()
        {
            if (attackSource != null && sourceId == Id && Id >= 0)
                return;
            sourceId = Id;
            attackSource = "Tower:" + Id;
            supportSource = "Support:" + Id;
        }

        public string AttackSource
        {
            get
            {
                RefreshSources();
                return attackSource;
            }
        }
        public string SupportSource
        {
            get
            {
                RefreshSources();
                return supportSource;
            }
        }
    }
}
