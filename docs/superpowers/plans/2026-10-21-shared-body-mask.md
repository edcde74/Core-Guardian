# Shared Body Mask Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** 동일 본체의 호환되는 프레임 내 마스크 합성을 재사용하고 기존 효과 픽셀·표시 순서·수명을 유지한다.

**Architecture:** 캐시가 본체별 마스크 RT 세대와 binding을 소유한다. SelectionOutline은 기존 합성 코드를 miss에만 실행하고, 각 소비자의 quad/부분 UV/PropertyBlock을 갱신한다. 같은 프레임의 다른 pose와 참조 중인 이전 세대는 분리한다.

**Tech Stack:** Unity6000.6.2f1, URP17.6, C#, 기존 CommandBuffer/RenderTexture/Profiler, 공식 Unity CLI. 패키지 추가 없음.

**실행 상태:** 사용자 Native 승인으로 구현/자동 검증 완료. 품질 실패에 따른 아래 실행 조정과 결과 정본을 따른다. 실제 키보드8방향/사람 플레이·정상30R·GPU VRAM은 미검증으로 남기며 해당 절차를 완료로 해석하지 않는다.

**Spec:** [승인된 설계](../specs/2026-10-21-shared-body-mask-design.md). 사용자가 설계 검토 요청 뒤 `진행해`로 승인했다. 계획 작성 당시에는 제품 미변경이었고 이후 Native 실행을 승인받았다. 제품 기준47ca358, 문서 기준011d866.

## Global Constraints

- 원본 아트/방향/보행/30R 물량/밸런스/HUD/효과 수·수명/사운드/입력 정책은 변경하지 않는다.
- 기존 공개 Present/Overlay/Contact 진입점과 null 재질 fallback을 보존한다.
- 실행 순서를 바꿔 공유를 강제하지 않는다.
- 다른 시스템이 쓰는 `Transform.hasChanged`를 지우지 않는다.
- 본체별 비참조 free record는 최대1개만 보관한다. 마지막 binding 반환 때 모든 해당 캐시 자원을 해제한다.
- 사용자 Play/다른 검사를 중단·중복하지 않는다. 같은 Unity 조작은 한 주체가 순차 수행한다.
- 아틀라스 자산/원본 import 설정/씬 YAML/패키지/전역 렌더 설정은 변경하지 않는다.
- 빌드·실행파일 실행·push 없음. 기존 사용자 설정2파일·미추적·기원 불명 native 잔류 보존.
- 과거162.609ms 지연/보스 전체 합성 미확정9건의 해결을 이번 기능 성공으로 확대하지 않는다.

## Review Focus

1. 수동 Present를 쓰는 disabled 소비자: 컴포넌트 enabled=false여도 호출은 동작하고 GameObject 비활성화/소유자 제거 시 binding을 안전하게 반환해야 한다(Task3).
2. GPU 합성 실패/미완료 record: publish 전 결과를 다른 소비자에게 hit로 제공하지 않아야 한다(Task2).
3. 같은 Camera의 targetTexture 교체/크기 변경: Camera 참조가 같아도 이전 마스크를 재사용하면 안 된다(Task2).
4. null custom material/전체 숨김/스캔→선택 전환: 기존 fallback과 PropertyBlock 초기화가 유지돼야 한다(Task2).
5. 마지막 사용자 한 개의 반복 갱신: 매 프레임 RT를 파괴·재할당하지 않으며 옛 binding 이중 반환이 새 세대 참조를 손상시키지 않아야 한다(Task3).

## 파일 경계와 내부 인터페이스

- Create `Assets/Scripts/CoreGuardianBodyMaskCache.cs`/meta: 캐시·RT record·수명·통계만. 셰이더 표시나 전투 규칙 없음.
- Modify `Assets/Scripts/CoreGuardianSelectionOutline.cs`: 소스/요청 구성, miss의 기존 DrawMaskParts 호출, 출력 UV/원점, binding 반환. 기존 기하/메시/명령 버퍼를 불필요하게 전부 옮기지 않는다.
- Modify `Assets/Resources/CoreGuardian/PhotoShadowContact.shader`: 기본0인 `_MaskOriginPx`로 접촉 밴드의 로컬 원점 보정.
- Modify `Assets/Scripts/CoreGuardianPhotoShadow.cs`, `CoreGuardianObjectEmphasis.cs`: 필요한 비활성화/명시 invalidate 연결만.
- Create `Assets/Game/Tests/EditMode/CoreGuardianBodyMaskSharingTests.cs`/meta: 기존 검사처럼 reflection으로 Assembly-CSharp 타입 접근, 명시적 private native cleanup.
- Create `docs/development/shared_body_mask_20261021/`: 기존 Probe/PixelComparison/MemoryLifecycle/FrameProbe와 분석 도구를 새 경로로 재사용. 새 범용 테스트 실행기 없음.

