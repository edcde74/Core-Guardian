# 코어 가디언 Unity 인수인계

최종 문서 갱신: **2026-10-15 (Asia/Seoul)**. 현재 사용자 지시와 최신 담당 노션이 우선이다. 상세 이력은 링크에서 필요한 것만 읽는다.

## 다음 대화의 최우선

**다음 첫 작업은 밸런스 테스트 지속이다.** UI·아트 추가 제작보다 현재 적용 수치의 사람 플레이/정상 전투 계측과 결과 보존을 먼저 한다. [상세 재개 순서](next_work_20261013.md), [새 대화 복사용 텍스트](resume_20261013.md).

- AGENTS.md와 LOCAL_GIT.md를 읽고 Git 변경, Unity 연결·열린 씬·Play·컴파일·저장 상태를 확인한다. 과거 인수인계 상태를 현재 사실로 가정하지 않고, 다른 테스트나 사용자 Play를 임의 중단하지 않는다.
- 테스트 요청은 임의 수치 적용이나 노션 밸런스 확정 권한이 아니다. 기존 ‘밸런스 기획은 아직 건드리지 말라’는 제한을 유지한다. 현재 기준으로 측정한 뒤 변경 후보와 근거를 제시하고, 적용은 해당 사용자 지시에 따른다.
- 정적 계산/자동 계측/사람 체감/기능 회귀를 구분한다. 강제 처치·재화 주입 검사는 밸런스 실측에 쓰지 않는다. 기존 도구와 원본 결과를 보존한다.

## 프로젝트와 확인 상태

- Unity 6000.6.2f1 「코어 가디언」. 경로 `C:\Users\edcde\OneDrive\Desktop\CD`. 디멘션 디펜스와 별개다.
- 전투 씬: `Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity`.
- 실제 프로토타입은 **1~11라운드**다. 준비 50초→전투→완료 수정→다음 준비로 진행한다. 10라운드 보스 처치 후 증강 3장/30초 선택→11라운드 준비로 연결된다. 보스가 도착해도 코어가 살아 있으면 증강 없이 진행한다. 코어 파괴는 패배, 11라운드 완료는 프로토타입 검증 종료다.
- 라운드 사이 포탑·강화·기술병 위치·재화·코어를 유지하며 처음부터 재시작하면 초기화한다. 적/포탑 3종, 설치/강화/파괴/환급, 피해/재화 숫자, 전투 효과, 역할 진단, 증강 18종의 선택/누적/재설정을 구현했다.
- 마지막 게임 적용 커밋은 `dfa046b`(갈무리), 직전은 `2162ad4`(중복 접기 제거), `134be1b`(상자/여백 맞춤)다. 전체 HUD `ba72484`, 사람 테스트용 초반 수치 `d5c45db`. 인수인계 정리 커밋은 `git log`로 확인한다. GitHub push 없음.
- **마지막 Unity 실시간 확인은 2026-10-13**이다. ready, Play stopped, compiling/domainReload=false, 전투 씬 active/loaded/dirty=false, 현재 콘솔 오류/경고 0. 임시 TimeScale은 1로 복원했다. 10월 15일에는 문서·디스크·Git만 재확인했고 Unity 상태를 새로 조회하거나 Play/테스트/빌드를 실행하지 않았다.
- 최신 기능 검사는 갈무리 적용 뒤 PlayMode **8/8**이다. 실제 1920×1080 HUD·지원 정보 4줄·접힌 상태·증강 18종 공개 카드에서 폰트/글리프/영역 검사 위반 0. EditMode 73/73과 BuildCombatPrototype.Verify는 이전 검증 기록이다. 기능 통과는 밸런스/사람 체감 승인과 별개다.

## 밸런스 재개 기준

- 디스크 기준 공격형 HP **27**(25→27, +8%, 모든 라운드 공용), 1라운드 15마리/간격 **1.62초**, 2라운드 16개 개별 슬롯/간격 **1.53초**, start 0~22.95초다. 3라운드 이후 일정·편성·수급은 유지했다. 사람 테스트용 실제 적용값이며 **최종 밸런스 미승인**이다. 재개 시 씬 참조→CombatData.Load로 재확인한다.
- [수치 적용·복구](early_pressure_applied_20261013.md), [이전 초반 12회](early_pressure_test_20261013/report.md), [이전 6라운드 집중안](tension_test_20261013/report.md), [정상 연속 1~9라운드](natural_playtest_20261010.md).
- 이전 HP27 초반 3회/5배속 고정 정책에서 코어 손실 0·기술병 기여 증가를 확인했다. 1배속 사람 체감·다양한 배치·3라운드 이후 HP27 영향은 미검증이다. 사람의 최신 직접 테스트 결과부터 확인한다. 이전 6라운드 집중안은 런타임 실험이며 CSV에 적용한 것으로 가정하지 않는다.
- 다음 순서: 1~2라운드 압박→3~9라운드 성장/실패(특히 6·7~9라운드)→10라운드 보스 처치/도착→11라운드 증강 후 성능. 강한 빌드의 보상감과 약한 선택의 회복 가능성을 본다. 시간/코어 손실/피해 기여/골드·수정 수지와 배치·구매·증강·배속을 함께 기록한다.
- `MeasureNaturalRounds.cs`에 초반 HP25 전제, 간격 0.9배 변형, 6라운드 총HP555, 과거 출력 경로가 남아 있다. 현재 데이터에서 단축을 중복 적용하거나 과거 결과를 덮어쓰지 않는다. 새 회차/출력 경로와 10~11라운드 증강 계측부터 점검한다. 상세는 next_work 문서에 있다.
- 노션 동기화에서 기획과 실행 CSV의 밸런스 차이를 자동 해소하지 않았다. [동기화 범위/밸런스 제외](notion_sync_20261013/report.md), [담당 노션 자료 위치](../skills/core-guardian-balance/references/sources.md). 개인 설치 `core-guardian-balance` 스킬도 사용할 수 있다.

