using CoreGuardian;
using UnityEngine;
using UnityEngine.UI;

// CoreGuardianHud: 배속 버튼 · 설정 창 · 개발 도구 숨김 (2026-11-04 사용자 결정).
// - 배속 버튼은 아래 조작 줄의 라운드 시작 오른쪽. 빨리 감기 그림 + '×1/×2/×3' 글자. 전투 중에만 빨라짐(기존 규칙 그대로).
// - 설정 창은 오른쪽 위 버튼으로 연다: 음량 · 다시 시작 · 게임 종료. 창을 열어도 게임은 멈추지 않는다.
// - 다시 시작·게임 종료는 '정말 할까요?'를 한 번 묻는다.
// - Esc: 포탑 설치 중이면 취소, 아니면 설정 창 여닫기(확인 창이 떠 있으면 확인 창만 닫음).
// - 개발 도구 버튼은 에디터·빌드 모두 평소 숨김. Ctrl+; 로 보이기/숨기기.
public sealed partial class CoreGuardianHud
{
    enum SettingsAsk
    {
        None,
        Restart,
        Quit,
    }

    bool settingsOpen,
        developerUnlocked;
    SettingsAsk settingsAsk;
    GameObject settingsPanel,
        settingsConfirmView,
        developerMenuView;

    public bool SettingsOpen => settingsOpen;
    public bool DeveloperUnlocked => developerUnlocked;

    void PlayUiCue(string cue) => game.GetComponent<CoreGuardianCombatAudio>().OrNull()?.PlayCue(cue);

    RectTransform IconButton(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, string icon, System.Action action)
    {
        var r = Box(name, parent, anchor, position, size);
        CoreGuardianHudSkin.Frame(r.GetComponent<Image>(), "Button");
        var b = r.gameObject.AddComponent<Button>();
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        b.onClick.AddListener(() => action());
        float side = Mathf.Min(size.x, size.y) - 20;
        CoreGuardianHudSkin.Icon(r, icon, new Vector2((size.x - side) / 2, -(size.y - side) / 2), new Vector2(side, side));
        r.gameObject.AddComponent<CoreGuardianHudReaction>();
        return r;
    }

    // 아래 조작 줄: 포탑 설치 · 라운드 시작 · [배속] · 범위 폭발 · (순간 이동)
    void BuildSpeedButton(RectTransform controls)
    {
        var r = Box("TestSpeed", controls, new Vector2(0, 1), new Vector2(468, -8), new Vector2(76, 72));
        CoreGuardianHudSkin.Frame(r.GetComponent<Image>(), "Button");
        var b = r.gameObject.AddComponent<Button>();
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        b.onClick.AddListener(() =>
        {
            PlayUiCue("ui_click");
            game.CycleTestSpeed();
            Refresh();
        });
        CoreGuardianHudSkin.Icon(r, "Speed", new Vector2(14, -6), new Vector2(48, 44));
        var label = Text("Key", r, new Vector2(0, -50), new Vector2(76, 18), 14);
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(1, .95f, .84f);
        r.gameObject.AddComponent<CoreGuardianHudReaction>();
        int shown = 0;
        refresh.Add(() =>
        {
            if (shown == game.TestSpeed)
                return;
            shown = game.TestSpeed;
            label.text = "×" + shown;
        });
    }

