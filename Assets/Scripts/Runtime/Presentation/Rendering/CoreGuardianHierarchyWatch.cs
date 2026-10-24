using UnityEngine;

// Structure-change stamp for CoreGuardianSpriteReferences. Unity sends these messages even to inactive objects and
// disabled scripts, so a child added, removed, destroyed or reparented anywhere below a watched root bumps the root's
// Version. Readers then skip the per-node childCount/parent walk while nothing changed. Presentation only.
[ExecuteAlways, DisallowMultipleComponent, AddComponentMenu("")]
public sealed class CoreGuardianHierarchyWatch : MonoBehaviour
{
    public int Version {get;private set;}
    void OnTransformChildrenChanged()=>Bump(transform);
    void OnTransformParentChanged()=>Bump(transform);
    static void Bump(Transform from)
    {
        for(var t=from;t!=null;t=t.parent)if(t.TryGetComponent(out CoreGuardianHierarchyWatch watch))watch.Version++;
    }
}