캐시 내부 API는 아래 이름/의미를 사용한다. Task2/3에서 같은 정의를 공유한다.

```csharp
internal static class CoreGuardianBodyMaskCache
{
    internal sealed class Lease { /* 소비자당 한 번 생성, 현재 record binding */ }
    internal readonly struct Request { /* artwork, camera, frame, matrices, viewport/target descriptor, raw threshold, atlas/revision, reusable source snapshot, requested rect */ }
    internal readonly struct Sample { /* Texture, ProducerRect, Capacity, GeometryEpoch */ }
    internal static bool Begin(Lease lease, in Request request, out Sample sample); // true=완료 record hit, false=합성 예약
    internal static void Publish(Lease lease); // 성공한 miss만 공개
    internal static void Abort(Lease lease); // 미완료 record 폐기/반환
    internal static void Release(Lease lease); // 중복 반환 안전
    internal static void Invalidate(Transform artwork); // 다음 요청 miss, 기존 참조 세대는 보존
    internal static void ResetOwned(); // 캐시 출력 숨김/lease 반환 후 캐시 자원만 해제
}
```

Request의 소스 snapshot은 renderer 참조, sprite/texture 참조와 updateCount, enabled/active/alpha/flip/실제 행렬을 재사용 버퍼에 저장한다. 각 record는 비교용 값을 복사해 보존한다. 기하 변경은 명시 invalidate와 GeometryEpoch로 전달한다. 본체 entry를 재생성해도 epoch가 재사용돼 이전 소비자의 기하 캐시가 유효해지는 일이 없어야 한다. Pose 비교는 해시만으로 통과시키지 않는다.

## Task 1: 기준과 실패 계약 확보

**Files:** 새 진단 폴더의 README/Probe/전 캡처, 새 CoreGuardianBodyMaskSharingTests.cs.

**Interfaces:** 기존 `Present`, `PresentAtOrder`, `PresentGroundContactWithBand`, private mask/lastMaskDrawCalls를 사용한다. 캐시 존재를 전제하지 않는 소비자 결과부터 검사한다.

- [x] 실제 HEAD/diff·연결/씬/dirty/Play/컴파일/검사/Profiler를 조회하고 작업 전 설정3파일 해시·native 상태·코드 해시를 저장한다. 사용자 작업 중이면 독립 준비만 한다.
- [x] 기존 `shared_mask_materials_20261021/Probe.cs`의 셋업/Profiler 설정을 유지해 새 폴더에 baseline을 수집한다. 128적/15포탑,1920×1080,카메라8.5,배속1,준비30/측정100×3. 완료/error=null과 실제100×3·native 유효 프레임을 확인한다. 자체 Play만 종료·복원한다.
- [x] `SameBodySameFrame_ReusesOneMask`와 `ContactPad4_EmphasisPad3_ReadsInnerPixels`를 작성한다. Assert는 두 소비자의 private mask `Is.SameAs`, 첫 소비자 생성 횟수1/후속0, 서로 다른 quad 범위, UV 원점 차이 정확히1texel이다. 서로 다른 body의 `Is.Not.SameAs`도 검사한다.
- [x] 공식 `run_tests --mode editor --filter CoreGuardianBodyMaskSharingTests --filter_type testName` 실행 후 test_status의 실제 테스트 수/실패 메시지를 저장한다. 현재 개별 RT로 인해 공유 assert가 실패해야 RED다. 컴파일/0건은 RED가 아니다.
- [x] baseline·실패 계약을 파일 지정 로컬 커밋으로 남긴다. 사용자 설정은 포함하지 않는다.

## Task 2: 공유 합성과 부분 UV 연결

**Files:** CoreGuardianBodyMaskCache.cs, CoreGuardianSelectionOutline.cs, PhotoShadowContact.shader, 공유 계약 파일.

**Interfaces:** 위 Begin/Publish/Abort/Request/Sample. 기존 공개 Present 계열은 유지한다. SelectionOutline의 `internal void ReleasePresentation()`은 출력 비활성화·CurrentTarget 초기화·binding 반환을 담당한다.

