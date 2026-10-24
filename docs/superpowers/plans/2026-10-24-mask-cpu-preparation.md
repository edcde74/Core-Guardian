# CPU 마스크 준비·출력 축소 계획

> 2026-10-24 실행 결과: 원본3회·A구현·대상47테스트까지 수행. 별도 승인한 보정512쌍도 출력해시74건 차이로 중단·미채택·시작원본 복구. 후보 성능3회 미실행·절감량 미검증. [실험 정본](../../development/mask_cpu_preparation_20261024/README.md). 아래는 원래 계획이며 현재 상태는 이 실행 결과를 따른다.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. 현재 승인은 계획 작성뿐이다. 구현·테스트·재로딩·Play·계측·설정 변경은 구체 승인 후 실행한다.

**Goal:** 같은 원본·픽셀·표시 빈도를 유지하며 CPU 마스크 준비·출력의 의미 있는 비용을 줄인다. 목표는 `CG.Mask.Present` 평균 1ms 이상 감소이며 달성 보장이 아니다.

**Architecture:** 기존 마스크 비용을 최신 조건에서 먼저 분해한다. 1차 후보는 자원 준비 이후 한 번의 PresentMask 안에서 렌더러 값을 수집해 bounds·캐시 입력·생산·정렬이 재사용하는 것이다. 출력 메시 갱신 생략은 독립 후보이며 동시에 구현하지 않는다.

**Tech Stack:** Unity6000.6.2f1/C#/URP, 공식 unity@unity-agent-plugin 스킬·현재 명령 스키마, 기존 ProfilerMarker/ProfilerRecorder와 대상 테스트.

**Spec:** 이 문서의 ‘설계 계약’과 사용자 요청 ‘CPU 마스크 준비·출력 줄일 계획’. 실행 정본은 [AGENTS](../../../AGENTS.md), [최적화 규칙](../../development/OPTIMIZATION_RULES.md), [대상 QA](../../development/TARGETED_QA.md), [아트 보호](../../development/task_rules/ART.md).

## 근거와 현재 상태

- 최신 원본270표본에서 강조평균5.858ms, 내부 마스크4.796ms. 원본3회 마스크평균4.734~4.877ms. [최신 원본 통계](../../development/view_sync_20261023/summary.json). 중첩된 두 비용을 합산하지 않는다.
- 과거 일반89프레임 분해는 Mask.Present self2.263ms, Output1.140ms, CacheLookup0.829ms, SpriteReferences.Read1.097ms를 보였다. 마지막 값은 여러 소비자 전체이며 모두를 마스크 내부로 더하지 않는다. [과거 분해](../../development/combat_cost_20261023/marker_summary.json). 최신 단계별 절감 근거로 그대로 재사용하지 않는다.
- 같은 최신 자료의 PlayerLoop14.729ms와 전체 Editor frame25.209ms는 의미가 다르다. ‘60FPS까지 게임 코드8.5ms 절감’으로 단정하지 않는다. 목표기기는 별도 미검증이다.
- 이전 좌표 후보는 실험 후 미채택·원본 복구했다. [실패/한계](../../development/view_sync_20261023/README.md). 0.06ms급 변경을 새 큰 비용 최적화로 포장하지 않는다.
- 계획 작성 시 HEAD1de8ced. SelectionOutline에는 기존 미커밋15줄 diff가 있다. 이를 이번 변경으로 간주하거나 HEAD로 복원하지 않는다. 실행 복구 기준은 **실행 직전 작업 트리 원본과 해시**다. Unity 현재 상태는 이 계획 작성에서 조회하지 않았다.

## 설계 계약

### 비교한 접근과 권장안

