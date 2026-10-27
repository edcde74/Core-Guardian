using System;
using System.Linq;
using CoreGuardian;
using UnityEngine;

// Display policy only: prices, progression, combat and placement remain owned by Session/Prototype.
public sealed partial class CoreGuardianHud
{
    enum InspectorSubject { None, Player, Tower }
    InspectorSubject inspectorSubject;
    bool inspectorOpen, purchaseOpen, developerOpen, stageFolded;
    SessionPhase? displayedPhase;
    RectTransform inspectorFold;
    UnityEngine.UI.Text playerInfo, playerTitle, towerTitle;
    RectTransform stagePlateView,stageFoldView;
    Transform stageFoldArt;
    UnityEngine.UI.Image towerIdentityIcon;
    GameObject cancelPlacementView;
    readonly System.Collections.Generic.List<GameObject> developerViews=new System.Collections.Generic.List<GameObject>();
    bool CanBuyPlayer=>game.Session.Phase==SessionPhase.Preparation||game.Session.Phase==SessionPhase.Combat;
    public Transform InspectionTarget
    {
        get
        {
            if(game==null||!game.Ready||!inspectorOpen||installOpen||game.Installing.HasValue||game.Session.Phase==SessionPhase.Augmentation)return null;
            if(inspectorSubject==InspectorSubject.Player)return game.player.transform;
            return inspectorSubject==InspectorSubject.Tower?game.TowerView(game.SelectedTower):null;
        }
    }
    void ReleaseInspection()
    {
        inspectorOpen=false;purchaseOpen=false;inspectorSubject=InspectorSubject.None;lastTower=-1;
        game.ClearInspectionSelection();
    }

