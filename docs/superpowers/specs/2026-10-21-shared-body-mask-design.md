# 동일 본체 마스크 합성 공유 설계

2026-10-21 Asia/Seoul. 구현 기준 `47ca358`. 상태: **사용자 설계 검토 대기, 제품 코드 미변경**. 사용자 요청은 탄환/에너미 풀링에 앞서 동일 본체 마스크의 중복 생성을 줄이는 것이다. 이번 문서는 해당 작업만 담당한다.

## 목표와 보존 조건

같은 본체·카메라·프레임·마스크 입력으로 생성한 이진 실루엣을 그림자/강조/선택/스캔이 재사용한다. 각 소비자의 quad, 표시 순서, 색, 외곽선 두께, 접촉 밴드, 스캔 수명은 따로 유지한다. 다른 개체끼리 애니메이션/마스크를 공유하지 않는다. 원본 아트/방향/보행/30R 물량/밸런스/HUD/효과 수·수명/사운드/입력 정책은 변경하지 않는다.

완료 조건은 실제 공유 hit·생성/제출 감소, 픽셀 동등성, 올바른 수명과 같은 조건의 CPU/FPS/메모리 검증이다. 공유 hit가 없거나 품질 회귀가 있으면 최적화 완료로 표시하지 않는다. FPS·메모리 절감률을 사전에 보장하지 않는다.

## 현재 확인한 구조

- [SelectionOutline](../../../Assets/Scripts/CoreGuardianSelectionOutline.cs)은 소비자마다 sources/geometry/메시/CommandBuffer/RT/출력 quad를 보유한다. `PresentMask`가 소스 선별→경계/크롭→RT clear→DrawMaskParts→Execute→quad/속성을 순차 처리한다.
- PhotoShadow 순서200, ObjectEmphasis450, 선택500, UpgradeFeedback600이다. 일반 경로에서는 접촉 그림자가 먼저 합성한다. 실행 순서를 바꿔 공유를 강제하지 않는다.
- 접촉 그림자의 기본 widthPixels=2이므로 pad=4, 강조 기본 widthPixels=1이므로 pad=3이다. 같은 본체라도 기존 크롭은 다르다. width/color/sorting은 이진 본체 합성과 소비자 표현을 구분해서 다뤄야 한다.
- 접촉 셰이더는 `uv.y * textureHeight`로 밴드를 계산한다. 공유 RT의 부분 영역을 읽을 때 로컬 원점을 보정하지 않으면 접지 모양이 바뀐다. 스캔 밴드는 world Y를 사용한다.
- 기존 공통 재질3개와 개체별 PropertyBlock은 유지한다. [직전 결과](../../development/shared_mask_materials_20261021/RESULTS.md)는 평균FPS30.083 관측이며 이번 작업의 새 전후 계측을 대신하지 않는다.

## 대안과 선택

|대안|이점|제약/판정|
|---|---|---|
|크롭까지 완전히 동일할 때만 공유|좌표 변경 최소|기본 pad3/4가 달라 핵심 그림자·강조가 묶이지 않음. 주 경로로 선택하지 않음|
|모든 소비자를 강제로 하나의 큰 크롭으로 변경|공유 조건 단순|기존 quad·정밀도·밴드가 달라질 위험. 선택하지 않음|
|먼저 만든 마스크가 요청 영역을 포함하면 부분 UV로 재사용|기본 실행 순서에서 중복 합성 감소, 소비자 기존 출력 영역 유지|영역 원점 보정·입력 검증·공유 수명 필요. **권장안**|

## 공유 대상과 적격 판정

새 내부 캐시 `CoreGuardianBodyMaskCache`가 canonical artwork Transform별 record를 관리한다. canonical artwork는 기존에 찾던 첫 `2D Sprite` Transform이며 전달된 부모 target 이름이나 재활용 가능한 숫자 ID만으로 식별하지 않는다. 서로 다른 본체는 절대 같은 record를 쓰지 않는다.

