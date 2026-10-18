using System;
using NUnit.Framework;
using UnityEngine;

public class CoreGuardianSoundTests
{
    [Test]
    public void AllAttackRolesUseApprovedClipsAndNoAuditionsRemain()
    {
        var type=Type.GetType("CoreGuardianCombatAudio, Assembly-CSharp");
        var field=type.GetField("SingleShotResourcePath");
        Assert.That(field,Is.Not.Null,"Approved single shot must be independent from old candidate preferences");
        Assert.That((string)field.GetRawConstantValue(),Is.EqualTo("CoreGuardian/Audio/TowerShot/03_Aura"));
        var player=type.GetField("PlayerShotResourcePath");
        Assert.That(player,Is.Not.Null,"Approved player attack must be connected");
        Assert.That((string)player.GetRawConstantValue(),Is.EqualTo("CoreGuardian/Audio/PlayerShot/03_Glyph"));
        Assert.That(Resources.Load<AudioClip>((string)player.GetRawConstantValue()),Is.Not.Null);
        var multi=type.GetField("MultiShotResourcePath");
        Assert.That(multi,Is.Not.Null,"Selected B must be connected to multi-tower attacks");
        Assert.That((string)multi.GetRawConstantValue(),Is.EqualTo("CoreGuardian/Audio/MultiShot/B_BrighterAttack"));
        var multiClip=Resources.Load<AudioClip>((string)multi.GetRawConstantValue());
        Assert.That(multiClip,Is.Not.Null);Assert.That(multiClip.length,Is.EqualTo(.64f).Within(.001f));
        foreach(string role in new[]{"Multi","Player","Support"})Assert.That(Resources.LoadAll<AudioClip>("CoreGuardian/Audio/Auditions/"+role),Is.Empty);
    }
    [Test]
    public void VolumeIsPersistedClampedAndRejectsNonFiniteInput()
    {
        const string key="CoreGuardian.SoundVolume";
        bool existed=PlayerPrefs.HasKey(key);float previous=PlayerPrefs.GetFloat(key);
        try
        {
            var type=Type.GetType("CoreGuardianSoundSettings, Assembly-CSharp");
            Assert.That(type,Is.Not.Null,"Sound volume controls are missing");
            var volume=type.GetProperty("Volume");
            volume.SetValue(null,.37f);
            Assert.That(PlayerPrefs.GetFloat(key),Is.EqualTo(.37f).Within(.00001f));
            volume.SetValue(null,-1f);Assert.That(volume.GetValue(null),Is.EqualTo(0f));
            volume.SetValue(null,2f);Assert.That(volume.GetValue(null),Is.EqualTo(1f));
            volume.SetValue(null,float.NaN);Assert.That(float.IsNaN((float)volume.GetValue(null)),Is.False);
            PlayerPrefs.SetFloat(key,float.PositiveInfinity);
            Assert.That(volume.GetValue(null),Is.EqualTo(.65f));
        }
        finally{if(existed)PlayerPrefs.SetFloat(key,previous);else PlayerPrefs.DeleteKey(key);}
    }

    [Test]
    public void MagicCannonCandidatesHaveAudibleSamplesAndShortNonLoopingTails()
    {
        var clips=Resources.LoadAll<AudioClip>("CoreGuardian/Audio/TowerShot");
        Assert.That(clips.Length,Is.EqualTo(1),"Only the approved single-tower clip remains in its runtime folder");
        foreach(var clip in clips)
        {
            Assert.That(clip.length,Is.InRange(.3f,.85f),clip.name);
            var data=new float[clip.samples*clip.channels];
            Assert.That(clip.GetData(data,0),Is.True);
            float peak=0;foreach(float value in data)peak=Mathf.Max(peak,Mathf.Abs(value));
            Assert.That(peak,Is.InRange(.1f,.9f),clip.name);
            Assert.That(Mathf.Abs(data[data.Length-1]),Is.LessThan(.001f),"Fade must avoid a cutoff click");
        }
    }
}
