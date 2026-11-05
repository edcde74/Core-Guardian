using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

// CoreGuardianPrototype: tower placement mode, preview overlay and occupied cells.
public sealed partial class CoreGuardianPrototype
{
    // Same cells as the ground/path/core tilemaps (checked 2026-10-28: 778 cells, no mismatch); Session decides (B16).
    bool CanInstall(Vector2Int c) => Session.IsBuildable(c);

    public void ChooseTower(TowerKind kind)
    {
        CancelInstall();
        if (!Session.CanChooseTower(kind, out var refusal))
        {
            GetComponent<CoreGuardianCombatAudio>().OrNull()?.PlayCue("install_fail");
            Notify(refusal);
            return;
        }
        Installing = kind;
        ghost = Instantiate(TowerArtFor(kind));
        ghost.name = "TowerPlacementPreview";
        var shadow = ghost.transform.Find("그림자");
        if (shadow != null)
        {
            shadow.gameObject.SetActive(false);
        }

        ghostRenderers = ghost.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in ghostRenderers)
        {
            sr.sortingOrder = ArtDepthSorting.OverlayBase + 1800;
        }

        foreach (var d in ghost.GetComponentsInChildren<ArtDepthSorting>())
        {
            d.enabled = false;
        }

        if (placementOverlay == null)
        {
            var overlayMaterial = PlacementOverlaySource();
            if (overlayMaterial != null)
            {
                placementOverlay = new CoreGuardianPlacementOverlay(transform, square, overlayMaterial);
            }
            else
            {
                Debug.LogWarning(
                    "Placement overlay material unavailable; tower placement continues without tile tint.",
                    this
                );
            }
        }
        placementOverlay?.Show(ground, CanInstall);
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Changed?.Invoke();
    }

    // URP returns default2DMaterial only inside the Editor (null in players), so builds use the same shader by name.
    Material PlacementOverlaySource()
    {
        var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        var source = pipeline != null ? pipeline.default2DMaterial : null;
        if (source != null)
            return source;

        if (placementOverlaySource == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (shader != null)
            {
                placementOverlaySource = new Material(shader) { name = "Installation overlay source" };
            }
        }
        return placementOverlaySource;
    }

    public void CancelInstall()
    {
        bool wasInstalling = Installing.HasValue;
        Installing = null;
        if (ghost != null)
        {
            Destroy(ghost);
        }

        ghostRenderers = System.Array.Empty<SpriteRenderer>();
        placementOverlay?.Hide();
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Cursor.visible = true;
        if (wasInstalling)
        {
            Changed?.Invoke();
        }
    }

    public bool Place(TowerKind kind, Vector2Int cell)
    {
        string reason;
        var tower = Session.TryInstall(kind, cell, out reason);
        if (tower == null)
        {
            GetComponent<CoreGuardianCombatAudio>().OrNull()?.PlayCue("install_fail");
            Notify(reason);
            return false;
        }
        // A merge (45 포탑 합체) can vacate cells, so occupancy follows the model.
        RebuildOccupied();
        // 10-30 사용자 결정: 기술병이 선 칸에 설치하면 기술병이 가장 가까운 빈 칸으로 비켜난다.
        player.MoveOffBlockedCell();
        // S4 합체: 합체 소리만 (설치음 없이).
        if (tower.Stars <= 1)
        {
            GetComponent<CoreGuardianCombatAudio>().OrNull()?.NotifyTowerInstalled();
        }
        SelectedTower = tower.Id;
        CancelInstall();
        Notify(tower.Stars > 1 ? Session.LastMessage : "포탑 설치 완료");
        SynchronizeViews();
        return true;
    }

    void RebuildOccupied()
    {
        occupied.ClearAllTiles();
        foreach (var t in Session.Towers)
        {
            occupied.SetTile(new Vector3Int(t.Position.x, t.Position.y, 0), occupiedTile);
        }
        // Buildings block placement and player movement exactly like towers.
        foreach (var c in labCells)
        {
            occupied.SetTile(new Vector3Int(c.x, c.y, 0), occupiedTile);
        }

        foreach (var c in benchCells)
        {
            occupied.SetTile(new Vector3Int(c.x, c.y, 0), occupiedTile);
        }
    }

    public bool PlaceAtPlayerForTest()
    {
        var p = player.CurrentCell;
        return Place(TowerKind.Single, new Vector2Int(p.x, p.y));
    }
}
