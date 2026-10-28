using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

// Immutable shader state only. Each owner keeps its changing texture and values in a property block.
internal sealed class CoreGuardianSharedMaskMaterial
{
    static readonly Dictionary<string, CoreGuardianSharedMaskMaterial> entries =
        new Dictionary<string, CoreGuardianSharedMaskMaterial>();
    readonly string path;
    int users;
    public Material Material { get; private set; }

    CoreGuardianSharedMaskMaterial(string path)
    {
        this.path = path;
        Material = new Material(Resources.Load<Shader>(path))
        {
            name = "Shared " + path,
            hideFlags = HideFlags.DontSave,
        };
    }

    public static CoreGuardianSharedMaskMaterial Acquire(string path)
    {
        if (!entries.TryGetValue(path, out var entry))
        {
            entry = new CoreGuardianSharedMaskMaterial(path);
            entries.Add(path, entry);
        }
        entry.users++;
        return entry;
    }

    public void Release()
    {
        if (users <= 0)
            return;

        if (--users != 0)
            return;

        entries.Remove(path);
        if (Material != null)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(Material);
            }
            else
            {
                Object.DestroyImmediate(Material);
            }
        }
        Material = null;
    }
}
