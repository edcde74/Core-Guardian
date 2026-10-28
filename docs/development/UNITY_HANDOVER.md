# 코어 가디언 인수인계

갱신: 2026-10-28 15:40 Asia/Seoul (Claude. 맨 위 '현재 상태' 절이 최신이고 아래 절들은 이전 기록 — 아래의 '개발 보류' 문구는 10-25 당시 기록이며 10-27부터 개발 재개, 10-28 확정 기획까지 구현 완료.) 현재 사용자 지시·최신 담당 Notion 우선. [WORK_ROUTING.md](WORK_ROUTING.md)에서 필요한 분야만 읽는다. 2026-10-24 오전까지의 상세 절(마스크/B1·B2/뷰 동기화/윤곽 등)은 [이전 인수인계 스냅샷](unity_handover_snapshot_20261025_before_rewrite.md)으로 옮겼다.

## 현재 상태 (2026-10-28 15:40) — 먼저 읽기

- 코드: 브랜치 `codex/shared-body-mask`. 10-28 오후 리팩토링(사용자 확정 방향, 규칙 [REFACTORING_RULES.md](REFACTORING_RULES.md), 검토 결과 [refactor_review_20261028/FINAL.md](refactor_review_20261028/FINAL.md), 사용자 결정 [DECISIONS.md](refactor_review_20261028/DECISIONS.md)).
  - 커밋 순서: b314b90(규칙·검토) → 8007b5a(셰이더 기준 테스트) → 92c40ab(화면·입력 코드를 `CoreGuardian.Game` 어셈블리로 분리) → feca580·d08c8ea(자동 모양 정리, CSharpier) → 4eb1ef8·15228e1(큰 파일 나누기) → 2080e48(Unity 물체 `OrNull`) → 7f8c4b3(테스트 typeof) → 6e3e541·9f85750(안 쓰는 코드 삭제) → 0f32be1(버그 묶음 + 빈 바닥 클릭으로 개발 도구 닫기) → B21 셰이더 '항상 포함' → 841a0b7(성능 F1~F31) → 이 문서 커밋. GitHub 푸시 완료.
  - 리팩토링 단계(버그 묶음 전까지)는 IL 지문으로 동작이 같음을 확인. 버그·성능 묶음은 의도한 변경.
  - 테스트: EditMode 318/318, PlayMode 54/54.
- 빌드: `Builds/CoreGuardian_20261028_refactor/CoreGuardian.exe` (10-28 리팩토링·버그·성능 포함).
- 성능(에디터 Play, 28라운드·포탑 32·3배속·증강 11, 적 약 113, 350프레임, VSync 끔): FPS 약 54, 메인 스레드 약 18ms. Profiler 결과 보통 프레임 16ms = 게임 약 8ms + 에디터 약 7ms, 게임 쪽 쓰레기 할당 평균 3.4KB/프레임. 가끔 40ms 튐은 쓰레기 수거(대부분 에디터가 만든 것). 게임 쪽 큰 비용: 강조 윤곽 1.9ms, 스크립트 Update 약 3ms, 바닥·그림자 1.1ms. 실행 파일 실측은 아직 없음.
- 증강 기획 논의로 넘긴 것: B3(미래형 카드 늦게 고를수록 보정), B10(29번 적용 범위), B11(27번 처형), B19(반복 증강 합산/평균) — Notion 증강 문서 '남은 결정'.
- 남겨 둔 것: F16, F27(테스트가 음량 저장값 즉시 반영을 확인), ArtDepthSorting 합치기(앞뒤 순서 바뀔 위험), B22(Windows 영향 없음), B23(빌드 때 아틀라스 준비 코드가 자산을 다시 저장 — 빌드 후 `Sources.asset` 변경은 커밋하지 않음), X21·X22(아트 정리 때).
- 작업 팁(리팩토링 이후):
  - 코드 모양: 수정 뒤 `dotnet csharpier` (`.config/dotnet-tools.json`, `.csharpierrc.yaml` 120자). Unity 물체에는 `?.`/`??` 대신 `OrNull()`.
  - 큰 git 명령은 eval 안에서 기다리지 말고 `cmd /c ... > Logs\x.txt`로 띄운 뒤 파일을 읽는다.
  - PlayMode 테스트는 `async_tests: true`. EditMode 결과는 파일로 나오니 요약만 읽는다.
  - 성능 측정 스크립트: `Logs/claude_perf_setup.cs` → (적 쌓일 때까지 대기) → `claude_perf_start.cs` → 350프레임 뒤 `claude_perf_read.cs`, 결과 `Logs/claude_perf_runs.txt`. 측정 중에는 eval을 부르지 않는다(값이 흔들림). 에디터 GC 수치에는 에디터 할당이 섞인다 — 게임만 보려면 Profiler(`claude_prof_*.cs`).

