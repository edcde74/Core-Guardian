using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// CoreGuardianHud: UI element factory helpers (boxes, texts, buttons).
public sealed partial class CoreGuardianHud
{
    static void SetHudActive(GameObject target, bool active)
    {
        if (target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }

    static void SetHudSize(RectTransform target, Vector2 size)
    {
        if (!target.sizeDelta.Equals(size))
        {
            target.sizeDelta = size;
        }
    }

    static void SetHudPosition(RectTransform target, Vector2 position)
    {
        if (!target.anchoredPosition.Equals(position))
        {
            target.anchoredPosition = position;
        }
    }

    static void SetHudText(UnityEngine.UI.Text target, string value)
    {
        if (target.text != value)
        {
            target.text = value;
        }
    }

    RectTransform Box(
        string name,
        Transform parent,
        Vector2 anchor,
        Vector2 position,
        Vector2 size,
        bool background = true
    )
    {
        var g = new GameObject(name, typeof(RectTransform));
        var r = g.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = anchor;
        r.pivot = anchor;
        r.anchoredPosition = position;
        r.sizeDelta = size;
        if (background)
        {
            var i = g.AddComponent<UnityEngine.UI.Image>();
            CoreGuardianHudSkin.Frame(i);
        }
        return r;
    }

    UnityEngine.UI.Text Text(string name, Transform parent, Vector2 position, Vector2 size, int textSize = 20)
    {
        var r = Box(name, parent, new Vector2(0, 1), position, size, false);
        var t = r.gameObject.AddComponent<UnityEngine.UI.Text>();
        t.font = name == "Title" || name == "Preparation" ? titleFont : font;
        t.fontSize = textSize;
        t.color = Color.white;
        t.raycastTarget = false;
        t.alignment = TextAnchor.MiddleLeft;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        return t;
    }

    UnityEngine.UI.Button Button(
        string name,
        Transform parent,
        Vector2 position,
        Vector2 size,
        Func<string> label,
        Action action,
        Func<bool> enabled = null
    )
    {
        var r = Box(name, parent, new Vector2(0, 1), position, size);
        CoreGuardianHudSkin.Frame(r.GetComponent<UnityEngine.UI.Image>(), "Button");
        var b = r.gameObject.AddComponent<UnityEngine.UI.Button>();
        b.onClick.AddListener(() =>
        {
            if (!b.interactable)
                return;
            if (!name.StartsWith("Preview"))
            {
                game.GetComponent<CoreGuardianCombatAudio>().OrNull()?.PlayCue("ui_click");
            }
            action();
        });
        bool towerChoice = name.StartsWith("Install_") || name == "공격 포탑" || name == "지원 포탑";
        float iconSize = Mathf.Min(towerChoice ? 40 : 30, size.y - 24);
        CoreGuardianHudSkin.Icon(
            r,
            CoreGuardianHudSkin.Symbol(name),
            new Vector2(16, -(size.y - iconSize) / 2),
            new Vector2(iconSize, iconSize)
        );
        float verticalInset = size.y <= 48 ? 8 : 16;
        float labelX = towerChoice ? 66 : 54;
        var t = Text(
            "Label",
            r,
            new Vector2(labelX, -verticalInset),
            size - new Vector2(labelX + 16, verticalInset * 2),
            size.y <= 48 ? 14 : 18
        );
        var reaction = r.gameObject.AddComponent<CoreGuardianHudReaction>();
        // The button name never changes: classify it once instead of per-frame string tests and concatenation.
        bool installation = name == "Installation",
            growth = name == "PlayerGrowth",
            specs = name == "PlayerSpecs",
            specTab = name.EndsWith("SpecTab"),
            buyTab = name.EndsWith("BuyTab"),
            installChoice = name.StartsWith("Install_");
        TowerKind? choiceKind = null;
        if (installChoice)
        {
            foreach (TowerKind kind in Enum.GetValues(typeof(TowerKind)))
            {
                if (name == "Install_" + kind)
                {
                    choiceKind = kind;
                    break;
                }
            }
        }

        reaction.Selected = () =>
            installation ? installOpen
            : growth ? playerOpen && purchaseOpen
            : specs ? playerOpen && !purchaseOpen
            : specTab ? !purchaseOpen
            : buyTab ? purchaseOpen
            : installChoice
                && game.Installing.HasValue
                && choiceKind.HasValue
                && game.Installing.Value == choiceKind.Value;
        void Apply(bool text)
        {
            if (text)
            {
                t.text = label();
            }
            b.interactable = enabled == null || enabled();
            t.color = b.interactable ? new Color(1, .95f, .84f) : new Color(.78f, .8f, .82f);
        }
        // F4: a hidden button (closed panel, developer menu) skips building its label text (state still updates);
        // the text catches up the moment the button is shown.
        refresh.Add(() => Apply(r.gameObject.activeInHierarchy));
        reaction.Shown = () =>
        {
            if (game != null && game.Ready)
            {
                t.text = label(); // text only: button state stays as the last refresh left it
            }
        };
        return b;
    }

    void Pin(string name, Vector2 anchor, Vector2 position)
    {
        var r = canvas.Find(name) as RectTransform;
        if (r == null)
            return;
        r.anchorMin = r.anchorMax = r.pivot = anchor;
        r.anchoredPosition = position;
    }
}
