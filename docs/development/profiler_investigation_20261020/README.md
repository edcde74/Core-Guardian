# 대량 전투 Profiler 병목 조사

2026-10-20 Asia/Seoul. 사용자 요청: 엔진 프로파일링으로 병목을 깊게 조사. 시작 HEAD `445ae8e`. 게임 소스는 유지했으며 이번 작업은 최적화 구현·성능 통과·밸런스/아트 승인이 아니다. [공통 규칙](../OPTIMIZATION_RULES.md)을 적용했다.

## 결론
**지속 비용의 우선 대상은 사진 그림자의 동적 접촉과 상시 외곽 강조가 공유하는 실루엣 마스크 경로다.** HUD 전행 재생성은 별도 개선 후보지만 이번 스트레스에서 가장 큰 지속 호출은 아니었다. Session 버프 계산도 반복 할당과 느린 프레임의 추가 하위 Tick 비용 때문에 후속 개선 가치가 있다.

같은 품질에서 마스크 공유·캐시·리소스 수명 관리와 반복 탐색/할당 감소를 먼저 검토한다. 그림자·강조·보행·효과 수/수명·사운드를 줄이지 않는다. 단순 풀링이나 Unity 설정 변경만으로 해결된다는 근거는 없으며 절감량/최종FPS를 미리 약속하지 않는다.

## 환경·측정 구분
- Unity6000.6.2f1, CLI1.0.0-beta.13 최신 확인, URP17.6.0. Ryzen5 5600X/RTX3070/RAM32681MB/Windows11, Game1920×1080, VSync1/targetFrameRate -1/1배속, 원래 카메라/표현 유지.
- 실제 전투씬·HP100000·재화/적 주입. 적8명 중1명 Buff, 나머지 Attack. 경로 거리20+(i%12)*0.3에서 시작. 포탑32기 설치 시도 중 **실제15기**. 초기 진단 적3종의 생성/퇴장은 원본 실제 적수/호출수에 반영된다.
- normal은 적32/128 교대로 각3회. 매회30엔진프레임 워밍업+100엔진프레임 관측. CPU/Rendering/Memory/UI/Audio를 사용하고 엔진 원본을 SaveProfile로 저장했다. RawFrameDataView로 Main/Render Thread 실제 샘플·호출·GC.Alloc을 추출했다.
- normal은 Deep/할당 호출 스택 끔. 상세 진단에서만 별도 활성화했다. Profiler 창 자체·Editor·대기·다른 프로세스 영향이 남는다.
- **100엔진프레임이100개 유효 PlayerLoop 샘플은 아니다.** 수신 지연/경계에서 PlayerLoop가 없는 raw 프레임은 제외했다. 엔진 delta/UnityStats와 raw marker는 시계·경계가 달라1:1 차감하지 않는다.
- 자세·위치·실제dt가 완전히 동일한 고정 재생이 아니므로 반복 편차를 그대로 제시한다. 진단 모드는 증강 추첨을 사용하지 않으며 고정 seed/입력 재생 실험이 아니다. 정상30R/사람 난이도/빌드FPS 결과로 사용하지 않는다.
- 환경: `captures_valid/normal_environment.json`. 주요 소스/패키지 전후 SHA256: `source_hashes_before/after.json`. 실제 참조 CSV/텍스트 해시: `active_data_hashes.json`의 기존 enemies/towers/stage/upgrades/waves.

## 일반 Profiler 반복 결과
시간 단위는 ms. marker 값은 프레임 내 같은 이름의 호출을 합한 뒤 구한 중앙값이다. 부모/자식 마커를 합산하지 않는다.

|실험|적|유효 Main 프레임|엔진 delta 중앙|그림자 LateUpdate 중앙|강조 LateUpdate 중앙|전투 어댑터 Update 중앙|Draw Calls 중앙|SetPass 중앙|Main GC.Alloc 중앙 B|
|---|---|---|---|---|---|---|---|---|---|
|normal_0|32|98|29.533|4.335|4.090|0.970|1097|453|140113.5|
|normal_1|128|99|97.487|15.350|13.629|3.855|3238|1637|524992|
|normal_2|32|99|32.107|4.400|4.191|1.051|1101|453|139998|
|normal_3|128|99|96.947|15.406|13.716|3.994|3247.5|1638.5|517890|
|normal_4|32|99|29.278|4.104|3.882|0.913|1102|453|139981|
|normal_5|128|98|105.273|16.330|14.238|4.116|3238|1635.5|524229|

전체 delta에는 Editor/대기가 포함된다. 위3함수와의 차이를 나머지 게임 CPU나 GPU로 취급하지 않는다. GPU 모듈의 유효 GPU 시간은 이번에 수집하지 않았다. Render Thread 실제 샘플은 확보했지만 대기 포함 시간을 순수 렌더 작업으로 해석하지 않는다.

