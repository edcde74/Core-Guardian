# CPU 마스크 준비 A — 미채택 / 시작 원본 복구

> 최신: 사용자 승인으로 수정한 격리 도구의 두 사례를 총8회 검사해 원본자기/후보자기/교차픽셀차이0,윤곽외활성Renderer0을 확인했다. 연결거절후원본복구하고 추가승인받은후보4회만완료. [격리검사 정본](isolation_check/README.md). 전체512·성능3회미검증,제품원본복구유지.

> 후속 원인 분리 조사: 승인된 두 사례8렌더는 원본/후보 모두 픽셀차이0. 지난원본과 이번원본의 출력이 다르고, 검사에서 포탑발광/음영MeshRenderer를 남긴 격리오류를 확인했다. 지난74건의 구체적 픽셀차이 원인은 미확정이며 제품회귀로 단정하지 않는다. [실제증거·검사수정안·한계](diagnosis/README.md). 후보채택·성능측정은 여전히 미완료,제품원본복구유지.

2026-10-24, HEAD `1de8ced`. [계획](../../superpowers/plans/2026-10-24-mask-cpu-preparation.md).

## 결과

호출 내부 입력 버퍼·cache overload 후보를 구현했으나 출력 픽셀 동등성 기준 미통과로 중단했다. 제품 결함인지 검사 환경 문제인지는 미확정이다. **후보 성능3회는 미실행이며 절감 ms/율·FPS 개선·GC/메모리 무회귀는 미검증**이다. 두 제품은 HEAD가 아닌 시작 working-tree 원본으로 복구했다. 후보·신규 테스트는 candidate 소스 및 `rejected_sources/`에 Assets 밖 보존했다.

## 승인·실패 이력

계획 실행 승인 후 고정 마커·원본 warm15+sample90×3을 수집했다. 원본2 첫 시도는312×548 조건오류로 표본0 종료·복원, 후속 반복 중단. 사용자 임시1920×1080 고정·실패2 단회 재실행·3 진행 승인 후 완료했다. Game View 선택 복원 기록 보존.

별도 Task2·3 승인으로 A 구현·회귀 테스트를 진행했다. 최초512쌍은 mask/geometry/rect 모두 같지만 composite 출력 해시265건 차이. EditMode의 보류 출력 batch 등록 경로가 섞여 비교 불가로 기록했다. `pixels_before/after.json`, `pixel_comparison.json` 보존.

사용자가 검사 경로 보정 후 **한 차례**512쌍 재비교를 승인했다. private PresentMask 직접 호출로 RegisterOutput을 제외하고 mask 준비 이후 source SpriteRenderer를 꺼 original outline 출력만 비교했다. 원본/후보512건 정상 완료. mask/geometry/rect 차이0, **출력 해시74건 차이**. `pixel_direct_comparison.json` 참조. 다시 실패했으므로 추가 비교·원인 추적·후보 측정 없이 중단·복구했다. 전체 body 합성/HUD/외부 투명 동률 동등성 통과가 아니다.

## 원본 비용 — 후보 절감량 아님

적128(Attack112/Buff16)·타워15,1920×1080/ortho8.5,고정simulation dt1/60,일반 current-thread ProfilerRecorder,recording/Deep OFF. 각warm15/sample90,유효270표본,engine gap모두1.

|평균 ms|원본1|원본2|원본3|
|---|---:|---:|---:|
|Mask.Present|4.872|4.878|4.882|
|SourceFilter|0.260|0.257|0.259|
|BoundsCrop|0.835|0.851|0.859|
|CacheLookup|0.988|0.977|0.988|
|Output|1.338|1.353|1.346|
|QuadUpload|0.295|0.305|0.304|
|PageProducer|0.497|0.460|0.497|
|SpriteReferences.Read|1.032|1.030|1.054|
|전체 wall frame|24.921|25.022|25.107|
|FPS 최소/최대/평균|14.44/55.51/42.32|17.94/55.74/42.35|17.56/52.51/41.73|

중첩 범위는 합산하지 않는다. QuadUpload는 Output 내부 vertices/RecalculateBounds/uv setter 두 scope. SpriteReferences.Read는 다른 소비자 포함. A 관련 준비1ms 이상이 구현 진입 근거였으며 전체가 제거 가능하다는 뜻은 아니다. QuadUpload 약0.30ms여서 B만으로 큰 절감 목표를 충족한다는 근거는 없다.

GC 수거 각1회, GC Allocated In Frame 평균387351/242852/57324B로 실행별 변동이 크다. Editor 결과이며 목표기기·GPU·장시간 결과가 아니다. [구간 통계](baseline_summary.json), [프레임/GC 통계](baseline_frame_summary.json), before_1/2/3 및 환경·종료·복원 JSON이 정본이다.

## 검증·검토·한계

신규API RED3실패→GREEN3통과, 기존BodyMaskSharing31·MaskProducer9·SelectionOutline3통과. 실제PlayMode QueuePage 소유 characterization1통과: 동일버퍼 재캡처 후 이전 queued matrix/alpha 출력 보존. 총47통과, raw mask*.json 보존. callback bounds만 원래 Encapsulate roundoff 때문에1e-6 허용, 픽셀 정확해시 유지.

독립검토 확인된 제품결함 없음. Important였던 실제QueuePage 연결 공백은 PlayMode 테스트로 보강했다. 활성/부품 변경·모든 public Present 조합 전수검증은 미완료. sample 구조체 목록과 owner별 buffer는 메모리 증가 가능성이 있으며 용량/잔류 실측은 픽셀실패 중단으로 미완료다. 메모리 무회귀 주장 없음. 검토가 픽셀 통과를 대체하지 않는다.

## 복구·다음 경계

protected_after.json: 보호10파일 동일·SelectionOutline 시작원본 hash동일(Cache는 보호목록 포함). 복구 재컴파일 성공. final_state.json: Play/changing/compiling/updating false,단일전투씬7roots/dirtyfalse,recording/Deep/profileEditor/FrameDebugger OFF,backgroundfalse/capture0/scale1,1920×1080,nativeBatchOFF.

기존 미커밋·강조/그림자/TMP·A보류/B제한·아틀라스보류·원본/fallback 유지. 빌드/실행파일/커밋/push/Notion 갱신 없음. 다음은 실패 원인/검사 신뢰성의 별도 조사 범위를 결정하는 것이다. 추가 조사·렌더 비교·측정·구현·후보 자동 재활성화 승인 없음. 이번 큰CPU절감 입증0.
