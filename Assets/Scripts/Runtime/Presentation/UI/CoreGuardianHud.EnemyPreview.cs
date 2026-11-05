using CoreGuardian;
using Cysharp.Text;
using UnityEngine;
using UnityEngine.UI;

// CoreGuardianHud: 다음에 나올 적 줄 (10-30 사용자 요청 #9).
// 라운드 표시 바로 아래 한 줄에 적 얼굴 아이콘 × 마리 수. 준비·증강 중엔 이번 라운드에 나올 적 전체,
// 전투 중엔 남은 적(살아 있는 적 + 아직 안 나온 적). 위쪽 접기 손잡이로 접고 펼친다.
public sealed partial class CoreGuardianHud
{
    const int PreviewSlots = 12;
    const float PreviewSlotWidth = 78;

    // 아이콘 순서 = 줄에 놓는 순서(보스 먼저).
    static readonly string[] PreviewKeys =
    {
        "iron",
        "crimson",
        "boss",
        "attack",
        "defense",
        "buff",
        "charger",
        "splitter",
        "splitchild",
        "healer",
        "armored",
        "flyer",
    };

    RectTransform enemyPreview;
    readonly Image[] previewIcons = new Image[PreviewSlots];
    readonly Text[] previewCounts = new Text[PreviewSlots];
    readonly int[] previewTally = new int[PreviewSlots];
    readonly int[] previewShown = new int[PreviewSlots];
    int previewSlotsShown = -1;

    // 10-31 사용자 요청: 얼굴에 마우스를 올리면 이름·특성·약점·저항 설명.
    readonly EnemySpec[] previewSpecs = new EnemySpec[PreviewSlots];
    readonly EnemyKind[] previewKinds = new EnemyKind[PreviewSlots];
    readonly int[] previewSlotKey = new int[PreviewSlots];
    RectTransform previewTooltip;
    Text previewTooltipText;

    static int PreviewIndex(EnemySpec spec, EnemyKind kind)
    {
        switch (kind)
        {
            case EnemyKind.Boss:
                return spec != null && spec.Key == "IronKnight" ? 0
                    : spec != null && spec.Key == "CrimsonLord" ? 1
                    : 2;
            case EnemyKind.Attack:
                return 3;
            case EnemyKind.Defense:
                return 4;
            case EnemyKind.Buff:
                return 5;
            case EnemyKind.Charger:
                return 6;
            case EnemyKind.Splitter:
                return 7;
            case EnemyKind.SplitChild:
                return 8;
            case EnemyKind.Healer:
                return 9;
            case EnemyKind.Armored:
                return 10;
            default:
                return 11;
        }
    }

    void BuildEnemyPreview()
    {
        enemyPreview = Box(
            "EnemyPreview",
            canvas,
            new Vector2(.5f, 1),
            new Vector2(0, -156),
            new Vector2(16 + PreviewSlots * PreviewSlotWidth, 42)
        );
        enemyPreview.GetComponent<Image>().raycastTarget = false;
        for (int i = 0; i < PreviewSlots; i++)
        {
            var slot = Box(
                "Slot" + i,
                enemyPreview,
                new Vector2(0, 1),
                Vector2.zero,
                new Vector2(PreviewSlotWidth, 42),
                false
            );
            var icon = Box("Icon", slot, new Vector2(0, 1), new Vector2(0, -6), new Vector2(30, 30), false)
                .gameObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            var count = Text("Count", slot, new Vector2(34, -5), new Vector2(PreviewSlotWidth - 34, 32), 17);
            count.color = new Color(1, .93f, .74f);
            count.horizontalOverflow = HorizontalWrapMode.Overflow;
            count.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
            previewIcons[i] = icon;
            previewCounts[i] = count;
            previewShown[i] = -1;
            var hit = slot.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            int index = i;
            slot.gameObject.AddComponent<CoreGuardianHoverHint>().Changed = on => ShowPreviewTooltip(index, on);
            slot.gameObject.SetActive(false);
        }
        enemyPreview.gameObject.SetActive(false);
        previewTooltip = Box("EnemyPreviewTooltip", canvas, new Vector2(.5f, 1), Vector2.zero, new Vector2(440, 112));
        previewTooltip.GetComponent<Image>().raycastTarget = false;
        previewTooltipText = Text("Text", previewTooltip, new Vector2(16, -10), new Vector2(408, 92), 15);
        previewTooltipText.alignment = TextAnchor.UpperLeft;
        previewTooltip.gameObject.SetActive(false);
    }

