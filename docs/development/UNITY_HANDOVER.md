# 코어 가디언 Unity 인수인계

최종 문서 갱신: 2026-10-20 (Asia/Seoul). 현재 사용자 지시·최신 담당 Notion이 우선이다. 최신 재개 확인은 [구현 범위 제안](optimization_scope_review_20261020.md)을 따른다. HEAD226ae5b·기존 변경·최신 Notion과 HUD 소스를 대조했으며 첫 단계로 피해 HUD 행 재사용을 제안했다. 사용자 결정 대기이며 구현 미착수다. 이번 재개에서 새 Unity 조회·Play/검사·게임/음량/설정 변경·빌드/실행·Notion 수정은 없다.

## 지금 재개할 작업
사용자는 현재30R 성장·물량 방향이 더 재미있다고 평가하고 강한 최적화를 요청했다. 제안에 대한 마지막 선택은 **“측정 결과부터 보고 결정”**이다. 사전계측만 완료했고 **최적화 구현은 미착수**다. 이번 기획서 추가를 실제 코드/표현 변경 승인으로 확대하지 않는다.

1. [새 대화 재개 텍스트](resume_optimization_latest_20261020.md)
2. [다음 순서·결정 상태](optimization_next_work_20261020.md)
3. [성능 원본·조건·관측·3후보](performance_baseline_20261020/README.md)
4. [Notion 최적화 계획](https://www.notion.so/3ff389dbfc6881779abdeb5eeeb1cda4), 기존 담당 문서 링크/재조회는 [이번 동기화 기록](handover_optimization_20261020/README.md)

추천은 HUD/표시 객체 재사용·뷰캐시→개체 표현/외곽 강조 렌더 통합→Session 반복 계산/할당 감소다. 목표FPS/기기/해상도/배속 예산과 구현 방법은 미확정이다. 승인된 효과를 끄거나 적 수/능력치/수입/증강을 낮춰 성능을 맞추지 않는다.

## 현재 게임·검증의 범위
- **30R 실험 aab5b86:** 설치60/60/75수정, 공유60→90→120(+30), 플레이어40→65→85→110…, 완료수정60~85/보스90·110·130, 준비20초, 증강5·10·15·20·25R완료/보스처치와독립/30R최종종료. 보스HP500/1600/4500·방어5/12/20. `waves.csv`의health/defense/speed/gold가enemies기본값보다우선. [담당](aggressive_30r_20261020/README.md).
- 종류별 공유강화는 같은 종류의 현재/이후 설치분·철거후유지·새세션초기화·설치비50%환급. 동일 능력치/강화 패널·세로탭/칸 위치/순서, 일반8/보스12칸 체력바·병종별높이를 유지. [표시 담당](shared_towers_hud_20261020/README.md)의 당시 가격40→100→120은 현30R가격으로 대체됐다.
- Edit175/175·집중Play15/15는 기능QA다. 동일Session/고정정책3×대응시드4의증설조건12개중2개30R완주·단일109전체기록재실행일치. 정상1R스모크와 구분하며 사람승률/정상30R/최종난이도·100R40분은 미검증/미달이다. 사용자의 긍정 피드백은 방향 평가로 남긴다.
- **WASD UI 탐색 차단9935523:** 포인터Hover/클릭·캐릭터이동 유지, 집중CLI PlayMode1/1. [원인/검사](hud_navigation_20261020/README.md). 전체Play/실제물리키검사로 확대하지 않는다.
- **기존 실행파일:** `Builds/Windows_20261020_30R/CoreGuardian.exe`, 게임aab5b86/빌드오류0·경고499·실행응답은 당시확인. WASD후속수정 미포함·최적화미적용. 현재프로세스실행여부는 이번문서작업에서 재조회하지 않았다. 빌드/실행은 별도명시지시가 있을 때만 한다.

## 성능 계측을 해석할 때
기준 기록2764c58, 게임소스9935523. 실제씬 적32/64/128·포탑15에서 Editor프레임중앙25.7/56.6/82.4ms·DrawCalls1114/1800/3208. 고정128명 강조켬/끔/켬 MainThread21.55/14.10/21.01ms. HUD16항목의 값1개변경마다16행재생성7.73~14.00ms. 별도Session합성보드 적32~256·포탑32는0.184~1.643ms/틱.
각 실험은 조건이 다르고 적/HP/자원 주입·Editor계측이다. 정상밸런스/standaloneFPS·GPU시간으로 부르거나 서로 직접 차감하지 않는다. 적0명 첫 강조대조·무효할당API의0·단일갱신시간을128명/무할당/p95로 재분류하지 않는다. 원본은 담당README로 연결하고 이 문서에 상세 규칙을 중복하지 않는다.

## 보존·상태·재개 주의
- 사운드B전체연결/지원무음·실제저장음량과6분류, 원본/사진그림자/개체강조B②·선택선/피격/강화/발광/HUD를 유지. [사운드](audio_B_applied_20261019/README.md), [그림자](photo_shadow_applied_20261018/README.md), 기타시각이력은 아래과거인수인계링크를 따른다.
- 마지막 성능계측 종료 확인: Playstopped/컴파일false·전투씬1dirtyfalse·TimeScale1/captureDelta0/runInBackgroundfalse·음량0.195603669·콘솔오류0/경고0. **현재 실시간 상태가 아니므로 다음Unity작업 전에 재조회한다.** 사용자Play/다른검사를 중단·중복하지 않는다.
- 기존 `ProjectSettings/PackageManagerSettings.asset`, `ProjectSettings/ProjectSettings.asset`의SENTIS_ANALYTICS_ENABLED제거 차이·미추적AudioProduction/아트/측정자료·font_tools_vendor접근오류표시는 보존. 개인정보/분석전송설정을 임의로 켜거나 복원하지 않는다.
- 전투씬하나/씬YAML직접수정금지/새빌드·실행파일·GitHubpush자동금지. 로컬Git은 [LOCAL_GIT](LOCAL_GIT.md), 실제HEAD/변경을 직접확인한다.
- 밸런스실측은 [현행 재개](resume_balance_latest_20261019.md)와 담당스킬/Notion/CSV→씬참조→CombatData.Load/Session 대조를 따름. 기존8회·11R계측기는 과거이며 현재정상30R로 둔갑시키거나 무점검재실행하지 않는다.

## 과거 상세 이력
이전 긴 인수인계와 당시수치/검증/시각·사운드참조는 [문서 정리 전 원문](handover_optimization_20261020/previous_UNITY_HANDOVER.md), 이전AGENTS우선순위는 [원문](handover_optimization_20261020/previous_AGENTS.md)에 보존했다. 날짜별 담당문서를 필요할 때만 읽고 과거가격·11R·미구현·옛음량을 현재값으로 가정하지 않는다.
