using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using NUnit.Framework;
using UnityEngine;

// Live numeric-ID data tables (Notion "데이터 테이블 명세", "웨이브 편성표 — 1~30라운드", "적 유형 및 보스 설계").
public class CoreGuardianDataTableTests
{
    static string Csv(string key)
    {
        var asset = Resources.Load<TextAsset>("CoreGuardian/" + key);
        Assert.That(asset, Is.Not.Null, "Missing CSV " + key);
        return asset.text;
    }

    static CombatData Data(Func<string, string> edit = null)
    {
        Func<string, string> c = k => edit == null ? Csv(k) : edit(k) ?? Csv(k);
        return CombatData.Load(
            c("enemies"),
            c("towers"),
            c("stage"),
            c("upgrades"),
            c("waves"),
            c("player"),
            c("augments"),
            c("effectTags"),
            c("skills")
        );
    }

    static Func<string, string> Edit(string sheet, Func<string, string> change) =>
        k => k == sheet ? change(Csv(k)) : null;

    static Session Game(CombatData d)
    {
        var s = new Session(d, 42);
        s.SetBoard(40, 25, new Vector2Int(10, 0), new[] { Vector2Int.zero, new Vector2Int(10, 0) });
        return s;
    }

    // Round: Attack, Defense, Buff, Charger, Splitter, Armored, Healer, Flyer, Boss (편성표 2026-10-27).
    static readonly int[][] Formation =
    {
        new[] { 15, 0, 0, 0, 0, 0, 0, 0, 0 },
        new[] { 16, 0, 0, 0, 0, 0, 0, 0, 0 },
        new[] { 20, 3, 0, 0, 0, 0, 0, 0, 0 },
        new[] { 19, 4, 0, 3, 0, 0, 0, 0, 0 },
        new[] { 21, 6, 1, 3, 0, 0, 0, 0, 0 },
        new[] { 24, 5, 2, 4, 0, 0, 0, 0, 0 },
        new[] { 24, 6, 2, 4, 2, 0, 0, 0, 0 },
        new[] { 25, 7, 3, 5, 2, 0, 0, 0, 0 },
        new[] { 26, 9, 3, 5, 3, 0, 0, 0, 0 },
        new[] { 20, 8, 3, 4, 2, 0, 0, 0, 1 },
        new[] { 26, 8, 3, 5, 3, 0, 0, 0, 0 },
        new[] { 25, 10, 4, 6, 3, 2, 0, 0, 0 },
        new[] { 26, 11, 5, 6, 4, 2, 0, 0, 0 },
        new[] { 26, 12, 5, 7, 4, 3, 0, 0, 0 },
        new[] { 27, 14, 6, 7, 5, 3, 0, 0, 0 },
        new[] { 28, 15, 6, 7, 5, 3, 1, 0, 0 },
        new[] { 28, 16, 7, 8, 5, 4, 1, 0, 0 },
        new[] { 28, 18, 8, 8, 6, 4, 2, 0, 0 },
        new[] { 29, 20, 8, 9, 6, 4, 2, 0, 0 },
        new[] { 24, 18, 8, 7, 5, 4, 2, 0, 1 },
        new[] { 32, 20, 9, 9, 6, 5, 2, 0, 0 },
        new[] { 29, 22, 10, 10, 7, 5, 2, 3, 0 },
        new[] { 30, 24, 10, 10, 7, 5, 3, 3, 0 },
        new[] { 29, 25, 11, 11, 7, 6, 3, 4, 0 },
        new[] { 30, 27, 12, 11, 8, 6, 3, 4, 0 },
        new[] { 30, 29, 12, 12, 8, 6, 3, 5, 0 },
        new[] { 30, 31, 13, 12, 8, 7, 4, 5, 0 },
        new[] { 29, 33, 14, 13, 9, 7, 4, 6, 0 },
        new[] { 31, 35, 15, 13, 9, 7, 4, 6, 0 },
        new[] { 27, 30, 12, 11, 8, 6, 3, 5, 1 },
    };
    static readonly EnemyKind[] FormationKinds =
    {
        EnemyKind.Attack,
        EnemyKind.Defense,
        EnemyKind.Buff,
        EnemyKind.Charger,
        EnemyKind.Splitter,
        EnemyKind.Armored,
        EnemyKind.Healer,
        EnemyKind.Flyer,
        EnemyKind.Boss,
    };