|접근|줄이는 작업/장점|한계·선택|
|---|---|---|
|A. 호출 내부 입력 수집 통합|동일 renderer의 sprite·bounds·행렬·alpha·정렬을 반복 native getter로 읽는 작업 축소. 마스크 키/그리기 의미 유지|권장 1차 후보. 최신 해당 단계 비용과 중복 읽기를 먼저 확인해야 한다. 새 수집 버퍼의 비용까지 포함한다.|
|B. 출력 메시 조건부 갱신|정확히 같은 최종 정점/UV가 반복될 때 mesh 업로드·bounds 재계산만 생략|별도 후속 후보. 걷는 개체에서 값이 자주 바뀔 수 있고 전체 Output1.14ms를 없애는 기법이 아니다. Task1에서 QuadUpload 비용이 충분한 경우만 재제안한다.|
|C. 소비자/프레임 간 광범위 캐시·출력 직접 배칭|잠재 범위는 크지만 동작·정렬·세대·변이 감지 계약 변경|이번 범위 제외. A 보류/B 제한·원본 출력/개별RT/fallback을 유지한다.|

A는 작성자의 권장안이며 사용자 채택/구현 승인이 아니다. 먼저 Task1 결과로 계속할 가치가 있는지 판단한다. 출력 B까지 묶어 실패 원인을 흐리지 않는다.

### Global Constraints

- 현30R 성장/적 물량·밸런스·원본/방향/보행·넓은 그림자·강조/HUD·효과 수/수명·사운드·입력 반응과 표시 주기를 유지한다. 화면 밖/정지 개체를 새로 생략하지 않는다.
- A 보류/B 제한·아틀라스 패킹 보류·원본/fallback·native body batch OFF 유지. 접촉 그림자·미채택 윤곽 shader·출력 batch를 자동 활성화하지 않는다.
- 씬은 `Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity` 하나. YAML 직접 수정·씬 복제 없음. 사용자 씬·설정·미추적·기존 코드 diff·다른 검사/Play 보존.
- shader·crop/좌표 수식·GPU 행렬 곱·mask 크기/threshold·픽셀 두께·정렬 동률 규칙·복사/개별RT 소유·세대/공개 검증 계약을 바꾸지 않는다.
- 자동 빌드·실행파일 실행·커밋·push 없음. timeout/예상 밖 부하/외부 검사/조건 불일치면 후속 명령과 다음 반복을 멈춘다. 실패 재시도는 원인을 보고하고 허락받는다.

### 입력 수집 범위

- 기존 가시성 필터와 `EnsureResources()` 실행 순서를 유지한다. 자원 생성이 OnEnable/OnTransformChildrenChanged를 유발할 수 있으므로 **자원 준비가 끝난 뒤** 입력을 수집한다. 기존 필터에서 제외/포함한 결과를 새 규칙으로 바꾸지 않는다.
- 수집한 값은 그 PresentMask의 bounds/crop→cache Begin→QueuePage 또는 DrawMaskParts→출력 정렬까지만 사용한다. 호출 종료 뒤, 다른 소비자·다른 카메라·다음 프레임에는 재사용하지 않는다.
- 한 구간에서 값이 변하지 않는다고 볼 수 있는 이유를 실제 호출 경로로 확인한다. 수집 이후 외부 콜백/재진입이 가능한 경로는 해당 값의 기존 live 읽기를 유지한다. 전체 프레임을 불변으로 가정하지 않는다.
- cache record와 지연 생산 명령은 수집 버퍼를 빌려 보관하지 않는다. 캐시 Part/생산 draw에는 값 복사를 남겨, 다음 Capture·풀 반환·다른 소비자가 이전 명령을 바꾸지 못하게 한다.
- Camera/Bounds/ScreenToWorld/Transform 수식을 basis 곱 등으로 재작성하지 않는다. 같은 입력 읽기를 줄이되 읽은 float·반복 순서·동률 정렬 결과는 보존한다.

## 파일 책임과 인터페이스

