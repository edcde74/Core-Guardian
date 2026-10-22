# Core Guardian A/B Rendering Batching Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans for native execution, or superpowers:subagent-driven-development if the user selects delegation. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 같은 품질/물량에서 스프라이트 제출과 마스크/윤곽/접촉 재질 전환을 단계별로 줄인다.
**Architecture:** A는 원본 sprite의 재질/정렬/프레임 계약을 유지하는 선택적 배칭 경로다. B는 기존 개별 mask fallback을 유지하면서 공용 page와 정렬 구간별 geometry를 추가한다. 복사 기반 출력부터 검증하고 마지막에 직접 page 생산으로 전환한다.
**Tech Stack:** Unity6000.6.2f1, URP17.6, C#/HLSL, 기존 Unity Pipeline0.7 공식 CLI/MCP, NUnit/기존 PixelComparison·RuntimeProbe.
**Spec:** [DESIGN.md](DESIGN.md). 2026-10-22 사용자 설계 승인. 계획 작성 기준 `6495a42`.

## Global Constraints

- 현30R 성장/적 수/전투 결과, 카메라·해상도·배속, 원본 스프라이트·방향·보행·그림자·강조·HUD·모든 효과 수/수명·사운드·입력을 보존한다.
- 외부 호출 `Present`, `PresentAtOrder`, `PresentGroundContactWithBand`, `PresentOverlay`, `IsVisible`의 의미와 기존 수명 계약을 보존한다.
- 초기 사용 범위는 기존 검증된1920×1080, 직교8.5/4.25, 기본 projection/정수 viewport 등 현 제한을 넘지 않는다.
- GPU/Memory Profiler 도구 설치·그래픽 API 변경·빌드는 별도 허용 없이 하지 않는다. 외부 GitHub 패키지는 조사만 하며 계획의 제품 의존성은 추가하지 않는다.
- 사용자 씬/TimeManager/PackageManagerSettings·미추적·폰트 접근 오류 표시를 보존한다. 씬 YAML 직접 수정·추가 씬·자동 빌드/실행파일/push 없음.
- 같은 Unity를 한 실행 주체가 순차 제어한다. 사용자의 지정 경로와 현재 연결 Editor를 기본으로 하고, worktree 생성 여부는 실행 방식 선택 때 확인한다.169개 기통과 검사를 이유 없이 재실행하지 않는다.
- Draw70%/SetPass50%는 실험 목표다. 기능/픽셀·비용 측정 전 완료/전체 성능 통과로 쓰지 않는다.

## Review Focus

1. 피격이 원본 sharedMaterial/MPB를 복원할 때 배칭 재질과 서로 덮어쓰지 않는가: Task1의 hit restoration 검사.
2. 보행 중 Sprite.Create의 부분rect/회전/음수scale/flip이 atlas UV에서 그대로인가: Task2의 cutout/flip 픽셀 검사.
3. 같은 정렬키 사이 다른 body/효과 또는 SortingGroup이 끼면 배치를 끊는가: Task4의 interleaved/SortingGroup fallback 검사.
4. 두 카메라가 같은 frame에 다른 viewport로 그리면 이전 page/slot을 읽지 않는가: Task3/5의 camera generation 검사.
5. 저프레임에서 죽음→동일frame 재사용→씬 unload가 연속돼도 stale mask·GPU 리소스가 남지 않는가: Task3/5의 stale handle/lifecycle 검사.

## File Map

- `Assets/Scripts/CoreGuardianSpriteBatching.cs`: 지원된 기본 SpriteRenderer 재질만 매핑/복원, 트리/수명별 캐시. 원본/피격 재질을 임의 변경하지 않는다.
- `Assets/Resources/CoreGuardian/BatchedSpriteUnlit.shader`: 현재 URP Unlit 정점/색/flip/fragment/블렌드를 유지하고 해당 sprite 경로만 SRP 비호환으로 분리하는 후보. Lit는 원본을 유지한다.
- `Assets/Scripts/CoreGuardianSpriteAtlas.cs`: 정적 texture page/UV mapping, sprite cutout geometry와 원본 identity 구분. 실제 병목/픽셀 효과가 있는 경우만 runtime factory에 연결.
- `Assets/Scripts/CoreGuardianMaskPage.cs`: camera/frame별 영역·slot generation·GPU page 수명·명시적 fallback.
- `Assets/Scripts/CoreGuardianMaskOutputBatch.cs`: outline/contact 출력 등록, 정렬상 연속된 구간의 proxy geometry와 카메라별 복원.
- `Assets/Scripts/CoreGuardianBodyMaskCache.cs` / `CoreGuardianSelectionOutline.cs`: 기존 lease/정밀도/외부 API를 보존해 위 내부 경로 연결.
- `Assets/Resources/CoreGuardian/BatchedMaskOutput.shader` / `BatchedSelectionMask.shader`: per-vertex 개체 파라미터로 출력/합성. 기존 shader는 fallback 유지.
- `Assets/Game/Tests/EditMode/CoreGuardianSpriteBatchingTests.cs`, `CoreGuardianMaskPageTests.cs`, `CoreGuardianMaskOutputBatchTests.cs`: 아래 새 계약 검사. Unity가 만든 해당 `.meta` 포함.
- 본 폴더: 진단 코드·원본·단계별 결과/ledger. 제품 디버그 hook/테스트 전용 접근자를 추가하지 않는다.

