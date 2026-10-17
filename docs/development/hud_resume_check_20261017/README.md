# HUD 재개 확인 — 2026-10-17

## 작업 범위

사용자가 이번 채팅은 HUD 관련 작업만 담당하며 이미 작업한 아웃라인·선택 해제 정책은 무시하라고 정정했다. 해당 기능을 다시 제안하거나 변경하지 않는다. 이번에는 상태 조회와 문서 정리만 했으며 새 HUD 디자인을 선택하거나 구현하지 않았다.

기존 [능력치 A안](../hud_ability_A_20261017/README.md), [손잡이/설치 심볼 B안](../hud_handles_tower_B_20261017/README.md), [피해 HUD](../damage_hud_applied_20261017/README.md)를 유지한다. 상세 표시 규칙은 기존 [Notion HUD](https://www.notion.so/3df389dbfc68811f9a87e5a5ff05a571), 검사 기록은 [Notion 테스트](https://www.notion.so/3df389dbfc6881fc9f1bd004adfc5999)가 담당한다. 두 페이지를 재조회했으며 기존 A/B 적용과 과거 검사 이력을 확인했다. 이번에는 Notion을 수정하지 않았다.

## 실제 재개 상태

- 시작 HEAD `de15d74`. 게임 소스·Assets/Packages/ProjectSettings/AGENTS.md의 추적 파일 변경은 없었다.
- 기존 미추적 AudioProduction, 밸런스/BGM 자료, 포신 probe/캡처, 강화 피드백 시안 프레임을 보존했다. font_tools_vendor의 접근 경고와 삭제 표시369건은 기존 인수인계와 같은 상태이며 삭제 확정·stage·복원하지 않았다.
- Unity 연결 프로젝트는 CD, 버전6000.6.2f1, ready/Play stopped/paused=false/compiling=false/compilationFailed=false였다.
- 열린 씬은 `Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity` 하나, loaded=true/dirty=false였다. HDR=false/post=true, TimeScale1/captureDelta0.
- 현재 콘솔 groundTruth 오류0/경고0. 보관 버퍼의 과거 오류29/경고691은 현재 콘솔과 구분한다.
- test_status는 total0의 기존 상태를 반환했다. 이번에는 검사를 실행하지 않았으며 통과로 세지 않는다.

## 확인 범위와 다음 작업

지정 문서·현재 Inspector/HUD 소스·기존 화면을 읽고 Unity 상태를 조회했다. 새 Play·입력 주입·사람 플레이·밸런스 실측·기능 회귀·새 화면 캡처는 하지 않았다. 씬/아트/카메라/게임 코드 변경, 빌드/실행파일 실행/push 없음.

이번 사용자 정정 이후 구체적인 새 HUD 수정 항목은 아직 지정되지 않았다. 기존 승인 A/B와 피해 HUD를 재선택 대상으로 만들지 않으며, 후속 HUD 지시에서 범위를 정한다. 아웃라인·선택 정책·입자·밸런스로 임의 확장하지 않는다.
