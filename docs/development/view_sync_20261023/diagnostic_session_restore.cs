using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CoreGuardian
{
    public sealed partial class Session : IDisposable
    {
        public readonly CombatData Data;
        public readonly ModelList<EnemyHandle> Enemies=new ModelList<EnemyHandle>();
        public readonly List<TowerState> Towers=new List<TowerState>();
        public readonly ModelList<ProjectileHandle> Projectiles=new ModelList<ProjectileHandle>();
        public readonly Dictionary<string,int> DamageTotals=new Dictionary<string,int>();
        public event Action<EnemySnapshot,int> DamageApplied;
        // Raised at creation, before movement/hits can remove a projectile in the same Tick.
        public event Action<ProjectileSnapshot> ProjectileLaunched;
        // Presentation event: exactly one notification per attack, after all projectiles exist.
        public event Action<string,int> AttackLaunched;
        public event Action DefeatReached;
        public event Action CombatStarted;
        public event Action RoundCompleted;
        public event Action VictoryReached;
        public EnemyHandle PlayerTarget => playerTarget;
        public EnemyHandle TowerTarget(int id)
        {
            var tower=FindTower(id);
            if(tower==null)return default;
            return TowerTarget(tower,null);
        }
        EnemyHandle TowerTarget(TowerState tower,EnemyRangeIndex index)
        {
            // Multi fires at its role-priority list, not the single-target acquisition cache.
            if(tower.Kind!=TowerKind.Multi)return tower.Target;
            EnemyHandle best=default;
            var work=shotWorkPool.Count>0?shotWorkPool.Pop():new ShotWork();
            try
            {
                if(index==null||!index.TryCollect(tower.Position,tower.Stats.Range,Enemies,work.Available,work.SpatialCandidates))
                    foreach(var enemy in Enemies)if(enemy.Alive&&Vector2.Distance(enemy.Position,tower.Position)<=tower.Stats.Range)work.Available.Add(enemy);
                foreach(var enemy in work.Available)if(best==default||ComparePriority(enemy,best,true)<0)best=enemy;
                return best;
            }
            finally{work.Clear();if(shotWorkPool.Count<MaxRetainedWorkBuffers)shotWorkPool.Push(work);}
        }
        readonly EnemyRangeIndex viewRangeIndex=new EnemyRangeIndex();
        bool towerViewsPrepared;
        public void BeginTowerViews()
        {
            towerViewsPrepared=false;
            foreach(var tower in Towers)if(tower.Kind==TowerKind.Multi){viewRangeIndex.Build(Enemies);towerViewsPrepared=true;break;}
        }
        public void EndTowerViews(){towerViewsPrepared=false;viewRangeIndex.Clear();}
        public EnemyHandle TowerTargetForView(TowerState tower)
        {
            tower=FindTower(tower.Id);if(tower==null)return default;
            if(!towerViewsPrepared)return TowerTarget(tower,null);
            viewRangeIndex.Invalidate(); // Views can invoke external code between tower visits.
            return TowerTarget(tower,viewRangeIndex);
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
        readonly List<float> segmentEnds=new List<float>();bool exactPathIndex;
        Vector2Int core;int width,height,nextEnemy=1,nextTower=1,nextProjectile=1;float pathLength,waveTime,playerCooldown;EnemyHandle playerTarget;bool playerAcquired;
        readonly Dictionary<WaveSpec,int> spawned=new Dictionary<WaveSpec,int>();
        // Rent per invocation so callbacks that re-enter Tick cannot overwrite outer snapshots.
        const int MaxRetainedWorkBuffers=8;
        struct RangeEntry {public EnemyHandle Enemy;public int Ordinal;}
        struct RangeStamp {public EnemyHandle Enemy;public Vector2 Position;public bool Alive;}
        sealed class RangeEntryComparer : IComparer<RangeEntry>
        {
            public int Compare(RangeEntry a,RangeEntry b)=>a.Ordinal.CompareTo(b.Ordinal);
        }
        static readonly IComparer<RangeEntry> rangeEntryComparer=new RangeEntryComparer();
        sealed class EnemyRangeIndex
        {
            const float CellSize=4,CoordinateLimit=65536;
            const int MinimumEnemies=32,MaximumQueryCells=256,MaximumRetainedBuckets=128;
            readonly Dictionary<Vector2Int,List<RangeEntry>> buckets=new Dictionary<Vector2Int,List<RangeEntry>>();
            readonly Stack<List<RangeEntry>> spareBuckets=new Stack<List<RangeEntry>>();
            readonly List<RangeStamp> stamps=new List<RangeStamp>();
            bool ready,needsValidation;
            int sourceCount;
            static bool Supported(Vector2 point)=>!float.IsNaN(point.x)&&!float.IsNaN(point.y)&&Mathf.Abs(point.x)<=CoordinateLimit&&Mathf.Abs(point.y)<=CoordinateLimit;
            public void Clear()
            {
                ready=false;needsValidation=false;stamps.Clear();
                foreach(var bucket in buckets.Values){bucket.Clear();if(spareBuckets.Count<MaximumRetainedBuckets)spareBuckets.Push(bucket);}
                buckets.Clear();sourceCount=0;
            }
            public void Invalidate(){needsValidation=true;}
            bool Matches(IReadOnlyList<EnemyHandle> enemies)
            {
                if(stamps.Count!=enemies.Count)return false;
                for(int i=0;i<stamps.Count;i++)
                {
                    var stamp=stamps[i];var enemy=enemies[i];
                    if(stamp.Enemy!=enemy||stamp.Alive!=enemy.Alive||!stamp.Position.Equals(enemy.Position))return false;
                }
                return true;
            }
            public void Build(IReadOnlyList<EnemyHandle> enemies)
            {
                Clear();sourceCount=enemies.Count;if(sourceCount<MinimumEnemies)return;
                for(int i=0;i<enemies.Count;i++)
                {
                    var enemy=enemies[i];stamps.Add(new RangeStamp{Enemy=enemy,Position=enemy.Position,Alive=enemy.Alive});if(!enemy.Alive)continue;
                    if(!Supported(enemy.Position))return;
                    var key=new Vector2Int(Mathf.FloorToInt(enemy.Position.x/CellSize),Mathf.FloorToInt(enemy.Position.y/CellSize));
                    if(!buckets.TryGetValue(key,out var bucket)){bucket=spareBuckets.Count>0?spareBuckets.Pop():new List<RangeEntry>();buckets.Add(key,bucket);}
                    bucket.Add(new RangeEntry{Enemy=enemy,Ordinal=i});
                }
                ready=true;
            }
            public bool TryCollect(Vector2 position,float range,IReadOnlyList<EnemyHandle> enemies,List<EnemyHandle> result,List<RangeEntry> candidates)
            {
                if(needsValidation){if(!Matches(enemies))Build(enemies);needsValidation=false;}
                if(!ready||!Supported(position)||float.IsNaN(range)||float.IsInfinity(range)||range<0)return false;
                double minX=Math.Floor(((double)position.x-range)/CellSize)-1,maxX=Math.Floor(((double)position.x+range)/CellSize)+1;
                double minY=Math.Floor(((double)position.y-range)/CellSize)-1,maxY=Math.Floor(((double)position.y+range)/CellSize)+1;
                double cells=(maxX-minX+1)*(maxY-minY+1);
                // One extra cell includes float-rounding/underflow boundary candidates. The
                // original float Distance remains the final predicate; unusual views fall back.
                if(cells>MaximumQueryCells||cells>=sourceCount||minX<int.MinValue||maxX>=int.MaxValue||minY<int.MinValue||maxY>=int.MaxValue)return false;
                candidates.Clear();
                for(int y=(int)minY;y<=(int)maxY;y++)for(int x=(int)minX;x<=(int)maxX;x++)
                    if(buckets.TryGetValue(new Vector2Int(x,y),out var bucket))
                        foreach(var entry in bucket)if(entry.Enemy.Alive&&Vector2.Distance(entry.Enemy.Position,position)<=range)candidates.Add(entry);
                candidates.Sort(rangeEntryComparer);
                foreach(var entry in candidates)result.Add(entry.Enemy);
                return true;
            }
        }
        sealed class StepWork
        {
            public readonly List<WaveSpec> Rows=new List<WaveSpec>();
            public readonly ModelList<EnemyHandle> Enemies=new ModelList<EnemyHandle>();
            public readonly List<EnemyHandle> BuffSources=new List<EnemyHandle>();
            public readonly Dictionary<EnemyHandle,float> Boosts=new Dictionary<EnemyHandle,float>();
            public readonly List<TowerState> Towers=new List<TowerState>();
            public readonly ModelList<ProjectileHandle> Projectiles=new ModelList<ProjectileHandle>();
            public readonly Dictionary<ProjectileHandle,int> ProjectileRemovals=new Dictionary<ProjectileHandle,int>();
            public readonly EnemyRangeIndex RangeIndex=new EnemyRangeIndex();
            public void Clear(){Rows.Clear();Enemies.Clear();BuffSources.Clear();Boosts.Clear();Towers.Clear();Projectiles.Clear();ProjectileRemovals.Clear();RangeIndex.Clear();}
        }
        sealed class ShotWork
        {
            public readonly List<EnemyHandle> Available=new List<EnemyHandle>();
            public readonly EnemyHandle[] Targets=new EnemyHandle[3];
            public readonly List<RangeEntry> SpatialCandidates=new List<RangeEntry>();
            public void Clear(){Available.Clear();SpatialCandidates.Clear();Array.Clear(Targets,0,Targets.Length);}
        }
        readonly Stack<StepWork> stepWorkPool=new Stack<StepWork>();
        readonly Stack<ShotWork> shotWorkPool=new Stack<ShotWork>();
        struct TowerStamp {public TowerState Tower;public int Id;}
        readonly List<TowerStamp> towerStamps=new List<TowerStamp>();
        readonly Dictionary<int,TowerState> towerLookup=new Dictionary<int,TowerState>();
        readonly Dictionary<string,int> sourceIds=new Dictionary<string,int>();
        public TowerState FindTower(int id)
        {
            bool matches=towerStamps.Count==Towers.Count;
            for(int i=0;matches&&i<Towers.Count;i++)matches=ReferenceEquals(towerStamps[i].Tower,Towers[i])&&towerStamps[i].Id==Towers[i].Id;
            if(!matches)
            {
                towerStamps.Clear();towerLookup.Clear();
                foreach(var tower in Towers)
                {
                    towerStamps.Add(new TowerStamp{Tower=tower,Id=tower.Id});
                    if(!towerLookup.ContainsKey(tower.Id))towerLookup.Add(tower.Id,tower);
                }
            }
            return towerLookup.TryGetValue(id,out var result)?result:null;
        }
        public TowerState TowerFromSource(string source)
        {
            return TryTowerSourceId(source,out int id)?FindTower(id):null;
        }
        public bool TryTowerSourceId(string source,out int id)
        {
            id=0;if(source==default||!source.StartsWith("Tower:"))return false;
            if(!sourceIds.TryGetValue(source,out id))
            {
                if(!int.TryParse(source.Substring(6),out id))return false;
                // Only unsigned ASCII decimal spellings are culture independent.
                bool cacheable=source.Length>6;
                for(int i=6;cacheable&&i<source.Length;i++)cacheable=source[i]>='0'&&source[i]<='9';
                if(cacheable){if(sourceIds.Count>=512)sourceIds.Clear();sourceIds.Add(source,id);}
            }
            return true;
        }
        struct SupportStamp
        {
            public TowerState Tower;public TowerKind Kind;public Vector2Int Position;public float Range,Damage;
        }
        struct SupportResult {public float Damage;public bool InRange;}
        sealed class SupportCache
        {
            readonly List<SupportStamp> stamps=new List<SupportStamp>();
            readonly Dictionary<Vector2Int,SupportResult> results=new Dictionary<Vector2Int,SupportResult>();
            public bool Dirty=true;
            public void Refresh(List<TowerState> towers)
            {
                bool matches=stamps.Count==towers.Count;
                for(int i=0;matches&&i<towers.Count;i++)
                {
                    var tower=towers[i];var stamp=stamps[i];
                    matches=ReferenceEquals(stamp.Tower,tower)&&stamp.Kind==tower.Kind;
                    if(matches&&tower.Kind==TowerKind.Support)
                        matches=stamp.Position==tower.Position&&stamp.Range.Equals(tower.Stats.Range)&&stamp.Damage.Equals(tower.Stats.BuffDamage);
                }
                if(!matches)
                {
                    stamps.Clear();results.Clear();
                    foreach(var tower in towers)
                    {
                        var stamp=new SupportStamp{Tower=tower,Kind=tower.Kind};
                        if(tower.Kind==TowerKind.Support){stamp.Position=tower.Position;stamp.Range=tower.Stats.Range;stamp.Damage=tower.Stats.BuffDamage;}
                        stamps.Add(stamp);
                    }
                }
                Dirty=false;
            }
            public SupportResult Get(List<TowerState> towers,Vector2Int position)
            {
                if(Dirty)Refresh(towers);
                if(results.TryGetValue(position,out var cached))return cached;
                double damage=0;bool inRange=false;
                foreach(var stamp in stamps)if(stamp.Kind==TowerKind.Support&&Vector2.Distance(stamp.Position,position)<=stamp.Range)
                {damage+=stamp.Damage;inRange=true;}
                var result=new SupportResult{Damage=(float)damage,InRange=inRange};
                if(results.Count>=256)results.Clear();
                results.Add(position,result);return result;
            }
        }
        readonly SupportCache supportCache=new SupportCache();
        public bool HasTowerSupport(TowerState tower)
        {supportCache.Refresh(Towers);return supportCache.Get(Towers,tower.Position).InRange;}
        // Session investment: independent per kind/stat, retained even with no towers installed.
        readonly Dictionary<TowerKind,Dictionary<string,int>> towerUpgradeSteps=new Dictionary<TowerKind,Dictionary<string,int>>();
        int TowerStep(TowerKind kind,string stat) => towerUpgradeSteps.TryGetValue(kind,out var steps)&&steps.TryGetValue(stat,out var n)?n:0;
        public Session(CombatData data, int? seed=null) { Data=data??throw new ArgumentNullException(nameof(data));augmentationRandom=seed.HasValue?new System.Random(seed.Value):new System.Random();Player=data.Player.Clone();Gold=data.StartGold;Crystals=data.StartCrystals;CoreEnergy=CoreMaximum=data.CoreEnergy;PreparationRemaining=data.PreparationSeconds;Phase=SessionPhase.Preparation;Wave=1;LastWave=data.Waves.Select(x=>x.Wave).DefaultIfEmpty(1).Max();LastMessage="준비";InitializeModelPools(); }
        public void SetBoard(int width,int height,Vector2Int core,IEnumerable<Vector2Int> route)
        {
            if(width<=0||height<=0)throw new ArgumentException("Invalid board");this.width=width;this.height=height;this.core=core;path.Clear();lengths.Clear();pathCells.Clear();segmentEnds.Clear();exactPathIndex=false;pathLength=0;
            foreach(var p in route) { if(p.x<0||p.y<0||p.x>=width||p.y>=height)throw new ArgumentException("Path outside board");path.Add(p); }
            if(path.Count<2||path[path.Count-1]!=(Vector2)core)throw new ArgumentException("Path must terminate at core");
            for(int i=1;i<path.Count;i++) {var a=Vector2Int.RoundToInt(path[i-1]);var b=Vector2Int.RoundToInt(path[i]);if(a.x!=b.x&&a.y!=b.y)throw new ArgumentException("Path must be axial");float len=Vector2.Distance(a,b);if(len<=0)throw new ArgumentException("Zero length segment");lengths.Add(len);pathLength+=len;var dir=new Vector2Int(Math.Sign(b.x-a.x),Math.Sign(b.y-a.y));for(var p=a;p!=b;p+=dir)pathCells.Add(p);pathCells.Add(b);}
            // Small integer axial lengths make subtracting a segment start exactly equal
            // to the old repeated subtraction for nonnegative travelled distances.
            exactPathIndex=pathLength<=1048576;
            float end=0;
            for(int i=0;i<lengths.Count;i++)
            {
                double axis=Math.Abs((double)path[i+1].x-path[i].x)+Math.Abs((double)path[i+1].y-path[i].y);
                if(lengths[i]!=axis||axis!=Math.Floor(axis))exactPathIndex=false;
                end+=lengths[i];segmentEnds.Add(end);
            }
        }
        public bool IsBuildable(Vector2Int cell) {return cell.x>=0&&cell.y>=0&&cell.x<width&&cell.y<height&&!pathCells.Contains(cell)&&cell!=core&&!Towers.Any(t=>t.Position==cell);}
        public bool CanInstall(TowerKind kind,Vector2Int cell,out string reason)
        {
            reason=null;if(Phase!=SessionPhase.Preparation)reason="준비 시간에만 설치 가능";else if(!IsBuildable(cell))reason="설치 불가 칸";else if(Crystals<Data.Towers[kind].Cost)reason="수정 부족";else if(kind==TowerKind.Support&&!Towers.Any(t=>t.Kind!=TowerKind.Support))reason="공격 포탑을 먼저 설치";return reason==null;
        }
        public TowerState TryInstall(TowerKind kind,Vector2Int cell,out string reason)
        {
            if(!CanInstall(kind,cell,out reason))return null;var s=Data.Towers[kind];var t=new TowerState{Id=nextTower++,Kind=kind,Position=cell,Spent=s.Cost,Stats=new CombatStats{Damage=s.Damage,Rate=s.Rate,Range=s.Range,ProjectileSpeed=s.ProjectileSpeed,BuffDamage=s.BuffDamage}};
            foreach(var u in Data.Upgrades.Where(u=>u.Target==kind.ToString()))
            {
                int step=TowerStep(kind,u.Stat);if(step==0)continue;
                t.Stats.Add(u.Stat,u.Delta*step);t.Stats.UpgradeSteps[u.Stat]=step;
            }
            Crystals-=s.Cost;Towers.Add(t);LastMessage="포탑 설치";return t;
        }
        public bool DestroyTower(int id) {if(Phase==SessionPhase.Augmentation)return false;var t=Towers.FirstOrDefault(x=>x.Id==id);if(t==null)return false;Towers.Remove(t);int refund=t.Spent/2;Crystals+=refund;LastMessage="철거 +"+refund+" 수정";return true;}
        UpgradeSpec FindUpgrade(string target,string stat) {return Data.Upgrades.FirstOrDefault(u=>u.Target==target&&u.Stat==stat);}
        static int Step(CombatStats s,string stat) {int n;return s.UpgradeSteps.TryGetValue(stat,out n)?n:0;}
        public int PlayerUpgradePrice(string stat) {return FindUpgrade("Player",stat)==null?-1:Data.PlayerUpgradePrice(Step(Player,stat)+1);}
        public int TowerUpgradePrice(int id,string stat) {var t=Towers.FirstOrDefault(x=>x.Id==id);if(t==null)return -1;var u=FindUpgrade(t.Kind.ToString(),stat);int step=TowerStep(t.Kind,stat);return u==null?-1:u.Cost+u.CostIncrement*step-(step==0?Data.TowerFirstUpgradeDiscount:0);}
        public bool UpgradePlayer(string stat,out string reason) {var u=FindUpgrade("Player",stat);reason=null;if(u==null)reason="강화 불가";else if(Phase!=SessionPhase.Preparation&&Phase!=SessionPhase.Combat)reason="현재 단계에서 강화 불가";else if(Gold<PlayerUpgradePrice(stat))reason="골드 부족";if(reason!=null)return false;Gold-=PlayerUpgradePrice(stat);Player.Add(stat,u.Delta);Player.UpgradeSteps[stat]=Step(Player,stat)+1;LastMessage="기술병 강화";return true;}
        public bool UpgradeTower(int id,string stat,out string reason)
        {
            var t=Towers.FirstOrDefault(x=>x.Id==id);var u=t==null?null:FindUpgrade(t.Kind.ToString(),stat);reason=null;
            if(Phase!=SessionPhase.Preparation)reason="준비 시간에만 강화 가능";else if(u==null)reason="강화 불가";else if(Crystals<TowerUpgradePrice(id,stat))reason="수정 부족";
            if(reason!=null)return false;
            Crystals-=TowerUpgradePrice(id,stat);int step=TowerStep(t.Kind,stat)+1;
            if(!towerUpgradeSteps.TryGetValue(t.Kind,out var steps))towerUpgradeSteps[t.Kind]=steps=new Dictionary<string,int>();
            steps[stat]=step;
            foreach(var tower in Towers.Where(x=>x.Kind==t.Kind)){tower.Stats.Add(stat,u.Delta);tower.Stats.UpgradeSteps[stat]=step;}
            LastMessage="같은 종류 포탑 전체 강화 · 이후 설치 포함";return true;
        }
        public bool StartCombat()
        {
            if(Phase!=SessionPhase.Preparation||path.Count<2)return false;
            Phase=SessionPhase.Combat;PreparationRemaining=0;waveTime=0;spawned.Clear();DamageTotals.Clear();
            BossKilled=false;coreDamagedThisRound=false;
            playerTarget=default;playerAcquired=false;playerCooldown=0;
            foreach(var tower in Towers){tower.Target=default;tower.AcquiredOnce=false;tower.Cooldown=0;}
            LastMessage=Diagnostic?"역할 검증 전투 시작":Wave+"라운드 전투 시작";CombatStarted?.Invoke();return true;
        }
        public void StartDiagnosticEncounter()
        {
            BeginModelOperation();try
            {
            towerUpgradeSteps.Clear();
            if(path.Count<2)throw new InvalidOperationException("Board missing");Diagnostic=true;Wave=1;CompletedWave=0;LastCompletionReward=0;waveTime=0;spawned.Clear();DamageTotals.Clear();ClearEnemies();ClearProjectiles();Towers.Clear();Gold=Data.DiagnosticGold;Crystals=Data.DiagnosticCrystals;CoreEnergy=Data.CoreEnergy;Phase=SessionPhase.Preparation;PreparationRemaining=Data.PreparationSeconds;playerTarget=default;playerAcquired=false;playerCooldown=0;Player=Data.Player.Clone();LastMessage="역할 검증 모드: 준비 후 시작";
            CoreMaximum=Data.CoreEnergy;Augments=new AugmentationEffects();BossKilled=false;choices=Array.Empty<AugmentationCard>();AugmentationRemaining=0;coreDamagedThisRound=false;
            }
            finally{EndModelOperation();}
        }
        public EnemyHandle DebugSpawn(EnemyKind kind,float distance=0,EnemySpec spec=null) {if(path.Count<2)throw new InvalidOperationException("Board missing");spec=spec??Data.EnemyForWave(Wave,kind);return RentEnemy(kind,spec,distance);}
        void PositionEnemy(EnemyHandle e)
        {
            float d=e.PathTravelled;
            if(exactPathIndex&&d>=0&&d<=pathLength)
            {
                int low=0,high=segmentEnds.Count-1;
                while(low<high){int mid=low+(high-low)/2;if(d<=segmentEnds[mid])high=mid;else low=mid+1;}
                float local=d-(low==0?0:segmentEnds[low-1]);
                e.Position=Vector2.Lerp(path[low],path[low+1],local/lengths[low]);e.PathRemaining=Mathf.Max(0,pathLength-e.PathTravelled);return;
            }
            for(int i=0;i<lengths.Count;i++){if(d<=lengths[i]){e.Position=Vector2.Lerp(path[i],path[i+1],d/lengths[i]);e.PathRemaining=Mathf.Max(0,pathLength-e.PathTravelled);return;}d-=lengths[i];}
            e.Position=path[path.Count-1];e.PathRemaining=0;
        }
        public void Tick(float dt,Vector2 playerPosition)
        {
            if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<0)throw new ArgumentException("Invalid dt");
            BeginModelOperation();try{while(dt>0&&!disposed){float step=Mathf.Min(dt,.05f);StepTick(step,playerPosition);dt-=step;}}
            finally{EndModelOperation();}
        }
        void StepTick(float dt,Vector2 playerPosition)
        {
            if(Phase==SessionPhase.Augmentation){AugmentationRemaining=Mathf.Max(0,AugmentationRemaining-dt);if(AugmentationRemaining<=.0001f)ChooseAugmentation(AugmentationDeck.BestIndex(choices));return;}
            if(Phase==SessionPhase.Preparation) {PreparationRemaining=Mathf.Max(0,PreparationRemaining-dt);if(PreparationRemaining<=0)StartCombat();return;}if(Phase!=SessionPhase.Combat)return;
            var work=stepWorkPool.Count>0?stepWorkPool.Pop():new StepWork();
            try
            {
                var rows=work.Rows;
                foreach(var row in Data.Waves)if(row.Wave==Wave)rows.Add(row);
                if(Diagnostic) {if(waveTime==0) {float diagnosticDistance=Mathf.Min(Data.DiagnosticStartDistance,pathLength*.2f);DebugSpawn(EnemyKind.Attack,diagnosticDistance);DebugSpawn(EnemyKind.Defense,diagnosticDistance+Data.DiagnosticSpacing);DebugSpawn(EnemyKind.Buff,diagnosticDistance+2*Data.DiagnosticSpacing);} }
                else foreach(var row in rows) {int count;spawned.TryGetValue(row,out count);while(count<row.Count&&waveTime+.00001f>=row.Start+count*row.Interval) {DebugSpawn(row.Kind,0,row.Enemy??Data.Enemies[row.Kind]);count++;}spawned[row]=count;}
                waveTime+=dt;
                // Calculate all auras before moving anyone so source order cannot alter the radius test.
                var boosts=work.Boosts;
                // Keep the original source order and freeze membership before any movement.
                foreach(var source in Enemies)if(source.Alive&&source.Kind==EnemyKind.Buff)work.BuffSources.Add(source);
                foreach(var enemy in Enemies)if(enemy.Alive)
                {
                    float boost=0;bool found=false;
                    foreach(var other in work.BuffSources)
                        if(other!=enemy&&Vector2.Distance(enemy.Position,other.Position)<=other.Spec.BuffRadius)
                        {
                            float value=other.Spec.BuffSpeed;
                            if(!found||value>boost||float.IsNaN(boost))boost=value;
                            found=true;
                        }
                    boosts.Add(enemy,boost);work.Enemies.Add(enemy);
                }
                foreach(var e in work.Enemies)e.Buffed=boosts[e]>0;
                foreach(var e in work.Enemies) {float boost=boosts[e];e.PathTravelled=Mathf.Min(pathLength,e.PathTravelled+e.MovementSpeed*(1+boost/100f)*dt);e.SlowRemaining=Mathf.Max(0,e.SlowRemaining-dt);PositionEnemy(e);if(e.PathRemaining<=.00001f){e.Exited=true;coreDamagedThisRound|=e.Spec.CoreDamage>0;CoreEnergy=Mathf.Max(0,CoreEnergy-e.Spec.CoreDamage);LastMessage="코어 -"+e.Spec.CoreDamage;}}
                if(CoreEnergy<=0) {Phase=SessionPhase.Defeat;ClearProjectiles();RemoveDeadEnemies();LastMessage="방어 실패";if(!Diagnostic)DefeatReached?.Invoke();return;}
                work.RangeIndex.Build(Enemies);
                supportCache.Dirty=true;
                Shoot(playerPosition,Player,"Player",0,ref playerCooldown,ref playerTarget,ref playerAcquired,dt,false,work.RangeIndex);
                if(disposed)return;
                work.Towers.AddRange(Towers);
                foreach(var t in work.Towers)
                {
                    if(t.Kind==TowerKind.Support)continue;
                    float buff=Augments.TowerBuffPercent+supportCache.Get(Towers,t.Position).Damage;
                    Shoot(t.Position,t.Stats,t.AttackSource,buff,ref t.Cooldown,ref t.Target,ref t.AcquiredOnce,dt,t.Kind==TowerKind.Multi,work.RangeIndex);
                    if(disposed)return;
                }
                work.Projectiles.AddRange(Projectiles);
                MoveProjectiles(dt,work);
                RemoveDeadEnemies();
                bool complete=Diagnostic;
                if(!complete&&rows.Count>0)
                {
                    complete=true;
                    foreach(var row in rows)if(!spawned.TryGetValue(row,out int count)||count<row.Count){complete=false;break;}
                }
                if(complete&&Enemies.Count==0)CompleteRound();
                }
            finally{work.Clear();if(stepWorkPool.Count<MaxRetainedWorkBuffers)stepWorkPool.Push(work);}
        }
        void MoveProjectiles(float dt,StepWork work)
        {
            var removals=work.ProjectileRemovals;
            try
            {
                foreach(var p in work.Projectiles)
                {
                    if(disposed)return;if(!p.Accessible)continue;
                    if(p.Target.Alive)
                    {
                        p.Slot.LastTarget=new EnemySnapshot(p.Target);
                        p.Slot.Position=Vector2.MoveTowards(p.Position,p.Target.Position,p.Speed*dt);
                        if(!(Vector2.Distance(p.Position,p.Target.Position)<=.00001f))continue;
                        if(DamageApplied!=null)
                        {
                            // Observers see prior removals, but still see the current shot.
                            // Remove after the callback using the original first-match rule:
                            // the observer may clear/reorder the list or re-enter Tick.
                            FlushProjectileRemovals(removals);
                            ApplyDamage(p.Target,p.Damage,p.Source);
                            RemoveProjectile(p);
                            continue;
                        }
                        ApplyDamage(p.Target,p.Damage,p.Source);
                    }
                    removals.TryGetValue(p,out int count);removals[p]=count+1;
                }
            }
            finally{FlushProjectileRemovals(removals);}
        }
        void FlushProjectileRemovals(Dictionary<ProjectileHandle,int> removals)
        {
            if(removals.Count==0)return;
            int write=0,count=Projectiles.Count;
            for(int read=0;read<count;read++)
            {
                var projectile=Projectiles[read];
                if(projectile!=default&&removals.TryGetValue(projectile,out int remaining)&&remaining>0)
                {removals[projectile]=remaining-1;ReleaseProjectileMembership(projectile);continue;}
                if(write!=read)Projectiles[write]=projectile;
                write++;
            }
            if(write<count)Projectiles.RemoveRange(write,count-write);
            removals.Clear();
        }
        void CompleteRound()
        {
            ClearProjectiles();
            if(Diagnostic){Phase=SessionPhase.Victory;LastMessage="역할 검증 종료";return;}
            CompletedWave=Wave;LastCompletionReward=Data.RewardForWave(Wave);Crystals+=LastCompletionReward;
            if(!coreDamagedThisRound){Gold+=Augments.FlawlessGold;Crystals+=Augments.FlawlessCrystals;}
            LastMessage=CompletedWave+"라운드 완료 +"+LastCompletionReward+" 수정";
            if(Wave>=LastWave){Phase=SessionPhase.Victory;LastMessage+=" · 1~"+LastWave+"라운드 검증 완료";VictoryReached?.Invoke();return;}
            if(!Data.Waves.Any(row=>row.Wave==Wave+1))throw new InvalidOperationException("Next wave is missing");
            Wave++;PreparationRemaining=Data.PreparationSeconds;
            if(CompletedWave%Data.AugmentationEvery==0&&(!Data.AugmentationRequiresBoss||BossKilled)){choices=AugmentationDeck.Draw(augmentationRandom,Augments.Selected);AugmentationRemaining=30;Phase=SessionPhase.Augmentation;LastMessage+=" · 증강 선택";RoundCompleted?.Invoke();return;}
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
        void Shoot(Vector2 position,CombatStats stats,string source,float buff,ref float cooldown,ref EnemyHandle target,ref bool acquired,float dt,bool multi,EnemyRangeIndex rangeIndex)
        {
            var work=shotWorkPool.Count>0?shotWorkPool.Pop():new ShotWork();
            try
            {
                var available=work.Available;
                if(!rangeIndex.TryCollect(position,stats.Range,Enemies,available,work.SpatialCandidates))
                    foreach(var enemy in Enemies)if(enemy.Alive&&Vector2.Distance(enemy.Position,position)<=stats.Range)available.Add(enemy);
                bool isTower=source!="Player";
                if(target!=default&&(!target.Alive||Vector2.Distance(target.Position,position)>stats.Range))target=default;
                if(isTower&&target!=default&&target.Kind==EnemyKind.Boss)
                    foreach(var enemy in available)if(enemy.Kind!=EnemyKind.Boss){target=default;break;}
                if(target==default&&available.Count>0)
                {
                    foreach(var enemy in available)
                        if(target==default||(acquired?ComparePriority(enemy,target,isTower):CompareInitialTarget(enemy,target,isTower))<0)target=enemy;
                    acquired=true;
                }
                cooldown-=dt;if(available.Count==0||stats.Rate<=0){cooldown=Mathf.Max(0,cooldown);return;}if(cooldown>0)return;
                var targets=work.Targets;int targetCount=1;
                if(!multi)targets[0]=target;
                else
                {
                    targetCount=0;
                    // Stable insertion keeps the first three entries in the original full sort.
                    foreach(var enemy in available)
                    {
                        int index=0;while(index<targetCount&&ComparePriority(enemy,targets[index],true)>=0)index++;
                        if(index>=3)continue;
                        for(int i=Math.Min(targetCount,2);i>index;i--)targets[i]=targets[i-1];
                        targets[index]=enemy;targetCount=Math.Min(targetCount+1,3);
                    }
                }
                while(cooldown<=.00001f)
                {
                    int launched=0;
                    for(int i=0;i<targetCount;i++)
                    {
                        var e=targets[i];if(e==default)continue;int shots=source=="Player"?1+Augments.ExtraPlayerProjectiles:1;
                        for(int shot=0;shot<shots;shot++)
                        {
                            var projectile=RentProjectile(e,position,stats.Damage*(1+buff/100f),stats.ProjectileSpeed,source);
                            launched++;
                            var onProjectile=ProjectileLaunched;
                            if(onProjectile!=null)
                            {
                                rangeIndex.Invalidate();supportCache.Dirty=true;
                                try{onProjectile(new ProjectileSnapshot(projectile));}finally{rangeIndex.Invalidate();supportCache.Dirty=true;}
                            }
                            if(disposed)return;
                        }
                    }
                    var onAttack=AttackLaunched;
                    if(launched>0&&onAttack!=null)
                    {
                        rangeIndex.Invalidate();supportCache.Dirty=true;
                        try{onAttack(source,launched);}finally{rangeIndex.Invalidate();supportCache.Dirty=true;}
                    }
                    if(disposed)return;
                    float nextCooldown=cooldown+1/stats.Rate;if(nextCooldown<=cooldown)throw new InvalidOperationException("Attack rate exceeds floating-point timing precision");cooldown=nextCooldown;
                }
                }
            finally{work.Clear();if(shotWorkPool.Count<MaxRetainedWorkBuffers)shotWorkPool.Push(work);}
        }
        static int ComparePriority(EnemyHandle a,EnemyHandle b,bool tower)=>CombatRules.CompareTargetPriority(a,b,tower);
        static int CompareInitialTarget(EnemyHandle a,EnemyHandle b,bool tower)
        {
            if(tower){int order=(a.Kind==EnemyKind.Boss?1:0).CompareTo(b.Kind==EnemyKind.Boss?1:0);if(order!=0)return order;}
            return a.Id.CompareTo(b.Id);
        }
        public int ApplyDamage(EnemyHandle target,float damage,string source)
        {
            if(disposed||target==default||!ReferenceEquals(target.Owner,this)||!target.IsValid||!target.Alive)return 0;
            BeginModelOperation();try
            {
                int actual=Math.Min(target.Health,CombatRules.Damage(damage,target.Spec.Defense));target.Health-=actual;int previous;DamageTotals.TryGetValue(source,out previous);DamageTotals[source]=previous+actual;if(actual>0&&source=="Player"&&Augments.PlayerSlow)target.SlowRemaining=1;if(target.Health==0){Gold+=target.Spec.Gold;if(target.Kind==EnemyKind.Boss)BossKilled=true;LastMessage="+"+target.Spec.Gold+" 골드";}if(actual>0)DamageApplied?.Invoke(new EnemySnapshot(target),actual);return actual;
            }
            finally{EndModelOperation();}
        }
    }
}


