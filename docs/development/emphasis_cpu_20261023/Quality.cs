using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using Unity.Profiling;
using UnityEngine.Rendering;

public static class CGEmphasisQuality
{
    const string Root="docs/development/emphasis_cpu_20261023/";
    public static object Run(string variant)
    {
        if(variant!="baseline"&&variant!="candidate")throw new Exception("Unknown variant");
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||UnityEngine.Profiling.Profiler.enabled||UnityEditorInternal.ProfilerDriver.deepProfiling)
            throw new Exception("Stopped idle Editor required");
        var scene=EditorSceneManager.NewPreviewScene();var previous=RenderTexture.active;
        var cameraRoot=new GameObject("Emphasis QA camera");SceneManager.MoveGameObjectToScene(cameraRoot,scene);
        var camera=cameraRoot.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.orthographic=true;camera.orthographicSize=2;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.17f,.19f,.21f,1);camera.cullingMask=1<<31;
        var rt=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);rt.Create();camera.targetTexture=rt;
        var read=new Texture2D(256,256,TextureFormat.RGBA32,false);var rows=new List<object>();int failures=0;
        try
        {
            foreach(var direction in new[]{Vector2.left,Vector2.right})
            foreach(float phase in new[]{.025f,.08f,.14f,.22f})
            foreach(bool overlap in new[]{false,true})
            {
                var roots=new List<GameObject>();
                try
                {
                    for(int i=0;i<(overlap?2:1);i++)
                    {
                        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Units/enemy_attack.prefab"));roots.Add(root);
                        SceneManager.MoveGameObjectToScene(root,scene);root.transform.position=new Vector3(i*.12f,i*.025f,0);
                        var animation=CoreGuardianUnitAnimation.Attach(root,"Attack");animation.Move(direction,phase+i*.013f);animation.Present(.2f);
                        foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                        if(i==1)foreach(var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))renderer.color=new Color(.8f,1,.9f,1);
                    }
                    var art=roots[0].GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite");
                    camera.transform.position=new Vector3(art.bounds.center.x,art.bounds.center.y,-10);
                    var before=Capture(typeof(CGEmphasisBaseline));var bytes=Capture(typeof(CoreGuardianObjectEmphasis));int differences=0;
                    for(int p=0;p<bytes.Length;p+=4)if(before[p]!=bytes[p]||before[p+1]!=bytes[p+1]||before[p+2]!=bytes[p+2]||before[p+3]!=bytes[p+3])differences++;
                    string id=direction.x+"_"+phase.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"_"+overlap;
                    File.WriteAllBytes(Root+"same_actor_"+id+"_before.rgba",before);File.WriteAllBytes(Root+"same_actor_"+id+"_after.rgba",bytes);
                    byte[] Capture(Type ownerType)
                    {
                        var host=new GameObject("Emphasis QA");SceneManager.MoveGameObjectToScene(host,scene);
                        var emphasis=host.AddComponent(ownerType);
                        try
                        {
                            ownerType.GetMethod("PresentTargets").Invoke(emphasis,new object[]{roots.Select(r=>r.transform),camera});
                            foreach(var t in host.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                            camera.Render();RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,256,256),0,0);read.Apply();
                            var pixels=read.GetPixels32();if(pixels.Count(c=>!c.Equals(pixels[0]))<20)throw new Exception("Empty render");
                            return pixels.SelectMany(c=>new[]{c.r,c.g,c.b,c.a}).ToArray();
                        }
                        finally
                        {
                            ownerType.GetMethod("OnDestroy",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(emphasis,null);
                            UnityEngine.Object.DestroyImmediate(host);
                        }
                    }
                    if(differences>0)failures++;rows.Add(new{direction=direction.x,phase,overlap,differences});
                }
                finally
                {
                    foreach(var root in roots)UnityEngine.Object.DestroyImmediate(root);
                }
                if(failures>0)goto Completed;
            }
            Completed: ;
        }
        finally
        {
            camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(read);rt.Release();UnityEngine.Object.DestroyImmediate(rt);EditorSceneManager.ClosePreviewScene(scene);
        }
        File.WriteAllText(Root+"same_actor_quality.json",JsonConvert.SerializeObject(new{pairs=rows.Count,failures,width=256,height=256,ortho=2,rows},Formatting.Indented));
        return new{pairs=rows.Count,failures};
    }
}

// Selected B2: original body tint plus a separate union silhouette, below inspection and upgrades.
[DisallowMultipleComponent]
[DefaultExecutionOrder(450)]
public sealed class CGEmphasisBaseline : MonoBehaviour
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
        public readonly CGOriginalSpriteReferences references=new CGOriginalSpriteReferences();
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
        CoreGuardianAttackBodyBatch.Flush(camera);
    }
    void EndCameraRendering(ScriptableRenderContext context,Camera camera)
    {
        if(camera==presentedCamera){CoreGuardianAttackBodyBatch.Restore();RestoreBodyTints();}
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
        if(state.union!=null)typeof(CoreGuardianSelectionOutline).GetMethod("ReleasePresentation",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(state.union,null);
        // Non-ExecuteAlways EditMode consumers may never receive OnDestroy.
        if(!Application.isPlaying&&state.union!=null)typeof(CoreGuardianSelectionOutline).GetMethod("ReleaseResources",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(state.union,null);
        state.host.SetActive(false);
        if(Application.isPlaying)Destroy(state.host);else DestroyImmediate(state.host);
    }
    public void InvalidateSources()
    {
        foreach(var state in states.Values){state.references.Invalidate();state.union.InvalidateSources();}
    }
    public void ReleaseTarget(Transform target)
    {
        if(target==null||!states.TryGetValue(target,out var state))return;
        Release(state);states.Remove(target);live.Remove(target);
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

// Per-consumer references only; this does not share masks or alter presentation order.
// Hierarchy checks do not use Transform.hasChanged: walking moves transforms every frame.
internal sealed class CGOriginalSpriteReferences
{
    static readonly ProfilerMarker readSample=new ProfilerMarker("CG.SpriteReferences.Read");
    Transform root;
    bool dirty=true;
    readonly List<SpriteRenderer> renderers=new List<SpriteRenderer>();
    readonly List<Transform> nodes=new List<Transform>();
    readonly List<int> childCounts=new List<int>();
    readonly List<Transform> parents=new List<Transform>();
    public void Invalidate()=>dirty=true;
    public List<SpriteRenderer> Read(Transform target)
    {
        using var readScope=readSample.Auto();
        bool refresh=dirty||root!=target;
        if(!refresh)
        {
            for(int i=0;i<nodes.Count;i++)
                if(nodes[i]==null||nodes[i].childCount!=childCounts[i]||nodes[i].parent!=parents[i]){refresh=true;break;}
            if(!refresh)for(int i=0;i<renderers.Count;i++)if(renderers[i]==null){refresh=true;break;}
        }
        if(refresh)
        {
            root=target;dirty=false;renderers.Clear();nodes.Clear();childCounts.Clear();parents.Clear();
            if(root!=null)
            {
                root.GetComponentsInChildren(true,renderers);root.GetComponentsInChildren(true,nodes);
                foreach(var node in nodes){childCounts.Add(node.childCount);parents.Add(node.parent);}
            }
        }
        return renderers;
    }
}
