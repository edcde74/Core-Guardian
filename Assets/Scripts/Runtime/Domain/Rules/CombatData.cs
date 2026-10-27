using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CoreGuardian
{
    // Appended values only: existing indices (pools, serialized art arrays) must not move.
    public enum EnemyKind { Attack, Defense, Buff, Boss, Charger, Splitter, Healer, Armored, Flyer, SplitChild }
    public enum TowerKind { Single, Multi, Support, Frost, Flame, Rail }
    public enum SessionPhase { Preparation, Combat, Victory, Defeat, Augmentation }
    public sealed class EnemySpec
    {
        public EnemyKind Kind; public int Health, CoreDamage, Gold; public float Speed, Defense, BuffRadius, BuffSpeed;
        // Data table (2026-10-26 spec). Legacy rows leave these at their defaults.
        public int Id; public string Key;
        public float HealthMul, GoldMul, DefenseRefMul, HealInterval, HealRadius, HealPercent, RageAt, RageSpeed;
        public int DefenseRefId, SplitIntoId, SplitCount; public bool Flying;
        // Boss abilities: shield thresholds (% of max health), summons (enemy ID x count per cycle).
        public float[] ShieldAt=Array.Empty<float>(); public float ShieldDefense, ShieldSeconds;
        public int SummonId, SummonCount; public float SummonInterval, SummonRageInterval;
        public float RagePercent=>RageAt>0?RageAt:50;
        public float RageBoost=>RageSpeed>0?RageSpeed:30;
        public EnemySpec Clone()=>(EnemySpec)MemberwiseClone();
    }
    public sealed class TowerSpec
    {
        public TowerKind Kind; public int Cost; public float Damage, Rate, Range, ProjectileSpeed, BuffDamage;
        public int Id, MaxTargets; public string Key, Tag, AttackType, Requires;
        public float SlowPercent, SlowDuration, BurnPerStack, BurnDuration, PierceFalloff;
    }
    public sealed class UpgradeSpec { public string Target, Stat; public float Delta; public int Cost, CostIncrement; public int Id, TargetId, Group; }
    public sealed class WaveSpec { public int Wave, Count; public EnemyKind Kind; public float Interval, Start; public EnemySpec Enemy; public int Id, EnemyId; }
    public sealed class AugmentSpec
    {
        public int Id, Number; public string Name, Category, Personality, Effect, Text; public AugmentationGrade Grade;
        public string[] Subcategories; public float[] Values; public bool Repeatable;
        public float Value(int index)=>index<Values.Length?Values[index]:0;
    }
    public sealed class EffectTagSpec { public int Id; public string Tag, Param, Unit; public float Value; public float? BossValue; public bool BossImmune; }
    public sealed class SkillSpec { public int Id, Owner; public string Key, Input, Phase; public float DamageMul, Radius, Cooldown; public bool ResetOnRoundStart, Android; }
    public sealed class CombatStats
    {
        public float Damage, Rate, Range, ProjectileSpeed, BuffDamage;
        public float SlowPercent, SlowDuration, BurnPerStack, BurnDuration, PierceFalloff;
        public readonly Dictionary<string,int> UpgradeSteps = new Dictionary<string,int>();
        public CombatStats Clone() { return new CombatStats { Damage=Damage,Rate=Rate,Range=Range,ProjectileSpeed=ProjectileSpeed,BuffDamage=BuffDamage,SlowPercent=SlowPercent,SlowDuration=SlowDuration,BurnPerStack=BurnPerStack,BurnDuration=BurnDuration,PierceFalloff=PierceFalloff }; }
        public static bool IsStat(string stat) { switch(stat){case "damage":case "rate":case "range":case "projectileSpeed":case "buffDamage":case "slowPercent":case "burnPerStack":case "burnDuration":case "pierceFalloff":return true;default:return false;} }
        public void Add(string stat,float value)
        {
            switch(stat)
            {
                case "damage": Damage+=value;break;case "rate":Rate+=value;break;case "range":Range+=value;break;case "projectileSpeed":ProjectileSpeed+=value;break;case "buffDamage":BuffDamage+=value;break;
                case "slowPercent":SlowPercent+=value;break;case "burnPerStack":BurnPerStack+=value;break;case "burnDuration":BurnDuration+=value;break;case "pierceFalloff":PierceFalloff+=value;break;
                default:throw new ArgumentException("Unknown stat: "+stat);
            }
        }
    }
    public sealed class CombatData
    {
        public readonly Dictionary<EnemyKind,EnemySpec> Enemies = new Dictionary<EnemyKind,EnemySpec>();
        public readonly Dictionary<TowerKind,TowerSpec> Towers = new Dictionary<TowerKind,TowerSpec>();
        public readonly List<UpgradeSpec> Upgrades = new List<UpgradeSpec>();
        public readonly List<WaveSpec> Waves = new List<WaveSpec>();
        public readonly Dictionary<int,int> WaveRewards = new Dictionary<int,int>();
        // Numeric-ID tables (empty for legacy string-ID data).
        public readonly Dictionary<int,EnemySpec> EnemiesById = new Dictionary<int,EnemySpec>();
        public readonly Dictionary<int,TowerSpec> TowersById = new Dictionary<int,TowerSpec>();
        public readonly List<AugmentSpec> Augments = new List<AugmentSpec>();
        public readonly Dictionary<int,EffectTagSpec> EffectTags = new Dictionary<int,EffectTagSpec>();
        public readonly List<SkillSpec> Skills = new List<SkillSpec>();
        public readonly Dictionary<string,float> Settings = new Dictionary<string,float>();
        public bool NumericIds {get;private set;}
        public int RewardForWave(int wave) => WaveRewards.TryGetValue(wave,out var reward)?reward:WaveReward;
        public int CoreEnergy, StartGold, StartCrystals, WaveReward, PlayerFirstCost, PlayerSecondIncrease, PlayerIncreaseStep, DiagnosticGold, DiagnosticCrystals;
        public int PlayerFirstUpgradeDiscount, TowerFirstUpgradeDiscount;
        public int AugmentationEvery=10;
        public bool AugmentationRequiresBoss=true;
        public EnemySpec EnemyForWave(int wave,EnemyKind kind)=>Waves.FirstOrDefault(w=>w.Wave==wave&&w.Kind==kind)?.Enemy??Enemies[kind];
        public float PreparationSeconds, DiagnosticStartDistance, DiagnosticSpacing;
        // Long preparation: before a boss round and right after an augmentation choice. Missing key keeps the normal length.
        public float PreparationSecondsLong;
        public bool IsBossWave(int wave){foreach(var w in Waves)if(w.Wave==wave&&w.Kind==EnemyKind.Boss)return true;return false;}
        public float PreparationFor(int wave,bool afterAugmentation)=>afterAugmentation||IsBossWave(wave)?PreparationSecondsLong:PreparationSeconds;
        public float Setting(string key,float fallback)=>Settings.TryGetValue(key,out var value)?value:fallback;
        public CombatStats Player;
        // Effect tag lookup with a fallback when the sheet lacks the row; NaN marks boss immunity.
        public float Tag(string tag,string param,bool boss,float fallback)
        {
            foreach(var t in EffectTags.Values)if(t.Tag==tag&&t.Param==param)return boss?(t.BossImmune?float.NaN:t.BossValue??t.Value):t.Value;
            return fallback;
        }
        // Effect tag lookup; returns null when the boss is immune.
        public float? TagValue(string tag,string param,bool boss)
        {
            foreach(var t in EffectTags.Values)if(t.Tag==tag&&t.Param==param)return boss?(t.BossImmune?(float?)null:t.BossValue??t.Value):t.Value;
            throw new KeyNotFoundException("Missing effect tag "+tag+"."+param);
        }
        static float F(string s) { float v; if(!float.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out v)||float.IsNaN(v)||float.IsInfinity(v)||v<0) throw new ArgumentException("Invalid non-negative number: "+s);return v; }
        // Double parse: 8-digit wave IDs exceed float's exact integer range (2^24).
        static int I(string s) { double v; if(!double.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out v)||double.IsNaN(v)||double.IsInfinity(v)||v<0||v>int.MaxValue) throw new ArgumentException("Invalid non-negative number: "+s);if(v!=Math.Floor(v))throw new ArgumentException("Expected integer: "+s);return (int)v; }
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
        static bool IsNumericTable(string text)
        {
            foreach(var raw in text.Replace("\r","").Split('\n'))
            {
                var line=raw.Trim();if(line.Length==0||line.StartsWith("#"))continue;
                var header=SplitCsv(line);return header.Length>1&&header[1]=="key";
            }
            return false;
        }
        public static CombatData Load(string enemies,string towers,string stage,string upgrades,string waves)
            =>IsNumericTable(enemies)?LoadTables(enemies,towers,stage,upgrades,waves,Sheet("player"),Sheet("augments"),Sheet("effectTags"),Sheet("skills")):LoadLegacy(enemies,towers,stage,upgrades,waves);
        // Sheets added by the 2026-10-26 data spec, read beside the scene-referenced CSVs.
        public static string Sheet(string name)=>Resources.Load<TextAsset>("CoreGuardian/"+name)?.text;
        public static CombatData Load(string enemies,string towers,string stage,string upgrades,string waves,string player,string augments,string effectTags,string skills)
            =>IsNumericTable(enemies)?LoadTables(enemies,towers,stage,upgrades,waves,player,augments,effectTags,skills):LoadLegacy(enemies,towers,stage,upgrades,waves);
        static CombatData LoadLegacy(string enemies,string towers,string stage,string upgrades,string waves)
        {
            var d=new CombatData();
            foreach(var r in Rows(enemies,9)) { var kind=Kind<EnemyKind>(r[0]); if(d.Enemies.ContainsKey(kind))throw new ArgumentException("Duplicate enemy kind"); d.Enemies.Add(kind,new EnemySpec {Kind=kind,Key=kind.ToString(),Health=I(r[1]),Speed=F(r[2]),CoreDamage=I(r[3]),Gold=I(r[4]),Defense=F(r[5]),BuffRadius=F(r[6]),BuffSpeed=F(r[7])}); if(r[8]!="provisional")throw new ArgumentException("Unknown balance status"); }
            foreach(var r in Rows(towers,8)) { var kind=Kind<TowerKind>(r[0]);d.Towers.Add(kind,new TowerSpec{Kind=kind,Key=kind.ToString(),Cost=I(r[1]),Damage=F(r[2]),Rate=F(r[3]),Range=F(r[4]),ProjectileSpeed=F(r[5]),BuffDamage=F(r[6])});if(r[7]!="provisional")throw new ArgumentException("Unknown balance status"); }
            var settings=Rows(stage,2).ToDictionary(r=>r[0],r=>r[1]);
            d.ApplyStage(settings);
            d.Player=new CombatStats{Damage=F(settings["playerDamage"]),Rate=F(settings["playerRate"]),Range=F(settings["playerRange"]),ProjectileSpeed=F(settings["playerProjectileSpeed"])};
            foreach(var r in Rows(upgrades,6)) { var u=new UpgradeSpec{Target=r[1],Stat=r[2],Delta=F(r[3]),Cost=I(r[4]),CostIncrement=I(r[5])}; u.Target=string.Equals(u.Target,"Player",StringComparison.OrdinalIgnoreCase)?"Player":Kind<TowerKind>(u.Target).ToString();d.AddUpgrade(u); }
            foreach(var r in Rows(waves,6,10))
            {
                var kind=Kind<EnemyKind>(r[2]);if(!d.Enemies.ContainsKey(kind))throw new ArgumentException("Missing enemy reference");
                var wave=new WaveSpec{Wave=I(r[1]),Kind=kind,Count=I(r[3]),Interval=F(r[4]),Start=F(r[5])};
                if(r.Length==10)wave.Enemy=WaveEnemy(d.Enemies[kind],r[6],r[7],r[8],r[9]);
                d.Waves.Add(wave);
            }
            d.ValidateCore();
            // Legacy balance fixtures still play with the live card list and effect-tag values.
            d.LoadRuleSheets(Sheet("augments"),Sheet("effectTags"),Sheet("skills"),false);
            return d;
        }
        void ApplyStage(Dictionary<string,string> settings)
        {
            var d=this;
            foreach(var pair in settings)if(float.TryParse(pair.Value,NumberStyles.Float,CultureInfo.InvariantCulture,out var raw))d.Settings[pair.Key]=raw;
            d.CoreEnergy=I(settings["coreEnergy"]);d.StartGold=I(settings["startGold"]);d.StartCrystals=I(settings["startCrystals"]);d.WaveReward=I(settings["waveReward"]);d.PreparationSeconds=F(settings["preparationSeconds"]);d.PreparationSecondsLong=settings.TryGetValue("preparationSecondsLong",out var longPreparation)?F(longPreparation):d.PreparationSeconds;
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
        }
        void AddUpgrade(UpgradeSpec u)
        {
            if(!CombatStats.IsStat(u.Stat))throw new ArgumentException("Unknown upgrade stat");
            if(Upgrades.Any(x=>x.Target==u.Target&&x.Stat==u.Stat))throw new ArgumentException("Duplicate target/stat upgrade");
            Upgrades.Add(u);
        }
        static EnemySpec WaveEnemy(EnemySpec b,string health,string defense,string speed,string gold)
        {
            var e=b.Clone();e.Health=I(health);e.Defense=F(defense);e.Speed=F(speed);e.Gold=I(gold);
            if(e.Health<=0||e.Speed<=0||e.Defense>=100)throw new ArgumentException("Invalid wave enemy stats");
            return e;
        }
        void ValidateCore()
        {
            var d=this;
            if(d.PlayerFirstUpgradeDiscount>0&&d.PlayerFirstUpgradeDiscount>=d.PlayerFirstCost||d.TowerFirstUpgradeDiscount>0&&d.Upgrades.Any(u=>u.Target!="Player"&&u.Cost<=d.TowerFirstUpgradeDiscount))throw new ArgumentException("First upgrade discount must leave a positive price");
            if(!d.Waves.Any(w=>w.Wave==1)||!new[]{EnemyKind.Attack,EnemyKind.Defense,EnemyKind.Buff}.All(d.Enemies.ContainsKey)||!new[]{TowerKind.Single,TowerKind.Multi,TowerKind.Support}.All(d.Towers.ContainsKey)||d.CoreEnergy<=0||d.Player.Rate<=0||d.Player.ProjectileSpeed<=0||d.Enemies.Values.Any(x=>x.Health<=0||x.Speed<=0)||d.Towers.Values.Any(x=>x.Kind!=TowerKind.Support&&(x.Rate<=0||x.Kind!=TowerKind.Frost&&x.ProjectileSpeed<=0))||d.Waves.Any(x=>x.Wave<=0||x.Count<=0||x.Interval<=0))throw new ArgumentException("Incomplete or invalid combat data");
        }
        public int PlayerUpgradePrice(int step) { if(step<1)throw new ArgumentOutOfRangeException(nameof(step));return checked(PlayerFirstCost+(step-1)*PlayerSecondIncrease+PlayerIncreaseStep*(step-1)*(step-2)/2-(step==1?PlayerFirstUpgradeDiscount:0)); }

        // ---------- Numeric-ID tables ----------
        internal sealed class Table
        {
            public string Name;public string[] Header;public readonly List<string[]> Rows=new List<string[]>();
            readonly Dictionary<string,int> columns=new Dictionary<string,int>();
            public Table(string name,string text,params string[] required)
            {
                Name=name;
                if(text==null)throw new ArgumentException("Missing CSV: "+name);
                foreach(var raw in text.Replace("\r","").Split('\n'))
                {
                    var line=raw.Trim();if(line.Length==0||line.StartsWith("#"))continue;
                    var cells=SplitCsv(line);
                    if(Header==null){Header=cells;for(int i=0;i<cells.Length;i++)columns[cells[i]]=i;continue;}
                    if(cells.Length!=Header.Length)throw new ArgumentException(name+": wrong column count: "+line);
                    Rows.Add(cells);
                }
                if(Header==null||Rows.Count==0)throw new ArgumentException(name+": CSV has no data rows");
                foreach(var column in required)if(!columns.ContainsKey(column))throw new ArgumentException(name+": missing column "+column);
            }
            public bool Has(string column)=>columns.ContainsKey(column);
            public string S(string[] row,string column)=>columns.TryGetValue(column,out int i)?row[i]:"";
            public string Required(string[] row,string column){var v=S(row,column);if(v.Length==0)throw new ArgumentException(Name+": empty "+column+" in row "+row[0]);return v;}
            public float F(string[] row,string column)=>CombatData.F(Required(row,column));
            public float F(string[] row,string column,float fallback){var v=S(row,column);return v.Length==0?fallback:CombatData.F(v);}
            public int I(string[] row,string column)=>CombatData.I(Required(row,column));
            public int I(string[] row,string column,int fallback){var v=S(row,column);return v.Length==0?fallback:CombatData.I(v);}
            public bool B(string[] row,string column){int v=I(row,column,0);if(v>1)throw new ArgumentException(Name+": expected 0/1 in "+column);return v==1;}
            public int Id(string[] row,int min,int max)
            {
                int id=I(row,"id");if(id<min||id>max)throw new ArgumentException(Name+": ID "+id+" outside "+min+"~"+max);return id;
            }
        }
        internal static string[] SplitCsv(string line)
        {
            var cells=new List<string>();var cell=new StringBuilder();bool quoted=false;
            for(int i=0;i<line.Length;i++)
            {
                char c=line[i];
                if(quoted){if(c=='"'){if(i+1<line.Length&&line[i+1]=='"'){cell.Append('"');i++;}else quoted=false;}else cell.Append(c);}
                else if(c=='"')quoted=true;
                else if(c==','){cells.Add(cell.ToString().Trim());cell.Clear();}
                else cell.Append(c);
            }
            if(quoted)throw new ArgumentException("Unterminated quote: "+line);
            cells.Add(cell.ToString().Trim());return cells.ToArray();
        }
        static void Unique(HashSet<int> ids,int id,string table){if(!ids.Add(id))throw new ArgumentException(table+": duplicate ID "+id);}
        static (int id,float value) Reference(string text,string table)
        {
            var parts=text.Split('*');if(parts.Length!=2)throw new ArgumentException(table+": reference must be ID*value: "+text);
            return (I(parts[0].Trim()),F(parts[1].Trim()));
        }
        static CombatData LoadTables(string enemies,string towers,string stage,string upgrades,string waves,string player,string augments,string effectTags,string skills)
        {
            var d=new CombatData{NumericIds=true};
            // stage
            var stageTable=new Table("stage",stage,"id","key","value");var ids=new HashSet<int>();var settings=new Dictionary<string,string>();
            foreach(var r in stageTable.Rows){Unique(ids,stageTable.Id(r,42000,42999),"stage");var key=stageTable.Required(r,"key");if(settings.ContainsKey(key))throw new ArgumentException("stage: duplicate key "+key);settings.Add(key,stageTable.Required(r,"value"));}
            d.ApplyStage(settings);
            // player
            if(player!=null)
            {
                var t=new Table("player",player,"id","damage","rate","range","projectileSpeed");if(t.Rows.Count!=1)throw new ArgumentException("player: expected one row");
                var r=t.Rows[0];t.Id(r,10000,10999);d.Player=new CombatStats{Damage=t.F(r,"damage"),Rate=t.F(r,"rate"),Range=t.F(r,"range"),ProjectileSpeed=t.F(r,"projectileSpeed")};
            }
            else if(settings.ContainsKey("playerDamage"))d.Player=new CombatStats{Damage=F(settings["playerDamage"]),Rate=F(settings["playerRate"]),Range=F(settings["playerRange"]),ProjectileSpeed=F(settings["playerProjectileSpeed"])};
            else throw new ArgumentException("Missing player data");
            // enemies
            var et=new Table("enemies",enemies,"id","key","health","speed","coreDamage","gold","defense","buffRadius","buffSpeed","status");ids.Clear();
            var pending=new List<(EnemySpec spec,string[] row)>();
            foreach(var r in et.Rows)
            {
                int id=et.Id(r,11000,11999);Unique(ids,id,"enemies");var key=et.Required(r,"key");
                EnemyKind kind;
                if(id>=11101&&id<=11199)kind=EnemyKind.Boss;
                else{kind=Kind<EnemyKind>(key);if(kind==EnemyKind.Boss)throw new ArgumentException("enemies: boss outside 11101~11199");}
                if(kind==EnemyKind.SplitChild!=(id>=11201))throw new ArgumentException("enemies: derived enemy ID range mismatch "+id);
                if(et.S(r,"status")!="provisional")throw new ArgumentException("Unknown balance status");
                var e=new EnemySpec{Id=id,Key=key,Kind=kind,Speed=et.F(r,"speed"),CoreDamage=et.I(r,"coreDamage"),BuffRadius=et.F(r,"buffRadius"),BuffSpeed=et.F(r,"buffSpeed"),
                    HealthMul=et.F(r,"healthMul",0),GoldMul=et.F(r,"goldMul",0),HealInterval=et.F(r,"healInterval",0),HealRadius=et.F(r,"healRadius",0),HealPercent=et.F(r,"healPercent",0),
                    Flying=et.B(r,"flying"),RageAt=et.F(r,"rageAt",0),RageSpeed=et.F(r,"rageSpeed",0),Health=et.I(r,"health",0),Gold=et.I(r,"gold",-1),Defense=et.F(r,"defense",-1)};
                var defenseRef=et.S(r,"defenseRef");if(defenseRef.Length>0){var x=Reference(defenseRef,"enemies");e.DefenseRefId=x.id;e.DefenseRefMul=x.value;}
                var shield=et.S(r,"shieldAt");if(shield.Length>0){e.ShieldAt=shield.Split('|').Select(x=>F(x.Trim())).OrderByDescending(x=>x).ToArray();e.ShieldDefense=et.F(r,"shieldDefense");e.ShieldSeconds=et.F(r,"shieldSeconds");}
                var summon=et.S(r,"summonInto");if(summon.Length>0){var x=Reference(summon,"enemies");e.SummonId=x.id;e.SummonCount=(int)x.value;e.SummonInterval=et.F(r,"summonInterval");e.SummonRageInterval=et.F(r,"summonRageInterval",e.SummonInterval);if(e.SummonCount<1||e.SummonInterval<=0)throw new ArgumentException("enemies: invalid summon");}
                var split=et.S(r,"splitInto");if(split.Length>0){var x=Reference(split,"enemies");e.SplitIntoId=x.id;e.SplitCount=(int)x.value;if(x.value!=e.SplitCount||e.SplitCount<1)throw new ArgumentException("enemies: split count must be a positive integer");}
                d.EnemiesById.Add(id,e);if(!d.Enemies.ContainsKey(kind))d.Enemies.Add(kind,e);pending.Add((e,r));
            }
            if(!d.Enemies.TryGetValue(EnemyKind.Attack,out var attack))throw new ArgumentException("enemies: missing Attack");
            foreach(var (e,r) in pending)
            {
                if(e.HealthMul>0&&e.Health==0)e.Health=Mathf.FloorToInt(attack.Health*e.HealthMul+.5f);
                if(e.Gold<0)e.Gold=e.GoldMul>0?Mathf.FloorToInt(attack.Gold*e.GoldMul+.5f):throw new ArgumentException("enemies: missing gold for "+e.Id);
                if(e.DefenseRefId!=0&&!d.EnemiesById.ContainsKey(e.DefenseRefId))throw new ArgumentException("enemies: unknown defenseRef "+e.DefenseRefId);
                if(e.SummonId!=0&&!d.EnemiesById.ContainsKey(e.SummonId))throw new ArgumentException("enemies: unknown summon "+e.SummonId);
                if(e.SplitIntoId!=0&&(!d.EnemiesById.TryGetValue(e.SplitIntoId,out var child)||child.Kind!=EnemyKind.SplitChild))throw new ArgumentException("enemies: splitInto must reference a derived enemy");
            }
            foreach(var (e,r) in pending)if(e.Defense<0)e.Defense=e.DefenseRefId!=0?d.EnemiesById[e.DefenseRefId].Defense*e.DefenseRefMul:throw new ArgumentException("enemies: missing defense for "+e.Id);
            if(d.EnemiesById.Values.Any(e=>e.Health<=0||e.Speed<=0||e.Defense>=100))throw new ArgumentException("enemies: invalid base stats");
            // towers
            var tt=new Table("towers",towers,"id","key","cost","damage","rate","range","projectileSpeed","buffDamage","status");ids.Clear();
            foreach(var r in tt.Rows)
            {
                int id=tt.Id(r,12000,12999);Unique(ids,id,"towers");var kind=Kind<TowerKind>(tt.Required(r,"key"));
                if(kind==TowerKind.Support!=(id>=12101))throw new ArgumentException("towers: support ID range mismatch "+id);
                if(d.Towers.ContainsKey(kind))throw new ArgumentException("towers: duplicate kind "+kind);
                if(tt.S(r,"status")!="provisional")throw new ArgumentException("Unknown balance status");
                var t=new TowerSpec{Id=id,Kind=kind,Key=kind.ToString(),Cost=tt.I(r,"cost"),Damage=tt.F(r,"damage"),Rate=tt.F(r,"rate"),Range=tt.F(r,"range"),ProjectileSpeed=tt.F(r,"projectileSpeed"),BuffDamage=tt.F(r,"buffDamage"),
                    Tag=tt.S(r,"tag"),AttackType=tt.S(r,"attackType"),MaxTargets=tt.I(r,"maxTargets",1),SlowPercent=tt.F(r,"slowPercent",0),SlowDuration=tt.F(r,"slowDuration",0),
                    BurnPerStack=tt.F(r,"burnPerStack",0),BurnDuration=tt.F(r,"burnDuration",0),PierceFalloff=tt.F(r,"pierceFalloff",0),Requires=tt.S(r,"requires")};
                if(t.PierceFalloff>1)throw new ArgumentException("towers: pierceFalloff above 1");
                d.Towers.Add(kind,t);d.TowersById.Add(id,t);
            }
            // upgrades
            var ut=new Table("upgrades",upgrades,"id","target","stat","delta","cost","costIncrement");ids.Clear();
            foreach(var r in ut.Rows)
            {
                int id=ut.Id(r,21000,21999);Unique(ids,id,"upgrades");int target=ut.I(r,"target");
                string name;if(target==10001)name="Player";else if(d.TowersById.TryGetValue(target,out var tower))name=tower.Kind.ToString();else throw new ArgumentException("upgrades: unknown target "+target);
                d.AddUpgrade(new UpgradeSpec{Id=id,TargetId=target,Target=name,Stat=ut.Required(r,"stat"),Delta=ut.F(r,"delta"),Cost=ut.I(r,"cost"),CostIncrement=ut.I(r,"costIncrement"),Group=ut.I(r,"group",id)});
            }
            foreach(var u in d.Upgrades){var g=d.Upgrades.FirstOrDefault(x=>x.Id==u.Group);if(g==null||g.TargetId!=u.TargetId||g.Group!=g.Id)throw new ArgumentException("upgrades: invalid group "+u.Group);}
            // waves
            var wt=new Table("waves",waves,"id","wave","enemy","count","interval","start","health","defense","speed","gold");ids.Clear();
            foreach(var r in wt.Rows)
            {
                int id=wt.Id(r,41000000,41999999);Unique(ids,id,"waves");int wave=wt.I(r,"wave");
                if(id/1000%1000!=wave||id%1000==0)throw new ArgumentException("waves: ID "+id+" does not match round "+wave);
                int enemyId=wt.I(r,"enemy");if(!d.EnemiesById.TryGetValue(enemyId,out var b))throw new ArgumentException("Missing enemy reference "+enemyId);
                if(b.Kind==EnemyKind.SplitChild)throw new ArgumentException("waves: derived enemies are not scheduled");
                d.Waves.Add(new WaveSpec{Id=id,EnemyId=enemyId,Wave=wave,Kind=b.Kind,Count=wt.I(r,"count"),Interval=wt.F(r,"interval"),Start=wt.F(r,"start"),Enemy=WaveEnemy(b,wt.Required(r,"health"),wt.Required(r,"defense"),wt.Required(r,"speed"),wt.Required(r,"gold"))});
            }
            d.LoadRuleSheets(augments,effectTags,skills,true);
            d.ValidateCore();
            return d;
        }
        void LoadRuleSheets(string augments,string effectTags,string skills,bool checkStage)
        {
            var ids=new HashSet<int>();
            if(effectTags!=null)
            {
                var t=new Table("effectTags",effectTags,"id","tag","param","value","bossValue","unit");ids.Clear();
                foreach(var r in t.Rows)
                {
                    int id=t.Id(r,31000,31999);Unique(ids,id,"effectTags");var boss=t.S(r,"bossValue");
                    this.EffectTags.Add(id,new EffectTagSpec{Id=id,Tag=t.Required(r,"tag"),Param=t.Required(r,"param"),Value=t.F(r,"value"),Unit=t.S(r,"unit"),BossImmune=boss=="immune",BossValue=boss.Length==0||boss=="immune"?(float?)null:F(boss)});
                }
            }
            if(skills!=null)
            {
                var t=new Table("skills",skills,"id","key","owner","damageMul","radius","cooldown");ids.Clear();
                foreach(var r in t.Rows)
                {
                    int id=t.Id(r,32000,32999);Unique(ids,id,"skills");int owner=t.I(r,"owner");if(owner!=10001)throw new ArgumentException("skills: unknown owner "+owner);
                    this.Skills.Add(new SkillSpec{Id=id,Key=t.Required(r,"key"),Owner=owner,Input=t.S(r,"input"),Phase=t.S(r,"phase"),DamageMul=t.F(r,"damageMul"),Radius=t.F(r,"radius"),Cooldown=t.F(r,"cooldown"),ResetOnRoundStart=t.B(r,"resetOnRoundStart"),Android=t.B(r,"android")});
                }
            }
            if(augments!=null)
            {
                var t=new Table("augments",augments,"id","name","grade","category","subcategory","personality","effect","value1","value2","value3","repeatable","text");ids.Clear();
                foreach(var r in t.Rows)
                {
                    int id=t.Id(r,22001,22050);Unique(ids,id,"augments");
                    var values=new List<float>();foreach(var c in new[]{"value1","value2","value3"}){var v=t.S(r,c);if(v.Length>0)values.Add(F(v));}
                    this.Augments.Add(new AugmentSpec{Id=id,Number=id-22000,Name=t.Required(r,"name"),Grade=Kind<AugmentationGrade>(t.Required(r,"grade")),Category=t.Required(r,"category"),
                        Subcategories=t.Required(r,"subcategory").Split('|'),Personality=t.Required(r,"personality"),Effect=t.Required(r,"effect"),Values=values.ToArray(),Repeatable=t.B(r,"repeatable"),Text=t.Required(r,"text")});
                }
                if(this.Augments.Count!=50||this.Augments.Count(a=>a.Grade==AugmentationGrade.Silver)!=18||this.Augments.Count(a=>a.Grade==AugmentationGrade.Gold)!=20||this.Augments.Count(a=>a.Grade==AugmentationGrade.Platinum)!=12)
                    throw new ArgumentException("augments: expected 50 cards (18/20/12)");
                if(checkStage&&Math.Abs(Setting("augmentGradeSilver",0)+Setting("augmentGradeGold",0)+Setting("augmentGradePlatinum",0)-100)>.001f)throw new ArgumentException("stage: augment grade chances must sum to 100");
            }
        }
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
        // 전투 피해 계산 규칙 우선순위: 치유사 > 수비형 > 버프형 > 장갑병 > 비행체 > 분열체(작은 개체) > 공격형 > 돌격병 > 보스(포탑).
        // The player keeps the boss level with Attack (documented current behaviour).
        static int TargetRank(EnemyHandle enemy,bool tower)
        {
            switch(enemy.Kind)
            {
                case EnemyKind.Healer:return 0;case EnemyKind.Defense:return 1;case EnemyKind.Buff:return 2;case EnemyKind.Armored:return 3;
                case EnemyKind.Flyer:return 4;case EnemyKind.Splitter:case EnemyKind.SplitChild:return 5;case EnemyKind.Charger:return 7;
                case EnemyKind.Boss:return 8;default:return 6; // 보스 맨 뒤: 플레이어·포탑 공통 (정본)
            }
        }
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
        // 45 포탑 합체 star level (1~3); per-attacker crit counters (26·44).
        public int Stars=1;internal int Attacks;internal float CritBonus;
        int sourceId;string attackSource,supportSource;
        void RefreshSources(){if(attackSource!=null&&sourceId==Id&&Id>=0)return;sourceId=Id;attackSource="Tower:"+Id;supportSource="Support:"+Id;}
        public string AttackSource {get{RefreshSources();return attackSource;}}
        public string SupportSource {get{RefreshSources();return supportSource;}}
    }
}
