# 선택 대상 아웃라인 · 접기 시 선택 해제

2026-10-17 사용자 지시: 능력치 조회/강화 UI가 열려 있을 때만 아웃라인을 활성화한다. 패널을 접으면 선택도 해제한다. 외형은 Inspector에서 사용자가 직접 조절한다. 복구 기준 `740be94`; 기존 게임 추적 파일 변경이 없어 빈 checkpoint를 만들지 않았다.

## 현재 적용

- `CoreGuardianHud.Inspector.cs`가 실제 `InspectionTarget`을 제공한다. 플레이어 조회로 바꾸면 이전 포탑 ID를 해제하고 플레이어만 표시한다. 조회↔구매 탭 전환은 같은 대상을 유지한다.
- 접기 손잡이/유효 빈 바닥 첫 클릭/같은 플레이어 버튼 닫기로 패널과 선택을 함께 해제한다. 접힌 포탑 표식·재개방 손잡이도 남지 않는다. 설치 진입·단계 전환으로 닫히는 정보창도 선택을 해제한다. 설치 입력 소비/취소·구매 권한·설치 후 정보창 자동 펼침 생략은 보존한다. 과거 ‘빈 바닥 첫 클릭은 선택 유지, 두 번째 해제’ 규칙을 이번 사용자 지시가 대체한다.
- `CoreGuardianSelectionOutline`은 현재 본체 `2D Sprite` 및 하위의 활성 SpriteRenderer만 원래 UV/메시/변환/반전을 사용해 하나의 이진 마스크로 합친다. 원본 비활성 레이어·그림자·체력바·탄환·MeshRenderer 발광은 마스크에 넣지 않는다. 조각 접합부 위에 내부 선을 덧그리지 않는다.
- 대상 화면 영역만 Point RenderTexture로 그린 후 바깥 0~8 화면 픽셀을 확장한다. 별도 셰이더 두 개가 Resources에 포함된다. 기존 본체 재질/아트/반동/포신 조준/국소 발광/후처리/전투 수치는 변경하지 않는다. 아웃라인 자체에 깜빡임/Bloom은 없다.
- 전투 씬은 Editor API로 설정 컴포넌트만 추가 저장했다. 다른 씬이나 비교 씬을 만들지 않았다.

## Inspector에서 조절

Hierarchy의 **CoreGuardian_Gameplay → Core Guardian Selection Outline** 컴포넌트:

- **Color**: 색상과 알파. 초기값은 황동색 RGBA(0.8,0.67,0.39,1)이며 최종 사용자 색상 확정은 아니다.
- **Width Pixels**: 화면 픽셀 두께 0~8, 초기 2. 0이면 숨김.
- **Alpha Threshold**: 본체 실루엣에 포함할 최소 알파, 초기 0.1.
- 컴포넌트 체크를 끄면 표시를 끈다. Play 중에도 즉시 반영되지만 Unity의 일반 동작대로 Play 종료 시 값은 복귀한다. 영구 보관하려면 Edit 모드에서 조절하고 씬을 저장한다.

## 검증

- 새 집중 PlayMode 최초 **0/3 실패**: 접기 후 포탑 ID 잔류, 플레이어 전환 후 포탑 ID 잔류, 아웃라인 미구현을 확인했다. 구현 중 MaterialPropertyBlock을 필드 초기화에서 생성해 Unity 직렬화 오류가 발생했고, 리소스 생성 단계로 이동한 후 **3/3 통과**했다.
- 대칭/비대칭 GPU 검사 **2/2**: 두 조각의 합집합 1536/1664픽셀을 확인하고, 실제 카메라 렌더의 모든 검사 픽셀을 CPU 기준 2px 외곽과 비교했다. 상하 반전·경계 어긋남·몸통 내부/겹치는 접합부 선 없음·두께0 숨김을 확인했다.
- 최종 전체 **EditMode117/117, PlayMode25/25 completed**, 실패/스킵0. [EditMode 원본](editmode.json), [PlayMode 원본](playmode.json). 이 결과는 자동 기능 회귀이며 사람 플레이나 밸런스 실측이 아니다.
- 읽기 전용 별도 코드 리뷰에서 actionable finding 없음. 대칭 GPU 도형만으로 상하 반전을 검출할 수 없다는 지적을 받아 위 비대칭/픽셀 단위 검사를 보강했다.
- 실제 Overlay1920×1080 캡처에서 플레이어·단일/멀티/지원·현재 능력치/강화·접기·색/두께 변경을 확인했다. [재현기](CaptureViews.cs)는 자체 Play·진단 재화·실제 배치·UI 콜백·어댑터 임시 비활성화를 사용했다. 원래 카메라/줌/TimeScale/captureDelta 유지. 사람 직접 입력/정상 다수 전투/성능/최종 아트 승인은 별도다.
- 종료 조회: Play=false/paused=false/compiling=false/compilationFailed=false, 전투 씬1·dirty=false, TimeScale1/captureDelta0, HDR=false/post=true, 현재 콘솔 오류0·경고1. 경고는 Unity AI Toolkit Account API 30초 접근 지연이다. 과거 오류 버퍼와 현재 콘솔을 구분했다.
- AudioProduction/밸런스/BGM·포신 raw/probe 미추적 자료 보존. font_tools_vendor 접근 오류를 실제 삭제로 단정하거나 stage하지 않았다. 빌드/실행파일/push 없음.

## 실제 화면

[단일 현재 능력치 2px](single-2px.png) · [단일 강화](single-upgrade.png) · [아웃라인 OFF 비교](single-off.png) · [멀티](multi-2px.png) · [지원](support-2px.png) · [플레이어 기본 2px](player-2px.png) · [플레이어 색/4px 변경](player-custom-4px.png) · [접기·선택 해제](folded-deselected.png).

표시 규칙 담당은 기존 Notion ‘HUD 화면 및 표시’, 표현 담당은 ‘셰이더·화면 후처리 기준’이다. 이번 작업은 로컬 구현/문서 갱신이며 Notion 재동기화는 하지 않았다. 최신 사용자 지시와 이 기록을 과거 미구현/미확정 상태보다 우선한다.
