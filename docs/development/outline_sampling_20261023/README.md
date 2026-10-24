# 윤곽 샘플링 축소 실험 — 후보 보류 / 원본 복구

2026-10-23, HEAD `1de8ced`·브랜치 `codex/shared-body-mask`, 미커밋. 사용자 “진행해” 승인으로 [계획](../script_shader_audit_20261023/OUTLINE_PLAN.md)의 중앙 재사용→조기 종료 후보를 단계별 구현/검증했다. **현재 제품 SelectionOutline.shader는 시작 원본과 바이트가 같으며 diff0이다.** 후보의 픽셀·논리 호출수 계약은 통과했지만 일반 단회 프레임 비교가 악화돼 채택하지 않았다.

## 후보 구현과 격리

- 중앙은 float로 한 번 읽고 기존 clip 비교를 보존, 기존 half edge 변환으로 누적한다. (0,0) 중복 읽기를 생략한다. 중앙.5를 단순 제외하지 않는다.
- 다음 단계는 edge≥.5일 때 기존 `_Color`를 반환하고 나머지 조회를 생략한다. 현재 실제 mask의 Point/Clamp·mip 없음 계약을 확인했으며 이웃 조회에 명시LOD0를 사용해 divergent return 이후 implicit derivative 의존을 피했다.
- 두께0~8·색·Blend/Pass·출력 정렬·마스크 생산/RT·C# 게임 동작은 바꾸지 않았다. [원본](SelectionOutline_original.shader), [중앙 재사용](SelectionOutline_central.shader), [조기 종료](SelectionOutline_early.shader).
- 최종 후보는 [테스트 fixture](../../../Assets/Scripts/Tests/EditMode/Fixtures/SelectionOutlineCandidate.shader)의 고유 Hidden 이름으로 격리했다. 제품 Resources shader와 이름/key가 다르며 런타임 연결을 추가하지 않았다. [원본 fixture](../../../Assets/Scripts/Tests/EditMode/Fixtures/SelectionOutlineReference.shader)와 [후보 대상 검사](../../../Assets/Scripts/Tests/EditMode/CoreGuardianSelectionOutlineShaderTests.cs)는 EditMode 검증용이다. 최종5개 검사는 **복구된 제품이 아니라 격리 후보를 검사**한다.
- 별도의 중앙-only 실전 성능 비교는 하지 않아 그것도 자동 채택하지 않았다. 추가 Play/다른 후보 측정·폭1전용 고정 코드/신규 keyword로 확대하지 않았다.

## 실제 검증과 TDD

|단계|결과|해석/원본|
|---|---|---|
|원본|출력2통과/호출수2실패|[RED](red_original.json): 최대9 기대/실제10, 첫 이웃 종료2 기대/실제10|
|중앙 재사용|3통과/1실패|[중간](central_tests.json): 픽셀동일·호출수9, 조기 종료 계약만 아직 실패|
|조기 종료|4/4통과|[GREEN](early_green.json), 폭1 512이웃 조합·폭0~8/float threshold/비정방Clamp 경계 출력 차이0|
|실제 원본 개체|64쌍 차이0|[실제 개체 비교](early_actor_quality.json): Attack/Buff,4방향×4위상×단독/겹침, 강조1px와 검사2px 중첩. 같은 개체의 출력재질만 전환,256×256/ortho2|
|기존 union/윤곽 렌더|2/2통과|[기존 회귀](legacy_union_green.json): 원본RT 증가·재사용/실제 윤곽·overlay 등 해당 case 계약|
|독립 검토 후 보강|5/5통과|[최종 후보](held_final_green.json): 큰 이웃1이 경계값 결과를 가리지 않게, 0/.499/.4999/.5/.5001/1 각각만 고립시킨 마스크를 폭0~8로 추가 비교|

첫 checker threshold fixture에는 모든 neighborhood에1이 있어 작은 neighbor 경계를 가리는 검토 지적이 있었다. 고립 fixture를 보강하고 통과했다. [검토/보완 기록](REVIEW.md). 검사시간 약0.56초/기존2case0.77초, 실제64쌍 외부 probe 실행0.96초였다. UI 전체/모든 원본9역할/목표 기기/native zoom 전수 승인과 구분한다. [처음1쌍 원본](early_actor_before.png) / [후보](early_actor_after.png).

## 논리적 조회수와 GPU 성능의 차이

테스트만 원본/후보 shader에 호출 카운터를 넣어 메모리 컴파일하고 GPU로 렌더했다. 제품에는 카운터/테스트 keyword를 넣지 않았다. 출력되는8031픽셀의 **논리적 샘플 함수 호출**은 다음과 같다.

|단계|픽셀당 최소~최대|합계|평균|원본 대비 논리 호출 감소|
|---|---:|---:|---:|---:|
|원본|10~10|80310|10.000|기준|
|중앙 재사용|9~9|72279|9.000|10.00%|
|조기 종료|2~9|39221|4.884|51.16%|

