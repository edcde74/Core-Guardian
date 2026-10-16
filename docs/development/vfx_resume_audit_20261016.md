# 타격 VFX 재개 점검

2026-10-16 (Asia/Seoul). 사용자 재개 지시에 따라 AGENTS.md → UNITY_HANDOVER.md → LOCAL_GIT.md 순서로 읽고 몸 피격 기록과 현재 코드를 대조했다. 이번 작업은 읽기 점검과 로컬 문서 기록이며 게임 구현·새 검사 실행·Play·빌드·실행파일 실행·Notion 변경·push는 하지 않았다.

## 실제 확인

- 점검 시작 HEAD: `dfba29b` (`Update VFX conversation handover and next core feedback scope`). `3ed4f1e`, `a7ad256`, `2990bdd`, `1c76a11` 후속 관계를 로컬 로그로 확인했다.
- Assets/Packages/ProjectSettings에 Git 변경 표시 없음. 기존 AudioProduction/, balance_tension_20261013/, bgm_sketches_20261013.md, player_facing_20261016/ 미추적 작업 및 font_tools_vendor/ 다수 삭제 표시를 보존했다. 삭제 원인·의도는 확인하지 않았다. Git 소유권 차이로 기본 조회가 거부되어 해당 명령에만 `-c safe.directory=C:/Users/edcde/OneDrive/Desktop/CD`를 지정했다. 전역 설정은 변경하지 않았다.
- Unity MCP editor_status: 해당 CD 경로, Unity6000.6.2f1, ready, playMode=stopped, compiling=false, domainReloadInProgress=false. list_open_scenes: 전투 씬1개 active/loaded, isDirty=false. 저장·정지·씬 교체를 실행하지 않았다.
- `CoreGuardianHitFlash.cs`: Duration=.09f, Strength=.22f, RetriggerDelay=.1f. 최초 전체 EditMode92/92·PlayMode16/16은 최초 적용 이력이고,90ms 조정은 집중5/5 이력이다. 이번에 다시 실행한 결과가 아니다. 보행 후속95/95·16/16도 별도 작업 이력으로 유지한다.

## 기존 코어 피해 연결

- `CoreGuardianPrototype.Update()`에서 Session.Tick 후 CoreEnergy가 previousCore보다 작을 때 차이를 `combatArt.CoreDamaged()`에 전달하고 Changed를 호출한다. 한 프레임의 코어 감소는 이 차이로 전달된다.
- `CoreGuardianCombatArt.CoreDamaged()`가 기존 CoreHit(폭.7, 지속.35초)과 hud.ShowCoreNumber를 표시한다. 효과 목록은 호출마다 추가되며 공통 Update가 알파 감소·제거를 처리한다.
- `CoreGuardianHud.Build()`의 TopStatus 내 Core 아이콘과 Status_Core 텍스트가 현재/최대 에너지를 표시한다. 확인한 경로에는 코어 피해 전용 본체 플래시·상단 표시 반응·연속 피해 중첩 억제가 없다.
- 강화 작업은 기존 연결을 재사용할 수 있다. 실제 런타임 시각·성능은 이번에 검증하지 않았다.

## 다음 사용자 지시 전 유지할 범위

코어 붉은색·흰색 플래시, 상단 코어 표시 반응, 연속 피해 중첩 억제는 제안 상태·미구현이다. 저체력 임계값·경고 반복 주기·화면 흔들림을 확정하지 않는다. 평상시 효과는 작게, 보스 가속·코어 피해 강조 방향과 사운드 제외를 유지한다. 기존 B 효과·숫자·몸 피격90ms 및 병행 애니메이션/사용자 작업을 보존한다. 노션 담당 문서는 [기존 분류 보고서](vfx_notion_20261016/report.md)를 사용하고 중복 생성하지 않는다.
