# 정상 초반 전투·종료 잔류의 제한 관측 · 2026-10-23

## 범위와 판정

- 복구 기준/실제 시작 HEAD `4f2516a` (`7483ff0` 이후 Notion 동기화 문서 커밋). 게임 소스·밸런스·씬·설정 변경 없음. A 보류, B 제한·원본 출력·개별 RT·방어형/보스 fallback 유지.
- 사용자 요청에 따라 **정상 스폰 1배속 최대120초 한 구간**만 관측했다. 2R 완료·3R 전투 중 종료, 30R 전체/장시간/GTX750Ti·RAM4~8GB/안정60FPS 검증은 미완료다. 전후 비교가 없으므로 절감량/율 N/A.
- 실제 씬 Update/Session.Tick 사용, 합법 TryStep으로 (13,14) 이동 → Multi (12,13) 설치 → 획득 Gold만 player damage에 사용 → 준비 즉시 전투 시작, 이후 정지. 적/재화/HP/dt 주입·배속·게임 코드 수정 없음. 사람 자연 조작/청취 QA가 아닌 자동 의사결정 관측이다.
- 기존 접촉 제거12개/누적169개/B72개 검사를 반복하지 않았다. 빌드·실행파일·push·패키지 설치·그래픽 API 변경·씬 저장 없음.

## 환경·도구

Unity6000.6.2f1, Ryzen5600X/RTX3070/32681MB, D3D12 Editor, 1920×1080, ortho8.5, VSync1/targetFrameRate−1, timeScale1/captureDelta0. 기존 auto-tick은 변경하지 않았다. Profiler recording/driver OFF·Deep OFF에서 관측했다. 환경 [environment.json](environment.json), 시작 상태 [state_before.json](state_before.json), CSV 해시 [전](csv_hashes_before.json)/[후](csv_hashes_after.json). CLI beta.14; self-update 조회 ERR_NETWORK여서 최신 여부 재확정 불가, 설치 변경 없음.

프레임은 Editor update에서 엔진 frameCount 변화마다 기록, warm30. Game camera render callback도 별도 계수했다. 5초 간격 리소스 조회·10초 간격 중간 JSON 저장이 다음 프레임에 영향을 줄 수 있으며 스파이크를 제외하지 않았다. 관측은 조건당1회로 제한했고 통계적 개선 판정에 쓰지 않는다.

## 실패와 안전 우회

