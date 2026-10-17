using System;
using System.Linq;
using CoreGuardian;
using UnityEngine;

// Display policy only: prices, progression, combat and placement remain owned by Session/Prototype.
public sealed partial class CoreGuardianHud
{
    enum InspectorSubject { None, Player, Tower }
    InspectorSubject inspectorSubject;
    bool inspectorOpen, purchaseOpen, developerOpen;
    SessionPhase? displayedPhase;
    RectTransform inspectorFold, inspectorSummary;
    UnityEngine.UI.Text playerInfo, towerTitle, summaryLabel;
    bool CanBuyPlayer=>game.Session.Phase==SessionPhase.Preparation||game.Session.Phase==SessionPhase.Combat;

    RectTransform SmallHandle(string name,Transform parent,Action action,bool horizontal)
    {
        var r=Box(name,parent,new Vector2(.5f,0),Vector2.zero,horizontal?new Vector2(80,28):new Vector2(28,80),false);
        var hit=r.gameObject.AddComponent<UnityEngine.UI.Image>();hit.color=Color.clear;
        var art=Box("HandleArt",r,new Vector2(.5f,.5f),Vector2.zero,new Vector2(22,80),false);
        var image=art.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=DamageSprite("DamageHandle");image.preserveAspect=true;image.raycastTarget=false;
        if(horizontal)art.localRotation=Quaternion.Euler(0,0,90);
        var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=image;
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
        var fold=canvas.Find("TopFold") as RectTransform;
        if(fold!=null){fold.anchoredPosition=new Vector2(0,compact?-64:-84);fold.Find("HandleArt").localScale=new Vector3(compact?-1:1,1,1);}
        var stage=canvas.Find("StagePlate") as RectTransform;if(stage!=null)stage.anchoredPosition=new Vector2(0,compact?-100:-120);
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
        CoreGuardianHudSkin.Icon(playerPanel,"Upgrade",new Vector2(16,-16),new Vector2(28,28));
        Text("Title",playerPanel,new Vector2(54,-12),new Vector2(820,32),22).text="기술병 · 현재 스펙 / 강화";
        CoreGuardianHudSkin.Icon(towerPanel,"Install",new Vector2(16,-16),new Vector2(28,28));
        towerTitle=Text("Title",towerPanel,new Vector2(54,-12),new Vector2(820,32),22);
        foreach(var panel in new[]{playerPanel,towerPanel})
        {
            bool isPlayer=panel==playerPanel;
            Button(isPlayer?"PlayerSpecTab":"TowerSpecTab",panel,new Vector2(16,-52),new Vector2(190,44),()=>"현재 스펙",()=>{purchaseOpen=false;Refresh();});
            Button(isPlayer?"PlayerBuyTab":"TowerBuyTab",panel,new Vector2(218,-52),new Vector2(190,44),()=>"강화 구매",()=>{purchaseOpen=true;Refresh();},()=>isPlayer?CanBuyPlayer:game.Session.Phase==SessionPhase.Preparation);
        }
        playerInfo=Text("Stats",playerPanel,new Vector2(20,-108),new Vector2(860,100),20);
        towerInfo=Text("Stats",towerPanel,new Vector2(20,-108),new Vector2(860,100),20);
        var stats=new[]{"damage","rate","projectileSpeed","range"};
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
        Button("Destroy",towerPanel,new Vector2(16,-216),new Vector2(426,44),()=>"파괴 · 실제 지출 50% 환급",()=>game.DestroySelected(),()=>CanBuyPlayer&&Selected()!=null);
        inspectorFold=SmallHandle("InspectorFold",canvas,()=>TryFoldDetails(),true);
        inspectorSummary=Box("InspectorSummary",canvas,new Vector2(.5f,0),new Vector2(0,112),new Vector2(340,42));
        summaryLabel=Text("Target",inspectorSummary,new Vector2(12,-6),new Vector2(250,30),18);
        var reopen=SmallHandle("InspectorReopen",inspectorSummary,()=>{if(inspectorSubject==InspectorSubject.None&&Selected()!=null)inspectorSubject=InspectorSubject.Tower;inspectorOpen=inspectorSubject!=InspectorSubject.None;purchaseOpen=false;},true);
        reopen.anchorMin=reopen.anchorMax=reopen.pivot=new Vector2(1,.5f);reopen.anchoredPosition=new Vector2(-4,0);reopen.sizeDelta=new Vector2(64,28);
        playerPanel.gameObject.SetActive(false);towerPanel.gameObject.SetActive(false);
    }
    void OpenPlayer(bool buy)
    {
        if(game.Installing.HasValue||game.Session.Phase==SessionPhase.Augmentation||buy&&!CanBuyPlayer)return;
        bool same=inspectorSubject==InspectorSubject.Player&&inspectorOpen&&purchaseOpen==buy;
        installOpen=false;installPanel.gameObject.SetActive(false);inspectorSubject=InspectorSubject.Player;inspectorOpen=!same;purchaseOpen=buy;Refresh();
    }
    public void OpenSelectedTowerSpecs()
    {
        if(Selected()==null||game.Installing.HasValue||game.Session.Phase==SessionPhase.Augmentation)return;
        installOpen=false;installPanel.gameObject.SetActive(false);lastTower=game.SelectedTower;
        inspectorSubject=InspectorSubject.Tower;inspectorOpen=true;purchaseOpen=false;Refresh();
    }
    // True consumes a floor click; never changes selection, currency, or placement state.
    public bool TryFoldDetails()
    {
        if(game.Installing.HasValue||game.Session.Phase==SessionPhase.Augmentation)return false;
        if(!installOpen&&!inspectorOpen)return false;
        installOpen=false;installPanel.gameObject.SetActive(false);inspectorOpen=false;purchaseOpen=false;
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
            if(displayedPhase.HasValue){inspectorOpen=false;purchaseOpen=false;developerOpen=false;}
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
        playerPanel.sizeDelta=new Vector2(900,purchaseOpen?300:220);towerPanel.sizeDelta=new Vector2(900,purchaseOpen?354:270);
        playerInfo.gameObject.SetActive(!purchaseOpen);towerInfo.gameObject.SetActive(!purchaseOpen);
        var st=s.Player;
        playerInfo.text="공격력 "+StatDisplay(st.Damage,"damage")+"       공격속도 "+st.Rate.ToString("0.##")+"발/초\n사거리 "+st.Range.ToString("0.##")+"타일       투사체 "+st.ProjectileSpeed.ToString("0.##")+"타일/초\n추가 투사체 "+s.Augments.ExtraPlayerProjectiles+"발 · 명중 둔화 "+(s.Augments.PlayerSlow?"적용":"없음");
        if(selected!=null)
        {
            st=selected.Stats;towerTitle.text=TowerName(selected.Kind)+" #"+selected.Id+" · "+(purchaseOpen?"강화 구매":"현재 스펙");
            float support=s.Towers.Where(t=>t.Kind==TowerKind.Support&&Vector2.Distance(t.Position,selected.Position)<=t.Stats.Range).Sum(t=>t.Stats.BuffDamage);
            float buff=support+s.Augments.TowerBuffPercent;
            towerInfo.text=selected.Kind==TowerKind.Support?"지원 공격력 +"+st.BuffDamage.ToString("0.##")+"% · 반경 "+st.Range.ToString("0.##")+"타일\n범위 안 공격 포탑에 적용 · 직접 공격 없음":
                "공격력 "+StatDisplay(st.Damage,"damage")+" · 보정 반영 "+StatDisplay(st.Damage*(1+buff/100f),"damage")+"       공격속도 "+st.Rate.ToString("0.##")+"발/초\n사거리 "+st.Range.ToString("0.##")+"타일       투사체 "+st.ProjectileSpeed.ToString("0.##")+"타일/초\n공격력 보정: 지원 +"+support.ToString("0.##")+"% / 증강 +"+s.Augments.TowerBuffPercent+"% · 적 방어 적용 전";
            ((RectTransform)towerPanel.Find("Destroy")).anchoredPosition=new Vector2(16,purchaseOpen?-300:-216);
        }
        bool details=playerOpen||towerOpen||installOpen;
        inspectorFold.gameObject.SetActive(details&&s.Phase!=SessionPhase.Augmentation);
        inspectorFold.anchoredPosition=new Vector2(0,112+(installOpen?installPanel.sizeDelta.y:playerOpen?playerPanel.sizeDelta.y:towerPanel.sizeDelta.y));
        bool hasTarget=inspectorSubject==InspectorSubject.Player||selected!=null;
        inspectorSummary.gameObject.SetActive(!details&&hasTarget&&s.Phase!=SessionPhase.Augmentation);
        summaryLabel.text=inspectorSubject==InspectorSubject.Player?"기술병 · 스펙":selected==null?"":"포탑 #"+selected.Id+" · 스펙";
        foreach(var name in new[]{"PrototypeActions","Reload","TestSpeed"}){var node=canvas.Find(name);if(node!=null)node.gameObject.SetActive(developerOpen);}
        var cancel=canvas.Find("CancelPlacement");if(cancel!=null)cancel.gameObject.SetActive(game.Installing.HasValue);
    }
}
