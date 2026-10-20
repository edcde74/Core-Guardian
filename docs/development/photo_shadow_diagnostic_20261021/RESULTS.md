# PhotoShadow 지연 조사 결과

2026-10-21 Asia/Seoul. 기준 `77aebb9`. 변경은 PhotoShadow/SelectionOutline 2파일의 ProfilerMarker 12개뿐이다. 외형·마스크 생성 순서·계산식·호출 횟수·공유 정책을 바꾸지 않았다. 이 기록은 최적화 완료가 아닌 **원인 조사**다.

## 판정

이전 PhotoShadow 162.609ms/자체159.540ms는 이번 1,500 관측 프레임에서 재현되지 않았다. 변경 전300프레임과 세분화 마커 후1,200프레임을 구분한다. 실제 PhotoShadow 마커가 있는 유효 native 프레임은 각각296/1,192건이다. 이번 최대 PhotoShadow 합계는 변경 전27.112ms, 마커 후26.427ms다. 미재현을 해결·Profiler 잡음·GC·GPU 업로드 원인 확정으로 바꾸지 않는다.

정상 구간에서는 접촉 마스크 생성이 그림자 비용의 대부분이다. 따라서 기존의 동일 본체 마스크 공유 후보는 여전히 근거가 있지만, 원래 순간 지연을 고치는 방법으로 확정한 것은 아니다. 공유는 이번에 구현하지 않았다.

## 조건과 전후 비교

기존 [Probe](../mask_atlas_20261021/Probe.cs)를 그대로 사용해 준비30/측정100프레임×3을 전후 수집했다. Unity6000.6.2f1, Editor D3D12, Ryzen5600X/RTX3070/32GB, 1920×1080/카메라8.5/VSync1/target -1/배속1/captureDeltaTime0/기존 사운드, 128적(112Attack/16Buff)/실제15포탑, 동일 주입/설치 정책이다. 상세는 `before/normal_environment.json`과 `after/normal_environment.json`이다. CPU/Rendering/Memory/UI/Audio, Deep/할당 스택/FrameDebugger 꺼짐. 자연 dt/보행/발사 위상은 완전 고정하지 않았다. 전후는 분 단위 간격이며 중간 재컴파일/Play 재진입이 있다.

|지표/단위/통계|마커 전|마커 후|절감량|절감률|원본/판정|
|---|---|---|---|---|---|
|프레임 평균 ms/각300건|35.502|36.538|-1.037|-2.92%|관측상 증가; 마커 오버헤드와 실행 변동 분리 불가|
|p95 ms/각300건|44.201|45.307|-1.106|-2.50%|관측상 증가|
|평균 FPS/총프레임÷총시간|28.168|27.368|-0.799|-2.84%|FPS 증가량/율이며 개선 아님|
|최소/최대 FPS|17.546/31.433|17.267/31.188|N/A|N/A|각 회차 분포는 원본 보존|
|PhotoShadow 평균 ms/유효296→295건|11.086|11.375|-0.289|-2.60%|마커 추가 비교이며 제품 절감 아님|
|Draw Calls 중앙|2178|2178|0|0%|3회 모두 동일|
|SetPass 중앙|578|578|0|0%|3회 모두 동일|

[전후 상세/GC/메모리/PlayerLoop](comparison.json), [전후 CPU 마커](marker_summary.json), [회차별 FPS·프레임 분포·누락](frame_summary.json). delta는 전부 양수·유한하며 제외0이다. native Main Thread는 전98/100/100, 후98/100/99건이며 PhotoShadow를 포함한 프레임은 전98/99/99, 후98/99/98건이다. 경계 프레임을0ms로 평균에 섞지 않는다. GPU 시간은 유효 계측이 없어 N/A다. Editor 전체 메모리는 Profiler 데이터/로드 차이를 포함해 제품 전용 증가로 귀속하지 않는다.

## 그림자 내부 비용

저장된 native 캡처를 [Inspect.cs](Inspect.cs)로 순회해 **PhotoShadow 호출 하위의 마커만** 분리했다. 전체 Mask 마커는 강조 등도 포함하므로 그림자 비용으로 합산하지 않는다. `after/photo_paths.json`과 [요약](path_summary.json) 참조.

