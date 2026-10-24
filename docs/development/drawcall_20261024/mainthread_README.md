# 메인 스레드 분해 — 2026-10-24

조건: Editor, 적128·타워15, SRP Batcher OFF + 윤곽 출력 배칭 상태, warm15+sample90 ×3 (`x_prof_1~3.json`).
방법: `CoreGuardianPrototype.Update`와 `Session.StepTick`에 임시 ProfilerMarker(`CGX.*`)를 넣어 측정했다. 측정 후 두 파일은 원래 상태로 되돌렸다(git 변경 없음 확인).

## 프레임 전체 (PlayerLoop 7.75~8.22ms)
|구간|ms (3회)|
|---|---|
|LateUpdate 스크립트 전체|3.27~3.37|
|└ CG.Emphasis.Present (강조·공유 윤곽)|2.20~2.33|
|  └ SharedOutline.Mask|0.76~0.85|
|└ CG.PhotoShadow.Present (136개)|0.79~0.83|
|└ SpriteReferences.Read (강조+그림자 구조 검사 합계)|0.84~0.88|
|Update 스크립트 전체|2.37~2.51|
|└ Prototype.Update = Tick + Sync|약 2.2|
|렌더 콜백 Emphasis.Tint (밝기 머티리얼 적용)|0.75~0.78|
|렌더 스레드|1.0|

## Prototype.Update 내부
|구간|ms|비고|
|---|---:|---|
|**Sync (화면 동기화)**|**1.64~1.66**||
|└ 적 루프|1.15~1.17|128마리|
|  └ combatAnimation.Enemy|0.77~0.78|마리당 약 6µs (보행 Move/Present)|
|  └ 체력바|0.02||
|  └ 나머지(위치·버프 표식 등)|약 0.38||
|└ **타워 루프**|**0.40**|15기인데 기당 약 26µs. `TowerTargetForView`가 타워마다 `viewRangeIndex.Invalidate()`로 적 위치 색인을 다시 만든다|
|└ 제거·투사체·플레이어|0.08||
|**Tick (게임 규칙)**|**0.50~0.58**||
|└ 버프 범위 계산|0.135|적×버프원 거리 비교|
|└ 타워 사격|0.14||
|└ 투사체 이동|0.09~0.15||
|└ 스폰 확인|0.07~0.09|매 스텝 웨이브 표 전체 순회|
|└ 이동·색인·제거|0.05||
|입력|0.01||

## 참고
- GC 중앙값은 프레임당 약 21KB다. 일부 실행에서 한 프레임에 수 MB 할당이 간헐적으로 보였다(Editor 측 할당이 섞였을 가능성이 있어 원인 미확정).

---

# 적용 결과 (사용자 승인 "1-4-2-3 진행해")

## 변경
1. **타워 화면 동기화 색인 검증 (Session.cs, SessionModels.cs, ModelPooling.cs)**: 적 상태 쓰기·모델 작업·풀링마다 `enemyRevision`을 올리고, `ModelList.Version`은 목록 변경마다 올린다. `TowerTargetForView`는 둘 중 하나라도 바뀌었을 때만 색인을 다시 검증한다(이전에는 타워마다 적 128마리를 전수 비교). 결과는 이전과 같다(SessionTests 75/75, 위치·체력·재생·순서·제거 추적 테스트 포함).
2. **피해 숫자 준비 (CoreGuardianHud.cs)**: 준비 단계에서 시간이 실제로 흐를 때(timeScale>0, 씬 로드 1초 이후) 숨긴 HitDamage를 프레임당 2개씩 32개까지 미리 만든다. Configure 시점의 3개 준비 계약과 전투 중 초과 생성 규칙은 그대로다.
3. **구조 검사 변경 감지 (CoreGuardianHierarchyWatch.cs 신규, CoreGuardianSpriteReferences)**: 추적하는 계층의 각 노드에 감시 컴포넌트를 붙인다. 자식 추가·제거·파괴·재부모 시 루트의 Version이 올라간다. Version이 그대로면 노드별 childCount/parent 순회를 건너뛴다(렌더러 null 검사는 유지). 비활성 오브젝트·비활성 스크립트에도 메시지가 오는 것을 Play 모드 프로브로 확인했다. Edit 모드는 ExecuteAlways로 같은 경로를 탄다. 씬 오브젝트가 아니면 기존 순회를 그대로 쓴다.
4. **윤곽 마스크 (CoreGuardianSharedOutline.cs)**: 정렬 레이어 값을 프레임당 1회만 조회한다. 마스크 메시는 수집 순서로 배치하고, (스프라이트, 뒤집기) 순서가 바뀔 때만 다시 만든다. 위치·깊이 순위·알파는 버퍼로만 갱신한다. 호출부가 이미 걸러 준 activeInHierarchy 검사는 중복이라 제거했다.
   - 같은 실행에서 재사용 ON/OFF 마스크를 비교했다. 정지 상태 0 / 50,206 texel, 20프레임 이동 후 0 / 50,584 texel, 화면 0px이다.
   - 이동 중에는 메시를 다시 만들지 않았다(재구성 횟수 변화 0).

