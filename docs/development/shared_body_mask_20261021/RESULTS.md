# 동일 본체 마스크 공유 결과

2026-10-21. 사용자 승인: 본체 마스크 중복 생성 제거, 설계·계획 승인 및 직접 순차 구현. 기준 제품47ca358, 기준/RED 체크포인트47e14b1. 최초 공유 구현d5771b8, 픽셀 보완9a0711e, 최종 성능 측정1c57020. 이후 수정은 EditMode 검사 자원 정리이며 실행 중 렌더 경로는 같다. 최종 소스는 [해시](implementation_hashes.json), 완료 커밋은 로컬 Git을 따른다.

## 적용과 제한

같은 본체·카메라·프레임·소스 상태·아틀라스 revision·**동일 생성 영역**의 완료 마스크를 참조한다. 소비자의 표시 quad/재질/PropertyBlock/정렬/수명은 독립이다. 참조 중인 이전 세대는 덮어쓰지 않고, 마지막 binding 반환 때 해당 본체의 RT를 모두 해제한다. 본체별 미참조 예비 record는 최대1개다. 프레임 간 합성을 생략하는 정적 캐시는 아니다.

포함 영역만으로 공유한 후보는 부동소수점 투영 차이로 원본 픽셀이 달라져 폐기했다. 최종 접촉 그림자는 **검증한1920×1080, 줌8.5/4.25, viewport 원점0·정수 경계, 카메라 회전identity·기본 직교 투영, stereo/dynamic resolution 없음, 카메라/본체XY±32, 접촉 폭2**에서 강조와 같은 pad3 마스크를 생성한다. 기존 pad4 출력 quad와 로컬 접촉 밴드는 `_MaskOriginPx=-1` 보정으로 유지한다. 강조/스캔은 원래 자신의 pad3 생성 영역을 그대로 사용한다. 다른 조건의 접촉 그림자와 비양수 compact 영역은 원래 합성으로 돌아간다. 서로 다른 일반 outline 생성 영역은 공유하지 않는다. 실행 순서를 바꾸지 않았다.

이 범위 밖의 전체 최적화 성공을 주장하지 않는다. 원본/방향/보행/HUD/색·효과 수·수명/음량·사운드/30R 성장·물량·밸런스/입력 소스와 씬은 변경하지 않았다.

## 같은 조건의 전후 계측

Ryzen5 5600X/RTX3070/RAM32681MB/Windows11, Unity6000.6.2f1/URP17.6, Editor. 전투씬1개,1920×1080, 카메라8.5, VSync1/target-1, 배속1/captureDelta0. 같은 Probe로128적(Attack112/Buff16), 실제 포탑15개, 적HP100000/Gold0/CoreDamage0을 주입했다. 정상30R 자연 전투 측정은 아니다. 음량 설정0.195603669·AudioListener1 유지. 워밍업30/관측100프레임×3, CPU/Rendering/Memory/UI/Audio, Deep/할당 스택/Frame Debugger OFF. [전 환경](before/normal_environment.json), [후 환경](after/normal_environment.json), [도구](Probe.cs).

양쪽 FPS 표본300개, 무효 시간 제외0. 평균 FPS=총 표본수×1000/전체 deltaMs 합계. Native Profiler는 각98/100/100프레임, 실제 CG 마커가 있는 게임 프레임296개로 CPU 마커를 비교했다. 외부 callback 표본300개와 엔진 캡처 표본을 동일 개수로 표기하지 않는다. Editor 제어 프레임을 제외한 CPU 마커 통계와 전체300개 FPS는 별개다. [반복별 비교](comparison.json), [합산/계산](summary.json), [분석](summarize.ps1).

|지표/단위/통계|변경 전|변경 후|절감량|절감률|반복/변동폭|원본/한계|
|---|---:|---:|---:|---:|---|---|
|마스크 합성·Execute/게임 frame|274|145|129|47.080%|동일3회|완료 후 공유로 생략|
|Draw Calls/중앙값|2178|2050|128|5.877%|각100×3|평균2190.397→2062.800(-5.825%)|
|SetPass/중앙값|578|450|128|22.145%|각100×3|평균582.463→453.210(-22.191%)|
|CG.Mask.Present ms/평균|15.11|12.65|약2.46|약16.3%|엔진 게임296프레임씩|세부는summary.json|
|CG.Mask.Execute ms/평균|5.41|3.23|약2.18|약40.3%|같은 표본|CacheLookup 비용 약1.67ms 신규|
|전체 delta ms/평균|32.741|30.882|1.858|5.676%|300개씩|Editor/주입 시나리오|
|delta ms/중앙값|31.358|29.094|2.264|7.219%|300개씩|분포 전체 보존|
|delta ms/p95|39.282|39.352|-0.070|-0.177%|300개씩|소폭 악화|
|delta ms/p99|48.535|47.759|0.776|1.599%|300개씩|p95 개선과 혼동하지 않음|
|delta ms/최대|95.909|53.907|42.002|43.793%|300개씩|스파이크 임의 제외 없음|
|FPS 최소/최대/평균|10.427/34.442/30.543|18.551/36.755/32.381|평균+1.838|평균+6.018%|반복별 comparison.json|증가량/율이며 비용 절감률 아님|

