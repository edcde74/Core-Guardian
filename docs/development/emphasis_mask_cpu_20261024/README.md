# 강조·마스크 CPU A 실측과 다음 해결안

2026-10-24. HEAD1de8ced, 기존 미커밋 보존. 사용자 승인 범위: 격리512조합 비교1회, 유효 원본3회 조건 확인 후 재사용, 후보warm15/sample90×3, 효과 부족 시 일반Profiler warm15/sample90 1회·상위2프레임 제한 분석, 추가 수정안 구체화. 필요한 재로딩·자체Play/종료·임시1920선택·복원 포함. 새 구조 수정은 원인/변경안을 먼저 제시한다. 빌드/커밋/push 없음.

최신: [B1/B2 독립 실행 결과·복원](B1_B2_RESULTS.md). 아래는 직전 A 실험 기록이다.

## 결과와 판정

**픽셀 문제는 해결됐고 CPU는 줄었지만, 큰 비용 목표에는 부족하다.** A는 채택하지 않고 제품을 시작 원본으로 복구했다. 다음은 추가 스냅샷을 늘리는 대신 기존 cache 입력을 재사용하고, 변하지 않는 출력 속성의 재구성을 줄이는 수정안이다. [구체적 다음 제안](NEXT_PROPOSAL.md).

|일반모드 직접비용|원본3회 평균|후보3회 평균|관측 절감|
|---|---:|---:|---:|
|Mask.Present|4.877387ms|4.544640ms|0.332747ms /6.822%|
|Emphasis.Present|5.923614ms|5.549576ms|0.374039ms /6.314%|
|CacheLookup|0.984119ms|0.605628ms|0.378491ms|
|Output|1.345681ms|1.211071ms|0.134610ms|
|BoundsCrop|0.848276ms|1.272189ms|0.423914ms 증가|

Emphasis에는 Mask가 포함되므로 두 절감량을 합산하지 않는다. BoundsCrop에는 후보의 입력 Capture가 들어간다. CacheLookup/Output이 줄어든 반면 Capture 쪽 증가가 일부를 상쇄했다. 기존 목표1ms와 최소0.5ms의 큰 비용 목표에 미달했다.

Mask 실행 평균 범위는 원본4.871869~4.882064ms, 후보4.525322~4.569427ms다. 관측 차이는 이번 실행 범위보다 크지만 순차 Editor 측정에서 변경하지 않은 Tint 등도 변동했으므로 전체 프레임 차이를 후보의 인과 효과로 배분하지 않는다.

## 조건·원자료·전체 프레임

Unity6000.6.2f1/Ryzen5600X/RTX3070/약32GB,1920×1080/ortho8.5,VSync1,target−1,고정simulation dt1/60,적128(Attack112/Buff16)/타워15. 일반 성능은 recording/Deep OFF,current-thread ProfilerRecorder다. 원본은 직전 동일 마커/부하의3회, 후보는 이번3회이며 각warm15/sample90. 각90개/engine gap1/개수 조건 정상이다.

|실행|frame평균ms|FPS 최소/최대/평균|GC수거0세대|
|---|---:|---|---:|
|원본1|24.920893|14.444/55.514/40.127|1|
|원본2|25.021559|17.945/55.744/39.966|1|
|원본3|25.107438|17.559/52.510/39.829|1|
|후보1|23.744218|20.439/59.808/42.116|1|
|후보2|22.638841|19.417/58.625/44.172|0|
|후보3|23.235390|21.251/59.306/43.038|1|

평균FPS는표본수×1000/프레임ms합. 원본GC평균387351/242852/57324B,후보70431/178708/56117B로간헐할당이섞인다. 기존원본과후보의시간대/Editor/수거차이로전체frame개선전부를A의성과로확정하지않는다. 원본에반영한그림자/강조/TMP변경을새성과로다시계산하지않는다. GPU/순수RenderCPU/목표기기60FPS는미검증이다.

성능정본: [실행별통계](performance_runs.json), [구간평균·변동](performance_summary.json), [프레임/GC](frame_gc_summary.json), [분석코드](Analyze.ps1), after_1/2/3.json과환경·완료·복원JSON. 이전원본은 [직전폴더](../mask_cpu_preparation_20261024/README.md)의before_1/2/3를사용했다. Benchmark.cs는동일부하/프레임정의를유지하고마지막표본후에만버퍼메타데이터를수집한다.

## 픽셀 검증

