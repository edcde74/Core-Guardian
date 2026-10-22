using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Fortress.Game.Tests
{
    public sealed class EnemyMovementPlayModeTests
    {
        [UnityTest]
        public IEnumerator Enemy_VisitsWaypointsInOrder_ThenExitsOnce()
        {
            var pathObject = new GameObject("Path");
            var path = pathObject.AddComponent<WaypointPath>();
            path.SetPoints(CreatePoint("Start", Vector2.zero), CreatePoint("Middle", Vector2.right), CreatePoint("Exit", Vector2.right * 2f));

            var enemyObject = new GameObject("Enemy");
            enemyObject.AddComponent<Health>();
            var enemy = enemyObject.AddComponent<EnemyController>();
            var definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.Configure(20, 10f, 5, Color.yellow, Vector2.one);

            var exits = 0;
            enemy.Exited += _ => exits++;
            enemy.Initialize(definition, path);

            yield return new WaitForSeconds(0.5f);

            Assert.That(exits, Is.EqualTo(1));
            Assert.That(enemy == null, Is.True);
        }

        private static Transform CreatePoint(string name, Vector2 position)
        {
            var point = new GameObject(name).transform;
            point.position = position;
            return point;
        }
    }
}
