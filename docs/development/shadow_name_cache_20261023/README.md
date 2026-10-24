# 넓은 그림자 이름 조회 캐시 — 반복 할당·CPU 부분 개선

2026-10-23. 사용자 승인: PhotoShadow 이름 분류 캐시, 원본 그림자/숨김/구조 변경·재사용 회귀, 동일90프레임 비교. 복구 HEAD `1de8ced`, 미커밋. **그림자 변경 소스 반영**, 원본 그림자·A/B 제한·SpriteAtlas 보류·네이티브 본체 배칭 OFF 유지. HUD/미확정 MB 급증은 수정 범위 밖이다.

## 변경

- PhotoShadow는 기존 숨김 이름과 replace 역할 조건을 유지하며 구조 참조 Revision이 바뀔 때만 이름을 읽어 억제 목록을 분류한다. 평상시에는 분류된 렌더러만 확인해 재활성화된 옛 그림자를 다시 끈다. 비활성 개체도 분류에 포함하며 소유 트리에서 제거된 렌더러는 다음 구조 갱신에 목록에서 빠진다.
- CoreGuardianSpriteReferences에 Revision 카운터만 추가했다. 기존 자식 수/parent/파괴 감지와 명시 무효화의 refresh에서 증가한다. 다른 마스크/강조 소비자의 필터·출력·정렬은 변경하지 않는다.
- OnEnable에서 무효화해 풀 재사용 시 분류를 갱신한다. **구조 변화 없이 기존 부품 이름을 바꾸는 호출자는 InvalidateSources를 호출해야 한다.** 이름을 매 프레임 읽어 임의 rename을 자동 감지하는 방식은 제거했다. 런타임에서 바뀌지 않는 부품 이름 분류를 캐시한 범위다. 기존 참조 캐시가 자동 감지하지 못하던 같은 Transform의 컴포넌트 추가 역시 명시 무효화 계약을 따른다.
- 넓은 그림자 sprite/material·위치·회전·크기·정렬과 몸체/보행/강조·HUD·밸런스·소리는 변경하지 않았다. 목록/참조 캐시 메모리를 사용하며 전체 peak/잔류 메모리 검증은 아니다.

## 검증

Unity 테스트 프레임워크의 native GC.Alloc constraint를 사용했다. 앞선 GetAllocatedBytesForCurrentThread의 무효0을 쓰지 않았다.

- [RED](red.json): 변경 전 안정 상태 Present가 Attack/Defense 모두 할당 검사에서 예상 실패2/2. 검사 도입 첫 시도의 Action/TestDelegate 타입 오류도 [보존](test_harness_error.json)했으며 올바른 TestDelegate로 바꾼 뒤 RED를 확인했다.
- [최종 그림자13/13](tests_final.json): 기존9종 원본/그림자 규칙, Attack/Defense 안정 Present 무할당, 구조 추가·비활성·소유권 제거·이름 변경+명시 무효화·재사용을 확인했다.
- 첫 구현에서는 EditMode Non-ExecuteAlways의 OnEnable 미전달로 재사용 검사가 실패했다. [당시 결과](first_green_lifecycle_failure.json)를 보존하고 테스트에 OnDisable/OnEnable을 명시 전달한 뒤 통과했다. 이는 엔진 Play 콜백의 전수 검증과 구분한다.
- [공유 참조 소비자3/3](tests_shared_references.json): 기존 독립 윤곽의 구조 갱신·마스크 출력 회귀 통과.
- [같은 원본 개체/같은 cast renderer의 픽셀18쌍](quality.json) 모두 차이0. Attack/Defense/Buff/Single/Multi/Support/Player/Core/Boss 각2위치·좌우 자세,256×256/ortho2. 변경 전 클래스 복사본과 현재 클래스를 같은 원본 렌더러에서 순차 렌더하고 옛 그림자를 재활성화한 입력에서도 비교했다. [재현](Quality.cs), 전후 PNG 보존. 전 방향/모든 상태·전체 HUD·정상30R·아트 승인은 아니다.
- [읽기 전용 소스 검토](REVIEW.md): 새 지적 없음. 코드/기능 QA와 실제 성능·전체 잔류 검증은 구분한다.