[원본](original_logical_reads.csv) / [중앙](central_logical_reads.csv) / [조기 종료](early_logical_reads.csv). 이 계측은 shader control flow의 호출수 확인이며, 계측에 의해 컴파일 최적화가 달라질 수 있다. **하드웨어 texture 요청수·GPU 시간·전체 게임 평균 조회수·FPS 개선율로 바꿔 쓰지 않는다.** 일반 성능 비교에는 계측 shader를 사용하지 않았다.

## 일반 모드 전후 — 악화 관측

동일 적128(Attack112/Buff16)·타워15,1920×1080/ortho8.5, 고정simulation dt1/60, 원본 표시/native batch OFF, warm15/sample90 **각1회**. Unity6000.6.2f1 Editor/Ryzen5 5600X/RTX3070/RAM32681MB/VSync1/targetFrameRate-1. Deep/recording/할당스택 OFF. 환경 [전](before_benchmark_environment.json) / [후](after_benchmark_environment.json), 원시 [전](before.json) / [후](after.json), [전체 수치](summary.json) / [분석](analyze.py), [측정 probe](Benchmark.cs). 양쪽90유효/gap0. 짧은 단회라 p95/p99·확정 인과 판정은 생략한다.

|지표/통계|원본|후보|절감량/변화|판정|
|---|---:|---:|---:|---|
|frame ms 평균|23.053|24.653|-1.600 /-6.94% 절감|악화 관측|
|frame ms 중앙|21.554|22.546|-0.991|악화 방향|
|frame ms 최대|55.223|58.522|-3.299|악화 방향|
|FPS 최소/최대/평균|18.11 /59.79 /43.38|17.09 /57.66 /40.56|평균 -2.82 /-6.49%|악화 관측|
|Main Thread 평균 ms|23.049|24.647|-1.598|대기 포함|
|마스크 CPU 평균 ms|4.450|4.690|-0.240|GPU shader 시간이 아님|
|강조 CPU 평균 ms|5.384|5.704|-0.321|마스크와 중첩 합산 금지|
|GC 평균 B/frame|55448|119692|-64244|후보 증가; 출처 미확정|
|GC 중앙 B/frame|20825|21063|-238|증가 방향|
|GC 최대 B/frame|546564|3488864|-2942300|별도 급증 포함|
|CollectionCount(0) 증가|0|1|-1|후보에 수거 발생|
|GC.Collect 평균 ms|0|0.193|N/A|수거 구간·shader 연산을 구분|
|incremental 평균 ms|0.000499|0.379638|N/A|GC marker끼리 중복 합산 금지|
|Draw 평균|1584.80|1587.33|-2.53|동일 draw 보장/감소 작업 아님|
|SetPass 평균|189.70|193.00|-3.30|증가 관측|
|GPU ms|미측정|미측정|N/A|현재 D3D12 GPUProfiler supported=false|

현재 API에서 GPU 지원 속성은 `isGPUProfilerSupported`다. 처음 과거식 메서드명 조회는 eval compile 단계에서 실패하여 실행/변경 없이 끝났고, 실제 속성을 확인해 [초기 상태](initial_state.json)에 기록했다. GPU 모듈/API 변경·도구 설치·빌드로 우회하지 않았다.

후보에는3.49MB 단발 할당·GC 수거가 있어1.600ms 증가 전체를 shader의 분기 비용 탓으로 확정할 수 없다. 반대로 논리 호출이 줄었다는 이유로 실제 성능 개선이라고 채택할 수도 없다. **악화된 예비 결과를 숨기지 않고 이번 제품 변경을 복구했다.** 새로운 반복·GC 출처 추적은 이번 범위를 넘어 자동 진행하지 않는다. 목표 GTX750Ti/RAM4~8GB60FPS 검증 아님.

## 최종 상태·다음 행동

- 제품 shader SHA256 `3847017d8bd1ff086377cb1548299e98dec92686c0582c6e10f3466dfa096ffd`, 원본 사본과 동일/diff0. [보존검사](verification.json).
- [실제 최종 상태](final_state.json): stopped/컴파일false/scene clean·7루트/Deep·recording·profileEditor·GPU·callstacks OFF/backgroundfalse/capture0/scale1/native batchOFF/자체 mask entries·bindings·pages0. 자체 Play2회는 정상 종료·복원 확인했다. [전 완료](before_benchmark_completion.json) / [후 완료](after_benchmark_completion.json).
- 사용자 씬/설정·기존 C# 최적화·미추적 파일 보존. 보호5파일 전후 hash는 [전](protected_before.json) / [후](protected_after.json). 새 제품 최적화 적용은0이며 후보 코드·대상 테스트·원본 자료만 남았다. 자동 commit/build/executable/push 없음.
- 다음은 결과 보고와 후보 보류다. 중앙-only 비교·지원되는 GPU 시간 확보·새 CPU 후보 수정은 별도 구체 승인 없이 실행하지 않는다.