- [x] threshold/카메라/동일 프레임 이동·회전·sprite·alpha·flip·텍스처 갱신/다른 본체/영역 역순 계약을 추가한다. 입력 변경 뒤 `Is.Not.SameAs` 또는 generation 증가, 이전 소비자 RT의 픽셀 불변을 assert한다. RGB만 변경하면 hit를 유지한다.
- [x] `UnpublishedRecord_IsNeverReturnedAsHit`, `SameCameraNewTargetOrSize_ForcesMiss`, `NullOverlayAndModeSwitch_PreserveFallback` 계약을 추가한다. Begin 미완료 결과를 두 번째 lease에 hit로 주지 않고, camera target descriptor 변경은 miss, null overlay는 기존 outline 상태가 되어야 한다.
- [x] Begin을 구현한다. snapshot/카메라 조건 동일·같은 frame·포함 rect·유효 RT에만 hit. miss는 미참조 free record 또는 새 RT를 예약하며 참조 중인 다른 세대는 덮어쓰지 않는다. RT는 기존 ARGB32/Linear/Point/Clamp/no-mip·capacity 재사용/전체 clear를 유지한다.
- [x] `PresentMask`의 소스/기존 경계와 requested lo/hi 계산은 유지한다. Begin hit면 DrawMaskParts/Execute를 생략한다. miss면 기존 명령을 record RT로 실행 후 Publish, 실패면 Abort와 binding 정리를 한다. 마커와 실제 생성/clear/제출/hit·miss 이유 통계를 추가한다.
- [x] quad의 기존 월드 좌표를 유지한다. UV 원점 `(requestedLo-producerLo)/capacity`, UV 크기 requested size/capacity를 사용한다. `_MaskOriginPx`는 각 출력 PropertyBlock에서 매번 초기화한다. 접촉 shader는 `uv.y*capacityHeight-offsetY`로 밴드만 보정한다.
- [x] raw threshold/source 필터, atlas revision/fallback, null material, 색/두께/표시 순서/스캔 world Y를 보존한다. 비지원 stereo/dynamic-resolution/비호환 조건은 독립 합성한다. 같은 frame의 호환되지 않는 요청을 stale hit로 만들지 않는다.
- [x] 새 계약을 GREEN까지 확인하고 기존 공유 재질2·아틀라스2·아웃라인3 계약을 관련 필터로 순차 검증한다. failed/0건이면 원인을 좁히고 제품 검증 완료로 표시하지 않는다. 로컬 구현 커밋을 남긴다.

## Task 3: 무효화·생존자·전환 수명 완성

**Files:** 캐시/SelectionOutline, PhotoShadow/ObjectEmphasis 연결, 공유 계약 및 필요 시 기존 cleanup.

**Interfaces:** ReleasePresentation/Invalidate/ResetOwned, Sample.GeometryEpoch. 소비자는 RT를 직접 Release/Destroy하지 않는다.

- [x] `DestroyProducer_LeavesBorrowerReadable`, `LastRelease_DisposesOwnedRecords`, `SameFramePoseChange_PreservesOldReader`, `SoloOwnerNextFrame_ReusesCapacity`, `RepeatedRelease_DoesNotTouchNewGeneration` 계약을 작성하고 실패를 확인한다. 생존자 출력 pixels 유지, 마지막 native RT/lease 수 기준 복귀, 반복 frame의 RT 새 할당0을 assert한다.
- [x] `DisabledManualConsumer_CanPresentThenRelease`, `ReleasedHardware_RegeneratesBeforeHit`, `GeometryAndAtlasInvalidation_RebuildsAllConsumers` 계약을 추가한다. enabled=false 수동 호출은 동작하고 GameObject 비활성화·target null/교체·폭0/색alpha0·삭제 시 반환한다. lost RT는 clear/재합성한다. 명시 geometry invalidate는 기존 기하/메시 캐시도 새 epoch로 갱신한다.
- [x] 새 binding 확보 후 이전 binding을 반환한다. 소비자 단독 참조의 다음 frame은 자기 RT를 재사용하고, 두 소비자의 이전 세대가 남아 있으면 다른 record로 전환한다. 본체당 미참조 free는 최대1개, 마지막 binding0이면 모든 해당 캐시 자원을 해제한다.
- [x] OnDisable/OnDestroy/무효화와 PhotoShadow 호스트 비활성화를 연결한다. disabled 소비자의 수동 Present를 막지 않는다. 기원 불명 기존 RT/메시를 캐시 정리 대상으로 포함하지 않는다.
- [x] scene/Play 전환을 캐시 epoch와 일회 등록 hook으로 처리한다. ResetOwned는 캐시가 추적하는 자기 출력부터 숨기고 binding을 반환한다. domain/scene reload 옵션의 시작값을 저장하고 각 변경한 조합의 동작을 확인한 뒤 정확히 복원한다. 사용자 진행 중 Play를 종료해 검사하지 않는다.
- [x] warmed 상태 GC.Alloc을 캐시 마커 하위에서 관측한다. reflection 호출 자체의 할당을 캐시 할당으로 합산하지 않는다. 매 frame 새 lease/배열을 만드는 구현은 버퍼 재사용으로 고친다. 관련 계약 GREEN과 임시 상태 복원 후 로컬 커밋한다.

## Task 4: 실제 공유 픽셀·CPU·수명 검증과 완료 기록

**Files:** 새 진단 폴더의 기존 도구 복사본/원본/RESULTS, 영향받은 기존 검사, UNITY_HANDOVER.md.

