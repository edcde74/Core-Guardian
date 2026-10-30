using System;
using System.Collections.Generic;
using System.Threading;
using CoreGuardian;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class CoreGuardianAugmentationView
    : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler
{
    public bool Revealed { get; private set; }
    public bool Busy { get; private set; }
    Button button;
    RectTransform body;
    GameObject back,
        front;
    Image rim;
    Text title,
        description;
    AugmentationCard card;
    Action select;
    Func<bool> allowed;
    bool hover;

    // Flip and reveal sparks run on LitMotion (unscaled time) and UniTask (2026-10-28); hiding the card stops both.
    readonly List<MotionHandle> motions = new List<MotionHandle>();
    CancellationTokenSource sparkCancel;
    static readonly float[] FlipDurations = { .44f, .66f, .92f };
    static readonly Color[] colors = { new Color(.76f, .83f, .9f), new Color(1, .76f, .28f), new Color(.5f, .94f, 1) };

    public void Configure(AugmentationCard value, Font font, Action chosen, Func<bool> canChoose)
    {
        card = value;
        select = chosen;
        allowed = canChoose;
        button = GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        GetComponent<Image>().color = new Color(0, 0, 0, 0);
        button.onClick.AddListener(Click);
        body = Rect("CardBody", transform, Vector2.zero, GetComponent<RectTransform>().sizeDelta);
        rim = body.gameObject.AddComponent<Image>();
        CoreGuardianHudSkin.Frame(rim, "Disabled");
        rim.color = colors[(int)card.Grade];
        rim.raycastTarget = false;
        back = Rect("Back", body, Vector2.zero, body.sizeDelta - new Vector2(16, 16)).gameObject;
        var art = back.AddComponent<Image>();
        art.sprite = CoreGuardianHudSkin.Sprite("CardBack" + card.Grade);
        art.preserveAspect = true;
        art.raycastTarget = false;
        front = Rect("Front", body, Vector2.zero, body.sizeDelta - new Vector2(48, 48)).gameObject;
        float width = ((RectTransform)front.transform).rect.width;
        // Card front: grade frame + card icon (32px art ×3, 10-30 사용자 요청) + name + text.
        var icon = Rect("Icon", front.transform, Vector2.zero, new Vector2(96, 96));
        icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(.5f, 1);
        icon.anchoredPosition = new Vector2(0, -16);
        var iconImage = icon.gameObject.AddComponent<Image>();
        iconImage.sprite = CoreGuardianHudSkin.Sprite("Augments/augment_" + card.Number.ToString("00"));
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        iconImage.enabled = iconImage.sprite != null;
        title = Label(
            "Title",
            front.transform,
            Resources.Load<Font>("CoreGuardian/Fonts/Galmuri11-Bold").OrNull() ?? font,
            new Vector2(14, -116),
            new Vector2(width - 28, 44),
            26
        );
        title.text = card.Title;
        description = Label("Effect", front.transform, font, new Vector2(14, -166), new Vector2(width - 28, 178), 20);
        description.text = card.Description;
        Label("Choose", front.transform, font, new Vector2(14, -354), new Vector2(width - 28, 34), 18).text =
            "다시 클릭하여 선택";
        front.SetActive(false);
    }

    public void Click()
    {
        if (Busy || allowed == null || !allowed())
            return;
        if (Revealed)
        {
            select();
            return;
        }
        Flip();
    }

    void Flip()
    {
        Busy = true;
        float duration = FlipDurations[(int)card.Grade];
        bool changed = false;
        PlaySound();
        motions.Add(
            LMotion
                .Create(0f, 1f, duration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithImmediateBind()
                .WithOnComplete(() =>
                {
                    body.localScale = Vector3.one;
                    Revealed = true;
                    Busy = false;
                })
                .Bind(p =>
                {
                    float eased = p * p * (3 - 2 * p);
                    body.localScale = new Vector3(
                        Mathf.Max(.025f, Mathf.Abs(Mathf.Cos(eased * Mathf.PI))),
                        1 + Mathf.Sin(p * Mathf.PI) * .035f,
                        1
                    );
                    if (!changed && p >= .5f)
                    {
                        changed = true;
                        Revealed = true;
                        back.SetActive(false);
                        front.SetActive(true);
                        if (sparkCancel == null)
                        {
                            sparkCancel = new CancellationTokenSource();
                        }

                        Sparks(sparkCancel.Token).Forget();
                    }
                })
        );
    }

    async UniTaskVoid Sparks(CancellationToken cancel)
    {
        int count = SparkCounts[(int)card.Grade];
        for (int batch = 0; batch < (card.Grade == AugmentationGrade.Platinum ? 2 : 1); batch++)
        {
            for (int i = 0; i < (batch == 0 ? count : 20); i++)
            {
                Ray(i);
            }

            if (
                await UniTask
                    .Delay(
                        TimeSpan.FromSeconds(.17f),
                        Cysharp.Threading.Tasks.DelayType.Realtime,
                        PlayerLoopTiming.Update,
                        cancel
                    )
                    .SuppressCancellationThrow()
            )
            {
                return;
            }
        }
    }

    void Ray(int index)
    {
        float seed = index * 2.399963f;
        int edge = index % 4;
        Vector2 dir =
            edge == 0 ? Vector2.left
            : edge == 1 ? Vector2.right
            : edge == 2 ? Vector2.up
            : Vector2.down;
        float along = Mathf.Sin(seed);
        Vector2 half = GetComponent<RectTransform>().rect.size * .5f;
        Vector2 start =
            edge < 2
                ? new Vector2(dir.x * half.x, along * (half.y - 10))
                : new Vector2(along * (half.x - 10), dir.y * half.y);
        var img = RentSpark();
        var rt = img.rectTransform;
        rt.anchoredPosition = start;
        rt.sizeDelta = new Vector2(3, 10 + (index % 4) * 5);
        rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90);
        float life = .3f + (index % 5) * .055f;
        float distance = 30 + ((int)card.Grade) * 30;
        var tint = colors[(int)card.Grade];
        motions.Add(
            LMotion
                .Create(0f, 1f, life)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithImmediateBind()
                .WithOnComplete(() => ReturnSpark(img))
                .Bind(p =>
                {
                    rt.anchoredPosition = start + dir * distance * p;
                    img.color = Color.Lerp(Color.white, tint, p) * new Color(1, 1, 1, 1 - p);
                })
        );
    }

    // F21: reveal sparks are reused per card (up to 104 per reveal); a resting one is renamed so only live sparks
    // are called "RevealSpark".
    static readonly int[] SparkCounts = { 14, 30, 52 };
    static readonly string[] GradeCues = { "aug_silver", "aug_gold", "aug_platinum" };
    const string SparkName = "RevealSpark",
        RestingSparkName = "RevealSpark (pooled)";
    readonly Stack<Image> sparkPool = new Stack<Image>();

    Image RentSpark()
    {
        while (sparkPool.Count > 0)
        {
            var pooled = sparkPool.Pop();
            if (pooled != null)
            {
                pooled.name = SparkName;
                pooled.rectTransform.localRotation = Quaternion.identity;
                pooled.gameObject.SetActive(true);
                return pooled;
            }
        }

        var rt = Rect(SparkName, transform, Vector2.zero, Vector2.zero);
        var img = rt.gameObject.AddComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    void ReturnSpark(Image img)
    {
        if (img == null)
            return;

        img.gameObject.SetActive(false);
        img.name = RestingSparkName;
        sparkPool.Push(img);
    }

    void PlaySound()
    {
        GetComponentInParent<CoreGuardianCombatAudio>().OrNull()?.PlayCue(GradeCues[(int)card.Grade]);
    }

    void Update()
    {
        if (body != null && !Busy)
        {
            body.localScale = Vector3.Lerp(
                body.localScale,
                hover ? new Vector3(1.025f, 1.025f, 1) : Vector3.one,
                Time.unscaledDeltaTime * 18
            );
            rim.color = colors[(int)card.Grade] * (hover ? 1.18f : 1);
        }
    }

    void OnDisable()
    {
        foreach (var motion in motions)
        {
            motion.TryCancel();
        }

        motions.Clear();
        if (sparkCancel != null)
        {
            sparkCancel.Cancel();
            sparkCancel.Dispose();
            sparkCancel = null;
        }

        Busy = false;
        if (body != null)
        {
            body.localScale = Vector3.one;
            foreach (Transform child in transform)
            {
                if (child.name == SparkName)
                {
                    ReturnSpark(child.GetComponent<Image>());
                }
            }
        }
        hover = false;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        hover = true;
    }

    public void OnPointerExit(PointerEventData e)
    {
        hover = false;
    }

    public void OnSelect(BaseEventData e)
    {
        hover = true;
    }

    public void OnDeselect(BaseEventData e)
    {
        hover = false;
    }

    static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        return r;
    }

    static Text Label(string name, Transform parent, Font font, Vector2 pos, Vector2 size, int points)
    {
        var r = Rect(name, parent, pos, size);
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
        var t = r.gameObject.AddComponent<Text>();
        t.font = font;
        t.fontSize = points;
        t.color = new Color(1, .95f, .84f);
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        t.raycastTarget = false;
        return t;
    }
}
