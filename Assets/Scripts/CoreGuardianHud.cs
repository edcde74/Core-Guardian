using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

// Runtime HUD with shared sprite frames and live Korean labels.
public sealed class CoreGuardianHud : MonoBehaviour
{
    CoreGuardianPrototype game;
    Font font;
    TMP_FontAsset damageFont;
    Material damageMaterial;
    RectTransform canvas, worldFeedback, installPanel, playerPanel, towerPanel, damagePanel, damageContent, topDetails;
    UnityEngine.UI.Text status, center, feedback, towerInfo;
    readonly List<Action> refresh = new List<Action>();
    float nextRefresh;
    string damageSignature;
    RectTransform augmentationPanel;
    UnityEngine.UI.Text augmentationTimer, bossStatus;
    readonly List<UnityEngine.UI.Button> augmentationButtons=new List<UnityEngine.UI.Button>();
    bool playerOpen, installOpen;
    RectTransform cardsRoot;
    string choiceSignature;
    bool cardsFolded, towerFolded;
    int lastTower=-1;
    sealed class DamageNumber
    {
        public TextMeshProUGUI Text;
        public RectTransform Root;
        public UnityEngine.UI.Image Icon;
        public Vector2 Offset;
        public Vector3 World;
        public float Started;
        public int Lane;
    }
    readonly List<DamageNumber> damageNumbers=new List<DamageNumber>();
    int nextDamageLane;
    public void Configure(CoreGuardianPrototype prototype)
    {
        game=prototype;
        font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",20);
        damageFont=Resources.Load<TMP_FontAsset>("CoreGuardian/Fonts/PixelifySansBoldDamage");
        damageMaterial=Resources.Load<Material>("CoreGuardian/Fonts/PixelifySansBoldDamageOutline");
        if(damageFont==null||damageMaterial==null)throw new InvalidOperationException("Pixelify Sans Bold TMP assets missing");
        var root=new GameObject("CombatHUD",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
        root.transform.SetParent(transform,false);canvas=root.GetComponent<RectTransform>();
        var c=root.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=2000;
        var scale=root.GetComponent<UnityEngine.UI.CanvasScaler>();scale.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1920,1080);scale.matchWidthOrHeight=.5f;
        if(EventSystem.current==null)new GameObject("CombatEventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule)).transform.SetParent(transform);
        worldFeedback=Box("WorldFeedback",canvas,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero,false);
        worldFeedback.anchorMin=Vector2.zero;worldFeedback.anchorMax=Vector2.one;worldFeedback.sizeDelta=Vector2.zero;
        // Floating combat feedback always renders before every HUD panel and modal.
        worldFeedback.SetAsFirstSibling();
        Build();game.Changed+=Refresh;game.Session.DamageApplied+=ShowDamageNumber;
        BuildAugmentationSelection();
    }
    void BuildAugmentationSelection()
    {
        var bossPlate=Box("BossPlate",canvas,new Vector2(.5f,1),new Vector2(0,-224),new Vector2(740,60));
        CoreGuardianHudSkin.Icon(bossPlate,"Damage",new Vector2(16,-15),new Vector2(30,30));
        bossStatus=Text("BossStatus",bossPlate,new Vector2(54,-14),new Vector2(666,32),22);
        refresh.Add(()=>bossPlate.gameObject.SetActive(!string.IsNullOrEmpty(bossStatus.text)));
        bossStatus.color=new Color(1f,.6f,.45f);
        augmentationPanel=Box("AugmentationSelection",canvas,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1920,1080));
        augmentationPanel.anchorMin=Vector2.zero;augmentationPanel.anchorMax=Vector2.one;augmentationPanel.sizeDelta=Vector2.zero;
        var shade=augmentationPanel.GetComponent<UnityEngine.UI.Image>();shade.sprite=null;shade.color=new Color(.015f,.025f,.04f,.82f);
        var panel=Box("Cards",augmentationPanel,new Vector2(.5f,.5f),new Vector2(0,0),new Vector2(1140,650));
        cardsRoot=panel;
        CoreGuardianHudSkin.Icon(panel,"Augment",new Vector2(24,-24),new Vector2(30,30));
        Text("Title",panel,new Vector2(66,-18),new Vector2(1050,44),30).text="보스 격파 · 증강 1개 선택";
        augmentationTimer=Text("ChoiceTimer",panel,new Vector2(24,-74),new Vector2(1092,68),21);
        var fold=Button("AugmentFold",augmentationPanel,new Vector2(0,0),new Vector2(196,56),()=>cardsFolded?"증강 펼치기":"증강 접기",()=>{cardsFolded=!cardsFolded;cardsRoot.gameObject.SetActive(!cardsFolded);});
        var foldRect=fold.GetComponent<RectTransform>();foldRect.anchorMin=foldRect.anchorMax=foldRect.pivot=new Vector2(.5f,.5f);foldRect.anchoredPosition=new Vector2(470,369);
        augmentationButtons.Add(fold);
        augmentationPanel.gameObject.SetActive(false);
        topDetails.SetAsLastSibling();
    }
    void RefreshCards()
    {
        string signature=string.Join("|",game.Session.AugmentationChoices.Select(x=>x.Id));
        if(choiceSignature==signature)return;choiceSignature=signature;
        foreach(var b in augmentationButtons.Where(b=>b.name.StartsWith("AugmentationCard")).ToArray()){augmentationButtons.Remove(b);b.gameObject.SetActive(false);Destroy(b.gameObject);}
        cardsFolded=false;cardsRoot.gameObject.SetActive(true);
        for(int i=0;i<game.Session.AugmentationChoices.Count;i++){
            int index=i;var r=Box("AugmentationCard"+i,cardsRoot,new Vector2(0,1),new Vector2(24+i*370,-164),new Vector2(350,460));
            var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();augmentationButtons.Add(b);
            r.gameObject.AddComponent<CoreGuardianAugmentationView>().Configure(game.Session.AugmentationChoices[i],font,()=>{
                if(game.Session.ChooseAugmentation(index)){game.Notify(game.Session.LastMessage);Refresh();}
            },()=>game.Session.Phase==SessionPhase.Augmentation);
        }
    }
    RectTransform Box(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size,bool background=true)
    {
        var g=new GameObject(name,typeof(RectTransform));var r=g.GetComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=anchor;r.pivot=anchor;r.anchoredPosition=position;r.sizeDelta=size;
        if(background){var i=g.AddComponent<UnityEngine.UI.Image>();CoreGuardianHudSkin.Frame(i);}return r;
    }
    UnityEngine.UI.Text Text(string name,Transform parent,Vector2 position,Vector2 size,int textSize=20)
    {
        var r=Box(name,parent,new Vector2(0,1),position,size,false);var t=r.gameObject.AddComponent<UnityEngine.UI.Text>();
        t.font=font;t.fontSize=textSize;t.color=Color.white;t.raycastTarget=false;t.alignment=TextAnchor.MiddleLeft;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
    }
    UnityEngine.UI.Button Button(string name,Transform parent,Vector2 position,Vector2 size,Func<string> label,Action action,Func<bool> enabled=null)
    {
        var r=Box(name,parent,new Vector2(0,1),position,size);CoreGuardianHudSkin.Frame(r.GetComponent<UnityEngine.UI.Image>(),"Button");
        var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.onClick.AddListener(()=>action());
        float iconSize=Mathf.Min(30,size.y-24);CoreGuardianHudSkin.Icon(r,CoreGuardianHudSkin.Symbol(name),new Vector2(16,-(size.y-iconSize)/2),new Vector2(iconSize,iconSize));
        var t=Text("Label",r,new Vector2(54,-16),size-new Vector2(70,32),size.y<=48?14:18);
        var reaction=r.gameObject.AddComponent<CoreGuardianHudReaction>();
        reaction.Selected=()=>name=="Installation"?installOpen:name=="PlayerGrowth"?playerOpen:name.StartsWith("Install_")&&game.Installing.HasValue&&name=="Install_"+game.Installing.Value;
        refresh.Add(()=>{t.text=label();b.interactable=enabled==null||enabled();t.color=b.interactable?new Color(1,.95f,.84f):new Color(.78f,.8f,.82f);});return b;
    }
    void Build()
    {
        topDetails=Box("TopStatus",canvas,new Vector2(.5f,1),new Vector2(0,-12),new Vector2(920,72));
        status=Text("Status",topDetails,new Vector2(24,0),new Vector2(875,64),23);status.color=Color.clear;
        string[] symbols={"Core","Wave","Gold","Crystal","Augment"};
        for(int i=0;i<5;i++){int index=i;CoreGuardianHudSkin.Icon(topDetails,symbols[i],new Vector2(16+i*181,-21),new Vector2(30,30));var value=Text("Status_"+symbols[i],topDetails,new Vector2(52+i*181,-16),new Vector2(126,40),19);
            refresh.Add(()=>{var s=game.Session;value.text=index==0?"코어 "+s.CoreEnergy+"/"+s.CoreMaximum:index==1?"라운드 "+s.Wave+"/"+s.LastWave:index==2?"골드 "+s.Gold:index==3?"수정 "+s.Crystals:"증강 "+s.Augments.Selected.Count;});}
        var controls=Box("BottomControls",canvas,new Vector2(.5f,0),new Vector2(0,12),new Vector2(1160,88));
        Button("Installation",controls,new Vector2(16,-16),new Vector2(200,56),()=>"타워 설치",()=>SetInstallPanelOpen(!installOpen),()=>game.Session.Phase==SessionPhase.Preparation);
        Button("Start",controls,new Vector2(462,-16),new Vector2(236,56),()=>game.Session.Phase==SessionPhase.Preparation?"라운드 시작":game.Session.Phase==SessionPhase.Combat?"전투 진행":game.Session.Phase==SessionPhase.Augmentation?"증강 선택 중":game.Session.Phase==SessionPhase.Defeat?"패배":"검증 완료",()=>game.StartCombat(),()=>game.Session.Phase==SessionPhase.Preparation);
        Button("PlayerGrowth",controls,new Vector2(920,-16),new Vector2(224,56),()=>"플레이어 강화",()=>{playerOpen=!playerOpen;playerPanel.gameObject.SetActive(playerOpen);});
        var stagePlate=Box("StagePlate",canvas,new Vector2(.5f,1),new Vector2(0,-96),new Vector2(900,116));
        CoreGuardianHudSkin.Icon(stagePlate,"Wave",new Vector2(16,-16),new Vector2(28,28));
        center=Text("Preparation",stagePlate,new Vector2(54,-14),new Vector2(826,32),21);
        feedback=Text("Feedback",stagePlate,new Vector2(20,-52),new Vector2(860,48),18);
        var debug=Box("PrototypeActions",canvas,new Vector2(0,1),new Vector2(16,-14),new Vector2(302,88));
        Button("RoleDiagnostic",debug,new Vector2(16,-16),new Vector2(270,56),()=>"3종 역할 검증 / 재설정",()=>game.Diagnostic());
        Button("Reload",canvas,new Vector2(1630,-16),new Vector2(276,56),()=>"처음부터 다시 시작",()=>game.Restart());
        Button("TestSpeed",canvas,new Vector2(1630,-80),new Vector2(276,48),()=>"테스트 배속 ×"+game.TestSpeed,()=>game.CycleTestSpeed());

        installPanel=Box("TowerInstallPanel",canvas,new Vector2(.5f,0),new Vector2(-126,112),new Vector2(660,328));
        BuildInstallCategories();installPanel.gameObject.SetActive(false);
        Button("CancelPlacement",canvas,new Vector2(18,-1000),new Vector2(270,56),()=>"설치 취소 · Esc",()=>CancelInstallation(),()=>game.Installing.HasValue||installOpen);
        playerPanel=Box("PlayerGrowthPanel",canvas,new Vector2(.5f,0),new Vector2(500,112),new Vector2(520,412));
        CoreGuardianHudSkin.Icon(playerPanel,"Upgrade",new Vector2(16,-16),new Vector2(28,28));
        Text("Title",playerPanel,new Vector2(54,-14),new Vector2(446,32),22).text="플레이어 능력치 강화";
        var stats=new[]{"damage","rate","projectileSpeed","range"};var names=new[]{"공격력","공격속도","투사체 속도","사거리"};
        for(int i=0;i<stats.Length;i++)
        {
            string stat=stats[i],label=names[i];
            Button(stat,playerPanel,new Vector2(16,-60-i*84),new Vector2(488,80),()=>UpgradeLabel(game.Session.Player,"Player",stat,game.Session.PlayerUpgradePrice(stat),"골드"),()=>PurchasePlayer(stat),()=>game.Session.Gold>=game.Session.PlayerUpgradePrice(stat)&&(game.Session.Phase==SessionPhase.Preparation||game.Session.Phase==SessionPhase.Combat));
        }
        playerPanel.gameObject.SetActive(false);
        towerPanel=Box("TowerInformation",canvas,new Vector2(0,1),new Vector2(24,-120),new Vector2(460,560));
        towerInfo=Text("Stats",towerPanel,new Vector2(16,-16),new Vector2(428,112),20);
        foreach(var stat in new[]{"damage","rate","projectileSpeed","range","buffDamage"})
        {
            int index=Array.IndexOf(new[]{"damage","rate","projectileSpeed","range","buffDamage"},stat);string captured=stat;
            var button=Button("Tower_"+stat,towerPanel,new Vector2(16,-140-index*84),new Vector2(428,80),()=>Selected()==null?"":UpgradeLabel(Selected().Stats,Selected().Kind.ToString(),captured,Price(captured),"수정"),()=>PurchaseTower(captured),()=>TowerCanUpgrade(captured));
            refresh.Add(()=>
            {
                var t=Selected();bool relevant=t!=null&&Relevant(t,captured);button.gameObject.SetActive(relevant);
                if(relevant)button.GetComponent<RectTransform>().anchoredPosition=new Vector2(16,-140-(t.Kind==TowerKind.Support?(captured=="range"?0:1):index)*84);
            });
        }
        Button("Destroy",towerPanel,new Vector2(16,-488),new Vector2(428,56),()=>"파괴 · 실제 지출 50% 환급",()=>game.DestroySelected());
        towerPanel.gameObject.SetActive(false);
        Button("TowerFold",canvas,new Vector2(24,-692),new Vector2(180,48),()=>towerFolded?"타워 펼치기":"타워 접기",()=>{towerFolded=!towerFolded;});
        damagePanel=Box("DamagePanel",canvas,new Vector2(1,1),new Vector2(-16,-140),new Vector2(320,380));
        CoreGuardianHudSkin.Icon(damagePanel,"Damage",new Vector2(16,-16),new Vector2(28,28));
        Text("Title",damagePanel,new Vector2(54,-14),new Vector2(250,32),20).text="라운드 누적 피해";
        var scroll=Box("Scroll",damagePanel,new Vector2(0,1),new Vector2(16,-56),new Vector2(288,308),false);
        var viewport=Box("Viewport",scroll,new Vector2(0,1),Vector2.zero,new Vector2(288,308),false);
        var scrollSurface=viewport.gameObject.AddComponent<UnityEngine.UI.Image>();scrollSurface.color=Color.clear;scrollSurface.raycastTarget=true;
        viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
        damageContent=Box("Content",viewport,new Vector2(0,1),Vector2.zero,new Vector2(288,0),false);
        var layout=damageContent.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();layout.spacing=8;layout.childControlHeight=true;layout.childForceExpandHeight=false;layout.childControlWidth=true;
        damageContent.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        var sr=scroll.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();sr.viewport=viewport;sr.content=damageContent;sr.horizontal=false;sr.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;
        Button("DamageFold",canvas,new Vector2(1730,-532),new Vector2(174,48),()=>damagePanel.gameObject.activeSelf?"피해 접기":"피해 펼치기",()=>damagePanel.gameObject.SetActive(!damagePanel.gameObject.activeSelf));
        // Top compact display retains all four critical values when collapsed.
        Button("TopFold",canvas,new Vector2(1432,-24),new Vector2(170,48),()=>topDetails.sizeDelta.y>52?"상태 접기":"상태 펼치기",()=>{bool compact=topDetails.sizeDelta.y>52;topDetails.sizeDelta=new Vector2(920,compact?52:72);foreach(Transform child in topDetails){var rt=child as RectTransform;if(rt==null||child.name=="Status")continue;bool symbol=child.name.StartsWith("Symbol");rt.anchoredPosition=new Vector2(rt.anchoredPosition.x,compact?-12:(symbol?-21:-16));if(!symbol)rt.sizeDelta=new Vector2(rt.sizeDelta.x,compact?28:40);}});
        Refresh();
        Pin("Reload",new Vector2(1,1),new Vector2(-30,-16));Pin("TestSpeed",new Vector2(1,1),new Vector2(-30,-80));
        Pin("DamageFold",new Vector2(1,1),new Vector2(-16,-532));
        Pin("CancelPlacement",new Vector2(0,0),new Vector2(18,32));
    }
    void Pin(string name,Vector2 anchor,Vector2 position){var r=canvas.Find(name) as RectTransform;if(r==null)return;r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=position;}
    string StatName(string stat){switch(stat){case "damage":return "공격력";case "rate":return "공격속도";case "range":return "사거리";case "buffDamage":return "지원 공격력 효과";default:return "투사체 속도";}}
    float StatValue(CombatStats stats,string stat)=>stat=="damage"?stats.Damage:stat=="rate"?stats.Rate:stat=="range"?stats.Range:stat=="buffDamage"?stats.BuffDamage:stats.ProjectileSpeed;
    string StatDisplay(float value,string stat)=>stat=="damage"?Mathf.FloorToInt(value+.5f).ToString():value.ToString("0.##");
    string UpgradeLabel(CombatStats stats,string target,string stat,int cost,string currency)
    {
        var spec=game.Session.Data.Upgrades.FirstOrDefault(u=>u.Target==target&&u.Stat==stat);float value=StatValue(stats,stat),delta=spec==null?0:spec.Delta;
        bool affordable=currency=="골드"?game.Session.Gold>=cost:game.Session.Crystals>=cost;
        return StatName(stat)+" "+StatDisplay(value,stat)+" → "+StatDisplay(value+delta,stat)+"\n+"+delta.ToString("0.##")+" · "+cost+currency+(affordable?"":" · 부족");
    }
    void PurchasePlayer(string stat){float before=StatValue(game.Session.Player,stat);game.UpgradePlayer(stat);if(StatValue(game.Session.Player,stat)>before){game.Notify(StatName(stat)+" +"+(StatValue(game.Session.Player,stat)-before).ToString("0.##"));var b=playerPanel.Find(stat);if(b!=null)b.GetComponent<CoreGuardianHudReaction>().Confirm();}}
    void PurchaseTower(string stat){var tower=Selected();if(tower==null)return;float before=StatValue(tower.Stats,stat);game.UpgradeTower(stat);if(StatValue(tower.Stats,stat)>before){game.Notify(TowerName(tower.Kind)+" · "+StatName(stat)+" +"+(StatValue(tower.Stats,stat)-before).ToString("0.##"));var b=towerPanel.Find("Tower_"+stat);if(b!=null)b.GetComponent<CoreGuardianHudReaction>().Confirm();}}
    string TowerName(TowerKind kind)=>kind==TowerKind.Single?"단일 고속":kind==TowerKind.Multi?"범위 공격":"지원 포탑";
    string SourceName(string source)
    {
        if(source=="Player")return "기술병";
        int id;if(source.StartsWith("Tower:")&&int.TryParse(source.Substring(6),out id))
        {
            var tower=game.Session.Towers.FirstOrDefault(t=>t.Id==id);return (tower==null?"철거한 포탑":TowerName(tower.Kind))+" #"+id;
        }
        return source;
    }
    TowerState Selected()=>game.Session.Towers.FirstOrDefault(t=>t.Id==game.SelectedTower);
    int Price(string stat){var t=Selected();if(t==null||!Relevant(t,stat))return 0;return game.Session.TowerUpgradePrice(t.Id,stat);}
    bool Relevant(TowerState t,string stat)=>t.Kind==TowerKind.Support?(stat=="range"||stat=="buffDamage"):stat!="buffDamage";
    bool TowerCanUpgrade(string stat){var t=Selected();return t!=null&&Relevant(t,stat)&&game.Session.Phase==SessionPhase.Preparation&&game.Session.Crystals>=Price(stat);}
    void SetInstallPanelOpen(bool open)
    {
        // Every new opening starts at categories; folding changes only panel visibility.
        if(open&&!installOpen)BuildInstallCategories();
        installOpen=open;installPanel.gameObject.SetActive(open);Refresh();
    }
    void BuildInstallCategories()
    {
        ClearInstallButtons();
        InstallButton(0,"공격 포탑",()=>BuildInstallList(false));InstallButton(1,"지원 포탑",()=>BuildInstallList(true));
    }
    readonly List<Action> installRefresh = new List<Action>();
    void ClearInstallButtons(){foreach(var a in installRefresh)refresh.Remove(a);installRefresh.Clear();foreach(Transform child in installPanel){child.gameObject.SetActive(false);Destroy(child.gameObject);}}
    void InstallButton(int index,string label,Action action,Func<bool> available=null)
    {
        int before=refresh.Count;Button(label,installPanel,new Vector2(16+(index%3)*212,-16-(index/3)*100),new Vector2(204,96),()=>label,action,()=>game.Session.Phase==SessionPhase.Preparation&&(available==null||available()));
        installRefresh.Add(refresh[before]);
    }
    void BuildInstallList(bool support)
    {
        ClearInstallButtons();
        if(support)InstallTowerButton(0,TowerKind.Support);
        else{InstallTowerButton(0,TowerKind.Single);InstallTowerButton(1,TowerKind.Multi);}
        InstallButton(8,"분류로 돌아가기",BuildInstallCategories);Refresh();
    }
    void InstallTowerButton(int index,TowerKind kind)
    {
        int before=refresh.Count;
        var b=Button("Install_"+kind,installPanel,new Vector2(16+(index%3)*212,-16-(index/3)*100),new Vector2(204,96),()=> {
            string reason=game.Session.Crystals<game.Session.Data.Towers[kind].Cost?"\n수정 부족":kind==TowerKind.Support&&!game.Session.Towers.Any(t=>t.Kind!=TowerKind.Support)?"\n공격 포탑 필요":"";
            return TowerName(kind)+"\n"+game.Session.Data.Towers[kind].Cost+"수정"+reason;
        },()=>{game.ChooseTower(kind);Refresh();},()=>game.Session.Phase==SessionPhase.Preparation&&game.Session.Crystals>=game.Session.Data.Towers[kind].Cost&&(kind!=TowerKind.Support||game.Session.Towers.Any(t=>t.Kind!=TowerKind.Support)));
        b.GetComponentInChildren<UnityEngine.UI.Text>().fontSize=16;
        installRefresh.Add(refresh[before]);
    }
    public bool CancelInstallation()
    {
        if(!game.Installing.HasValue&&!installOpen)return false;
        game.CancelInstall();installOpen=false;installPanel.gameObject.SetActive(false);
        if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);
        game.Notify("포탑 설치 취소");return true;
    }
    void ShowDamageNumber(EnemyState enemy,int amount)
    {
        FloatingNumber("HitDamage",amount.ToString(),game.World(enemy.Position)+Vector3.up*.95f,new Color(1f,.94f,.68f),nextDamageLane++%3-1);
    }
    public void ShowGoldNumber(Vector3 position,int amount,Sprite icon)
    {
        var number=FloatingNumber("GoldNumber","+"+amount,position,new Color(1f,.76f,.22f),0);
        var text=number.Text;
        float textWidth=Mathf.Ceil(text.GetPreferredValues(text.text).x);
        var group=Box("GoldFeedback",worldFeedback,new Vector2(.5f,.5f),Vector2.zero,new Vector2(40+textWidth,52),false);
        text.rectTransform.SetParent(group,false);
        text.rectTransform.anchorMin=text.rectTransform.anchorMax=text.rectTransform.pivot=new Vector2(0,.5f);
        text.rectTransform.anchoredPosition=new Vector2(40,0);
        text.rectTransform.sizeDelta=new Vector2(textWidth,52);
        text.alignment=TextAlignmentOptions.Left;
        var symbol=Box("GoldGain",group,new Vector2(0,.5f),Vector2.zero,new Vector2(36,36),false).gameObject.AddComponent<UnityEngine.UI.Image>();
        symbol.sprite=icon;symbol.preserveAspect=true;symbol.raycastTarget=false;
        number.Root=group;number.Icon=symbol;number.Offset=new Vector2(0,24);
    }
    public void ShowCoreNumber(Vector3 position,int amount)=>FloatingNumber("CoreDamageNumber","-"+amount,position,new Color(1f,.36f,.32f),0);
    DamageNumber FloatingNumber(string name,string value,Vector3 position,Color color,int lane)
    {
        var rect=Box(name,worldFeedback,new Vector2(.5f,.5f),Vector2.zero,new Vector2(200,52),false);
        var text=rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font=damageFont;text.fontSharedMaterial=damageMaterial;text.fontSize=32;
        text.alignment=TextAlignmentOptions.Center;text.text=value;text.color=color;
        text.raycastTarget=false;text.textWrappingMode=TextWrappingModes.NoWrap;
        text.overflowMode=TextOverflowModes.Overflow;
        var number=new DamageNumber{Text=text,Root=rect,World=position,Started=Time.time,Lane=lane};
        damageNumbers.Add(number);return number;
    }
    void LateUpdate()
    {
        var camera=Camera.main;
        for(int i=damageNumbers.Count-1;i>=0;i--)
        {
            var number=damageNumbers[i];float age=Time.time-number.Started;
            if(age>=.85f){Destroy(number.Root.gameObject);damageNumbers.RemoveAt(i);continue;}
            if(camera==null)continue;
            var screen=camera.WorldToScreenPoint(number.World);
            number.Root.gameObject.SetActive(screen.z>0);
            Vector2 local;RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,screen,null,out local);
            number.Root.anchoredPosition=local+number.Offset+new Vector2(number.Lane*42,Mathf.Abs(number.Lane)*12+age*42);
            var color=number.Text.color;color.a=1-Mathf.Clamp01((age-.35f)/.5f);number.Text.color=color;
            if(number.Icon!=null){var iconColor=number.Icon.color;iconColor.a=color.a;number.Icon.color=iconColor;}
        }
    }
    public void ClearDamageNumbers()
    {
        foreach(var number in damageNumbers)if(number.Root!=null)Destroy(number.Root.gameObject);
        damageNumbers.Clear();nextDamageLane=0;
    }
    void Update(){if(game==null||!game.Ready||Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.2f;Refresh();}
    void Refresh()
    {
        if(game==null||!game.Ready||status==null)return;
        var s=game.Session;
        status.text="코어 "+s.CoreEnergy+" / "+s.CoreMaximum+"  |  "+(s.Diagnostic?"역할 검증":"라운드 "+s.Wave+" / "+s.LastWave)+"  |  골드 "+s.Gold+"  |  수정 "+s.Crystals+"  |  증강 "+s.Augments.Selected.Count;
        bool selecting=s.Phase==SessionPhase.Augmentation;
        if(augmentationPanel!=null){augmentationPanel.gameObject.SetActive(selecting);if(selecting){RefreshCards();augmentationTimer.text="선택 "+Mathf.CeilToInt(s.AugmentationRemaining)+"초 · 클릭 공개 / 공개된 카드 다시 클릭 선택\n시간 초과 시 최고 등급 자동 선택 · 동률이면 왼쪽 우선";}else choiceSignature=null;}
        if(bossStatus!=null){var boss=s.Enemies.FirstOrDefault(e=>e.Alive&&e.Kind==EnemyKind.Boss);bossStatus.text=boss==null?"":"보스 · "+boss.Health+" / "+boss.Spec.Health+(boss.Enraged?" · 격노: 이동속도 +30%":"");}
        center.text=s.Phase==SessionPhase.Preparation?s.Wave+"라운드 준비 "+Mathf.CeilToInt(s.PreparationRemaining)+"초":s.Phase==SessionPhase.Combat?s.Wave+"라운드 전투 · 살아 있는 적 "+s.Enemies.Count(e=>e.Alive):s.Phase==SessionPhase.Defeat?"코어 파괴 · 패배":s.Diagnostic?"역할 검증 완료":"1~"+s.LastWave+"라운드 검증 완료";
        feedback.text=game.Feedback;
        if(selecting)center.text="보스 라운드 완료 · 증강 선택";
        foreach(var action in refresh.ToArray())action();
        if(selecting){foreach(var button in canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true))if(!augmentationButtons.Contains(button))button.interactable=false;if(EventSystem.current!=null&&!augmentationButtons.Any(b=>b.gameObject==EventSystem.current.currentSelectedGameObject))EventSystem.current.SetSelectedGameObject(augmentationButtons[0].gameObject);}
        var selected=Selected();if(selected!=null&&lastTower!=selected.Id){towerFolded=false;lastTower=selected.Id;}if(selected==null)lastTower=-1;towerPanel.gameObject.SetActive(selected!=null&&!towerFolded);canvas.Find("TowerFold").gameObject.SetActive(selected!=null);
        if(selected!=null){var st=selected.Stats;towerInfo.text=TowerName(selected.Kind)+" #"+selected.Id+"\n피해 "+StatDisplay(st.Damage,"damage")+" · 초당 "+st.Rate.ToString("0.##")+"\n사거리 "+st.Range.ToString("0.##")+" · 투사체 "+st.ProjectileSpeed.ToString("0.##")+(selected.Kind==TowerKind.Support?"\n지원 +"+st.BuffDamage.ToString("0.#")+"%":"");}
        var totals=s.DamageTotals.OrderByDescending(x=>x.Value).ThenBy(x=>x.Key).ToArray();
        var signature=string.Join("|",totals.Select(x=>x.Key+":"+x.Value));
        if(signature==damageSignature)return;damageSignature=signature;
        foreach(Transform child in damageContent)Destroy(child.gameObject);
        int max=totals.Length==0?0:totals[0].Value;
        foreach(var pair in totals)
        {
            var row=Box(pair.Key,damageContent,new Vector2(0,1),Vector2.zero,new Vector2(288,60),false);
            row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight=60;
            CoreGuardianHudSkin.Icon(row,pair.Key=="Player"?"Upgrade":"Install",new Vector2(4,-2),new Vector2(25,25));
            Text("Value",row,new Vector2(34,-2),new Vector2(250,28),17).text=SourceName(pair.Key)+"  "+pair.Value;
            var bar=Box("Bar",row,new Vector2(0,1),new Vector2(4,-34),new Vector2(max==0?0:280f*pair.Value/max,16));bar.GetComponent<UnityEngine.UI.Image>().color=new Color(.22f,.78f,.8f);
        }
    }
    void OnDestroy(){if(game!=null){game.Changed-=Refresh;if(game.Session!=null)game.Session.DamageApplied-=ShowDamageNumber;}ClearDamageNumbers();if(font!=null)Destroy(font);}
}
