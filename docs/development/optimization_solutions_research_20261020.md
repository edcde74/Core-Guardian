# 최적화4항목 해결방안 조사

2026-10-20 Asia/Seoul. 사용자 요청에 따른 자료 조사·현재 코드 대조·해결방안 보고다. 시작 HEAD b91e99b. 게임 코드·셰이더·설정 수정, 새 Unity 조회/Play/계측/빌드/실행·패키지 설치·push는 없다. 구현 승인/설계 확정이나 절감량 보장이 아니다.

측정 근거는 [Profiler 조사](profiler_investigation_20261020/README.md)·[FPS 표](profiler_investigation_20261020/FPS.md), 모든 전후 기록·품질 조건은 [공통 규칙](OPTIMIZATION_RULES.md)을 따른다. 수치/표현 규칙은 기존 담당 문서가 정본이다. 최신 Notion 최적화 계획을 재조회했으며 구현 상태는 시작 전이다. Notion의 초기 HUD우선 순서는 추가 Profiler 조사 전 제안으로 구분한다.

## 추천 요약
|항목|먼저 검토할 방법|후속/대안|주요 위험|
|---|---|---|---|
|1. 마스크 리소스/참조|본체 참조·배열 캐시, 용량을 유지하는 RT 재사용|크기별 RT 풀|UV/화면픽셀/접촉 밴드 변화, 메모리 증가|
|2. 같은 본체의 합성 공유|동일 본체·카메라·조건의 원본 실루엣을1회 합성, 출력은 역할별 유지|마스크 아틀라스/명령 묶음은 그 다음|패딩·알파 기준·프레임 위상·정렬 차이|
|3. 전투 표시/HUD|표시 전용 풀과 피해원ID별 HUD 행 재사용|실측 후 동적 UI의 Canvas 분리|남은 상태·이벤트/소리 중복·표시 누락|
|4. Session|재사용 버퍼·이동 전 버프 공급자 목록·같은 비교 규칙의 top3|공간 분할은 필요할 때, Jobs/Burst/ECS는 뒤로|Tick/조준/이벤트 순서·경계 판정 변화|

현재 계측에서는1→2가 지속 비용의 우선 과제다.3의 HUD/효과와4의 로직은 각각 별도 변경으로 성과를 분리한다. 아래 제안의 절감 시간/FPS/개선율은 모두 아직 미측정이다.

## 1. 마스크 리소스 재사용·참조 캐시
### 현재 코드 근거
CoreGuardianSelectionOutline.Present/EnsureMask는 화면상 정수 w/h가 하나라도 다르면 RT를 Release/파괴하고 새로 만든다. 같은 함수에서 자식 레이어 조회·quad 정점 배열 생성·최상단 레이어 LINQ 정렬도 반복한다. PhotoShadow.Present는 매번 자식 SpriteRenderer 배열을 만들고 이름으로 기존 그림자 레이어를 찾는다.

### 권장안A: 필요한 픽셀 영역과 할당 용량을 분리
- 개체별 RT를 유지하고 필요한 w/h가 현재 용량 안이면 새로 만들지 않는다. 용량 초과 때만 확대하거나 크기 구간의 RT로 교체한다. 작은 자세 변경마다 축소하지 않는다. 구간 크기는 실제 크기 이력을 보고 결정한다.
- 논리 픽셀 크기·viewport·crop matrix·사용 UV 사각형을 별도로 관리한다. 할당 RT가 더 커졌다는 이유로 본체를 늘리거나 축소해서 그리지 않는다. 사용 영역 밖까지 외곽선 샘플이 새지 않도록 clear/경계 처리를 한다.
- Point/Clamp/알파 문턱/기존 색공간과 포맷을 우선 보존한다. PhotoShadowContact의 _Band 및 _MainTex_TexelSize를 용량 크기와 혼동하면 발 접점이 달라질 수 있다. 픽셀 두께/접촉 밴드를 논리 영역 기준으로 정확히 대응해야 한다.
- 참조는 초기 아트/보행 rig 생성 뒤 비활성 레이어까지 캐시하고, 매 프레임 enabled/active/sprite/alpha 등 실제 상태만 검사한다. 런타임 자식 생성/교체·방향별 rig 변경·파괴/재사용 시에는 명시적으로 갱신한다. 움직임마다 바뀌는 Transform.hasChanged를 구조 변경 신호로 사용하지 않는다.
- quad의 Vector3[4]와 조회 List를 재사용한다. 최상단 sorting layer/order는 같은 비교 방식의 단일 순회로 찾되 동률 시 기존 Last 동작을 보존한다. SpriteMesh 캐시는 이미 있으므로 없는 최적화를 새로 도입한 것처럼 성과를 중복 계산하지 않는다.

