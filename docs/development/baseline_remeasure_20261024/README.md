# 현재 기준 재측정 — 2026-10-24

사용자 승인 범위: "1번 재측정만". 제품 코드·씬·설정을 수정하지 않았고, 일반 측정 1회(warm15+sample90)만 실행했다. Play 진입·종료와 Game View 크기 선택은 자동 복원했다. Profiler 녹화·Deep·Frame Debugger는 OFF.

## 조건
- Unity 6000.6.2f1 Editor, Ryzen 5 5600X / RTX 3070 / 32GB, 1920×1080, ortho 8.5, VSync 1, 고정 dt 1/60
- 적 128(Attack112/Buff16), 실제 타워 15, 전투씬 `CoreGuardian_CombatPrototype_20261008`
- 도구: [Benchmark.cs](Benchmark.cs) (직전 `mask_submission_path_20261024/Benchmark.cs` 기반. Diagnostic 호출 제거, 경로 변경, FrameTiming·렌더 카운터 추가), 실행 래퍼 [Run.cs](Run.cs)
- 표본 90, 프레임 누락 0, 적/타워 수 변동 0, GC 수거 0회, error null
- 복원: Play false, scene clean 7 roots, recording/Deep OFF, driver true, runInBackground false, captureDeltaTime 0, timeScale 1, native batch OFF, Game View index 3→3

## 전체 프레임
|지표|값|
|---|---:|
|평균 FPS (90×1000/ms합)|43.85|
|최저 / 최고 FPS|26.52 / 61.10|
|frame 평균 / 중앙 / 최대 ms|22.80 / 22.05 / 37.71|
|CPU Main Thread Frame Time 평균 (FrameTiming)|12.94 ms|
|CPU Render Thread Frame Time 평균|3.82 ms|
|GPU Frame Time 평균|0.69 ms|
|PlayerLoop 평균|13.15 ms|
|Draw / SetPass 평균 (UnityStats)|1583.7 / 187.6|
|GC Allocated In Frame 평균 / 중앙 / 최대|58,019 / 22,527 / 628,846 B|

**해석:** 1프레임 22.8ms 가운데 게임 자체(PlayerLoop)는 13.15ms이다. 나머지 약 9.6ms는 Editor 쪽 처리로 보인다. EditorLoop 마커는 현재 스레드 합산 방식에서 0으로 나와 직접 확인하지 못했다. GPU는 0.69ms로 이 PC에서는 병목이 아니다. Editor FPS 43.85는 실행 파일 성능이 아니다.

## PlayerLoop 안의 주요 비용 (평균 ms, 중첩 합산 금지)
|구간|평균|중앙|최대|
|---|---:|---:|---:|
|PreLateUpdate.ScriptRunBehaviourLateUpdate|6.389|6.105|11.475|
|└ CG.Emphasis.Present (강조 전체)|5.310|5.047|10.238|
|　└ CG.Mask.Present (마스크)|4.432|4.190|8.431|
|　　└ CG.Mask.Output|1.175|1.117|2.267|
|　　└ CG.Mask.CacheLookup|0.908|0.799|2.527|
|└ CG.Emphasis.Tint|0.886|0.843|1.840|
|└ CG.PhotoShadow.Present|0.757|0.728|1.198|
|CG.SpriteReferences.Read|0.829|0.812|1.171|
|Update.ScriptRunBehaviourUpdate|2.863|2.565|8.389|
|WaitForJobGroupID|1.756|1.724|2.895|
|UGUI.Rendering.UpdateBatches|0.509|0.375|4.978|
|CG.Mask.PageProducer|0.432|0.490|1.069|

- 강조(윤곽) 시스템 5.31ms는 게임 메인 스레드(13.15ms)의 약 40%다.
- `CG.Mask.CacheFlush`/`CopyBack` 마커는 현재 제품 원본에 없다. 직전 실험의 계측용 마커였고 원본 복구 때 함께 빠졌다. 그래서 이번에는 Flush 전체 시간을 측정하지 못했다.
- 렌더 제출 마커(DoRenderLoop/URP)와 Draw/SetPass ProfilerRecorder 카운터는 이번 방식에서 무효 또는 0이었다. Draw/SetPass는 UnityStats 값을 사용했다.

## 한계
- 단회 90프레임이다. Editor 측정이며 실행 파일(Player)이나 목표기기(GTX750Ti급) 결과가 아니다.
- 주입 부하(적 HP 100000, 고정 위치 스폰)이며 정상 30R 플레이가 아니다.
- EditorLoop 비용은 직접 측정하지 못했다. "프레임 − PlayerLoop" 차이로 추정했을 뿐이다.

원자료: [baseline_1.json](baseline_1.json), [환경](baseline_1_benchmark_environment.json), [완료](baseline_1_benchmark_completion.json), [복원](baseline_1_benchmark_restored.json)
