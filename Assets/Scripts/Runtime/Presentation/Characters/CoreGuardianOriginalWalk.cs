using UnityEngine;

// Original cutout legs on X; separate source-based rear/front frames on Y.
public sealed class CoreGuardianOriginalWalk
{
    internal static void ClearCachedParts() => VerticalLegPair.ClearCachedParts();

    readonly SpriteRenderer original,
        body;

    // New roles (2026-10 enemies) carry their leg layers on a canvas that differs from the full
    // sprite; joints are measured on that layer canvas instead of the original sprite.
    readonly Sprite reference;
    readonly bool generic;

    // 11-02: 분열 조각의 아래 걷기 그림 4장은 모두 같은 발(왼발)만 내딛는다.
    // 앞 2장(내딛기·모으기)을 뒤 2장에서 좌우로 뒤집어 두 발이 번갈아 나가게 한다.
    readonly bool mirrorDownSteps;

    // Joint pixels from the top of the layer canvas: hip, knee, ankle (left then right), cycle, lift, anchor x, top.
    static readonly System.Collections.Generic.Dictionary<string, float[]> NewRoles =
        new System.Collections.Generic.Dictionary<string, float[]>
        {
            { "Charger", new float[] { 24, 43, 24, 49, 24, 61, 38.5f, 44, 39.5f, 51, 39.5f, 61, 14, 1.8f, 7.75f, 52 } },
            { "Splitter", new float[] { 13, 31, 12, 36, 12, 43, 23, 32, 24, 37, 24, 43, 10, 1.2f, 3, 38 } },
            { "SplitChild", new float[] { 13, 31, 12, 36, 12, 43, 23, 32, 24, 37, 24, 43, 9, 1.2f, 3, 38 } },
            { "Healer", new float[] { 11.5f, 62, 11, 67, 11, 74, 23, 62, 23, 69, 23, 77, 12, 1.6f, -.5f, 62 } },
        };

    public static bool Supports(string role) =>
        role == "Attack"
        || role == "Defense"
        || role == "Armored"
        || role == "Buff"
        || role == "Boss"
        || NewRoles.ContainsKey(role);

    // 10-30 사용자 결정(B안): 장갑병은 방패병 몸을 검은 강철색으로 바꾼 그림이라 관절·걷는 동작은 방패병 것을 쓴다.
    // 그림 파일만 Armored 폴더(색을 바꾼 사본)에서 읽는다.
    static string RigRole(string role) => role == "Armored" ? "Defense" : role;

    readonly Transform rig;
    readonly SpriteRenderer[] layers;

    // 분열체(10-27 확정 그림): 뒤 병사(어두운 Back* 조각, 반 박자 늦게) + 허리 끈. 오프셋은 Animation/Splitter/joints.json의
    // back.offset [14, -2]·cordOffset [0, -2](원점 왼쪽 위 픽셀 → 여기선 위가 +). 끈 그림(46×48, 발 피벗 23)은 앞 병사 캔버스(30×46,
    // 피벗 15)와 왼쪽 위를 맞춘 좌표라 피벗 기준으로는 오른쪽 8·위 0. 그리기 순서: 뒤 병사 → 앞 병사 → 끈.
    static readonly Vector2 SplitterBackOffsetPixels = new Vector2(14, 2),
        SplitterCordOffsetPixels = new Vector2(8, 0);
    readonly Transform back;
    readonly SpriteRenderer backBody,
        cord;
    readonly SpriteRenderer[] backLegParts;

    // 뒤 다리 복사(10-30 장갑병용)는 장갑병 그림을 방패병 바탕으로 바꾸면서 쓰지 않는다. 그리기 코드는 남겨 둔다.
    static readonly Color RearLegShade = new Color(.62f, .62f, .66f, 1);
    readonly Leg rearLeg;
    readonly SpriteRenderer[] rearLegParts;
    readonly Leg backLeft,
        backRight;
    readonly float ppu,
        cyclePixels,
        lift;
    readonly Vector2 groundAnchor;
    readonly Vector3 groundInParent;
    readonly float healthTopPixels;
    readonly SpriteRenderer vertical;
    readonly Sprite[,] verticalFrames = new Sprite[2, 4];
    readonly VerticalLegPair verticalLegs;
    readonly Leg left,
        right;
    readonly Color initialColor;
    readonly bool initialFlipY;
    float phase,
        weight;
    bool walking,
        mirror;
    int verticalFacing;
    Vector2 groundDirection = Vector2.left;