매 요청에서 아래 항목이 정확히 같고, 생성된 화면 픽셀 영역이 요청 영역을 포함할 때만 cache hit다.

- 현재 `Time.frameCount`, Camera 객체 참조, 실제 view/projection matrix, pixelRect 및 렌더 대상/해상도 조건.
- raw alphaThreshold와 실제 마스크 텍스처/아틀라스 참조·Revision·레이어/geometry 유효성.
- 소스 renderer 목록 및 enabled/active/sprite/alpha/flip/실제 변환행렬·명시적 기하 revision·텍스처 updateCount. RGB tint는 마스크에 사용하지 않으므로 공유를 막지 않는다.
- 명시적 `InvalidateSources` 또는 hierarchy/atlas 변경이 없고, record RT가 살아 있으며 `IsCreated`가 true일 것.

해시만으로 동등성을 판정하지 않으며 최종 값 비교를 한다. 다른 시스템이 쓰는 `Transform.hasChanged`를 지우지 않는다. 같은 프레임 안의 이동/회전/방향 교체/알파/소스 변경도 재합성을 유발해야 한다. 변경되지 않은 본체라도 프레임이 바뀌면 새로 합성한다. 프레임 간 정지 화면 캐싱은 이번 범위가 아니다.

GPU 쪽에서 원본 텍스처를 변경하는 외부 코드는 updateCount 증가 또는 명시적 invalidate가 필요하다. 같은 Sprite의 런타임 기하를 바꾸는 코드는 `InvalidateSources`를 호출해야 하며 이 호출은 기하/메시 캐시의 revision도 갱신한다. 기하 배열을 매 요청 새로 읽어 할당하지 않는다. 현재 정적 스프라이트 경로를 검증하며, 추적되지 않는 사용자 정의 변형은 독립 합성으로 처리한다. 다른 카메라/다른 threshold/불충분한 크롭/비호환 조건에서는 별도 record로 기존 합성을 수행한다. 현재 orthographic 경로부터 공유하며 비지원 stereo/dynamic-resolution 경로는 기존 독립 합성을 유지한다.

## 좌표와 출력

현재 소비자가 계산하던 requested lo/hi, quad의 월드 좌표와 정렬은 그대로 유지한다. 생산자 record는 자신의 lo/hi와 RT capacity를 보관한다. 소비자 UV는 `(requestedLo - producerLo) / capacity`에서 시작해 requested width/height만큼 확장한다. 필터 Point/Clamp, mip 없음, 한 texel=한 화면 pixel을 유지한다.

예: 접촉 pad4가 먼저 생성되면 강조 pad3은 안쪽1px에서 시작하는 UV를 사용한다. 강조가 먼저 호출되어 큰 접촉 요청을 포함하지 못하면 새 합성을 허용한다. 역순에서 오래된 작은 마스크를 억지로 쓰지 않는다.

접촉 셰이더에는 기본0인 `_MaskOriginPx`를 추가해 밴드의 좌표를 `uv.y * capacityHeight - consumerOffsetY`로 계산한다. 접촉의 역할별 밴드 값은 변경하지 않는다. 이웃 texel 샘플 간격은 공유 RT의 실제 texel size다. 윤곽/스캔도 기존 색/두께/world-Y 밴드·alpha·sorting을 유지한다. 기존 공개 Present/Overlay/Contact 진입점과 null 재질 fallback을 보존한다.

크롭 변경으로 GPU 반올림 경계가 달라질 수 있으므로 정수 픽셀 오프셋만으로 동등성을 가정하지 않는다. 픽셀 검증에서 차이가 발생하면 원인을 좁히며 해당 호환 경로를 독립 합성으로 돌린다. 화질을 낮춰 공유 hit를 확보하지 않는다. 기본 병사/플레이어 쌍이 실제로 공유되지 못하면 설계의 성능 목표 미달로 보고한다.

## 소유와 수명

