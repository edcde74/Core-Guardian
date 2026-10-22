# 설치 칸 표시 메시 재사용

2026-10-23 Asia/Seoul. 사용자 승인: 타워 설치 모드의 많은 draw calls를 줄이는 구현·대상 검증 진행. 복구 기준 `257d91d`. A 보류/B 마스크 생성 배칭의 제한·원본 출력·fallback 및 길 메시 적용은 변경하지 않았다.

## 변경

- `CoreGuardianPrototype.ChooseTower`의 칸별 `InstallationTile` GameObject/SpriteRenderer 생성과 Cancel 시 전부 Destroy를 `CoreGuardianPlacementOverlay`의 단일 MeshRenderer로 교체했다. 실제 보드의858칸을 모두 유지한다.
- 위치·원래 sprite vertex/UV·cellSize×(.55/.6) 간격·정렬3·설치 가능/불가능 색(.1,1,.35,.13)/(1,.15,.1,.2)·기존 default Sprite-Lit-Default 재질을 유지한다. 색은 기존 SpriteRenderer처럼 Linear 작업 공간에 맞춰 float vertex color로 옮긴다. 메시의 각 칸 색만 다르고, 표시 수/투명도/판정/카메라 품질을 줄이지 않았다.
- Cancel은 숨기고 다음 선택에서 현재 ground/CanInstall 결과로 메시를 갱신한다. 같은 mesh/material/renderer와 managed 작업 버퍼를 재사용한다. 매 프레임 rebuild나 Update 추가는 없다. 게임 종료에서 자체 리소스만 해제하며 caller sprite/texture/material은 소유하지 않는다.
- 신규 클래스 메모리 관측: mesh351,656B + material3,576B. 취소 후 이 버퍼를 유지하는 절충이 있다. 이전858개 GameObject 전체/엔진 allocator/driver 메모리와 같은 조건의 총량 비교가 아니므로 메모리 절감이라고 주장하지 않는다.

## 대상 검증

- [새 EditMode4개](tests_editmode.json): 희소 칸의 geometry·색·alpha·정렬, cancel/reenter의 리소스 재사용·색 갱신, 소유 리소스 정리, 변환된 parent 아래 원래 world corners 보존. 구현 전4개 모두 missing class로 실패([RED](tests_red.json)), 구현 후4/4.
- [기존 설치 PlayMode4개](tests_playmode.json): 수동 라운드 시작 시 설치창 닫힘, 설치 중 HUD 숨김과 Cancel 복원, 실패 설치 유지/성공 설치 복원, 준비 시간 만료 시 취소. 4/4 통과. 다른 기존72/169개를 반복하거나 누적 전체 통과 수에 합산하지 않았다.
- [실제 씬 화면 비교](pixels.json): 기본줌8.5/확대줌4.25,1920x1080. 기존858개 sprite 방식과 새858칸 mesh의 **차이0픽셀**, 원본 반복 control도0. Camera.Render이므로 Overlay HUD 미포함이며 정상 전체 플레이/모든 시점의 전수 검증은 아니다. before/control/after PNG가 원본이다.
- 읽기 전용 독립 검토: 두 제품 파일과 신규 검사에서 해당 고정 URP2D Lit 범위의 중요한 결함 지적 없음. 검토자는 Unity 조작/파일 수정을 하지 않았다.

## 같은 조건 성능 비교

[환경](environment.json): Ryzen5600X/RTX3070/RAM32681MB/Windows11 10.0.26200/Unity6000.6.2f1/URP2D/D3D12/Linear/1920x1080/줌8.5/VSync1/targetFrameRate−1. 준비 단계, 단일 포탑 ghost 하나, 설치858칸. 사용자의 Play는 시작 시 stopped임을 확인했고 자체 Play만 사용했다.

`PlacementProbe.cs`가 **직전 source의 칸별 생성 코드를 그대로 재현한 legacy grid**와 최종 mesh를 같은 자체 Play에서 번갈아 표시했다. 제품 소스를 교체한 두 실행파일 비교가 아니다. 메인 game.enabled=false로 준비 카운트다운/입력을 고정했고 다른 view/animation/HUD 갱신은 유지했다. baseline는 legacy만 활성, after는 mesh만 활성이다.

