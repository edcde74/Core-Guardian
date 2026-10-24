# Mask Submission Path Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking. Execution was approved by the user and is now closed; see the actual results below.

**Goal:** 같은 화면을 유지하면서 마스크를 그리기 위한 CPU 준비·생성·복사의 중복을 줄인다.

**Architecture:** 현재의 원본 출력 렌더러와 개별 완료 RT를 유지한다. 내부 호출에서 이미 읽은 카메라 입력을 재사용하고, 완료된 마스크의 정확한 입력이 같으면 다음 프레임에도 생성·복사를 건너뛴다. pending 페이지 슬롯의 프레임 수명은 그대로 둔다.

**Tech Stack:** Unity6000.6.2f1, 기존 C#/URP/CommandBuffer/ProfilerMarker/ProfilerRecorder, NUnit EditMode/PlayMode.

**Spec:** 이 문서의 「설계 근거와 선택」 및 「실행 제안」이 이번 계획의 설계 명세다. 근거: 사용자 셰이더 호출 코드 검수·해결 계획 요청, [B1/B2 실제 결과](../../development/emphasis_mask_cpu_20261024/B1_B2_RESULTS.md), [최적화 규칙](../../development/OPTIMIZATION_RULES.md), [작업 주의점](../../development/OPTIMIZATION_PITFALLS.md).

**상태:** 사용자 승인 실행 종료. Task1 진단/원본3회, Task2 구현/52테스트/512비교/후보3회 완료. 합산절감0.367ms로1ms목표미달·후보미채택/원본복구. Task3은gate미달생략. 컴파일선언누락/응답지연은추가승인후한줄복구·정상컴파일. 독립검토사용량제한미완료·Profiler버퍼차이도 [실제결과](../../development/mask_submission_path_20261024/README.md)에명시. 추가조사/수정미승인.

## Global Constraints

- 현30R 성장/물량·밸런스·원본/방향/애니메이션·강조/그림자/TMP/HUD·효과 수/수명·사운드·반응성 보존.
- A 출력배칭 보류/B 마스크 제한·SpriteAtlas 보류·원본출력/fallback·native body batch OFF 유지. 이미 활성인 마스크용 atlas와 보류 중인 SpriteAtlas를 혼동하지 않는다.
- 빌드·실행파일·커밋·push·Notion 변경 없음. 사용자 씬/설정/미추적·다른 검사/Play 보존. 씬 YAML 수정/비교 씬 디스크 생성 없음.
- 실패 픽셀 비교/조건 실패/예상 밖 부하/timeout 이후 자동 반복·대체검사 금지. 이 문서의 프레임 수는 제안이며 과거 승인을 재사용하지 않는다.
- broad source snapshot/새 개체별 배열을 만들지 않는다. ProfilerRecorder.ToArray/CopyTo 사용 금지. 기존 capacity1/wrap/LastValue만 사용.
- 프레임 전체 불변 카메라를 가정하지 않는다. 공개 API 호출 사이·카메라 렌더 콜백 사이 입력 변경을 감지한다.
- 이번 안에 Quad GPU 연산 변경·페이지 직접 출력·output batch 재활성화·SRP Batcher/instancing 전환을 섞지 않는다.

## Review Focus

1. 이전 프레임의 pending 슬롯은 완료 RT 재사용으로 잘못 인정하지 않는다.
2. 같은 프레임 두 호출 사이의 카메라/부품 변경, 자원 생성 콜백 뒤 입력을 반영한다.
3. sprite/alpha/flip/matrix/texture version/atlas revision/RT 손실이 바뀌면 마스크를 새로 생성한다.
4. 공개 출력의 외부 MPB 변경 복구·정렬·모드전환·다른 소비자 수명을 보존한다.
5. 풀 반환/대여·타깃 파괴·다른 카메라·preview/씬 종료 후 오래된 RT나 queued draw를 재사용하지 않는다.

## 설계 근거와 선택

### 확인된 사실