    void BuildSettings()
    {
        IconButton("Settings", canvas, new Vector2(1, 1), new Vector2(-16, -16), new Vector2(64, 64), "Settings", ToggleSettings);

        var panel = Box("SettingsPanel", canvas, new Vector2(1, 1), new Vector2(-16, -88), new Vector2(380, 296));
        settingsPanel = panel.gameObject;
        CoreGuardianHudSkin.Icon(panel, "Settings", new Vector2(16, -14), new Vector2(28, 28));
        Text("Title", panel, new Vector2(54, -12), new Vector2(240, 32), 22).text = "설정";
        IconButton("SettingsClose", panel, new Vector2(0, 1), new Vector2(316, -10), new Vector2(48, 48), "Cancel", CloseSettings);

        // 음량: 개발 도구 쪽 음량 줄과 같은 값(CoreGuardianSoundSettings.Volume)을 쓴다.
        var soundIcon = CoreGuardianHudSkin.Icon(panel, "Sound", new Vector2(16, -66), new Vector2(36, 36));
        var volumeLabel = Text("VolumeLabel", panel, new Vector2(62, -62), new Vector2(300, 28), 20);
        var track = Box("SettingsVolume", panel, new Vector2(0, 1), new Vector2(62, -96), new Vector2(298, 32), false);
        var hit = track.gameObject.AddComponent<Image>();
        hit.color = Color.clear; // 얇은 줄도 잡기 쉽게 높이 전체를 누를 수 있게
        var background = Box("Track", track, new Vector2(0, .5f), Vector2.zero, new Vector2(298, 8), false)
            .gameObject.AddComponent<Image>();
        background.color = new Color(.12f, .16f, .18f);
        background.raycastTarget = false;
        var fillArea = Box("FillArea", track, new Vector2(0, .5f), Vector2.zero, new Vector2(298, 8), false);
        var fill = Box("Fill", fillArea, new Vector2(0, .5f), Vector2.zero, Vector2.zero, false).gameObject.AddComponent<Image>();
        fill.color = new Color(.24f, .72f, .66f);
        fill.raycastTarget = false;
        var handleArea = Box("HandleArea", track, new Vector2(0, .5f), new Vector2(9, 0), new Vector2(280, 28), false);
        var handle = Box("Handle", handleArea, new Vector2(0, .5f), Vector2.zero, new Vector2(18, 0), false)
            .gameObject.AddComponent<Image>();
        handle.color = new Color(.83f, .72f, .44f);
        var slider = track.gameObject.AddComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.SetValueWithoutNotify(CoreGuardianSoundSettings.Volume);
        slider.onValueChanged.AddListener(value =>
        {
            CoreGuardianSoundSettings.Volume = value;
            Refresh();
        });

        Button(
            "SettingsRestart",
            panel,
            new Vector2(16, -150),
            new Vector2(348, 56),
            () => "다시 시작",
            () => AskSettings(SettingsAsk.Restart)
        );
        Button("SettingsQuit", panel, new Vector2(16, -222), new Vector2(348, 56), () => "게임 종료", () => AskSettings(SettingsAsk.Quit));

        // '정말 할까요?' 확인 창: 설정 창 위를 덮어 아래 버튼을 못 누르게 한다.
        var confirm = Box("SettingsConfirm", panel, new Vector2(0, 1), Vector2.zero, new Vector2(380, 296));
        settingsConfirmView = confirm.gameObject;
        var question = Text("Question", confirm, new Vector2(16, -56), new Vector2(348, 96), 22);
        question.alignment = TextAnchor.MiddleCenter;
        Button("ConfirmYes", confirm, new Vector2(16, -222), new Vector2(166, 56), () => "확인", ConfirmSettings);
        Button(
            "ConfirmNo",
            confirm,
            new Vector2(198, -222),
            new Vector2(166, 56),
            () => "취소",
            () =>
            {
                settingsAsk = SettingsAsk.None;
                Refresh();
            }
        );

        int volumeShown = -1;
        refresh.Add(() =>
        {
            if (developerMenuView != null)
            {
                SetHudActive(developerMenuView, developerUnlocked);
            }
            SetHudActive(settingsPanel, settingsOpen);
            SetHudActive(settingsConfirmView, settingsOpen && settingsAsk != SettingsAsk.None);
            if (!settingsOpen)
                return;

            SetHudText(question, settingsAsk == SettingsAsk.Quit ? "게임을 종료할까요?" : "처음부터 다시 시작할까요?");
            float volume = CoreGuardianSoundSettings.Volume;
            int percent = Mathf.RoundToInt(volume * 100);
            if (percent != volumeShown)
            {
                volumeShown = percent;
                volumeLabel.text = "전체 음량  " + percent + "%";
                soundIcon.sprite = CoreGuardianHudSkin.Sprite(percent <= 0 ? "SoundMute" : "Sound");
            }
            slider.SetValueWithoutNotify(volume);
        });
    }

    public void ToggleSettings()
    {
        if (settingsOpen && settingsAsk != SettingsAsk.None)
        {
            settingsAsk = SettingsAsk.None; // 확인 창이 떠 있으면 확인 창만 닫는다
            PlayUiCue("ui_close");
            Refresh();
            return;
        }
        settingsOpen = !settingsOpen;
        settingsAsk = SettingsAsk.None;
        PlayUiCue(settingsOpen ? "ui_open" : "ui_close");
        Refresh();
    }

    void CloseSettings()
    {
        if (!settingsOpen)
            return;

        settingsOpen = false;
        settingsAsk = SettingsAsk.None;
        PlayUiCue("ui_close");
        Refresh();
    }

    void AskSettings(SettingsAsk ask)
    {
        settingsAsk = ask;
        Refresh();
    }

    void ConfirmSettings()
    {
        var ask = settingsAsk;
        settingsAsk = SettingsAsk.None;
        settingsOpen = false;
        if (ask == SettingsAsk.Restart)
        {
            game.Restart();
        }
        else if (ask == SettingsAsk.Quit)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        Refresh();
    }

    // Ctrl+; : 개발 도구 버튼 보이기/숨기기. 숨길 때는 열린 개발 도구도 닫는다.
    public void ToggleDeveloperUnlocked()
    {
        developerUnlocked = !developerUnlocked;
        if (!developerUnlocked)
        {
            developerOpen = false;
        }
        Refresh();
    }
}
