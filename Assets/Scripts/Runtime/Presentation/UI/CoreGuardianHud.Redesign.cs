using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// HUD 개편 B안 (2026-10-28 사용자 확정): 왼쪽 아래 고정 선택 정보창, 받은 증강 세로 줄 + Tab 목록,
// 범위 폭발 아이콘 버튼(시계 방향 덮개, 클릭 사용), 누적 피해 스크롤바. 아트는 Hud/Redesign·Hud/Augments.
public sealed partial class CoreGuardianHud
{
    enum InfoSubject { None, Player, Tower, Enemy }
    InfoSubject infoSubject;
    EnemyHandle infoEnemy;
    RectTransform infoPanel;
    Image infoIcon;
    Text infoTitle, infoNote;
    const int InfoRows=8;
    readonly Image[] infoRowIcons=new Image[InfoRows];
    readonly Text[] infoRowTexts=new Text[InfoRows];
    Button infoDestroy;
    int infoRowCount;

    // ---------------- selection ----------------
    void ReleaseInfo(){infoSubject=InfoSubject.None;infoEnemy=default;lastTower=-1;game.ClearInspectionSelection();}
    bool InfoBlocked=>game.Installing.HasValue||game.Session.Phase==SessionPhase.Augmentation;
    public void ShowPlayerInfo()
    {
        if(InfoBlocked)return;
        if(infoSubject!=InfoSubject.Player)game.GetComponent<CoreGuardianCombatAudio>()?.PlayCue("ui_open");
        game.ClearInspectionSelection();infoSubject=InfoSubject.Player;infoEnemy=default;lastTower=-1;Refresh();
    }
    public void ShowTowerInfo()
    {
        var t=Selected();if(t==null||InfoBlocked)return;
        if(infoSubject!=InfoSubject.Tower||lastTower!=t.Id)game.GetComponent<CoreGuardianCombatAudio>()?.PlayCue("ui_open");
        infoSubject=InfoSubject.Tower;infoEnemy=default;lastTower=t.Id;Refresh();
    }
    public void ShowEnemyInfo(EnemyHandle enemy)
    {
        if(InfoBlocked||!enemy.Alive)return;
        if(infoSubject!=InfoSubject.Enemy||infoEnemy!=enemy)game.GetComponent<CoreGuardianCombatAudio>()?.PlayCue("ui_open");
        game.ClearInspectionSelection();lastTower=-1;infoSubject=InfoSubject.Enemy;infoEnemy=enemy;Refresh();
    }
    public bool InfoVisible=>infoPanel!=null&&infoPanel.gameObject.activeSelf;

