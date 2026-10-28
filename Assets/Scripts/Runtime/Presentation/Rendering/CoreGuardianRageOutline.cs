using System.Collections.Generic;
using UnityEngine;

// 상태 효과·아트 목록 B7 (10-26 확정, 10-28 적용): 보스 격노 진입 순간 붉은 테두리 빛이 한 번 번쩍.
// The common selection-outline union in red on a dedicated disabled instance, like the upgrade scan.
[DefaultExecutionOrder(600)]
[DisallowMultipleComponent]
public sealed class CoreGuardianRageOutline : MonoBehaviour
{
    public static readonly Color RageColor = new Color(1f, .16f, .1f, 1f);
    public const float Duration = .5f;
    const int WidthPixels = 2;

    sealed class Flash
    {
        public Transform target;
        public GameObject root;
        public CoreGuardianSelectionOutline union;
        public float age;
        public int startedFrame;
    }

    readonly List<Flash> flashes = new List<Flash>();

    // F17: finished flashes keep their outline object for the next one (play mode only).
    readonly Stack<Flash> pool = new Stack<Flash>();
    bool clearing;
    public int ActiveCount => flashes.Count;

    /// <summary>Stops a flash on a body that is being returned to the pool (same-frame reuse must not inherit it).</summary>
    public void ReleaseTarget(Transform target)
    {
        for (int i = flashes.Count - 1; i >= 0; i--)
        {
            if (flashes[i].target == target)
            {
                Remove(i);
            }
        }
    }

    public void Trigger(Transform target)
    {
        if (target == null || !isActiveAndEnabled)
            return;

        Flash flash = null;
        foreach (var f in flashes) // loop instead of a lambda search (F17)
        {
            if (f.target == target)
            {
                flash = f;
                break;
            }
        }
        while (flash == null && pool.Count > 0)
        {
            var pooled = pool.Pop();
            if (pooled.root != null && pooled.union != null)
            {
                pooled.root.SetActive(true);
                pooled.target = target;
                flash = pooled;
                flashes.Add(flash);
            }
        }
        if (flash == null)
        {
            var root = new GameObject("BossRageOutline_B7") { hideFlags = HideFlags.DontSave };
            root.transform.SetParent(transform, false);
            var union = root.AddComponent<CoreGuardianSelectionOutline>();
            union.enabled = false;
            union.widthPixels = WidthPixels;
            union.color = RageColor;
            flash = new Flash
            {
                target = target,
                root = root,
                union = union,
            };
            flashes.Add(flash);
        }
        flash.age = 0;
        flash.startedFrame = Time.frameCount;
    }

    void LateUpdate()
    {
        for (int i = flashes.Count - 1; i >= 0; i--)
        {
            var flash = flashes[i];
            if (flash.startedFrame != Time.frameCount)
            {
                flash.age += Time.deltaTime;
            }

            if (flash.target == null || !flash.target.gameObject.activeInHierarchy || flash.age >= Duration)
            {
                Remove(i);
                continue;
            }
            // Full red for the first 40%, then fades out.
            float t = flash.age / Duration;
            var c = RageColor;
            c.a = t < .4f ? 1 : Mathf.Clamp01((1 - t) / .6f);
            flash.union.color = c;
            flash.union.Present(flash.target, Camera.main);
        }
    }

    void Remove(int index)
    {
        var flash = flashes[index];
        flashes.RemoveAt(index);
        if (flash.union != null)
        {
            flash.union.Present(null, Camera.main);
        }

        if (Application.isPlaying && !clearing && flash.root != null && flash.union != null && pool.Count < 8)
        {
            flash.target = null;
            flash.union.color = RageColor;
            flash.root.SetActive(false);
            pool.Push(flash);
            return;
        }

        if (flash.root != null)
        {
            if (Application.isPlaying)
            {
                Destroy(flash.root);
            }
            else
            {
                DestroyImmediate(flash.root);
            }
        }
    }

    public void Clear()
    {
        clearing = true;
        try
        {
            for (int i = flashes.Count - 1; i >= 0; i--)
            {
                Remove(i);
            }

            while (pool.Count > 0)
            {
                var flash = pool.Pop();
                if (flash.root != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(flash.root);
                    }
                    else
                    {
                        DestroyImmediate(flash.root);
                    }
                }
            }
        }
        finally
        {
            clearing = false;
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
}
