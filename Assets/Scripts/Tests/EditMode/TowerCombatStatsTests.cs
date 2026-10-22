using NUnit.Framework;

namespace Fortress.Game.Tests
{
    public sealed class TowerCombatStatsTests
    {
        [Test]
        public void BannerBuff_ChangesBaseStatsOnce_AndRestoresThem()
        {
            var stats = new TowerCombatStats();
            stats.Configure(3f, 0.8f);

            stats.SetBannerActive(true);
            stats.SetBannerActive(true);
            Assert.That(stats.Range, Is.EqualTo(3.6f).Within(0.001f));
            Assert.That(stats.AttackInterval, Is.EqualTo(0.64f).Within(0.001f));

            stats.SetBannerActive(false);
            Assert.That(stats.Range, Is.EqualTo(3f));
            Assert.That(stats.AttackInterval, Is.EqualTo(0.8f));
        }
    }
}