    void BuildSelectionInfo()
    {
        // 왼쪽 아래 구석의 포탑 연구소 건물과 받은 증강 줄을 가리지 않게 그 위·오른쪽에 둠 (10-28 사용자 확정: 정보창 위치 변경).
        infoPanel=Box("SelectionInfo",canvas,Vector2.zero,new Vector2(76,200),new Vector2(440,400));
        infoIcon=CoreGuardianHudSkin.Icon(infoPanel,"PlayerIdentity",new Vector2(16,-14),new Vector2(30,30));
        infoTitle=Text("Title",infoPanel,new Vector2(54,-10),new Vector2(318,38),20);
        Button("InfoClose",infoPanel,new Vector2(440-62,-10),new Vector2(46,40),()=>"",()=>{ReleaseInfo();Refresh();});
        for(int i=0;i<InfoRows;i++)
        {
            infoRowIcons[i]=CoreGuardianHudSkin.Icon(infoPanel,"Damage",new Vector2(18,-58-i*32),new Vector2(26,26));
            infoRowTexts[i]=Text("Row"+i,infoPanel,new Vector2(52,-56-i*32),new Vector2(372,30),16);
        }
        infoNote=Text("Note",infoPanel,new Vector2(18,-60),new Vector2(406,60),14);
        infoNote.alignment=TextAnchor.UpperLeft;infoNote.color=new Color(.82f,.86f,.88f);
        infoDestroy=Button("Destroy",infoPanel,new Vector2(16,-300),new Vector2(408,44),()=>"파괴 · 설치비 "+game.Session.RefundPercent+"% 반환 · 공유 강화 유지",()=>{game.DestroySelected();Refresh();},()=>CanBuyPlayer&&Selected()!=null);
        infoPanel.gameObject.SetActive(false);
    }
    void InfoRow(Sprite icon,string text)
    {
        if(infoRowCount>=InfoRows)return;int i=infoRowCount++;
        if(infoRowIcons[i].sprite!=icon)infoRowIcons[i].sprite=icon;
        SetHudActive(infoRowIcons[i].gameObject,icon!=null);SetHudActive(infoRowTexts[i].gameObject,true);SetHudText(infoRowTexts[i],text);
    }
    static Sprite HudSprite(string name)=>CoreGuardianHudSkin.Sprite(name);
    static Sprite H5(string name)=>CoreGuardianHudSkin.Sprite("Redesign/H5_"+name);
    void RefreshSelectionInfo()
    {
        if(infoPanel==null)return;
        var s=game.Session;
        if(infoSubject==InfoSubject.Tower)
        {
            var t=Selected();
            if(t==null||t.Id!=lastTower){infoSubject=InfoSubject.None;lastTower=-1;}
        }
        if(infoSubject==InfoSubject.Enemy&&!infoEnemy.Alive){infoSubject=InfoSubject.None;infoEnemy=default;}
        bool show=infoSubject!=InfoSubject.None&&s.Phase!=SessionPhase.Augmentation;
        SetHudActive(infoPanel.gameObject,show);
        if(!show)return;
        infoRowCount=0;string note="";bool destroy=false;
        if(infoSubject==InfoSubject.Player)
        {
            var p=s.Player;
            infoIcon.sprite=HudSprite("PlayerIdentity");SetHudText(infoTitle,"기술병");
            InfoRow(HudSprite("StatDamage"),"공격력 "+StatDisplay(p.Damage,"damage"));
            InfoRow(HudSprite("StatRate"),"공격속도 "+p.Rate.ToString("0.##")+"발/초");
            InfoRow(HudSprite("StatRange"),"사거리 "+p.Range.ToString("0.##")+"타일");
            InfoRow(HudSprite("StatProjectile"),"탄환 속도 "+p.ProjectileSpeed.ToString("0.##")+"타일/초");
            InfoRow(HudSprite("AreaBlast"),"범위 폭발 "+(s.AreaBlast==null?"없음":s.Phase!=SessionPhase.Combat?"전투 중 사용":s.AreaBlastReady?"준비":Mathf.CeilToInt(s.AreaBlastCooldown)+"초 남음"));
            s.DamageTotals.TryGetValue("Player",out int dealt);
            InfoRow(HudSprite("Damage"),"이번 라운드 피해 "+dealt);
            note="약점·저항은 포탑에만 적용 · 강화는 기술병 작업대(2)";
        }
        else if(infoSubject==InfoSubject.Tower)
        {
            var t=Selected();var spec=s.Data.Towers[t.Kind];var st=t.Stats;
            infoIcon.sprite=HudSprite(CoreGuardianHudSkin.Symbol("Install_"+t.Kind));
            SetHudText(infoTitle,TowerName(t.Kind)+" #"+t.Id+(t.Stars>1?" ★"+t.Stars:""));
            if(t.Kind==TowerKind.Support)
            {
                InfoRow(HudSprite("Augment"),"공격력 증가 +"+st.BuffDamage.ToString("0.##")+"% · 범위 "+st.Range.ToString("0.##")+"타일");
                note="범위 안 공격 포탑의 공격력을 높입니다. 직접 공격하지 않습니다.";
            }
            else
            {
                float support=s.Towers.Where(x=>x.Kind==TowerKind.Support&&Vector2.Distance(x.Position,t.Position)<=x.Stats.Range).Sum(x=>x.Stats.BuffDamage);
                float buff=support+s.Augments.TowerBuffPercent;
                float hit=st.Damage*(1+buff/100f);
                InfoRow(H5("dps"),"초당 피해 "+(hit*st.Rate).ToString("0.#")+(t.Kind==TowerKind.Multi?" (대상마다, 최대 "+spec.MaxTargets+"명)":t.Kind==TowerKind.Frost?" (범위 안 전체)":"")+" · 버프 +"+buff.ToString("0.##")+"%");
                InfoRow(HudSprite("StatDamage"),"피해 "+StatDisplay(st.Damage,"damage")+" · 공속 "+st.Rate.ToString("0.##")+"발/초 · 사거리 "+st.Range.ToString("0.##")+"타일");
                string tag=TowerTagText(t.Kind,st);if(tag.Length>0)InfoRow(HudSprite(CoreGuardianHudSkin.Symbol("Install_"+t.Kind)),"효과: "+tag);
                InfoRow(H5("weakness"),"잘 잡는 적: "+EnemyList(e=>(e.WeakTowers&(1<<(int)t.Kind))!=0));
                InfoRow(H5("resistance"),"약한 적: "+EnemyList(e=>(e.ResistTowers&(1<<(int)t.Kind))!=0));
                s.DamageTotals.TryGetValue(t.AttackSource,out int dealt);
                InfoRow(HudSprite("Damage"),"이번 라운드 피해 "+dealt);
                note="공격 순서: "+OrderText(spec);
            }
            destroy=true;
        }
        else
        {
            var e=infoEnemy;var spec=e.Spec;
            infoIcon.sprite=HudSprite("Damage");
            SetHudText(infoTitle,EnemyName(e)+" · "+EnemyFamily(e));
            InfoRow(H5("hp"),"체력 "+e.Health+" / "+spec.Health);
            float defense=spec.Defense+(e.Shielded?spec.ShieldDefense:0);
            InfoRow(H5("defense"),"방어 "+defense.ToString("0.##")+"% (받는 피해 감소)"+(e.Shielded?" · 방패 전개":""));
            InfoRow(H5("move_speed"),"이동속도 "+e.MovementSpeed.ToString("0.##")+"타일/초 (기본 "+spec.Speed.ToString("0.##")+")");
            InfoRow(H5("core_damage"),"코어 피해 "+spec.CoreDamage);
            InfoRow(HudSprite("Gold"),"처치 골드 "+(e.Summoned?0:spec.Gold));
            InfoRow(H5("weakness"),"약점: "+TowerList(spec.WeakTowers)+" (×"+WeakMul().ToString("0.##")+")");
            InfoRow(H5("resistance"),"저항: "+TowerList(spec.ResistTowers)+" (×"+ResistMul().ToString("0.##")+")");
            note="능력: "+AbilityText(e)+"\n상태: "+StatusText(e);
        }
        for(int i=infoRowCount;i<InfoRows;i++){SetHudActive(infoRowIcons[i].gameObject,false);SetHudActive(infoRowTexts[i].gameObject,false);}
        float noteY=-60-infoRowCount*32;
        SetHudPosition(infoNote.rectTransform,new Vector2(18,noteY));SetHudText(infoNote,note);
        float bottom=noteY-(note.Length>0?60:0);
        SetHudActive(infoDestroy.gameObject,destroy);
        if(destroy){SetHudPosition((RectTransform)infoDestroy.transform,new Vector2(16,bottom-4));bottom-=52;}
        SetHudSize(infoPanel,new Vector2(440,-bottom+12));
    }
    float WeakMul(){var s=game.Session;return s.Augments.Has(49)?s.Augments.Value(49)/s.Augments.Count(49):s.Data.Tag("Affinity","weakMultiplier",false,1.3f);}
    float ResistMul(){var s=game.Session;return s.Augments.Has(49)?s.Augments.Value(49,1)/s.Augments.Count(49):s.Data.Tag("Affinity","resistMultiplier",false,.7f);}
    string TowerList(int mask)
    {
        if(mask==0)return "없음";
        var names=new List<string>();foreach(TowerKind k in Enum.GetValues(typeof(TowerKind)))if((mask&(1<<(int)k))!=0)names.Add(TowerName(k));
        return string.Join(", ",names);
    }
    string EnemyList(Func<EnemySpec,bool> match)
    {
        var names=new List<string>();var seen=new HashSet<EnemyKind>();
        foreach(var e in game.Session.Data.EnemiesById.Values)
            if(e.Kind!=EnemyKind.SplitChild&&e.Kind!=EnemyKind.Boss&&match(e)&&seen.Add(e.Kind))names.Add(KindName(e.Kind));
        return names.Count==0?"없음":string.Join(", ",names);
    }
    string OrderText(TowerSpec spec)
    {
        if(spec.TargetOrder==null)return spec.Kind==TowerKind.Frost?"없음 (범위 안 전체 펄스)":"직접 공격하지 않음";
        var names=new List<string>();var seen=new HashSet<EnemyKind>();
        foreach(var id in spec.TargetOrder.EnemyIds)
        {
            if(!game.Session.Data.EnemiesById.TryGetValue(id,out var e))continue;
            var k=e.Kind==EnemyKind.SplitChild?EnemyKind.Splitter:e.Kind;
            if(seen.Add(k))names.Add(KindName(k));
        }
        string tie=spec.TargetOrder.Tiebreak==TargetOrder.HighestHealth?"체력 많은 적":spec.TargetOrder.Tiebreak==TargetOrder.NearestCore?"코어 가까운 적":spec.TargetOrder.Tiebreak==TargetOrder.UnburnedNearestCore?"화상 없는 적 → 코어 가까운 적":spec.TargetOrder.Tiebreak==TargetOrder.MostPierced?"가장 많이 꿰뚫는 방향":"체력 낮은 적";
        return string.Join(" > ",names)+"\n같은 순위: "+tie;
    }
    string TowerTagText(TowerKind kind,CombatStats st)
    {
        switch(kind)
        {
            case TowerKind.Frost:return "둔화 "+st.SlowPercent.ToString("0.##")+"% · "+st.SlowDuration.ToString("0.##")+"초";
            case TowerKind.Flame:return "화상 스택당 초당 "+st.BurnPerStack.ToString("0.##")+" · "+st.BurnDuration.ToString("0.##")+"초";
            case TowerKind.Rail:return "관통 · 통과마다 ×"+st.PierceFalloff.ToString("0.##");
            case TowerKind.Multi:return "최대 "+game.Session.Data.Towers[kind].MaxTargets+"명 동시 공격";
            default:return "";
        }
    }
    static string KindName(EnemyKind kind)
    {
        switch(kind)
        {
            case EnemyKind.Attack:return "공격형";case EnemyKind.Defense:return "수비형";case EnemyKind.Buff:return "버프형";
            case EnemyKind.Charger:return "돌격병";case EnemyKind.Splitter:return "분열체";case EnemyKind.SplitChild:return "분열 작은 개체";
            case EnemyKind.Healer:return "치유사";case EnemyKind.Armored:return "장갑병";case EnemyKind.Flyer:return "비행체";default:return "보스";
        }
    }
    static string EnemyName(EnemyHandle e)=>e.Kind==EnemyKind.Boss?BossName(e.Spec):KindName(e.Kind)+(e.Summoned?" (소환 졸개)":"");
    static string EnemyFamily(EnemyHandle e)
    {
        switch(e.Kind)
        {
            case EnemyKind.Defense:case EnemyKind.Armored:return "수비형 계열";
            case EnemyKind.Buff:case EnemyKind.Healer:return "버프형 계열";
            case EnemyKind.Boss:return e.Spec.Key=="IronKnight"?"수비형 보스":e.Spec.Key=="CrimsonLord"?"버프형 보스":"보스";
            default:return "공격형 계열";
        }
    }
    static string AbilityText(EnemyHandle e)
    {
        var s=e.Spec;var parts=new List<string>();
        if(s.BuffRadius>0&&s.BuffSpeed>0)parts.Add("주변 "+s.BuffRadius.ToString("0.##")+"타일 이동속도 +"+s.BuffSpeed.ToString("0.##")+"%");
        if(s.HealInterval>0)parts.Add(s.HealInterval.ToString("0.##")+"초마다 "+s.HealRadius.ToString("0.##")+"타일 회복 "+s.HealPercent.ToString("0.##")+"%");
        if(s.SplitIntoId!=0)parts.Add("처치 시 "+s.SplitCount+"마리로 분열");
        if(s.Flying)parts.Add("비행 경로(지름길)");
        if(e.Kind==EnemyKind.Boss)parts.Add("체력 "+s.RagePercent.ToString("0")+"% 이하 격노 +"+s.RageBoost.ToString("0")+"%");
        if(s.ShieldAt.Length>0)parts.Add("체력 "+string.Join("·",s.ShieldAt.Select(x=>x.ToString("0")+"%"))+"에서 "+s.ShieldSeconds.ToString("0.#")+"초 방어 +"+s.ShieldDefense.ToString("0"));
        if(s.SummonId!=0)parts.Add(s.SummonInterval.ToString("0.#")+"초마다 졸개 "+s.SummonCount+"마리 소환");
        if(parts.Count==0)parts.Add(e.Kind==EnemyKind.Charger?"매우 빠름":e.Kind==EnemyKind.Armored?"높은 방어":"없음");
        return string.Join(" · ",parts);
    }
    static string StatusText(EnemyHandle e)
    {
        var parts=new List<string>();
        float slow=e.SlowPercent;if(slow>0)parts.Add("둔화 "+slow.ToString("0")+"%");
        if(e.BurnStacks>0)parts.Add("화상 "+e.BurnStacks+"스택");
        if(e.Frozen)parts.Add("얼어붙음");else if(e.Stunned)parts.Add("기절");
        if(e.Shielded)parts.Add("방패 전개");
        if(e.Enraged)parts.Add("격노");
        if(e.Buffed)parts.Add("버프 받음");
        return parts.Count==0?"없음":string.Join(" · ",parts);
    }

