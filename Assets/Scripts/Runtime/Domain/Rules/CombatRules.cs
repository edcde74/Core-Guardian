using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CoreGuardian
{
    public static class CombatRules
    {
        public static int Damage(float damage, float defense)
        {
            return Mathf.FloorToInt(Mathf.Max(0, damage * (1 - defense / 100f)) + .5f);
        }
    }
}
