using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Shared by all small fold grips. Decoration never changes the button hit rectangle.
public sealed class CoreGuardianHudHandleReaction : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler,ISelectHandler,IDeselectHandler
{
    Button button;Image art;CanvasGroup edge;Vector2 rest;bool horizontal,hover,pressed,focused;
    public void Configure(Image image,bool isHorizontal)
    {
        button=GetComponent<Button>();button.transition=Selectable.Transition.None;art=image;horizontal=isHorizontal;rest=art.rectTransform.anchoredPosition;
        var g=new GameObject("HoverEdge",typeof(RectTransform),typeof(CanvasGroup));var r=g.GetComponent<RectTransform>();r.SetParent(art.transform,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=art.rectTransform.sizeDelta;
        edge=g.GetComponent<CanvasGroup>();edge.interactable=false;edge.blocksRaycasts=false;
        for(int i=0;i<2;i++)
        {
            var line=new GameObject(i==0?"Left":"Right",typeof(RectTransform),typeof(Image));var lr=line.GetComponent<RectTransform>();lr.SetParent(r,false);lr.anchorMin=lr.anchorMax=lr.pivot=new Vector2(.5f,.5f);
            lr.sizeDelta=new Vector2(1,Mathf.Round(r.sizeDelta.y*.68f));lr.anchoredPosition=new Vector2((i==0?-1:1)*Mathf.Round(r.sizeDelta.x*.28f),0);
            var graphic=line.GetComponent<Image>();graphic.color=new Color(.12f,.9f,1);graphic.raycastTarget=false;
        }
        Apply();
    }
    void Apply()
    {
        if(art==null||edge==null)return;bool usable=button!=null&&button.interactable;
        edge.alpha=usable&&(hover||focused)?(pressed?.35f:1):0;
        art.color=!usable?new Color(.55f,.58f,.6f):pressed?new Color(.6f,.7f,.74f):Color.white;
        art.rectTransform.anchoredPosition=rest+(usable&&pressed?(horizontal?new Vector2(0,-1):new Vector2(1,0)):Vector2.zero);
    }
    void LateUpdate(){Apply();}
    public void OnPointerEnter(PointerEventData e){hover=true;Apply();}
    public void OnPointerExit(PointerEventData e){hover=pressed=focused=false;Apply();}
    public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)pressed=true;Apply();}
    public void OnPointerUp(PointerEventData e){pressed=false;Apply();}
    public void OnSelect(BaseEventData e){focused=true;Apply();}
    public void OnDeselect(BaseEventData e){focused=pressed=false;Apply();}
    void OnDisable(){hover=pressed=focused=false;Apply();}
}