- ObjectEmphasis는 대상별 SelectionOutline.Present를 호출한다. 마스크 생성은 정상 범위에서 QueuePage → Producer.Flush 배칭을 사용한다. 부품마다 Graphics.ExecuteCommandBuffer를 호출하는 구조가 아니다.
- Cache.Compatible는 request.frame이 다르면 입력이 같아도 miss다. 다음 프레임 정적 입력의 재사용을 의도적으로 막는 현행 계약이며 관련 테스트도 이를 요구한다. 단순 frame 조건 삭제가 해결책은 아니다.
- 공유 페이지에서 생성한 결과는 BodyMaskCache.Flush에서 개별 RT로 복사한다. 런타임 RegisterOutput은 조기 반환하므로 최종 출력은 원본 Renderer/Material이다.
- Request/QueuePage/Page.View가 같은 camera viewport/view/projection/target 등을 반복 읽는다. QueueShape의 synchronous slot 검증 생략은 이미 적용돼 있다.
- CG.Mask.Present 약4.877ms는 준비 구간이다. 이후 카메라 콜백의 PageProducer 약0.46~0.50ms와 copyback 전체가 이 안에 포함된 것은 아니다. 기존4.88을 경로 전체로 쓰지 않는다.
- 현재 PhotoShadow는 별도 스프라이트다. 접촉 마스크 API가 존재한다는 사실을 실제 그림자 사용으로 확대하지 않는다.
- 원본 마스크 셰이더와 배칭 마스크 셰이더의 명시적 입력에는 _Time이 없다. 이것만으로 모든 외부 동적 텍스처/변이 안전이 증명되지는 않는다.
- 보행 코드는 부품의 위치/회전을 갱신한다. 적128/포탑15 조건에서 입력이 실제로 동일한 개체 비율은 미측정이다. 정적 재사용을 모든 적의 비용 절감으로 계산하지 않는다.

### 검토한 접근

|접근|줄이는 작업|한계/선택|
|---|---|---|
|완료 RT의 정확한 입력 일치 재사용|해당 개체의 재생성·큐 등록·copyback|실제 적용 비율 확인 후 진행. 움직이는 개체에 억지 적용하지 않음|
|호출 내부 카메라 상태 재사용|반복 native getter/지원조건 조회|동적 적에도 적용 가능. 매 공개 호출/렌더 경계에서는 새 상태 필요. 기본 단계로 권장|
|페이지 직접 출력|개별 RT copyback·텍스처 분리|픽셀/순서/페이지 수명 영향 큼. 현재 보류 출력 경로와 겹쳐 이번 실행안 제외|

권장안: Task1에서 재사용 가능한 실제 작업량을 확인하고 Task2의 카메라 입력 중복을 줄인다. Task3은 Task1의 적용성 기준을 통과한 경우에만 구현한다. Task1에서 재사용 기회가 없으면 Task3을 자동으로 다른 구조 변경으로 대체하지 않는다. 이 경우 Task2 결과와 적용 불가 원인을 보고한다.

## Task 1: 경로 전체 비용과 정적 재사용 가능량 확인

**Files:** Modify `Assets/Scripts/Runtime/Presentation/Rendering/CoreGuardianBodyMaskCache.cs`, `CoreGuardianMaskProducerBatch.cs`의 고정 마커/진단 카운터만. 필요 시 `CoreGuardianSelectionOutline.cs`의 생성 요청 경계 마커. Create `docs/development/mask_submission_path_20261024/`에 기존 Benchmark/PixelProbe를 복제해 실험 자료를 분리한다. 이 폴더와 도구는 실행 승인 전 생성하지 않는다.

**Interfaces:** `CG.Mask.CacheFlush`는 pending 수집→producer→copyback→discard 전체를 감싼다. `CG.Mask.CopyBack`은 복사 명령 작성과 그 Execute만 감싼다. `CG.Mask.PageProducer`는 기존 유지. `CG.Mask.Present`와 CacheFlush는 서로 다른 호출 구간으로 합산 가능하되 PageProducer/CopyBack을 다시 더하지 않는다.

