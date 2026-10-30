using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using Cysharp.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// CoreGuardianHud: selection info panel (player, tower, enemy).
public sealed partial class CoreGuardianHud
{
    enum InfoSubject
    {
        None,
        Player,
        Tower,
        Enemy,
    }

    // ---------------- selection ----------------
    void ReleaseInfo()
    {
        infoSubject = InfoSubject.None;
        infoEnemy = default;
        lastTower = -1;
        game.ClearInspectionSelection();
    }

    public void ShowPlayerInfo()
    {
        if (InfoBlocked)
            return;

        if (infoSubject != InfoSubject.Player)
        {
            game.GetComponent<CoreGuardianCombatAudio>().OrNull()?.PlayCue("ui_open");
        }

        game.ClearInspectionSelection();
        infoSubject = InfoSubject.Player;
        infoEnemy = default;
        lastTower = -1;
        Refresh();
    }

    public void ShowTowerInfo()
    {
        var t = Selected();
        if (t == null || InfoBlocked)
            return;

        if (infoSubject != InfoSubject.Tower || lastTower != t.Id)
        {
            game.GetComponent<CoreGuardianCombatAudio>().OrNull()?.PlayCue("ui_open");
        }

        infoSubject = InfoSubject.Tower;
        infoEnemy = default;
        lastTower = t.Id;
        Refresh();
    }

    public void ShowEnemyInfo(EnemyHandle enemy)
    {
        if (InfoBlocked || !enemy.Alive)
            return;

        if (infoSubject != InfoSubject.Enemy || infoEnemy != enemy)
        {
            game.GetComponent<CoreGuardianCombatAudio>().OrNull()?.PlayCue("ui_open");
        }

        game.ClearInspectionSelection();
        lastTower = -1;
        infoSubject = InfoSubject.Enemy;
        infoEnemy = enemy;
        Refresh();
    }

    void BuildSelectionInfo()
    {
        // 왼쪽 아래 구석의 포탑 연구소 건물과 받은 증강 줄을 가리지 않게 그 위·오른쪽에 둠 (10-28 사용자 확정: 정보창 위치 변경).
        infoPanel = Box("SelectionInfo", canvas, Vector2.zero, new Vector2(76, 200), new Vector2(440, 400));
        infoIcon = CoreGuardianHudSkin.Icon(infoPanel, "PlayerIdentity", new Vector2(16, -14), new Vector2(30, 30));
        infoTitle = Text("Title", infoPanel, new Vector2(54, -10), new Vector2(318, 38), 20);
        Button(
            "InfoClose",
            infoPanel,
            new Vector2(440 - 62, -10),
            new Vector2(46, 40),
            () => "",
            () =>
            {
                ReleaseInfo();
                Refresh();
            }
        );
        for (int i = 0; i < InfoRows; i++)
        {
            infoRowIcons[i] = CoreGuardianHudSkin.Icon(
                infoPanel,
                "Damage",
                new Vector2(18, -58 - i * 32),
                new Vector2(26, 26)
            );
            infoRowTexts[i] = Text("Row" + i, infoPanel, new Vector2(52, -56 - i * 32), new Vector2(372, 30), 16);
        }
        infoNote = Text("Note", infoPanel, new Vector2(18, -60), new Vector2(406, 60), 14);
        infoNote.alignment = TextAnchor.UpperLeft;
        infoNote.color = new Color(.82f, .86f, .88f);
        infoDestroy = Button(
            "Destroy",
            infoPanel,
            new Vector2(16, -300),
            new Vector2(408, 44),
            () => "파괴 · 설치비 " + game.Session.RefundPercent + "% 반환 · 공유 강화 유지",
            () =>
            {
                game.DestroySelected();
                Refresh();
            },
            () => CanBuyPlayer && Selected() != null
        );
        infoPanel.gameObject.SetActive(false);
    }

    // Shows the line built in hudLine as the next info row.
    void InfoRow(Sprite icon)
    {
        if (infoRowCount >= InfoRows)
            return;

        int i = infoRowCount++;
        if (infoRowIcons[i].sprite != icon)
        {
            infoRowIcons[i].sprite = icon;
        }

        SetHudActive(infoRowIcons[i].gameObject, icon != null);
        SetHudActive(infoRowTexts[i].gameObject, true);
        SetHudText(infoRowTexts[i], ref hudLine);
    }

    static Sprite HudSprite(string name) => CoreGuardianHudSkin.Sprite(name);

