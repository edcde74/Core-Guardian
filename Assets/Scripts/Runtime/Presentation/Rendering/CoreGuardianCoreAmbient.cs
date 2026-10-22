using System;
using UnityEngine;

// Core-local presentation only: no camera, Volume, lighting, source material or combat changes.
public sealed class CoreGuardianCoreAmbient : IDisposable
{
    readonly SpriteRenderer source;
    readonly GameObject root;
    readonly Mesh mesh;
    readonly MeshRenderer glow;
    readonly Material glowMaterial,particleMaterial;
    readonly ParticleSystem particles;
    readonly ParticleSystemRenderer particleRenderer;
    readonly CoreGuardianCoreParticleSettings particleSettings;
    float phase,suppression;
    bool enabled=true,disposed;

    public CoreGuardianCoreAmbient(SpriteRenderer artwork):this(artwork,null){}
    public CoreGuardianCoreAmbient(SpriteRenderer artwork,CoreGuardianCoreParticleSettings settings)
    {
        particleSettings=settings;
        if(artwork==null||artwork.sprite==null)throw new ArgumentException("Visible core artwork required",nameof(artwork));
        var shader=Resources.Load<Shader>("CoreGuardian/CoreAmbient");
        if(shader==null)throw new InvalidOperationException("Missing core ambient shader");
        source=artwork;
        root=new GameObject("CoreAmbient_Local");root.transform.SetParent(source.transform,false);
        var sprite=source.sprite;var bounds=sprite.bounds;var rect=sprite.textureRect;float pad=5/sprite.pixelsPerUnit;
        float x0=bounds.min.x-pad,x1=bounds.max.x+pad,y0=bounds.min.y-pad,y1=bounds.max.y+pad;
        float u0=(rect.xMin-5)/sprite.texture.width,u1=(rect.xMax+5)/sprite.texture.width;
        float v0=(rect.yMin-5)/sprite.texture.height,v1=(rect.yMax+5)/sprite.texture.height;
        mesh=new Mesh{name="Core ambient expanded source quad"};
        mesh.vertices=new[]{new Vector3(x0,y0),new Vector3(x1,y0),new Vector3(x1,y1),new Vector3(x0,y1)};
        mesh.uv=new[]{new Vector2(u0,v0),new Vector2(u1,v0),new Vector2(u1,v1),new Vector2(u0,v1)};
        mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();
        root.AddComponent<MeshFilter>().sharedMesh=mesh;glow=root.AddComponent<MeshRenderer>();
        glowMaterial=new Material(shader){name="Core local crystal glow"};glowMaterial.mainTexture=sprite.texture;
        glowMaterial.SetVector("_UVRect",new Vector4(rect.xMin/sprite.texture.width,rect.yMin/sprite.texture.height,rect.xMax/sprite.texture.width,rect.yMax/sprite.texture.height));
        glow.sharedMaterial=glowMaterial;
        var particleGo=new GameObject("CoreCrystalParticles");particleGo.transform.SetParent(root.transform,false);
        // Particle dimensions are world-sized even when the original artwork has an imported scale.
        var scale=source.transform.lossyScale;particleGo.transform.localScale=new Vector3(1/Mathf.Max(.001f,Mathf.Abs(scale.x)),1/Mathf.Max(.001f,Mathf.Abs(scale.y)),1);
        particleGo.transform.position=source.bounds.center+Vector3.up*source.bounds.size.y*.25f;
        particles=particleGo.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=particles.main;main.playOnAwake=false;main.loop=true;main.duration=3;main.simulationSpace=ParticleSystemSimulationSpace.World;
        main.startLifetime=new ParticleSystem.MinMaxCurve(1.4f,2.1f);main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(.025f,.045f);
        main.startColor=new Color(.25f,.8f,1,.65f);main.maxParticles=12;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
        var emission=particles.emission;emission.rateOverTime=4;
        var shape=particles.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Rectangle;shape.scale=new Vector3(.45f,.15f,.01f);
        var velocity=particles.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;
        velocity.x=new ParticleSystem.MinMaxCurve(-.035f,.035f);velocity.y=new ParticleSystem.MinMaxCurve(.18f,.3f);velocity.z=new ParticleSystem.MinMaxCurve(0,0);
        var lifetime=particles.colorOverLifetime;lifetime.enabled=true;
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.65f,.15f),new GradientAlphaKey(0,1)});lifetime.color=gradient;
        particleRenderer=particles.GetComponent<ParticleSystemRenderer>();particleRenderer.renderMode=ParticleSystemRenderMode.Billboard;
        particleMaterial=new Material(shader){name="Core local pixel particles"};particleMaterial.SetFloat("_Particles",1);particleRenderer.sharedMaterial=particleMaterial;
        Advance(0);
        if(Application.isPlaying)particles.Play(true);
    }
    public void Advance(float delta)
    {
        if(disposed||root==null||source==null||particles==null)return;
        if(particleSettings!=null)particleSettings.ApplyTo(particles);
        delta=Mathf.Max(0,delta);phase+=delta;suppression=Mathf.Max(0,suppression-delta);
        bool visible=enabled&&suppression<=0&&source!=null&&source.enabled;
        bool showParticles=visible&&(particleSettings==null||particleSettings.isActiveAndEnabled);
        glow.enabled=visible;particleRenderer.enabled=showParticles;
        var emission=particles.emission;emission.enabled=showParticles;
        glowMaterial.SetFloat("_Strength",.34f+.06f*Mathf.Sin(phase*Mathf.PI*2/3));
        if(source!=null)
        {
            glow.sortingLayerID=source.sortingLayerID;glow.sortingOrder=source.sortingOrder+1;
            particleRenderer.sortingLayerID=source.sortingLayerID;particleRenderer.sortingOrder=source.sortingOrder+2;
        }
    }
    public void NotifyCoreDamage()
    {
        if(disposed||particles==null)return;
        suppression=.3f;particles.Clear(true);Advance(0);
    }
    public void SetEnabled(bool value)
    {
        if(disposed)return;
        enabled=value;if(!value&&particles!=null)particles.Clear(true);Advance(0);
    }
    public void Dispose()
    {
        if(disposed)return;disposed=true;
        DestroyOwned(root);DestroyOwned(mesh);DestroyOwned(glowMaterial);DestroyOwned(particleMaterial);
    }
    static void DestroyOwned(UnityEngine.Object value)
    {
        if(value==null)return;
        if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);
    }
}