## 성능 (Editor, 적128·타워15, warm15+sample90 ×3)
|지표|이전 (e_off 2·3회)|**적용 후 (f_off 1~3)**|
|---|---:|---:|
|PlayerLoop 평균 ms|8.50 / 8.50|**7.60 / 7.04 / 7.26**|
|PlayerLoop 중앙값 ms|8.29 / 8.40|**7.11 / 6.87 / 7.06**|
|LateUpdate 스크립트 ms|3.73|**2.47~2.60**|
|└ 강조·윤곽 Present|2.61~2.64|1.83~1.95|
|└ 그림자|0.82~0.83|0.36|
|└ 구조 검사 SpriteReferences.Read|0.86~0.87|0.15~0.16|
|└ 윤곽 마스크|0.81~0.82|0.61~0.65|
|Update 스크립트 ms|2.46~2.52|2.40~2.48|
|평균 / 최저 FPS|66.2 / 45.1, 65.7 / 40.8|69.9 / 33.2, 74.2 / 43.2, 72.1 / 41.5|
|Draw / SetPass|131 / 106.5|130 / 105.5|

- 1번(타워 색인)은 측정값이 타워 목표 계산 0.235 → 0.164ms로, 예상(−0.3ms)보다 작았다. 남은 비용은 Multi 타워 12기의 범위 조회·우선순위 비교다.
- 4번(피해 숫자 준비)은 이 벤치마크가 준비 단계 없이 바로 전투를 시작하므로 측정에 나타나지 않는다. 실제 첫 라운드에서 전투 중 TMP 생성을 줄이는 변경이다.
- 회귀: EditMode 299 통과 / 4 inconclusive / 0 실패, PlayMode 53/55(기존 실패 2개 동일). 화면은 이전 실행 대비 적 무리 영역(실행마다 피격 상태가 다름) 밖에서 새 차이가 없다.

## AttackBodyBatch 제거 (사용자 승인 "2번 실시")
- SRP Batcher가 켜져 있어야 동작하는 실험 기능(기본 OFF)이었다. SRP OFF 채택으로 쓸 일이 없어져 삭제했다: `CoreGuardianAttackBodyBatch.cs`, `AttackBodyBatch.shader`, `CoreGuardianAttackBodyBatchTests.cs`, 그리고 ObjectEmphasis·OriginalWalk의 호출부.
- 회귀: EditMode 296/296 통과(inconclusive 0), PlayMode 53/55(기존 실패 2개 동일).
- 이전 측정 드라이버(Run_*, Cap*, SrpPix* 등)는 삭제한 클래스를 참조하므로 다시 쓰려면 `BenchTemplate.cs`(성능)와 `CaptureTemplate.cs`(화면)를 쓴다. 확인 실행: PlayerLoop 6.71ms.

## A1·A3·A5·A7 적용 (사용자 승인 "응 그렇게 진행해", 결정 사항은 "추천으로")
### 변경
- **A1 강조 밝기 (CoreGuardianObjectEmphasis.cs)**: 밝기 쌍둥이 머티리얼을 렌더 직전에 붙였다가 끝나면 떼던 왕복을 없앴다. 강조 중에는 쌍둥이를 계속 붙여 두고, 다른 시스템이 바꾼 머티리얼(피격 번쩍임 등)은 다음 프레임에 새 원본으로 받는다. 쌍둥이는 Play 종료까지 공유한다.
- **A3 보행 (CoreGuardianOriginalWalk.cs)**: flipX·스케일·색·정렬값을 값이 바뀔 때만 쓴다. 위치·회전은 `SetLocalPositionAndRotation` 한 번으로 쓴다.
- **A7 매 프레임 GC 제거**
  - `List.Sort(IComparer)`는 호출마다 delegate를 새로 만든다(약 128B). 적 범위 색인(타워 사격·타워 화면 동기화, 프레임당 약 21회), 윤곽 부품 정렬, HUD 피해 집계 정렬을 캐시한 `Comparison`으로 바꿨다. 같은 정렬 알고리즘에 같은 비교 함수라서 결과 순서가 같다.
  - `SystemInfo.SupportsRenderTextureFormat`는 enum 검증에 리플렉션을 써서 호출마다 할당한다(프레임당 약 14회). 기기별로 고정된 값이라 한 번만 조회한다.
  - HUD·업그레이드 가격 조회의 LINQ `FirstOrDefault`(클로저 할당)를 루프로 바꿨다.
