# 새 대화용 최신 인수인계 — HUD A/B 적용 후

2026-10-17. 아래 텍스트를 복사한다. 이전 resume_20261017.md와 resume_hud_ux_20261017.md는 과거 요청/이력이며 아래 최신 적용 상태를 우선한다. 이번 정리는 문서 대조이며 새 Unity 검사나 사람 플레이가 아니다.

```text
코어 가디언 프로젝트를 이어서 진행해줘.
경로: C:\Users\edcde\OneDrive\Desktop\CD

먼저 읽어줘:
- AGENTS.md
- docs/development/UNITY_HANDOVER.md
- docs/development/LOCAL_GIT.md
- docs/development/resume_hud_latest_20261017.md

현재 HUD는 이미 구현된 상태야. 다시 초기 시안 단계로 되돌리지 마.
1. 하단 공용 정보창에서 현재 능력치 조회와 강화 구매를 분리했어. 빈 바닥 첫 클릭은 접기만 하고 선택을 유지하며, 다음 클릭이 선택 해제야. 접기와 설치 취소는 구분하고 UI를 닫는 클릭이 설치까지 실행하면 안 돼. 설치 성공 후 큰 능력치창은 자동으로 펼치지 않아.
2. 능력치 화면은 내가 선택한 A안 적용 완료(64f0b26). 안쪽 금속 패널 하나에 공격력/공격속도/사거리/탄환 속도를2×2로 정렬하고 항목별 심볼·현재 수치를 보여줘. 지원 포탑은 공격력 증가/지원 범위만 표시해. 현재 능력치=목록, 능력치 강화=렌치+상승 화살표야. 스펙 같은 표현 대신 능력치 등 게임에서 이해할 수 있는 용어를 유지해.
3. 접기 반응과 설치 심볼은 둘 다 내가 선택한 B안 적용 완료(778256e). 손잡이는 마우스 오버/포커스 청록 강조·눌림 반응이 있어. 설치 심볼은 공격 분류의 짧은 포신, 단일의 긴 포신, 범위의 세 포신, 지원의 양팔 사이 수정으로 구분해. 기존 피해 HUD의 역할 심볼과는 별도야.
4. 상단 손잡이는 전투 안내판(라운드/전투/살아 있는 적)을 실제로 숨기고 남은 자원 상태줄 아래로 이동해. 코어·현재 라운드·재화와 준비/증강 제한 시간은 유지해.
5. 피해 HUD는 기존 빈 트랙·안쪽 여백·최대 피해 항목 대비 비율·범위/단일/기술병 역할 심볼·얇은 황동 손잡이를 유지해. 접어도 집계는 계속돼.

최신 담당 기록과 실제 화면:
- docs/development/hud_handles_tower_B_20261017/README.md
- docs/development/hud_ability_A_20261017/README.md
- docs/development/hud_readability_20261017/README.md
- docs/development/hud_usability_applied_20261017/README.md
- docs/development/damage_hud_applied_20261017/README.md
주요 소스는 Assets/Scripts/CoreGuardianHud.cs, CoreGuardianHud.Inspector.cs, CoreGuardianHud.Abilities.cs, CoreGuardianHudSkin.cs, CoreGuardianHudHandleReaction.cs와 CoreGuardianPrototype.cs야.

Notion도 기존 담당 페이지에 반영했고 실제 화면을 첨부했어. 새 중복 페이지를 만들지 말고 최신 내용을 읽어줘.
- HUD: https://www.notion.so/3df389dbfc68811f9a87e5a5ff05a571
- 테스트: https://www.notion.so/3df389dbfc6881fc9f1bd004adfc5999

남은 요청에는 강화 중 플레이어/포탑의 월드 선택 대상 아웃라인이 있어. 아직 미구현이며 방식·색·두께·접기/선택 해제 시 표시 정책은 미확정이야. 이 요청을 기존 선택/구매 규칙 변경 승인으로 확대하지 말고 현재 코드/최신 기획을 확인해 시안부터 검토받아. 이미 확정한 아트 스타일·피해 HUD·능력치 A안·손잡이/설치 심볼 B안은 다시 묻지 마.

병행 적용도 보존해:
- 단일 포탑 원본 포신 조준/연결부·실제 포구 발사 수정211fc83. docs/development/single_tower_aim_20261017/README.md.
- 포탑 발광 단일1/멀티3/지원1, 코어 국소 발광/입자, 코어 피해 B안·확정 숫자 위치, 몸 피격0.09초.
- 플레이어/몬스터 방향별 애니메이션·그림자·체력바와 원본 리소스.
- 현재 카메라 색보정35/12/-0.45EV, 후처리true/HDRfalse. Bloom 등 미선택 효과는 추가하지 마.

마지막 HUD 기능 QA에서는 손잡이5종 이벤트 경로, 임시 InputSystem 마우스를 통한 Hover/누름/이동·반전 뒤 재개방, 클릭 투과 방지, 설치/빈 바닥2단계, 전투·증강 흐름을 확인했어. 진단 재화·콜백·강제 처치·임시 마우스/일부 TimeScale0 검사이며 사람 플레이·밸런스 실측·전체 회귀가 아니야. 종료 당시 Play=false/씬1·dirty=false/컴파일 실패 없음/TimeScale1·captureDelta0/현재 콘솔 오류0, 경고1은 Unity AI Toolkit Account API 접근 지연이었어. 이번 문서 갱신으로 새 Unity 검사를 한 것은 아니니 현재 상태를 다시 확인해.

실제 HEAD와 미커밋 변경부터 확인하고 다른 채팅/사용자 작업을 보존해. 사용자 Play나 다른 검사를 중단하거나 같은 Unity에서 검사를 겹치지 마. 전투 씬 Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity 하나와 원본을 유지하고 삭제된 비교/샘플 씬을 복원하지 마. 씬 YAML 직접 수정 금지. 빌드/실행파일 실행은 명시 요청 때만, GitHub push 금지.
현재는 HUD·시각 작업을 마무리하는 단계야. 기존 밸런스 후속은 보존하되 별도 일정/지시 없이 밸런스 테스트나 수치 변경으로 넘어가지 마. 작업 후 담당 문서·UNITY_HANDOVER.md·로컬 커밋을 갱신해.
```