- [x] 실제 HEAD/대상 diff·시작 원본 hash, Unity 연결/씬/dirty/Play/compile/다른 검사·Profiler 상태·화면 선택을 확인하고 자체 복원 기준을 보관한다.
- [x] 고정 마커를 추가한다. 역할별 요청 수/당일 새 생성 수/완료 재사용 가능 수/기존 same-frame hit/대기 생성/copy 수를 진단15프레임에만 집계한다. 동일 조건의 기존 Request·Part를 활용하며 별도 source snapshot/매프레임 파일출력 없이 종료 후 한 번 저장한다.
- [x] warm15+진단15프레임1회에서 정확한 일치로 재사용 가능한 요청을 분류한다. 바뀐 입력은 최초 불일치 항목별(frame 제외 pose/sprite/alpha/texture/camera/crop/유효성)로 집계한다. 진단 분류 overhead를 일반 성능 수치로 쓰지 않는다.
- [x] 정상/고정 자세/부품 변이 테스트로 분류가 맞는지 확인한다. `EqualCompletedInputs_IsEligible`, `ChangedPose_IsNotEligible`, `PendingOldFrame_IsNotEligible`를 RED→GREEN으로 검증한다. 현행 행동은 아직 변경하지 않는다.
- [x] 정적 가능 요청이 0이거나 입력 비교 후 남은 생성/제출 비용이 누적1ms 목표에 기여하기 어려우면 Task3의 우선도를 내린다. 전체4.88ms에 단순 hit비율을 곱하지 않는다. 비교/범위 계산은 재사용해도 남는다. 이 판단은 기대치를 걸러내는 용도이며 실측 절감량이 아니다.
- [ ] **생략: gate미달.** Task3 gate를 통과한 경우에만 이 시점의 원본에서 `docs/development/mask_submission_path_20261024/TemporalProbe.cs`로8역할×2줌×실제3프레임(초기/동일/pose변경)의48렌더 원본을1회 저장한다. own fixture의 body Renderer 상태를 매프레임 복원하고, Present→실제 Flush→원본 윤곽만 격리한 render 순서를 보존한다. Task3에서 후보48렌더와 비교하며 원본 재로딩/재수집을 불필요하게 추가하지 않는다.
- [x] 진단을 끄고 새 원본 warm15/sample90×3을 수집한다. 기존 원본은 새 CacheFlush/copyback 비교와 계측 동일성이 없어 주 비교에 재사용하지 않는다. 과거 자료는 참고만 한다.

## Task 2: 같은 호출의 카메라 입력을 재사용

**Files:** Create `Assets/Scripts/Runtime/Presentation/Rendering/CoreGuardianMaskCameraState.cs`(readonly struct만). Modify SelectionOutline/BodyMaskCache/MaskPage/MaskProducerBatch. Test `Assets/Scripts/Tests/EditMode/CoreGuardianMaskCameraStateTests.cs` 및 기존 sharing/page/producer의 관련 필터.

**Interfaces:**
- `CoreGuardianMaskCameraState.Capture(Camera camera)`는 camera참조,view,projection,viewport,target참조/실제width·height/AA/format,orthographic/size/aspect/near/far/stereo/dynamic/enabled,transform position/rotation/forward를 값으로 읽는다.
- Request에 `in CoreGuardianMaskCameraState` 내부 생성 경로를 추가한다. 기존 signature는 유지하고 fresh Capture를 수행한다.
- QueuePage 및 TryAcquire/Matches/IsValid의 내부 overload는 `in CoreGuardianMaskCameraState`를 받는다. public entry는 매 호출 fresh 상태를 만든다.
- Present는 EnsureResources의 콜백이 끝난 후 Capture한다. 한 synchronous 호출 안에서만 전달하며 스택상 struct를 새 owner배열/전역 frame cache로 저장하지 않는다.
- Producer.Flush는 그 렌더 경계에서 별도 fresh Capture를 한 번 하고 해당 flush의 슬롯 검증에 공유한다. LateUpdate의 오래된 상태를 그대로 신뢰하지 않는다.

