# 적·탄환 모델 풀링 구현 기록

2026-10-21 Asia/Seoul. 복구 기준 `ed81777`, 이전 제품 기준 `1f523a8`. 사용자가 [핸들/스냅샷 설계](../../superpowers/specs/2026-10-21-model-pooling-design.md)를 확인하고 진행을 승인했다. [실행 계획](../../superpowers/plans/2026-10-21-model-pooling.md). **제품 구현·정적 리뷰·컴파일은 확인했지만 Play/자동 테스트/성능 측정은 사용자 지시로 계속 보류**다.

## 구현과 API 변경

- EnemyState/ProjectileState는 어셈블리 내부의 재사용 데이터다. Session별 풀을 소유하고, 외부에는 슬롯 참조+소유 Session+64bit 세대를 확인하는 EnemyHandle/ProjectileHandle 값 타입을 준다. 재대여 때 세대와 게임 ID가 바뀐다. 세대 최대값 슬롯은 다시 보관하지 않는다.
- 게임 ID는 재사용하지 않고 핸들에 복사하므로 만료 후에도 이전 Id를 식별할 수 있다. 기본/외부 Session/회수 후 핸들은 TryRead/TryUpdate/TrySetHealth에서 실패하고 새 개체를 수정하지 않는다. 편의 읽기/쓰기 프로퍼티도 세대를 확인하며 쓰기는 Session 검증을 거친다. 완전히 만료된 상태 읽기/쓰기는 예외, Alive는 false다.
- 공개 Enemies/Projectiles는 읽기 전용 ModelList다. 원시 모델·가변 backing List를 외부로 내보내지 않는다. 진단용 목록 변경은 DebugProjectile/DebugAddProjectile/DebugReplaceProjectiles/DebugClearProjectiles/DebugRemoveEnemy/DebugReverseEnemies로 이관했다. 타깃·인덱스·호출별 임시 컬렉션은 객체 참조 대신 핸들 동등성을 쓴다.
- ProjectileLaunched는 ProjectileSnapshot, DamageApplied는 EnemySnapshot+실제 피해를 보낸다. ID/위치/수치/Spec 수치는 불변 값으로 복사한다. 저장해 둔 이벤트가 후속 Tick·Spec 변경·모델 재사용에 따라 바뀌지 않는다. 스냅샷 Handle은 만료될 수 있으며 후속 변경은 Session.TryUpdate를 통해 유효성을 확인한다. 후속 구독자도 동일한 당시 스냅샷을 본다는 점은 승인된 이벤트 API 변경이다.
- 남아 있는 탄환의 타깃 모델이 같은 틱 후반 사망하면 회수 직전에 마지막 타깃 스냅샷을 갱신한다. 이전 탄환의 표시 위치/오프셋 계산은 마지막 위치를 쓰고, 새로 같은 슬롯에 나온 적으로 이동/명중하지 않는다. ProjectileSnapshot.TargetIsValid로 현재 유효 여부와 마지막 알려진 값을 구분한다. 진단 코드에서 Spec을 null로 바꿔도 스냅샷은 HasSpec=false를 기록하여 회수 정리를 막지 않는다.

## 회수·제한·동작 보존

- 회수는 원래 목록 제거 시점에 등록한다. 최외곽 Tick/피해 이벤트/진단 작업이 끝날 때까지 모델은 격리되어 재대여되지 않는다. 이 구간의 내부 스냅샷 처리는 이전 데이터에 접근할 수 있지만 활성 TryRead/쓰기 API는 이미 회수한 핸들을 거부한다. 재진입·관찰자/처리 예외에도 finally에서 격리를 비운다. 같은 탄환이 목록에 중복 참조된 진단 사례는 membership 횟수로 이중 반환을 막는다.
- 초기화 중 예외가 나면 빌린 슬롯도 반환한다. 사망/코어 퇴장 정리, 탄환 타깃 사망/명중/전체 정리, 진단 재시작, Session.Dispose에 회수 경로를 연결했다. Prototype.OnDestroy는 Session을 Dispose한다.
- **패배 시 살아 있는 모델은 화면의 멈춘 적 본체를 유지하기 위해 즉시 회수하지 않는다.** 이미 퇴장/사망한 모델과 탄환은 정리하고, 남은 살아 있는 모델은 진단 재시작/Dispose 때 회수한다. 원래 패배 화면을 유지하는 설계 구체화다.
- 비활성 보관만 일반 병종별32/보스4·탄환256으로 제한한다. 활성 개체 수/생성/발사 수는 제한하지 않는다. 초과 보관은 버리고 Spec·타깃·출처·수치·위치·시간·버프/퇴장 상태를 비운다. 풀은 Session 간 공유하지 않는다. Active/Retained/PeakActive/Created/Reused 카운터를 제공하지만 실제 전투 값은 이번에 수집하지 않았다.
- 전투 이동/피해/발사 계산·타입→낮은HP→남은경로·최초/대상 유지·현재30R/CSV·뷰 풀·마스크·HUD/효과/사운드 수·수명/음량은 유지하는 구현이다. 제품 소스9개(신규2개 포함), 기존 검사 소스5개를 이관했다. 씬/프리팹/CSV/설정은 수정하지 않았다.

