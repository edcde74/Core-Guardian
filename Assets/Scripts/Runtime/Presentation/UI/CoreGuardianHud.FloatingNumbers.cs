using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// CoreGuardianHud: pooled floating damage/gold/core numbers.
public sealed partial class CoreGuardianHud
{
    sealed class DamageNumber
    {
        public string Kind;
        public TextMeshProUGUI Text;
        public RectTransform Root;
        public UnityEngine.UI.Image Icon;
        public Vector2 Offset;
        public Vector3 World;
        public float Started;
        public int Lane;
    }

    void ShowDamageNumber(EnemySnapshot enemy, int amount)
    {
        // 치명타: 노란색 + !, 2배 (약점·저항보다 우선, 10-28 확정). 약점: 크게·강조색 / 저항: 작게·회색 (포탑 공격만).
        bool crit = game.Session.HitCritical;
        int affinity = crit ? 0 : game.Session.HitAffinity;
        var number = FloatingNumber(
            "HitDamage",
            null,
            game.World(enemy.Position) + Vector3.up * .95f,
            crit ? CritNumberColor
                : affinity > 0 ? WeakNumberColor
                : affinity < 0 ? ResistNumberColor
                : new Color(1f, .94f, .68f),
            nextDamageLane++ % 3 - 1
        );
        // F6: digits go straight into TMP's character buffer; no string per hit.
        SetNumberText(number.Text, amount, crit ? '!' : '\0');
        if (crit)
        {
            number.Root.localScale = Vector3.one * CritNumberScale;
        }
        else if (affinity != 0)
        {
            number.Root.localScale = Vector3.one * (affinity > 0 ? 1.3f : .8f);
        }
    }

    public void ShowGoldNumber(Vector3 position, int amount, Sprite icon)
    {
        if (amount <= 0)
        {
            return; // 졸개 등 처치 골드 0은 표시하지 않음
        }

        var number = FloatingNumber("GoldNumber", "+" + amount, position, new Color(1f, .76f, .22f), 0);
        var text = number.Text;
        float textWidth = Mathf.Ceil(text.GetPreferredValues(text.text).x);
        number.Root.sizeDelta = new Vector2(40 + textWidth, 52);
        text.rectTransform.sizeDelta = new Vector2(textWidth, 52);
        number.Icon.sprite = icon;
    }

    public UnityEngine.UI.Graphic[] CoreDamageGraphics() =>
        new UnityEngine.UI.Graphic[]
        {
            topDetails.Find("Symbol_Core").GetComponent<UnityEngine.UI.Image>(),
            topDetails.Find("Status_Core").GetComponent<UnityEngine.UI.Text>(),
        };

    public void ShowCoreNumber(Vector3 position, int amount)
    {
        var number = FloatingNumber("CoreDamageNumber", "-" + amount, position, new Color(1f, .36f, .32f), 0);
        number.Text.alignment = TextAlignmentOptions.Left;
        number.Text.rectTransform.pivot = new Vector2(0, .5f);
    }

    DamageNumber FloatingNumber(string name, string value, Vector3 position, Color color, int lane)
    {
        var pool = NumberPool(name);
        DamageNumber number = null;
        while (pool.Count > 0 && number == null)
        {
            var candidate = pool.Pop();
            if (candidate.Root != null && candidate.Text != null)
            {
                number = candidate;
            }
            else if (candidate.Root != null)
            {
                Destroy(candidate.Root.gameObject);
            }
        }
        if (number == null)
        {
            number = CreateFloatingNumber(name);
        }

        number.World = position;
        number.Started = Time.time;
        number.Lane = lane;
        if (value != null)
        {
            number.Text.text = value;
        }
        number.Text.color = color;
        if (number.Icon != null)
        {
            number.Icon.color = Color.white;
        }

        number.Root.localScale = Vector3.one;
        number.Root.localRotation = Quaternion.identity;
        number.Root.SetAsLastSibling();
        number.Root.gameObject.SetActive(true);
        damageNumbers.Add(number);
        return number;
    }

    readonly char[] numberChars = new char[16];