조건별 warm10 + sample60 ×3회=180프레임, 합계360개. Time.frameCount 진행을 확인해 같은 프레임 중복 수집을 막았다. 각 회차60개가 연속이며 회차 내 미관측 프레임0([커버리지](frame_coverage.json)). engine ProfilerRecorder의 Main Thread/GC Allocated In Frame 및 UnityStats, native Time.unscaledDeltaTime을 사용했다. 유효 프레임시간 모두 양수. Deep Profile·전체 Profiler 녹화·Frame Debugger OFF. 기존 auto-tick true/16ms 유지, background는 자체 Play 중 true. 메모리 callback/Editor와 계측 자체 부하가 포함되며 baseline→after 순차 실행의 환경 변동 한계가 있다.

|지표|기존 칸별 sprite|단일 mesh|절감량 / 절감률|
|---|---:|---:|---:|
|Draw Calls 중앙값|915|58|857 / 93.661%|
|Draw Calls 최소~최대|915~1596|58~60|서로 다른 극값끼리 절감률 계산 안 함|
|Draw Calls 평균|941.950|58.778|883.172 / 93.760%|
|SetPass 평균|21.517|17.778|3.739 / 17.377%|
|관측 전체 frame 평균 ms|5.245706|3.766183|1.479523 / 28.204%|
|Profiler Main Thread 평균 ms|5.233813|3.762667|1.471146 / 28.108%|
|GC Allocated In Frame 평균 B|16198.333|16193.689|4.644 / 0.029% (개선 확정 안 함)|

|프레임/FPS 통계|기존|mesh|
|---|---:|---:|
|유효 표본|180|180|
|최소/최대 frame ms|3.899001 / 10.889796|3.032802 / 5.722300|
|p95 / p99 ms|8.097499 / 10.509396|4.453004 / 5.029103|
|최저/최고/평균 FPS|91.829 / 256.476 / 190.632|174.755 / 329.728 / 265.521|

평균FPS는 총 관측프레임/총시간이며 증가74.889FPS(+39.284%)다. native frame 지연/Editor 영향이 포함되므로 게임 Update를 고정한 **설치 표시 비교 조건의 부분 성과**다. 정상30R/전투/Player/750Ti의60FPS를 보장하지 않는다. GPU 시간은 미측정이다.

UnityStats는 Editor의 마지막 렌더 통계를 읽는 방식이라 Scene/Game View 등의 영향이 섞일 수 있다. legacy의1596 spike를 제거하지 않았고 평균과 범위를 모두 남겼다. 현재858칸→한 renderer라는 구조적 감소와 두 줌 pixel0도 별도 증거다. 특정 스크린샷의914를 이번 실측915와 동일 표본이라고 쓰지 않는다. 재계산 원본은 [frames.json](frames.json), [회차별](analysis_runs.json), [전체](analysis.json), [완료](completion.json).

## 실패·복원

- 최초 own Play에서 probe 호출 전 준비 시간이 지나 Combat이 되어 설치 시작을 거절했다. 제품 결함이 아니라 진단 준비 실패이며 제품 전투 중 설치 금지 계약이 유지된 결과다. 자체 Play만 재시작해 준비 상태를 먼저 확인하고 game을 고정한 다음 수집했다. phase 검사와 실패 시 game.enabled 복원을 probe에 보완했다. 사용자 Play를 중단하지 않았다.
- 종료 시 자체 Play stopped, scene clean/원래7루트/임시 테스트루트0/compiling false, 자체 overlay mesh/material0. background false/time1/capture0/paused false/Profiler OFF/driver true 복원, auto-tick은 기존 true16 유지([시작 상태](editor_before.json), [최종](final_state.json)). ProfilerRecorder만 별도로 사용해 기존 Profiler 녹화 버퍼를 교체하지 않았다.
- 씬·PackageManagerSettings·TimeManager SHA256 **3/3 동일**([전](user_before.json), [후](user_after.json)). 사용자가 저장한 길 메시 씬과 기존 미커밋/미추적 자료를 보존했다. 새 씬 저장/패키지 설치/그래픽 설정 변경/빌드/실행파일/push 없음.

