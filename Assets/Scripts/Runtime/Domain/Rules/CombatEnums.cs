using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CoreGuardian
{
    // Appended values only: existing indices (pools, serialized art arrays) must not move.
    public enum EnemyKind
    {
        Attack,
        Defense,
        Buff,
        Boss,
        Charger,
        Splitter,
        Healer,
        Armored,
        Flyer,
        SplitChild,
    }

    public enum TowerKind
    {
        Single,
        Multi,
        Support,
        Frost,
        Flame,
        Rail,
    }

    public enum SessionPhase
    {
        Preparation,
        Combat,
        Victory,
        Defeat,
        Augmentation,
    }
}
