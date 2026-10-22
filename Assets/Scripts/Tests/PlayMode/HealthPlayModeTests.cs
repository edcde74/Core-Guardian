using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

namespace Fortress.Game.Tests
{
    public sealed class HealthPlayModeTests
    {
        [UnityTest]
        public IEnumerator Health_DiesOnce_WhenDamageReachesMaximum()
        {
            var subject = new GameObject("Health Test Subject").AddComponent<Health>();
            subject.Configure(20);
            var deathCount = 0;
            subject.Died += () => deathCount++;

            subject.TakeDamage(20);
            subject.TakeDamage(20);

            Assert.That(subject.IsDead, Is.True);
            Assert.That(deathCount, Is.EqualTo(1));
            yield return null;
        }
    }
}
