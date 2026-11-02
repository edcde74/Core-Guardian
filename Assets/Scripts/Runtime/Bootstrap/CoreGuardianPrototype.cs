using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

// Scene adapter: model positions use tile units; approved art roots use ground contact centers.
public sealed partial class CoreGuardianPrototype : MonoBehaviour
{
    public Tilemap ground,
        enemyPath,
        coreMap,
        occupied;
    public PrototypeGridMovement player;
    public GameObject[] enemyArt,
        towerArt;
    public TextAsset enemiesCsv,
        towersCsv,
        stageCsv,
        upgradesCsv,
        wavesCsv;

    [SerializeField, NaughtyAttributes.BoxGroup("맵 배치")]
    Vector2Int boardSize = new Vector2Int(45, 25);

    [SerializeField, NaughtyAttributes.BoxGroup("맵 배치")]
    Vector2Int coreCell = new Vector2Int(29, 9);

    [SerializeField, NaughtyAttributes.BoxGroup("맵 배치")]
    Vector2Int[] routeCells =
    {
        new Vector2Int(0, 12),
        new Vector2Int(11, 12),
        new Vector2Int(11, 18),
        new Vector2Int(35, 18),
        new Vector2Int(35, 6),
        new Vector2Int(17, 6),
        new Vector2Int(17, 12),
        new Vector2Int(29, 12),
        new Vector2Int(29, 9),
    };
    public Vector2Int BoardSize => boardSize;
    public Vector2Int CoreCell => coreCell;
    public IReadOnlyList<Vector2Int> RouteCells => Array.AsReadOnly(routeCells);
    public Session Session { get; private set; }
    string feedbackText;
    int goldNoticeAmount;

    // A gold notice keeps its amount and becomes text only when the HUD reads it (F11).
    public string Feedback
    {
        get => feedbackText ??= goldNoticeAmount > 0 ? "+" + goldNoticeAmount + " 골드 획득" : null;
        private set
        {
            feedbackText = value;
            goldNoticeAmount = 0;
        }
    }
    public int SelectedTower { get; private set; } = -1;
    public TowerKind? Installing { get; private set; }
    public event Action Changed;
    readonly Dictionary<int, GameObject> enemyViews = new Dictionary<int, GameObject>();
    readonly Dictionary<int, GameObject> towerViews = new Dictionary<int, GameObject>();
    readonly Dictionary<int, GameObject> projectileViews = new Dictionary<int, GameObject>();
    readonly Dictionary<int, GameObject> auraViews = new Dictionary<int, GameObject>();
    readonly Dictionary<int, GameObject> towerAuraViews = new Dictionary<int, GameObject>();
    readonly Dictionary<int, Transform> healthViews = new Dictionary<int, Transform>();
    readonly List<EnemyHandle> livingViewEnemies = new List<EnemyHandle>();
    readonly HashSet<int> activeEnemyViewIds = new HashSet<int>();
    readonly HashSet<int> activeTowerViewIds = new HashSet<int>();
    readonly HashSet<int> activeProjectileViewIds = new HashSet<int>();
    readonly List<int> removedViewIds = new List<int>();
    const int MaxRetainedProjectileViews = 256;
    readonly Stack<SpriteRenderer> projectileViewPool = new Stack<SpriteRenderer>();

    readonly Dictionary<int, EnemyBodyView> enemyBodies = new Dictionary<int, EnemyBodyView>();
    readonly Dictionary<EnemyKind, Stack<EnemyBodyView>> enemyBodyPools =
        new Dictionary<EnemyKind, Stack<EnemyBodyView>>();
    Transform enemyPoolRoot;
    CoreGuardianPlacementOverlay placementOverlay;
    Material placementOverlaySource;
    GameObject ghost;
    SpriteRenderer[] ghostRenderers = System.Array.Empty<SpriteRenderer>();
    readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
    Sprite square;
    Tile occupiedTile;
    CoreGuardianHud hud;
    CoreGuardianCombatArt combatArt;
    CoreGuardianEffectsView effects;
    CoreGuardianSessionEvents sessionEvents;

