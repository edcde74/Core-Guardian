# 보스 가속 전환음 ① 적용

2026-10-18. 사용자 ‘1’ 선택으로 무료 RPG Essentials의 `8_Buffs_Heals_SFX/48_Speed_up_02.wav`를 보스 가속 전환에 연결했다. 시작 HEAD `af83f59`. 관련 게임 변경이 없어 빈 복구 커밋 없이 기준으로 사용했다. 기존 병행 애니메이션 3파일과 PackageManagerSettings 변경은 그대로 보존했다.

## 현재 연결

- AssetDatabase API로 `Assets/Resources/CoreGuardian/Audio/BossAcceleration/01_SpeedUp.wav`에 원본을 복사했다. 길이 4초, PCM/DecompressOnLoad/preload, 비루프/고정피치1, 무편집. 원본/복사본 SHA256은 모두 `DEC16CC731A187C2A183EA771589FB551ECFB5C8D006E593ADD5813CA1686EFB`다.
- 현재 EnemyState.Enraged는 보스 Health<=Spec.Health/2f 판정이다. DamageApplied 후 살아 있는 보스의 첫 가속 상태를 같은 판정으로 감지한다. 별도 게임 규칙/속도/체력/CSV를 바꾸지 않았다.
- 가속 전환 타격에서는 보통 피격음 대신 ①을 재생한다. 체력이0이 된 타격은 처치음 경로가 먼저 처리하므로 가속음이 없다. 보스ID별 한 번이며 같은 세션 재연결/Disable/Enable/추가 피격으로 반복하지 않는다. 다른 Session으로 바뀔 때만 기록을 비운다.
- 음소거 중 전환도 완료로 기록해 나중에 늦게 재생하지 않는다. 기존4음성 풀/gain0.55/공통음량을 사용하고 포화 시 코어 피해음이 아닌 음성을 교체한다. 모든 음성이 코어음이면 생략한다.
- 보스 등장①/공격3종/피격③/처치②/코어⑥/지원무음과 멀티 묶음, 개발도구, BGM을 유지했다. 무료/상업사용 조건은 [동일 팩 공식 조회](../core_damage_audio_auditions_20261018.md)를 따른다.

## 이번 검증

- 새 검사에서 Selected boss acceleration audio is missing 실패를 확인 후 구현했다.
- 실제 러너 **Edit3/3·Play6/6 통과**: [Edit](editmode.json), [Play](playmode.json).
- 새 검사는 실제 전투씬 컴포넌트/진단Session의 보스 Health 주입과 ApplyDamage 직접 호출 기능QA다. 50%보다 위에서 무음, 경계 아래에서 한 번, 기존1.3배 속도 판정 유지, 일반 피격음 대체, 같은 세션 재연결/추가피해 반복 없음, 치명타격 제외, 음소거 중 전환의 지연재생 없음, 다른 보스 전환, 포화 시 코어음 보호와 음량을 확인했다.
- 기존 실제 발사/지원무음/피격/처치/코어/보스 등장 검사도 통과했다. 정상10R 진행/다수전투 녹음·믹스/직접청취/사람플레이/성능/전체회귀·밸런스 검증은 아니다.
- 종료 Playfalse/pausedfalse/compilingfalse, 컴파일실패false, 전투씬1dirtyfalse, TimeScale1/captureDelta0, 저장음량 시작/종료0.0681817755(약6.8%) 동일: [최종조회](final_state.json).
- 현재 콘솔 오류0/경고0이며 누적 보관 로그와 구분한다: [콘솔](console.json). 자체 Play/임시음량 복원, 씬YAML/빌드/실행파일/push 없음. 병행 변경/미추적 자료/font_tools_vendor 접근오류 표시는 보존한다.

다음 구체 사운드는 미정이다. 현재 음색 선택과 연결 기능QA를 정상 다수전투 믹스 승인으로 확대하지 않는다.
