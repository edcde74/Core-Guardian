using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

// Per-consumer references only; this does not share masks or alter presentation order.
// Hierarchy checks do not use Transform.hasChanged: walking moves transforms every frame.
internal sealed class CoreGuardianSpriteReferences
{
    static readonly ProfilerMarker readSample = new ProfilerMarker("CG.SpriteReferences.Read");
    Transform root;
    bool dirty = true;
    public int Revision { get; private set; }
    readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();

    // GameObject of each renderer (same index), captured at refresh so per-frame visibility checks skip the getter.
    public readonly List<GameObject> GameObjects = new List<GameObject>();
    readonly List<Transform> nodes = new List<Transform>();
    readonly List<int> childCounts = new List<int>();
    readonly List<Transform> parents = new List<Transform>();

    // Root structure stamp (children added/removed/reparented below it, root reparent). The full node walk runs
    // only when the stamp moved, the watcher is missing, or the root is not a scene object.
    CoreGuardianHierarchyWatch watch;
    int watchVersion;

    public void Invalidate() => dirty = true;

    public List<SpriteRenderer> Read(Transform target)
    {
        using var readScope = readSample.Auto();
        bool refresh = dirty || root != target;
        if (!refresh)
        {
            if (watch == null || watch.Version != watchVersion)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i] == null || nodes[i].childCount != childCounts[i] || nodes[i].parent != parents[i])
                    {
                        refresh = true;
                        break;
                    }
                }

                if (!refresh && watch != null)
                {
                    watchVersion = watch.Version; // e.g. root reparent with unchanged contents
                }
            }
            if (!refresh)
            {
                for (int i = 0; i < renderers.Count; i++)
                {
                    if (renderers[i] == null)
                    {
                        refresh = true;
                        break;
                    }
                }
            }
        }
        if (refresh)
        {
            Revision++;
            root = target;
            dirty = false;
            renderers.Clear();
            GameObjects.Clear();
            nodes.Clear();
            childCounts.Clear();
            parents.Clear();
            watch = null;
            if (root != null)
            {
                root.GetComponentsInChildren(true, renderers);
                foreach (var r in renderers)
                {
                    GameObjects.Add(r.gameObject);
                }

                root.GetComponentsInChildren(true, nodes);
                foreach (var node in nodes)
                {
                    childCounts.Add(node.childCount);
                    parents.Add(node.parent);
                }
                // Never add components to prefab assets or other non-scene objects.
                if (root.gameObject.scene.IsValid())
                {
                    foreach (var node in nodes)
                    {
                        if (!node.TryGetComponent(out CoreGuardianHierarchyWatch _))
                        {
                            node.gameObject.AddComponent<CoreGuardianHierarchyWatch>().hideFlags =
                                HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.HideInInspector;
                        }
                    }

                    root.TryGetComponent(out watch);
                    watchVersion = watch != null ? watch.Version : 0;
                }
            }
        }
        return renderers;
    }
}
