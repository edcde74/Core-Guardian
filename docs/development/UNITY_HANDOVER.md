# 코어 가디언 Unity 인수인계

최종 문서 갱신: **2026-10-16 (Asia/Seoul)**. 현재 사용자 지시와 최신 담당 노션이 우선이다. 상세 이력은 링크에서 필요한 것만 읽는다.

## 다음 대화의 최우선

**2026-10-16 최신 확인:** 사용자 지시로 이후 빌드/실행파일 실행은 명시 요청 때만 한다. 몬스터/그림자 중심 불일치를 격리 프리뷰로 확인했다. 기존 본체 발 위치 보정을 반영하지 않은 이미지 피벗 좌우 반전으로, 우보행 시 공격형 약9도트·수비형11도트 왼쪽, 버프형4도트 오른쪽으로 벗어나고 우보행 뒤 정지에도 남는다. 보스 가로 차이는1도트 이하다. **원인 확인만 했고 게임 동작 수정은 아직 안 했다.** 다음 수정 후보는 원본 발 중심을 공통 반전축으로 쓰는 보행/정지 보정이다. [수치·실제 비교·재현](monster_shadow_check_20261016/README.md). 원본 PNG/씬/프리팹 보존, 새 빌드/실행/Play 시작 없음.

**최신 UI 수정:** 라운드 시작 시 설치 패널/설치 모드를 정리한다. 설치 중에는 상단 중앙 상태/라운드 UI와 왼쪽 ESC 취소만 남기고, 설치 성공·취소 시 기존 HUD 표시/접힘 상태를 복원한다. PlayMode14/14·EditMode78/78 및 실제 화면 확인. [구현·검증·화면](placement_focus_20261015/README.md).

**2026-10-15 최신: 몬스터3종·보스 보행을 승인 원본 도트의 관절 동작으로 교체하고 플레이어 Idle을 추가했다.** 과거 생성 4프레임의 외형/보폭 변화 대신 양발 동일 궤적·반 주기, 월드 이동 거리 기반 보행을 사용한다. 좌우는 원본 반전, 상하는 원본 사선 시점을 유지한 발 궤적이다. 새 뒷모습을 그린 것이 아니다. 플레이어 Idle은 발 고정/상체 호흡이며 공격·이동이 우선한다. 기존 원본 기반 공격 동작/보스 피벗 유지. **플레이어 전 방향 공격 조준·방향 전환/보행은 미완료, 자연스러움·아트 승인은 별도**다. [최신 구현·검증·움직이는 비교](original_walk_idle_20261015/README.md), [공격 동작 1차](original_animation_rework_20261015/README.md), [외형 복구](original_art_restore_20261015/README.md). 이후 밸런스 순서는 [next_work](next_work_20261013.md)·[resume](resume_20261013.md)에 보존한다.

- AGENTS.md와 LOCAL_GIT.md를 읽고 Git 변경, Unity 연결·열린 씬·Play·컴파일·저장 상태를 확인한다. 과거 인수인계 상태를 현재 사실로 가정하지 않고, 다른 테스트나 사용자 Play를 임의 중단하지 않는다.
- 테스트 요청은 임의 수치 적용이나 노션 밸런스 확정 권한이 아니다. 기존 ‘밸런스 기획은 아직 건드리지 말라’는 제한을 유지한다. 현재 기준으로 측정한 뒤 변경 후보와 근거를 제시하고, 적용은 해당 사용자 지시에 따른다.
- 정적 계산/자동 계측/사람 체감/기능 회귀를 구분한다. 강제 처치·재화 주입 검사는 밸런스 실측에 쓰지 않는다. 기존 도구와 원본 결과를 보존한다.

## 프로젝트와 확인 상태

