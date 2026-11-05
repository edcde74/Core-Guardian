using System.Collections.Generic;
using UnityEngine;

// Selected B: one brief cyan scan on the actual successfully upgraded unit, independent of selection.
[DefaultExecutionOrder(600)]
[DisallowMultipleComponent]
public sealed class CoreGuardianUpgradeFeedback : MonoBehaviour
{
    [Header("강화 성공 B · 청록빛 스캔")]
    [Min(.05f), Tooltip("스캔 전체 시간(초). 이동과 시작/끝 페이드가 함께 늘어납니다. Play 중에도 조절할 수 있습니다.")]
    public float duration = .8f;
    public float Duration => Mathf.Max(.05f, duration);
    static readonly int BandYId = Shader.PropertyToID("_BandY"),
        OpacityId = Shader.PropertyToID("_Opacity");

    sealed class Scan
    {
        public Transform target;
        public GameObject root;
        public CoreGuardianSelectionOutline union;
        public Material material;
        public float age;
        public int startedFrame;
    }

    readonly List<Scan> scans = new List<Scan>();
    public int TriggerCount { get; private set; }
    public int ActiveCount => scans.Count;

    public bool IsScanning(Transform target) => FindScan(target) != null;

    // Loop instead of lambda searches (F17).
    Scan FindScan(Transform target)
    {
        foreach (var s in scans)
        {
            if (s.target == target)
                return s;
        }
        return null;
    }

    // F17: finished scans keep their outline object and material for the next one (play mode only).
    readonly Stack<Scan> pool = new Stack<Scan>();
    bool clearing;

    public void Trigger(Transform target)
    {
        if (target == null || !isActiveAndEnabled)
            return;

        var scan = FindScan(target);
        while (scan == null && pool.Count > 0)
        {
            var pooled = pool.Pop();
            if (pooled.root != null && pooled.union != null && pooled.material != null)
            {
                pooled.root.SetActive(true);
                pooled.target = target;
                scan = pooled;
                scans.Add(scan);
            }
            else
            {
                DisposeObject(pooled.root);
                DisposeObject(pooled.material);
            }
        }
        if (scan == null)
        {
            var root = new GameObject("UpgradeSuccessScan_B") { hideFlags = HideFlags.DontSave };
            root.transform.SetParent(transform, false);
            var union = root.AddComponent<CoreGuardianSelectionOutline>();
            union.enabled = false;
            scan = new Scan
            {
                target = target,
                root = root,
                union = union,
                material = new Material(Resources.Load<Shader>("CoreGuardian/UpgradeScan"))
                {
                    hideFlags = HideFlags.DontSave,
                },
            };
            scans.Add(scan);
        }
        scan.age = 0;
        scan.startedFrame = Time.frameCount;
        TriggerCount++;
    }

    void LateUpdate()
    {
        for (int i = scans.Count - 1; i >= 0; i--)
        {
            var scan = scans[i];
            float total = Duration;
            if (scan.startedFrame != Time.frameCount)
            {
                scan.age += Time.deltaTime;
            }

            if (scan.target == null || scan.age >= total)
            {
                Remove(i);
                continue;
            }
            if (!scan.union.PresentOverlay(scan.target, Camera.main, scan.material))
                continue;

            var bounds = scan.union.TargetBounds;
            // Keep the approved B timing proportions while scaling the entire visible sweep.
            float phase = scan.age / total * .55f;
            scan.material.SetFloat(
                BandYId,
                Mathf.Lerp(bounds.min.y - .06f, bounds.max.y + .06f, Mathf.Clamp01(phase / .48f))
            );
            scan.material.SetFloat(OpacityId, Mathf.Clamp01(phase / .06f) * Mathf.Clamp01((.55f - phase) / .08f));
        }
    }

    void Remove(int index)
    {
        var scan = scans[index];
        scans.RemoveAt(index);
        if (scan.union != null)
        {
            scan.union.Present(null, Camera.main);
        }

        if (
            Application.isPlaying
            && !clearing
            && scan.root != null
            && scan.union != null
            && scan.material != null
            && pool.Count < 16
        )
        {
            scan.target = null;
            scan.root.SetActive(false);
            pool.Push(scan);
            return;
        }

        DisposeObject(scan.root);
        DisposeObject(scan.material);
    }

    public void Clear()
    {
        clearing = true;
        try
        {
            for (int i = scans.Count - 1; i >= 0; i--)
            {
                Remove(i);
            }

            while (pool.Count > 0)
            {
                var scan = pool.Pop();
                DisposeObject(scan.root);
                DisposeObject(scan.material);
            }
        }
        finally
        {
            clearing = false;
        }
    }

    public void ReleaseTarget(Transform target)
    {
        for (int i = scans.Count - 1; i >= 0; i--)
        {
            if (scans[i].target == target)
            {
                Remove(i);
            }
        }
    }

    void OnDisable()
    {
        Clear();
    }

    void OnDestroy()
    {
        Clear();
    }

    void OnValidate()
    {
        duration = Mathf.Max(.05f, duration);
    }

    static void DisposeObject(Object value)
    {
        if (value == null)
            return;

        if (Application.isPlaying)
        {
            Destroy(value);
        }
        else
        {
            DestroyImmediate(value);
        }
    }
}
