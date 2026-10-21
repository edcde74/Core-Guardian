# 코어 가디언 인수인계

갱신: 2026-10-22 Asia/Seoul. 현재 사용자 지시·최신 담당 Notion 우선. 필요한 분야만 [WORK_ROUTING.md](WORK_ROUTING.md)를 따른다.

## 현재 상태와 승인 범위

- 2026-10-22 사용자 명시 요청으로 A/B 복원 상태의 Windows64 일반 빌드를 생성했다. `Builds/CoreGuardian_Windows_20261022_062018.zip` 및 동명 폴더의 `CoreGuardian.exe`. BuildReport Succeeded/오류0/경고499, 35.383초. 실행파일은 실행하지 않았다. 사용자 씬·설정6파일 해시 동일, Play stopped/컴파일 false. 원본 보고서: `Logs/CoreGuardian_build_report_20261022_062018.json`. 이는 빌드 성공 확인이며 실행 품질 검증은 아니다.

- **A/B 렌더 배칭 작업 중지·구현 전 복원 완료**(2026-10-22 사용자 요청). A 후보는 픽셀 회귀/성능 악화로 미채택·제거했고, B는 신규 검사만 작성한 단계에서 제거했다. 제품 소스/리소스/기존 검사는 A/B 전 `1278449`와 차이가 없다. 계획·조사·실험 원본은 기록으로만 보관하며 A/B를 자동 재개하지 않는다. [복원 기록](render_batching_20261022/ROLLBACK.md).
- SRP OFF 진단도 미채택·원복 상태다. 이번 복원에서 Play stopped·scene clean을 확인하고 기존 Profiler buffer 및 driver true/Profiler OFF를 복원했다. 사용자 씬/설정4파일은 실험 전 해시와 동일하다. 추가 Play·테스트·빌드·실행파일 실행·push는 하지 않았다. [복원 확인](render_batching_20261022/rollback_state.json).
- 구현·검증 완료 커밋 **`5943523`**, 브랜치 `codex/shared-body-mask`. 구현 기준 `51fb785`, 누적 최적화 비교 전 `5914347`. 최신 결과 정본은 [누적 런타임 검증](runtime_validation_20261021/RESULTS.md)이다.
- 사용자 승인으로 HUD/표시·적 본체·모델 풀링, Session 버퍼/탐색/타깃/탄환 제거·캐시 최적화를 반영했다. 이후 사용자가 Play·자동 테스트·성능 측정 보류를 해제해 검증했다. 전투 우선순위는 타입→낮은 HP→남은 경로, 최초 선택/현재 대상 유지 규칙을 보존한다.
- QA에서 보스 없음 상태의 기본 EnemyHandle을 null과 비교하던 HUD 오류를 수정했다. 새 회귀검사와 현재 경로/좌표·스냅샷 API에 맞춘 기존 검사 보완을 포함해 **EditMode127 + PlayMode42 =169개 최종 통과**. 첫 실패와 수정 내용은 결과 정본에만 기록한다.
- 같은128적/실제15포탑·고정dt·주기적 교체 주입·3회에서 이벤트 transcript/최종 상태가 일치했다. native542frame/조건, 실제 Tick540개/조건을 분석했다. Tick38.616%·View9.599%·Main Thread GC.Alloc/frame61.504% 감소. 평균FPS30.169→30.026(−0.476%)로 **FPS 개선은 확인되지 않았다**. 정상30R 자연 플레이/장시간 성능이나 전체 품질 통과로 확대하지 않는다.
- 사용자가 승인한 **원본 백업 후 현재 전투씬 저장**을 실행했다. 저장된 사용자 씬은 보존하고 완료 커밋에서 제외했다. 사용자 변경은 현재 전투씬과 `PackageManagerSettings.asset`/`TimeManager.asset`에 남아 있으며 임의 복원하지 않는다. 미추적·폰트 도구 접근오류 표시도 보존한다.
- 비교 소스25개를 바이트/해시로 백업·임시 비교·복원했고 이후 Runtime 검사 기대값을 보완했다. 설정4파일과 저장된 씬은 비교 전후 해시가 같다. 원본/현재 씬 백업, 소스 복구 사본, native `.data`는 `Logs/CG_RuntimeValidation_20261022/`에 보존했다. [해시 manifest](runtime_validation_20261021/source_manifest.json), [복원 절차](runtime_validation_20261021/SwapSources.ps1).
- 누적 런타임 검증 종료 상태는 전투씬1/clean·Play stopped·컴파일/domainReload false·콘솔 오류0. Profiler OFF/driver 원래true·배경 실행false·시간1/capture0·autotick true/16ms 복원. [종료 원본](runtime_validation_20261021/final_state.json). `1278449` 인수인계 정리는 문서만 수정했다. 이후 렌더 진단의 최신 종료 상태는 위 렌더 정본을 따른다. 재개 시 현재 상태를 새로 확인한다.
- 빌드·실행파일 실행·push·CLI 업데이트는 하지 않았다. 해당 동작은 별도 명시 승인 없이 실행하지 않는다. 이번 인수인계 요청은 추가 Play/검사 권한이 아니다.

