using UnityEngine;

// A single synchronous operation owns these values. Public entry points capture again.
internal readonly struct CoreGuardianMaskCameraState
{
    internal readonly Camera camera;
    internal readonly Matrix4x4 view,projection;
    internal readonly Rect viewport;
    internal readonly RenderTexture target;
    internal readonly int width,height,antiAliasing;
    internal readonly UnityEngine.Experimental.Rendering.GraphicsFormat format;
    internal readonly bool orthographic,stereo,dynamicResolution,enabled;
    internal readonly float size,aspect,near,far;
    internal readonly Vector3 position,forward;
    internal readonly Quaternion rotation;
    CoreGuardianMaskCameraState(Camera value)
    {
        camera=value;view=value.worldToCameraMatrix;projection=value.projectionMatrix;viewport=value.pixelRect;target=value.targetTexture;
        width=target!=null?target.width:Screen.width;height=target!=null?target.height:Screen.height;
        antiAliasing=target!=null?target.antiAliasing:QualitySettings.antiAliasing;format=target!=null?target.graphicsFormat:UnityEngine.Experimental.Rendering.GraphicsFormat.None;
        orthographic=value.orthographic;stereo=value.stereoEnabled;dynamicResolution=value.allowDynamicResolution;enabled=value.enabled;
        size=value.orthographicSize;aspect=value.aspect;near=value.nearClipPlane;far=value.farClipPlane;
        var transform=value.transform;position=transform.position;rotation=transform.rotation;forward=transform.forward;
    }
    internal static CoreGuardianMaskCameraState Capture(Camera camera)=>camera!=null?new CoreGuardianMaskCameraState(camera):default;
    internal bool DefaultProjection=>projection.Equals(Matrix4x4.Ortho(-size*aspect,size*aspect,-size,size,near,far));
}
