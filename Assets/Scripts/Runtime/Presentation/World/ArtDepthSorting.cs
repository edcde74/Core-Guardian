using UnityEngine;

// Sort by the prefab's ground contact point, not the sprite's tall bounding box.
[ExecuteAlways]
public sealed class ArtDepthSorting : MonoBehaviour
{
    [SerializeField] private SpriteRenderer artwork;

    public void SetArtwork(SpriteRenderer renderer) => artwork = renderer;

    private void LateUpdate()
    {
        if (artwork != null)
            artwork.sortingOrder = 1000 - Mathf.RoundToInt(transform.position.y * 20f);
    }
}
