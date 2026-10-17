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
    RectTransform inspectorFold, inspectorSummary;
    UnityEngine.UI.Text playerInfo, playerTitle, towerTitle, summaryLabel;
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
        b.onClick.AddListener(()=>{action();Refresh();});
        return r;
    }
    void SetTopCompact(bool compact)
    {
        topDetails.sizeDelta=new Vector2(920,compact?52:72);
        foreach(Transform child in topDetails){var r=child as RectTransform;if(r==null||child.name=="Status")continue;bool icon=child.name.StartsWith("Symbol");r.anchoredPosition=new Vector2(r.anchoredPosition.x,compact?-12:(icon?-21:-16));if(!icon)r.sizeDelta=new Vector2(r.sizeDelta.x,compact?28:40);}
        var stage=canvas.Find("StagePlate") as RectTransform;if(stage!=null)stage.anchoredPosition=new Vector2(0,compact?-74:-94);
    }
    void RefreshStageFold()
    {
        var phase=game.Session.Phase;
        var stage=(RectTransform)canvas.Find("StagePlate");var handle=(RectTransform)canvas.Find("TopFold");
        // The user's collapse choice hides combat details, while preparation/augmentation timers
        // retain their existing phase-specific presentation and the resource row stays visible.
        bool hidden=phase==SessionPhase.Combat&&stageFolded;
        stage.gameObject.SetActive(!hidden);
        handle.gameObject.SetActive(phase==SessionPhase.Combat);
        handle.anchoredPosition=new Vector2(0,hidden?topDetails.anchoredPosition.y-topDetails.sizeDelta.y:stage.anchoredPosition.y-stage.sizeDelta.y);
        handle.Find("HandleArt").localScale=new Vector3(hidden?-1:1,1,1);
    }
    void BuildDeveloperMenu()
    {
        Button("DeveloperMenu",canvas,new Vector2(16,-14),new Vector2(150,48),()=>"개발 도구",()=>developerOpen=!developerOpen);
        // Keep legacy object names for diagnostic scripts; only the menu exposes them now.
        var debug=canvas.Find("PrototypeActions") as RectTransform;debug.anchoredPosition=new Vector2(16,-72);
        var reload=canvas.Find("Reload") as RectTransform;reload.anchorMin=reload.anchorMax=reload.pivot=new Vector2(0,1);reload.anchoredPosition=new Vector2(32,-168);
        var speed=canvas.Find("TestSpeed") as RectTransform;speed.anchorMin=speed.anchorMax=speed.pivot=new Vector2(0,1);speed.anchoredPosition=new Vector2(32,-232);
        SetTopCompact(true);
    }
    void BuildInspectors()
    {
        playerPanel=Box("PlayerGrowthPanel",canvas,new Vector2(.5f,0),new Vector2(0,112),new Vector2(900,220));
        towerPanel=Box("TowerInformation",canvas,new Vector2(.5f,0),new Vector2(0,112),new Vector2(900,270));
        CoreGuardianHudSkin.Icon(playerPanel,"PlayerIdentity",new Vector2(16,-16),new Vector2(28,28));
        playerTitle=Text("Title",playerPanel,new Vector2(54,-12),new Vector2(820,32),22);
        CoreGuardianHudSkin.Icon(towerPanel,"TowerIdentity",new Vector2(16,-16),new Vector2(28,28));
        towerTitle=Text("Title",towerPanel,new Vector2(54,-12),new Vector2(820,32),22);
        foreach(var panel in new[]{playerPanel,towerPanel})
        {
            bool isPlayer=panel==playerPanel;
            Button(isPlayer?"PlayerSpecTab":"TowerSpecTab",panel,new Vector2(16,-56),new Vector2(220,56),()=>"현재 능력치",()=>{purchaseOpen=false;Refresh();});
            Button(isPlayer?"PlayerBuyTab":"TowerBuyTab",panel,new Vector2(16,-120),new Vector2(220,56),()=>"능력치 강화",()=>{purchaseOpen=true;Refresh();},()=>isPlayer?CanBuyPlayer:game.Session.Phase==SessionPhase.Preparation);
        }
        playerAbilities=BuildAbilityPlate(playerPanel);towerAbilities=BuildAbilityPlate(towerPanel);
        playerInfo=Text("Stats",playerPanel,new Vector2(280,-186),new Vector2(580,24),16);playerInfo.alignment=TextAnchor.UpperLeft;
        towerInfo=Text("Stats",towerPanel,new Vector2(280,-186),new Vector2(580,36),14);towerInfo.alignment=TextAnchor.UpperLeft;
        var stats=new[]{"damage","rate","range","projectileSpeed"};
        for(int i=0;i<stats.Length;i++)
        {
            string stat=stats[i];
            var b=Button(stat,playerPanel,new Vector2(16+i%2*442,-108-i/2*88),new Vector2(426,80),()=>UpgradeLabel(game.Session.Player,"Player",stat,game.Session.PlayerUpgradePrice(stat),"골드"),()=>PurchasePlayer(stat),()=>CanBuyPlayer&&game.Session.Gold>=game.Session.PlayerUpgradePrice(stat));
            refresh.Add(()=>b.gameObject.SetActive(purchaseOpen));
        }
        foreach(var stat in stats.Concat(new[]{"buffDamage"}))
        {
            string captured=stat;int index=Array.IndexOf(stats,captured);
            var b=Button("Tower_"+stat,towerPanel,Vector2.zero,new Vector2(426,80),()=>Selected()==null?"":UpgradeLabel(Selected().Stats,Selected().Kind.ToString(),captured,Price(captured),"수정"),()=>PurchaseTower(captured),()=>TowerCanUpgrade(captured));
            refresh.Add(()=>{var t=Selected();bool relevant=t!=null&&Relevant(t,captured);b.gameObject.SetActive(purchaseOpen&&relevant);if(relevant){int row=t.Kind==TowerKind.Support?(captured=="range"?0:1):index;((RectTransform)b.transform).anchoredPosition=new Vector2(16+row%2*442,-108-row/2*88);}});
        }
        Button("Destroy",towerPanel,new Vector2(264,-188),new Vector2(612,44),()=>"포탑 철거 · 사용한 수정 50% 반환",()=>game.DestroySelected(),()=>CanBuyPlayer&&Selected()!=null);
        inspectorFold=SmallHandle("InspectorFold",canvas,()=>TryFoldDetails(),true);
        inspectorSummary=Box("InspectorSummary",canvas,new Vector2(.5f,0),new Vector2(0,112),new Vector2(340,42));
        summaryLabel=Text("Target",inspectorSummary,new Vector2(12,-6),new Vector2(250,30),18);
        var reopen=SmallHandle("InspectorReopen",inspectorSummary,()=>{if(inspectorSubject==InspectorSubject.None&&Selected()!=null)inspectorSubject=InspectorSubject.Tower;inspectorOpen=inspectorSubject!=InspectorSubject.None;purchaseOpen=false;},true);
        reopen.anchorMin=reopen.anchorMax=reopen.pivot=new Vector2(1,.5f);reopen.anchoredPosition=new Vector2(-4,0);reopen.sizeDelta=new Vector2(64,28);
        playerPanel.gameObject.SetActive(false);towerPanel.gameObject.SetActive(false);
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
        ReleaseInspection();
        installOpen=false;installPanel.gameObject.SetActive(false);
        if(!same){inspectorSubject=InspectorSubject.Player;inspectorOpen=true;purchaseOpen=buy;}
        Refresh();
    }
    public void OpenSelectedTowerSpecs()
    {
        if(Selected()==null||game.Installing.HasValue||game.Session.Phase==SessionPhase.Augmentation)return;
        installOpen=false;installPanel.gameObject.SetActive(false);lastTower=game.SelectedTower;
        inspectorSubject=InspectorSubject.Tower;inspectorOpen=true;purchaseOpen=false;Refresh();
    }
    // One floor click folds the panel AND releases inspection. Never places or spends currency.
    public bool TryFoldDetails()
    {
        if(game.Installing.HasValue||game.Session.Phase==SessionPhase.Augmentation)return false;
        if(!installOpen&&!inspectorOpen)return false;
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
            if(displayedPhase.HasValue){ReleaseInspection();developerOpen=false;}
            displayedPhase=s.Phase;
            if(s.Phase==SessionPhase.Combat)SetTopCompact(true);
        }
        var selected=Selected();
        if(selected==null){lastTower=-1;if(inspectorSubject==InspectorSubject.Tower){inspectorSubject=InspectorSubject.None;inspectorOpen=false;}}
        else if(lastTower!=selected.Id){lastTower=selected.Id;inspectorSubject=InspectorSubject.Tower;inspectorOpen=false;purchaseOpen=false;}
        playerOpen=inspectorOpen&&inspectorSubject==InspectorSubject.Player&&!installOpen;
        bool towerOpen=inspectorOpen&&inspectorSubject==InspectorSubject.Tower&&selected!=null&&!installOpen;
        towerFolded=!towerOpen;
        playerPanel.gameObject.SetActive(playerOpen);towerPanel.gameObject.SetActive(towerOpen);
        playerPanel.sizeDelta=new Vector2(900,purchaseOpen?300:220);towerPanel.sizeDelta=new Vector2(900,purchaseOpen?354:280);
        foreach(var panel in new[]{playerPanel,towerPanel})
        {
            bool isPlayer=panel==playerPanel;int index=0;
            foreach(var name in new[]{isPlayer?"PlayerSpecTab":"TowerSpecTab",isPlayer?"PlayerBuyTab":"TowerBuyTab"})
            {
                var tab=(RectTransform)panel.Find(name);tab.anchoredPosition=purchaseOpen?new Vector2(16+index*442,-52):new Vector2(16,-56-index*64);
                tab.sizeDelta=purchaseOpen?new Vector2(426,44):new Vector2(220,56);
                tab.GetComponent<CoreGuardianHudReaction>().SetLabelLayout(new Vector2(54,purchaseOpen?-8:-16),tab.sizeDelta-new Vector2(70,purchaseOpen?16:32));
                var icon=tab.GetComponentsInChildren<UnityEngine.UI.Image>().First(i=>i.transform!=tab).rectTransform;
                icon.anchoredPosition=new Vector2(16,-(tab.sizeDelta.y-icon.sizeDelta.y)/2);
                index++;
            }
        }
        playerInfo.gameObject.SetActive(!purchaseOpen);towerInfo.gameObject.SetActive(!purchaseOpen);
        var st=s.Player;
        RefreshAbilityPlate(playerAbilities,st,false);
        playerTitle.text=purchaseOpen?"플레이어 능력치 강화":"플레이어 능력치";
        playerInfo.text="추가 발사 "+s.Augments.ExtraPlayerProjectiles+"발 · 둔화 효과 "+(s.Augments.PlayerSlow?"있음":"없음");
        if(selected!=null)
        {
            st=selected.Stats;towerTitle.text=TowerName(selected.Kind)+" #"+selected.Id+" · "+(purchaseOpen?"능력치 강화":"능력치");
            towerPanel.Find("Symbol_TowerIdentity").GetComponent<UnityEngine.UI.Image>().sprite=CoreGuardianHudSkin.Sprite(CoreGuardianHudSkin.Symbol("Install_"+selected.Kind));
            RefreshAbilityPlate(towerAbilities,st,selected.Kind==TowerKind.Support);
            float support=s.Towers.Where(t=>t.Kind==TowerKind.Support&&Vector2.Distance(t.Position,selected.Position)<=t.Stats.Range).Sum(t=>t.Stats.BuffDamage);
            float buff=support+s.Augments.TowerBuffPercent;
            towerInfo.rectTransform.anchoredPosition=new Vector2(280,selected.Kind==TowerKind.Support?-138:-186);
            towerInfo.text=selected.Kind==TowerKind.Support?"범위 안 공격 포탑의 공격력을 높입니다.\n직접 공격하지 않습니다.":
                "지원 포탑 +"+support.ToString("0.##")+"% · 증강 +"+s.Augments.TowerBuffPercent+"%\n버프 적용 공격력 "+StatDisplay(st.Damage*(1+buff/100f),"damage")+" (적 방어력 적용 전)";
            var destroy=(RectTransform)towerPanel.Find("Destroy");destroy.anchoredPosition=purchaseOpen?new Vector2(16,-300):new Vector2(264,-220);destroy.sizeDelta=new Vector2(purchaseOpen?868:612,44);
            ((RectTransform)destroy.Find("Label")).sizeDelta=new Vector2(destroy.sizeDelta.x-70,28);
        }
        bool details=playerOpen||towerOpen||installOpen;
        inspectorFold.gameObject.SetActive(details&&s.Phase!=SessionPhase.Augmentation);
        inspectorFold.anchoredPosition=new Vector2(0,112+(installOpen?installPanel.sizeDelta.y:playerOpen?playerPanel.sizeDelta.y:towerPanel.sizeDelta.y));
        bool hasTarget=inspectorSubject==InspectorSubject.Tower&&selected!=null;
        inspectorSummary.gameObject.SetActive(!details&&hasTarget&&s.Phase!=SessionPhase.Augmentation);
        summaryLabel.text=selected==null?"":"포탑 #"+selected.Id+" · 능력치";
        foreach(var name in new[]{"PrototypeActions","Reload","TestSpeed"}){var node=canvas.Find(name);if(node!=null)node.gameObject.SetActive(developerOpen);}
        var cancel=canvas.Find("CancelPlacement");if(cancel!=null)cancel.gameObject.SetActive(game.Installing.HasValue);
    }
}
