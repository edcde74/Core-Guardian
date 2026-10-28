using System;
using System.Linq;
using CoreGuardian;
using UnityEngine;

// Display policy only: prices, progression, combat and placement remain owned by Session/Prototype.
// HUD 개편 B안 (2026-10-28): 하단 큰 패널은 설치 목록 · 포탑 연구소(1) · 기술병 작업대(2) 중 하나만,
// 플레이어·포탑·적 선택 정보는 왼쪽 아래 고정 정보창(CoreGuardianHud.Redesign.cs).
public sealed partial class CoreGuardianHud
{
    enum BigPanel
    {
        None,
        Bench,
        Lab,
    }

    BigPanel bigPanel;
    bool purchaseOpen = true,
        developerOpen,
        stageFolded;
    TowerKind labKind = TowerKind.Single;
    SessionPhase? displayedPhase;
    RectTransform inspectorFold;
    UnityEngine.UI.Text playerInfo,
        playerTitle,
        towerTitle;
    RectTransform stagePlateView,
        stageFoldView;
    Transform stageFoldArt;
    UnityEngine.UI.Image towerIdentityIcon;
    GameObject cancelPlacementView;
    readonly System.Collections.Generic.List<GameObject> developerViews =
        new System.Collections.Generic.List<GameObject>();
    static readonly TowerKind[] LabKinds =
    {
        TowerKind.Single,
        TowerKind.Multi,
        TowerKind.Frost,
        TowerKind.Flame,
        TowerKind.Rail,
        TowerKind.Support,
    };
    static readonly string[] LabStats =
    {
        "damage",
        "rate",
        "range",
        "projectileSpeed",
        "buffDamage",
        "slowPercent",
        "burnDuration",
        "pierceFalloff",
    };
    bool CanBuyPlayer => game.Session.Phase == SessionPhase.Preparation || game.Session.Phase == SessionPhase.Combat;
    public bool LabOpen => bigPanel == BigPanel.Lab && !installOpen;
    public bool BenchOpen => bigPanel == BigPanel.Bench && !installOpen;
    public TowerKind LabKind => labKind;

    // Outline follows the left-bottom selection (player, tower or enemy).
    public Transform InspectionTarget
    {
        get
        {
            if (
                game == null
                || !game.Ready
                || installOpen
                || game.Installing.HasValue
                || game.Session.Phase == SessionPhase.Augmentation
            )
            {
                return null;
            }

            switch (infoSubject)
            {
                case InfoSubject.Player:
                    return game.player.transform;
                case InfoSubject.Tower:
                    return game.TowerView(game.SelectedTower);
                case InfoSubject.Enemy:
                    return infoEnemy.Alive ? game.EnemyView(infoEnemy.Id) : null;
                default:
                    return null;
            }
        }
    }

    RectTransform SmallHandle(string name, Transform parent, Action action, bool horizontal)
    {
        var r = Box(
            name,
            parent,
            new Vector2(.5f, 0),
            Vector2.zero,
            horizontal ? new Vector2(80, 28) : new Vector2(28, 80),
            false
        );
        var hit = r.gameObject.AddComponent<UnityEngine.UI.Image>();
        hit.color = Color.clear;
        var art = Box("HandleArt", r, new Vector2(.5f, .5f), Vector2.zero, new Vector2(22, 80), false);
        var image = art.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = DamageSprite("DamageHandle");
        image.preserveAspect = true;
        image.raycastTarget = false;
        if (horizontal)
        {
            art.localRotation = Quaternion.Euler(0, 0, 90);
        }

        var b = r.gameObject.AddComponent<UnityEngine.UI.Button>();
        b.targetGraphic = image;
        r.gameObject.AddComponent<CoreGuardianHudHandleReaction>().Configure(image, horizontal);
        // Augmentation disables ordinary buttons, including inactive handles. Restore on the
        // next refresh before modal gating is applied again, just like the normal Button helper.
        refresh.Add(() => b.interactable = true);
        b.onClick.AddListener(() =>
        {
            if (!b.interactable)
                return;
            game.GetComponent<CoreGuardianCombatAudio>().OrNull()?.PlayCue("ui_click");
            action();
            Refresh();
        });
        return r;
    }

    void SetTopCompact(bool compact)
    {
        topDetails.sizeDelta = new Vector2(920, compact ? 52 : 72);
        foreach (Transform child in topDetails)
        {
            var r = child as RectTransform;
            if (r == null || child.name == "Status")
                continue;

            bool icon = child.name.StartsWith("Symbol");
            r.anchoredPosition = new Vector2(r.anchoredPosition.x, compact ? -12 : (icon ? -21 : -16));
            if (!icon)
            {
                r.sizeDelta = new Vector2(r.sizeDelta.x, compact ? 28 : 40);
            }
        }
        if (stagePlateView != null)
        {
            SetHudPosition(stagePlateView, new Vector2(0, compact ? -74 : -94));
        }
    }

