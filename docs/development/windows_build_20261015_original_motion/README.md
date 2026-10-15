# Windows 빌드·실행 — 2026-10-15

- 사용자 요청에 따라 게임 커밋 `3c5da7a`를 빌드하고 그래픽 창으로 실행했다. 수정 전 추적 파일 변경 없음. 사용자 BGM/밸런스 미커밋 자료 보존.
- 출력: `Builds/Windows_20261015_OriginalMotion/CoreGuardian.exe`. 기존 10월 13일 빌드 보존. 전투 씬 하나만 BuildPlayerOptions에 명시했으며 EditorBuildSettings/씬/원본 프리팹/게임 데이터는 수정하지 않았다.
- [빌드 보고서](build_report.json): Succeeded, 48.83초, 152,665,703바이트. Windows64, BuildOptions.None. 현재 1~11라운드·보스·증강·HUD·원본 기본 방향 공격 동작 1차 포함. 전 방향 조준/보행은 미완료.
- CLI 요청은 30초에 시간초과했지만 빌드는 계속돼 완료 보고서와 실행 파일을 확인했다. 재시도 빌드로 기존 결과를 덮어쓰지 않았다.
- errors3: 기존 전역 조명 중복2, CLI 요청 시간초과1. warnings497. 이를 경고/오류 없음으로 기록하지 않는다.
- 창 모드1600×900으로 실행. [실행 기록](launch.json), [프로세스 확인](running.json): PID31028, 창 제목CD, windowHandle 존재, Responding=true. 게임을 종료하지 않고 사용자 테스트용으로 열어두었다.
- 런타임 로그는 빌드 폴더 `Player-preview.log`. 후처리 셰이더 미지원/스트리핑 메시지가 있다. 화면/입력/전체 라운드의 사람 테스트 통과나 아트·밸런스 승인을 주장하지 않는다.
- [에디터 종료 상태](editor_state.json): Play stopped, compiling=false, 전투 씬dirty=false. 빌드 산출물은 기존 .gitignore 정책에 따라 Git 제외. 새 ZIP 배포는 요청 범위 밖이므로 생성하지 않았다. GitHub push 없음.
