# 능력치 HUD A안 적용

2026-10-17. 사용자 ‘A로 해줘’ 승인에 따라 적용. 수정 전 복구 기준 `75bedc2`. [선택한 비교 시안](../hud_ability_proposals_20261017/ability_layout_ABC.png)은 디자인 참고이며 아래 PNG는 실제 Unity Overlay 캡처다.

## 구현

- 플레이어와 공격 포탑의 현재 능력치를 한 장의 안쪽 금속 패널에 2×2로 표시한다. 순서는 공격력/공격속도, 사거리/탄환 속도다. 이름과 큰 현재 수치를 분리하고 보조 효과는 패널 아래에 표시한다.
- 지원 포탑은 공격력 증가/지원 범위만 표시한다. 직접 공격하지 않는다는 설명을 유지한다. 공격 포탑의 지원·증강 비율과 버프 적용 공격력도 기존 계산/표시 규칙을 유지한다.
- 교차 검/시계+탄환/바깥 화살표 조준점/직선 탄환+잔상으로 능력치를 구분한다. 플레이어와 포탑의 현재/강화 탭은 목록/렌치+상승 화살표, 제목은 헬멧/포탑으로 구분한다. 구매 항목에도 같은 능력치 심볼과 같은 배치 순서를 쓴다.
- 탭 글자 눌림 기준점 수정은 유지하며 가로/세로 전환 시 심볼도 세로 중앙으로 정렬한다. 새 장식 Image/Text는 raycastTarget=false이며 바깥 패널의 입력 차단은 유지한다.
- `CoreGuardianHud.Abilities.cs`가 능력치판 생성/표시만 담당한다. Inspector가 대상·탭 표시를 연결하고 Skin이 심볼을 매핑한다. Session/Prototype·가격·성장·전투 공식은 변경하지 않았다.
- 아이콘은 imagegen built-in으로 제작한 투명 원본 `symbols_source.png`를 보존하고 `ImportSymbols.cs`로 알파 영역 정리·최근접 샘플링 후32×32 PNG8개를 Unity API로 임포트했다. Point/밉맵 없음/무압축. 최종 리소스는 `Assets/Resources/CoreGuardian/Hud/`의 AbilityList/AbilityUpgrade/PlayerIdentity/TowerIdentity/StatDamage/StatRate/StatRange/StatProjectile이다. 제작 프롬프트는 [prompt.md](prompt.md).

## 실제 적용 화면

1920×1080·현재 전투 씬·원래 카메라/후처리·Overlay 포함. 진단 재화·실제 Session 설치·HUD 콜백을 사용한 준비 단계 QA 화면이며 사람 직접 플레이나 밸런스 결과가 아니다.

- [플레이어 현재](player_current.png) / [플레이어 강화](player_upgrade.png)
- [공격 포탑 현재](tower_current.png) / [공격 포탑 강화](tower_upgrade.png)
- [지원 포탑 현재](support_current.png) / [지원 포탑 강화](support_upgrade.png)

## 검증

- 수정 전 `VerifyAbilityA.cs`: `A inset ability panel must exist` 실패를 확인했다.
- 수정 후 [green.json](green.json): 실제 안쪽 패널/4개 수치/실제 구매 후 기존 반올림으로 갱신/구분되는 심볼/현재↔구매 전환/지원2항목/접어도 선택 유지 통과.
- 기존 `VerifyTabText.cs` 재실행: 플레이어/포탑의 현재·강화 탭 위아래 여백 유지 통과.
- 기존 `VerifyFlow.cs` 재실행 [flow.json](flow.json): 실제 Session 구매, 배타적 하단 표시, 설치 실패/성공/목록 복원, 자동 스펙 펼침 생략, 빈 바닥 첫 접기/다음 선택 해제, 전투 진입 접기/재개방, 증강 접힘 중 후보·공개·시간·자동선택 유지 통과. 진단/강제 처치를 사용한 기능 QA다.
- 읽기 전용 코드 리뷰에서 막는 문제 없음. 이후 지원2항목 및 실제 구매 후 수치 검사를 추가해 통과했다. 실제 Overlay6화면도 직접 확인했다. 새 실제 마우스 검사·전체 NUnit 회귀·다른 해상도·사람 판독/자연 다수 전투 성능·밸런스 실측은 하지 않았다.
- 자체 Play 종료 후 ready/Play stopped/컴파일false·실패false/전투 씬1·dirtyfalse/현재 콘솔 오류0·경고0을 조회했다. 이 작업은 카메라·TimeScale·captureDelta·입력 장치를 변경하지 않았다. 씬 YAML/원본 아트·피해 HUD·병행 애니메이션/포탑 발광 수정은 보존한다. 빌드/실행파일/push 없음.

## 문서 동기화

기존 Notion [HUD 화면 및 표시](https://www.notion.so/3df389dbfc68811f9a87e5a5ff05a571)의 담당 절만 갱신하고 플레이어 실제 적용 화면을 첨부했다. [플레이테스트 및 변경 이력](https://www.notion.so/3df389dbfc6881fc9f1bd004adfc5999)에 이번 검사와 미검증 범위를 기록했다. 두 페이지 재조회로 본문·첨부 존재를 확인했다. 기존 하위 링크/피해 HUD 규칙을 보존했다.
