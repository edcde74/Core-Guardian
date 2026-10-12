# 코어 가디언 Unity 작업 인수인계

최종 갱신: 2026-10-13 (Asia/Seoul)

이 파일은 Unity 작업의 재개 지점이다. 현재 상태를 갱신해 유지하며, 상세 기획과 과거 이력은 담당 문서에 연결한다. 사용자 지시와 최신 노션이 이 파일보다 우선한다.

## 작업 시작·종료

1. 이 파일을 읽고 요청에 필요한 상세 기록과 최신 노션을 확인한다.
2. Unity 연결, 현재 열린 씬, Play·컴파일·저장 상태를 다시 확인한다. 아래 마지막 확인 상태를 현재 사실로 가정하지 않는다.
3. 요청 범위의 작업과 검증을 수행한다. 씬 YAML·에셋 메타를 직접 수정하지 않고 에디터 API를 사용한다.
4. 종료 전에 이 파일의 현재 상태·검증·남은 작업을 교체 갱신하고 날짜별 상세 기록을 연결한다. 미검증 항목과 승인 여부를 명확히 기록한다.

## 프로젝트·현재 상태

- 프로젝트: Unity 6000.6.2f1 타워디펜스 「코어 가디언」. 디멘션 디펜스와 별개.
- 경로: `C:\Users\edcde\OneDrive\Desktop\CD`.
- 전투 씬: `Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity`.
- 전체 프로토타입 목표: 1~11라운드. 현재 실제 진행 구현: **1~9라운드**.
- 종료→완료 수정 보상→다음 50초 준비→전투 연결. 9라운드 완료는 현재 검증 종료다.
- 포탑·포탑 강화·기술병 강화·재화·코어·기술병 위치 유지. 처음부터 재시작하면 초기화.
- 코어 파괴 시 패배하며 완료 보상·다음 준비로 진행하지 않음.
- 2~9라운드의 고정 순서·시점은 사용자 승인 완료. `waves.csv`에 반영됨.
- 실제 전투·적/포탑 3종·설치/강화/파괴/환급·피해 숫자·전투 효과 구현. 별도 역할 검증 모드 유지.
- 미구현: 보스·증강·10~11라운드·최종 보행/공격 애니메이션·BGM. 최신 Windows 빌드는 2026-10-13 생성.
- 마지막 Unity 확인 (2026-10-13 Windows 빌드 후): CD 연결 응답 정상, 전투 씬 isDirty=false, Play stopped, compiling=false, Windows64 대상 유지. 빌드 중 중복 전역 조명/CLI 시간초과 메시지는 상세 기록 참조. 마지막 게임 회귀 검사는 2026-10-10의 EditMode28/28 + PlayMode6/6이며 이번 빌드에서 재실행하지 않음.
- 2026-10-13 로컬 Git 구성. Unity 소스·에셋·메타·설정·문서를 초기 커밋에 보존하고 캐시·빌드는 제외. GitHub 연결 없음. 기능 수정 전후 복구 기준 커밋은 [LOCAL_GIT.md](LOCAL_GIT.md), 구성 기록은 [git_setup_20261013.md](git_setup_20261013.md) 참조.

## 최근 검증과 다음 작업

