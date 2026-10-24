# 공유 윤곽 채택 + 다음 최적화 시도 — 2026-10-24

## 채택 (사용자 "새방식 채택할게")
- `CoreGuardianSharedOutline.Enabled` 기본값을 **true**로 바꿨다. `SetEnabled(false)`로 원본 경로에 돌아갈 수 있다.
- 채택 상태 회귀: EditMode **303/303 통과**. PlayMode 55개 중 53개 통과, 2개 실패:
  - `CoreGuardianCoreFeedbackPlayModeTests.ActualCoreExitKeepsDamageAndExistingHit_…` (Expired enemy handle)
  - `CoreGuardianPlayerFacingPlayModeTests.NormalGridStepsDriveOriginalLegs_…` (Expected > 0, was 0)
  - 토글 OFF(원본 경로)로 PlayMode 55개를 다시 돌려도 **같은 2개가 같은 메시지로 실패**했다(실행 중 토글 false 확인). 따라서 이번 변경 이전부터 있던 실패로 판단한다. 원인은 이번 범위에서 조사하지 않았다.
- 전체 EditMode 실행 후 기존 추적 파일 `docs/development/outline_sampling_20261023/held_final_logical_reads.csv`·`held_final_pixels.csv`가 수정됨(테스트 출력 재기록으로 추정). 임의 복원하지 않았다.

## 프로파일 1회 (Profiler 녹화 ON, warm15 + 30프레임, 공유 ON) — 비용 위치 파악용, 녹화 부하 포함이라 비교값 아님
|구간|ms/프레임|
|---|---:|
|CoreGuardianObjectEmphasis.LateUpdate|4.04|
|└ SharedOutline.Mask → **Execute(DrawRenderer 제출)**|1.92 → **1.67**|
|└ SharedOutline.Collect (부품 수집 145회)|0.97|
|└ SpriteReferences.Read (강조)|0.36|
|CoreGuardianPhotoShadow.LateUpdate (136회)|1.03 (그중 SpriteReferences.Read 0.69)|
|CoreGuardianPrototype.Update|3.14 (하위 마커가 거의 없어 내부 분해 불가)|
|렌더(DoRenderLoop)|1.44 (그중 Emphasis.Tint 0.43)|

[원자료](profile_shared_hierarchy.json). 프로파일러 버퍼는 지우지 않았다.

## 시도: 마스크 v2 — 미채택, 원본(채택 v1)으로 복구
- 내용: 개체별 고정 머티리얼(개체 ID를 material 값으로 고정)로 command buffer 안의 개체 ID 전역값 쓰기를 없앴다. 정렬 레이어 값 캐시, z를 bounds에서 읽기, 중복 activeInHierarchy 검사 제거도 함께 적용했다(묶음 변경).
- 픽셀: 같은 고정 장면의 마스크 RT를 v1과 비교한 결과 **텍셀 차이 0**(50,206 / 50,206). 전체 화면 차이 423px은 실행마다 달라지는 피격 효과이고, 실행 간 원본끼리도 613px 차이가 난다.
- 성능 (v2 각 3회 + 원본 대조 1회, 같은 드라이버):

|지표|v1 3회 (3단계)|v2 3회|원본 대조|
|---|---|---|---|
|PlayerLoop ms|10.23 / 9.93 / 10.12|10.59 / 9.91 / 8.94|13.62|
|SharedOutline.Mask ms|1.75 / 1.70 / 1.90|2.06 / 1.85 / 1.74|—|
|SetPass|840~841|840~842|188.5|

- 판정: **변동 범위 내, 개선 미확인.** SetPass도 그대로였다. 비용의 본체는 전역값 쓰기가 아니라 **부품 약 640개를 하나씩 그리는 DrawRenderer 제출 자체**(Execute 1.6~1.9ms)다. 머티리얼 145개만 늘어나므로 채택하지 않았다. 제품 코드는 v1(+세부 마커 4개)로 되돌렸다. 후보 소스는 [candidate_v2/](candidate_v2/)에 있다.
- 유지한 변경: 세부 ProfilerMarker 4개(`CG.SharedOutline.Collect/Sort/Record/Execute`)만 남겼다. 동작 변화는 없다.

## 다음 후보 (근거와 예상, 승인 필요)
1. **마스크 그리기 일괄화:** 부품 640개를 기존 MaskAtlas(Texture2DArray)와 instancing 또는 합친 메시로 몇 번의 draw에 그린다. 대상 비용은 Execute 1.6~1.9ms와 SetPass +650이다. 아틀라스에 없는 방어형·보스 부품은 기존 DrawRenderer로 처리한다. 픽셀 위험은 중간 수준이다(아틀라스 UV 반올림. 기존 B에서 동등성 검증 이력 있음).
2. **CoreGuardianPrototype.Update 3.1ms 분해:** 마커가 없어 원인을 모른다. 일반 Profiler에 마커를 추가해 분해한 뒤 후보를 정한다.
3. **SpriteReferences.Read 중복:** 그림자 0.69 + 강조 0.36ms. 매 프레임 계층 전체를 구조 검사한다. 검사 의미(콜백·구조 변경 감지)를 바꾸지 않는 공유 방법이 필요해서 설계 검토가 먼저다.

