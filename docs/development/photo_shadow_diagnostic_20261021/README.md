# PhotoShadow 지연 진단

2026-10-21. 사용자 승인: 최적화 제안 1번인 순간 지연 원인 조사. 기준 `77aebb9`. 마스크 공유/HUD/Session 최적화는 범위 밖이다.

## 질문과 절차
- 이전 캡처의 PhotoShadow 총162.609ms/자체159.540ms 원인은 미확정이다. 원본은 [이전 결과](../mask_atlas_20261021/RESULTS.md)를 따른다.
- 현재 제품으로 기존 128적/15포탑, 준비30/측정100프레임×3을 확보한다. 같은 환경에서 세분화 ProfilerMarker만 추가한 뒤 동일 횟수로 비교한다. 진단 마커 오버헤드와 변동을 개선으로 표시하지 않는다.
- 마커는 카메라 조회, 기존 그림자 갱신, 참조/경계/RT 준비, 메시 기록, ExecuteCommandBuffer, 출력 갱신을 구분한다. 필요한 경우 첫 프레임/초기 준비를 별도로 수집한다.
- Deep/FrameDebugger는 끄며 CPU/Rendering/Memory/UI/Audio를 기록한다. 기존 Probe를 재사용한다. 제품 표현/업데이트 정책은 변경하지 않는다.
- 연결/씬/dirty/Play/컴파일/Profiler를 확인한 뒤 자체 검사만 순차 수행한다. 사용자 작업이 시작되면 겹치지 않는다. 빌드/실행파일/push 금지.

## 시작
현재 전투씬1/clean, Play/pause/compile/update/Profiler/Deep/FrameDebugger=false. Unity6000.6.2f1/CLI1.0.0-beta.13. 기존 사용자 설정2파일과 미추적 파일을 보존한다. [설정 해시](settings_before.json).

## 현재 결과
[결과 정본](RESULTS.md). 전후100×3/추가300×3, 총1,500프레임에서 원래162.609ms는 재현되지 않았다. 정상 구간의 접촉 마스크 경로는 약8.7~8.8ms이며 마커 후 평균 프레임은 관측상 증가했다. 원인 해결/최적화 성공이 아니다. 관련 계약13/13과 종료 상태·설정 해시를 확인했다. 기존 EditMode 검사 후 native 잔류의 소유권 한계는 결과 정본을 따른다.

재분석: `analyze.ps1` → `summarize_paths.ps1`. native 하위 경로 재추출은 기록이 꺼진 상태에서 공식 `run_script`로 `Inspect.cs`의 `CGShadowInspect.Run("after")`/`Run("extended")`를 순차 사용한다. 압축 원본 검증은 `archive_profiler.ps1`을 따른다. 기존 압축이 있으면 덮어쓰지 않는다.

공식 근거: [Unity6.0 ProfilerMarker 추가](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/profiler/customizing/adding-information-code/add-markers-code), [Unity6.6 API](https://docs.unity.com/ja-jp/engine/6000.6/script-reference/unity/profiling/profilermarker). 2026-10-21 검색에 노출된 공식 본문 요약의 static marker/Auto 범위를 확인했다. 전체 영상 시청/Editor 계측 오버헤드0의 근거로 확대하지 않는다. 기존 사례 조사는 [정본](../optimization_research_20261020.md)을 재사용했다.