|파일|범위|
|---|---|
|`Assets/Scripts/Runtime/Presentation/Rendering/CoreGuardianSelectionOutline.cs`|고정 구간 마커, 호출 내부 수집 버퍼 소유·사용. 기존 공개 Present API 유지.|
|`Assets/Scripts/Runtime/Presentation/Rendering/CoreGuardianBodyMaskCache.cs`|추가 Begin overload가 수집 값을 기존 Part 값으로 복사. 기존 Request/Begin·Compatible 검증 유지.|
|신규 `Assets/Scripts/Runtime/Presentation/Rendering/CoreGuardianMaskSourceFrame.cs`|호출 범위의 재사용 목록·값 구조체. 전역/프레임 간 캐시나 revision 체계는 추가하지 않음.|
|신규 `Assets/Scripts/Tests/EditMode/CoreGuardianMaskSourceFrameTests.cs`|getter 재수집·버퍼 수명·콜백 경계·같은 입력의 출력 검증. 기존 테스트 fixture와 reflection 방식 재사용.|
|`docs/development/mask_cpu_preparation_20261024/`|실행 승인 후만 생성: 원본/hash·고정 계측 스크립트·raw JSON·픽셀 비교·분석/실패/복원.|

제안 인터페이스(미구현):

```csharp
internal sealed class CoreGuardianMaskSourceFrame
{
    internal IReadOnlyList<CoreGuardianMaskSourceSample> Samples { get; }
    internal void Capture(List<SpriteRenderer> filteredSources);
    internal void Clear();
}
internal readonly struct CoreGuardianMaskSourceSample { /* 아래 필드 계약 */ }
// 기존 Begin(Lease, in Request, out Sample)는 유지한다.
internal static bool Begin(Lease lease, in Request request,
    IReadOnlyList<CoreGuardianMaskSourceSample> capturedSources, out Sample sample);
```

Sample 필드: `SpriteRenderer Renderer`, `Sprite Sprite`, `Texture Source`, `uint SourceVersion`, `Color Color`, `bool FlipX/FlipY`, `Matrix4x4 LocalToWorld`, `Bounds Bounds`, `int SortingLayerID/SortingOrder`. atlas 매핑/버전은 기존 캐시 키의 시점과 판정식대로 유지한다. 불필요한 필드를 더 읽어 비용을 키우지 않으며 실제 단계 분해로 사용하는 필드만 확정한다.

## Review Focus

1. 자원 생성·OnEnable/OnDisable·자식 변경 콜백 뒤에는 변경된 입력을 반영해야 한다. Task2의 생성 콜백 회귀로 검증한다.
2. 같은 프레임에 같은 개체를 다시 Present해 sprite/alpha/flip/정렬/transform이 바뀌면 새 값이어야 한다. Task2의 입력 변경 표로 검증한다.
3. 다음 Capture/생산자 파괴/같은 ID 풀 재사용이 이미 참조 중인 record와 지연 명령을 바꾸면 안 된다. Task2의 수명 및 기존 producer 회귀로 검증한다.
4. fractional viewport·다른 RT/카메라·camera projection/줌·atlas revision/texture update·RT 손실은 기존 공유 거부/재생성/fallback을 지킨다. Task2의 기존 sharing 회귀를 유지한다.
5. 같은 sorting layer/order의 동률·가려진/비활성 부품·Defense/Boss fallback·방향 전환·겹침/선택 중첩을 보존한다. Task2 픽셀/정렬 비교로 검증한다.

## Task 1: 최신 비용 분해와 구현 진입 판단

**Files:** SelectionOutline의 고정 마커만, 기존 실험 스크립트의 제한된 파생 기록. 캐시/생산 동작 변경 없음.

