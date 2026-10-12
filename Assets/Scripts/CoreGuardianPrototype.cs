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
    readonly List<GameObject> highlights = new List<GameObject>();
    GameObject ghost;
    Sprite square, dot;
    Tile occupiedTile;
    CoreGuardianHud hud;
    CoreGuardianCombatArt combatArt;
    int previousGold;
    int previousCore;
    float notificationUntil;
    Vector3[] route;
    public bool Ready => Session != null;

    void Start()
    {
        Application.runInBackground = true;
        try
        {
            if(ground==null||player==null||occupied==null)throw new InvalidOperationException("Missing gameplay scene references");
            Session=new Session(CombatData.Load(enemiesCsv.text,towersCsv.text,stageCsv.text,upgradesCsv.text,wavesCsv.text));
            var cells=new[]{new Vector2Int(0,12),new Vector2Int(11,12),new Vector2Int(11,18),new Vector2Int(35,18),new Vector2Int(35,6),new Vector2Int(17,6),new Vector2Int(17,12),new Vector2Int(29,12),new Vector2Int(29,9)};
            route=cells.Select(c=>World(c)).ToArray();
            Session.SetBoard(45,25,new Vector2Int(29,9),cells);
            occupiedTile=ScriptableObject.CreateInstance<Tile>(); // Hidden occupancy map, independent of visible art.
            square=MakeSprite(false);dot=MakeSprite(true);
            hud=gameObject.AddComponent<CoreGuardianHud>();hud.Configure(this);
            combatArt=gameObject.AddComponent<CoreGuardianCombatArt>();combatArt.Configure(this,hud);
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
        var p=ground.transform.InverseTransformPoint(player.transform.position-ground.GetCellCenterWorld(Vector3Int.zero));
        return new Vector2(p.x/ground.layoutGrid.cellSize.x,p.y/ground.layoutGrid.cellSize.y);
    }
    void Update()
    {
        if(!Ready)return;
        var phase=Session.Phase;
        Session.Tick(Time.deltaTime,PlayerTile());
        player.enabled=Session.Phase!=SessionPhase.Augmentation;
        if(Session.Gold>previousGold){ Feedback="+"+(Session.Gold-previousGold)+" 골드 획득";notificationUntil=Time.unscaledTime+2; }
        previousGold=Session.Gold;
        if(Session.CoreEnergy<previousCore){combatArt.CoreDamaged(previousCore-Session.CoreEnergy);Feedback="코어 에너지 -"+(previousCore-Session.CoreEnergy);Changed?.Invoke();}
        previousCore=Session.CoreEnergy;
        if(phase!=Session.Phase){CancelInstall();Notify(Session.LastMessage);}
        SynchronizeViews();
        if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame&&hud.CancelInstallation())return;
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
            foreach(var sr in ghost.GetComponentsInChildren<SpriteRenderer>())sr.color=new Color(valid?.55f:1f,valid?1f:.35f,valid?.65f:.35f,.6f);
        }
        if(!Mouse.current.leftButton.wasPressedThisFrame||OverUI(mouse))return;
        if(Installing.HasValue)Place(Installing.Value,xy);
        else {var tower=Session.Towers.FirstOrDefault(t=>t.Position==xy);SelectedTower=tower==null?-1:tower.Id;Changed?.Invoke();}
    }
    bool OverUI(Vector2 position)
    {
        if(EventSystem.current==null)return false;
        var results=new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=position},results);
        return results.Count>0;
    }
    bool CanInstall(Vector2Int c)
    {
        var cell=new Vector3Int(c.x,c.y,0);
        return ground.HasTile(cell)&&!enemyPath.HasTile(cell)&&!coreMap.HasTile(cell)&&!occupied.HasTile(cell);
    }
    public void ChooseTower(TowerKind kind)
    {
        CancelInstall();
        if(Session.Phase!=SessionPhase.Preparation){Notify("전투 중에는 설치할 수 없습니다.");return;}
        Installing=kind;
        ghost=Instantiate(towerArt[(int)kind]);ghost.name="TowerPlacementPreview";
        var shadow=ghost.transform.Find("그림자");if(shadow!=null)shadow.gameObject.SetActive(false);
        foreach(var sr in ghost.GetComponentsInChildren<SpriteRenderer>())sr.sortingOrder=1800;
        foreach(var d in ghost.GetComponentsInChildren<ArtDepthSorting>())d.enabled=false;
        foreach(var p in ground.cellBounds.allPositionsWithin)
        {
            if(!ground.HasTile(p))continue;
            var h=CreateSprite("InstallationTile",square,ground.GetCellCenterWorld(p),CanInstall(new Vector2Int(p.x,p.y))?new Color(.1f,1f,.35f,.13f):new Color(1f,.15f,.1f,.2f),3);
            h.transform.localScale=new Vector3(.55f,.55f,1);highlights.Add(h);
        }
        Cursor.SetCursor(null,Vector2.zero,CursorMode.Auto);Changed?.Invoke();
    }
    public void CancelInstall()
    {
        Installing=null;if(ghost!=null)Destroy(ghost);
        foreach(var h in highlights)if(h!=null)Destroy(h);highlights.Clear();
        Cursor.SetCursor(null,Vector2.zero,CursorMode.Auto);
        Cursor.visible=true;
    }
    public bool Place(TowerKind kind,Vector2Int cell)
    {
        if(!CanInstall(cell)){Notify("이 칸에는 설치할 수 없습니다.");return false;}
        string reason;var tower=Session.TryInstall(kind,cell,out reason);
        if(tower==null){Notify(reason);return false;}
        occupied.SetTile(new Vector3Int(cell.x,cell.y,0),occupiedTile);
        SelectedTower=tower.Id;Notify("포탑 설치 완료");CancelInstall();SynchronizeViews();return true;
    }
    public bool PlaceAtPlayerForTest(){var p=player.CurrentCell;return Place(TowerKind.Single,new Vector2Int(p.x,p.y));}
    public void DestroySelected()
    {
        var t=Session.Towers.FirstOrDefault(x=>x.Id==SelectedTower);if(t==null)return;
        var pos=t.Position;Session.DestroyTower(t.Id);occupied.SetTile(new Vector3Int(pos.x,pos.y,0),null);
        SelectedTower=-1;SynchronizeViews();Notify(Session.LastMessage);
    }
    public void UpgradePlayer(string stat){string reason;Session.UpgradePlayer(stat,out reason);Notify(reason);}
    public void UpgradeTower(string stat){string reason;Session.UpgradeTower(SelectedTower,stat,out reason);Notify(reason);}
    public void StartCombat(){CancelInstall();Session.StartCombat();Notify(Session.LastMessage);}
    public void Diagnostic()
    {
        hud.ClearDamageNumbers();
        combatArt.ClearEffects();
        CancelInstall();Session.StartDiagnosticEncounter();SelectedTower=-1;previousGold=Session.Gold;previousCore=Session.CoreEnergy;
        occupied.ClearAllTiles();foreach(var t in Session.Towers)occupied.SetTile(new Vector3Int(t.Position.x,t.Position.y,0),occupiedTile);
        SynchronizeViews();Notify("역할 검증 모드 — 정식 라운드 편성·재화와 별도");
    }
    public void Restart(){UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.name);}
    public void Notify(string text){Feedback=string.IsNullOrEmpty(text)?Session.LastMessage:text;notificationUntil=Time.unscaledTime+3;Changed?.Invoke();}
    void SynchronizeViews()
    {
        var living=Session.Enemies.Where(e=>e.Alive).ToArray();
        SyncRemoved(enemyViews,living.Select(e=>e.Id));SyncRemoved(auraViews,living.Select(e=>e.Id));
        foreach(var id in healthViews.Keys.Except(living.Select(e=>e.Id)).ToArray())healthViews.Remove(id);
        foreach(var e in living)
        {
            GameObject view;
            if(!enemyViews.TryGetValue(e.Id,out view))
            {
                view=Instantiate(enemyArt[e.Kind==EnemyKind.Boss?1:(int)e.Kind]);view.name="Enemy_"+e.Kind+"_"+e.Id;enemyViews.Add(e.Id,view);
                if(e.Kind==EnemyKind.Boss)view.transform.localScale*=1.4f;
                var bar=CreateSprite("Health",square,Vector3.zero,new Color(.35f,1f,.45f),1900);
                bar.transform.SetParent(view.transform,false);bar.transform.localPosition=new Vector3(0,.8f,0);healthViews[e.Id]=bar.transform;
            }
            view.transform.position=World(e.Position);
            if(e.Kind==EnemyKind.Boss)foreach(var renderer in view.GetComponentsInChildren<SpriteRenderer>())if(renderer.name!="Health")renderer.color=e.Enraged?new Color(1f,.42f,.36f):new Color(.88f,.7f,1f);
            healthViews[e.Id].localScale=new Vector3(.35f*e.Health/e.Spec.Health,.035f,1);
            if(e.Buffed&&!auraViews.ContainsKey(e.Id))auraViews[e.Id]=combatArt.Marker("EnemySpeedBuff",World(e.Position),.17f);
            if(auraViews.TryGetValue(e.Id,out var aura)){aura.SetActive(e.Buffed);aura.transform.position=World(e.Position)+Vector3.up*1.02f;}
        }
        SyncRemoved(towerViews,Session.Towers.Select(t=>t.Id));
        SyncRemoved(towerAuraViews,Session.Towers.Select(t=>t.Id));
        foreach(var t in Session.Towers)
        {
            if(!towerViews.TryGetValue(t.Id,out var view)){view=Instantiate(towerArt[(int)t.Kind]);view.name="Tower_"+t.Kind+"_"+t.Id;towerViews.Add(t.Id,view);}
            view.transform.position=World(t.Position);
            bool supported=t.Kind!=TowerKind.Support&&(Session.Augments.TowerBuffPercent>0||Session.Towers.Any(s=>s.Kind==TowerKind.Support&&Vector2.Distance(s.Position,t.Position)<=s.Stats.Range));
            if(supported&&!towerAuraViews.ContainsKey(t.Id))towerAuraViews[t.Id]=combatArt.Marker("TowerDamageBuff",World(t.Position),.2f);
            if(towerAuraViews.TryGetValue(t.Id,out var aura)){aura.SetActive(supported);aura.transform.position=World(t.Position)+Vector3.up*.88f;}
        }
        SyncRemoved(projectileViews,Session.Projectiles.Select(p=>p.Id));
        foreach(var p in Session.Projectiles)
        {
            if(!projectileViews.TryGetValue(p.Id,out var view)){view=CreateSprite("Projectile_"+p.Id,combatArt.Projectile(p),World(p.Position),Color.white,1500);CoreGuardianCombatArt.SetWidth(view.GetComponent<SpriteRenderer>(),p.Source=="Player"?.2f:.275f);projectileViews.Add(p.Id,view);}
            view.transform.position=World(p.Position)+Vector3.up*.25f;
            var direction=p.Target.Position-p.Position;
            if(direction.sqrMagnitude>.000001f)view.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
        }
    }
    void SyncRemoved(Dictionary<int,GameObject> views,IEnumerable<int> active)
    {
        var ids=new HashSet<int>(active);foreach(var id in views.Keys.Where(k=>!ids.Contains(k)).ToArray()){if(views[id]!=null)Destroy(views[id]);views.Remove(id);}
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
        Cursor.SetCursor(null,Vector2.zero,CursorMode.Auto);
        Cursor.visible=true;
        if(occupiedTile!=null)Destroy(occupiedTile);
        foreach(var views in new[]{enemyViews,towerViews,projectileViews,auraViews,towerAuraViews})foreach(var g in views.Values)if(g!=null)Destroy(g);
        if(ghost!=null)Destroy(ghost);if(square!=null){Destroy(square.texture);Destroy(square);}if(dot!=null){Destroy(dot.texture);Destroy(dot);}
    }
}

