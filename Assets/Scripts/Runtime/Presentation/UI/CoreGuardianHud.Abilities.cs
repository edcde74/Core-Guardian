using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class CoreGuardianHud
{
    RectTransform playerAbilities;
    static readonly string[] abilityKeys = { "damage", "rate", "range", "projectileSpeed" };

    sealed class AbilityCell
    {
        public RectTransform Root;
        public UnityEngine.UI.Text Name,
            Value;
        public UnityEngine.UI.Image Icon;
        public Sprite NormalIcon;
        public string NormalName;
        public float LastValue;
        public bool ValueReady;
    }

    sealed class AbilityPlateView
    {
        public readonly AbilityCell[] Cells = new AbilityCell[4];
        public GameObject RowRule;
        public RectTransform ColumnRule;
        string decimalSeparator,
            negativeSign,
            nan,
            positiveInfinity,
            negativeInfinity;
        bool formatReady;

        public bool UpdateNumberFormat()
        {
            var format = System.Globalization.CultureInfo.CurrentCulture.NumberFormat;
            bool changed =
                !formatReady
                || decimalSeparator != format.NumberDecimalSeparator
                || negativeSign != format.NegativeSign
                || nan != format.NaNSymbol
                || positiveInfinity != format.PositiveInfinitySymbol
                || negativeInfinity != format.NegativeInfinitySymbol;
            decimalSeparator = format.NumberDecimalSeparator;
            negativeSign = format.NegativeSign;
            nan = format.NaNSymbol;
            positiveInfinity = format.PositiveInfinitySymbol;
            negativeInfinity = format.NegativeInfinitySymbol;
            formatReady = true;
            return changed;
        }
    }

    readonly Dictionary<RectTransform, AbilityPlateView> abilityPlates =
        new Dictionary<RectTransform, AbilityPlateView>();

    void StyleAbilityPurchase(Button button)
    {
        // 10-30 사용자 결정: 강화 칸도 종류 탭과 같은 테두리 버튼으로 보인다(예전 바탕 없는 칸 방식 대신).
        button.GetComponent<CoreGuardianHudReaction>().SetLabelLayout(new Vector2(62, -8), new Vector2(228, 48));
        var label = button.transform.Find("Label").GetComponent<Text>();
        label.fontSize = 16;
        var icon = button.GetComponentsInChildren<Image>().First(i => i.transform != button.transform);
        icon.rectTransform.anchoredPosition = new Vector2(16, -16);
        icon.rectTransform.sizeDelta = new Vector2(32, 32);
    }

    RectTransform BuildAbilityPlate(RectTransform panel)
    {
        var plate = Box("AbilityPlate", panel, new Vector2(0, 1), new Vector2(264, -52), new Vector2(612, 128));
        var view = new AbilityPlateView();
        plate.GetComponent<Image>().raycastTarget = false;
        // One recessed plate, with quiet rules instead of four competing card borders.
        foreach (bool vertical in new[] { true, false })
        {
            var line = Box(
                vertical ? "ColumnRule" : "RowRule",
                plate,
                new Vector2(0, 1),
                vertical ? new Vector2(306, -12) : new Vector2(16, -64),
                vertical ? new Vector2(1, 104) : new Vector2(580, 1),
                false
            );
            var image = line.gameObject.AddComponent<Image>();
            image.color = new Color(.45f, .5f, .54f, .45f);
            image.raycastTarget = false;
            if (vertical)
            {
                view.ColumnRule = line;
            }
            else
            {
                view.RowRule = line.gameObject;
            }
        }
        for (int i = 0; i < abilityKeys.Length; i++)
        {
            var key = abilityKeys[i];
            var cell = Box(
                key,
                plate,
                new Vector2(0, 1),
                // 강화 칸(테두리 버튼)과 같은 자리·크기: 칸 사이 틈 8.
                new Vector2(4 + i % 2 * 306, -2 - i / 2 * 64),
                new Vector2(298, 60),
                false
            );
            var icon = CoreGuardianHudSkin.Icon(
                cell,
                CoreGuardianHudSkin.Symbol(key),
                new Vector2(16, -16),
                new Vector2(32, 32)
            );
            var label = Text("Name", cell, new Vector2(62, -6), new Vector2(228, 22), 16);
            label.color = new Color(.78f, .82f, .85f);
            label.text = StatName(key);
            var value = Text("Value", cell, new Vector2(62, -28), new Vector2(228, 30), 22);
            view.Cells[i] = new AbilityCell
            {
                Root = cell,
                Icon = icon,
                NormalIcon = icon.sprite,
                Name = label,
                NormalName = label.text,
                Value = value,
            };
        }
        abilityPlates.Add(plate, view);
        return plate;
    }

    // Only the player plate uses this (the old support-tower variant was removed).
    void RefreshAbilityPlate(RectTransform plate, CombatStats stats)
    {
        SetHudActive(plate.gameObject, true);
        var view = abilityPlates[plate];
        bool formatChanged = view.UpdateNumberFormat();
        for (int i = 0; i < abilityKeys.Length; i++)
        {
            var cell = view.Cells[i];
            SetHudActive(cell.Root.gameObject, !purchaseOpen);
            SetHudPosition(cell.Root, new Vector2(4 + i % 2 * 306, -2 - i / 2 * 64));
            SetHudText(cell.Name, cell.NormalName);
            if (cell.NormalIcon == null)
            {
                cell.NormalIcon = CoreGuardianHudSkin.Sprite(CoreGuardianHudSkin.Symbol(abilityKeys[i]));
            }

            var sprite = cell.NormalIcon;
            if (cell.Icon.sprite != sprite)
            {
                cell.Icon.sprite = sprite;
            }

            float number =
                i == 0 ? stats.Damage
                : i == 1 ? stats.Rate
                : i == 2 ? stats.Range
                : stats.ProjectileSpeed;
            if (!cell.ValueReady || formatChanged || !cell.LastValue.Equals(number))
            {
                hudLine.Clear();
                AppendStat(ref hudLine, number, i == 0 ? "damage" : "");
                if (i != 0)
                {
                    hudLine.Append(
                        i == 1 ? "발/초"
                        : i == 2 ? "칸"
                        : "칸/초"
                    );
                }

                SetHudText(cell.Value, ref hudLine);
                cell.LastValue = number;
                cell.ValueReady = true;
            }
        }
        SetHudActive(view.RowRule, true);
        SetHudSize(plate, new Vector2(612, 128));
        SetHudSize(view.ColumnRule, new Vector2(1, 104));
    }
}
