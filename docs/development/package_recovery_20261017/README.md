# Unity Safe Mode·빈 패키지 캐시 복구

2026-10-17. 사용자 ‘Unity 에러를 고쳐’ 요청. 시작 HEAD `88a83bf`. 게임·Packages·ProjectSettings의 미커밋 변경 없음. 다른 사용자 자료/게임 코드/씬/manifest/lockfile는 변경하지 않는다.

## 원인 근거

- CLI는 CD(pid24572)가 열려 있지만 Pipeline load pending/연결 불가라고 응답했다. Logs/Editor.log에 Safe Mode 진입이 명시돼 있으며 컴파일 오류줄810개를 확인했다. Pipeline list의 safeMode=null을 Safe Mode 아님으로 해석하지 않았다.
- 첫 오류는 2D Common UTess.cs의 NativeSortExtension 부재,AppUI의 InputSystem 부재,Sprite Editor 타입 부재이며 이후 Inference 타입 누락 등 연쇄 오류다.
- Library/PackageCache에서 **68개 디렉터리가 실제로 비어 있고 package.json도 없음**을 읽기와 권한을 높인 재조회 양쪽으로 확인했다. 접근 제한/실제 누락을 구분했다. 비어 있게 된 외부 원인은 확인하지 않았고 특정 사용자 작업 탓으로 단정하지 않는다.
- 잠금 버전 원본의 NativeSort.cs에는 NativeSortExtension이 존재한다. 해당 캐시/어셈블리 누락이 현재 오류의 직접 원인이다. 게임 코드의 using/타입을 바꾸는 방식으로 우회하지 않는다.

## 복원

`restore_empty_packages.py`가 **빈 디렉터리에만** 복사한다. 목적지가 Library/PackageCache 바로 아래인지 resolve 검증,설치본/아카이브의 패키지 이름·버전을 lockfile과 대조한다. 파일이 이미 있는 폴더는 덮어쓰지 않는다. tar 경로탈출/링크를 거부하고 공식 Unity tarball의 SHA1을 레지스트리 shasum과 검사했다. 작업 전 계획은 recovery_plan.json,완료 내역은 recovery_results.json.

- Unity6000.6.2f1 설치본의 동일 버전54개 복사.
- 공식 레지스트리의 동일 버전14개 다운로드·체크섬 검증·복사. 임시 다운로드 전개는 Temp/PackageRecovery_20261017에만 둔다.
- **68개 복원,누락0개**를 확인했다. 기존 비어 있지 않은8개 패키지는 그대로 보존했다.
- 첫 레지스트리 `/name/version` 메타데이터 조회는404였으며 `/name` 응답의 versions[고정버전].dist로 올바른 공식 주소를 확인했다. 최신 버전으로 올리지 않았다.

## 재색인·최종 검증

사용자 Ctrl+R 후 초기 누락 오류가 줄었지만 Timeline/Tilemap Extras의 복원된 일부 파일이 컴파일 rsp 소스목록에서 빠져 있었다. 사용자가 에디터를 닫았다고 응답한 뒤 재시작을 진행했다. 종료가 끝나기 전에 열기 요청이 겹쳐 중복 인스턴스 경고가 나타났으며 사용자에게 내 실수임을 알리고 OK만 눌러 달라고 안내했다. 기존 프로세스 종료를 확인하는 첫 캐시 이동은 차단돼 아무 것도 바꾸지 않았다. 다음 시도에서는 Bee/ScriptAssemblies/ScriptMapper를 `Library/Recovery_20261017_0034`로 이동·보존했으며 SourceAssetDB/ArtifactDB는 새로 열리는 프로세스가 사용 중이라 이동되지 않았다. 해당DB가 백업/초기화됐다고 기록하지 않는다. 프로세스를 강제 종료하거나 원본 캐시를 삭제하지 않았다.

Unity pid11208/포트7800 ready 복구·컴파일false/compilationFailedfalse·콘솔오류0 확인. 깨끗한 빈 씬으로 열려 기존 전투 씬을 Editor API로 다시 열었다.

- 최초 추가 비동기 EditMode 검사는 실행기의 씬 복구 중 PlayMode 관련 예외로 중단됐다(`editmode.json`). 통과로 세지 않았다. 실제 TestRunner IsRunActive=false/Play=false를 확인한 뒤 겹치지 않게 동기로 재실행했다. 진단 도중 존재하지 않는 API 이름 조회는 컴파일 진단으로 실패했고 실제 설치 패키지의 내부 IsRunActive를 읽어 확인했다. 원인 불명의 예외를 게임 회귀 실패로 단정하거나 로그를 임의 삭제하지 않았다.
- **이번 전체 EditMode102/102 통과**: `editmode_sync.json`.
- 이어서 **이번 전체 PlayMode19/19 completed**: `playmode.json`.
- 최종 `final_editor_state.json`: 기존 전투 씬active/dirtyfalse,Play/paused/compiling/compilationFailed=false,TimeScale1/captureDelta0,원래 cameraHDRfalse/postfalse/Volume0 보존.
- `final_console_status.json` groundTruth의 consoleErrors0/consoleWarnings0. 캡처 버퍼에 남은 과거 검사 실행기 예외와 현재 콘솔 카운트를 구분한다.
- Assets/Packages/ProjectSettings Git 변경 없음. 정상 플레이/밸런스 실측/아트 자연스러움 승인이 아니다. 빌드/실행파일 실행/Notion 변경/GitHub push 없음.