이는 프로젝트 적용 설계 제안이다. 공식 [RTHandle17 문서](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/manual/rthandle-system-using.html)도 할당 크기와 viewport/UV의 구분을 설명한다. 다만 문서17.0과 설치17.6의 API 차이는 구현 전에 확인한다. RTHandle의 카메라 최대 해상도 기준 전체화면 할당을 개체마다 그대로 도입하지 않는다. 고정 크기 RTHandle로 래핑하는 것만으로 현재의 정확한 크기 재할당 정책이 해결되지는 않는다.

### 대안B: 크기 구간별 RT 풀
필요 크기/포맷별 유휴 RT를 빌리고 개체의 마지막 렌더 소비가 끝난 뒤 반환한다. 메모리 예산은 유휴 RT 회수로 관리하며 표시 중인 개체를 제거하지 않는다. 서로 다른 개체가 같은 활성 RT를 덮어쓰면 안 된다.

Unity의 [GetTemporary](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/RenderTexture.GetTemporary.html)는 크기/포맷에 맞는 임시 RT 풀을 사용하지만 내용 보존은 보장하지 않는다. 현재 출력 MeshRenderer가 나중에 마스크를 읽으므로 ExecuteCommandBuffer 직후 무조건 반환하는 치환은 적합하지 않다. 수명·clear·용량 구간까지 설계해야 하며 A와 어느 쪽이 나은지는 실제 CPU/메모리 비교로 정한다.

### 검증
같은 위치/이동/보행/방향전환·가장자리·보스·카메라/해상도 변경에서 RT Create/Release·개체별 요구/할당 크기·최대/잔류 메모리, 함수별 ms와 GC를 비교한다. 사진 접촉·1px강조·2px선택선·피격/강화 중첩을 같은 배율/위상으로 비교한다. 메모리가 늘 수 있으며 재할당 제거를 전체 함수 비용 제거로 계산하지 않는다.

## 2. 같은 본체의 마스크 공유
### 현재 코드 근거
강조와 사진 동적 접촉은 각자 SelectionOutline 인스턴스로 같은 본체 조각을 합성한다. 고정128명/15탑에서 강조145+접촉129의 표시 마스크274개가 확인됐다. 두 출력의 역할·패딩·접촉 밴드는 같지 않다.

### 권장안: 원본 실루엣 데이터와 소비 효과 분리
- 본체별 provider가 현재 보이는 레이어·합집합 Bounds·화면 영역을 제공하고, 조건이 맞는 소비자에게 같은 실루엣 RT를 빌려준다. 외곽 강조·접촉·선택·강화의 출력 shader/order/색/수명은 각각 유지한다.
- 한 프레임 캐시라도 target ID·camera/viewport/행렬·표시 레이어/프레임/변환·alpha threshold·논리 사각형/패딩을 구분한다. camera나 알파 기준이 다르면 같은 결과로 취급하지 않는다. Time.frameCount 하나만으로 재사용하지 않는다.
- 더 넓은 공통 영역을 합성하고 소비자별 기존 영역을 UV/quad/밴드로 재현하는 방안을 검토한다. 폭/패딩/그림자 접점이 맞지 않으면 우선 **본체 참조/Bounds 계산만 공유**하고 RT 공유는 보류한다.
- 보행/포즈가 확정된 시점에 합성하고 소비자가 읽는 순서를 정한다. 완전히 정지한 본체도 카메라/피격 알파/레이어 상태가 달라지면 갱신한다. 임의의 N프레임마다 갱신해 애니메이션/접촉을 늦추지 않는다.
- 개체 파괴/씬 종료·재사용 때 소유권을 해제하고 모든 소비자가 끝나기 전 RT/mesh/material을 반환하지 않는다. body tint/property block의 기존 복원 계약은 유지한다.