    RectTransform SmallHandle(string name,Transform parent,Action action,bool horizontal)
    {
        var r=Box(name,parent,new Vector2(.5f,0),Vector2.zero,horizontal?new Vector2(80,28):new Vector2(28,80),false);
        var hit=r.gameObject.AddComponent<UnityEngine.UI.Image>();hit.color=Color.clear;
        var art=Box("HandleArt",r,new Vector2(.5f,.5f),Vector2.zero,new Vector2(22,80),false);
        var image=art.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=DamageSprite("DamageHandle");image.preserveAspect=true;image.raycastTarget=false;
        if(horizontal)art.localRotation=Quaternion.Euler(0,0,90);
        var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=image;
        r.gameObject.AddComponent<CoreGuardianHudHandleReaction>().Configure(image,horizontal);
        // Augmentation disables ordinary buttons, including inactive handles. Restore on the
        // next refresh before modal gating is applied again, just like the normal Button helper.
        refresh.Add(()=>b.interactable=true);
        b.onClick.AddListener(()=>{if(!b.interactable)return;game.GetComponent<CoreGuardianCombatAudio>()?.PlayCue("ui_click");action();Refresh();});
        return r;
    }
    void SetTopCompact(bool compact)
    {
        topDetails.sizeDelta=new Vector2(920,compact?52:72);
        foreach(Transform child in topDetails){var r=child as RectTransform;if(r==null||child.name=="Status")continue;bool icon=child.name.StartsWith("Symbol");r.anchoredPosition=new Vector2(r.anchoredPosition.x,compact?-12:(icon?-21:-16));if(!icon)r.sizeDelta=new Vector2(r.sizeDelta.x,compact?28:40);}
        if(stagePlateView!=null)SetHudPosition(stagePlateView,new Vector2(0,compact?-74:-94));
    }
    void RefreshStageFold()
    {
        var phase=game.Session.Phase;
        var stage=stagePlateView;var handle=stageFoldView;
        // The user's collapse choice hides combat details, while preparation/augmentation timers
        // retain their existing phase-specific presentation and the resource row stays visible.
        bool hidden=phase==SessionPhase.Combat&&stageFolded;
        SetHudActive(stage.gameObject,!hidden);
        SetHudActive(handle.gameObject,phase==SessionPhase.Combat);
        SetHudPosition(handle,new Vector2(0,hidden?topDetails.anchoredPosition.y-topDetails.sizeDelta.y:stage.anchoredPosition.y-stage.sizeDelta.y));
        var scale=new Vector3(hidden?-1:1,1,1);if(!stageFoldArt.localScale.Equals(scale))stageFoldArt.localScale=scale;
    }
    void BuildDeveloperMenu()
    {
        Button("DeveloperMenu",canvas,new Vector2(16,-14),new Vector2(150,48),()=>"개발 도구",()=>{developerOpen=!developerOpen;game.GetComponent<CoreGuardianCombatAudio>()?.PlayCue(developerOpen?"ui_open":"ui_close");});
        // Keep legacy object names for diagnostic scripts; only the menu exposes them now.
        var debug=canvas.Find("PrototypeActions") as RectTransform;debug.anchoredPosition=new Vector2(16,-72);
        var reload=canvas.Find("Reload") as RectTransform;reload.anchorMin=reload.anchorMax=reload.pivot=new Vector2(0,1);reload.anchoredPosition=new Vector2(32,-168);
        var speed=canvas.Find("TestSpeed") as RectTransform;speed.anchorMin=speed.anchorMax=speed.pivot=new Vector2(0,1);speed.anchoredPosition=new Vector2(32,-232);
        BuildSoundControls();
        BuildCheatPanel();
        developerViews.Add(debug.gameObject);developerViews.Add(reload.gameObject);developerViews.Add(speed.gameObject);
        var sound=canvas.Find("SoundControls");if(sound!=null)developerViews.Add(sound.gameObject);
        SetTopCompact(true);
    }
    void BuildInspectors()
    {
        playerPanel=Box("PlayerGrowthPanel",canvas,new Vector2(.5f,0),new Vector2(0,112),new Vector2(900,220));
        towerPanel=Box("TowerInformation",canvas,new Vector2(.5f,0),new Vector2(0,112),new Vector2(900,280));
        CoreGuardianHudSkin.Icon(playerPanel,"PlayerIdentity",new Vector2(16,-16),new Vector2(28,28));
        playerTitle=Text("Title",playerPanel,new Vector2(54,-12),new Vector2(820,32),22);
        towerIdentityIcon=CoreGuardianHudSkin.Icon(towerPanel,"TowerIdentity",new Vector2(16,-16),new Vector2(28,28));
        towerTitle=Text("Title",towerPanel,new Vector2(54,-12),new Vector2(820,32),22);
        foreach(var panel in new[]{playerPanel,towerPanel})
        {
            bool isPlayer=panel==playerPanel;
            var specTab=Button(isPlayer?"PlayerSpecTab":"TowerSpecTab",panel,new Vector2(16,-56),new Vector2(220,56),()=>"현재 능력치",()=>{purchaseOpen=false;Refresh();});
            var buyTab=Button(isPlayer?"PlayerBuyTab":"TowerBuyTab",panel,new Vector2(16,-120),new Vector2(220,56),()=>isPlayer?"능력치 강화":"종류 전체 강화",()=>{purchaseOpen=true;Refresh();},()=>isPlayer?CanBuyPlayer:game.Session.Phase==SessionPhase.Preparation);
            StyleInspectorTab(specTab);StyleInspectorTab(buyTab);
        }
        playerAbilities=BuildAbilityPlate(playerPanel);towerAbilities=BuildAbilityPlate(towerPanel);
        playerInfo=Text("Stats",playerPanel,new Vector2(280,-186),new Vector2(580,24),16);playerInfo.alignment=TextAnchor.UpperLeft;
        towerInfo=Text("Stats",towerPanel,new Vector2(280,-186),new Vector2(580,36),14);towerInfo.alignment=TextAnchor.UpperLeft;
        var stats=new[]{"damage","rate","range","projectileSpeed"};
        for(int i=0;i<stats.Length;i++)
        {
            string stat=stats[i];
            var b=Button(stat,playerPanel,new Vector2(264+i%2*306,-52-i/2*64),new Vector2(306,64),()=>UpgradeLabel(game.Session.Player,"Player",stat,game.Session.PlayerUpgradePrice(stat),"골드"),()=>PurchasePlayer(stat),()=>CanBuyPlayer&&game.Session.Gold>=game.Session.PlayerUpgradePrice(stat));
            StyleAbilityPurchase(b);
            refresh.Add(()=>b.gameObject.SetActive(purchaseOpen));
        }
        foreach(var stat in stats.Concat(new[]{"buffDamage","slowPercent","burnDuration","pierceFalloff"}))
        {
            string captured=stat;
            var b=Button("Tower_"+stat,towerPanel,Vector2.zero,new Vector2(306,64),()=>Selected()==null?"":UpgradeLabel(Selected().Stats,Selected().Kind.ToString(),captured,Price(captured),"수정"),()=>PurchaseTower(captured),()=>TowerCanUpgrade(captured));
            StyleAbilityPurchase(b);
            refresh.Add(()=>{var t=Selected();bool relevant=t!=null&&Relevant(t,captured);b.gameObject.SetActive(purchaseOpen&&relevant);if(relevant){int row=UpgradeRows(t.Kind).IndexOf(captured);((RectTransform)b.transform).anchoredPosition=new Vector2(264+row%2*306,-52-row/2*64);}});
        }
        var destroy=Button("Destroy",towerPanel,new Vector2(264,-220),new Vector2(612,44),()=>"철거 · 설치비 "+game.Session.RefundPercent+"% 반환 · 공유 강화 유지",()=>game.DestroySelected(),()=>CanBuyPlayer&&Selected()!=null);
        var destroyLabel=(RectTransform)destroy.transform.Find("Label");
        SetHudSize(destroyLabel,new Vector2(542,28));
        inspectorFold=SmallHandle("InspectorFold",canvas,()=>TryFoldDetails(),true);
        playerPanel.gameObject.SetActive(false);towerPanel.gameObject.SetActive(false);
    }
    void StyleInspectorTab(UnityEngine.UI.Button button)
    {
        var tab=(RectTransform)button.transform;
        button.GetComponent<CoreGuardianHudReaction>().SetLabelLayout(new Vector2(54,-16),tab.sizeDelta-new Vector2(70,32));
        var icon=tab.GetComponentsInChildren<UnityEngine.UI.Image>().First(i=>i.transform!=tab).rectTransform;
        icon.anchoredPosition=new Vector2(16,-(tab.sizeDelta.y-icon.sizeDelta.y)/2);
    }
    void OpenPlayer(bool buy)
    {
        SetPlayerInspection(buy,true);
    }
    public void OpenPlayerSpecs()=>SetPlayerInspection(false,false);
    void SetPlayerInspection(bool buy,bool toggle)
    {
        if(game.Installing.HasValue||game.Session.Phase==SessionPhase.Augmentation||buy&&!CanBuyPlayer)return;
        bool same=toggle&&inspectorSubject==InspectorSubject.Player&&inspectorOpen&&purchaseOpen==buy;
        if(same||!inspectorOpen||inspectorSubject!=InspectorSubject.Player)game.GetComponent<CoreGuardianCombatAudio>()?.PlayCue(same?"ui_close":"ui_open");
        ReleaseInspection();
        installOpen=false;installPanel.gameObject.SetActive(false);
        if(!same){inspectorSubject=InspectorSubject.Player;inspectorOpen=true;purchaseOpen=buy;}
        Refresh();
    }
    public void OpenSelectedTowerSpecs()
    {
        if(Selected()==null||game.Installing.HasValue||game.Session.Phase==SessionPhase.Augmentation)return;
        if(!inspectorOpen||inspectorSubject!=InspectorSubject.Tower||lastTower!=game.SelectedTower)game.GetComponent<CoreGuardianCombatAudio>()?.PlayCue("ui_open");
        installOpen=false;installPanel.gameObject.SetActive(false);lastTower=game.SelectedTower;
        inspectorSubject=InspectorSubject.Tower;inspectorOpen=true;purchaseOpen=false;Refresh();
    }
    // One floor click folds the panel AND releases inspection. Never places or spends currency.
    public bool TryFoldDetails()
    {
        if(game.Installing.HasValue||game.Session.Phase==SessionPhase.Augmentation)return false;
        if(!installOpen&&!inspectorOpen)return false;
        game.GetComponent<CoreGuardianCombatAudio>()?.PlayCue("ui_close");
        installOpen=false;installPanel.gameObject.SetActive(false);ReleaseInspection();
        if(UnityEngine.EventSystems.EventSystem.current!=null)UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        Refresh();return true;
    }
    void SetCardsVisible()
    {
        var group=cardsRoot.GetComponent<CanvasGroup>();if(group==null)group=cardsRoot.gameObject.AddComponent<CanvasGroup>();
        group.alpha=cardsFolded?0:1;group.interactable=!cardsFolded;group.blocksRaycasts=!cardsFolded;
        if(augmentationPanel!=null&&augmentationPanel.Find("AugmentFold")!=null)augmentationPanel.Find("AugmentFold/HandleArt").localScale=new Vector3(cardsFolded?-1:1,1,1);
    }
    void RefreshInspectors()
    {
        var s=game.Session;
        if(displayedPhase!=s.Phase)
        {
            if(displayedPhase.HasValue){ReleaseInspection();if(s.Phase==SessionPhase.Augmentation)developerOpen=false;} // 개발 도구는 증강 화면에서만 닫음(테스트 중 유지)
            displayedPhase=s.Phase;
            if(s.Phase==SessionPhase.Combat)SetTopCompact(true);
        }
        var selected=Selected();
        if(selected==null){lastTower=-1;if(inspectorSubject==InspectorSubject.Tower){inspectorSubject=InspectorSubject.None;inspectorOpen=false;}}
        else if(lastTower!=selected.Id){lastTower=selected.Id;inspectorSubject=InspectorSubject.Tower;inspectorOpen=false;purchaseOpen=false;}
        playerOpen=inspectorOpen&&inspectorSubject==InspectorSubject.Player&&!installOpen;
        bool towerOpen=inspectorOpen&&inspectorSubject==InspectorSubject.Tower&&selected!=null&&!installOpen;
        towerFolded=!towerOpen;
        SetHudActive(playerPanel.gameObject,playerOpen);SetHudActive(towerPanel.gameObject,towerOpen);
        SetHudActive(playerInfo.gameObject,true);SetHudActive(towerInfo.gameObject,true);
        var st=s.Player;
        RefreshAbilityPlate(playerAbilities,st,false);
        SetHudText(playerTitle,purchaseOpen?"플레이어 능력치 강화":"플레이어 능력치");
        SetHudText(playerInfo,"범위 폭발(Space) "+(s.AreaBlast==null?"없음":s.Phase!=SessionPhase.Combat?"전투 중 사용":s.AreaBlastReady?"준비":Mathf.CeilToInt(s.AreaBlastCooldown)+"초")+" · 증강 "+s.Augments.Selected.Count+"장");
        if(selected!=null)
        {
            st=selected.Stats;SetHudText(towerTitle,TowerName(selected.Kind)+(selected.Stars>1?" ★"+selected.Stars:"")+(purchaseOpen?" · 종류 전체 강화":" #"+selected.Id+" · 능력치"));
            var identity=CoreGuardianHudSkin.Sprite(CoreGuardianHudSkin.Symbol("Install_"+selected.Kind));if(towerIdentityIcon.sprite!=identity)towerIdentityIcon.sprite=identity;
            RefreshAbilityPlate(towerAbilities,st,selected.Kind==TowerKind.Support);
            float support=s.Towers.Where(t=>t.Kind==TowerKind.Support&&Vector2.Distance(t.Position,selected.Position)<=t.Stats.Range).Sum(t=>t.Stats.BuffDamage);
            float buff=support+s.Augments.TowerBuffPercent;
            SetHudPosition(towerInfo.rectTransform,new Vector2(280,selected.Kind==TowerKind.Support?-138:-186));
            SetHudText(towerInfo,purchaseOpen?"같은 종류의 모든 포탑과 이후 설치분에 적용됩니다.\n철거해도 공유 강화 단계는 유지됩니다.":selected.Kind==TowerKind.Support?"범위 안 공격 포탑의 공격력을 높입니다.\n직접 공격하지 않습니다.":
                "지원 포탑 +"+support.ToString("0.##")+"% · 증강 +"+s.Augments.TowerBuffPercent+"%\n버프 적용 공격력 "+StatDisplay(st.Damage*(1+buff/100f),"damage")+" (적 방어력 적용 전)");
        }
        bool details=playerOpen||towerOpen||installOpen;
        SetHudActive(inspectorFold.gameObject,details&&s.Phase!=SessionPhase.Augmentation);
        SetHudPosition(inspectorFold,new Vector2(0,112+(installOpen?installPanel.sizeDelta.y:playerOpen?playerPanel.sizeDelta.y:towerPanel.sizeDelta.y)));
        foreach(var node in developerViews)SetHudActive(node,developerOpen);
        if(cancelPlacementView!=null)SetHudActive(cancelPlacementView,game.Installing.HasValue);
    }
}
