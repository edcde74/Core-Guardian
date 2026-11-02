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
        // 10-30 사용자 결정: 자리는 PlaceInfoPanel이 매 프레임 누른 물체의 오른쪽 위 끝에 맞춘다.
        infoPanel = Box("SelectionInfo", canvas, Vector2.zero, new Vector2(76, 200), new Vector2(InfoWidth, 400));
        infoIcon = CoreGuardianHudSkin.Icon(infoPanel, "PlayerIdentity", new Vector2(16, -14), new Vector2(30, 30));
        infoTitle = Text("Title", infoPanel, new Vector2(54, -10), new Vector2(InfoWidth - 122, 38), 20);
        infoTitle.resizeTextForBestFit = true;
        infoTitle.resizeTextMinSize = 13;
        infoTitle.resizeTextMaxSize = 20;
        Button(
            "InfoClose",
            infoPanel,
            new Vector2(InfoWidth - 62, -10),
            new Vector2(46, 40),
            () => "",
            () =>
            {
                ReleaseInfo();
                Refresh();
            }
        );
        BuildInfoCells();
        infoDestroy = Button(
            "Destroy",
            infoPanel,
            new Vector2(16, -300),
            new Vector2(InfoWidth - 32, 44),
            () => Selected() == null ? "" : (Selected().Spent * game.Session.RefundPercent / 100).ToString(),
            () =>
            {
                game.DestroySelected();
                Refresh();
            },
            () => CanBuyPlayer && Selected() != null
        );
        CoreGuardianHudSkin.Icon(infoDestroy.transform, "Crystal", new Vector2(82, -10), new Vector2(24, 24));
        BuildInfoTooltip();
        infoPanel.gameObject.SetActive(false);
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

        BeginInfoCells();
        bool destroy = false;
        if (infoSubject == InfoSubject.Player)
        {
            var p = s.Player;
            infoIcon.sprite = HudSprite("PlayerIdentity");
            SetHudText(infoTitle, "기술병");
            AppendStat(ref hudLine, p.Damage, "damage");
            InfoCell(InfoTip.Damage, HudSprite("StatDamage"), InfoStatWidth);
            hudLine.Append(p.Rate, "0.##");
            InfoCell(InfoTip.Rate, HudSprite("StatRate"), InfoStatWidth);
            hudLine.Append(p.Range, "0.##");
            InfoCell(InfoTip.Range, HudSprite("StatRange"), InfoStatWidth);
            hudLine.Append(p.ProjectileSpeed, "0.##");
            InfoCell(InfoTip.Projectile, HudSprite("StatProjectile"), InfoStatWidth);
            if (s.AreaBlast == null)
            {
                hudLine.Append("-");
            }
            else if (s.Phase != SessionPhase.Combat || s.AreaBlastReady)
            {
                hudLine.Append("준비");
            }
            else
            {
                hudLine.Append(Mathf.CeilToInt(s.AreaBlastCooldown));
                hudLine.Append("초");
            }
            InfoCell(InfoTip.Blast, HudSprite("AreaBlast"), InfoStatWidth);
            s.DamageTotals.TryGetValue("Player", out int dealt);
            hudLine.Append(dealt);
            InfoCell(InfoTip.RoundDamage, HudSprite("Damage"), InfoStatWidth);
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
            hudLine.Clear();
            if (t.Kind == TowerKind.Support)
            {
                hudLine.Append("+");
                hudLine.Append(st.BuffDamage, "0.##");
                hudLine.Append("%");
                InfoCell(InfoTip.SupportBuff, HudSprite("Augment"), InfoStatWidth);
                hudLine.Append(st.Range, "0.##");
                InfoCell(InfoTip.SupportRange, HudSprite("StatRange"), InfoStatWidth);
            }
            else
            {
                float buff = s.TowerDamageBuffPercent(t);
                float hit = st.Damage * (1 + buff / 100f);
                hudLine.Append(hit * s.TowerFireRate(t, game.PlayerTile()), "0.#");
                InfoCell(InfoTip.Dps, H5("dps"), InfoStatWidth);
                AppendStat(ref hudLine, st.Damage, "damage");
                InfoCell(InfoTip.Damage, HudSprite("StatDamage"), InfoStatWidth);
                hudLine.Append(st.Rate, "0.##");
                InfoCell(InfoTip.Rate, HudSprite("StatRate"), InfoStatWidth);
                hudLine.Append(st.Range, "0.##");
                InfoCell(InfoTip.Range, HudSprite("StatRange"), InfoStatWidth);
                hudLine.Append("+");
                hudLine.Append(buff, "0.##");
                hudLine.Append("%");
                InfoCell(InfoTip.TowerBuff, InstallSprite(TowerKind.Support), InfoStatWidth);
                if (HasTowerTag(t.Kind))
                {
                    AppendTowerTagValue(ref hudLine, t.Kind, st);
                    InfoCell(InfoTip.Effect, InstallSprite(t.Kind), InfoStatWidth);
                }

                s.DamageTotals.TryGetValue(t.AttackSource, out int dealt);
                hudLine.Append(dealt);
                InfoCell(InfoTip.RoundDamage, HudSprite("Damage"), InfoStatWidth);
                NextInfoRow();
                InfoEnemyFaces(InfoTip.GoodEnemies, H5("weakness"), 1 << (int)t.Kind, true);
                InfoEnemyFaces(InfoTip.BadEnemies, H5("resistance"), 1 << (int)t.Kind, false);
                InfoCell(InfoTip.Order, InfoSymbol("Order"), InfoIconWidth);
            }
            destroy = true;
        }
        else
        {
            var e = infoEnemy;
            var spec = e.Spec;
            infoIcon.sprite = EnemyFace(spec, e.Kind);
            hudLine.Clear();
            AppendEnemyName(ref hudLine, e);
            hudLine.Append(" · ");
            hudLine.Append(EnemyFamily(e));
            SetHudText(infoTitle, ref hudLine);
            hudLine.Clear();
            hudLine.Append(e.Health);
            hudLine.Append(" / ");
            hudLine.Append(spec.Health);
            InfoCell(InfoTip.Health, H5("hp"), InfoStatWidth);
            float defense = spec.Defense + (e.Shielded ? spec.ShieldDefense : 0);
            hudLine.Append(defense, "0.##");
            hudLine.Append("%");
            InfoCell(InfoTip.Defense, H5("defense"), InfoStatWidth);
            hudLine.Append(e.MovementSpeed * (1 + e.SpeedBoostPercent / 100f), "0.##");
            InfoCell(InfoTip.Speed, H5("move_speed"), InfoStatWidth);
            hudLine.Append(spec.CoreDamage);
            InfoCell(InfoTip.CoreDamage, H5("core_damage"), InfoStatWidth);
            hudLine.Append(e.Summoned ? 0 : spec.Gold);
            InfoCell(InfoTip.Gold, HudSprite("Gold"), InfoStatWidth);
            NextInfoRow();
            InfoTowerIcons(InfoTip.Weak, H5("weakness"), spec.WeakTowers);
            InfoTowerIcons(InfoTip.Resist, H5("resistance"), spec.ResistTowers);
            hudNote.Clear();
            AppendAbility(ref hudNote, e);
            if (!hudNote.AsSpan().SequenceEqual("없음".AsSpan()))
            {
                InfoCell(InfoTip.Ability, InfoSymbol("Ability"), InfoIconWidth);
            }
            NextInfoRow();
            AddStatusCells(e);
        }
        float bottom = EndInfoCells() - 4;
        SetHudActive(infoDestroy.gameObject, destroy);
        if (destroy)
        {
            SetHudPosition((RectTransform)infoDestroy.transform, new Vector2(16, bottom));
            bottom -= 48;
        }
        SetHudSize(infoPanel, new Vector2(InfoWidth, -bottom + 12));
        RefreshInfoTip();
    }

    // 걸린 상태만 심볼로 보여 준다.
    void AddStatusCells(EnemyHandle e)
    {
        float slow = e.SlowPercent;
        if (slow > 0)
        {
            hudLine.Append(slow, "0");
            hudLine.Append("%");
            InfoCell(InfoTip.Slow, HudSprite("DamageFrost"), 76);
        }

        if (e.BurnStacks > 0)
        {
            hudLine.Append(e.BurnStacks);
            InfoCell(InfoTip.Burn, HudSprite("DamageFlame"), 60);
        }

        if (e.Frozen)
        {
            InfoCell(InfoTip.Frozen, InfoSymbol("Frozen"), InfoIconWidth);
        }
        else if (e.Stunned)
        {
            InfoCell(InfoTip.Stun, InfoSymbol("Stun"), InfoIconWidth);
        }

        if (e.Shielded)
        {
            InfoCell(InfoTip.Shield, InfoSymbol("Shield"), InfoIconWidth);
        }

        if (e.Enraged)
        {
            InfoCell(InfoTip.Rage, InfoSymbol("Rage"), InfoIconWidth);
        }

        if (e.Buffed)
        {
            InfoCell(InfoTip.Buffed, InfoSymbol("Buffed"), InfoIconWidth);
        }
    }

    // 10-30 사용자 결정: 정보창(포탑·기술병·적)은 누른 물체의 오른쪽 위 끝에 붙어 뜨고, 움직이는 적·기술병을 따라간다.
    // 화면 밖으로 나가면 안쪽으로 밀어 넣는다.
    void PlaceInfoPanel(Camera camera)
    {
        if (infoPanel == null || camera == null || !infoPanel.gameObject.activeSelf)
            return;

        // 11-02 사용자 확정: 심볼에 마우스를 올릴 수 있게, 마우스가 창 위에 있는 동안은 창을 멈춘다.
        if (PointerOverInfoPanel())
            return;

        Vector2 tile;
        if (infoSubject == InfoSubject.Tower)
        {
            var t = Selected();
            if (t == null)
                return;
            tile = new Vector2(t.Position.x + .5f, t.Position.y + 1f);
        }
        else if (infoSubject == InfoSubject.Enemy)
        {
            if (!infoEnemy.Alive)
                return;
            tile = infoEnemy.Position + new Vector2(.4f, 1.1f);
        }
        else if (infoSubject == InfoSubject.Player)
        {
            tile = game.PlayerTile() + new Vector2(.4f, 1.1f);
        }
        else
        {
            return;
        }

        var screen = camera.WorldToScreenPoint(game.World(tile));
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, null, out Vector2 local))
            return;

        var area = canvas.rect.size;
        var size = infoPanel.sizeDelta;
        var p = local + area * .5f + new Vector2(6, 0);
        p.x = Mathf.Clamp(p.x, 8, Mathf.Max(8, area.x - size.x - 8));
        p.y = Mathf.Clamp(p.y, 8, Mathf.Max(8, area.y - size.y - 8));
        SetHudPosition(infoPanel, new Vector2(Mathf.Round(p.x), Mathf.Round(p.y)));
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
}
