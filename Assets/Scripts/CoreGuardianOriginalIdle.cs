using UnityEngine;

// Small breathing motion of the existing upper-body pixels. Feet and shadow roots stay still.
public sealed class CoreGuardianOriginalIdle
{
    readonly SpriteRenderer original,feet,upper;
    readonly Transform rig;
    readonly float ppu;
    public CoreGuardianOriginalIdle(SpriteRenderer artwork)
    {
        original=artwork;ppu=original.sprite.pixelsPerUnit;
        rig=new GameObject("OriginalIdle").transform;rig.SetParent(artwork.transform,false);
        feet=Layer("Feet");upper=Layer("Upper");rig.gameObject.SetActive(false);
    }
    SpriteRenderer Layer(string name)
    {
        var sprite=Resources.Load<Sprite>("CoreGuardian/OriginalIdle/Player/"+name);
        if(sprite==null)throw new System.InvalidOperationException("Missing player Idle layer: "+name);
        var renderer=new GameObject(name).AddComponent<SpriteRenderer>();renderer.transform.SetParent(rig,false);renderer.sprite=sprite;renderer.sharedMaterial=original.sharedMaterial;return renderer;
    }
    public void Present(bool active,float time)
    {
        rig.gameObject.SetActive(active);if(!active)return;
        original.enabled=false;
        feet.color=upper.color=original.color;feet.sortingLayerID=upper.sortingLayerID=original.sortingLayerID;
        feet.sortingOrder=original.sortingOrder;upper.sortingOrder=original.sortingOrder+1;
        float breath=(1-Mathf.Cos(time*Mathf.PI*2/2.8f))*.5f;
        upper.transform.localPosition=Vector3.up*(.55f*breath/ppu);
    }
}
