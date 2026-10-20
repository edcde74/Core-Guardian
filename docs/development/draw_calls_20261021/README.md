# Draw Calls / SetPass 사전 진단

2026-10-21 Asia/Seoul. 시작 HEAD `1ea021a`. 사용자는 SetPass Calls/Draw Calls 감소를 우선 과제로 지정했고, 구현 방식 질문에는 **진단 결과만 정리**를 선택했다. 이번 작업은 조사·사전 계측까지다. 제품 코드·셰이더·씬·원본/CSV·패키지를 변경하지 않았다. 후속 구현 승인이 아니다.

## 변경 전 기준
[계측 코드](Probe.cs)는 기존 ControlledProbe의 경로/클래스 이름을 바꿔 재사용했다. Unity6000.6.2f1 Editor, URP17.6.0, Ryzen5600X/RTX3070,1920×1080,VSync1,targetFrameRate-1,배속1,camera8.5. 적128/실제15포탑, HP100000/CoreDamage0/Gold0 주입 스트레스다. waveTime=.001로 자동 첫 적을 건너뛰고, 워밍업30+관측100 엔진프레임×3회를 수행했다. Deep/할당 스택 끔, CPU/Rendering/Memory/UI/Audio 모듈을 사용했다. [환경](before/normal_environment.json), [완료](before/normal_completion.json), [수치](baseline_summary.json).

|회차|프레임|Draw Calls 중앙|SetPass 중앙|delta 중앙ms|최저/최고/평균 FPS|
|---|---:|---:|---:|---:|---|
|0|100|3,208|1,608|43.610|9.177 / 25.363 / 21.123|
|1|100|3,208|1,608|49.031|8.049 / 25.213 / 19.056|
|2|100|3,208|1,608|41.943|14.219 / 25.657 / 22.726|

평균 FPS=프레임 수×1000/delta 합계다. Editor·주입 진단이며 정상30R/standalone 성능이나 목표 달성값이 아니다. 변경 후 비교가 없어 절감량/율은 N/A다. 각 `*_frames.json`에 원시 delta를 보존했다. [캡처 유효성](capture_validity.json)의 Main PlayerLoop98/99/99개와 엔진 delta 각100개는 구분한다. RT 추적/Editor 대기/계측 부하도 포함한다.

Profiler 원본3개는 [압축](profiler_raw.zip)에 보관하고 [manifest](archive_manifest.json)의 SHA256을 압축 해제 스트림으로 모두 대조했다. 미압축 원본도 로컬에 유지하고 중복 추적만 제외했다. [압축 코드](archive_profiler.ps1). 최초 압축은 탐색 경로 오류로0개/22byte였으므로 유효 원본으로 세지 않고 경로 수정 후3개를 다시 검증했다.

## Frame Debugger에서 확인한 구조
[구조 캡처](frame/frame_debugger.json), [종료/복원](frame/frame_debugger_completion.json), [코드](FrameProbe.cs). 실제 씬128적/15포탑을60프레임 진행한 뒤 Prototype 갱신을 멈추고 한 프레임을 채집했다. 진단 부하가 있는 구조 확인이며 위 시간 계측과 직접 비교하지 않는다.

- 전체1,904이벤트 중 `Selected unit sprite union`1,578개=ClearColor274+Mesh1,304다. 본체 파츠의 반복 마스크 그리기가 우선 조사 대상이다.
- 일반 RenderLoop.Draw275, DrawSRPBatcher24, Overlay Canvas24이벤트도 있다. 이벤트 수와 UnityStats Draw Calls/SetPass는 같은 지표가 아니므로 비율/절감량을 직접 계산하지 않는다.
- GetFrameEventData는1,904건 중1건만 유효했다. dataValid=false의 shader/renderTarget/batchBreak는 근거로 쓰지 않는다. 전부 UI/Default로 보이는 값은 유효한 셰이더 분류가 아니다. 전체 배칭 단절 이유/셰이더별 호출 귀속은 미확인이다.
- [리소스](frame/resources.json)의 표시 마스크274=강조145+접촉129. RT307개에는 시작 전부터 남아 있던33개가 포함된다. GPU VRAM/Memory Profiler 스냅샷이 아니다.

추가 [소스 집계](repeated_sources.json)는 메인 스레드 대기5초로 응답이 타임아웃됐지만 파일 출력은 완료됐다. 적0명으로 끝난 후의 상태이므로128적 부하의 근거/개선 예상치로 쓰지 않는다. FrameProbe의 ProfilerDriver 네임스페이스 누락은 임시 메모리 컴파일에서 발견·수정했으며 제품 컴파일 실패가 아니다.

## 검토 후보 — 미구현
기존 `CoreGuardianSelectionOutline` 안에서 같은 texture/opacity의 파츠를 재사용 메시로 묶어 한 그룹당 한 번 그리는 방식을 검토했다. 다른 조건은 분리하고 Blend Max 합집합·RT/UV·알파 문턱·flip/변환·출력 순서를 보존해야 한다. 실제로 묶을 수 있는 그룹 수와 메시 갱신 CPU/GC 비용은 미측정이며,1,304회가274회로 줄어든다거나 FPS가 특정 수치만큼 오른다고 보장하지 않는다.

같은 텍스처라는 이유만으로 배칭을 가정하지 않고 향후 구현 시 Draw/SetPass·CPU·GC·메모리·픽셀을 같은 조건에서 대조한다. 픽셀 회귀나 비용 악화는 통과가 아니다. 사용자는 현재 **진단 결과만 정리**를 선택했으므로 이 후보나 마스크 공유/아틀라스/HUD/Session을 적용하지 않았다. 우선순위는 SetPass/Draw Calls 감소 검토를 먼저 두고, 구현 재개 요청 시 구체 범위를 정한다.

## 공식 근거와 한계
- Unity 공식 [draw call 방법 선택](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/graphics-performance-profiling/reduce-draw-calls/optimizing-draw-calls-choose-method):6000.0 본문의 Stats/Profiler/Frame Debugger 조사, 머티리얼 재사용과 URP의 MPB 제약을 읽었다. 단순 SRP Batcher 설정이 Draw Calls 자체를 합쳐 준다고 해석하지 않는다. 사용 엔진6000.6과 문서 버전을 구분한다.
- Unity 공식 [병합 메시 속성](https://docs.unity.com/en-us/engine/6000.3/manual/analysis/graphics-performance-profiling/reduce-draw-calls/draw-call-batching/properties):MPB의 SRP Batcher 호환성 제약을 확인했다. 기존 마스크 CommandBuffer의 MPB를 무조건 바꾸면 개선된다는 증거는 아니다.
- 기존 공식 영상/게임 사례 조사는 [담당 기록](../optimization_solutions_research_20261020.md)을 참조한다. 이번에 영상 전체를 새로 시청하거나 타 게임 개선율을 적용하지 않았다.

## 종료 상태
[엔진 복원](restored_engine.json): 자체 Play 종료, 전투씬1/dirty=false, compile/update=false, Profiler/Deep/FrameDebugger=false, camera8.5, timeScale1/captureDelta0/backgroundfalse, 음량/저장 음량은 시작값이다. 직접 연 Frame Debugger 창도 닫았다.

[설정3파일](settings_restored.json)의 SHA256은 [시작값](settings_before.json)과 일치한다. 종료 시 disk Standalone define은 이미APP_UI_EDITOR_ONLY여서 추가 변경하지 않았다. 기존 미커밋 설정 차이·미추적·폰트 도구 접근오류는 보존했다. 제품 변경·패키지 설치·빌드·실행파일 실행·push는 하지 않았다.
