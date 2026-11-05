using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CoreGuardian
{
    // CombatData: table loading (CSV sheets → specs) and validation.
    public sealed partial class CombatData
    {
        public static CombatData Load(string enemies, string towers, string stage, string upgrades, string waves) =>
            IsNumericTable(enemies)
                ? LoadTables(
                    enemies,
                    towers,
                    stage,
                    upgrades,
                    waves,
                    Sheet("player"),
                    Sheet("augments"),
                    Sheet("effectTags"),
                    Sheet("skills")
                )
                : LoadLegacy(enemies, towers, stage, upgrades, waves);

        // Sheets added by the 2026-10-26 data spec, read beside the scene-referenced CSVs.
        public static string Sheet(string name)
        {
            var asset = Resources.Load<TextAsset>("CoreGuardian/" + name);
            return asset != null ? asset.text : null;
        }

        public static CombatData Load(
            string enemies,
            string towers,
            string stage,
            string upgrades,
            string waves,
            string player,
            string augments,
            string effectTags,
            string skills
        ) =>
            IsNumericTable(enemies)
                ? LoadTables(enemies, towers, stage, upgrades, waves, player, augments, effectTags, skills)
                : LoadLegacy(enemies, towers, stage, upgrades, waves);

        static CombatData LoadLegacy(string enemies, string towers, string stage, string upgrades, string waves)
        {
            var d = new CombatData();
            foreach (var r in Rows(enemies, 9))
            {
                var kind = Kind<EnemyKind>(r[0]);
                if (d.Enemies.ContainsKey(kind))
                {
                    throw new ArgumentException("Duplicate enemy kind");
                }

                d.Enemies.Add(
                    kind,
                    new EnemySpec
                    {
                        Kind = kind,
                        Key = kind.ToString(),
                        Health = I(r[1]),
                        Speed = F(r[2]),
                        CoreDamage = I(r[3]),
                        Gold = I(r[4]),
                        Defense = F(r[5]),
                        BuffRadius = F(r[6]),
                        BuffSpeed = F(r[7]),
                    }
                );
                if (r[8] != "provisional")
                {
                    throw new ArgumentException("Unknown balance status");
                }
            }
            foreach (var r in Rows(towers, 8))
            {
                var kind = Kind<TowerKind>(r[0]);
                d.Towers.Add(
                    kind,
                    new TowerSpec
                    {
                        Kind = kind,
                        Key = kind.ToString(),
                        Cost = I(r[1]),
                        Damage = F(r[2]),
                        Rate = F(r[3]),
                        Range = F(r[4]),
                        ProjectileSpeed = F(r[5]),
                        BuffDamage = F(r[6]),
                    }
                );
                if (r[7] != "provisional")
                {
                    throw new ArgumentException("Unknown balance status");
                }
            }
            var settings = Rows(stage, 2).ToDictionary(r => r[0], r => r[1]);
            d.ApplyStage(settings);
            d.Player = new CombatStats
            {
                Damage = F(settings["playerDamage"]),
                Rate = F(settings["playerRate"]),
                Range = F(settings["playerRange"]),
                ProjectileSpeed = F(settings["playerProjectileSpeed"]),
            };
            foreach (var r in Rows(upgrades, 6))
            {
                var u = new UpgradeSpec
                {
                    Target = r[1],
                    Stat = r[2],
                    Delta = F(r[3]),
                    Cost = I(r[4]),
                    CostIncrement = I(r[5]),
                };
                u.Target = string.Equals(u.Target, "Player", StringComparison.OrdinalIgnoreCase)
                    ? "Player"
                    : Kind<TowerKind>(u.Target).ToString();
                d.AddUpgrade(u);
            }
            foreach (var r in Rows(waves, 6, 10))
            {
                var kind = Kind<EnemyKind>(r[2]);
                if (!d.Enemies.ContainsKey(kind))
                {
                    throw new ArgumentException("Missing enemy reference");
                }

                var wave = new WaveSpec
                {
                    Wave = I(r[1]),
                    Kind = kind,
                    Count = I(r[3]),
                    Interval = F(r[4]),
                    Start = F(r[5]),
                };
                if (r.Length == 10)
                {
                    wave.Enemy = WaveEnemy(d.Enemies[kind], r[6], r[7], r[8], r[9]);
                }

                d.Waves.Add(wave);
            }
            d.ValidateCore();
            // Legacy balance fixtures still play with the live card list and effect-tag values.
            d.LoadRuleSheets(Sheet("augments"), Sheet("effectTags"), Sheet("skills"), false);
            return d;
        }

        void ApplyStage(Dictionary<string, string> settings)
        {
            var d = this;
            foreach (var pair in settings)
            {
                if (float.TryParse(pair.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var raw))
                {
                    d.Settings[pair.Key] = raw;
                }
            }

            d.CoreEnergy = I(settings["coreEnergy"]);
            d.StartGold = I(settings["startGold"]);
            d.StartCrystals = I(settings["startCrystals"]);
            d.WaveReward = I(settings["waveReward"]);
            d.PreparationSeconds = F(settings["preparationSeconds"]);
            d.PreparationSecondsLong = settings.TryGetValue("preparationSecondsLong", out var longPreparation)
                ? F(longPreparation)
                : d.PreparationSeconds;
            foreach (var setting in settings.Where(x => x.Key.StartsWith("waveReward") && x.Key != "waveReward"))
            {
                int wave = I(setting.Key.Substring("waveReward".Length));
                if (wave <= 0)
                {
                    throw new ArgumentException("Invalid reward wave");
                }

                d.WaveRewards.Add(wave, I(setting.Value));
            }
            d.PlayerFirstCost = I(settings["playerFirstCost"]);
            d.PlayerSecondIncrease = I(settings["playerSecondIncrease"]);
            d.PlayerIncreaseStep = I(settings["playerIncreaseStep"]);
            d.PlayerFirstUpgradeDiscount = settings.TryGetValue("playerFirstUpgradeDiscount", out var playerDiscount)
                ? I(playerDiscount)
                : 0;
            d.TowerFirstUpgradeDiscount = settings.TryGetValue("towerFirstUpgradeDiscount", out var towerDiscount)
                ? I(towerDiscount)
                : 0;
            d.AugmentationEvery = settings.TryGetValue("augmentationEvery", out var every) ? I(every) : 10;
            d.AugmentationRequiresBoss =
                !settings.TryGetValue("augmentationRequiresBoss", out var bossRequired) || I(bossRequired) == 1;
            if (
                d.AugmentationEvery < 1
                || settings.TryGetValue("augmentationRequiresBoss", out var flag) && I(flag) > 1
            )
            {
                throw new ArgumentException("Invalid augmentation schedule");
            }

            d.DiagnosticStartDistance = F(settings["diagnosticStartDistance"]);
            d.DiagnosticSpacing = F(settings["diagnosticSpacing"]);
            d.DiagnosticGold = I(settings["diagnosticGold"]);
            d.DiagnosticCrystals = I(settings["diagnosticCrystals"]);
        }

        void AddUpgrade(UpgradeSpec u)
        {
            if (!CombatStats.IsStat(u.Stat))
            {
                throw new ArgumentException("Unknown upgrade stat");
            }

            if (Upgrades.Any(x => x.Target == u.Target && x.Stat == u.Stat))
            {
                throw new ArgumentException("Duplicate target/stat upgrade");
            }

            Upgrades.Add(u);
        }

        void ValidateCore()
        {
            var d = this;
            if (
                d.PlayerFirstUpgradeDiscount > 0 && d.PlayerFirstUpgradeDiscount >= d.PlayerFirstCost
                || d.TowerFirstUpgradeDiscount > 0
                    && d.Upgrades.Any(u => u.Target != "Player" && u.Cost <= d.TowerFirstUpgradeDiscount)
            )
            {
                throw new ArgumentException("First upgrade discount must leave a positive price");
            }

            if (
                !d.Waves.Any(w => w.Wave == 1)
                || !new[] { EnemyKind.Attack, EnemyKind.Defense, EnemyKind.Buff }.All(d.Enemies.ContainsKey)
                || !new[] { TowerKind.Single, TowerKind.Multi, TowerKind.Support }.All(d.Towers.ContainsKey)
                || d.CoreEnergy <= 0
                || d.Player.Rate <= 0
                || d.Player.ProjectileSpeed <= 0
                || d.Enemies.Values.Any(x => x.Health <= 0 || x.Speed <= 0)
                || d.Towers.Values.Any(x =>
                    x.Kind != TowerKind.Support && (x.Rate <= 0 || x.Kind != TowerKind.Frost && x.ProjectileSpeed <= 0)
                )
                || d.Waves.Any(x => x.Wave <= 0 || x.Count <= 0 || x.Interval <= 0)
            )
            {
                throw new ArgumentException("Incomplete or invalid combat data");
            }
        }

        static CombatData LoadTables(
            string enemies,
            string towers,
            string stage,
            string upgrades,
            string waves,
            string player,
            string augments,
            string effectTags,
            string skills
        )
        {
            var d = new CombatData { NumericIds = true };
            // stage
            var stageTable = new Table("stage", stage, "id", "key", "value");
            var ids = new HashSet<int>();
            var settings = new Dictionary<string, string>();
            foreach (var r in stageTable.Rows)
            {
                var key = stageTable.Required(r, "key");
                // 라운드별 완료 보상(waveRewardN)은 43001~43999, 끝 세 자리 = 라운드. 나머지 설정은 42000~42999.
                bool roundReward = key.StartsWith("waveReward") && key != "waveReward";
                int stageId = roundReward ? stageTable.Id(r, 43000, 43999) : stageTable.Id(r, 42000, 42999);
                Unique(ids, stageId, "stage");
                if (roundReward && stageId - 43000 != I(key.Substring("waveReward".Length)))
                {
                    throw new ArgumentException("stage: ID " + stageId + " does not match " + key);
                }
                if (settings.ContainsKey(key))
                {
                    throw new ArgumentException("stage: duplicate key " + key);
                }
                settings.Add(key, stageTable.Required(r, "value"));
            }
            d.ApplyStage(settings);
            // player
            string playerPriority = "",
                playerTiebreak = "";
            if (player != null)
            {
                var t = new Table("player", player, "id", "damage", "rate", "range", "projectileSpeed");
                if (t.Rows.Count != 1)
                {
                    throw new ArgumentException("player: expected one row");
                }

                var r = t.Rows[0];
                t.Id(r, 10000, 10999);
                d.Player = new CombatStats
                {
                    Damage = t.F(r, "damage"),
                    Rate = t.F(r, "rate"),
                    Range = t.F(r, "range"),
                    ProjectileSpeed = t.F(r, "projectileSpeed"),
                };
                playerPriority = t.S(r, "targetPriority");
                playerTiebreak = t.S(r, "targetTiebreak");
            }
            else if (settings.ContainsKey("playerDamage"))
            {
                d.Player = new CombatStats
                {
                    Damage = F(settings["playerDamage"]),
                    Rate = F(settings["playerRate"]),
                    Range = F(settings["playerRange"]),
                    ProjectileSpeed = F(settings["playerProjectileSpeed"]),
                };
            }
            else
            {
                throw new ArgumentException("Missing player data");
            }
            // enemies
            var et = new Table(
                "enemies",
                enemies,
                "id",
                "key",
                "health",
                "speed",
                "coreDamage",
                "gold",
                "defense",
                "buffRadius",
                "buffSpeed",
                "status"
            );
            ids.Clear();
            var pending = new List<(EnemySpec spec, string[] row)>();
            var affinity = new List<(EnemySpec spec, string weak, string resist)>();
            foreach (var r in et.Rows)
            {
                int id = et.Id(r, 11000, 11999);
                Unique(ids, id, "enemies");
                var key = et.Required(r, "key");
                EnemyKind kind;
                if (id >= 11101 && id <= 11199)
                {
                    kind = EnemyKind.Boss;
                }
                else
                {
                    kind = Kind<EnemyKind>(key);
                    if (kind == EnemyKind.Boss)
                    {
                        throw new ArgumentException("enemies: boss outside 11101~11199");
                    }
                }
                if (kind == EnemyKind.SplitChild != (id >= 11201))
                {
                    throw new ArgumentException("enemies: derived enemy ID range mismatch " + id);
                }

                if (et.S(r, "status") != "provisional")
                {
                    throw new ArgumentException("Unknown balance status");
                }

                var e = new EnemySpec
                {
                    Id = id,
                    Key = key,
                    Kind = kind,
                    Speed = et.F(r, "speed"),
                    CoreDamage = et.I(r, "coreDamage"),
                    BuffRadius = et.F(r, "buffRadius"),
                    BuffSpeed = et.F(r, "buffSpeed"),
                    HealthMul = et.F(r, "healthMul", 0),
                    GoldMul = et.F(r, "goldMul", 0),
                    HealInterval = et.F(r, "healInterval", 0),
                    HealRadius = et.F(r, "healRadius", 0),
                    HealPercent = et.F(r, "healPercent", 0),
                    Flying = et.B(r, "flying"),
                    RageAt = et.F(r, "rageAt", 0),
                    RageSpeed = et.F(r, "rageSpeed", 0),
                    Health = et.I(r, "health", 0),
                    Gold = et.I(r, "gold", -1),
                    Defense = et.F(r, "defense", -1),
                };
                var defenseRef = et.S(r, "defenseRef");
                if (defenseRef.Length > 0)
                {
                    var x = Reference(defenseRef, "enemies");
                    e.DefenseRefId = x.id;
                    e.DefenseRefMul = x.value;
                }
                var shield = et.S(r, "shieldAt");
                if (shield.Length > 0)
                {
                    e.ShieldAt = shield.Split('|').Select(x => F(x.Trim())).OrderByDescending(x => x).ToArray();
                    e.ShieldDefense = et.F(r, "shieldDefense");
                    e.ShieldSeconds = et.F(r, "shieldSeconds");
                }
                var summon = et.S(r, "summonInto");
                if (summon.Length > 0)
                {
                    var x = Reference(summon, "enemies");
                    e.SummonId = x.id;
                    e.SummonCount = (int)x.value;
                    e.SummonInterval = et.F(r, "summonInterval");
                    e.SummonRageInterval = et.F(r, "summonRageInterval", e.SummonInterval);
                    if (e.SummonCount < 1 || e.SummonInterval <= 0)
                    {
                        throw new ArgumentException("enemies: invalid summon");
                    }
                }
                var split = et.S(r, "splitInto");
                if (split.Length > 0)
                {
                    var x = Reference(split, "enemies");
                    e.SplitIntoId = x.id;
                    e.SplitCount = (int)x.value;
                    if (x.value != e.SplitCount || e.SplitCount < 1)
                    {
                        throw new ArgumentException("enemies: split count must be a positive integer");
                    }
                }
                d.EnemiesById.Add(id, e);
                if (!d.Enemies.ContainsKey(kind))
                {
                    d.Enemies.Add(kind, e);
                }

                pending.Add((e, r));
                affinity.Add((e, et.S(r, "weakTower"), et.S(r, "resistTower")));
            }
            if (!d.Enemies.TryGetValue(EnemyKind.Attack, out var attack))
            {
                throw new ArgumentException("enemies: missing Attack");
            }

            foreach (var (e, r) in pending)
            {
                if (e.HealthMul > 0 && e.Health == 0)
                {
                    e.Health = Mathf.FloorToInt(attack.Health * e.HealthMul + .5f);
                }

                if (e.Gold < 0)
                {
                    e.Gold =
                        e.GoldMul > 0
                            ? Mathf.FloorToInt(attack.Gold * e.GoldMul + .5f)
                            : throw new ArgumentException("enemies: missing gold for " + e.Id);
                }

                if (e.DefenseRefId != 0 && !d.EnemiesById.ContainsKey(e.DefenseRefId))
                {
                    throw new ArgumentException("enemies: unknown defenseRef " + e.DefenseRefId);
                }

                if (e.SummonId != 0 && !d.EnemiesById.ContainsKey(e.SummonId))
                {
                    throw new ArgumentException("enemies: unknown summon " + e.SummonId);
                }

                if (
                    e.SplitIntoId != 0
                    && (!d.EnemiesById.TryGetValue(e.SplitIntoId, out var child) || child.Kind != EnemyKind.SplitChild)
                )
                {
                    throw new ArgumentException("enemies: splitInto must reference a derived enemy");
                }
            }
            foreach (var (e, r) in pending)
            {
                if (e.Defense < 0)
                {
                    e.Defense =
                        e.DefenseRefId != 0
                            ? d.EnemiesById[e.DefenseRefId].Defense * e.DefenseRefMul
                            : throw new ArgumentException("enemies: missing defense for " + e.Id);
                }
            }

            if (d.EnemiesById.Values.Any(e => e.Health <= 0 || e.Speed <= 0 || e.Defense >= 100))
            {
                throw new ArgumentException("enemies: invalid base stats");
            }
            // towers
            var tt = new Table(
                "towers",
                towers,
                "id",
                "key",
                "cost",
                "damage",
                "rate",
                "range",
                "projectileSpeed",
                "buffDamage",
                "status"
            );
            ids.Clear();
            foreach (var r in tt.Rows)
            {
                // 포탑 ID 끝 두 자리 = 강화 ID의 대상 번호라서 12001~12099만 씁니다.
                int id = tt.Id(r, 12001, 12099);
                Unique(ids, id, "towers");
                var kind = Kind<TowerKind>(tt.Required(r, "key"));

                if (d.Towers.ContainsKey(kind))
                {
                    throw new ArgumentException("towers: duplicate kind " + kind);
                }

                if (tt.S(r, "status") != "provisional")
                {
                    throw new ArgumentException("Unknown balance status");
                }

                var t = new TowerSpec
                {
                    Id = id,
                    Kind = kind,
                    Key = kind.ToString(),
                    Cost = tt.I(r, "cost"),
                    Damage = tt.F(r, "damage"),
                    Rate = tt.F(r, "rate"),
                    Range = tt.F(r, "range"),
                    ProjectileSpeed = tt.F(r, "projectileSpeed"),
                    BuffDamage = tt.F(r, "buffDamage"),
                    Tag = tt.S(r, "tag"),
                    AttackType = tt.S(r, "attackType"),
                    MaxTargets = tt.I(r, "maxTargets", 1),
                    SlowPercent = tt.F(r, "slowPercent", 0),
                    SlowDuration = tt.F(r, "slowDuration", 0),
                    BurnPerStack = tt.F(r, "burnPerStack", 0),
                    BurnDuration = tt.F(r, "burnDuration", 0),
                    PierceFalloff = tt.F(r, "pierceFalloff", 0),
                    Requires = tt.S(r, "requires"),
                };
                if (t.PierceFalloff > 1)
                {
                    throw new ArgumentException("towers: pierceFalloff above 1");
                }

                d.Towers.Add(kind, t);
                d.TowersById.Add(id, t);
                var priority = tt.S(r, "targetPriority");
                if (priority.Length > 0)
                {
                    t.TargetOrder = TargetOrder.Parse(priority, tt.S(r, "targetTiebreak"), d.EnemiesById, "towers");
                }
            }
            if (playerPriority.Length > 0)
            {
                d.PlayerTargetOrder = TargetOrder.Parse(playerPriority, playerTiebreak, d.EnemiesById, "player");
            }

            foreach (var (e, weak, resist) in affinity)
            {
                e.WeakTowers = d.TowerMask(weak);
                e.ResistTowers = d.TowerMask(resist);
                if ((e.WeakTowers & e.ResistTowers) != 0)
                {
                    throw new ArgumentException("enemies: tower both weak and resist for " + e.Id);
                }
            }
            // upgrades
            var ut = new Table("upgrades", upgrades, "id", "target", "stat", "delta", "cost", "costIncrement");
            ids.Clear();
            foreach (var r in ut.Rows)
            {
                int id = ut.Id(r, 21000, 21999);
                Unique(ids, id, "upgrades");
                int target = ut.I(r, "target");
                string name;
                if (target == 10001)
                {
                    name = "Player";
                }
                else if (d.TowersById.TryGetValue(target, out var tower))
                {
                    name = tower.Kind.ToString();
                }
                else
                {
                    throw new ArgumentException("upgrades: unknown target " + target);
                }
                // 강화 ID = 21 + 대상 두 자리(기술병 00, 포탑 ID 끝 두 자리) + 능력치 한 자리.
                if (id / 10 % 100 != (target == 10001 ? 0 : target - 12000))
                {
                    throw new ArgumentException("upgrades: ID " + id + " does not match target " + target);
                }

                d.AddUpgrade(
                    new UpgradeSpec
                    {
                        Id = id,
                        TargetId = target,
                        Target = name,
                        Stat = ut.Required(r, "stat"),
                        Delta = ut.F(r, "delta"),
                        Cost = ut.I(r, "cost"),
                        CostIncrement = ut.I(r, "costIncrement"),
                        Group = ut.I(r, "group", id),
                    }
                );
            }
            foreach (var u in d.Upgrades)
            {
                var g = d.Upgrades.FirstOrDefault(x => x.Id == u.Group);
                if (g == null || g.TargetId != u.TargetId || g.Group != g.Id)
                {
                    throw new ArgumentException("upgrades: invalid group " + u.Group);
                }
            }
            // waves
            var wt = new Table(
                "waves",
                waves,
                "id",
                "wave",
                "enemy",
                "count",
                "interval",
                "start",
                "health",
                "defense",
                "speed",
                "gold"
            );
            ids.Clear();
            foreach (var r in wt.Rows)
            {
                int id = wt.Id(r, 41000000, 41999999);
                Unique(ids, id, "waves");
                int wave = wt.I(r, "wave");
                if (id / 1000 % 1000 != wave || id % 1000 == 0)
                {
                    throw new ArgumentException("waves: ID " + id + " does not match round " + wave);
                }

                int enemyId = wt.I(r, "enemy");
                if (!d.EnemiesById.TryGetValue(enemyId, out var b))
                {
                    throw new ArgumentException("Missing enemy reference " + enemyId);
                }

                if (b.Kind == EnemyKind.SplitChild)
                {
                    throw new ArgumentException("waves: derived enemies are not scheduled");
                }

                d.Waves.Add(
                    new WaveSpec
                    {
                        Id = id,
                        EnemyId = enemyId,
                        Wave = wave,
                        Kind = b.Kind,
                        Count = wt.I(r, "count"),
                        Interval = wt.F(r, "interval"),
                        Start = wt.F(r, "start"),
                        Enemy = WaveEnemy(
                            b,
                            wt.Required(r, "health"),
                            wt.Required(r, "defense"),
                            wt.Required(r, "speed"),
                            wt.Required(r, "gold")
                        ),
                    }
                );
            }
            d.LoadRuleSheets(augments, effectTags, skills, true);
            d.ValidateCore();
            return d;
        }

        void LoadRuleSheets(string augments, string effectTags, string skills, bool checkStage)
        {
            var ids = new HashSet<int>();
            if (effectTags != null)
            {
                var t = new Table("effectTags", effectTags, "id", "tag", "param", "value", "bossValue", "unit");
                ids.Clear();
                foreach (var r in t.Rows)
                {
                    int id = t.Id(r, 31000, 31999);
                    Unique(ids, id, "effectTags");
                    var boss = t.S(r, "bossValue");
                    this.EffectTags.Add(
                        id,
                        new EffectTagSpec
                        {
                            Id = id,
                            Tag = t.Required(r, "tag"),
                            Param = t.Required(r, "param"),
                            Value = t.F(r, "value"),
                            Unit = t.S(r, "unit"),
                            BossImmune = boss == "immune",
                            BossValue = boss.Length == 0 || boss == "immune" ? (float?)null : F(boss),
                        }
                    );
                }
            }
            if (skills != null)
            {
                var t = new Table("skills", skills, "id", "key", "owner", "damageMul", "radius", "cooldown");
                ids.Clear();
                foreach (var r in t.Rows)
                {
                    int id = t.Id(r, 32000, 32999);
                    Unique(ids, id, "skills");
                    int owner = t.I(r, "owner");
                    if (owner != 10001)
                    {
                        throw new ArgumentException("skills: unknown owner " + owner);
                    }

                    this.Skills.Add(
                        new SkillSpec
                        {
                            Id = id,
                            Key = t.Required(r, "key"),
                            Owner = owner,
                            Input = t.S(r, "input"),
                            Phase = t.S(r, "phase"),
                            DamageMul = t.F(r, "damageMul"),
                            Radius = t.F(r, "radius"),
                            Cooldown = t.F(r, "cooldown"),
                            ResetOnRoundStart = t.B(r, "resetOnRoundStart"),
                            Android = t.B(r, "android"),
                        }
                    );
                }
            }
            if (augments != null)
            {
                var t = new Table(
                    "augments",
                    augments,
                    "id",
                    "name",
                    "grade",
                    "category",
                    "subcategory",
                    "personality",
                    "effect",
                    "value1",
                    "value2",
                    "value3",
                    "repeatable",
                    "text"
                );
                ids.Clear();
                foreach (var r in t.Rows)
                {
                    int id = t.Id(r, 22001, 22050);
                    Unique(ids, id, "augments");
                    var values = new List<float>();
                    foreach (var c in new[] { "value1", "value2", "value3" })
                    {
                        var v = t.S(r, c);
                        if (v.Length > 0)
                        {
                            values.Add(F(v));
                        }
                    }
                    this.Augments.Add(
                        new AugmentSpec
                        {
                            Id = id,
                            Number = id - 22000,
                            Name = t.Required(r, "name"),
                            Grade = Kind<AugmentationGrade>(t.Required(r, "grade")),
                            Category = t.Required(r, "category"),
                            Subcategories = t.Required(r, "subcategory").Split('|'),
                            Personality = t.Required(r, "personality"),
                            Effect = t.Required(r, "effect"),
                            Values = values.ToArray(),
                            Repeatable = t.B(r, "repeatable"),
                            Text = t.Required(r, "text"),
                            Drawable = !t.Has("draw") || t.B(r, "draw"),
                        }
                    );
                }
                if (
                    this.Augments.Count != 50
                    || this.Augments.Count(a => a.Grade == AugmentationGrade.Silver) != 18
                    || this.Augments.Count(a => a.Grade == AugmentationGrade.Gold) != 20
                    || this.Augments.Count(a => a.Grade == AugmentationGrade.Platinum) != 12
                )
                {
                    throw new ArgumentException("augments: expected 50 cards (18/20/12)");
                }

                if (
                    checkStage
                    && Math.Abs(
                        Setting("augmentGradeSilver", 0)
                            + Setting("augmentGradeGold", 0)
                            + Setting("augmentGradePlatinum", 0)
                            - 100
                    ) > .001f
                )
                {
                    throw new ArgumentException("stage: augment grade chances must sum to 100");
                }
            }
        }
    }
}
