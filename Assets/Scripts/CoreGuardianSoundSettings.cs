using UnityEngine;

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
    public static int Candidate
    {
        get=>Mathf.Clamp(PlayerPrefs.GetInt(CandidateKey,0),0,2);
        set=>PlayerPrefs.SetInt(CandidateKey,Mathf.Clamp(value,0,2));
    }
}