    // 지금 보여 줄 적 수를 previewTally에 센다(할당 없음).
    void TallyEnemyPreview()
    {
        var s = game.Session;
        System.Array.Clear(previewTally, 0, PreviewSlots);
        bool combat = s.Phase == SessionPhase.Combat;
        foreach (var row in s.Data.Waves)
        {
            if (row.Wave != s.Wave)
                continue;

            int left = combat ? Mathf.Max(0, row.Count - s.SpawnedCount(row)) : row.Count;
            int k = PreviewIndex(row.Enemy, row.Kind);
            previewTally[k] += left;
            previewSpecs[k] = row.Enemy;
            previewKinds[k] = row.Kind;
        }
        if (!combat)
            return;

        foreach (var e in s.Enemies)
        {
            if (e.Alive)
            {
                int k = PreviewIndex(e.Spec, e.Kind);
                previewTally[k]++;
                previewSpecs[k] = e.Spec;
                previewKinds[k] = e.Kind;
            }
        }
    }

    void RefreshEnemyPreview(bool visible)
    {
        if (enemyPreview == null)
            return;

        var phase = game.Session.Phase;
        visible &= phase == SessionPhase.Preparation || phase == SessionPhase.Combat;
        SetHudActive(enemyPreview.gameObject, visible);
        if (!visible)
        {
            SetHudActive(previewTooltip.gameObject, false);
            return;
        }

        TallyEnemyPreview();
        int shown = 0;
        bool changed = false;
        for (int k = 0; k < PreviewSlots; k++)
        {
            if (previewTally[k] <= 0)
                continue;

            previewSlotKey[shown] = k;

            if (previewShown[shown] != previewTally[k] * 16 + k)
            {
                previewShown[shown] = previewTally[k] * 16 + k;
                previewIcons[shown].sprite = Resources.Load<Sprite>("CoreGuardian/Hud/EnemyIcons/" + PreviewKeys[k]);
                SetHudText(previewCounts[shown], "×" + previewTally[k]);
                changed = true;
            }
            shown++;
        }
        if (!changed && shown == previewSlotsShown)
            return;

        previewSlotsShown = shown;
        float width = 16 + Mathf.Max(1, shown) * PreviewSlotWidth;
        SetHudSize(enemyPreview, new Vector2(width, 42));
        for (int i = 0; i < PreviewSlots; i++)
        {
            var slot = (RectTransform)previewIcons[i].transform.parent;
            SetHudActive(slot.gameObject, i < shown);
            SetHudPosition(slot, new Vector2(12 + i * PreviewSlotWidth, 0));
            if (i >= shown)
            {
                previewShown[i] = -1;
            }
        }
    }

    void ShowPreviewTooltip(int slot, bool on)
    {
        if (previewTooltip == null)
            return;

        var spec = on && slot < previewSlotsShown ? previewSpecs[previewSlotKey[slot]] : null;
        if (spec == null)
        {
            SetHudActive(previewTooltip.gameObject, false);
            return;
        }

        var kind = previewKinds[previewSlotKey[slot]];
        var sb = ZString.CreateStringBuilder(true);
        try
        {
            AppendEnemyName(ref sb, kind, spec);
            sb.Append(" · ");
            sb.Append(EnemyFamily(kind, spec));
            sb.Append("\n특성: ");
            AppendAbility(ref sb, kind, spec);
            sb.Append("\n약점: ");
            AppendTowerList(ref sb, spec.WeakTowers);
            sb.Append(" (×");
            sb.Append(WeakMul(), "0.##");
            sb.Append(")\n저항: ");
            AppendTowerList(ref sb, spec.ResistTowers);
            sb.Append(" (×");
            sb.Append(ResistMul(), "0.##");
            sb.Append(")");
            SetHudText(previewTooltipText, sb.ToString());
        }
        finally
        {
            sb.Dispose();
        }

        var slotRect = (RectTransform)previewIcons[slot].transform.parent;
        float left = enemyPreview.anchoredPosition.x - enemyPreview.sizeDelta.x * .5f;
        SetHudPosition(
            previewTooltip,
            new Vector2(
                left + slotRect.anchoredPosition.x + PreviewSlotWidth * .5f,
                enemyPreview.anchoredPosition.y - enemyPreview.sizeDelta.y - 6
            )
        );
        previewTooltip.SetAsLastSibling();
        SetHudActive(previewTooltip.gameObject, true);
    }
}
