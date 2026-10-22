# Unity 전체 에셋 디렉터리 정리 · 2026-10-23

## 범위·결과

사용자는 Art/Scripts/Audio를 예시로 들었고, **일부3종 중심이 아니라 현재 Unity Assets 파일 전체를 큰 용도별 분류·하위 폴더로 통일**하라고 정정했다. 이 요청에 따라 기존552개 전부를 분류하고 전투씬1개를 제외한551개를 이동했다. 파일 삭제/게임 리소스 교체가 목적이 아니다. 기존 사용자 복구씬2개는 GUID/내용을 유지해 Scenes/Recovery로 이동했다.

복구 기준: **`c1203f11c9469c7370dd0ec353151596a21168d3`** (이동 전 파일 전체·GUID/hash/의존성/조립 단위·이동표 기록). 앞선 리소스 삭제 복구는 별도 `5ae39a1`이다.

|최상위|정리한 하위 분류|기존 파일 수|
|---|---|---:|
|Art|Characters(Player/Enemies/Towers/Core), Environment(Backgrounds/Tiles/Meshes), UI, Effects(Combat/Shadows/MagicAura), Atlases, Rendering(Materials/Shaders)|286|
|Audio|선택 런타임 음원·catalog, Libraries(RPG Essentials/MagicAura)|98|
|Data|Balance, Animation, Presentation|19|
|Documentation|에셋 설명·아트 메타데이터·타사 사용 설명|6|
|Fonts|런타임/SDF·원본 폰트·관련 소재/라이선스|14|
|Prefabs|Gameplay/Units, Characters/Enemies/Boss, Effects|10|
|Scenes|기존 전투씬, Recovery|3|
|Scripts|Runtime/Domain(규칙/Common/Enemies/Towers), Bootstrap, Presentation(Audio/Characters/Combat/Rendering/UI/World), Editor, Tests(EditMode/PlayMode)|106|
|Settings|Rendering, Input, Editor, Resources/TMP Settings|10|

기존552개 외에 사용자 탐색용 [ProjectStructure.md](../../../Assets/Documentation/ProjectStructure.md)1개를 새로 추가했다. 전체 파일 위치는 [DIRECTORY_INDEX.md](DIRECTORY_INDEX.md), 이전→새 경로는 [path_map.csv](path_map.csv)에서 검색할 수 있다. 실제 적용 [move_map.json](move_map.json), 기존 [inventory](inventory_before.json)와 [이동 결과](move_result.json).

## 이름·경로 계약

- 날짜/ScreenTests/제작자/임시 실험명이 최상위 탐색 기준이던 구조를 없애고 역할별로 모았다. 일반 길 메시·재질12개는 `Route_Order_01_Part_02`, `Route_Material_05`처럼 용도/순서 이름으로 통일했다.
- Sprite.name, MonoBehaviour 파일/클래스명, Resources key처럼 게임이 이름으로 찾는 계약은 유지한다. 예를 들어 Up_0/Down_0 및 D2_*는 실제 테스트와 동작에서 읽는다. 이름만 바꿔 선택/피격/보행 계약을 깨지 않는다. 해당 파일은 역할별 폴더와 색인에서 찾는다.
- **Resources key227개 전부 동일**하게 보존했다. 각 용도 폴더 안에 Resources/CoreGuardian/...를 두므로 Art/Audio/Data/Fonts로 모아도 기존 Resources.Load가 그대로 로드한다. 임의 경로 변환 런타임 코드/리다이렉터를 추가하지 않았다.
- **스크립트102개의 컴파일 assembly 동일**. Fortress.Game asmdef는 Domain 범위만 포함하고, 기존 global presentation은 그 밖에 둔다. 테스트2개 assembly와 Welcome assembly 이름/참조는 그대로다.
- 코드10개만 실제 디스크 경로에 맞춰 갱신했다: 테스트9개 및 RouteMeshBuilder1개. 런타임게임 코드의 로직/수치/리소스 키 변경 없음. Route 제작 도구는 메시와 재질의 새 출력 폴더로 저장하도록 수정해 구형 폴더를 다시 만들지 않는다.
- Unity가 정한 프로젝트 루트 Library/Packages/ProjectSettings/Logs/Builds 등의 기능 폴더와 별도 HUDDesigner 저장소는 에셋 분류 대상으로 옮기지 않았다. 기존 제작·기록·캡처는 docs 등 프로젝트 바깥 분류를 유지했다.

