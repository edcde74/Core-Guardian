using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CoreGuardian
{
    // Session: combat start, round completion and augmentation choices.
    public sealed partial class Session
    {
        public bool StartCombat()
        {
            if (Phase != SessionPhase.Preparation || path.Count < 2)
                return false;

            Phase = SessionPhase.Combat;
            PreparationRemaining = 0;
            waveTime = 0;
            spawned.Clear();
            DamageTotals.Clear();
            BossKilled = false;
            coreDamagedThisRound = false;
            coreReaches.Clear();
            playerTarget = default;
            playerAcquired = false;
            playerCooldown = 0;
            fireCells.Clear();
            fireTick = 0;
            if (AreaBlast == null || AreaBlast.ResetOnRoundStart)
            {
                areaBlastCooldown = 0;
                teleportCooldown = 0;
            }

            foreach (var tower in Towers)
            {
                tower.Target = default;
                tower.AcquiredOnce = false;
                tower.Cooldown = 0;
            }
            LastMessage = Diagnostic ? "역할 검증 전투 시작" : Wave + "라운드 전투 시작";
            CombatStarted?.Invoke();
            return true;
        }

        public void StartDiagnosticEncounter()
        {
            BeginModelOperation();
            try
            {
                towerUpgradeSteps.Clear();
                playerAttacks = 0; // A new diagnostic run must not inherit crit progress (26·44) (B14).
                playerCritBonus = 0;
                if (path.Count < 2)
                {
                    throw new InvalidOperationException("Board missing");
                }

                Diagnostic = true;
                Wave = 1;
                CompletedWave = 0;
                LastCompletionReward = 0;
                waveTime = 0;
                spawned.Clear();
                DamageTotals.Clear();
                ClearEnemies();
                ClearProjectiles();
                Towers.Clear();
                Gold = Data.DiagnosticGold;
                Crystals = Data.DiagnosticCrystals;
                CoreEnergy = Data.CoreEnergy;
                Phase = SessionPhase.Preparation;
                PreparationRemaining = Data.PreparationSeconds;
                playerTarget = default;
                playerAcquired = false;
                playerCooldown = 0;
                Player = Data.Player.Clone();
                LastMessage = "역할 검증 모드: 준비 후 시작";
                CoreMaximum = Data.CoreEnergy;
                Augments = new AugmentationEffects();
                BossKilled = false;
                choices = Array.Empty<AugmentationCard>();
                AugmentationRemaining = 0;
                coreDamagedThisRound = false;
            }
            finally
            {
                EndModelOperation();
            }
        }

        public EnemyHandle DebugSpawn(EnemyKind kind, float distance = 0, EnemySpec spec = null)
        {
            if (path.Count < 2)
            {
                throw new InvalidOperationException("Board missing");
            }
            spec = spec ?? Data.EnemyForWave(Wave, kind);
            return RentEnemy(kind, spec, distance, spec.Flying ? 1 : 0);
        }

        void CompleteRound()
        {
            ClearProjectiles();
            fireCells.Clear();
            if (Diagnostic)
            {
                Phase = SessionPhase.Victory;
                LastMessage = "역할 검증 종료";
                return;
            }
            CompletedWave = Wave;
            int reward = Data.RewardForWave(Wave);
            // 12 수정 조각: next N completions ×mult, then −X per round for augmentPenaltyRounds (10-31 확정: 기간 한정).
            if (Augments.Has(12))
            {
                if (Augments.Rounds12 > 0)
                {
                    reward = Mathf.FloorToInt(reward * Augments.Value(12, 1) / Augments.Count(12));
                    Augments.Rounds12--;
                }
                else if (Augments.Penalty12 > 0)
                {
                    reward = Mathf.Max(0, reward - (int)Augments.Value(12, 2));
                    Augments.Penalty12--;
                }
            }
            // 32 수정 광산: income after a delay.
            if (Augments.Has(32))
            {
                Augments.Rounds32++;
                if (Augments.Rounds32 > Augments.Value(32) / Augments.Count(32))
                {
                    reward += Augments.MineIncome32;
                }
            }
            LastCompletionReward = reward;
            Crystals += reward;
            if (Augments.Has(33))
            {
                int moved = Mathf.FloorToInt(Gold * Augments.Value(33) / 100f);
                float cap = Augments.Value(33, 1); // 라운드당 최대 수정 (11-02 확정 50)
                if (cap > 0)
                    moved = Mathf.Min(moved, Mathf.FloorToInt(cap));
                Gold -= moved;
                Crystals += moved;
            }
            if (Augments.Has(35))
            {
                CoreEnergy = Mathf.Min(CoreMaximum, CoreEnergy + (int)Augments.Value(35));
            }

            if (!coreDamagedThisRound)
            {
                Augments.CleanRounds++;
            }

            if (Augments.Rounds18 > 0)
            {
                Augments.Rounds18--;
            }

            if (Augments.Rounds34 > 0)
            {
                Augments.Rounds34--;
            }
            else if (Augments.Penalty34 > 0)
            {
                Augments.Penalty34--;
            }

            LastMessage = CompletedWave + "라운드 완료 +" + LastCompletionReward + " 수정";
            if (Wave >= LastWave)
            {
                Phase = SessionPhase.Victory;
                LastMessage += " · 1~" + LastWave + "라운드 검증 완료";
                VictoryReached?.Invoke();
                return;
            }
            if (!Data.Waves.Any(row => row.Wave == Wave + 1))
            {
                throw new InvalidOperationException("Next wave is missing");
            }

            Wave++;
            PreparationRemaining = Data.PreparationFor(Wave, false);
            int every =
                CompletedWave > 30
                    ? (int)Data.Setting("augmentationEveryLate", Data.AugmentationEvery)
                    : Data.AugmentationEvery;
            if (
                every > 0
                && CompletedWave % every == 0
                && (!Data.AugmentationRequiresBoss || BossKilled)
                && Data.Augments.Count > 0
            )
            {
                DrawChoices(null);
                ResetCardRerolls();
                AugmentationRemaining = Data.Setting("augmentSeconds", 30);
                Phase = SessionPhase.Augmentation;
                LastMessage += " · 증강 선택";
                RoundCompleted?.Invoke();
                return;
            }
            Phase = SessionPhase.Preparation;
            LastMessage += " · " + Wave + "라운드 준비";
            RoundCompleted?.Invoke();
        }

        void DrawChoices(ICollection<int> exclude)
        {
            int count = Augments.Has(47) ? Augments.Cards : (int)Data.Setting("augmentCards", 3);
            choices = AugmentationDeck.Draw(
                augmentationRandom,
                Data.Augments,
                Augments,
                count,
                exclude,
                Augments.PendingPlatinum,
                Data.Setting("augmentGradeSilver", 60),
                Data.Setting("augmentGradeGold", 30),
                Data.Setting("augmentSubcategoryWeight", 25)
            );
        }

        void ResetCardRerolls()
        {
            int each = (int)Data.Setting("augmentRerolls", 1) + (int)Augments.ExtraRerolls;
            cardRerollsLeft = new int[choices.Length];
            for (int i = 0; i < cardRerollsLeft.Length; i++)
            {
                cardRerollsLeft[i] = each;
            }
        }

        // Free reroll of one card (카드마다 따로). The shown cards are excluded and the timer keeps running.
        // 10-31 확정: 새 카드는 바꾸는 카드와 같은 등급(실버는 실버끼리).
        public bool RerollAugmentation(int index)
        {
            if (Phase != SessionPhase.Augmentation || index < 0 || index >= choices.Length)
                return false;
            if (index >= cardRerollsLeft.Length || cardRerollsLeft[index] <= 0)
                return false;

            previousScreen.Clear();
            foreach (var c in choices)
            {
                previousScreen.Add(c.Spec.Id);
            }

            // Draw falls back to shown cards only when nothing else fits; never put a duplicate on screen.
            AugmentationCard drawn = null;
            for (int attempt = 0; attempt < 4 && drawn == null; attempt++)
            {
                var one = AugmentationDeck.Draw(
                    augmentationRandom,
                    Data.Augments,
                    Augments,
                    1,
                    previousScreen,
                    false,
                    Data.Setting("augmentGradeSilver", 60),
                    Data.Setting("augmentGradeGold", 30),
                    Data.Setting("augmentSubcategoryWeight", 25),
                    choices[index].Grade
                );
                if (one.Length > 0 && !previousScreen.Contains(one[0].Spec.Id))
                {
                    drawn = one[0];
                }
            }
            if (drawn == null)
                return false;

            cardRerollsLeft[index]--;
            choices[index] = drawn;
            RaiseCue("reroll", Vector2.zero);
            FlushCues();
            LastMessage = "증강 다시 뽑기";
            return true;
        }

        public bool ChooseAugmentation(int index)
        {
            if (Phase != SessionPhase.Augmentation || index < 0 || index >= choices.Length)
                return false;

            var card = choices[index];
            if (Augments.PendingPlatinum && card.Number != 48)
            {
                Augments.PendingPlatinum = false;
            }

            Augments.Apply(card);
            ApplyImmediate(card);
            choices = Array.Empty<AugmentationCard>();
            AugmentationRemaining = 0;
            cardRerollsLeft = Array.Empty<int>();
            Phase = SessionPhase.Preparation;
            PreparationRemaining = Data.PreparationFor(Wave, true);
            LastMessage = card.GradeName + " · " + card.Title + " 선택 · " + Wave + "라운드 준비";
            return true;
        }

        // QA/test hook: grant a card exactly as if it had been chosen, without the selection screen.
        public bool DebugGrantAugmentation(int number)
        {
            var spec = Data.Augments.FirstOrDefault(x => x.Number == number);
            if (spec == null)
                return false;

            var card = new AugmentationCard(spec);
            Augments.Apply(card);
            ApplyImmediate(card);
            return true;
        }

        // 10-31 확정: 12·34번의 손해는 이득이 끝난 뒤 augmentPenaltyRounds 라운드 동안만.
        int PenaltyRounds => (int)Data.Setting("augmentPenaltyRounds", 5);

        // 10-31 확정: 정해진 양을 주는 경제 증강(18·32)은 고른 시점 1~10 / 11~20 / 21~30라운드에 1배 / 2배 / 3배.
        int EconomyTier() => 1 + (int)Data.Setting("augmentEconomyStep", 1) * ((Mathf.Max(1, CompletedWave) - 1) / 10);

        // One-time effects at selection; standing effects are read live from Augments.
        void ApplyImmediate(AugmentationCard card)
        {
            var v = card.Spec;
            switch (v.Number)
            {
                case 2:
                    Player.Damage += v.Value(0);
                    break;
                case 20:
                    Player.Damage += v.Value(0);
                    Player.Range += v.Value(1);
                    break;
                case 14:
                    CoreMaximum += (int)v.Value(0);
                    CoreEnergy += (int)v.Value(0);
                    break;
                case 12:
                    Augments.Penalty12 += PenaltyRounds;
                    break;
                case 18:
                    Gold += (int)v.Value(0) * EconomyTier();
                    break;
                case 32:
                    Augments.MineIncome32 += (int)v.Value(1) * EconomyTier();
                    break;
                case 34:
                    Augments.Penalty34 += PenaltyRounds;
                    break;
                case 48:
                    CoreMaximum = Mathf.Max(1, CoreMaximum - (int)v.Value(0));
                    CoreEnergy = Mathf.Min(CoreEnergy, CoreMaximum);
                    break;
                case 50:
                    CoreMaximum = Mathf.Max(1, Mathf.CeilToInt(CoreMaximum * v.Value(0) / 100f));
                    CoreEnergy = Mathf.Min(CoreEnergy, CoreMaximum);
                    break;
            }
        }
    }
}
