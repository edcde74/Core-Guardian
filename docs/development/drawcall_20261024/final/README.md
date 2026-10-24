# Draw/SetPass 감소 — SRP Batcher OFF + 윤곽 출력 배칭 (2026-10-24)

사용자 승인 범위: SRP Batcher OFF 채택, 강조 밝기를 머티리얼 방식으로 바꾸고 관련 테스트 수정, 겹친 같은 정렬값의 그리는 순서 차이는 허용.
모든 수치는 Editor 기준이다(적128·타워15, 1920×1080, ortho 8.5, warm15+sample90).

## 결과
|지표|v3 (SRP ON, 이전 채택)|SRP OFF만|**최종 (OFF + 윤곽 출력 배칭)**|
|---|---:|---:|---:|
|Draw|1587|269|**131**|
|SetPass|199|245|**106.5**|
|렌더 스레드 ms|1.58~1.63|1.41~1.63|**1.04~1.16**|
|PlayerLoop ms|8.18~9.01 (b_on)|8.78~9.81|**8.50 / 8.50 / 10.06**|
|GC B/frame|56K (수거 프레임 제외)|56K|56K|

- 최종 3회 원자료: `e_off_1~3.json`(1회차 10.06ms는 GC 수거 1회와 첫 실행 영향이 섞여 있다). 비교군은 `b_on_*`, `c_*`, `d_*`이다.
- Draw는 92% 줄었고 SetPass는 46% 줄었다. 메인 스레드 시간은 측정 노이즈 범위 안에서 같다. 강조 틴트는 0.78 → 0.86~1.1ms로 늘었다.

## 변경
1. `UniversalRP.asset` `useSRPBatcher=false`(저장). 스프라이트가 엔진의 동적 배칭으로 합쳐진다.
2. **강조 밝기 ×1.15**를 `renderer.color` 대신 같은 셰이더의 쌍둥이 머티리얼(`_Color`×1.15)로 넣는다(렌더 구간에만 적용하고 끝나면 복원). 동적 배칭은 정점 색을 8비트로 구워 1보다 큰 값이 잘리기 때문이다. `CoreGuardianObjectEmphasis.cs`, EditMode 테스트 계약은 "실효 색 = renderer.color × material._Color"로 바꿨다.
3. `CoreGuardianSpriteBatchCompat.cs`(신규): Play 시작 시 (a) 씬에서 색이 1보다 큰 정적 스프라이트(경로 연석 1.25 / 포장돌 1.3)의 색을 공유 머티리얼 `_Color`로 옮기고, (b) 스프라이트 셰이더를 쓰는 장식 MeshRenderer에 RouteMeshBake와 같은 MPB를 준다. 씬 파일과 에셋은 수정하지 않는다.
4. **공유 윤곽 출력**: 개체별 MeshRenderer + MPB(배칭 불가, 약 150 draw) 대신 1×1 흰 스프라이트 SpriteRenderer + 공유 머티리얼로 그린다. 개체 ID 1~255는 renderer alpha 바이트로 넘긴다(linear 색공간 변환에서 alpha는 바뀌지 않음). 256번 이상은 기존처럼 MPB를 쓴다.
5. SRP Batcher OFF에서는 엔진이 모든 SpriteRenderer에 `_MainTex` 블록을 붙여 `HasPropertyBlock()`이 항상 true가 된다. 그래서 `CoreGuardianSpriteBlocks.HasCustomBlock`(신규)을 RouteMeshBuilder에 적용했다. 네이티브 본체 배치(AttackBodyBatch, 기본 OFF)는 SRP Batcher 전제이므로 OFF일 때 동작하지 않게 막았다. 해당 픽셀/소스 테스트 4개는 Assume으로 inconclusive 처리된다.

## 화면 동등성
- 같은 실행 안에서 ON과 OFF를 비교하면 차이는 같은 정렬값으로 쌓인 적 무리의 위아래 순서뿐이다(피격 번쩍임 중인 적이 이웃 아래로 감, 허용 항목). 그 외에는 플레이어 그림자 ±2 정도다.
- 실행 간 비교에서는 피격 위치·이펙트가 실행마다 달라 무리 영역에 차이가 생긴다. 그 밖의 영역은 같다. 원인 분석은 [srp_off_cause.md](../srp_off_cause.md)에 있다.

## 회귀
- EditMode 303: 통과 299 / inconclusive 4(SRP Batcher 전용 네이티브 배치 테스트) / 실패 0.
- PlayMode 55: 통과 53. 실패 2개는 기존과 같은 테스트·같은 메시지다.

## 남은 Draw 구성 (subtraction 측정, sub_run.json)
UI Canvas 약 120 draw / 97 SetPass(가장 큼), 타워 음영·발광 14, 적 본체 27, 그림자 9, 나머지 소수. 팔다리 4장 텍스처를 하나로 가정하면 −22 정도였다(potential_run.json). 다만 기존 기록상 보행 아틀라스는 픽셀 회귀가 있어 보류했다.