    // ---------------- received augments ----------------
    sealed class AugmentSlot{public RectTransform Root;public Image Frame,Icon;public Text Count;public AugmentSpec Spec;}
    RectTransform augmentStrip, augmentTooltip, augmentList, augmentGrid;
    Text augmentTooltipText, augmentListTitle, augmentListDetail, augmentMoreLabel;
    RectTransform augmentMore;
    readonly AugmentSlot[] stripSlots=new AugmentSlot[6];
    readonly List<AugmentSlot> listSlots=new List<AugmentSlot>();
    bool augmentListPinned;
    int augmentListSignature=-1;
    Dictionary<string,AugmentSpec> augmentById;
    AugmentSpec AugmentSpecOf(string id)
    {
        if(augmentById==null){augmentById=new Dictionary<string,AugmentSpec>();foreach(var a in game.Session.Data.Augments)augmentById[a.Id.ToString()]=a;}
        return augmentById.TryGetValue(id,out var spec)?spec:null;
    }
    static Sprite AugmentIcon(AugmentSpec spec)=>spec==null?null:CoreGuardianHudSkin.Sprite("Augments/augment_"+spec.Number.ToString("00"));
    static Sprite GradeFrame(AugmentationGrade grade)=>CoreGuardianHudSkin.Sprite("Redesign/H4_frame_"+grade.ToString().ToLowerInvariant());
    static string GradeName(AugmentationGrade grade)=>grade==AugmentationGrade.Silver?"실버":grade==AugmentationGrade.Gold?"골드":"플래티넘";
    AugmentSlot MakeAugmentSlot(Transform parent,string name,Vector2 position,float size,Action<AugmentSlot,bool> hover)
    {
        var root=Box(name,parent,new Vector2(0,1),position,new Vector2(size,size),false);
        var frame=root.gameObject.AddComponent<Image>();frame.preserveAspect=true;frame.raycastTarget=true;
        float scale=size/36f;
        var icon=Box("Icon",root,new Vector2(0,1),new Vector2(2*scale,-2*scale),new Vector2(32*scale,32*scale),false).gameObject.AddComponent<Image>();
        icon.preserveAspect=true;icon.raycastTarget=false;
        var count=Text("Count",root,new Vector2(size-34,-size+20),new Vector2(36,20),14);count.alignment=TextAnchor.LowerRight;count.color=new Color(1,.95f,.7f);
        count.gameObject.AddComponent<Outline>().effectColor=new Color(0,0,0,.9f);
        var slot=new AugmentSlot{Root=root,Frame=frame,Icon=icon,Count=count};
        var hint=root.gameObject.AddComponent<CoreGuardianHoverHint>();hint.Changed=on=>hover(slot,on);
        return slot;
    }
    void SetSlot(AugmentSlot slot,AugmentSpec spec,int count)
    {
        slot.Spec=spec;SetHudActive(slot.Root.gameObject,spec!=null);if(spec==null)return;
        var frame=GradeFrame(spec.Grade);if(slot.Frame.sprite!=frame)slot.Frame.sprite=frame;
        var icon=AugmentIcon(spec);if(slot.Icon.sprite!=icon)slot.Icon.sprite=icon;
        SetHudText(slot.Count,count>1?"×"+count:"");
    }
    void BuildAugmentStrip()
    {
        augmentStrip=Box("AugmentStrip",canvas,new Vector2(0,1),new Vector2(16,-300),new Vector2(48,6*50+40),false);
        for(int i=0;i<stripSlots.Length;i++)stripSlots[i]=MakeAugmentSlot(augmentStrip,"Augment"+i,new Vector2(0,-i*50),46,(slot,on)=>ShowAugmentTooltip(slot,on));
        augmentMore=Box("AugmentMore",augmentStrip,new Vector2(0,1),new Vector2(0,-300),new Vector2(48,36));
        CoreGuardianHudSkin.Frame(augmentMore.GetComponent<Image>(),"Button");
        var more=augmentMore.gameObject.AddComponent<Button>();more.navigation=new Navigation{mode=Navigation.Mode.None};
        augmentMoreLabel=Text("Label",augmentMore,new Vector2(0,-4),new Vector2(48,28),15);augmentMoreLabel.alignment=TextAnchor.MiddleCenter;
        augmentMore.gameObject.AddComponent<CoreGuardianHudReaction>();
        more.onClick.AddListener(()=>{game.GetComponent<CoreGuardianCombatAudio>()?.PlayCue("ui_click");augmentListPinned=!augmentListPinned;RefreshAugmentList();});
        augmentTooltip=Box("AugmentTooltip",canvas,new Vector2(0,1),new Vector2(76,-300),new Vector2(380,112));
        augmentTooltip.GetComponent<Image>().raycastTarget=false;
        augmentTooltipText=Text("Text",augmentTooltip,new Vector2(16,-10),new Vector2(348,92),15);augmentTooltipText.alignment=TextAnchor.UpperLeft;
        augmentTooltip.gameObject.SetActive(false);
        // Tab: centred grid of every received card (received order, duplicates ×N) + the hovered card below.
        augmentList=Box("AugmentList",canvas,new Vector2(.5f,.5f),new Vector2(0,20),new Vector2(880,600));
        CoreGuardianHudSkin.Icon(augmentList,"Augment",new Vector2(20,-18),new Vector2(30,30));
        augmentListTitle=Text("Title",augmentList,new Vector2(60,-14),new Vector2(640,38),24);
        var hint=Text("Hint",augmentList,new Vector2(560,-14),new Vector2(300,38),15);hint.alignment=TextAnchor.MiddleRight;hint.color=new Color(.75f,.8f,.84f);hint.text="Tab을 누르는 동안 표시";
        augmentGrid=Box("Grid",augmentList,new Vector2(0,1),new Vector2(24,-64),new Vector2(832,400),false);
        var detailPlate=Box("Detail",augmentList,new Vector2(0,1),new Vector2(24,-476),new Vector2(832,104));
        augmentListDetail=Text("Text",detailPlate,new Vector2(16,-10),new Vector2(800,84),16);augmentListDetail.alignment=TextAnchor.UpperLeft;
        augmentList.gameObject.SetActive(false);
    }
    void ShowAugmentTooltip(AugmentSlot slot,bool on)
    {
        if(augmentTooltip==null)return;
        if(!on||slot.Spec==null||!slot.Root.gameObject.activeInHierarchy){SetHudActive(augmentTooltip.gameObject,false);return;}
        SetHudText(augmentTooltipText,slot.Spec.Name+" · "+GradeName(slot.Spec.Grade)+"\n"+slot.Spec.Text);
        var y=augmentStrip.anchoredPosition.y+slot.Root.anchoredPosition.y;
        SetHudPosition(augmentTooltip,new Vector2(76,y));
        augmentTooltip.SetAsLastSibling();SetHudActive(augmentTooltip.gameObject,true);
    }
    void RefreshAugmentStrip()
    {
        if(augmentStrip==null)return;
        var selected=game.Session.Augments.Selected;
        bool show=selected.Count>0&&game.Session.Phase!=SessionPhase.Augmentation;
        SetHudActive(augmentStrip.gameObject,show);
        if(!show){SetHudActive(augmentTooltip.gameObject,false);}
        // Most recent first.
        for(int i=0;i<stripSlots.Length;i++){int index=selected.Count-1-i;SetSlot(stripSlots[i],index>=0?AugmentSpecOf(selected[index]):null,1);}
        int extra=selected.Count-stripSlots.Length;
        SetHudActive(augmentMore.gameObject,selected.Count>0);
        SetHudPosition(augmentMore,new Vector2(0,-Math.Min(selected.Count,stripSlots.Length)*50));
        SetHudText(augmentMoreLabel,extra>0?"+"+extra:"전체");
        if(extra<=0)augmentMoreLabel.fontSize=13;else augmentMoreLabel.fontSize=15;
        RefreshAugmentList();
    }
    bool AugmentListWanted=>(augmentListPinned||Keyboard.current!=null&&Keyboard.current.tabKey.isPressed)&&!placementFocused&&game!=null&&game.Ready;
    void RefreshAugmentList()
    {
        if(augmentList==null)return;
        bool show=AugmentListWanted;
        if(augmentList.gameObject.activeSelf!=show){augmentList.gameObject.SetActive(show);if(show)augmentList.SetAsLastSibling();}
        if(!show)return;
        var selected=game.Session.Augments.Selected;
        SetHudText(augmentListTitle,"받은 증강 "+selected.Count+"장");
        if(augmentListSignature==selected.Count)return;
        augmentListSignature=selected.Count;
        var order=new List<string>();var counts=new Dictionary<string,int>();
        foreach(var id in selected){if(!counts.ContainsKey(id)){counts[id]=0;order.Add(id);}counts[id]++;}
        while(listSlots.Count<order.Count){int n=listSlots.Count;listSlots.Add(MakeAugmentSlot(augmentGrid,"Card"+n,new Vector2(n%10*83,-n/10*80),72,(slot,on)=>{if(on&&slot.Spec!=null)SetHudText(augmentListDetail,slot.Spec.Name+" · "+GradeName(slot.Spec.Grade)+"\n"+slot.Spec.Text);}));}
        for(int i=0;i<listSlots.Count;i++)SetSlot(listSlots[i],i<order.Count?AugmentSpecOf(order[i]):null,i<order.Count?counts[order[i]]:0);
        SetHudText(augmentListDetail,order.Count==0?"아직 받은 증강이 없습니다.":"아이콘에 마우스를 올리면 이름·효과·등급을 봅니다.");
    }