- 2026-10-13 `core-guardian-balance` 스킬 작성·개인 스킬 폴더 설치. 최신 담당 노션10페이지·실행 CSV/코드·기존 자동 Play3정책으로1~10R 시험 분석. 현재 구현1~9R의 정적 수급/발수와 과거 실측을 분리하고10R/증강은 기획 가안·미구현으로 표시. 형식 검증·분석 도구9개 행동 검사·독립 분석/획득가능성 보완 재검증 통과. 게임/노션 변경 없음, 밸런스 승인 아님. [시험 보고서](balance_analysis_20261013/report.md), [스킬 원본](../skills/core-guardian-balance/SKILL.md), [검증·재실행](balance_analysis_20261013/validation.md).
- 밸런스 후속:1배속 사람 플레이에서 초반 구매 공백·기술병 사거리/기여 판독·준비 시작버튼 사용 우선 측정. 첫 강화2R 가능 시나리오는 제안만 작성, 미적용. 첫 증강 실측은10R 보스/증강 구현 뒤11R까지 필요. 강한 빌드 평준화·일괄 난이도 상승을 기본 해법으로 삼지 않음.
- 밸런스 분석 중 Unity 읽기 조회: CD ready, 전투 씬active/loaded/dirty=false, Play stopped, compiling=false, domainReload=false,timeScale1. CSV5종 씬참조·로더·계산 경로는 정적 확인. 새 Play/Verify/게임 회귀 검사를 실행하지 않음. 콘솔 조회에서 빌드의 중복 글로벌 조명2건·API 타임아웃1건을 읽었고 후속 응답 현재 groundTruth는 오류0/컴파일실패false. 빌드 상세는 아래 별도 작업 기록을 보존함.
- 최신 배포: `Builds/CoreGuardian_Windows_20261013.zip` / `Builds/Windows_20261013/CoreGuardian.exe`. 최신 전투 씬만 포함, BuildReport Succeeded, 기본 로딩 검사 통과. 중복 전역 조명·패키지 셰이더 메시지는 남음. 그래픽/입력/전체 라운드 수동 플레이는 미검증. [빌드 상세 기록](windows_build_20261013.md).
- 최신 표시 수정: 사용자 요청으로 투사체 3종 폭25% 증가(기술병0.20/포탑0.275). 처치 골드 심볼36×36과 숫자를 같은 HUD 그룹에 간격4로 붙여 동시 이동·페이드·삭제. 기존 피해·재화 판정은 유지. 실제 처치 +3, 여러 자리 +123, 만료·재설정·클릭 차단 없음 확인. [수정/검증 기록](feedback_polish_20261010.md).
- 최신 회귀 결과: 표시 수정 후 EditMode28/28 + PlayMode6/6, 총34개 통과. 새 표시의 사용자 체감 확인은 남아 있으며 선택 체감은 사용자 답변상 아직 미확인. 최종 밸런스 승인으로 처리하지 않음.
- 이전 연속 검증: 실제 Play Mode의 정상 이동·구매 API와 자동 공격으로 세 연속 실행 측정. 저장 선택 6R 패배, 포탑 강화 선택 7R 패배, 경로에 한 칸 더 가까운 기술병/포탑 배치에서 1~9R 완료·코어20 유지. 최종 골드80/수정105, 기술병 강화 4/6/9R에 100/140/190골드. 결과는 [자동 조작 연속 실행](natural_playtest_20261010.md).
- 측정은 5배속·준비 시작 명령·고정 위치의 자동 조작이며 사람의 실제 입력/체감 난이도 검증과 구분. 수치·아트 변경 없음, 최종 밸런스 승인 아님. 첫 실행의 캡처 경로 도구 오류 1건과 기록 직렬화 복구는 상세 기록 참조. 이후 두 실행 runtimeErrors 없음.
- 다음 우선 작업: 사용자 실제 플레이에서 커진 투사체와 골드 심볼/숫자 묶음 판독 확인. 설치 커서·Esc·피해 숫자·버프/체력·사거리/배치 차이와 설치/강화 선택 체감은 계속 확인. 다른 위치·강화 선택을 반복 비교한 뒤 필요 시 수치 조정.
- 이후 보스→증강→11라운드 연결. 애니메이션 제작 전 방향 수·프레임 수·발사 시점은 사용자 확인 필요.

## 사용자 승인·유지할 기준

