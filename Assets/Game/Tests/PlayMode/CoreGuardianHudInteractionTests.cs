using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using CoreGuardian;

public class CoreGuardianHudInteractionTests
{
    static Type ViewType()=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianAugmentationView")).First(t=>t!=null);
    [UnityTest]
    public IEnumerator CardRevealIgnoresDoubleClickAndAllowsSelectionBeforeOtherCardsOpen()
    {
        var font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",20);var root=new GameObject("CardTest",typeof(RectTransform),typeof(Image),typeof(Button));
        root.GetComponent<RectTransform>().sizeDelta=new Vector2(320,410);var type=ViewType();var view=root.AddComponent(type);int selected=0;bool allowed=true;
        type.GetMethod("Configure").Invoke(view,new object[]{new AugmentationCard(AugmentationGrade.Platinum,AugmentationFamily.Gold),font,(Action)(()=>selected++),(Func<bool>)(()=>allowed)});
        try{
            Assert.That(root.GetComponentsInChildren<Text>(),Is.Empty,"Back has no labels or badges");
            root.GetComponent<Button>().onClick.Invoke();root.GetComponent<Button>().onClick.Invoke();Assert.That(selected,Is.Zero,"Double click during flip must not choose");
            float deadline=Time.realtimeSinceStartup+3f;
            while((bool)type.GetProperty("Busy").GetValue(view)&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(type.GetProperty("Busy").GetValue(view),Is.False,"The flip must finish within the deadline");
            Assert.That(root.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("600")),Is.True);
            root.GetComponent<Button>().onClick.Invoke();Assert.That(selected,Is.EqualTo(1));
            allowed=false;root.GetComponent<Button>().onClick.Invoke();Assert.That(selected,Is.EqualTo(1),"Timeout/phase change must reject stale clicks");
        }finally{UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(font);}
    }
    [UnityTest]
    public IEnumerator ClosingDuringRevealCancelsSparksAndLeavesNoPendingSelection()
    {
        var font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",20);var root=new GameObject("CardCancelTest",typeof(RectTransform),typeof(Image),typeof(Button));root.GetComponent<RectTransform>().sizeDelta=new Vector2(320,410);
        var type=ViewType();var view=root.AddComponent(type);int selected=0;
        type.GetMethod("Configure").Invoke(view,new object[]{new AugmentationCard(AugmentationGrade.Gold,AugmentationFamily.Core),font,(Action)(()=>selected++),(Func<bool>)(()=>true)});
        try{root.GetComponent<Button>().onClick.Invoke();yield return new WaitForSecondsRealtime(.4f);root.SetActive(false);yield return new WaitForSecondsRealtime(.8f);Assert.That(selected,Is.Zero);Assert.That(type.GetProperty("Busy").GetValue(view),Is.EqualTo(false));Assert.That(root.transform.Cast<Transform>().Any(t=>t.name=="RevealSpark"),Is.False);}finally{UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(font);}
    }
}
