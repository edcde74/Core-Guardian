using System;
using CoreGuardian;
using Cysharp.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// CoreGuardianHud: 정보창 칸(11-02 사용자 확정). 창에는 심볼과 숫자만 두고, 마우스를 올리면 이름과 설명을 띄운다.
public sealed partial class CoreGuardianHud
{
    enum InfoTip
    {
        Damage,
        Rate,
        Range,
        Projectile,
        Blast,
        RoundDamage,
        Dps,
        TowerBuff,
        Effect,
        GoodEnemies,
        BadEnemies,
        Order,
        SupportBuff,
        SupportRange,
        Health,
        Defense,
        Speed,
        CoreDamage,
        Gold,
        Weak,
        Resist,
        Ability,
        Slow,
        Burn,
        Frozen,
        Stun,
        Shield,
        Rage,
        Buffed,
        Status,
        EnemyName,
        TowerName,
        Destroy,
    }

    const int InfoCells = 34;
    const int InfoTipDestroy = InfoCells;
    const float InfoWidth = 400,
        InfoPad = 16,
        InfoRowHeight = 32,
        InfoStatWidth = 184,
        InfoIconWidth = 30,
        InfoLabelWidth = 92,
        InfoTipWidth = 300;

    readonly RectTransform[] infoCells = new RectTransform[InfoCells];
    readonly Image[] infoCellIcons = new Image[InfoCells];
    readonly Text[] infoCellTexts = new Text[InfoCells];
    readonly InfoTip[] infoCellTips = new InfoTip[InfoCells];
    readonly int[] infoCellArgs = new int[InfoCells];
    readonly Sprite[] enemyFaces = new Sprite[PreviewSlots];
    RectTransform infoTooltip;
    Text infoTooltipText;
    int infoCellCount,
        infoTipIndex = -1;
    float infoFlowX,
        infoFlowY;

    void BuildInfoCells()
    {
        for (int i = 0; i < InfoCells; i++)
        {
            var cell = Box("Cell" + i, infoPanel, new Vector2(0, 1), Vector2.zero, new Vector2(30, 30), false);
            var hit = cell.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            infoCells[i] = cell;
            infoCellIcons[i] = CoreGuardianHudSkin.Icon(cell, "Damage", new Vector2(0, -2), new Vector2(26, 26));
            var text = Text("Value", cell, new Vector2(32, 0), new Vector2(150, 30), 16);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            infoCellTexts[i] = text;
            int index = i;
            cell.gameObject.AddComponent<CoreGuardianHoverHint>().Changed = on => SetInfoTip(index, on);
            cell.gameObject.SetActive(false);
        }
    }

    void BuildInfoTooltip()
    {
        infoDestroy.gameObject.AddComponent<CoreGuardianHoverHint>().Changed = on => SetInfoTip(InfoTipDestroy, on);
        infoTooltip = Box("InfoTooltip", canvas, Vector2.zero, Vector2.zero, new Vector2(InfoTipWidth, 80));
        infoTooltip.GetComponent<Image>().raycastTarget = false;
        infoTooltipText = Text("Text", infoTooltip, new Vector2(14, -10), new Vector2(InfoTipWidth - 28, 60), 15);
        infoTooltipText.alignment = TextAnchor.UpperLeft;
        infoTooltipText.verticalOverflow = VerticalWrapMode.Overflow;
        infoTooltip.gameObject.SetActive(false);
    }

    void BeginInfoCells()
    {
        infoCellCount = 0;
        infoFlowX = InfoPad;
        infoFlowY = -56;
        hudLine.Clear();
    }

    void NextInfoRow()
    {
        if (infoFlowX <= InfoPad)
            return;

        infoFlowX = InfoPad;
        infoFlowY -= InfoRowHeight;
    }

