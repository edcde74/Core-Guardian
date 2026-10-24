# 공유 화면 마스크 윤곽 프로토타입 — 1단계 (기본 OFF)

2026-10-24. 사용자 "진행해"로 [설계안](../../superpowers/plans/2026-10-24-emphasis-outline-redesign.md) 1단계(프로토타입 구현)를 승인받았다. 픽셀 비교·Play·성능 측정은 2·3단계로, 별도 승인이 필요하다.

## 시작 상태 (복구 기준)
- HEAD `bdbda95`, `git status`: 미추적 `docs/development/baseline_remeasure_20261024/`와 `docs/superpowers/plans/2026-10-24-emphasis-outline-redesign.md`뿐(이번 세션 문서)
- Editor: Play false, compile false, scene clean 7 roots, Profiler/Deep/FrameDebugger OFF, native batch OFF
- 수정 전 원본: [original/CoreGuardianObjectEmphasis.cs](original/CoreGuardianObjectEmphasis.cs) (sha256 `6a8c8d03fcddf545f8d2f8410393703da1e9dab16a2ac0d0758361d7a2f08385`)

## 변경 파일
|파일|변경|
|---|---|
|`Assets/Scripts/Runtime/Presentation/Rendering/CoreGuardianObjectEmphasis.cs`|토글이 켜져 있고 카메라가 지원 조건을 만족할 때만 상시 강조를 공유 경로로 보낸다. 꺼져 있으면 기존 `PresentAtOrderWithSources` 그대로다.|
|`Assets/Scripts/Runtime/Presentation/Rendering/CoreGuardianSharedOutline.cs` (신규)|부품을 시각 정렬 순서(정렬 레이어 → order → z)로 정렬한다. `CommandBuffer.DrawRenderer`로 화면+pad 크기 RFloat RT 1장에 개체 ID를 그리고, 개체별 출력 quad(자식 `SharedOutline`)를 원래 정렬값(맨 앞 부품 +3)에 배치한다.|
|`Assets/Art/Rendering/Shaders/Resources/CoreGuardian/SharedOutlineMask.shader` (신규)|원본 스프라이트 알파 × 렌더러 알파 ≥ 기준(0.1) 픽셀에 개체 ID를 기록한다. SpriteRenderer flip(`unity_SpriteProps`)을 반영한다.|
|`Assets/Art/Rendering/Shaders/Resources/CoreGuardian/SharedOutlineOutput.shader` (신규)|자기 ID 픽셀은 제외하고, 반경 `_Width` 정사각 커널 안에 자기 ID가 있으면 윤곽색을 칠한다. 기존 SelectionOutline과 같은 커널·색 규칙이다.|

- 토글: `CoreGuardianSharedOutline.SetEnabled(true/false)` (static, 기본 false). 씬·설정·직렬화 값은 바꾸지 않는다.
- 범위 밖(기존 경로 유지): HUD 선택 윤곽, 강화 overlay, 방어형·보스 fallback 판정 코드, Tint(×1.15), A/B·아틀라스·native batch 정책.

## 1단계 확인 결과
- 재컴파일 성공, 컴파일 오류 0, 두 셰이더 isSupported·메시지 0.
- 첫 컴파일 후 기존 `CoreGuardianObjectEmphasisTests` 4개 중 3개 실패: `Clear()`에서 NullReferenceException이 났다. 원인은 필드 초기화에서 만든 공유 객체(MaterialPropertyBlock 포함)가 MonoBehaviour 생성 시점에 null로 남은 것이다. 처음 사용할 때 생성하도록 바꾼 뒤 **4/4 통과**(토글 OFF 기준).
- 마스크 셰이더 EditMode 단독 프로브(임시 HideAndDontSave 스프라이트, 8×8 RT): 위치·flipX 정상. 처음에는 렌더러 알파 0.5가 반영되지 않았다(`unity_SpriteColor.a`만 사용). 정점 색 × `unity_SpriteColor` 알파로 바꾼 뒤 0.5×0.5=0.25 < 0.3 픽셀이 제외되는 것을 확인했다. 프로브 후 scene dirty false.
- `CommandBuffer.DrawRenderer`가 SpriteRenderer의 스프라이트 텍스처를 `_MainTex`로 바인딩하는 것을 같은 프로브로 확인했다.

## 아직 검증하지 않은 것 (2·3단계)
- 실제 전투 화면에서 출력 셰이더 동작, 화면 좌표 대응(D3D12 y 방향 포함), 원본 대비 픽셀 차이
- 풀 재사용·사망·부품 전환·줌 변경·재시작 회귀, RT·머티리얼 잔류
- `DrawRenderer` 약 1,500회의 메인 스레드 비용과 전체 CPU 절감량. 절감량은 아직 측정 전이다.
- Scene 뷰 카메라에서는 출력 quad가 게임 화면 기준 마스크를 읽어 엉뚱하게 보일 수 있다(Editor 표시 한정, 미확인).