- [ ] 실제 HEAD/diff·원본·보호 파일 해시 및 Unity 연결/씬/dirty/Play/컴파일/다른 검사/Profiler 상태 확인. 공유 작업 중이면 시작하지 않는다. 정상 종료·자체 복원 수단부터 확보한다.
- [ ] 기존 `CG.Mask.Present/Resources/CacheLookup/Output/Parts/MeshUpload/PageProducer`와 `CG.SpriteReferences.Read/CG.Emphasis.Present`를 사용한다. 고정 신규 구간은 `CG.Mask.SourceFilter`, `CG.Mask.BoundsCrop`, `CG.Mask.QuadUpload`. 각 코드 범위를 명시하고 중첩을 합산하지 않는다. baseline/candidate에서 위치·계측 설정을 동일하게 유지한다.
- [ ] 적128(Attack112/Buff16)·포탑15·1920×1080/ortho8.5/time1·고정dt1/60·동일 fixture에서 원본 `warm15+sample90 × 3` 수집. 첫 생성/TMP 준비와 steady 구간의 정의를 수집 전에 고정한다. 관측 도구의 할당/마커 오버헤드도 기록한다.
- [ ] 원본3회는 이후 A의 baseline으로 재사용한다. 별도 동일 baseline을 다시 수집하지 않는다. SourceFilter/BoundsCrop/CacheLookup/Output/QuadUpload의 평균·중앙·실행별 범위, 유효 표본 수와 각 지원 상태를 보고한다.
- [ ] **진입 기준:** A가 실제로 줄이는 반복 getter/준비가 어느 구간에 있는지 소스로 확인하고, 그 구간이 합산 중복 없이 평균1ms 이상인지 확인한다. 구간의 전체 비용이 절감 가능량은 아니다. 그보다 작거나 native/업로드의 지배 원인을 나누지 못하면 구현을 시작하지 않고 결과를 보고한다. 증거 없이 메모이제이션/상수/문자열 후보로 이동하지 않는다.

제안 승인 단위: Task1만 약10~20분, 편집·재로딩 및 짧은 고정 전투/일반 ProfilerRecorder 계측. 유효 표본270프레임·warm45. Deep/할당 스택/Frame Debugger/메모리 snapshot/장기 실행은 포함하지 않는다. 현재 실행 승인이 아니다.

## Task 2: A 후보 하나만 구현·회귀 검증

**Files/Interfaces:** 위 파일 책임 및 제안 인터페이스. Task1 진입 기준을 통과하고 구체 구현 승인을 받은 뒤 수행한다.

- [ ] 테스트를 먼저 작성하고 의도한 RED를 확인한다. 기존 상태에 이미 통과하는 동등성 검사는 characterization이라고 기록하고 실패를 꾸미지 않는다. 신설 내부 API는 reflection으로 기능 누락을 명시적으로 확인한다.
- [ ] `CaptureReflectsSameFrameChanges`: 첫 capture 뒤 sprite/texture update/alpha/flip/localToWorld/bounds/sorting/활성·부품 목록을 각각 변경하고 다음 capture 값과 실제 공개 Present 출력이 새 입력을 반영하는지 확인한다.
- [ ] `ResourceCreationCallbackRunsBeforeCapture`: 최초 출력 자식 생성 콜백에서 Grid/renderer 값을 바꾼다. legacy와 후보의 mask/quad/정렬이 같아야 한다. 수집 전 고정 값을 쓰면 실패해야 한다.
- [ ] `NextCaptureDoesNotMutateExistingGenerationOrQueuedDraw`: 동일 버퍼를 다음 개체/카메라/pose로 채워도 이전 공유 reader·지연 producer 픽셀은 그대로인지 확인한다. 실제 Release→풀 재대여/같은 ID 새 세대도 기존 stale mask 회귀와 연결한다.
- [ ] 최소 구현: filtered sources를 재사용 버퍼에 수집하고 해당 호출의 bounds·cache Part·생산 입력·front 정렬에 전달한다. 기존 순서/정확 비교/crop/GPU 행렬·API는 유지한다. 다른 소비자의 live 검증이나 계층 검사를 삭제하지 않는다.
- [ ] 신규 클래스 + 기존 `CoreGuardianBodyMaskSharingTests`, `CoreGuardianMaskProducerTests`, `CoreGuardianSelectionOutlineTests`의 관련 필터를 현재 스키마로 실행한다. 변경 영향에 필요한 경우에만 ObjectEmphasis/PlayMode 대상으로 넓힌다. 0건은 미검증이며 실제 수·실패를 저장한다. 새 실행기를 만들지 않는다.
- [ ] 원본과 후보를 같은 입력·같은 시점에서 독립 렌더한다. Attack/Defense/Buff/Boss/Player/포탑의 활성 표현, 방향4·phase4, 단독/겹침·선택 중첩, 기본/native 줌8.5/4.25에서 mask 및 출력 픽셀 차이0을 요구한다. 정렬/알파/두께·외부 투명 동률도 확인한다. 전체 HUD/정상30R 승인으로 확대하지 않는다.

