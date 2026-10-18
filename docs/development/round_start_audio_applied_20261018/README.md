# 라운드 전투 시작음 ③ 적용

2026-10-18. 사용자 ‘3’ 선택으로 Kenney Interface Sounds `confirmation_004.ogg`를 준비→전투 성공 전환에 연결했다. 시작 HEAD7c0f0a5, 관련 게임 변경이 없어 빈 복구 커밋 없이 기준으로 사용했다. 기존 PackageManagerSettings 변경은 보존했다.

## 구현

- 원본 `AudioProduction/CoreGuardian_Upgrade_20261018/Kenney_Interface/Originals/Audio/confirmation_004.ogg`를 에디터API로 복사/임포트해 `Assets/Resources/CoreGuardian/Audio/RoundStart/03_Confirmation004.ogg`로 연결했다.
- 원본/복사본 SHA256 `568967A3D9F8A8F6AF54EA01729C4882284308F2A27D78C07FFD7EE0D6951661` 동일.0.490408초·PCM/DecompressOnLoad/preload·무편집/비루프/고정피치1. [CC0/상업사용·원본팩 기록](../../../AudioProduction/CoreGuardian_Upgrade_20261018/Kenney_Interface/README.md).
- Session.StartCombat 성공 지점에 CombatStarted 알림 이벤트만 추가했다. 기존 수동 시작과 준비시간 만료가 공통 성공 경로를 사용한다. 준비 시작/완료/결과/카운트다운에는 연결하지 않았다. 전투 중 재시작 요청은 기존 false 반환으로 무음이다. 라운드/준비시간/재화/수치/게임규칙 변경 없음.
- Audio.Configure 중복호출/Disable/Enable 구독해제로 중복 재생을 방지한다. 지난 시작을 저장해 재생하는 방식이 아니어서 음소거 해제나 재연결 때 늦게 재생하지 않는다. 진단 시작에도 같은 성공 이벤트를 사용한다.
- 기존4음성/gain0.55/공통음량·풀포화시코어음보호를 유지한다. 기존 공격/피격/처치/보스/설치/강화/지원무음과 개발도구/BGM 보존.

## 이번 검증

- 신규 Play 검사에서 Selected round start audio is missing 실패 확인 후 구현했다. 기존 실제발사 검사의 PlayedCount는 이제 시작 알림을 제외한 발사 증가분으로 비교해 여전히 단일1+멀티2대각1을 검증한다.
- 실제 러너 **Edit3/3·Play10/10 통과**: [Edit](editmode.json), [Play](playmode.json).
- 새 검사는 전투씬 실제 컴포넌트/진단 Session에서 Prototype.StartCombat 직접호출 및 실제 PreparationRemaining만큼 Session.Tick 수동진행 기능QA다. 정상 사람 버튼 입력/실시간 준비시간 대기/정상다수전투 믹스 검사는 아니다.
- 준비무음·수동성공1회·중복시작무음·같은세션Configure2회 단일구독·Disable/Enable/재연결재생없음·준비타임아웃1회·전투Tick반복무음·음소거중시작/해제후지연재생없음·다음시작재생·풀포화시코어음보호·공통음량 확인. 기존 사운드9개 집중검사도 통과했다.
- 종료Playfalse/pausedfalse/compilingfalse·컴파일실패false·전투씬1dirtyfalse·TimeScale1/captureDelta0·저장음량0.0681817755(약6.8%)시작/종료동일: [최종조회](final_state.json). 현재오류0/경고0와누적보관로그구분: [콘솔](console.json).
- 자체 Play/임시 음량 복원. 씬 YAML 수정/빌드/실행파일/push 없음. 병행 자료와 font_tools_vendor 오류 삭제 표시 보존.

연결 기능QA이며 전체회귀/성능/사람청취/정상다수전투믹스·밸런스통과가 아니다. 다음 구체 사운드는 미정이다.
