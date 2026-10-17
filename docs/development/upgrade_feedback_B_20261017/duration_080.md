# B 스캔 시간 Inspector · 현재 0.8초

2026-10-17 사용자 지시: 기존0.55초가 짧으므로 Inspector에서 조절할 수 있게 하고 우선0.8초 적용.

- 작업 시작 HEAD `1cfecfa`(병행 포탑 요약바 제거 보존). 사용자 미저장 코어 입자 sizeMax0.1을 발견해 Editor API로 저장하고 변경이 해당 한 줄뿐임을 확인, 복구 커밋 `d193770`으로 보존했다.
- Hierarchy **CoreGuardian_Gameplay → Core Guardian Upgrade Feedback → Duration**이 전체 재생 시간(초)이다. 현재 **0.8**, 최소0.05. 컴포넌트를 기존 전투 씬에 Editor API로 저장했고 Prototype은 기존 컴포넌트를 재사용해 중복 생성하지 않는다.
- 전체 길이에 맞춰 본체 스캔 이동과 시작/끝 페이드도 같은 비율로 늘린다. 단순히 효과가 사라진 뒤 빈 대기시간만 추가하지 않는다. 강화 성공/실패·연속 재시작·본체 합집합·원본/선택 아웃라인/발광·코어 입자 규칙은 유지한다.
- Play 중 Inspector 값 변경도 반영한다. 영구 저장하려면 마음에 든 값을 Stop 후 Edit 모드에 입력하고 씬을 저장한다. Unity 일반 Play 설정 복귀 동작을 따른다.
- [설정 미노출 실패](duration-red.json) 확인 후 새 지속시간 테스트에서0.15초 종료/1.1초 설정은0.85초에도 실제 셰이더 불투명도 유지/이후 종료를 확인했다. 기존 구매 테스트도 기본0.8초가0.65초 이후 유지·만료하는지 확인하도록 갱신했다. 최종 전체 [EditMode118/118](duration-editmode.json)·[PlayMode28/28 completed](duration-playmode.json), 별도 읽기 전용 리뷰 actionable finding 없음.
- 종료 Playfalse/pausedfalse/compilingfalse/failedfalse·씬1/dirtyfalse·TimeScale1/captureDelta0, 설정0.8초/컴포넌트1개/사용자 코어 입자max0.1 유지. 현재 콘솔 오류0/경고1은 Unity AI Toolkit Account API30초 접근 지연이다. 새0.8초 GIF/사람 플레이·밸런스·성능 검사는 하지 않았다. 기존 applied_B.gif는 최초0.55초 당시 기록이다.
- 빌드/실행파일/push 없음. 관련 소스/씬/검사/인수인계만 로컬 기록하며 다른 작업/미추적자료는 보존한다.
