using UnityEngine;

// Persistent scene settings for the dynamically created core particles, not core glow or combat.
[DisallowMultipleComponent]
public sealed class CoreGuardianCoreParticleSettings : MonoBehaviour
{
    [Header("코어 입자 · Play 중 변경하면 즉시 반영")]
    [Min(.001f), Tooltip("가장 작은 입자의 월드 크기. 기존값 0.025.")]
    public float sizeMin = .025f;

    [Min(.001f), Tooltip("가장 큰 입자의 월드 크기. 기존값 0.045.")]
    public float sizeMax = .045f;

    [Min(0), Tooltip("초당 생성하는 입자 수. 기존값 4. 0이면 새 입자를 만들지 않습니다.")]
    public float emissionPerSecond = 4;

    [Min(0), Tooltip("동시에 살아 있을 수 있는 최대 입자 수. 기존값 12.")]
    public int maxParticles = 12;

    ParticleSystem applied;
    float previousMin = -1,
        previousMax = -1,
        previousRate = -1;
    int previousCount = -1;
    ParticleSystem.Particle[] buffer;

    public void ApplyTo(ParticleSystem particles)
    {
        if (particles == null)
            return;

        float min = Mathf.Max(.001f, sizeMin),
            max = Mathf.Max(min, sizeMax),
            rate = Mathf.Max(0, emissionPerSecond);
        int count = Mathf.Max(0, maxParticles);
        bool newSystem = applied != particles;
        if (!newSystem && min == previousMin && max == previousMax && rate == previousRate && count == previousCount)
        {
            return;
        }

        var main = particles.main;
        var emission = particles.emission;
        int restored = -1;
        // Resize already-live particles as well as newborns without restarting their lifetime.
        if (!newSystem && (min != previousMin || max != previousMax || count != previousCount))
        {
            int live = particles.particleCount;
            if (buffer == null || buffer.Length < live)
            {
                buffer = new ParticleSystem.Particle[Mathf.Max(16, live)];
            }

            int n = particles.GetParticles(buffer);
            for (int i = 0; i < n; i++)
            {
                float t =
                    previousMax > previousMin
                        ? Mathf.InverseLerp(previousMin, previousMax, buffer[i].startSize)
                        : (buffer[i].randomSeed % 1000) / 999f;
                buffer[i].startSize = Mathf.Lerp(min, max, t);
            }
            restored = Mathf.Min(n, count);
        }
        main.startSize = new ParticleSystem.MinMaxCurve(min, max);
        main.maxParticles = count;
        emission.rateOverTime = rate;
        // Changing native capacity can rebuild the particle storage; restore after module changes.
        if (restored >= 0)
        {
            particles.SetParticles(buffer, restored);
        }

        applied = particles;
        previousMin = min;
        previousMax = max;
        previousRate = rate;
        previousCount = count;
    }

    void OnValidate()
    {
        sizeMin = Mathf.Max(.001f, sizeMin);
        sizeMax = Mathf.Max(sizeMin, sizeMax);
        emissionPerSecond = Mathf.Max(0, emissionPerSecond);
        maxParticles = Mathf.Max(0, maxParticles);
    }
}
