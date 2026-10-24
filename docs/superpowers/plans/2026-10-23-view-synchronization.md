# C# 뷰 동기화 중복 축소 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking. 구현·Unity 조작은 사용자 구체 승인 후 실행한다.

> **2026-10-24 실행 결과:** Task1+2 승인 범위 검증 완료. 원본/최종 후보 각각 warm15/sample90×3, 별도 호출 진단 완료. Views2.146→2.085ms는 실행 변동폭과 겹치며 전체FPS39.67→39.60으로 개선 없음. 후보 미채택·제품 두 파일 원본 복구/diff0. Present 자식 콜백 회귀 때문에 aura live World를 유지하고 포탑 좌표 재사용은 제외했다. 신규5/5·기존29/29는 후보의 대상 검증 결과다. **Task3 미실행·추가 승인 없음.** 상세/원본/실패/복원은 [검증 정본](../../development/view_sync_20261023/README.md)을 따른다. 아래 제안/당시 승인 대기 문구는 작성 시점 기록이다.
**Goal:** 표시 빈도와 전투 동작을 유지하면서 SynchronizeViews의 반복 좌표 변환과 표시용 조회 CPU 비용을 줄인다.

**Architecture:** 먼저 현재 동기화 내부 비용을 나누어 확인한다. 좌표는 각 개체 처리 안에서 재사용한다. 포탑 조회는 공개 live API의 변이 감지 계약을 유지하고, 별도로 준비한 표시 결과를 소비하는 방식이 안전하고 유효한지 확인한 후 분리 구현한다. 두 후보를 한 번에 섞지 않는다.

**Tech Stack:** Unity 6000.6.2f1, C#, 기존 Unity CLI / 대상 테스트 / 일반 모드 계측.

**Spec:** 사용자 요청: C# 뷰 동기화 중복 해결 계획. AGENTS.md, docs/development/OPTIMIZATION_RULES.md, TARGETED_QA.md가 실행과 채택 기준이다.

## Global Constraints

- 현재는 계획만 승인됐다. 제품 수정·테스트·재로딩·Play·Profiler 조작은 미실행이다.
- 적128/타워15, 원본 표시·보행·조준·체력바·그림자·강조·효과 수/수명·사운드·밸런스를 유지한다.
- 표시 주기를 낮추거나 일부 개체를 생략하지 않는다. A 보류/B 제한, 아틀라스 보류, native body batch OFF 유지.
- 사용자 Play/다른 검사와 중복하지 않고, 씬·설정·미추적 파일을 보존한다. 자동 커밋·빌드·실행파일 실행·push 없음.
- 아래 비교 횟수는 실행 승인 요청의 제안이며 과거 단회90프레임 승인을 재사용하지 않는다.

## 확인한 소스와 중복

|파일|현재 동작|변경 방향|
|---|---|---|
|Assets/Scripts/Runtime/Bootstrap/CoreGuardianPrototype.cs|SynchronizeViews에서 본체·aura에 World 반복 호출|개체 처리의 currentWorld 한 번을 전달/재사용|
|Assets/Scripts/Runtime/Presentation/Characters/CoreGuardianCombatAnimation.cs|Enemy에서 현재/이전 위치 World 각각 호출|현재 좌표를 인자로 받고 이전 tile은 기존 World로 변환|
|Assets/Scripts/Runtime/Domain/Rules/Session.cs|TowerTargetForView마다 FindTower 검증, 공간 인덱스 무효화/검증; HasTowerSupport마다 지원 목록 검증|비용 확인 후 표시 준비 단계와 소비 단계 분리 후보|

적 한 개가 dt>0인 안정 구간에 소비하는 World 호출은 본체1+애니메이션2=3회다. aura 객체가 남아 있으면 활성 여부와 관계없이 위치 갱신1회가 추가된다. aura 최초 생성 프레임에는 생성 위치1회도 더 있다. 현재 위치를 전달하면 안정 구간 3~4회가 2회로 줄어드는 구조다. 이는 소스상 호출 수이며 ms/FPS 절감 실측이 아니다.

## Review Focus

- 이전 tile을 이전 프레임 world로 대체하면 움직인 Grid에서 속도/방향이 달라질 수 있다. 이전 tile도 현재 World로 변환하는 기존 의미를 보존한다.
- 첫 생성·풀 재사용·같은 ID의 다른 세대에서 이전 위치/타깃이 남지 않아야 한다.
- Instantiate/OnEnable/Present 사이 모델 변이가 가능하다. 공개 API의 변이 검사 제거 또는 프레임 전체를 무조건 불변으로 간주하지 않는다.
- 단일/멀티/지원 포탑의 타깃 우선순위·동률·중복 ID·목록 교체/재정렬과 범위 경계를 유지한다.
- 생존/포탄 ID 목록을 공유할 경우 정리 시점과 콜백 중 신규 생성/제거가 달라질 수 있다. 이번 1차 후보에서는 목록 공유를 하지 않는다.

## Task 1: 변경 전 직접 비용 확인

**Files:** 두 presentation 파일의 해당 구간과 Assets 밖 실험 기록만. Session 동작 변경 없음.

- [x] 실제 HEAD/diff·수정 대상 원본을 보존하고 Unity 연결/Play/컴파일/다른 검사 상태를 확인한다.
- [x] SynchronizeViews 전체와 적 처리/포탑 처리/포탄 처리의 고정 ProfilerMarker를 추가한다. 전후 동일 계측을 사용한다.
- [x] World 호출 수와 포탑 목록/인덱스 검증 수를 별도의 짧은 진단으로 센다. 계측용 카운터의 호출당 추가 비용을 성능 결과와 섞지 않는다.
- [x] 일반 모드 기준 자료를 수집한다. Deep/할당 스택/Frame Debugger는 사용하지 않는다.
- [x] 뷰 동기화가 차지하는 ms와 하위 비용을 보고한다. 중첩 마커를 더하지 않는다. 이 작업에서 줄일 수 있는 상한이 작은 경우 큰 FPS 개선을 약속하지 않고 구현 확대 전에 보고한다.

