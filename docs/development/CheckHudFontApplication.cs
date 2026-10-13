using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using CoreGuardian;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public static class CheckHudFontApplication
{
    public static string Import(){UnityEditor.AssetDatabase.Refresh();return "Galmuri import requested";}
    public static string Fonts(){
        foreach(var name in new[]{"Galmuri11","Galmuri11-Bold"}){
            var path="Assets/Resources/CoreGuardian/Fonts/"+name+".ttf";
            var importer=(UnityEditor.TrueTypeFontImporter)UnityEditor.AssetImporter.GetAtPath(path);
            importer.fontTextureCase=UnityEditor.FontTextureCase.Dynamic;
            importer.fontRenderingMode=UnityEditor.FontRenderingMode.HintedRaster;
            importer.SaveAndReimport();
            if(Resources.Load<Font>("CoreGuardian/Fonts/"+name)==null)throw new Exception("Missing font "+name);
        }
        return "Dynamic Galmuri fonts imported with hinted raster rendering";
    }
    static CoreGuardianPrototype Game()=>UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();
    static Button Button(string name)=>Game().GetComponentsInChildren<Button>(true).First(b=>b.name==name);
    public static string OpenInstall(){Time.timeScale=0;Button("Installation").onClick.Invoke();Button("PlayerGrowth").onClick.Invoke();return "Panels open";}
    public static string AttackList(){Button("공격 포탑").onClick.Invoke();return "Attack list open";}
    public static string SupportList(){Button("분류로 돌아가기").onClick.Invoke();Button("지원 포탑").onClick.Invoke();return "Support list open";}
    public static string Tower(){var g=Game();g.Diagnostic();return "Diagnostic reset; inspection towers are installed separately";}
    public static string SelectTower(){var g=Game();if(!g.Place(TowerKind.Single,new Vector2Int(14,15)))throw new Exception("Cannot place inspection tower");g.Notify("타워 텍스트 배치 검사");return "Single placed and selected via gameplay API";}
    public static string SelectSupport(){var g=Game();if(!g.Place(TowerKind.Support,new Vector2Int(16,15)))throw new Exception("Cannot place support");return "Support selected";}
    public static string SetCards(int group){var g=Game();var choices=AugmentationDeck.Cards.Skip(group*3).Take(3).ToArray();if(choices.Length!=3||g.Session.Phase!=SessionPhase.Augmentation)throw new Exception("Card inspection needs selection phase and group 0..5");typeof(Session).GetField("choices",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(g.Session,choices);g.Notify("카드 문구 배치 검사 · 실제 추첨 아님");return string.Join(",",choices.Select(x=>x.Id));}
    public static string RevealAll(){foreach(var view in Game().GetComponentsInChildren<CoreGuardianAugmentationView>())view.Click();return "All cards revealing for text inspection";}
    public static string Compact(){Button("TopFold").onClick.Invoke();return "Compact status";}
    public static string Press(string name){var b=Button(name);ExecuteEvents.Execute(b.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerDownHandler);return "Pressed";}
    public static string Release(string name){var b=Button(name);ExecuteEvents.Execute(b.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerUpHandler);return "Released";}
    public static string ScrollItems(){
        Time.timeScale=0;var g=Game();g.Session.DamageTotals.Clear();g.Session.DamageTotals["Player"]=123456;
        for(int i=1;i<=12;i++)g.Session.DamageTotals["Tower:"+i]=100000-i*1234;
        g.Notify("피해 목록 배치·스크롤 검사 · UI용 주입");return "13 inspection rows; no gameplay data changes";
    }
    public static string ScrollCheck(){
        Canvas.ForceUpdateCanvases();var viewport=GameObject.Find("DamagePanel/Scroll/Viewport");
        if(viewport==null)viewport=GameObject.Find("DamagePanel").transform.Find("Scroll/Viewport").gameObject;
        var sr=viewport.GetComponentInParent<ScrollRect>();if(viewport.GetComponent<RectMask2D>()==null||!viewport.GetComponent<Image>().raycastTarget)throw new Exception("Viewport missing clip or scroll hit surface");
        var p=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,viewport.transform.TransformPoint(((RectTransform)viewport.transform).rect.center)),scrollDelta=new Vector2(0,-4)};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(p,hits);
        if(!hits.Any(h=>h.gameObject.GetComponentInParent<ScrollRect>()==sr))throw new Exception("Scroll viewport raycast blocked");
        ExecuteEvents.ExecuteHierarchy(viewport,p,ExecuteEvents.scrollHandler);Canvas.ForceUpdateCanvases();
        if(sr.content.anchoredPosition.y<=0)throw new Exception("Scroll input did not move rows");
        return "13 rows clipped, viewport hit surface works, scroll y="+sr.content.anchoredPosition.y;
    }
    public static string FinalizeImages(){
        string dir="docs/development/hud_font_applied_20261013";string source="Assets/"+dir;
        foreach(var name in new[]{"Panels.png","Tower.png","Cards.png","TowerFinal.png","PanelsFinal.png","DamageScroll.png"}){
            string file=source+"/"+name;if(!File.Exists(file))continue;File.Copy(file,dir+"/"+name,true);UnityEditor.AssetDatabase.DeleteAsset(file);
        }
        if(Directory.Exists(source)&&Directory.GetFileSystemEntries(source).Length==0)UnityEditor.AssetDatabase.DeleteAsset(source);
        UnityEditor.AssetDatabase.Refresh();return "Inspection captures moved outside Assets";
    }
    public static string Inspect(string label)
    {
        Canvas.ForceUpdateCanvases();var canvas=GameObject.Find("CombatHUD").GetComponent<Canvas>();
        var rows=new List<object>();var failures=new List<string>();
        foreach(var t in canvas.GetComponentsInChildren<Text>().Where(t=>t.color.a>.01f&&!string.IsNullOrEmpty(t.text)))
        {
            var rect=t.rectTransform.rect;
            var settings=t.GetGenerationSettings(rect.size);
            settings.resizeTextForBestFit=false;settings.fontSize=t.fontSize;
            settings.verticalOverflow=VerticalWrapMode.Overflow;
            float preferred=new TextGenerator().GetPreferredHeight(t.text,settings)/t.pixelsPerUnit;
            var path=Path(t.transform);bool height=preferred<=rect.height+1;
            if(!height)failures.Add(path+" text height "+preferred+" > "+rect.height);
            var parent=t.transform.parent as RectTransform;
            bool contained=true;bool borderSafe=true;
            if(parent!=null){
                var corners=new Vector3[4];t.rectTransform.GetWorldCorners(corners);
                contained=corners.All(c=>{var p=parent.InverseTransformPoint(c);return p.x>=parent.rect.xMin-1&&p.x<=parent.rect.xMax+1&&p.y>=parent.rect.yMin-1&&p.y<=parent.rect.yMax+1;});
                if(!contained)failures.Add(path+" outside immediate box");
                var image=parent.GetComponent<Image>();
                if(image!=null&&image.type==Image.Type.Sliced&&image.sprite!=null){
                    var border=image.sprite.border/(image.pixelsPerUnit*image.pixelsPerUnitMultiplier);
                    var safe=parent.rect;safe.xMin+=border.x;safe.yMin+=border.y;safe.xMax-=border.z;safe.yMax-=border.w;
                    borderSafe=corners.All(c=>{var p=parent.InverseTransformPoint(c);return p.x>=safe.xMin-.5f&&p.x<=safe.xMax+.5f&&p.y>=safe.yMin-.5f&&p.y<=safe.yMax+.5f;});
                    if(!borderSafe)failures.Add(path+" text rectangle enters decorative frame");
                }
            }
            if(t.font==null||!t.font.name.StartsWith("Galmuri11"))failures.Add(path+" unexpected HUD font");
            t.font.RequestCharactersInTexture(t.text,t.fontSize,t.fontStyle);
            foreach(char c in t.text.Distinct().Where(c=>!char.IsWhiteSpace(c)))if(!t.font.HasCharacter(c))failures.Add(path+" missing glyph "+c);
            rows.Add(new {path,text=t.text,font=t.fontSize,fontName=t.font==null?null:t.font.name,width=rect.width,height=rect.height,preferredHeight=preferred,heightFits=height,contained,borderSafe});
        }
        var result=new {label,screen=new[]{Screen.width,Screen.height},canvas=new[]{((RectTransform)canvas.transform).rect.width,((RectTransform)canvas.transform).rect.height},textCount=rows.Count,failures,rows};
        var json=Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented);
        Directory.CreateDirectory("docs/development/hud_font_applied_20261013");
        File.WriteAllText("docs/development/hud_font_applied_20261013/"+label+".json",json.Replace("\r\n", "\n"));
        return Newtonsoft.Json.JsonConvert.SerializeObject(new{label,rows=rows.Count,failures});
    }
    static string Path(Transform t)=>t.parent==null?t.name:Path(t.parent)+"/"+t.name;
    public static string Restore(){Time.timeScale=1;return "timeScale 1";}
}