원본/후보 각512사례,8역할×4방향×4위상×2줌×단독/겹침. **mask/geometry/rect/outputhash 차이0,예상밖활성Renderer0,빈후보출력0**. body의모든Renderer는mask생산뒤끄고별도윤곽출력집합만활성인지강제검사했다. pixels_before/after.json 및 각사례의rawRGBA·PNG를보존한다. pixel_comparison.json 참조.

이것은격리된윤곽출력비교다. body합성·OverlayHUD·모든효과중첩통과로확대하지않는다. 후보제품소스는직전47대상테스트/두사례격리검사의보존본과동일하므로무관한기통과테스트를반복하지않았다. 이번에는후보실제일반3회와512범위를새로확인했다.

## 왜 큰 절감이 안 나왔는가

1. **별도Snapshot 생성 비용이 생겼다.** getter중복을줄이지만144B sample목록을만든뒤기존cache Part로다시복사한다. 일반전후에서는BoundsCrop이약0.424ms늘어CacheLookup/Output절감일부를상쇄했다.
2. **큰 고정 준비 작업이 남았다.** source수집뒤에도Request/지원조건·페이지획득·geometry·출력MPB native설정을개체마다계속수행한다. 이번A는그구조를줄이는후보가아니었다.
3. **메모리 추가가 확인됐다.**146owner,668sample,capacity1092,stride144B로값배열payload만157248B(153.56KiB)가추가된다. list/객체header·다른cache/전체heap·GPUpeak는제외다. fallback batch용량은이번부하에서0. 새목록비용을숨기고무메모리회귀라고할수없다. buffer_summary.json과각after_N_buffers.json 참조.

## 일반 Profiler 1회로 확인한 남은 구간

diagnostic.json의90표본,Deep/profileEditor/할당스택OFF,recordingON. 진단용고정마커를추가했으므로앞의recordingOFF일반성능과직접절감률을계산하지않는다.

|진단구간|평균ms|해석|
|---|---:|---|
|CaptureSources|약0.79|필드읽기와144Bsample목록구성의합.각getter/복사단독비용으로세분한값은아님|
|QueueInputs|약0.78|지원조건·page획득·shape/geometry조회·queue입력|
|OutputGeometry|약0.69|screen/world변환·quad/UV계산/업로드|
|OutputBinding|약0.55|MPB재구성·적용·출력material/enabled|
|ProjectionCrop|약0.57|bounds·8corner투영·crop|
|RequestInput|약0.14|camera/target/atlas메타데이터수집|
|OutputSorting|약0.10|정렬선택/설정|
|Emphasis.Structure|약0.89|구조참조검사와활성source목록재구축.일부Read중첩|

Trace.cs/Outline_A_diagnostic.cs/Emphasis_diagnostic.cs가실행소스다. 상위2개nativeview는5141/6739샘플을10/3ms에스캔해2만샘플·1초상한내처리했다. **선택값하나가profilerFrame−1이었다.** 유효latest-view는얻었지만정확한상위표본과의대응을확정할수없으므로'고정상위2프레임귀속완료'로보고하지않는다. 다른view는frame7. phase평균은별도90개유효Recorder표본이며이alias문제와구분한다. 저장native범위도0..87의88프레임으로counter90행과1:1이라고주장하지않는다. 자동추가Profiler/추출반복은하지않았다. 미래도구는음수frame필터와native경계확인이필요하다.

## 종료와 다음 경계

자체Profiler기록전기존0..11버퍼를prior_profile.data로보존했고분석뒤LoadProfile=true·0..11복원했다. 이보존본은약727.6MB,자체진단native는약41.6MB다. 제품메모리비용과섞지않는다. profiler_buffer_restored.json 참조.

Outline/BodyMaskCache/ObjectEmphasis를시작원본으로hash동일복구,helper/meta는Assets밖격리,복구컴파일성공. 보호10파일모두동일. final_state.json에서Play/changing/compiling/updatingfalse·씬clean7roots·Profiler/Deep/profileEditor/callstacks/FrameDebuggerOFF·1920×1080/nativeBatchOFF확인. A보류/B제한·아틀라스보류·원본/fallback·기존강조/그림자/TMP와사용자변경보존.

다음B1/B2수정은 [NEXT_PROPOSAL.md](NEXT_PROPOSAL.md)에구체화했다. 기존Lease.scratch재사용으로별도snapshot을제거하고,MPBpayload는변경시에만만들되renderer에는매번재적용해외부변이복구를보존하는방향이다. 실제효과는미측정이며새구조구현/검증은별도승인전미실행이다.
