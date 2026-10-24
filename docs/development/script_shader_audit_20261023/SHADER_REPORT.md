# 셰이더 스크립트 보고

2026-10-23. 사용자 승인 범위: 소스 읽기·C# 사용 경로/재질 참조 대조·기존 C# 조사와 분리 보고. Unity 연결/Play/Profiler/Frame Debugger/컴파일/설정 변경/제품 수정은 하지 않았다.

## 조사 범위

프로젝트 Assets/Packages에서 **관련 파일38개: .shader29 / .hlsl1 / .cginc4 / .shadergraph4**. 그중 CoreGuardian 커스텀11개, TMP 관련27개다. 전체 본문을 정적 스캔하고 커스텀11개와 실제 피해 숫자 Mobile SDF의 실행 함수는 직접 읽어 C# 호출/재질을 대조했다. TMP Graph4개는 직렬화 노드/Target/Custom Function 연결 검토이며 생성 GPU 코드를 컴파일·분석한 것은 아니다. 전체53,529줄에는 Graph JSON이 포함돼 있어 수만 줄 GPU 프로그램을 분석했다는 뜻이 아니다.

[파일별 hash/Pass/분기/참조/Graph 구성](shader_inventory.json), [전체38개 표](shader_inventory.csv), [스캔 재현 코드](inventory.py). 소스상의 샘플 호출 지점 수는 함수 호출·loop 횟수를 펼친 실행 수와 다르며, 둘 다 GPU 하드웨어 요청 수/시간과 같지 않다. 아래 샘플 수는 함수 호출과 loop를 펼친 **코드상 상한/경로별 수**다. 컴파일러 최적화·clip·분기·캐시·오버드로·실제 화면 점유율에 따라 실비용이 달라진다.

## 커스텀11개 사용 경로와 비용 구조

커스텀11개는 모두 소스에 **Pass1개**다. 하나의 셰이더가 무조건 여러 패스를 돌리는 구조가1600드로우의 원인이라는 근거는 없다. 실제 화면의 렌더러/부품/상태·패스 제출 분포는 새 캡처 없이 확정하지 않는다. custom shader의 직렬화 GUID 참조0은 미사용을 의미하지 않는다. C#에서 Resources.Load로 생성하는 연결을 함께 확인했다.