    sealed class Leg
    {
        public Vector2 hip,
            knee,
            ankle;
        public Transform upper,
            lower;

        // Fixed source joints: lengths, bend side and rest bones never change after creation.
        public float upperLength,
            lowerLength,
            side;
        public Vector2 upperRest,
            lowerRest;

        public Leg Prepare()
        {
            upperLength = Vector2.Distance(hip, knee);
            lowerLength = Vector2.Distance(knee, ankle);
            var rest = ankle - hip;
            var kneeRest = knee - hip;
            side = Mathf.Sign(rest.x * kneeRest.y - rest.y * kneeRest.x);
            if (side == 0)
            {
                side = 1;
            }

            upperRest = knee - hip;
            lowerRest = ankle - knee;
            return this;
        }
    }

    public float Phase => phase;
    public Vector3 HealthBarWorld =>
        original.transform.parent.TransformPoint(
            groundInParent + Vector3.up * ((healthTopPixels + 4) * original.transform.localScale.y / ppu)
        );

    public CoreGuardianOriginalWalk(SpriteRenderer artwork, string role)
    {
        original = artwork;
        string rigRole = RigRole(role);
        ppu = original.sprite.pixelsPerUnit;
        initialColor = original.color;
        initialFlipY = original.flipY;
        // Maximum opaque height above the foot anchor across source and all Y frames.
        // Include raised equipment, so the bar never cuts across a spear or banner.
        // Stable per role: walking frames must not make the health bar bob.
        rig = new GameObject("OriginalWalk").transform;
        rig.SetParent(artwork.transform, false);
        body = Layer("Body", role);
        var lu = Layer("LeftUpper", role);
        var ll = Layer("LeftLower", role);
        var ru = Layer("RightUpper", role);
        var rl = Layer("RightLower", role);
        layers = new[] { body, lu, ll, ru, rl };
        generic = NewRoles.TryGetValue(rigRole, out var table);
        mirrorDownSteps = role == "SplitChild";
        reference = generic ? body.sprite : original.sprite;
        float directionalTop;
        if (generic)
        {
            left = Bone(lu, ll, table[0], table[1], table[2], table[3], table[4], table[5]);
            right = Bone(ru, rl, table[6], table[7], table[8], table[9], table[10], table[11]);
            cyclePixels = table[12];
            lift = table[13];
            groundAnchor = new Vector2(table[14], 0);
            directionalTop = table[15];
        }
        else
        {
            if (rigRole == "Attack")
            {
                left = Bone(lu, ll, 18, 43, 19, 49, 17, 61);
                right = Bone(ru, rl, 30, 43, 31, 51, 35, 60);
                cyclePixels = 14;
                lift = 1.8f;
            }
            else if (rigRole == "Defense")
            {
                left = Bone(lu, ll, 17, 31, 17, 36, 17, 42);
                right = Bone(ru, rl, 24, 29, 24, 36, 27, 44);
                cyclePixels = 10;
                lift = 1.4f;
            }
            else if (rigRole == "Buff")
            {
                left = Bone(lu, ll, 13, 61, 12, 67, 9, 74);
                right = Bone(ru, rl, 22, 60, 24, 69, 24, 77);
                cyclePixels = 12;
                lift = 1.6f;
            }
            else
            {
                left = Bone(lu, ll, 32, 51, 32, 59, 28, 67);
                right = Bone(ru, rl, 47, 51, 49, 60, 53, 70);
                cyclePixels = 18;
                lift = 2.2f;
            }
            // Midpoint of the two original boot soles, in source pixels relative to its pivot.
            // This is a ground anchor, not the center of the canvas/weapon/banner.
            groundAnchor =
                rigRole == "Attack" ? new Vector2(3.5f, 0)
                : rigRole == "Defense" ? new Vector2(5.5f, 4)
                : rigRole == "Buff" ? new Vector2(-1.75f, 1.5f)
                : new Vector2(-.75f, 2);
            directionalTop =
                rigRole == "Attack" ? 65
                : rigRole == "Defense" ? 46
                : rigRole == "Buff" ? 84
                : 65;
        }
        if (role == "Splitter")
        {
            backBody = Layer("BackBody", role);
            var blu = Layer("BackLeftUpper", role);
            var bll = Layer("BackLeftLower", role);
            var bru = Layer("BackRightUpper", role);
            var brl = Layer("BackRightLower", role);
            back = new GameObject("Back").transform;
            back.SetParent(rig, false);
            back.localPosition = SplitterBackOffsetPixels / ppu;
            foreach (var part in new[] { backBody, blu, bll, bru, brl })
            {
                part.transform.SetParent(back, false);
            }
            backLeft = Bone(blu, bll, table[0], table[1], table[2], table[3], table[4], table[5]);
            backRight = Bone(bru, brl, table[6], table[7], table[8], table[9], table[10], table[11]);
            cord = Layer("Cord", role);
            cord.transform.SetParent(body.transform, false);
            cord.transform.localPosition = SplitterCordOffsetPixels / ppu;
            backLegParts = new[] { blu, bll, bru, brl };
        }
        healthTopPixels = Mathf.Max(directionalTop, reference.rect.height - reference.pivot.y - groundAnchor.y);
        var parent = artwork.transform.parent;
        groundInParent = parent.InverseTransformPoint(artwork.transform.TransformPoint(groundAnchor / ppu));
        foreach (var renderer in parent.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer.sprite != null && renderer.sprite.name.Contains("Shadow"))
            {
                groundInParent = parent.InverseTransformPoint(renderer.bounds.center);
                break;
            }
        }
        // The prefab's flattened original soles are static. They look like extra feet
        // when the walking rig lifts or turns; retain the central ellipse instead.
        foreach (var renderer in parent.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer.name == "바닥 밀착선")
            {
                renderer.enabled = false;
            }
        }

        AlignGround();
        rig.gameObject.SetActive(false);
        vertical = new GameObject("VerticalWalk").AddComponent<SpriteRenderer>();
        vertical.transform.SetParent(artwork.transform, false);
        vertical.sharedMaterial = artwork.sharedMaterial;
        vertical.transform.localPosition = groundAnchor / ppu;
        for (int d = 0; d < 2; d++)
        {
            for (int f = 0; f < 4; f++)
            {
                string path = "CoreGuardian/VerticalWalk/" + role + "/" + (d == 0 ? "Up_" : "Down_") + f;
                verticalFrames[d, f] = Resources.Load<Sprite>(path);
                if (verticalFrames[d, f] == null)
                {
                    throw new System.InvalidOperationException("Missing directional walk frame " + path);
                }
            }
        }

        vertical.gameObject.SetActive(false);
        // New roles' Up/Down drawings are complete walking frames; no leg splitting.
        verticalLegs = generic ? null : new VerticalLegPair(vertical, verticalFrames, rigRole);
    }

    SpriteRenderer Layer(string name, string role)
    {
        var sprite = Resources.Load<Sprite>("CoreGuardian/OriginalWalk/" + role + "/" + name);
        if (sprite == null)
        {
            throw new System.InvalidOperationException("Missing original walk layer " + role + "/" + name);
        }

        var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
        renderer.transform.SetParent(rig, false);
        renderer.sprite = sprite;
        renderer.sharedMaterial = original.sharedMaterial;
        return renderer;
    }

    Vector2 Point(float x, float top) => (new Vector2(x, reference.rect.height - top) - reference.pivot);

    Leg Bone(SpriteRenderer upper, SpriteRenderer lower, float hx, float hy, float kx, float ky, float ax, float ay) =>
        new Leg
        {
            hip = Point(hx, hy),
            knee = Point(kx, ky),
            ankle = Point(ax, ay),
            upper = upper.transform,
            lower = lower.transform,
        }.Prepare();

    public void Move(Vector2 worldVelocity, float delta)
    {
        walking = worldVelocity.sqrMagnitude > .000001f;
        if (!walking)
            return;

        Vector2 local = original.transform.InverseTransformVector(worldVelocity);
        // Select the presented direction from the actual waypoint displacement, on this turn.
        // A vertical turn must replace the side artwork, not just its leg trajectory.
        verticalFacing = Mathf.Abs(local.y) > Mathf.Abs(local.x) ? (local.y > 0 ? 1 : -1) : 0;
        if (verticalFacing != 0)
        {
            mirror = false;
        }
        else if (local.x > .0001f)
        {
            mirror = true;
        }
        else if (local.x < -.0001f)
        {
            mirror = false;
        }

        phase = Mathf.Repeat(phase + local.magnitude * Mathf.Max(0, delta) * ppu / cyclePixels, 1);
        if (mirror)
        {
            local.x = -local.x;
        }

        groundDirection = local.normalized;
    }

    public void ResetForReuse()
    {
        phase = weight = 0;
        walking = mirror = false;
        verticalFacing = 0;
        groundDirection = Vector2.left;
        original.color = initialColor;
        original.flipX = false;
        original.flipY = initialFlipY;
        original.enabled = true;
        rig.localScale = Vector3.one;
        rig.gameObject.SetActive(false);
        vertical.gameObject.SetActive(false);
        AlignGround();
    }

    // Stance lasts exactly half a cycle for either leg. The other half is a lifted return.
    public static Vector2 FootOffset(float phase, float cyclePixels, float lift, Vector2 direction)
    {
        float p = Mathf.Repeat(phase, 1),
            stride = cyclePixels * .5f;
        if (p < .5f)
            return direction * (stride * (.5f - p * 2));

        float t = (p - .5f) * 2,
            forward = t * t * (3 - 2 * t);
        return direction * (stride * (forward - .5f)) + Vector2.up * (lift * Mathf.Sin(t * Mathf.PI));
    }

    public void Present(float delta)
    {
        weight = Mathf.MoveTowards(weight, walking ? 1 : 0, Mathf.Max(0, delta) * 8);
        if (original.flipX != mirror)
        {
            original.flipX = mirror;
        }

        AlignGround();
        // F28: the same values, written only when they differ.
        SetActive(vertical.gameObject, verticalFacing != 0);
        if (verticalFacing != 0)
        {
            SetActive(rig.gameObject, false);
            if (original.enabled)
            {
                original.enabled = false;
            }
            int step = walking ? Mathf.Min(3, (int)(phase * 4)) : 0;
            bool flipStep = mirrorDownSteps && verticalFacing < 0 && step >= 2;
            var frame = verticalFrames[verticalFacing > 0 ? 0 : 1, flipStep ? step - 2 : step];
            if (vertical.flipX != flipStep)
            {
                vertical.flipX = flipStep;
            }
            if (vertical.sprite != frame)
            {
                vertical.sprite = frame;
            }
            if (!SameColor(vertical.color, original.color))
            {
                vertical.color = original.color;
            }
            if (vertical.sortingLayerID != original.sortingLayerID)
            {
                vertical.sortingLayerID = original.sortingLayerID;
            }
            if (vertical.sortingOrder != original.sortingOrder)
            {
                vertical.sortingOrder = original.sortingOrder;
            }
            verticalLegs?.Present(vertical.sprite, verticalFacing > 0 ? 0 : 1, phase, walking);
            return;
        }
        bool active = weight > .0001f;
        SetActive(rig.gameObject, active);
        if (original.enabled != !active)
        {
            original.enabled = !active;
        }
        if (!active)
            return;

        var scale = new Vector3(mirror ? -1 : 1, 1, 1);
        if (rig.localScale != scale)
        {
            rig.localScale = scale;
        }
        // Write only values that differ: each renderer write re-dirties its draw data. Final state is identical.
        var color = original.color;
        int layerId = original.sortingLayerID,
            order = original.sortingOrder;
        foreach (var layer in layers)
        {
            if (!SameColor(layer.color, color))
            {
                layer.color = color;
            }

            if (layer.sortingLayerID != layerId)
            {
                layer.sortingLayerID = layerId;
            }

            if (layer.sortingOrder != order)
            {
                layer.sortingOrder = order;
            }
        }
        if (body.sortingOrder != order + 1)
        {
            body.sortingOrder = order + 1;
        }
        // Two equal weight transfers per cycle, rather than one deep dip on a single leg.
        Vector2 pelvis = Vector2.up * ((-.65f + .3f * Mathf.Cos(phase * Mathf.PI * 4)) * weight);
        body.transform.localPosition = pelvis / ppu;
        Pose(left, WalkingFootOffset(phase, cyclePixels, lift, groundDirection) * weight, pelvis);
        Pose(right, WalkingFootOffset(phase + .5f, cyclePixels, lift, groundDirection) * weight, pelvis);
        if (back != null)
        {
            PresentSplitterBack(order);
        }
        if (rearLeg != null)
        {
            foreach (var part in rearLegParts)
            {
                SyncPart(part, order - 1, RearLegShade);
            }
            Pose(rearLeg, WalkingFootOffset(phase, cyclePixels, lift, groundDirection) * weight, pelvis);
        }
    }

    // Back soldier half a step behind the front one; drawn under the front soldier, the cord over both.
    void PresentSplitterBack(int order)
    {
        SyncPart(backBody, order - 1);
        foreach (var part in backLegParts)
        {
            SyncPart(part, order - 2);
        }
        SyncPart(cord, order + 2);
        float backPhase = phase + .5f;
        Vector2 backPelvis = Vector2.up * ((-.65f + .3f * Mathf.Cos(backPhase * Mathf.PI * 4)) * weight);
        backBody.transform.localPosition = backPelvis / ppu;
        Pose(backLeft, WalkingFootOffset(backPhase, cyclePixels, lift, groundDirection) * weight, backPelvis);
        Pose(backRight, WalkingFootOffset(backPhase + .5f, cyclePixels, lift, groundDirection) * weight, backPelvis);
    }

    // Same color/layer as the original; order per part. Written only when different (F28).
    void SyncPart(SpriteRenderer part, int order) => SyncPart(part, order, Color.white);

    void SyncPart(SpriteRenderer part, int order, Color shade)
    {
        var color = original.color * shade;
        if (!SameColor(part.color, color))
        {
            part.color = color;
        }
        if (part.sortingLayerID != original.sortingLayerID)
        {
            part.sortingLayerID = original.sortingLayerID;
        }
        if (part.sortingOrder != order)
        {
            part.sortingOrder = order;
        }
    }

    static void SetActive(GameObject target, bool value)
    {
        if (target.activeSelf != value)
        {
            target.SetActive(value);
        }
    }

    // Exact component comparison (Color == is approximate).
    static bool SameColor(Color a, Color b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

    void AlignGround()
    {
        Vector3 anchor = new Vector3((mirror ? -1 : 1) * groundAnchor.x, groundAnchor.y, 0) / ppu;
        var transform = original.transform;
        transform.localPosition =
            groundInParent - transform.localRotation * Vector3.Scale(transform.localScale, anchor);
    }

    public static Vector2 WalkingFootOffset(float phase, float cyclePixels, float lift, Vector2 direction)
    {
        // On the ground plane, depth travel is foreshortened; boot lift remains screen-up.
        // Using full vertical stride stretched a nearly straight leg against the IK limit,
        // concealing the return step, particularly when travelling toward +Y.
        float depth = Mathf.Abs(direction.y);
        return FootOffset(phase, cyclePixels, lift * (1 + depth), new Vector2(direction.x, direction.y * .3f));
    }

    void Pose(Leg leg, Vector2 offset, Vector2 pelvis)
    {
        Vector2 hip = leg.hip + pelvis,
            goal = leg.ankle + offset;
        float upper = leg.upperLength,
            lower = leg.lowerLength;
        Vector2 difference = goal - hip;
        float distance = Mathf.Clamp(difference.magnitude, Mathf.Abs(upper - lower) + .001f, upper + lower - .001f);
        var direction = difference.normalized;
        float along = (upper * upper - lower * lower + distance * distance) / (2 * distance);
        float side = leg.side;
        Vector2 knee =
            hip
            + direction * along
            + new Vector2(-direction.y, direction.x) * (side * Mathf.Sqrt(Mathf.Max(0, upper * upper - along * along)));
        Vector2 ankle = hip + direction * distance;
        Place(leg.upper, leg.hip, hip, Vector2.SignedAngle(leg.upperRest, knee - hip));
        Place(leg.lower, leg.knee, knee, Vector2.SignedAngle(leg.lowerRest, ankle - knee));
    }

    void Place(Transform part, Vector2 sourceJoint, Vector2 destination, float angle)
    {
        var rotation = Quaternion.Euler(0, 0, angle);
        part.SetLocalPositionAndRotation(((Vector3)destination - rotation * (Vector3)sourceJoint) / ppu, rotation);
    }

    // The directional drawings held one knee up in every frame. Reuse their existing
    // boot pixels as two independent legs; the upper body and equipment keep their art.
    sealed class VerticalLegPair
    {
        static readonly System.Collections.Generic.Dictionary<Sprite, Sprite[]> bodies =
            new System.Collections.Generic.Dictionary<Sprite, Sprite[]>();
        static readonly System.Collections.Generic.Dictionary<Sprite, Sprite> boots =
            new System.Collections.Generic.Dictionary<Sprite, Sprite>();

        internal static void ClearCachedParts()
        {
            foreach (var pair in bodies)
            {
                foreach (var sprite in pair.Value)
                {
                    Release(sprite);
                }
            }

            foreach (var pair in boots)
            {
                Release(pair.Value);
            }

            bodies.Clear();
            boots.Clear();
            void Release(Sprite sprite)
            {
                if (sprite == null)
                    return;

                if (Application.isPlaying)
                {
                    Object.Destroy(sprite);
                }
                else
                {
                    Object.DestroyImmediate(sprite);
                }
            }
        }

        readonly SpriteRenderer body,
            leftFoot,
            rightFoot;
        readonly SpriteRenderer[] bodyParts;
        readonly Rect[][] bodyRects;
        readonly Sprite[] templates = new Sprite[2];
        readonly bool frontOnly;
        readonly bool defenseRear,
            coherentFront;
        readonly float leftX,
            rightX;
        readonly Sprite frontRest,
            rearRest;

        public VerticalLegPair(SpriteRenderer renderer, Sprite[,] frames, string role)
        {
            body = renderer;
            frontOnly = role != "Attack";
            defenseRear = role == "Defense";
            coherentFront = role == "Boss";
            frontRest = frames[1, 0];
            rearRest = frames[0, 0];
            // Cut only the fixed front legs. Keep shield/banner/hammer columns and
            // upper-body artwork intact. Defense also has a wider rear stance.
            int top = 19,
                cutLeft = 42,
                cutRight = 59,
                bootX = 44,
                bootWidth = 7,
                bootHeight = 23;
            leftX = -3;
            rightX = 5;
            if (role == "Defense")
            {
                top = 22;
                cutLeft = 43;
                cutRight = 55;
                bootX = 48;
                bootWidth = 6;
                bootHeight = 22;
                leftX = -4;
                rightX = 3;
            }
            else if (role == "Buff")
            {
                top = 22;
                cutLeft = 43;
                cutRight = 58;
                bootX = 44;
                bootWidth = 8;
                bootHeight = 22;
                leftX = -1.5f;
                rightX = 6.5f;
            }
            else if (role == "Boss")
            {
                top = 25;
                cutLeft = 40;
                cutRight = 67;
                bootX = 45;
                bootWidth = 12;
                bootHeight = 25;
                leftX = 0;
                rightX = 10;
            }
            bodyRects = new[]
            {
                defenseRear ? FrontBodyRects(rearRest, 22, 42, 54)
                : frontOnly ? null
                : BodyRects(frames[0, 0], 19, 39, 59),
                frontOnly
                    ? FrontBodyRects(frontRest, top, cutLeft, cutRight)
                    : BodyRects(frontRest, top, cutLeft, cutRight),
            };
            bodyParts = frontOnly
                ? new[]
                {
                    FootRenderer("UpperBody"),
                    FootRenderer("LeftEquipment"),
                    FootRenderer("RightEquipment"),
                    FootRenderer("HipCloth"),
                }
                : new[] { FootRenderer("UpperBody"), FootRenderer("LeftEquipment"), FootRenderer("RightEquipment") };
            leftFoot = FootRenderer("LeftFoot");
            rightFoot = FootRenderer("RightFoot");
            rightFoot.flipX = true;
            for (int d = 0; d < 2; d++)
            {
                if (frontOnly && d == 0 && !defenseRear)
                    continue;

                var source = frames[d, 0];
                var key = source;
                if (!boots.TryGetValue(key, out var boot) || boot == null)
                {
                    // Rear and front drawings have different leg columns. The rear leg is
                    // x40..46; x44..50 retained only its thin edge plus part of the bent leg.
                    int height = d == 0 ? (defenseRear ? 22 : 23) : bootHeight;
                    boot = CoreGuardianSpriteAtlasSources.Cut(
                        source,
                        new Rect(d == 0 ? (defenseRear ? 43 : 40) : bootX, 0, d == 0 ? 7 : bootWidth, height),
                        new Vector2(.5f, 3f / height),
                        source.pixelsPerUnit
                    );
                    boot.name = source.name + "_Boot";
                    boot.hideFlags = HideFlags.HideAndDontSave;
                    boots[key] = boot;
                }
                templates[d] = boot;
            }
        }

        SpriteRenderer FootRenderer(string name)
        {
            var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(body.transform, false);
            renderer.sharedMaterial = body.sharedMaterial;
            return renderer;
        }

        static Rect[] BodyRects(Sprite source, int top, int left, int right) =>
            new[]
            {
                new Rect(0, top, source.rect.width, source.rect.height - top),
                new Rect(0, 0, left, top),
                new Rect(right, 0, source.rect.width - right, top),
            };

        static Rect[] FrontBodyRects(Sprite source, int top, int left, int right) =>
            new[]
            {
                new Rect(0, top + 10, source.rect.width, source.rect.height - top - 10),
                new Rect(0, 0, left, top + 10),
                new Rect(right, 0, source.rect.width - right, top + 10),
                new Rect(left, top, right - left, 10),
            };

        static Sprite[] BodyWithoutFixedLegs(Sprite source, Rect[] rects, Sprite rest)
        {
            if (bodies.TryGetValue(source, out var found) && found[0] != null)
                return found;

            var sprites = new Sprite[rects.Length];
            for (int i = 0; i < rects.Length; i++)
            {
                // A few source frames raise a fixed knee into the hem. Keep the
                // resting hip/cloth strip so it cannot appear as a third knee.
                var art = i == 3 ? rest : source;
                sprites[i] = CoreGuardianSpriteAtlasSources.Cut(art, rects[i], new Vector2(.5f, 0), art.pixelsPerUnit);
                sprites[i].name = source.name + "_Body" + i;
                sprites[i].hideFlags = HideFlags.HideAndDontSave;
            }
            bodies[source] = sprites;
            return sprites;
        }

        public void Present(Sprite source, int direction, float phase, bool walking)
        {
            bool split = !frontOnly || direction == 1 || defenseRear;
            foreach (var part in bodyParts)
            {
                part.gameObject.SetActive(split);
            }

            leftFoot.gameObject.SetActive(split);
            rightFoot.gameObject.SetActive(split);
            if (!split)
            {
                body.enabled = true;
                return;
            }
            body.enabled = false;
            var rects = bodyRects[direction];
            bool coherent = coherentFront && direction == 1 || defenseRear && direction == 0;
            var rest = direction == 0 ? rearRest : frontRest;
            // Torso and hip armour must move as one pose. Mixing a changing chest
            // with a fixed lower strip breaks the waist silhouette on the boss.
            var bodySource = coherent ? rest : source;
            var sprites = BodyWithoutFixedLegs(bodySource, rects, rest);
            float pelvis = coherent && walking ? -.75f * (1 - Mathf.Cos(phase * Mathf.PI * 4)) * .5f : 0;
            for (int i = 0; i < bodyParts.Length; i++)
            {
                var part = bodyParts[i];
                part.sprite = sprites[i];
                part.transform.localPosition =
                    (new Vector3(rects[i].center.x, rects[i].y + pelvis, 0) - (Vector3)source.pivot)
                    / source.pixelsPerUnit;
                part.color = body.color;
                part.sortingLayerID = body.sortingLayerID;
                part.sortingOrder = body.sortingOrder + 1;
            }
            leftFoot.sprite = rightFoot.sprite = templates[direction];
            float Lift(float p) => walking ? Mathf.Max(0, Mathf.Sin(p * Mathf.PI * 2)) * 3.5f : 0;
            leftFoot.transform.localPosition =
                new Vector3(defenseRear && direction == 0 ? -4 : leftX, Lift(phase), 0) / source.pixelsPerUnit;
            rightFoot.transform.localPosition =
                new Vector3(defenseRear && direction == 0 ? 5 : rightX, Lift(phase + .5f), 0) / source.pixelsPerUnit;
            CopyPresentation(leftFoot);
            CopyPresentation(rightFoot);
        }

        void CopyPresentation(SpriteRenderer foot)
        {
            foot.color = body.color;
            foot.sortingLayerID = body.sortingLayerID;
            foot.sortingOrder = body.sortingOrder;
        }
    }
}
