using CoreGuardian;
using UnityEngine;
using UnityEngine.UI;

// CoreGuardianHud: 46 순간 이동 버튼(Shift, 10-30). 범위 폭발 버튼 오른쪽, 46번을 받았을 때만 보인다.
// 대기 표시는 범위 폭발과 같다: 대기 중 아이콘 회색 + 큰 숫자, 다 차면 한 번 반짝 + ui_open 소리.
public sealed partial class CoreGuardianHud
{
    // 11-04: 배속 버튼이 들어가 84씩 넓어짐.
    const float ControlsWidth = 640,
        ControlsWidthWithTeleport = 724;
    RectTransform bottomControls;
    Button teleportButton;
    Image teleportIcon,
        teleportCooldown,
        teleportGlow;
    Text teleportTimer;
    bool teleportWasCooling;
    float teleportFlashUntil;
    int teleportSecondsShown = int.MinValue;

    void BuildTeleportButton(RectTransform controls)
    {
        bottomControls = controls;
        var r = Box("Teleport", controls, new Vector2(0, 1), new Vector2(636, -8), new Vector2(76, 72));
        CoreGuardianHudSkin.Frame(r.GetComponent<Image>(), "Button");
        var b = r.gameObject.AddComponent<Button>();
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        b.onClick.AddListener(() =>
        {
            if (!b.interactable)
                return;
            game.UseTeleport();
            Refresh();
        });
        teleportIcon = CoreGuardianHudSkin.Icon(r, "Teleport", new Vector2(14, -6), new Vector2(48, 44));
        teleportGlow = Box("ReadyGlow", r, new Vector2(0, 1), Vector2.zero, new Vector2(76, 72), false)
            .gameObject.AddComponent<Image>();
        CoreGuardianHudSkin.Frame(teleportGlow, "Hover");
        teleportGlow.raycastTarget = false;
        teleportGlow.color = new Color(1, .86f, .45f, 0);
        teleportCooldown = Box("Cooldown", r, new Vector2(0, 1), new Vector2(14, -6), new Vector2(48, 44), false)
            .gameObject.AddComponent<Image>();
        teleportCooldown.sprite = teleportIcon.sprite;
        teleportCooldown.preserveAspect = true;
        teleportCooldown.raycastTarget = false;
        teleportCooldown.color = new Color(0, 0, 0, .68f);
        teleportCooldown.type = Image.Type.Filled;
        teleportCooldown.fillMethod = Image.FillMethod.Radial360;
        teleportCooldown.fillOrigin = (int)Image.Origin360.Top;
        teleportCooldown.fillClockwise = false;
        teleportCooldown.fillAmount = 0;
        teleportTimer = Text("Timer", r, new Vector2(0, -6), new Vector2(76, 44), 30);
        teleportTimer.alignment = TextAnchor.MiddleCenter;
        var outline = teleportTimer.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, .95f);
        outline.effectDistance = new Vector2(2, -2);
        var key = Text("Key", r, new Vector2(0, -50), new Vector2(76, 18), 12);
        key.alignment = TextAnchor.MiddleCenter;
        key.text = "Shift";
        key.color = new Color(1, .95f, .84f);
        r.gameObject.AddComponent<CoreGuardianHudReaction>();
        refresh.Add(() =>
        {
            bool owned = game.Session.Augments.Has(46);
            SetHudActive(r.gameObject, owned);
            var size = new Vector2(owned ? ControlsWidthWithTeleport : ControlsWidth, bottomControls.sizeDelta.y);
            if (bottomControls.sizeDelta != size)
            {
                bottomControls.sizeDelta = size;
            }
            b.interactable = game.Session.TeleportReady;
        });
        teleportButton = b;
    }

    void UpdateTeleportCooldown()
    {
        if (teleportCooldown == null || game == null || !game.Ready || !teleportButton.gameObject.activeSelf)
            return;

        var s = game.Session;
        float total = s.TeleportCooldownTotal;
        bool cooling = total > 0 && s.TeleportCooldown > 0;
        float fill = cooling ? Mathf.Clamp01(s.TeleportCooldown / total) : 0;
        if (!Mathf.Approximately(teleportCooldown.fillAmount, fill))
        {
            teleportCooldown.fillAmount = fill;
        }
        var iconColor = cooling ? new Color(.42f, .42f, .42f) : Color.white;
        if (teleportIcon.color != iconColor)
        {
            teleportIcon.color = iconColor;
        }
        if (s.TeleportReady && teleportWasCooling)
        {
            teleportFlashUntil = Time.unscaledTime + BlastFlashSeconds;
            teleportButton.GetComponent<CoreGuardianHudReaction>().OrNull()?.Confirm();
            game.GetComponent<CoreGuardianCombatAudio>().OrNull()?.PlayCue("ui_open");
        }
        teleportWasCooling = cooling;
        float glow = Mathf.Clamp01((teleportFlashUntil - Time.unscaledTime) / BlastFlashSeconds);
        if (!Mathf.Approximately(teleportGlow.color.a, glow))
        {
            teleportGlow.color = new Color(1, .86f, .45f, glow);
        }
        int seconds = cooling ? Mathf.CeilToInt(s.TeleportCooldown) : -1;
        if (seconds != teleportSecondsShown || teleportTimer.text == null)
        {
            teleportSecondsShown = seconds;
            SetHudText(teleportTimer, seconds >= 0 ? seconds.ToString() : "");
        }
    }
}