---

# 후보 1 실행 — 마스크 일괄 그리기 (채택, 2026-10-24)

사용자 "1번 진행해" 승인.

## 구현
- `CoreGuardianSharedOutline`: 부품을 **스프라이트 텍스처별로 묶어** 한 동적 메시의 submesh로 만든다. `DrawMesh`는 텍스처 그룹 수만큼만 호출한다. 부품별 행렬 행·개체 ID·깊이·알파는 StructuredBuffer(`_CGMaskParts`)로 넘긴다. 부품 정점은 `Sprite.vertices/uv/triangles`를 스프라이트별로 캐시해 쓰고, flip은 부호만 바꿔 적용한다.
- 그리는 순서가 바뀌어도 결과가 같도록, 기존 시각 정렬 순위를 **깊이 (rank+1)/(N+1)** 로 기록하고 `ZTest Greater` + 깊이 0 클리어를 쓴다. 마스크 RT에는 24bit 깊이를 추가했다. reversed-Z API(D3D 등)에서는 Unity가 비교 함수와 클리어 값을 뒤집으므로 셰이더가 기록값도 `1-d`로 뒤집는다. 이 부분은 EditMode 프로브에서 처음에 뒤 개체가 이기는 오류를 확인하고 고친 것이다.
- Simple이 아닌 drawMode, Tight로 패킹된 스프라이트, compute 미지원 환경은 기존 `DrawRenderer`(마스크 셰이더 pass 1, 깊이·ID는 전역값)로 처리한다. `SetBatched(false)`로 일괄화를 끌 수 있다.

## 동등성
- 같은 고정 장면 캡처 드라이버로 v1(부품별 DrawRenderer) 마스크와 비교:
  - main(적128·타워15, 8.5): **텍셀 차이 0** / 50,206
  - zoom 4.25: **0** / 118,659
  - variety(Attack/Defense/Buff/Boss): **0** / 67,368
- 마스크가 비트 단위로 같으므로 윤곽 출력도 같다. 전체 화면 차이(948px)는 실행마다 달라지는 피격 효과다.
- EditMode 프로브: 두 부품의 그리는 순서를 바꿔도 앞 개체 ID가 유지됐다(pass 0·1 모두).
- 회귀: EditMode **303/303**. PlayMode 53/55 — 실패 2개는 앞서 확인한 기존 실패와 같은 테스트·같은 메시지다. 캡처 드라이버의 제거·풀 재사용·줌·토글·Restart 단계 오류 0, Play 종료 후 리소스 잔류 0.

## 성능 (Editor, 적128·타워15, 각 warm15+sample90 ×3)
|지표|원본 (3단계)|v1 공유 (3단계)|**v3 일괄**|v1 → v3|
|---|---:|---:|---:|---:|
|PlayerLoop ms|13.532|10.095|**8.550**|−1.545 (−15.3%)|
|3회 범위|13.45~13.63|9.93~10.23|8.54~8.56|겹침 없음|
|CG.Emphasis.Present ms|5.491|3.470|2.556|−0.914|
|CG.SharedOutline.Mask ms|—|1.783|0.869|−0.914 (−51%)|
|렌더 스레드 ms|3.811|1.610|1.617|변동 범위|
|Draw|1585.9|2228.2|**1586.6**|−641.6|
|SetPass|189.5|840.7|**198.8**|−641.9|
|GC 평균 B/frame|97,931|180,594|**57,159**|−123,435|
|GC 수거(3회 합)|1|3|**0**||
|평균 / 최저 / 최고 FPS|42.97 / 19.74 / 58.76|58.04 / 21.98 / 74.42|**65.38 / 44.01 / 81.76**||

- 원본 대비 누적: 게임 메인 스레드 **13.53 → 8.55ms (−4.98ms, −36.8%)**. Draw·SetPass는 원본과 거의 같은 수준으로 돌아왔다(+0.7 / +9.3).
- 남은 마스크 비용: Collect 0.67 + Record(메시·버퍼 구성) 0.63 + Execute 0.14 + Sort 0.09ms.
- 한계: Editor 측정이다. 실행 파일·목표기기는 미검증이고 GPU 시간은 이 PC 값이다. 이 PC 기준 목표 7~8ms에 근접했지만, 목표기기 60FPS 달성 여부는 별도 확인이 필요하다.