- Unity 6000.6.2f1 「코어 가디언」. 경로 `C:\Users\edcde\OneDrive\Desktop\CD`. 디멘션 디펜스와 별개다.
- 전투 씬: `Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity`.
- 실제 프로토타입은 **1~11라운드**다. 준비 50초→전투→완료 수정→다음 준비로 진행한다. 10라운드 보스 처치 후 증강 3장/30초 선택→11라운드 준비로 연결된다. 보스가 도착해도 코어가 살아 있으면 증강 없이 진행한다. 코어 파괴는 패배, 11라운드 완료는 프로토타입 검증 종료다.
- 라운드 사이 포탑·강화·기술병 위치·재화·코어를 유지하며 처음부터 재시작하면 초기화한다. 적/포탑 3종, 설치/강화/파괴/환급, 피해/재화 숫자, 전투 효과, 역할 진단, 증강 18종의 선택/누적/재설정을 구현했다.
- 애니메이션 수정 전 복구 기준은 `6a910bd`, 그 이전 게임 적용은 `dfa046b`(갈무리)다. `12ff270`의 플레이어/포탑 외형은 사용자 거부 후 원본 복구했다. 최신 복구 커밋은 로컬 git log로 확인한다. 전체 HUD `ba72484`, 사람 테스트용 초반 수치 `d5c45db` 유지. GitHub push 없음.
- **마지막 Unity 실시간 확인은 2026-10-16 한도 중단 재개 후**다. ready, Play stopped, compiling=false, 전투 씬dirty=false, TimeScale1. 설치 UI 수정 빌드는 중단 전에 성공했고, 재개 후 새 창으로 실행/응답·예외 로그0 확인까지 마쳤다. 원본 씬/프리팹/CSV 변경 없음. [재개·검증·실행 기록](placement_focus_20261015/README.md). 과거 콘솔 이력을 임의 삭제하지 않았다.
- 최신 설치 UI 수정 후 전체 EditMode **78/78**, 전체 PlayMode **14/14 completed**. 수동/자동 시작 설치 UI 정리, 설치 집중 화면과 완료/취소 복원, 기존 전투/애니메이션/HUD/증강 유지. 실제 화면도 확인했다. [최신 결과](placement_focus_20261015/README.md). 원본 보행/Idle 픽셀·관절/발 고정 검증은 [해당 기록](original_walk_idle_20261015/README.md)에 보존했다. 10월16일 재개 때는 기존 완료 결과를 확인했으며 새 재실행으로 기록하지 않는다. 기능 통과는 사람 체감/아트 승인의 근거가 아니다.

## 밸런스 재개 기준

- 디스크 기준 공격형 HP **27**(25→27, +8%, 모든 라운드 공용), 1라운드 15마리/간격 **1.62초**, 2라운드 16개 개별 슬롯/간격 **1.53초**, start 0~22.95초다. 3라운드 이후 일정·편성·수급은 유지했다. 사람 테스트용 실제 적용값이며 **최종 밸런스 미승인**이다. 재개 시 씬 참조→CombatData.Load로 재확인한다.
- [수치 적용·복구](early_pressure_applied_20261013.md), [이전 초반 12회](early_pressure_test_20261013/report.md), [이전 6라운드 집중안](tension_test_20261013/report.md), [정상 연속 1~9라운드](natural_playtest_20261010.md).
- 이전 HP27 초반 3회/5배속 고정 정책에서 코어 손실 0·기술병 기여 증가를 확인했다. 1배속 사람 체감·다양한 배치·3라운드 이후 HP27 영향은 미검증이다. 사람의 최신 직접 테스트 결과부터 확인한다. 이전 6라운드 집중안은 런타임 실험이며 CSV에 적용한 것으로 가정하지 않는다.
- 2026-10-15 사용자 확인: HP27 적용 후 새 직접 테스트 결과 없음. 최신 노션 플레이테스트/웨이브/적/증강 담당 페이지를 조회했으며 새 사람 결과를 찾지 못했다. 이번 애니메이션 진단의 재화·적 생성/고속 공격 주입을 밸런스 실측으로 사용하지 않는다.
- 다음 순서: 1~2라운드 압박→3~9라운드 성장/실패(특히 6·7~9라운드)→10라운드 보스 처치/도착→11라운드 증강 후 성능. 강한 빌드의 보상감과 약한 선택의 회복 가능성을 본다. 시간/코어 손실/피해 기여/골드·수정 수지와 배치·구매·증강·배속을 함께 기록한다.
- `MeasureNaturalRounds.cs`에 초반 HP25 전제, 간격 0.9배 변형, 6라운드 총HP555, 과거 출력 경로가 남아 있다. 현재 데이터에서 단축을 중복 적용하거나 과거 결과를 덮어쓰지 않는다. 새 회차/출력 경로와 10~11라운드 증강 계측부터 점검한다. 상세는 next_work 문서에 있다.
- 노션 동기화에서 기획과 실행 CSV의 밸런스 차이를 자동 해소하지 않았다. [동기화 범위/밸런스 제외](notion_sync_20261013/report.md), [담당 노션 자료 위치](../skills/core-guardian-balance/references/sources.md). 개인 설치 `core-guardian-balance` 스킬도 사용할 수 있다.

