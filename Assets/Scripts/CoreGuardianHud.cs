using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

// Functional prototype UI following the existing planning layout; final HUD art is separate.
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
        bossStatus=Text("BossStatus",canvas,new Vector2(700,-124),new Vector2(660,42),24);
        bossStatus.color=new Color(1f,.6f,.45f);
        augmentationPanel=Box("AugmentationSelection",canvas,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1920,1080));
        augmentationPanel.anchorMin=Vector2.zero;augmentationPanel.anchorMax=Vector2.one;augmentationPanel.sizeDelta=Vector2.zero;
        var panel=Box("Cards",augmentationPanel,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1040,430));
        Text("Title",panel,new Vector2(30,-14),new Vector2(980,42),30).text="보스 격파 · 증강 1개 선택";
        augmentationTimer=Text("ChoiceTimer",panel,new Vector2(30,-60),new Vector2(980,54),21);
        for(int i=0;i<3;i++) {
            int index=i;
            var button=Button("AugmentationCard"+i,panel,new Vector2(30+i*330,-128),new Vector2(320,252),()=> {
                var choices=game.Session.AugmentationChoices;
                return index<choices.Count?choices[index].GradeName+"\n"+choices[index].Title+"\n\n"+choices[index].Description:"";
            },()=> {
                if(game.Session.ChooseAugmentation(index)){game.Notify(game.Session.LastMessage);Refresh();}
            },()=>game.Session.Phase==SessionPhase.Augmentation);
            button.GetComponentInChildren<UnityEngine.UI.Text>().fontSize=23;
            augmentationButtons.Add(button);
        }
        augmentationPanel.gameObject.SetActive(false);
    }
    RectTransform Box(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size,bool background=true)
    {
        var g=new GameObject(name,typeof(RectTransform));var r=g.GetComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=anchor;r.pivot=anchor;r.anchoredPosition=position;r.sizeDelta=size;
        if(background){var i=g.AddComponent<UnityEngine.UI.Image>();i.color=new Color(.055f,.07f,.09f,.94f);}return r;
    }
    UnityEngine.UI.Text Text(string name,Transform parent,Vector2 position,Vector2 size,int textSize=20)
    {
        var r=Box(name,parent,new Vector2(0,1),position,size,false);var t=r.gameObject.AddComponent<UnityEngine.UI.Text>();
        t.font=font;t.fontSize=textSize;t.color=Color.white;t.raycastTarget=false;t.alignment=TextAnchor.MiddleLeft;t.horizontalOverflow=HorizontalWrapMode.Wrap;return t;
    }
    UnityEngine.UI.Button Button(string name,Transform parent,Vector2 position,Vector2 size,Func<string> label,Action action,Func<bool> enabled=null)
    {
        var r=Box(name,parent,new Vector2(0,1),position,size);r.GetComponent<UnityEngine.UI.Image>().color=new Color(.16f,.22f,.27f,.98f);
        var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.onClick.AddListener(()=>action());
        var t=Text("Label",r,new Vector2(10,0),size-new Vector2(20,0),19);
        refresh.Add(()=>{t.text=label();b.interactable=enabled==null||enabled();t.color=b.interactable?Color.white:new Color(.55f,.58f,.6f);});return b;
    }
    void Build()
    {
        topDetails=Box("TopStatus",canvas,new Vector2(.5f,1),new Vector2(0,-12),new Vector2(920,64));
        status=Text("Status",topDetails,new Vector2(24,0),new Vector2(875,64),23);
        var controls=Box("BottomControls",canvas,new Vector2(.5f,0),new Vector2(0,12),new Vector2(1160,68));
        Button("Installation",controls,new Vector2(12,-10),new Vector2(200,48),()=>"타워 설치",()=>SetInstallPanelOpen(!installOpen),()=>game.Session.Phase==SessionPhase.Preparation);
        Button("Start",controls,new Vector2(462,-10),new Vector2(236,48),()=>game.Session.Phase==SessionPhase.Preparation?"라운드 시작":game.Session.Phase==SessionPhase.Combat?"전투 진행":game.Session.Phase==SessionPhase.Augmentation?"증강 선택 중":game.Session.Phase==SessionPhase.Defeat?"패배":"검증 완료",()=>game.StartCombat(),()=>game.Session.Phase==SessionPhase.Preparation);
        Button("PlayerGrowth",controls,new Vector2(946,-10),new Vector2(202,48),()=>"플레이어 강화",()=>{playerOpen=!playerOpen;playerPanel.gameObject.SetActive(playerOpen);});
        center=Text("Preparation",canvas,new Vector2(800,-82),new Vector2(540,40),22);
        feedback=Text("Feedback",canvas,new Vector2(430,-898),new Vector2(1060,36),20);
        var debug=Box("PrototypeActions",canvas,new Vector2(0,1),new Vector2(16,-14),new Vector2(262,66));
        Button("RoleDiagnostic",debug,new Vector2(8,-8),new Vector2(246,48),()=>"3종 역할 검증 / 재설정",()=>game.Diagnostic());
        Button("Reload",canvas,new Vector2(1630,-16),new Vector2(260,48),()=>"처음부터 다시 시작",()=>game.Restart());
        Button("TestSpeed",canvas,new Vector2(1630,-70),new Vector2(260,30),()=>"테스트 배속 ×"+game.TestSpeed,()=>game.CycleTestSpeed());

        installPanel=Box("TowerInstallPanel",canvas,new Vector2(0,0),new Vector2(380,88),new Vector2(492,246));
        BuildInstallCategories();installPanel.gameObject.SetActive(false);
        Button("CancelPlacement",canvas,new Vector2(18,-1000),new Vector2(270,48),()=>"설치 취소 · Esc",()=>CancelInstallation(),()=>game.Installing.HasValue||installOpen);
        playerPanel=Box("PlayerGrowthPanel",canvas,new Vector2(1,0),new Vector2(-380,88),new Vector2(330,250));
        Text("Title",playerPanel,new Vector2(16,-4),new Vector2(300,40),22).text="플레이어 능력치 강화";
        var stats=new[]{"damage","rate","projectileSpeed","range"};var names=new[]{"공격력","공격속도","투사체 속도","사거리"};
        for(int i=0;i<stats.Length;i++)
        {
            string stat=stats[i],label=names[i];
            Button(stat,playerPanel,new Vector2(16,-48-i*47),new Vector2(298,42),()=>label+"  "+game.Session.PlayerUpgradePrice(stat)+"골드",()=>game.UpgradePlayer(stat),()=>game.Session.Gold>=game.Session.PlayerUpgradePrice(stat)&&(game.Session.Phase==SessionPhase.Preparation||game.Session.Phase==SessionPhase.Combat));
        }
        playerPanel.gameObject.SetActive(false);
        towerPanel=Box("TowerInformation",canvas,new Vector2(0,1),new Vector2(16,-120),new Vector2(330,400));
        towerInfo=Text("Stats",towerPanel,new Vector2(16,-8),new Vector2(298,120),20);
        foreach(var stat in new[]{"damage","rate","projectileSpeed","range","buffDamage"})
        {
            int index=Array.IndexOf(new[]{"damage","rate","projectileSpeed","range","buffDamage"},stat);string captured=stat;
            var button=Button("Tower_"+stat,towerPanel,new Vector2(16,-134-index*42),new Vector2(298,36),()=>StatName(captured)+"  "+Price(captured)+"수정",()=>game.UpgradeTower(captured),()=>TowerCanUpgrade(captured));
            refresh.Add(()=>
            {
                var t=Selected();bool relevant=t!=null&&Relevant(t,captured);button.gameObject.SetActive(relevant);
                if(relevant)button.GetComponent<RectTransform>().anchoredPosition=new Vector2(16,-134-(t.Kind==TowerKind.Support?(captured=="range"?0:1):index)*42);
            });
        }
        Button("Destroy",towerPanel,new Vector2(16,-352),new Vector2(298,38),()=>"파괴 · 실제 지출 50% 환급",()=>game.DestroySelected());
        towerPanel.gameObject.SetActive(false);
        damagePanel=Box("DamagePanel",canvas,new Vector2(1,1),new Vector2(-16,-104),new Vector2(280,490));
        Text("Title",damagePanel,new Vector2(14,-2),new Vector2(252,40),21).text="라운드 누적 피해";
        var scroll=Box("Scroll",damagePanel,new Vector2(0,1),new Vector2(10,-48),new Vector2(260,430),false);
        var viewport=Box("Viewport",scroll,new Vector2(0,1),Vector2.zero,new Vector2(260,430));
        viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic=false;
        damageContent=Box("Content",viewport,new Vector2(0,1),Vector2.zero,new Vector2(260,0),false);
        var layout=damageContent.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();layout.spacing=8;layout.childControlHeight=true;layout.childForceExpandHeight=false;layout.childControlWidth=true;
        damageContent.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        var sr=scroll.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();sr.viewport=viewport;sr.content=damageContent;sr.horizontal=false;sr.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;
        Button("DamageFold",canvas,new Vector2(1848,-610),new Vector2(56,38),()=>damagePanel.gameObject.activeSelf?"▶":"◀",()=>damagePanel.gameObject.SetActive(!damagePanel.gameObject.activeSelf));
        Button("InstallFold",canvas,new Vector2(380,-938),new Vector2(50,34),()=>installOpen?"▼":"▲",()=>SetInstallPanelOpen(!installOpen));
        Button("PlayerFold",canvas,new Vector2(1490,-938),new Vector2(50,34),()=>playerOpen?"▼":"▲",()=>{playerOpen=!playerOpen;playerPanel.gameObject.SetActive(playerOpen);});
        // Top compact display retains all four critical values when collapsed.
        Button("TopFold",canvas,new Vector2(1425,-20),new Vector2(52,36),()=>topDetails.sizeDelta.y>40?"▲":"▼",()=>{topDetails.sizeDelta=new Vector2(920,topDetails.sizeDelta.y>40?40:64);status.rectTransform.sizeDelta=topDetails.sizeDelta-new Vector2(45,0);});
        Refresh();
    }
    string StatName(string stat){switch(stat){case "damage":return "공격력";case "rate":return "공격속도";case "range":return "사거리";case "buffDamage":return "지원 공격력 효과";default:return "투사체 속도";}}
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
        InstallButton(0,"공격 포탑",()=>BuildInstallList(false));InstallButton(1,"지원 포탑",()=>BuildInstallList(true));InstallButton(8,"접기",()=>SetInstallPanelOpen(false));
    }
    readonly List<Action> installRefresh = new List<Action>();
    void ClearInstallButtons(){foreach(var a in installRefresh)refresh.Remove(a);installRefresh.Clear();foreach(Transform child in installPanel){child.gameObject.SetActive(false);Destroy(child.gameObject);}}
    void InstallButton(int index,string label,Action action,Func<bool> available=null)
    {
        int before=refresh.Count;Button(label,installPanel,new Vector2(12+(index%3)*160,-12-(index/3)*76),new Vector2(148,64),()=>label,action,()=>game.Session.Phase==SessionPhase.Preparation&&(available==null||available()));
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
        var b=Button("Install_"+kind,installPanel,new Vector2(12+(index%3)*160,-12-(index/3)*76),new Vector2(148,64),()=> {
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
        if(augmentationPanel!=null){augmentationPanel.gameObject.SetActive(selecting);if(selecting){augmentationTimer.text="선택 "+Mathf.CeilToInt(s.AugmentationRemaining)+"초 · 준비 시간은 선택 후 시작\n시간 초과 시 최고 등급 자동 선택 · 동률이면 왼쪽 우선";for(int i=0;i<3;i++){var grade=s.AugmentationChoices[i].Grade;augmentationButtons[i].GetComponent<UnityEngine.UI.Image>().color=grade==AugmentationGrade.Platinum?new Color(.18f,.32f,.39f):grade==AugmentationGrade.Gold?new Color(.38f,.27f,.10f):new Color(.21f,.24f,.29f);}}}
        if(bossStatus!=null){var boss=s.Enemies.FirstOrDefault(e=>e.Alive&&e.Kind==EnemyKind.Boss);bossStatus.text=boss==null?"":"보스 · "+boss.Health+" / "+boss.Spec.Health+(boss.Enraged?" · 격노: 이동속도 +30%":"");}
        center.text=s.Phase==SessionPhase.Preparation?s.Wave+"라운드 준비 "+Mathf.CeilToInt(s.PreparationRemaining)+"초":s.Phase==SessionPhase.Combat?s.Wave+"라운드 전투 · 살아 있는 적 "+s.Enemies.Count(e=>e.Alive):s.Phase==SessionPhase.Defeat?"코어 파괴 · 패배":s.Diagnostic?"역할 검증 완료":"1~"+s.LastWave+"라운드 검증 완료";
        feedback.text=game.Feedback;
        if(selecting)center.text="보스 라운드 완료 · 증강 선택";
        foreach(var action in refresh.ToArray())action();
        if(selecting){foreach(var button in canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true))if(!augmentationButtons.Contains(button))button.interactable=false;if(EventSystem.current!=null&&!augmentationButtons.Any(b=>b.gameObject==EventSystem.current.currentSelectedGameObject))EventSystem.current.SetSelectedGameObject(augmentationButtons[0].gameObject);}
        var selected=Selected();towerPanel.gameObject.SetActive(selected!=null);
        if(selected!=null){var st=selected.Stats;towerInfo.text=TowerName(selected.Kind)+" #"+selected.Id+"\n피해 "+st.Damage.ToString("0.##")+" · 초당 "+st.Rate.ToString("0.##")+"\n사거리 "+st.Range.ToString("0.##")+" · 투사체 "+st.ProjectileSpeed.ToString("0.##")+(selected.Kind==TowerKind.Support?"\n지원 +"+st.BuffDamage.ToString("0.#")+"%":"");}
        var totals=s.DamageTotals.OrderByDescending(x=>x.Value).ThenBy(x=>x.Key).ToArray();
        var signature=string.Join("|",totals.Select(x=>x.Key+":"+x.Value));
        if(signature==damageSignature)return;damageSignature=signature;
        foreach(Transform child in damageContent)Destroy(child.gameObject);
        int max=totals.Length==0?0:totals[0].Value;
        foreach(var pair in totals)
        {
            var row=Box(pair.Key,damageContent,new Vector2(0,1),Vector2.zero,new Vector2(260,60),false);
            row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight=60;
            Text("Value",row,new Vector2(4,-2),new Vector2(250,28),18).text=SourceName(pair.Key)+"  "+pair.Value;
            var bar=Box("Bar",row,new Vector2(0,1),new Vector2(4,-34),new Vector2(max==0?0:248f*pair.Value/max,16));bar.GetComponent<UnityEngine.UI.Image>().color=new Color(.22f,.78f,.8f);
        }
    }
    void OnDestroy(){if(game!=null){game.Changed-=Refresh;if(game.Session!=null)game.Session.DamageApplied-=ShowDamageNumber;}ClearDamageNumbers();if(font!=null)Destroy(font);}
}