### Task 1: A1 선택적 SpriteRenderer 배칭

**Interfaces:** `CoreGuardianSpriteBatching.Acquire(Material source): Material`, `Apply(Transform root): IDisposable`; 지원하지 않는 재질은 source를 반환한다. token Dispose는 여전히 자신이 설정한 매핑값인 renderer만 복원한다. 원본/피격/외부 변경을 덮어쓰지 않는다.
**Files:** 위 SpriteBatching/helper/shader/Tests. 연결 위치는 `CoreGuardianPrototype.cs`의 Start와 RentEnemyBody/포탑 최초 생성이며, 새 child sprite가 원본에서 재질을 상속하는 시점도 포함한다.

- [ ] `SupportedUnlitUsesOneSharedMapping_PreservesSourceProperties`, `UnknownOrLitMaterialReturnsOriginal`, `HitFlashThenRestoreRetainsBatchingMaterialAndPropertyBlock`, `ReleaseDoesNotOverwriteNewOwnerMaterial` 4개를 reflection으로 작성한다. 원본 참조/색/MPB 불변, 같은source→같은mapped, unknown/Lit→원본, 외부 덮어쓰기 보존을 assert한다.
- [ ] 공식 `run_tests(mode=editor, filter=CoreGuardianSpriteBatchingTests, filter_type=testName)`로4개가 아직 없는 mapper 때문에 실패하는 것을 저장한다.0건/컴파일 실패는 RED가 아니다.
- [ ] 위 API/shader를 최소 구현하고 같은4개를 GREEN으로 확인한다. URP 원본 include/인스턴스/색상 경로를 그대로 사용하며 `_Color` 등 SRP 적합성 변경 이외의 HLSL 계산은 바꾸지 않는다.
- [ ] 원본 shader와 후보를 같은 pose에서 렌더해 RGBA 픽셀0차이 확인. 기존 HitFlash/ObjectEmphasis/SelectionOutline 중 실제 영향 테스트만 실행한다. 실패 시 후보를 보류하고 Task2로 원인을 구분한다.
- [ ] A1 전후128적/15포탑·실제 fixed tick 3회 native 캡처. Draw/SetPass·CPU·FPS·GC/업로드/메모리를 비교해 채택/보류를 기록한다. 제품 적용 여부를 분리해 자체 파일만 로컬 커밋한다.

### Task 2: A2 texture atlas와 동적 sprite cutout

**Interfaces:** `CoreGuardianSpriteAtlas.TryMap(Sprite source, out Sprite mapped): bool`, `Release(): void`. 매핑 sprite의 vertices/pivot/PPU/border/rect/UV 의미는 원본을 보존한다. dynamic cutout 원본 texture/rect identity를 key로 사용한다. 변환을 증명하지 못하는 sprite는 false로 반환한다.
**Files:** SpriteAtlas/helper, 필요 시 `CoreGuardianOriginalWalk.cs`, `CoreGuardianOriginalMotion.cs`, `CoreGuardianPlayerMotion.cs`, `SegmentedEnemyHealthBar.cs`의 생성 경로, 새 SpriteBatchingTests 확장.

- [ ] `CutoutRectAndFlipRenderExactly`, `AtlasPointSamplingHasNoNeighbourBleed`, `UnmappedSpriteRetainsOriginal`, `SharedHealthStatesKeepAllFillPixels` 4개를 추가해 RED를 확인한다. 위치/scale/flip/PPU/부분rect를 포함해 전후RGBA 동일을 assert한다.
- [ ] 페이지는 Point/무압축/mipmap없음/회전없음/trim없음으로 만들고 원본 pixels를 재제작하지 않는다. packed source의 `textureRect`/UV를 확인해 runtime `Sprite.Create`가 잘못된 영역을 읽지 않게 한다. 페이지 가득 차면 새 페이지 또는 false fallback이며 sprite 수 제한은 없다.
- [ ]8개 새 A검사와 관련 보행/피격/타워 원본 검사 중 변경 경로만 GREEN 확인. X±/Y± 및 줌8.5/4.25 전후픽셀을 저장하고 동일조건3회 A1→A2 단계 성능을 기록/커밋한다.

