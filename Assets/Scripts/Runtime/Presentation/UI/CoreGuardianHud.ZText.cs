using System;
using System.Collections.Generic;
using CoreGuardian;
using Cysharp.Text;
using UnityEngine;

// CoreGuardianHud: ZString text building (2026-10-28). Lines are built in reusable pooled buffers
// without temporary strings, and a new string is made only when the shown text actually changes.
public sealed partial class CoreGuardianHud
{
    static readonly TowerKind[] allTowerKinds = (TowerKind[])Enum.GetValues(typeof(TowerKind));

    // Kept for the HUD's lifetime; used one line at a time (never nested).
    Utf16ValueStringBuilder hudLine = ZString.CreateStringBuilder();
    Utf16ValueStringBuilder hudNote = ZString.CreateStringBuilder();

    readonly Dictionary<string, Sprite> h5Sprites = new Dictionary<string, Sprite>();
    Sprite[] installSprites;

    void SetHudText(UnityEngine.UI.Text target, ref Utf16ValueStringBuilder text)
    {
        string current = target.text;
        if (current == null || !text.AsSpan().SequenceEqual(current.AsSpan()))
        {
            target.text = text.ToString();
        }
    }

    // Starts a new info line in hudLine.
    void Line(string start)
    {
        hudLine.Clear();
        hudLine.Append(start);
    }

    // Stat value as shown in the HUD: damage rounded to a whole number, others up to 2 decimals.
    static void AppendStat(ref Utf16ValueStringBuilder sb, float value, string stat)
    {
        if (stat == "damage")
        {
            sb.Append(Mathf.FloorToInt(value + .5f));
        }
        else if (stat == "pierceFalloff")
        {
            // 10-31: 레일 관통 피해는 % 로 보여 준다(0.82 → 82%).
            sb.Append(Mathf.RoundToInt(value * 100));
            sb.Append("%");
        }
        else
        {
            sb.Append(value, "0.##");
        }
    }

    static void Separate(ref Utf16ValueStringBuilder sb, ref bool any, string separator)
    {
        if (any)
        {
            sb.Append(separator);
        }

        any = true;
    }

    Sprite H5(string name)
    {
        if (!h5Sprites.TryGetValue(name, out var sprite) || sprite == null)
        {
            sprite = CoreGuardianHudSkin.Sprite("Redesign/H5_" + name);
            h5Sprites[name] = sprite;
        }

        return sprite;
    }

    Sprite InstallSprite(TowerKind kind)
    {
        if (installSprites == null)
        {
            int max = 0;
            foreach (var k in allTowerKinds)
            {
                max = Math.Max(max, (int)k);
            }

            installSprites = new Sprite[max + 1];
        }

        int i = (int)kind;
        var sprite = installSprites[i];
        if (sprite == null)
        {
            sprite = HudSprite(CoreGuardianHudSkin.Symbol("Install_" + kind));
            installSprites[i] = sprite;
        }

        return sprite;
    }
}