|셰이더|현재 C# 경로|코드상 비용|판단/방향|
|---|---|---|---|
|[SelectionOutline](../../../Assets/Art/Rendering/Shaders/Resources/CoreGuardian/SelectionOutline.shader)|상시 강조 및 선택 윤곽. 씬의 강조 폭1, 독립 선택 기본 폭2|최초 중앙1회 후 본체 밖에서 (2r+1)² 주변 mask 조회. r1이면 최대10, r2이면26, 허용 최대r8이면290회. 본체 내부는 첫 clip으로 제외.|현재 사용 경로 중 **우선 GPU 후보**. 중앙 읽기 재사용·경계 판정이 끝나면 조기 종료·폭1/2 고정 경로를 검토. 두께/실루엣 축소는 제안하지 않음.|
|[SelectionMask](../../../Assets/Art/Rendering/Shaders/Resources/CoreGuardian/SelectionMask.shader)|개별/미지원 원본 마스크 fallback|2D 또는 array 한 분기에서 alpha1회, threshold clip, BlendOp Max. part matrix/VP 정점 변환|픽셀 연산 자체는 비교적 단순. 부품별 제출·행렬·출력 면적·블렌드와 CPU 준비/RT 수를 함께 봐야 함.|
|[BatchedSelectionMask](../../../Assets/Art/Rendering/Shaders/Resources/CoreGuardian/BatchedSelectionMask.shader)|지원 조건의 B 마스크 페이지 생산. 정렬 출력 결합과 별개|alpha1회+영역 clip, StructuredBuffer의 두 행렬 변환, subpixel 정렬/페이지 배치|`precise`·256격자 보정·두 행렬은 원본 픽셀 보존 근거가 있는 코드. 단순히 half/단일행렬로 바꾸지 않음. GPU 절감은 미측정.|
|[TowerGlow](../../../Assets/Art/Rendering/Shaders/Resources/CoreGuardian/TowerGlow.shader)|포탑 원본/Fixed/Moving/Housing별 별도 overlay. source.enabled를 따라 활성화|중앙+상하좌우 총5회 원본RGBA 읽기. 각 읽기마다 청색/녹색·alpha 분류. 역할별 pulse, Support는 sin도 계산|고정 원본 발광마스크 재사용과 픽셀에 불변인 pulse/strength 계산 위치 검토. C#은 layer별 Material/Renderer와 매 Present SetFloat3개가 연결돼 있음. 효과 수/순서/위상 유지 필요.|
|[TowerBaseShade](../../../Assets/Art/Rendering/Shaders/Resources/CoreGuardian/TowerBaseShade.shader)|포탑당 별도 base shade renderer|원본 alpha1회. 아래5개 소스 행의 strength만 유효하지만 원본 sprite geometry 전체를 처리|유효 밴드 밖을 texture fetch 전에 거르거나 출력 geometry 범위를 줄이는 후보. 정렬 bounds·원본 alpha/UV·픽셀 동등성 별도 확인 필요.|
|[CoreAmbient](../../../Assets/Art/Rendering/Shaders/Resources/CoreGuardian/CoreAmbient.shader)|코어1개의 발광과 기존 입자|코어 발광 중앙+8주변 총9회 원본RGBA/청록 분류. `_Particles` 분기는 텍스처 읽기 없이 vertex color 출력|고정 마스크 재사용 후보. **9회×12입자라고 계산하면 틀림**. 코어1개 범위여서 적 전체 윤곽보다 우선순위 낮게 둠.|
|[BodyHitFlash](../../../Assets/Art/Rendering/Shaders/Resources/CoreGuardian/BodyHitFlash.shader)|피격 중 원본 SpriteRenderer 재질 교체, 코어 피격|일반 비DEBUG 분기 원본RGBA1회+색상 lerp. 원본 URP Unlit도1회 읽기|별도 overlay를 새로 그리는 방식이 아님. 개체별 재질/property block 상태는 batching에 영향을 줄 수 있으나 모든 피격을 추가 draw1회로 합산하지 않음. 우선 샘플 축소 대상 아님.|
|[UpgradeScan](../../../Assets/Art/Rendering/Shaders/Resources/CoreGuardian/UpgradeScan.shader)|강화 성공한 대상에만 잠깐 overlay|mask1회 후 world Y band clip|밴드 밖을 먼저 clip하면 샘플을 피할 여지가 있음. 순간 효과이므로 상시 윤곽보다 후순위. 유지 중인 원본 표시·스캔 수/수명 보존.|
|[PhotoShadowContact](../../../Assets/Art/Rendering/Shaders/Resources/CoreGuardian/PhotoShadowContact.shader)|남아 있는 contact API. 현 PhotoShadow는 PhotoCastShadow sprite만 사용하며 런타임 contact 호출자를 찾지 못함|밴드 검사 후 중앙+위2 mask 총3회|이미 제거된 동적 발밑 접촉 비용을 현재 병목으로 재제시하지 않음. 넓은 그림자와 구분. 삭제/재활성화하지 않음.|
|[BatchedMaskOutput](../../../Assets/Art/Rendering/Shaders/Resources/CoreGuardian/BatchedMaskOutput.shader)|SelectionOutline.RegisterOutput은 Play 중 즉시 return. 현재 런타임 출력 등록 보류|윤곽은 반복 mask Load, contact는3회. `DisableBatching=True`와 per-part matrix 경로 존재|파일의 batching flag만 보고 현1600draw 원인이라고 판단하지 않음. 현재 보류 유지.|
|[AttackBodyBatch](../../../Assets/Art/Rendering/Shaders/Resources/CoreGuardian/AttackBodyBatch.shader)|native 본체 배칭 후보. 기본 Enabled false/Reset Clear, 마지막 실험 OFF 기록|vertex에서 palette StructuredBuffer, fragment에서 Texture2DArray1회. `_Color`는 native sprite batching 의도로 UnityPerMaterial 밖에 있음|셰이더 자체보다 기존 C# 교체/복원·property 전송 비용 문제도 확인된 보류 후보. CBUFFER 위치를 일괄 수정하거나 자동 활성화하지 않음.|

### 윤곽 반복 조회를 줄이는 후보의 정확한 의미

`SelectionOutline.Frag`는 이웃 중 edge가 .5 이상이면 최종 출력색이 동일하다. 현재는 조건을 만족한 뒤에도 남은 이웃을 전부 조사한다. 최초 읽은 중앙값을 재사용하고, 충분한 edge를 찾은 경우 조기에 판정을 마치는 방향이 있다. 폭1 고정의8이웃 경로도 검토할 수 있다. 다만 분기/early exit가 GPU wave 실행을 더 빠르게 만든다는 보장은 없으며, 컴파일러가 이미 중앙 중복을 합쳤을 수도 있다. **10→9 소스상 읽기 감소를 GPU10%/FPS10% 개선으로 환산하지 않는다.** 원본 mask threshold/viewport/point sampling/폭1·2·최대8 fallback을 모두 유지해야 한다.

### CPU 마커와 GPU 시간을 분리

이전 실측 `CG.Mask.Present4.672ms`는 C# bounds/파트 준비·RT/command 작업 등의 CPU 범위다. 이 숫자를 SelectionOutline 픽셀 셰이더의 GPU4.672ms로 표시하면 틀린다. shader 샘플 수와 화면 중첩에 따른 실제 GPU 비용은 **이번에 측정하지 않았다**.

## TMP 관련27개와 실제 피해 숫자