- RT는 캐시 record만 소유한다. 소비자는 현재 출력에 사용하는 record의 binding을 보유하고, 자기 quad/표시 renderer/PropertyBlock은 계속 직접 소유한다. 기존 private mask 필드는 현재 binding의 실제 RT alias로 유지해 진단을 가능하게 한다.
- `DrawMaskParts`의 기하/메시/CommandBuffer는 당장은 기존 소비자에 남긴다. cache miss의 첫 요청자만 명령을 기록·제출하고, hit 소비자는 이를 반복하지 않는다. 생산자 파괴가 캐시 RT를 조기 해제해서는 안 된다.
- 참조 중인 record의 내용을 다른 pose/카메라/크롭으로 덮어쓰지 않는다. 새 세대 요청은 다른 free record를 쓰거나 새 RT를 확보한다. 혼자만 참조하는 이전 record는 자기 다음 출력용으로 재사용할 수 있다.
- 같은 본체의 두 소비자가 이전/현재 프레임을 교대로 참조하면 두 RT가 필요할 수 있다. 기본 공유만으로 할당 RT 메모리가 절반이 된다고 주장하지 않는다. 본체별 비참조 free record는 최대1개만 보관하며 나머지는 소유한 것만 해제한다.
- 새 binding 확보 후 이전 binding을 반환해 매 프레임 마지막 사용자0으로 캐시가 파괴·재생성되는 churn을 막는다. target 변경/null/숨김/소비자 제거 시 binding을 반환한다. 마지막 binding이 사라지면 해당 본체 record·대기 RT를 모두 Release+Destroy하고 캐시 인덱스에서 제거한다.
- 명시 무효화는 다음 요청의 재합성을 보장하되, 다른 소비자가 참조 중인 기존 세대를 먼저 파괴하지 않는다. RT가 lost/released 상태이면 빈 RT를 hit로 돌려주지 않고 재생성·clear·재합성한다.
- 새 컴포넌트의 binding 반환은 중복 호출에 안전해야 한다. Play 전환/씬 종료/domain reload 옵션에서도 캐시 소유 출력부터 비활성화·binding 반환 후 캐시 전용 자원만 해제한다. 살아 있는 사용자 원본/공용 자산이나 기원 불명의 과거 RT는 정리하지 않는다. Editor hook이 필요하면 한 번만 등록하고 영속 Editor/프로젝트 설정은 바꾸지 않는다.
- warmed 상태에서 binding 교환·snapshot·목록 비교를 위해 매 프레임 새 managed 배열/lease 객체를 만들지 않는다. 기존 버퍼/record capacity를 재사용한다. 캐시/검증 비용도 측정에 포함한다.

## 파일 책임

|파일|책임|
|---|---|
|새 `Assets/Scripts/CoreGuardianBodyMaskCache.cs`와 meta|record 적격 판정, RT 소유·binding·세대/회수·통계|
|`CoreGuardianSelectionOutline.cs`|기존 합성 함수를 miss에만 실행, 소비자별 quad/부분 UV/PropertyBlock, binding 반환|
|`PhotoShadowContact.shader`|기본0의 로컬 픽셀 원점 보정. 색/밴드/샘플 수 보존|
|`CoreGuardianPhotoShadow.cs`/`CoreGuardianObjectEmphasis.cs`|필요한 binding 무효화 연결만. 실행 순서·색·두께·역할 설정은 유지|
|새 공유 마스크 계약과 관련 기존 EditMode 검사|동일 입력 재사용/다른 입력 분리/부분 UV/세대·해제·source 변경 계약|
|새 날짜별 진단 폴더|전후 원본·통계·품질·실패·복원 기록. 기존 Probe/픽셀/수명 도구 재사용|

아틀라스 자산/원본 import 설정/씬 YAML/패키지/전역 렌더 설정은 변경하지 않는다. 탄환/에너미 풀링은 별도 범위다.

## 검증 계약과 순서

