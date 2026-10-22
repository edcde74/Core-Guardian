# Draw Calls / SetPass 감소 설계

> 아래는 최초 승인 당시 설계다. 2026-10-22 B 재개 후 최종 채택 범위·출력/방어형/보스 fallback·실측·미완료는 [B_RESULTS.md](B_RESULTS.md)가 정본이다. A는 보류, B2 출력 교체는 런타임 미채택, B3 생성 배칭 및 개별RT copyback을 유지한다.

2026-10-22 Asia/Seoul. 사용자 승인: 스프라이트 배칭 경로 개선 + 공용 마스크 아틀라스/출력 배칭 방향, 단계별 전후 측정·픽셀 대조. 이후 사용자가 A/B 둘 다 순서대로 진행하는 것을 재확인하고 “응 이렇게 진행해”로 승인했다. **설계 승인 완료, 제품 구현은 아직 없다.** 최초 복구 기준 HEAD `1278449`, 조사/설계 커밋 `6495a42`, 구현 정본 `5943523`.

## 목표와 고정 계약

목표는 현재 전투의 렌더 제출·재질 전환을 크게 줄이는 것이다. 현30R 성장/적 수/전투 결과, 카메라·해상도·배속, 원본 스프라이트·방향·보행·그림자·강조·HUD·모든 효과 수/수명·사운드·입력을 보존한다. 품질을 낮추거나 표시 수를 제한해 지표를 줄이지 않는다.

실험 목표는 같은128적/실제15포탑에서 Draw Calls 70% 이상, SetPass 50% 이상 감소다. **달성 약속이나 전체 성능 예산이 아니다.** 작은 설정 개선만으로 두 목표를 달성하지 못하면 구조 개선을 별도 단계로 진행한다. 시간·GC·메모리 악화도 같이 보고한다. 목표기기/FPS 예산은 여전히 미정이다.

## 확인한 원인과 제한

- Unity6000.6.2f1 / URP17.6 / D3D12. SRP Batcher ON, Dynamic Batching 직렬화 필드 OFF. 설치된 URP 소스에서 Dynamic Batching API는 `Obsolete(..., true)`이고 필드는 과거 직렬화 호환용이다. 이 필드를 켜는 작업은 채택하지 않는다.
- 현재 Frame Debugger: 마스크 clear145 + draw145, 일반 Mesh 출력274. 마스크 합성은 이미 개체 내부 부품끼리 합쳐져 있지만 개체 간에는 개별 RT/제출이다.
- 런타임 SpriteRenderer: Unlit1322/공유재질1/텍스처19, Lit259/공유재질1/텍스처2. 단순한 `new Material` 전부 제거만으로 본체 제출 증가를 해결할 수 없다. TowerBaseShade15/재질15, 활성 TowerGlow15/재질15는 별도 소규모 후보다.
- SRP 설정 교차3회씩 결과는 [RESULTS.md](RESULTS.md), 원본은 [analysis.json](analysis.json). 전역 OFF는 Draw 감소·SetPass 증가라 미채택, ON 복원했다. 이 실험은 게임 Update 정지/시간0인 렌더링 진단이며 정상30R 성능이 아니다.
- Frame Debugger 이벤트617개 중616개는 `GetFrameEventData=false`다. shader/renderTarget/batchBreak 필드를 사실로 사용하지 않는다. 이벤트 type/info와 별도 재질/렌더러 인벤토리만 쓴다.

## 단계 A: 스프라이트 배칭 경로

현재 전체 SRP 설정을 즉시 OFF로 저장하지 않는다. 원본 SpriteRenderer 정렬/알파 블렌드/색/flip/애니메이션을 유지하면서 다음 후보를 한 번에 하나씩 비교한다.

1. **SpriteRenderer만 기존 SRP 경로에서 분리하는 호환 shader/material 경로.** 현재 URP Sprite Unlit/Lit의 정점·색·텍스처·flip 계산과 패스 태그를 그대로 보존한다. 전역 MPB 주입은 피격/선택 등 다른 소유자의 값과 충돌하므로 사용하지 않는다. Lit를 Unlit로 교체하지 않는다. Lit가 불필요하다는 별도 증거 없이 조명 경로를 제거하지 않는다.
2. **본체/그림자/체력바 텍스처 아틀라스.** 텍스처를 묶을 경우 원본 파일/PPU/필터/압축/프레임을 바꾸지 않는다. `Sprite.Create`로 잘라내는 보행·포신 등의 경로가 있으므로 Sprite Atlas 설정만 추가하고 완료로 처리하지 않는다. 실제 sprite.rect/UV/textureRect를 사용하는 모든 관련 경로를 대조하고, 지원하지 못하는 경우 기존 텍스처를 유지한다. texture-array 기반 현 마스크 아틀라스와 컬러 본체 아틀라스를 혼동하지 않는다.

