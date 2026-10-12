# 자료 탐색

기본 프로젝트: `C:/Users/edcde/OneDrive/Desktop/CD`. 다른 프로젝트 경로가 주어지면 그 인수인계부터 읽는다. 현재 위치·상태·수치는 최신 자료에서 재확인한다.

| 담당 | 노션 URL |
|---|---|
| HUD 우선순위·표시·접기 | https://www.notion.so/3df389dbfc68811f9a87e5a5ff05a571 |
| PC 조작 | https://www.notion.so/3ea389dbfc688108b75cf8b9bbc99cb3 |
| 게임 루프·행동 가능 단계 | https://www.notion.so/3e6389dbfc68819fbfe5ca17f14460d3 |
| 플레이어 전투·이동 | https://www.notion.so/3df389dbfc6881f8be26ff810ba7c18e |
| 플레이어 성장 | https://www.notion.so/3e6389dbfc68811283ffe9b73d0f0da8 |
| 타워·강화·지원 | https://www.notion.so/3df389dbfc6881a2b037fbce63f2f534 |
| 증강 | https://www.notion.so/3e6389dbfc6881bf9600d5f42fb68a44 |
| 피해 계산 | https://www.notion.so/3ea389dbfc6881919546c09d4ae7ade6 |
| 효과·타이포그래피 | https://www.notion.so/3f4389dbfc688118965ecb7a1ffaaf3c |
| 레벨·보드 | https://www.notion.so/3e9389dbfc688159ae8cec214b4e3e25 |

직접 페이지를 fetch하고 필요한 하위 원자료만 읽는다. 잘림·누락 블록·접근 실패를 확인하며 마지막 편집일만으로 규칙 승인을 판단하지 않는다. 기획 이미지의 임시 카드/수치를 현재 기능으로 오인하지 않는다. 각 문서의 담당 규칙은 연결하고 상세 규칙을 다른 문서에 중복 작성하지 않는다.

실행 탐색 출발점:

- `Assets/Scripts/CoreGuardianHud.cs`: Canvas 생성·패널·앵커·텍스트·강화/피해표시·접기.
- `Assets/Scripts/CoreGuardianPrototype.cs`: HUD 생성 호출·Notify·선택/클릭/취소·이동 연결.
- `Assets/Scripts/CoreGuardianCombatArt.cs`: 명중·처치·코어 효과·골드 표시.
- `Assets/Game/Scripts/CoreGuardian/{CombatData,Session}.cs`: 실제 능력치·피해·강화·허용 단계·누적 집계·시간 기록 존재 여부.
- 현재 씬과 프리팹: 인수인계에서 경로를 찾고 Unity API로 읽는다. 프리팹이 없을 수 있으므로 런타임 생성 코드와 구별한다.
- `Assets/docs/development/`와 인수인계 연결 날짜별 캡처/기록: 실제 촬영 해상도·날짜·역할검증/자연전투 여부를 읽는다. 과거 화면에 현재 코드 근거를 함께 붙인다.

산출물은 `docs/development/hud_ux_analysis_<date>/` 같은 Assets 밖 새 폴더에 저장한다. 게임 테스트를 위해 HUD를 임의로 생성하거나 기존 씬을 바꾸지 않는다.
