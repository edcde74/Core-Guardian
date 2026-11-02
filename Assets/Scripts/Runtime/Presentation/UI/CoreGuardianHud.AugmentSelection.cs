using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// CoreGuardianHud: augmentation card selection screen.
public sealed partial class CoreGuardianHud
{
    void BuildAugmentationSelection()
    {
        // 피드백 7: 하단 조작 바(높이 88, 아래 12) 바로 위.
        var bossPlate = Box("BossPlate", canvas, new Vector2(.5f, 0), new Vector2(0, 112), new Vector2(740, 60));
        CoreGuardianHudSkin.Icon(bossPlate, "Damage", new Vector2(16, -15), new Vector2(30, 30));
        bossStatus = Text("BossStatus", bossPlate, new Vector2(54, -14), new Vector2(666, 32), 22);
        refresh.Add(() => bossPlate.gameObject.SetActive(!string.IsNullOrEmpty(bossStatus.text)));
        bossStatus.color = new Color(1f, .6f, .45f);
        // 10-31 사용자 요청: 보스 체력 줄은 강화 창(연구소·작업대)보다 아래에 그린다.
        if (playerPanel != null && towerPanel != null)
        {
            bossPlate.SetSiblingIndex(Mathf.Min(playerPanel.GetSiblingIndex(), towerPanel.GetSiblingIndex()));
        }
        augmentationPanel = Box(
            "AugmentationSelection",
            canvas,
            new Vector2(.5f, .5f),
            Vector2.zero,
            new Vector2(1920, 1080)
        );
        augmentationPanel.anchorMin = Vector2.zero;
        augmentationPanel.anchorMax = Vector2.one;
        augmentationPanel.sizeDelta = Vector2.zero;
        var shade = augmentationPanel.GetComponent<UnityEngine.UI.Image>();
        shade.sprite = null;
        shade.color = new Color(.015f, .025f, .04f, .82f);
        var panel = Box("Cards", augmentationPanel, new Vector2(.5f, .5f), new Vector2(0, -30), new Vector2(1140, 710));
        cardsRoot = panel;
        CoreGuardianHudSkin.Icon(panel, "Augment", new Vector2(24, -24), new Vector2(30, 30));
        Text("Title", panel, new Vector2(66, -18), new Vector2(700, 44), 30).text = "증강 1개 선택";
        // 10-30: 다시 뽑기 버튼은 카드마다 아래에 따로 있다. 최대 4장(47번)이라 4개를 미리 만들고 장 수만큼 보인다.
        for (int i = 0; i < cardRerollButtons.Length; i++)
        {
            int index = i;
            var reroll = Button(
                "AugmentReroll",
                panel,
                new Vector2(24 + i * 370, -634),
                new Vector2(350, 56),
                () => "다시 뽑기 " + game.Session.RerollsLeftFor(index) + "회",
                () =>
                {
                    if (game.Session.RerollAugmentation(index))
                    {
                        Refresh();
                    }
                },
                () => game.Session.Phase == SessionPhase.Augmentation && game.Session.RerollsLeftFor(index) > 0
            );
            reroll.name = "AugmentReroll" + i;
            reroll.gameObject.SetActive(false);
            cardRerollButtons[i] = reroll;
            augmentationButtons.Add(reroll);
        }
        // The timer and input shield survive folding; hiding CanvasGroups preserves reveal coroutines.
        var timerPlate = Box(
            "AugmentTimerPlate",
            augmentationPanel,
            new Vector2(.5f, 1),
            new Vector2(0, -100),
            new Vector2(1140, 92)
        );
        augmentationTimer = Text("ChoiceTimer", timerPlate, new Vector2(20, -12), new Vector2(1100, 68), 21);
        var foldRect = SmallHandle(
            "AugmentFold",
            augmentationPanel,
            () =>
            {
                cardsFolded = !cardsFolded;
                SetCardsVisible();
            },
            true
        );
        foldRect.anchorMin = foldRect.anchorMax = foldRect.pivot = new Vector2(.5f, 1);
        foldRect.anchoredPosition = new Vector2(0, -192);
        var fold = foldRect.GetComponent<UnityEngine.UI.Button>();
        augmentationButtons.Add(fold);
        augmentationPanel.gameObject.SetActive(false);
        topDetails.SetAsLastSibling();
    }

    // Cards shown per slot: a reroll replaces only its own card, so the other cards keep their reveal state.
    string[] shownCardIds = Array.Empty<string>();
    UnityEngine.UI.Button[] cardButtons = Array.Empty<UnityEngine.UI.Button>();
    readonly UnityEngine.UI.Button[] cardRerollButtons = new UnityEngine.UI.Button[4];

    void RefreshCards()
    {
        var choices = game.Session.AugmentationChoices;
        string signature = string.Join("|", choices.Select(x => x.Id));
        if (choiceSignature == signature)
            return;

        bool sameScreen = choiceSignature != null && shownCardIds.Length == choices.Count;
        choiceSignature = signature;
        int count = choices.Count;
        if (!sameScreen)
        {
            foreach (var b in augmentationButtons.Where(b => b.name.StartsWith("AugmentationCard")).ToArray())
            {
                augmentationButtons.Remove(b);
                b.gameObject.SetActive(false);
                Destroy(b.gameObject);
            }
            cardsFolded = false;
            SetCardsVisible();
            // 47 넓은 선택 shows four cards; the panel widens instead of shrinking the cards.
            float width = Mathf.Max(1140, 28 + count * 370);
            cardsRoot.sizeDelta = new Vector2(width, cardsRoot.sizeDelta.y);
            shownCardIds = new string[count];
            cardButtons = new UnityEngine.UI.Button[count];
            for (int i = 0; i < cardRerollButtons.Length; i++)
            {
                cardRerollButtons[i].gameObject.SetActive(i < count);
            }
        }

        for (int i = 0; i < count; i++)
        {
            if (shownCardIds[i] == choices[i].Id)
                continue;

            if (cardButtons[i] != null)
            {
                augmentationButtons.Remove(cardButtons[i]);
                cardButtons[i].gameObject.SetActive(false);
                Destroy(cardButtons[i].gameObject);
            }
            int index = i;
            var r = Box(
                "AugmentationCard" + i,
                cardsRoot,
                new Vector2(0, 1),
                new Vector2(24 + i * 370, -164),
                new Vector2(350, 460)
            );
            var b = r.gameObject.AddComponent<UnityEngine.UI.Button>();
            augmentationButtons.Add(b);
            r.gameObject.AddComponent<CoreGuardianAugmentationView>()
                .Configure(
                    choices[i],
                    font,
                    () =>
                    {
                        if (game.Session.ChooseAugmentation(index))
                        {
                            game.Notify(game.Session.LastMessage);
                            Refresh();
                        }
                    },
                    () => game.Session.Phase == SessionPhase.Augmentation
                );
            cardButtons[i] = b;
            shownCardIds[i] = choices[i].Id;
        }
    }
}
