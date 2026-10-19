using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class CoreGuardianAudioCue
{
    public string id,category,resourcePath;
    public float gain=.55f;
    public int priority;
    public bool important,protectCore;
    [NonSerialized] AudioClip clip;
    public CoreGuardianAudioCategory Category=>(CoreGuardianAudioCategory)Enum.Parse(typeof(CoreGuardianAudioCategory),category);
    public AudioClip Clip=>clip!=null?clip:(clip=Resources.Load<AudioClip>(resourcePath));
    public float Volume=>CoreGuardianSoundSettings.EffectiveVolume(Category)*gain;
}

public static class CoreGuardianAudioCatalog
{
    [Serializable] sealed class Data {public CoreGuardianAudioCue[] cues;}
    static Dictionary<string,CoreGuardianAudioCue> cues;
    public static CoreGuardianAudioCue Find(string id)
    {
        if(cues==null)
        {
            var asset=Resources.Load<TextAsset>("CoreGuardian/Audio/SelectedB/catalog");
            if(asset==null)throw new InvalidOperationException("Missing selected audio catalog");
            cues=new Dictionary<string,CoreGuardianAudioCue>();
            foreach(var cue in JsonUtility.FromJson<Data>(asset.text).cues)cues.Add(cue.id,cue);
        }
        return cues.TryGetValue(id,out var result)?result:null;
    }
    public static CoreGuardianAudioCue ForClip(AudioClip clip)
    {
        Find("");
        return cues.Values.FirstOrDefault(c=>c.Clip==clip);
    }
}