    void RefreshStageFold()
    {
        var phase = game.Session.Phase;
        var stage = stagePlateView;
        var handle = stageFoldView;
        bool hidden = phase == SessionPhase.Combat && stageFolded;
        SetHudActive(stage.gameObject, !hidden);
        SetHudActive(handle.gameObject, phase == SessionPhase.Combat);
        SetHudPosition(
            handle,
            new Vector2(
                0,
                hidden
                    ? topDetails.anchoredPosition.y - topDetails.sizeDelta.y
                    : stage.anchoredPosition.y - stage.sizeDelta.y
            )
        );
        var scale = new Vector3(hidden ? -1 : 1, 1, 1);
        if (!stageFoldArt.localScale.Equals(scale))
        {
            stageFoldArt.localScale = scale;
        }
    }

    void BuildDeveloperMenu()
    {
        Button(
            "DeveloperMenu",
            canvas,
            new Vector2(16, -14),
            new Vector2(150, 48),
            () => "개발 도구",
            () =>
            {
                developerOpen = !developerOpen;
                game.GetComponent<CoreGuardianCombatAudio>().OrNull()?.PlayCue(developerOpen ? "ui_open" : "ui_close");
            }
        );
        var debug = canvas.Find("PrototypeActions") as RectTransform;
        debug.anchoredPosition = new Vector2(16, -72);
        var reload = canvas.Find("Reload") as RectTransform;
        reload.anchorMin = reload.anchorMax = reload.pivot = new Vector2(0, 1);
        reload.anchoredPosition = new Vector2(32, -168);
        var speed = canvas.Find("TestSpeed") as RectTransform;
        speed.anchorMin = speed.anchorMax = speed.pivot = new Vector2(0, 1);
        speed.anchoredPosition = new Vector2(32, -232);
        BuildSoundControls();
        BuildCheatPanel();
        developerViews.Add(debug.gameObject);
        developerViews.Add(reload.gameObject);
        developerViews.Add(speed.gameObject);
        var sound = canvas.Find("SoundControls");
        if (sound != null)
        {
            developerViews.Add(sound.gameObject);
        }

        SetTopCompact(true);
    }

    void BuildInspectors()
    {
        // 기술병 작업대 (2): 현재 능력치 + 능력치 강화(골드). Object names kept from the old player panel.
        playerPanel = Box("PlayerGrowthPanel", canvas, new Vector2(.5f, 0), new Vector2(0, 112), new Vector2(900, 220));
        CoreGuardianHudSkin.Icon(playerPanel, "PlayerIdentity", new Vector2(16, -16), new Vector2(28, 28));
        playerTitle = Text("Title", playerPanel, new Vector2(54, -12), new Vector2(820, 32), 22);
        var specTab = Button(
            "PlayerSpecTab",
            playerPanel,
            new Vector2(16, -56),
            new Vector2(220, 56),
            () => "현재 능력치",
            () =>
            {
                purchaseOpen = false;
                Refresh();
            }
        );
        var buyTab = Button(
            "PlayerBuyTab",
            playerPanel,
            new Vector2(16, -120),
            new Vector2(220, 56),
            () => "능력치 강화",
            () =>
            {
                purchaseOpen = true;
                Refresh();
            },
            () => CanBuyPlayer
        );
        StyleInspectorTab(specTab);
        StyleInspectorTab(buyTab);
        playerAbilities = BuildAbilityPlate(playerPanel);
        playerInfo = Text("Stats", playerPanel, new Vector2(280, -186), new Vector2(580, 24), 16);
        playerInfo.alignment = TextAnchor.UpperLeft;
        var stats = new[] { "damage", "rate", "range", "projectileSpeed" };
        for (int i = 0; i < stats.Length; i++)
        {
            string stat = stats[i];
            var b = Button(
                stat,
                playerPanel,
                new Vector2(264 + i % 2 * 306, -52 - i / 2 * 64),
                new Vector2(306, 64),
                () => UpgradeLabel(game.Session.Player, "Player", stat, game.Session.PlayerUpgradePrice(stat), "골드"),
                () => PurchasePlayer(stat),
                () => CanBuyPlayer && game.Session.Gold >= game.Session.PlayerUpgradePrice(stat)
            );
            StyleAbilityPurchase(b);
            refresh.Add(() => b.gameObject.SetActive(purchaseOpen));
        }
        // 포탑 연구소 (1): 종류 탭 + 종류별 공유 강화(수정).
        towerPanel = Box("TowerLabPanel", canvas, new Vector2(.5f, 0), new Vector2(0, 112), new Vector2(900, 352));
        towerIdentityIcon = CoreGuardianHudSkin.Icon(
            towerPanel,
            "TowerIdentity",
            new Vector2(16, -16),
            new Vector2(28, 28)
        );
        towerTitle = Text("Title", towerPanel, new Vector2(54, -12), new Vector2(820, 32), 22);
        for (int i = 0; i < LabKinds.Length; i++)
        {
            var kind = LabKinds[i];
            var tab = Button(
                "LabKind_" + kind,
                towerPanel,
                new Vector2(16 + i * 146, -52),
                new Vector2(138, 48),
                () => TowerShortName(kind),
                () =>
                {
                    labKind = kind;
                    Refresh();
                }
            );
            tab.GetComponent<CoreGuardianHudReaction>().Selected = () => labKind == kind;
        }
        foreach (var stat in LabStats)
        {
            string captured = stat;
            var b = Button(
                "Tower_" + stat,
                towerPanel,
                Vector2.zero,
                new Vector2(430, 64),
                () =>
                    UpgradeLabel(
                        game.Session.TowerKindStats(labKind),
                        labKind.ToString(),
                        captured,
                        game.Session.TowerKindUpgradePrice(labKind, captured),
                        "수정",
                        game.Session.TowerKindStatMaxed(labKind, captured)
                    ),
                () => PurchaseLab(captured),
                () => LabCanUpgrade(captured)
            );
            StyleAbilityPurchase(b);
            b.GetComponent<CoreGuardianHudReaction>().SetLabelLayout(new Vector2(62, -8), new Vector2(352, 48));
            refresh.Add(() =>
            {
                bool relevant = UpgradeRows(labKind).Contains(captured);
                b.gameObject.SetActive(relevant);
                if (relevant)
                {
                    int row = UpgradeRows(labKind).IndexOf(captured);
                    ((RectTransform)b.transform).anchoredPosition = new Vector2(
                        16 + row % 2 * 438,
                        -112 - row / 2 * 64
                    );
                }
            });
        }
        towerInfo = Text("Stats", towerPanel, new Vector2(16, -308), new Vector2(860, 36), 15);
        towerInfo.alignment = TextAnchor.UpperLeft;
        inspectorFold = SmallHandle("InspectorFold", canvas, () => CloseBigPanels(), true);
        playerPanel.gameObject.SetActive(false);
        towerPanel.gameObject.SetActive(false);
    }

