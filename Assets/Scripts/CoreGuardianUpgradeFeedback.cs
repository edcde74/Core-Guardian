using System.Collections.Generic;
using UnityEngine;

// Selected B: one brief cyan scan on the actual successfully upgraded unit, independent of selection.
[DefaultExecutionOrder(600)]
public sealed class CoreGuardianUpgradeFeedback : MonoBehaviour
{
    public const float Duration=.55f;
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
            if(scan.startedFrame!=Time.frameCount)scan.age+=Time.deltaTime;
            if(scan.target==null||scan.age>=Duration){Remove(i);continue;}
            if(!scan.union.PresentOverlay(scan.target,Camera.main,scan.material))continue;
            var bounds=scan.union.TargetBounds;
            scan.material.SetFloat("_BandY",Mathf.Lerp(bounds.min.y-.06f,bounds.max.y+.06f,Mathf.Clamp01(scan.age/.48f)));
            scan.material.SetFloat("_Opacity",Mathf.Clamp01(scan.age/.06f)*Mathf.Clamp01((Duration-scan.age)/.08f));
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
    static void DisposeObject(Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
}
