using System.Linq;
using UnityEngine;

namespace CoreGuardian
{
    // 개발 도구 (developer menu) cheats for manual testing. Normal play never calls these.
    public sealed partial class Session
    {
        public const string DebugSource = "개발 도구";

        public void DebugAddGold(int amount)
        {
            Gold = Mathf.Max(0, Gold + amount);
            LastMessage = "개발 도구 · 골드 +" + amount;
        }

        public void DebugAddCrystals(int amount)
        {
            Crystals = Mathf.Max(0, Crystals + amount);
            LastMessage = "개발 도구 · 수정 +" + amount;
        }

        public void DebugRestoreCore()
        {
            CoreEnergy = CoreMaximum;
            LastMessage = "개발 도구 · 코어 체력 회복";
        }

        public void DebugResetAreaBlast()
        {
            areaBlastCooldown = 0;
            teleportCooldown = 0;
            LastMessage = "개발 도구 · 범위 폭발 바로 사용 가능";
        }

        // Kills everything on the field (split children and pending summons included). Rewards apply as usual.
        public int DebugKillAll()
        {
            if (Phase != SessionPhase.Combat)
                return 0;

            int killed = 0;
            BeginModelOperation();
            try
            {
                for (int pass = 0; pass < 4; pass++)
                {
                    var alive = Enemies.Where(e => e.Alive).ToList();
                    if (alive.Count == 0 && pendingSpawns.Count == 0)
                        break;

                    foreach (var e in alive)
                    {
                        if (e.Alive)
                        {
                            ApplyDamage(e, 1e9f, DebugSource);
                            killed++;
                        }
                    }

                    FlushSpawns();
                }
                RemoveDeadEnemies();
            }
            finally
            {
                EndModelOperation();
            }
            FlushCues();
            LastMessage = "개발 도구 · 적 " + killed + "마리 처치";
            return killed;
        }

        // Finishes the current round now: remaining spawns are skipped and the field is cleared.
        public bool DebugCompleteRound()
        {
            if (Phase == SessionPhase.Preparation)
            {
                StartCombat();
            }

            if (Phase != SessionPhase.Combat || Diagnostic)
                return false;

            foreach (var row in Data.Waves)
            {
                if (row.Wave == Wave)
                {
                    spawned[row] = row.Count;
                }
            }

            DebugKillAll();
            BeginModelOperation();
            try
            {
                ClearEnemies();
                ClearProjectiles();
                CompleteRound();
            }
            finally
            {
                EndModelOperation();
            }
            FlushCues();
            return true;
        }

        // Jump to a round during preparation (earlier rounds count as completed).
        public bool DebugSetWave(int wave)
        {
            if (Phase != SessionPhase.Preparation)
                return false;

            wave = Mathf.Clamp(wave, 1, LastWave);
            if (!Data.Waves.Any(r => r.Wave == wave))
                return false;

            Wave = wave;
            CompletedWave = wave - 1;
            PreparationRemaining = Data.PreparationFor(wave, false);
            LastMessage = "개발 도구 · " + wave + "라운드 준비로 이동";
            return true;
        }

        // Opens a normal augmentation choice now (preparation only).
        public bool DebugOpenAugmentation()
        {
            if (Phase != SessionPhase.Preparation || Data.Augments.Count == 0)
                return false;

            DrawChoices(null);
            ResetCardRerolls();
            AugmentationRemaining = Data.Setting("augmentSeconds", 30);
            Phase = SessionPhase.Augmentation;
            LastMessage = "개발 도구 · 증강 선택";
            return true;
        }

        public string DebugAugmentName(int number)
        {
            var a = Data.Augments.FirstOrDefault(x => x.Number == number);
            return a == null ? "없음" : a.Name;
        }

        public int DebugAugmentCount => Data.Augments.Count == 0 ? 0 : Data.Augments.Max(x => x.Number);

        public bool DebugGrantAugmentationWithMessage(int number)
        {
            if (!DebugGrantAugmentation(number))
                return false;

            LastMessage = "개발 도구 · 증강 " + number + "번 " + DebugAugmentName(number) + " 획득";
            return true;
        }

        // Spawnable list for the developer menu: every enemy row in the data table, bosses by their own ID.
        int[] debugEnemyIds;
        public int[] DebugEnemyIds =>
            debugEnemyIds ??= Data.EnemiesById.Keys.Where(id => id / 100 != 112).OrderBy(id => id).ToArray();

        public string DebugEnemyName(int id)
        {
            if (!Data.EnemiesById.TryGetValue(id, out var e))
                return "?";

            return EnemyNames.Of(e);
        }

        // Spawns at the route start with this round's values (new kinds use the attack-type multipliers).
        public int DebugSpawnEnemy(int id, int count)
        {
            if (Phase != SessionPhase.Combat || !Data.EnemiesById.TryGetValue(id, out var baseSpec))
                return 0;

            var spec = DebugRoundSpec(baseSpec);
            for (int i = 0; i < count; i++)
            {
                DebugSpawn(spec.Kind, i * .35f, spec);
            }

            LastMessage = "개발 도구 · " + DebugEnemyName(id) + " " + count + "마리 소환";
            return count;
        }

        EnemySpec DebugRoundSpec(EnemySpec baseSpec)
        {
            foreach (var row in Data.Waves)
            {
                if (row.Wave == Wave && row.Enemy != null && row.Enemy.Id == baseSpec.Id)
                    return row.Enemy;
            }

            if (baseSpec.Kind == EnemyKind.Boss)
                return baseSpec;

            var spec = baseSpec.Clone();
            var attack = Data.EnemyForWave(Wave, EnemyKind.Attack);
            if (spec.HealthMul > 0)
            {
                spec.Health = Mathf.Max(1, Mathf.FloorToInt(attack.Health * spec.HealthMul + .5f));
            }

            if (spec.GoldMul > 0)
            {
                spec.Gold = Mathf.FloorToInt(attack.Gold * spec.GoldMul + .5f);
            }

            if (spec.Health <= 0)
            {
                spec.Health = attack.Health;
            }

            if (spec.Gold < 0)
            {
                spec.Gold = attack.Gold;
            }

            if (spec.DefenseRefId != 0 && Data.EnemiesById.TryGetValue(spec.DefenseRefId, out var reference))
            {
                spec.Defense = Data.EnemyForWave(Wave, reference.Kind).Defense * spec.DefenseRefMul;
            }

            return spec;
        }
    }
}
