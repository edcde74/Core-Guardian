# 포탑 설치 집중 화면·라운드 시작 정리 — 2026-10-15

복구 기준 `e36e42c`. 수정 전 추적 파일 변경 없음. 사용자 AudioProduction/·밸런스/BGM 미커밋 자료 보존.

## 변경

- 기존 `CancelInstall`은 미리보기/설치 모드만 해제하고 HUD의 `installOpen`을 닫지 않았다. HUD Refresh도 전투 단계에서 설치 패널을 닫지 않아 팝업이 남았다.
- 수동 라운드 시작/준비 시간 만료 시 설치 패널을 닫는다. 준비 단계 밖에서는 설치 패널을 열지 못하고, 남아 있는 설치 모드는 컨트롤러에서 정리한다. 미리보기/설치 표시 타일 제거·커서 복구 유지.
- 설치 모드 중 HUD 직계 영역에 CanvasGroup을 적용해 상단 중앙 TopStatus/StagePlate/BossPlate와 왼쪽 CancelPlacement만 남긴다. 나머지는 alpha0, interactable=false, blocksRaycasts=false로 표시·클릭·키보드 선택을 막는다. 숨긴 HUD가 설치 위치를 가로막지 않는다.
- activeSelf/접기·펼치기·선택 플래그를 강제로 바꾸지 않는다. 이전 CanvasGroup alpha/interactable/blocksRaycasts를 보존하고 설치 종료에 복원한다. 주기적인 HUD Refresh도 설치 중에는 숨김을 유지한다.
- 정상 설치 성공 후 이전 HUD와 설치 선택 목록을 다시 표시한다. 새 포탑 정보는 기존 선택 동작에 따라 표시한다. ESC/왼쪽 취소 버튼은 기존 흐름대로 설치 선택 목록도 닫고 다른 HUD를 복원한다. 잘못된 위치 설치 실패는 설치 모드/숨김을 유지한다.
- `CancelInstall`은 실제 설치 모드를 해제했을 때 Changed로 HUD에 즉시 알린다. 성공 설치도 설치 해제 후 완료 알림을 보내 UI가 이전 모드로 남지 않는다.

## 검증

- 신규 회귀4개: 수동 시작 시 열린 팝업 닫기, 설치 집중 화면/취소 상태 복원, 설치 실패 유지/성공 복원, 준비 만료 시 모드·팝업·미리보기 정리.
- [수정 전](red.json): 전체14개 중 기존10개 통과/신규4개 실제 실패. 초기 필터 실행0개는 성공으로 쓰지 않았고 [등록 확인](test_inventory.json) 후 전체 실행으로 재현했다.
- [수정 후 PlayMode](playmode.json): **14/14 completed**. [전체 EditMode](editmode.json): **78/78 completed**. 기존 전투/애니메이션/HUD/증강 유지. CanvasGroup 이전 값, 접힌 피해 패널, 열린 강화 패널, 상태바 접힘과 커서 복원까지 검사했다.
- 실제 Play/source=screen, 1600×900: [선택 전](before.png) → [설치 중](focus.png) → [취소](cancel.png), [설치 완료](install.png) → [라운드 시작](combat.png). 상태 JSON도 같은 폴더에 보존했다. UI 확인용 자체 Play/정상 구매를 사용했으며 밸런스 실측·사람 체감 승인으로 쓰지 않는다.
- 직접 시작한 검증 Play만 종료/TimeScale1 복구. 캡처 도구가 Assets/docs에 생성한 이번 임시 이미지는 문서로 복사 후 Unity AssetDatabase로 제거했다. 씬/원본 프리팹/아트/CSV/노션 밸런스 변경 없음. GitHub push 없음.

최신 빌드 결과와 종료 상태는 후속 기록을 확인한다.
