using UnityEngine;
using UnityEngine.UI;

public sealed partial class CoreGuardianHud
{
    void BuildSoundControls()
    {
        var panel=Box("SoundControls",canvas,new Vector2(0,1),new Vector2(16,-296),new Vector2(302,182));
        var label=Text("VolumeLabel",panel,new Vector2(16,-10),new Vector2(270,30),20);
        var track=Box("SoundVolume",panel,new Vector2(0,1),new Vector2(20,-50),new Vector2(262,32),false);
        // A full-height hit area keeps a thin visual track easy to drag.
        var hit=track.gameObject.AddComponent<Image>();hit.color=Color.clear;
        var background=Box("Track",track,new Vector2(0,.5f),Vector2.zero,new Vector2(262,8),false).gameObject.AddComponent<Image>();
        background.color=new Color(.12f,.16f,.18f);background.raycastTarget=false;
        var fillArea=Box("FillArea",track,new Vector2(0,.5f),Vector2.zero,new Vector2(262,8),false);
        var fill=Box("Fill",fillArea,new Vector2(0,.5f),Vector2.zero,Vector2.zero,false).gameObject.AddComponent<Image>();
        fill.color=new Color(.24f,.72f,.66f);fill.raycastTarget=false;
        var handleArea=Box("HandleArea",track,new Vector2(0,.5f),new Vector2(9,0),new Vector2(244,28),false);
        var handle=Box("Handle",handleArea,new Vector2(0,.5f),Vector2.zero,new Vector2(18,0),false).gameObject.AddComponent<Image>();
        handle.color=new Color(.83f,.72f,.44f);
        var slider=track.gameObject.AddComponent<Slider>();slider.minValue=0;slider.maxValue=1;
        slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;
        slider.SetValueWithoutNotify(CoreGuardianSoundSettings.Volume);
        slider.onValueChanged.AddListener(value=>{CoreGuardianSoundSettings.Volume=value;label.text="효과음 음량  "+Mathf.RoundToInt(value*100)+"%";});
        var candidate=Button("ShotCandidate",panel,new Vector2(16,-94),new Vector2(170,48),()=>"마력포 "+CoreGuardianCombatAudio.CandidateNames[CoreGuardianSoundSettings.Candidate],()=>CoreGuardianSoundSettings.Candidate=(CoreGuardianSoundSettings.Candidate+1)%3);
        var preview=Button("PreviewShot",panel,new Vector2(194,-94),new Vector2(92,48),()=>"듣기",()=>{var audio=game.GetComponent<CoreGuardianCombatAudio>();if(audio!=null)audio.Preview();});
        foreach(var button in new[]{candidate,preview})
        {
            // These compact audio controls use centered labels; the generic button reserves an icon column.
            foreach(Transform child in button.transform)if(child.name.StartsWith("Symbol_"))child.gameObject.SetActive(false);
            var text=button.transform.Find("Label").GetComponent<Text>();
            text.rectTransform.anchoredPosition=new Vector2(8,-8);
            text.rectTransform.sizeDelta=new Vector2(((RectTransform)button.transform).sizeDelta.x-16,32);
            text.alignment=TextAnchor.MiddleCenter;text.fontSize=18;
        }
        Text("Scope",panel,new Vector2(16,-148),new Vector2(270,24),15).text="단일 포탑 발사음 · 시험 적용";
        refresh.Add(()=>
        {
            slider.SetValueWithoutNotify(CoreGuardianSoundSettings.Volume);
            label.text="효과음 음량  "+Mathf.RoundToInt(CoreGuardianSoundSettings.Volume*100)+"%";
            slider.interactable=game.Session.Phase!=CoreGuardian.SessionPhase.Augmentation;
        });
    }
}
