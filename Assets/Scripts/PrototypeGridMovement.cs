using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public sealed class PrototypeGridMovement : MonoBehaviour
{
    [SerializeField] private Tilemap ground;
    [SerializeField] private Tilemap enemyPath;
    [SerializeField] private Tilemap core;
    [SerializeField] private Tilemap occupiedTowerCells;
    [SerializeField] private Vector3 spriteOffset = new Vector3(0f, 0.11f, 0f);
    [SerializeField, Min(0.01f)] private float stepDuration = 0.12f;
    // Art review slabs cover 2x2 base cells; existing scenes retain one-cell steps.
    [SerializeField] private Vector2Int movementTileSize = Vector2Int.one;
    [SerializeField] private Vector2Int movementTileOrigin = Vector2Int.zero;

    private Vector3Int currentCell;
    private Vector2Int pendingDirection;
    private Vector3 stepStart;
    private Vector3 stepEnd;
    private float stepProgress;
    private bool isMoving;

    public Vector3Int CurrentCell => currentCell;

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
            movementTileOrigin.x + Mathf.FloorToInt((float)(initialCell.x - movementTileOrigin.x) / movementTileSize.x) * movementTileSize.x,
            movementTileOrigin.y + Mathf.FloorToInt((float)(initialCell.y - movementTileOrigin.y) / movementTileSize.y) * movementTileSize.y,
            initialCell.z);
        transform.position = TileCenter(currentCell) + spriteOffset;
    }

    private void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.wasPressedThisFrame) pendingDirection = Vector2Int.up;
            else if (Keyboard.current.sKey.wasPressedThisFrame) pendingDirection = Vector2Int.down;
            else if (Keyboard.current.aKey.wasPressedThisFrame) pendingDirection = Vector2Int.left;
            else if (Keyboard.current.dKey.wasPressedThisFrame) pendingDirection = Vector2Int.right;
        }

        if (isMoving)
        {
            stepProgress = Mathf.Min(1f, stepProgress + Time.deltaTime / stepDuration);
            transform.position = Vector3.Lerp(stepStart, stepEnd, stepProgress);
            if (stepProgress >= 1f)
                isMoving = false;
        }

        if (isMoving || pendingDirection == Vector2Int.zero)
            return;

        Vector2Int direction = pendingDirection;
        pendingDirection = Vector2Int.zero;
        TryStep(direction);
    }

    public bool TryStep(Vector2Int direction)
    {
        if (!enabled || isMoving || Mathf.Abs(direction.x) + Mathf.Abs(direction.y) != 1)
            return false;

        int distance = direction.x != 0 ? movementTileSize.x : movementTileSize.y;
        var delta = new Vector3Int(direction.x, direction.y, 0);
        // Check the swept footprint as well as the destination; never jump over blockers.
        for (int step = 1; step <= distance; step++)
            if (!CanOccupy(currentCell + delta * step))
                return false;
        Vector3Int nextCell = currentCell + delta * distance;

        currentCell = nextCell;
        stepStart = transform.position;
        stepEnd = TileCenter(currentCell) + spriteOffset;
        stepProgress = 0f;
        isMoving = true;
        return true;
    }

    private Vector3 TileCenter(Vector3Int origin)
    {
        var opposite = origin + new Vector3Int(movementTileSize.x - 1, movementTileSize.y - 1, 0);
        return (ground.GetCellCenterWorld(origin) + ground.GetCellCenterWorld(opposite)) * 0.5f;
    }

    private bool CanOccupy(Vector3Int origin)
    {
        for (int y = 0; y < movementTileSize.y; y++)
        for (int x = 0; x < movementTileSize.x; x++)
        {
            var cell = origin + new Vector3Int(x, y, 0);
            if (!ground.HasTile(cell) || enemyPath.HasTile(cell) || core.HasTile(cell) ||
                (occupiedTowerCells != null && occupiedTowerCells.HasTile(cell)))
                return false;
        }
        return true;
    }
}
