# Windows 플레이테스트 빌드 — 2026-10-13

- 수정 전 복구 기준: `db8abec` (깨끗한 CD 작업 트리 확인 후 시작). 게임 코드·씬·메타 수정 없음.
- 시작/포함 씬: `Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity` 한 개만 BuildPlayerOptions에 명시. EditorBuildSettings는 변경하지 않음.
- 빌드 전: 연결 ready, Play stopped, compiling=false, domainReloadInProgress=false. 열린 전투 씬 isDirty=false, Windows64 지원 확인.
- 실행기: [BuildWindowsPlayer.cs](BuildWindowsPlayer.cs). 기존 빌드 보존을 위해 새 출력 폴더만 허용.
- 결과: [BuildReport](windows_build_20261013.json) success=true / Succeeded, 46.98초, 140,808,919바이트. Unity 6000.6.2f1, Windows64, BuildOptions.None.
- CLI는 서버 메인 스레드의 30초 제한으로 응답 시간초과. 실제 빌드는 계속되어 성공 보고서와 실행 파일 생성 확인. 시간초과를 빌드 실패나 경고 없음으로 해석하지 않음.
- 보고서 errors=3: 중복 전역 조명 2건, CLI API 응답 시간초과 1건. warnings=497: 주로 Sentis/AI inference 셰이더 변형 경고와 플레이어 Pipeline 런타임 설정 미지정. 씬·패키지 변경 없이 기록.
- 새 플레이어를 `-batchmode -nographics`로 별도 실행해 프로세스 생존·응답과 엔진/씬 로딩 확인. 검사 로그에 Exception·Missing·error 없음. 검사 프로세스만 종료. 그래픽·입력·전체 라운드 플레이 검증은 미수행.
- 배포: `Builds/CoreGuardian_Windows_20261013.zip`. 실행 파일·Data·DLL·Mono 런타임·README·폰트 OFL 라이선스를 포함. ZIP 전체를 해제한 뒤 exe 실행.
- 포장 검증: ZIP 52,500,988바이트, 219개 항목. exe·UnityPlayer.dll·Data 185개·Mono 22개·폰트 라이선스 2개 포함 확인. 빌드 후 전투 씬 isDirty=false, playing=false, compiling=false, Windows64 유지 확인.
- 포함 범위: 최신 1~9라운드, 투사체 확대, 골드 심볼/숫자 묶음. 보스·증강·10~11라운드·BGM은 미포함. 밸런스·아트 승인을 의미하지 않음.
- 작업 도중 발견된 `docs/development/balance_analysis_20261013/`, `docs/skills/`는 이번 작업에서 생성하지 않았으며 보존·커밋 제외.
