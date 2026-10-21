# 누적 런타임 최적화 검증 결과

2026-10-21 시작, 2026-10-22 Asia/Seoul 완료. 사용자 요청으로 Play·자동 검사·성능 측정 보류 해제. 복구 기준 `51fb785`, 누적 비교 기준 `5914347`(본체 마스크 공유 이후, 후속 재사용 적용 전). 현재 버전은 `51fb785` + 아래 HUD 오류 수정이다. **CPU/할당 부분 개선 확인, 평균 FPS 개선 없음. 전체 성능 통과 판정은 하지 않는다.**

## 기능 QA와 수정

|검사|최종 결과|원본|
|---|---:|---|
|Session / AnimationTiming / ThirtyRound / Augmentation|75 / 3 / 5 / 44 통과|[Session](session_tests.json), [Timing](animation_timing.json), [30R](thirty_round.json), [증강](augmentation.json)|
|Animation + 새 보스 HUD 회귀|7/7|[최종](animation_play_passed.json), [최초 실패](animation_play_initial_failed.json)|
|CombatAudio / SelectedAudio|12 / 7 통과|[전투](combat_audio.json), [선택](selected_audio.json)|
|HitFlash / 카드 HUD / 설치 HUD|1 / 2 / 4 통과|[피격](hit_flash_fixed.json), [카드](hud_interaction_fixed.json), [설치](placement_hud_fixed.json)|
|TowerGlow / SingleTowerAim|1 / 1 통과|[발광](tower_glow.json), [조준](single_tower_aim_fixed.json)|
|Runtime(30R·재시작·배속·이동 포함)|7/7|[최종](runtime_fixed.json), [최초](runtime.json)|

EditMode127 + PlayMode42 = **169개 최종 통과**. 정상 입력으로 30라운드를 자연 플레이한 결과나 밸런스 통과를 뜻하지 않는다. Runtime 검사는 주입/수동 Tick을 사용하는 기능 QA다.

- 제품 오류: `FirstOrDefault`로 보스를 못 찾으면 값 형식 `EnemyHandle`은 기본값인데 HUD가 `boss==null`로 검사해 Health 읽기에서 `Expired enemy handle` 발생. `!boss.IsValid`로 수정했다. 기존 Animation6개가 같은 로그로 실패했고, 보스 없음→등장→마지막 보스 회수 시 HUD가 비는 회귀를 추가해7/7 확인했다.
- 테스트 준비/호출 이관: 피격의 고정 경로77, 설치의 과거 유효/무효 좌표, Runtime의 월드X8.7/간격0.6은 현재 맵과 달랐다. 현재 경로/World/타일맵으로 기대값을 구했다. 조준 검사의 reflection 인자는 새 `ProjectileSnapshot.Handle`로 이관했다. 카드 공개 검사는 짧은 고정 대기 대신 최대3초 안에 Busy 해제를 기다리되 최종 카드 내용/클릭 규칙 검증은 유지했다. 제품 전투 수치/경로/아트/애니메이션 시간은 변경하지 않았다.
- 최초 실패는 [피격](hit_flash.json), [카드](hud_interaction.json), [설치](placement_hud.json), [조준](single_tower_aim.json), [Runtime](runtime.json)에 보존했다.
- 공식 runner가 완료 뒤 다음 필터를0건으로 반환하는 상태가 있었다. 실제 tree에는 검사가 존재했다. 정지 상태에서 스크립트 reload로 완료 상태를 비운 뒤 실행해 실제 건수를 확인했다. reload는 HTTP 응답 뒤 예약하는 방법으로 응답 손실을 피했다.0건은 통과로 집계하지 않았다. PlayMode 동기 실행 거부 기록은 `animation_play_fixed.json`이며 실제 최종 통과 원본과 구분한다.
- 마지막 컴파일/콘솔 조회는 오류0. 독립 읽기 전용 검토에서 제품/검사 추가 결함 없음. 계측의 이벤트 종료 구독 해제를 보완했다.

## 고정 비교 조건

Ryzen5 5600X / RTX3070 / RAM32681MB / Unity6000.6.2f1 Editor, 전투씬 하나,1920×1080, VSync1/targetFrameRate-1. CPU/Rendering/Memory/UI/Audio 모듈, Deep·할당 스택 OFF. 같은 저장된 씬/CSV/아트/품질 설정을 사용했다. [전 환경](baseline/environment.json), [후 환경](current/environment.json).

