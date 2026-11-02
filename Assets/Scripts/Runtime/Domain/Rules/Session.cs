using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CoreGuardian
{
    public sealed partial class Session : IDisposable
    {
        public readonly CombatData Data;
        public readonly ModelList<EnemyHandle> Enemies = new ModelList<EnemyHandle>();
        public readonly List<TowerState> Towers = new List<TowerState>();
        public readonly ModelList<ProjectileHandle> Projectiles = new ModelList<ProjectileHandle>();
        public readonly Dictionary<string, int> DamageTotals = new Dictionary<string, int>();
        public event Action<EnemySnapshot, int> DamageApplied;

        // Raised at creation, before movement/hits can remove a projectile in the same Tick.
        public event Action<ProjectileSnapshot> ProjectileLaunched;

        // Presentation event: exactly one notification per attack, after all projectiles exist.
        public event Action<string, int> AttackLaunched;
        public event Action DefeatReached;
        public event Action CombatStarted;
        public event Action RoundCompleted;
        public event Action VictoryReached;
        public EnemyHandle PlayerTarget => playerTarget;

        readonly EnemyRangeIndex viewRangeIndex = new EnemyRangeIndex();
        bool towerViewsPrepared;
        int viewRevision,
            viewListVersion;

        public CombatStats Player { get; private set; }
        public int Gold { get; private set; }
        public int Crystals { get; private set; }
        public int CoreEnergy { get; private set; }
        public SessionPhase Phase { get; private set; }
        public int Wave { get; private set; }
        public float PreparationRemaining { get; private set; }

        // Kill gold and core hits happen many times a second; their text is made only when read (F10).
        string lastMessage;
        int lastMessageNumber;
        char lastMessageKind; // 'g' = "+N 골드", 'c' = "코어 -N", '\0' = text already set

        public string LastMessage
        {
            get
            {
                if (lastMessageKind != '\0')
                {
                    lastMessage =
                        lastMessageKind == 'g' ? "+" + lastMessageNumber + " 골드" : "코어 -" + lastMessageNumber;
                    lastMessageKind = '\0';
                }
                return lastMessage;
            }
            private set
            {
                lastMessage = value;
                lastMessageKind = '\0';
            }
        }

        void SetNumberMessage(char kind, int number)
        {
            lastMessageKind = kind;
            lastMessageNumber = number;
        }

        public bool Diagnostic { get; private set; }
        public int LastWave { get; private set; }
        public int CompletedWave { get; private set; }
        public int LastCompletionReward { get; private set; }
        public int CoreMaximum { get; private set; }

        /// <summary>Total core energy lost to enemies reaching the core (never to maximum changes like 48·50).</summary>
        public int CoreDamageTaken { get; private set; }
        public bool BossKilled { get; private set; }
        public float AugmentationRemaining { get; private set; }
        public AugmentationEffects Augments { get; private set; } = new AugmentationEffects();
        AugmentationCard[] choices = Array.Empty<AugmentationCard>();
        public IReadOnlyList<AugmentationCard> AugmentationChoices => Array.AsReadOnly(choices);
        readonly System.Random augmentationRandom;
        bool coreDamagedThisRound;
        readonly List<Vector2> path = new List<Vector2>();
        readonly List<float> lengths = new List<float>();
        readonly HashSet<Vector2Int> pathCells = new HashSet<Vector2Int>();
        readonly List<float> segmentEnds = new List<float>();
        bool exactPathIndex;
        Vector2Int core;
        int width,
            height,
            nextEnemy = 1,
            nextTower = 1,
            nextProjectile = 1;
        float pathLength,
            waveTime,
            playerCooldown;
        EnemyHandle playerTarget;
        bool playerAcquired;
        readonly Dictionary<WaveSpec, int> spawned = new Dictionary<WaveSpec, int>();

        /// <summary>How many of this wave row have spawned this round (HUD 다음 적 줄, 10-30).</summary>
        public int SpawnedCount(WaveSpec row) => spawned.TryGetValue(row, out int n) ? n : 0;

        // Rent per invocation so callbacks that re-enter Tick cannot overwrite outer snapshots.
        const int MaxRetainedWorkBuffers = 8;

        struct BuffAura
        {
            public EnemyHandle Source;
            public Vector2 Position;
            public EnemySpec Spec;
        }

        // List.Sort(IComparer) wraps comparer.Compare in a new delegate on every call (about 128 B per query);
        // a cached Comparison runs the same introsort with the same comparisons and allocates nothing.
        static readonly Comparison<RangeEntry> rangeEntryOrder = (a, b) => a.Ordinal.CompareTo(b.Ordinal);

        readonly Stack<StepWork> stepWorkPool = new Stack<StepWork>();
        readonly Stack<ShotWork> shotWorkPool = new Stack<ShotWork>();

        readonly List<TowerStamp> towerStamps = new List<TowerStamp>();
        readonly Dictionary<int, TowerState> towerLookup = new Dictionary<int, TowerState>();
        readonly Dictionary<string, int> sourceIds = new Dictionary<string, int>();

        readonly SupportCache supportCache = new SupportCache();

        // Session investment: independent per kind/stat, retained even with no towers installed.
        readonly Dictionary<TowerKind, Dictionary<string, int>> towerUpgradeSteps =
            new Dictionary<TowerKind, Dictionary<string, int>>();

        public Session(CombatData data, int? seed = null)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            augmentationRandom = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            combatRandom = seed.HasValue ? new System.Random(unchecked(seed.Value * 31 + 7)) : new System.Random();
            tags = TagCache.From(data);
            Player = data.Player.Clone();
            Gold = data.StartGold;
            Crystals = data.StartCrystals;
            CoreEnergy = CoreMaximum = data.CoreEnergy;
            PreparationRemaining = data.PreparationFor(1, false);
            Phase = SessionPhase.Preparation;
            Wave = 1;
            LastWave = data.Waves.Select(x => x.Wave).DefaultIfEmpty(1).Max();
            LastMessage = "준비";
            InitializeModelPools();
        }

        // HUD 개편 B안 (2026-10-28): 포탑 연구소·기술병 작업대가 설치 칸을 차지. 경로·적과는 무관.
        readonly HashSet<Vector2Int> structureCells = new HashSet<Vector2Int>();
        public IReadOnlyCollection<Vector2Int> StructureCells => structureCells;

        // 전투 중 설치·강화 허용 (2026-10-28 사용자 확정). 34 급속 건설 discounts then taxes.
        public bool CanBuildNow => Phase == SessionPhase.Preparation || Phase == SessionPhase.Combat;

        public int RefundPercent =>
            Augments.Has(13) ? (int)Augments.Value(13) / Augments.Count(13) : (int)Data.Setting("refundPercent", 50);

        // 45 포탑 합체: ★1+★1 side by side → ★2, two adjacent ★2 → ★3. The later tower's cell frees.
        static readonly Vector2Int[] Sides = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        public void Tick(float dt, Vector2 playerPosition)
        {
            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt < 0)
            {
                throw new ArgumentException("Invalid dt");
            }

            BeginModelOperation();
            try
            {
                while (dt > 0 && !disposed)
                {
                    float step = Mathf.Min(dt, .05f);
                    StepTick(step, playerPosition);
                    dt -= step;
                }
            }
            finally
            {
                EndModelOperation();
            }
            FlushCues();
        }

        void StepTick(float dt, Vector2 playerPosition)
        {
            if (Phase == SessionPhase.Augmentation)
            {
                AugmentationRemaining = Mathf.Max(0, AugmentationRemaining - dt);
                if (AugmentationRemaining <= .0001f)
                {
                    ChooseAugmentation(AugmentationDeck.BestIndex(choices));
                }
                return;
            }
            if (Phase == SessionPhase.Preparation)
            {
                PreparationRemaining = Mathf.Max(0, PreparationRemaining - dt);
                if (PreparationRemaining <= 0)
                {
                    StartCombat();
                }
                return;
            }
            if (Phase != SessionPhase.Combat)
                return;

            var work = stepWorkPool.Count > 0 ? stepWorkPool.Pop() : new StepWork();
            try
            {
                var rows = work.Rows;
                foreach (var row in Data.Waves)
                {
                    if (row.Wave == Wave)
                    {
                        rows.Add(row);
                    }
                }

                if (Diagnostic)
                {
                    if (waveTime == 0)
                    {
                        float diagnosticDistance = Mathf.Min(Data.DiagnosticStartDistance, pathLength * .2f);
                        DebugSpawn(EnemyKind.Attack, diagnosticDistance);
                        DebugSpawn(EnemyKind.Defense, diagnosticDistance + Data.DiagnosticSpacing);
                        DebugSpawn(EnemyKind.Buff, diagnosticDistance + 2 * Data.DiagnosticSpacing);
                    }
                }
                else
                {
                    foreach (var row in rows)
                    {
                        int count;
                        spawned.TryGetValue(row, out count);
                        while (count < row.Count && waveTime + .00001f >= row.Start + count * row.Interval)
                        {
                            DebugSpawn(row.Kind, 0, row.Enemy ?? Data.Enemies[row.Kind]);
                            count++;
                        }
                        spawned[row] = count;
                    }
                }

                waveTime += dt;
                if (areaBlastCooldown > 0)
                {
                    areaBlastCooldown = Mathf.Max(0, areaBlastCooldown - dt);
                }
                if (teleportCooldown > 0)
                {
                    teleportCooldown = Mathf.Max(0, teleportCooldown - dt);
                }
                // Status timers, burn ticks and abilities run before movement, on a frozen list.
                work.Enemies.AddRange(Enemies);
                foreach (var e in work.Enemies)
                {
                    if (!e.Alive)
                        continue;

                    TickStun(e, dt);
                    if (!e.Alive)
                        continue;

                    TickBurn(e, dt);
                    if (!e.Alive)
                        continue;

                    TickAbilities(e, dt);
                }
                TickFire(dt);
                FlushSpawns();
                work.Enemies.Clear();
                // Calculate all auras before moving anyone so source order cannot alter the radius test.
                var boosts = work.Boosts;
                // Keep the original source order and freeze membership before any movement.
                foreach (var source in Enemies)
                {
                    if (source.Alive && source.Kind == EnemyKind.Buff)
                    {
                        work.BuffSources.Add(source);
                    }
                }
                // Nothing writes enemies inside this pass, so each source's position/spec is read once instead of per pair.
                var auras = work.Auras;
                foreach (var source in work.BuffSources)
                {
                    auras.Add(
                        new BuffAura
                        {
                            Source = source,
                            Position = source.Position,
                            Spec = source.Spec,
                        }
                    );
                }

                // O(enemies × buff enemies) per step; fine at today's sizes (≤150 enemies) (F22).
                foreach (var enemy in Enemies)
                {
                    if (enemy.Alive)
                    {
                        float boost = 0;
                        bool found = false;
                        var position = enemy.Position;
                        foreach (var other in auras)
                        {
                            if (
                                other.Source != enemy
                                && Vector2.Distance(position, other.Position) <= other.Spec.BuffRadius
                            )
                            {
                                float value = other.Spec.BuffSpeed;
                                if (!found || value > boost || float.IsNaN(boost))
                                {
                                    boost = value;
                                }

                                found = true;
                            }
                        }

                        boosts.Add(enemy, boost);
                        work.Enemies.Add(enemy);
                    }
                }

                foreach (var e in work.Enemies)
                {
                    e.Buffed = boosts[e] > 0;
                }

                foreach (var e in work.Enemies)
                {
                    float boost = boosts[e];
                    e.Slot.SpeedBoostPercent = boost;
                    var slot = e.Slot;
                    float length = RouteLength(slot);
                    e.PathTravelled = Mathf.Min(length, e.PathTravelled + e.MovementSpeed * (1 + boost / 100f) * dt);
                    e.SlowRemaining = Mathf.Max(0, e.SlowRemaining - dt);
                    PositionEnemy(e);
                    if (e.PathRemaining <= .00001f)
                    {
                        e.Exited = true;
                        CountCoreReach(e.Spec);
                        coreDamagedThisRound |= e.Spec.CoreDamage > 0;
                        int coreBefore = CoreEnergy;
                        CoreEnergy = Mathf.Max(0, CoreEnergy - e.Spec.CoreDamage);
                        CoreDamageTaken += coreBefore - CoreEnergy;
                        SetNumberMessage('c', e.Spec.CoreDamage);
                    }
                }
                if (CoreEnergy <= 0)
                {
                    Phase = SessionPhase.Defeat;
                    ClearProjectiles();
                    RemoveDeadEnemies();
                    LastMessage = "방어 실패";
                    if (!Diagnostic)
                    {
                        DefeatReached?.Invoke();
                    }
                    return;
                }
                work.RangeIndex.Build(Enemies);
                supportCache.Dirty = true;
                Shoot(
                    playerPosition,
                    Player,
                    "Player",
                    0,
                    ref playerCooldown,
                    ref playerTarget,
                    ref playerAcquired,
                    dt,
                    false,
                    work.RangeIndex,
                    null,
                    1 + Augments.PlayerRatePercent / 100f
                );
                if (disposed)
                    return;

                work.Towers.AddRange(Towers);
                foreach (var t in work.Towers)
                {
                    if (t.Kind == TowerKind.Support)
                        continue;

                    float buff = Augments.TowerDamagePercent + supportCache.Get(Towers, t.Position).Damage;
                    float rate = 1 + TowerRatePercent(t, playerPosition) / 100f;
                    if (t.Kind == TowerKind.Frost)
                    {
                        Pulse(t, buff, rate, dt, work.RangeIndex);
                    }
                    else
                    {
                        Shoot(
                            t.Position,
                            t.Stats,
                            t.AttackSource,
                            buff,
                            ref t.Cooldown,
                            ref t.Target,
                            ref t.AcquiredOnce,
                            dt,
                            t.Kind == TowerKind.Multi,
                            work.RangeIndex,
                            t,
                            rate
                        );
                    }

                    if (disposed)
                        return;
                }
                work.Projectiles.AddRange(Projectiles);
                MoveProjectiles(dt, work);
                FlushSpawns();
                RemoveDeadEnemies();
                bool complete = Diagnostic;
                if (!complete && rows.Count > 0)
                {
                    complete = true;
                    foreach (var row in rows)
                    {
                        if (!spawned.TryGetValue(row, out int count) || count < row.Count)
                        {
                            complete = false;
                            break;
                        }
                    }
                }
                if (complete && Enemies.Count == 0)
                {
                    CompleteRound();
                }
            }
            finally
            {
                work.Clear();
                if (stepWorkPool.Count < MaxRetainedWorkBuffers)
                {
                    stepWorkPool.Push(work);
                }
            }
        }

        const float LineHitRadius = .35f;
        readonly List<EnemyHandle> lineHits = new List<EnemyHandle>(8);

        // 10-30 사용자 결정: 다시 뽑기는 카드마다 따로(카드 아래 버튼). 카드마다 기본 1회 + 17번 '한 번 더' 1장당 1회.
        int[] cardRerollsLeft = Array.Empty<int>();

        // Total left across every shown card.
        public int RerollsLeft
        {
            get
            {
                if (Phase != SessionPhase.Augmentation)
                    return 0;

                int total = 0;
                foreach (int left in cardRerollsLeft)
                {
                    total += left;
                }
                return total;
            }
        }

        public int RerollsLeftFor(int index) =>
            Phase == SessionPhase.Augmentation && index >= 0 && index < cardRerollsLeft.Length
                ? cardRerollsLeft[index]
                : 0;

        readonly HashSet<int> previousScreen = new HashSet<int>();

        TargetOrder PlayerOrder => Data.PlayerTargetOrder ?? TargetOrder.Common;
    }
}