## 미완료와 다음 행동

1. 실제 HEAD/diff·사용자 씬/설정·미추적·연결/씬/dirty/Play/컴파일·다른 검사부터 확인한다. 사용자 Play/다른 검사를 중단하거나 겹치지 않는다. 이미 통과한169개 검사를 이유 없이 반복하지 않는다.
2. A/B는 사용자 중지·복원 지시로 보류한다. 기존 설계/계획 승인을 근거로 자동 재개하지 않는다. 후속 작업은 새 사용자 지시에 따른다.
3. [검증 결과](runtime_validation_20261021/RESULTS.md)와 [분석 원본](runtime_validation_20261021/analysis.json)에 남은 정상30R·장시간 실제 부하, GPU/Render Thread 병목, Memory Profiler/최종 잔류 검증을 유지한다. 목표기기/FPS/배속 예산은 미확정이다. 주입 결과만으로 FPS 개선이나 잔류 메모리 절감을 주장하지 않는다. 현 D3D12 Editor는 GPU Profiler supported=false, Memory Profiler package 미검출이며 설치/그래픽 API 변경을 자동 수행하지 않았다.
4. 모델 풀링 직전 버전 단독 효과, 모든 재등장 효과·전후 픽셀, 실제 조작·청취는 남아 있다. 이미 구현된 풀링/캐시를 중복 구현하거나 현재30R 물량/성장·아트·효과 수/수명·사운드를 임의 줄이지 않는다.
5. 본체 마스크 공유는 기존에 검증한1920×1080·줌8.5/4.25·기본 직교/정수 경계 등 적용 제한을 유지한다. 범위 밖은 기존 합성이다. 사용자는 직접 본 범위에서 깜박임·잘림·방향 이상이 없다고 보고했다. 보스 전체 합성 미확정9건과 원래 순간 지연은 별도 미완료다.

## 필요한 정본

- [최적화 규칙](OPTIMIZATION_RULES.md), [대상 QA](TARGETED_QA.md), [로컬 Git](LOCAL_GIT.md), [작업 흐름](WORKFLOW.md).
- [최신 측정/실패/한계](runtime_validation_20261021/RESULTS.md), [검사 집계](runtime_validation_20261021/test_summary.json), [재분석](runtime_validation_20261021/Analyze.ps1), [진단 코드](runtime_validation_20261021/RuntimeProbe.cs).
- [모델 풀/API 수명](model_pooling_20261021/README.md), [적 본체 풀](enemy_body_pool_20261021/README.md), [초기 표시/Session 재사용](runtime_reuse_20261021/README.md), [HUD 갱신](hud_refresh_reuse_20261021/README.md), [남은 런타임 구현](remaining_runtime_20261021/README.md).
- [타깃/노션 근거](target_range_20261021/README.md), [버프 원천](buff_sources_20261021/README.md), [탄환 제거](projectile_removal_20261021/README.md).
- [본체 공유 적용 제한](shared_body_mask_20261021/RESULTS.md), [보스 등 기존 미확정](mask_validation_20261021/README.md), [현재30R 기준](aggressive_30r_20261020/README.md).
