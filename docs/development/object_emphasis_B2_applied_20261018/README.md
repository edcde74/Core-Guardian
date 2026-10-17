# 개체 강조 B② 적용 · Inspector 조절

2026-10-18 (Asia/Seoul). 사용자가 B 비교의②(본체 밝기+어두운1px윤곽)를 선택하고 두께·색을 Inspector에서 조절하도록 요청했다. 복구 기준 `ced8215`. 배경 완화 A는 계속 원복 상태다.

## 사용 위치와 현재값

`CoreGuardian_Gameplay` → `CoreGuardianObjectEmphasis`.

- Outline Width Pixels: **1**, 화면 픽셀 기준0~8. 0이면 상시 윤곽만 숨긴다.
- Outline Color: **RGBA(.06,.065,.075,.90)**. 기존 초록2px 선택선과 별도다.
- Body Brightness: **1.15**, 1이면 원래 본체 밝기. Alpha Threshold는.1.
- Edit/Play에서 값을 조절할 수 있고 새 몬스터/설치 포탑에도 공통 적용된다. 저장된 Edit 값은 세션 재시작에도 유지되며 Unity 기본 동작상 Play 중 변경은 Play 종료 시 되돌아간다.

## 구현 범위

- `CoreGuardianObjectEmphasis`를 씬 컨트롤러에 저장했다. `CoreGuardianPrototype.GetCombatBodyTargets`는 현재 실제 플레이어/포탑/몬스터 뷰를 수집하며 코어도 별도 참조한다. 전투 규칙/이벤트/수치 변경 없음.
- 본체 `2D Sprite` 아래 현재 활성 스프라이트 조각의 RGB만 원본 틴트에 비례해 보강하며 알파를 유지한다. 매 프레임 누적 곱셈을 방지하고 애니메이션이 새로 공급한 색도 반영한다. 대상 제거/컴포넌트 끄기 때 원래색을 복원하며 다른 표현 시스템이 더 최근에 바꾼 색을 덮지 않는다. 원본 텍스처/프리팹/몸체 형태는 수정하지 않는다.
- 기존 본체 합집합 마스크를 별도 disabled 인스턴스로 재사용해 관절 조각 내부 이음매 없이 외부 윤곽만 그린다. 그림자/체력바/탄환/코어·포탑 국소발광 메시는 포함하지 않는다. `PresentAtOrder`는 기존 `Present`를 유지하고 상시 윤곽+3/선택선+4/강화 스캔+5로 구분한다. 기존 선택선의색·2px·패널 열림/접기 정책은 보존한다.
- 씬 저장 변경은 `CoreGuardian_Gameplay`에 컴포넌트1개를 추가한 것뿐이다. 39×22/.69·길/땅굴②/원래배경/③색보정26·7·-.20/기존HUD/강화.8초/입자 사용자값·전투CSV/원본아트 유지. 씬은 에디터API로 저장했고 YAML/메타를 직접 수정하지 않았다.

## 확인 결과와 한계

- 코드 검토에서 idle 원본색을 복사하는 발사/보행 조각에 밝기가 중복될 수 있는 경로를 확인했다. 최종 구현은 카메라 beginCameraRendering에서만1.15배를 적용하고 endCameraRendering에서 원래색을 복원해 애니메이션 입력으로 강조색이 되먹임되지 않게 했다. 전환 회귀 검사1개 추가 red→green, 전체 새검사 최종3/3. 재검토에서 남은 차단문제 없음. 최종 코드로 실제 Inspector QA를 다시 통과했고 CG PlayMode도 다시21/27·같은6실패를 확인했다. [최종Edit결과](final_edit_tests.json), [최종Play결과](final_play_tests.json), [최종저장상태](final_state.json).

- 새 EditMode 검사: 구현 전2/2 의도대로 실패(타입 미구현), 전환 회귀1개 추가 후 최종 **3/3 통과**. 밝기 누적 방지/새 틴트 반영/대상 해제 시 원복/다른시스템의 더최근색 보존/그림자·알파 제외/실시간3px·색 변경/0px/선택선 레이어 우선을 확인했다. 비ExecuteAlways 컴포넌트의 EditMode OnDisable 콜백은 실제 Play 동작과 달라 해당 검증은 아래 실제 Play에서 수행했다. 기존 합집합 GPU 검사도 **2/2 통과**.
- [실제 적용](applied_combat.png) / [강조OFF](before_combat.png) / [Inspector3px·색 변경](inspector_width3_color.png) / [초록 선택선 병행](selection_above_emphasis.png): 자체 정상 새세션/API 수정100 멀티포탑 구매·StartCombat/자연 몬스터4마리·총7대상에서 확인했다. 1920×1080 실제 합성 GameView/Overlay 포함, 카메라 줌/재화/몹 주입 없음. 같은TimeScale0시점 비교 후 원래1/captureDelta0으로 복원하고 자체Play종료했다.
- 런타임 집중QA: 모든 대상에 Inspector 두께3·색 변경,0px숨김,끄기시원래색/켜기시재구성,기존선택선위치우선/접기선택해제,카메라및선택컴포넌트설정동일 확인. 최종 구현은 카메라 begin/end 렌더 콜백에서만 RGB 보강/복원을 수행하므로 게임 로직과 애니메이션 Update/LateUpdate에는 원본 틴트가 유지된다. 초기QA의 잘못된 비활성 원본 선택/Update중간 틴트 기준을 수정해 실제 활성 Body의 끄기 후 색과 미변경 원본 틴트를 비교했다. 제품 복원 동작은 별도즉시조회/최종QA에서 확인했다. [런타임근거](runtime_qa.json), [끄기복원](disable_check.json).
- 기존 CoreGuardian PlayMode **27개 중21통과/6실패**. 기존 선택선4/4·강화2/2·코어입자/피해/포탑효과/단일조준·여러방향보행 검사는 통과했다. 실패6개에는 과거0.6칸/옛경로/고정공격거리 전제와 수동Tick/프레임 전제가 남아 있다. 전체통과로 기록하지 않는다. 강조를 끈 저장씬에서도6개를 수동IEnumerator진단해 모두 실패했다(5개 같은실패문구, 플레이어방향검사1개는 프레임간격차이로 다른앞선assert 실패). 이는 새 TestRunner 결과가 아니며 B와독립적인 실패 재현 근거다. 기존 게임좌표/수치나 무관한검사를 이번범위에서 바꾸지 않았다. [B켜짐자동결과](emphasis_play_enabled.json), [꺼짐수동진단](baseline_diagnostics.json). 자동재호출0건/PlayMode동기HTTP거부는 통과가 아니다.
- 종료조회: 컴포넌트enabledtrue/1px/선택색/.1/1.15 저장, Playfalse/씬1/root7/dirtyfalse·compilingfalse/컴파일실패false·현재콘솔오류0/경고0·TimeScale1/captureDelta0. 자동검사 버퍼의 과거경고와 현재콘솔을 구분한다.

실제 API/HUD 집중 기능QA와 외형 확인이다. 사람 직접 플레이/전체회귀통과/후반다수전투 성능/밸런스 측정/최종자연스러움 승인이 아니다. 각 대상별 합집합 렌더의 다수개체 성능은 아직 실측하지 않았다. 사운드/밸런스/BGM/기존미추적자료/font_tools_vendor 접근오류 삭제표시 보존, 빌드/실행파일/push/Notion 변경 없음.

다음은 사용자가 Inspector로 두께/색을 조절하며 실제 플레이 느낌을 확인하는 단계다. B2가 현재 적용이며 이전 B미선택·A적용 문구를 현재로 해석하지 않는다.
