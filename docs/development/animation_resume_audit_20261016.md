# 애니메이션 피드백 재개 점검

2026-10-16 (Asia/Seoul). 이번 작업은 읽기 점검과 기록 정리다. 새 아트 제작·게임 코드 수정·Play 실행·테스트 재실행·빌드·실행파일 실행·push는 하지 않았다.

## 현재 확인

- 시작 HEAD `a3cb12d`. 애니메이션 적용 `49dc331`과 별도 VFX 체크포인트 `ed71e07` 이후 상태다.
- Unity MCP: 프로젝트 CD, Unity 6000.6.2f1, ready, Play stopped, compiling=false. Editor API: updating=false, 전투 씬 하나 loaded/active, dirty=false.
- CLI 1.0.0-beta.13의 status는 에디터 검색 파일 접근 거부로 실패했다. 이를 에디터 종료로 해석하지 않고 MCP로 위 상태를 확인했다.
- 별도 VFX 작업: CoreGuardianCombatAnimation.cs / CoreGuardianUnitAnimation.cs 수정, HitFlash 코드·shader·Edit/PlayMode 테스트 및 meta가 미추적 상태였다. 해당 파일은 수정·스테이징하지 않았다. AudioProduction·balance_tension_20261013·bgm_sketches 자료도 보존했다.
- font_tools_vendor 하위에는 현재 계정의 접근 거부가 있어 일반 git status에서 삭제처럼 보이는 항목이 있었다. 실제 삭제로 단정하지 않고 복원·스테이징하지 않았다.

## 애니메이션 검토 범위

MONSTER_ART_STYLE.md와 지정된 실제 공격/수비/버프 원본 PNG, current_monsters_reference.png, vertical_facing의 native3x_comparison.png 및 four_directions.png를 직접 열어 확인했다. 정지 비교에서 상행 뒷모습/하행 앞모습 구분은 보인다. 이번에는 실제 경로를 새로 실행하거나 움직이는 프레임을 재검증하지 않았다.

기존 X축 보행은 사용자 승인 상태를 유지한다. Y축 파생 외형·자연스러움은 미승인이다. 이전 EditMode87/87·PlayMode15/15는 [당시 검증 기록](vertical_facing_20261016/README.md)이며 이번 작업의 새 테스트 결과가 아니다. 현재 별도 VFX 미커밋 상태까지 검증한 것으로 해석하지 않는다. 구체적인 최신 외형/걸음/그림자 피드백을 요청했으며, 기존 원본 기준 제작 권한을 다시 묻지 않았다.

## 밸런스 재개 사전 점검

- Editor API로 현재 전투 씬의 CoreGuardianPrototype이 enemies/towers/stage/upgrades/waves를 모두 Assets/Resources/CoreGuardian/*.csv에 참조함을 확인했다. 해당 TextAsset 내용을 CombatData.Load로 읽었다(실제 Play 계측 아님).
- 공격형 HP27, 1R 15마리/1.62초, 2R 16마리/마지막 start22.95초. 디스크 CSV에서도 2R 간격1.53초 확인. 6R 총HP는 **581**이다.
- MeasureNaturalRounds.cs는 여전히 HP25 전제(92행), start/interval 0.9배 변형(93행), 총HP555(88행), 과거 출력 경로(79~80행), File.WriteAllText(182행)를 사용한다. Start 기본 경로에는 기존 파일 덮어쓰기 방지가 없다. 현 상태로 실행하지 않았다.
- 10~11R은 일반 루프에서 진행할 수 있으나 증강 직접/자동 선택·후보/선택 결과를 보고하는 필드가 없다. 단순 완주를 증강별 비교 완료로 간주할 수 없다.
- 노션 [플레이테스트 및 변경 이력](https://www.notion.so/3df389dbfc6881fc9f1bd004adfc5999)을 다시 fetch했다. 반환 마지막 편집 시각은 2026-10-12T23:46:15.244Z이며 조회 페이지 안에서 새 HP27 사람 직접 테스트 결과는 찾지 못했다. 개인의 미기록 플레이 유무는 확인하지 못했다. 로컬 balance_tension 폴더도 10월13일 자료였다.

수치·노션·과거 결과는 변경하지 않았다. 애니메이션 피드백 후 [기존 재개 순서](next_work_20261013.md)에 따라 새 사람 결과를 확인하고, 계측기를 현재 데이터/새 출력 경로에 맞춰 안전하게 준비한 후 측정한다.
