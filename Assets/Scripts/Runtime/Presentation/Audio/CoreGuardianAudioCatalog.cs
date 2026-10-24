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
    [NonSerialized] bool categoryParsed;
    [NonSerialized] string parsedCategory;
    [NonSerialized] CoreGuardianAudioCategory parsedValue;
    // Parsed once per category string. Enum.Parse boxed on every voice-volume update each frame.
    public CoreGuardianAudioCategory Category
    {
        get
        {
            if(!categoryParsed||!ReferenceEquals(parsedCategory,category))
            {parsedValue=(CoreGuardianAudioCategory)Enum.Parse(typeof(CoreGuardianAudioCategory),category);parsedCategory=category;categoryParsed=true;}
            return parsedValue;
        }
    }
    public AudioClip Clip=>clip!=null?clip:(clip=Resources.Load<AudioClip>(resourcePath));
    public float Volume=>CoreGuardianSoundSettings.EffectiveVolume(Category)*gain;
}

public static class CoreGuardianAudioCatalog
{
    [Serializable] sealed class Data {public CoreGuardianAudioCue[] cues;}
    static Dictionary<string,CoreGuardianAudioCue> cues;
    static readonly Dictionary<AudioClip,CoreGuardianAudioCue> byClip=new Dictionary<AudioClip,CoreGuardianAudioCue>();
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
        // The catalog is immutable after load. Reuse a first match only while it still owns this clip.
        if(clip!=null&&byClip.TryGetValue(clip,out var cached)&&cached.Clip==clip)return cached;
        var result=cues.Values.FirstOrDefault(c=>c.Clip==clip);
        if(clip!=null&&result!=null)byClip[clip]=result;
        return result;
    }
}