    [Test]
    public void LiveTablesLoadWithNumericIdsAndEverySheet()
    {
        var d = Data();
        Assert.That(d.NumericIds, Is.True);
        Assert.That(
            d.EnemiesById.Keys.OrderBy(x => x),
            Is.EqualTo(new[] { 11001, 11002, 11003, 11004, 11005, 11006, 11007, 11008, 11101, 11102, 11103, 11201 })
        );
        Assert.That(d.TowersById.Keys.OrderBy(x => x), Is.EqualTo(new[] { 12001, 12002, 12003, 12004, 12005, 12101 }));
        Assert.That(Enum.GetValues(typeof(TowerKind)).Cast<TowerKind>().All(d.Towers.ContainsKey), Is.True);
        Assert.That(d.Player.Damage, Is.EqualTo(10));
        Assert.That(d.Player.Rate, Is.EqualTo(1));
        Assert.That(d.Player.Range, Is.EqualTo(3));
        Assert.That(d.Player.ProjectileSpeed, Is.EqualTo(6));
        Assert.That(d.Augments.Count, Is.EqualTo(50));
        Assert.That(d.Augments.Select(a => a.Id), Is.EqualTo(Enumerable.Range(22001, 50)));
        Assert.That(
            d.Augments.Where(a => !a.Repeatable).Select(a => a.Number),
            Is.EqualTo(new[] { 5, 6, 22, 23, 28, 29, 39, 40, 42, 43, 44, 45, 46, 47, 49, 50 })
        );
        Assert.That(
            d.Augments.Single(a => a.Number == 26).Text,
            Does.Contain(","),
            "Quoted CSV cells keep their commas"
        );
        Assert.That(d.Augments.Single(a => a.Number == 27).Subcategories, Is.EqualTo(new[] { "Slow", "Execute" }));
        Assert.That(d.TagValue("Slow", "maxPercent", false), Is.EqualTo(60));
        Assert.That(d.TagValue("Slow", "maxPercent", true), Is.EqualTo(30));
        Assert.That(d.TagValue("Burn", "maxStacks", true), Is.EqualTo(5));
        Assert.That(d.TagValue("Execute", "baseThreshold", true), Is.Null);
        Assert.That(d.TagValue("Crit", "baseChance", false), Is.EqualTo(0));
        var blast = d.Skills.Single();
        Assert.That(blast.Id, Is.EqualTo(32001));
        Assert.That(blast.DamageMul, Is.EqualTo(4));
        Assert.That(blast.Radius, Is.EqualTo(2));
        Assert.That(blast.Cooldown, Is.EqualTo(12));
        Assert.That(
            d.Setting("augmentGradeSilver", 0)
                + d.Setting("augmentGradeGold", 0)
                + d.Setting("augmentGradePlatinum", 0),
            Is.EqualTo(100)
        );
        Assert.That(d.RewardForWave(1), Is.EqualTo(60));
        Assert.That(d.RewardForWave(30), Is.EqualTo(325));
        Assert.That(d.CoreEnergy, Is.EqualTo(20));
    }