    // Same characters as amount.ToString() (+ suffix) for the invariant digits a hit amount uses.
    void SetNumberText(TextMeshProUGUI text, int amount, char suffix)
    {
        int length = 0;
        long value = amount;
        if (value < 0)
        {
            numberChars[length++] = '-';
            value = -value;
        }
        int start = length;
        do
        {
            numberChars[length++] = (char)('0' + value % 10);
            value /= 10;
        } while (value > 0);
        System.Array.Reverse(numberChars, start, length - start);
        if (suffix != '\0')
        {
            numberChars[length++] = suffix;
        }
        text.SetCharArray(numberChars, 0, length);
    }

    Stack<DamageNumber> NumberPool(string kind) =>
        kind == "GoldNumber" ? goldNumberPool
        : kind == "CoreDamageNumber" ? coreNumberPool
        : hitNumberPool;

    void PrepareDamageNumbers()
    {
        using var scope = prepareDamageNumbersSample.Auto();
        // Resolve TMP's synchronous first-use settings load before combat callbacks.
        if (TMP_Settings.instance == null)
        {
            throw new InvalidOperationException("TMP Settings missing");
        }
        // The observed first volley creates three numbers. This only warms that initial
        // demand; FloatingNumber retains its unrestricted expansion and existing lifetime.
        for (int i = hitNumberPool.Count; i < 3; i++)
        {
            var number = CreateFloatingNumber("HitDamage");
            number.Text.color = Color.clear;
            number.Text.text = "0123456789";
            number.Text.ForceMeshUpdate();
            ReturnNumber(number);
        }
    }

    DamageNumber CreateFloatingNumber(string name)
    {
        using var scope = createDamageNumberSample.Auto();
        var rect = Box(name, worldFeedback, new Vector2(.5f, .5f), Vector2.zero, new Vector2(200, 52), false);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = damageFont;
        text.fontSharedMaterial = damageMaterial;
        text.fontSize = 32;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        var number = new DamageNumber
        {
            Kind = name,
            Text = text,
            Root = rect,
        };
        if (name == "GoldNumber")
        {
            var group = Box(
                "GoldFeedback",
                worldFeedback,
                new Vector2(.5f, .5f),
                Vector2.zero,
                new Vector2(40, 52),
                false
            );
            rect.SetParent(group, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, .5f);
            rect.anchoredPosition = new Vector2(40, 0);
            text.alignment = TextAlignmentOptions.Left;
            var symbol = Box("GoldGain", group, new Vector2(0, .5f), Vector2.zero, new Vector2(36, 36), false)
                .gameObject.AddComponent<UnityEngine.UI.Image>();
            symbol.preserveAspect = true;
            symbol.raycastTarget = false;
            number.Root = group;
            number.Icon = symbol;
            number.Offset = new Vector2(0, 24);
        }
        return number;
    }

    void ReturnNumber(DamageNumber number)
    {
        if (number.Root == null)
            return;

        number.Root.gameObject.SetActive(false);
        var pool = NumberPool(number.Kind);
        if (!disposingNumbers && pool.Count < MaxRetainedNumbersPerKind)
        {
            pool.Push(number);
        }
        else
        {
            Destroy(number.Root.gameObject);
        }
    }

    public void ClearDamageNumbers()
    {
        foreach (var number in damageNumbers)
        {
            ReturnNumber(number);
        }

        damageNumbers.Clear();
        nextDamageLane = 0;
    }

    void PrepareNumbersDuringPreparation()
    {
        if (
            game == null
            || !game.Ready
            || game.Session.Phase != SessionPhase.Preparation
            || Time.timeScale <= 0
            || Time.timeSinceLevelLoad < 1f
        )
        {
            return;
        }

        if (hitNumberPool.Count >= PreparationHitNumbers || disposingNumbers)
            return;

        using var scope = prepareDamageNumbersSample.Auto();
        for (int i = 0; i < PreparationNumbersPerFrame && hitNumberPool.Count < PreparationHitNumbers; i++)
        {
            var number = CreateFloatingNumber("HitDamage");
            number.Text.color = Color.clear;
            number.Text.text = "0123456789";
            number.Text.ForceMeshUpdate();
            ReturnNumber(number);
        }
    }

    void DestroyNumberPool(Stack<DamageNumber> pool)
    {
        while (pool.Count > 0)
        {
            var number = pool.Pop();
            if (number.Root != null)
            {
                Destroy(number.Root.gameObject);
            }
        }
    }
}