공유로 강조 쪽 합성은 줄었지만 PhotoShadow.Contact CPU는 약7.72→9.55ms로 증가했다. 비용이 먼저 실행되는 생산자 쪽으로 이동하고 소스 비교 비용이 추가된 결과이며, 접촉 경로 단독 개선으로 쓰지 않는다. 전체 mask Present 비용의 감소와 구분한다. 전체 프레임 p95는 악화했다. 평균 FPS 변화는 제한된 Editor 관측이며 버벅임 해결/목표FPS 달성/전체 성능 통과가 아니다. 최초 자료의162.609ms 지연 해결도 아니다.

## 메모리·GC·수명

후 관측의 binding274가 가리키는 **distinct 현재 RT는145개**다. 그러나 예비/이전 세대를 포함한 **실제 캐시 RT는274개**다. 참조 수를 실제 할당 수나 메모리로 합산하지 않았다. 100×3 종료 시 엔진 RT 메모리 평균은12,832,174.67→12,592,922.67bytes, 절감239,252bytes/1.865%다. 현재 bound RT만 보면 약6.485MB이지만 이를 전체 메모리 절반 절감으로 쓰면 안 된다. GPU VRAM/Memory Profiler 스냅샷은 미측정이다.

초기30frame 워밍업 뒤에도 용량 확장으로 CacheLookup 직접 GC가 평균155.14B/game frame 관측됐다. 매 프레임0 할당 성공으로 포장하지 않는다. 별도600×3의500frame 이후 캡처에서는86개 유효 게임 프레임/23,564회 lookup 전체 하위 GC0bytes, 접촉 직접 GC0bytes였다. [확인 원본](lifecycle/warmed_cache_gc.json).

600frame×3, 줌8.5→4.25→8.5, 초기화 후90frame. 오류없음/각600개 관측. 초기화 후3회 모두 entries2/bindings3/allocated3/304,248bytes로 돌아왔다(플레이어·코어가 계속 표시되는 초기 상태). 관측 행 기준 할당 RT 최대274, 총 RT bytes 피크37,017,740/45,217,596/35,391,936. 순간 전체 GPU 피크가 아니다. [전체 수명](lifecycle/memory_lifecycle.json), [관측 피크](lifecycle/observed_peaks.json).

이 수명 도구는530frame에 엔진 마커 추출·파일 저장을 동기 수행한다. 그 부하를 포함한 각 min/max/avg FPS는1.455/41.873/33.087,1.532/41.329/32.579,1.554/41.392/33.933이다. 유효 스파이크를 버리지 않았고 위 성능100×3 표와 섞지 않는다. 1FPS대 값을 게임 자체의 재현된 지연 원인으로 확정하지 않는다. 장시간 누수/사람 반응성 검증은 아니다.

domain/scene reload 옵션0·1·2·3에서 프레임 진행과 Play 종료 후 캐시0을 확인하고 원래 enabled=true/options1 및 EditorSettings 해시를 복원했다. [결과](transition_results.json), [복원](transition_restored.json). 이 전환 관측은 compact crop 전 공유 수명 코드에서 수행했으며 해당 hook/refcount 정책은 동일하다. 최종 Play 종료도 캐시 entries/bindings/allocated0, 이름 기준 RT0/batch mesh0이다.

## 품질·검사·구조

- 신규 공유 계약31/31, 기존 공유 재질2/아틀라스2/선택3/그림자10/강조3, 합계51/51. `final_*Tests.json`이 실제 결과다. 마지막 Editor 정리 변경은 공유31/강조3을 다시 확인했다. 컴파일 오류0/경고0.
- 최종 실제1920×1080/8.5·4.25, 좌표16의8역할×3배치×2줌×16건=768건, 좌표0 중첩8역할×2줌×16건=256건, **실제 공유 포함1,024건 차이0**. [native 결과들](pixels/). 추가 좌표16의 기존3해상도/3줌/3배치/4방향/4출력,8역할 fallback3,456건 차이0. 합계4,480건이며 빈 출력은 각 원본의 empty로 별도 기록했다.
- 보스도 효과만 비교했다. 예전 보스 전체 본체 합성의 미확정9건을 해결했다고 하지 않는다. 8방향 실제 키보드/사람 플레이, 정상30R 전체/수십분, 실제 청취·입력 체감은 이번 검증에 포함하지 않았다.
- 별도 Frame Debugger에서 union 이벤트 clear145/draw145, enabled 출력274를 확인했다. [이벤트](frame/frame_debugger.json), [인벤토리](frame/resources.json), [복원](frame/frame_debugger_completion.json). GetFrameEventData=false이므로 개별 shader/renderTarget 필드는 무효다. 이벤트 type/info와 인벤토리만 근거로 쓴다. 시간 비교에 이 동결 진단을 섞지 않았다.
- 독립 읽기 전용 리뷰에서 fractional viewport 문제를 재현/수정했고, 정확한 영역 공유·제한 뷰·수명 코드를 다시 검토받았다. [리뷰](review.md).

