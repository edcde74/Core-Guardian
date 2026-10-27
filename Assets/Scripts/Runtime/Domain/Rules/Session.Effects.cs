using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreGuardian
{
    // Effect tags, enemy abilities, area blast and augment combat hooks.
    // Rules: Notion 전투 피해 계산 규칙 · 적 유형 및 보스 설계 · 타워 시스템 · 증강 시스템 (2026-10-26/27).
    public sealed partial class Session
    {
        public readonly struct CueEvent {public readonly string Id;public readonly Vector2 Position;public CueEvent(string id,Vector2 position){Id=id;Position=position;}}
        // Presentation cues (sound/VFX): crit, execute, freeze, shatter, burn_explode, chain, heal_pulse, shield_deploy,
        // summon, split, area_blast, cold_pulse, flame_shot, rail_shot, hit_cold, hit_flame, hit_rail, star_merge, reroll.
        public event Action<CueEvent> Cue;
        public event Action<int,Vector2,float> TowerPulsed;
        public event Action<Vector2,float> AreaBlastUsed;
        readonly List<CueEvent> pendingCues=new List<CueEvent>();
        readonly List<CueEvent> cueWork=new List<CueEvent>();
        void RaiseCue(string id,Vector2 position){if(Cue!=null&&pendingCues.Count<256)pendingCues.Add(new CueEvent(id,position));}
        void FlushCues()
        {
            if(pendingCues.Count==0)return;
            cueWork.Clear();cueWork.AddRange(pendingCues);pendingCues.Clear();
            var cue=Cue;if(cue==null)return;
            foreach(var c in cueWork)cue(c);
            cueWork.Clear();
        }
        // Effect-tag numbers (effectTags.csv) read once; hot paths never search the table.
        struct TagCache
        {
            public float SlowMax,SlowMaxBoss,SlowBossScale,BurnTick,BurnIgnore,ChainRadius,ChainFalloff,PierceFalloff,ExecuteBase,CritBase,CritMul,CritCap,StunBossScale,StunImmune;public int BurnMax;
            public static TagCache From(CombatData d)=>new TagCache{
                SlowMax=d.Tag("Slow","maxPercent",false,60),SlowMaxBoss=d.Tag("Slow","maxPercent",true,30),SlowBossScale=d.Tag("Slow","effectScale",true,.5f),
                BurnMax=(int)d.Tag("Burn","maxStacks",false,5),BurnTick=d.Tag("Burn","tickSeconds",false,1),BurnIgnore=d.Tag("Burn","defenseIgnore",false,50),
                ChainRadius=d.Tag("Chain","radius",false,2),ChainFalloff=d.Tag("Chain","falloff",false,.7f),PierceFalloff=d.Tag("Pierce","falloff",false,.8f),
                ExecuteBase=d.Tag("Execute","baseThreshold",false,8),CritBase=d.Tag("Crit","baseChance",false,0),CritMul=d.Tag("Crit","baseMultiplier",false,2),CritCap=d.Tag("Crit","chanceCap",false,100),
                StunBossScale=d.Tag("Stun","durationScale",true,.25f),StunImmune=d.Tag("Stun","immuneSeconds",false,3)};
        }
        TagCache tags;
        // ---- random for crits (separate stream so card draws stay reproducible per seed)
        readonly System.Random combatRandom;
        // ---- flying route (generic polyline; diagonal segments allowed)
        readonly List<Vector2> flyPath=new List<Vector2>();readonly List<float> flyEnds=new List<float>();float flyLength;
        public bool HasFlyingRoute=>flyPath.Count>=2;
        public IReadOnlyList<Vector2> FlyingRoute=>flyPath;
        public void SetFlyingRoute(IEnumerable<Vector2> route)
        {
            flyPath.Clear();flyEnds.Clear();flyLength=0;
            foreach(var p in route)flyPath.Add(p);
            if(flyPath.Count<2){flyPath.Clear();return;}
            if(flyPath[flyPath.Count-1]!=(Vector2)core)throw new ArgumentException("Flying route must terminate at core");
            for(int i=1;i<flyPath.Count;i++){float len=Vector2.Distance(flyPath[i-1],flyPath[i]);if(len<=0)throw new ArgumentException("Zero length segment");flyLength+=len;flyEnds.Add(flyLength);}
        }
        void PositionFlying(EnemyState e)
        {
            float d=Mathf.Clamp(e.PathTravelled,0,flyLength);
            for(int i=0;i<flyEnds.Count;i++)
            {
                if(d<=flyEnds[i]){float start=i==0?0:flyEnds[i-1];float len=flyEnds[i]-start;e.Position=Vector2.Lerp(flyPath[i],flyPath[i+1],len<=0?1:(d-start)/len);e.PathRemaining=Mathf.Max(0,flyLength-e.PathTravelled);return;}
            }
            e.Position=flyPath[flyPath.Count-1];e.PathRemaining=0;
        }
        float RouteLength(EnemyState e)=>e.Route==1?flyLength:pathLength;

        // ================= Slow =================
        bool IsBoss(EnemyState e)=>e.Kind==EnemyKind.Boss;
        internal float SlowFraction(EnemyState e)
        {
            if(e==null||e.Spec==null)return 0;
            bool boss=IsBoss(e);
            float scale=boss?tags.SlowBossScale:1,cap=(boss?tags.SlowMaxBoss:tags.SlowMax)/100f;
            float keep=1;
            if(e.SlowFrostTime>0)keep*=1-e.SlowFrost*scale/100f;
            if(e.SlowRemaining>0)keep*=1-(e.SlowPlayer>0?e.SlowPlayer:20)*scale/100f;
            if(Augments.Has(15)&&e.PathTravelled<=Augments.Value(15)/Augments.Count(15))keep*=1-Augments.Value(15,1)/Augments.Count(15)*scale/100f;
            return Mathf.Clamp(1-keep,0,cap);
        }
        // Bosses count as reaching any "slowed by X%" condition once they hit their slow cap.
        bool SlowedAtLeast(EnemyState e,float percent)
        {
            float slow=SlowFraction(e)*100;if(slow<=0)return false;
            if(IsBoss(e))return slow+.001f>=tags.SlowMaxBoss;
            return slow+.001f>=percent;
        }
        void ApplyFrostSlow(EnemyState e,float percent,float seconds)
        {
            percent=Mathf.Min(percent,tags.SlowMax);
            // Same kind of slow: strongest wins; a hit refreshes the duration.
            if(e.SlowFrostTime<=0||percent>=e.SlowFrost)e.SlowFrost=percent;
            e.SlowFrostTime=Mathf.Max(e.SlowFrostTime,seconds);
        }

        // ================= Burn =================
        void AddBurn(EnemyState e,float perStack,float duration,int stacks,string source=null)
        {
            if(stacks<=0||perStack<=0||duration<=0||e.Health<=0)return;
            int max=tags.BurnMax;
            bool fresh=e.BurnStacks==0;if(fresh){e.BurnTick=0;e.BurnCarry=0;}
            e.BurnStacks=Mathf.Min(max,e.BurnStacks+stacks);e.BurnTime=duration;e.BurnPerStack=fresh?perStack:Mathf.Max(e.BurnPerStack,perStack);
            if(source!=null)e.BurnSource=source;
        }
        // Flame tower values with shared upgrades (불씨·불길 칸 use these).
        void FlameBurn(out float perStack,out float duration)
        {
            perStack=0;duration=0;
            if(!Data.Towers.TryGetValue(TowerKind.Flame,out var flame))return;
            perStack=flame.BurnPerStack;duration=flame.BurnDuration;
            foreach(var u in Data.Upgrades)if(u.Target=="Flame"){int n=TowerStep(TowerKind.Flame,UpgradeKey(u));if(u.Stat=="burnPerStack")perStack+=u.Delta*n;else if(u.Stat=="burnDuration")duration+=u.Delta*n;}
        }
        void TickBurn(EnemyHandle h,float dt)
        {
            var e=h.Slot;if(e.BurnStacks<=0)return;
            float tick=tags.BurnTick,ignore=tags.BurnIgnore/100f;
            e.BurnTime-=dt;e.BurnTick+=dt;
            while(e.BurnTick>=tick&&e.BurnStacks>0&&e.Health>0)
            {
                e.BurnTick-=tick;
                float raw=e.BurnPerStack*e.BurnStacks*(1+DamageIncreasePercent(e)/100f)*Mathf.Max(0,1-EffectiveDefense(e)*(1-ignore)/100f);
                e.BurnCarry+=raw;int whole=Mathf.FloorToInt(e.BurnCarry);e.BurnCarry-=whole;
                if(whole>0){e.BurnKill=true;DealDamage(h,whole,e.BurnSource??"Burn",raw,default);if(h.Accessible)e.BurnKill=false;}
            }
            if(e.BurnTime<=0){e.BurnStacks=0;e.BurnCarry=0;}
        }

        // ================= Stun / freeze =================
        bool ApplyStun(EnemyState e,float seconds)
        {
            if(e.StunTime>0||e.StunImmune>0||seconds<=0)return false;
            e.StunTime=seconds*(IsBoss(e)?tags.StunBossScale:1);return true;
        }
        void TickStun(EnemyHandle h,float dt)
        {
            var e=h.Slot;
            if(e.StunImmune>0)e.StunImmune=Mathf.Max(0,e.StunImmune-dt);
            if(e.StunTime<=0)return;
            e.StunTime-=dt;
            if(e.StunTime<=0){e.StunTime=0;e.StunImmune=tags.StunImmune;if(e.Frozen)Shatter(h);}
        }
        void Shatter(EnemyHandle h)
        {
            var e=h.Slot;if(!e.Frozen)return;e.Frozen=false;
            float damage=e.FrozenDamage*.5f;e.FrozenDamage=0;var at=e.Position;
            RaiseCue("shatter",at);
            if(damage<=0)return;
            float radius=Augments.Value(39,2)/Mathf.Max(1,Augments.Count(39));
            foreach(var other in Enemies)if(other!=h&&other.Alive&&Vector2.Distance(other.Position,at)<=radius)Strike(other,damage,"Shatter",default);
        }

        // ================= Damage pipeline =================
        float EffectiveDefense(EnemyState e)=>e.Spec.Defense+(e.ShieldTime>0?e.Spec.ShieldDefense:0);
        // 3-3 피해 증가: 21 동상, 37 위기 대응, 38 보스 사냥꾼, 50 유리 대포 (cards add together).
        float DamageIncreasePercent(EnemyState e)
        {
            float percent=0;
            if(Augments.Has(21)&&SlowedAtLeast(e,Augments.Value(21)/Augments.Count(21)))percent+=Augments.Value(21,1);
            if(Augments.Has(37)&&CoreEnergy<=CoreMaximum*Augments.Value(37)/Augments.Count(37)/100f)percent+=Augments.Value(37,1);
            if(Augments.Has(38)&&IsBoss(e))percent+=Augments.Value(38);
            if(Augments.Has(50))percent+=Augments.Value(50,1);
            return percent;
        }
        // Damage input after buff/crit/falloff; applies increase, defense, rounding, then on-hit effects.
        int Strike(EnemyHandle target,float damage,string source,HitPayload payload)
        {
            if(!target.Alive)return 0;
            var e=target.Slot;
            float scaled=damage*(1+DamageIncreasePercent(e)/100f);
            int dealt=DealDamage(target,CombatRules.Damage(scaled,EffectiveDefense(e)),source,scaled*Mathf.Max(0,1-EffectiveDefense(e)/100f),payload);
            if(!target.Alive)return dealt;
            if(payload.BurnStacks>0){AddBurn(e,payload.BurnPerStack,payload.BurnDuration,payload.BurnStacks,source);RaiseCue("hit_flame",e.Position);}
            if(payload.SlowPercent>0)
            {
                if(payload.Player){e.SlowPlayer=Mathf.Max(e.SlowPlayer,payload.SlowPercent);e.SlowRemaining=Mathf.Max(e.SlowRemaining,payload.SlowDuration);}
                else ApplyFrostSlow(e,payload.SlowPercent,payload.SlowDuration);
            }
            // 7 처형 (기술병), 27 얼음 감옥 (둔화된 적), 43 사신 (범위 폭발). Highest threshold only. Bosses immune.
            float threshold=payload.Execute;
            if(Augments.Has(27)&&SlowFraction(e)>0)threshold=threshold>0?threshold+Augments.Value(27):Augments.Value(27);
            if(threshold>0&&!IsBoss(e)&&e.Health>0&&e.Health<=e.Spec.Health*threshold/100f)
            {
                RaiseCue("execute",e.Position);
                DealDamage(target,e.Health,source,e.Health,payload);
                if(Augments.Has(25))areaBlastCooldown=Mathf.Max(0,areaBlastCooldown-Augments.Value(25));
            }
            // 39 절대 영도
            if(target.Alive&&Augments.Has(39)&&!e.Frozen&&SlowedAtLeast(e,Augments.Value(39)/Augments.Count(39)))
            {
                float seconds=Augments.Value(39,1)/Augments.Count(39);
                if(ApplyStun(e,seconds)){e.Frozen=true;e.FrozenDamage=0;RaiseCue("freeze",e.Position);}
            }
            return dealt;
        }
        // Integer health change, totals, kill handling. "raw" is the pre-rounding value used for overkill.
        int DealDamage(EnemyHandle target,int amount,string source,float raw,HitPayload payload)
        {
            if(!target.Alive||amount<=0)return 0;
            var e=target.Slot;int before=e.Health;int actual=Mathf.Min(before,amount);
            enemyRevision++;e.Health-=actual;
            DamageTotals.TryGetValue(source,out int previous);DamageTotals[source]=previous+actual;
            if(e.Frozen)e.FrozenDamage+=actual;
            if(e.Health>0)CheckShield(e,before);
            if(actual>0)DamageApplied?.Invoke(new EnemySnapshot(target),actual);
            if(e.Health==0&&!e.Exited)OnKilled(target,source,raw-before,payload);
            return actual;
        }
        void CheckShield(EnemyState e,int before)
        {
            var thresholds=e.Spec.ShieldAt;if(thresholds.Length==0)return;
            bool deploy=false;
            for(int i=0;i<thresholds.Length;i++)
            {
                if((e.ShieldUsed&(1<<i))!=0)continue;
                float limit=e.Spec.Health*thresholds[i]/100f;
                if(before>limit&&e.Health<=limit){e.ShieldUsed|=1<<i;deploy=true;}
            }
            // Crossing two thresholds in one hit deploys once; both are consumed.
            if(deploy){e.ShieldTime=e.Spec.ShieldSeconds;RaiseCue("shield_deploy",e.Position);}
        }
        void OnKilled(EnemyHandle target,string source,float overkill,HitPayload payload)
        {
            var e=target.Slot;
            if(e.SummonerId==0)
            {
                int gold=Mathf.Max(0,Mathf.FloorToInt(e.Spec.Gold*(1+Augments.KillGoldPercent/100f)+.5f));
                Gold+=gold;LastMessage="+"+gold+" 골드";
            }
            Augments.Kills++;
            if(IsBoss(e)){BossKilled=true;Augments.BossKills++;if(Augments.Has(31))Player.Damage+=Augments.Value(31);}
            if(payload.Player||source=="Player"){Augments.PlayerKills++;if(Augments.Has(9))Player.Damage+=Augments.Value(9);}
            if(e.Frozen)Shatter(target);
            // Splitter: children spawn where it fell, 0.3 tiles ahead and behind on the same route.
            if(e.Spec.SplitIntoId!=0&&e.Spec.SplitCount>0)
                for(int i=0;i<e.Spec.SplitCount;i++)
                    pendingSpawns.Add(new PendingSpawn{Spec=SplitChildSpec(e.Spec),Distance=e.PathTravelled+(i%2==0?.3f:-.3f),Route=e.Route});
            if(e.Spec.SplitIntoId!=0)RaiseCue("split",e.Position);
            int max=tags.BurnMax;
            // 40 업화: max-stack burn victims explode.
            if(Augments.Has(40)&&e.BurnStacks>=max)
            {
                RaiseCue("burn_explode",e.Position);
                float radius=Augments.Value(40)/Augments.Count(40);int stacks=(int)Augments.Value(40,1);
                foreach(var other in Enemies)if(other!=target&&other.Alive&&Vector2.Distance(other.Position,e.Position)<=radius)AddBurn(other.Slot,e.BurnPerStack,Mathf.Max(e.BurnTime,1),stacks);
            }
            // 28 번지는 불: a burn death moves its stacks to the nearest enemy (fresh duration).
            if(Augments.Has(28)&&e.BurnKill&&e.BurnStacks>0)
            {
                var near=Nearest(e.Position,float.PositiveInfinity,target,null);
                if(near!=default){FlameBurn(out _,out float duration);AddBurn(near.Slot,e.BurnPerStack,duration>0?duration:3,e.BurnStacks);}
            }
            // 42 연쇄 폭풍: leftover damage bounces once.
            if(Augments.Has(42)&&!payload.NoBounce&&overkill>0)
            {
                var near=Nearest(e.Position,tags.ChainRadius,target,null);
                if(near!=default){var bounce=payload;bounce.NoBounce=true;bounce.Chain=0;RaiseCue("chain",near.Position);Strike(near,overkill,source,bounce);}
            }
        }
        EnemyHandle Nearest(Vector2 from,float radius,EnemyHandle except,List<int> exclude)
        {
            EnemyHandle best=default;float bestDistance=float.PositiveInfinity;
            foreach(var other in Enemies)
            {
                if(other==except||!other.Alive||exclude!=null&&exclude.Contains(other.Id))continue;
                float d=Vector2.Distance(other.Position,from);
                if(d<=radius&&(d<bestDistance||d==bestDistance&&best!=default&&other.Id<best.Id)){best=other;bestDistance=d;}
            }
            return best;
        }
        readonly List<int> chainHits=new List<int>(8);
        // Hit plus instant chain bounces (반지름 2, ×0.7, 24번이면 ×1).
        int HitWithChain(EnemyHandle target,float damage,string source,HitPayload payload)
        {
            var at=target.Position;int id=target.Id;
            int dealt=Strike(target,damage,source,payload);
            if(payload.Chain<=0)return dealt;
            chainHits.Clear();chainHits.Add(id);
            float radius=tags.ChainRadius,falloff=payload.ChainFalloff;
            var next=payload;next.Chain=0;
            for(int i=0;i<payload.Chain;i++)
            {
                var near=Nearest(at,radius,default,chainHits);if(near==default)break;
                damage*=falloff;chainHits.Add(near.Id);at=near.Position;RaiseCue("chain",at);
                dealt+=Strike(near,damage,source,next);
            }
            return dealt;
        }

        // ================= Spawns (split children, summons) =================
        struct PendingSpawn{public EnemySpec Spec;public float Distance;public int Route,Summoner;}
        readonly List<PendingSpawn> pendingSpawns=new List<PendingSpawn>();
        readonly Dictionary<long,EnemySpec> roundSpecs=new Dictionary<long,EnemySpec>();
        int roundSpecWave=-1;
        EnemySpec RoundAttack()=>Data.EnemyForWave(Wave,EnemyKind.Attack);
        EnemySpec SplitChildSpec(EnemySpec parent)
        {
            if(roundSpecWave!=Wave){roundSpecs.Clear();roundSpecWave=Wave;}
            long key=((long)parent.SplitIntoId<<20)^(parent.Id&0xFFFFF);
            if(roundSpecs.TryGetValue(key,out var cached))return cached;
            var baseChild=Data.EnemiesById[parent.SplitIntoId];var attack=RoundAttack();var attackBase=Data.Enemies[EnemyKind.Attack];
            var child=baseChild.Clone();
            float growth=attackBase.Speed>0?attack.Speed/attackBase.Speed:1;
            if(baseChild.HealthMul>0)child.Health=Mathf.Max(1,Mathf.FloorToInt(attack.Health*baseChild.HealthMul+.5f));
            if(baseChild.GoldMul>0)child.Gold=Mathf.FloorToInt(attack.Gold*baseChild.GoldMul+.5f);
            child.Defense=parent.Defense;child.Speed=baseChild.Speed*growth;
            roundSpecs[key]=child;return child;
        }
        EnemySpec SummonSpec(EnemySpec lord)
        {
            if(roundSpecWave!=Wave){roundSpecs.Clear();roundSpecWave=Wave;}
            long key=((long)lord.SummonId<<20)^0xFFFFF;
            if(roundSpecs.TryGetValue(key,out var cached))return cached;
            var kind=Data.EnemiesById[lord.SummonId].Kind;
            var spec=Data.EnemyForWave(Wave,kind).Clone();spec.Gold=0;
            roundSpecs[key]=spec;return spec;
        }
        void FlushSpawns()
        {
            if(pendingSpawns.Count==0)return;
            var list=pendingSpawns.ToArray();pendingSpawns.Clear();
            foreach(var p in list)
            {
                var h=RentEnemy(p.Spec.Kind,p.Spec,p.Distance,p.Route);
                h.Slot.SummonerId=p.Summoner;
            }
        }

        // ================= Enemy abilities =================
        void TickAbilities(EnemyHandle h,float dt)
        {
            var e=h.Slot;var spec=e.Spec;
            if(e.ShieldTime>0)e.ShieldTime=Mathf.Max(0,e.ShieldTime-dt);
            if(e.SlowFrostTime>0)e.SlowFrostTime=Mathf.Max(0,e.SlowFrostTime-dt);
            if(spec.HealInterval>0)
            {
                e.AbilityTimer+=dt;
                if(e.AbilityTimer>=spec.HealInterval)
                {
                    e.AbilityTimer-=spec.HealInterval;bool any=false;
                    foreach(var other in Enemies)
                    {
                        if(other==h||!other.Alive)continue;var o=other.Slot;
                        if(Vector2.Distance(o.Position,e.Position)>spec.HealRadius)continue;
                        // Overlapping healers each heal (no per-target limit, 사용자 확정 10-27).
                        float percent=spec.HealPercent*(o.BurnStacks>0?.5f:1)*(IsBoss(o)?.5f:1);
                        int amount=Mathf.FloorToInt(o.Spec.Health*percent/100f+.5f);
                        if(amount<=0||o.Health>=o.Spec.Health)continue;
                        enemyRevision++;o.Health=Mathf.Min(o.Spec.Health,o.Health+amount);any=true;
                    }
                    if(any)RaiseCue("heal_pulse",e.Position);
                }
            }
            if(spec.SummonId!=0&&spec.SummonCount>0)
            {
                e.AbilityTimer+=dt;
                float interval=h.Enraged?spec.SummonRageInterval:spec.SummonInterval;
                if(e.AbilityTimer>=interval)
                {
                    e.AbilityTimer-=interval;
                    // No cap on living summons (사용자 확정 10-27): every cycle summons the full count.
                    {
                        var summoned=SummonSpec(spec);
                        for(int i=0;i<spec.SummonCount;i++)pendingSpawns.Add(new PendingSpawn{Spec=summoned,Distance=e.PathTravelled+(i%2==0?.3f:-.3f),Route=e.Route,Summoner=e.Id});
                        RaiseCue("summon",e.Position);
                    }
                }
            }
        }

        // ================= 22 불길 발자국 =================
        readonly Dictionary<Vector2Int,float> fireCells=new Dictionary<Vector2Int,float>();
        readonly List<Vector2Int> fireWork=new List<Vector2Int>();
        float fireTick;
        public Dictionary<Vector2Int,float>.Enumerator FireCellEnumerator()=>fireCells.GetEnumerator();
        public int FireCellCount=>fireCells.Count;
        public void NotifyPlayerStep(Vector2Int left)
        {
            if(!Augments.Has(22)||Phase!=SessionPhase.Combat)return;
            float seconds=Augments.Value(22)/Augments.Count(22);
            foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right})
            {var c=left+d;if(pathCells.Contains(c))fireCells[c]=seconds;}
        }
        void TickFire(float dt)
        {
            if(fireCells.Count==0){fireTick=0;return;}
            fireWork.Clear();fireWork.AddRange(fireCells.Keys);
            foreach(var c in fireWork){float t=fireCells[c]-dt;if(t<=0)fireCells.Remove(c);else fireCells[c]=t;}
            fireTick+=dt;if(fireTick<1)return;fireTick-=1;
            FlameBurn(out float perStack,out float duration);if(perStack<=0)return;
            int stacks=(int)Mathf.Max(1,Augments.Value(22,1)/Mathf.Max(1,Augments.Count(22)));
            foreach(var enemy in Enemies)if(enemy.Alive&&enemy.Slot.Route==0&&fireCells.ContainsKey(Vector2Int.RoundToInt(enemy.Position)))AddBurn(enemy.Slot,perStack,duration,stacks);
        }

        // ================= Area blast (32001) =================
        float areaBlastCooldown;
        public SkillSpec AreaBlast=>Data.Skills.Count>0?Data.Skills[0]:null;
        public float AreaBlastCooldown=>areaBlastCooldown;
        public bool AreaBlastReady=>AreaBlast!=null&&Phase==SessionPhase.Combat&&areaBlastCooldown<=0;
        public bool UseAreaBlast(Vector2 playerPosition)
        {
            var skill=AreaBlast;if(skill==null||Phase!=SessionPhase.Combat||areaBlastCooldown>0)return false;
            BeginModelOperation();
            try
            {
                areaBlastCooldown=skill.Cooldown;
                float damage=Player.Damage*skill.DamageMul;
                var payload=new HitPayload{Player=true,Skill=true};
                if(Augments.Has(43))payload.Execute=tags.ExecuteBase*Augments.Value(43)/Augments.Count(43);
                blastWork.Clear();foreach(var e in Enemies)if(e.Alive&&Vector2.Distance(e.Position,playerPosition)<=skill.Radius)blastWork.Add(e);
                foreach(var e in blastWork)
                {
                    float crit=RollCrit(ref playerAttacks,ref playerCritBonus);
                    if(crit>1)RaiseCue("crit",e.Position);
                    Strike(e,damage*crit,"PlayerSkill",payload);
                }
                RaiseCue("area_blast",playerPosition);
                AreaBlastUsed?.Invoke(playerPosition,skill.Radius);
                FlushSpawns();RemoveDeadEnemies();
            }
            finally{EndModelOperation();FlushCues();}
            return true;
        }
        readonly List<EnemyHandle> blastWork=new List<EnemyHandle>();

        // ================= Crit =================
        int playerAttacks;float playerCritBonus;
        // Rolled once per attack at launch. Over-cap chance adds to the multiplier (130% → ×2.3).
        float RollCrit(ref int attacks,ref float bonus)
        {
            attacks++;
            float chance=tags.CritBase+Augments.CritChance+bonus;bonus=0;
            bool forced=Augments.Has(44)&&attacks%(int)Mathf.Max(1,Augments.Value(44)/Augments.Count(44))==0;
            if(chance<=0&&!forced)return 1;
            bool crit=forced||combatRandom.NextDouble()*100<Mathf.Min(chance,tags.CritCap);
            if(!crit)return 1;
            if(Augments.Has(26))bonus=Augments.Value(26,1)/Augments.Count(26);
            return tags.CritMul+Augments.CritBonusPercent/100f+Mathf.Max(0,chance-100)/100f;
        }
    }
}
