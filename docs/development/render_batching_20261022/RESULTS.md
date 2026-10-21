# 렌더링 제출 원인 조사 / 설정 교차 진단

2026-10-22 Asia/Seoul. 기준 `1278449` / 제품 구현 `5943523`. 사용자가 정상30R·장시간 후속 중 Draw Calls/SetPass 감소를 우선 요청했다. **제품 코드·씬·설정 변경은 없고, 설정 실험은 원복했다.** 스프라이트 배칭 + 공용 마스크 아틀라스/출력 배칭 방향은 사용자 승인, 구체 검토용 설계는 [DESIGN.md](DESIGN.md)다.

## 조건 / 실행 범위

- Unity6000.6.2f1 / URP17.6 / D3D12 Editor, 전투씬 하나·clean,1920×1080·기존 카메라. 기존 [누적 검사](../runtime_validation_20261021/RESULTS.md)의169개를 반복하지 않았다.
- 자체 Play에서 프레임 진행을 확인하고, 기존 FrameProbe를 새 ID/폴더로 복사해128적/실제15포탑을 준비했다. 진단 주입은 Attack112/Buff16, HP100000/골드0/코어피해0이며 정상30R 부하가 아니다. FrameProbe의 실시간60프레임 워밍업 후 동결 구조 캡처다.
- 다음 설정 교차는 동일한 생성 상태에서 `game.enabled=false`, `Time.timeScale=0`, SRP ON/OFF 순서로3회씩, 각60프레임 워밍업+180행 관측. 렌더·LateUpdate·일부 unscaled 효과/애니메이션은 계속 진행하므로 완전한 동일 픽셀 상태가 아니며 정상전투 FPS로 확대하지 않는다. TestSpeed/품질/물량을 조정하지 않았다.
- CPU/Rendering/Memory/UI/Audio 등 기존 Profiler 모듈, Deep·할당 스택 OFF. Native 기록은6개이며 `Logs/CG_RenderBatching_20261022/settings_{0..5}.data`에 보존한다. 사전 Profiler buffer도 같은 폴더 `prior_profiler_buffer.data`로 보존하고 종료 때 다시 로드했다. 새 바이너리 삭제/압축이나 Git 추가는 하지 않았다.
- 기록 종료 경계로 native178/179개 대 stats180행이다. 없는 프레임을 보간하거나 stats와1:1 연결하지 않았다. 각 native 관측 FPS는 유효 frameTimeMs 합계로 계산했다. 아래는 관측 표본 FPS이며 전체 연속 창 FPS가 아니다. 유효 스파이크를 제외하지 않았다. 원본·무효 표본 수·p95/p99는 [analysis.json](analysis.json).

## 설정 하나만 바꾼 결과

|지표 / 평균|SRP ON|SRP OFF|절감량(ON−OFF)|절감률|
|---|---:|---:|---:|---:|
|Draw Calls / frame|2073.963|538.402|1535.561|74.040%|
|SetPass / frame|463.428|513.339|−49.911|−10.770%|

|회차 / SRP|stats / native 프레임|평균 Draw / SetPass|native 평균ms|관측 최소 / 최대 / 평균FPS|
|---|---|---|---:|---|
|0 / ON|180 /178|2070.428 /462.489|26.762|6.126 /41.221 /37.367|
|1 / OFF|180 /179|538.033 /512.972|25.991|21.209 /41.072 /38.475|
|2 / ON|180 /179|2075.694 /464.028|26.220|26.142 /40.817 /38.139|
|3 / OFF|180 /179|538.483 /513.439|26.806|23.500 /40.801 /37.305|
|4 / ON|180 /179|2075.767 /463.767|27.257|22.377 /40.749 /36.688|
|5 / OFF|180 /179|538.689 /513.606|25.905|23.823 /41.327 /38.602|

Draw는 줄지만 SetPass는 늘어 두 지표 동시 개선이 아니므로 **미채택**이다. FPS 반복 범위도 겹치며 정상 전투 전체 개선이 아니다. 기존 약30FPS와 이번 약37FPS는 조건이 달라 비교하지 않는다.

## 구조의 근거

