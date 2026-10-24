using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;

// Scene adapter: model positions use tile units; approved art roots use ground contact centers.
public sealed class CoreGuardianPrototype : MonoBehaviour
{
    public Tilemap ground, enemyPath, coreMap, occupied;
    public PrototypeGridMovement player;
    public GameObject[] enemyArt, towerArt;
    public TextAsset enemiesCsv, towersCsv, stageCsv, upgradesCsv, wavesCsv;
    [SerializeField] Vector2Int boardSize = new Vector2Int(45,25);
    [SerializeField] Vector2Int coreCell = new Vector2Int(29,9);
    [SerializeField] Vector2Int[] routeCells = {new Vector2Int(0,12),new Vector2Int(11,12),new Vector2Int(11,18),new Vector2Int(35,18),new Vector2Int(35,6),new Vector2Int(17,6),new Vector2Int(17,12),new Vector2Int(29,12),new Vector2Int(29,9)};
    public Vector2Int BoardSize => boardSize;
    public Vector2Int CoreCell => coreCell;
    public IReadOnlyList<Vector2Int> RouteCells => Array.AsReadOnly(routeCells);
    public Session Session { get; private set; }
    public string Feedback { get; private set; }
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
    const int MaxRetainedProjectileViews=256;
    readonly Stack<SpriteRenderer> projectileViewPool = new Stack<SpriteRenderer>();
    const int MaxRetainedEnemyBodies=32,MaxRetainedBossBodies=4;
    sealed class EnemyBodyView
    {
        public EnemyKind Kind;
        public GameObject Source,Root;
        public CoreGuardianUnitAnimation Animation;
        public CoreGuardianPhotoShadow Shadow;
        public SegmentedEnemyHealthBar Health;
        public Vector3 InitialScale;
        public Quaternion InitialRotation;
        public bool Valid=>Root!=null&&Animation!=null&&Health!=null&&Shadow!=null;
    }
    readonly Dictionary<int,EnemyBodyView> enemyBodies=new Dictionary<int,EnemyBodyView>();
    readonly Dictionary<EnemyKind,Stack<EnemyBodyView>> enemyBodyPools=new Dictionary<EnemyKind,Stack<EnemyBodyView>>();
    Transform enemyPoolRoot;
    CoreGuardianPlacementOverlay placementOverlay;
    GameObject ghost;
    SpriteRenderer[] ghostRenderers=System.Array.Empty<SpriteRenderer>();
    readonly List<RaycastResult> uiRaycastResults=new List<RaycastResult>();
    Sprite square, dot;
    Tile occupiedTile;
    CoreGuardianHud hud;
    CoreGuardianCombatArt combatArt;
    CoreGuardianCombatAnimation combatAnimation;
    CoreGuardianUpgradeFeedback upgradeFeedback;
    GameObject bossArt;
    int previousGold;
    int previousCore;
    float notificationUntil;
    bool pointerGesture, pointerBlocked, pointerPlayer;
    Vector2Int pointerCell;
    public bool HasFeedback=>Time.unscaledTime<notificationUntil;
    Vector3[] route;
    public bool Ready => Session != null;
    // HUD owns inspection visibility; changing its subject must not leave a competing tower ID.
    public void ClearInspectionSelection(){SelectedTower=-1;}
    public Transform TowerView(int id)=>towerViews.TryGetValue(id,out var view)&&view!=null?view.transform:null;
    public void GetCombatBodyTargets(List<Transform> targets)
    {
        if(player!=null&&player.gameObject.activeInHierarchy)targets.Add(player.transform);
        foreach(var view in towerViews.Values)if(view!=null&&view.activeInHierarchy)targets.Add(view.transform);
        foreach(var view in enemyViews.Values)if(view!=null&&view.activeInHierarchy)targets.Add(view.transform);
    }

