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
        if (TryPickEnemy(xy, out var picked))
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
        // 10-30: 빈 바닥이면 길·설치 못 하는 칸이어도 펼친 창을 닫는다(적 정보창은 그대로).
        if (hud.TryFoldDetails())
            return;

        SelectedTower = -1;
        Changed?.Invoke();
    }

    // 11-02: 누적 피해 목록에서 고른 포탑. 철거한 포탑이면 아무것도 하지 않는다.
    public void SelectTower(int id)
    {
        if (Installing.HasValue || !Session.Towers.Any(t => t.Id == id))
            return;
        SelectedTower = id;
        hud.OpenSelectedTowerSpecs();
        Changed?.Invoke();
    }

    // Enemy: nearest living one whose body covers the tile (art stands above its foot cell).
    bool TryPickEnemy(Vector2Int xy, out EnemyHandle picked)
    {
        picked = default;
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
        return best < float.MaxValue;
    }

    // 10-31 확정: 마우스를 올린 플레이어·포탑·몬스터·코어에 1칸 아웃라인(색은 누를 때와 같음).
    // 강화 건물은 그림 테두리(_hover)로 따로 처리한다.
    public Transform HoverTarget { get; private set; }
    public OutlineKind HoverKind { get; private set; }
    SpriteRenderer hoverCoreBody;
    bool hoverCoreSearched;

    void RefreshHover(Vector2 mouse, Vector2Int xy, Vector3 world, bool overUi)
    {
        Transform target = null;
        var kind = OutlineKind.None;
        if (
            !overUi
            && !Installing.HasValue
            && Session.Phase != SessionPhase.Augmentation
            && !labCells.Contains(xy)
            && !benchCells.Contains(xy)
        )
        {
            if (PlayerHit(mouse))
            {
                target = player.transform;
                kind = OutlineKind.Player;
            }
            else
            {
                foreach (var t in Session.Towers)
                {
                    if (t.Position == xy)
                    {
                        target = TowerView(t.Id);
                        kind = OutlineKind.Tower;
                        break;
                    }
                }
                if (target == null && TryPickEnemy(xy, out var enemy))
                {
                    target = EnemyView(enemy.Id);
                    kind = OutlineKind.Enemy;
                }
                if (target == null)
                {
                    var core = HoverCoreBody();
                    if (core != null && core.bounds.Contains(new Vector3(world.x, world.y, core.bounds.center.z)))
                    {
                        target = core.transform.parent;
                        kind = OutlineKind.Core;
                    }
                }
            }
        }
        HoverTarget = target;
        HoverKind = target != null ? kind : OutlineKind.None;
    }

    SpriteRenderer HoverCoreBody()
    {
        if (!hoverCoreSearched)
        {
            hoverCoreSearched = true;
            foreach (var r in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (
                    r.gameObject.scene == gameObject.scene
                    && r.name == "2D Sprite"
                    && r.sprite != null
                    && r.sprite.name == "core"
                )
                {
                    hoverCoreBody = r;
                    break;
                }
            }
        }
        return hoverCoreBody;
    }

    PointerEventData uiPointer;
    EventSystem uiPointerOwner;

    // 누적 피해 줄 클릭: 뗀 자리의 맨 위 UI 물체를 HUD에 넘긴다.
    void SelectDamageRowAt(Vector2 position)
    {
        if (EventSystem.current == null || !OverUI(position))
            return;

        uiPointer.Reset();
        uiPointer.position = position;
        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(uiPointer, uiRaycastResults);
        var top = uiRaycastResults.Count > 0 ? uiRaycastResults[0].gameObject : null;
        uiRaycastResults.Clear();
        if (top != null)
        {
            hud.TrySelectDamageRow(top);
        }
    }

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
