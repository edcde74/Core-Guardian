using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CoreGuardianGridCoordinatesTests
{
    [TestCase(0f, 0f, 13f, 9f)]
    [TestCase(.045f, -.09f, 13f, 9f)]
    [TestCase(.045f, -.09f, 13.35f, 8.7f)]
    public void PlayerLogicalPositionInvertsWorldPosition_WithTranslatedGrid(
        float originX,
        float originY,
        float x,
        float y
    )
    {
        var root = new GameObject("Coordinate test grid", typeof(Grid));
        var actor = new GameObject("Coordinate test player");
        var host = new GameObject("Coordinate test adapter");
        try
        {
            root.transform.position = new Vector3(originX, originY, 0);
            root.GetComponent<Grid>().cellSize = new Vector3(.69f, .69f, 1);
            var floor = new GameObject("Coordinate test floor", typeof(Tilemap));
            floor.transform.SetParent(root.transform, false);
            var type = TypeCache.GetTypesDerivedFrom<MonoBehaviour>().Single(t => t.Name == "CoreGuardianPrototype");
            var movement = TypeCache
                .GetTypesDerivedFrom<MonoBehaviour>()
                .Single(t => t.Name == "PrototypeGridMovement");
            var game = host.AddComponent(type);
            var player = actor.AddComponent(movement);
            type.GetField("ground").SetValue(game, floor.GetComponent<Tilemap>());
            type.GetField("player").SetValue(game, player);
            var logical = new Vector2(x, y);
            actor.transform.position = (Vector3)type.GetMethod("World").Invoke(game, new object[] { logical });
            var actual = (Vector2)
                type.GetMethod("PlayerTile", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);
            Assert.That(
                Vector2.Distance(actual, logical),
                Is.LessThan(.00001f),
                "Player aim/range origin must match its displayed grid position"
            );
        }
        finally
        {
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(root);
        }
    }
}