## 승인 적용한 UI·아트

- HUD: 실제 투명 2D 프레임, 심볼+한글, hover/pressed/selected/강화 성공 반응, 강화 현재→다음/증가량/비용, 피해 스크롤/접기. 하단 ‘타워 설치’/‘플레이어 강화’가 해당 패널을 열고 닫는다. 외부 설치 접기·강화 접기·설치 분류 안 접기는 삭제했다. ‘설치 취소’와 ‘분류로 돌아가기’는 별도 기능이다.
- 갈무리 A안: 본문/버튼 Galmuri11, 패널/단계/증강 제목 Galmuri11 Bold. Dynamic/HintedRaster, TTF/OFL 번들. 피해/처치 골드 숫자는 승인 Pixelify Sans Bold/TMP 유지. [폰트 적용](hud_font_applied_20261013/report.md), [중복 버튼 제거](hud_toggle_cleanup_20261013/report.md), [상자 맞춤](hud_text_fit_20261013/report.md), [전체 HUD](hud_applied_20261013.md).
- 증강 뒷면은 은색/금색/청백색 테두리·장식·광택으로 등급을 기대하게 한다. 등급 글자/배지는 없다. 첫 클릭 공개, 연출 중 선택 차단, 완료 후 재클릭 선택이며 다른 카드를 전부 공개하지 않아도 선택할 수 있다. 등급별 뒤집기·스파크·합성 사운드, 30초 최고 등급 자동 선택/동률 왼쪽 우선 유지. [기능](boss_augmentation_20261013.md), [시안 기준](hud_sprite_review_20261013/augmentation_flip_review.md).
- 보스 안광V3 정지 외형은 승인·적용 완료, 보행/공격 애니메이션은 미구현이다. 몬스터 작업 전 MONSTER_ART_STYLE.md와 현재 병사 3종 실물을 동일 픽셀 배율로 비교한다. [보스 적용](boss_art_applied_20261013.md), [공통 화풍](../art/MONSTER_ART_STYLE.md).
- 승인 전투 효과 B, E2 금속 너트, 확대 투사체/골드 심볼 유지. [효과/숫자](combat_art_applied_20261010.md), [표시 수정](feedback_polish_20261010.md). 이미 승인한 아트/UI를 다시 컨펌받거나 재제작하지 않는다.

## 남은 작업·배포 주의

밸런스 테스트 이후 보스 애니메이션, BGM 청취/승인/적용, 다른 해상도·화면비 HUD 사용성, 스파크·사운드 직접 청취를 진행한다. 현재 화면 확인은 1920×1080 중심이며 최종 Pixel Perfect 규격/팔레트/기준 해상도는 미확정이다. 슬라이드/삼각형 접기와 통합 전투력은 미적용이다.

- BGM 루프 WAV 3개는 Assets 외부 시안이며 미승인/게임 미적용이다. [자료](bgm_sketches_20261013.md). AudioProduction/와 관련 분석 폴더의 미커밋 작업을 임의 삭제·되돌리지 않는다.
- 기존 `Builds/Windows_20261013/CoreGuardian.exe`와 ZIP은 1~9라운드 기준이다. 최신 1~11라운드/HUD/갈무리/HP27을 시험할 빌드로 오인하지 않는다. 사용자 요청 없이 새 빌드/실행파일 실행을 하지 않는다. [기존 빌드 검증](windows_build_20261013.md).

## 코드·도구·보존

- 모델/계산: `Assets/Game/Scripts/CoreGuardian/{CombatData,Session,Augmentations}.cs`. 연결/HUD: `Assets/Scripts/{CoreGuardianPrototype,CoreGuardianHud,CoreGuardianAugmentationView}.cs`.
- CSV: `Assets/Resources/CoreGuardian/{enemies,towers,stage,upgrades,waves}.csv`. stage 기본 수치도 테스트 가안이다. 폰트: `Assets/Resources/CoreGuardian/Fonts/`.
- [로컬 Git 지침](LOCAL_GIT.md)에 따라 주요 변경 전후 복구 기준/완료 커밋을 남긴다. 사용자 작업 보존, push는 별도 지시에 따른다.
- Unity CLI에 현재 연결 프로젝트를 명시하고 에디터 API를 사용한다. `BuildCombatPrototype.Main`은 씬 생성기라 재실행하지 않는다. 확인은 Verify로 한다. 씬 YAML/메타 직접 편집 금지, SampleScene/비교 씬/원본 프리팹 보존. Save/Reimport 후 컴파일과 내부 success/errorDetails를 확인하고 async 테스트는 completed까지 확인한다.
- HUD 캡처는 Play/source=screen이며 반환 savedPath를 확인한다. 임시 TimeScale·Play·생성 Assets를 자신의 작업 범위에서 복원한다. 현재 사용자 세션/파일을 임의 초기화하지 않는다.
- 문서마다 담당 역할을 유지한다. 게임 규칙 상세는 최신 노션 담당 문서에 연결하고 중복하지 않는다.

## 상세 이력 보존

기존 인수인계 전체는 [10월 13일 정리 전 스냅샷](unity_handover_snapshot_20261013_before_resume.md)에 보존했다. 날짜별 원본 보고서/JSON/시안/코드도 삭제하지 않았다. 최신 시작 순서는 이 파일과 next_work/resume 문서를 따른다.