## 실패·보완 기록

1. 최초 RED11건 중3개 공유 실패. 첫 GREEN 시도는 SceneHandle API 컴파일 오류로 이전 assembly를 검사했다. 실제 로드 타입/필드를 확인하고 Scene identity로 수정한 뒤11/11. `initial_green_tests.json`을 통과로 쓰지 않는다.
2. contained-crop 후보: 원거리174/432, 원점 Buff2/432, 좌표16 Boss4/432(max6pix). 원점/뷰포트 gate만으로 해결되지 않았다. 실제 offset을 반영한 마스크에도3픽셀 차이가 있어 exact crop으로 전환했다. [후보 픽셀](pixels_candidate_containment/), [초기 원거리](pixels_candidate_far/), [초기 겹침](pixels_candidate_overlap/).
3. 제한 없는 compact-contact 후보도 좌표16 Attack4/Buff2건이 달랐다. [보존](pixels_candidate_compact/). 최종은 검증한 gameplay 뷰만 사용한다. 화면 바로 밖의 compact 영역0 크기는 원래 유효 crop으로 fallback하는 계약을 추가했다.
4. shader.name 비교 후보에서 접촉 직접 GC10,836B/frame가 관측됐다. cached Shader 참조로 변경한 최종 엔진 캡처에서는 직접 GC0. [이전 원본](after_name_candidate/), 현재 after 비교. 보조 GC.GetAllocatedBytesForCurrentThread는100KB 양성 대조군도0을 반환해 **무효 API**로 폐기했다(`shader_identity_allocation_positive.json`). 이 API의0을 성과로 쓰지 않았다.
5. 최초 수명 도구는 Profiler Driver를 켜지 않아4byte 파일과first/last=-1을 만들었다. 오프라인 읽기도 실패했다. [실패 보존](lifecycle_candidate_invalid/). Driver 활성·유효 엔진 프레임 확인·추출을 넣고 다시600×3 수행했다. 최종28/30/30개 native 프레임 중 CG 활성86개를 사용했다.
6. 다중 클래스 regex 필터는0건이어서 클래스별로 바꿨다. outer CLI30초 timeout은 runner120초와 달라 실행 상태 확인 후 outer180초로31건을 재검사했다. 전환 RPC는 도메인/씬 재로드 완료 대기 전5초 timeout이 났고 로그 근거로 대기를 보완했다. 무효/실패 결과도 보존했다.
7. 기존 강조 검사는 이미 PropertyBlock으로 옮긴 값을 sharedMaterial에서 읽어 실패했다. 실제 renderer PropertyBlock 조회로 수정했다. EditMode 미기동 컴포넌트의 OnDestroy 누락도 드러나, 소유한 테스트 cleanup과 Editor 전용 ReleaseResources를 보완했다. 기존에 남아 추적 레지스트리에 없는 native SelectionMask/Outline 재질6개는 일괄 삭제하지 않았다. 전역 native 누수 통과가 아니다.

## 종료·원본·다음 범위

[종료 상태](restored_engine.json): 전투씬1/clean, Play/pause/compile/update/Profiler/Driver/Deep/stacks/FrameDebugger=false, background=false, 카메라8.5/배속1/음량 보존. 캐시 owned RT/binding0. [설정 전](settings_before.json)/[후](settings_after.json)3파일 해시 동일, 기존 사용자 TimeManager/PackageManager 변경은 그대로다. [native 재질 잔류](native_material_residual_fixed.json)의 공유 레지스트리는0이고 소유권 미확정6개는 보존했다. 마지막 보완 검사 뒤 상태는 [최종 확인](final_state.json)을 따른다.

최종 및 shader-name 후보 native15개를 [압축](profiler_raw.zip)/[길이·SHA256 검증](archive_manifest.json)으로 보존했다. 최초 contained-crop 후보9개는 [별도 압축](profiler_containment_candidate.zip)/[검증](archive_containment_candidate_manifest.json). `.data` 원본은 삭제하지 않았고 로컬 보존·Git 제외다. 전체 분석 방법은 analyze.ps1/summarize.ps1, 원본 JSON은 before/after/lifecycle에 둔다.

현재 판정은 **제한된 뷰에서 반복 마스크 합성·호출 감소 및 평균 FPS 관측 개선**이다. 다음은 현재 사용자 플레이에서 해당 뷰의 방향/선택/강조/접촉·체감 확인, 검증 뷰 범위 확장 또는 예전 순간 지연 재현 시 마커 추적이다. HUD/Session 탐색·다른 풀링은 별도 후속 범위이며 자동 확대하지 않았다. 빌드·실행파일·push는 하지 않았다.
