# 강조 CPU 중복 처리 축소 — 부분 개선, GC 확인 미완료

2026-10-23. 사용자 승인: 강조 표시 유지·중복 순회/조회 축소, 관련 검사·픽셀 비교·적128/타워15 전후 각90프레임. 복구 HEAD `1de8ced`, 미커밋. **소스에는 강조 CPU 변경이 반영돼 있으나 전체 성능 통과 판정은 보류**다. 기존 네이티브 본체 배칭은 계속 OFF, A/B 제한·SpriteAtlas 패킹 보류·원본 출력 유지.

## 변경과 범위

- 강조에서 이미 검증한 본체 렌더러 목록을 같은 호출의 윤곽 마스크에 전달한다. 구조 변경 검사를 두 소비자가 반복하지 않는다. 독립 선택/HUD/강화 마스크는 기존 참조 검증 경로를 유지한다.
- 활성 부품만 목록에 담는 원본 규칙 유지. 마스크는 enabled/sprite/alpha/active 필터를 그대로 적용한다. 마스크 공유·출력 순서·원본 아트·밝기·윤곽선 파라미터는 변경하지 않았다.
- 렌더 전에 색상 사전을 미리 조회하는 반복을 제거하고, 실제 색상을 적용한 항목만 복원 목록으로 추적한다. 새 외부 tint 보존·퇴장/비활성화 복원 유지. 밝기 Clamp는 부품별 대신 카메라당 계산한다.
- 동일 계측을 위해 CG.Emphasis.Present / CG.Emphasis.Tint 마커를 원본 측정 전 추가하고 양쪽 유지했다. 전자는 PresentTargets, 후자는 BeginCameraRendering 범위다. **EndCameraRendering의 복원 등 전체 수명주기 비용은 이 두 마커 합에 포함되지 않는다.** CG.Mask.Present는 Present에 중첩되므로 합산 금지.

## 검증

[RED 요약](red_summary.json): 신규 공유 참조 진입점 부재로4개 중1개 예상 실패 후 구현. [최종 강조4/4](tests_emphasis_final.json), [윤곽3/3](tests_outline.json) 통과. 활성/비활성·렌더러 enabled 변경·재부모화, 원본 tint·비누적·복원·선택 정렬·독립 마스크 경로를 확인했다. 첫 구현 테스트도 [보존](tests_emphasis.json).

[동일 개체 픽셀 비교](same_actor_quality.json): 좌우×4위상×단일/겹침 **16쌍 RGBA 차이0**, 256×256/ortho2. [Quality.cs](Quality.cs)는 변경 전 강조 클래스 복사본과 변경 후 클래스를 **같은 원본 SpriteRenderer 개체**에서 순차 렌더한다. 두 복사본은 초기화/해제에 필요한 내부 접근만 reflection으로 바꾸며 아트/색/마스크 조건은 유지한다. 원시 RGBA와 전후 코드 보존. 모든 방향·상태·전체 HUD·정상30R·사람 아트 승인까지 뜻하지 않는다.

별도 재로딩 전후 저장 화면 비교에서는 겹침 사례에 차이가 나 [2쌍/1실패](candidate_quality.json)를 보존했다. 개체가 새로 생성돼 동률 정렬을 통제하지 못하는 비교여서 원인/회귀를 확정하지 않았다. 이를 숨기거나 픽셀 허용오차로 통과시키지 않고 같은 원본 개체 비교로 교정했다. [기존 진단 코드](Quality_separate_capture_failed.cs), [원본 캡처 기록](baseline_quality.json).

## 실제 계측

Unity6000.6.2f1 Windows Editor, Ryzen5600X/RTX3070/RAM32681MB,1920×1080/ortho8.5,VSync1/targetFrameRate-1,timeScale1·고정 simulation dt1/60. Attack112+Buff16/타워15, 양쪽90프레임 모두 수량 동일·누락/무효시간0. 포탄도 min0/max36/평균6.733 동일. 조건별 warm15/sample90 각1회. FPS는 실제 wall interval에서 계산하며 고정 dt 역수를 쓰지 않는다.