- 실제 `PixelifySansBoldDamageOutline.mat`의 Shader GUID가 [TMP_SDF-Mobile.shader](../../../Assets/Art/Rendering/Shaders/TextMeshPro/TMP_SDF-Mobile.shader)에 연결된다. 유효 keyword는 **OUTLINE_ON만**이며 UNDERLAY는 없다. [재질](../../../Assets/Fonts/Resources/CoreGuardian/Fonts/PixelifySansBoldDamageOutline.mat).
- 이 Mobile SDF는 **단일 Pass**, 현재 해당 keyword 조합의 pixel 함수는 font atlas alpha1회로 face+outline을 합성한다. “글자 외곽선 때문에2패스”나 “무거운 bevel/bump/cubemap을 현재 숫자마다 실행”이라는 결론은 맞지 않는다. glyph/Canvas 갱신·투명 겹침·표시 수는 별도 비용이다.
- 실제 shader 본문에 `#pragma enable_d3d11_debug_symbols`가 있다. CPU/GC 스파이크의 직접 원인으로 보지 않으며 향후 승인된 빌드/심볼 정책 점검 때 검토할 항목이다. 이번에 삭제하지 않았다.
- 나머지17개 TMP shader는 프로젝트의 .mat/.asset/.prefab/.unity에서 직접 Shader GUID 참조를 찾지 못했다. Mobile/일반 SDF/Bitmap/Masking/Overlay/SSD/Surface/SpaceWarp/2-Pass별 pragma·샘플·Pass 구조를 분류했다. **직접 참조0을 빌드 미포함/Shader.Find 불가/메모리0의 증거로 쓰지 않는다.**
- `TMP_SDF-Mobile-2-Pass`는 Outline/Face2개 explicit Pass. SpaceWarp4개는 일반 출력 외에 `XRMotionVectors` Pass가 있어 소스에2개인 것이며 매 일반 화면에2회 실행된다는 뜻은 아니다. `TMP_Bitmap-Mobile`의 explicit Pass2개는 **서로 다른 SubShader의 대안**이다. 단순 Pass 토큰 개수로 draw를 계산하지 않는다. Surface2개는 생성 Pass 수를 소스의 explicit ShadowCaster1개만으로 계산할 수 없다.
- Graph4개(HDRP Lit/Unlit, URP Lit/Unlit)는 각각7개 SampleTexture2D 노드·10개 CustomFunction 노드가 직렬화돼 있다. 함수는 [SDFFunctions.hlsl](../../../Assets/Art/Rendering/Shaders/TextMeshPro/SDFFunctions.hlsl)의 GUID로 연결된다. 분기/keyword와 코드 생성으로 실제 샘플·Pass는 달라지므로 “항상7회”가 아니다. 실제 피해 숫자 재질은 이 Graph들을 사용하지 않는다.
- cginc4개는 SDF 기본/모바일/Surface/공통 property를 확인했다. 무거운 normal/bevel/glow 함수가 파일에 존재하는 것과 현재 Mobile SDF keyword에서 실행하는 것을 구분했다. 보관 중인 TMP 변형을 일괄 수정·삭제하지 않는다.

## 기본 원본 Sprite 셰이더와 Draw Call 해석

원본 환경 재질 GUID는 로컬 URP package의 `Sprite-Unlit-Default.shader`로 이어진다. 실제 설치 소스와 `2DCommon.hlsl`을 보조 확인했다. 이 버전도 explicit Pass1개, 일반 fragment는 원본RGBA1회다. `#pragma multi_compile_instancing`가 있다고 동일 prefab의 모든 렌더러가 자동1draw로 합쳐지는 것은 아니다.

현재 구조는 **원본 부품/텍스처·투명 정렬 + 개체별 윤곽 출력 + 그림자/체력바/포탄/효과/HUD + 별도 마스크 생산**이 연결된다. 각 renderer를 제출하는 C# 구조와 shader의 픽셀 연산을 구분해야 한다. 일부 custom shader에 UnityPerMaterial이 없고 property block/CommandBuffer 경로도 있으나, 이를 단순한 CBUFFER 일괄 추가로 해결된다고 주장하지 않는다. SRP의 제출 비용 최적화와 실제 draw 결합도 같은 지표가 아니다.

## 추천 순서와 미완료

1. CPU 목표는 기존 [C# 보고](CSHARP_REPORT.md)의 뷰/타깃 유효성 검사·변환 중복과 마스크 준비/업로드 범위부터 작게 분리한다. 이것이 기존 CPU 실측과 직접 연결된다.
2. 셰이더만 고른다면 **활성 SelectionOutline의 동일 결과 반복 조회 축소**가 첫 후보다. 다음은 포탑 발광의 고정 분류 재사용·밑 음영의 유효 밴드 처리다. 이 순서는 코드 구조·사용 개체 수에 따른 판단이며 GPU 비용 실측 순위가 아니다.
3. 정렬/외형을 바꿔 윤곽·발광·그림자/부품 수를 줄이거나, 보류 배칭/아틀라스를 재활성화하지 않는다. 새 아트·메모리 절충·실행 검사는 별도 승인 대상이다.

새 성능 전후 측정/절감량/율/FPS·GPU/품질 픽셀 결과는 **N/A: 정적 조사만 승인·수행**. 기존 결과를 새 셰이더 개선 성과로 재사용하지 않는다. 기기별 컴파일 지원/variant stripping/최종 실제 pass/오버드로·GPU ms/전체 resident memory는 미검증이며, 이 보고는 수정 후보와 실행 경로를 확정한 문서다.