자동 배칭에 맡기면 앞뒤 순서가 바뀌는 경우에는 해당 렌더러를 원래 경로로 남긴다. 색상 공간/알파 블렌드/점 필터/가장자리 픽셀 검사를 통과한 후보만 다음 단계에 포함한다. 전역 SRP OFF는 단계B 적용 후 두 지표와 CPU 비용까지 함께 좋아지는 경우에만 다시 채택 여부를 판단한다.

## 단계 B: 공용 마스크 아틀라스와 출력 배칭

외부 호출 `Present`, `PresentAtOrder`, `PresentGroundContactWithBand`, `PresentOverlay`, `IsVisible`의 의미와 기존 수명 계약을 보존한다. `CoreGuardianBodyMaskCache`의 동일 본체 재사용/카메라 키/기하 epoch/lease 소유권을 이어 쓴다. 내부 기록에 아틀라스 영역 핸들을 추가하고, 기존 개별 RT 경로를 fallback으로 유지한다.

### 생산과 픽셀 공간

- 카메라/프레임별 공유 페이지에 개체별 독립 사각영역을 할당한다. 모든 합성 결과를 화면 전체 하나의 silhouette로 합치지 않는다. 개체별 가림·윤곽·접촉·선택 규칙이 달라 서로의 내부를 지워서는 안 된다.
- 영역은 기존 ProducerRect와 같은 정수 픽셀 크기, 1mask texel=1screen pixel을 보존한다. 기존 atlas의 Texture2DArray 입력과 부품 GPU 행렬 곱을 재사용한다. CPU 좌표 베이크로 바꿔 이전에 발견된 정밀도 오차를 재도입하지 않는다.
- 페이지는 유효한 공통 포맷에서 한 번 clear하고, 개체 영역을 정점 데이터로 구분해 같은 패스/입력 아틀라스의 합성 geometry를 묶는다. 개체별 projection/영역 변환을 정점 속성에 싣는다. 서로 다른 threshold/opacity도 geometry 데이터로 보존하거나 별도 배치로 나눈다.
- 페이지의 UV 샘플링은 개체 영역 내부로 clamp한다. outline 최대8px, contact 상단2px, 기존 패딩과 비공유 crop 바깥의 clamp 동작을 보존한다. 여백/인접 영역의 alpha가 번지는 것을 허용하지 않는다.
- 처음부터 생산과 출력을 한 번에 교체하지 않는다. 기존 개별 mask를 atlas로 복사하는 중간 검증 경로로 출력 픽셀 동등성을 먼저 확인하고, 다음으로 직접 atlas 생산 경로를 검증한다. 복사 비용/추가 메모리를 별도로 기록하고 최종 비용으로 숨기지 않는다.

### 소비와 정렬

- 공통 페이지/패스/렌더 레이어/동일한 정렬 구간을 만족하는 output quad를 한 geometry 제출로 묶는다. outline color/width, mask bounds/texel size/origin, contact band 등 개체별 값은 정점 속성으로 보존한다. 각 개체의 고유 RT를 MPB `_MainTex`로 바꾸던 재질 전환을 제거하는 것이 목적이다.
- contact는 현재 sortingOrder43, cast shadow는42다. 같은 contact색의 alpha 중첩과 다른 투명 렌더러 사이 순서까지 비교한다. sortingOrder가 같다는 이유만으로 모든 투명 mesh를 일괄 합치지 않는다.
- 본체·무기·outline·선택(+4)·강화 scan(+5)의 상대 정렬을 유지한다. Outline들을 합칠 때 사이에 다른 body/효과가 그려져야 하는 경우 배치를 끊는다. 같은 정렬 키에서 거리/정렬 안정성을 보장할 수 없으면 그 항목은 개별 제출한다.
- 포탑 glow/grounding/피격의 shader/시간/색은 공용 mask 구조와 분리한다. 본 작업에서 국소 glow 수를 줄이거나 업데이트를 건너뛰지 않는다.

### 지원 범위와 실패