    void RefreshSelectionInfo()
    {
        if (infoPanel == null)
            return;

        var s = game.Session;
        if (infoSubject == InfoSubject.Tower)
        {
            var t = Selected();
            if (t == null || t.Id != lastTower)
            {
                infoSubject = InfoSubject.None;
                lastTower = -1;
            }
        }
        if (infoSubject == InfoSubject.Enemy && !infoEnemy.Alive)
        {
            infoSubject = InfoSubject.None;
            infoEnemy = default;
        }
        bool show = infoSubject != InfoSubject.None && s.Phase != SessionPhase.Augmentation;
        SetHudActive(infoPanel.gameObject, show);
        if (!show)
            return;

        infoRowCount = 0;
        hudNote.Clear();
        bool destroy = false;
        if (infoSubject == InfoSubject.Player)
        {
            var p = s.Player;
            infoIcon.sprite = HudSprite("PlayerIdentity");
            SetHudText(infoTitle, "기술병");
            Line("공격력 ");
            AppendStat(ref hudLine, p.Damage, "damage");
            InfoRow(HudSprite("StatDamage"));
            Line("공격속도 ");
            hudLine.Append(p.Rate, "0.##");
            hudLine.Append("발/초");
            InfoRow(HudSprite("StatRate"));
            Line("사거리 ");
            hudLine.Append(p.Range, "0.##");
            hudLine.Append("타일");
            InfoRow(HudSprite("StatRange"));
            Line("탄환 속도 ");
            hudLine.Append(p.ProjectileSpeed, "0.##");
            hudLine.Append("타일/초");
            InfoRow(HudSprite("StatProjectile"));
            Line("범위 폭발 ");
            if (s.AreaBlast == null)
            {
                hudLine.Append("없음");
            }
            else if (s.Phase != SessionPhase.Combat)
            {
                hudLine.Append("전투 중 사용");
            }
            else if (s.AreaBlastReady)
            {
                hudLine.Append("준비");
            }
            else
            {
                hudLine.Append(Mathf.CeilToInt(s.AreaBlastCooldown));
                hudLine.Append("초 남음");
            }
            InfoRow(HudSprite("AreaBlast"));
            s.DamageTotals.TryGetValue("Player", out int dealt);
            Line("이번 라운드 피해 ");
            hudLine.Append(dealt);
            InfoRow(HudSprite("Damage"));
            hudNote.Append("약점·저항은 포탑에만 적용 · 강화는 기술병 작업대(2)");
        }
        else if (infoSubject == InfoSubject.Tower)
        {
            var t = Selected();
            var spec = s.Data.Towers[t.Kind];
            var st = t.Stats;
            infoIcon.sprite = InstallSprite(t.Kind);
            Line(TowerName(t.Kind));
            hudLine.Append(" #");
            hudLine.Append(t.Id);
            if (t.Stars > 1)
            {
                hudLine.Append(" ★");
                hudLine.Append(t.Stars);
            }
            SetHudText(infoTitle, ref hudLine);
            if (t.Kind == TowerKind.Support)
            {
                Line("공격력 증가 +");
                hudLine.Append(st.BuffDamage, "0.##");
                hudLine.Append("% · 범위 ");
                hudLine.Append(st.Range, "0.##");
                hudLine.Append("타일");
                InfoRow(HudSprite("Augment"));
                hudNote.Append("범위 안 공격 포탑의 공격력을 높입니다. 직접 공격하지 않습니다.");
            }
            else
            {
                float buff = s.TowerDamageBuffPercent(t);
                float hit = st.Damage * (1 + buff / 100f);
                Line("초당 피해 ");
                hudLine.Append(hit * s.TowerFireRate(t, game.PlayerTile()), "0.#");
                if (t.Kind == TowerKind.Multi)
                {
                    hudLine.Append(" (대상마다, 최대 ");
                    hudLine.Append(spec.MaxTargets);
                    hudLine.Append("명)");
                }
                else if (t.Kind == TowerKind.Frost)
                {
                    hudLine.Append(" (범위 안 전체)");
                }
                hudLine.Append(" · 버프 +");
                hudLine.Append(buff, "0.##");
                hudLine.Append("%");
                InfoRow(H5("dps"));
                Line("피해 ");
                AppendStat(ref hudLine, st.Damage, "damage");
                hudLine.Append(" · 공속 ");
                hudLine.Append(st.Rate, "0.##");
                hudLine.Append("발/초 · 사거리 ");
                hudLine.Append(st.Range, "0.##");
                hudLine.Append("타일");
                InfoRow(HudSprite("StatDamage"));
                if (HasTowerTag(t.Kind))
                {
                    Line("효과: ");
                    AppendTowerTag(ref hudLine, t.Kind, st);
                    InfoRow(InstallSprite(t.Kind));
                }

                Line("잘 잡는 적: ");
                AppendEnemyList(ref hudLine, 1 << (int)t.Kind, true);
                InfoRow(H5("weakness"));
                Line("약한 적: ");
                AppendEnemyList(ref hudLine, 1 << (int)t.Kind, false);
                InfoRow(H5("resistance"));
                s.DamageTotals.TryGetValue(t.AttackSource, out int dealt);
                Line("이번 라운드 피해 ");
                hudLine.Append(dealt);
                InfoRow(HudSprite("Damage"));
                hudNote.Append("공격 순서: ");
                AppendOrder(ref hudNote, spec);
            }
            destroy = true;
        }
        else
        {
            var e = infoEnemy;
            var spec = e.Spec;
            infoIcon.sprite = HudSprite("Damage");
            hudLine.Clear();
            AppendEnemyName(ref hudLine, e);
            hudLine.Append(" · ");
            hudLine.Append(EnemyFamily(e));
            SetHudText(infoTitle, ref hudLine);
            Line("체력 ");
            hudLine.Append(e.Health);
            hudLine.Append(" / ");
            hudLine.Append(spec.Health);
            InfoRow(H5("hp"));
            float defense = spec.Defense + (e.Shielded ? spec.ShieldDefense : 0);
            Line("방어 ");
            hudLine.Append(defense, "0.##");
            hudLine.Append("% (받는 피해 감소)");
            if (e.Shielded)
            {
                hudLine.Append(" · 방패 전개");
            }
            InfoRow(H5("defense"));
            Line("이동속도 ");
            hudLine.Append(e.MovementSpeed * (1 + e.SpeedBoostPercent / 100f), "0.##");
            hudLine.Append("타일/초 (기본 ");
            hudLine.Append(spec.Speed, "0.##");
            hudLine.Append(")");
            InfoRow(H5("move_speed"));
            Line("코어 피해 ");
            hudLine.Append(spec.CoreDamage);
            InfoRow(H5("core_damage"));
            Line("처치 골드 ");
            hudLine.Append(e.Summoned ? 0 : spec.Gold);
            InfoRow(HudSprite("Gold"));
            Line("약점: ");
            AppendTowerList(ref hudLine, spec.WeakTowers);
            hudLine.Append(" (×");
            hudLine.Append(WeakMul(), "0.##");
            hudLine.Append(")");
            InfoRow(H5("weakness"));
            Line("저항: ");
            AppendTowerList(ref hudLine, spec.ResistTowers);
            hudLine.Append(" (×");
            hudLine.Append(ResistMul(), "0.##");
            hudLine.Append(")");
            InfoRow(H5("resistance"));
            hudNote.Append("능력: ");
            AppendAbility(ref hudNote, e);
            hudNote.Append("\n상태: ");
            AppendStatus(ref hudNote, e);
        }
        for (int i = infoRowCount; i < InfoRows; i++)
        {
            SetHudActive(infoRowIcons[i].gameObject, false);
            SetHudActive(infoRowTexts[i].gameObject, false);
        }
        float noteY = -60 - infoRowCount * 32;
        SetHudPosition(infoNote.rectTransform, new Vector2(18, noteY));
        SetHudText(infoNote, ref hudNote);
        float bottom = noteY - (hudNote.Length > 0 ? 60 : 0);
        SetHudActive(infoDestroy.gameObject, destroy);
        if (destroy)
        {
            SetHudPosition((RectTransform)infoDestroy.transform, new Vector2(16, bottom - 4));
            bottom -= 52;
        }
        SetHudSize(infoPanel, new Vector2(440, -bottom + 12));
    }