    [Test]
    public void TowerAndUpgradeRowsMatchTheTowerDocument()
    {
        var d = Data();
        var frost = d.Towers[TowerKind.Frost];
        Assert.That(
            (
                frost.Cost,
                frost.Damage,
                frost.Rate,
                frost.Range,
                frost.SlowPercent,
                frost.SlowDuration,
                frost.AttackType
            ),
            Is.EqualTo((60, 4f, .5f, 2f, 30f, 2.5f, "Pulse"))
        );
        var flame = d.Towers[TowerKind.Flame];
        Assert.That(
            (
                flame.Cost,
                flame.Damage,
                flame.Rate,
                flame.Range,
                flame.ProjectileSpeed,
                flame.BurnPerStack,
                flame.BurnDuration
            ),
            Is.EqualTo((65, 4f, .8f, 3f, 6f, 2.4f, 4f))
        );
        var rail = d.Towers[TowerKind.Rail];
        Assert.That(
            (rail.Cost, rail.Damage, rail.Rate, rail.Range, rail.ProjectileSpeed, rail.PierceFalloff),
            Is.EqualTo((120, 20f, .4f, 5f, 12f, .4f))
        );
        var multi = d.Towers[TowerKind.Multi];
        Assert.That(
            (multi.Cost, multi.Damage, multi.Rate, multi.MaxTargets),
            Is.EqualTo((70, 8f, .5f, 3)),
            "2026-10-31 포탑 피해 −20%: 범위 공격 70·8·0.5"
        );
        Assert.That(d.EnemiesById[11002].WeakTowers, Is.EqualTo(1 << (int)TowerKind.Flame));
        Assert.That(d.EnemiesById[11002].ResistTowers, Is.EqualTo(1 << (int)TowerKind.Multi));
        Assert.That(d.EnemiesById[11201].WeakTowers, Is.EqualTo(d.EnemiesById[11005].WeakTowers));
        Assert.That(d.EnemiesById[11101].WeakTowers | d.EnemiesById[11101].ResistTowers, Is.Zero);
        var exploit = d.Augments.Single(a => a.Number == 49);
        Assert.That((exploit.Name, exploit.Value(0), exploit.Value(1)), Is.EqualTo(("약점 공략", 1.6f, .85f)));
        Assert.That(d.Augments.Single(a => a.Number == 46).Value(1), Is.EqualTo(.5f));
        Assert.That(d.Upgrades.Count, Is.EqualTo(27));
        var burn = d.Upgrades.Single(u => u.Id == 21511);
        Assert.That(
            (burn.Target, burn.Stat, burn.Delta, burn.Group),
            Is.EqualTo(("Flame", "burnPerStack", .48f, 21501))
        );
        Assert.That(
            d.Upgrades.Where(u => u.Target != "Player").All(u => u.Cost == 60 && u.CostIncrement == 30),
            Is.True
        );
        Assert.That(d.Upgrades.Where(u => u.Id != u.Group).Select(u => u.Id), Is.EqualTo(new[] { 21511 }));
        var s = Game(d);
        typeof(Session).GetProperty("Crystals").SetValue(s, 1000);
        var t = s.TryInstall(TowerKind.Rail, new Vector2Int(2, 1), out _);
        Assert.That(t, Is.Not.Null);
        Assert.That(t.Stats.PierceFalloff, Is.EqualTo(.4f));
    }

    [Test]
    public void WavesFollowTheFormationTableWithStableEightDigitIds()
    {
        var d = Data();
        for (int wave = 1; wave <= 30; wave++)
        {
            var rows = d.Waves.Where(w => w.Wave == wave).ToList();
            for (int k = 0; k < FormationKinds.Length; k++)
            {
                Assert.That(
                    rows.Count(w => w.Kind == FormationKinds[k]),
                    Is.EqualTo(Formation[wave - 1][k]),
                    "Round " + wave + " " + FormationKinds[k]
                );
            }

            Assert.That(
                rows.Select(w => w.Id),
                Is.EqualTo(Enumerable.Range(1, rows.Count).Select(n => 41000000 + wave * 1000 + n))
            );
            Assert.That(rows.Count(w => w.Kind == EnemyKind.SplitChild), Is.Zero);
        }
        Assert.That(
            d.Waves.Where(w => w.Kind == EnemyKind.Boss)
                .Select(w => (w.Wave, w.Enemy.Key, w.Enemy.Health, w.Enemy.Defense, w.Enemy.Gold)),
            Is.EqualTo(
                new[]
                {
                    (10, "Boss", 1000, 5f, 120),
                    (20, "IronKnight", 4000, 12f, 240),
                    (30, "CrimsonLord", 16000, 20f, 400),
                }
            )
        );
    }

