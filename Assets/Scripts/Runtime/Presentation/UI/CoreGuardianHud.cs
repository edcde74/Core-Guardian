using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// Runtime HUD with shared sprite frames and live Korean labels.
public sealed partial class CoreGuardianHud : MonoBehaviour
{
    static readonly ProfilerMarker prepareDamageNumbersSample = new ProfilerMarker("CG.HUD.PrepareDamageNumbers");
    static readonly ProfilerMarker createDamageNumberSample = new ProfilerMarker("CG.HUD.CreateDamageNumber");
    CoreGuardianPrototype game;
    Font font,
        titleFont;
    TMP_FontAsset damageFont;
    Material damageMaterial;
    RectTransform canvas,
        worldFeedback,
        installPanel,
        playerPanel,
        towerPanel,
        damagePanel,
        damageContent,
        topDetails;
    UnityEngine.UI.Text status,
        center,
        feedback,
        towerInfo;
    readonly List<Action> refresh = new List<Action>();
    readonly List<Action> refreshSnapshot = new List<Action>();
    readonly List<UnityEngine.UI.Button> modalButtonScratch = new List<UnityEngine.UI.Button>();
    int refreshDepth;
    float nextRefresh;

    static readonly IComparer<DamageTotal> damageTotalComparer = new DamageTotalComparer();

    // Cached delegate: List.Sort(IComparer) allocates one per call.
    static readonly Comparison<DamageTotal> damageTotalCompare = damageTotalComparer.Compare;
    readonly List<DamageTotal> damageTotals = new List<DamageTotal>(),
        previousDamageTotals = new List<DamageTotal>();
    string damageNegativeSign;
    bool damageTotalsPresented;

    // Retain the peak row count; inactive rows do not participate in the layout.
    readonly List<DamageRow> damageRows = new List<DamageRow>();
    RectTransform damageFold;
    static readonly Dictionary<string, Sprite> damageSprites = new Dictionary<string, Sprite>();

    // Domain Reload is off: forget looked-up (and missing) damage icons on every Play (B24).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetDamageSprites() => damageSprites.Clear();

    RectTransform augmentationPanel;
    UnityEngine.UI.Text augmentationTimer,
        bossStatus;
    readonly List<UnityEngine.UI.Button> augmentationButtons = new List<UnityEngine.UI.Button>();
    bool playerOpen,
        installOpen;

    readonly Dictionary<CanvasGroup, PlacementVisibility> placementHidden =
        new Dictionary<CanvasGroup, PlacementVisibility>();
    bool placementFocused;
    RectTransform cardsRoot;
    string choiceSignature;
    bool cardsFolded;
    int lastTower = -1;

    readonly List<DamageNumber> damageNumbers = new List<DamageNumber>();
    const int MaxRetainedNumbersPerKind = 128;
    readonly Stack<DamageNumber> hitNumberPool = new Stack<DamageNumber>();
    readonly Stack<DamageNumber> goldNumberPool = new Stack<DamageNumber>();
    readonly Stack<DamageNumber> coreNumberPool = new Stack<DamageNumber>();
    bool disposingNumbers;
    int nextDamageLane;