### Task 3: B1 독립 mask page와 수명

**Interfaces:** 내부 `CoreGuardianMaskPage`의 `TryAcquire(Camera camera, int frame, int width, int height, out Slot slot): bool`, `IsValid(in Slot slot): bool`, `Release(in Slot slot): void`, `ReleaseCamera(Camera camera): void`. `Slot`은 page RT/정수rect/cameraID/frame/generation을 가진 값이다. 기존 `BodyMaskCache.Sample`의 Texture/ProducerRect/GeometryEpoch 의미는 유지하고 별도 atlas 영역을 확장한다.
**Files:** MaskPage/helper/Tests, BodyMaskCache 내부 연결.

- [ ] `RegionsNeverOverlapAndOverflowUsesAnotherPage`, `ReusedSlotRejectsOldGeneration`, `CameraFrameAndViewportNeverShareStaleSlot`, `DisableDestroyUnloadReleaseOnlyOwnedResources` 4개 RED 확인. 폭/높이가 page를 넘는 입력은 false와 기존fallback, 다른camera/frame slot 무효, 외부RT존속을 assert한다.
- [ ] page 초기2048²/ARGB32 Linear/Point를 후보로 사용하며 실제 format/최대texture 크기 확인 후 할당한다.2048 미지원이면 false. 패딩은 최대outline8+2로 고정하고 원래 texture 영역 clamp를 보존한다. 비용 절감으로 수용 개체 수를 제한하지 않는다.
- [ ] stale generation/sceneUnload/epoch 무효화를 기존 lease수명에 연결한다.4개 및 기존 BodyMaskSharing 계약 검사를 GREEN으로 확인한다. 이 단계는 아직 개별 mask 생산을 바꾸지 않는다.
- [ ] page 수/bytes/slot/확장/반환·재시작 잔류를 저장하고 수명 기능만 커밋한다. 성능 달성으로 쓰지 않는다.

### Task 4: B2 atlas 복사 출력 배칭

**Interfaces:** `CoreGuardianMaskOutputBatch.Register(CoreGuardianSelectionOutline owner, Camera camera, MeshRenderer source, Mesh quad, in CoreGuardianMaskPage.Slot slot, in OutputParameters parameters): bool`, `Release(owner): void`, `Flush(Camera camera): void`. `OutputParameters`는 kind(Outline/Contact), color,width,band,maskOriginPx,sourceSize를 가진다. 등록실패는 기존 source renderer를 유지한다.
**Files:** MaskOutputBatch/helper/Tests, BatchedMaskOutput.shader, SelectionOutline의 mask/output 연결.

- [ ] `TwoDistinctMasksKeepIndividualInteriorAndOutline`, `AtlasEdgesMatchOriginalClamp`, `InterleavedBodyBreaksBatch`, `SortingGroupOrForeignCameraFallsBack`, `ContactBandAndAlphaOverlapMatchOriginal`, `DestroyOrHideRemovesOnlyItsOutput` 6개 RED 확인.
- [ ] 기존 mask→page `CopyTexture` 중간 경로를 구현한다. sourceSize/UV 변환/경계clamp/outline color·width/contact band·origin을 정점속성으로 전달한다. page/패스/레이어/실제연속 정렬 구간이 같을 때만 geometry를 합친다.
- [ ] beginCameraRendering에서 proxy만 켜고 source를 숨겼다가 endCameraRendering/예외에서 원상복구한다. `IsVisible` 등 외부 계약은 기존 논리 가시성을 유지한다. 정렬 키가 같아도 다른 renderer와의 순서를 증명할 수 없으면 fallback한다.
- [ ]6개 GREEN과 기존 SelectionOutline/PhotoShadow/ObjectEmphasis/BodyMaskSharing 관련 검사를 실행한다. 실제 픽셀 겹침/가장자리/선택·피격·강화와 새proxy 실제 제출을 확인한다.
- [ ] A최종→B2 3회 동일성능 및 Frame Debugger 전후를 저장한다. CopyTexture 횟수/시간·추가page메모리 포함해 커밋한다. 이 중간 결과를 최종 직접생산 성과로 표시하지 않는다.

### Task 5: B3 mask 생산 geometry 병합

최종 실행 범위/검증/설계 변경은 [B_RESULTS.md](B_RESULTS.md)를 따른다. 개별RT 제거와 B2 런타임 출력 교체는 채택하지 않았고, 정상30R 자연 플레이/장시간/GPU·전체 잔류 검증은 미완료다. 아래 체크리스트는 최초 계획이며 완료 목록으로 해석하지 않는다.

