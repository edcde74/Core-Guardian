using System.Collections.Generic;
using UnityEngine;

// Name lookup for confirmed sprites that live outside Resources folders (boss walks, hit sparks,
// boss ability effects, rail bolt). The asset only references the source sprites; nothing is copied.
[CreateAssetMenu(menuName = "CoreGuardian/Sprite Library")]
public sealed class CoreGuardianSpriteLibrary : ScriptableObject
{
    public const string ResourcePath = "CoreGuardian/SpriteLibrary";

    [SerializeField]
    string[] names = System.Array.Empty<string>();

    [SerializeField]
    Sprite[] sprites = System.Array.Empty<Sprite>();
    Dictionary<string, Sprite> lookup;
    static CoreGuardianSpriteLibrary instance;
    static bool searched;

    // Domain Reload is off: search again on every Play (a missing library is not remembered forever) (B24).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCache()
    {
        instance = null;
        searched = false;
    }

    public static CoreGuardianSpriteLibrary Instance
    {
        get
        {
            if (instance == null && !searched)
            {
                searched = true;
                instance = Resources.Load<CoreGuardianSpriteLibrary>(ResourcePath);
            }
            return instance;
        }
    }
    public IReadOnlyList<Sprite> Sprites => sprites;

    // Keys are "Group/Name" (e.g. IronKnight/Left_0) so equal file names never collide.
    public void SetSprites(string[] keys, Sprite[] value)
    {
        names = keys ?? System.Array.Empty<string>();
        sprites = value ?? System.Array.Empty<Sprite>();
        lookup = null;
    }

    public Sprite Get(string name)
    {
        if (lookup == null)
        {
            lookup = new Dictionary<string, Sprite>();
            for (int i = 0; i < sprites.Length && i < names.Length; i++)
            {
                if (sprites[i] != null && !lookup.ContainsKey(names[i]))
                {
                    lookup.Add(names[i], sprites[i]);
                }
            }
        }
        return lookup.TryGetValue(name, out var sprite) ? sprite : null;
    }

    // Frames named Prefix_0.., Prefix_00.. in order; empty when none exist.
    public Sprite[] Frames(string prefix)
    {
        var list = new List<Sprite>();
        for (int i = 0; i < 64; i++)
        {
            var s = Get(prefix + i).OrNull() ?? Get(prefix + i.ToString("00"));
            if (s == null)
                break;
            list.Add(s);
        }
        return list.ToArray();
    }

    // Resources first (Resources.Load path), then the library by key.
    public static Sprite Find(string resourcePath, string name)
    {
        var sprite = resourcePath != null ? Resources.Load<Sprite>(resourcePath) : null;
        return sprite != null ? sprite
            : Instance != null ? Instance.Get(name)
            : null;
    }
}
