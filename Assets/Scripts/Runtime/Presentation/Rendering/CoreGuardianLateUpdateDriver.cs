using System.Collections.Generic;
using UnityEngine;

// F24: one LateUpdate for every tower base shade and photo shadow instead of one per body (~185 bodies → 1 call).
// Order 100 = the former CoreGuardianUnitAnimation slot; no other LateUpdate sits between it and the shadows' 200,
// so running the tower shades and then the shadows here keeps the same frame order.
[DefaultExecutionOrder(100)]
public sealed class CoreGuardianLateUpdateDriver : MonoBehaviour
{
    static CoreGuardianLateUpdateDriver instance;
    static readonly List<CoreGuardianUnitAnimation> towers = new List<CoreGuardianUnitAnimation>();
    static readonly List<CoreGuardianPhotoShadow> shadows = new List<CoreGuardianPhotoShadow>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetLists()
    {
        towers.Clear();
        shadows.Clear();
        instance = null;
    }

    static void EnsureDriver()
    {
        if (instance != null || !Application.isPlaying)
            return;

        var host = new GameObject("CoreGuardianLateUpdateDriver") { hideFlags = HideFlags.HideAndDontSave };
        DontDestroyOnLoad(host);
        instance = host.AddComponent<CoreGuardianLateUpdateDriver>();
    }

    internal static void Add(CoreGuardianUnitAnimation tower)
    {
        if (!towers.Contains(tower))
        {
            towers.Add(tower);
        }
        EnsureDriver();
    }

    internal static void Remove(CoreGuardianUnitAnimation tower) => towers.Remove(tower);

    internal static void Add(CoreGuardianPhotoShadow shadow)
    {
        if (!shadows.Contains(shadow))
        {
            shadows.Add(shadow);
        }
        EnsureDriver();
    }

    internal static void Remove(CoreGuardianPhotoShadow shadow) => shadows.Remove(shadow);

    void LateUpdate()
    {
        for (int i = 0; i < towers.Count; i++)
        {
            if (towers[i] != null)
            {
                towers[i].PresentGrounding();
            }
        }

        for (int i = 0; i < shadows.Count; i++)
        {
            if (shadows[i] != null)
            {
                shadows[i].PresentLate();
            }
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
