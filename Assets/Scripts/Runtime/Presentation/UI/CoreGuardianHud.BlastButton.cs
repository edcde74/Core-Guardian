using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// CoreGuardianHud: area blast button cooldown and damage panel scrollbar.
public sealed partial class CoreGuardianHud
{
    void BuildBlastButton(RectTransform controls)
    {
        var r = Box("AreaBlast", controls, new Vector2(0, 1), new Vector2(468, -8), new Vector2(76, 72));
        CoreGuardianHudSkin.Frame(r.GetComponent<Image>(), "Button");
        var b = r.gameObject.AddComponent<Button>();
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        b.onClick.AddListener(() =>
        {
            if (!b.interactable)
                return;
            game.UseAreaBlast();
            Refresh();
        });
        var icon = CoreGuardianHudSkin.Icon(r, "AreaBlast", new Vector2(14, -6), new Vector2(48, 44));
        blastCooldown = Box("Cooldown", r, new Vector2(0, 1), new Vector2(14, -6), new Vector2(48, 44), false)
            .gameObject.AddComponent<Image>();
        blastCooldown.sprite = icon.sprite;
        blastCooldown.preserveAspect = true;
        blastCooldown.raycastTarget = false;
        blastCooldown.color = new Color(0, 0, 0, .68f);
        blastCooldown.type = Image.Type.Filled;
        blastCooldown.fillMethod = Image.FillMethod.Radial360;
        blastCooldown.fillOrigin = (int)Image.Origin360.Top;
        blastCooldown.fillClockwise = false;
        blastCooldown.fillAmount = 0;
        blastTimer = Text("Timer", r, new Vector2(0, -6), new Vector2(76, 44), 20);
        blastTimer.alignment = TextAnchor.MiddleCenter;
        blastTimer.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
        var key = Text("Key", r, new Vector2(0, -50), new Vector2(76, 18), 12);
        key.alignment = TextAnchor.MiddleCenter;
        key.text = "Space";
        key.color = new Color(1, .95f, .84f);
        r.gameObject.AddComponent<CoreGuardianHudReaction>();
        refresh.Add(() =>
        {
            b.interactable = game.Session.AreaBlastReady;
        });
        blastButton = b;
    }

    void UpdateBlastCooldown()
    {
        if (blastCooldown == null || game == null || !game.Ready)
            return;

        var s = game.Session;
        float total = s.AreaBlast != null ? s.AreaBlast.Cooldown : 0;
        bool cooling = total > 0 && s.AreaBlastCooldown > 0;
        float fill = cooling ? Mathf.Clamp01(s.AreaBlastCooldown / total) : 0;
        if (!Mathf.Approximately(blastCooldown.fillAmount, fill))
        {
            blastCooldown.fillAmount = fill;
        }

        // F3: the seconds text is made only when the whole-second value changes.
        int seconds = cooling ? Mathf.CeilToInt(s.AreaBlastCooldown) : -1;
        if (seconds != blastSecondsShown || blastTimer.text == null)
        {
            blastSecondsShown = seconds;
            SetHudText(blastTimer, seconds >= 0 ? seconds.ToString() : "");
        }
    }

    int blastSecondsShown = int.MinValue;

    // ---------------- damage panel scrollbar ----------------
    void AddDamageScrollbar(ScrollRect scroll)
    {
        var bar = Box("Scrollbar", damagePanel, new Vector2(0, 1), new Vector2(310, -56), new Vector2(10, 308), false);
        var track = bar.gameObject.AddComponent<Image>();
        track.sprite = CoreGuardianHudSkin.Sprite("Redesign/H6_scroll_track");
        track.type = Image.Type.Sliced;
        var area = Box("SlidingArea", bar, new Vector2(0, 1), Vector2.zero, new Vector2(10, 308), false);
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.sizeDelta = Vector2.zero;
        area.anchoredPosition = Vector2.zero;
        var handle = Box("Handle", area, new Vector2(0, 1), Vector2.zero, Vector2.zero, false);
        handle.anchorMin = Vector2.zero;
        handle.anchorMax = Vector2.one;
        handle.sizeDelta = Vector2.zero;
        var handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.sprite = CoreGuardianHudSkin.Sprite("Redesign/H6_scroll_handle");
        handleImage.type = Image.Type.Sliced;
        var scrollbar = bar.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handleImage;
        scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
    }
}
