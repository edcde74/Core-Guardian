using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
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

    void InfoRow(Sprite icon, string text)
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
        SetHudText(infoRowTexts[i], text);
    }

    static Sprite HudSprite(string name) => CoreGuardianHudSkin.Sprite(name);

    static Sprite H5(string name) => CoreGuardianHudSkin.Sprite("Redesign/H5_" + name);

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
        string note = "";
        bool destroy = false;
        if (infoSubject == InfoSubject.Player)
        {
            var p = s.Player;
            infoIcon.sprite = HudSprite("PlayerIdentity");
            SetHudText(infoTitle, "기술병");
            InfoRow(HudSprite("StatDamage"), "공격력 " + StatDisplay(p.Damage, "damage"));
            InfoRow(HudSprite("StatRate"), "공격속도 " + p.Rate.ToString("0.##") + "발/초");
            InfoRow(HudSprite("StatRange"), "사거리 " + p.Range.ToString("0.##") + "타일");
            InfoRow(HudSprite("StatProjectile"), "탄환 속도 " + p.ProjectileSpeed.ToString("0.##") + "타일/초");
            InfoRow(
                HudSprite("AreaBlast"),
                "범위 폭발 "
                    + (
                        s.AreaBlast == null ? "없음"
                        : s.Phase != SessionPhase.Combat ? "전투 중 사용"
                        : s.AreaBlastReady ? "준비"
                        : Mathf.CeilToInt(s.AreaBlastCooldown) + "초 남음"
                    )
            );
            s.DamageTotals.TryGetValue("Player", out int dealt);
            InfoRow(HudSprite("Damage"), "이번 라운드 피해 " + dealt);
            note = "약점·저항은 포탑에만 적용 · 강화는 기술병 작업대(2)";
        }
        else if (infoSubject == InfoSubject.Tower)
        {
            var t = Selected();
            var spec = s.Data.Towers[t.Kind];
            var st = t.Stats;
            infoIcon.sprite = HudSprite(CoreGuardianHudSkin.Symbol("Install_" + t.Kind));
            SetHudText(infoTitle, TowerName(t.Kind) + " #" + t.Id + (t.Stars > 1 ? " ★" + t.Stars : ""));
            if (t.Kind == TowerKind.Support)
            {
                InfoRow(
                    HudSprite("Augment"),
                    "공격력 증가 +" + st.BuffDamage.ToString("0.##") + "% · 범위 " + st.Range.ToString("0.##") + "타일"
                );
                note = "범위 안 공격 포탑의 공격력을 높입니다. 직접 공격하지 않습니다.";
            }
            else
            {
                float buff = s.TowerDamageBuffPercent(t);
                float hit = st.Damage * (1 + buff / 100f);
                InfoRow(
                    H5("dps"),
                    "초당 피해 "
                        + (hit * s.TowerFireRate(t, game.PlayerTile())).ToString("0.#")
                        + (
                            t.Kind == TowerKind.Multi ? " (대상마다, 최대 " + spec.MaxTargets + "명)"
                            : t.Kind == TowerKind.Frost ? " (범위 안 전체)"
                            : ""
                        )
                        + " · 버프 +"
                        + buff.ToString("0.##")
                        + "%"
                );
                InfoRow(
                    HudSprite("StatDamage"),
                    "피해 "
                        + StatDisplay(st.Damage, "damage")
                        + " · 공속 "
                        + st.Rate.ToString("0.##")
                        + "발/초 · 사거리 "
                        + st.Range.ToString("0.##")
                        + "타일"
                );
                string tag = TowerTagText(t.Kind, st);
                if (tag.Length > 0)
                {
                    InfoRow(HudSprite(CoreGuardianHudSkin.Symbol("Install_" + t.Kind)), "효과: " + tag);
                }

                InfoRow(H5("weakness"), "잘 잡는 적: " + EnemyList(e => (e.WeakTowers & (1 << (int)t.Kind)) != 0));
                InfoRow(H5("resistance"), "약한 적: " + EnemyList(e => (e.ResistTowers & (1 << (int)t.Kind)) != 0));
                s.DamageTotals.TryGetValue(t.AttackSource, out int dealt);
                InfoRow(HudSprite("Damage"), "이번 라운드 피해 " + dealt);
                note = "공격 순서: " + OrderText(spec);
            }
            destroy = true;
        }
        else
        {
            var e = infoEnemy;
            var spec = e.Spec;
            infoIcon.sprite = HudSprite("Damage");
            SetHudText(infoTitle, EnemyName(e) + " · " + EnemyFamily(e));
            InfoRow(H5("hp"), "체력 " + e.Health + " / " + spec.Health);
            float defense = spec.Defense + (e.Shielded ? spec.ShieldDefense : 0);
            InfoRow(
                H5("defense"),
                "방어 " + defense.ToString("0.##") + "% (받는 피해 감소)" + (e.Shielded ? " · 방패 전개" : "")
            );
            InfoRow(
                H5("move_speed"),
                "이동속도 "
                    + (e.MovementSpeed * (1 + e.SpeedBoostPercent / 100f)).ToString("0.##")
                    + "타일/초 (기본 "
                    + spec.Speed.ToString("0.##")
                    + ")"
            );
            InfoRow(H5("core_damage"), "코어 피해 " + spec.CoreDamage);
            InfoRow(HudSprite("Gold"), "처치 골드 " + (e.Summoned ? 0 : spec.Gold));
            InfoRow(H5("weakness"), "약점: " + TowerList(spec.WeakTowers) + " (×" + WeakMul().ToString("0.##") + ")");
            InfoRow(
                H5("resistance"),
                "저항: " + TowerList(spec.ResistTowers) + " (×" + ResistMul().ToString("0.##") + ")"
            );
            note = "능력: " + AbilityText(e) + "\n상태: " + StatusText(e);
        }
        for (int i = infoRowCount; i < InfoRows; i++)
        {
            SetHudActive(infoRowIcons[i].gameObject, false);
            SetHudActive(infoRowTexts[i].gameObject, false);
        }
        float noteY = -60 - infoRowCount * 32;
        SetHudPosition(infoNote.rectTransform, new Vector2(18, noteY));
        SetHudText(infoNote, note);
        float bottom = noteY - (note.Length > 0 ? 60 : 0);
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

    string TowerList(int mask)
    {
        if (mask == 0)
            return "없음";

        var names = new List<string>();
        foreach (TowerKind k in Enum.GetValues(typeof(TowerKind)))
        {
            if ((mask & (1 << (int)k)) != 0)
            {
                names.Add(TowerName(k));
            }
        }

        return string.Join(", ", names);
    }

    string EnemyList(Func<EnemySpec, bool> match)
    {
        var names = new List<string>();
        var seen = new HashSet<EnemyKind>();
        foreach (var e in game.Session.Data.EnemiesById.Values)
        {
            if (e.Kind != EnemyKind.SplitChild && e.Kind != EnemyKind.Boss && match(e) && seen.Add(e.Kind))
            {
                names.Add(KindName(e.Kind));
            }
        }

        return names.Count == 0 ? "없음" : string.Join(", ", names);
    }
}