- 피해 숫자: 실제 감소 체력만 표시. 초과 피해 제외, 마지막 처치 피해 포함. 피해 0·죽은 대상·코어 도착에 적 피해 숫자 생성 안 함.
- 전투 효과 B안, 스패너 기술병의 E2 금속 너트 투사체, 숫자 폰트 Pixelify Sans Bold 승인·적용. TMP 숫자용이며 한글 HUD 전체 교체 아님. OFL 자료 보관.
- 2026-10-10 후속 사용자 요청: 공격 투사체를 조금 확대, 처치 골드 심볼 확대 및 바로 오른쪽 숫자 정렬. 적용값과 검증은 feedback_polish_20261010.md. 새 표시 체감·선택 체감은 미확인.
- 설치 시 타일 중앙 커서를 표시하고 보드 위 OS 포인터 숨김. UI 위 일반 포인터 유지. OS 포인터 자체를 강제 이동하는 구현으로 설명하지 않음.
- Esc/설치 취소는 설치 선택 취소. 패널 접기는 선택 유지. 새로 열 때 공격/지원 초기 분류 화면 표시.
- 기술병이 서 있는 칸에 포탑 설치 허용, 자동 이동 없음. 나간 뒤 점유 칸 재진입 차단.
- 현재 대상이 사거리 안에서 살아 있으면 유지. 사망·사거리 이탈 시 해제·재선정.
- 처치 골드 즉시 지급, 바닥 줍기 없음. 비행 중 대상 소멸 시 투사체 피해 없이 소멸, 대상 변경·스플래시 없음.
- 준비 중만 포탑 설치/강화, 전투 중 파괴 허용. 플레이어 강화는 준비/전투 가능. 포탑 없이도 준비 만료 시 전투 시작.
- 피해 float 계산→방어력 1당 1% 감소→0.5 이상 올림. 실제 체력 감소만 누적.
- 현재 크기·그림자·발/받침대 앵커·A 길 유지. A 재질 색상 (2.15,2.18,2.22,1).
- 세계관: 수리한 석조 요새, 새 금속 보강과 수정 장치. 회색 갑옷·차분한 붉은 천·짧고 단순한 병사형 적. 큰 픽셀·단순한 형태.
- 45×25 논리 칸, 한 칸 0.6 Unity 단위. 연결 통로의 실제 폭 5칸을 검사해야 하며 ‘5×5가 포함됨’으로 대체하지 않음.
- 최종 픽셀 규격·제한 팔레트·Pixel Perfect Camera·기준 해상도는 미확정. 외형을 이유로 새 수리 기능을 추가하지 않음.
- 규칙과 수치는 최신 담당 노션에서 확인. 승인된 실행 순서와 최종 밸런스 확정을 구분.

## 코드·데이터 위치

- 모델: `Assets/Game/Scripts/CoreGuardian/CombatData.cs`, `Session.cs` (namespace CoreGuardian).
- 연결/HUD/효과: `Assets/Scripts/CoreGuardianPrototype.cs`, `CoreGuardianHud.cs`, `CoreGuardianCombatArt.cs`.
- 이동/깊이: `Assets/Scripts/PrototypeGridMovement.cs`, `ArtDepthSorting.cs`.
- 실행 데이터: `Assets/Resources/CoreGuardian/{enemies,towers,stage,upgrades,waves}.csv`.
- `stage.csv`: 기본 waveReward=20, waveReward5~9=25. 시작 코어20/골드0/수정100, 준비50초. 수치는 플레이테스트 가안.
- `Session`: LastWave, CompletedWave, LastCompletionReward. 다음 전투 시작 시 일정·대상·공격 대기·라운드 누적 피해 초기화.
- 효과: `Assets/Resources/CoreGuardian/Art/CombatB.png` (9개 Sprite, Point/무압축/mipmap 없음).
- 폰트: `Assets/Art/Combat/20261010/Fonts/PixelifySans-Bold.ttf`, Resources의 `CoreGuardian/Fonts/PixelifySansBoldDamage` 및 `PixelifySansBoldDamageOutline`.
- 승인 원본 프리팹: `Assets/Art/ScreenTests/20261008_Depth/Prefabs/`.
- 기존 Assets/Game 기초 코드는 보존됐지만 현재 전투 규칙의 근거로 오인하지 않음.

## 필요한 상세 기록만 읽기

- 최신 라운드 구현/검증: [round_flow_20261010.md](round_flow_20261010.md).
- 최신 투사체/골드 표시 수정: [feedback_polish_20261010.md](feedback_polish_20261010.md), [EditMode](feedback_polish_editmode_20261010.json), [PlayMode](feedback_polish_playmode_20261010.json).
- 최신 자동 조작 연속 실행/재개: [natural_playtest_20261010.md](natural_playtest_20261010.md), [실행기](MeasureNaturalRounds.cs). 보상/피해 주입 없이 1~9R 성공 및 두 선택의 실패 지점 기록.
- 결과: [EditMode](round_flow_editmode_20261010.json), [PlayMode](round_flow_playmode_20261010.json).
- 확정 순서 전체: [wave_order_confirmed_20261010.json](wave_order_confirmed_20261010.json). proposal 파일은 승인 전 역사 기록.
- 사용법: [combat_prototype_20261008.md](combat_prototype_20261008.md) (최신 범위 업데이트 확인).
- 후속 작업: [next_work_20261010.md](next_work_20261010.md).
- 아트/폰트 적용: [combat_art_applied_20261010.md](combat_art_applied_20261010.md), [톤 기준](../art/combat_art_style_20261010.md).
- 설치 UI 수정: [install_menu_fix_20261010.md](install_menu_fix_20261010.md).
- 추가 아트 목록: [art_requirements_20261008.md](art_requirements_20261008.md).
- 이전 전투/레벨/그림자: [combat_progress_20261008.md](combat_progress_20261008.md), [A 길 비교](../art/path_contrast_20261008.md).

