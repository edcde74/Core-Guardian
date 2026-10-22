# B 마스크 생성 배칭 결과

2026-10-22. 사용자의 B 재개·계속 지시에 따라 반영했다. **B의 마스크 생성 경로만 채택했고, 출력 재질 교체/출력 메시 병합은 런타임에서 사용하지 않는다. A는 보류다.** 당초 Draw 70%/SetPass 50% 실험 목표에는 미달했다. 전체 최적화 완료 판정이 아니다.

## 구현과 적용 제한

- 카메라·프레임별 GPU 전용 2048² 페이지에 개체별 마스크 영역을 배치하고 같은 입력 텍스처의 부품 geometry를 묶는다. 원래 part→VP GPU 곱, alpha/threshold, 개체별 영역, D3D subpixel 격자를 보존한다. 원본 본체/애니메이션/효과/성장/물량을 줄이지 않았다. SRP Batcher 설정도 원래 ON이다.
- 런타임은 D3D11/12 지원 기기, 1920×1080, 기본 직교 투영/회전, 줌8.5/4.25, 지원 좌표 범위에서만 사용한다. 지원 밖은 원본 합성이다. 방어형·보스는 네 종류 화면 검사에서 회귀가 확인되어 **원본 마스크 생성**을 유지한다. 종류의 표시·개수는 유지한다.
- 출력은 기존 renderer/material/sortingOrder를 그대로 사용한다. B2 출력 배칭 후보는 별도 성능 이득을 입증하지 못했고 외부 투명 렌더러와 같은 정렬 키에서 재질 교체의 순서까지 보장하지 못해 런타임 등록을 차단했다. EditMode의 실험·회귀검사 코드는 남아 있다.
- 생성 완료 뒤 기존 개별 RT로 CopyTexture한다. 기존 Sample.Texture/공유 lease/다중 카메라/원본 fallback 계약을 유지하기 위한 선택이다. 따라서 개별 RT 제거·출력 draw 감소·메모리 절감을 주장하지 않는다.
- 페이지 overflow는 추가 페이지, 카메라·뷰·프레임 변화는 generation 무효화, scene/play 종료는 자체 자원을 해제한다. 같은 프레임 두 번째 Flush가 먼저 완성한 영역을 지우지 않는다. pending 결과는 완료 전 공개하지 않고 producer 소유자가 사라져도 보관한 geometry로 완성한다.

복구 기준은 B 재개 전 `98f997a`(제품 원본은 `5943523`/`1278449`), 중간 B1 `653296e`, B2 `ddca0c4`다. 마지막 구현은 아래 검사·측정이 적용된 로컬 완료 커밋에서 확인한다. 원본 비교 소스/해시는 `Logs/CG_B_SourceComparison_20261022/manifest.json` 및 `baseline/`에 보존했다. 그 manifest의 current 항목은 최종 변경 전 중간 B이므로 최종 파일에 복원 스크립트를 그대로 실행하면 안 된다.

## 최종 같은 조건 전후 측정

정본은 [분석 JSON](b2_benchmark/analysis_shipping.json), [재분석](b2_benchmark/AnalyzeShipping.ps1), [동일 계측 코드](B2RuntimeProbe.cs)다. 변경 전은 실제 원본 소스로 측정한 `baseline_late`, 변경 후는 최종 코드 `b_shipping`이다. 이전에 다른 시간대에서 관측한 큰 변동/악화도 아래에 남겼다.

Unity6000.6.2f1/URP17.6/D3D12 Editor, Ryzen5600X/RTX3070, 1920×1080, VSync1/targetFrameRate−1. 각 조건 3회, 회당 warm60 + sample180, 고정 simulation dt1/60. 실제 128적(Attack112/Buff16), 포탑15, 30step마다 적8개 교체. 요청 설치 수32와 실제 설치15를 혼동하지 않는다. Debug 주입이며 정상 자연 플레이가 아니다. 게임 자동 Update를 멈추고 계측자가 Tick/View를 순차 수행한다. CPU/Rendering/Memory/UI/Audio Profiler ON, Deep/할당 호출 스택/Frame Debugger OFF, background ON. 원래 시간/Profiler 상태는 종료 시 복원했다.

