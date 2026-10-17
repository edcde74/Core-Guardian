# 코어 입자 Inspector 조절

2026-10-17. 최신 사용자 요청: 자동으로 크기를 정하기보다, 직접 보면서 입자 숫자/크기를 Inspector에서 조절한다. 이전 ‘조금 키우기’ 요청보다 이 지시를 우선한다. 복구 기준 `92e4c9c`; 시작 추적 Assets 변경 없음·Playfalse/컴파일실패false/전투씬1·dirtyfalse.

## 사용

Hierarchy **CoreGuardian_Gameplay → Core Guardian Core Particle Settings**:

- **Size Min / Size Max**: 입자 최소/최대 월드 크기. 기본0.025/0.045 유지. 더 눈에 띄게 보려면 우선0.05/0.08 정도로 올려 비교할 수 있다. 화면 픽셀 크기 설정은 아니다.
- **Emission Per Second**: 초당 생성 수, 기본4. 0이면 새로운 입자는 생성하지 않으며 기존 입자는 수명이 끝날 때 사라진다.
- **Max Particles**: 동시에 살아 있는 입자 상한, 기본12. 상한을 낮추면 초과분도 바로 줄인다. 실제 개수는 생성 수와 수명에도 영향을 받는다.
- 컴포넌트 체크를 끄면 입자만 숨기고 코어 본체 발광은 유지한다.

Play 중 Inspector 변경은 실제 생성되는 입자와 이미 떠 있는 입자의 크기에 즉시 반영한다. 이미 살아 있는 입자의 색/남은 수명은 유지한다. Unity의 일반 동작대로 Play 중 조절한 값은 Stop 시 복귀하므로 마음에 든 값을 기록해 Edit 모드에서 입력하고 씬을 저장하면 영구 보존된다.

## 구현·보존

`CoreGuardianCoreParticleSettings`는 씬에 직렬화된 설정만 담당한다. `CoreGuardianCombatArt`가 기존 `CoreGuardianCoreAmbient` 생성 시 이 컴포넌트를 전달하고 `Advance`가 변경된 값만 실제 ParticleSystem에 반영한다. 일시정지 중에도 Inspector 변경은 반영된다. 기존 원본/청록색/수명/상승 속도/발생 범위/발광 셰이더·피해 시0.3초 억제는 보존했다. 설정 컴포넌트만 Editor API로 전투 씬에 저장했다. 카메라/후처리/전투 수치·선택 아웃라인과 사용자 초록색 설정은 변경하지 않았다.

기존 코어 표현/피해 규칙은 [코어 발광 담당 기록](../core_ambient_20261017/README.md), 강화 시안은 [별도3안](../upgrade_feedback_options_20261017/README.md)을 따른다.

## 검증

- [설정 미구현 실패](settings-red.json) → [집중 EditMode1/1](settings-green.json). 실시간 기존 입자 크기 변경·수명/색 보존·생성률/상한 변경과 상한 축소를 검증했다. 초기 테스트는 stopped ParticleSystem에 SetParticles만 호출해 입자가 없었으며 실제 playing 시스템으로 테스트 전제를 수정했다.
- [실제 Play/SerializedObject Inspector 검증](live-tuning.json), [검증기](VerifyTuning.cs): 실제8입자의0.055~0.085 크기·초당8/상한24 임시값을 확인했다. 동일 seed/남은 수명 유지, 기존 입자 즉시 크기 증가, 컴포넌트Off는 입자만 숨김, 피해 때 숨김/0.31초 후 복귀를 확인했다. TimeScale0 전환 후 한 프레임 안정화를 기다린 동일 시점 검사다. [실제 Overlay 이전](before.png) / [임시 확대](larger-preview.png). 원래 카메라/줌 유지, 검사 후 설정/TimeScale 복원.
- 최종 전체 [EditMode118/118](editmode.json), [기존 코어 집중PlayMode1/1 completed](core-playmode.json) 통과. 이후 [전체 PlayMode0건](full-playmode-zero.json)은 통과가 아니다. 사람 직접 플레이/다수 전투/밸런스/성능·최종 입자 크기 승인은 별도다.
- 별도 읽기 전용 리뷰 actionable finding 없음. 종료 Playfalse/pausedfalse/compilingfalse/failedfalse·씬1/dirtyfalse·TimeScale1/captureDelta0·기본0.025/0.045/초당4/최대12·HDRfalse/posttrue·현재콘솔오류0/경고0. 자체 Play/임시 카메라·프리뷰 리소스 복원 확인. 빌드/실행파일/push 없음.
