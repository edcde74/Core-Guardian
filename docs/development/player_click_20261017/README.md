# 월드 플레이어 클릭 → 현재 능력치

2026-10-17 사용자 요청: 플레이어를 직접 눌러 선택하고 플레이어 능력치 UI를 연다.

- 시작 HEAD `3f05188`, Unity stopped/컴파일 실패 없음/전투 씬1이었으며 사용자 초록색 아웃라인 설정이 미저장이었다. Editor API로 저장하고 diff가 color 한 줄뿐임을 확인해 `e704aad`에 먼저 보존했다. 조명/카메라/아트는 변경하지 않았다.
- `CoreGuardianPrototype`은 플레이어 발밑 칸 클릭과 현재 활성 본체 SpriteRenderer 영역 클릭을 처리한다. 그림자는 제외하고 몸통/머리처럼 발밑보다 위에 있는 조각도 선택할 수 있다. 클릭 영역은 조각 사각 bounds 기준이며 알파 픽셀 단위 판정은 아니다.
- `CoreGuardianHud.Inspector`의 `OpenPlayerSpecs`는 현재 능력치 탭을 연다. 월드 재클릭은 닫기 토글이 아니며 강화 탭에서 누르면 현재 능력치로 돌아온다. 하단 버튼의 기존 토글 기능은 유지한다. 포탑 선택은 해제하고 아웃라인 대상도 플레이어로 교체한다. 패널 접기는 기존처럼 선택과 아웃라인을 해제한다.
- UI 누름/해제 제스처 차단과 설치 모드 우선을 유지한다. 플레이어를 누른 채 이동하더라도 같은 실제 플레이어 위에서 해제하면 선택할 수 있다. 경로/적/그 외 빈 바닥 규칙·전투 수치·애니메이션은 유지한다.

## 검증

- [새 월드 선택 검사 실패](red.json): 직접 플레이어 칸 클릭으로 능력치 창이 열리지 않음 확인 → [선택/아웃라인 집중 PlayMode4/4](focused-playmode.json) 통과. 월드 재클릭 유지/현재 능력치 진입/포탑 선택 해제/접기 아웃라인 해제를 확인했다.
- [임시 마우스 검사](pointer.json), [검증기](VerifyPointer.cs): 실제 InputSystem 누름/해제로 상체 칸(15,11), 발밑 칸(15,10)이 다른 상태의 상체 클릭을 확인했다. 재클릭 유지·실제 UI 접기·투명 UI 차단·UI누름→숨김→플레이어위해제 클릭 투과 방지·설치 모드 우선·포탑→플레이어 선택 전환 통과. 초기 재화100으로 포탑1개 정상 설치, TimeScale0·임시 UI 차단막/콜백 설정의 기능 QA이며 사람 플레이나 밸런스 실측이 아니다. 초기 검증기는 포탑 설치 후 잔여 수정0에서 설치 모드를 다시 시도해 전제가 성립하지 않았다. 실제 UI로 먼저 설치 모드 진입을 검증하도록 순서를 수정했고, 제품 코드는 이 실패 때문에 변경하지 않았다.
- [최종 전체 EditMode117/117](editmode.json) 통과. 이후 전체 PlayMode 및 선택/설치 필터 재호출은 모두0건 반환했다. [전체0건 원본](full-playmode-zero.json). 이 재호출은 통과가 아니며 새 전체 Play 회귀 완료로 표현하지 않는다. 앞선 집중4/4와 실제 마우스 검사는 완료된 별도 근거다.
- 별도 읽기 전용 코드 리뷰에서 actionable finding 없음. Inspector의 사용자 초록색2px가 유지된 [실제1920×1080 Overlay 화면](player-click.png)을 확인했다. 원래 카메라/줌·captureDelta 유지. 임시 마우스/TimeScale/backgroundBehavior 복원과 자체Play종료를 확인했다.
- 최종 Playfalse/pausedfalse/compilingfalse/컴파일실패false·씬1/dirtyfalse·TimeScale1/captureDelta0·현재콘솔오류0/경고0. 빌드/실행파일/push 없음. 기존 미추적 AudioProduction/밸런스/BGM/포신raw/probe와 font_tools_vendor 접근 오류 보존.

아웃라인 자체 구현/조절은 [담당 기록](../selection_outline_20261017/README.md)을 따른다. 이번 변경은 로컬 구현과 인수인계에 반영했으며 Notion은 갱신하지 않았다.
