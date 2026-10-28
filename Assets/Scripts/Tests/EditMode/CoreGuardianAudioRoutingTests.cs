using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CoreGuardianAudioRoutingTests
{
    [Test]
    public void CategoryVolumeMultipliesMasterWithoutChangingOtherCategoriesOrSavedMaster()
    {
        var settings = typeof(CoreGuardianSoundSettings);
        var category = typeof(CoreGuardianAudioCategory);
        Assert.That(
            category,
            Is.Not.Null,
            "Independent audio categories must exist before connecting the selected library"
        );
        var get = settings.GetMethod("GetCategoryVolume");
        var set = settings.GetMethod("SetCategoryVolume");
        var effective = settings.GetMethod("EffectiveVolume");
        string[] names = Enum.GetNames(category);
        Assert.That(names, Is.EquivalentTo(new[] { "Music", "Tower", "Player", "MonsterCore", "UI", "Progress" }));
        string[] keys = names
            .Select(n => "CoreGuardian.Audio." + n)
            .Concat(new[] { "CoreGuardian.SoundVolume" })
            .ToArray();
        var existed = keys.Select(PlayerPrefs.HasKey).ToArray();
        var old = keys.Select(k => PlayerPrefs.GetFloat(k)).ToArray();
        try
        {
            PlayerPrefs.SetFloat("CoreGuardian.SoundVolume", .37f);
            foreach (string name in names)
            {
                PlayerPrefs.DeleteKey("CoreGuardian.Audio." + name);
            }

            object tower = Enum.Parse(category, "Tower"),
                ui = Enum.Parse(category, "UI");
            Assert.That(get.Invoke(null, new[] { tower }), Is.EqualTo(1f));
            Assert.That(
                PlayerPrefs.HasKey("CoreGuardian.Audio.Tower"),
                Is.False,
                "Reading defaults must not rewrite saved preferences"
            );
            set.Invoke(null, new[] { tower, (object).25f });
            Assert.That(PlayerPrefs.GetFloat("CoreGuardian.Audio.Tower"), Is.EqualTo(.25f));
            Assert.That(effective.Invoke(null, new[] { tower }), Is.EqualTo(.0925f).Within(.00001f));
            Assert.That(effective.Invoke(null, new[] { ui }), Is.EqualTo(.37f).Within(.00001f));
            set.Invoke(null, new[] { tower, (object)0f });
            Assert.That(effective.Invoke(null, new[] { tower }), Is.EqualTo(0f));
            set.Invoke(null, new[] { tower, (object)float.NaN });
            Assert.That(get.Invoke(null, new[] { tower }), Is.EqualTo(1f));
            set.Invoke(null, new[] { tower, (object)2f });
            Assert.That(get.Invoke(null, new[] { tower }), Is.EqualTo(1f));
            Assert.That(PlayerPrefs.GetFloat("CoreGuardian.SoundVolume"), Is.EqualTo(.37f));
        }
        finally
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (existed[i])
                {
                    PlayerPrefs.SetFloat(keys[i], old[i]);
                }
                else
                {
                    PlayerPrefs.DeleteKey(keys[i]);
                }
            }
        }
    }

    [Test]
    public void EverySelectedCueHasOneCategorizedRuntimeEntryAndResolvableClip()
    {
        var catalog = typeof(CoreGuardianAudioCatalog);
        Assert.That(catalog, Is.Not.Null, "A data driven cue catalog is required");
        var ids = new[]
        {
            "single",
            "player",
            "hit",
            "death",
            "core",
            "boss_spawn",
            "boss_accel",
            "install",
            "install_fail",
            "upgrade_ok",
            "upgrade_fail",
            "bgm",
            "cancel_install",
            "round_complete",
            "victory",
            "defeat",
            "ui_click",
            "ui_open",
            "ui_close",
            "aug_silver",
            "aug_bgm",
            "aug_gold",
            "aug_platinum",
            "demolish",
            "round_start",
            "multi_1",
            "multi_2",
            "multi_3",
        };
        var find = catalog.GetMethod("Find");
        foreach (var id in ids)
        {
            var cue = find.Invoke(null, new object[] { id });
            Assert.That(cue, Is.Not.Null, id);
            var type = cue.GetType();
            string path = (string)type.GetField("resourcePath").GetValue(cue);
            Assert.That(Resources.Load<AudioClip>(path), Is.Not.Null, id + " clip");
            string group = (string)type.GetField("category").GetValue(cue);
            Assert.That(Enum.IsDefined(typeof(CoreGuardianAudioCategory), group), Is.True, id + " category");
        }
        Assert.That(find.Invoke(null, new object[] { "support" }), Is.Null, "Support combat remains silent");
    }
}
