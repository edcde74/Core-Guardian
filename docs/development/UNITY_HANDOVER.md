# 코어 가디언 인수인계

갱신: 2026-10-25 저녁 Asia/Seoul (Claude). **개발 보류 중 — 사용자가 기획 작업에 집중(2026-10-25 결정). 재개 시 아래 '미완료·다음 행동'부터.** 현재 사용자 지시·최신 담당 Notion 우선. [WORK_ROUTING.md](WORK_ROUTING.md)에서 필요한 분야만 읽는다. 2026-10-24 오전까지의 상세 절(마스크/B1·B2/뷰 동기화/윤곽 등)은 [이전 인수인계 스냅샷](unity_handover_snapshot_20261025_before_rewrite.md)으로 옮겼다.

## 작업 환경 (2026-10-24부터)

- Codex → Claude로 이동. Claude 데스크톱 앱이 이 PC와 연결된 상태에서 Unity MCP(`mcp__remote-devices__unity__*`)로 에디터를 조작한다. 에디터가 꺼져 있거나 Pipeline이 안 잡히면 `unity-cli-status`로 재확인 후 도구 목록을 새로고침한다.
- Unity 6000.6.2f1, URP 2D, D3D12, Windows. 전투 씬 하나: `Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity`.
- 주의:
  - Unity MCP `eval`은 메인 스레드 5초 제한. 오래 걸리는 일(git, zip 등)은 백그라운드 스레드/cmd로 돌리고 `Temp/*.log`로 확인한다. 타임아웃이 나면 AGENTS 규칙대로 재시도하지 말고 보고한다.
  - Play 진입 직후 eval은 5초 제한에 걸리기 쉽다(2026-10-24 1회 발생). 진입 후 잠시 기다린 뒤 호출한다.
  - Domain Reload OFF: static 값이 다음 컴파일 전까지 남는다. 옛 측정 드라이버(AllocStacks.cs, ProfileLatest*.cs, shared_outline_20261024/step2·3 등)는 종료 시 `CoreGuardianSharedOutline.SetEnabled(false)`를 호출하므로 실행 후 `SetEnabled(true)`로 되돌린다(AllocStacks2.cs는 수정됨).
  - PlayMode 테스트는 `run_tests mode=playmode, async_tests=true`. 빈 결과면 잠시 후 재시도. 테스트가 남긴 InitTestScene은 저장하지 말고 전투 씬을 다시 연다. 빈 모달 창으로 에디터가 멈추면 Esc.
  - `device_commit_files`로 같은 경로를 덮어쓸 때 반영이 안 된 적이 있다 → 쓴 뒤 내용을 확인한다.
  - Unity 재시작 시 `Temp/`가 비워진다(예: 2026-10-24의 `Temp/cg_pub.cmd`, `cg_push.cmd`는 사라짐). 재사용할 스크립트는 Temp 밖에 둔다.

## 현재 상태 (2026-10-25 저녁)

### Git
- 로컬 작업 브랜치 `codex/shared-body-mask`. 2026-10-25 저녁 커밋(성능 표시 + 빌드 전용 버그 수정 + 문서)과 GitHub 업로드 결과는 `git log -1`과 [LOCAL_GIT.md](LOCAL_GIT.md) "GitHub 업로드" 절의 최신 기록을 본다. 직전 기준 HEAD `641b881`, GitHub main `2092e20`.
- GitHub main은 로컬과 기록이 다르다(LFS 재작성). 로컬 브랜치를 그대로 push 하지 않는다. 올리는 방법은 [LOCAL_GIT.md](LOCAL_GIT.md)의 "GitHub 업로드" 절.
- 커밋에서 일부러 뺀 것(보존, 임의 삭제 금지):
  - `docs/development/outline_sampling_20261023/held_final_*.csv` 2개(테스트가 덧붙인 것으로 보임)
  - `Assets/_Recovery/`(+.meta): 2026-10-25 Unity 재시작 때 생긴 씬 복구 사본 `0.unity`. 사용자 확인 전 삭제·커밋하지 않는다.

