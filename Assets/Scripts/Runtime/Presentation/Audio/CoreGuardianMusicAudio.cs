using CoreGuardian;
using UnityEngine;

// Two bounded music voices: phase crossfades never contend with combat/UI effects.
public sealed class CoreGuardianMusicAudio : MonoBehaviour
{
    readonly AudioSource[] sources=new AudioSource[2];
    readonly float[] levels=new float[2];
    readonly int[] resumeSamples=new int[2];
    CoreGuardianAudioCue[] cues;
    Session session;
    float duckUntil,duck=1;
    public string CurrentCue {get;private set;}
    void Awake()
    {
        cues=new[]{CoreGuardianAudioCatalog.Find("bgm"),CoreGuardianAudioCatalog.Find("aug_bgm")};
        for(int i=0;i<2;i++)
        {
            var child=new GameObject(i==0?"BattleMusic":"AugmentationMusic");child.transform.SetParent(transform,false);
            var source=child.AddComponent<AudioSource>();sources[i]=source;
            source.playOnAwake=false;source.loop=true;source.spatialBlend=0;source.pitch=1;source.ignoreListenerPause=true;source.volume=0;source.clip=cues[i].Clip;
        }
    }
    public void Configure(Session value)
    {
        if(!ReferenceEquals(value,session))
        {
            foreach(var source in sources)source.Stop();
            for(int i=0;i<2;i++){levels[i]=0;resumeSamples[i]=0;}
            duckUntil=0;duck=1;
        }
        session=value;
    }
    public void Duck(float seconds)=>duckUntil=Mathf.Max(duckUntil,Time.unscaledTime+seconds);
    void Update()
    {
        int target=session==null||session.Phase==SessionPhase.Victory||session.Phase==SessionPhase.Defeat?-1:session.Phase==SessionPhase.Augmentation?1:0;
        CurrentCue=target<0?null:cues[target].id;
        duck=Mathf.MoveTowards(duck,Time.unscaledTime<duckUntil?.4f:1f,Time.unscaledDeltaTime*4);
        for(int i=0;i<2;i++)
        {
            var source=sources[i];
            // Muting does not restart the track; phase changes still advance normally.
            levels[i]=Mathf.MoveTowards(levels[i],i==target?1:0,Time.unscaledDeltaTime/.35f);
            if(i==target&&!source.isPlaying)
            {
                if(source.clip!=null){source.timeSamples=Mathf.Clamp(resumeSamples[i],0,source.clip.samples-1);source.Play();}
            }
            source.volume=cues[i].Volume*levels[i]*duck;
            if(i!=target&&levels[i]<=0&&source.isPlaying){resumeSamples[i]=source.timeSamples;source.Stop();}
        }
    }
    void OnDisable(){foreach(var source in sources)if(source!=null)source.Stop();}
}