    // 다음 칸을 흐름대로 놓는다. 표시할 숫자는 hudLine에 담아 둔다(없으면 심볼만).
    void InfoCell(InfoTip tip, Sprite icon, float width, int arg = 0)
    {
        if (infoCellCount >= InfoCells)
        {
            hudLine.Clear();
            return;
        }

        if (infoFlowX + width > InfoWidth - InfoPad)
        {
            NextInfoRow();
        }

        int i = infoCellCount++;
        infoCellTips[i] = tip;
        infoCellArgs[i] = arg;
        if (infoCellIcons[i].sprite != icon)
        {
            infoCellIcons[i].sprite = icon;
        }

        // 11-02 사용자 확정: 심볼이 없는 칸은 줄 이름이나 '-' 글자만 보여 준다.
        SetHudActive(infoCellIcons[i].gameObject, icon != null);
        SetHudPosition(infoCellTexts[i].rectTransform, new Vector2(icon != null ? 32 : 0, 0));
        SetHudText(infoCellTexts[i], ref hudLine);
        hudLine.Clear();
        SetHudPosition(infoCells[i], new Vector2(infoFlowX, infoFlowY));
        SetHudSize(infoCells[i], new Vector2(width, 30));
        SetHudActive(infoCells[i].gameObject, true);
        infoFlowX += width;
    }

    float EndInfoCells()
    {
        for (int i = infoCellCount; i < InfoCells; i++)
        {
            SetHudActive(infoCells[i].gameObject, false);
        }

        NextInfoRow();
        return infoFlowY;
    }

    Sprite EnemyFace(EnemySpec spec, EnemyKind kind)
    {
        int k = PreviewIndex(spec, kind);
        if (enemyFaces[k] == null)
        {
            enemyFaces[k] = Resources.Load<Sprite>("CoreGuardian/Hud/EnemyIcons/" + PreviewKeys[k]);
        }

        return enemyFaces[k];
    }

    static Sprite InfoSymbol(string name) => HudSprite("InfoSymbols/" + name);

    // 줄을 새로 시작하고 맨 앞에 이름을 놓는다.
    void InfoRowLabel(InfoTip tip, string label)
    {
        NextInfoRow();
        hudLine.Append(label);
        InfoCell(tip, null, InfoLabelWidth);
    }

    void InfoEmptyMark(InfoTip tip)
    {
        hudLine.Append("-");
        InfoCell(tip, null, InfoIconWidth);
    }

    void InfoEnemyFaces(InfoTip tip, string label, int towerBit, bool weak)
    {
        InfoRowLabel(tip, label);
        if (!HasEnemyInList(towerBit, weak))
        {
            InfoEmptyMark(tip);
        }

        int seen = 0;
        foreach (var e in game.Session.Data.EnemiesById.Values)
        {
            int mask = weak ? e.WeakTowers : e.ResistTowers;
            int bit = 1 << (int)e.Kind;
            if (
                e.Kind != EnemyKind.SplitChild
                && e.Kind != EnemyKind.Boss
                && (mask & towerBit) != 0
                && (seen & bit) == 0
            )
            {
                seen |= bit;
                InfoCell(InfoTip.EnemyName, EnemyFace(e, e.Kind), InfoIconWidth, (int)e.Kind);
            }
        }
    }

    bool HasEnemyInList(int towerBit, bool weak)
    {
        foreach (var e in game.Session.Data.EnemiesById.Values)
        {
            int mask = weak ? e.WeakTowers : e.ResistTowers;
            if (e.Kind != EnemyKind.SplitChild && e.Kind != EnemyKind.Boss && (mask & towerBit) != 0)
                return true;
        }

        return false;
    }

    void InfoTowerIcons(InfoTip tip, string label, int mask)
    {
        InfoRowLabel(tip, label);
        if (mask == 0)
        {
            InfoEmptyMark(tip);
        }

        foreach (var k in allTowerKinds)
        {
            if ((mask & (1 << (int)k)) != 0)
            {
                InfoCell(InfoTip.TowerName, InstallSprite(k), InfoIconWidth, (int)k);
            }
        }
    }

    void SetInfoTip(int index, bool on)
    {
        if (on)
        {
            infoTipIndex = index;
        }
        else if (infoTipIndex == index)
        {
            infoTipIndex = -1;
        }

        RefreshInfoTip();
    }

