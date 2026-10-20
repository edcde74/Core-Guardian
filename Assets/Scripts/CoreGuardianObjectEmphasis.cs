using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Selected B2: original body tint plus a separate union silhouette, below inspection and upgrades.
[DisallowMultipleComponent]
[DefaultExecutionOrder(450)]
public sealed class CoreGuardianObjectEmphasis : MonoBehaviour
{
    [Header("개체 강조 B2 · 상시 윤곽")]
    [Tooltip("상시 윤곽의 색과 불투명도. 초록색 선택 아웃라인과 별도입니다.")]
    public Color outlineColor=new Color(.06f,.065f,.075f,.90f);
    [Range(0,8),Tooltip("게임 화면 기준 픽셀 두께. 0이면 윤곽만 숨깁니다. Edit/Play에서 조절 가능합니다.")]
    public int outlineWidthPixels=1;
    [Range(1,1.5f),Tooltip("원본 본체 RGB 배율. 1이면 원래 밝기, 선택 시안은 1.15입니다. 그림자·체력바·별도 발광에는 적용하지 않습니다.")]
    public float bodyBrightness=1.15f;
    [Range(.01f,1),Tooltip("윤곽 마스크에 포함할 본체 알파 기준입니다.")]
    public float alphaThreshold=.1f;
    public int ActiveTargetCount=>states.Count;

    sealed class Tint
    {
        public Color baseline,last;
        public bool applied;
        public void Apply(SpriteRenderer renderer,float brightness)
        {
            var current=renderer.color;
            if(!applied||current!=last)baseline=current;
            last=new Color(baseline.r*brightness,baseline.g*brightness,baseline.b*brightness,baseline.a);
            renderer.color=last;applied=true;
        }
        public void Restore(SpriteRenderer renderer)
        {
            // Another presentation system may have supplied a newer tint; never overwrite it.
            if(renderer!=null&&applied&&renderer.color==last)renderer.color=baseline;
            applied=false;
        }
    }
    sealed class Body
    {
        public GameObject host;
        public CoreGuardianSelectionOutline union;
        public Transform art;
        public readonly Dictionary<SpriteRenderer,Tint> tints=new Dictionary<SpriteRenderer,Tint>();
        public readonly List<SpriteRenderer> sources=new List<SpriteRenderer>();
        public readonly CoreGuardianSpriteReferences references=new CoreGuardianSpriteReferences();
    }
    readonly Dictionary<Transform,Body> states=new Dictionary<Transform,Body>();
    readonly HashSet<Transform> live=new HashSet<Transform>();
    readonly List<Transform> targets=new List<Transform>(),removed=new List<Transform>();
    CoreGuardianPrototype game;
    Transform core;
    Camera presentedCamera;

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering+=BeginCameraRendering;
        RenderPipelineManager.endCameraRendering+=EndCameraRendering;
    }
    void BeginCameraRendering(ScriptableRenderContext context,Camera camera)
    {
        if(!isActiveAndEnabled||camera!=presentedCamera)return;
        foreach(var state in states.Values)
            foreach(var renderer in state.sources)
                if(renderer!=null&&renderer.enabled&&renderer.gameObject.activeInHierarchy&&renderer.sprite!=null)
                {
                    if(!state.tints.TryGetValue(renderer,out var tint)){tint=new Tint();state.tints.Add(renderer,tint);}
                    tint.Apply(renderer,Mathf.Clamp(bodyBrightness,1,1.5f));
                }
    }
    void EndCameraRendering(ScriptableRenderContext context,Camera camera)
    {
        if(camera==presentedCamera)RestoreBodyTints();
    }
    void RestoreBodyTints(){foreach(var state in states.Values)foreach(var pair in state.tints)pair.Value.Restore(pair.Key);}

    void LateUpdate()
    {
        if(game==null)game=GetComponent<CoreGuardianPrototype>();
        if(game==null||!game.Ready)return;
        targets.Clear();game.GetCombatBodyTargets(targets);
        if(core==null)
            foreach(var r in FindObjectsByType<SpriteRenderer>())
                if(r.gameObject.scene==gameObject.scene&&r.name=="2D Sprite"&&r.sprite!=null&&r.sprite.name=="core"){core=r.transform.parent;break;}
        if(core!=null&&core.gameObject.activeInHierarchy)targets.Add(core);
        PresentTargets(targets,Camera.main);
    }
    public void PresentTargets(IEnumerable<Transform> bodyTargets,Camera camera)
    {
        // Keep animation inputs unmodified between renders, including whole-sprite -> cutout transitions.
        RestoreBodyTints();presentedCamera=camera;
        live.Clear();
        if(camera!=null&&bodyTargets!=null)
        foreach(var target in bodyTargets)
        {
            if(target==null||!target.gameObject.activeInHierarchy||!live.Add(target))continue;
            if(!states.TryGetValue(target,out var state))
            {
                Transform art=null;
                foreach(var r in target.GetComponentsInChildren<SpriteRenderer>(true))if(r.name=="2D Sprite"){art=r.transform;break;}
                if(art==null)continue;
                var host=new GameObject("ObjectEmphasis_B2_"+target.name){hideFlags=HideFlags.DontSave};host.transform.SetParent(transform,false);
                var union=host.AddComponent<CoreGuardianSelectionOutline>();union.enabled=false;
                state=new Body{host=host,union=union,art=art};states.Add(target,state);
            }
            if(state.art==null)continue;
            state.sources.Clear();
            foreach(var r in state.references.Read(state.art))
                if(r!=null&&r.gameObject.activeInHierarchy)state.sources.Add(r);
            foreach(var renderer in state.sources)
            {
                if(!renderer.enabled||renderer.sprite==null)continue;
                if(!state.tints.TryGetValue(renderer,out var tint)){tint=new Tint();state.tints.Add(renderer,tint);}
            }
            state.union.widthPixels=Mathf.Clamp(outlineWidthPixels,0,8);state.union.color=outlineColor;
            state.union.alphaThreshold=Mathf.Clamp(alphaThreshold,.01f,1);
            state.union.PresentAtOrder(target,camera,3);
        }
        removed.Clear();foreach(var pair in states)if(pair.Key==null||!live.Contains(pair.Key))removed.Add(pair.Key);
        foreach(var target in removed){Release(states[target]);states.Remove(target);}
    }
    static void Release(Body state)
    {
        foreach(var pair in state.tints)pair.Value.Restore(pair.Key);
        if(state.host==null)return;
        state.host.SetActive(false);
        if(Application.isPlaying)Destroy(state.host);else DestroyImmediate(state.host);
    }
    public void InvalidateSources()
    {
        foreach(var state in states.Values){state.references.Invalidate();state.union.InvalidateSources();}
    }
    void Clear(){foreach(var state in states.Values)Release(state);states.Clear();presentedCamera=null;}
    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering-=BeginCameraRendering;
        RenderPipelineManager.endCameraRendering-=EndCameraRendering;
        Clear();
    }
    void OnDestroy(){Clear();}
    void OnValidate(){outlineWidthPixels=Mathf.Clamp(outlineWidthPixels,0,8);bodyBrightness=Mathf.Clamp(bodyBrightness,1,1.5f);alphaThreshold=Mathf.Clamp(alphaThreshold,.01f,1);}
}