## Task 2: 좌표 재사용 후보 하나만 구현

**Files:** CoreGuardianPrototype.cs, CoreGuardianCombatAnimation.cs 및 관련 대상 테스트.

**Interfaces:** 기존 Enemy(EnemyHandle, GameObject)를 유지한다. 내부 호출용 Enemy(EnemyHandle, GameObject, Vector3 currentWorld)를 추가하고 기존 진입점은 World 계산 후 위임한다.

- [ ] 현재/이전 tile, dt=0, 첫 생성, 풀 재사용, Grid 이동/회전/비균일 scale 조건에서 기존 속도·방향·위치와 같은지 확인할 회귀 테스트를 먼저 작성/확인한다.
- [ ] 적 처리에서 currentWorld를 한 번 계산해 본체 위치와 Enemy 호출에 사용한다. aura 위치도 동일값을 쓰되 중간 콜백이 Grid/모델을 바꿀 수 있는 경로는 기존 계산을 유지한다.
- [ ] 포탑의 본체/aura 역시 안전한 동일 처리 범위에서 위치를 재사용한다. 정지 포탑 Transform 갱신 전체 생략이나 장기 캐시는 도입하지 않는다.
- [ ] 이전 위치는 기존 previousEnemies의 tile을 현재 World로 변환한다. World 수식을 basis 곱으로 재배열하거나 반올림을 바꾸지 않는다.
- [ ] 포탄·플레이어·발사 콜백·목록 정리 방식은 그대로 둔다. 대상 회귀/같은 위상의 표시 비교 후 별도 성능 비교로 채택 여부를 판단한다.

## Task 3: 포탑 표시 조회는 별도 후보로 설계/검증

**Files:** Session.cs, CoreGuardianCombatAnimation.cs, CoreGuardianPrototype.cs, CoreGuardianSessionTests.cs.

- [ ] Task 1의 포탑 조회 비용이 실제 줄일 가치가 있는지 먼저 확인한다. 단순히 O(T×E)라는 이유만으로 새 캐시를 추가하지 않는다.
- [ ] 표시용 결과는 재사용 버퍼의 순서별 tower 참조/타깃/지원 여부로 준비하는 방안을 검증한다. 새 Dictionary와 매 프레임 할당을 추가하지 않는다.
- [ ] 기존 FindTower / TowerTargetForView / HasTowerSupport의 live 호출 동작과 변이 테스트는 유지한다.
- [ ] 준비→소비 사이 Instantiate/OnEnable/애니메이션 콜백의 모델 변경 가능성과 재진입을 확인한다. 이를 보존할 경계/무효화 방법을 입증하지 못하면 일괄 준비 방식은 채택하지 않는다. 승인 없이 전역 revision 체계나 모델 setter 변경으로 확대하지 않는다.
- [ ] 적 이동/사망/제거/재정렬, 포탑 ID/참조/목록/지원 범위 변경, 동률 타깃, 비정상 범위, 재진입/종료 후 live fallback을 테스트한다. 기존 ViewTargets 검증을 삭제/약화하지 않는다.
- [ ] 안전한 구현안과 수정 범위가 확정된 뒤 사용자에게 구체적으로 보고하고 별도 구현 승인을 받는다. Task 2 승인만으로 Task 3 구현까지 진행하지 않는다.

## 측정과 채택

- 제안: 조건당 warm15/sample90을 3회씩, 동일 적128(Attack112/Buff16)/타워15·seed/입력/dt·해상도/카메라·배속·음향·Profiler 상태로 수집한다. 실행 전에 이 횟수/부하를 명시 승인받는다. 타임아웃/사용자 검사 발생 시 중단하고 자동 재시도하지 않는다.
- 준비가 가능한 동일 시점에서 비교한다. TMP/리소스 초기화 구간과 안정 구간을 섞지 않고, 구간 정의는 수집 전에 고정한다. GC가 발생한 유효 프레임을 임의 제거하지 않는다.
- 직접 지표: SynchronizeViews 전체 및 대상 구간 평균/중앙 ms, 호출/검증 수, GC B/frame. 전체 지표: frame 평균/중앙/최대, 최소/최대/평균 FPS, GC 수거, Draw/SetPass.
- 해당 구간 CPU 절감이 반복 결과에서 확인되고 전체 프레임/GC/동작에 회귀가 없어야 채택한다. 호출 수 감소만으로 성공이라고 하지 않는다.
- 전체 결과가 흔들리거나 GC 조건이 불일치하면 비교 불가로 보고한다. 같은 검사를 승인 없이 반복하지 않는다. 직접 비용 절감이 없다면 해당 후보만 원본으로 복구한다.
- 예상 구현/대상 검증 작업 시간 20~40분, Unity 실제 비교 구간은 위 고정 프레임만 사용한다. 새 측정 결과에 따라 Task 3 범위를 다시 판단한다.

## 승인 경계

현재 산출물은 이 계획뿐이다. 권장 다음 승인 단위는 Task 1+2의 계측·좌표 재사용·관련 테스트·재로딩·명시한 일반 비교와 자체 상태 복원이다. Task 3, 마스크/렌더/셰이더/전투 판정 개선은 포함하지 않는다. Git 커밋은 별도 승인 사항이다.