## 승인 적용한 UI·아트

- HUD: 실제 투명 2D 프레임, 심볼+한글, hover/pressed/selected/강화 성공 반응, 강화 현재→다음/증가량/비용, 피해 스크롤/접기. 하단 ‘타워 설치’/‘플레이어 강화’가 해당 패널을 열고 닫는다. 외부 설치 접기·강화 접기·설치 분류 안 접기는 삭제했다. ‘설치 취소’와 ‘분류로 돌아가기’는 별도 기능이다.
- 설치 중에는 상단 중앙 UI/왼쪽 ESC만 남기고 다른 HUD의 표시·입력을 임시로 숨긴다. 설치 성공 시 기존 HUD/선택 목록 복원, 취소 시 선택 목록을 닫고 HUD 복원, 실패 시 설치 모드 유지. 수동 시작/준비 만료에는 모드·선택 목록을 정리한다. [설치 표시 상태 책임/검증](placement_focus_20261015/README.md).
- 갈무리 A안: 본문/버튼 Galmuri11, 패널/단계/증강 제목 Galmuri11 Bold. Dynamic/HintedRaster, TTF/OFL 번들. 피해/처치 골드 숫자는 승인 Pixelify Sans Bold/TMP 유지. [폰트 적용](hud_font_applied_20261013/report.md), [중복 버튼 제거](hud_toggle_cleanup_20261013/report.md), [상자 맞춤](hud_text_fit_20261013/report.md), [전체 HUD](hud_applied_20261013.md).
- 증강 뒷면은 은색/금색/청백색 테두리·장식·광택으로 등급을 기대하게 한다. 등급 글자/배지는 없다. 첫 클릭 공개, 연출 중 선택 차단, 완료 후 재클릭 선택이며 다른 카드를 전부 공개하지 않아도 선택할 수 있다. 등급별 뒤집기·스파크·합성 사운드, 30초 최고 등급 자동 선택/동률 왼쪽 우선 유지. [기능](boss_augmentation_20261013.md), [시안 기준](hud_sprite_review_20261013/augmentation_flip_review.md).
- 보스 안광V3 승인 외형과 BossAnimated 별도 프리팹, 발 피벗(42,6.5)px을 유지한다. 몬스터3종/보스는 생성 아틀라스 대신 원본 픽셀 관절 보행을 사용해 원본 피벗을 그대로 쓴다. 기술병·포탑 원본 PNG/프리팹을 보존했고 원본 공격 동작1차/플레이어 Idle 적용·자연스러움은 별도 검토다. [현재 보행/Idle](original_walk_idle_20261015/README.md), [공격](original_animation_rework_20261015/README.md), [복구](original_art_restore_20261015/README.md), [공통 화풍](../art/MONSTER_ART_STYLE.md).
- 승인 전투 효과 B, E2 금속 너트, 확대 투사체/골드 심볼 유지. [효과/숫자](combat_art_applied_20261010.md), [표시 수정](feedback_polish_20261010.md). 이미 승인한 아트/UI를 다시 컨펌받거나 재제작하지 않는다.

## 남은 작업·배포 주의

