using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace CoreGuardian
{
    public enum EnemyKind { Attack, Defense, Buff, Boss }
    public enum TowerKind { Single, Multi, Support }
    public enum SessionPhase { Preparation, Combat, Victory, Defeat, Augmentation }
    public sealed class EnemySpec { public EnemyKind Kind; public int Health, CoreDamage, Gold; public float Speed, Defense, BuffRadius, BuffSpeed; }
    public sealed class TowerSpec { public TowerKind Kind; public int Cost; public float Damage, Rate, Range, ProjectileSpeed, BuffDamage; }
    public sealed class UpgradeSpec { public string Target, Stat; public float Delta; public int Cost, CostIncrement; }
    public sealed class WaveSpec { public int Wave, Count; public EnemyKind Kind; public float Interval, Start; public EnemySpec Enemy; }
    public sealed class CombatStats
    {
        public float Damage, Rate, Range, ProjectileSpeed, BuffDamage;
        public readonly Dictionary<string,int> UpgradeSteps = new Dictionary<string,int>();
        public CombatStats Clone() { return new CombatStats { Damage=Damage,Rate=Rate,Range=Range,ProjectileSpeed=ProjectileSpeed,BuffDamage=BuffDamage }; }
        public void Add(string stat,float value) { switch(stat) { case "damage": Damage+=value;break;case "rate":Rate+=value;break;case "range":Range+=value;break;case "projectileSpeed":ProjectileSpeed+=value;break;case "buffDamage":BuffDamage+=value;break;default:throw new ArgumentException("Unknown stat: "+stat); } }
    }
    public sealed class CombatData
    {
        public readonly Dictionary<EnemyKind,EnemySpec> Enemies = new Dictionary<EnemyKind,EnemySpec>();
        public readonly Dictionary<TowerKind,TowerSpec> Towers = new Dictionary<TowerKind,TowerSpec>();
        public readonly List<UpgradeSpec> Upgrades = new List<UpgradeSpec>();
        public readonly List<WaveSpec> Waves = new List<WaveSpec>();
        public readonly Dictionary<int,int> WaveRewards = new Dictionary<int,int>();
        public int RewardForWave(int wave) => WaveRewards.TryGetValue(wave,out var reward)?reward:WaveReward;
        public int CoreEnergy, StartGold, StartCrystals, WaveReward, PlayerFirstCost, PlayerSecondIncrease, PlayerIncreaseStep, DiagnosticGold, DiagnosticCrystals;
        public int PlayerFirstUpgradeDiscount, TowerFirstUpgradeDiscount;
        public int AugmentationEvery=10;
        public bool AugmentationRequiresBoss=true;
        public EnemySpec EnemyForWave(int wave,EnemyKind kind)=>Waves.FirstOrDefault(w=>w.Wave==wave&&w.Kind==kind)?.Enemy??Enemies[kind];
        public float PreparationSeconds, DiagnosticStartDistance, DiagnosticSpacing;
        public CombatStats Player;
        static float F(string s) { float v; if(!float.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out v)||float.IsNaN(v)||float.IsInfinity(v)||v<0) throw new ArgumentException("Invalid non-negative number: "+s);return v; }
        static int I(string s) { float f=F(s);if(f!=Mathf.Floor(f))throw new ArgumentException("Expected integer: "+s);return (int)f; }
        static IEnumerable<string[]> Rows(string text,int columns,int alternate=0)
        {
            var lines=text.Replace("\r","").Split('\n').Where(x=>!string.IsNullOrWhiteSpace(x)&&!x.TrimStart().StartsWith("#")).ToArray();
            if(lines.Length<2)throw new ArgumentException("CSV has no data rows");
            var ids=new HashSet<string>(); foreach(var l in lines.Skip(1)) { var r=l.Split(',').Select(x=>x.Trim()).ToArray();if((r.Length!=columns&&r.Length!=alternate)||r.Any(x=>x.Length==0)||!ids.Add(r[0]))throw new ArgumentException("Invalid/duplicate CSV row: "+l);yield return r; }
        }
        static T Kind<T>(string value) where T:struct
        {
            T result;
            if(!Enum.TryParse<T>(value,true,out result)||!Enum.IsDefined(typeof(T),result)) throw new ArgumentException("Unknown "+typeof(T).Name+": "+value);
            return result;
        }
        public static CombatData Load(string enemies,string towers,string stage,string upgrades,string waves)
        {
            var d=new CombatData();
            foreach(var r in Rows(enemies,9)) { var kind=Kind<EnemyKind>(r[0]); if(d.Enemies.ContainsKey(kind))throw new ArgumentException("Duplicate enemy kind"); d.Enemies.Add(kind,new EnemySpec {Kind=kind,Health=I(r[1]),Speed=F(r[2]),CoreDamage=I(r[3]),Gold=I(r[4]),Defense=F(r[5]),BuffRadius=F(r[6]),BuffSpeed=F(r[7])}); if(r[8]!="provisional")throw new ArgumentException("Unknown balance status"); }
            foreach(var r in Rows(towers,8)) { var kind=Kind<TowerKind>(r[0]);d.Towers.Add(kind,new TowerSpec{Kind=kind,Cost=I(r[1]),Damage=F(r[2]),Rate=F(r[3]),Range=F(r[4]),ProjectileSpeed=F(r[5]),BuffDamage=F(r[6])});if(r[7]!="provisional")throw new ArgumentException("Unknown balance status"); }
            var settings=Rows(stage,2).ToDictionary(r=>r[0],r=>r[1]);
            d.CoreEnergy=I(settings["coreEnergy"]);d.StartGold=I(settings["startGold"]);d.StartCrystals=I(settings["startCrystals"]);d.WaveReward=I(settings["waveReward"]);d.PreparationSeconds=F(settings["preparationSeconds"]);
            foreach(var setting in settings.Where(x=>x.Key.StartsWith("waveReward")&&x.Key!="waveReward"))
            {
                int wave=I(setting.Key.Substring("waveReward".Length));
                if(wave<=0)throw new ArgumentException("Invalid reward wave");
                d.WaveRewards.Add(wave,I(setting.Value));
            }
            d.PlayerFirstCost=I(settings["playerFirstCost"]);d.PlayerSecondIncrease=I(settings["playerSecondIncrease"]);d.PlayerIncreaseStep=I(settings["playerIncreaseStep"]);
            d.PlayerFirstUpgradeDiscount=settings.TryGetValue("playerFirstUpgradeDiscount",out var playerDiscount)?I(playerDiscount):0;
            d.TowerFirstUpgradeDiscount=settings.TryGetValue("towerFirstUpgradeDiscount",out var towerDiscount)?I(towerDiscount):0;
            d.AugmentationEvery=settings.TryGetValue("augmentationEvery",out var every)?I(every):10;
            d.AugmentationRequiresBoss=!settings.TryGetValue("augmentationRequiresBoss",out var bossRequired)||I(bossRequired)==1;
            if(d.AugmentationEvery<1||settings.TryGetValue("augmentationRequiresBoss",out var flag)&&I(flag)>1)throw new ArgumentException("Invalid augmentation schedule");
            d.DiagnosticStartDistance=F(settings["diagnosticStartDistance"]);d.DiagnosticSpacing=F(settings["diagnosticSpacing"]);d.DiagnosticGold=I(settings["diagnosticGold"]);d.DiagnosticCrystals=I(settings["diagnosticCrystals"]);
            d.Player=new CombatStats{Damage=F(settings["playerDamage"]),Rate=F(settings["playerRate"]),Range=F(settings["playerRange"]),ProjectileSpeed=F(settings["playerProjectileSpeed"])};
            foreach(var r in Rows(upgrades,6)) { var u=new UpgradeSpec{Target=r[1],Stat=r[2],Delta=F(r[3]),Cost=I(r[4]),CostIncrement=I(r[5])}; u.Target=string.Equals(u.Target,"Player",StringComparison.OrdinalIgnoreCase)?"Player":Kind<TowerKind>(u.Target).ToString();if(!new[]{"damage","rate","range","projectileSpeed","buffDamage"}.Contains(u.Stat))throw new ArgumentException("Unknown upgrade stat");if(d.Upgrades.Any(x=>x.Target==u.Target&&x.Stat==u.Stat))throw new ArgumentException("Duplicate target/stat upgrade");d.Upgrades.Add(u); }
            if(d.PlayerFirstUpgradeDiscount>0&&d.PlayerFirstUpgradeDiscount>=d.PlayerFirstCost||d.TowerFirstUpgradeDiscount>0&&d.Upgrades.Any(u=>u.Target!="Player"&&u.Cost<=d.TowerFirstUpgradeDiscount))throw new ArgumentException("First upgrade discount must leave a positive price");
            foreach(var r in Rows(waves,6,10))
            {
                var kind=Kind<EnemyKind>(r[2]);if(!d.Enemies.ContainsKey(kind))throw new ArgumentException("Missing enemy reference");
                var wave=new WaveSpec{Wave=I(r[1]),Kind=kind,Count=I(r[3]),Interval=F(r[4]),Start=F(r[5])};
                if(r.Length==10)
                {
                    var b=d.Enemies[kind];
                    wave.Enemy=new EnemySpec{Kind=kind,Health=I(r[6]),Defense=F(r[7]),Speed=F(r[8]),Gold=I(r[9]),CoreDamage=b.CoreDamage,BuffRadius=b.BuffRadius,BuffSpeed=b.BuffSpeed};
                    if(wave.Enemy.Health<=0||wave.Enemy.Speed<=0||wave.Enemy.Defense>=100)throw new ArgumentException("Invalid wave enemy stats");
                }
                d.Waves.Add(wave);
            }
            if(!d.Waves.Any(w=>w.Wave==1)||!new[]{EnemyKind.Attack,EnemyKind.Defense,EnemyKind.Buff}.All(d.Enemies.ContainsKey)||d.Towers.Count!=3||d.CoreEnergy<=0||d.Player.Rate<=0||d.Player.ProjectileSpeed<=0||d.Enemies.Values.Any(x=>x.Health<=0||x.Speed<=0)||d.Towers.Values.Any(x=>x.Kind!=TowerKind.Support&&(x.Rate<=0||x.ProjectileSpeed<=0))||d.Waves.Any(x=>x.Wave<=0||x.Count<=0||x.Interval<=0))throw new ArgumentException("Incomplete or invalid combat data");
            return d;
        }
        public int PlayerUpgradePrice(int step) { if(step<1)throw new ArgumentOutOfRangeException(nameof(step));return checked(PlayerFirstCost+(step-1)*PlayerSecondIncrease+PlayerIncreaseStep*(step-1)*(step-2)/2-(step==1?PlayerFirstUpgradeDiscount:0)); }
    }
    public static class CombatRules
    {
        public static int Damage(float damage,float defense) { return Mathf.FloorToInt(Mathf.Max(0,damage*(1-defense/100f))+.5f); }
        sealed class TargetComparer : IComparer<EnemyHandle>
        {
            readonly bool tower;
            public TargetComparer(bool tower){this.tower=tower;}
            public int Compare(EnemyHandle a,EnemyHandle b)=>CompareTargetPriority(a,b,tower);
        }
        static readonly IComparer<EnemyHandle> playerPriority=new TargetComparer(false),towerPriority=new TargetComparer(true);
        static int TargetRank(EnemyHandle enemy,bool tower)=>enemy.Kind==EnemyKind.Defense?0:enemy.Kind==EnemyKind.Buff?1:tower&&enemy.Kind==EnemyKind.Boss?3:2;
        public static int CompareTargetPriority(EnemyHandle a,EnemyHandle b,bool tower)
        {
            int order=TargetRank(a,tower).CompareTo(TargetRank(b,tower));if(order!=0)return order;
            order=a.Health.CompareTo(b.Health);if(order!=0)return order;
            // Session derives PathRemaining from route length minus travelled length. It is
            // distance along every remaining X/Y segment, never straight-line distance to core.
            order=a.PathRemaining.CompareTo(b.PathRemaining);return order!=0?order:a.Id.CompareTo(b.Id);
        }
        public static EnemyHandle Priority(IEnumerable<EnemyHandle> enemies)=>enemies.Where(e=>e.Alive).OrderBy(e=>e,playerPriority).FirstOrDefault();
        public static IOrderedEnumerable<EnemyHandle> TowerPriorityOrder(IEnumerable<EnemyHandle> enemies)=>enemies.Where(e=>e.Alive).OrderBy(e=>e,towerPriority);
    }
    public sealed class TowerState
    {
        public int Id,Spent;public TowerKind Kind;public Vector2Int Position;public CombatStats Stats;internal float Cooldown;internal EnemyHandle Target;internal bool AcquiredOnce;
        int sourceId;string attackSource,supportSource;
        void RefreshSources(){if(attackSource!=null&&sourceId==Id&&Id>=0)return;sourceId=Id;attackSource="Tower:"+Id;supportSource="Support:"+Id;}
        public string AttackSource {get{RefreshSources();return attackSource;}}
        public string SupportSource {get{RefreshSources();return supportSource;}}
    }
}