|지표|변경 전|최종 B|절감량 / 절감률|
|---|---:|---:|---:|
|Draw Calls/frame, 540 simulation 표본 평균|2087.311|1948.954|138.357 / 6.628%|
|SetPass/frame, 같은 표본 평균|458.993|319.426|139.567 / 30.407%|
|native 전체 frame 평균 ms|36.217|30.169|6.048 / 16.700%|
|Main Thread GC.Alloc B/frame|118690.024|118316.380|373.644 / 0.315%|
|Tick ms|0.462|0.471|−0.009 / −1.890%|
|View ms|1.954|1.915|0.039 / 1.972%|

GC 차이는 작아 실질 개선으로 확정하지 않는다. Tick도 소폭 악화했다. frame 시간은 native 값으로 계산했으며 고정 simulation dt를 FPS로 사용하지 않았다.

|native 프레임 통계|변경 전|최종 B|
|---|---:|---:|
|유효 프레임 수|542|542|
|최소 / 중앙 / 최대 ms|29.939 / 34.839 / 231.296|23.544 / 29.169 / 54.309|
|p95 / p99 ms|43.961 / 54.603|39.432 / 44.981|
|최저 / 최고 / 평균 FPS|4.323 / 33.401 / 27.612|18.413 / 42.474 / 33.147|

평균 FPS 증가량은 **5.535, +20.047%**로 이 Editor 주입 조건에서는 개선됐다. baseline의 231ms spike도 제거하지 않았다. 최저 FPS 상승을 일반적인 프레임 안정성 보장으로 해석하지 않는다. 각 반복 분포는 분석 JSON의 runs에 있다. 조건당 540 실제 Tick/개수 기록과 542 native 유효 프레임(측정 경계의 no-Tick 2개 포함)을 모두 보존했으며 누락 보간·스파이크 삭제는 없다. 각 회 264개 이벤트 및 에너지/재화/상태/적/탄환/피해 원본은 **3회 모두 그대로 일치**했다.

원본: [baseline](b2_benchmark/baseline_late/environment.json), [최종 B](b2_benchmark/b_shipping/environment.json)의 같은 폴더에 completion·run_0..2의 frames/counts/outcome/threads JSON을 보존한다. native `.data` 6개는 `Logs/CG_RenderBatching_20261022/b2_benchmark/{baseline_late,b_shipping}/run_0..2.data`다. [스레드 추출 코드](ShippingThreads.cs), [요약](b2_benchmark/shipping_thread_summary.json). native Rendering counter도 일반 draw 약454.03→314.50, SRP draw1631.30→1632.47, SetPass459.08→319.51로 같은 방향이다(542 경계 포함 평균이라 위 540표본 평균과 다르다).

Main/Render Thread의 frameTime은 대기 포함 프레임 벽시계 값이다. Render Thread 평균36.217→30.169ms를 순수 Render CPU 비용으로 부르지 않는다. Gfx.WaitForGfxCommandsFromMainThread 관측 평균17.966→16.593ms도 함께 보존했다. GPU Profiler는 현 D3D12 Editor에서 미지원이고 raw GPU 값0은 **N/A**다. GPU 병목 해소는 입증하지 않았다.

Windows 환경에서 측정했으며 OS 상세 버전/RAM 용량은 이 실험 metadata에 기록되지 않았다(최종 CIM 조회는 접근 거부). 같은 PC의 전후 비교이며 다른 장치 성능으로 일반화하지 않는다. 중간 B3 [Frame Debugger](b_final_frame/frame_debugger.json)는299이벤트/Mesh24/SRPBatch24를 기록했다. 정지한 같은 프레임을 다시 그린 캡처의 clear0을 매 프레임 clear 제거로 해석하지 않는다. 이 이벤트 수는 native Draw Calls와 다른 단위이고 마지막 출력 원본 유지 변경 뒤의 새 FD 캡처도 아니므로 최종 절감률은 위 native counter로 판정한다.