### 2026-10-25 실행 파일 전용 버그 수정 (사용자 확인 완료)
- 실행 파일에서 설치 가능 칸(초록/빨강)이 안 뜨고 Esc 설치 취소가 안 되던 문제. 원인은 URP `default2DMaterial`이 Player에서 null → 칸 표시 생성 예외. `CoreGuardianPrototype.PlacementOverlaySource()`로 수정(Editor 동작 동일).
- 컴파일 0 오류, EditMode 300/300. 새 일반 빌드 `Builds/CoreGuardian_20261025_fix/CoreGuardian.exe`(오류 0, 약 183MB)에서 사용자가 두 버그 해결 확인.
- 같은 날 전체 스크립트 점검(읽기만): Editor/빌드 차이 후보와 매 프레임 멈춤 구조 목록. 정본: [build_diff_audit_20261025/README.md](build_diff_audit_20261025/README.md).
- 실행 파일 첫 전투 기록(3배속 5~30라운드, 최대 적 60·포탑 27): 144FPS(VSync 상한) 유지, 1% low 약 137. 측정 모드가 처음 5초만 켜져 실제 전투 CPU 비용은 모름.

### 2026-10-24 최적화 결과 (Editor, 적128·포탑15, 1920x1080, warm15+sample90)
- Draw 1587 → 약 130, SetPass 199 → 약 106 (SRP Batcher OFF + 공유 윤곽 출력 배칭).
- PlayerLoop 약 13ms(오전) → 8.5 → 7.0~7.8ms, GC 약 23 → 19KB/프레임.
- 핵심 구조
  - `UniversalRP.asset useSRPBatcher=false`. 동적 배칭이 renderer.color를 8비트 정점색으로 구워 1 초과 값이 잘림 → 강조 밝기(×1.15)는 쌍둥이 머티리얼(_Color) 방식(`CoreGuardianObjectEmphasis.cs`), 강조 중 쌍둥이 유지(A1).
  - 공유 윤곽: `CoreGuardianSharedOutline.cs` + `SharedOutlineMask/Output.shader`. 화면 마스크 1장(개체 ID), 출력은 1x1 흰 스프라이트(ID=alpha). 선택·강화 윤곽은 기존 `CoreGuardianSelectionOutline` 경로.
  - `CoreGuardianSpriteBatchCompat.cs`(HDR 정적 스프라이트·스프라이트 셰이더 메시 보정), `CoreGuardianSpriteBlocks.cs`(SRP OFF에서 HasPropertyBlock 항상 true 대응), `CoreGuardianHierarchyWatch.cs`(구조 검사 생략). AttackBodyBatch 삭제.
  - A7: `List.Sort(IComparer)` 호출마다 delegate 할당 → 캐시 Comparison, SupportsRenderTextureFormat 캐시, HUD LINQ 제거. A5: 버프 오라 위치·스펙 한 번만 읽기.
- 준비 시간 규칙: stage.csv `preparationSeconds 30`, `preparationSecondsLong 45`. 보스 라운드 직전과 증강 선택 직후 45초, 증강 30초는 준비 시간 소모 안 함. `CombatData.PreparationFor / IsBossWave`, 테스트 추가.
- 테스트: EditMode 300/300(CoreGuardian 필터), PlayMode 53/55. 실패 2개는 기존부터: `CoreFeedback…ActualCoreExit…`(Expired enemy handle), `PlayerFacing…NormalGridSteps…`(launches 0).
- 정본: [drawcall final](drawcall_20261024/final/README.md), [메인 스레드](drawcall_20261024/mainthread_README.md), [SRP OFF 원인](drawcall_20261024/srp_off_cause.md), [프로파일](profile_20261024/README.md). 측정 도구: `drawcall_20261024/BenchTemplate.cs`(1행 라벨만 바꿔 `Run_g_off_N.cs`처럼), `CapA7.cs`/`CaptureTemplate.cs`(화면), `AllocStacks2.cs`(할당 콜스택).