기존 플레이어·포탑 외형을 유지한 애니메이션 재작업이 먼저다. 얼굴/복장/무기·포신/받침대 비율까지 원본과 같은 방향·같은 픽셀 배율로 비교해야 한다. 새 도안이나 팔레트 일치만으로 완료 처리하지 않는다. 이후 밸런스 실측, BGM, 다른 화면비 HUD/사운드 검토가 남는다. 몬스터 코어 타격/사망 클립·최종 Pixel Perfect 규격은 미확정이다.

- BGM 루프 WAV 3개는 Assets 외부 시안이며 미승인/게임 미적용이다. [자료](bgm_sketches_20261013.md). AudioProduction/와 관련 분석 폴더의 미커밋 작업을 임의 삭제·되돌리지 않는다.
- 최신 실행 파일은 **`Builds/Windows_20261015_PlacementFocus/CoreGuardian.exe`**(게임`cac61dd`)다. 기존 1~11·보스·증강·HUD·HP27·원본 공격/보행·Idle에 설치 UI 숨김/복원과 라운드 시작 정리를 포함한다. 빌드Succeeded(18.97초), 10월16일1600×900 창으로 실행/응답 확인 후 열어두었다. 기존 전역 조명 중복2건/Pipeline 설정 경고1건이 남아 있다. [빌드·실행/최신 검증](placement_focus_20261015/README.md). 과거 WalkIdle/OriginalMotion/10월13일 빌드와 ZIP은 보존했다.

## 코드·도구·보존

- 모델/계산: `Assets/Game/Scripts/CoreGuardian/{CombatData,Session,Augmentations}.cs`. 연결/HUD: `Assets/Scripts/{CoreGuardianPrototype,CoreGuardianHud,CoreGuardianAugmentationView}.cs`.
- 애니메이션: `Assets/Scripts/CoreGuardian{UnitAnimation,CombatAnimation,OriginalMotion,OriginalWalk,OriginalIdle}.cs`. 원본 공격 레이어 `Resources/CoreGuardian/OriginalMotion`, 보행 `OriginalWalk`, 플레이어 호흡 `OriginalIdle`을 사용한다. 과거 생성 프레임 로드 경로를 제거했다. 지원 수정은 실제 지원 범위 내 공격 포탑 발사에 반응한다. 플레이어 방향별 조준은 미완료. 전투 수치·원본 프리팹·씬 YAML 변경 없음.
- CSV: `Assets/Resources/CoreGuardian/{enemies,towers,stage,upgrades,waves}.csv`. stage 기본 수치도 테스트 가안이다. 폰트: `Assets/Resources/CoreGuardian/Fonts/`.
- [로컬 Git 지침](LOCAL_GIT.md)에 따라 주요 변경 전후 복구 기준/완료 커밋을 남긴다. 사용자 작업 보존, push는 별도 지시에 따른다.
- Unity CLI에 현재 연결 프로젝트를 명시하고 에디터 API를 사용한다. `BuildCombatPrototype.Main`은 씬 생성기라 재실행하지 않는다. 확인은 Verify로 한다. 씬 YAML/메타 직접 편집 금지, SampleScene/비교 씬/원본 프리팹 보존. Save/Reimport 후 컴파일과 내부 success/errorDetails를 확인하고 async 테스트는 completed까지 확인한다.
- HUD 캡처는 Play/source=screen이며 반환 savedPath를 확인한다. 임시 TimeScale·Play·생성 Assets를 자신의 작업 범위에서 복원한다. 현재 사용자 세션/파일을 임의 초기화하지 않는다.
- 문서마다 담당 역할을 유지한다. 게임 규칙 상세는 최신 노션 담당 문서에 연결하고 중복하지 않는다.

## 상세 이력 보존

기존 인수인계 전체는 [10월 13일 정리 전 스냅샷](unity_handover_snapshot_20261013_before_resume.md)에 보존했다. 날짜별 원본 보고서/JSON/시안/코드도 삭제하지 않았다. 최신 시작 순서는 이 파일과 next_work/resume 문서를 따른다.
