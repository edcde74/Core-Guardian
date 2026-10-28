using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// CoreGuardianHud: received augment strip, tooltip and Tab list.
public sealed partial class CoreGuardianHud
{
    // ---------------- received augments ----------------
    sealed class AugmentSlot
    {
        public RectTransform Root;
        public Image Frame,
            Icon;
        public Text Count;
        public AugmentSpec Spec;
    }

    AugmentSpec AugmentSpecOf(string id)
    {
        if (augmentById == null)
        {
            augmentById = new Dictionary<string, AugmentSpec>();
            foreach (var a in game.Session.Data.Augments)
            {
                augmentById[a.Id.ToString()] = a;
            }
        }
        return augmentById.TryGetValue(id, out var spec) ? spec : null;
    }

    // F4: sprite names are built once per card number / grade, not on every refresh.
    static readonly Dictionary<int, string> augmentIconNames = new Dictionary<int, string>();
    static readonly Dictionary<AugmentationGrade, string> gradeFrameNames = new Dictionary<AugmentationGrade, string>();

    static Sprite AugmentIcon(AugmentSpec spec)
    {
        if (spec == null)
            return null;
        if (!augmentIconNames.TryGetValue(spec.Number, out var name))
        {
            name = "Augments/augment_" + spec.Number.ToString("00");
            augmentIconNames.Add(spec.Number, name);
        }
        return CoreGuardianHudSkin.Sprite(name);
    }

    static Sprite GradeFrame(AugmentationGrade grade)
    {
        if (!gradeFrameNames.TryGetValue(grade, out var name))
        {
            name = "Redesign/H4_frame_" + grade.ToString().ToLowerInvariant();
            gradeFrameNames.Add(grade, name);
        }
        return CoreGuardianHudSkin.Sprite(name);
    }

    static readonly Dictionary<int, string> countLabels = new Dictionary<int, string>();

    static string CountLabel(int count)
    {
        if (!countLabels.TryGetValue(count, out var label))
        {
            label = "×" + count;
            countLabels.Add(count, label);
        }
        return label;
    }

    static string GradeName(AugmentationGrade grade) =>
        grade == AugmentationGrade.Silver ? "실버"
        : grade == AugmentationGrade.Gold ? "골드"
        : "플래티넘";

    AugmentSlot MakeAugmentSlot(
        Transform parent,
        string name,
        Vector2 position,
        float size,
        Action<AugmentSlot, bool> hover
    )
    {
        var root = Box(name, parent, new Vector2(0, 1), position, new Vector2(size, size), false);
        var frame = root.gameObject.AddComponent<Image>();
        frame.preserveAspect = true;
        frame.raycastTarget = true;
        float scale = size / 36f;
        var icon = Box(
            "Icon",
            root,
            new Vector2(0, 1),
            new Vector2(2 * scale, -2 * scale),
            new Vector2(32 * scale, 32 * scale),
            false
        )
            .gameObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        var count = Text("Count", root, new Vector2(size - 34, -size + 20), new Vector2(36, 20), 14);
        count.alignment = TextAnchor.LowerRight;
        count.color = new Color(1, .95f, .7f);
        count.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
        var slot = new AugmentSlot
        {
            Root = root,
            Frame = frame,
            Icon = icon,
            Count = count,
        };
        var hint = root.gameObject.AddComponent<CoreGuardianHoverHint>();
        hint.Changed = on => hover(slot, on);
        return slot;
    }

    void SetSlot(AugmentSlot slot, AugmentSpec spec, int count)
    {
        slot.Spec = spec;
        SetHudActive(slot.Root.gameObject, spec != null);
        if (spec == null)
            return;

        var frame = GradeFrame(spec.Grade);
        if (slot.Frame.sprite != frame)
        {
            slot.Frame.sprite = frame;
        }

        var icon = AugmentIcon(spec);
        if (slot.Icon.sprite != icon)
        {
            slot.Icon.sprite = icon;
        }

        SetHudText(slot.Count, count > 1 ? CountLabel(count) : "");
    }

