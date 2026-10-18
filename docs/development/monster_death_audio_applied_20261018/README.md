# 몬스터 처치음② 적용

2026-10-18. 사용자 ‘2번’ 선택으로 무료 RPG Essentials의 `10_Battle_SFX/77_flesh_02.wav`를 몬스터 처치에 연결했다. 시작 HEAD44f5017·관련 변경 없음으로 빈 복구 커밋 없이 기준으로 사용했다.

## 구현

- 원본을 AssetDatabase API로 `Assets/Resources/CoreGuardian/Audio/MonsterDeath/02_Flesh.wav`에 복사했다. SHA256 원본/복사본 모두 `A1611B80C5091D97079F361B3FE198AE4C1220E30FE057C8D01334AEDDFEF4D1`.0.666667초·PCM/DecompressOnLoad/preload·무편집/고정피치1.
- DamageApplied의 양수피해 후 Health<=0이면 처치음②만 재생한다. 기존 피격음③은 살아남는 타격에서만 재생한다. 피격 쿨다운 중 발생한 처치도 독립적으로 처리하고, 처치음 전체는 unscaled0.1초로 묶는다. 코어 도착 Exited는 제외한다.
- 기존4음성 풀/gain0.55/공통효과음음량을 사용하고 풀 포화 때 처치음만 생략한다. 코어⑥우선·공격3종·멀티묶음·지원무음·개발도구를 유지한다. 피해량/처치보상/기타 게임규칙은 변경하지 않았다.
- 무료/상업사용 조건은 [동일 팩 공식 확인](../core_damage_audio_auditions_20261018.md)에 있다. 원본/씬/CSV/프로젝트설정/BGM/병행 그림자·미추적자료와 font_tools_vendor 오류 삭제표시를 보존했다.

## 검증

- 신규 검사에서 Selected monster death audio is missing 실패 확인 후 구현했다. 기존 피격 검사의 치명타격 기대를 ‘피격음 없음/처치음 전용’으로 갱신했다.
- 실제 러너 **Edit3/3·Play4/4 통과**: [Edit](editmode.json), [Play](playmode.json).
- 새 처치 검사는 전투씬 실제 컴포넌트/진단Session에서 ApplyDamage 직접호출·적위치/수동Tick 기능QA다. 정상투사체충돌/다수전투믹스/사람청취/성능/전체회귀·밸런스 검증이 아니다.
- 일반피격③→같은시점치명타격②·동일사건피격음추가없음·Health0/Gold보상동일·죽은적재피해무음·동시처치묶음·코어도착무음·풀포화/음소거/Disable/Enable·공통음량을 확인했다. 기존 실제발사/지원무음·코어피해/피격 검사도 통과했다.
- 종료Playfalse/pausedfalse/compilingfalse·컴파일실패false·전투씬1dirtyfalse·TimeScale1/captureDelta0·저장음량0.0681817755(약6.8%)시작/종료동일: [최종조회](final_state.json).
- 현재콘솔오류0/경고0이며 누적보관로그와 구분한다: [콘솔](console.json). 자체Play/임시음량복원·씬YAML수정/빌드/실행파일/push없음.

사용자 음색선택과 연결기능QA 완료. 정상 다수전투에서 피격/처치/공격의 반복청취·중첩·최종믹스는 남아 있다. 다음 구체 사운드는 미정이다.
