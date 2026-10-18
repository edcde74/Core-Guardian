# 보스 등장음① 적용

2026-10-18. 사용자 ‘1번’ 선택으로 RPG Essentials의 `10_Battle_SFX/55_Encounter_02.wav`를 보스 첫 월드 표시 생성에 연결했다. 시작 HEAD342fa70. 관련 게임 파일은 변경 없어 빈 복구 커밋 없이 기준으로 사용했다. 시작부터 존재한 병행 애니메이션/그림자 변경 및 미추적 자료는 보존한다.

## 연결

- AssetDatabase API로 `Assets/Resources/CoreGuardian/Audio/BossSpawn/01_Encounter.wav`에 원본을 복사했다. 원본/복사본 SHA256 모두 `1D20C58985FF62A7441FE6F4600410180BA0ED2582F73E93C0E54F957060A7A9`.5.333333초·PCM/DecompressOnLoad/preload·무편집/비루프/고정피치1.
- Prototype.SynchronizeViews의 보스 새 표시 생성 시 NotifyBossSpawn(Id)를 호출한다. 일반 적/보스 가속과 별도이며 같은 보스는 화면 갱신·Disable/Enable·같은 Session.Configure로 재재생하지 않는다. 다른 Session으로 변경될 때만 보스ID 집합을 비운다.
- 음소거 중 등장도 알림 완료로 기록해 나중에 늦게 재생하지 않는다. 기존4음성/gain0.55/공통음량 유지. 풀이 가득 차면 코어 피해음이 아닌 음성 하나를 교체한다. 전부 코어 피해음이면 보스음을 생략한다. 코어 피해음은 기존 우선권을 유지한다.
- 기존 공격3종/피격③/처치②/코어⑥/지원무음·멀티묶음·개발도구·게임수치/BGM을 보존한다. 무료/상업사용 조건은 [동일 팩 공식 조회](../core_damage_audio_auditions_20261018.md)를 따른다.

## 확인

- 신규 Play 검사가 Selected boss spawn audio is missing으로 실패하는 것을 확인 후 구현했다. 테스트 파일 갱신 직후 eval 통신 오류가 한 번 있었으나 재조회에서 컴파일완료/stopped 및 다른 검사의 완료 상태를 확인했다. 다른 채팅의 검사결과146/146은 이번 사운드 검증 결과로 사용하지 않았다.
- 실제 러너 **Edit3/3·Play5/5 통과**: [Edit](editmode.json), [Play](playmode.json). 새 검사는 전투씬 실제 컴포넌트·진단 Session.DebugSpawn과 실제 SynchronizeViews 호출 기능QA다. 정상10R 스케줄을 진행하거나 사람이 직접 청취한 검사는 아니다.
- 일반 적 무음/보스 표시 생성 시1회·동일ID재생방지·같은Session.Configure/Disable/Enable 반복방지·음소거 중 등장 지연재생없음·다른보스재생·풀포화 시코어음보존·음량/비루프/고정피치를 확인했다. 기존 실제 공격/피격/처치/코어 검사도 통과했다.
- 종료Playfalse/pausedfalse/compilingfalse·컴파일실패false·전투씬1dirtyfalse·TimeScale1/captureDelta0·저장음량0.0681817755(약6.8%)시작/종료동일: [최종조회](final_state.json).
- 현재콘솔오류0/경고0와 누적보관로그는 구분한다: [콘솔](console.json). 씬YAML/CSV/프로젝트설정·빌드/실행파일/push 변경 없음. 병행 작업은 커밋 대상에서 제외하고 보존한다.

종료 Git 조회에서는 작업 중 별도로 생긴 `ProjectSettings/PackageManagerSettings.asset` 변경도 확인했다. 사운드 작업에서 수정한 파일이 아니며 되돌리거나 이번 커밋에 포함하지 않는다. 위 씬/설정 변경 없음은 사운드 작업의 수정 범위에 대한 설명이다.

사용자 음색선택·등장 연결기능QA 완료. 정상 다수전투 믹스/전체회귀/성능/사람플레이·밸런스 검증은 아니다. 다음 구체 사운드는 미정이다.