    // ---------------- area blast icon ----------------
    Image blastCooldown;Text blastTimer;
    void BuildBlastButton(RectTransform controls)
    {
        var r=Box("AreaBlast",controls,new Vector2(0,1),new Vector2(468,-8),new Vector2(76,72));
        CoreGuardianHudSkin.Frame(r.GetComponent<Image>(),"Button");
        var b=r.gameObject.AddComponent<Button>();b.navigation=new Navigation{mode=Navigation.Mode.None};
        b.onClick.AddListener(()=>{if(!b.interactable)return;game.UseAreaBlast();Refresh();});
        var icon=CoreGuardianHudSkin.Icon(r,"AreaBlast",new Vector2(14,-6),new Vector2(48,44));
        blastCooldown=Box("Cooldown",r,new Vector2(0,1),new Vector2(14,-6),new Vector2(48,44),false).gameObject.AddComponent<Image>();
        blastCooldown.sprite=icon.sprite;blastCooldown.preserveAspect=true;blastCooldown.raycastTarget=false;blastCooldown.color=new Color(0,0,0,.68f);
        blastCooldown.type=Image.Type.Filled;blastCooldown.fillMethod=Image.FillMethod.Radial360;blastCooldown.fillOrigin=(int)Image.Origin360.Top;blastCooldown.fillClockwise=false;blastCooldown.fillAmount=0;
        blastTimer=Text("Timer",r,new Vector2(0,-6),new Vector2(76,44),20);blastTimer.alignment=TextAnchor.MiddleCenter;blastTimer.gameObject.AddComponent<Outline>().effectColor=new Color(0,0,0,.9f);
        var key=Text("Key",r,new Vector2(0,-50),new Vector2(76,18),12);key.alignment=TextAnchor.MiddleCenter;key.text="Space";key.color=new Color(1,.95f,.84f);
        r.gameObject.AddComponent<CoreGuardianHudReaction>();
        refresh.Add(()=>{b.interactable=game.Session.AreaBlastReady;});
        blastButton=b;
    }
    void UpdateBlastCooldown()
    {
        if(blastCooldown==null||game==null||!game.Ready)return;
        var s=game.Session;float total=s.AreaBlast!=null?s.AreaBlast.Cooldown:0;
        bool cooling=total>0&&s.AreaBlastCooldown>0;
        float fill=cooling?Mathf.Clamp01(s.AreaBlastCooldown/total):0;
        if(!Mathf.Approximately(blastCooldown.fillAmount,fill))blastCooldown.fillAmount=fill;
        SetHudText(blastTimer,cooling?Mathf.CeilToInt(s.AreaBlastCooldown).ToString():"");
    }
    void UpdateRedesignFrame()
    {
        UpdateBlastCooldown();
        if(augmentList!=null&&augmentList.gameObject.activeSelf!=AugmentListWanted)RefreshAugmentList();
    }