[RuntimeProbe.cs](RuntimeProbe.cs)를 공식 `run_script`로 메모리에서 컴파일했다. 게임 Update만 자체 Play 안에서 잠시 끄고 진단 컴포넌트의 실제 LateUpdate에서 Tick(1/60초)와 SynchronizeViews를 구분해 native Profiler 마커로 측정했다. captureDeltaTime도1/60이며 **FPS는 이 고정 dt의 역수가 아니라 native `frameTimeMs`의 합계로 계산**한다. 진단 리플레이는 확률 증강/랜덤 선택을 호출하지 않아 시드 의존 입력이 없다.

128적(Attack112/Buff16), HP100000/Gold0/CoreDamage0, 실제 포탑15개(요청32개 중 예산으로15개 설치), 30단계마다8개 치명 피해→회수→재생성. 워밍업60단계 + 관측180단계 ×3회/조건. 적 수/포탑 수는540개 관측 모두128/15다. 활성 수나 효과 수/수명 제한으로 비용을 줄이지 않았다. Tick/View 마커 밖의 교체·생성 비용은 해당 마커 절감으로 주장하지 않는다.

원본 native frame은 회차별180/181/181개로 **각 조건542개**다.2·3회차 시작 경계에는 Tick 없는 프레임1개씩이 추가로 들어왔다. 무효 시간/누락 프레임0, 유효 스파이크와 경계 프레임을 전체 FPS에서 제거하지 않았다. 마커별 평균은 Tick이 실제 존재하는180×3=540개를 썼다. Native `.data`는 `Logs/CG_RuntimeValidation_20261022/{baseline,current}/run_{0,1,2}.data`에 보존했고, 분석에 사용한 프레임별 원본 JSON은 각 조건 폴더에 있다. JSON은 값과 표본을 유지하고 Git 기록용 줄바꿈만LF로 정규화했으며 native 바이너리는 그대로다. 재분석은 [Analyze.ps1](Analyze.ps1), 계산 원본은 [analysis.json](analysis.json).

## 비용 결과

아래 절감량은 전−후, 절감률은 (전−후)/전이다. 음수는 악화다. B는 byte이다.

|지표/통계|전|후|절감량|절감률|
|---|---:|---:|---:|---:|
|전체 native frame 평균 ms|33.1463|33.3048|−0.1585|−0.478%|
|전체 native frame p95 ms|45.4019|40.7803|4.6216|10.179%|
|Session Tick 평균 ms|0.71033|0.43603|0.27430|38.616%|
|SynchronizeViews 평균 ms|2.05144|1.85452|0.19692|9.599%|
|Main Thread GC.Alloc 평균 B/native frame|307,728.10|118,462.45|189,265.66|61.504%|
|Tick 하위 GC.Alloc 평균 B/tick|79,658.58|1,788.84|77,869.74|97.754%|
|View 하위 GC.Alloc 평균 B/호출|48,583.40|2,009.60|46,573.80|95.864%|
|Draw Calls 관측 평균|2,086.837|2,089.130|−2.293|−0.110%|
|SetPass 관측 평균|459.237|459.561|−0.324|−0.071%|

|FPS 반복|전 최소 / 최대 / 평균|후 최소 / 최대 / 평균|
|---|---|---|
|0|17.905 / 36.923 / 30.332|18.630 / 35.466 / 30.320|
|1|14.182 / 36.590 / 30.046|21.358 / 35.120 / 30.510|
|2|16.143 / 36.854 / 30.132|16.615 / 35.190 / 29.278|
|전체542개|14.182 / 36.923 / **30.169**|16.615 / 35.466 / **30.026**|

평균 FPS 증감은 **−0.144 FPS, −0.476%**. 반복 변동폭이 겹치므로 FPS 개선으로 판정하지 않는다. 비용 절감은 위 누적 묶음의 결과이며 모델 풀링 한 기법의 성과로 나누지 않는다. Native frame에는 대기와 계측 비용도 포함된다. GPU/Render Thread 상세 병목과 GC 수집 정지시간/횟수는 이번 축약 추출에서 분석하지 않았다.0이라고 가정하지 않는다.

## 동등성과 풀/메모리

회차별264개 발사/피해 이벤트의 기록 순서와 적 ID/병종/HP/위치/남은 경로·탄환 상태·골드/수정/코어·단계·피해 집계가 **3회 모두 정확히 일치**했다. 이벤트 transcript는 ID/대상ID/출처/피해를 비교한 범위이며 모든 이벤트 필드/모든 정상 전투를 망라하지 않는다. [전 outcome0](baseline/run_0_outcome.json), [후 outcome0](current/run_0_outcome.json)과1/2회차 같은 이름 파일, [비교 결과](analysis.json)를 따른다.

