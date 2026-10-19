using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CoreGuardian
{
    public sealed class Session
    {
        public readonly CombatData Data;
        public readonly List<EnemyState> Enemies=new List<EnemyState>();
        public readonly List<TowerState> Towers=new List<TowerState>();
        public readonly List<ProjectileState> Projectiles=new List<ProjectileState>();
        public readonly Dictionary<string,int> DamageTotals=new Dictionary<string,int>();
        public event Action<EnemyState,int> DamageApplied;
        // Raised at creation, before movement/hits can remove a projectile in the same Tick.
        public event Action<ProjectileState> ProjectileLaunched;
        // Presentation event: exactly one notification per attack, after all projectiles exist.
        public event Action<string,int> AttackLaunched;
        public event Action DefeatReached;
        public event Action CombatStarted;
        public event Action RoundCompleted;
        public event Action VictoryReached;
        public EnemyState PlayerTarget => playerTarget;
        public EnemyState TowerTarget(int id)
        {
            var tower=Towers.FirstOrDefault(t=>t.Id==id);
            if(tower==null)return null;
            // Multi fires at its role-priority list, not the single-target acquisition cache.
            return tower.Kind==TowerKind.Multi?CombatRules.Priority(Enemies.Where(e=>e.Alive&&Vector2.Distance(e.Position,tower.Position)<=tower.Stats.Range)):tower.Target;
        }
        public CombatStats Player {get;private set;}
        public int Gold {get;private set;} public int Crystals {get;private set;} public int CoreEnergy {get;private set;}
        public SessionPhase Phase {get;private set;} public int Wave {get;private set;} public float PreparationRemaining {get;private set;}
        public string LastMessage {get;private set;} public bool Diagnostic {get;private set;}
        public int LastWave {get;private set;}
        public int CompletedWave {get;private set;}
        public int LastCompletionReward {get;private set;}
        public int CoreMaximum {get;private set;}
        public bool BossKilled {get;private set;}
        public float AugmentationRemaining {get;private set;}
        public AugmentationEffects Augments {get;private set;} = new AugmentationEffects();
        AugmentationCard[] choices=Array.Empty<AugmentationCard>();
        public IReadOnlyList<AugmentationCard> AugmentationChoices => Array.AsReadOnly(choices);
        readonly System.Random augmentationRandom;
        bool coreDamagedThisRound;
        readonly List<Vector2> path=new List<Vector2>(); readonly List<float> lengths=new List<float>(); readonly HashSet<Vector2Int> pathCells=new HashSet<Vector2Int>();
        Vector2Int core;int width,height,nextEnemy=1,nextTower=1,nextProjectile=1;float pathLength,waveTime,playerCooldown;EnemyState playerTarget;bool playerAcquired;
        readonly Dictionary<WaveSpec,int> spawned=new Dictionary<WaveSpec,int>();
        public Session(CombatData data, int? seed=null) { Data=data??throw new ArgumentNullException(nameof(data));augmentationRandom=seed.HasValue?new System.Random(seed.Value):new System.Random();Player=data.Player.Clone();Gold=data.StartGold;Crystals=data.StartCrystals;CoreEnergy=CoreMaximum=data.CoreEnergy;PreparationRemaining=data.PreparationSeconds;Phase=SessionPhase.Preparation;Wave=1;LastWave=data.Waves.Select(x=>x.Wave).DefaultIfEmpty(1).Max();LastMessage="준비"; }
        public void SetBoard(int width,int height,Vector2Int core,IEnumerable<Vector2Int> route)
        {
            if(width<=0||height<=0)throw new ArgumentException("Invalid board");this.width=width;this.height=height;this.core=core;path.Clear();lengths.Clear();pathCells.Clear();pathLength=0;
            foreach(var p in route) { if(p.x<0||p.y<0||p.x>=width||p.y>=height)throw new ArgumentException("Path outside board");path.Add(p); }
            if(path.Count<2||path[path.Count-1]!=(Vector2)core)throw new ArgumentException("Path must terminate at core");
            for(int i=1;i<path.Count;i++) {var a=Vector2Int.RoundToInt(path[i-1]);var b=Vector2Int.RoundToInt(path[i]);if(a.x!=b.x&&a.y!=b.y)throw new ArgumentException("Path must be axial");float len=Vector2.Distance(a,b);if(len<=0)throw new ArgumentException("Zero length segment");lengths.Add(len);pathLength+=len;var dir=new Vector2Int(Math.Sign(b.x-a.x),Math.Sign(b.y-a.y));for(var p=a;p!=b;p+=dir)pathCells.Add(p);pathCells.Add(b);}
        }
        public bool IsBuildable(Vector2Int cell) {return cell.x>=0&&cell.y>=0&&cell.x<width&&cell.y<height&&!pathCells.Contains(cell)&&cell!=core&&!Towers.Any(t=>t.Position==cell);}
        public bool CanInstall(TowerKind kind,Vector2Int cell,out string reason)
        {
            reason=null;if(Phase!=SessionPhase.Preparation)reason="준비 시간에만 설치 가능";else if(!IsBuildable(cell))reason="설치 불가 칸";else if(Crystals<Data.Towers[kind].Cost)reason="수정 부족";else if(kind==TowerKind.Support&&!Towers.Any(t=>t.Kind!=TowerKind.Support))reason="공격 포탑을 먼저 설치";return reason==null;
        }
        public TowerState TryInstall(TowerKind kind,Vector2Int cell,out string reason)
        {
            if(!CanInstall(kind,cell,out reason))return null;var s=Data.Towers[kind];var t=new TowerState{Id=nextTower++,Kind=kind,Position=cell,Spent=s.Cost,Stats=new CombatStats{Damage=s.Damage,Rate=s.Rate,Range=s.Range,ProjectileSpeed=s.ProjectileSpeed,BuffDamage=s.BuffDamage}};Crystals-=s.Cost;Towers.Add(t);LastMessage="포탑 설치";return t;
        }
        public bool DestroyTower(int id) {if(Phase==SessionPhase.Augmentation)return false;var t=Towers.FirstOrDefault(x=>x.Id==id);if(t==null)return false;Towers.Remove(t);int refund=t.Spent/2;Crystals+=refund;LastMessage="철거 +"+refund+" 수정";return true;}
        UpgradeSpec FindUpgrade(string target,string stat) {return Data.Upgrades.FirstOrDefault(u=>u.Target==target&&u.Stat==stat);}
        static int Step(CombatStats s,string stat) {int n;return s.UpgradeSteps.TryGetValue(stat,out n)?n:0;}
        public int PlayerUpgradePrice(string stat) {return FindUpgrade("Player",stat)==null?-1:Data.PlayerUpgradePrice(Step(Player,stat)+1);}
        public int TowerUpgradePrice(int id,string stat) {var t=Towers.FirstOrDefault(x=>x.Id==id);if(t==null)return -1;var u=FindUpgrade(t.Kind.ToString(),stat);return u==null?-1:u.Cost+u.CostIncrement*Step(t.Stats,stat)-(Step(t.Stats,stat)==0?Data.TowerFirstUpgradeDiscount:0);}
        public bool UpgradePlayer(string stat,out string reason) {var u=FindUpgrade("Player",stat);reason=null;if(u==null)reason="강화 불가";else if(Phase!=SessionPhase.Preparation&&Phase!=SessionPhase.Combat)reason="현재 단계에서 강화 불가";else if(Gold<PlayerUpgradePrice(stat))reason="골드 부족";if(reason!=null)return false;Gold-=PlayerUpgradePrice(stat);Player.Add(stat,u.Delta);Player.UpgradeSteps[stat]=Step(Player,stat)+1;LastMessage="기술병 강화";return true;}
        public bool UpgradeTower(int id,string stat,out string reason) {var t=Towers.FirstOrDefault(x=>x.Id==id);var u=t==null?null:FindUpgrade(t.Kind.ToString(),stat);reason=null;if(Phase!=SessionPhase.Preparation)reason="준비 시간에만 강화 가능";else if(u==null)reason="강화 불가";else if(Crystals<TowerUpgradePrice(id,stat))reason="수정 부족";if(reason!=null)return false;int cost=TowerUpgradePrice(id,stat);Crystals-=cost;t.Spent+=cost;t.Stats.Add(stat,u.Delta);t.Stats.UpgradeSteps[stat]=Step(t.Stats,stat)+1;LastMessage="포탑 강화";return true;}
        public bool StartCombat()
        {
            if(Phase!=SessionPhase.Preparation||path.Count<2)return false;
            Phase=SessionPhase.Combat;PreparationRemaining=0;waveTime=0;spawned.Clear();DamageTotals.Clear();
            BossKilled=false;coreDamagedThisRound=false;
            playerTarget=null;playerAcquired=false;playerCooldown=0;
            foreach(var tower in Towers){tower.Target=null;tower.AcquiredOnce=false;tower.Cooldown=0;}
            LastMessage=Diagnostic?"역할 검증 전투 시작":Wave+"라운드 전투 시작";CombatStarted?.Invoke();return true;
        }
        public void StartDiagnosticEncounter()
        {
            if(path.Count<2)throw new InvalidOperationException("Board missing");Diagnostic=true;Wave=1;CompletedWave=0;LastCompletionReward=0;waveTime=0;spawned.Clear();DamageTotals.Clear();Enemies.Clear();Projectiles.Clear();Towers.Clear();Gold=Data.DiagnosticGold;Crystals=Data.DiagnosticCrystals;CoreEnergy=Data.CoreEnergy;Phase=SessionPhase.Preparation;PreparationRemaining=Data.PreparationSeconds;playerTarget=null;playerAcquired=false;playerCooldown=0;Player=Data.Player.Clone();LastMessage="역할 검증 모드: 준비 후 시작";
            CoreMaximum=Data.CoreEnergy;Augments=new AugmentationEffects();BossKilled=false;choices=Array.Empty<AugmentationCard>();AugmentationRemaining=0;coreDamagedThisRound=false;
        }
        public EnemyState DebugSpawn(EnemyKind kind,float distance=0) {if(path.Count<2)throw new InvalidOperationException("Board missing");var spec=Data.Enemies[kind];var e=new EnemyState{Id=nextEnemy++,Kind=kind,Spec=spec,Health=spec.Health,PathTravelled=Mathf.Clamp(distance,0,pathLength)};PositionEnemy(e);Enemies.Add(e);return e;}
        void PositionEnemy(EnemyState e) {float d=e.PathTravelled;for(int i=0;i<lengths.Count;i++) {if(d<=lengths[i]){e.Position=Vector2.Lerp(path[i],path[i+1],d/lengths[i]);e.PathRemaining=Mathf.Max(0,pathLength-e.PathTravelled);return;}d-=lengths[i];}e.Position=path[path.Count-1];e.PathRemaining=0;}
        public void Tick(float dt,Vector2 playerPosition)
        {
            if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<0)throw new ArgumentException("Invalid dt");while(dt>0) {float step=Mathf.Min(dt,.05f);StepTick(step,playerPosition);dt-=step;}
        }
        void StepTick(float dt,Vector2 playerPosition)
        {
            if(Phase==SessionPhase.Augmentation){AugmentationRemaining=Mathf.Max(0,AugmentationRemaining-dt);if(AugmentationRemaining<=.0001f)ChooseAugmentation(AugmentationDeck.BestIndex(choices));return;}
            if(Phase==SessionPhase.Preparation) {PreparationRemaining=Mathf.Max(0,PreparationRemaining-dt);if(PreparationRemaining<=0)StartCombat();return;}if(Phase!=SessionPhase.Combat)return;
            var rows=Data.Waves.Where(x=>x.Wave==Wave).ToArray();
            if(Diagnostic) {if(waveTime==0) {float diagnosticDistance=Mathf.Min(Data.DiagnosticStartDistance,pathLength*.2f);DebugSpawn(EnemyKind.Attack,diagnosticDistance);DebugSpawn(EnemyKind.Defense,diagnosticDistance+Data.DiagnosticSpacing);DebugSpawn(EnemyKind.Buff,diagnosticDistance+2*Data.DiagnosticSpacing);} }
            else foreach(var row in rows) {int count;spawned.TryGetValue(row,out count);while(count<row.Count&&waveTime+.00001f>=row.Start+count*row.Interval) {DebugSpawn(row.Kind);count++;}spawned[row]=count;}
            waveTime+=dt;
            // Calculate all auras before moving anyone so source order cannot alter the radius test.
            var boosts=Enemies.Where(e=>e.Alive).ToDictionary(e=>e,e=>Enemies.Where(other=>other!=e&&other.Alive&&other.Kind==EnemyKind.Buff&&Vector2.Distance(e.Position,other.Position)<=other.Spec.BuffRadius).Select(other=>other.Spec.BuffSpeed).DefaultIfEmpty(0).Max()); foreach(var e in boosts.Keys) e.Buffed=boosts[e]>0;
            foreach(var e in Enemies.Where(e=>e.Alive).ToArray()) {float boost=boosts[e];e.PathTravelled=Mathf.Min(pathLength,e.PathTravelled+e.MovementSpeed*(1+boost/100f)*dt);e.SlowRemaining=Mathf.Max(0,e.SlowRemaining-dt);PositionEnemy(e);if(e.PathRemaining<=.00001f){e.Exited=true;coreDamagedThisRound|=e.Spec.CoreDamage>0;CoreEnergy=Mathf.Max(0,CoreEnergy-e.Spec.CoreDamage);LastMessage="코어 -"+e.Spec.CoreDamage;}}
            if(CoreEnergy<=0) {Phase=SessionPhase.Defeat;Projectiles.Clear();LastMessage="방어 실패";if(!Diagnostic)DefeatReached?.Invoke();return;}
            Shoot(playerPosition,Player,"Player",0,ref playerCooldown,ref playerTarget,ref playerAcquired,dt,false);
            foreach(var t in Towers.ToArray()) {if(t.Kind==TowerKind.Support)continue;float buff=Augments.TowerBuffPercent+Towers.Where(s=>s.Kind==TowerKind.Support&&Vector2.Distance(s.Position,t.Position)<=s.Stats.Range).Sum(s=>s.Stats.BuffDamage);Shoot(t.Position,t.Stats,"Tower:"+t.Id,buff,ref t.Cooldown,ref t.Target,ref t.AcquiredOnce,dt,t.Kind==TowerKind.Multi);}
            foreach(var p in Projectiles.ToArray()) {if(!p.Target.Alive){Projectiles.Remove(p);continue;}p.Position=Vector2.MoveTowards(p.Position,p.Target.Position,p.Speed*dt);if(Vector2.Distance(p.Position,p.Target.Position)<=.00001f){ApplyDamage(p.Target,p.Damage,p.Source);Projectiles.Remove(p);}}
            Enemies.RemoveAll(e=>!e.Alive);
            bool complete=Diagnostic||rows.Length>0&&rows.All(r=>spawned.ContainsKey(r)&&spawned[r]>=r.Count);
            if(complete&&Enemies.Count==0)CompleteRound();
        }
        void CompleteRound()
        {
            Projectiles.Clear();
            if(Diagnostic){Phase=SessionPhase.Victory;LastMessage="역할 검증 종료";return;}
            CompletedWave=Wave;LastCompletionReward=Data.RewardForWave(Wave);Crystals+=LastCompletionReward;
            if(!coreDamagedThisRound){Gold+=Augments.FlawlessGold;Crystals+=Augments.FlawlessCrystals;}
            LastMessage=CompletedWave+"라운드 완료 +"+LastCompletionReward+" 수정";
            if(Wave>=LastWave){Phase=SessionPhase.Victory;LastMessage+=" · 1~"+LastWave+"라운드 검증 완료";VictoryReached?.Invoke();return;}
            if(!Data.Waves.Any(row=>row.Wave==Wave+1))throw new InvalidOperationException("Next wave is missing");
            Wave++;PreparationRemaining=Data.PreparationSeconds;
            if(CompletedWave%10==0&&CompletedWave<100&&BossKilled){choices=AugmentationDeck.Draw(augmentationRandom,Augments.Selected);AugmentationRemaining=30;Phase=SessionPhase.Augmentation;LastMessage+=" · 증강 선택";RoundCompleted?.Invoke();return;}
            Phase=SessionPhase.Preparation;
            LastMessage+=" · "+Wave+"라운드 준비";
            RoundCompleted?.Invoke();
        }
        public bool ChooseAugmentation(int index)
        {
            if(Phase!=SessionPhase.Augmentation||index<0||index>=choices.Length)return false;
            var card=choices[index];var grant=Augments.Apply(card);
            Gold+=grant.Gold;Crystals+=grant.Crystals;CoreMaximum+=grant.Core;CoreEnergy+=grant.Core;Player.Damage+=grant.PlayerDamage;
            choices=Array.Empty<AugmentationCard>();AugmentationRemaining=0;Phase=SessionPhase.Preparation;PreparationRemaining=Data.PreparationSeconds;
            LastMessage=card.GradeName+" · "+card.Title+" 선택 · "+Wave+"라운드 준비";return true;
        }
        void Shoot(Vector2 position,CombatStats stats,string source,float buff,ref float cooldown,ref EnemyState target,ref bool acquired,float dt,bool multi)
        {
            var available=Enemies.Where(e=>e.Alive&&Vector2.Distance(e.Position,position)<=stats.Range).ToArray();
            if(target!=null&&(!target.Alive||Vector2.Distance(target.Position,position)>stats.Range))target=null;
            if(target==null&&available.Length>0) {target=acquired?CombatRules.Priority(available):available.OrderBy(e=>e.Id).First();acquired=true;}
            cooldown-=dt;if(available.Length==0||stats.Rate<=0){cooldown=Mathf.Max(0,cooldown);return;}if(cooldown>0)return;
            var targets=multi?available.OrderBy(e=>e.Kind==EnemyKind.Defense?0:e.Kind==EnemyKind.Buff?1:2).ThenBy(e=>e.Health).ThenBy(e=>e.PathRemaining).ThenBy(e=>e.Id).Take(3).ToArray():new[]{target};
            while(cooldown<=.00001f) {int launched=0;foreach(var e in targets) {if(e==null)continue;int shots=source=="Player"?1+Augments.ExtraPlayerProjectiles:1;for(int shot=0;shot<shots;shot++) {var projectile=new ProjectileState{Id=nextProjectile++,Position=position,Target=e,Source=source,Damage=stats.Damage*(1+buff/100f),Speed=stats.ProjectileSpeed};Projectiles.Add(projectile);launched++;ProjectileLaunched?.Invoke(projectile);}}if(launched>0)AttackLaunched?.Invoke(source,launched);float nextCooldown=cooldown+1/stats.Rate;if(nextCooldown<=cooldown)throw new InvalidOperationException("Attack rate exceeds floating-point timing precision");cooldown=nextCooldown;}
        }
        public int ApplyDamage(EnemyState target,float damage,string source)
        {
            if(target==null||!target.Alive)return 0;int actual=Math.Min(target.Health,CombatRules.Damage(damage,target.Spec.Defense));target.Health-=actual;int previous;DamageTotals.TryGetValue(source,out previous);DamageTotals[source]=previous+actual;if(actual>0&&source=="Player"&&Augments.PlayerSlow)target.SlowRemaining=1;if(target.Health==0){Gold+=target.Spec.Gold;if(target.Kind==EnemyKind.Boss)BossKilled=true;LastMessage="+"+target.Spec.Gold+" 골드";}if(actual>0)DamageApplied?.Invoke(target,actual);return actual;
        }
    }
}