- [x] `SameCallInternalChecks_UseCapturedValues`, `NextPublicCallSeesChangedCamera`, `TwoCamerasDoNotShareState`, `ResourceCallbackCameraChange_IsCapturedAfterCallback` 테스트를 먼저 작성·실패 확인한다. 이미 통과하는 기존 동작 검사는 characterization으로 구분한다.
- [x] 내부 overload를 구현한다. crop/ScreenToWorld/행렬 연산 순서 자체를 다시 쓰지 않고 기존 엔진 호출 유지, 지원 범위/정확 비교식/공개 검증은 보존한다. public API에 검증 생략 플래그를 노출하지 않는다.
- [x] 신규 테스트와 영향 sharing/page/producer/outline 테스트를 통과시킨다. Flush 직전 카메라 변경 시 queued원본복구가 유지되는 실제 Play 회귀를 포함한다.
- [x] 기존 격리 원본512의 소스·조건 동일성 확인 후 후보512쌍을1회 비교한다. mask/geometry/rect/output차이0·Renderer격리·비어 있지 않은 출력 조건을 모두 확인한다.
- [x] 후보 warm15/sample90×3. 새 원본과 Present/CacheFlush/전체frame/GC·FPS를 비교하고 단계 소스·diff를 보관한다.

## Task 3: 완료된 마스크의 프레임 간 재사용

**실행 결과: 적용성 gate 미달로 아래 구현/테스트/시간축 렌더/후보 측정을 모두 생략했다. 미승인 자동 확대 없음.**

**Gate:** Task1의 동일입력 요청이 실제로 존재하고, 해당 요청이 생성/제출을 건너뛸 가치가 있다는 근거가 있을 때만 실행한다. Task2 통과본을 단계 기준으로 사용하며 효과는 최초원본/직전단계를 둘 다 보고한다.

**Files:** Modify BodyMaskCache/SelectionOutline. Add `Assets/Scripts/Tests/EditMode/CoreGuardianCompletedMaskReuseTests.cs`, `Assets/Scripts/Tests/PlayMode/CoreGuardianCompletedMaskReusePlayModeTests.cs`. 기존 `SoloOwnerNextFrame_ReusesCapacityButRegenerates`의 의도와 새계약을 명시적으로 갱신한다.

**Interfaces:** `SameContent(in Request a,in Request b,List<Part> aParts,List<Part> bParts)`는 frame을 제외한 기존 정확한 의미의 입력을 비교한다. 완료 재사용은 valid && !pending && texture!=null && IsCreated && epoch/모든content입력일치일 때만 허용한다. pending same-frame 공유는 기존 page validity/frame 계약을 유지하는 별도 분기다. 다른camera/texture mutation/viewport·target변경을 무시하지 않는다.

- [ ] `CompletedUnchangedNextFrame_DoesNotGenerateOrQueueOrCopy`, `ChangedPoseNextFrame_Regenerates`, `PendingPreviousFrame_CannotBecomeCompletedHit`를 RED로 확인한다.
- [ ] 완료 재사용을 구현한다. Begin의 입력검증 비용은 그대로 인정하고, hit일 때 QueuePage/생성/CopyBack 대상이 되지 않도록 한다. completed hit가 오래된 page slot을 새프레임 출력에 노출하지 않게 한다.
- [ ] 실제 연속 프레임에서 sprite/alpha/flip/matrix/threshold/texture update/atlas revision/camera/projection/targetResize/RT.Release가 각 miss를 만드는 회귀를 실행한다. 자원생성콜백·Release/재대여·같은target 새generation·다른owner구독·파괴도 확인한다. mockframe만 바꾼 테스트로 끝내지 않는다.
- [ ] 별도 fixture의 실제3프레임 고정/동일/변경 시퀀스를8역할×2줌=16개로 고정한다. 원본/후보 각각48렌더,총96렌더를 비교한다. 원본은 Task1 기준버전에서 한 번 수집해 보관하고 후보와 비교한다. 이 검사는 독립512의 시간축 부족을 보완하며, 제품의 전체게임검증으로 확대하지 않는다.
- [ ] 후보 격리512쌍1회와 warm15/sample90×3을 수행한다. Task2의3회자료는 동일계측이면 직전기준으로 재사용한다. 새 후보 전체/원본/직전단계 절감과 queued·copy감소를 연결해 보고한다.

## 효과 부족·실패를 해석하는 방법

