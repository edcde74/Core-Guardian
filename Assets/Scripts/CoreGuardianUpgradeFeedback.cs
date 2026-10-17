using System.Collections.Generic;
using UnityEngine;

// Selected B: one brief cyan scan on the actual successfully upgraded unit, independent of selection.
[DefaultExecutionOrder(600)]
[DisallowMultipleComponent]
public sealed class CoreGuardianUpgradeFeedback : MonoBehaviour
{
    [Header("강화 성공 B · 청록빛 스캔")]
    [Min(.05f),Tooltip("스캔 전체 시간(초). 이동과 시작/끝 페이드가 함께 늘어납니다. Play 중에도 조절할 수 있습니다.")]
    public float duration=.8f;
    public float Duration=>Mathf.Max(.05f,duration);
    sealed class Scan
    {
        public Transform target;
        public GameObject root;
        public CoreGuardianSelectionOutline union;
        public Material material;
        public float age;
        public int startedFrame;
    }
    readonly List<Scan> scans=new List<Scan>();
    public int TriggerCount {get;private set;}
    public int ActiveCount=>scans.Count;
    public bool IsScanning(Transform target)=>scans.Exists(s=>s.target==target);

    public void Trigger(Transform target)
    {
        if(target==null||!isActiveAndEnabled)return;
        var scan=scans.Find(s=>s.target==target);
        if(scan==null)
        {
            var root=new GameObject("UpgradeSuccessScan_B"){hideFlags=HideFlags.DontSave};root.transform.SetParent(transform,false);
            var union=root.AddComponent<CoreGuardianSelectionOutline>();union.enabled=false;
            scan=new Scan{target=target,root=root,union=union,material=new Material(Resources.Load<Shader>("CoreGuardian/UpgradeScan")){hideFlags=HideFlags.DontSave}};
            scans.Add(scan);
        }
        scan.age=0;scan.startedFrame=Time.frameCount;TriggerCount++;
    }
    void LateUpdate()
    {
        for(int i=scans.Count-1;i>=0;i--)
        {
            var scan=scans[i];
            float total=Duration;
            if(scan.startedFrame!=Time.frameCount)scan.age+=Time.deltaTime;
            if(scan.target==null||scan.age>=total){Remove(i);continue;}
            if(!scan.union.PresentOverlay(scan.target,Camera.main,scan.material))continue;
            var bounds=scan.union.TargetBounds;
            // Keep the approved B timing proportions while scaling the entire visible sweep.
            float phase=scan.age/total*.55f;
            scan.material.SetFloat("_BandY",Mathf.Lerp(bounds.min.y-.06f,bounds.max.y+.06f,Mathf.Clamp01(phase/.48f)));
            scan.material.SetFloat("_Opacity",Mathf.Clamp01(phase/.06f)*Mathf.Clamp01((.55f-phase)/.08f));
        }
    }
    void Remove(int index)
    {
        var scan=scans[index];scans.RemoveAt(index);
        if(scan.union!=null)scan.union.Present(null,Camera.main);
        DisposeObject(scan.root);DisposeObject(scan.material);
    }
    public void Clear(){for(int i=scans.Count-1;i>=0;i--)Remove(i);}
    void OnDisable(){Clear();}
    void OnDestroy(){Clear();}
    void OnValidate(){duration=Mathf.Max(.05f,duration);}
    static void DisposeObject(Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
}
