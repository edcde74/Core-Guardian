using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

// CoreGuardianPrototype: art lookup, sheets and generated sprites.
public sealed partial class CoreGuardianPrototype
{
    // Sheets added by the 2026-10-26 data spec live beside the scene-referenced CSVs.
    static string Sheet(string name) => CombatData.Sheet(name);

    // 비행 경로 (프로토타입 레벨 디자인): the ground route without its inner loop — corners 0..3, then 6.. to the core.
    IEnumerable<Vector2> FlyingRoute()
    {
        if (routeCells.Length < 9)
            return null;

        var list = new List<Vector2>();
        for (int i = 0; i <= 3; i++)
        {
            list.Add(routeCells[i]);
        }

        for (int i = 6; i < routeCells.Length; i++)
        {
            list.Add(routeCells[i]);
        }

        return list;
    }

    GameObject UnitPrefab(string name)
    {
        if (!unitPrefabs.TryGetValue(name, out var prefab))
        {
            prefab = Resources.Load<GameObject>("CoreGuardian/Units/" + name);
            unitPrefabs[name] = prefab;
        }
        return prefab;
    }

    GameObject EnemyArtFor(EnemyKind kind)
    {
        if ((int)kind < enemyArt.Length && enemyArt[(int)kind] != null)
            return enemyArt[(int)kind];

        return UnitPrefab("enemy_" + kind.ToString().ToLowerInvariant()).OrNull() ?? enemyArt[(int)EnemyKind.Attack];
    }

    public GameObject TowerArtFor(TowerKind kind)
    {
        if ((int)kind < towerArt.Length && towerArt[(int)kind] != null)
            return towerArt[(int)kind];

        return UnitPrefab("tower_" + kind.ToString().ToLowerInvariant()).OrNull() ?? towerArt[(int)TowerKind.Single];
    }

    GameObject BossArtFor(EnemySpec spec)
    {
        if (spec == null || string.IsNullOrEmpty(spec.Key) || spec.Key == "Boss")
            return bossArt;

        if (!unitPrefabs.TryGetValue("boss_" + spec.Key, out var prefab))
        {
            prefab = Resources.Load<GameObject>("CoreGuardian/Animation/Boss/" + spec.Key + "Animated");
            unitPrefabs["boss_" + spec.Key] = prefab;
        }
        return prefab != null ? prefab : bossArt;
    }

    string EnemyArtRole(EnemyHandle enemy)
    {
        if (enemy.Kind == EnemyKind.Boss)
            return BossArtFor(enemy.Spec) == bossArt ? "Boss" : enemy.Spec.Key;

        var source = EnemyArtFor(enemy.Kind);
        return source == enemyArt[(int)EnemyKind.Attack] && enemy.Kind != EnemyKind.Attack
            ? "Attack"
            : enemy.Kind.ToString();
    }

    GameObject CreateSprite(string name, Sprite sprite, Vector3 pos, Color color, int order)
    {
        var g = new GameObject(name);
        g.transform.SetParent(transform);
        g.transform.position = pos;
        var r = g.AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.color = color;
        r.sortingOrder = order;
        return g;
    }

    Sprite MakeSprite(bool circle)
    {
        var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                texture.SetPixel(
                    x,
                    y,
                    !circle || new Vector2(x - 7.5f, y - 7.5f).sqrMagnitude < 56 ? Color.white : Color.clear
                );
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(.5f, .5f), 16);
    }
}
