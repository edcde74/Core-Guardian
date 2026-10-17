# 손잡이 반응·포탑 심볼 B안 적용

2026-10-17. 사용자 ‘B로 적용해줘’에 따라 손잡이/포탑 심볼 둘 다 B안 적용. 복구 기준은 실제 HEAD `211fc83`(최신 단일 포탑 조준 포함). [이전 시안](../hud_handles_tower_proposals_20261017/README.md).

## 구현

- 공통 `CoreGuardianHudHandleReaction`으로 상단/피해/하단 정보창/재개방/증강 손잡이5종의 Hover·포커스에 청록 안쪽 선을 표시한다. Pressed에는 선과 본체를 어둡게 하고 장식을1px 눌린 방향으로 이동한다. 이탈/비활성 시 초기화하며 장식은 Raycast를 막지 않는다.
- 기존 원본 황동 손잡이 스프라이트·가로/세로 방향·접힘/펼침 위치·클릭 크기는 유지한다. 피해 손잡이는 동일 크기의 자식 장식으로 분리하고 루트는 기존 투명 클릭 영역/반전을 유지한다. 피해 트랙·역할 심볼·집계는 변경하지 않는다.
- 설치 심볼은 공격 분류=짧은 포신, 단일 고속=긴 포신 하나, 범위 공격=세 포신, 지원 분류/개별=양팔 사이 수정으로 구분한다. 목록에서는40×40 심볼과 이름/비용/부족/선행조건을 함께 표시한다. 선택 포탑의 능력치 제목도 해당 종류의 심볼을 표시한다. 플레이어/능력치 A안·게임 포탑 원본은 유지한다.
- imagegen built-in으로 원본3종과 B안 보드를 참조한 투명4심볼을 생성했다. 원본 `symbols_source.png` 보존, `ImportSymbols.cs`가 Unity API로 알파 영역/최근접 샘플링 후48×48 Point·밉맵 없음·무압축으로 임포트한다. 실제 리소스는 `Assets/Resources/CoreGuardian/Hud/TowerAttackCategory.png`, `TowerSingle.png`, `TowerMulti.png`, `TowerSupport.png`. [프롬프트](prompt.md).

## 검증과 실제 화면

- `VerifyB.cs`: [변경 전](red.json) `B cyan hover edge required: TopFold` 실패 → [변경 후](green.json)5종 Hover/Pressed/해제/이탈·3종 심볼·분류 전환 통과. 숨긴 손잡이는 검사용으로 부모를 활성화했다가 원래 상태로 복구했다. 이벤트 디스패치 QA다.
- `VerifyHoverPointer.cs`: [임시 InputSystem 마우스](hover_pointer.json)로 상단/피해 손잡이 실제 UI Hover·Press·접힘 뒤 이동/반전 위치의 Hover·재개방 통과. 별도 [기존 포인터 흐름](pointer_flow.json)도 UI 누름→창 숨김→바닥 해제 클릭 투과 방지, 설치 클릭 중복 설치 방지, 설치1회/빈 바닥2단계/재선택/실제 손잡이 적중 통과.
- [기존 VerifyFlow 재실행](flow.json): 실제 Session 구매, 설치 실패/성공/목록 복원, 자동 스펙 펼침 생략, 선택 유지, 전투 접기/재개방, 증강 후보·공개·시간·자동선택 유지 통과.
- 실제 Overlay1920×1080: [분류](categories.png), [공격 목록](attack_list.png), [지원 목록](support_list.png), [손잡이 기본](handles_idle.png), [Hover](handles_hover.png), [Pressed](handles_pressed.png). 6화면 직접 확인. Hover/Pressed 그림은2손잡이에 이벤트를 동시에 주입한 표시 QA이며 사람 마우스 동작으로 오해하지 않는다. [별도 임시 마우스 검사 후 화면](pointer_verified.png)도 보존했다.
- 읽기 전용 코드 리뷰: 클릭 영역/반전/장식 Raycast/게임 수치 변경 여부에서 막는 문제 없음.
- 진단 재화/콜백/강제 처치/임시 마우스·일부 TimeScale0의 기능 QA이며 사람 플레이·밸런스 실측·전체 회귀가 아니다. 다른 해상도/실제 축소 판독 선호/다수 전투 성능은 미검증이다.

## 종료·문서

자체 Play 종료, 임시 마우스·TimeScale·backgroundBehavior 복원. ready/Play stopped/컴파일false·실패false/전투 씬1·dirtyfalse/현재 콘솔 오류0을 조회했다. 현재 경고1은 Unity AI Toolkit Account API 접근30초 지연이며 HUD 오류와 구분했다. 이전 컴파일에서 기존 코드의 Unity obsolete 경고도 버퍼에 있으나 새 HUD 컴파일 오류는 없었다. 카메라·씬·전투 수치·원본 아트·최신 단일 포탑 조준/발광을 보존했다. 빌드/실행파일/push 없음.

기존 Notion HUD 페이지의 B안 표시 규칙·실제 분류/Hover 화면2장을 갱신하고 테스트 담당 페이지에는 이번 검사/주입/한계를 기록했다. 두 페이지 재조회로 본문과 첨부를 확인했다. 기존 확정 피해 HUD 규칙/하위 링크를 보존했다.