- **A5 버프 범위**: 버프원의 위치·스펙을 쌍마다 읽지 않고 한 번만 읽는다. 거리 판정식은 그대로다. 스폰 확인 캐싱은 웨이브 표 변경 시 결과가 달라질 위험이 있어 하지 않았다.
- **테스트**: `CoreGuardianCoreAmbientPlayModeTests`는 피격 후 몸체 머티리얼이 원래 것으로 돌아오는지 본다. A1 이후에는 강조 쌍둥이가 붙어 있으므로, 쌍둥이의 원본 머티리얼로 비교하게 고쳤다(사용자 결정: 추천안).

### 결과 (Editor, 적128·타워15, warm15+sample90 ×3, `g_off_1~3.json`)
|지표|이전 (f_off 1~3)|**적용 후 (g_off 1~3)**|
|---|---:|---:|
|PlayerLoop 평균 ms|7.60 / 7.04 / 7.26|7.51 / 7.77 / 7.57|
|PlayerLoop 중앙값 ms|7.11 / 6.87 / 7.06|7.20 / 7.70 / 7.44|
|GC 할당 중앙값 (프레임당)|22.7~23.0KB|**18.7~18.8KB**|
|최저 FPS|33.2 / 43.2 / 41.5|37.7 / 45.8 / 46.3|
|강조 Tint ms|0.80~0.84|0.93|
|그림자 ms (변경 없음)|0.36|0.39|
|Draw / SetPass|130 / 105.5|130~131 / 105.5~106.5|

- 이번 측정은 손대지 않은 그림자·구조 검사도 8~10% 느렸다. 기기 전체 부하 차이로 보이며, 메인 스레드 시간의 변화는 이 잡음 범위 안이다. 이전 확인 실행 6.71ms까지 포함하면 같은 코드의 실행 간 범위는 약 6.7~7.8ms다.
- 확실히 줄어든 것은 GC다: 프레임당 약 4KB, 할당 횟수 약 40회. 할당 콜스택 재측정(`profile_20261024/alloc_stacks_allocs.json`)에서 타워 색인·윤곽 정렬·리플렉션 항목이 사라졌다. 남은 상위 항목은 엔진 네이티브(관리 코드 없음), 벤치마크가 준비 단계를 건너뛰어 생기는 TMP 피해 숫자 생성(실제 게임은 준비 단계에서 미리 만든다), Editor 도구다.
- A1은 Tint 시간을 줄이지 못했다. 렌더러마다 enabled·activeInHierarchy·sprite·sharedMaterial을 읽는 네이티브 호출이 남아 있기 때문으로 보인다.
- **회귀**: EditMode 297/297, PlayMode 53/55(실패 2개는 기존과 같은 수, `CoreFeedback…ActualCoreExit…` Expired handle, `PlayerFacing…NormalGridSteps…` launches 0).
- **화면**: 이전 최종 캡처(`final/f_off`)와 이번 캡처(`a7/f_off`)의 차이는 492px다. 적 무리의 피격 이펙트(실행마다 다름)와 코어 결정 파티클(무작위) 두 곳뿐이다. 같은 실행 안의 기존 잡음(1,287px)보다 작다.

### 측정 도구 주의
- `AllocStacks*.cs`, `ProfileLatest*.cs`와 예전 단계 드라이버 일부는 종료할 때 `CoreGuardianSharedOutline.SetEnabled(false)`를 호출했다. Domain Reload가 꺼져 있어, 이 상태가 다음 컴파일 전까지 Editor에 남는다. `AllocStacks2.cs`는 이전 값으로 복원하도록 고쳤다. 다른 옛 드라이버는 실행 후 `SetEnabled(true)`로 되돌려야 한다.
