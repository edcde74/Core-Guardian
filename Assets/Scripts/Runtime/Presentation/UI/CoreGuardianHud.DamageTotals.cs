using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// CoreGuardianHud: round damage totals panel.
public sealed partial class CoreGuardianHud
{
    struct DamageTotal
    {
        public string Key;
        public int Value,
            Ordinal;
    }

    sealed class DamageTotalComparer : IComparer<DamageTotal>
    {
        public int Compare(DamageTotal a, DamageTotal b)
        {
            int order = b.Value.CompareTo(a.Value);
            if (order != 0)
                return order;

            order = Comparer<string>.Default.Compare(a.Key, b.Key);
            return order != 0 ? order : a.Ordinal.CompareTo(b.Ordinal);
        }
    }

    sealed class DamageRow
    {
        public RectTransform Root,
            Bar;
        public UnityEngine.UI.Image Icon;
        public UnityEngine.UI.Text Label,
            Total;
    }

    static Sprite DamageSprite(string name)
    {
        if (damageSprites.Count == 0)
        {
            foreach (var sprite in Resources.LoadAll<Sprite>("CoreGuardian/Hud/DamageRoles"))
            {
                damageSprites[sprite.name] = sprite;
            }
        }

        if (damageSprites.TryGetValue(name, out var result))
            return result;
        // New tower roles (냉기·화염·레일) are separate confirmed sprites next to the sheet.
        result = Resources.Load<Sprite>("CoreGuardian/Hud/" + name);
        damageSprites[name] = result;
        return result;
    }

    void BuildDamageFold()
    {
        damageFold = Box("DamageFold", canvas, Vector2.one, Vector2.zero, new Vector2(28, 108), false);
        var hit = damageFold.gameObject.AddComponent<UnityEngine.UI.Image>();
        hit.color = Color.clear;
        var art = Box("HandleArt", damageFold, new Vector2(.5f, .5f), Vector2.zero, new Vector2(28, 108), false);
        var image = art.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = DamageSprite("DamageHandle");
        image.preserveAspect = true;
        image.raycastTarget = false;
        var button = damageFold.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        refresh.Add(() => button.interactable = true);
        damageFold.gameObject.AddComponent<CoreGuardianHudHandleReaction>().Configure(image, false);
        button.onClick.AddListener(() =>
        {
            damagePanel.gameObject.SetActive(!damagePanel.gameObject.activeSelf);
            PositionDamageFold();
        });
        PositionDamageFold();
    }

    void PositionDamageFold()
    {
        bool open = damagePanel.gameObject.activeSelf;
        damageFold.anchoredPosition = new Vector2(
            open ? damagePanel.anchoredPosition.x - damagePanel.sizeDelta.x : -16,
            damagePanel.anchoredPosition.y - damagePanel.sizeDelta.y * .5f
        );
        // Mirror only the symmetric handle artwork; keep its clickable edge anchor unchanged.
        damageFold.GetComponent<UnityEngine.UI.Image>().rectTransform.localScale = Vector3.one;
        damageFold.pivot = new Vector2(open ? 1 : 0, .5f);
        damageFold.localScale = new Vector3(open ? 1 : -1, 1, 1);
    }

    string DamageRole(string source)
    {
        if (source == "Player")
            return "DamageEngineer";

        int id;
        if (source.StartsWith("Tower:", StringComparison.Ordinal) && int.TryParse(source.Substring(6), out id))
        {
            var tower = game.Session.Towers.FirstOrDefault(t => t.Id == id);
            if (tower != null)
            {
                switch (tower.Kind)
                {
                    case TowerKind.Multi:
                        return "DamageRange";
                    case TowerKind.Frost:
                        return "DamageFrost";
                    case TowerKind.Flame:
                        return "DamageFlame";
                    case TowerKind.Rail:
                        return "DamageRail";
                }
            }
        }
        return "DamageSingle";
    }

    string SourceName(string source)
    {
        if (source == "Player")
            return "기술병";

        if (source == "Burn")
            return "화상";

        if (source == "Shatter")
            return "얼음 부서짐";

        int id;
        if (source.StartsWith("Tower:", StringComparison.Ordinal) && int.TryParse(source.Substring(6), out id))
        {
            var tower = game.Session.Towers.FirstOrDefault(t => t.Id == id);
            return (tower == null ? "철거한 포탑" : TowerName(tower.Kind)) + " #" + id;
        }
        return source;
    }

    // F4: a row's name and icon are made once per damage source and remade only when its tower appears or goes.
    sealed class DamageLabelCache
    {
        public int TowerId = -1;
        public bool Exists;
        public string Name,
            Role;
    }

    readonly Dictionary<string, DamageLabelCache> damageLabels = new Dictionary<string, DamageLabelCache>();

    Session damageLabelSession;

    DamageLabelCache DamageLabel(string key)
    {
        if (!ReferenceEquals(damageLabelSession, game.Session))
        {
            damageLabelSession = game.Session; // tower ids restart in a new game
            damageLabels.Clear();
        }
        if (!damageLabels.TryGetValue(key, out var label))
        {
            label = new DamageLabelCache();
            if (key.StartsWith("Tower:", StringComparison.Ordinal) && int.TryParse(key.Substring(6), out int id))
            {
                label.TowerId = id;
            }
            damageLabels.Add(key, label);
        }

        bool exists = false;
        if (label.TowerId >= 0)
        {
            foreach (var t in game.Session.Towers)
            {
                if (t.Id == label.TowerId)
                {
                    exists = true;
                    break;
                }
            }
        }
        if (label.Name == null || exists != label.Exists)
        {
            label.Exists = exists;
            label.Name = SourceName(key);
            label.Role = DamageRole(key);
        }
        return label;
    }

