# 포탑 접힘 요약 표시줄 제거

2026-10-17 사용자 첨부 화면의 `포탑 #1 · 능력치` 작은 표시줄과 오른쪽 재개방 손잡이를 제거했다. 복구 기준 HEAD `9429264`이며 수정 전 Assets/Packages/ProjectSettings 추적 파일 변경과 staged 변경은 없었다. 빈 checkpoint는 만들지 않았다. 에디터의 전투 씬은 이미 dirty=true였으며 저장하지 않았다.

## 원인과 수정

설치 성공 후 선택된 포탑이 남고 큰 창이 닫힌 상태에서 `RefreshInspectors`의 `!details && hasTarget` 분기가 예전 `InspectorSummary`를 표시했다. `CoreGuardianHud.Inspector.cs`에서 요약 바·라벨·`InspectorReopen` 생성과 표시 갱신을 제거했다. 설치/선택 해제 정책, 능력치 A안, 실제 능력치 창의 접기 손잡이, 설치 심볼 B안, 피해 HUD는 변경하지 않았다. 능력치 확인은 기존 월드 포탑 클릭 경로를 유지한다.

기존 PlayMode 검사의 요약 바 접근은 제거된 객체를 역참조하지 않고 객체 부재를 확인하도록 갱신했다. 새 테스트를 추가하지 않았다.

## 확인 범위

- 소스에서 요약 바/재개방 손잡이 생성·참조 제거를 확인하고 수정 파일의 `git diff --check`를 통과했다.
- AssetDatabase.Refresh 후 Unity compiling=false/compilationFailed=false. 로드된 HUD 형식에서 요약 바·라벨 필드 부재와 기존 접기 필드 유지를 읽기 조회했다.
- 현재 콘솔 오류0/경고51. 보관 버퍼의 과거 오류와 구분한다.
- Play=false, 전투 씬 하나·dirty=true 유지. 사용자 미저장 씬은 저장하거나 되돌리지 않았다.
- 이번에는 Play 재현/수정한 PlayMode 테스트 실행/화면 재캡처를 하지 않았다. 실제 입력 기능 QA·사람 플레이·전체 회귀·밸런스 결과가 아니다.

씬 YAML/원본 아트/병행 강화 성공 B·VFX/후처리 변경, 빌드/실행파일 실행/push 없음. 기존 미추적 자료와 font_tools_vendor 접근 문제를 보존했다. 기존 HUD 담당 기록의 접힘 요약 바 유지 설명은 이번 사용자 제거 지시로 대체한다.