## 실패·교정

1. 첫 일괄 시도에서 새 폴더를 import 일시정지 중 생성해 MoveAsset 부모로 인식되지 않았다. **이동0·코드변경0에서 중단**했다. [원본](first_attempt_failed.json). 부모 폴더를 먼저 등록한 다음 일괄 이동하도록 고쳐551개 이동 성공. 같은 원인 그대로 반복하지 않았다.
2. 검증에서 길 에셋12개의 내용 hash 차이가 검출됐다. Unity MoveAsset의 rename이 `m_Name`을 바꾼 것이며, checkpoint 원본과 **이름 줄을 제외한 모든 직렬화 데이터가 동일**함을 확인했다. [증명](name_only_changes.json), [최초 검증](first_verification_name_changes.json). 메시 정점/UV/인덱스·재질 파라미터 변경으로 숨기지 않았다.

## 실제 검증

[최종 검증](verification.json):

|확인|결과|
|---|---|
|원본552개 GUID→새 경로|552/552 동일|
|원본.meta 내용|552/552 동일|
|원본 파일 내용|530개 byte 동일·코드 경로10개·이름만 변경12개|
|에셋 의존성 GUID 집합|552/552 동일|
|기존 Resources key 실제 로드|227/227 성공·새 파일 경로 일치|
|스크립트 컴파일 assembly|102/102 동일|
|커스텀 마스크 source dependencyHash|92/92 일치·폴더 이동에 의한 fallback 전환 없음|
|컴파일|completed·failed=false·errors0|
|보호된 전투씬/사용자 설정2개|SHA2563/3 동일|
|빈 폴더 정리|파일/참조가 없는132폴더만 Unity API로 제거|

대상 EditMode 검사는 필터/기대 수를 미리 확인하고 실행했다. **RouteMesh7/7 + PhotoShadow10/10 + PlayerFacing5/5 =22/22 통과**. [길 검사](route_tests.json), [그림자 검사](shadow_tests.json), [방향 검사](facing_tests.json). 기존169/72 전체 검사를 반복하지 않았다.

최종 Play stopped·scene clean·컴파일/업데이트 없음·Profiler OFF/driver true. 사용자 Play/다른 검사 중단 없음. 전투씬의 경로/파일 bytes를 유지했고 새 씬 저장·Play·빌드·실행파일·push는 하지 않았다. 기존 사용자 Settings/폰트 도구 접근오류/기록 원본은 보존했다. FPS/메모리 절감이나 사람 플레이·청취 통과를 주장하지 않는다.

## 문서·후속

현재 아트 기준 문서/QA 색인/오디오 정본과 실제 파일 링크를 갱신했다. [문서 변경 목록](document_changes.json). 과거 JSON/CSV/캡처/당시 실행한 진단 C#는 실행 증거라 일괄 문자열 치환하지 않았다. 과거 도구를 재실행하기 전 path_map.csv로 해당 경로를 현재 위치로 맞춘다.

기록/캡처는 앞선 사용자 요청대로 보존한다. 정리한 SpriteAtlas는 `Assets/Art/Atlases/Sprites/CG_Enemies.spriteatlasv2`이며 입력은 기존 그대로 비어 있다. 아틀라스 생성/재패킹·아트 재채택은 하지 않았다. A 보류·B 제한/원본 출력/개별 RT/fallback, 기존 게임 밸런스와 품질은 유지한다.
