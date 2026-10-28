using System;
using System.Linq;
using UnityEngine;

// Presentation only. Shared shader, separate property blocks; no damage or movement changes.
// Ordinary hit flash (Trigger) plus a steady recolor while a state lasts (얼어붙음: 색 치환표 FreezeRamp).
// The shared material is swapped in only while either is on.
public sealed class CoreGuardianHitFlash
{
    public const float Duration = .09f,
        RetriggerDelay = .1f,
        Strength = .22f;
    static readonly int AmountId = Shader.PropertyToID("_HitFlashAmount"),
        RampId = Shader.PropertyToID("_RampTex"),
        RampAmountId = Shader.PropertyToID("_RampAmount");
    static Material shared;

    // Domain Reload is off: rebuild the shared material on every Play so shader edits apply (B24).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetShared()
    {
        if (shared != null)
        {
            UnityEngine.Object.DestroyImmediate(shared);
        }
        shared = null;
    }

    readonly SpriteRenderer[] renderers;
    readonly Material[] originals;
    readonly MaterialPropertyBlock[] saved,
        working;
    readonly bool[] hadPropertyBlocks;
    float age = Duration,
        cooldown,
        rampAmount;
    Texture ramp;
    bool active,
        engaged;
    public bool Recolored => rampAmount > 0;

    public CoreGuardianHitFlash(SpriteRenderer artwork)
    {
        if (shared == null)
        {
            var shader = Resources.Load<Shader>("CoreGuardian/BodyHitFlash");
            if (shader == null)
            {
                throw new InvalidOperationException("Missing CoreGuardian body hit shader");
            }

            shared = new Material(shader)
            {
                name = "CoreGuardian Body Hit (shared)",
                hideFlags = HideFlags.HideAndDontSave,
            };
        }
        renderers = artwork
            .GetComponentsInChildren<SpriteRenderer>(true)
            .Where(r => r.sprite == null || !r.sprite.name.Contains("Shadow"))
            .ToArray();
        originals = new Material[renderers.Length];
        saved = new MaterialPropertyBlock[renderers.Length];
        working = new MaterialPropertyBlock[renderers.Length];
        hadPropertyBlocks = new bool[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            saved[i] = new MaterialPropertyBlock();
            working[i] = new MaterialPropertyBlock();
        }
    }

    public bool Trigger()
    {
        if (cooldown > 0)
            return false;

        Engage();
        age = 0;
        cooldown = RetriggerDelay;
        active = true;
        Apply();
        return true;
    }

    // Steady recolor by brightness ramp. amount 0 (or no texture) returns to the original look.
    public void SetRamp(Texture texture, float amount)
    {
        amount = texture == null ? 0 : Mathf.Clamp01(amount);
        if (amount <= 0)
        {
            if (rampAmount <= 0)
                return;

            rampAmount = 0;
            ramp = null;
            if (active)
            {
                Apply();
            }
            else
            {
                Release();
            }

            return;
        }
        if (engaged && rampAmount == amount && ramp == texture)
            return;

        ramp = texture;
        rampAmount = amount;
        Engage();
        Apply();
    }

    void Engage()
    {
        if (engaged)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                originals[i] = renderers[i].sharedMaterial;
                hadPropertyBlocks[i] = renderers[i].HasPropertyBlock();
                renderers[i].GetPropertyBlock(saved[i]);
                renderers[i].GetPropertyBlock(working[i]);
                renderers[i].sharedMaterial = shared;
            }
        }

        engaged = true;
    }

    public void Advance(float delta)
    {
        delta = Mathf.Max(0, delta);
        cooldown = Mathf.Max(0, cooldown - delta);
        if (!active)
            return;

        age += delta;
        if (age >= Duration)
        {
            active = false;
            if (rampAmount > 0)
            {
                Apply();
            }
            else
            {
                Release();
            }

            return;
        }
        Apply();
    }

    void Apply()
    {
        float flash = active ? Strength * (1 - age / Duration) : 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                working[i].SetFloat(AmountId, flash);
                working[i].SetFloat(RampAmountId, rampAmount);
                if (ramp != null)
                {
                    working[i].SetTexture(RampId, ramp);
                }

                renderers[i].SetPropertyBlock(working[i]);
            }
        }
    }

    void Release()
    {
        active = false;
        if (!engaged)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].sharedMaterial = originals[i];
                // An empty override still changes sprite batching; restore absence as null, not an empty block.
                renderers[i].SetPropertyBlock(hadPropertyBlocks[i] ? saved[i] : null);
            }
        }

        engaged = false;
    }

    public void Clear()
    {
        rampAmount = 0;
        ramp = null;
        Release();
    }

    // A pooled body belongs to a new enemy; an old retrigger cooldown must not follow it.
    public void ResetForReuse()
    {
        Clear();
        age = Duration;
        cooldown = 0;
    }
}