## 이전 상태 (2026-10-28 12:30)

- 코드: 브랜치 `codex/shared-body-mask`. 로컬 커밋 순서 290822e(10-28 기획 1~4·6) → b14a22e(HUD 개편 B안) → 582e96a(사용자 확정 2건) → 이 문서 커밋. GitHub에도 푸시(github-sync 정리 커밋). EditMode 308/308, PlayMode 57/57. 커밋 안 된 사용자 파일 `held_final_*.csv`, `Assets/_Recovery/`는 건드리지 않는다.
- 10-28 기획 확정 1~6 **전부 게임 반영**: 범위 공격 70·10·0.5, 화염 화상 4초 / 약점 ×1.3·저항 ×0.7(포탑만) / 포탑별 공격 순서 CSV / 전투 중 설치·강화 허용, 49 '약점 공략' / 46 도착 폭발 / HUD 개편 B안.
- HUD 개편 결과·노션 반영 목록: [hud_redesign_applied_20261028/README.md](hud_redesign_applied_20261028/README.md). 아트 원본: [docs/art/hud_redesign_20261028/](../art/hud_redesign_20261028/README.md).
- 빌드: `Builds/CoreGuardian_20261027_feedback/CoreGuardian.exe`(10-27). 10-28 변경은 미포함 — 새 빌드는 요청 시.
- 성능: 10-27 측정(적 ~130·3배속, FPS 평균 약 61) 이후 실측 없음.
- 다음 후보(사용자가 고름): 기술병 밸런스·포탑 강화비 구조 논의(+기술병 문서 46번 반영) / 상태 표현 미적용분(격노 번쩍임·치유 +·장갑 튀김·치명타 숫자) / HUD 문서 본문 전면 정리 / 성능(그리기 횟수) / 새 빌드.
- 작업 팁: 코드 수정은 eval 문자열 치환(정확히 1곳) 또는 `write_text_file`. PlayMode 테스트는 `async_tests: true` 필수. git 커밋 메시지는 Logs 아래 파일로 쓰고 `commit -F`, GitHub 푸시는 `Logs\ghpush.bat`(-p는 직전 GitHub 커밋).

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

## 기획 상태 (2026-10-26 09:30) — 게임성 확장 버티컬 슬라이스 1~30R

