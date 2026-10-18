# 몬스터 피격음③ 적용

2026-10-18. 사용자 ‘3번’ 선택으로 무료 RPG Essentials의 `12_Player_Movement_SFX/61_Hit_03.wav`를 실제 몬스터 피해 이벤트에 연결했다. 시작 HEAD6355a39, 관련 변경 없어 빈 복구 커밋 없이 기준으로 사용했다.

## 적용 범위

- 원본을 AssetDatabase API로 `Assets/Resources/CoreGuardian/Audio/MonsterHit/03_Hit.wav`에 복사했다. 원본/복사본 SHA256 `3E2EE7D0E9733404E10AE4CF9DC89AEE36AB3B4946E02F1A7FD5697C774CC08E` 동일.0.666667초·PCM/DecompressOnLoad/preload,음색/피치/길이 편집 없음.
- `CoreGuardianCombatAudio`가 Session.DamageApplied를 구독한다. 실제 양수 피해(치명 타격 포함)에만 재생한다. Configure 재호출/Disable/Enable 구독 해제로 중복을 방지한다.
- 전체 피격 계열에 unscaled0.1초 재생 간격을 두며 기존4음성 풀/gain0.55/공통 효과음 음량을 쓴다. 풀 포화 시 피격음만 생략한다. 코어 피해음 우선 재생/공격음3종/멀티 포탑별35ms 묶음/지원 무음은 보존했다.
- 공격/적 피해량·속도·보드·CSV·씬·프로젝트설정·HUD·BGM·병행 그림자 변경 없음. 무료/상업 사용 조건은 [동일 팩 공식 조회 기록](../core_damage_audio_auditions_20261018.md)에 있다.

## 새 확인

- 먼저 새 Play 검사에서 ‘Selected monster hit audio is missing’ 실패를 확인했다. 구현 후 첫 검사 요청은 스크립트 컴파일 중 연결 reset으로 실행되지 않았다. 컴파일 완료와 검사 비실행 상태를 재조회하고 실제 러너를 다시 실행했다.
- **Edit3/3·Play3/3 통과**: [Edit](editmode.json), [Play](playmode.json). 새 검사는 실제 전투 씬의 컴포넌트/진단 Session에서 ApplyDamage를 직접 호출한 기능QA다. 실제 투사체 충돌/사람 플레이/정상 다수전투 녹음 검사가 아니다.
- 양수피해/Health 감소 동일·0피해/null 무음·치명타격 재생·죽은 적 재타격 무음·0.1초 묶음·Configure2회 단일구독·풀4개 포화 때 기존 공격음 유지·음소거·Disable/Enable 구독·공통음량 반영을 확인했다. 기존 실제발사/코어도착 검사도 함께 통과했다.
- 종료Playfalse/pausedfalse/compilingfalse·컴파일실패false·전투씬1dirtyfalse·TimeScale1/captureDelta0. 저장음량 시작/종료0.0681817755(약6.8%) 동일: [조회](final_state.json).
- 현재 콘솔 오류0/경고0이며 보관 누적 카운터와 구분한다: [콘솔](console.json). 씬 저장/YAML 변경·빌드/실행파일/push 없음.

사용자 음색 선택/연결 기능검증이며 정상 다수전투 믹스/반복 청취 피로·성능·전체 회귀·밸런스 통과는 아니다. 다음 구체 사운드는 아직 미정이다.
