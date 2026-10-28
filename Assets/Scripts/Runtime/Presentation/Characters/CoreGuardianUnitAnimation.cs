using System;
using System.Linq;
using UnityEngine;

// Presentation only. Keeps source artwork and its directional extensions; Session owns combat and movement.
[DefaultExecutionOrder(100)]
public sealed class CoreGuardianUnitAnimation : MonoBehaviour
{
    // Retained for reading archived animation metadata in diagnostic tools.
    [Serializable]
    public sealed class FrameInfo
    {
        public string name;
        public int x,
            y;
        public float muzzleX,
            muzzleY;
    }

    [Serializable]
    public sealed class AtlasInfo
    {
        public FrameInfo[] frames;
    }

    static readonly float[] PlayerAngles = { 0, 45, 90, 135, 180, -135, -90, -45 };
    static readonly float[] CardinalAngles = { 0, 180, 90, -90 };
    SpriteRenderer artwork;
    CoreGuardianOriginalMotion originalMotion;
    CoreGuardianOriginalWalk originalWalk;
    CoreGuardianFrameWalk frameWalk;
    CoreGuardianPlayerMotion playerMotion;
    CoreGuardianHitFlash hitFlash;
    CoreGuardianTowerGlow towerGlow;
    CoreGuardianTowerGrounding towerGrounding;
    bool justHit;
    string role;

    // Fixed at Configure; per-frame branches test these instead of comparing strings (F23).
    bool isPlayer,
        usesOriginalArtwork,
        usesOriginalWalking,
        usesFrameWalking;
    float shotAge = 100,
        shotDuration = .25f,
        idleTime,
        facingAngle = -90;
    Vector2 aim = Vector2.down;
    int direction = 3,
        lastShotFrame = -1;
    bool walking,
        justFired,
        aiming;
    public int Direction => direction;
    internal string PresentationRole => role;

    // Presented volleys, never a combat or balance metric.
    public int ShotCount { get; private set; }
    public Vector3 MuzzleWorld { get; private set; }
    public Vector3 AimOriginWorld => originalMotion != null ? originalMotion.AimOriginWorld : transform.position;
    public Vector3 HealthBarWorld =>
        originalWalk != null ? originalWalk.HealthBarWorld
        : frameWalk != null ? frameWalk.HealthBarWorld
        : transform.position;
    public bool Loaded => originalMotion != null || originalWalk != null || frameWalk != null || playerMotion != null;
    public bool UsesOriginalArtwork => usesOriginalArtwork;
    public bool UsesOriginalWalking => usesOriginalWalking;
    public bool UsesFrameWalking => usesFrameWalking;
    public bool IsEnemyBody => UsesOriginalWalking || UsesFrameWalking;

    public static CoreGuardianUnitAnimation Attach(GameObject root, string role)
    {
        var animation = root.GetComponent<CoreGuardianUnitAnimation>();
        if (animation != null)
            return animation;

        animation = root.AddComponent<CoreGuardianUnitAnimation>();
        animation.Configure(role);
        CoreGuardianPhotoShadow.Attach(root, role);
        return animation;
    }

    void Configure(string value)
    {
        role = value;
        isPlayer = role == "Player";
        usesOriginalArtwork =
            role == "Player"
            || role == "Single"
            || role == "Multi"
            || role == "Support"
            || role == "Frost"
            || role == "Flame"
            || role == "Rail";
        usesOriginalWalking = CoreGuardianOriginalWalk.Supports(role);
        usesFrameWalking = role == "Flyer" || role == "IronKnight" || role == "CrimsonLord";
        if (role == "Player")
        {
            direction = 5;
            facingAngle = -135;
            aim = new Vector2(-8, -6).normalized;
        }
        if (role == "Single" || role == "Rail")
        {
            aim = new Vector2(-23, 13).normalized;
            facingAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        }
        if (role == "Flame")
        {
            aim = new Vector2(-9, 8).normalized;
            facingAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        }
        artwork = GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r => r.name == "2D Sprite");
        if (artwork == null)
        {
            throw new InvalidOperationException("Missing approved artwork child: " + name);
        }

        if (role == "Player")
        {
            playerMotion = new CoreGuardianPlayerMotion(artwork);
        }
        else if (UsesOriginalArtwork)
        {
            originalMotion = new CoreGuardianOriginalMotion(artwork, role);
            if (role == "Single" || role == "Multi" || role == "Support")
            {
                towerGlow = new CoreGuardianTowerGlow(artwork, role);
            }

            towerGrounding = new CoreGuardianTowerGrounding(artwork, role);
            if (isActiveAndEnabled)
            {
                CoreGuardianLateUpdateDriver.Add(this);
            }
        }
        else if (UsesOriginalWalking)
        {
            originalWalk = new CoreGuardianOriginalWalk(artwork, role);
            hitFlash = new CoreGuardianHitFlash(artwork);
        }
        else if (UsesFrameWalking)
        {
            frameWalk = CreateFrameWalk(artwork, role);
            hitFlash = new CoreGuardianHitFlash(artwork);
        }
        else
        {
            throw new InvalidOperationException("Unsupported original animation role: " + role);
        }

