using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

// CoreGuardianPrototype: turret lab and engineer bench buildings.
public sealed partial class CoreGuardianPrototype
{
    void SetupBuildings()
    {
        int size = Mathf.Max(1, Mathf.RoundToInt(Session.Data.Setting("buildingSize", 2)));
        var lab = new Vector2Int(
            Mathf.RoundToInt(Session.Data.Setting("buildingLabX", 2)),
            Mathf.RoundToInt(Session.Data.Setting("buildingLabY", 1))
        );
        var bench = new Vector2Int(
            Mathf.RoundToInt(Session.Data.Setting("buildingBenchX", 35)),
            Mathf.RoundToInt(Session.Data.Setting("buildingBenchY", 1))
        );
        labCells.Clear();
        benchCells.Clear();
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                labCells.Add(lab + new Vector2Int(x, y));
                benchCells.Add(bench + new Vector2Int(x, y));
            }
        }

        Session.SetStructures(labCells.Concat(benchCells));
        labSprite = Resources.Load<Sprite>("CoreGuardian/Hud/Redesign/H1_turret_lab");
        labHighlight = Resources.Load<Sprite>("CoreGuardian/Hud/Redesign/H1_turret_lab_highlight");
        benchSprite = Resources.Load<Sprite>("CoreGuardian/Hud/Redesign/H2_engineer_bench");
        benchHighlight = Resources.Load<Sprite>("CoreGuardian/Hud/Redesign/H2_engineer_bench_highlight");
        var material = TowerArtFor(TowerKind.Single)
            .OrNull()
            ?.GetComponentInChildren<SpriteRenderer>(true)
            .OrNull()
            ?.sharedMaterial;
        labView = BuildingView("Building_TurretLab", labSprite, lab, size, material);
        benchView = BuildingView("Building_EngineerBench", benchSprite, bench, size, material);
    }

    SpriteRenderer BuildingView(string name, Sprite sprite, Vector2Int origin, int size, Material material)
    {
        if (sprite == null)
        {
            Debug.LogWarning("Missing building art " + name, this);
            return null;
        }
        var go = new GameObject(name);
        go.transform.position = World(new Vector2(origin.x + (size - 1) * .5f, origin.y - .5f));
        // 0.6배 = 그림 1칸이 적·코어·바닥과 같은 도트 크기. 그림(H1·H2)은 3×3칸 크기로 그려져 있다(10-30).
        go.transform.localScale = Vector3.one * .6f;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        if (material != null)
        {
            sr.sharedMaterial = material;
        }

        sr.sortingOrder = ArtDepthSorting.Order(go.transform.position.y, 0);
        return sr;
    }

    void RefreshBuildingHighlights(Vector2Int hover, bool pointerOnBoard)
    {
        bool idle = !Installing.HasValue && Session.Phase != SessionPhase.Augmentation;
        if (labView != null)
        {
            var sprite =
                (hud.LabOpen || idle && pointerOnBoard && labCells.Contains(hover)) && labHighlight != null
                    ? labHighlight
                    : labSprite;
            if (labView.sprite != sprite)
            {
                labView.sprite = sprite;
            }
        }
        if (benchView != null)
        {
            var sprite =
                (hud.BenchOpen || idle && pointerOnBoard && benchCells.Contains(hover)) && benchHighlight != null
                    ? benchHighlight
                    : benchSprite;
            if (benchView.sprite != sprite)
            {
                benchView.sprite = sprite;
            }
        }
    }
}