## 화면·수명·기능 검증

- 최종 신규 EditMode24: [producer9](shipping_CoreGuardianMaskProducerTests.json), [page5](shipping_CoreGuardianMaskPageTests.json), [output 후보10](shipping_CoreGuardianMaskOutputBatchTests.json) 통과. 출력 후보 검사는 실제 런타임에서 출력 교체를 채택했다는 의미가 아니다.
- 기존 관련 EditMode47: [BodyMaskSharing31](shipping_CoreGuardianBodyMaskSharingTests.json), [SelectionOutline3](shipping_CoreGuardianSelectionOutlineTests.json), [PhotoShadow10](shipping_CoreGuardianPhotoShadowTests.json), [ObjectEmphasis3](shipping_CoreGuardianObjectEmphasisTests.json) 통과. 옛169개 전체를 반복한 결과가 아니다.
- 실제 씬 [30라운드·재시작 PlayMode1](shipping_30R_status.json) 통과. 가속 스크립트 기능검사이며 자연30R/장시간 성능 검증이 아니다. 이번 대상 합계72개 통과.
- [128적·15포탑 비교](b3_scene_pixels.json): 줌8.5/4.25에서 실제 페이지 마스크145/138개를 기존 RT에 복사해 원본 독립 합성과 비교, 차이0. [4종132적·15포탑](b_all_kinds_scene_pixels.json): 페이지 마스크147/140개, 두 줌 모두 차이0. before 반복 대조도0. 출력 후보 등록은0으로 원본 재질/정렬을 사용했다. [비교 코드](B3ScenePixels.cs), [4종 코드](BAllKindsPixels.cs). PNG의 before/after가 원본이다. Camera.Render 캡처이므로 Overlay HUD는 미포함이며 모든 방향/보행 위상/효과 중첩의 전수 검증은 아니다.
- 재개 직전 사용자 씬·설정6파일의 [최종 해시](shipping_user_hashes.json)는 **6/6 동일**. [종료 상태](shipping_final_state.json): scene clean, Play 종료, compiling false, 자체 마스크 RT/페이지 메시/재질0. 기존 사용자 M3/폰트 접근불가 D369 및 미추적 파일은 보존한다.
- Profiler 복원 첫 LoadProfile은 false였다. 재개 직전 저장본 `Logs/CG_B_20261022/prior.data`가 4바이트 빈 header였음을 확인해 자체 캡처를 비워 first/last−1인 [빈 버퍼 상태로 복원](shipping_profiler_restore.json)했다. 과거 다른 세션의 큰 버퍼를 이번 원상태로 오인해 덮어쓰지 않았다. driver true/Profiler OFF/background false/time1/capture0/paused false다.
- 최종 콘솔에는 위 자체 빈 버퍼 LoadProfile의 signature/deserializer 오류2건이 남아 있다. [콘솔 원본](shipping_console_errors.json)에서 제품 호출이 아닌 진단 복원 오류임을 확인했고 삭제하지 않았다. Play 진입 직후의 CLI main-thread timeout1건도 기록에 남아 있다. 이후 실제 측정3회/검사/화면 비교의 완료 결과를 확인했으며 오류0이라고 바꿔 쓰지 않는다.

## 실패·악화와 결정