    void BuildAugmentStrip()
    {
        augmentStrip = Box(
            "AugmentStrip",
            canvas,
            new Vector2(0, 1),
            new Vector2(16, -300),
            new Vector2(48, 6 * 50 + 40),
            false
        );
        for (int i = 0; i < stripSlots.Length; i++)
        {
            stripSlots[i] = MakeAugmentSlot(
                augmentStrip,
                "Augment" + i,
                new Vector2(0, -i * 50),
                46,
                (slot, on) => ShowAugmentTooltip(slot, on)
            );
        }

        augmentMore = Box("AugmentMore", augmentStrip, new Vector2(0, 1), new Vector2(0, -300), new Vector2(48, 36));
        CoreGuardianHudSkin.Frame(augmentMore.GetComponent<Image>(), "Button");
        var more = augmentMore.gameObject.AddComponent<Button>();
        more.navigation = new Navigation { mode = Navigation.Mode.None };
        refresh.Add(() => more.interactable = true); // The selection modal disables every other button (B5).
        augmentMoreLabel = Text("Label", augmentMore, new Vector2(0, -4), new Vector2(48, 28), 15);
        augmentMoreLabel.alignment = TextAnchor.MiddleCenter;
        augmentMore.gameObject.AddComponent<CoreGuardianHudReaction>();
        more.onClick.AddListener(() =>
        {
            game.GetComponent<CoreGuardianCombatAudio>().OrNull()?.PlayCue("ui_click");
            augmentListPinned = !augmentListPinned;
            RefreshAugmentList();
        });
        augmentTooltip = Box("AugmentTooltip", canvas, new Vector2(0, 1), new Vector2(76, -300), new Vector2(380, 112));
        augmentTooltip.GetComponent<Image>().raycastTarget = false;
        augmentTooltipText = Text("Text", augmentTooltip, new Vector2(16, -10), new Vector2(348, 92), 15);
        augmentTooltipText.alignment = TextAnchor.UpperLeft;
        augmentTooltip.gameObject.SetActive(false);
        // Tab: centred grid of every received card (received order, duplicates ×N) + the hovered card below.
        augmentList = Box("AugmentList", canvas, new Vector2(.5f, .5f), new Vector2(0, 20), new Vector2(880, 600));
        CoreGuardianHudSkin.Icon(augmentList, "Augment", new Vector2(20, -18), new Vector2(30, 30));
        augmentListTitle = Text("Title", augmentList, new Vector2(60, -14), new Vector2(640, 38), 24);
        var hint = Text("Hint", augmentList, new Vector2(560, -14), new Vector2(300, 38), 15);
        hint.alignment = TextAnchor.MiddleRight;
        hint.color = new Color(.75f, .8f, .84f);
        hint.text = "Tab을 누르는 동안 표시";
        augmentGrid = Box("Grid", augmentList, new Vector2(0, 1), new Vector2(24, -64), new Vector2(832, 400), false);
        var detailPlate = Box("Detail", augmentList, new Vector2(0, 1), new Vector2(24, -476), new Vector2(832, 104));
        augmentListDetail = Text("Text", detailPlate, new Vector2(16, -10), new Vector2(800, 84), 16);
        augmentListDetail.alignment = TextAnchor.UpperLeft;
        augmentList.gameObject.SetActive(false);
    }

    void ShowAugmentTooltip(AugmentSlot slot, bool on)
    {
        if (augmentTooltip == null)
            return;

        if (!on || slot.Spec == null || !slot.Root.gameObject.activeInHierarchy)
        {
            SetHudActive(augmentTooltip.gameObject, false);
            return;
        }
        SetHudText(augmentTooltipText, slot.Spec.Name + " · " + GradeName(slot.Spec.Grade) + "\n" + slot.Spec.Text);
        var y = augmentStrip.anchoredPosition.y + slot.Root.anchoredPosition.y;
        SetHudPosition(augmentTooltip, new Vector2(76, y));
        augmentTooltip.SetAsLastSibling();
        SetHudActive(augmentTooltip.gameObject, true);
    }

    void RefreshAugmentStrip()
    {
        if (augmentStrip == null)
            return;

        var selected = game.Session.Augments.Selected;
        bool show = selected.Count > 0 && game.Session.Phase != SessionPhase.Augmentation;
        SetHudActive(augmentStrip.gameObject, show);
        if (!show)
        {
            SetHudActive(augmentTooltip.gameObject, false);
        }
        // Most recent first.
        for (int i = 0; i < stripSlots.Length; i++)
        {
            int index = selected.Count - 1 - i;
            SetSlot(stripSlots[i], index >= 0 ? AugmentSpecOf(selected[index]) : null, 1);
        }
        int extra = selected.Count - stripSlots.Length;
        SetHudActive(augmentMore.gameObject, selected.Count > 0);
        SetHudPosition(augmentMore, new Vector2(0, -Math.Min(selected.Count, stripSlots.Length) * 50));
        SetHudText(augmentMoreLabel, extra > 0 ? "+" + extra : "전체");
        if (extra <= 0)
        {
            augmentMoreLabel.fontSize = 13;
        }
        else
        {
            augmentMoreLabel.fontSize = 15;
        }

        RefreshAugmentList();
    }

    void RefreshAugmentList()
    {
        if (augmentList == null)
            return;

        bool show = AugmentListWanted;
        if (augmentList.gameObject.activeSelf != show)
        {
            augmentList.gameObject.SetActive(show);
            if (show)
            {
                augmentList.SetAsLastSibling();
            }
        }
        if (!show)
            return;

        var selected = game.Session.Augments.Selected;
        SetHudText(augmentListTitle, "받은 증강 " + selected.Count + "장");
        if (augmentListSignature == selected.Count)
            return;

        augmentListSignature = selected.Count;
        var order = new List<string>();
        var counts = new Dictionary<string, int>();
        foreach (var id in selected)
        {
            if (!counts.ContainsKey(id))
            {
                counts[id] = 0;
                order.Add(id);
            }
            counts[id]++;
        }
        while (listSlots.Count < order.Count)
        {
            int n = listSlots.Count;
            listSlots.Add(
                MakeAugmentSlot(
                    augmentGrid,
                    "Card" + n,
                    new Vector2(n % 10 * 83, -n / 10 * 80),
                    72,
                    (slot, on) =>
                    {
                        if (on && slot.Spec != null)
                        {
                            SetHudText(
                                augmentListDetail,
                                slot.Spec.Name + " · " + GradeName(slot.Spec.Grade) + "\n" + slot.Spec.Text
                            );
                        }
                    }
                )
            );
        }
        for (int i = 0; i < listSlots.Count; i++)
        {
            SetSlot(
                listSlots[i],
                i < order.Count ? AugmentSpecOf(order[i]) : null,
                i < order.Count ? counts[order[i]] : 0
            );
        }

        SetHudText(
            augmentListDetail,
            order.Count == 0 ? "아직 받은 증강이 없습니다." : "아이콘에 마우스를 올리면 이름·효과·등급을 봅니다."
        );
    }
}
