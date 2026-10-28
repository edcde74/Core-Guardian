using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

// CoreGuardianPrototype: world clicks, player hit test and UI pointer check.
public sealed partial class CoreGuardianPrototype
{
    // Hit the visible artwork, including the torso above the ground cell, never its shadow.
    readonly List<SpriteRenderer> playerHitRenderers = new List<SpriteRenderer>();

    bool PlayerHit(Vector2 screen)
    {
        if (player == null || Camera.main == null)
            return false;

        // Reused list instead of two arrays and a LINQ search per click (F19); same order and filters.
        player.GetComponentsInChildren(true, playerHitRenderers);
        SpriteRenderer artwork = null;
        foreach (var candidate in playerHitRenderers)
        {
            if (candidate.name == "2D Sprite")
            {
                artwork = candidate;
                break;
            }
        }
        playerHitRenderers.Clear();
        if (artwork == null)
            return false;

        var ray = Camera.main.ScreenPointToRay(screen);
        artwork.GetComponentsInChildren(false, playerHitRenderers);
        foreach (var r in playerHitRenderers)
        {
            if (!r.enabled || r.sprite == null || r.color.a <= 0)
                continue;

            if (!new Plane(r.transform.forward, r.transform.position).Raycast(ray, out float distance))
                continue;

            var point = r.transform.InverseTransformPoint(ray.GetPoint(distance));
            point.z = 0;
            if (r.flipX)
            {
                point.x = -point.x;
            }

            if (r.flipY)
            {
                point.y = -point.y;
            }

            if (r.sprite.bounds.Contains(point))
                return true;
        }
        return false;
    }

    public void HandleWorldClick(Vector2Int xy)
    {
        if (Session.Phase == SessionPhase.Augmentation)
            return;

        if (Installing.HasValue)
        {
            Place(Installing.Value, xy);
            return;
        }
        if (labCells.Contains(xy))
        {
            hud.ToggleLab();
            return;
        }
        if (benchCells.Contains(xy))
        {
            hud.ToggleBench();
            return;
        }
        var tower = Session.Towers.FirstOrDefault(t => t.Position == xy);
        if (tower != null)
        {
            SelectedTower = tower.Id;
            hud.OpenSelectedTowerSpecs();
            Changed?.Invoke();
            return;
        }
        // Enemy: nearest living one whose body covers the clicked tile (art stands above its foot cell).
        EnemyHandle picked = default;
        float best = float.MaxValue;
        foreach (var e in Session.Enemies)
        {
            if (!e.Alive)
                continue;
            float dx = Mathf.Abs(e.Position.x - xy.x),
                dy = xy.y - e.Position.y;
            if (dx > .6f || dy < -.4f || dy > 1.2f)
                continue;
            float d = dx * dx + dy * dy;
            if (d < best)
            {
                best = d;
                picked = e;
            }
        }
        if (best < float.MaxValue)
        {
            hud.ShowEnemyInfo(picked);
            Changed?.Invoke();
            return;
        }
        if (Vector2Int.RoundToInt(PlayerTile()) == xy)
        {
            hud.OpenPlayerSpecs();
            return;
        }
        bool empty =
            Session.IsBuildable(xy) // same cells as the old tilemap test (B16)
            && !Session.Enemies.Any(e => e.Alive && Vector2Int.RoundToInt(e.Position) == xy)
            && Vector2Int.RoundToInt(PlayerTile()) != xy;
        if (!empty)
            return;

        if (hud.TryFoldDetails())
            return;

        SelectedTower = -1;
        Changed?.Invoke();
    }

    PointerEventData uiPointer;
    EventSystem uiPointerOwner;

    bool OverUI(Vector2 position)
    {
        if (EventSystem.current == null)
            return false;

        // One reusable event per EventSystem; a new one each call allocated 1~3 times a frame (F1).
        if (uiPointer == null || uiPointerOwner != EventSystem.current)
        {
            uiPointerOwner = EventSystem.current;
            uiPointer = new PointerEventData(uiPointerOwner);
        }
        uiPointer.Reset();
        uiPointer.position = position;
        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(uiPointer, uiRaycastResults);
        bool hit = uiRaycastResults.Count > 0;
        uiRaycastResults.Clear();
        return hit;
    }
}
