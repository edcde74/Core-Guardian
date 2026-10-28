using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Builds the runtime references for the 2026-10 content (신규 적·포탑·보스 prefab, sprite library).
// Copies existing prefabs and swaps only the confirmed body sprite; source art is never moved or edited.
public static class CoreGuardianContentAssets
{
    const string UnitDir = "Assets/Prefabs/Gameplay/Units/Resources/CoreGuardian/Units";
    const string BossDir = "Assets/Prefabs/Characters/Enemies/Resources/CoreGuardian/Animation/Boss";
    const string LibraryPath = "Assets/Data/Presentation/Resources/CoreGuardian/SpriteLibrary.asset";

    [MenuItem("CoreGuardian/Build Content Assets")]
    public static void Build()
    {
        Directory.CreateDirectory(UnitDir);
        foreach (var kind in new[] { "charger", "splitter", "healer", "armored", "flyer", "splitchild" })
        {
            Copy(
                "Assets/Prefabs/Gameplay/Units/enemy_attack.prefab",
                UnitDir + "/enemy_" + kind + ".prefab",
                "Assets/Art/Characters/Enemies/Sprites/enemy_" + kind + "_v1.png"
            );
        }

        foreach (var kind in new[] { "frost", "flame", "rail" })
        {
            Copy(
                "Assets/Prefabs/Gameplay/Units/tower_single.prefab",
                UnitDir + "/tower_" + kind + ".prefab",
                "Assets/Art/Characters/Towers/Sprites/tower_" + kind + ".png"
            );
        }

        foreach (var boss in new[] { "IronKnight", "CrimsonLord" })
        {
            Copy(
                BossDir + "/BossAnimated.prefab",
                BossDir + "/" + boss + "Animated.prefab",
                "Assets/Art/Characters/Enemies/Boss/" + boss + "/Walk/Left_0.png"
            );
        }

        BuildLibrary();
        AssetDatabase.SaveAssets();
    }

    static void Copy(string source, string target, string spritePath)
    {
        var sprite = AssetDatabase.LoadAllAssetsAtPath(spritePath).OfType<Sprite>().FirstOrDefault();
        if (sprite == null)
        {
            Debug.LogError("Missing sprite " + spritePath);
            return;
        }
        if (!File.Exists(target) && !AssetDatabase.CopyAsset(source, target))
        {
            Debug.LogError("Copy failed " + target);
            return;
        }
        var root = PrefabUtility.LoadPrefabContents(target);
        try
        {
            var art = root.GetComponentsInChildren<SpriteRenderer>(true).First(r => r.name == "2D Sprite");
            art.sprite = sprite;
            PrefabUtility.SaveAsPrefabAsset(root, target);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void BuildLibrary()
    {
        var keys = new List<string>();
        var sprites = new List<Sprite>();
        void Add(string folder, string group)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                return;

            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sub = Path.GetDirectoryName(path).Replace('\\', '/').Substring(folder.Length).Trim('/');
                foreach (var s in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
                {
                    keys.Add(
                        group + (sub.Length > 0 && group == "Fx" ? "/" + sub.Split('/').Last() : "") + "/" + s.name
                    );
                    sprites.Add(s);
                }
            }
        }
        foreach (var boss in new[] { "IronKnight", "CrimsonLord" })
        {
            Add("Assets/Art/Characters/Enemies/Boss/" + boss, boss);
        }

        Add("Assets/Art/Effects/Bosses/Sprites", "Fx");
        Add("Assets/Art/Effects/Combat/Sprites/Hits", "Fx");
        Add("Assets/Art/Effects/Combat/Sprites/RailBolt", "Fx");
        var library = AssetDatabase.LoadAssetAtPath<CoreGuardianSpriteLibrary>(LibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<CoreGuardianSpriteLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }
        library.SetSprites(keys.ToArray(), sprites.ToArray());
        EditorUtility.SetDirty(library);
        Debug.Log("CoreGuardian sprite library: " + keys.Count + " sprites");
    }
}