**Interfaces:** 캐시가 제공하는 hit/miss 이유·생성/clear/submit 수·binding 수·distinct bound RT와 allocated RT(대기 포함) 통계를 사용한다. private mask 참조 합산을 실제 할당 수로 취급하지 않는다.

- [x] Task1과 같은 조건/도구의 후100×3을 수집한다. CPU/GC/호출 수·frame 평균/중앙/p95/p99/최대·FPS 최소/최대/총프레임÷총시간·유효 native 프레임을 전후 계산한다. 제외/실패/악화를 보존한다. Frame Debugger는 별도 진단에서 실제 clear/draw/소비자 표시 수를 확인한다.
- [x] 기존8역할3,456 효과 픽셀 대조를 새로 수행한다. 실제 접촉+강조 쌍의 공유 hit를 검사에 포함하고, 화면 가장자리/다른 여백/동일 frame 변경/선택·스캔 중첩을 추가한다. 실제 키보드8방향 입력은 미검증이며 결과 정본의 한계로 남겼다. 보스 전체 합성의 과거 미확정9건은 별도 대조군 판정으로 남긴다.
- [x] 차이가 있으면 해당 원인을 조사하고 호환 경로를 독립 합성으로 돌린다. 기본 병사/플레이어 공유 hit가 사라지면 이 결과를 목표 미달로 표시한다. 픽셀 오류를 허용하거나 입력/표시 횟수를 줄이지 않는다.
- [x] 기존600frame×3 줌8.5→4.25→8.5/초기화90frame 도구를 재사용해 binding·distinct bound·allocated/free RT·bytes/피크·생성/해제·공유 재질/메시를 기록한다. 종료 후 자기 cache lease/owned record0을 확인한다. 전체 native 잔류가0이라는 판정으로 확대하지 않는다.
- [x] 기존 그림자10/강조3/선택3/공통 재질2/아틀라스2는 현재 목록의 기대 수를 확인해 관련 필터로 순차 실행한다. 추가 공유 계약 수는 실제 작성한 NUnit 목록으로 확정한다. 새 범용 검사 실행기를 만들지 않는다.
- [x] requesting-code-review로 완성 diff를 읽기 전용 검토받고 중요한 결함을 수정한다. 실제 변경/새 실패가 있을 때 필요한 검사만 재실행한다. Unity 조작은 계속 한 주체가 담당한다.
- [x] Profiler 원본을 검증된 압축+해시로 보존하고 source/settings 해시·원래 사용자 diff·종료 상태를 대조한다. GPU/VRAM 무효 값은 N/A. 결과 정본과 인수인계에 구현/실측/미완료를 구분해 기록한 뒤 이번 파일만 완료 로컬 커밋한다. 빌드/실행파일/push 없음.

## 계획 자체 검토와 실행 선택

설계의 포함 영역/같은 frame 변경/원점/불변 세대/마지막 해제/free 제한/0 신규 frame 할당/검증·한계는 Task1~4로 연결했다. Review Focus 5개는 Task2/3에 대응 계약을 배치했다. 공개 API와 내부 인터페이스 이름은 위 정의를 공유한다. 탄환·에너미 풀링이나 HUD 최적화를 포함하지 않는다.

권장 실행은 **Native: 이 대화에서 제가 Task1→4를 순차 구현하고 마지막에 독립 리뷰**다. RT/셰이더/소비자 연결의 의존성이 높고 같은 Unity 조작은 병렬화하면 안 되기 때문이다. 대안은 task별 새 구현자·리뷰어를 사용하는 Subagent-driven이다. 어느 쪽이든 Unity 조작은 한 주체만 수행한다. 사용자의 Native 선택에 따라 executing-plans로 순차 실행했다.

## 실행 중 품질 근거에 따른 조정

2026-10-21 Native 실행 승인. 실제 포함 영역 공유는 좌표16 보스에서도 원본 마스크3픽셀 차이를 만들었다. 확대/세로/원거리만 제외하는 초기 보완으로 일반 동등성을 보장할 수 없었다. 따라서 캐시는 **생성 영역이 정확히 같은 경우만 공유**한다. 접촉 전용 생산자는 검증한 게임 뷰(1920×1080, ortho8.5/4.25, 카메라/본체XY±32, 정수 viewport, stereo/dynamic resolution 없음)에서 강조의 pad3 생성 영역을 사용하고 원래 pad4 출력 quad·로컬 band는 유지한다. 이외 뷰와 비양수 compact 영역은 원래 접촉 합성을 유지한다. 일반 outline의 서로 다른 생성 영역은 독립 합성한다. 실행 순서·표시 수·아트 자산은 유지한다. 변경된 긍정 계약은 실제 접촉→강조 쌍으로 확인하며 native 픽셀과 비용·수명을 다시 수집한다. 후보의 성능 수치는 최종 성과로 사용하지 않는다.