모두 Notion에만 반영했고 **게임 코드·데이터에는 반영 전**이다(코드는 아직 포탑 3종·적 4종·옛 증강 18장·5라운드 증강 주기, CSV ID는 영문). 총괄 정본: [게임성 확장 로드맵](https://app.notion.com/p/403389dbfc688163ae05d0cd3b45c974).

### 확정 내용 (담당 문서)
- 포탑 6종: 기존 3 + 냉기(둔화)·화염(화상, 스택당 초당 3)·레일(관통). 설치비 60/60/75/60/65/75수정, 6종 공유 강화. 증강 45번으로 롤체식 ★ 합체(★2 ×1.8, ★3 ×3.2). 단일 고속 기본 치명타 확률 0%(증강으로만). → [타워 시스템](https://app.notion.com/p/3df389dbfc6881a2b037fbce63f2f534)
- 적 8종 + 보스: 신규 돌격병·분열체·치유사·장갑병·비행체(체력·처치 골드는 같은 라운드 공격형 배율). → [적 유형 및 보스 설계](https://app.notion.com/p/3df389dbfc6881ea9f5ec4e0f7b8084c)
- 웨이브: 신규 첫 등장 4/7/12/16/22R, 공격형 자리를 대체(라운드 합계·간격 유지). 표 검산 완료. → [웨이브 편성표](https://app.notion.com/p/3e6389dbfc6881c5aa2bed3c32047474)
- 효과 태그 7개: 둔화·화상·관통·연쇄·처형·치명타·기절(얼어붙음 포함, 보스 지속 25%·3초 면역). → [전투 피해 계산 규칙](https://app.notion.com/p/3ea389dbfc6881919546c09d4ae7ade6)
- 기술병 능동 스킬 '범위 폭발': Space, 현재 공격력 ×4, 반지름 2, 쿨 12초, 전투 단계만, 라운드 시작 시 초기화. → [플레이어 전투 및 이동](https://app.notion.com/p/3df389dbfc6881f8be26ff810ba7c18e)
- 증강: 50장(실버18·골드20·플래티넘12, 분류 5종 × 성격 5종). 등급 60/30/10, 1~30R 3라운드마다(9회)·31~100R 5라운드마다, 무료 리롤 1회, 소분류 가중치 +25%. → [증강 시스템](https://app.notion.com/p/3e6389dbfc6881bf9600d5f42fb68a44)
- 재화: 골드·수정 분리 유지, 30R 누적 처치 골드 11,923 / 완료 수정 2,295(검산 완료), 환급은 실제 낸 설치비의 50%. → [재화 시스템](https://app.notion.com/p/3e3389dbfc688160a9b7efcc83505cb3)
- 게임 루프: 코어 최대 에너지 20, 한 판 100R 약 41분. → [게임 루프](https://app.notion.com/p/3e6389dbfc68819fbfe5ca17f14460d3)
- 레벨: 경로 길이 79칸 / 경로 칸 수 80개(용어 분리), 비행 경로 약 64칸, 입구 감속 구간 좌표 정본. → [프로토타입 레벨 디자인](https://app.notion.com/p/3e9389dbfc688159ae8cec214b4e3e25)
- 데이터(2026-10-26 확정): **CSV가 원본**(엑셀 원본 없음). 시트 8개 player·enemies·towers·upgrades·augments·effectTags·skills·waves·stage(신규 player·augments·effectTags·skills). **숫자 ID 범위**: 기술병 10xxx · 적 11xxx(11001~ 일반, 11101 보스, 11201 분열 작은 개체) · 포탑 12xxx(12101 지원) · 강화 21xxx(대상×100+능력치) · 증강 22000+카드번호 · 효과 태그 31xxx · 스킬 32xxx · 한 판 설정 42xxx · 웨이브 8자리 41+라운드3+순번3. 참조 칸도 ID. 현재 CSV 상태는 문서 토글. → [데이터 테이블 명세](https://app.notion.com/p/3df389dbfc68816083ced1e50bf03c20)
- 2026-10-26 오전: 기획서 42개 교차 점검·70건 수정. 이후 그림 5개 교체(증강 등급 확률·증강 선택 흐름·웨이브 편성·누적 수입·핵심 루프, 그리기 전 표 검산), 게임 컨셉 범위표는 기획값만(코드 상태 메모 삭제).

### 아트·사운드 (2026-10-26 오전 방향 확정, 실제 시안은 아직 없음)
- 신규 아트 30개(포탑 A1~A4 · 적 B1~B7 · 기술병 C1~C2 · 효과 태그 D1~D7 · 증강 효과 E1~E4 · 카드 F1~F2 · HUD G1~G4) 항목별 방향 확정. 우선순위는 판독 기준: 신규 1 = 18개(신규 포탑·적·보스 격노·범위 폭발·둔화/화상/기절·HUD 4종), 신규 2 = 10개, 신규 3 = 2개. 아트 방향은 기존 화풍 연장(적은 일반 병사 화풍, 포탑은 기존 받침대 공유, 효과는 정지 스프라이트). F1은 기존 등급 표현 재사용, F3 성격 아이콘은 삭제(성격은 화면 미표시 내부 분류). → [전투 프로토타입 추가 아트 제작 목록](https://app.notion.com/p/3f3389dbfc6881d2a70be916ded8ddb7)
- 적은 계열 3개(공격형: 공격형·돌격병·분열체·비행체 / 수비형: 수비형·장갑병 / 버프형: 버프형·치유사)로 묶고 같은 계열끼리 색·장비를 맞춤. 규칙·수치는 그대로. → [적 유형 및 보스 설계](https://app.notion.com/p/3df389dbfc6881ea9f5ec4e0f7b8084c)
- 상태 표식 모두 방향 확정: 둔화 = 아래쪽 이중 화살표 새 시안(장화 미채택), 화상 = 불꽃 + 스택 숫자, 기절 = 머리 위 별 / 얼어붙음 = 청백 색조 + 얼음 조각, 관통·연쇄·처형·치명타는 표식 없이 짧은 효과, 머리 위 표식은 최대 3개, 보스 격노 = 붉은 번쩍임 + 발 뒤 속도선, 코어 30% 이하 HUD 1초 깜빡임(비네트 없음). → [상태 효과·위험 알림](https://app.notion.com/p/3fb389dbfc68816d96b0ed1ad4e98d8c), HUD 반영 → [HUD 화면 및 표시](https://app.notion.com/p/3df389dbfc68811f9a87e5a5ff05a571)
- 소리 23개(S1~S23)는 **실제 아트가 확정된 뒤 그 질감·모습을 보고 정함**(사용자 지시). → [전투 사운드 설계 방향](https://app.notion.com/p/3fb389dbfc68815f8923cb93f5b90743)

### 다음 순서 (사용자 지시, 앞당기지 말 것)
1. 아트 개별 시안 — 신규 1순위 18개부터. 시작 전에 시안 방식(Claude가 도트 시안을 그려 비교판 / 제작 지시서만 / 둘 다)을 질문 카드로 묻는다. 게임·Unity에는 넣지 않는다.
2. 아트 시안 확정 후 사운드 23개.
3. 그다음 개발 재개(아래 구현 목록), 그 뒤 외부 플레이테스트(로드맵 5단계). 앞 단계가 대충이라도 끝나기 전에는 플레이테스트 준비를 꺼내지 않는다.
- 개발 재개 시: 증강 effect 이름·value1~3 의미를 코드 구조에 맞춰 정함(데이터 명세 남은 결정).
- 플레이테스트로 검증할 가안: 신규 몬스터·포탑 수치, 증강 50장 수치, ★ 합체 배율, 화상 3, 처치 골드 +16%.

### 개발 재개 시 구현 목록
신규 포탑 3종·적 5종·효과 태그 7종·증강 50장·기술병 스킬·데이터 외부화와 CSV 숫자 ID 전환(stage augmentationEvery 5 → 3 포함). 순서와 담당 링크는 Notion [후속 작업 순서](https://app.notion.com/p/3f4389dbfc688171b84dc19b138925f4) 7번 '재개 시 구현 목록'.

### 기획 작업 방식 (사용자 지시)
- 한 대화에서 문서 하나씩 순서대로. 다른 문서에 바꿀 것은 지금 문서 '남은 결정'에 `[요청 → 문서명]`으로 적고 그 문서 차례에 처리.
- 제안과 확정을 구분. 선택지를 먼저 보여주고 사용자가 고른 것만 확정으로 기록.
- 사용자에게 요청·선택은 질문 카드(선택지 + 추천 표시)로, 보기 쉽고 고르기 쉽게. 답은 짧게. 노션 문서를 고치거나 언급하면 링크를 같이.
- 문서 형식: 한 줄 요약 → 용어 → 수치표 → 규칙 → 남은 결정 → 관련 문서 → 변경 이력(토글).
- 노션 그림 교체 요령(Claude): 이미지 블록은 update_content로 못 바꿔 replace_content로 페이지 전체를 다시 씀. SVG는 텍스트 글꼴 지정 방식으로 작게 만들어 notion-create-attachment(content)로 올림(샌드박스에서 Notion·S3 직접 업로드는 막힘). 그리기 전에 표 수치를 검산.

## 현재 상태 (2026-10-25 저녁, 개발)

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
- [게임성 확장 로드맵](https://app.notion.com/p/403389dbfc688163ae05d0cd3b45c974) — 기획 총괄(태그·범위·요청 목록)
- 기획 담당 문서는 위 '기획 상태' 절 링크
- [대량 전투 성능·최적화 계획](https://app.notion.com/p/3ff389dbfc6881779abdeb5eeeb1cda4)
- [게임 루프 상세 기획서](https://app.notion.com/p/3e6389dbfc68819fbfe5ca17f14460d3)
- [게임 기획서(루트)](https://app.notion.com/p/3df389dbfc6881bc858bce200d1bb742)
- [플레이테스트 및 변경 이력](https://app.notion.com/p/3df389dbfc6881fc9f1bd004adfc5999)
- [후속 작업 순서](https://www.notion.so/3f4389dbfc688171b84dc19b138925f4)

## 공통 진입 문서
[AGENTS.md](../../AGENTS.md) → 이 문서 → [WORK_ROUTING.md](WORK_ROUTING.md) → 작업별 정본. 최적화는 [OPTIMIZATION_RULES.md](OPTIMIZATION_RULES.md)·[OPTIMIZATION_PITFALLS.md](OPTIMIZATION_PITFALLS.md), 실행 검증은 [TARGETED_QA.md](TARGETED_QA.md), Git은 [LOCAL_GIT.md](LOCAL_GIT.md), 문서 흐름은 [WORKFLOW.md](WORKFLOW.md).