    [Test]
    public void NewEnemyRoundStatsFollowAttackMultipliers()
    {
        var d = Data();
        EnemySpec At(int wave, EnemyKind kind) => d.Waves.First(w => w.Wave == wave && w.Kind == kind).Enemy;
        // 적 문서 30라운드 표본(11-02 체력 21~30R ×2.2 반영): 돌격병 363/0 · 분열체 1089/8.75 · 치유사 871/8.75 · 장갑병 1452/50 · 비행체 581/0 (공격형 726 × 배율).
        Assert.That((At(30, EnemyKind.Charger).Health, At(30, EnemyKind.Charger).Defense), Is.EqualTo((363, 0f)));
        Assert.That((At(30, EnemyKind.Splitter).Health, At(30, EnemyKind.Splitter).Defense), Is.EqualTo((1089, 8.75f)));
        Assert.That((At(30, EnemyKind.Healer).Health, At(30, EnemyKind.Healer).Defense), Is.EqualTo((871, 8.75f)));
        Assert.That((At(30, EnemyKind.Armored).Health, At(30, EnemyKind.Armored).Defense), Is.EqualTo((1452, 50f)));
        Assert.That((At(30, EnemyKind.Flyer).Health, At(30, EnemyKind.Flyer).Defense), Is.EqualTo((581, 0f)));
        Assert.That(
            At(30, EnemyKind.Charger).Speed,
            Is.EqualTo(2.2f).Within(1e-4f),
            "Charger keeps the round speed growth (+10% at 30)"
        );
        Assert.That(At(5, EnemyKind.Charger).Health, Is.EqualTo(35));
        Assert.That(At(22, EnemyKind.Flyer).Flying, Is.True);
        var splitter = d.EnemiesById[11005];
        Assert.That((splitter.SplitIntoId, splitter.SplitCount), Is.EqualTo((11201, 2)));
        var child = d.EnemiesById[11201];
        Assert.That(
            (child.Kind, child.HealthMul, child.GoldMul, child.Speed),
            Is.EqualTo((EnemyKind.SplitChild, .4f, .5f, 1.3f))
        );
        var healer = d.EnemiesById[11006];
        Assert.That((healer.HealInterval, healer.HealRadius, healer.HealPercent), Is.EqualTo((3f, 2f, 8f)));
        Assert.That(d.EnemiesById[11007].Defense, Is.EqualTo(2f), "Armored base defense follows Defense x2");
        Assert.That(
            d.EnemiesById.Values.Where(e => e.Kind == EnemyKind.Boss).All(e => e.RageAt == 50 && e.RageSpeed == 30),
            Is.True
        );
        var knight = d.EnemiesById[11102];
        Assert.That(knight.ShieldAt, Is.EqualTo(new[] { 75f, 40f }));
        Assert.That((knight.ShieldDefense, knight.ShieldSeconds), Is.EqualTo((50f, 5f)));
        var lord = d.EnemiesById[11103];
        Assert.That(
            (lord.SummonId, lord.SummonCount, lord.SummonInterval, lord.SummonRageInterval),
            Is.EqualTo((11004, 2, 10f, 7f))
        );
    }

