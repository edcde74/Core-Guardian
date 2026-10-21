# 적 본체·체력바 풀링

2026-10-21 Asia/Seoul. 복구 기준 `afb7813`. 사용자가 적 본체 풀링 진행을 요청하고, 병종/원본 프리팹 일치·수명 초기화·비활성 보관 상한의 구체안을 승인했다. 앞선 지시대로 새 Play·자동 테스트·성능 측정은 보류했다. 이번은 코드 반영·컴파일·정적 리뷰까지이며 실행 동등성/성능 통과가 아니다.

## 구현 범위

- `CoreGuardianPrototype`: 활성 적 ID별 본체 기록과 병종별 대기 풀을 소유한다. 본체와 생성된 체력바를 함께 재사용하며 기존 `enemyViews`/`healthViews` 조회를 유지한다. 같은 병종과 같은 프리팹 참조만 재사용하고, 파괴/손상되었거나 프리팹이 다른 대기 객체는 폐기한다. 원래 root 회전/배율을 보관하고 재등장 위치·이름·체력을 새 상태에 맞춘다.
- `CoreGuardianCombatAnimation`: 회수 시 이전 ID의 animation/previousEnemies/pendingHits 참조를 제거한다. 재등장 ID는 첫 위치로 새로 바인딩하므로 이전 개체 위치로부터 이동량을 계산하지 않는다.
- `CoreGuardianOriginalWalk`/`CoreGuardianUnitAnimation`: 보행 위상/보간량·방향/미러·기본 색·표시 rig·발사/조준/피격 플래그를 새 적의 초기 상태로 되돌린다. 원본과 방향별 스프라이트/프리팹·발 접촉 기준·보행 공식은 유지한다.
- `CoreGuardianHitFlash`: 기존 재질/PropertyBlock을 복원하고 이전 적의 피격 재발동 cooldown을 초기화한다.
- `CoreGuardianObjectEmphasis`/`CoreGuardianUpgradeFeedback`: 적 계층 밖의 강조·스캔 host를 회수 시 대상별로 해제한다. 같은 프레임에 root가 새 ID로 재사용돼도 이전 대상 표시가 이어지지 않도록 한다.
- 그림자는 기존 `CoreGuardianPhotoShadow.OnDisable`의 contact binding 해제와 cast 비활성화를 사용한다. 재등장 때 기존 `InvalidateSources` 경로로 소스/마스크 geometry를 갱신하며 공유 마스크의 적용 제한은 변경하지 않았다.

**활성 적 수에는 상한을 추가하지 않았다.** 비활성 보관은 Attack/Defense/Buff 각각32, Boss4, 합계 최대100본체다. 넘친 회수분은 삭제한다. 풀 부모는 소유 Prototype 아래 런타임 비활성 객체이며 씬/프리팹 파일에 저장하지 않는다. 소유자 종료 때 활성·대기 본체를 정리한다. Destroy 지연 중 객체는 대기 슬롯 수와 구분해야 한다.

대기 본체는 보행 계층·체력바·공유 리소스 참조를 보관한다. 비활성화로 메모리가 즉시 줄어든다고 주장하지 않는다. 체력바의 기존 공통 sprite 캐시는 대기 본체 수명 동안 유지될 수 있다. 실제 메모리/피크/잔류는 미측정이다.

`EnemyState`·`ProjectileState` 모델 객체와 ID 생성·CSV·이동/공격/드롭/보스 알림 정책은 변경하지 않았다. 보스 알림은 새 ID 생성 표시 시 기존처럼 발생한다. 버프 aura는 기존 별도 표시/정리 경로를 사용한다. 이전 HUD/효과/Session 구현은 [후속 재사용 정본](../runtime_reuse_20261021/README.md)을 따른다.

## 실제 확인

- 수정 제품 소스7개에 대해 `git diff --check` 통과.
- 독립 읽기 전용 코드 리뷰: 같은 프레임 회수/재사용, pending-hit ID 정리, 보행/피격 초기화, 체력바, 외부 강조/스캔, mask 해제/소스 갱신, 프리팹/병종 일치, 상한/종료 정리에서 수정 필요 사항 보고 없음. 실행 증거는 아니다.
- 공식 Unity MCP `recompile(focus=false)`: completed, failed=false, errors=[], compilationFailed=false. [검증 기록](verification.json), [소스 해시](implementation_hashes.json).
- 재컴파일 전 Console ground truth 오류0/경고117, 후 오류0/경고5. 이번 cursor 이후 수집은 기존 CombatArt의 CS0618 2건과 계정 API 접근 지연1건이다. 분석기 경고의 재출력 범위가 다르므로 경고 감소를 제품 수정 성과로 쓰지 않는다. [원본](compile_warnings.json).
- Unity6000.6.2f1, 전투씬1, dirty=true(수정 전부터 사용자 상태), Play stopped/컴파일·domainReload=false, 실행 중 Unity 테스트 없음. 씬 저장/전환·새 Play·Profiler 조작·빌드·실행파일·push 없음. [종료 조회](final_state.json).
- TimeManager/PackageManagerSettings/ProjectSettings 3파일 SHA256은 기존 본체 공유 종료 기록과 일치했다. 사용자 설정2파일 변경·미추적 자료·폰트 도구 접근오류 표시는 보존했다.

## 미검증과 나중에 확인할 항목

사용자 지시로 전후 계측·계약 검사·실제 플레이를 수행하지 않았다. 아래 값은 모두 **미측정**, 절감량/율은 N/A다. 과거 마스크 공유 성과를 이번 풀링 수치로 쓰지 않는다.

|항목|현재 판정|
|---|---|
|전후 최소/최대/평균 FPS·프레임 ms 분포·CPU/GC|미측정|
|생성/회수 비용·신규 확장/대기·활성 최대 수|미측정|
|대기 본체 메모리·전체 피크·재시작/종료 잔류|미측정|
|재사용 전후 픽셀·체력바·방향·피격·강조·접촉 그림자|실행 미검증|
|정상30R·장시간·새 ID와 이벤트/사운드 동등성|실행 미검증|

검사를 재개할 때는 병종별 생존→피격/보행/수직 전환→사망/퇴장→같은 프레임 재등장, 새 ID/이전 위치/cooldown, 체력바 전체/부분 체력, 프리팹 교체/파괴된 대기 객체, 대기100개 및 초과 회수, 진단 리셋/씬 종료를 우선한다. 기존 정적 검사가 매번 GameObject 파괴를 가정하는지도 새 회수 계약과 대조해야 한다. [대상 QA](../TARGETED_QA.md)와 [최적화 규칙](../OPTIMIZATION_RULES.md)을 따르며 같은 검사를 이유 없이 반복하지 않는다.
