# GC 할당 급증 출처 조사 — 일반 Profiler 추적 완료

2026-10-23. 사용자 요청: GC 스파이크 출처 확인. 이번 단계는 저장된 자료·관련 코드 읽기와 분포 분석만 수행했다. Unity Play/Profiler/Deep/스크립트 재로딩/게임 코드 수정은 하지 않았다. 이전 소스 해시5개 동일 확인.

## 확인한 사실

기존 [원본](../emphasis_cpu_20261023/baseline.json)/[첫 후보](../emphasis_cpu_20261023/candidate.json)/[최종 수정본](../emphasis_cpu_20261023/candidate_active.json)의 각90개 전체 표본을 분석했다. [분포 원본](existing_capture_analysis.json).

|관측|원본|첫 후보|최종 수정본|
|---|---:|---:|---:|
|할당 평균 B/frame|263368.42|352104.01|350920.10|
|할당 중앙값 B/frame|127104.5|127151.5|126910.0|
|1MB 초과 프레임 수/90|2|3|4|
|1MB 초과 프레임이 전체 할당에서 차지하는 비율|33.05%|54.54%|53.85%|

1MB 기준은 분포 설명용 분류이며 성능/FPS 원본 표본을 제거한 것이 아니다. 최종 큰 할당 프레임은49/52/54/55,5.67/5.52/4.15/1.67MB. 해당 프레임은 모두 포탄 수0이지만 이전 효과의 지연 처리까지 배제하는 근거는 아니다. 큰 할당의 시점은 실행마다 달랐다(원본17/18, 첫 후보80~82). **지속적인 매 프레임 할당 증가와 간헐적 큰 할당을 구분해야 한다.**

GC Allocated In Frame은 할당 바이트다. GC 수거 발생/정지 시간/수거 호출 출처를 측정한 값이 아니다. 큰 할당 시점과 frame spike가 항상 일치하지 않는다. 기존 JSON에는 할당 함수/부모 호출/수거 마커가 없으므로, 변경 코드·마스크·포탄·Editor/계측 중 어디가 원인인지 확정할 수 없다.

## 소스 확인 범위

강조의 적용 tint 목록과 참조 전달, 윤곽의 Geometry 캐시 miss에서 발생하는 sprite 배열/LINQ 할당, 기존 진단 Poll의 dictionary/object 생성·마지막 JSON 직렬화를 확인했다. 이들은 추적 후보이며 실제 MB 급 할당의 출처라고 확정하지 않았다. 계측 Poll의 일상 할당과 게임 내부 할당·Editor 할당을 구분할 필요가 있다. 추측에 따른 수정은 하지 않았다.

## 당시 제안한 검사 — 이후 사용자 승인으로 실행

현재 Editor가 사용자 Play/다른 검사 중이면 중단하거나 겹치지 않는다. 유휴/clean 확인 후 같은 적128·타워15에서 warm15/sample90 **1회**의 일반 Profiler 기록을 제안한다. Deep·할당 호출스택·Frame Debugger는 OFF. 할당량 및 GC 수거 마커를 기록하고, 종료·상태 복원 후 큰 할당 상위 최대2프레임의 Main Thread에서 GC.Alloc 부모 구간만 제한적으로 집계한다. 프레임당 샘플2만개 상한, 초과 시 추출을 중단하고 미확정으로 보고한다. 게임/씬/설정 변경·빌드·실행파일·push·커밋 없음. 30초 watchdog·정상 완료/오류 시 자체 Play 종료·원래 Profiler/배경/시간 복원 확인을 유지한다.

AGENTS의 Play/Profiler 조작 사전 승인 규칙 때문에 이 추가 실행은 사용자 확인 전 보류한다. 기존 원시 Profiler 전체 트리·Deep 추출 도구는 재사용하지 않는다.

## 승인 후 일반 Profiler 추적 결과

사용자가 추적 진행을 승인해 적128/타워15·warm15/sample90 **1회** 일반 Profiler 기록을 수행했다. Deep/할당 호출스택/Frame Debugger는 OFF, profileEditor=false 유지. 게임 코드·씬·설정 변경 없이 기존 동일 진단 부하를 사용했다. [Trace.cs](Trace.cs)는 Assets 밖에서 메모리 컴파일, 재로딩 없음. [90프레임 원본](trace.json), [환경](trace_benchmark_environment.json), [추적 상세](trace_parents.json), [요약](trace_summary.json).

