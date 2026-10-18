using UnityEngine;
using UnityEngine.UI;

public sealed partial class CoreGuardianHud
{
    void BuildSoundControls()
    {
        var panel=Box("SoundControls",canvas,new Vector2(0,1),new Vector2(16,-296),new Vector2(302,358));
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
        Text("ApprovedSingle",panel,new Vector2(16,-100),new Vector2(174,36),18).text="단일 · ③ 확정";
        CompactSoundButton("PreviewShot",panel,new Vector2(194,-94),new Vector2(92,48),()=>"듣기",()=>{var audio=game.GetComponent<CoreGuardianCombatAudio>();if(audio!=null)audio.Preview();});
        BuildAuditionRow(panel,"Multi",152,"멀티",new[]{"① 얼음","② 독분출","③ 문양2"});
        Text("ApprovedPlayer",panel,new Vector2(16,-216),new Vector2(174,36),18).text="플레이어 · ③ 확정";
        CompactSoundButton("PreviewPlayer",panel,new Vector2(194,-210),new Vector2(92,48),()=>"듣기",()=>{var audio=game.GetComponent<CoreGuardianCombatAudio>();if(audio!=null)audio.PreviewPlayer();});
        Text("SupportSilent",panel,new Vector2(16,-274),new Vector2(270,36),18).text="지원 · 소리 사용 안 함";
        Text("Scope",panel,new Vector2(16,-326),new Vector2(270,24),15).text="멀티는 새 후보 듣기 전용";
        refresh.Add(()=>
        {
            slider.SetValueWithoutNotify(CoreGuardianSoundSettings.Volume);
            label.text="효과음 음량  "+Mathf.RoundToInt(CoreGuardianSoundSettings.Volume*100)+"%";
            slider.interactable=game.Session.Phase!=CoreGuardian.SessionPhase.Augmentation;
        });
    }
    void BuildAuditionRow(RectTransform panel,string role,float y,string title,string[] names)
    {
        int choice=0;
        CompactSoundButton("Audition_"+role,panel,new Vector2(16,-y),new Vector2(170,48),()=>title+" "+names[choice],()=>{choice=(choice+1)%names.Length;Refresh();});
        CompactSoundButton("Listen_"+role,panel,new Vector2(194,-y),new Vector2(92,48),()=>"듣기",()=>{var audio=game.GetComponent<CoreGuardianCombatAudio>();if(audio!=null)audio.PreviewAudition(role,choice);});
    }
    void CompactSoundButton(string name,Transform parent,Vector2 position,Vector2 size,System.Func<string> label,System.Action action)
    {
        var button=Button(name,parent,position,size,label,action);
        foreach(Transform child in button.transform)if(child.name.StartsWith("Symbol_"))child.gameObject.SetActive(false);
        var text=button.transform.Find("Label").GetComponent<UnityEngine.UI.Text>();
        text.rectTransform.anchoredPosition=new Vector2(8,-8);text.rectTransform.sizeDelta=new Vector2(size.x-16,32);
        text.alignment=TextAnchor.MiddleCenter;text.fontSize=16;
    }
}
