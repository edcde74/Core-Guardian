# B안 코어 피해 반응 적용과 숫자 위치 수정

2026-10-16 (Asia/Seoul). 사용자가 B ‘흰색 짧게 → 붉은색’을 선택하고 코어 감소 숫자를 코어 오른쪽 위 빈칸에 빨간색/몬스터 피해와 같은 폰트로 표시하도록 지시했다. 복구 기준 커밋 `1e0e12e`에 앞선 시안과 재개 점검 문서를 저장한 뒤 구현했다.

## 현재 구현

- `CoreGuardianCoreFeedback`이 기존 몸 피격 셰이더의 tint/amount를 별도 재질·property block으로 사용한다. 첫45ms 흰색55% → 붉은색48% 감쇠,본체240ms·상단 코어 아이콘/수치300ms 복귀,추가 플래시 재점등 제한300ms. 원래 재질/property block/UI 색·알파를 복원하며 원본 sprite·색·Transform·그림자를 변경하지 않는다. 비활성/초기화/파괴 시 정리한다.
- 기존 CoreEnergy 감소 감지→`CoreGuardianCombatArt.CoreDamaged()` 연결을 재사용한다. CoreHit/감소 숫자는 피해마다 기존대로 호출하고 새 플래시만 제한한다. 피해/수급/전투 수치에 관여하지 않는다.
- 현재 씬의 실제 코어 `2D Sprite` 경계를 기준으로 오른쪽 .2월드단위·위 .12월드단위 밖에 숫자 기준점을 잡는다. 텍스트를 왼쪽 정렬·왼쪽 피벗으로 해 문자열이 길어져도 코어 중앙으로 침범하지 않게 한다. 빨간색(1,.36,.32),기존850ms/상승 곡선 유지.
- 몬스터 피해와 **동일한 FloatingNumber 경로/PixelifySansBoldDamage TMP 폰트/외곽선 재질/32크기**를 사용한다. 기존 코어 숫자도 이미 같은 폰트였으며 이번에는 위치·정렬을 수정했다.
- 몸 피격90ms·기존 B 효과·사운드·원본 아트/씬/프리팹/CSV는 변경하지 않았다. 저체력 반복 경고·흔들림·사운드 추가 없음. 별도 플레이어 방향 작업도 수정/커밋하지 않았다.

## 이번에 직접 실행한 검사

- 새 EditMode 검사: 미구현으로1/1실패([red.json](red.json)) → 구현 후1/1통과([focused_editmode.json](focused_editmode.json)). 흰색→붉은색,재점등 거부,본체/상단 복귀,기존property block/알파 보존·정리를 확인했다.
- 새 실제 씬 PlayMode 검사1/1통과. DebugSpawn으로 적을 경로 끝에 놓고 실제 Session.Tick/Prototype.Update의 코어 도착·감소 경로를 실행했다. 기존 CoreHit 유지,감소량·상단 반응,숫자 빨간색·동일 폰트·코어 오른쪽/위 screen 위치를 검사했다. 기능 격리 검사이며 정상 플레이/밸런스 계측이 아니다.
- 전체 **PlayMode17/17 completed**: [playmode.json](playmode.json). 위 새 검사 포함.
- 전체 **EditMode98건 중96통과·2실패**: [editmode.json](editmode.json). 코어/기존 몸 피격/보행은 통과. 아래 두 실패는 시작 전부터 존재한 미추적 `CoreGuardianPlayerFacingTests.cs`의 병행 작업 검사이며,이번에 구현·수정·제외하지 않았다. 전체 회귀 통과로 표현하지 않는다.
  - `EightDirectionsChangeVisibleArtwork_WithoutRotatingTheSourceOrShadow`: direction-specific walking art null.
  - `WalkingBothLegsContinuesWhileAiming_AndRealFireImmediatelyUsesThatPose`: expected not null.
- 구현·검사 전 Unity ready/Play stopped/compiling=false/전투 씬active·loaded·dirty=false 확인. 구현 후 재컴파일 completed/failed=false 확인. Git diff --check 통과.

## 마지막 화면 확인과 연결 제한

위 검사 후 수동 화면 캡처용으로 이 작업에서 Play를 시작했다. 이후 eval/스크린샷/editor_status/editor_stop이 각각60초 timeout이었고 실제 화면 파일을 확보하지 못했다. eval 내부의 TimeScale0/피해 생성이 실행됐는지도 결과가 없어 확정하지 않는다. 강제 프로세스 종료·씬 저장·사용자 작업 복구를 하지 않았다.

사용자가 ‘플레이모드 꺼놨다’고 알려줬다. 재조회에서도 MCP editor_status가 timeout,CLI `unity status`는 CD/pid8636/ready를 반환했지만 `editor_status --timeout 5`는 timeout이었다. 따라서 **사용자 Play 종료 보고와 최종 API 상태 미확인을 구분**한다. 캡처를 직접 확인했다거나 최종 Play/TimeScale/compiling/dirty가 확인됐다고 말하지 않는다. 다음 재개 시 연결과 실제 화면부터 확인한다. 화면상의 가독성/연속 피해 체감 최종 승인은 남아 있다.

빌드·실행파일 실행·GitHub push·Notion 변경 없음. 기존 실행파일에는 미반영이다. 앞선 `preview.html`/GIF는 선택 전 비교 자료이며 숫자 위치/글꼴은 이번 Unity 구현 결과와 구분한다.
