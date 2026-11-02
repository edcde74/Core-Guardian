using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// HUD 개편 B안 (2026-10-28 사용자 확정): 왼쪽 아래 고정 선택 정보창, 받은 증강 세로 줄 + Tab 목록,
// 범위 폭발 아이콘 버튼(시계 방향 덮개, 클릭 사용), 누적 피해 스크롤바. 아트는 Hud/Redesign·Hud/Augments.
public sealed partial class CoreGuardianHud
{
    InfoSubject infoSubject;
    EnemyHandle infoEnemy;
    RectTransform infoPanel;
    Image infoIcon;
    Text infoTitle;
    Button infoDestroy;

    bool InfoBlocked => game.Installing.HasValue || game.Session.Phase == SessionPhase.Augmentation;

    RectTransform augmentStrip,
        augmentTooltip,
        augmentList,
        augmentGrid;
    Text augmentTooltipText,
        augmentListTitle,
        augmentListDetail,
        augmentMoreLabel;
    RectTransform augmentMore;
    readonly AugmentSlot[] stripSlots = new AugmentSlot[6];
    readonly List<AugmentSlot> listSlots = new List<AugmentSlot>();
    bool augmentListPinned;
    int augmentListSignature = -1;
    Dictionary<string, AugmentSpec> augmentById;

    bool AugmentListWanted =>
        (augmentListPinned || Keyboard.current != null && Keyboard.current.tabKey.isPressed)
        && !placementFocused
        && game != null
        && game.Ready;

    // ---------------- area blast icon ----------------
    Image blastCooldown;
    Text blastTimer;

    void UpdateRedesignFrame()
    {
        UpdateBlastCooldown();
        UpdateTeleportCooldown();
        if (augmentList != null && augmentList.gameObject.activeSelf != AugmentListWanted)
        {
            RefreshAugmentList();
        }
    }

    // ---------------- hit numbers: 약점 크게·강조색 / 저항 작게·회색 ----------------
    static readonly Color WeakNumberColor = new Color(1f, .55f, .22f),
        ResistNumberColor = new Color(.66f, .68f, .7f);

    // 치명타(D6): 노란색 #FFD63C + !, 일반 숫자의 2배 (10-26 아트 확정, 10-28 사용자 재확인).
    static readonly Color CritNumberColor = new Color(1f, .839f, .235f);
    const float CritNumberScale = 2f;
}
