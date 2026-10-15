using UnityEngine;

// Cutout animation of the approved PNG. No replacement drawing, recolouring or scale changes.
public sealed class CoreGuardianOriginalMotion
{
    readonly SpriteRenderer original, fixedPart, movingPart;
    readonly Transform rig;
    readonly string role;
    readonly float ppu;
    readonly Vector2 sourcePivot;
    public CoreGuardianOriginalMotion(SpriteRenderer artwork,string role)
    {
        original=artwork;this.role=role;ppu=artwork.sprite.pixelsPerUnit;sourcePivot=artwork.sprite.pivot;
        var stationary=Resources.Load<Sprite>("CoreGuardian/OriginalMotion/"+role+"/Fixed");
        var moving=Resources.Load<Sprite>("CoreGuardian/OriginalMotion/"+role+"/Moving");
        if(stationary==null||moving==null)throw new System.InvalidOperationException("Missing original cutout layers: "+role);
        rig=new GameObject("OriginalMotion").transform;rig.SetParent(artwork.transform,false);
        fixedPart=Layer("Fixed",stationary);movingPart=Layer("Moving",moving);
        rig.gameObject.SetActive(false);
    }
    SpriteRenderer Layer(string name,Sprite sprite)
    {
        var renderer=new GameObject(name).AddComponent<SpriteRenderer>();renderer.transform.SetParent(rig,false);
        renderer.sprite=sprite;renderer.sharedMaterial=original.sharedMaterial;
        return renderer;
    }
    public void Present(float age,float duration)
    {
        bool active=age<duration;
        rig.gameObject.SetActive(active);original.enabled=!active;
        if(!active)return;
        fixedPart.color=movingPart.color=original.color;
        fixedPart.sortingLayerID=movingPart.sortingLayerID=original.sortingLayerID;
        fixedPart.sortingOrder=original.sortingOrder;movingPart.sortingOrder=original.sortingOrder+1;
        // The actual launch begins the movement; recovery always ends at the untouched source sprite.
        float t=Mathf.Clamp01(age/duration);
        float seconds=t*.48f;
        float kick=Mathf.Sin(Mathf.Min(1,seconds/.09f)*Mathf.PI*.5f)*Mathf.Exp(-Mathf.Max(0,seconds-.09f)*11);
        var part=movingPart.transform;part.localRotation=Quaternion.identity;part.localPosition=Vector3.zero;
        if(role=="Player")
        {
            var pivot=(new Vector2(10,original.sprite.rect.height-23)-sourcePivot)/ppu;
            var rotation=Quaternion.Euler(0,0,-.24f*Mathf.Rad2Deg*kick);
            part.localRotation=rotation;part.localPosition=(Vector3)pivot-rotation*(Vector3)pivot;
        }
        else if(role=="Single")part.localPosition=new Vector3(1.4f,-1,0)*(kick/ppu);
        else if(role=="Multi")part.localPosition=Vector3.down*(1.4f*kick/ppu);
        else part.localPosition=Vector3.up*(1.4f*Mathf.Sin(t*Mathf.PI)/ppu);
    }
    public Vector3 MuzzleWorld(Vector2 aim)
    {
        Vector2 point;
        if(role=="Player")point=new Vector2(2,original.sprite.rect.height-29);
        else if(role=="Single")point=new Vector2(3,original.sprite.rect.height-4);
        else if(role=="Multi")point=aim.y>Mathf.Abs(aim.x)?new Vector2(21,original.sprite.rect.height-3):aim.x>0?new Vector2(47,original.sprite.rect.height-13):new Vector2(4,original.sprite.rect.height-12);
        else point=new Vector2(20,original.sprite.rect.height-16);
        return movingPart.transform.TransformPoint((point-sourcePivot)/ppu);
    }
}