예상 추가20~40분+렌더 QA10~20분. 비용·대상 테스트 수/픽셀 비교 조합은 승인 요청에서 고정한다. 실패한 비교를 승인 없이 반복하거나 통과 사례만 골라 쓰지 않는다.

## Task 3: A 일반 비교와 채택/복구

- [ ] 원본과 같은 스크립트·마커·조건에서 후보 `warm15+sample90 × 3`만 수집한다. Task1과 합쳐 유효 비교는총630프레임(warm90+sample540). 날짜 경계/기기/창 상태/GC·초기화 조건 차이가 있으면 비교 한계를 명시하고 자동 추가 반복하지 않는다.
- [ ] 첫 sample 직전 GC.CollectionCount 기준을 갱신한다. wall interval·recorder의 프레임 대응을 확인해 gap!=1인 실행은 비교 불가로 남긴다. 시작 화면 guard 실패면 표본0으로 종료하고 다음 반복도 멈춘다. 조건을 맞추려고 Game View/카메라를 자동 변경하지 않는다.
- [ ] 직접 지표: Mask.Present와 겹치지 않는 구간별 ms 평균/중앙·실행별 범위, Emphasis 총비용, GC B/frame/수거, 자체 버퍼의 용량/잔류. 전체 지표: PlayerLoop/Main Thread/전체 frame 평균·중앙·p95/p99·최대와 최소/최대/평균FPS·Draw/SetPass. GPU/순수 Render CPU 미지원은 N/A다.
- [ ] **제안 채택 기준:** 마스크1ms 감소가 목표. 0.5ms 미만은 이번 큰 비용 목표 미달로 미채택한다. 0.5~1ms는 부분 개선 후보이며 재량으로 목표 달성이라 하지 않는다. 어느 경우든 실행별 변동과 겹치는 차이·늘어난 GC/메모리·전체 프레임 회귀·픽셀/동작 회귀가 있으면 채택하지 않는다. ms와 함께 절감률을 기록한다.
- [ ] 미측정/비교 불가/변동 범위 내/악화/품질 회귀이면 이번 후보만 실행 직전 원본으로 복구하고 후보·실패를 Assets 밖에 격리한다. 기존 사용자 미커밋 diff를 지우지 않는다. 채택 시에도 목표기기60FPS 통과와 구분한다.
- [ ] 정상 종료 후 자체 상태 복원·실제 Play/컴파일/씬/Profiler·보호 hash/diff 확인. 실험 정본과 인수인계에 결과·미완료·다음 승인 경계만 기록한다. 커밋/Notion 갱신/빌드/push는 별도 승인이다.

## B 후보 및 확대 경계

Task1에서 QuadUpload 자체가 충분히 비싸고 최종 정점/UV가 반복되는 증거가 있을 때만 B를 별도로 설계·승인받는다. B는 동일 계산 결과의 mesh 업로드와 RecalculateBounds만 생략하는 방향이다. 외부에서 PropertyBlock을 바꾼 뒤 다음 Present가 복구하는 기존 의미를 유지해야 하므로 property/material/renderer 쓰기까지 일괄 생략하지 않는다. Texture/RT copyback 제거·출력 재질 결합·전역 dirty/revision·shared topology/GraphicsBuffer 재설계는 이 계획의 자동 후속이 아니다.

## 계획 자체 확인

- 현재 산출물은 이 계획 한 파일이다. 제품·씬·설정·테스트를 변경하거나 Unity를 조회/실행하지 않았다.
- 근거의 최신 반복/과거 단회, 실제 비용/절감 목표, 권장안/사용자 승인, Task1/후속 구현 경계를 구분했다.
- Review Focus5개는 Task2의 명시 테스트·픽셀 표에 대응한다. 캐시 키·지연 명령이 수집 버퍼를 소유하지 않는 인터페이스를 확인했다.
- Task1 결과가 약하면 여기서 멈춘다. 이 계획 작성 요청만으로 Task1이나 Task2/3을 실행하지 않는다.