[Frame Debugger 원본](baseline_frame/frame_debugger.json):617개 이벤트, `Selected unit sprite union` clear145/draw145, 일반 Mesh 출력274, SRPBatch 이벤트24, HUD Mesh25. SRPBatch 이벤트는 여러 draw를 포함하므로617개 이벤트를617 Draw Calls로 부르지 않는다.616개 GetFrameEventData=false여서 shader/renderTarget/batchBreak는 무효이며 type/info만 사용했다. [완료](baseline_frame/frame_debugger_completion.json)error=null.

[Renderer inventory](renderer_inventory.json)는 캡처 사이의 일시 상태이며 카메라 callback에서만 유효한 outline 전체를 나타내지 않는다. Unlit SpriteRenderer1322/재질1/texture19, Lit259/재질1/texture2, TowerBaseShade15/재질15, 활성 TowerGlow15/재질15다. outline 별도 조회에서 contact129개가 sort43이고 emphasis는 각 body 정렬에 따라 분산돼 있었다. 모든 outline을 무조건 한 번에 그리는 방법은 채택하지 않는다.

현재 공유된 것은 한 본체의 강조/접촉 mask이며, 서로 다른 개체의 mask 타깃/출력 texture는 별개다. 아틀라스와 순서를 보존하는 출력 batch가 SetPass 감소 후보다. 기존 pooling이나 Flyweight 방식의 재질 공유만으로 이 texture 전환은 사라지지 않는다.

## 참조와 실패

- [Unity6 SRP Batcher](https://docs.unity3d.com/6000.0/Documentation/Manual/SRPBatcher.html), [draw call batching](https://docs.unity3d.com/6000.0/Documentation/Manual/DrawCallBatching.html), [Sprite atlas](https://docs.unity.com/en-us/engine/6000.5/manual/unity2d/sprite/atlas/atlas-introduction). 공식 페이지 본문/검색 결과와 실제 설치된 URP 소스를 확인했다. 새로운 동영상을 전체 시청한 것은 아니다. 실제 게임 사례는 [기존 조사](../optimization_research_20261020.md)를 재사용하며 타 게임 개선율을 가져오지 않는다.
- Dynamic Batching getter/setter는 현재 패키지의 `Obsolete(..., true)`로 진단 컴파일 거부. 소스에서 필드가 과거 직렬화 호환용임을 확인해 반사로 강제ON하지 않았다. 게임 컴파일 실패가 아니다.
- settings probe 초안은 `UnityStats.batches` 부재로 진단 컴파일 실패. 해당 행을 빼고 실행했다. native 추출 초안은 null sample name으로 예외, null 제외를 추가해6개 파일을 정상 추출했다. 실패한 진단은 통과로 세지 않는다.
- 선행 잔류 메모리 준비에서 옛 namespace의 MemoryProfiler 참조 진단이 CS0619. 계측/snapshot을 실행하지 않았으며 API가 `Unity.Profiling.Memory`로 이동한 것만 확인했다.
- `SystemInfo.supportsGpuRecorder=true`지만 `ProfilerDriver.isGPUProfilerSupported=false`, OS=true/buggy=false다. GPU0ms는 유효 측정이 아니다. Memory Profiler package도 manifest/loaded assembly에서 미검출. GPU/VRAM·snapshot/최종 잔류는 미완료다.

## 복원과 다음

[초기 상태](initial_state.json) → [종료 상태](final_state.json):Play 정지, scene1/clean, compile=false, SRP ON, Profiler OFF/driver true, background false, time1/capture0, FrameDebugger OFF. 기존 Profiler buffer 재로드 성공. 콘솔 조회는 제품 오류0/compile failure 없음.

[전 hash](hashes_before.json)/[후 hash](hashes_after.json):저장된 사용자 전투scene, PackageManagerSettings, TimeManager, UniversalRP4개 모두 일치. Assets/Scripts·Assets/Resources·Assets/Settings의 디스크diff 추가 없음. 원래 Git 상태는 M3/D369/미추적29항목(본 폴더 생성 시점, font path 접근 표시 포함)이며 복원/삭제하지 않았다. 자체 로그/진단 소스/설계만 추가한다.

사용자가 승인한 설계 방향에 따라 [설계 문서](DESIGN.md) 검토→구현 계획→단계A/B 구현·새 대상 검사/픽셀 대조/동일 조건3회 성능 측정으로 진행한다. 정상30R·장시간·잔류 memory의 기존 미완료도 유지한다. 빌드·실행파일·push·CLI 업데이트는 하지 않았다.