    public void Configure(CoreGuardianPrototype prototype)
    {
        game = prototype;
        font = Resources.Load<Font>("CoreGuardian/Fonts/Galmuri11");
        titleFont = Resources.Load<Font>("CoreGuardian/Fonts/Galmuri11-Bold");
        if (font == null || titleFont == null)
        {
            throw new InvalidOperationException("Galmuri HUD fonts missing");
        }

        damageFont = Resources.Load<TMP_FontAsset>("CoreGuardian/Fonts/PixelifySansBoldDamage");
        damageMaterial = Resources.Load<Material>("CoreGuardian/Fonts/PixelifySansBoldDamageOutline");
        if (damageFont == null || damageMaterial == null)
        {
            throw new InvalidOperationException("Pixelify Sans Bold TMP assets missing");
        }

        var root = new GameObject(
            "CombatHUD",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler),
            typeof(UnityEngine.UI.GraphicRaycaster)
        );
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<RectTransform>();
        var c = root.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 2000;
        var scale = root.GetComponent<UnityEngine.UI.CanvasScaler>();
        scale.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scale.referenceResolution = new Vector2(1920, 1080);
        scale.matchWidthOrHeight = .5f;
        if (EventSystem.current == null)
        {
            new GameObject(
                "CombatEventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule)
            ).transform.SetParent(transform);
        }
        // WASD belongs to player movement; pointer hover/click still use the UI module.
        EventSystem.current.sendNavigationEvents = false;
        worldFeedback = Box("WorldFeedback", canvas, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero, false);
        worldFeedback.anchorMin = Vector2.zero;
        worldFeedback.anchorMax = Vector2.one;
        worldFeedback.sizeDelta = Vector2.zero;
        // Floating combat feedback always renders before every HUD panel and modal.
        worldFeedback.SetAsFirstSibling();
        PrepareDamageNumbers();
        Build();
        game.Changed += Refresh;
        game.Session.DamageApplied += ShowDamageNumber;
        BuildAugmentationSelection();
    }

    UnityEngine.UI.Button blastButton;

    void Build()
    {
        topDetails = Box("TopStatus", canvas, new Vector2(.5f, 1), new Vector2(0, -12), new Vector2(920, 72));
        status = Text("Status", topDetails, new Vector2(24, 0), new Vector2(875, 64), 23);
        status.color = Color.clear;
        string[] symbols = { "Core", "Wave", "Gold", "Crystal", "Augment" };
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            CoreGuardianHudSkin.Icon(topDetails, symbols[i], new Vector2(16 + i * 181, -21), new Vector2(30, 30));
            var value = Text(
                "Status_" + symbols[i],
                topDetails,
                new Vector2(52 + i * 181, -16),
                new Vector2(126, 40),
                19
            );
            value.horizontalOverflow = HorizontalWrapMode.Overflow; // "라운드 25/30"이 줄바꿈되어 사라지지 않게
            // F4: text is rebuilt only when its two numbers change.
            int shownA = int.MinValue,
                shownB = int.MinValue;
            refresh.Add(() =>
            {
                var s = game.Session;
                int a = index == 0 ? s.CoreEnergy
                    : index == 1 ? s.Wave
                    : index == 2 ? s.Gold
                    : index == 3 ? s.Crystals
                    : s.Augments.Selected.Count,
                    b = index == 0 ? s.CoreMaximum
                    : index == 1 ? s.LastWave
                    : 0;
                if (a == shownA && b == shownB)
                    return;
                shownA = a;
                shownB = b;
                value.text =
                    index == 0 ? "코어 " + a + "/" + b
                    : index == 1 ? "라운드 " + a + "/" + b
                    : index == 2 ? "골드 " + a
                    : index == 3 ? "수정 " + a
                    : "증강 " + a;
            });
        }
        var controls = Box("BottomControls", canvas, new Vector2(.5f, 0), new Vector2(0, 12), new Vector2(556, 88));
        Button(
            "Installation",
            controls,
            new Vector2(16, -16),
            new Vector2(180, 56),
            () => "타워 설치",
            () => SetInstallPanelOpen(!installOpen),
            () => game.Session.CanBuildNow
        );
        Button(
            "Start",
            controls,
            new Vector2(220, -16),
            new Vector2(224, 56),
            () =>
                game.Session.Phase == SessionPhase.Preparation ? "라운드 시작"
                : game.Session.Phase == SessionPhase.Combat ? "전투 진행"
                : game.Session.Phase == SessionPhase.Augmentation ? "증강 선택 중"
                : game.Session.Phase == SessionPhase.Defeat ? "패배"
                : "라운드 완료",
            () => game.StartCombat(),
            () => game.Session.Phase == SessionPhase.Preparation
        );
        // 범위 폭발 (Space / 클릭): 아이콘 + 시계 방향 쿨타임 덮개 (HUD 개편 B안).
        BuildBlastButton(controls);
        BuildTeleportButton(controls);
        var stagePlate = Box("StagePlate", canvas, new Vector2(.5f, 1), new Vector2(0, -74), new Vector2(900, 78));
        stagePlateView = stagePlate;
        CoreGuardianHudSkin.Icon(stagePlate, "Wave", new Vector2(16, -16), new Vector2(28, 28));
        center = Text("Preparation", stagePlate, new Vector2(54, -8), new Vector2(826, 28), 21);
        feedback = Text("Feedback", stagePlate, new Vector2(20, -38), new Vector2(860, 30), 17);
        var debug = Box("PrototypeActions", canvas, new Vector2(0, 1), new Vector2(16, -14), new Vector2(302, 88));
        Button(
            "RoleDiagnostic",
            debug,
            new Vector2(16, -16),
            new Vector2(270, 56),
            () => "3종 역할 검증 / 재설정",
            () => game.Diagnostic()
        );
        Button(
            "Reload",
            canvas,
            new Vector2(1630, -16),
            new Vector2(276, 56),
            () => "처음부터 다시 시작",
            () => game.Restart()
        );
        Button(
            "TestSpeed",
            canvas,
            new Vector2(1630, -80),
            new Vector2(276, 48),
            () => "전투 배속 ×" + game.TestSpeed,
            () => game.CycleTestSpeed()
        );

        installPanel = Box(
            "TowerInstallPanel",
            canvas,
            new Vector2(.5f, 0),
            new Vector2(0, 112),
            new Vector2(660, 328)
        );
        BuildInstallCategories();
        installPanel.gameObject.SetActive(false);
        cancelPlacementView = Button(
            "CancelPlacement",
            canvas,
            new Vector2(18, -1000),
            new Vector2(270, 56),
            () => "설치 취소 · Esc",
            () => CancelInstallation(),
            () => game.Installing.HasValue || installOpen
        ).gameObject;
        BuildInspectors();
        BuildSelectionInfo();
        damagePanel = Box("DamagePanel", canvas, new Vector2(1, 1), new Vector2(-16, -140), new Vector2(332, 380));
        CoreGuardianHudSkin.Icon(damagePanel, "Damage", new Vector2(16, -16), new Vector2(28, 28));
        Text("Title", damagePanel, new Vector2(54, -14), new Vector2(250, 32), 20).text = "라운드 누적 피해";
        var scroll = Box("Scroll", damagePanel, new Vector2(0, 1), new Vector2(16, -56), new Vector2(288, 308), false);
        var viewport = Box("Viewport", scroll, new Vector2(0, 1), Vector2.zero, new Vector2(288, 308), false);
        var scrollSurface = viewport.gameObject.AddComponent<UnityEngine.UI.Image>();
        scrollSurface.color = Color.clear;
        scrollSurface.raycastTarget = true;
        viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
        damageContent = Box("Content", viewport, new Vector2(0, 1), Vector2.zero, new Vector2(288, 0), false);
        var layout = damageContent.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.spacing = 8;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        damageContent.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine
            .UI
            .ContentSizeFitter
            .FitMode
            .PreferredSize;
        var sr = scroll.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        sr.viewport = viewport;
        sr.content = damageContent;
        sr.horizontal = false;
        sr.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
        AddDamageScrollbar(sr);
        BuildDamageFold();
        // Top compact display retains all four critical values when collapsed.
        var topFold = SmallHandle("TopFold", canvas, () => stageFolded = !stageFolded, true);
        stageFoldView = topFold;
        stageFoldArt = topFold.Find("HandleArt");
        topFold.anchorMin = topFold.anchorMax = topFold.pivot = new Vector2(.5f, 1);
        topFold.anchoredPosition = new Vector2(0, -84);
        BuildDeveloperMenu();
        BuildAugmentStrip();
        Refresh();
        Pin("CancelPlacement", new Vector2(0, 0), new Vector2(18, 32));
    }

    // Upgrade rows follow upgrades.csv (group leaders only), so new towers show their own stats.
    readonly Dictionary<TowerKind, List<string>> upgradeRows = new Dictionary<TowerKind, List<string>>();

    // Fixed HUD row order (cells never move between towers); unknown stats keep sheet order after these.
    static readonly string[] UpgradeOrder =
    {
        "buffDamage",
        "damage",
        "rate",
        "range",
        "projectileSpeed",
        "slowPercent",
        "burnDuration",
        "pierceFalloff",
    };

    readonly List<Action> installRefresh = new List<Action>();

    void LateUpdate()
    {
        var camera = Camera.main;
        for (int i = damageNumbers.Count - 1; i >= 0; i--)
        {
            var number = damageNumbers[i];
            float age = Time.time - number.Started;
            if (age >= .85f)
            {
                ReturnNumber(number);
                damageNumbers.RemoveAt(i);
                continue;
            }
            if (camera == null)
                continue;

            var screen = camera.WorldToScreenPoint(number.World);
            SetHudActive(number.Root.gameObject, screen.z > 0);
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, null, out local);
            number.Root.anchoredPosition =
                local + number.Offset + new Vector2(number.Lane * 42, Mathf.Abs(number.Lane) * 12 + age * 42);
            var color = number.Text.color;
            float alpha = 1 - Mathf.Clamp01((age - .35f) / .5f);
            if (color.a == alpha)
                continue; // Before the fade starts nothing else changes (F29).
            color.a = alpha;
            number.Text.color = color;
            if (number.Icon != null)
            {
                var iconColor = number.Icon.color;
                iconColor.a = color.a;
                number.Icon.color = iconColor;
            }
        }
    }

    // First-wave hit numbers: a combat round needs ~24 at once; creating TMP objects mid-combat costs GC and spikes.
    // Top up the hidden pool a couple per frame while preparation time is actually running (scaled time), never in combat.
    const int PreparationHitNumbers = 32,
        PreparationNumbersPerFrame = 2;

    void Update()
    {
        PrepareNumbersDuringPreparation();
        UpdateRedesignFrame();
        if (game == null || !game.Ready || Time.unscaledTime < nextRefresh)
            return;
        nextRefresh = Time.unscaledTime + .2f;
        Refresh();
    }

    SessionPhase installPhase;

    void Refresh()
    {
        if (game == null || !game.Ready || status == null)
            return;

        var s = game.Session;
        // Phase changes still close the chooser (existing behaviour); combat itself no longer does (10-28).
        if (!s.CanBuildNow || s.Phase != installPhase)
        {
            installOpen = false;
            SetHudActive(installPanel.gameObject, false);
        }
        installPhase = s.Phase;
        // F4: the status line (clear text, read by tests) is rebuilt only when one of its numbers changes.
        var statusKey = (
            s.CoreEnergy,
            s.CoreMaximum,
            s.Diagnostic,
            s.Wave,
            s.LastWave,
            s.Gold,
            s.Crystals,
            s.Augments.Selected.Count
        );
        if (!statusBuilt || !statusKey.Equals(statusShown))
        {
            statusBuilt = true;
            statusShown = statusKey;
            status.text =
                "코어 "
                + s.CoreEnergy
                + " / "
                + s.CoreMaximum
                + "  |  "
                + (s.Diagnostic ? "역할 검증" : "라운드 " + s.Wave + " / " + s.LastWave)
                + "  |  골드 "
                + s.Gold
                + "  |  수정 "
                + s.Crystals
                + "  |  증강 "
                + s.Augments.Selected.Count;
        }
        bool selecting = s.Phase == SessionPhase.Augmentation;
        if (augmentationPanel != null)
        {
            augmentationPanel.gameObject.SetActive(selecting);
            if (selecting)
            {
                RefreshCards();
                int seconds = Mathf.CeilToInt(s.AugmentationRemaining);
                if (seconds != augmentationSecondsShown)
                {
                    augmentationSecondsShown = seconds;
                    augmentationTimer.text =
                        "선택 "
                        + seconds
                        + "초 · 클릭 공개 / 공개된 카드 다시 클릭 선택\n시간 초과 시 최고 등급 자동 선택 · 동률이면 왼쪽 우선";
                }
            }
            else
            {
                choiceSignature = null;
                augmentationSecondsShown = int.MinValue;
            }
        }
        if (bossStatus != null)
        {
            var boss = FirstLivingBoss(s);
            (EnemySpec, int, bool, bool) bossKey = boss.IsValid
                ? (boss.Spec, boss.Health, boss.Shielded, boss.Enraged)
                : (null, 0, false, false);
            if (!bossBuilt || !bossKey.Equals(bossShown))
            {
                bossBuilt = true;
                bossShown = bossKey;
                bossStatus.text = !boss.IsValid
                    ? ""
                    : BossName(boss.Spec)
                        + " · "
                        + boss.Health
                        + " / "
                        + boss.Spec.Health
                        + (boss.Shielded ? " · 방패 전개" : "")
                        + (boss.Enraged ? " · 격노: 이동속도 +" + boss.Spec.RageBoost.ToString("0") + "%" : "");
            }
        }
        int centerNumber =
            s.Phase == SessionPhase.Preparation ? Mathf.CeilToInt(s.PreparationRemaining)
            : s.Phase == SessionPhase.Combat ? LivingEnemyCount(s)
            : 0;
        var centerKey = (s.Phase, s.Wave, centerNumber, s.Diagnostic, s.LastWave, selecting);
        if (!centerBuilt || !centerKey.Equals(centerShown))
        {
            centerBuilt = true;
            centerShown = centerKey;
            center.text =
                selecting ? "라운드 완료 · 증강 선택"
                : s.Phase == SessionPhase.Preparation ? s.Wave + "라운드 준비 " + centerNumber + "초"
                : s.Phase == SessionPhase.Combat ? s.Wave + "라운드 전투 · 살아 있는 적 " + centerNumber
                : s.Phase == SessionPhase.Defeat ? "코어 파괴 · 패배"
                : s.Diagnostic ? "역할 검증 완료"
                : "1~" + s.LastWave + "라운드 완료";
        }
        RefreshFeedback();
        // Same snapshot semantics as ToArray (actions may rebuild the list), without a new array per refresh.
        bool outerRefresh = refreshDepth == 0;
        refreshDepth++;
        try
        {
            if (outerRefresh)
            {
                refreshSnapshot.Clear();
                refreshSnapshot.AddRange(refresh);
                for (int i = 0; i < refreshSnapshot.Count; i++)
                {
                    refreshSnapshot[i]();
                }
            }
            else
            {
                foreach (var action in refresh.ToArray())
                {
                    action();
                }
            }
        }
        finally
        {
            refreshDepth--;
            if (outerRefresh)
            {
                refreshSnapshot.Clear();
            }
        }
        if (selecting)
        {
            // Same buttons and order as the array overload; the list is reused across refreshes.
            canvas.GetComponentsInChildren(true, modalButtonScratch);
            foreach (var button in modalButtonScratch)
            {
                if (!augmentationButtons.Contains(button))
                {
                    button.interactable = false;
                }
            }

            modalButtonScratch.Clear();
            if (EventSystem.current != null)
            {
                bool selectedChoice = false;
                foreach (var b in augmentationButtons)
                {
                    if (b.gameObject == EventSystem.current.currentSelectedGameObject)
                    {
                        selectedChoice = true;
                        break;
                    }
                }

                if (!selectedChoice)
                {
                    EventSystem.current.SetSelectedGameObject(augmentationButtons[0].gameObject);
                }
            }
        }
        RefreshInspectors();
        RefreshStageFold();
        RefreshPlacementVisibility();
        RefreshDamageTotals();
    }

    // Notification line only; a core hit no longer re-runs the whole HUD refresh (F5).
    internal void RefreshFeedback()
    {
        if (game == null || !game.Ready || feedback == null)
            return;

        SetHudText(feedback, game.Feedback);
        bool showFeedback = game.Session.Phase == SessionPhase.Preparation || game.HasFeedback;
        SetHudActive(feedback.gameObject, showFeedback);
        SetHudSize(stagePlateView, new Vector2(900, showFeedback ? 78 : 42));
    }

    // F4: last values each rebuilt line was made from.
    bool statusBuilt,
        bossBuilt,
        centerBuilt;
    (int, int, bool, int, int, int, int, int) statusShown;
    (EnemySpec, int, bool, bool) bossShown;
    (SessionPhase, int, int, bool, int, bool) centerShown;
    int augmentationSecondsShown = int.MinValue;

    static int LivingEnemyCount(Session s)
    {
        int count = 0;
        foreach (var e in s.Enemies)
        {
            if (e.Alive)
            {
                count++;
            }
        }
        return count;
    }

    static EnemyHandle FirstLivingBoss(Session s)
    {
        foreach (var e in s.Enemies)
        {
            if (e.Alive && e.Kind == EnemyKind.Boss)
                return e;
        }
        return default;
    }

    void OnDestroy()
    {
        if (game != null)
        {
            game.Changed -= Refresh;
            if (game.Session != null)
            {
                game.Session.DamageApplied -= ShowDamageNumber;
            }
        }
        disposingNumbers = true;
        ClearDamageNumbers();
        DestroyNumberPool(hitNumberPool);
        DestroyNumberPool(goldNumberPool);
        DestroyNumberPool(coreNumberPool);
    }
}
