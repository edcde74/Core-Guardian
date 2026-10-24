# SRP Batcher OFF 픽셀 회귀 원인 — 2026-10-24

조건: 적128·타워15, 1920×1080, 같은 프레임에서 timeScale 0으로 고정한 뒤 캡처. 드라이버는 SrpPix6~10.cs, MpbProbe*.cs.

## 결론: 원인 3가지
1. **HDR 색(1보다 큰 값)이 잘림.** SRP Batcher가 꺼지면 스프라이트 동적 배칭이 renderer.color를 8비트 정점 색으로 굽기 때문에 1을 넘는 값은 1로 잘린다.
   - 강조 밝기 ×1.15: 전투 본체 전부(renderer.color 1.15). 차이 약 42.6k px. pix5와 pix6(밝기 1)를 비교해 분리했다.
   - 경로 연석·포장돌(`CoreGuardian_Route01_VisualOnly`의 Low curb / Road stone 23개, 씬에서 색 1.25/1.3으로 지정). 50×50 모서리 칸이 0.8배로 어두워진다.
   - EditMode 프로브(MpbProbe): 색 1.5 스프라이트가 SRP ON에서는 0.314, OFF에서는 0.200(=원본)으로 나왔다. 재현이 확인됐다.
2. **장식 MeshRenderer(OuterDecoration, LowProps, SpawnTunnel02)에 스프라이트 MPB가 없음.** OFF에서는 사라진다. 해결법은 이미 알고 있다(RouteMeshBake.ConfigureOutputs 방식).
3. **같은 정렬값끼리의 그리는 순서가 달라짐.** 피격 번쩍임(MPB + 별도 머티리얼) 본체는 ON에서 같은 order 이웃보다 위에, OFF에서는 아래에 그려진다(MpbProbe3로 재현). 고정 장면에서는 같은 위치에 겹친 적이 많아 피격 개체가 완전히 가려졌다. 번쩍임 자체는 정상 동작한다. 0.09초짜리 연출이 겹친 적 아래로 들어가는 정도의 차이다.

그 밖의 화면은 비트 단위로 동일했다(밝기 1 + 장식 MPB 적용 시 남은 차이 12,025px = 위 1·3번뿐).

## 고치는 방법
- 1번(강조): renderer.color 대신, 같은 셰이더에 `_Color`=×1.15인 '밝기 쌍둥이 머티리얼'로 렌더 직전에 바꿨다가 되돌린다. Sprite-Lit/Unlit-Default와 BodyHitFlash 모두 `input.color*_Color*unity_SpriteColor`이므로 수식상 같은 결과다. 쌍둥이 머티리얼은 공유되므로 배칭도 유지된다. 단, 기존 EditMode 테스트는 "renderer.color가 1.15가 된다"는 계약이라 테스트 수정이 필요하다.
- 1번(연석): 런타임 시작 시 해당 스프라이트의 색을 머티리얼 `_Color`로 옮긴다(씬 파일은 수정하지 않는다).
- 2번: 런타임 시작 시 Sprite 셰이더를 쓰는 MeshRenderer에 MPB를 부여한다.
- 3번: 그대로 두거나(정렬값이 같으면 원래부터 순서가 보장되지 않음), 별도로 처리한다.

## 이번에 되돌린 것
- ObjectEmphasis의 틴트 타이밍 이동(LateUpdate 끝에서 적용)은 원인이 아니었으므로 이전 상태로 복구했다. 컴파일 OK.
