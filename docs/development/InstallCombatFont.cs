using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;
using Newtonsoft.Json;

public static class InstallCombatFont
{
    const string Source = "Assets/Art/Combat/20261010/Fonts/PixelifySans-Bold.ttf";
    const string Folder = "Assets/Resources/CoreGuardian/Fonts";
    const string FontPath = Folder + "/PixelifySansBoldDamage.asset";
    const string MaterialPath = Folder + "/PixelifySansBoldDamageOutline.mat";
    const string Glyphs = "0123456789+-., ";

    public static object Main()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play mode before font asset installation.");
        if (Resources.Load<TMP_Settings>("TMP Settings") == null)
        {
            TMP_PackageResourceImporter.ImportResources(true, false, false);
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var source = AssetDatabase.LoadAssetAtPath<Font>(Source);
        if (source == null) throw new Exception("Static Bold TTF was not imported: " + Source);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            font = TMP_FontAsset.CreateFontAsset(source, 64, 8, GlyphRenderMode.SDFAA, 512, 512, AtlasPopulationMode.Dynamic, false);
            if (font == null || font.material == null || font.material.shader == null) throw new Exception("TMP font creation failed; check TMP Essential Resources.");
            string missing;
            if (!font.TryAddCharacters(Glyphs, out missing, false) || !string.IsNullOrEmpty(missing)) throw new Exception("TMP atlas missing glyphs: " + missing);
            font.name = "PixelifySansBoldDamage";
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
            font.material.name = "PixelifySansBoldDamage Base";
            AssetDatabase.CreateAsset(font, FontPath);
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var atlas in font.atlasTextures)
            {
                atlas.name = "PixelifySansBoldDamage Atlas";
                atlas.wrapMode = TextureWrapMode.Clamp;
                atlas.filterMode = FilterMode.Bilinear;
                AssetDatabase.AddObjectToAsset(atlas, font);
                EditorUtility.SetDirty(atlas);
            }
            EditorUtility.SetDirty(font);
        }
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(font.material);
            material.name = "PixelifySansBoldDamageOutline";
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        if (!material.HasProperty("_OutlineWidth")) throw new Exception("Selected TMP SDF shader has no outline support.");
        material.SetColor("_FaceColor", Color.white);
        material.SetColor("_OutlineColor", new Color(0.055f, 0.07f, 0.085f, 1f));
        material.SetFloat("_OutlineWidth", 0.12f);
        material.SetFloat("_OutlineSoftness", 0f);
        material.EnableKeyword("OUTLINE_ON");
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return Verify();
    }

    public static object Verify()
    {
        var font = Resources.Load<TMP_FontAsset>("CoreGuardian/Fonts/PixelifySansBoldDamage");
        var material = Resources.Load<Material>("CoreGuardian/Fonts/PixelifySansBoldDamageOutline");
        if (font == null || material == null) throw new Exception("Damage font resources load failed.");
        var missing = Glyphs.Where(c => !font.HasCharacter(c)).Select(c => c.ToString()).ToArray();
        if (missing.Length > 0) throw new Exception("Saved atlas missing characters: " + string.Join("", missing));
        if (font.atlasPopulationMode != AtlasPopulationMode.Static) throw new Exception("Expected static numeric atlas.");
        if (material.mainTexture != font.atlasTextures[0]) throw new Exception("Outline material atlas mismatch.");
        var canvasGo = new GameObject("TMP_DamageFont_TransientCanvas", typeof(RectTransform), typeof(Canvas));
        canvasGo.hideFlags = HideFlags.HideAndDontSave;
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var go = new GameObject("TMP_DamageFont_TransientVerify", typeof(RectTransform));
        go.hideFlags = HideFlags.HideAndDontSave;
        go.transform.SetParent(canvasGo.transform, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 80);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSharedMaterial = material;
        text.fontSize = 32;
        text.text = "13 25 128 1024";
        Canvas.ForceUpdateCanvases();
        text.ForceMeshUpdate(true, true);
        int characters = text.textInfo.characterCount;
        int vertices = text.textInfo.meshInfo[0].vertexCount;
        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(canvasGo);
        if (characters < 13 || vertices == 0) throw new Exception("TMP mesh smoke verification failed: characters=" + characters + ", vertices=" + vertices);
        var report = new {
            success = true, source = Source, fontPath = FontPath, materialPath = MaterialPath,
            fontResource = "CoreGuardian/Fonts/PixelifySansBoldDamage", materialResource = "CoreGuardian/Fonts/PixelifySansBoldDamageOutline",
            family = font.faceInfo.familyName, style = font.faceInfo.styleName, population = font.atlasPopulationMode.ToString(),
            glyphs = Glyphs, glyphCount = font.glyphTable.Count, atlasWidth = font.atlasTextures[0].width, atlasHeight = font.atlasTextures[0].height,
            shader = material.shader.name, outlineWidth = material.GetFloat("_OutlineWidth"), smokeCharacters = characters, smokeVertices = vertices
        };
        File.WriteAllText("docs/development/combat_font_install_20261010.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return report;
    }
}