**Interfaces:** MaskPage에 `Queue(in Slot slot, Mesh mesh, Material material, Matrix4x4 partMatrix, Matrix4x4 maskVP, Texture source, float opacity, float threshold): bool`, `Flush(Camera camera): void`를 추가한다. 샘플링 전에 해당카메라의page clear/생산이 완료돼야 한다. 기존 `Publish/Abort`는 성공완료/실패fallback을 구분한다.
**Files:** MaskPage, BodyMaskCache, SelectionOutline.DrawMaskParts, BatchedSelectionMask.shader, MaskPageTests 확장.

- [ ] `CombinedProducerMatchesEveryIndependentMaskPixel`, `DifferentAlphaThresholdsStayIndependent`, `CameraChangesFlushOnlyItsCommands`, `SameFrameReturnAndReuseNeverShowsOldMask` 4개 RED 확인.
- [ ] 기존 GPU 부품행렬곱을 유지해 geometry vertex attributes로 object crop/page rect/threshold/opacity를 전달한다. 페이지별 clear1회, 가능한 같은shader/입력atlas geometry1회.16bit index초과는 분리한다. 지원하지 않는 texture/geometry는 기존 즉시생산 경로다.
- [ ]4개 GREEN 뒤 B2 복사경로 대비 픽셀0차이를 먼저 확인한다. 실패 조건은 fallback으로 좁히고 원본을 삭제하지 않는다. 생산배칭이 검증된 조건의 개별RT/CopyTexture만 제거해 중복메모리를 줄인다.
- [ ] B2→B3 및 최초6495a42제품 대비3회 캡처/Frame Debugger/풀·수명/GC·메모리 결과를 각각 기록하고 제품 단계를 커밋한다.

### Task 6: 통합 품질·실부하·인수인계

**Interfaces:** Task1/2는 기존SpriteRenderer 외부계약, Task3/4/5는 기존Present/lease계약에만 의존한다. 서로의 비공개 구현을 복사하지 않는다.
**Files:** 본 폴더의 `RuntimeProbe.cs`/`PixelComparison.cs`(기존 정본을 새ID로 복사·확장), 분석/결과, `UNITY_HANDOVER.md`.

- [ ] 새22개 회귀검사 결과와 변경에 관련된 기존 검사 건수를 실제 tree와 대조해 실행한다. 무효0건은 통과가 아니다. 기존169개 전체 재실행을 추가하지 않는다.
- [ ]128적/15포탑·고정dt·주기교체·워밍업60/관측180×3을 전후같게 고정한다. source/CSV/설정/씬 hash·동등 outcome·native/Main/Render Thread/GC/Draw/SetPass·최소/최대/평균FPS·분포를 보존한다. 프로파일러 overhead·경계누락·실패/악화도 표에 포함한다.
- [ ] 줌8.5/4.25·원점/화면경계·겹침·3병종/보스·정지/방향/선택/피격/강화를 같은위상 픽셀로 비교한다. 기존미확정 보스9건·사람조작/실제청취를 자동통과로 쓰지 않는다.
- [ ] 정상30R/장시간 Update 부하·반복 재시작/씬종료 잔류 검증을 기존 미완료에서 이어간다. 유효 GPU 계측 미지원이면 Render Thread/CPU draw 조사만 보고한다. 목표기기/FPS 예산 미정은 전체통과로 쓰지 않는다.
- [ ] 전체branch fresh-context review 후 중요한 결함은 한 번의 수정 단계에서 RED→GREEN/대상검증으로 해결한다. 실행방식이 native면 구현은 한 주체가 수행하고 최종review만 분리한다.
- [ ] 자체 임시 Play/배속/카메라/시간/Profiler/배경/소유GPU리소스만 복원한다. 사용자diff·hash일치/콘솔/씬상태·기록링크를 확인해 인수인계와 완료커밋을 남긴다. 승인된 기능/실측/미완료를 구분한다.

## 자체 검토 / 실행 방식

설계의 A/B·출력중간단계·직접생산·fallback·수명·픽셀·성능·잔류/미지원GPU 항목을 Task1–6에 연결했다. Task3의 Slot을 Task4/5가 소비하고 Task4 OutputParameters는 Task5에서 변경하지 않는다. 테스트4+4+4+6+4=22개, 실제 runner가 parameterized case를 추가하면 계획기대건수와 실제 case수를 별도 기록한다.

계획 작성/자체 검토 후 사용자가 **“계획 확인, 직접 순차 구현”**으로 승인했다. 실행방식은 Native(현재 대화에서 직접 순차 구현, 마지막 독립 리뷰)다. Task3–5가 같은 mask/slot/정렬 계약을 연속 수정하고 같은 Editor를 공유하므로 구현은 한 주체가 순차 수행한다. 실제 작업 진행은 계획 소유 ledger와 날짜별 결과/커밋에 기록한다.