    static string TowerShortName(TowerKind kind)
    {
        switch (kind)
        {
            case TowerKind.Single:
                return "단일";
            case TowerKind.Multi:
                return "범위";
            case TowerKind.Frost:
                return "냉기";
            case TowerKind.Flame:
                return "화염";
            case TowerKind.Rail:
                return "레일";
            default:
                return "지원";
        }
    }

    bool LabCanUpgrade(string stat)
    {
        int price = game.Session.TowerKindUpgradePrice(labKind, stat);
        return UpgradeRows(labKind).Contains(stat)
            && game.Session.CanBuildNow
            && game.Session.HasTowerKind(labKind)
            && !game.Session.TowerKindStatMaxed(labKind, stat)
            && price >= 0
            && game.Session.Crystals >= price;
    }

    void PurchaseLab(string stat)
    {
        var kind = labKind;
        float before = StatValue(game.Session.TowerKindStats(kind), stat);
        game.UpgradeTowerKind(kind, stat);
        float after = StatValue(game.Session.TowerKindStats(kind), stat);
        if (after > before)
        {
            game.Notify(TowerName(kind) + " · " + StatName(stat) + " +" + (after - before).ToString("0.##"));
            var b = towerPanel.Find("Tower_" + stat);
            if (b != null)
            {
                b.GetComponent<CoreGuardianHudReaction>().Confirm();
            }
        }
    }

    void StyleInspectorTab(UnityEngine.UI.Button button)
    {
        var tab = (RectTransform)button.transform;
        button
            .GetComponent<CoreGuardianHudReaction>()
            .SetLabelLayout(new Vector2(54, -16), tab.sizeDelta - new Vector2(70, 32));
        var icon = tab.GetComponentsInChildren<UnityEngine.UI.Image>().First(i => i.transform != tab).rectTransform;
        icon.anchoredPosition = new Vector2(16, -(tab.sizeDelta.y - icon.sizeDelta.y) / 2);
    }

    public void ToggleLab() => ToggleBig(BigPanel.Lab);

    public void ToggleBench() => ToggleBig(BigPanel.Bench);

    void ToggleBig(BigPanel panel)
    {
        if (game.Installing.HasValue || game.Session.Phase == SessionPhase.Augmentation)
            return;

        bool close = bigPanel == panel && !installOpen;
        game.GetComponent<CoreGuardianCombatAudio>().OrNull()?.PlayCue(close ? "ui_close" : "ui_open");
        installOpen = false;
        installPanel.gameObject.SetActive(false);
        bigPanel = close ? BigPanel.None : panel;
        if (!close && panel == BigPanel.Bench)
        {
            purchaseOpen = true;
        }

        if (!close && panel == BigPanel.Lab)
        {
            var t = Selected();
            if (t != null && infoSubject == InfoSubject.Tower)
            {
                labKind = t.Kind;
            }
        }
        Refresh();
    }

