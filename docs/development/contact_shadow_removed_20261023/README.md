# 발밑 동적 접촉 그림자 제거 · 2026-10-23

- 복구 기준 HEAD `21857ff`. 변경 전 해당 소스/테스트 추적 diff 없음. 사용자 씬·설정·미추적 파일은 보존한다.
- 사용자가 `Logs/CG_ContactPreview_20261023/before.png` / `without-contact.png` 실제 동일 프레임 확대 비교를 보고 영구 제거 승인. 본체/원본 발 도트/애니메이션/넓은 투사 스프라이트는 유지한다.
- `CoreGuardianPhotoShadow`의 PhotoDynamicContact 생성·공유 접촉 재질 lease·프레임별 접촉 마스크 갱신·불필요한 Camera.main 조회 제거. PhotoCastShadow 위치/스케일/정렬/기존 중첩 억제는 유지. 공개 Present(Camera) 호출 계약 유지. 다른 선택/피격/강화 마스크와 공통 API·셰이더 리소스는 건드리지 않는다.
- 제거 회귀 기대값으로 그림자 테스트를 먼저 변경: [RED](red.json) 10건 중 제거 대상5건 예상 실패. 구현 후 [GREEN](green.json) 10/10, [전용 마스크 재질 미생성/기존 공유재질](materials.json) 2/2 통과. 프리팹으로 플레이어/공격/방어/버프/보스/코어/포탑3종 범위 확인.
- [자체 실제 Play](live.json): frame1421, PhotoDynamicContact0, 플레이어/코어 PhotoCastShadow visible=true. 사람 조작·장시간 전투 검증은 아니다. 종료 Play stopped/compiling false/scene dirty=false, Profiler recording=false/driver=true/background=false 복구. 씬 저장·빌드·실행파일·push 없음.
- 범위 한정 독립 코드 리뷰 actionable findings 없음. diff --check 통과.
- 구현 완료 시점에는 성능 미측정이었고 아래 사용자 요청으로 짧은 후속 비교를 수행했다. 정상30R/저사양60FPS/장시간 잔류 검증은 미완료다.

## 사용자 요청 후속 성능 비교

복구 기준/측정 후 HEAD `0fa0f47`. 게임 코드 추가 변경 없음. 제거 전 동일 진단 코드의 기존60프레임 원본을 재사용하고 제거 후60프레임만 신규 수집했다. 조건당1회·워밍업10프레임, 양쪽 frame 누락0·error null. 짧게 확인하라는 사용자 방향을 유지해 기본3회 반복 대신 예비 비교로 제한했다. 동적 전투의 seed/입력/위상은 완전 고정하지 않았으므로 통계적 확정/정확한 단독 효과 추정은 아니다.

Unity6000.6.2f1/URP17.6/D3D12 Editor, Ryzen5600X/RTX3070/약32GB, 1920x1080·ortho8.5·camera(13.5,7.5,-10), time1/capture0, 주입 적128~129(Attack/Buff, HP100000)·실제 타워15(Multi/Support). 동일 Probe로 진단 초기화→배치→전투 진행. Profiler recording OFF/driver OFF, main-thread ProfilerRecorder. PlayerLoop와 하위 마커는 inclusive·서로 합산하지 않는다. 카메라/Profiler 상태는 현재 조회했으며 과거 원본 환경 기록은 제한적이다.

|지표|제거 전|제거 후|차이|
|---|---:|---:|---:|
|프레임 평균 ms|24.785|22.221|2.564ms 감소, 10.34%|
|최저/최고/평균 FPS|25.88/47.75/40.35|28.55/57.33/45.00|평균 +4.65, 11.54%|
|PhotoShadow.Present 평균 ms|6.480|1.682|4.798ms 감소, 74.04%|
|PhotoShadow.Contact 평균 ms|4.579|0 (호출 제거)|경로 제거 확인|
|CG.Mask.Present 평균 ms|7.329|4.379|2.950ms 감소, 40.25%|
|PlayerLoop 평균 ms|16.823|13.221|3.602ms 감소, 21.41%|
|draw 평균|1750.78|1595.38|155.40 감소, 8.88%|
|SetPass 평균|330.98|187.67|143.32 감소, 43.30%|

원본/프레임별 FPS 재계산 가능 값/마커별 ns·call count: `Logs/CG_ContactPerf_20261023/{before,after,summary}.json`; 재현 `Probe.cs`는 기존 `Logs/CG_QuickCost_20261023/Probe.cs`에서 클래스명/출력 경로만 변경했다. GPU·순수 Render Thread·메모리 peak/잔류 미측정. UnityStats draw/SetPass는 에디터 마지막 렌더 영향을 받을 수 있다. 평균 FPS=표본수×1000/프레임ms합, min/max는 전체60표본에서 계산하며 spike 제외 없음.

판정: 제한된 짧은 표본에서 전체 프레임·CPU·그리기 비용 개선 관측, 목표60FPS 미달. 제거 범위는 직전 사용자 실제 미리보기 승인 및 구현 단계 테스트12/12 결과를 재사용했다. 자체 Play 중 contact0 확인, 종료 stopped/scene clean/compiling false·Profiler OFF/driver true/background false 복구. `user_hashes_{before,after}.json`의 사용자 씬·설정2파일 3/3 동일. 추가 게임 테스트·빌드·실행파일·push 없음.
