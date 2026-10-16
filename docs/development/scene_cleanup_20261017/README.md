# 전투 씬 하나만 유지

2026-10-17. 사용자 요청: 마지막 작업한 `CoreGuardian_CombatPrototype_20261008` 외 모든 씬을 삭제하되 삭제 전에 Git에 보관한다. 이전 SampleScene/비교 씬 보존 지침은 이 직접 지시 범위에서 변경했다. 원본 프리팹·아트·게임 코드는 보존한다. GitHub 원격은 없으며 push하지 않는다.

## 삭제 전 보관

- 시작 HEAD `fe7a038`,Assets/Packages/ProjectSettings 미커밋 변경 없음. Unityready/전투 씬active·loaded/dirtyfalse,Playstopped/compilingfalse 및 직전 검사completed를 확인했다.
- 에디터 AssetDatabase로12개 씬을 확인하고 남길 씬/삭제11개/빌드목록/종속템플릿을 before.json에 저장했다. 모든 씬·메타와 종속템플릿26개 파일의 SHA256을 before_hashes.json에 보존했다.
- **삭제 전 복구 커밋 `a306a60f0536e00c82b3584a969457789267d0b1`**. 모든26개 파일이 해당 커밋에 실제 존재하는지 git cat-file로 확인한 뒤 삭제했다.

## 정리

- 기존 비교8개/SampleScene/Assets/_Recovery/0/URP2DSceneTemplate 씬 총11개를 에디터 `AssetDatabase.DeleteAsset`으로 메타와 함께 삭제했다. 정확한 목록은 after.json.
- 삭제한 URP2DSceneTemplate만을 참조하던 `Assets/Settings/Lit2DSceneTemplate.scenetemplate`과 메타도 삭제했다. 끊어진 템플릿을 남기지 않기 위한 종속 정리다.
- EditorBuildSettings의 과거 비교 씬 항목을 에디터 API로 제거해 활성 전투 씬 한 개만 남겼다. 빌드/실행은 하지 않았다. 씬 YAML을 직접 편집하지 않았다.
- AGENTS.md와 현재 인수인계에 이번 사용자 지시와 복구 기준을 반영해 삭제한 비교 씬을 다음 작업에서 임의 재생성하지 않게 했다. 상세 과거 문서의 비교 씬 경로는 이력으로 남긴다.

## 검증

남긴 전투 씬 원본 SHA256 동일,삭제한 씬 GUID를 참조하는 나머지 Assets/ProjectSettings 직렬화 파일0개를 확인했다(`verification.json`). 검사 도중 Unity Test Runner가 만든 InitTestScene은 임시 실행기 씬이며 건드리지 않고 검사 종료 후 사라지는 것을 확인했다. 부분 이름/전체 이름/필터 없는 PlayMode 시도 모두0건completed를 반환해 **이번에 자동 회귀를 통과했다고 말하지 않는다**(`playmode_focused.json`,`playmode_focused_final.json`,`playmode_all.json`). 추가 실행을 반복하지 않았다.

대신 자체 Play로 실제 전투 씬을 시작해 Ready=true/HUD=true/1라운드 Preparation을 확인하고 종료했다(`runtime_smoke.json`). 강제 처치/재화 주입/전투 수치 변경 없음. 이는 시작 smoke 확인이며 전체 라운드 회귀/사람 플레이/밸런스 실측은 아니다. 최종 씬1개/빌드목록1개/원래 전투 씬active·dirtyfalse/Play중지/compilingfalse/TimeScale1을 `final_editor_state.json`에 확인했다. 현재 콘솔오류0(`final_console_status.json`).

복구가 필요하면 작업 상태를 먼저 보존한 뒤 위 커밋에서 삭제한 씬과 `.meta`를 함께 복원한다. 원본 프리팹/리소스는 삭제하지 않았으므로 기존 GUID로 연결할 수 있다. 별도 사용자 요청 없이 git restore/reset을 실행하지 않는다.