    /// <summary>라운드 기록 (2026-10-30): per-round core reaches written to PerfLogs/rounds_*.csv.</summary>
    public CoreGuardianRoundLog RoundLog { get; private set; }
    public CoreGuardianPlayLog PlayLog { get; private set; }
    CoreGuardianCombatAnimation combatAnimation;
    CoreGuardianUpgradeFeedback upgradeFeedback;
    GameObject bossArt;
    int previousGold;
    int previousCore,
        previousCoreDamageTaken;
    float notificationUntil;
    bool pointerGesture,
        pointerBlocked,
        pointerPlayer;
    Vector2Int pointerCell;
    Vector2 pointerScreen;
    public bool HasFeedback => Time.unscaledTime < notificationUntil;
    Vector3[] route;
    public bool Ready => Session != null;

    // HUD owns inspection visibility; changing its subject must not leave a competing tower ID.
    public void ClearInspectionSelection()
    {
        SelectedTower = -1;
    }

    // HUD 개편 B안: 포탑 연구소(1)·기술병 작업대(2) 건물. 2×2 칸, 위치는 stage.csv 427xx.
    readonly HashSet<Vector2Int> labCells = new HashSet<Vector2Int>(),
        benchCells = new HashSet<Vector2Int>();
    SpriteRenderer labView,
        benchView;
    Sprite labSprite,
        labHighlight,
        labHover,
        benchSprite,
        benchHighlight,
        benchHover;

    void Start()
    {
        Application.runInBackground = true;
        // 10-30 결정 #1: 색보정(화면 후처리)을 다시 켠다. 씬은 건드리지 않고 실행 때 카메라에서 켬. FPS 차이 없음 확인.
        var cameraData = Camera
            .main.OrNull()
            ?.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (cameraData != null)
        {
            cameraData.renderPostProcessing = true;
        }

        try
        {
            if (ground == null || player == null || occupied == null)
            {
                throw new InvalidOperationException("Missing gameplay scene references");
            }

            bossArt = Resources.Load<GameObject>("CoreGuardian/Animation/Boss/BossAnimated");
            if (bossArt == null)
            {
                throw new InvalidOperationException("Missing approved boss art prefab");
            }

            Session = new Session(
                CombatData.Load(
                    enemiesCsv.text,
                    towersCsv.text,
                    stageCsv.text,
                    upgradesCsv.text,
                    wavesCsv.text,
                    Sheet("player"),
                    Sheet("augments"),
                    Sheet("effectTags"),
                    Sheet("skills")
                )
            );
            route = routeCells.Select(c => World(c)).ToArray();
            Session.SetBoard(boardSize.x, boardSize.y, coreCell, routeCells);
            var flying = FlyingRoute();
            if (flying != null)
            {
                Session.SetFlyingRoute(flying);
            }

            SetupBuildings();
            player.Stepped += cell => Session.NotifyPlayerStep(new Vector2Int(cell.x, cell.y));
            var combatAudio = GetComponent<CoreGuardianCombatAudio>();
            if (combatAudio == null)
            {
                combatAudio = gameObject.AddComponent<CoreGuardianCombatAudio>();
            }

            combatAudio.Configure(Session);
            occupiedTile = ScriptableObject.CreateInstance<Tile>(); // Hidden occupancy map, independent of visible art.
            square = MakeSprite(false);
            hud = gameObject.AddComponent<CoreGuardianHud>();
            hud.Configure(this);
            combatArt = gameObject.AddComponent<CoreGuardianCombatArt>();
            combatArt.Configure(this, hud);
            combatAnimation = gameObject.AddComponent<CoreGuardianCombatAnimation>();
            combatAnimation.Configure(this);
            // Bridge joins the Session events here, where the effects view used to, so handler order is unchanged.
            sessionEvents = new CoreGuardianSessionEvents(Session);
            RoundLog = new CoreGuardianRoundLog(Session);
            PlayLog = new CoreGuardianPlayLog(Session);
            effects = gameObject.AddComponent<CoreGuardianEffectsView>();
            effects.Configure(this);
            upgradeFeedback = GetComponent<CoreGuardianUpgradeFeedback>();
            if (upgradeFeedback == null)
            {
                upgradeFeedback = gameObject.AddComponent<CoreGuardianUpgradeFeedback>();
            }

            previousGold = Session.Gold;
            previousCore = Session.CoreEnergy;
            previousCoreDamageTaken = Session.CoreDamageTaken;
            RebuildOccupied();
            Feedback = "공격 포탑을 선택하고 바닥 칸을 클릭하세요. WASD: 한 칸 이동";
        }
        catch (Exception e)
        {
            // A half-built game must not look ready; the HUD may not exist yet, so OnGUI shows the reason (B15).
            Debug.LogException(e, this);
            Session = null;
            Feedback = e.Message;
            startFailure = "게임을 시작하지 못했습니다: " + e.Message;
        }
    }

