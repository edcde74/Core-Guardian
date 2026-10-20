# 코어 가디언 인수인계

갱신: 2026-10-21 Asia/Seoul. 현재 사용자 지시·최신 담당 기획서 우선. 작업별 정본/보호 규칙은 [읽기 색인](WORK_ROUTING.md)을 따른다.

## 현재 작업
사용자는 SetPass/Draw Calls 감소를 우선 과제로 지정한 뒤 구현 방식 질문에는 **진단 결과만 정리**를 선택했다. 시작 `1ea021a`,128적/15포탑100프레임×3회에서 Draw Calls중앙3,208/SetPass중앙1,608을 확인했다. Frame Debugger에서 마스크 clear274/파츠그리기1,304 이벤트를 확인했으나 Stats와 같은 지표로 합산하지 않는다. 제품 변경/새 최적화는 없으며 [원본·후보·한계·복원](draw_calls_20261021/README.md)을 따른다.

사용자 승인으로 잔여 자동 검증을 진행했다. 보행 PlayMode6/6, 효과 출력 픽셀3,456건 차이0, 실제 씬128적/15포탑600프레임×3회에서 초기화 후RT274→3개/305,992bytes를 확인했다. 테스트의 낡은 준비 조건만 수정했고 제품 소스/셰이더/씬/밸런스 변경은 없다. 보스 본체까지 합친9건 차이는 원본끼리 대조군에서도 재현돼 판정 미확정이다. [실패·보완·원본·한계·복원](mask_validation_20261021/README.md).

문서 경량화와 단독 **인수인계** 단축 지시는 적용 상태를 유지한다. [경량화 기록](context_efficiency_20261021/README.md), [단축 지시 등록/백업](context_efficiency_20261021/handoff_alias.md). 자체 Play/검사만 수행·종료했고 빌드·실행파일·push는 하지 않았다.

## 게임 최신 상태
1차 최적화 코드 `494381f`, 설정 후속 기록 `d5d1c63`: 본체 참조·배열/정렬·RT 용량 재사용 적용. [구현/검증/한계](mask_reuse_20261020/README.md), [전후 수치](mask_reuse_20261020/RESULTS.md). Editor 주입 스트레스이며 standalone/정상30R/사람 체감 승인이 아니다. 현재30R 성장/물량은 유지, 가격/증강/공유 강화는 [담당 기록](aggressive_30r_20261020/README.md)을 따른다. 기존 실행파일은 이 최적화/WASD 수정 미포함.

## 미완료와 다음 행동
- 마스크 공유/아틀라스·표시/HUD 풀링·Session 탐색 감소는 미적용. 첫 항목 승인으로 후속을 확대하지 않는다. 목표 FPS/기기/배속 예산도 미확정이다.
- 보행 실행0건은 이번에 재현되지 않았다. 줌/해상도/겹침의 효과 출력은 검사했으나 보스 본체 전체 합성 비교·수십분/정상30R·GPU VRAM/메모리 스냅샷은 남는다. 제한된3회 관측을 장시간/전체 성능 통과로 확대하지 않는다.
- 종료 시 전투씬1/dirty=false·Play정지·compile/update=false·Profiler/Deep/FrameDebugger=false를 확인했다. AnalyticsSettings=false, 설정3파일 해시는 시작과 일치한다. Inference의 런타임 SENTIS 재추가 한계와 기존 TimeManager 차이는 남는다. [설정 한계](mask_reuse_20261020/settings_followup.json).
- 다음 구현 재개 시 SetPass/Draw Calls 감소를 먼저 검토한다. 같은texture/opacity 파츠 메시 묶기는 제안만 했으며 현재 사용자가 진단 정리로 범위를 제한했으므로 임의 착수하지 않는다. 이후 동일 본체 마스크 공유/보스 전체 합성의 불확실성·실게임 중첩·장기 검증을 이어가며 아틀라스/HUD/Session은 별도 범위다. [과거 범위 점검](mask_reuse_20261020/resume_review_20261021.md)은 실제 검증 전 기록이다. 실행 직전 사용 중 상태와 설정 diff를 재확인한다.