공유의 가능성과 구체 구조는 소스/계측에서 도출한 **프로젝트 제안**이며 Unity가 이 게임에 대해 보증하는 기법은 아니다. 동일 조건의 중복 합성을 줄일 잠재력은 있지만 전체 마스크 수·ms가 절반으로 줄어든다고 약속하지 않는다.

### 후속 대안: 아틀라스/그리기 명령 통합
공통 마스크가 안정화된 뒤 여러 마스크를 RT 아틀라스에 배치하거나 카메라 단위 command buffer로 묶는 방안을 비교한다. atlas tile 경계·외곽선 샘플 여유·정렬·갱신/이주·대형 RT 메모리 위험 때문에 첫 변경으로 묶지 않는다. 개별RT→atlas는 단순 Sprite Atlas 설정과 다른 렌더 구조 변경이다.

### 검증
provider 생성/합성 호출 수·소비자 수·중복 제거율·RT/재질/SetPass·전체 CPU/GPU 유효 지표를 기록한다. 서로 다른 개체가 겹칠 때 각자 외곽선을 유지하는지, 앞/뒤/좌/우·다른 threshold·선택/피격/강화 동시 출력이 같은지 확인한다. 화면 전체 하나의 합집합으로 합쳐 개체 사이 윤곽을 없애지 않는다.

## 3. 전투 표시 오브젝트 풀링·HUD 행 재사용
### 권장안: 표시 계층부터 작게 분리
CoreGuardianPrototype의 projectileViews, CombatArt의 피격/처치 효과, Hud의 피해/금화/코어 숫자를 종류별 표시 풀로 관리한다. Session.ProjectileState와 피해 이벤트는 우선 유지한다. 표시 풀링과 전투 모델 풀링을 한 번에 바꾸지 않는다.

