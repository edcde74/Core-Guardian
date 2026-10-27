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
        int viewRevision,viewListVersion;
        public void BeginTowerViews()
        {
            towerViewsPrepared=false;
            foreach(var tower in Towers)if(tower.Kind==TowerKind.Multi){viewRangeIndex.Build(Enemies);viewRevision=enemyRevision;viewListVersion=Enemies.Version;towerViewsPrepared=true;break;}
        }
        public void EndTowerViews(){towerViewsPrepared=false;viewRangeIndex.Clear();}
        public EnemyHandle TowerTargetForView(TowerState tower)
        {
            tower=FindTower(tower.Id);if(tower==null)return default;
            if(!towerViewsPrepared)return TowerTarget(tower,null);
            // Views can invoke external code between tower visits; re-validate only when an enemy write,
            // model operation or membership change happened since the index was last checked.
            if(viewRevision!=enemyRevision||viewListVersion!=Enemies.Version){viewRangeIndex.Invalidate();viewRevision=enemyRevision;viewListVersion=Enemies.Version;}
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
        struct BuffAura {public EnemyHandle Source;public Vector2 Position;public EnemySpec Spec;}
        // List.Sort(IComparer) wraps comparer.Compare in a new delegate on every call (about 128 B per query);
        // a cached Comparison runs the same introsort with the same comparisons and allocates nothing.
        static readonly Comparison<RangeEntry> rangeEntryOrder=(a,b)=>a.Ordinal.CompareTo(b.Ordinal);
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
                candidates.Sort(rangeEntryOrder);
                foreach(var entry in candidates)result.Add(entry.Enemy);
                return true;
            }
        }
        sealed class StepWork
        {
            public readonly List<WaveSpec> Rows=new List<WaveSpec>();
            public readonly ModelList<EnemyHandle> Enemies=new ModelList<EnemyHandle>();
            public readonly List<EnemyHandle> BuffSources=new List<EnemyHandle>();
            public readonly List<BuffAura> Auras=new List<BuffAura>();
            public readonly Dictionary<EnemyHandle,float> Boosts=new Dictionary<EnemyHandle,float>();
            public readonly List<TowerState> Towers=new List<TowerState>();
            public readonly ModelList<ProjectileHandle> Projectiles=new ModelList<ProjectileHandle>();
            public readonly Dictionary<ProjectileHandle,int> ProjectileRemovals=new Dictionary<ProjectileHandle,int>();
            public readonly EnemyRangeIndex RangeIndex=new EnemyRangeIndex();
            public void Clear(){Rows.Clear();Enemies.Clear();BuffSources.Clear();Auras.Clear();Boosts.Clear();Towers.Clear();Projectiles.Clear();ProjectileRemovals.Clear();RangeIndex.Clear();}
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
        public Session(CombatData data, int? seed=null) { Data=data??throw new ArgumentNullException(nameof(data));augmentationRandom=seed.HasValue?new System.Random(seed.Value):new System.Random();combatRandom=seed.HasValue?new System.Random(unchecked(seed.Value*31+7)):new System.Random();tags=TagCache.From(data);Player=data.Player.Clone();Gold=data.StartGold;Crystals=data.StartCrystals;CoreEnergy=CoreMaximum=data.CoreEnergy;PreparationRemaining=data.PreparationFor(1,false);Phase=SessionPhase.Preparation;Wave=1;LastWave=data.Waves.Select(x=>x.Wave).DefaultIfEmpty(1).Max();LastMessage="준비";InitializeModelPools(); }
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
        // 49 전선 건설 allows combat installs (+25%); 34 급속 건설 discounts then taxes.
        public bool CanBuildNow=>Phase==SessionPhase.Preparation||Phase==SessionPhase.Combat&&Augments.Has(49);
        public int InstallCost(TowerKind kind)
        {
            float cost=Data.Towers[kind].Cost;
            if(Augments.Has(34)){cost*=Augments.Rounds34>0?1-Augments.Value(34,1)/Augments.Count(34)/100f:1+Augments.Value(34,2)/Augments.Count(34)/100f;}
            if(Phase==SessionPhase.Combat&&Augments.Has(49))cost*=1+Augments.Value(49)/Augments.Count(49)/100f;
            return Mathf.Max(0,Mathf.FloorToInt(cost+.5f));
        }
        public bool CanInstall(TowerKind kind,Vector2Int cell,out string reason)
        {
            reason=null;if(!CanBuildNow)reason="준비 시간에만 설치 가능";else if(!IsBuildable(cell))reason="설치 불가 칸";else if(Crystals<InstallCost(kind))reason="수정 부족";else if(kind==TowerKind.Support&&!Towers.Any(t=>t.Kind!=TowerKind.Support))reason="공격 포탑을 먼저 설치";return reason==null;
        }
        public TowerState TryInstall(TowerKind kind,Vector2Int cell,out string reason)
        {
            if(!CanInstall(kind,cell,out reason))return null;var s=Data.Towers[kind];int cost=InstallCost(kind);var t=new TowerState{Id=nextTower++,Kind=kind,Position=cell,Spent=cost,Stats=new CombatStats{Damage=s.Damage,Rate=s.Rate,Range=s.Range,ProjectileSpeed=s.ProjectileSpeed,BuffDamage=s.BuffDamage,SlowPercent=s.SlowPercent,SlowDuration=s.SlowDuration,BurnPerStack=s.BurnPerStack,BurnDuration=s.BurnDuration,PierceFalloff=s.PierceFalloff}};
            foreach(var u in Data.Upgrades.Where(u=>u.Target==kind.ToString()))
            {
                int step=TowerStep(kind,UpgradeKey(u));if(step==0)continue;
                t.Stats.Add(u.Stat,u.Delta*step);t.Stats.UpgradeSteps[u.Stat]=step;
            }
            ClampTowerStats(t);
            Crystals-=cost;Towers.Add(t);LastMessage="포탑 설치";
            if(Augments.Has(45))t=TryMerge(t);
            return t;
        }
        public int RefundPercent=>Augments.Has(13)?(int)Augments.Value(13)/Augments.Count(13):(int)Data.Setting("refundPercent",50);
        public bool DestroyTower(int id) {if(Phase==SessionPhase.Augmentation)return false;var t=Towers.FirstOrDefault(x=>x.Id==id);if(t==null)return false;Towers.Remove(t);int refund=t.Spent*RefundPercent/100;Crystals+=refund;LastMessage="철거 +"+refund+" 수정";return true;}
        // 45 포탑 합체: ★1+★1 side by side → ★2, two adjacent ★2 → ★3. The later tower's cell frees.
        static readonly Vector2Int[] Sides={Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
        TowerState TryMerge(TowerState placed)
        {
            var current=placed;
            for(int guard=0;guard<3;guard++)
            {
                if(current.Stars>=3)break;
                TowerState partner=null;
                foreach(var d in Sides){var other=Towers.FirstOrDefault(x=>x!=current&&x.Kind==current.Kind&&x.Stars==current.Stars&&x.Position==current.Position+d);if(other!=null){partner=other;break;}}
                if(partner==null)break;
                var keep=partner.Id<current.Id?partner:current;var vacate=keep==partner?current:partner;
                Towers.Remove(vacate);keep.Spent+=vacate.Spent;SetStars(keep,keep.Stars+1);
                RaiseCue("star_merge",keep.Position);LastMessage="포탑 합체 ★"+keep.Stars;current=keep;
            }
            return current;
        }
        float StarDamage(int stars)=>stars>=3?Data.Setting("starDamage3",3.2f):stars==2?Data.Setting("starDamage2",1.8f):1;
        float StarRange(int stars)=>stars>=3?Data.Setting("starRange3",1):stars==2?Data.Setting("starRange2",.5f):0;
        void SetStars(TowerState t,int stars)
        {
            float ratio=StarDamage(stars)/StarDamage(t.Stars);float range=StarRange(stars)-StarRange(t.Stars);
            t.Stats.Damage*=ratio;t.Stats.BuffDamage*=ratio;t.Stats.BurnPerStack*=ratio;t.Stats.Range+=range;t.Stars=stars;
        }
        void ClampTowerStats(TowerState t)
        {
            t.Stats.PierceFalloff=Mathf.Min(1,t.Stats.PierceFalloff);
            if(t.Stats.SlowPercent>0)t.Stats.SlowPercent=Mathf.Min(t.Stats.SlowPercent,tags.SlowMax);
        }
        // Grouped upgrades (21501 + 21511) share the step of their group leader's stat.
        string UpgradeKey(UpgradeSpec u){if(u.Group==0||u.Group==u.Id)return u.Stat;foreach(var g in Data.Upgrades)if(g.Id==u.Group)return g.Stat;return u.Stat;}
        UpgradeSpec FindUpgrade(string target,string stat) {foreach(var u in Data.Upgrades)if(u.Target==target&&u.Stat==stat)return u;return null;}
        public IEnumerable<string> UpgradeStats(TowerKind kind){foreach(var u in Data.Upgrades)if(u.Target==kind.ToString()&&UpgradeKey(u)==u.Stat)yield return u.Stat;}
        static int Step(CombatStats s,string stat) {int n;return s.UpgradeSteps.TryGetValue(stat,out n)?n:0;}
        public int PlayerUpgradePrice(string stat) {return FindUpgrade("Player",stat)==null?-1:Data.PlayerUpgradePrice(Step(Player,stat)+1);}
        public int TowerUpgradePrice(int id,string stat) {TowerState t=null;foreach(var x in Towers)if(x.Id==id){t=x;break;}if(t==null)return -1;var u=FindUpgrade(t.Kind.ToString(),stat);int step=TowerStep(t.Kind,stat);return u==null?-1:u.Cost+u.CostIncrement*step-(step==0?Data.TowerFirstUpgradeDiscount:0);}
        public bool UpgradePlayer(string stat,out string reason) {var u=FindUpgrade("Player",stat);reason=null;if(u==null)reason="강화 불가";else if(Phase!=SessionPhase.Preparation&&Phase!=SessionPhase.Combat)reason="현재 단계에서 강화 불가";else if(Gold<PlayerUpgradePrice(stat))reason="골드 부족";if(reason!=null)return false;Gold-=PlayerUpgradePrice(stat);Player.Add(stat,u.Delta);Player.UpgradeSteps[stat]=Step(Player,stat)+1;LastMessage="기술병 강화";return true;}
        public bool UpgradeTower(int id,string stat,out string reason)
        {
            var t=Towers.FirstOrDefault(x=>x.Id==id);var u=t==null?null:FindUpgrade(t.Kind.ToString(),stat);reason=null;
            if(Phase!=SessionPhase.Preparation)reason="준비 시간에만 강화 가능";else if(u==null)reason="강화 불가";else if(Crystals<TowerUpgradePrice(id,stat))reason="수정 부족";
            if(reason!=null)return false;
            Crystals-=TowerUpgradePrice(id,stat);int step=TowerStep(t.Kind,stat)+1;
            if(!towerUpgradeSteps.TryGetValue(t.Kind,out var steps))towerUpgradeSteps[t.Kind]=steps=new Dictionary<string,int>();
            steps[stat]=step;
            foreach(var member in Data.Upgrades)
            {
                if(member.Target!=u.Target||member!=u&&UpgradeKey(member)!=stat)continue;
                foreach(var tower in Towers.Where(x=>x.Kind==t.Kind))
                {
                    bool scaled=member.Stat=="damage"||member.Stat=="buffDamage"||member.Stat=="burnPerStack";
                    tower.Stats.Add(member.Stat,member.Delta*(scaled?StarDamage(tower.Stars):1));tower.Stats.UpgradeSteps[member.Stat]=step;ClampTowerStats(tower);
                }
            }
            LastMessage="같은 종류 포탑 전체 강화 · 이후 설치 포함";return true;
        }
        public bool StartCombat()
        {
            if(Phase!=SessionPhase.Preparation||path.Count<2)return false;
            Phase=SessionPhase.Combat;PreparationRemaining=0;waveTime=0;spawned.Clear();DamageTotals.Clear();
            BossKilled=false;coreDamagedThisRound=false;
            playerTarget=default;playerAcquired=false;playerCooldown=0;fireCells.Clear();fireTick=0;
            if(AreaBlast==null||AreaBlast.ResetOnRoundStart)areaBlastCooldown=0;
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
        public EnemyHandle DebugSpawn(EnemyKind kind,float distance=0,EnemySpec spec=null) {if(path.Count<2)throw new InvalidOperationException("Board missing");spec=spec??Data.EnemyForWave(Wave,kind);return RentEnemy(kind,spec,distance,spec.Flying?1:0);}
        void PositionEnemy(EnemyHandle e)
        {
            if(e.Slot.Route==1){enemyRevision++;PositionFlying(e.Slot);return;}
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
            FlushCues();
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
                if(areaBlastCooldown>0)areaBlastCooldown=Mathf.Max(0,areaBlastCooldown-dt);
                // Status timers, burn ticks and abilities run before movement, on a frozen list.
                work.Enemies.AddRange(Enemies);
                foreach(var e in work.Enemies)
                {
                    if(!e.Alive)continue;
                    TickStun(e,dt);if(!e.Alive)continue;
                    TickBurn(e,dt);if(!e.Alive)continue;
                    TickAbilities(e,dt);
                }
                TickFire(dt);
                FlushSpawns();
                work.Enemies.Clear();
                // Calculate all auras before moving anyone so source order cannot alter the radius test.
                var boosts=work.Boosts;
                // Keep the original source order and freeze membership before any movement.
                foreach(var source in Enemies)if(source.Alive&&source.Kind==EnemyKind.Buff)work.BuffSources.Add(source);
                // Nothing writes enemies inside this pass, so each source's position/spec is read once instead of per pair.
                var auras=work.Auras;foreach(var source in work.BuffSources)auras.Add(new BuffAura{Source=source,Position=source.Position,Spec=source.Spec});
                foreach(var enemy in Enemies)if(enemy.Alive)
                {
                    float boost=0;bool found=false;var position=enemy.Position;
                    foreach(var other in auras)
                        if(other.Source!=enemy&&Vector2.Distance(position,other.Position)<=other.Spec.BuffRadius)
                        {
                            float value=other.Spec.BuffSpeed;
                            if(!found||value>boost||float.IsNaN(boost))boost=value;
                            found=true;
                        }
                    boosts.Add(enemy,boost);work.Enemies.Add(enemy);
                }
                foreach(var e in work.Enemies)e.Buffed=boosts[e]>0;
                foreach(var e in work.Enemies)
                {
                    float boost=boosts[e];var slot=e.Slot;float length=RouteLength(slot);
                    e.PathTravelled=Mathf.Min(length,e.PathTravelled+e.MovementSpeed*(1+boost/100f)*dt);e.SlowRemaining=Mathf.Max(0,e.SlowRemaining-dt);PositionEnemy(e);
                    if(e.PathRemaining<=.00001f){e.Exited=true;coreDamagedThisRound|=e.Spec.CoreDamage>0;CoreEnergy=Mathf.Max(0,CoreEnergy-e.Spec.CoreDamage);LastMessage="코어 -"+e.Spec.CoreDamage;}
                }
                if(CoreEnergy<=0) {Phase=SessionPhase.Defeat;ClearProjectiles();RemoveDeadEnemies();LastMessage="방어 실패";if(!Diagnostic)DefeatReached?.Invoke();return;}
                work.RangeIndex.Build(Enemies);
                supportCache.Dirty=true;
                Shoot(playerPosition,Player,"Player",0,ref playerCooldown,ref playerTarget,ref playerAcquired,dt,false,work.RangeIndex,null,1+Augments.PlayerRatePercent/100f);
                if(disposed)return;
                work.Towers.AddRange(Towers);
                foreach(var t in work.Towers)
                {
                    if(t.Kind==TowerKind.Support)continue;
                    float buff=Augments.TowerDamagePercent+supportCache.Get(Towers,t.Position).Damage;
                    float rate=1+TowerRatePercent(t,playerPosition)/100f;
                    if(t.Kind==TowerKind.Frost)Pulse(t,buff,rate,dt,work.RangeIndex);
                    else Shoot(t.Position,t.Stats,t.AttackSource,buff,ref t.Cooldown,ref t.Target,ref t.AcquiredOnce,dt,t.Kind==TowerKind.Multi,work.RangeIndex,t,rate);
                    if(disposed)return;
                }
                work.Projectiles.AddRange(Projectiles);
                MoveProjectiles(dt,work);
                FlushSpawns();
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
                    if(p.Slot.Line){if(MoveLine(p,dt))continue;removals.TryGetValue(p,out int finished);removals[p]=finished+1;continue;}
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
                            if(ProjectileHit(p))continue;
                            RemoveProjectile(p);
                            continue;
                        }
                        if(ProjectileHit(p))continue;
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
        // Homing hit. Piercing shots (레일, 5 꿰뚫는 탄) fly as lines from launch and never home.
        bool ProjectileHit(ProjectileHandle p)
        {
            var slot=p.Slot;HitWithChain(p.Target,p.Damage,p.Source,slot.Payload);return false;
        }
        const float LineHitRadius=.35f;
        readonly List<EnemyHandle> lineHits=new List<EnemyHandle>(8);
        // Straight piercing flight (레일, 관통). Returns true while the shot is still flying.
        bool MoveLine(ProjectileHandle p,float dt)
        {
            var s=p.Slot;float step=Mathf.Min(s.Remaining,s.Speed*dt);
            Vector2 from=s.Position,to=from+s.Direction*step;
            lineHits.Clear();
            foreach(var e in Enemies)
            {
                if(!e.Alive||s.Hits.Contains(e.Id))continue;
                Vector2 d=to-from;float t=d.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(e.Position-from,d)/d.sqrMagnitude):0;
                if(Vector2.Distance(from+d*t,e.Position)<=LineHitRadius)lineHits.Add(e);
            }
            // Insertion sort by distance along the flight (few hits; no comparer allocation).
            for(int i=1;i<lineHits.Count;i++){var x=lineHits[i];float k=Vector2.Dot(x.Position-from,s.Direction);int j=i-1;while(j>=0&&Vector2.Dot(lineHits[j].Position-from,s.Direction)>k){lineHits[j+1]=lineHits[j];j--;}lineHits[j+1]=x;}
            foreach(var e in lineHits)
            {
                if(s.HitsLeft==0)break;
                s.Hits.Add(e.Id);var payload=s.Payload;
                Strike(e,s.Damage,s.Source,payload);
                if(payload.Rail)RaiseCue("hit_rail",e.Position);
                if(s.HitsLeft>0)s.HitsLeft--;
                // 관통: ×falloff per enemy passed; 29 crit → none; 41 일직선 → none and +N% each.
                if(payload.PierceGrowth)s.Damage*=1+Augments.Value(41)/100f;
                else if(!payload.CritNoFalloff)s.Damage*=payload.PierceFalloff>0?payload.PierceFalloff:1;
                if(disposed)return false;
            }
            s.Position=to;s.Remaining-=step;
            if(s.HitsLeft==0)return false;
            if(s.Remaining<=.00001f)
            {
                // 23 레일 반사: returns once along the same line; earlier victims can be hit again.
                if(s.Payload.Rail&&Augments.Has(23)&&!s.Reflected){s.Reflected=true;s.Direction=-s.Direction;s.Remaining=s.Range;s.Hits.Clear();return true;}
                return false;
            }
            return true;
        }
        void CompleteRound()
        {
            ClearProjectiles();fireCells.Clear();
            if(Diagnostic){Phase=SessionPhase.Victory;LastMessage="역할 검증 종료";return;}
            CompletedWave=Wave;int reward=Data.RewardForWave(Wave);
            // 12 수정 조각: next N completions ×mult, then −X per round.
            if(Augments.Has(12)){if(Augments.Rounds12>0){reward=Mathf.FloorToInt(reward*Augments.Value(12,1)/Augments.Count(12));Augments.Rounds12--;}else reward=Mathf.Max(0,reward-(int)Augments.Value(12,2));}
            // 32 수정 광산: income after a delay.
            if(Augments.Has(32)){Augments.Rounds32++;if(Augments.Rounds32>Augments.Value(32)/Augments.Count(32))reward+=(int)Augments.Value(32,1);}
            LastCompletionReward=reward;Crystals+=reward;
            if(Augments.Has(33)){int moved=Mathf.FloorToInt(Gold*Augments.Value(33)/100f);Gold-=moved;Crystals+=moved;}
            if(Augments.Has(35))CoreEnergy=Mathf.Min(CoreMaximum,CoreEnergy+(int)Augments.Value(35));
            if(!coreDamagedThisRound)Augments.CleanRounds++;
            if(Augments.Rounds18>0)Augments.Rounds18--;
            if(Augments.Rounds34>0)Augments.Rounds34--;
            LastMessage=CompletedWave+"라운드 완료 +"+LastCompletionReward+" 수정";
            if(Wave>=LastWave){Phase=SessionPhase.Victory;LastMessage+=" · 1~"+LastWave+"라운드 검증 완료";VictoryReached?.Invoke();return;}
            if(!Data.Waves.Any(row=>row.Wave==Wave+1))throw new InvalidOperationException("Next wave is missing");
            Wave++;PreparationRemaining=Data.PreparationFor(Wave,false);
            int every=CompletedWave>30?(int)Data.Setting("augmentationEveryLate",Data.AugmentationEvery):Data.AugmentationEvery;
            if(every>0&&CompletedWave%every==0&&(!Data.AugmentationRequiresBoss||BossKilled)&&Data.Augments.Count>0)
            {
                rerollsLeft=(int)Data.Setting("augmentRerolls",1)+(int)Augments.ExtraRerolls;
                DrawChoices(null);AugmentationRemaining=Data.Setting("augmentSeconds",30);Phase=SessionPhase.Augmentation;LastMessage+=" · 증강 선택";RoundCompleted?.Invoke();return;
            }
            Phase=SessionPhase.Preparation;
            LastMessage+=" · "+Wave+"라운드 준비";
            RoundCompleted?.Invoke();
        }
        int rerollsLeft;
        public int RerollsLeft=>Phase==SessionPhase.Augmentation?rerollsLeft:0;
        readonly HashSet<int> previousScreen=new HashSet<int>();
        void DrawChoices(ICollection<int> exclude)
        {
            int count=Augments.Has(47)?Augments.Cards:(int)Data.Setting("augmentCards",3);
            choices=AugmentationDeck.Draw(augmentationRandom,Data.Augments,Augments,count,exclude,Augments.PendingPlatinum,
                Data.Setting("augmentGradeSilver",60),Data.Setting("augmentGradeGold",30),Data.Setting("augmentSubcategoryWeight",25));
        }
        // Free reroll per selection; the shown cards are excluded and the timer keeps running.
        public bool RerollAugmentation()
        {
            if(Phase!=SessionPhase.Augmentation||rerollsLeft<=0)return false;
            rerollsLeft--;previousScreen.Clear();foreach(var c in choices)previousScreen.Add(c.Spec.Id);
            DrawChoices(previousScreen);RaiseCue("reroll",Vector2.zero);FlushCues();LastMessage="증강 리롤";return true;
        }
        public bool ChooseAugmentation(int index)
        {
            if(Phase!=SessionPhase.Augmentation||index<0||index>=choices.Length)return false;
            var card=choices[index];
            if(Augments.PendingPlatinum&&card.Number!=48)Augments.PendingPlatinum=false;
            Augments.Apply(card);ApplyImmediate(card);
            choices=Array.Empty<AugmentationCard>();AugmentationRemaining=0;rerollsLeft=0;Phase=SessionPhase.Preparation;PreparationRemaining=Data.PreparationFor(Wave,true);
            LastMessage=card.GradeName+" · "+card.Title+" 선택 · "+Wave+"라운드 준비";return true;
        }
        // QA/test hook: grant a card exactly as if it had been chosen, without the selection screen.
        public bool DebugGrantAugmentation(int number)
        {
            var spec=Data.Augments.FirstOrDefault(x=>x.Number==number);if(spec==null)return false;
            var card=new AugmentationCard(spec);Augments.Apply(card);ApplyImmediate(card);return true;
        }
        // One-time effects at selection; standing effects are read live from Augments.
        void ApplyImmediate(AugmentationCard card)
        {
            var v=card.Spec;
            switch(v.Number)
            {
                case 2:Player.Damage+=v.Value(0);break;
                case 20:Player.Damage+=v.Value(0);Player.Range+=v.Value(1);break;
                case 14:CoreMaximum+=(int)v.Value(0);CoreEnergy+=(int)v.Value(0);break;
                case 18:Gold+=(int)v.Value(0);break;
                case 48:CoreMaximum=Mathf.Max(1,CoreMaximum-(int)v.Value(0));CoreEnergy=Mathf.Min(CoreEnergy,CoreMaximum);break;
                case 50:CoreMaximum=Mathf.Max(1,Mathf.CeilToInt(CoreMaximum*v.Value(0)/100f));CoreEnergy=Mathf.Min(CoreEnergy,CoreMaximum);break;
            }
        }
        // 16 촘촘한 진지 (adjacent tower), 30 화력 지원 (player within N tiles).
        float TowerRatePercent(TowerState t,Vector2 player)
        {
            float percent=0;
            if(Augments.Has(16))foreach(var d in Sides){bool found=false;foreach(var o in Towers)if(o!=t&&o.Position==t.Position+d){found=true;break;}if(found){percent+=Augments.Value(16);break;}}
            if(Augments.Has(30)&&Vector2.Distance(player,t.Position)<=Augments.Value(30)/Augments.Count(30))percent+=Augments.Value(30,1);
            return percent;
        }
        HitPayload LaunchPayload(TowerState tower,CombatStats stats,float buff,float crit)
        {
            var p=new HitPayload{Crit=crit};
            if(tower==null)
            {
                p.Player=true;
                if(Augments.Has(3)){p.SlowPercent=Augments.Value(3)/Augments.Count(3);p.SlowDuration=Augments.Value(3,1)/Augments.Count(3);}
                if(Augments.Has(5)){p.ExtraPierce=Augments.Count(5);p.PierceFalloff=tags.PierceFalloff;}
                if(Augments.Has(7))p.Execute=Augments.Value(7)/Augments.Count(7);
                return p;
            }
            switch(tower.Kind)
            {
                case TowerKind.Flame:p.BurnStacks=1;p.BurnPerStack=stats.BurnPerStack*(1+buff/100f);p.BurnDuration=stats.BurnDuration;break;
                case TowerKind.Single:if(Augments.Has(4)){FlameBurn(out float per,out float duration);p.BurnStacks=Augments.Count(4);p.BurnPerStack=per*(1+buff/100f);p.BurnDuration=duration;}break;
                case TowerKind.Multi:p.Chain=Augments.ChainCount;p.ChainFalloff=Augments.Has(24)?1:tags.ChainFalloff;break;
                case TowerKind.Rail:p.Rail=true;p.PierceFalloff=stats.PierceFalloff>0?stats.PierceFalloff:tags.PierceFalloff;p.PierceGrowth=Augments.Has(41);p.CritNoFalloff=Augments.Has(29)&&crit>1;break;
            }
            return p;
        }
        // 냉기 포탑: every 1/rate seconds, damage + slow to every enemy within the pulse radius. No target choice.
        void Pulse(TowerState t,float buff,float rateMul,float dt,EnemyRangeIndex rangeIndex)
        {
            var stats=t.Stats;float rate=stats.Rate*rateMul;
            var work=shotWorkPool.Count>0?shotWorkPool.Pop():new ShotWork();
            try
            {
                var available=work.Available;
                if(!rangeIndex.TryCollect(t.Position,stats.Range,Enemies,available,work.SpatialCandidates))
                    foreach(var enemy in Enemies)if(enemy.Alive&&Vector2.Distance(enemy.Position,t.Position)<=stats.Range)available.Add(enemy);
                t.Cooldown-=dt;if(available.Count==0||rate<=0){t.Cooldown=Mathf.Max(0,t.Cooldown);return;}if(t.Cooldown>0)return;
                while(t.Cooldown<=.00001f)
                {
                    float crit=RollCrit(ref t.Attacks,ref t.CritBonus);
                    var payload=LaunchPayload(t,stats,buff,crit);payload.SlowPercent=stats.SlowPercent;payload.SlowDuration=stats.SlowDuration;
                    float damage=stats.Damage*(1+buff/100f)*crit;
                    RaiseCue("cold_pulse",t.Position);if(crit>1)RaiseCue("crit",t.Position);
                    TowerPulsed?.Invoke(t.Id,t.Position,stats.Range);
                    foreach(var e in available){if(e.Alive){Strike(e,damage,t.AttackSource,payload);RaiseCue("hit_cold",e.Position);}}
                    rangeIndex.Invalidate();supportCache.Dirty=true;
                    var onAttack=AttackLaunched;if(onAttack!=null){try{onAttack(t.AttackSource,1);}finally{rangeIndex.Invalidate();supportCache.Dirty=true;}}
                    if(disposed)return;
                    float next=t.Cooldown+1/rate;if(next<=t.Cooldown)throw new InvalidOperationException("Attack rate exceeds floating-point timing precision");t.Cooldown=next;
                    available.RemoveAll(e=>!e.Alive);if(available.Count==0){t.Cooldown=Mathf.Max(t.Cooldown,0);break;}
                }
            }
            finally{work.Clear();if(shotWorkPool.Count<MaxRetainedWorkBuffers)shotWorkPool.Push(work);}
        }
        void Shoot(Vector2 position,CombatStats stats,string source,float buff,ref float cooldown,ref EnemyHandle target,ref bool acquired,float dt,bool multi,EnemyRangeIndex rangeIndex,TowerState tower,float rateMul)
        {
            var work=shotWorkPool.Count>0?shotWorkPool.Pop():new ShotWork();
            try
            {
                var available=work.Available;
                if(!rangeIndex.TryCollect(position,stats.Range,Enemies,available,work.SpatialCandidates))
                    foreach(var enemy in Enemies)if(enemy.Alive&&Vector2.Distance(enemy.Position,position)<=stats.Range)available.Add(enemy);
                // 공격 대상 선택(정본)은 플레이어·포탑 공통: 사거리가 비면 다음 진입 때 다시 '가장 먼저 들어온 적',
                // 보스는 일반 적보다 뒤, 보스 조준 중 일반 적이 들어오면 바꿈.
                if(available.Count==0)acquired=false;
                if(target!=default&&(!target.Alive||Vector2.Distance(target.Position,position)>stats.Range))target=default;
                if(target!=default&&target.Kind==EnemyKind.Boss)
                    foreach(var enemy in available)if(enemy.Kind!=EnemyKind.Boss){target=default;break;}
                // 화염 포탑: unburned enemies first, then the shared priority.
                bool flame=tower!=null&&tower.Kind==TowerKind.Flame;
                if(flame&&target!=default&&target.BurnStacks>0)foreach(var enemy in available)if(enemy.BurnStacks==0){target=default;break;}
                if(target==default&&available.Count>0)
                {
                    foreach(var enemy in available)
                    {
                        if(target==default){target=enemy;continue;}
                        if(flame&&(enemy.BurnStacks==0)!=(target.BurnStacks==0)){if(enemy.BurnStacks==0)target=enemy;continue;}
                        if((acquired?ComparePriority(enemy,target,true):CompareInitialTarget(enemy,target))<0)target=enemy;
                    }
                    acquired=true;
                }
                float rate=stats.Rate*rateMul;
                cooldown-=dt;if(available.Count==0||rate<=0){cooldown=Mathf.Max(0,cooldown);return;}if(cooldown>0)return;
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
                    float crit=tower!=null?RollCrit(ref tower.Attacks,ref tower.CritBonus):RollCrit(ref playerAttacks,ref playerCritBonus);
                    var payload=LaunchPayload(tower,stats,buff,crit);
                    if(crit>1)RaiseCue("crit",position);
                    if(tower!=null&&tower.Kind==TowerKind.Flame)RaiseCue("flame_shot",position);
                    if(tower!=null&&tower.Kind==TowerKind.Rail)RaiseCue("rail_shot",position);
                    for(int i=0;i<targetCount;i++)
                    {
                        var e=targets[i];if(e==default)continue;int shots=1;
                        for(int shot=0;shot<shots;shot++)
                        {
                            var projectile=RentProjectile(e,position,stats.Damage*(1+buff/100f)*crit,stats.ProjectileSpeed,source);
                            projectile.Slot.Payload=payload;
                            // 관통(정본): 대상을 따라가지 않고 사거리 끝까지 직진. 레일은 전부, 5 꿰뚫는 탄은 1+N명.
                            if(payload.Rail||payload.ExtraPierce>0)
                            {
                                var slot=projectile.Slot;var direction=e.Position-position;
                                slot.Line=true;slot.Direction=direction.sqrMagnitude>.000001f?direction.normalized:Vector2.right;slot.Remaining=slot.Range=stats.Range;slot.HitsLeft=payload.Rail?-1:1+payload.ExtraPierce;slot.Target=default;
                            }
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
                    float nextCooldown=cooldown+1/rate;if(nextCooldown<=cooldown)throw new InvalidOperationException("Attack rate exceeds floating-point timing precision");cooldown=nextCooldown;
                }
                }
            finally{work.Clear();if(shotWorkPool.Count<MaxRetainedWorkBuffers)shotWorkPool.Push(work);}
        }
        static int ComparePriority(EnemyHandle a,EnemyHandle b,bool tower)=>CombatRules.CompareTargetPriority(a,b,tower);
        static int CompareInitialTarget(EnemyHandle a,EnemyHandle b)
        {
            int order=(a.Kind==EnemyKind.Boss?1:0).CompareTo(b.Kind==EnemyKind.Boss?1:0);if(order!=0)return order;
            return a.Id.CompareTo(b.Id);
        }
        public int ApplyDamage(EnemyHandle target,float damage,string source)
        {
            if(disposed||target==default||!ReferenceEquals(target.Owner,this)||!target.IsValid||!target.Alive)return 0;
            BeginModelOperation();try
            {
                var payload=source=="Player"?LaunchPayload(null,Player,0,1):default;
                int dealt=Strike(target,damage,source,payload);
                if(operationDepth==1)FlushSpawns();
                return dealt;
            }
            finally{EndModelOperation();}
        }
    }
}