1. 첫 준비 과정에서 namespace 조회 오타가 있었고 수정했다. Play 진입 뒤 recording 자동 활성화로 진단 시작 guard가 거절됐으며, 준비 시간이 지나 정상 Combat으로 전환돼 fresh-session guard도 거절됐다. 자체 Play만 종료하고 새 정상 세션을 사용했다. 게임 테스트 실패로 세지 않는다.
2. 첫120초 진단은 약104.607초에 2R 완료·3R, 관측22247프레임까지 도구 상태로 확인했다. **종료 처리의 ProfilerRecorder.ToArray() → native CopyTo_Pointer/CopyAndReset에서 Editor 충돌**. [스택 원본](crash_stack.txt), 당시 [진단 소스](Observe_failed.cs). 완료 원본이 없어 FPS/GPU/메모리 판정에 사용하지 않는다. 어느 recorder/엔진 내부 원인이었는지는 확정하지 않았다. 버그 리포트 UI 자체가 원인이라는 판정도 하지 않는다.
3. 사용자 재개 요청 후 재연결·idle/clean·해시 동일을 확인했다. ToArray/CopyTo를 사용하지 않고 Valid/Count 확인 후 LastValue만 읽는 [Observe.cs](Observe.cs)로120초를 다시 관측, 정상 종료·원본 저장했다.
4. 해당120초의 CPU/GC/렌더 카운터가 모두 고정값이었다. capacity1에 명시 옵션을 지정하면서 `WrapAroundWhenCapacityReached`를 누락한 **진단 코드 결함**이다. 해당 비용값 전부 무효이며0을 절감/무할당으로 쓰지 않는다. [Unity 공식 옵션 설명](https://docs.unity.com/en-us/engine/6000.0/script-reference/unity/profiling/profilerrecorderoptions/wraparoundwhencapacityreached)으로 덮어쓰기 동작을 확인했다. `Observe.cs`는 당시 실행 소스 보존용이므로 수정 없이 재사용하지 않는다.
5. 덮어쓰기 옵션을 추가한 [CounterCheck.cs](CounterCheck.cs)로 **15초만** 검증했다. CPU/GC/SetPass의 서로 다른 값과 정상 종료를 확인했다.120초 전체를 또 반복하지 않았다. GPU는 시도하지 않았고 Draw0은 무효다. 엔진 충돌 자체가 수정됐다는 판정은 아니다.

## 유효한120초 프레임 관측

최종 wall120.008초, warm 후24193연속 프레임, gap0·무효0·수집 Error/Exception/Assert0. Game render callback24223회(warm 포함), wall당201.85회; 화면 표시/목표 GPU 처리율과 동치로 보지 않는다. 최종2R 완료·3R Combat, 최대 살아 있는 적23·투사체4·포탑1. [요약/분석](summary.json), [재계산](Analyze.ps1), [원본 ZIP](raw.zip)의 result.json.

|지표|관측값|한계|
|---|---:|---|
|평균/중앙 frame ms|4.955/4.553|초반·현재 고사양 Editor·진단 포함|
|p95/p99/max frame ms|7.092/9.974/347.669|유효 스파이크 보존|
|최저/최고/평균 FPS|2.876/348.943/201.807|평균=표본수×1000/ms합; 안정60 판정 불가|
|1R/2R/3R 평균 FPS|220.84/215.09/167.95|3R은 부분 구간, 라운드간 부하 다름|
|1R/2R/3R peak alive|11/12/23|후반 정상30R 최대부하 아님|

전투 구간만24089프레임 평균4.961ms/201.589FPS, 동일 최대347.669ms. 이전128적·15포탑 주입60프레임 결과와 조건이 달라 비교하지 않는다. 30R 후반 성능·정상 입력·증강/보스/장시간 품질 미검증이다.

## 수정한 카운터15초 확인

[환경](counter_check/environment.json), [요약](counter_check/summary.json), [원본](counter_check/raw.zip).1R 초반·완료0·3542프레임·Error/Exception0. 전체 frame 평균4.203ms, min/max/avg FPS15.731/349.577/237.935, max63.568ms.120초 결과와 합산/전후 비교하지 않는다.

|카운터|평균|표본/판정|
|---|---:|---|
|Main Thread inclusive|4.200ms|3542·서로 다른 값3207개|
|PlayerLoop inclusive|1.358ms|3542·서로 다른 값2900개; Main과 합산 금지|
|GC Allocated In Frame|49054.81B/frame|진단 할당 포함, 개선율 없음|
|SetPass Calls Count|26.04|서로 다른 값31개|
|Draw Calls Count|미측정|3542개 모두0, 유효 렌더 비용으로 쓰지 않음|
|GPU/순수 Render Thread|미측정|지원 flag/카운터 목록만으로 유효 계측을 확정하지 않음|

## 메모리·잔류

[진입 전](resources_before.json), [120초 관측 중·종료 시](raw.zip), [관측 종료 뒤 실제 Play 상태](resources_active_end.json), [자체 Play 종료 후](resources_after.json), [추가15초 자체 Play 종료 후](counter_check/resources_after.json).

|항목|진입 전 Edit|120초 Play 종료 후 Edit|판정|
|---|---:|---:|---|
|대상 mask RT/페이지|0|0|이 자체 세션 종료에서 대상0 확인|
|대상 mask/페이지 메시|0|0|동일|
|선택 마스크·outline/페이지 공유재질|0|0|동일|
|Unity total allocated|904.930MiB|1177.903MiB|+272.973MiB, 감소/전체 잔류 통과 아님|
|Unity total reserved|2095.258MiB|2863.539MiB|예약량 증가 보존|
|Mono used|154.902MiB|182.969MiB|관리 메모리 증가 보존|

120초의 **간격 관측 peak**는 allocated1219.375MiB/Mono302.145MiB/대상RT17.290MiB. 전체 순간 peak가 아니다. B 페이지1장의 API 보고16778272B와 기존 개별 maskRT 동시 존재를 확인했으며 B의 추가 공간 비용이 남는다. 추가15초 종료 후에도 대상RT/메시/재질0이지만 allocated1251221338B/Mono197382144B로 전역 증가가 남았다. 메모리 데이터를 강제 GC/Unload로 정리하지 않았다.

Memory Profiler package 미설치, snapshot/객체 참조 그래프·전체 GPU/프로세스 working set/장시간 반복 잔류는 미검증. WorkingSet64가0을 반환하여 무효 처리했다. 진단 리스트·JSON 직렬화·동적 어셈블리·에디터 캐시가 섞여 **전체 누수 유무를 확정할 수 없다**. 대상 GPU 객체0과 전체 메모리 회복을 혼동하지 않는다.

## 복원·다음 행동

- 최종 Play stopped/scene clean/원래7루트/compiling false/background false/time1/capture0/paused false/Profiler OFF/driver true. [복원 상태](final_state.json). 사용자 씬·설정2개 SHA256 **3/3 동일**, CSV 해시도 동일. 기존 미추적·폰트 도구 접근오류 표시 보존.
- 원본 result.json은 ZIP과 `Logs/CG_NormalObservation_20261023/`에 보존한다. ZIP을 풀지 않아도 Analyze.ps1이 읽어 재계산한다. 카운터 확인 재계산은 `-InputRoot <counter_check 절대경로>`로 실행한다.
- 다음은 현재120초를 반복하지 말고 **남은 정상30R 후반/긴 구간**을 분할 관측한다. 수정 카운터 경로·중간 저장을 재사용하고 ToArray 경로 금지. 본 진단의 단일 Multi/정지 정책은 후반 대표 빌드가 아니므로 정상 획득 재화 기반 행동/증강/입력 정책과 중단·재개 경계를 먼저 고정한다.
- GPU·순수 Render Thread·Memory Profiler snapshot 및 목표기기 해상도/배속 예산은 별도 미완료. 현 환경 수치를 목표기기 통과로 쓰거나 자동 패키지/API 변경·빌드·실행파일 실행으로 확대하지 않는다.