- B2 출력 geometry 후보는 dense 장면에서 병합 이득이 없고 초기 frame +31.757%/GC +206.394%, single-output 후보도 frame +20.904%였다. 출력 배칭 성과로 계산하지 않았다. 관련 `b2_benchmark/analysis*.json`과 후보 코드를 보존한다.
- B3 초기 frame +5.977%/GC +111.728%. shader.name, 임시 source Mesh, 매 개체 SystemInfo 포맷 검사에서 발생한 할당을 제거했다. 실제 [할당 호출 스택](allocation_callstacks.json)이 근거다. `format_gc_red.json` 단위 검사는 오히려 통과해 런타임 할당을 재현하지 못했으므로 RED 증거로 부르지 않는다.
- 중간 `b_complete`는 평균61.838ms(+62.402%)로 악화했고 native 표본1개가 경계에서 빠졌다. [원본 분석](b2_benchmark/analysis_complete.json)을 숨기거나 최종 측정과 섞지 않았다. 시간대 변동을 확인한 뒤 실제 원본 소스를 복구해 새 기준 `baseline_late`를 수집했고, 복원된 B의 `b_late` 및 최종 `b_shipping`을 비교했다. 순차 Editor 실험의 환경 변동 한계는 남는다.
- 실제 producer 초기145/138 마스크는 7/2개에서 단일 픽셀 차이 → subpixel 격자 보존 뒤0. [첫 실패](b3_actual_masks_initial_failed.json), [수정 결과](b3_actual_masks.json). 네 종류 확대 때 줌8.5에서14픽셀 차이 → 방어형/보스 원본 fallback 뒤0. [실패 JSON](b_all_kinds_scene_initial_failed.json)을 보존하며 초기 실패 PNG는 후속 동일 파일명 캡처로 갱신되어 별도 남지 않았다.
- 독립 검토자가 같은 프레임 두 번째 Flush의 완료 영역 clear, queued geometry 소유 Mesh 파괴를 지적했다. 각각 신규 검사 RED→GREEN으로 수정했다. 저자가 추가로 미세 카메라 행렬 변화, 파괴된 카메라 복원, pending spare 반환/카메라 해제도 RED→GREEN 검증했다. review_red/green 및 camera_pending_red/green JSON이 원본이다. **독립 검토는 서비스 사용량 제한으로 중단됐으며 전체 완료/최종 무결점 재검토를 받았다고 주장하지 않는다.** 나머지는 저자 검토·대상 검사의 범위다.

최종 결정: (1) 출력 재질/정렬 보존, 따라서 출력 draw 자체는 줄이지 않음. (2) 개별 RT/copyback 보존, 따라서 추가 페이지 메모리/복사 비용 감수. (3) 방어형/보스 원본 생산 보존, 따라서 해당 두 rig의 배칭 절감은 없음. 모두 품질 보존을 우선한 적용 제한이며 추후 별도 픽셀/성능 근거 없이 확대하지 않는다.

## 메모리와 남은 검증

페이지당 pixel storage는16MiB이고 overflow/카메라별 peak 페이지를 유지하므로 사용량이 늘 수 있다. 이전 B3 활성 관측은 [1페이지 약16MiB + 개별RT274개 약12MiB](b_complete_memory_active.json), 다른 실험은 [2페이지 약32MiB](b_ready_memory_active.json)였다. 이는 최종 실행 peak 전수 측정도 전체 GPU driver 메모리도 아니다. 개별 RT는 원래 cache/spare 정책으로 남고 버퍼·메시도 추가된다. 메모리 절감 주장은 하지 않는다. Play 종료 자체 Unity 객체0은 확인했지만 managed static capacity/driver 전체 잔류를 증명하지 않는다.

정상30R 자연 플레이·장시간 적/효과 부하·전 방향/위상·실제 조작/청취, 유효 GPU 시간/순수 Render CPU 분석, Memory Profiler 기반 peak/잔류/재시작 반복은 미완료다. Memory Profiler package 미검출이며 설치/API 변경을 자동 수행하지 않았다. 기존 보스 전체 합성 미확정9건도 이 네 종류 정지 화면만으로 완료 처리하지 않는다. A 재검토와 기존 RT 제거/출력 직접 배칭도 별도 후속이다. **추가 빌드·실행파일 실행·push는 하지 않았다. 기존 06:20 빌드는 B 반영 전 제품이다.**
