using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// CoreGuardianHud: tower install panel and placement focus visibility.
public sealed partial class CoreGuardianHud
{
    sealed class PlacementVisibility
    {
        public float Alpha;
        public bool Interactable,
            Raycasts;
    }

    void SetInstallPanelOpen(bool open)
    {
        open = open && game.Session.CanBuildNow;
        if (open != installOpen)
        {
            game.GetComponent<CoreGuardianCombatAudio>().OrNull()?.PlayCue(open ? "ui_open" : "ui_close");
        }

        if (open)
        {
            bigPanel = BigPanel.None; // 선택 정보창은 유지
        }
        // Every new opening starts at categories; folding changes only panel visibility.
        if (open && !installOpen)
        {
            BuildInstallCategories();
        }

        installOpen = open;
        installPanel.gameObject.SetActive(open);
        Refresh();
    }

    void BuildInstallCategories()
    {
        ClearInstallButtons();
        InstallButton(0, "공격 포탑", () => BuildInstallList(false));
        InstallButton(1, "지원 포탑", () => BuildInstallList(true));
    }

    void ClearInstallButtons()
    {
        foreach (var a in installRefresh)
        {
            refresh.Remove(a);
        }
        installRefresh.Clear();
        foreach (Transform child in installPanel)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    void InstallButton(int index, string label, Action action, Func<bool> available = null)
    {
        int before = refresh.Count;
        Button(
            label,
            installPanel,
            new Vector2(16 + (index % 3) * 212, -16 - (index / 3) * 100),
            new Vector2(204, 96),
            () => label,
            action,
            () => game.Session.CanBuildNow && (available == null || available())
        );
        installRefresh.Add(refresh[before]);
    }

    void BuildInstallList(bool support)
    {
        ClearInstallButtons();
        if (support)
        {
            InstallTowerButton(0, TowerKind.Support);
        }
        else
        {
            InstallTowerButton(0, TowerKind.Single);
            InstallTowerButton(1, TowerKind.Multi);
            InstallTowerButton(2, TowerKind.Frost);
            InstallTowerButton(3, TowerKind.Flame);
            InstallTowerButton(4, TowerKind.Rail);
        }
        InstallButton(8, "분류로 돌아가기", BuildInstallCategories);
        Refresh();
    }

    void InstallTowerButton(int index, TowerKind kind)
    {
        int before = refresh.Count;
        var b = Button(
            "Install_" + kind,
            installPanel,
            new Vector2(16 + (index % 3) * 212, -16 - (index / 3) * 100),
            new Vector2(204, 96),
            () =>
            {
                int cost = game.Session.InstallCost(kind);
                string reason =
                    game.Session.Crystals < cost ? "\n수정 부족"
                    : kind == TowerKind.Support && !game.Session.Towers.Any(t => t.Kind != TowerKind.Support)
                        ? "\n공격 포탑 필요"
                    : "";
                return TowerName(kind) + "\n" + cost + "수정" + reason;
            },
            () =>
            {
                game.ChooseTower(kind);
                Refresh();
            },
            () => game.Session.CanChooseTower(kind, out _)
        );
        b.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 16;
        installRefresh.Add(refresh[before]);
    }

    public bool CancelInstallation()
    {
        if (!game.Installing.HasValue && !installOpen)
            return false;

        game.GetComponent<CoreGuardianCombatAudio>().OrNull()?.PlayCue("cancel_install");
        game.CancelInstall();
        installOpen = false;
        installPanel.gameObject.SetActive(false);
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        game.Notify("포탑 설치 취소");
        return true;
    }

    void RefreshPlacementVisibility()
    {
        bool focused = game.Installing.HasValue && game.Session.Phase == SessionPhase.Preparation;
        if (!focused)
        {
            foreach (var entry in placementHidden)
            {
                if (entry.Key == null)
                    continue;

                entry.Key.alpha = entry.Value.Alpha;
                entry.Key.interactable = entry.Value.Interactable;
                entry.Key.blocksRaycasts = entry.Value.Raycasts;
            }
            placementHidden.Clear();
            placementFocused = false;
            return;
        }
        foreach (Transform root in canvas)
        {
            if (
                root.name == "TopStatus"
                || root.name == "StagePlate"
                || root.name == "BossPlate"
                || root.name == "CancelPlacement"
            )
            {
                continue;
            }

            var group = root.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = root.gameObject.AddComponent<CanvasGroup>();
            }

            if (!placementHidden.ContainsKey(group))
            {
                placementHidden.Add(
                    group,
                    new PlacementVisibility
                    {
                        Alpha = group.alpha,
                        Interactable = group.interactable,
                        Raycasts = group.blocksRaycasts,
                    }
                );
            }
            // Preserve activeSelf and every open/folded state while hiding and unblocking the field.
            group.alpha = 0;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
        if (!placementFocused && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        placementFocused = true;
    }
}
