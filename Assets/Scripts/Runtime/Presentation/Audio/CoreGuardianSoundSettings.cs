using UnityEngine;

public enum CoreGuardianAudioCategory { Music, Tower, Player, MonsterCore, UI, Progress }

// Local listening preferences, separate from combat data and BGM production assets.
public static class CoreGuardianSoundSettings
{
    const string VolumeKey="CoreGuardian.SoundVolume", CandidateKey="CoreGuardian.ShotCandidate";
    static float SafeVolume(float value)=>float.IsNaN(value)||float.IsInfinity(value)?.65f:Mathf.Clamp01(value);
    public static float Volume
    {
        get=>SafeVolume(PlayerPrefs.GetFloat(VolumeKey,.65f));
        set=>PlayerPrefs.SetFloat(VolumeKey,SafeVolume(value));
    }
    // Preserve the legacy master key. New category preferences default to unity gain.
    public static float GetCategoryVolume(CoreGuardianAudioCategory category)
    {
        float value=PlayerPrefs.GetFloat("CoreGuardian.Audio."+category,1f);
        return float.IsNaN(value)||float.IsInfinity(value)?1f:Mathf.Clamp01(value);
    }
    public static void SetCategoryVolume(CoreGuardianAudioCategory category,float value)
    {
        PlayerPrefs.SetFloat("CoreGuardian.Audio."+category,float.IsNaN(value)||float.IsInfinity(value)?1f:Mathf.Clamp01(value));
    }
    public static float EffectiveVolume(CoreGuardianAudioCategory category)=>Volume*GetCategoryVolume(category);
    // Legacy audition preference retained for compatibility; approved single audio ignores it.
    public static int Candidate
    {
        get=>Mathf.Clamp(PlayerPrefs.GetInt(CandidateKey,0),0,2);
        set=>PlayerPrefs.SetInt(CandidateKey,Mathf.Clamp(value,0,2));
    }
}