    void RefreshInfoTip()
    {
        if (infoTooltip == null)
            return;

        bool show =
            infoTipIndex >= 0
            && infoSubject != InfoSubject.None
            && infoPanel.gameObject.activeSelf
            && (infoTipIndex == InfoTipDestroy || infoTipIndex < infoCellCount);
        if (!show)
        {
            SetHudActive(infoTooltip.gameObject, false);
            return;
        }

        var tip = infoTipIndex == InfoTipDestroy ? InfoTip.Destroy : infoCellTips[infoTipIndex];
        int arg = infoTipIndex == InfoTipDestroy ? 0 : infoCellArgs[infoTipIndex];
        var sb = ZString.CreateStringBuilder(true);
        try
        {
            sb.Append("<color=#f0c45a>");
            sb.Append(InfoTipName(tip, arg));
            sb.Append("</color>\n");
            AppendInfoTip(ref sb, tip);
            SetHudText(infoTooltipText, sb.ToString());
        }
        finally
        {
            sb.Dispose();
        }

        float height = infoTooltipText.preferredHeight + 20;
        SetHudSize(infoTooltip, new Vector2(InfoTipWidth, height));
        var target = infoTipIndex == InfoTipDestroy ? (RectTransform)infoDestroy.transform : infoCells[infoTipIndex];
        var screen = RectTransformUtility.WorldToScreenPoint(null, target.position);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, null, out Vector2 local))
            return;

        var area = canvas.rect.size;
        var p = local + area * .5f;
        float y = p.y + 4;
        if (y + height > area.y - 8)
        {
            y = p.y - target.sizeDelta.y - height - 4;
        }

        SetHudPosition(
            infoTooltip,
            new Vector2(
                Mathf.Round(Mathf.Clamp(p.x, 8, Mathf.Max(8, area.x - InfoTipWidth - 8))),
                Mathf.Round(Mathf.Max(8, y))
            )
        );
        infoTooltip.SetAsLastSibling();
        SetHudActive(infoTooltip.gameObject, true);
    }

    string InfoTipName(InfoTip tip, int arg)
    {
        switch (tip)
        {
            case InfoTip.Damage:
                return "공격력";
            case InfoTip.Rate:
                return "공격 속도";
            case InfoTip.Range:
                return "사거리";
            case InfoTip.Projectile:
                return "탄환 속도";
            case InfoTip.Blast:
                return "범위 폭발";
            case InfoTip.RoundDamage:
                return "이번 라운드 피해";
            case InfoTip.Dps:
                return "초당 피해";
            case InfoTip.TowerBuff:
                return "버프";
            case InfoTip.Effect:
                var kind = Selected() == null ? TowerKind.Rail : Selected().Kind;
                return kind == TowerKind.Frost ? "둔화"
                    : kind == TowerKind.Flame ? "화상"
                    : kind == TowerKind.Multi ? "동시 공격"
                    : "관통";
            case InfoTip.GoodEnemies:
                return "잘 잡는 적";
            case InfoTip.BadEnemies:
                return "약한 적";
            case InfoTip.Order:
                return "공격 순서";
            case InfoTip.SupportBuff:
                return "공격력 증가";
            case InfoTip.SupportRange:
                return "범위";
            case InfoTip.Health:
                return "체력";
            case InfoTip.Defense:
                return "방어";
            case InfoTip.Speed:
                return "이동 속도";
            case InfoTip.CoreDamage:
                return "코어 피해";
            case InfoTip.Gold:
                return "처치 골드";
            case InfoTip.Weak:
                return "약점";
            case InfoTip.Resist:
                return "저항";
            case InfoTip.Ability:
                return "능력";
            case InfoTip.Slow:
                return "둔화";
            case InfoTip.Burn:
                return "화상";
            case InfoTip.Frozen:
                return "얼어붙음";
            case InfoTip.Stun:
                return "기절";
            case InfoTip.Shield:
                return "방패 전개";
            case InfoTip.Rage:
                return "격노";
            case InfoTip.Buffed:
                return "버프 받음";
            case InfoTip.Status:
                return "상태";
            case InfoTip.EnemyName:
                return KindName((EnemyKind)arg);
            case InfoTip.TowerName:
                return TowerName((TowerKind)arg);
            default:
                return "파괴";
        }
    }

    void AppendInfoTip(ref Utf16ValueStringBuilder sb, InfoTip tip)
    {
        var s = game.Session;
        var t = infoSubject == InfoSubject.Tower ? Selected() : null;
        var st = t != null ? t.Stats : s.Player;
        var e = infoEnemy;
        switch (tip)
        {
            case InfoTip.Damage:
                AppendDamageTip(ref sb, t, st);
                break;
            case InfoTip.Rate:
                sb.Append("1초에 ");
                sb.Append(st.Rate, "0.##");
                sb.Append(t != null ? "번 공격합니다." : "발 쏩니다.");
                break;
            case InfoTip.Range:
                sb.Append(st.Range, "0.##");
                sb.Append("칸 안의 적을 공격합니다.");
                break;
            case InfoTip.Projectile:
                sb.Append("탄이 1초에 ");
                sb.Append(st.ProjectileSpeed, "0.##");
                sb.Append("칸 날아갑니다.");
                break;
            case InfoTip.Blast:
                sb.Append("전투 중 사용합니다.");
                if (s.AreaBlast != null && s.Phase == SessionPhase.Combat && !s.AreaBlastReady)
                {
                    sb.Append(" ");
                    sb.Append(Mathf.CeilToInt(s.AreaBlastCooldown));
                    sb.Append("초 뒤 다시 쓸 수 있습니다.");
                }
                break;
            case InfoTip.RoundDamage:
                sb.Append("이번 라운드에 준 피해입니다.");
                break;
            case InfoTip.Dps:
                sb.Append("버프까지 합친 1초당 피해입니다.");
                if (t != null && t.Kind == TowerKind.Multi)
                {
                    sb.Append(" 대상마다, 최대 ");
                    sb.Append(s.Data.Towers[t.Kind].MaxTargets);
                    sb.Append("명입니다.");
                }
                else if (t != null && t.Kind == TowerKind.Frost)
                {
                    sb.Append(" 범위 안 전체에 줍니다.");
                }
                break;
            case InfoTip.TowerBuff:
                sb.Append("지원 포탑이 올려 준 공격력입니다.");
                break;
            case InfoTip.Effect:
                AppendEffectTip(ref sb, t, st);
                break;
            case InfoTip.GoodEnemies:
                sb.Append("이 적들에게 피해 ×");
                sb.Append(WeakMul(), "0.##");
                sb.Append(". 얼굴에 올리면 적 이름이 나옵니다.");
                break;
            case InfoTip.BadEnemies:
                sb.Append("이 적들에게 피해 ×");
                sb.Append(ResistMul(), "0.##");
                sb.Append(".");
                break;
            case InfoTip.Order:
                if (t != null)
                {
                    AppendOrder(ref sb, s.Data.Towers[t.Kind]);
                }
                break;
            case InfoTip.SupportBuff:
                sb.Append("범위 안 공격 포탑의 공격력을 높입니다. 직접 공격하지 않습니다.");
                break;
            case InfoTip.SupportRange:
                sb.Append(st.Range, "0.##");
                sb.Append("칸 안의 포탑에 적용됩니다.");
                break;
            case InfoTip.Health:
                sb.Append("남은 체력 / 최대 체력입니다.");
                break;
            case InfoTip.Defense:
                sb.Append("받는 피해를 ");
                sb.Append(e.Spec.Defense + (e.Shielded ? e.Spec.ShieldDefense : 0), "0.##");
                sb.Append("% 줄입니다.");
                break;
            case InfoTip.Speed:
                sb.Append("1초에 ");
                sb.Append(e.MovementSpeed * (1 + e.SpeedBoostPercent / 100f), "0.##");
                sb.Append("칸 움직입니다. 기본 ");
                sb.Append(e.Spec.Speed, "0.##");
                sb.Append("입니다.");
                break;
            case InfoTip.CoreDamage:
                sb.Append("코어에 닿으면 코어가 ");
                sb.Append(e.Spec.CoreDamage);
                sb.Append(" 줄어듭니다.");
                break;
            case InfoTip.Gold:
                sb.Append("처치하면 ");
                sb.Append(e.Summoned ? 0 : e.Spec.Gold);
                sb.Append("골드를 얻습니다.");
                break;
            case InfoTip.Weak:
                sb.Append("이 포탑들에게 피해를 ×");
                sb.Append(WeakMul(), "0.##");
                sb.Append(" 받습니다. 포탑 그림에 올리면 이름이 나옵니다.");
                break;
            case InfoTip.Resist:
                sb.Append("이 포탑들에게 피해를 ×");
                sb.Append(ResistMul(), "0.##");
                sb.Append(" 받습니다.");
                break;
            case InfoTip.Ability:
                AppendAbility(ref sb, e);
                break;
            case InfoTip.Slow:
                sb.Append("이동 속도가 ");
                sb.Append(e.SlowPercent, "0");
                sb.Append("% 느립니다.");
                break;
            case InfoTip.Burn:
                sb.Append("화상 ");
                sb.Append(e.BurnStacks);
                sb.Append("스택입니다.");
                break;
            case InfoTip.Frozen:
                sb.Append("움직이지 못합니다.");
                break;
            case InfoTip.Stun:
                sb.Append("잠시 아무것도 못 합니다.");
                break;
            case InfoTip.Shield:
                sb.Append("방어가 올라갑니다.");
                break;
            case InfoTip.Rage:
                sb.Append("이동 속도가 올라갑니다.");
                break;
            case InfoTip.Buffed:
                sb.Append("군기병에게 이동 속도를 받고 있습니다.");
                break;
            case InfoTip.Status:
                sb.Append("지금 걸린 효과입니다. 심볼에 올리면 내용이 나옵니다.");
                break;
            case InfoTip.Destroy:
                sb.Append("포탑을 없애고 설치비의 ");
                sb.Append(s.RefundPercent);
                sb.Append("%를 돌려받습니다. 공유 강화는 유지됩니다.");
                break;
            case InfoTip.EnemyName:
                sb.Append("이 적의 이름입니다.");
                break;
            case InfoTip.TowerName:
                sb.Append("이 포탑의 이름입니다.");
                break;
        }
    }

    void AppendDamageTip(ref Utf16ValueStringBuilder sb, TowerState t, CombatStats st)
    {
        var s = game.Session;
        float baseDamage = t != null ? s.Data.Towers[t.Kind].Damage : s.Data.Player.Damage;
        int now = Mathf.FloorToInt(st.Damage + .5f),
            start = Mathf.FloorToInt(baseDamage + .5f);
        if (now != start)
        {
            sb.Append(now);
            sb.Append(" (기본 ");
            sb.Append(start);
            sb.Append(t != null && t.Stars > 1 ? " + 강화·합체 " : " + 강화 ");
            sb.Append(now - start);
            sb.Append("). ");
        }

        sb.Append(t != null ? "한 번 공격의 피해입니다." : "탄 한 발의 피해입니다.");
        if (t == null)
        {
            sb.Append("\n약점·저항은 포탑에만 적용됩니다. 강화는 기술병 작업대(2)에서 합니다.");
        }
    }

    void AppendEffectTip(ref Utf16ValueStringBuilder sb, TowerState t, CombatStats st)
    {
        if (t == null)
            return;

        switch (t.Kind)
        {
            case TowerKind.Frost:
                sb.Append("맞은 적을 ");
                sb.Append(st.SlowPercent, "0.##");
                sb.Append("% 느리게 합니다. ");
                sb.Append(st.SlowDuration, "0.##");
                sb.Append("초 동안입니다.");
                break;
            case TowerKind.Flame:
                sb.Append("화상 1스택당 초당 ");
                sb.Append(st.BurnPerStack, "0.##");
                sb.Append(" 피해를 줍니다. ");
                sb.Append(st.BurnDuration, "0.##");
                sb.Append("초 동안입니다.");
                break;
            case TowerKind.Rail:
                sb.Append("일직선의 적을 꿰뚫습니다. 한 명 뚫을 때마다 피해가 ");
                sb.Append(Mathf.RoundToInt(st.PierceFalloff * 100));
                sb.Append("%로 줄어듭니다.");
                break;
            case TowerKind.Multi:
                sb.Append("최대 ");
                sb.Append(game.Session.Data.Towers[t.Kind].MaxTargets);
                sb.Append("명을 동시에 공격합니다.");
                break;
        }
    }

    bool PointerOverInfoPanel()
    {
        var mouse = Mouse.current;
        return mouse != null
            && RectTransformUtility.RectangleContainsScreenPoint(infoPanel, mouse.position.ReadValue(), null);
    }
}