normal128의 평균 직접 GC.Alloc 귀속은 사진 그림자 Invoke 바로 아래 약210.8~211.2KB/frame, 강조 Invoke 아래89.5~89.9KB/frame, Prototype.Update 아래212.9~224.1KB/frame이다. **가장 가까운 부모 마커에 대한 귀속이며 재귀적 함수 전체 할당량이 아니다.** GC.Alloc 전체 합계와 구분한다.

normal128의 GC.Collect는 각 실행의 최대10.38~11.95ms, GarbageCollector.CollectIncremental은 최대13.59~16.62ms였다. 관련/중첩 마커일 수 있어 합산하지 않는다. Editor 포함 관측이므로 게임만의 GC 정지 시간으로 단정하지 않는다.

## 상세 호출에서 확인한 구조
`captures_instrumented/instrumented_0_n128.data`는 정식 Deep Profiling 전환·스크립트 재로딩 후 새 Play에서 확보했다.20엔진프레임/18유효 Main 프레임의 짧은 진단이다. 큰 계측 부하 때문에 normal과의 시간 차이·CPU 비중·예상 절감량 계산에 사용하지 않는다.

|상세 마커|평균 호출/유효 프레임|해석|
|---|---|---|
|SelectionOutline.Present|275|강조145+이동 접촉129+숨긴 일반 선택1. 실제 표시274|
|PresentAtOrder|145|ObjectEmphasis.PresentTargets에서 호출|
|PresentGroundContact|129|PhotoShadow.Present에서 호출|
|EnsureMask|274|표시 실체마다 마스크 크기 확인|
|RenderTexture.Create|246.78|고빈도 생성. 소스의 크기 불일치 시 Release/새 RT 생성과 일치|
|Component.GetComponentsInChildren|571.56|본체/자식 레이어 반복 탐색. GameObject판과 부모/자식 중복을 합산하지 않음|
|Session.StepTick|5.72|Deep 부하로 delta가 늘어 최대0.05초 분할 Tick도 증가|

- SelectionOutline.Present는 매번 자식 SpriteRenderer 조회·Bounds/화면 사각형 계산·마스크 그리기·quad 정점 배열 생성·LINQ 정렬을 수행한다. EnsureMask는 정수 크기가 달라지면 기존 RT를 Release하고 새로 만든다.
- **크기 흔들림에 따른 재생성과, 같은 몸체의 중복 합성**이 우선 검토 가설이다. 개체별 크기 이력까지 추적한 인과 대조는 하지 않았으므로246.78회 전부를 특정 개체/원인에 귀속하지 않는다.
- PhotoShadow.Present는 자식 SpriteRenderer 배열을 다시 만들고, 이동 접촉에 같은 합성 마스크 경로를 사용한다. 강조 쪽도 본체 레이어를 재탐색한다. 접촉 품질을 유지하며 합성 결과/조회 참조를 공유할 수 있는지 검토한다.
- 같은 클래스여도 접촉/강조의 폭·패딩·화면 잘림·접촉 밴드 요구가 다를 수 있다. 기존 RT를 그대로 하나로 합칠 수 있다고 확정하지 않는다. 공유 시 동일 픽셀·자세·발 접점·외곽선·중첩을 보존하는 설계가 필요하다.
- Session.StepTick의 전체 적→각 적→Buff 적 탐색→ToDictionary/LINQ가 할당원으로 나타났다. `allocation_examples.json`에서 버프용 람다·PhotoShadow.Present·SelectionOutline.Present/EnsureMask·SynchronizeViews/SyncRemoved의 호출 스택을 확인했다.3개 선택 프레임의 GC.Alloc31건마다 추출한 예시이므로 표본 bytes를 전체 귀속 비율로 사용하지 않는다.
- HUD.Refresh도 상세 샘플에 있지만 이번 조건은16피해 항목 전체의 빈번한 변경을 재현한 HUD 전용 스트레스가 아니다. [이전 HUD16행 진단](../performance_baseline_20261020/README.md)과 구분한다.

## Frame Debugger·메모리 실체
새 자체 Play에서 적128/포탑15를 만든 뒤 Session/뷰 동기화를 멈춘 고정 자세의 단일 프레임을 확보했다. 자체 Play만 일시pause하고 종료 후 해제했다. 이동 중 normal의 Draw Calls와 직접 비교하지 않는다.