## 담당 노션

- [프로젝트 루트](https://www.notion.so/3df389dbfc6881bc858bce200d1bb742)
- [게임 루프](https://www.notion.so/3e6389dbfc68819fbfe5ca17f14460d3), [편성표](https://www.notion.so/3e6389dbfc6881c5aa2bed3c32047474), [웨이브 시스템](https://www.notion.so/3e6389dbfc68815db2e7e95be4598092)
- [재화](https://www.notion.so/3e3389dbfc688160a9b7efcc83505cb3), [타워](https://www.notion.so/3df389dbfc6881a2b037fbce63f2f534), [플레이어 전투/이동](https://www.notion.so/3df389dbfc6881f8be26ff810ba7c18e), [성장](https://www.notion.so/3e6389dbfc68811283ffe9b73d0f0da8), [피해 계산](https://www.notion.so/3ea389dbfc6881919546c09d4ae7ade6)
- [적/보스](https://www.notion.so/3df389dbfc6881ea9f5ec4e0f7b8084c), [증강](https://www.notion.so/3e6389dbfc6881bf9600d5f42fb68a44), [HUD](https://www.notion.so/3df389dbfc68811f9a87e5a5ff05a571), [레벨](https://www.notion.so/3e9389dbfc688159ae8cec214b4e3e25)
- [효과 아트/타이포그래피](https://www.notion.so/3f4389dbfc688118965ecb7a1ffaaf3c), [추가 아트](https://www.notion.so/3f3389dbfc6881d2a70be916ded8ddb7)
- [후속 작업](https://www.notion.so/3f4389dbfc688171b84dc19b138925f4), [플레이테스트 이력](https://www.notion.so/3df389dbfc6881fc9f1bd004adfc5999)

## Unity CLI 재개 명령

- 2026-10-11 CLI 인증: `auth login` 성공 후 `license activate` 성공, Unity Personal·Asset Store 활성화 확인. 로그인/라이선스 상태 재조회 확인. 프로젝트 코드·씬 변경 및 Unity 씬 상태 재검사는 수행하지 않음.

```powershell
$cgUnity = 'C:\Users\edcde\AppData\Local\Unity\bin\unity.exe'
$cgProject = 'C:\Users\edcde\OneDrive\Desktop\CD'
& $cgUnity command editor_status --format json --project-path $cgProject
& $cgUnity command run_script --file docs/development/BuildCombatPrototype.cs --entry BuildCombatPrototype.Verify --format json --project-path $cgProject
& $cgUnity command editor_play --project-path $cgProject
& $cgUnity command editor_stop --project-path $cgProject
& $cgUnity command run_tests --mode editor --format json --project-path $cgProject
& $cgUnity command run_tests --mode playmode --async_tests true --format json --project-path $cgProject
& $cgUnity command test_status --format json --project-path $cgProject
```

- `BuildCombatPrototype.Main`은 기존 씬 생성용으로 재실행하지 않는다. 확인은 Verify.
- 최상위 success만 보지 않고 내부 result.success·결과 문자열·errorDetails 확인. async 테스트는 completed 최종 결과 확인.
- 새 테스트가 발견되지 않으면 `RefreshRoundFlow.Main`으로 에디터 가져오기/컴파일 요청 후 완료 확인.
- 일반 sandbox 터미널 초기화 문제가 있었고 require_escalated 실행을 사용한 이력이 있음. 실패를 성공으로 간주하지 않음.
- Application.runInBackground=true. 임시 timeScale 변경 시 복구 후 Play 종료.
- HUD 포함 캡처는 Play 중 source=screen. 반환 savedPath 확인.
- 최신 실행 파일: `Builds/Windows_20261013/CoreGuardian.exe`, 전달 ZIP: `Builds/CoreGuardian_Windows_20261013.zip`. 전체 폴더를 함께 유지. 포함 씬·폰트 라이선스·남은 빌드 메시지는 windows_build_20261013.md 참조. `Builds/Windows_20261002`는 이전 B 비교 씬 빌드로 보존.