|결과|확인할 원인|다음 행동|
|---|---|---|
|동일입력 요청이 거의 없음|pose/sprite/camera/crop 중 무엇이 바뀌는지 Task1 분류|재사용을 억지 확대하지 않고 Task3 생략. 움직임/화질을 바꾸지 않음|
|hit는 많지만 CPU절감이 작음|남은 입력 수집·bounds·출력 갱신과 감소한 Queue/Flush를 비교|바뀌지 않은 비용을 원인으로 보고. getter재사용을 다른이름으로 반복하지 않음|
|마스크 같고 최종픽셀 다름|quad/UV/origin/정렬/재질·실제활성Renderer 경계|실패원자료 보존·후보복구. 반복렌더나 새경로 자동시도 안 함|
|정적 재사용 후 잔상/RT손실|pending완료오인·누락된input/epoch/Release|해당회귀를 먼저재현하고 캐시경계 수정안을 보고|
|전체frame만 좋아지고 대상scope가 그대로|다른scope/GC/Editor변동|최적화성공으로 판정하지 않음|
|목표미달이며 출력기하가 남은 상위비용|BoundsCrop/출력기하/QuadUpload의 새실측|정확한기하signature 재사용을 다음별도안으로 제시. 이번범위에서자동구현 안 함|

한 번의 추가 원인 진단이 필요할 경우에만 normal Profiler warm15/sample90 1회, 최대상위2프레임/MainThread로 제한하는 별도 실행항목을 제안한다. 기본 실행량에는 포함하지 않으며, 승인 없이 자동 실행하지 않는다. Deep/FrameDebugger/heap snapshot/무제한 native 추출은 이번 안에 없다.

## 판정과 실행 제안

- 목표 제안: 품질을 유지하며 **마스크 호출경로의 누적 CPU1ms 이상 감소**. 기대효과 확정값이 아니다. Present와CacheFlush의분리/합산값을 함께 보고한다. 강조시간과마스크시간은 중첩되므로 더하지 않는다.
- 중간단계0.5ms미만이라는이유만으로검증된부분개선을모두폐기하지않는다. 단계별소스와효과를보존해최종조합을실측한다. 제품채택은누적결과·변동·GC/메모리·품질에근거하며자동활성화하지않는다. 기존B1/B2는이번후보에자동합치지않는다.
- 기본최대량:Task1 warm15+진단15=30프레임1회;일반원본/Task2/Task3 각각warm15+sample90×3=945프레임. 합975프레임(테스트/16시퀀스렌더별도). Task3 gate미달이면그단계315프레임/512비교를실행하지않는다. 단회조건실패뒤무승인보충없음.
- 새격리검사는후보별512×최대2회. 동일한기존원본512는검사/소스/조건일치시재사용한다. 시간축16시퀀스는원본/Task3각1회로96렌더이며불필요한Task2시간축반복을추가하지않는다.
- 성능조건:일반모드/적128·타워15/1920×1080/ortho8.5/dt1/60/기존부하. GPU미계측은그대로명시한다. 평균·중앙·p95·최대frame/각회최저·최고·평균FPS/GC·수거/Draw·SetPass/대상CPU·할당·메모리한계를보고한다.
- 필요한조작제안:스크립트재로딩·자체Play진입/정상종료·측정중1920임시선택/복원. user Play/다른검사중이면시작하지않는다. 기존Profiler기록OFF와buffer를보존하고평상시새capture를만들지않는다. 종료조건은정해진표본수이며임의120초실행없음.
- 예상실행시간90~150분:준비/진단15~25분,카메라구현/회귀/비교30~45분,완료재사용35~55분,분석/복구/검토10~25분. 상태·실패시자동연장하지않고그때까지의근거와복구상태를보고한다.
- 마지막에fresh read-only검토1회(실행스킬요구),실제Unity종료/compile/scene/settings/프로파일/보호파일hash확인. 후보미채택이면자체변경만시작working-tree원본복구·새파일Assets밖격리. 결과와인수인계는그실행승인에포함시킨다.

## 계획 자체 검증

- [x] 기존오류정정:4.88ms범위/실제PhotoShadow/이미존재하는생성배칭을구분했다.
- [x] 가장큰불확실성인정적입력비율을구현전gate로뒀다.
- [x] completed RT와pending page계약·콜백·외부출력복원회귀를각소유Task에배치했다.
- [x] 새flush계측으로기존자료재사용이불가능한범위와재사용가능한512를구분했다.
- [x] 작업범위/반복/시간/복구/실패경계가정해졌다. 실제완료/생략상태를각Task와결과정본에반영했다.