    void Start()
    {
        Application.runInBackground = true;
        try
        {
            if(ground==null||player==null||occupied==null)throw new InvalidOperationException("Missing gameplay scene references");
            bossArt=Resources.Load<GameObject>("CoreGuardian/Animation/Boss/BossAnimated");
            if(bossArt==null)throw new InvalidOperationException("Missing approved boss art prefab");
            Session=new Session(CombatData.Load(enemiesCsv.text,towersCsv.text,stageCsv.text,upgradesCsv.text,wavesCsv.text));
            route=routeCells.Select(c=>World(c)).ToArray();
            Session.SetBoard(boardSize.x,boardSize.y,coreCell,routeCells);
            var combatAudio=GetComponent<CoreGuardianCombatAudio>();
            if(combatAudio==null)combatAudio=gameObject.AddComponent<CoreGuardianCombatAudio>();
            combatAudio.Configure(Session);
            occupiedTile=ScriptableObject.CreateInstance<Tile>(); // Hidden occupancy map, independent of visible art.
            square=MakeSprite(false);dot=MakeSprite(true);
            hud=gameObject.AddComponent<CoreGuardianHud>();hud.Configure(this);
            combatArt=gameObject.AddComponent<CoreGuardianCombatArt>();combatArt.Configure(this,hud);
            combatAnimation=gameObject.AddComponent<CoreGuardianCombatAnimation>();combatAnimation.Configure(this);
            upgradeFeedback=GetComponent<CoreGuardianUpgradeFeedback>();
            if(upgradeFeedback==null)upgradeFeedback=gameObject.AddComponent<CoreGuardianUpgradeFeedback>();
            previousGold=Session.Gold;
            previousCore=Session.CoreEnergy;
            Feedback="공격 포탑을 선택하고 바닥 칸을 클릭하세요. WASD: 한 칸 이동";
        }
        catch(Exception e) { Debug.LogException(e,this);Feedback=e.Message;enabled=false; }
    }
    public Vector3 World(Vector2 tile)
    {
        var origin=ground.GetCellCenterWorld(Vector3Int.zero);
        return origin+ground.transform.TransformVector(new Vector3(tile.x*ground.layoutGrid.cellSize.x,tile.y*ground.layoutGrid.cellSize.y,0));
    }
    Vector2 PlayerTile()
    {
        // Grid root position includes the approved sprite offset. Reference foot center via tile grid.
        var p=ground.transform.InverseTransformVector(player.transform.position-ground.GetCellCenterWorld(Vector3Int.zero));
        return new Vector2(p.x/ground.layoutGrid.cellSize.x,p.y/ground.layoutGrid.cellSize.y);
    }
    void Update()
    {
        if(!Ready)return;
        var phase=Session.Phase;
        SyncTestSpeed();
        Session.Tick(Time.deltaTime,PlayerTile());
        SyncTestSpeed();
        if(Installing.HasValue&&Session.Phase!=SessionPhase.Preparation)CancelInstall();
        player.enabled=Session.Phase!=SessionPhase.Augmentation;
        if(Session.Gold>previousGold){ Feedback="+"+(Session.Gold-previousGold)+" 골드 획득";notificationUntil=Time.unscaledTime+2; }
        previousGold=Session.Gold;
        if(Session.CoreEnergy<previousCore){combatArt.CoreDamaged(previousCore-Session.CoreEnergy);GetComponent<CoreGuardianCombatAudio>()?.NotifyCoreDamage(previousCore-Session.CoreEnergy);Notify("코어 에너지 -"+(previousCore-Session.CoreEnergy));}
        previousCore=Session.CoreEnergy;
        if(phase!=Session.Phase){pointerBlocked=true;CancelInstall();Notify(Session.LastMessage);}
        SynchronizeViews();
        if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame&&hud.CancelInstallation()){pointerBlocked=true;return;}
        if(Mouse.current==null||Camera.main==null)return;
        var mouse=Mouse.current.position.ReadValue();
        var world=Camera.main.ScreenToWorldPoint(new Vector3(mouse.x,mouse.y,-Camera.main.transform.position.z));
        var cell=ground.WorldToCell(world);var xy=new Vector2Int(cell.x,cell.y);
        if(Installing.HasValue&&ghost!=null)
        {
            // The tile-centered preview is the visible placement cursor. Preserve normal UI input.
            bool onBoard=ground.HasTile(cell)&&!OverUI(mouse);
            Cursor.visible=!onBoard;
            ghost.SetActive(onBoard);
            ghost.transform.position=ground.GetCellCenterWorld(cell);
            bool valid=CanInstall(xy);
            // Same active renderer set as the previous per-frame lookup: captured once, applied only while shown.
            if(onBoard)foreach(var sr in ghostRenderers)if(sr!=null)sr.color=new Color(valid?.55f:1f,valid?1f:.35f,valid?.65f:.35f,.6f);
        }
        // A UI press owns the entire gesture, including a release after its panel disappears.
        // Commit world clicks on release so the UI Button release cannot also place/select.
        if(Mouse.current.leftButton.wasPressedThisFrame)
        {
            pointerGesture=true;pointerCell=xy;
            pointerBlocked=OverUI(mouse)||Session.Phase==SessionPhase.Augmentation||phase!=Session.Phase;
            pointerPlayer=!Installing.HasValue&&PlayerHit(mouse);
        }
        if(!Mouse.current.leftButton.wasReleasedThisFrame)return;
        bool playerClick=pointerPlayer&&!Installing.HasValue&&PlayerHit(mouse);
        bool worldClick=pointerGesture&&!pointerBlocked&&(pointerCell==xy||playerClick)&&!OverUI(mouse);
        pointerGesture=false;pointerBlocked=false;
        if(worldClick&&playerClick){hud.OpenPlayerSpecs();return;}
        if(worldClick)HandleWorldClick(xy);
    }
    // Hit the visible artwork, including the torso above the ground cell, never its shadow.
    bool PlayerHit(Vector2 screen)
    {
        if(player==null||Camera.main==null)return false;
        var artwork=player.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r=>r.name=="2D Sprite");
        if(artwork==null)return false;
        var ray=Camera.main.ScreenPointToRay(screen);
        foreach(var r in artwork.GetComponentsInChildren<SpriteRenderer>())
        {
            if(!r.enabled||r.sprite==null||r.color.a<=0)continue;
            if(!new Plane(r.transform.forward,r.transform.position).Raycast(ray,out float distance))continue;
            var point=r.transform.InverseTransformPoint(ray.GetPoint(distance));point.z=0;
            if(r.flipX)point.x=-point.x;if(r.flipY)point.y=-point.y;
            if(r.sprite.bounds.Contains(point))return true;
        }
        return false;
    }
    public void HandleWorldClick(Vector2Int xy)
    {
        if(Session.Phase==SessionPhase.Augmentation)return;
        if(Installing.HasValue){Place(Installing.Value,xy);return;}
        var tower=Session.Towers.FirstOrDefault(t=>t.Position==xy);
        if(tower!=null){SelectedTower=tower.Id;hud.OpenSelectedTowerSpecs();Changed?.Invoke();return;}
        if(Vector2Int.RoundToInt(PlayerTile())==xy){hud.OpenPlayerSpecs();return;}
        var cell=new Vector3Int(xy.x,xy.y,0);
        bool empty=ground.HasTile(cell)&&!enemyPath.HasTile(cell)&&!coreMap.HasTile(cell)&&!occupied.HasTile(cell)
            &&!Session.Enemies.Any(e=>e.Alive&&Vector2Int.RoundToInt(e.Position)==xy)&&Vector2Int.RoundToInt(PlayerTile())!=xy;
        if(!empty)return;
        if(hud.TryFoldDetails())return;
        SelectedTower=-1;Changed?.Invoke();
    }
    bool OverUI(Vector2 position)
    {
        if(EventSystem.current==null)return false;
        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=position},uiRaycastResults);
        bool hit=uiRaycastResults.Count>0;uiRaycastResults.Clear();return hit;
    }
    bool CanInstall(Vector2Int c)
    {
        var cell=new Vector3Int(c.x,c.y,0);
        return ground.HasTile(cell)&&!enemyPath.HasTile(cell)&&!coreMap.HasTile(cell)&&!occupied.HasTile(cell);
    }
    public void ChooseTower(TowerKind kind)
    {
        CancelInstall();
        if(Session.Phase!=SessionPhase.Preparation){GetComponent<CoreGuardianCombatAudio>()?.PlayCue("install_fail");Notify("전투 중에는 설치할 수 없습니다.");return;}
        if(Session.Crystals<Session.Data.Towers[kind].Cost){GetComponent<CoreGuardianCombatAudio>()?.PlayCue("install_fail");Notify("수정 부족 · 필요 "+Session.Data.Towers[kind].Cost+"수정");return;}
        if(kind==TowerKind.Support&&!Session.Towers.Any(t=>t.Kind!=TowerKind.Support)){GetComponent<CoreGuardianCombatAudio>()?.PlayCue("install_fail");Notify("공격 포탑을 먼저 설치하세요.");return;}
        Installing=kind;
        ghost=Instantiate(towerArt[(int)kind]);ghost.name="TowerPlacementPreview";
        var shadow=ghost.transform.Find("그림자");if(shadow!=null)shadow.gameObject.SetActive(false);
        ghostRenderers=ghost.GetComponentsInChildren<SpriteRenderer>();
        foreach(var sr in ghostRenderers)sr.sortingOrder=1800;
        foreach(var d in ghost.GetComponentsInChildren<ArtDepthSorting>())d.enabled=false;
        if(placementOverlay==null)
            placementOverlay=new CoreGuardianPlacementOverlay(transform,square,UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.default2DMaterial);
        placementOverlay.Show(ground,CanInstall);
        Cursor.SetCursor(null,Vector2.zero,CursorMode.Auto);Changed?.Invoke();
    }
    public void CancelInstall()
    {
        bool wasInstalling=Installing.HasValue;
        Installing=null;if(ghost!=null)Destroy(ghost);ghostRenderers=System.Array.Empty<SpriteRenderer>();
        placementOverlay?.Hide();
        Cursor.SetCursor(null,Vector2.zero,CursorMode.Auto);
        Cursor.visible=true;
        if(wasInstalling)Changed?.Invoke();
    }
    public bool Place(TowerKind kind,Vector2Int cell)
    {
        if(!CanInstall(cell)){GetComponent<CoreGuardianCombatAudio>()?.PlayCue("install_fail");Notify("이 칸에는 설치할 수 없습니다.");return false;}
        string reason;var tower=Session.TryInstall(kind,cell,out reason);
        if(tower==null){GetComponent<CoreGuardianCombatAudio>()?.PlayCue("install_fail");Notify(reason);return false;}
        occupied.SetTile(new Vector3Int(cell.x,cell.y,0),occupiedTile);
        GetComponent<CoreGuardianCombatAudio>()?.NotifyTowerInstalled();
        SelectedTower=tower.Id;CancelInstall();Notify("포탑 설치 완료");SynchronizeViews();return true;
    }
    public bool PlaceAtPlayerForTest(){var p=player.CurrentCell;return Place(TowerKind.Single,new Vector2Int(p.x,p.y));}
    public void DestroySelected()
    {
        var t=Session.Towers.FirstOrDefault(x=>x.Id==SelectedTower);if(t==null)return;
        var pos=t.Position;if(!Session.DestroyTower(t.Id))return;occupied.SetTile(new Vector3Int(pos.x,pos.y,0),null);
        GetComponent<CoreGuardianCombatAudio>()?.PlayCue("demolish");
        SelectedTower=-1;SynchronizeViews();Notify(Session.LastMessage);
    }
    public void UpgradePlayer(string stat)
    {
        string reason;if(Session.UpgradePlayer(stat,out reason)){upgradeFeedback.Trigger(player.transform);GetComponent<CoreGuardianCombatAudio>()?.NotifyUpgradeSucceeded();}
        else GetComponent<CoreGuardianCombatAudio>()?.NotifyUpgradeFailed();
        Notify(reason);
    }
    public void UpgradeTower(string stat)
    {
        int id=SelectedTower;string reason;
        if(Session.UpgradeTower(id,stat,out reason))
        {
            var kind=Session.Towers.First(t=>t.Id==id).Kind;
            foreach(var tower in Session.Towers.Where(t=>t.Kind==kind))upgradeFeedback.Trigger(TowerView(tower.Id));
            GetComponent<CoreGuardianCombatAudio>()?.NotifyUpgradeSucceeded();
        }
        else GetComponent<CoreGuardianCombatAudio>()?.NotifyUpgradeFailed();
        Notify(reason);
    }
    public void StartCombat(){CancelInstall();Session.StartCombat();SyncTestSpeed();Notify(Session.LastMessage);}
    public void Diagnostic()
    {
        hud.ClearDamageNumbers();
        upgradeFeedback.Clear();
        combatArt.ClearEffects();
        CancelInstall();Session.StartDiagnosticEncounter();SelectedTower=-1;previousGold=Session.Gold;previousCore=Session.CoreEnergy;
        SyncTestSpeed();
        occupied.ClearAllTiles();foreach(var t in Session.Towers)occupied.SetTile(new Vector3Int(t.Position.x,t.Position.y,0),occupiedTile);
        SynchronizeViews();Notify("역할 검증 모드 — 정식 라운드 편성·재화와 별도");
    }
    public int TestSpeed {get;private set;}=1;
    void SyncTestSpeed(bool force=false)
    {
        // An external QA pause/clock at default speed is not owned by this button.
        if(force||TestSpeed!=1)Time.timeScale=Ready&&Session.Phase==SessionPhase.Combat?TestSpeed:1;
    }
    public void CycleTestSpeed()
    {
        TestSpeed=TestSpeed==3?1:TestSpeed+1;
        SyncTestSpeed(true);
        Notify("전투 배속 ×"+TestSpeed+" · 준비/증강/결과는 ×1");
    }
    public void Restart(){if(TestSpeed!=1)Time.timeScale=1;UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.name);}
    public void Notify(string text){Feedback=string.IsNullOrEmpty(text)?Session.LastMessage:text;notificationUntil=Time.unscaledTime+3;Changed?.Invoke();}
    void SynchronizeViews()
    {
        livingViewEnemies.Clear();activeEnemyViewIds.Clear();
        foreach(var enemy in Session.Enemies)if(enemy.Alive){livingViewEnemies.Add(enemy);activeEnemyViewIds.Add(enemy.Id);}
        ReturnRemovedEnemyBodies();SyncRemoved(auraViews,activeEnemyViewIds);
        foreach(var e in livingViewEnemies)
        {
            if(!enemyBodies.TryGetValue(e.Id,out var body))
            {
                body=RentEnemyBody(e);enemyBodies.Add(e.Id,body);enemyViews.Add(e.Id,body.Root);
                healthViews.Add(e.Id,body.Health.transform);
                if(e.Kind==EnemyKind.Boss)GetComponent<CoreGuardianCombatAudio>()?.NotifyBossSpawn(e.Id);
            }
            var view=body.Root;
            view.transform.position=World(e.Position);
            combatAnimation.Enemy(e,view);
            body.Health.transform.position=body.Animation.HealthBarWorld;
            body.Health.SetHealth(e.Health,e.Spec.Health);
            if(e.Buffed&&!auraViews.ContainsKey(e.Id))auraViews[e.Id]=combatArt.Marker("EnemySpeedBuff",World(e.Position),.17f);
            if(auraViews.TryGetValue(e.Id,out var aura)){aura.SetActive(e.Buffed);aura.transform.position=World(e.Position)+Vector3.up*1.02f;}
        }
        activeTowerViewIds.Clear();foreach(var tower in Session.Towers)activeTowerViewIds.Add(tower.Id);
        SyncRemoved(towerViews,activeTowerViewIds);
        SyncRemoved(towerAuraViews,activeTowerViewIds);
        Session.BeginTowerViews();
        try
        {
        foreach(var t in Session.Towers)
        {
            if(!towerViews.TryGetValue(t.Id,out var view)){view=Instantiate(towerArt[(int)t.Kind]);view.name="Tower_"+t.Kind+"_"+t.Id;towerViews.Add(t.Id,view);}
            view.transform.position=World(t.Position);
            combatAnimation.Tower(t,view);
            bool supported=t.Kind!=TowerKind.Support&&(Session.Augments.TowerBuffPercent>0||Session.HasTowerSupport(t));
            if(supported&&!towerAuraViews.ContainsKey(t.Id))towerAuraViews[t.Id]=combatArt.Marker("TowerDamageBuff",World(t.Position),.2f);
            if(towerAuraViews.TryGetValue(t.Id,out var aura)){aura.SetActive(supported);aura.transform.position=World(t.Position)+Vector3.up*.88f;}
        }
        }
        finally{Session.EndTowerViews();}
        activeProjectileViewIds.Clear();foreach(var projectile in Session.Projectiles)activeProjectileViewIds.Add(projectile.Id);
        SyncRemoved(projectileViews,activeProjectileViewIds,true);
        foreach(var p in Session.Projectiles)
        {
            if(!projectileViews.TryGetValue(p.Id,out var view)){view=RentProjectileView(p);projectileViews.Add(p.Id,view);}
            view.transform.position=combatAnimation.ProjectilePosition(p);
            var direction=World(p.TargetPosition)+Vector3.up*.25f-view.transform.position;
            if(direction.sqrMagnitude>.000001f)view.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
        }
        combatAnimation.Player();
    }
    void ReturnRemovedEnemyBodies()
    {
        removedViewIds.Clear();
        foreach(var entry in enemyViews)
            if(!activeEnemyViewIds.Contains(entry.Key)||entry.Value==null||!enemyBodies.TryGetValue(entry.Key,out var body)||!body.Valid)removedViewIds.Add(entry.Key);
        foreach(int id in removedViewIds)
        {
            combatAnimation.ReleaseEnemy(id);
            enemyBodies.TryGetValue(id,out var body);
            var root=enemyViews[id];
            if(root!=null)
            {
                // These hosts live outside the enemy hierarchy. Detach them before same-frame reuse.
                GetComponent<CoreGuardianObjectEmphasis>()?.ReleaseTarget(root.transform);
                upgradeFeedback?.ReleaseTarget(root.transform);
                root.SetActive(false); // Restores hit materials and releases the contact-mask binding.
                if(body!=null&&body.Valid)
                {
                    body.Animation.ResetEnemyForReuse();
                    if(!enemyBodyPools.TryGetValue(body.Kind,out var pool)){pool=new Stack<EnemyBodyView>();enemyBodyPools.Add(body.Kind,pool);}
                    int capacity=body.Kind==EnemyKind.Boss?MaxRetainedBossBodies:MaxRetainedEnemyBodies;
                    if(pool.Count<capacity)
                    {
                        if(enemyPoolRoot==null)
                        {
                            var host=new GameObject("CoreGuardianEnemyBodyPool");host.transform.SetParent(transform,false);host.SetActive(false);enemyPoolRoot=host.transform;
                        }
                        root.transform.SetParent(enemyPoolRoot,false);pool.Push(body);
                    }
                    else Destroy(root);
                }
                else Destroy(root);
            }
            enemyViews.Remove(id);healthViews.Remove(id);enemyBodies.Remove(id);
        }
        removedViewIds.Clear();
    }
    EnemyBodyView RentEnemyBody(EnemyHandle enemy)
    {
        var source=enemy.Kind==EnemyKind.Boss?bossArt:enemyArt[(int)enemy.Kind];
        EnemyBodyView body=null;
        if(enemyBodyPools.TryGetValue(enemy.Kind,out var pool))
            while(pool.Count>0&&body==null)
            {
                var candidate=pool.Pop();
                if(candidate.Valid&&candidate.Source==source)body=candidate;
                else if(candidate.Root!=null)Destroy(candidate.Root);
            }
        if(body==null)
        {
            var root=Instantiate(source);root.name="Enemy_"+enemy.Kind+"_"+enemy.Id;root.transform.position=World(enemy.Position);
            var bar=CreateSprite("Health",square,Vector3.zero,new Color(.35f,1f,.45f),1900);
            bar.transform.SetParent(root.transform,false);
            var health=bar.AddComponent<SegmentedEnemyHealthBar>();health.Configure(enemy.Kind==EnemyKind.Boss);
            var animation=CoreGuardianUnitAnimation.Attach(root,enemy.Kind.ToString());
            body=new EnemyBodyView{Kind=enemy.Kind,Source=source,Root=root,Animation=animation,Shadow=root.GetComponent<CoreGuardianPhotoShadow>(),Health=health,InitialScale=root.transform.localScale,InitialRotation=root.transform.rotation};
        }
        else
        {
            body.Root.transform.SetParent(null,false);
            body.Root.transform.localScale=body.InitialScale;body.Root.transform.rotation=body.InitialRotation;
            body.Root.transform.position=World(enemy.Position);
            body.Animation.ResetEnemyForReuse();body.Shadow.InvalidateSources();
            body.Health.Configure(enemy.Kind==EnemyKind.Boss);
        }
        body.Root.name="Enemy_"+enemy.Kind+"_"+enemy.Id;
        body.Health.SetHealth(enemy.Health,enemy.Spec.Health);
        body.Health.transform.position=body.Animation.HealthBarWorld;
        body.Root.SetActive(true);return body;
    }
    void SyncRemoved(Dictionary<int,GameObject> views,HashSet<int> active,bool poolProjectiles=false)
    {
        removedViewIds.Clear();
        foreach(var id in views.Keys)if(!active.Contains(id))removedViewIds.Add(id);
        foreach(var id in removedViewIds)
        {
            var view=views[id];
            if(view!=null)
            {
                var renderer=poolProjectiles?view.GetComponent<SpriteRenderer>():null;
                if(renderer!=null&&projectileViewPool.Count<MaxRetainedProjectileViews){view.SetActive(false);projectileViewPool.Push(renderer);}
                else Destroy(view);
            }
            views.Remove(id);
        }
        removedViewIds.Clear();
    }
    GameObject RentProjectileView(ProjectileHandle projectile)
    {
        SpriteRenderer renderer=null;
        while(projectileViewPool.Count>0&&renderer==null)renderer=projectileViewPool.Pop();
        if(renderer==null)renderer=CreateSprite("Projectile_"+projectile.Id,combatArt.Projectile(projectile),World(projectile.Position),Color.white,1500).GetComponent<SpriteRenderer>();
        else{renderer.name="Projectile_"+projectile.Id;renderer.sprite=combatArt.Projectile(projectile);}
        renderer.color=Color.white;renderer.flipX=renderer.flipY=false;renderer.sortingOrder=1500;
        renderer.transform.position=World(projectile.Position);renderer.transform.rotation=Quaternion.identity;
        CoreGuardianCombatArt.SetWidth(renderer,projectile.Source=="Player"?.2f:.275f);
        renderer.transform.SetAsLastSibling();renderer.gameObject.SetActive(true);return renderer.gameObject;
    }
    GameObject CreateSprite(string name,Sprite sprite,Vector3 pos,Color color,int order)
    {
        var g=new GameObject(name);g.transform.SetParent(transform);g.transform.position=pos;
        var r=g.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=order;return g;
    }
    GameObject Ring(string name,Color color,float radius)
    {
        var g=new GameObject(name);g.transform.SetParent(transform);
        var l=g.AddComponent<LineRenderer>();l.useWorldSpace=false;l.loop=true;l.positionCount=48;l.widthMultiplier=.025f;
        l.material=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));l.startColor=l.endColor=color;l.sortingOrder=4;
        for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;l.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius*.4f,0));}return g;
    }
    Sprite MakeSprite(bool circle)
    {
        var texture=new Texture2D(16,16,TextureFormat.RGBA32,false);texture.filterMode=FilterMode.Point;
        for(int y=0;y<16;y++)for(int x=0;x<16;x++)texture.SetPixel(x,y,!circle||new Vector2(x-7.5f,y-7.5f).sqrMagnitude<56?Color.white:Color.clear);
        texture.Apply();return Sprite.Create(texture,new Rect(0,0,16,16),new Vector2(.5f,.5f),16);
    }
    void OnDestroy()
    {
        placementOverlay?.Dispose();placementOverlay=null;
        Session?.Dispose();
        if(TestSpeed!=1)Time.timeScale=1;
        Cursor.SetCursor(null,Vector2.zero,CursorMode.Auto);
        Cursor.visible=true;
        if(occupiedTile!=null)Destroy(occupiedTile);
        foreach(var views in new[]{enemyViews,towerViews,projectileViews,auraViews,towerAuraViews})foreach(var g in views.Values)if(g!=null)Destroy(g);
        foreach(var pool in enemyBodyPools.Values)
            while(pool.Count>0){var body=pool.Pop();if(body.Root!=null)Destroy(body.Root);}
        enemyBodyPools.Clear();enemyBodies.Clear();
        while(projectileViewPool.Count>0){var renderer=projectileViewPool.Pop();if(renderer!=null)Destroy(renderer.gameObject);}
        if(ghost!=null)Destroy(ghost);if(square!=null){Destroy(square.texture);Destroy(square);}if(dot!=null){Destroy(dot.texture);Destroy(dot);}
    }
}