1. 실제 연결/씬/dirty/Play/컴파일/테스트/Profiler와 HEAD/diff를 다시 확인하고 설정 해시를 저장한다. 사용 중이면 기존 작업을 중단하지 않는다. 현재 확인 상태는 전투씬 clean/Play·compile·Profiler false, 사용자 설정2파일 미커밋이다.
2. 같은128적/15포탑·1920×1080·카메라8.5·배속1·기존 음량·준비30/측정100×3을 구현 전후 수집한다. 기본 셋업/Profiler 설정은 직전 실험과 같게 하고, 캐시 마커를 사전에 추가한다면 양쪽 동일 계측 상태로 재수집한다.
3. RED→GREEN 계약: 같은 본체/카메라/프레임의 재사용, pad4→3 부분 UV, 호출 역순의 안전한 miss, threshold/카메라/동일 프레임 pose·sprite·alpha·flip·텍스처/atlas 변경 분리, 한 소유자 제거 후 생존자/마지막 해제, 다른 본체 혼동 방지, 비활성화·재등장·RT lost와 cache 무효화.
4. 기존 공유 재질/아틀라스/윤곽/그림자/강조 계약을 영향 범위로 선택한다. 기존8역할3,456 효과 픽셀 기준을 새로 대조하고, 실제 접촉+강조 공유 쌍·같은 프레임 변경·선택/스캔 중첩·화면 가장자리/여백 다른 조합을 추가한다. 플레이어8방향 입력을 포함한다. 보스 과거 전체 합성 미확정9건은 원본끼리 대조군과 구분하며 자동 해결로 쓰지 않는다.
5. 유한600프레임×3의 줌/복원/초기화와 자체 Play 종료에서 binding 수, distinct bound RT, cache allocated RT(대기 포함), 총 bytes/피크/생성·해제, 공유 재질/메시 수를 나눠 기록한다. private mask 참조를 단순 합산해 공유 RT를 중복 계수하지 않는다. scene/domain reload 옵션의 현재 상태를 먼저 기록하고 임시로 검사한 옵션만 복원한다.
6. CPU·GC·Draw Calls·SetPass·clear/DrawMaskParts/Execute 실제 횟수·cache hit/miss 이유·평균/중앙/p95/p99/최대 frame time·최소/최대/평균 FPS를 전후 기록한다. Frame Debugger는 정상 성능 캡처와 분리한다. GPU/VRAM은 유효 계측이 없으면 N/A다. 원본/실패/악화/복원 한계를 보존한다.

현재 역할 수로는 접촉129+강조145=274개의 합성 요청이 있고, 모두 호환된다면 생성145/hit129까지 줄일 수 있다는 **구조상 후보**다. cache allocated RT145개/FPS47% 개선의 예측이 아니다. 다른 입력·순서/추가 소비자로 miss가 발생할 수 있으므로 실제 hit와 합성 감소를 확인한다.

## 공식 근거와 다음 단계

2026-10-21 공식 API 설명 확인: [Texture.updateCount](https://docs.unity3d.com/kr/6000.0/ScriptReference/Texture-updateCount.html)는 texture 갱신 추적에 쓰며 GPU 측 변경은 명시 증가가 필요하다. [RenderTexture.Release](https://docs.unity3d.com/cn/6000.0/ScriptReference/RenderTexture.Release.html)는 하드웨어 자원을 반환하지만 객체 파괴와 다르다. [IsCreated](https://docs.unity3d.com/cn/6000.0/ScriptReference/RenderTexture.IsCreated.html)로 lost 상태를 확인한다. 전체 영상 시청으로 기록하지 않는다. 기존 사례·측정 규칙은 [담당 문서](../../development/optimization_research_20261020.md), [최적화 규칙](../../development/OPTIMIZATION_RULES.md)을 따른다.

자체 검토: 미정 placeholder 없음, consumer 표현과 producer RT 소유 책임을 분리했고, 기본 pad 차이/원점/불변 세대/대기 RT 중복 계수를 명시했다. 이번 문서는 동일 본체 mask sharing 한 범위에 집중한다.

다음은 사용자 written-spec 검토 후 해당 설계의 구현 계획과 검증 단계를 작성하는 것이다. 제품 구현·추가 Play/테스트·빌드/실행파일/push는 이 문서 작성에서 수행하지 않았다.
