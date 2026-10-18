# 코어 피해음 ⑥ 적용

2026-10-18. 사용자 ‘6번으로 진행해’ 선택으로 `Aura_Glyph_2.wav` 원본을 코어 에너지 감소에 연결했다. 시작 HEAD198befc에 관련 게임 변경이 없어 빈 복구 커밋 없이 기준으로 사용했다. 작업 중 병행 그림자 커밋31578e5를 보존했다.

## 구현

- 원본은 그대로 보존하고 에디터 AssetDatabase API로 `Assets/Resources/CoreGuardian/Audio/CoreDamage/06_Aura_Glyph_2.wav`에 복사했다. 원본/복사본 SHA256은 모두 `F280AE85A7A4F8BD0846B8DD08DC9D015A69A2BA75E8DD9E3142B98497A59A07`. 길이1.981247초·PCM/DecompressOnLoad/preload. 잘라내기/피치/음색 편집 없음.
- 기존 Prototype.Update의 실제 코어 에너지 감소 감지에만 NotifyCoreDamage를 추가했다. 같은 프레임의 피해는 기존 감지처럼 묶고, 추가 재생은 unscaled0.3초 제한이다. 피해0에는 재생하지 않는다. 게임 모델·코어 피해량·시각 피드백은 보존한다.
- 기존4음성 풀/gain0.55/공통 효과음 음량/비루프/고정피치를 사용한다. 풀이 가득 차면 첫 음성을 중단하고 코어 알림을 우선 재생한다. 공격음3종·멀티 포탑별35ms 묶음·지원 무음은 유지한다. 개발 도구 기존3듣기/슬라이더는 변경하지 않았다.
- 현재 사용자 저장값은 시작/종료 모두0.0681817755(약6.8%)였다. 이전 약13%로 복귀하거나 기본65%로 초기화하지 않았다. 검사 임시값은 finally에서 복원했다.

## 새 검사

먼저 신규 Play 검사에서 ‘Selected core damage audio is missing’ 실패를 확인했다. 구현 후 실제 도착/풀 우선/재생 제한은 통과했으나 테스트가 기존 활성 음성의 Update 음량 반영보다 먼저 검사해 실패했다. 음량 변경 후1프레임을 기다리도록 검사 시점을 수정했다.

- 실제 러너 Edit3/3·Play2/2 통과: [Edit](editmode.json), [Play](playmode.json).
- 새 Play 검사는 진단 Session/별도 경로/적 도착 직전 위치/수동Tick과 실제 Prototype.Update 호출이다. 풀4개가 가득 차도 실제 코어 피해에 선택 클립 재생,0.3초 연속 억제,0피해 제외,음소거,음량 반영,Disable 정지를 확인했다. 정상 사람 플레이/다수전투 믹스/밸런스 측정이 아니다.
- 기존 실제 단일/멀티2대/플레이어 발사·지원 무음·개발 도구·음량/풀 제한 검사도 통과했다.
- 종료 Playfalse/pausedfalse/compilingfalse·컴파일실패false·전투씬1dirtyfalse·TimeScale1/captureDelta0·음량 원복: [최종조회](final_state.json). 콘솔 현재 상태와 보관 로그는 [조회](console.json)를 구분해 본다.
- 에이전트 직접청취/정상 다수전투 믹스/성능/전체 회귀·최종 사람 체감 검사는 하지 않았다. 음색은 사용자⑥선택이며 이번 검사는 연결 기능QA다.

무료/상업 사용 조건은 [후보 단계 공식 조회](../core_damage_audio_auditions_20261018.md)에 있다. BGM/병행 그림자·원본·씬·CSV·프로젝트설정 보존. 씬 YAML 수정/빌드/실행파일/push 없음. 다음 구체 사운드는 사용자 후속 지시를 따른다.
