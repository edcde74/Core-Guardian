using UnityEngine;

// Sort by the prefab's ground contact point, not the sprite's tall bounding box.
// 2026-10-27 사용자 피드백 5·6: 같은 줄의 개체가 같은 값을 받으면 부위(+0~+2)와 윤곽(+3)이 서로 끼어든다.
// 개체마다 Stride칸을 따로 쓰고, 같은 높이 칸 안에서는 개체 번호(lane)로 앞뒤를 고정한다.
[ExecuteAlways]
public sealed class ArtDepthSorting : MonoBehaviour
{
    public const int Base = 2000,
        Stride = 5,
        Lanes = 16,
        MinRow = -50,
        MaxRow = 250;

    // World-space overlays (projectiles, health bars, effects) sit above every unit order.
    public const int OverlayBase = 30000;

    // 비행체(10-30 사용자 결정): 땅 위 개체(적·포탑)보다 항상 앞, 비행체끼리는 높이 순서. 최대 약 27,600 < OverlayBase.
    public const int AirBase = Base + (MaxRow - MinRow + 1) * Stride * Lanes;
    public bool Airborne { get; set; }

    public static int AirOrder(float worldY)
    {
        int row = Mathf.Clamp(Mathf.RoundToInt(worldY * RowsPerUnit), MinRow, MaxRow);
        return AirBase + (MaxRow - row) * Stride;
    }

    const float RowsPerUnit = 10f;
    static int nextLane;

    // Domain Reload is off: start every Play from lane 0 so overlapping units sort the same each run (B12).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetLanes() => nextLane = 0;

    [SerializeField]
    private SpriteRenderer artwork;
    int lane = -1;

    public static int Order(float worldY, int lane)
    {
        int row = Mathf.Clamp(Mathf.RoundToInt(worldY * RowsPerUnit), MinRow, MaxRow);
        return Base + (MaxRow - row) * Stride * Lanes + (lane & (Lanes - 1)) * Stride;
    }

    private void LateUpdate()
    {
        if (artwork == null)
            return;

        if (lane < 0)
        {
            lane = (nextLane++) & (Lanes - 1);
        }

        artwork.sortingOrder = Airborne ? AirOrder(transform.position.y) : Order(transform.position.y, lane);
    }
}
