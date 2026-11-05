using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// CoreGuardianHud: upgrade purchase rows and prices.
public sealed partial class CoreGuardianHud
{
    void PurchasePlayer(string stat)
    {
        float before = StatValue(game.Session.Player, stat);
        game.UpgradePlayer(stat);
        if (StatValue(game.Session.Player, stat) > before)
        {
            game.Notify(StatName(stat) + " +" + (StatValue(game.Session.Player, stat) - before).ToString("0.##"));
            var b = playerPanel.Find(stat);
            if (b != null)
            {
                b.GetComponent<CoreGuardianHudReaction>().Confirm();
            }
        }
    }

    TowerState Selected()
    {
        int id = game.SelectedTower;
        foreach (var t in game.Session.Towers)
        {
            if (t.Id == id)
                return t;
        }
        return null;
    }

    List<string> UpgradeRows(TowerKind kind)
    {
        if (!upgradeRows.TryGetValue(kind, out var rows))
        {
            rows = game.Session.UpgradeStats(kind).OrderBy(UpgradeRowOrder).ToList();
            upgradeRows[kind] = rows;
        }
        return rows;
    }

    static int UpgradeRowOrder(string stat)
    {
        int i = System.Array.IndexOf(UpgradeOrder, stat);
        return i < 0 ? UpgradeOrder.Length : i;
    }
}
