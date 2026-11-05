using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CoreGuardian;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;

// CoreGuardianPerfOverlay: overlay UI and text.
public sealed partial class CoreGuardianPerfOverlay
{
    // ---------- overlay ----------
    void SetVisible(bool value)
    {
        visible = value;
        if (visible && canvasRoot == null)
        {
            BuildUi();
        }

        if (canvasRoot != null)
        {
            canvasRoot.SetActive(visible);
        }

        display.Clear();
        displayTimer = 0;
        skipFrames = 2;
        UpdateSampling();
        if (visible)
        {
            RefreshText();
        }
    }

    void BuildUi()
    {
        canvasRoot = new GameObject(
            "PerfOverlayCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler)
        );
        canvasRoot.transform.SetParent(transform, false);
        var canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        var scale = canvasRoot.GetComponent<UnityEngine.UI.CanvasScaler>();
        scale.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scale.referenceResolution = new Vector2(1920, 1080);
        scale.matchWidthOrHeight = .5f;
        // No GraphicRaycaster: the overlay never blocks clicks.
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        var rect = panel.GetComponent<RectTransform>();
        rect.SetParent(canvasRoot.transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-16, -8);
        rect.sizeDelta = new Vector2(320, 130);
        // Same frame as the HUD panels (a plain translucent Image was nearly invisible here).
        var image = panel.GetComponent<UnityEngine.UI.Image>();
        CoreGuardianHudSkin.Frame(image, "Panel");
        image.raycastTarget = false;
        var text = new GameObject("Text", typeof(RectTransform), typeof(UnityEngine.UI.Text));
        var textRect = text.GetComponent<RectTransform>();
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14, 8);
        textRect.offsetMax = new Vector2(-12, -9);
        label = text.GetComponent<UnityEngine.UI.Text>();
        var font = Resources.Load<Font>("CoreGuardian/Fonts/Galmuri11");
        label.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 14;
        label.lineSpacing = 1.05f;
        label.color = new Color(.92f, 1f, .9f);
        label.alignment = TextAnchor.UpperLeft;
        label.raycastTarget = false;
        label.supportRichText = true;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
    }

    void RefreshText()
    {
        if (label == null)
            return;

        int n = recentCount;
        Array.Copy(recent, recentSorted, n);
        Array.Sort(recentSorted, 0, n);
        float low = LowFps(recentSorted, n, .01f);
        var w = display;
        sb.Clear();
        sb.Append("FPS ")
            .Append(w.Frames > 0 ? w.Fps.ToString("0", Inv) : "-")
            .Append("   1% low ")
            .Append(low > 0 ? low.ToString("0", Inv) : "-")
            .Append('\n');
        sb.Append("프레임 ")
            .Append(w.AvgMs.ToString("0.0", Inv))
            .Append("ms   최대 ")
            .Append(w.Max.ToString("0.0", Inv))
            .Append("ms\n");
        sb.Append("CPU ")
            .Append(Ms(w.AvgCpu))
            .Append("  렌더 ")
            .Append(Ms(w.AvgRender))
            .Append("  GPU ")
            .Append(Ms(w.AvgGpu))
            .Append(" ms\n");
        sb.Append("GC ")
            .Append(w.GcKb >= 0 ? w.GcKb.ToString("0.0", Inv) + "KB/프레임" : "-")
            .Append("   메모리 ")
            .Append(MemoryMb().ToString("0", Inv))
            .Append("MB\n");
        if (ReadGame(out int wave, out var phase, out int enemies, out int towers, out int speed))
        {
            sb.Append("적 ")
                .Append(enemies)
                .Append("  포탑 ")
                .Append(towers)
                .Append("  라운드 ")
                .Append(wave)
                .Append("  ×")
                .Append(speed)
                .Append('\n');
        }
        else
        {
            sb.Append("전투 정보 없음\n");
        }

        sb.Append(QualitySettings.vSyncCount > 0 ? "VSync 켬" : "<color=#7fd0ff>제한 없음</color>").Append("(F5)  ");
        if (recording)
        {
            int sec = (int)(Time.realtimeSinceStartup - recordStartTime);
            sb.Append("<color=#ff6a6a>REC ")
                .Append((sec / 60).ToString("00", Inv))
                .Append(':')
                .Append((sec % 60).ToString("00", Inv))
                .Append("</color>(F4)");
        }
        else if (notice != null && Time.unscaledTime < noticeUntil)
        {
            sb.Append(notice);
        }
        else
        {
            sb.Append("기록 F4");
        }

        label.text = sb.ToString();
    }
}