- 초기 사용 범위는 기존 검증된1920×1080, 직교8.5/4.25, 기본 projection/정수 viewport 등 [현 제한](../shared_body_mask_20261021/RESULTS.md)을 넘지 않는다. 이 범위가 현재 공유 캐시 계약 전체와 같다고 가정하지 않고 각 eligibility를 명시한다.
- 다중 카메라, 카메라 변경, 화면 가장자리, fractional viewport, 지원하지 않는 포맷/texture array/큰 도형/SortingGroup은 기존 개별 렌더 fallback으로 동작한다. 최적화 미적용은 허용하지만 표시 누락은 허용하지 않는다.
- 페이지 부족 시 새 페이지를 추가하거나 기존 경로로 돌린다. 고정 적 수/출력 수 상한으로 삭제하거나 먼저 끝내지 않는다. 한 mesh의16bit index 한계를 넘으면 제출을 분리한다.
- 반환된 slot은 generation/프레임 소유권으로 확인한다. pool 재등장·Disable·Destroy·재시작·씬 unload에서 외부 객체를 파괴하지 않고 자체 page/mesh/material/subscription만 정리한다. 실패 중간에도 stale mask/이전 개체 출력이 남지 않게 한다.

## 검증과 채택

새 제품 코드는 실패를 확인한 관련 회귀검사부터 추가한다. 기존169개 전체를 관성적으로 반복하지 않는다. 변경한 mask/outline/contact 경로에 맞춰 기존 SelectionOutline/ObjectEmphasis/PhotoShadow 검사와 실제 필요 범위만 실행한다.

1. **회귀 계약:** 개체별 겹침/중첩, 위치·크기·카메라 변경, 재사용·overflow·fallback·씬 종료, lease generation/cleanup, 색·폭·contact band·정렬. 실제 새 테스트 이름과 예상 건수는 구현 전 계획에 고정한다.
2. **픽셀:** 기존 `PixelComparison.cs`/관련 검사 도구를 확장해 같은 위상/프레임 전후를 대조한다.3종+보스/플레이어/포탑, X±/Y± 보행, 겹친 적, 화면 밖/가장자리, 줌8.5/4.25, 선택/피격/강화 동시 상태를 포함한다. 하나라도 설명되지 않은 픽셀 차이가 있으면 그 조건을 fallback하거나 후보를 폐기한다. 기존 보스9건을 신규 통과로 확대하지 않는다.
3. **엔진 성능:**128적/실제15포탑·고정dt·실제 렌더·동일 seed/상태·주기적 교체를 조건당3회. 이전 `RuntimeProbe.cs`를 새 폴더/ID로 재사용하되 draw/SetPass와 Main/Render Thread를 추가한다. 같은 HUD/효과/물량을 확인한다. Native frame 평균/중앙/p95/p99/max와 FPS최소/최대/평균, GC, draw/SetPass, API호출·마스크 생산/출력 제출 수, CPU 업로드·메모리/VRAM 유효 지표를 남긴다.
4. **Frame Debugger:** clear/합성/출력 이벤트의 실제 감소를 따로 확인한다. 데이터 유효성을 매번 검사하고 동결 진단 수치를 시간 비교에 혼합하지 않는다.
5. **실제 부하 후속:** 현재 버전 정상30R/장시간/반복 후 잔류를 이어간다. 목표 FPS 예산 미정이므로 전체 성능 통과 선언은 보류한다. D3D12 Editor GPU Profiler 지원 false는0ms가 아니다. GPU/Memory Profiler 도구 설치·그래픽 API 변경·빌드는 별도 허용 없이 하지 않는다.

최종 보고는 직전 단계 대비/최초 기준 대비를 구분한다. Draw 감소/SetPass 악화나 FPS 차이 불확정도 그대로 기록한다. 실제 구현·회귀/픽셀·계측 결과가 확인된 변경만 커밋한다. 사용자의 전투씬/TimeManager/PackageManagerSettings와 미추적 파일·폰트 접근 오류 표시는 제외·보존한다.

## 구현 계획

사용자 설계 승인에 따라 [구현 계획](PLAN.md)을 작성했다. 다음은 계획 검토/실행 방식 확인 후 A→B의 단계별 구현이다. 승인된 A/B 방향 자체는 다시 묻지 않는다. [GitHub 후보 조사](GITHUB_TOOLS.md)는 설치나 의존성 추가 승인이 아니다.