    void CloseBigPanels()
    {
        installOpen = false;
        installPanel.gameObject.SetActive(false);
        bigPanel = BigPanel.None;
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
    }

    // Old entry points (world clicks, tests): now the left-bottom information panel.
    public void OpenPlayerSpecs() => ShowPlayerInfo();

    public void OpenSelectedTowerSpecs() => ShowTowerInfo();

    // One floor click closes the open panels AND releases the selection. Never places or spends currency.
    public bool TryFoldDetails()
    {
        if (game.Installing.HasValue || game.Session.Phase == SessionPhase.Augmentation)
            return false;

        if (!installOpen && bigPanel == BigPanel.None && infoSubject == InfoSubject.None && !developerOpen)
            return false;

        game.GetComponent<CoreGuardianCombatAudio>().OrNull()?.PlayCue("ui_close");
        CloseBigPanels();
        ReleaseInfo();
        developerOpen = false; // 빈 바닥 클릭은 열린 개발 도구도 닫음 (2026-10-28 사용자 요청)
        Refresh();
        return true;
    }

    void SetCardsVisible()
    {
        var group = cardsRoot.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = cardsRoot.gameObject.AddComponent<CanvasGroup>();
        }

        group.alpha = cardsFolded ? 0 : 1;
        group.interactable = !cardsFolded;
        group.blocksRaycasts = !cardsFolded;
        if (augmentationPanel != null && augmentationPanel.Find("AugmentFold") != null)
        {
            augmentationPanel.Find("AugmentFold/HandleArt").localScale = new Vector3(cardsFolded ? -1 : 1, 1, 1);
        }
    }

    void RefreshInspectors()
    {
        var s = game.Session;
        if (displayedPhase != s.Phase)
        {
            if (displayedPhase.HasValue)
            {
                bigPanel = BigPanel.None;
                if (s.Phase == SessionPhase.Augmentation)
                {
                    ReleaseInfo();
                    developerOpen = false;
                }
            } // 개발 도구는 증강 화면에서만 닫음
            displayedPhase = s.Phase;
            if (s.Phase == SessionPhase.Combat)
            {
                SetTopCompact(true);
            }
        }
        playerOpen = BenchOpen;
        bool labOpen = LabOpen;
        SetHudActive(playerPanel.gameObject, playerOpen);
        SetHudActive(towerPanel.gameObject, labOpen);
        RefreshAbilityPlate(playerAbilities, s.Player);
        SetHudText(playerTitle, "기술병 작업대 · " + (purchaseOpen ? "능력치 강화" : "현재 능력치"));
        SetHudText(
            playerInfo,
            "범위 폭발(Space) "
                + (
                    s.AreaBlast == null ? "없음"
                    : s.Phase != SessionPhase.Combat ? "전투 중 사용"
                    : s.AreaBlastReady ? "준비"
                    : Mathf.CeilToInt(s.AreaBlastCooldown) + "초"
                )
                + " · 증강 "
                + s.Augments.Selected.Count
                + "장"
        );
        SetHudText(towerTitle, "포탑 연구소 · " + TowerName(labKind) + " 공유 강화");
        var identity = CoreGuardianHudSkin.Sprite(CoreGuardianHudSkin.Symbol("Install_" + labKind));
        if (towerIdentityIcon.sprite != identity)
        {
            towerIdentityIcon.sprite = identity;
        }

        int count = s.Towers.Count(t => t.Kind == labKind);
        SetHudText(
            towerInfo,
            count == 0
                ? "이 종류 포탑을 1기 이상 설치하면 강화할 수 있습니다."
                : "같은 종류의 모든 포탑과 이후 설치분에 적용됩니다(지금 "
                    + count
                    + "기). 철거해도 공유 강화 단계는 유지됩니다."
        );
        bool details = playerOpen || labOpen || installOpen;
        SetHudActive(inspectorFold.gameObject, details && s.Phase != SessionPhase.Augmentation);
        SetHudPosition(
            inspectorFold,
            new Vector2(
                0,
                112
                    + (
                        installOpen ? installPanel.sizeDelta.y
                        : playerOpen ? playerPanel.sizeDelta.y
                        : towerPanel.sizeDelta.y
                    )
            )
        );
        foreach (var node in developerViews)
        {
            SetHudActive(node, developerOpen);
        }

        if (cancelPlacementView != null)
        {
            SetHudActive(cancelPlacementView, game.Installing.HasValue);
        }

        RefreshSelectionInfo();
        RefreshAugmentStrip();
    }
}