## 비용 절충과 확인한 범위

- 모델 객체 신규 할당을 풀 대여로 바꾸지만, 대기 모델 메모리·세대/소유권 검사·큰 값 스냅샷 복사·membership 사전·회수 시 남은 탄환1회 순회 비용이 추가된다. 모델 보관 제한은 전체 메모리 상한이 아니다. 컬렉션 피크 용량은 보관하고, 외부에서 저장한 핸들/스냅샷은 슬롯과 소유 Session의 수명을 늘릴 수 있다. 실제 GC/CPU/피크/잔류 메모리 개선을 보장하지 않는다.
- 기존 검사 중 모델 동일성은 핸들 동등성, 발사/피해 리플렉션 구독은 새 스냅샷으로 바꿨다. 문자열/스프라이트/포탑 참조 재사용 검사 자체는 SameAs를 유지했다. 원시 null 모델 입력은 기본 Invalid 핸들로 이관했다. 원시 모델 초기화 대신 안전한 진단 API를 쓴다.
- **새 모델 풀 회귀 검사14사례 추가/실행0:** 적 초기화/재사용/구핸들 쓰기 차단, 기본/외부 Session, 탄환 스냅샷/Spec 불변, 재사용된 적에 대한 오래된 탄환 차단, 이벤트 안 중첩 Tick/격리, 관찰자 예외, 일반/보스 보관 상한2사례, 탄환 보관 상한/Dispose, 진단 재시작, 최종 타깃 위치/슬롯 재사용, 이벤트 중 Dispose, 초기화 실패/Spec 누락 정리, 패배 시 퇴장 회수/살아 있는 본체 유지. 기존 검사 소스도 컴파일만 했고 실행하지 않았다.
- 최초 컴파일에서 검사 API 이관 오류3건을 확인해 수정했다. 중간 재컴파일 반환값에 이전 compilationFailed=true가 남았으나 후속 엔진 ground truth가 false임을 확인했다. **최종 재컴파일 completed/failed=false/errors=[]/compilationFailed=false, Console 오류0·경고10**이다. 시작 cursor 이후 경고/오류 관측484건에는 수정된 검사 컴파일 오류3·CS0618 316·UAC0005 161·기타4건이 포함된다. 변경 제품 소스 경고0, 과거 Console 이력은 지우지 않았다. 재컴파일별 Console 집계 차이를 최적화 효과로 해석하지 않는다.
- 독립 정적 리뷰에서 최종 타깃 위치 보존·패배 퇴장 회수·초기화 예외 반환·기존 재사용 assertion·최종 스냅샷 실패 시 finally 정리를 보완했고 후속 리뷰에서 남은 지적 없음. 정적 리뷰는 실행 동등성 통과를 뜻하지 않는다.
- 전후 전투씬1/dirty=true(기존 사용자 상태)·Play stopped·컴파일/domainReload=false·실행 중 테스트 없음. 씬 저장/복원 없음. 설정3파일 해시는 기존 종료 기록과 일치하며 설정2파일 변경·미추적·폰트 도구 접근오류 표시는 보존했다. 빌드·실행파일·push 없음.
- [원본 컴파일/에디터/경고·리뷰 요약](verification.json), [컴파일 소스·meta 해시](implementation_hashes.json).

|지표|변경 전|변경 후|절감량/율·판정|
|---|---|---|---|
|최소/최대/평균 FPS·프레임 ms·CPU/GC·메모리|미측정|미측정|N/A, 사용자 지시로 보류|
|실제 풀 활성/대기/피크/신규/재사용|미측정|카운터 구현·실전 미수집|N/A|
|게임/타깃/이벤트·뷰/사운드 동등성|기존 자료/코드|정적 리뷰·컴파일만 확인|실행 미검증|

검사 재개 시 위14사례와 기존 Edit/Play 검사 API 이관의 assertions, 같은 seed/입력/dt의 전투/이벤트 비교부터 수행한다. 실제 회수/재등장·패배/재시작·풀 피크/잔류·동일 조건 Profiler 전후/Memory Profiler·HUD/사운드/마스크 품질은 아직 미검증이다. [대상 QA](../TARGETED_QA.md), [최적화 규칙](../OPTIMIZATION_RULES.md), 최신 [인수인계](../UNITY_HANDOVER.md)를 따른다.
