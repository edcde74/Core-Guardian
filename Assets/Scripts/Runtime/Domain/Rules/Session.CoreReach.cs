using System.Collections.Generic;

namespace CoreGuardian
{
    // 적 종류별 코어 도착 수 (2026-10-30 사용자 요청, 밸런싱 기록용).
    // Counts are per round: they reset when combat starts and stay readable after the round ends.
    public sealed partial class Session
    {
        readonly Dictionary<int, int> coreReaches = new Dictionary<int, int>();

        /// <summary>Enemy ID to the number that reached the core since this round's combat started.</summary>
        public IReadOnlyDictionary<int, int> CoreReachesThisRound => coreReaches;

        /// <summary>Game seconds since this round's combat started (does not depend on the speed setting).</summary>
        public float RoundCombatSeconds => waveTime;

        void CountCoreReach(EnemySpec spec)
        {
            coreReaches.TryGetValue(spec.Id, out int n);
            coreReaches[spec.Id] = n + 1;
        }
    }
}