### 2026-10-24 저녁 — 빌드용 성능 표시·기록 (사용자 승인·구현·빌드 완료)
- 일반(non-development) 빌드에 들어가는 오른쪽 위 성능 표시 + CSV 기록. F3 표시, F4 기록, F5 측정 모드(VSync 끔·프레임 제한 해제). 상세·검증·한계는 [perf_overlay 정본](perf_overlay_20261024/README.md).
- 빌드: `Builds/CoreGuardian_20261024_perf/CoreGuardian.exe`(일반 빌드, 174MB, 오류0), zip `Builds/CoreGuardian_20261024_perf.zip`.
- 사용자가 2026-10-25 16:32 빌드 실행·기록 2회(준비 단계만, 전투 없음, 측정 모드 ON): 평균 FPS 약 1074~1374, CPU 메인 0.5~0.7ms. **일반 빌드에서 GC 칸은 "-"**(Unity 카운터·대체 API 모두 값 없음). 17:14 전투 기록은 위 '버그 수정' 절과 정본 참고.

## 미완료·다음 행동 (우선순위, 모두 실행 전 사용자 승인 필요)

1. 빌드에서 전투 구간 재실측: **F5 측정 모드를 켠 채** F4 기록(10-25 기록은 VSync에 묶여 CPU 비용을 못 봄). 목표 조건(적 약 130 + 포탑 32, 3배속)까지 → 요약을 Editor 값(PlayerLoop 7~7.8ms)과 비교. 사용자가 직접 플레이·기록하거나, 실행파일 실행을 명시 요청할 때만 Claude가 실행.
2. 점검 결과 후속(각각 방법 먼저 보고): ① 빠른 윤곽·마스크 경로의 정확히 1920×1080 조건(720p 최소 사양 측정 전에 필요) ② 코드 생성 SpriteRenderer 기본 재질 지정 ③ 매 프레임 멈춤 구조 방어. 목록은 [build_diff_audit_20261025](build_diff_audit_20261025/README.md).
2-1. 성능 표시 후속(선택): VSync 중 CPU 메인 칸에 대기 포함 표시/보정, 일반 빌드 GC 대체 측정 방법.
3. 남은 메인 스레드 비용: 강조 Tint 약 0.9ms, 강조 Present 약 1.9ms, 적 보행 약 0.8ms — **고치기 전에 줄일 방법(선택지)을 먼저 보고**(사용자 선호).
4. 기존 PlayMode 실패 2개 원인 조사.
5. 목표 측정 조건으로 Editor 기준값 재측정(승인 후).
6. 진행 목록 옛 항목 #5~#7(체력바 레이어·그림자 정렬·아틀라스)은 현재 방식으로 대체되어 미진행 — 삭제 여부 사용자 확인.
7. (완료 2026-10-25) Notion [플레이테스트 및 변경 이력](https://app.notion.com/p/3df389dbfc6881fc9f1bd004adfc5999)·[대량 전투 성능·최적화 계획](https://app.notion.com/p/3ff389dbfc6881779abdeb5eeeb1cda4)·[후속 작업 순서](https://www.notion.so/3f4389dbfc688171b84dc19b138925f4)에 10-24~25 작업 기록.

## Notion (수정·참조 시 링크를 함께 적는다)
- [대량 전투 성능·최적화 계획](https://app.notion.com/p/3ff389dbfc6881779abdeb5eeeb1cda4)
- [게임 루프 상세 기획서](https://app.notion.com/p/3e6389dbfc68819fbfe5ca17f14460d3)
- [게임 기획서(루트)](https://app.notion.com/p/3df389dbfc6881bc858bce200d1bb742)
- [플레이테스트 및 변경 이력](https://app.notion.com/p/3df389dbfc6881fc9f1bd004adfc5999)
- [후속 작업 순서](https://www.notion.so/3f4389dbfc688171b84dc19b138925f4)

## 공통 진입 문서
[AGENTS.md](../../AGENTS.md) → 이 문서 → [WORK_ROUTING.md](WORK_ROUTING.md) → 작업별 정본. 최적화는 [OPTIMIZATION_RULES.md](OPTIMIZATION_RULES.md)·[OPTIMIZATION_PITFALLS.md](OPTIMIZATION_PITFALLS.md), 실행 검증은 [TARGETED_QA.md](TARGETED_QA.md), Git은 [LOCAL_GIT.md](LOCAL_GIT.md), 문서 흐름은 [WORKFLOW.md](WORKFLOW.md).
