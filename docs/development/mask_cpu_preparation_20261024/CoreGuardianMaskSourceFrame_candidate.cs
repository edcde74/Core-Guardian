using System.Collections.Generic;
using UnityEngine;

// Values belong to one PresentMask call. Records and queued commands copy their own data.
internal readonly struct CoreGuardianMaskSourceSample
{
    internal readonly SpriteRenderer Renderer;
    internal readonly Sprite Sprite;
    internal readonly Texture Source;
    internal readonly uint SourceVersion;
    internal readonly Color Color;
    internal readonly bool FlipX,FlipY;
    internal readonly Matrix4x4 LocalToWorld;
    internal readonly Bounds Bounds;
    internal readonly int SortingLayerID,SortingOrder;
    internal CoreGuardianMaskSourceSample(SpriteRenderer renderer)
    {
        Renderer=renderer;Sprite=renderer.sprite;Source=Sprite.texture;SourceVersion=Source.updateCount;
        Color=renderer.color;FlipX=renderer.flipX;FlipY=renderer.flipY;LocalToWorld=renderer.transform.localToWorldMatrix;
        Bounds=renderer.bounds;SortingLayerID=renderer.sortingLayerID;SortingOrder=renderer.sortingOrder;
    }
}

internal sealed class CoreGuardianMaskSourceFrame
{
    internal readonly List<CoreGuardianMaskSourceSample> Values=new List<CoreGuardianMaskSourceSample>();
    internal IReadOnlyList<CoreGuardianMaskSourceSample> Samples=>Values;
    internal void Capture(List<SpriteRenderer> filteredSources)
    {
        Values.Clear();foreach(var renderer in filteredSources)Values.Add(new CoreGuardianMaskSourceSample(renderer));
    }
    internal void Clear()=>Values.Clear();
}
