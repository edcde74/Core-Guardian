using UnityEngine;

// Whole-frame walk for art drawn as finished frames (비행체 hover, 철벽 기사·진홍의 군주 4-direction walks).
// Side frames face left like the original enemies and are mirrored for rightward travel unless a
// dedicated Right set exists. Presentation only.
public sealed class CoreGuardianFrameWalk
{
    readonly SpriteRenderer art;
    readonly Sprite[] left,right,up,down;
    readonly Sprite rest;
    readonly float cyclePixels,ppu,hoverFps;
    readonly Vector3 initialPosition;
    readonly float topPixels;
    float phase,clock;bool walking,mirror;int vertical;
    public CoreGuardianFrameWalk(SpriteRenderer artwork,Sprite[] left,Sprite[] right,Sprite[] up,Sprite[] down,float cyclePixels,float hoverFps)
    {
        art=artwork;this.left=left;this.right=right!=null&&right.Length>0?right:null;this.up=up;this.down=down;rest=artwork.sprite;
        this.cyclePixels=Mathf.Max(1,cyclePixels);this.hoverFps=hoverFps;ppu=artwork.sprite!=null?artwork.sprite.pixelsPerUnit:32;
        initialPosition=artwork.transform.localPosition;
        float top=0;
        foreach(var set in new[]{left,right,up,down})if(set!=null)foreach(var s in set)if(s!=null)top=Mathf.Max(top,s.rect.height-s.pivot.y);
        if(rest!=null)top=Mathf.Max(top,rest.rect.height-rest.pivot.y);
        topPixels=top;
    }
    public Vector3 HealthBarWorld=>art.transform.parent.TransformPoint(initialPosition+Vector3.up*((topPixels+4)*art.transform.localScale.y/ppu));
    public void Move(Vector2 worldVelocity,float delta)
    {
        walking=worldVelocity.sqrMagnitude>.000001f;clock+=Mathf.Max(0,delta);
        if(!walking)return;
        Vector2 local=art.transform.InverseTransformVector(worldVelocity);
        vertical=Mathf.Abs(local.y)>Mathf.Abs(local.x)?(local.y>0?1:-1):0;
        if(vertical==0){if(local.x>.0001f)mirror=true;else if(local.x<-.0001f)mirror=false;}
        phase=Mathf.Repeat(phase+local.magnitude*Mathf.Max(0,delta)*ppu/cyclePixels,1);
    }
    static Sprite Pick(Sprite[] frames,float t){if(frames==null||frames.Length==0)return null;return frames[Mathf.Min(frames.Length-1,(int)(t*frames.Length))];}
    public void Present(float delta)
    {
        Sprite frame=null;bool flip=false;
        if(vertical>0)frame=Pick(up,phase);
        else if(vertical<0)frame=Pick(down,phase);
        if(frame==null)
        {
            float t=hoverFps>0?Mathf.Repeat(clock*hoverFps/Mathf.Max(1,left?.Length??1),1):phase;
            if(mirror&&right!=null)frame=Pick(right,t);else{frame=Pick(left,t);flip=mirror;}
        }
        if(frame==null)frame=rest;
        if(art.sprite!=frame)art.sprite=frame;
        if(art.flipX!=flip)art.flipX=flip;
    }
    public void ResetForReuse(){phase=clock=0;walking=mirror=false;vertical=0;art.sprite=rest;art.flipX=false;art.transform.localPosition=initialPosition;}
}
