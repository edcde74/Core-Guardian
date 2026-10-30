using System;
using UnityEngine;

// 실행 파일은 1920x1080 창 화면으로 시작 (2026-10-30 사용자 요청). Player settings already default to this,
// but Windows remembers the last fullscreen choice per game, so the start-up forces it once.
// Explicit command-line screen options (-screen-fullscreen, -popupwindow) still win. Editor play is untouched.
public static class CoreGuardianWindowMode
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply()
    {
        if (Application.isEditor)
            return;

        foreach (var arg in Environment.GetCommandLineArgs())
        {
            if (arg == "-screen-fullscreen" || arg == "-popupwindow")
                return;
        }

        Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
    }
}
