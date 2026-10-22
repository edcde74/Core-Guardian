using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Fortress.Game.Tests
{
    public sealed class BeanProjectilePlayModeTests
    {
        [UnityTest]
        public IEnumerator BeanProjectile_HitsLiveTargetExactlyOnce()
        {
            var targetObject = new GameObject("Target");
            targetObject.transform.position = Vector2.right;
            var target = targetObject.AddComponent<Health>();
            target.Configure(40);

            var projectile = new GameObject("Projectile").AddComponent<BeanProjectile>();
            projectile.Launch(target, 20, 10f);

            yield return new WaitForSeconds(0.25f);

            Assert.That(target.Current, Is.EqualTo(20));
        }
    }
}