        MuzzleWorld = artwork.transform.position;
    }

    static CoreGuardianFrameWalk CreateFrameWalk(SpriteRenderer artwork, string role)
    {
        if (role == "Flyer")
        {
            var hover = new Sprite[6];
            for (int i = 0; i < 6; i++)
            {
                hover[i] = Resources.Load<Sprite>("CoreGuardian/OriginalWalk/Flyer/Hover_" + i);
            }

            Sprite[] V(string d)
            {
                var f = new Sprite[4];
                for (int i = 0; i < 4; i++)
                {
                    f[i] = Resources.Load<Sprite>("CoreGuardian/VerticalWalk/Flyer/" + d + "_" + i);
                }
                return f;
            }
            return new CoreGuardianFrameWalk(artwork, hover, null, V("Up"), V("Down"), 14, 10);
        }
        var library = CoreGuardianSpriteLibrary.Instance;
        Sprite[] B(string d)
        {
            var f = new Sprite[4];
            for (int i = 0; i < 4; i++)
            {
                f[i] = library != null ? library.Get(role + "/" + d + "_" + i) : null;
            }
            return f;
        }
        var walk = new CoreGuardianFrameWalk(artwork, B("Left"), B("Right"), B("Up"), B("Down"), 18, 0);
        if (role == "IronKnight")
        {
            walk.SideBobPixels = 2;
        }

        return walk;
    }

    public void Move(Vector2 worldVelocity, float delta) => TrackMotion(worldVelocity, delta, true);

    public void MoveWithoutTurning(Vector2 worldVelocity, float delta) => TrackMotion(worldVelocity, delta, false);

    void TrackMotion(Vector2 velocity, float delta, bool turn)
    {
        originalWalk?.Move(velocity, delta);
        frameWalk?.Move(velocity, delta);
        playerMotion?.Move(velocity, delta);
        walking = velocity.sqrMagnitude > .000001f;
        if (isPlayer)
        {
            aiming = !turn;
        }

        if (walking && turn)
        {
            Face(velocity, isPlayer, delta);
        }
    }

    public void Aim(Vector2 vector, float delta)
    {
        if (vector.sqrMagnitude > .000001f)
        {
            aiming = true;
            Face(vector, false, delta);
        }
    }

    void Face(Vector2 vector, bool snap, float delta)
    {
        aim = vector.normalized;
        float target = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        facingAngle = snap ? target : Mathf.MoveTowardsAngle(facingAngle, target, 900 * delta);
        var angles = isPlayer ? PlayerAngles : CardinalAngles;
        int best = direction;
        for (int i = 0; i < angles.Length; i++)
        {
            if (
                Mathf.Abs(Mathf.DeltaAngle(facingAngle, angles[i])) + 6
                < Mathf.Abs(Mathf.DeltaAngle(facingAngle, angles[best]))
            )
            {
                best = i;
            }
        }

        direction = best;
    }

    public void Fire(Vector2 vector, float attacksPerSecond)
    {
        Face(vector, true, 0);
        if (lastShotFrame != Time.frameCount)
        {
            ShotCount++;
            lastShotFrame = Time.frameCount;
        }
        shotAge = 0;
        shotDuration = Mathf.Clamp(.8f / Mathf.Max(.01f, attacksPerSecond), .045f, .28f);
        justFired = true;
        ApplyFrame(0);
    }

    public void Present(float delta)
    {
        if (!walking && shotAge >= shotDuration)
        {
            idleTime += delta;
        }
        else
        {
            idleTime = 0;
        }

        if (!justFired)
        {
            shotAge += delta;
        }

        justFired = false;
        ApplyFrame(delta);
        hitFlash?.Advance(justHit ? 0 : delta);
        justHit = false;
    }

    public void ReceiveHit()
    {
        if (hitFlash != null && hitFlash.Trigger())
        {
            justHit = true;
        }
    }

    // 얼어붙음(D7): 상태 동안 몸을 색 치환표(FreezeRamp)로 바꿈.
    public void SetBodyRecolor(Texture ramp, float amount) => hitFlash?.SetRamp(ramp, amount);

    public void ResetEnemyForReuse()
    {
        if (originalWalk == null && frameWalk == null)
        {
            throw new InvalidOperationException("Only configured enemy walking bodies can be reused");
        }

        hitFlash?.ResetForReuse();
        originalWalk?.ResetForReuse();
        frameWalk?.ResetForReuse();
        justHit = walking = justFired = aiming = false;
        shotAge = 100;
        shotDuration = .25f;
        idleTime = 0;
        facingAngle = -90;
        aim = Vector2.down;
        direction = 3;
        lastShotFrame = -1;
        ShotCount = 0;
        MuzzleWorld = artwork.transform.position;
    }

    void OnEnable()
    {
        towerGlow?.Enable();
        if (towerGrounding != null)
        {
            CoreGuardianLateUpdateDriver.Add(this);
        }
    }

    void OnDisable()
    {
        CoreGuardianLateUpdateDriver.Remove(this);
        hitFlash?.Clear();
        if (towerGlow != null)
        {
            shotAge = 100;
            justFired = false;
            towerGlow.Clear();
        }
    }

    void OnDestroy()
    {
        towerGrounding?.Dispose();
        towerGlow?.Dispose();
        originalMotion?.Dispose();
    }

    // Depth sorting can run after Present; keep the stationary shade above the final base order.
    // Called from CoreGuardianLateUpdateDriver for towers only (F24); enemies never had a shade to present.
    internal void PresentGrounding()
    {
        if (isActiveAndEnabled)
        {
            towerGrounding?.Present();
        }
    }

    void ApplyFrame(float delta)
    {
        if (artwork == null)
            return;

        if (playerMotion != null)
        {
            playerMotion.Present(direction, aim, shotAge, shotDuration, idleTime, aiming);
            MuzzleWorld = playerMotion.MuzzleWorld;
            return;
        }
        if (originalWalk != null)
        {
            originalWalk.Present(delta);
            return;
        }
        if (frameWalk != null)
        {
            frameWalk.Present(delta);
            return;
        }
        originalMotion.Present(shotAge, shotDuration, (Vector2)(Quaternion.Euler(0, 0, facingAngle) * Vector3.right));
        towerGlow?.Present(shotAge, shotDuration, delta);
        towerGrounding?.Present();
        MuzzleWorld = originalMotion.MuzzleWorld(aim);
    }
}