    float WeakMul()
    {
        var s = game.Session;
        return s.Augments.Has(49)
            ? s.Augments.Value(49) / s.Augments.Count(49)
            : s.Data.Tag("Affinity", "weakMultiplier", false, 1.3f);
    }

    float ResistMul()
    {
        var s = game.Session;
        return s.Augments.Has(49)
            ? s.Augments.Value(49, 1) / s.Augments.Count(49)
            : s.Data.Tag("Affinity", "resistMultiplier", false, .7f);
    }

    void AppendTowerList(ref Utf16ValueStringBuilder sb, int mask)
    {
        if (mask == 0)
        {
            sb.Append("없음");
            return;
        }

        bool any = false;
        foreach (var k in allTowerKinds)
        {
            if ((mask & (1 << (int)k)) != 0)
            {
                Separate(ref sb, ref any, ", ");
                sb.Append(TowerName(k));
            }
        }
    }

    // Non-boss enemy kinds whose weakness (or resistance) mask includes the given tower bit.
    void AppendEnemyList(ref Utf16ValueStringBuilder sb, int towerBit, bool weak)
    {
        int seen = 0;
        bool any = false;
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
                Separate(ref sb, ref any, ", ");
                sb.Append(KindName(e.Kind));
            }
        }

        if (!any)
        {
            sb.Append("없음");
        }
    }
}