    [Test]
    public void NewEnemiesAreSpreadHealersFollowBlockersAndFlyersComeLate()
    {
        var d = Data();
        for (int wave = 1; wave <= 30; wave++)
        {
            var rows = d.Waves.Where(w => w.Wave == wave).OrderBy(w => w.Id).ToList();
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Kind == EnemyKind.Healer)
                {
                    Assert.That(
                        rows[i - 1].Kind == EnemyKind.Defense || rows[i - 1].Kind == EnemyKind.Armored,
                        Is.True,
                        "Round " + wave + " healer slot " + (i + 1)
                    );
                }

                if (rows[i].Kind == EnemyKind.Flyer)
                {
                    Assert.That(i, Is.GreaterThanOrEqualTo(rows.Count / 2), "Round " + wave + " flyer slot " + (i + 1));
                }

                Assert.That(rows[i].Interval, Is.GreaterThan(0));
            }
            var chargers = rows.Select((w, i) => (w, i))
                .Where(x => x.w.Kind == EnemyKind.Charger)
                .Select(x => x.i)
                .ToList();
            if (chargers.Count > 1)
            {
                Assert.That(
                    chargers.Last() - chargers.First(),
                    Is.GreaterThan(rows.Count / 2),
                    "Round " + wave + " chargers are spread over the round"
                );
            }
        }
    }

    [Test]
    public void LiveDataRunsThirtyRoundsWithEveryNewKindPooled()
    {
        var d = Data();
        var s = Game(d);
        var seen = new HashSet<EnemyKind>();
        for (int wave = 1; wave <= 30; wave++)
        {
            Assert.That(s.StartCombat(), Is.True);
            // Forced lethal hits only verify progression; never difficulty or clear time.
            for (int i = 0; i < 1600 && s.Phase == SessionPhase.Combat; i++)
            {
                s.Tick(.05f, new Vector2(100, 100));
                foreach (var e in s.Enemies.ToArray())
                {
                    seen.Add(e.Kind);
                    s.ApplyDamage(e, 100000, "DataQA");
                }
            }
            Assert.That(s.CompletedWave, Is.EqualTo(wave));
            if (s.Phase == SessionPhase.Augmentation)
            {
                Assert.That(s.ChooseAugmentation(0), Is.True);
            }
        }
        Assert.That(s.Phase, Is.EqualTo(SessionPhase.Victory));
        Assert.That(
            seen,
            Is.SupersetOf(
                new[]
                {
                    EnemyKind.Charger,
                    EnemyKind.Splitter,
                    EnemyKind.Healer,
                    EnemyKind.Armored,
                    EnemyKind.Flyer,
                    EnemyKind.Boss,
                }
            )
        );
        foreach (var kind in seen)
        {
            Assert.That(s.EnemyPool(kind).Active, Is.Zero);
        }
    }

    [Test]
    public void ValidationRejectsBrokenIdsReferencesAndCounts()
    {
        Assert.DoesNotThrow(() => Data());
        void Bad(string sheet, Func<string, string> change, string because) =>
            Assert.Throws<ArgumentException>(() => Data(Edit(sheet, change)), because);
        Bad("enemies", t => t + "\n11001,Attack,40,1,2,3,0,0,0,,,,,0,0,0,0,0,0,,,,,,,,provisional", "duplicate ID");
        Bad("enemies", t => t.Replace("11004,Charger", "13004,Charger"), "enemy ID outside 11000~11999");
        Bad("enemies", t => t.Replace("11201*2", "11001*2"), "split must reference a derived enemy");
        Bad("enemies", t => t.Replace("11002*2", "11999*2"), "unknown defenseRef");
        Bad("towers", t => t.Replace("12101,Support", "12006,Support"), "support outside 12101~");
        Bad("upgrades", t => t.Replace("21001,10001", "21001,10002"), "unknown upgrade target");
        Bad(
            "upgrades",
            t => t.Replace("21511,12004,burnPerStack,0.48,60,30,21501", "21511,12004,burnPerStack,0.48,60,30,21401"),
            "group of another target"
        );
        Bad("waves", t => t.Replace("41001001,1,11001", "41001001,1,11999"), "unknown wave enemy");
        Bad("waves", t => t.Replace("41001001,1,11001", "41001001,1,11201"), "derived enemy scheduled");
        Bad("waves", t => t.Replace("41002001,2,", "41002001,3,"), "wave ID does not match round");
        Bad(
            "stage",
            t => t.Replace("42305,augmentGradeSilver,60", "42305,augmentGradeSilver,61"),
            "grade chances must sum to 100"
        );
        Bad("stage", t => t.Replace("42101,coreEnergy", "52101,coreEnergy"), "stage ID outside 42000~42999");
        Bad(
            "augments",
            t => string.Join("\n", t.Replace("\r", "").Split('\n').Where(l => !l.StartsWith("22050,"))),
            "augments must stay 50 cards"
        );
        Bad("augments", t => t.Replace("22050,유리 대포,Platinum", "22050,유리 대포,Gold"), "grade counts 18/20/12");
        Bad("effectTags", t => t + "\n31101,Slow,maxPercent,60,30,%", "duplicate tag ID");
        Bad("skills", t => t.Replace("32001,AreaBlast,10001", "32001,AreaBlast,10002"), "unknown skill owner");
    }
}
