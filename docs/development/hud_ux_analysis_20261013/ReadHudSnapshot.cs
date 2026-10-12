using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using Newtonsoft.Json;

// Read-only inventory. Never creates UI, changes Play state, saves a scene or updates layout.
public static class ReadHudSnapshot
{
    static string PathOf(Transform t) { return t.parent==null?"/"+t.name:PathOf(t.parent)+"/"+t.name; }
    public static string Read()
    {
        var canvases=Resources.FindObjectsOfTypeAll<Canvas>().Where(c=>c.gameObject.scene.IsValid()&&c.gameObject.scene.isLoaded).ToArray();
        return JsonConvert.SerializeObject(new {
            capturedUtc=DateTime.UtcNow.ToString("o"),
            playing=EditorApplication.isPlaying, compiling=EditorApplication.isCompiling,
            activeScene=SceneManager.GetActiveScene().path,
            screen=new {width=Screen.width,height=Screen.height},
            canvases=canvases.Select(c=>new {
                path=PathOf(c.transform),active=c.gameObject.activeInHierarchy,renderMode=c.renderMode.ToString(),sort=c.sortingOrder,
                scaler=c.GetComponent<CanvasScaler>()==null?null:new {mode=c.GetComponent<CanvasScaler>().uiScaleMode.ToString(), reference=c.GetComponent<CanvasScaler>().referenceResolution.ToString(),match=c.GetComponent<CanvasScaler>().matchWidthOrHeight},
                elements=c.GetComponentsInChildren<RectTransform>(true).Select(r=>new {
                    path=PathOf(r),active=r.gameObject.activeSelf,hierarchyActive=r.gameObject.activeInHierarchy,
                    anchorMin=r.anchorMin.ToString(),anchorMax=r.anchorMax.ToString(),pivot=r.pivot.ToString(),position=r.anchoredPosition.ToString(),size=r.sizeDelta.ToString(),rect=r.rect.ToString(),
                    text=r.GetComponent<Text>()==null?null:r.GetComponent<Text>().text,
                    fontSize=r.GetComponent<Text>()==null?0:r.GetComponent<Text>().fontSize,
                    graphicRaycast=r.GetComponent<Graphic>()==null?(bool?)null:r.GetComponent<Graphic>().raycastTarget,
                    interactable=r.GetComponent<Selectable>()==null?(bool?)null:r.GetComponent<Selectable>().interactable,
                    components=r.GetComponents<Component>().Where(v=>v!=null).Select(v=>v.GetType().Name).ToArray()
                }).ToArray()
            }).ToArray()
        },Formatting.Indented);
    }
}
