using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public sealed class PrototypeGridMovement : MonoBehaviour
{
    [SerializeField, NaughtyAttributes.BoxGroup("타일맵"), NaughtyAttributes.Required]
    private Tilemap ground;

    [SerializeField, NaughtyAttributes.BoxGroup("타일맵"), NaughtyAttributes.Required]
    private Tilemap enemyPath;

    [SerializeField, NaughtyAttributes.BoxGroup("타일맵"), NaughtyAttributes.Required]
    private Tilemap core;

    [SerializeField, NaughtyAttributes.BoxGroup("타일맵"), NaughtyAttributes.Required]
    private Tilemap occupiedTowerCells;

    [SerializeField, NaughtyAttributes.BoxGroup("이동")]
    private Vector3 spriteOffset = new Vector3(0f, 0.11f, 0f);

    [SerializeField, Min(0.01f), NaughtyAttributes.BoxGroup("이동")]
    private float stepDuration = 0.12f;

    // Art review slabs cover 2x2 base cells; existing scenes retain one-cell steps.
    [SerializeField, NaughtyAttributes.BoxGroup("이동")]
    private Vector2Int movementTileSize = Vector2Int.one;

    [SerializeField, NaughtyAttributes.BoxGroup("이동")]
    private Vector2Int movementTileOrigin = Vector2Int.zero;

    private Vector3Int currentCell;
    private Vector2Int pendingDirection;
    private Vector3 stepStart;
    private Vector3 stepEnd;
    private float stepProgress;
    private bool isMoving;

    public Vector3Int CurrentCell => currentCell;

    // Last WASD direction pressed (46 순간 이동) and a notice per completed step (22 불길 발자국: the cell left).
    public Vector2Int LastDirection { get; private set; }
    public event System.Action<Vector3Int> Stepped;

    private void Awake()
    {
        if (ground == null || enemyPath == null || core == null)
        {
            Debug.LogError("PrototypeGridMovement needs ground, enemy path, and core tilemaps.", this);
            enabled = false;
            return;
        }

        movementTileSize = new Vector2Int(Mathf.Max(1, movementTileSize.x), Mathf.Max(1, movementTileSize.y));
        Vector3Int initialCell = ground.WorldToCell(transform.position);
        currentCell = new Vector3Int(
            movementTileOrigin.x
                + Mathf.FloorToInt((float)(initialCell.x - movementTileOrigin.x) / movementTileSize.x)
                    * movementTileSize.x,
            movementTileOrigin.y
                + Mathf.FloorToInt((float)(initialCell.y - movementTileOrigin.y) / movementTileSize.y)
                    * movementTileSize.y,
            initialCell.z
        );
        transform.position = TileCenter(currentCell) + spriteOffset;
    }

    private void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.wasPressedThisFrame)
            {
                pendingDirection = Vector2Int.up;
            }
            else if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                pendingDirection = Vector2Int.down;
            }
            else if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                pendingDirection = Vector2Int.left;
            }
            else if (Keyboard.current.dKey.wasPressedThisFrame)
            {
                pendingDirection = Vector2Int.right;
            }
        }

        if (isMoving)
        {
            stepProgress = Mathf.Min(1f, stepProgress + Time.deltaTime / stepDuration);
            transform.position = Vector3.Lerp(stepStart, stepEnd, stepProgress);
            if (stepProgress >= 1f)
            {
                isMoving = false;
            }
        }

        if (isMoving || pendingDirection == Vector2Int.zero)
            return;

        Vector2Int direction = pendingDirection;
        pendingDirection = Vector2Int.zero;
        // 10-30 결정: 실제로 움직인 방향만 기억한다(막힌 방향은 순간 이동 방향이 되지 않음).
        if (TryStep(direction))
        {
            LastDirection = direction;
        }
    }

    public bool TryStep(Vector2Int direction)
    {
        if (!enabled || isMoving || Mathf.Abs(direction.x) + Mathf.Abs(direction.y) != 1)
            return false;

        int distance = direction.x != 0 ? movementTileSize.x : movementTileSize.y;
        var delta = new Vector3Int(direction.x, direction.y, 0);
        // Check the swept footprint as well as the destination; never jump over blockers.
        for (int step = 1; step <= distance; step++)
        {
            if (!CanOccupy(currentCell + delta * step))
                return false;
        }

        Vector3Int nextCell = currentCell + delta * distance;

        var left = currentCell;
        currentCell = nextCell;
        Stepped?.Invoke(left);
        stepStart = transform.position;
        stepEnd = TileCenter(currentCell) + spriteOffset;
        stepProgress = 0f;
        isMoving = true;
        return true;
    }

    // Jumps over path/tower/core cells; lands on the farthest free cell within maxCells (3 → 2 → 1).
    public bool TryTeleport(Vector2Int direction, int maxCells)
    {
        if (!enabled || Mathf.Abs(direction.x) + Mathf.Abs(direction.y) != 1)
            return false;

        var delta = new Vector3Int(direction.x, direction.y, 0);
        int unit = direction.x != 0 ? movementTileSize.x : movementTileSize.y;
        for (int cells = maxCells; cells >= 1; cells--)
        {
            var target = currentCell + delta * (cells * unit);
            if (!CanOccupy(target))
                continue;

            currentCell = target;
            isMoving = false;
            pendingDirection = Vector2Int.zero;
            transform.position = TileCenter(currentCell) + spriteOffset;
            return true;
        }
        return false;
    }

    // 10-30: 지금 칸이 막히면(포탑 설치) 가장 가까운 빈 칸으로 비켜난다. 거리는 상하좌우 칸 수,
    // 같은 거리면 코어에 가까운 칸, 그래도 같으면 먼저 찾은 칸. 22 불길 발자국은 걸음이 아니라서 남기지 않는다.
    public bool MoveOffBlockedCell(int maxDistance = 8)
    {
        if (!enabled || CanOccupy(currentCell))
            return false;

        Vector3 coreCenter = core.cellBounds.center;
        for (int d = 1; d <= maxDistance; d++)
        {
            bool found = false;
            Vector3Int best = currentCell;
            float bestCore = float.MaxValue;
            for (int dx = -d; dx <= d; dx++)
            {
                int dy = d - Mathf.Abs(dx);
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    if (dy == 0 && sign > 0)
                        continue;

                    var cell = currentCell + new Vector3Int(dx * movementTileSize.x, sign * dy * movementTileSize.y, 0);
                    if (!CanOccupy(cell))
                        continue;

                    float toCore = ((Vector3)cell - coreCenter).sqrMagnitude;
                    if (toCore < bestCore)
                    {
                        bestCore = toCore;
                        best = cell;
                        found = true;
                    }
                }
            }
            if (!found)
                continue;

            currentCell = best;
            pendingDirection = Vector2Int.zero;
            stepStart = transform.position;
            stepEnd = TileCenter(currentCell) + spriteOffset;
            stepProgress = 0f;
            isMoving = true;
            return true;
        }
        return false;
    }

    private Vector3 TileCenter(Vector3Int origin)
    {
        var opposite = origin + new Vector3Int(movementTileSize.x - 1, movementTileSize.y - 1, 0);
        return (ground.GetCellCenterWorld(origin) + ground.GetCellCenterWorld(opposite)) * 0.5f;
    }

    private bool CanOccupy(Vector3Int origin)
    {
        for (int y = 0; y < movementTileSize.y; y++)
        {
            for (int x = 0; x < movementTileSize.x; x++)
            {
                var cell = origin + new Vector3Int(x, y, 0);
                if (
                    !ground.HasTile(cell)
                    || enemyPath.HasTile(cell)
                    || core.HasTile(cell)
                    || (occupiedTowerCells != null && occupiedTowerCells.HasTile(cell))
                )
                {
                    return false;
                }
            }
        }

        return true;
    }
}