    void RefreshDamageTotals()
    {
        damageTotals.Clear();
        int ordinal = 0;
        foreach (var pair in game.Session.DamageTotals)
        {
            damageTotals.Add(
                new DamageTotal
                {
                    Key = pair.Key,
                    Value = pair.Value,
                    Ordinal = ordinal++,
                }
            );
        }
        // Ordinal preserves LINQ's stable ordering even for culture-equivalent distinct keys.
        damageTotals.Sort(damageTotalCompare);
        string negativeSign = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NegativeSign;
        bool changed =
            !damageTotalsPresented
            || negativeSign != damageNegativeSign
            || damageTotals.Count != previousDamageTotals.Count;
        if (!changed)
        {
            for (int i = 0; i < damageTotals.Count; i++)
            {
                if (
                    damageTotals[i].Key != previousDamageTotals[i].Key
                    || damageTotals[i].Value != previousDamageTotals[i].Value
                )
                {
                    changed = true;
                    break;
                }
            }
        }

        if (!changed)
            return;

        int max = damageTotals.Count == 0 ? 0 : damageTotals[0].Value;
        for (int i = 0; i < damageTotals.Count; i++)
        {
            if (i == damageRows.Count)
            {
                damageRows.Add(CreateDamageRow());
            }

            var row = damageRows[i];
            var pair = damageTotals[i];
            if (row.Root.name != pair.Key)
            {
                row.Root.name = pair.Key;
            }

            var label = DamageLabel(pair.Key);
            var icon = DamageSprite(label.Role);
            if (row.Icon.sprite != icon)
            {
                row.Icon.sprite = icon;
            }

            SetHudText(row.Label, label.Name);
            if (
                !damageTotalsPresented
                || negativeSign != damageNegativeSign
                || i >= previousDamageTotals.Count
                || previousDamageTotals[i].Value != pair.Value
            )
            {
                SetHudText(row.Total, pair.Value.ToString());
            }

            SetHudSize(row.Bar, new Vector2(max == 0 ? 0 : 272f * pair.Value / max, 10));
            SetHudActive(row.Root.gameObject, true);
        }
        for (int i = damageTotals.Count; i < damageRows.Count; i++)
        {
            SetHudActive(damageRows[i].Root.gameObject, false);
        }

        previousDamageTotals.Clear();
        previousDamageTotals.AddRange(damageTotals);
        damageNegativeSign = negativeSign;
        damageTotalsPresented = true;
    }

    void SelectDamageSource(string key)
    {
        if (key == "Player")
        {
            OpenPlayerSpecs();
            return;
        }
        if (key.StartsWith("Tower:", StringComparison.Ordinal) && int.TryParse(key.Substring(6), out int id))
        {
            game.SelectTower(id);
        }
    }

    // 11-02 사용자 요청: 클릭이 UI 버튼 입력에 묶이지 않게, 마우스를 뗀 자리의 맨 위 물체가 줄이면 고른다.
    public bool TrySelectDamageRow(GameObject top)
    {
        foreach (var row in damageRows)
        {
            if (row.Root.gameObject == top)
            {
                SelectDamageSource(row.Root.name);
                return true;
            }
        }

        return false;
    }

    DamageRow CreateDamageRow()
    {
        var row = Box("DamageRow", damageContent, new Vector2(0, 1), Vector2.zero, new Vector2(288, 60), false);
        row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 60;
        // 줄 어디든(이름·피해 숫자·막대) 누르면 그 포탑(또는 기술병)을 고른다. 판단은 TrySelectDamageRow.
        var hit = row.gameObject.AddComponent<UnityEngine.UI.Image>();
        hit.color = Color.clear;
        hit.raycastTarget = true;
        var icon = Box("RoleIcon", row, new Vector2(0, 1), new Vector2(4, -2), new Vector2(25, 25), false)
            .gameObject.AddComponent<UnityEngine.UI.Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        var label = Text("Value", row, new Vector2(34, -2), new Vector2(182, 28), 17);
        var total = Text("Total", row, new Vector2(216, -2), new Vector2(68, 28), 17);
        total.alignment = TextAnchor.MiddleRight;
        total.resizeTextForBestFit = true;
        total.resizeTextMinSize = 11;
        total.resizeTextMaxSize = 17;
        total.horizontalOverflow = HorizontalWrapMode.Overflow;
        var track = Box("BarTrack", row, new Vector2(0, 1), new Vector2(4, -34), new Vector2(280, 18));
        track.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        var bar = Box("Bar", track, new Vector2(0, 1), new Vector2(4, -4), new Vector2(0, 10), false);
        var fill = bar.gameObject.AddComponent<UnityEngine.UI.Image>();
        fill.color = new Color(.1f, .73f, .77f);
        fill.raycastTarget = false;
        return new DamageRow
        {
            Root = row,
            Bar = bar,
            Icon = icon,
            Label = label,
            Total = total,
        };
    }
}
