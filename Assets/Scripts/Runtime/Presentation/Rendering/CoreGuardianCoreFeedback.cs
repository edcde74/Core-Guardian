using System;
using UnityEngine;
using UnityEngine.UI;

// Presentation only. Core health and existing damage effects are never throttled.
public sealed class CoreGuardianCoreFeedback : IDisposable
{
    const float BodyDuration = .24f,
        HudDuration = .3f,
        WhiteDuration = .045f,
        RetriggerDelay = .3f;
    static readonly int Amount = Shader.PropertyToID("_HitFlashAmount"),
        Tint = Shader.PropertyToID("_HitFlashTint");
    readonly SpriteRenderer body;
    readonly Graphic[] graphics;
    readonly Color[] colors;
    readonly Material material;
    readonly MaterialPropertyBlock saved = new MaterialPropertyBlock(),
        working = new MaterialPropertyBlock();
    Material original;
    bool hadPropertyBlock;
    float age = HudDuration,
        cooldown;
    bool active,
        bodyActive;
    static readonly Color White = new Color(1, .957f, .894f, 1),
        Red = new Color(1, .282f, .282f, 1);

    public CoreGuardianCoreFeedback(SpriteRenderer artwork, Graphic[] hudGraphics)
    {
        body = artwork;
        graphics = hudGraphics ?? Array.Empty<Graphic>();
        colors = new Color[graphics.Length];
        var shader = Resources.Load<Shader>("CoreGuardian/BodyHitFlash");
        if (shader == null)
        {
            throw new InvalidOperationException("Missing core feedback shader");
        }

        material = new Material(shader) { name = "CoreGuardian Core Feedback", hideFlags = HideFlags.HideAndDontSave };
    }

    public bool Trigger()
    {
        if (cooldown > 0)
            return false;

        if (body != null)
        {
            original = body.sharedMaterial;
            hadPropertyBlock = body.HasPropertyBlock();
            body.GetPropertyBlock(saved);
            body.GetPropertyBlock(working);
            body.sharedMaterial = material;
            bodyActive = true;
        }
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
            {
                colors[i] = graphics[i].color;
            }
        }

        age = 0;
        cooldown = RetriggerDelay;
        active = true;
        Apply();
        return true;
    }

    public void Advance(float delta)
    {
        delta = Mathf.Max(0, delta);
        cooldown = Mathf.Max(0, cooldown - delta);
        if (!active)
            return;

        age += delta;
        Apply();
        if (age >= HudDuration)
        {
            Clear();
        }
    }

    void Apply()
    {
        bool white = age < WhiteDuration;
        var tint = white ? White : Red;
        if (bodyActive)
        {
            if (age >= BodyDuration)
            {
                RestoreBody();
            }
            else if (body != null)
            {
                working.SetFloat(Amount, (white ? .55f : .48f) * Mathf.Clamp01(1 - age / BodyDuration));
                working.SetColor(Tint, tint);
                body.SetPropertyBlock(working);
            }
        }
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
            {
                var color = Color.Lerp(colors[i], tint, Mathf.Clamp01(1 - age / HudDuration) * .75f);
                color.a = colors[i].a;
                graphics[i].color = color;
            }
        }
    }

    void RestoreBody()
    {
        if (body != null)
        {
            body.sharedMaterial = original;
            // F30: like HitFlash, put back "no block" instead of an empty block (an empty one changes batching).
            body.SetPropertyBlock(hadPropertyBlock ? saved : null);
        }
        bodyActive = false;
    }

    public void Clear()
    {
        if (bodyActive)
        {
            RestoreBody();
        }

        if (active)
        {
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null)
                {
                    graphics[i].color = colors[i];
                }
            }
        }

        active = false;
        age = HudDuration;
        cooldown = 0;
    }

    public void Dispose()
    {
        Clear();
        if (material != null)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(material);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }
    }
}