후3회차 모델 telemetry: Attack 활성112/대기0/피크112/누적생성272/재사용211, Buff16/0/16/16/53, Projectile0/36/36/36/285. 재시작 때 일반 병종 보관 상한32 때문에128개 전부를 보관하지 않으며 다음 회차 확장이 발생한다. 생성0이나 메모리 무비용 풀이라고 표시하지 않는다. 퇴장/재시작/Dispose·오래된 핸들은 Session75개와 Runtime7개에서 해당 계약을 확인했다. 화면의 모든 풀 종류별 누적 생성/잔류를 계측한 것은 아니다.

Editor monoUsed 평균은164.360→175.218MiB로 오히려 증가했다. 전체 할당 메모리 평균은1585.23→1549.86MiB 근처지만, Profiler 버퍼·에디터/리소스 캐시와 측정 순서(후→전)가 섞인다. **최종 잔류 메모리 절감이나 GPU VRAM 개선으로 해석하지 않는다.** 프레임별 최소/최대/평균은 analysis.json에 보존했다. Memory Profiler/장시간 반복/씬 종료 잔류 검사는 별도 미완료다.

## 실패·검토 한계와 복원

- 초기 probe v1은 Setup마다 관측 이벤트 구독이 누적되는 것을 발견해 사용하지 않았다. `Logs/CG_RuntimeValidation_20261022/probe_v1_current`에 별도 보존했다. 최종 비교는 Begin에서 한 번만 구독한 버전이다. 이후 Finish의 구독 해제도 보완했다. 이 종료 정리 추가 전 current 캡처와 추가 후 baseline 캡처는 측정 루프가 같고, 각 조건 사이 own Play를 종료해 잔존 구독을 분리했다.
- 초기 `eval_file`은 전체 클래스 소스를 statements로 해석해 진단 컴파일에 실패했다. 제품 컴파일 실패가 아니며 공식 `run_script`로 변경했다. Profiler driver=true이면 자체 Play 진입 때 자동 녹화가 켜짐을 Play 정지 시 false로 돌아오는 것으로 확인했다. 자체 비교 동안 driver를false로 두고 probe 구간만 녹화했다. 종료 후 원래true 복원했다.
- 스크린 캡처 명령이 상대 경로를 Assets 아래에 저장했다. 생성한 단일 캡처와meta만 확인한 뒤 [현재 관측 화면](current_observed.png)을 Assets 밖으로 옮기고 자체 생성 폴더를 정리했다. 이 화면은 비교 위상이 아니므로 픽셀 동등성 근거로 사용하지 않는다.
- 사용자가 승인한 **원본 백업 후 현재 씬 저장**을 실행했다. 최초 원본SHA256 `7D3332D5...C1FBCB`; 에디터 재시작으로 Temp가 사라진 후 Git51fb785에서 같은 바이트/해시의 원본을 `Logs/.../scene_original_disk_recovered.bytes`에 다시 확보했다. 저장된 사용자 씬SHA256 `050AC151...E2B4F`는 측정 전후 동일하며 `scene_saved.bytes`에도 보존했다. 이 씬 변경은 사용자 소유로 커밋에 포함하지 않는다.
- 비교 소스25개를 정확한 바이트/해시로 Logs에 보존하고 [해시 manifest](source_manifest.json), [일괄 교체/복원 스크립트](SwapSources.ps1)로 검증했다. Unity 정지·Profiler정지 상태에서 import/reload lock을 잡아 기준 코드를 임시 적용한 뒤 현재 코드를 복원했다. 이후 Runtime 테스트 기대값 추가 수정만 별도 차이다.
- 최종 상태/설정/소스 확인은 [final_state.json](final_state.json)을 따른다. 사용자 미추적·설정·폰트 도구 접근오류 표시는 보존한다. 빌드·실행파일 실행·push·도구 업데이트 없음.

남은 범위: 정상30R/장시간의 성능과 조작·실제 청취, 모든 재등장 효과/픽셀 동등성, GPU/VRAM·Memory Profiler 및 최종 잔류, 모델 풀링 직전 버전 단독 효과, 목표기기/FPS 예산. 기존 본체 공유 적용 제한과 미확정 보스9건은 [해당 정본](../shared_body_mask_20261021/RESULTS.md)을 따른다. 이번 결과로 그 제한을 해제하지 않는다.
