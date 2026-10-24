# 작업별 읽기 경로

요청에 맞는 행만 읽는다. 여러 분야를 실제로 바꾸면 해당 행을 함께 적용한다. 링크는 탐색 출발점이며 현재 상태는 담당 문서·최신 기획서·실제 Git으로 확인한다.

|작업|먼저 읽을 문서|기존 스킬/검증|
|---|---|---|
|게임 성능|[최적화 규칙](OPTIMIZATION_RULES.md), [작업 주의점·실패 방지](OPTIMIZATION_PITFALLS.md), 인수인계가 가리키는 최신 실험|공식 Unity 해당 스킬, [대상 QA](TARGETED_QA.md)|
|전투/경제/라운드/증강|[밸런스 주의](task_rules/BALANCE.md)|core-guardian-balance + headless-game-balance|
|몬스터/보행/방향/그림자/체력바|[아트 보호](task_rules/ART.md)|공식 Unity 해당 스킬; 생성은 imagegen|
|HUD/입력/포탑 발광|[HUD 보호](task_rules/HUD.md)|core-guardian-hud-ux, 공식 Unity UI 해당 스킬|
|사운드|[현재 사운드](audio_B_applied_20261019/README.md)|필요한 audio/Unity 스킬만|
|문서/도구/재개 정리|[작업 흐름](WORKFLOW.md)|기존 인수인계·로컬 Git 재사용; Unity 실행 불필요|

Unity 테스트·계측을 실행할 때는 [대상 QA](TARGETED_QA.md)를 추가로 읽는다. 모든 문서/스킬의 링크를 재귀적으로 읽지 않는다.
