using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

// CoreGuardianPrototype: player commands from HUD and keys (upgrade, destroy, blast, speed, restart).
public sealed partial class CoreGuardianPrototype
{
    public void DestroySelected()
    {
        var t = Session.Towers.FirstOrDefault(x => x.Id == SelectedTower);
        if (t == null)
            return;

        var pos = t.Position;
        if (!Session.DestroyTower(t.Id))
            return;

        occupied.SetTile(new Vector3Int(pos.x, pos.y, 0), null);
        GetComponent<CoreGuardianCombatAudio>().OrNull()?.PlayCue("demolish");
        SelectedTower = -1;
        SynchronizeViews();
        Notify(Session.LastMessage);
    }

    public void UpgradePlayer(string stat)
    {
        string reason;
        if (Session.UpgradePlayer(stat, out reason))
        {
            upgradeFeedback.Trigger(player.transform);
            GetComponent<CoreGuardianCombatAudio>().OrNull()?.NotifyUpgradeSucceeded();
        }
        else
        {
            GetComponent<CoreGuardianCombatAudio>().OrNull()?.NotifyUpgradeFailed();
        }

        Notify(reason);
    }

    public void UpgradeTower(string stat)
    {
        int id = SelectedTower;
        string reason;
        if (Session.UpgradeTower(id, stat, out reason))
        {
            var kind = Session.Towers.First(t => t.Id == id).Kind;
            foreach (var tower in Session.Towers.Where(t => t.Kind == kind))
            {
                upgradeFeedback.Trigger(TowerView(tower.Id));
            }

            GetComponent<CoreGuardianCombatAudio>().OrNull()?.NotifyUpgradeSucceeded();
        }
        else
        {
            GetComponent<CoreGuardianCombatAudio>().OrNull()?.NotifyUpgradeFailed();
        }

        Notify(reason);
    }

    // 포탑 연구소: 종류별 공유 강화. 그 종류 포탑이 1기 이상 있어야 구매 (10-28).
    public void UpgradeTowerKind(TowerKind kind, string stat)
    {
        string reason;
        if (Session.UpgradeTowerKind(kind, stat, out reason))
        {
            foreach (var tower in Session.Towers.Where(t => t.Kind == kind))
            {
                upgradeFeedback.Trigger(TowerView(tower.Id));
            }

            GetComponent<CoreGuardianCombatAudio>().OrNull()?.NotifyUpgradeSucceeded();
        }
        else
        {
            GetComponent<CoreGuardianCombatAudio>().OrNull()?.NotifyUpgradeFailed();
        }

        Notify(reason);
    }

    // 범위 폭발 (Space, 전투 중).
    public bool UseAreaBlast()
    {
        if (Session.Phase != SessionPhase.Combat || !Session.UseAreaBlast(PlayerTile()))
            return false;

        return true;
    }

    // 46 순간 이동 (Shift, 전투 중, 10-30): 마지막으로 움직인 방향으로 이동 후 도착 폭발. 대기 시간은 범위 폭발과 따로.
    public bool UseTeleport()
    {
        if (!Session.TeleportReady)
            return false;

        var dir = player.LastDirection;
        if (dir == Vector2Int.zero)
        {
            var p = Vector2Int.RoundToInt(PlayerTile());
            var d = coreCell - p;
            dir =
                Mathf.Abs(d.x) >= Mathf.Abs(d.y)
                    ? new Vector2Int(Math.Sign(d.x), 0)
                    : new Vector2Int(0, Math.Sign(d.y));
            if (dir == Vector2Int.zero)
            {
                dir = Vector2Int.right;
            }
        }
        var from = player.transform.position;
        // 10-30 결정 (기획서): 세 칸이 모두 막히면 제자리에서 도착 폭발, 대기 시간은 그대로 시작.
        bool moved = player.TryTeleport(
            dir,
            (int)(Session.Augments.Value(46) / Math.Max(1, Session.Augments.Count(46)))
        );
        Session.StartTeleportCooldown();
        if (moved)
        {
            effects.Teleport(from, player.transform.position);
        }

        // 46 도착 폭발 (2026-10-28): 도착 칸에서 범위 폭발 한 번 더 (피해 50%).
        Session.UseTeleportBlast(PlayerTile());
        // S12 순간 이동음: 폭발음 0.1초 뒤, 폭발음을 끊지 않고.
        GetComponent<CoreGuardianCombatAudio>().OrNull()?.PlayCueAfter("teleport", .1f);
        return true;
    }

    public void StartCombat()
    {
        CancelInstall();
        Session.StartCombat();
        SyncTestSpeed();
        Notify(Session.LastMessage);
    }

    public void Diagnostic()
    {
        hud.ClearDamageNumbers();
        upgradeFeedback.Clear();
        combatArt.ClearEffects();
        effects?.Clear();
        CancelInstall();
        Session.StartDiagnosticEncounter();
        SelectedTower = -1;
        previousGold = Session.Gold;
        previousCore = Session.CoreEnergy;
        previousCoreDamageTaken = Session.CoreDamageTaken;
        SyncTestSpeed();
        RebuildOccupied();
        SynchronizeViews();
        Notify("역할 검증 모드 — 정식 라운드 편성·재화와 별도");
    }

    void SyncTestSpeed(bool force = false)
    {
        // An external QA pause/clock at default speed is not owned by this button.
        if (DebugPaused)
        {
            Time.timeScale = 0;
            return;
        }
        if (force || TestSpeed != 1)
        {
            Time.timeScale = Ready && Session.Phase == SessionPhase.Combat ? TestSpeed : 1;
        }
    }

    public void ToggleDebugPause()
    {
        DebugPaused = !DebugPaused;
        if (DebugPaused)
        {
            Time.timeScale = 0;
        }
        else
        {
            SyncTestSpeed(true);
        }

        Notify(DebugPaused ? "개발 도구 · 일시정지" : "개발 도구 · 재개");
    }

    public void CycleTestSpeed()
    {
        TestSpeed = TestSpeed == 3 ? 1 : TestSpeed + 1;
        SyncTestSpeed(true);
        Notify("전투 배속 ×" + TestSpeed + " · 준비/증강/결과는 ×1");
    }

    public void Restart()
    {
        if (TestSpeed != 1 || DebugPaused)
        {
            Time.timeScale = 1;
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.name);
    }

    public void Notify(string text)
    {
        Feedback = string.IsNullOrEmpty(text) ? Session.LastMessage : text;
        notificationUntil = Time.unscaledTime + 3;
        Changed?.Invoke();
    }
}
