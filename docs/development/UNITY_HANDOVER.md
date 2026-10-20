# 코어 가디언 Unity 인수인계

최종 갱신: 2026-10-21 Asia/Seoul. 현재 사용자 지시와 최신 담당 기획서가 우선이다.

## 현재 최적화 상태
사용자가 해결방안 보고 후 **“첫 번째거부터 진행”**을 승인했다. 복구 기준 HEAD `a135d9d`에서 본체 참조·배열/정렬과 Render Texture 용량 재사용을 적용했다. **최적화 미착수라는 이전 문구는 이력이다.** 변경/원본/실험/한계 정본은 [1차 최적화 기록](mask_reuse_20261020/README.md), 수치 표는 [전후 결과](mask_reuse_20261020/RESULTS.md)다. 모든 후속 작업 전 [공통 규칙](OPTIMIZATION_RULES.md)을 읽는다.

적128/실제 포탑15의 Editor 주입 스트레스, 전후 각3회×100프레임에서 그림자 LateUpdate 중앙15.355→11.339ms, 강조13.519→10.225ms, GC.Alloc 중앙517,978→217,966B/frame. RT 교체 총횟수/관측프레임221.047→1.420회/frame. 묶음 최소/최고/평균 FPS는3.924/24.132/9.486→9.987/29.932/25.302다. 효과 제한 없이 줄인 묶음 변화이며 자유실행 dt/위상 차이가 있어 결정론적 전투·순수 GPU·standalone FPS·목표 예산 통과로 확대하지 않는다.

Edit176/176·선택/접기 Play4/4·원본 코드와 고립 렌더 픽셀576건 불일치0. 실제씬128적/15포탑 프레임 진행·HUD 포함 화면도 확인했다. 추가 보행 Play 테스트는 목록에6건이나 실행 도구0건으로 미완료이며 통과로 세지 않는다. RT 최대 용량 유지의 장기 메모리/해상도 절충도 남는다. 검사별 조건/한계·원시 ZIP과 SHA manifest는 담당 기록을 따른다.

**후속 미적용:** 같은 본체 마스크 공유/아틀라스/명령 통합, HUD·전투 표시 풀링, Session 탐색/할당 감소. 이번 승인은1차 범위뿐이다. 현재30R 성장·물량/밸런스·원본/방향·그림자·강조·효과 수/수명·사운드·HUD를 보존한다. 목표기기/FPS/배속 예산은 미확정이다.

최종 자체 Play 종료/컴파일false/씬1 dirtyfalse, TimeScale1/captureDelta0/backgroundfalse, Profiler/Deep/할당스택/FrameDebugger=false. Edit AudioListener1·저장음량0.195603669. 다음 Unity 작업 전 실시간 상태를 다시 조회한다. 사용자 Play/다른 검사를 중단·중복하지 않는다. 새 빌드/실행파일 실행/패키지 설치/GitHub push는 없다. 기존 사용자 설정/미추적/폰트 접근오류 표시는 보존한다.

재개: [새 대화 텍스트](resume_optimization_latest_20261020.md) → [다음 순서](optimization_next_work_20261020.md) → 위1차 기록. [기존 Profiler 병목 조사](profiler_investigation_20261020/README.md)와 [4항목 해결방안](optimization_solutions_research_20261020.md)은 원인/대안 조사다. [Notion 최적화 계획](https://www.notion.so/3ff389dbfc6881779abdeb5eeeb1cda4)은 재조회했으나 과거 시작 전 상태이며 로컬 구현 결과는 아직 동기화하지 않았다. 초기 HUD우선 추천을 최신 결정으로 사용하지 않는다.

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