    // ---------------- damage panel scrollbar ----------------
    void AddDamageScrollbar(ScrollRect scroll)
    {
        var bar=Box("Scrollbar",damagePanel,new Vector2(0,1),new Vector2(310,-56),new Vector2(10,308),false);
        var track=bar.gameObject.AddComponent<Image>();track.sprite=CoreGuardianHudSkin.Sprite("Redesign/H6_scroll_track");track.type=Image.Type.Sliced;
        var area=Box("SlidingArea",bar,new Vector2(0,1),Vector2.zero,new Vector2(10,308),false);
        area.anchorMin=Vector2.zero;area.anchorMax=Vector2.one;area.sizeDelta=Vector2.zero;area.anchoredPosition=Vector2.zero;
        var handle=Box("Handle",area,new Vector2(0,1),Vector2.zero,Vector2.zero,false);
        handle.anchorMin=Vector2.zero;handle.anchorMax=Vector2.one;handle.sizeDelta=Vector2.zero;
        var handleImage=handle.gameObject.AddComponent<Image>();handleImage.sprite=CoreGuardianHudSkin.Sprite("Redesign/H6_scroll_handle");handleImage.type=Image.Type.Sliced;
        var scrollbar=bar.gameObject.AddComponent<Scrollbar>();scrollbar.direction=Scrollbar.Direction.BottomToTop;scrollbar.handleRect=handle;scrollbar.targetGraphic=handleImage;
        scrollbar.navigation=new Navigation{mode=Navigation.Mode.None};
        scroll.verticalScrollbar=scrollbar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
    }

    // ---------------- hit numbers: 약점 크게·강조색 / 저항 작게·회색 ----------------
    static readonly Color WeakNumberColor=new Color(1f,.55f,.22f),ResistNumberColor=new Color(.66f,.68f,.7f);
}

// Pointer hover relay for HUD hints (augment icons).
public sealed class CoreGuardianHoverHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Action<bool> Changed;
    bool inside;
    public void OnPointerEnter(PointerEventData e){inside=true;Changed?.Invoke(true);}
    public void OnPointerExit(PointerEventData e){inside=false;Changed?.Invoke(false);}
    void OnDisable(){if(inside){inside=false;Changed?.Invoke(false);}}
}
