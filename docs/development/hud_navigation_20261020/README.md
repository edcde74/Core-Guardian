# WASD HUD 탐색 차단

2026-10-20 사용자 보고: 하단 버튼 클릭 후 WASD 이동 시 UI 선택 강조가 방향에 따라 이동한다.

원인: CoreGuardianHud.Configure가 InputSystemUIInputModule을 생성하지만 EventSystem의 기본 sendNavigationEvents=true를 유지한다. 기본 UI Move 입력이 플레이어 이동 키와 함께 처리되어 선택한 버튼에서 이웃 버튼으로 포커스를 옮긴다.

수정: HUD 구성 시 현재 EventSystem.sendNavigationEvents=false. UI 방향 탐색/Submit/Cancel 이벤트만 차단하며 포인터 Hover/클릭, 플레이어 WASD 입력, 별도 Escape 설치 취소 코드는 유지한다. 씬 YAML/아트/밸런스/음량 변경 없음.

검증: CLI PlayMode SharedUpgradeHudKeepsPanelTabsAndStatCellsFixed 1/1 통과(playmode.xml). 실제 씬 HUD 구성 시 탐색 꺼짐 단언과 기존 플레이어/포탑 탭·구매·고정 패널 회귀를 확인했다. 실제 물리 키 입력 체감 검사는 아직 하지 않았다. 처음 연결된 에디터가 없어 CLI 테스트를 실행했고, 완료 후 사용자가 새로 연결한 에디터는 별도 검사 없이 읽기 조회만 했다. 최종 Play stopped/컴파일 false/실패 false/오류0·경고112, 전투씬1 dirtyfalse, TimeScale1/captureDelta0.

복구 기준27d1d89. 기존 PackageManagerSettings·미추적·font_tools_vendor 접근오류 표시는 보존한다. 이전 Windows_20261020_30R 실행파일에는 이 후속 수정이 포함되지 않는다. 이번 수정에서는 새 빌드/실행 없음.