- `frame_debugger.json`:869이벤트. Selected unit sprite union554이벤트(274ClearColor+280Mesh), 일반2D RenderLoop.Draw275이벤트 등을 확인했다. 이벤트 수는 UnityStats Draw Calls와 같은 지표가 아니다.
- `resources.json`: 강조145/접촉129/일반 선택1, 표시274, 마스크 RT274개. GetRuntimeMemorySizeLong 기준 마스크 실체 합계5687576B. SelectionMask 재질274/SelectionOutline 재질274/PhotoShadowContact 재질129개.
- 리소스 목록은 Memory Profiler 스냅샷이나 GPU 실측VRAM이 아니다. 로드된 미사용 재질도 포함하므로 Bloom 재질 존재를 효과 활성화로 해석하지 않는다.
- Profile Analyzer/Memory Profiler 패키지는 미설치(profiling.core1.0.3만 확인). 새 패키지를 설치하지 않고 엔진 RawFrameDataView로 분석했다. 전용 스냅샷의 참조 유지/누수와 GPU 시간은 미검증이다.

## 무효/부분 시도와 진단 오류
- 최초 Profiler.enabled=true만으로는 수신 프레임이-1이었다. 루트 normal_*는 무효. `captures/`는 최초 추출 중 무명root 처리 오류가 난 부분 시도다. 일반 반복 근거는 **captures_valid/normal_0~5만** 사용한다.
- `captures_valid/deep_*`는 deep 플래그만 바꿔 관리 메서드 상세가 부족했다. 정식 상세 호출 근거는 captures_instrumented다.
- 정식 전환에는 Reload 대화상자가 나타났다. 자체 Play 중 재로딩으로 비직렬화 Session/표현 참조가 끊겨 NullReference가 발생해 Play를 종료하고 새 세션으로 다시 측정했다. 오류 상태의 프레임은 결과에 섞지 않았고 제품 코드를 수정하지 않았다.
- Frame Debugger 저수준 SetEnabled만으로는 이벤트0이었다. 전용 창의 EnableFrameDebugger를 거친 마지막 수집이 유효하다.0건을 그리기 없음으로 해석하지 않는다.
- Profiler UI 범위 밖 프레임 오류 등 진단 로그 이력은 `diagnostic_console.json`에 저장했다. 모든 시도가 성공했다고 표시하지 않는다.

## 복원·보존
자체 Play 종료, Deep/할당 호출 스택/Profiler/Frame Debugger 중지, 이번에 연 두 진단 창 닫기를 확인했다. TimeScale1/captureDelta0/runInBackgroundfalse, 원래 Edit AudioListener 음량1 복원, 저장 음량0.195603669 유지. `restored.json` 참조.

전투씬1·dirtyfalse·compilingfalse. 최종 Console groundTruth 오류0/경고0이며 MCP 버퍼의 과거 오류 이력과 구분한다. Assets/Packages diff 없음, 주요 소스/패키지 전후 해시 일치. 기존 ProjectSettings2파일·미추적 자료·font_tools_vendor 접근 오류 표시는 보존했다. 씬 YAML 편집·최적화 구현·빌드/실행파일 실행·push는 없다.

## 원본·재개
- `ProfilerProbe.cs`: Assets 밖 진단 코드, run_script로 메모리 안에서 컴파일. 자동 실행 자산이 아니다. 수신 검사와 무명root 처리를 포함한다. 일반 반복과 짧은 Deep/alloc 진단을 구분한다.
- `FrameProbe.cs`: 자체 고정 스트레스용 Frame Debugger/리소스 조사. 사용자 Play에서 재실행하지 않는다.
- `ReadAllocationStacks.cs`: 저장된 alloc 원본에서 호출 스택 예시 추출.
- `analyze.py`와 `marker_summary.json`: 유효 Main PlayerLoop 프레임의 평균/중앙/p95/최대/호출/직접 할당 전체 표. 짧은 Deep의p95는 참고 분포이며 반복 성능 판단에 사용하지 않는다. p99는 보고하지 않는다.
- 무압축.data는 디스크에 유지하고 Git에는 `raw_profiler_captures.zip`과 `capture_archive_manifest.json`을 저장한다.13원본 모두 SHA256과 ZIP에서 복원한 결과의 일치를 검증했다. 실패/부분 시도도 포함하므로 위 유효성 구분을 따른다. Unity에서 보려면 원하는.data를 추출해 Profiler Load/Import로 연다.

## 다음 구현 범위 결정
1. 공통 본체 참조·합성 마스크 공유/캐시와 크기/수명 관리부터 설계한다. 사진 접촉/외곽선의 동등성을 유지하는 방안을 비교한다.
2. 뷰/전투 표시 객체 풀링과 HUD 행 재사용은 각각 생성/삭제 피크·지속 할당을 분리해 계측한다.
3. Session 버프 공급자·후보 탐색/LINQ는 보조 후보. 처리 순서·타이브레이크·전투 결과·이벤트 일치가 조건이다.
4. Unity 설정은 현재 설정·버전·적용 조건을 감사한 뒤 개별 실험한다. 근거 없는 일괄 토글은 하지 않는다.

현재 모든 최적화는 미적용이며 변경 후 비용/절감률은 N/A다.