|지표/평균|원본|최종 수정|절감량|절감률|변동/한계|
|---|---:|---:|---:|---:|---|
|강조 준비 ms|5.7011|5.0171|0.6840|12.00%|각90개, 단회|
|카메라 색상 적용 ms|0.7975|0.7731|0.0244|3.06%|작은 차이, 개선 확정 아님|
|위 두 비중첩 구간 합 ms|6.4987|5.7903|0.7084|10.90%|전체 강조 수명주기 합 아님|
|참조 검사 ms (전체 소비자)|1.1948|0.9283|0.2665|22.30%|준비 등 내부에 중첩, 합산 금지|
|마스크 Present ms (전체 소비자)|4.5156|4.1224|0.3933|8.71%|준비에 중첩, 합산 금지|
|Main Thread ms|24.7498|22.4353|2.3144|9.35%|대기/변동 포함, 인과 효과 확정 아님|
|wall frame ms|24.7538|22.4411|2.3128|9.34%|단회 Editor 변동 포함|
|Native Draw (Standard+SRP)|1581.16|1593.88|-12.72|-0.80%|원본1554~2137/수정1555~1900, Draw 절감 작업 아님|
|SetPass|186.33|194.98|-8.64|-4.64%|증가 관측, 인과 미확정|
|GC B/frame 평균|263368.42|350920.10|-87551.68|-33.24%|스파이크 포함, 악화 관측|
|GC B/frame 중앙값|127104.5|126910.0|194.5|0.15%|평균과 방향 다름, GC 개선 주장하지 않음|

|프레임/FPS 통계|원본|최종 수정|
|---|---:|---:|
|프레임 ms 중앙값|21.956|19.379|
|프레임 ms p95 nearest-rank|47.126|36.372|
|프레임 ms 최대|108.874|111.972|
|최저 FPS|9.18|8.93|
|최고 FPS|58.98|61.46|
|평균 FPS (총프레임/총시간)|40.40|44.56|

평균 FPS +4.16/+10.31%는 단회 관측이며 개선 확정/목표기기60FPS 검증이 아니다. p95는90개 안의 기술통계, p99는 표본 부족으로N/A. 유효 스파이크를 제거하지 않았다. GC 평균은87.55KB/frame 증가했고 최대3.94MB→5.67MB, 중앙값은 거의 같아 지속 할당 회귀/변경 코드 원인을 확정하지 못했다. **GC/SetPass 악화를 포함한 전체 성능 통과를 선언하지 않는다.** GPU/순수 Render Thread·GC 발생 횟수/정지·peak/잔류 메모리·장기 플레이는 미측정.

원본 [baseline.json](baseline.json), 최종 [candidate_active.json](candidate_active.json), [분석](summary_final.json)/[코드](analyze_final.py), [환경](candidate_active_benchmark_environment.json), [측정 코드](Benchmark.cs). capacity1+wrap+LastValue만 사용, recording/Deep/FrameDebugger/할당스택/호출트리 추출 미사용. 진단 할당 포함. 승인된 짧은 범위로 기본3회 반복은 미수행. 이전 다른 실험 FPS와 직접 비교하지 않는다.

## 첫 후보 실패·도구 한계

최초 후보는 비활성 방향 부품까지 렌더 직전 순회해 준비5.701→5.016ms 감소가 색상0.798→1.389ms 증가로 상쇄됐다. [첫 후보](candidate.json)/[분석](summary.json)/[소스](CoreGuardianObjectEmphasis_first_candidate.cs) 보존. 이를 확인한 뒤 **원본의 활성 부품 목록 규칙을 복구한 수정본만** 추가90프레임 측정했다. 최종 표는 baseline과 수정본이며 좋은 표본을 골라 평균 낸 결과가 아니다. 실행 횟수는 원본1·첫 후보1·수정본1, 각warm15/sample90이다.

CLI self-update 확인은 네트워크 오류로 실패했고 재시도/설치하지 않았다. 기존 정상 MCP 사용. 최초 파일 준비의 Python 문자 인코딩 오류와 동일개체 진단의 using/internal 접근 컴파일 오류는 원인을 수정한 뒤 진행했다. 엔진 응답 지연·Deep 추출은 없었다. [읽기 전용 코드 검토](REVIEW.md)에서 좁은 변경 및 활성 목록 수정에 새 지적 없음.

## 종료와 미완료

[자동 종료](candidate_active_benchmark_completion.json)/[복원](candidate_active_benchmark_restored.json): error null,90개 완료,Play stopped/compiling false/Deep·recording OFF/driver true/background false/captureDeltaTime0/timeScale1/native batch OFF/scene clean. [보호5파일](protected_after.json) 모두 변경 전후 해시 동일. [소스 해시](source_hashes.json), 수정 전 두 클래스 소스 보존. 사용자 파일·설정·미추적 보존. 씬 저장·빌드·실행파일·push·커밋 없음.

미완료는 GC 스파이크 원인과 반복/목표기기 검증이다. 다음 제안은 GC 증가를 할당 출처별로 좁히는 것이며, 추가 Play/프로파일링·다른 시스템·네이티브 배칭 활성화는 새 승인 없이 진행하지 않는다.
