using System.Collections.Generic;
using UnityEngine;

namespace CoreGuardian
{
    // One renderer per enemy. Shared point-filtered sprites encode discrete fill states,
    // avoiding a hierarchy of UI/renderers and per-frame texture work for each monster.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SegmentedEnemyHealthBar : MonoBehaviour
    {
        static readonly Dictionary<int, Sprite> sprites = new Dictionary<int, Sprite>();
        static int activeBars;
        SpriteRenderer display;
        public int SegmentCount { get; private set; } = 8;
        public int FilledSegments { get; private set; } = -1;

        void Awake()
        {
            activeBars++;
        }

        void OnDestroy()
        {
            if (--activeBars <= 0)
            {
                activeBars = 0;
                // F20: during Play the few bar sprites stay for the next enemies (cleared at quit / next Play).
                if (!Application.isPlaying)
                {
                    ClearSprites();
                }
            }
        }

        public void Configure(bool boss)
        {
            SegmentCount = boss ? 12 : 8;
            FilledSegments = -1;
            display = GetComponent<SpriteRenderer>();
            display.color = Color.white;
            transform.localScale = Vector3.one;
        }

        public void SetHealth(int health, int maximum)
        {
            int filled =
                maximum <= 0
                    ? 0
                    : Mathf.Clamp(
                        Mathf.CeilToInt(Mathf.Clamp01(health / (float)maximum) * SegmentCount - .00001f),
                        0,
                        SegmentCount
                    );
            if (FilledSegments == filled && display != null && display.sprite != null)
                return;

            if (display == null)
            {
                display = GetComponent<SpriteRenderer>();
            }

            FilledSegments = filled;
            display.sprite = StateSprite(SegmentCount, filled);
        }

        static Sprite StateSprite(int count, int filled)
        {
            int key = count * 100 + filled;
            if (sprites.TryGetValue(key, out var sprite) && sprite != null)
                return sprite;

            int width = count * 4 + 1,
                height = 5;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "EnemyHP_" + count + "_" + filled,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var pixels = new Color32[width * height];
            var frame = new Color32(30, 35, 38, 255);
            var rim = new Color32(123, 133, 133, 255);
            var empty = new Color32(42, 58, 49, 255);
            var fill = new Color32(89, 255, 115, 255);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool edge = x == 0 || x == width - 1 || y == 0 || y == height - 1;
                    pixels[y * width + x] = edge
                        ? (y == height - 1 ? rim : frame)
                        : (
                            (x - 1) % 4 == 3 ? frame
                            : (x - 1) / 4 < filled ? fill
                            : empty
                        );
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            sprite = Sprite.Create(
                texture,
                new Rect(0, 0, width, height),
                new Vector2(.5f, .5f),
                64,
                0,
                SpriteMeshType.FullRect
            );
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            sprites[key] = sprite;
            return sprite;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache()
        {
            activeBars = 0;
            ClearSprites();
            Application.quitting -= ClearSprites;
            Application.quitting += ClearSprites;
        }

        static void ClearSprites()
        {
            foreach (var sprite in sprites.Values)
            {
                if (sprite == null)
                    continue;

                var texture = sprite.texture;
                if (Application.isPlaying)
                {
                    Destroy(sprite);
                    Destroy(texture);
                }
                else
                {
                    DestroyImmediate(sprite);
                    DestroyImmediate(texture);
                }
            }
            sprites.Clear();
        }
    }
}