## 동일 부하 계측

Unity6000.6.2f1 Windows Editor, Ryzen5600X/RTX3070/RAM32681MB,1920×1080/ortho8.5,VSync1/targetFrameRate-1,timeScale1·고정 simulation dt1/60. Attack112+Buff16·타워15가 양쪽90프레임 내내 유지, 포탄도 min0/max36/평균6.733 동일. 각warm15/sample90 **1회씩**, 무효 시간/프레임 누락0. 실제 wall frame 간격으로 FPS 계산, 고정 dt 역수 아님. recording/Deep/FrameDebugger OFF·ProfilerRecorder capacity1+wrap+LastValue. 기존 큰 할당 trace의 recording ON 수치와 직접 비교하지 않는다.

|지표/단위/통계|원본|수정|절감량|절감률|반복/변동폭·한계|
|---|---:|---:|---:|---:|---|
|PhotoShadow.Present 평균ms|1.6589|0.8497|0.8092|48.78%|각90개, 단회|
|PhotoShadow.Cast 평균ms|1.6210|0.8121|0.8089|49.90%|Present 내부, 합산 금지|
|전체 GC B/frame 중앙값|127182|20402|106780|83.96%|평소 할당 감소|
|전체 GC B/frame 평균|165148.86|121456.10|43692.76|26.46%|수정본 MB 급증 포함|
|Main Thread 평균ms|23.7150|22.9209|0.7940|3.35%|대기·변동 포함|
|wall frame 평균ms|23.7204|22.9256|0.7948|3.35%|전체 개선 확정 아님|
|Native Draw 평균|1587.16|1579.33|7.82|0.49%|범위1555~1790/1554~1668, Draw 절감 주장 아님|
|SetPass 평균|192.30|187.48|4.82|2.51%|단회 변동 포함|

|프레임/FPS 통계|원본|수정|
|---|---:|---:|
|wall ms 중앙값|21.297|20.711|
|wall ms p95 nearest-rank|36.025|35.128|
|wall ms 최대|114.835|111.037|
|최저 FPS|8.71|9.01|
|최고 FPS|60.50|61.50|
|평균 FPS (총프레임/총시간)|42.16|43.62|

평균 FPS +1.46/+3.47%는 단회 관측이며 통계적 개선/목표기기60FPS 통과가 아니다. p95는90개 내부 기술통계, p99는 표본 부족N/A. 승인된 짧은 비교로 기본3회 반복 미수행. 유효 스파이크를 제외하지 않았다. [원본90개](baseline.json)/[수정90개](candidate.json), [분석](summary.json)/[코드](analyze.py), [환경](candidate_benchmark_environment.json), [실행](Benchmark.cs).

**수정본 엔진106프레임에서4,822,006B 단발 할당을 관측했다.** 직전105프레임787,320B, 그 프레임 wall25.785ms. 원본 최대713157B. recording OFF 비교라 해당 급증의 호출 출처는 없으며 원인 미확정이다. 따라서 반복 이름 할당 감소와 별개로 MB 급증 해결을 선언하지 않는다. 이전5~6MB와 같은 원인인지도 확정하지 않았다. HUD Text 초기 확장·Editor/계측 할당·실제 GC 수거 정지/피크·전체 잔류는 별도 미완료다.

## 종료 상태

[자동 종료](candidate_benchmark_completion.json)/[복원](candidate_benchmark_restored.json) 및 실제 재조회: error null,Play stopped/compiling false/Deep·recording·callstacks·profileEditor OFF/driver true/background false/captureDeltaTime0/timeScale1/scene clean/native batch OFF. [보호5파일 해시](protected_after.json) 모두 동일. [소스 해시](source_hashes.json)·수정 전 소스 보존. 사용자 변경/미추적 파일 보존. 씬 저장·빌드·실행파일·push·커밋 없음.

다음은 남은 MB 급증과 HUD 생성 비용의 구분이다. 이번 캐시 승인 범위를 넘어 추가 Play/프로파일링·제품 수정은 진행하지 않았다.