- 90개/누락0. 할당 평균164514.57B/frame·중앙127507.5B·최대560836B. **앞선5~6MB 급증은 이번에 재현되지 않았다.** 이번 원인 분류를 이전5~6MB의 확정 원인으로 확대하지 않는다.
- 일반 GC.CollectionCount(0)는1879→1879, 증가0. GC.Collect 마커 최대0ms, GarbageCollector.CollectIncremental 마커 최대0.0018ms. 관측 구간에서 큰 GC 수거 정지 근거 없음. 작은 증분 마커 시간은 실제 수거 작업량/전체 컬렉션 비용으로 확정하지 않는다.
- 큰 할당 상위2프레임만 Main Thread를 집계했다. profiler4549/4556의9345/8561샘플을 각19/14ms에 처리했다. 2만 샘플·1초 상한 이내, 잘림/재시도/호출스택/전체 트리 출력 없음.

|Profiler 프레임|카운터 할당|추적된 Main Thread 할당|HUD.Update 경로|PhotoShadow.Cast 경로|
|---|---:|---:|---:|---:|
|4549|560836B|540168B|426612B|106966B|
|4556|381629B|363091B|249978B|106966B|

HUD의 큰 할당 경로는 `CoreGuardianHud.Update → GameObject.AddComponent → UnityEngine.UI.Text.OnEnable`에 집중됐다. 해당 Text.OnEnable 경로만376068/213152B, 생성자/Image/직접 HUD 할당까지 합한 값이 위 HUD 열이다. 여기의 calls는 **GC.Alloc 샘플 수**이며 생성한 Text 컴포넌트 수가 아니다. 소스의 RefreshDamageTotals/CreateDamageRow는 누적 피해 목록 확장 시 Label/Total Text를 생성하고 기존 행은 재사용한다. 초기 행 확장과 상시 재생성을 구분해야 하며 기존 pooling을 미구현으로 취급하지 않는다. 일반 Profiler가 모든 C# 보조 함수명을 기록한 것은 아니므로 helper 단위 세분화는 추가 확인 전이다.

PhotoShadow.Cast에서는 두 프레임 모두106966B/GC.Alloc2681샘플. 해당 범위의 소스는 렌더러마다 `r.name`을 반복 가져와 숨길 옛 그림자 이름과 비교한다. 참조 캐시 Read의 별도 마커 내부 할당과 구분됐으며, 이름 getter 반복이 줄일 수 있는 지속 할당 경로다. 넓은 그림자 이미지/위치/크기/회전/정렬을 바꾸는 제안이 아니다.

카운터와 추적 Main Thread 합의 차이20668/18538B는 미귀속이다. profileEditor=false·메인 스레드 한정·시점 차이/다른 스레드/계측 영향은 분리하지 못했다. 계측용 GetAllocatedBytesForCurrentThread가 할당이 있음에도0을 반환했으므로 diagnosticPollBytes=0은 **유효한 무할당 근거가 아니다.**

기록 중 FPS 최소/최대/평균은 20.62/55.81/40.94, wall frame 평균/중앙/최대 24.424/22.841/48.502ms다. 일반 Profiler 기록 부하가 있어 앞선 recording OFF 성능 수치와 직접 비교하지 않는다. 변경 전후 절감량/율은 제품 수정 전이므로N/A, 목표기기 검증 아님.

[자동 종료](trace_benchmark_completion.json)/[복원](trace_benchmark_restored.json) 및 실제 재조회: Play stopped/compiling false/Deep·recording·callstacks·profileEditor OFF/driver true/background false/captureDeltaTime0/timeScale1/scene clean/native batch OFF. [보호5파일 해시 동일](protected_after_trace.json). 게임 코드 수정·빌드·실행파일·push·커밋 없음.

다음 제안은 먼저 PhotoShadow의 반복 이름 조회를 구조 변경/명시 무효화 시 분류하는 캐시로 바꾸고, 원본 그림자 표시·숨김·재사용 회귀 검사와 같은 짧은 전후 비교로 확인하는 것이다. HUD 행 재사용은 유지하며 미리 준비하는 시점/비활성 생성 비용은 별도 검토한다. 이전5~6MB 출처 미확정은 유지한다. 코드 수정·추가 Play는 구체 범위 승인 전 진행하지 않는다.