공식 [ObjectPool](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/pool/objectpool1) 및 [Unity 풀 소스](https://raw.githubusercontent.com/Unity-Technologies/UnityCsReference/master/Runtime/Export/ObjectPool/ObjectPools.cs)를 확인했다. 비어 있으면 새 인스턴스를 만들고 반환 시 유휴 보관 한도를 넘으면 파괴하는 방식이다. defaultCapacity는 사전 생성 수가 아니다. 별도 prewarm과 피크 관측이 필요하다. 활성 표시 수를 maxSize로 제한하거나 오래된 효과를 강제 회수하지 않는다.

- 대여 중 ID→뷰 연결, 위치/회전/스케일·sprite/material·색/알파·sorting·타깃·시작시간/수명·잔상/콜백을 초기화한다. 금화는 텍스트+아이콘 그룹을 함께 재사용하고 글자 길이에 따른 폭을 다시 반영한다.
- 프레임 갱신 루프와 반납 루프의 처리 순서는 기존 것을 유지한다. 종료/취소/재시작/씬 이동에서 중복 반환·이전ID 참조·이벤트 중복을 검사한다.
- 표시 OnEnable에서 전투 이벤트/사운드를 재발생시키지 않는다. 발사/명중/처치는 기존 모델 이벤트를 기준으로 하고 지원 전투 무음을 보존한다. 효과 표시 수·기존 .85초 숫자 수명과 개별 효과 수명은 줄이지 않는다.
- 적 본체 풀링은 초기 대상에서 제외하는 제안이다. 복잡한 방향 rig·강조/접촉 캐시·체력바·이벤트/애니메이션 초기화까지 수명이 얽혀 있어 표시 풀의 회귀를 먼저 줄인다.

### HUD 행 재사용
피해원 키별 row/value/bar/icon 참조를 유지한다. 값 변화는 텍스트와 막대만, 정렬 변화는 필요한 sibling 위치만 갱신한다. 최대 피해가 바뀌면 다른 모든 행의 비율도 다시 계산해야 한다. 동일 피해 정렬의 기존 문자열키 비교, 역할 심볼·빈 트랙·황동 손잡이·접힌 중 집계/펼침·WASD 분리는 유지한다. 숨겨진 보관 행은 layout에 참여하지 않게 한다.

공식 [UI 최적화 지침](https://unity.com/how-to/unity-ui-optimization-tips)은 반환 시 disable→reparent, 대여 시 reparent→값 갱신→enable 순서를 설명한다. 실제로는 같은 부모 아래 비활성 보관해 reparent를 피하는 안도 비교한다. Canvas/Layout 분리는 행 재사용 후 프로파일에 비용이 남을 때만 검토한다. 기존 ScrollRect의 스크롤 위치/마스크/정렬과 접기 클릭 영역을 함께 보존한다.

### 검증
미리 준비된 수 이내의 안정 구간에서 새 생성/Destroy 횟수, 풀 miss/확장/활성/유휴/메모리·UI rebuild를 기록한다. 초기 준비와 피크 초과 확장 비용도 따로 측정한다. 전체 표시 동시 수·수명·피해 누적/최대 대비 막대·실제 클릭/접기·발사당 소리 횟수·재시작 잔류가 같아야 한다. 풀링했다고0할당으로 단정하지 않는다.

## 4. Session 탐색·임시 할당 감소
공식 [컬렉션 재사용 지침](https://docs.unity.com/en-us/engine/6000.6/manual/analysis/performance-memory/managed-memory/optimizing-code-managed-memory/reusable-code)은 List/Dictionary를 보관하고 Clear로 용량을 재사용하는 접근을 제시한다. 아래 알고리즘은 실제 Session/CombatRules를 읽고 제안한 것이며 결과 동등성 검증이 필요하다.

### 첫 단계: 같은 계산을 적은 할당으로 수행
- 매 하위 StepTick 시작에 기존 순서대로 살아 있는 적과 **Buff 공급자만의 목록**을 재사용 버퍼에 수집한다. 같은 이동 전 위치에서 모든 버프 값을 먼저 계산하고 그 다음 이동한다. 현재 최대 BuffSpeed 선택과 자기 자신 제외를 보존한다. N명마다 다시 N명을 훑는 대신 N×B(B=Buff적 수)로 후보를 줄이되 동일 거리 연산/경계 판정을 우선 유지한다.
- ToDictionary/ToArray·각종 Where/Select/closure를 필요한 핫패스에서만 명시적 루프·보유 버퍼로 바꾼다. 사망/퇴장/재시작·라운드 전환 때 참조와 길이를 초기화한다. 파괴/소환/버프 변경을 놓치는 영구 캐시를 만들지 않는다.
- 포탑 타깃 선택은 CombatRules의 병종→HP→남은경로→ID 비교를 그대로 사용해 top3를 한 번에 유지한다. 최초 조준·유지 조준·일반 적 진입 시 보스 조준 해제는 기존 분기를 보존한다. 플레이어/포탑의 서로 다른 병종 순서를 섞지 않는다.
- 투사체/타워 순회용 복사본은 기존 ToArray의 snapshot 의미를 보존한다. 이벤트 콜백에서 목록이 바뀌거나 재진입해도 공유 임시 버퍼가 덮어써지면 안 된다. 투사체 정리는 순서가 바뀌는 swap-remove 대신 안정 순서를 유지하는 방법을 검토한다.
- 현재 Tick의 최대.05초 하위 분할과 누적 시간/발사 while loop는 유지한다. 느린 프레임에서 Tick을 생략/상한 제한하는 방법으로 성능을 맞추지 않는다.

### 후속 후보
Buff 목록 축소 후에도 거리 탐색이 비용을 차지하면 그리드/공간 해시로 후보만 줄이고 같은 정확 판정을 수행한다. 이동 셀 갱신·경계·최대 범위 누락 위험이 있어 처음부터 도입하지 않는다. Jobs/Burst/ECS 전환은 현행 관리 객체/이벤트 구조의 변경 범위가 크므로 위 개선 후에도 측정상 필요할 때 별도로 설계한다.

### 검증
같은 로직·정책 버전·대응 시드·입력/dt에서 생성/이동/조준/발사/명중/피해/재화/코어/증강/승패와 이벤트 순서를 대조한다. 거리 경계·같은 HP/남은경로·ID·보스/일반 동시 진입·Buff 처치/퇴장·큰 dt/빠른 연사를 포함한다. 같은 틱 결과만이 아니라 중간 타깃/발사/사운드 이벤트도 같아야 한다. 기존 비지정시드8회나 기능QA를 이 대조로 대체하지 않는다.

## Unity 설정과 실제 게임 사례
- 저장된 UniversalRP.asset에는 SRP Batcher=1, RenderScale=1, MSAA=1, Depth/Opaque 요구0이 이미 존재한다. QualitySettings의 해당 URP GUID 참조도 확인했다. 실시간 유효 pipeline/quality는 구현 직전 에디터에서 재조회한다. 이번에는 Unity에 접속하지 않았다.
- SRP Batcher를 켜면 개체별 RT와 ExecuteCommandBuffer가 자동으로 합쳐진다고 판단하지 않는다. MPB를 무조건 추가하면 SRP Batcher 호환성을 해칠 수 있다. [공식 호환 조건](https://docs.unity.cn/6000.6/Documentation/Manual/SRPBatcher-Materials.html)의 검색 본문을 확인했으나 직접 열기는 오류여서 해당 버전의 전체 본문 검증은 남았다. 실제 경로는 Frame Debugger에서 확인해야 한다.
- [Skul 공식 사례](https://unity.com/resources/skul-the-hero-slayer-case-study)는 Sprite Atlas/Tilemap과 CPU 프로파일링을 참고할 근거다. 이 프로젝트의 개체별 렌더 마스크 재생성을 Sprite Atlas 설정만으로 해결할 수 있다는 증거는 아니다. 사례의 물리 정밀도/아트 재작업을 가져오지 않는다.
- [V Rising 공식 이식 사례](https://unity.com/blog/5-key-learnings-v-rising-playstation-5-port)는 Main Thread 병목에 맞춘 Jobs 활용·여러 도구의 교차 검증·도구 변경 회귀 검사의 참고다. 그 게임의 ECS 구조/개선율이 우리 게임에 그대로 적용되지는 않는다.
- RenderScale/해상도/후처리·물량·음성·표시 수를 낮추는 안은 이번 추천에 포함하지 않는다. VSync/제한 해제로 수치만 달라진 것을 작업량 절감으로 보고하지 않는다.

## 공식 유튜브 확인 범위
- Unity 공식 UI 지침에서 [Unite Europe2017 — Squeezing Unity: Tips for raising performance](https://www.youtube.com/watch?v=_wxitgdx-UI)를 직접 연결하고24:55의 UI rebuild 설명을 안내하는 것을 확인했다. 영상 본문은 접근 가능한 텍스트가 거의 없어 전체 시청/자막 학습 완료로 기록하지 않는다. 위 UI 권고는 읽을 수 있는 공식 지침 본문으로 교차 확인했다.
- [Unity 공식2026 Seoul 렌더링 세션](https://www.youtube.com/watch?v=oJnRMqftoiE)은 Unity 게시자/설명 검색 결과에서 Unity6.6 GPU 병목 분석·URP 기능을 다루는 것을 확인했다. 제목이 검색에 비어 있고 전체 본문/자막은 확보하지 못해 세부 설정이나 수치를 이 영상에서 인용하지 않는다.
- 추가 기본 강연과 조사 범위는 [이전 공식 자료 조사](optimization_research_20261020.md)를 따른다. 기법 적용 근거는 확인한 공식 문서와 현재 코드·실측이며 미확인 영상 내용을 근거로 채택하지 않는다.

## 적용 전 정할 최소 계약
각 변경은 별도 실험ID/복구 기준으로 구분하고 마커를 나눈다. 기존128명 조건의 normal/Deep/고정 장면은 서로 다른 자료다. 전후 검증에는 먼저 재현 가능한 같은 조건·워밍업·관측 길이·반복 횟수(기본3회 이상)를 고정한다. 기존 가변dt 반복을 고정입력 재생으로 부르지 않는다.

공통 결과표에는 **최저/최고/평균 FPS, 프레임 ms 분포, 관련 함수 ms/호출 수, GC 할당/회수, Draw Calls/SetPass, 최대/잔류 메모리, 품질 비교**를 남긴다. 개선 없음/악화도 보존한다. 실제 형상/수치/타이밍과 품질 보존 검사를 통과한 뒤에만 성능 향상을 인정한다. 어느 항목도 아직 구현하지 않았다.
