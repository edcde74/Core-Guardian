using System;
using System.Collections;
using CoreGuardian;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class CoreGuardianAugmentationView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public bool Revealed {get;private set;}
    public bool Busy {get;private set;}
    Button button;RectTransform body;GameObject back,front;Image rim;Text title,description;
    AugmentationCard card;Action select;Func<bool> allowed;bool hover;
    static readonly Color[] colors={new Color(.76f,.83f,.9f),new Color(1,.76f,.28f),new Color(.5f,.94f,1)};
    public void Configure(AugmentationCard value,Font font,Action chosen,Func<bool> canChoose)
    {
        card=value;select=chosen;allowed=canChoose;button=GetComponent<Button>();button.transition=Selectable.Transition.None;
        GetComponent<Image>().color=new Color(0,0,0,0);button.onClick.AddListener(Click);
        body=Rect("CardBody",transform,Vector2.zero,GetComponent<RectTransform>().sizeDelta);
        rim=body.gameObject.AddComponent<Image>();CoreGuardianHudSkin.Frame(rim,"Disabled");rim.color=colors[(int)card.Grade];rim.raycastTarget=false;
        back=Rect("Back",body,Vector2.zero,body.sizeDelta-new Vector2(16,16)).gameObject;
        var art=back.AddComponent<Image>();art.sprite=CoreGuardianHudSkin.Sprite("CardBack"+card.Grade);art.preserveAspect=true;art.raycastTarget=false;
        front=Rect("Front",body,Vector2.zero,body.sizeDelta-new Vector2(48,48)).gameObject;
        float width=((RectTransform)front.transform).rect.width;
        // Card front: grade frame + name + text only (category icon removed, 아트 F2 · 2026-10-26).
        title=Label("Title",front.transform,Resources.Load<Font>("CoreGuardian/Fonts/Galmuri11-Bold")??font,new Vector2(14,-60),new Vector2(width-28,48),26);title.text=card.Title;
        description=Label("Effect",front.transform,font,new Vector2(14,-130),new Vector2(width-28,200),20);description.text=card.Description;
        Label("Choose",front.transform,font,new Vector2(14,-354),new Vector2(width-28,34),18).text="다시 클릭하여 선택";
        front.SetActive(false);
    }
    public void Click(){if(Busy||allowed==null||!allowed())return;if(Revealed){select();return;}StartCoroutine(Flip());}
    IEnumerator Flip()
    {
        Busy=true;float duration=new[]{.44f,.66f,.92f}[(int)card.Grade];float started=Time.unscaledTime;bool changed=false;
        PlaySound();
        while(Time.unscaledTime-started<duration){
            float p=(Time.unscaledTime-started)/duration;float eased=p*p*(3-2*p);
            body.localScale=new Vector3(Mathf.Max(.025f,Mathf.Abs(Mathf.Cos(eased*Mathf.PI))),1+Mathf.Sin(p*Mathf.PI)*.035f,1);
            if(!changed&&p>=.5f){changed=true;Revealed=true;back.SetActive(false);front.SetActive(true);StartCoroutine(Sparks());}
            yield return null;
        }
        body.localScale=Vector3.one;Revealed=true;Busy=false;
    }
    IEnumerator Sparks()
    {
        int count=new[]{14,30,52}[(int)card.Grade];
        for(int batch=0;batch<(card.Grade==AugmentationGrade.Platinum?2:1);batch++){
            for(int i=0;i<(batch==0?count:20);i++)StartCoroutine(Ray(i));
            yield return new WaitForSecondsRealtime(.17f);
        }
    }
    IEnumerator Ray(int index)
    {
        float seed=index*2.399963f;int edge=index%4;Vector2 dir=edge==0?Vector2.left:edge==1?Vector2.right:edge==2?Vector2.up:Vector2.down;
        float along=Mathf.Sin(seed);Vector2 half=GetComponent<RectTransform>().rect.size*.5f;Vector2 start=edge<2?new Vector2(dir.x*half.x,along*(half.y-10)):new Vector2(along*(half.x-10),dir.y*half.y);
        var rt=Rect("RevealSpark",transform,start,new Vector2(3,10+(index%4)*5));var img=rt.gameObject.AddComponent<Image>();img.raycastTarget=false;
        rt.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(dir.y,dir.x)*Mathf.Rad2Deg-90);
        float began=Time.unscaledTime;float life=.3f+(index%5)*.055f;
        while(Time.unscaledTime-began<life){float p=(Time.unscaledTime-began)/life;rt.anchoredPosition=start+dir*(30+((int)card.Grade)*30)*p;img.color=Color.Lerp(Color.white,colors[(int)card.Grade],p)*new Color(1,1,1,1-p);yield return null;}
        Destroy(rt.gameObject);
    }
    void PlaySound()
    {
        GetComponentInParent<CoreGuardianCombatAudio>()?.PlayCue(new[]{"aug_silver","aug_gold","aug_platinum"}[(int)card.Grade]);
    }
    void Update(){if(body!=null&&!Busy){body.localScale=Vector3.Lerp(body.localScale,hover?new Vector3(1.025f,1.025f,1):Vector3.one,Time.unscaledDeltaTime*18);rim.color=colors[(int)card.Grade]*(hover?1.18f:1);}}
    void OnDisable(){StopAllCoroutines();Busy=false;if(body!=null){body.localScale=Vector3.one;foreach(Transform child in transform)if(child.name=="RevealSpark")Destroy(child.gameObject);}hover=false;}
    public void OnPointerEnter(PointerEventData e){hover=true;}public void OnPointerExit(PointerEventData e){hover=false;}public void OnSelect(BaseEventData e){hover=true;}public void OnDeselect(BaseEventData e){hover=false;}
    static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;return r;}
    static Text Label(string name,Transform parent,Font font,Vector2 pos,Vector2 size,int points){var r=Rect(name,parent,pos,size);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=points;t.color=new Color(1,.95f,.84f);t.alignment=TextAnchor.MiddleCenter;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.raycastTarget=false;return t;}
}
