using System;
using System.Reflection;
using NUnit.Framework;

namespace Fortress.Game.Tests
{
    public class CoreGuardianRulesTests
    {
        [TestCase(12.5f, 0f, 13)]
        [TestCase(12.5f, 1f, 12)]
        [TestCase(15f, 100f, 0)]
        public void Damage_RoundsAfterDefense(float damage, float defense, int expected)
        {
            var type = typeof(TowerCombatStats).Assembly.GetType("CoreGuardian.CombatRules");
            Assert.That(type, Is.Not.Null, "Current Core Guardian combat rules are not implemented");
            var method = type.GetMethod("Damage", BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            Assert.That(method.Invoke(null, new object[] { damage, defense }), Is.EqualTo(expected));
        }
    }
}