    string startFailure;

    void OnGUI()
    {
        if (startFailure == null)
            return;

        GUI.Label(new Rect(16, 16, Screen.width - 32, 80), startFailure);
    }

    public Vector3 World(Vector2 tile)
    {
        var origin = ground.GetCellCenterWorld(Vector3Int.zero);
        return origin
            + ground.transform.TransformVector(
                new Vector3(tile.x * ground.layoutGrid.cellSize.x, tile.y * ground.layoutGrid.cellSize.y, 0)
            );
    }

    internal Vector2 PlayerTile()
    {
        // Grid root position includes the approved sprite offset. Reference foot center via tile grid.
        var p = ground.transform.InverseTransformVector(
            player.transform.position - ground.GetCellCenterWorld(Vector3Int.zero)
        );
        return new Vector2(p.x / ground.layoutGrid.cellSize.x, p.y / ground.layoutGrid.cellSize.y);
    }

    void Update()
    {
        if (!Ready)
            return;

        var phase = Session.Phase;
        SyncTestSpeed();
        Session.Tick(Time.deltaTime, PlayerTile());
        SyncTestSpeed();
        // 전투 중 설치 허용(10-28) 뒤에도 단계가 바뀌면 설치 중인 것은 취소 (기존 동작 유지).
        if (Installing.HasValue && (!Session.CanBuildNow || phase != Session.Phase))
        {
            CancelInstall();
        }

        player.enabled = Session.Phase != SessionPhase.Augmentation;
        if (Session.Gold > previousGold)
        {
            // F11: the text is made only when shown; the amount is kept.
            feedbackText = null;
            goldNoticeAmount = Session.Gold - previousGold;
            notificationUntil = Time.unscaledTime + 2;
        }
        previousGold = Session.Gold;
        // Only real hits count; a lower core maximum from augments 48·50 is not damage (B1).
        int coreDamage = Session.CoreDamageTaken - previousCoreDamageTaken;
        if (coreDamage > 0)
        {
            combatArt.CoreDamaged(coreDamage);
            GetComponent<CoreGuardianCombatAudio>().OrNull()?.NotifyCoreDamage(coreDamage);
            // Only the notice line changes here; the rest of the HUD keeps its 0.2 s refresh (F5).
            Feedback = "코어 에너지 -" + coreDamage;
            notificationUntil = Time.unscaledTime + 3;
            hud.RefreshFeedback();
        }
        previousCore = Session.CoreEnergy;
        previousCoreDamageTaken = Session.CoreDamageTaken;
        if (phase != Session.Phase)
        {
            pointerBlocked = true;
            CancelInstall();
            Notify(Session.LastMessage);
        }
        SynchronizeViews();
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && hud.CancelInstallation())
        {
            pointerBlocked = true;
            return;
        }
        // Space belongs to the skill: drop any UI selection so it never submits a focused button.
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
            UseAreaBlast();
        }
        // Shift: 46 순간 이동(받았을 때만, 전투 중).
        if (
            Keyboard.current != null
            && (Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame)
        )
        {
            UseTeleport();
        }
        // 1: 포탑 연구소 / 2: 기술병 작업대 (설치 중·증강 선택 중 제외).
        if (Keyboard.current != null && !Installing.HasValue && Session.Phase != SessionPhase.Augmentation)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
            {
                hud.ToggleLab();
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
            {
                hud.ToggleBench();
            }
        }
        if (Mouse.current == null || Camera.main == null)
            return;

        var mouse = Mouse.current.position.ReadValue();
        var world = Camera.main.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, -Camera.main.transform.position.z));
        var cell = ground.WorldToCell(world);
        var xy = new Vector2Int(cell.x, cell.y);
        bool overUi = OverUI(mouse);
        RefreshBuildingHighlights(xy, !overUi);
        RefreshHover(mouse, xy, world, overUi);
        if (Installing.HasValue && ghost != null)
        {
            // The tile-centered preview is the visible placement cursor. Preserve normal UI input.
            bool onBoard = ground.HasTile(cell) && !OverUI(mouse);
            Cursor.visible = !onBoard;
            ghost.SetActive(onBoard);
            ghost.transform.position = ground.GetCellCenterWorld(cell);
            bool valid = CanInstall(xy);
            // Same active renderer set as the previous per-frame lookup: captured once, applied only while shown.
            if (onBoard)
            {
                foreach (var sr in ghostRenderers)
                {
                    if (sr != null)
                    {
                        sr.color = new Color(valid ? .55f : 1f, valid ? 1f : .35f, valid ? .65f : .35f, .6f);
                    }
                }
            }
        }
        // A UI press owns the entire gesture, including a release after its panel disappears.
        // Commit world clicks on release so the UI Button release cannot also place/select.
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            pointerGesture = true;
            pointerCell = xy;
            pointerScreen = mouse;
            pointerBlocked = OverUI(mouse) || Session.Phase == SessionPhase.Augmentation || phase != Session.Phase;
            pointerPlayer = !Installing.HasValue && PlayerHit(mouse);
        }
        if (!Mouse.current.leftButton.wasReleasedThisFrame)
            return;

        bool playerClick = pointerPlayer && !Installing.HasValue && PlayerHit(mouse);
        // 10-31 (빈 바닥 클릭이 한 번 씹힘): 누른 뒤 칸 경계를 살짝 넘어도(12픽셀 이내) 누른 칸의 클릭으로 보고,
        // 누름을 놓친 뗌(누른 기록 없음)도 UI 밖이면 그 칸의 클릭으로 받는다.
        bool missedPress = !pointerGesture;
        bool steady = pointerCell == xy || (mouse - pointerScreen).sqrMagnitude <= 144f;
        bool worldClick =
            (
                pointerGesture && !pointerBlocked && (steady || playerClick)
                || missedPress && Session.Phase != SessionPhase.Augmentation
            ) && !OverUI(mouse);
        var clicked = missedPress ? xy : pointerCell;
        pointerGesture = false;
        pointerBlocked = false;
        if (worldClick && playerClick)
        {
            hud.OpenPlayerSpecs();
            return;
        }
        if (worldClick)
        {
            HandleWorldClick(clicked);
        }
    }

    public int TestSpeed { get; private set; } = 1;

    // 개발 도구 일시정지 (2026-10-27 사용자 요청). Overrides combat speed until resumed.
    public bool DebugPaused { get; private set; }

    // Scene arrays cover the original kinds; later kinds load their prefab from Resources by name.
    readonly Dictionary<string, GameObject> unitPrefabs = new Dictionary<string, GameObject>();

    void OnDestroy()
    {
        placementOverlay?.Dispose();
        placementOverlay = null;
        if (placementOverlaySource != null)
        {
            Destroy(placementOverlaySource);
            placementOverlaySource = null;
        }
        sessionEvents?.Dispose();
        sessionEvents = null;
        RoundLog?.Dispose();
        RoundLog = null;
        PlayLog?.Dispose();
        PlayLog = null;
        Session?.Dispose();
        if (TestSpeed != 1 || DebugPaused)
        {
            Time.timeScale = 1;
        }

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Cursor.visible = true;
        if (occupiedTile != null)
        {
            Destroy(occupiedTile);
        }

        foreach (var views in new[] { enemyViews, towerViews, projectileViews, auraViews, towerAuraViews })
        {
            foreach (var g in views.Values)
            {
                if (g != null)
                {
                    Destroy(g);
                }
            }
        }

        foreach (var pool in enemyBodyPools.Values)
        {
            while (pool.Count > 0)
            {
                var body = pool.Pop();
                if (body.Root != null)
                {
                    Destroy(body.Root);
                }
            }
        }

        enemyBodyPools.Clear();
        enemyBodies.Clear();
        while (projectileViewPool.Count > 0)
        {
            var renderer = projectileViewPool.Pop();
            if (renderer != null)
            {
                Destroy(renderer.gameObject);
            }
        }
        if (ghost != null)
        {
            Destroy(ghost);
        }

        if (square != null)
        {
            Destroy(square.texture);
            Destroy(square);
        }
    }
}