|마커/평균 ms per frame|후0|후1|후2|해석|
|---|---|---|---|---|
|PhotoShadow 전체|11.413|11.278|11.433|각 유효98/99/98프레임|
|Camera.main 조회|약0.05~0.06|동일 범위|동일 범위|정상 구간 주요 비용 아님|
|Cast: 기존 그림자/참조/Transform/정렬|2.38|2.34|2.39|마스크와 별도|
|Contact: 접촉 마스크 경로|8.81|8.70|8.80|정상 비용의 대부분|
|Mask.Parts|2.05|2.05|2.07|메시 준비/업로드/명령 기록; MeshUpload는 이 값에 포함|
|Mask.Execute|2.99|2.97|2.98|명령 제출 및 엔진 작업; GPU 소요시간 아님|
|Mask.Output|1.50|1.49|1.50|출력 quad/UV/정렬/재질 갱신|

행들은 부모/자식 관계이므로 전부 더하지 않는다. Contact에는 마스크의 참조 확인·경계/투영·RT 용량 확인 등도 포함된다.

## 추가 지연 관측

세분화 상태의 [기존 Probe 복사본](LongProbe.cs)은 클래스/출력 경로/측정300프레임/마감120초만 바꿨다. 준비30/측정300×3, 원래 전후 비교와 별도다. [조건](extended/normal_environment.json), [완료](extended/normal_completion.json).

추가900건의 평균35.716ms/p95 44.068ms/최대97.222ms, FPS 최소10.286/최대31.454/평균27.999. 유효 native900건 중 PhotoShadow가 있는897건의 평균11.295ms/최대26.427ms, 개별 PhotoShadow 호출 최대5.931ms다. 원래162.609ms는 재현되지 않았다.

추가1회차 가장 긴 native Main Thread는94.121ms/frame4로, PhotoShadow 합26.427ms(Cast12.588/Contact13.479), EditorLoop self44.160ms였다([원본 발췌](extended/worst_main.json)). 이 프레임은 Cast와 Contact 양쪽이 늘었으며 원래 지연과 같은 원인이라고 할 근거가 없다. 타이밍 도구의 profilerFrame=-1이 계속되어 native frame4와 engine delta97.222ms의 정확한 일대일 결합도 주장하지 않는다. 단일 Cast 최대5.778ms 내부는 추가 분리하지 않았으며 OS 스케줄링/렌더 동기화 등으로 추정하지 않는다.

## 회귀·원본·복원·한계

- 이번 기존 그림자10/10([결과](photo_tests.json)), 기존 아웃라인3/3([결과](outline_tests.json)). 아웃라인의 단순 GPU 픽셀/용량 재사용/캐시 계약을 포함한다. 전체 게임/전체3456픽셀/보스 전체 합성/사람 체감 재검사 아님.
- [Profiler 압축](profiler_raw.zip)은 전3/후3/추가3의9개 원본, 384,412,921bytes. [SHA256/크기](archive_manifest.json)를 압축 내부와 대조했다. [보존/검증 스크립트](archive_profiler.ps1). `.data`는 로컬에 유지하고 중복 추적하지 않는다.
- 오프라인 분석 첫 실행은 이름 없는 native 루트의 null 처리 누락으로 실패했다. null 보완 후 후297/추가900 native 프레임 분석을 완료했다. 상태를 eval에서 ephemeral 클래스명으로 직접 읽으려던 요청도 컴파일 실패했다. 원래 캡처들은 모두 완료/error=null이며 이 분석 실패를 캡처 실패로 섞지 않는다.
- [종료 상태](restored_engine.json): 전투씬1/clean, Play/pause/compile/update/Profiler/Driver/Deep/profileEditor/stacks/FrameDebugger=false, background=false, 카메라8.5. autotick은 이미 켜져 있었고 진단에16ms/persist=false를 사용했다. 기존 영속 설정은 변경하지 않았다.
- [시작 설정](settings_before.json)과 [종료 설정](settings_after.json) 3파일의 해시는 동일하다. 기존 사용자 설정2파일/미추적 파일은 보존했다. 빌드·실행파일·push 없음.
- 기존 EditMode 검사 후 이름 기준 배치 메시6/RT35가 남았다. 검사 전 native 소유권 스냅샷이 없어 각 객체의 이번 생성 여부를 안전하게 확정하지 못해 일괄 삭제하지 않았다. 과거 기록0/27을 현재 시작 실측으로 대체하지 않는다. native 잔류/누수 통과 아님.

다음 판단: 세분화 마커를 유지해 원래 지연 재현 시 경로를 확인한다. 정상 반복 비용을 줄이는 다음 구현 후보는 동일 본체 마스크 공유이며, 알파 기준·카메라·크롭 여백·해상도·표시 순서 동등성을 먼저 설계해야 한다. 이 진단에서 원인 해결 또는 공유 구현 완료를 주장하지 않는다.
