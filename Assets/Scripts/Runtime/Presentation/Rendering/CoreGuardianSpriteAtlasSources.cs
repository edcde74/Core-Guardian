using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

// Source identity is separate from the packed display texture. B keeps its original mask arrays.
public sealed class CoreGuardianSpriteAtlasSources : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public Sprite sprite;
        public Texture2D texture;
        public Rect rect;
    }

    // 빌드 도구(CoreGuardianSpriteAtlasPipeline)가 채움 — 인스펙터에서는 읽기 전용.
    [SerializeField, NaughtyAttributes.ReadOnly]
    Entry[] entries = Array.Empty<Entry>();

    sealed class Mapping
    {
        public Texture2D texture;
        public Vector4 uv;
    }

    static ConditionalWeakTable<Sprite, Mapping> mappings = new ConditionalWeakTable<Sprite, Mapping>();
    static readonly Dictionary<Sprite, Mapping> authored = new Dictionary<Sprite, Mapping>();
    static CoreGuardianSpriteAtlasSources loaded;
    static bool initialized;

    public void Initialize(Entry[] values)
    {
        entries = values;
        Reset();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        CoreGuardianOriginalWalk.ClearCachedParts();
        mappings = new ConditionalWeakTable<Sprite, Mapping>();
        authored.Clear();
        loaded = null;
        initialized = false;
    }

    static void EnsureLoaded()
    {
        if (initialized)
            return;

        initialized = true;
        loaded = Resources.Load<CoreGuardianSpriteAtlasSources>("CoreGuardian/SpriteAtlases/Sources");
        if (loaded == null)
            return;

        foreach (var entry in loaded.entries)
        {
            if (entry.sprite != null && entry.texture != null)
            {
                Register(entry.sprite, entry.texture, entry.rect);
            }
        }
    }

    static void Register(Sprite sprite, Texture2D original, Rect originalRect)
    {
        if (
            sprite.packed
            && (sprite.packingRotation != SpritePackingRotation.None || sprite.packingMode == SpritePackingMode.Tight)
        )
        {
            throw new InvalidOperationException(
                "Core Guardian part sprites require rectangular, unrotated atlas packing: " + sprite.name
            );
        }

        var packed = sprite.textureRect;
        var scale = new Vector2(
            sprite.texture.width / (float)original.width,
            sprite.texture.height / (float)original.height
        );
        var offset = (originalRect.position - packed.position) / new Vector2(original.width, original.height);
        // Imported Sprite references can have different managed wrappers for the same Unity object.
        authored[sprite] = new Mapping { texture = original, uv = new Vector4(scale.x, scale.y, offset.x, offset.y) };
    }

    public static bool TryGetSource(Sprite sprite, out Texture2D texture, out Vector4 uv)
    {
        EnsureLoaded();
        if (mappings.TryGetValue(sprite, out var value) || authored.TryGetValue(sprite, out value))
        {
            texture = value.texture;
            uv = value.uv;
            return texture != null;
        }
        texture = sprite.texture;
        uv = new Vector4(1, 1, 0, 0);
        return !sprite.packed;
    }

    public static Sprite Cut(Sprite source, Rect localRect, Vector2 pivot, float ppu)
    {
        if (
            source.packed
            && (source.packingRotation != SpritePackingRotation.None || source.packingMode == SpritePackingMode.Tight)
        )
        {
            throw new InvalidOperationException("Cannot crop a rotated or tightly packed source: " + source.name);
        }

        var rect = localRect;
        rect.position += source.textureRect.position;
        var result = Sprite.Create(source.texture, rect, pivot, ppu, 0, SpriteMeshType.FullRect);
        if (TryGetSource(source, out var texture, out var uv))
        {
            mappings.Add(result, new Mapping { texture = texture, uv = uv });
        }

        return result;
    }

    public static Rect OriginalRect(Sprite sprite, out Texture2D texture)
    {
        var rect = sprite.textureRect;
        if (!TryGetSource(sprite, out texture, out var uv) || texture == sprite.texture)
            return rect;

        return new Rect(
            Mathf.Round((rect.x / sprite.texture.width * uv.x + uv.z) * texture.width),
            Mathf.Round((rect.y / sprite.texture.height * uv.y + uv.w) * texture.height),
            rect.width,
            rect.height
        );
    }
}
