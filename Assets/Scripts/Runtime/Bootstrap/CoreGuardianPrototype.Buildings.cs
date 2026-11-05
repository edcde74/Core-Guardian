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
    // 10-31: 강화 건물 칸의 가운데(타일 좌표). 작업대 창을 건물 옆으로 옮기는 데 쓴다.
    public Vector2 BuildingCenter(bool bench)
    {
        var cells = bench ? benchCells : labCells;
        if (cells.Count == 0)
            return Vector2.zero;

        var sum = Vector2.zero;
        foreach (var c in cells)
        {
            sum += c;
        }
        return sum / cells.Count;
    }

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
        // 10-31 확정: 창이 열리면 금색 2칸(_highlight), 마우스만 올리면 금색 1칸(_hover).
        labHover = Resources.Load<Sprite>("CoreGuardian/Hud/Redesign/H1_turret_lab_hover");
        benchHover = Resources.Load<Sprite>("CoreGuardian/Hud/Redesign/H2_engineer_bench_hover");
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
        // 10-31 확정(그림자 2안): 빛은 서쪽 → 그림자는 오른쪽 위로. 포탑·코어 그림자와 같은 바닥 층(42)에 그린다.
        var shadow = Resources.Load<Sprite>("CoreGuardian/Hud/Redesign/" + sprite.name + "_shadow");
        if (shadow != null)
        {
            var cast = new GameObject("BuildingCastShadow").AddComponent<SpriteRenderer>();
            cast.transform.SetParent(go.transform, false);
            cast.transform.localPosition = new Vector3(
                (shadow.pivot.x - sprite.pivot.x) / sprite.pixelsPerUnit,
                (shadow.pivot.y - sprite.pivot.y) / sprite.pixelsPerUnit,
                0
            );
            cast.sprite = shadow;
            if (material != null)
            {
                cast.sharedMaterial = material;
            }

            cast.sortingOrder = 42;
        }
        return sr;
    }

    // 11-01 사용자 요청: 마우스를 올린 강화 건물(0 없음, 1 포탑 연구소, 2 기술병 작업대). HUD가 건물 위에 이름·설명창을 띄운다.
    public int HoverBuilding { get; private set; }

    // 11-01: 강화 건물 그림 윗변 가운데(타일 좌표). 이름·설명창을 이 위에 붙인다.
    public Vector2 BuildingTop(bool bench) => BuildingCenter(bench) + new Vector2(.5f, .9f);

    void RefreshBuildingHighlights(Vector2Int hover, bool pointerOnBoard)
    {
        bool idle = !Installing.HasValue && Session.Phase != SessionPhase.Augmentation;
        HoverBuilding =
            !idle || !pointerOnBoard ? 0
            : labCells.Contains(hover) && !hud.LabOpen ? 1
            : benchCells.Contains(hover) && !hud.BenchOpen ? 2
            : 0;
        if (labView != null)
        {
            var sprite =
                hud.LabOpen && labHighlight != null ? labHighlight
                : idle && pointerOnBoard && labCells.Contains(hover) && labHover != null ? labHover
                : labSprite;
            if (labView.sprite != sprite)
            {
                labView.sprite = sprite;
            }
        }
        if (benchView != null)
        {
            var sprite =
                hud.BenchOpen && benchHighlight != null ? benchHighlight
                : idle && pointerOnBoard && benchCells.Contains(hover) && benchHover != null ? benchHover
                : benchSprite;
            if (benchView.sprite != sprite)
            {
                benchView.sprite = sprite;
            }
        }
    }
}
