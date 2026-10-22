# SpriteAtlas 적용 조사·확장 기준

2026-10-23. 사용자 요청: Unity 공식 문서에 근거해 현재 게임과 향후 몬스터·타워 추가를 고려한 제작/최적화 방안 보고. 조사 기준 HEAD `569e3e5`, Unity6000.6.2f1. 에셋·설정 변경, Unity 조작/Play/테스트/빌드/실행파일/push 없음. 아래는 적용 전 제안이며 성능 개선 수치는 미측정이다.

## 확인된 현재 구현

- `Assets/Art/Atlases/Sprites/CG_Enemies.spriteatlasv2`는 packables가 비어 있다. meta는 Max2048/Point/mipmap off/readable off/rotation off/tight off/padding4. EditorSettings 저장값 SpritePackerMode5. 이번 조사에서 에디터 실시간 상태를 조회하거나 변경하지 않았다.
- `CoreGuardianCombatArt.cs`는 Resources/CoreGuardian/Art/CombatB의 9개 스프라이트를 이름으로 읽는다. EngineerChip/SingleBolt/MultiBolt와 피격·사망·버프·획득 표시다. 실제 폴더는 Art/Effects/Combat 아래다.
- `CoreGuardianOriginalWalk.cs:205,226-227`은 source.rect/원본 부위 좌표와 source.texture로 런타임 Sprite.Create를 수행한다. 패킹 이후 texture가 아틀라스를 가리키면 원본 rect 기준과 어긋날 수 있다. Rotation/Tight OFF만으로 해결되지 않는다.
- `CoreGuardianOriginalMotion.cs:42-43`은 textureRect로 잘라 타워 동작 부품을 만든다. 회전/비직사각 패킹 지원을 검토해야 한다. CoreAmbient/TowerGlow/TowerGrounding도 textureRect·주변 픽셀을 사용한다.
- `CoreGuardianMaskAtlas.cs:22-40`과 BodyMaskCache는 원본 Texture2D 키·크기/hash로 별도 마스크 배열을 조회한다. SpriteAtlas 패킹으로 texture 참조가 바뀌면 B 조회가 실패해 원본 fallback으로 갈 가능성이 있다. 이 배열은 표시용 SpriteAtlas와 다른 시스템이다.
- RouteMeshBuilder는 에디터에서 sprite.uv/texture로 길 메시·재질을 저장한다. 이미 저장된 길 메시를 포함한 환경 원본을 뒤늦게 패킹하면 저장 UV/재질과의 일치 여부를 확인해야 한다.
- 2D Tooling4.0.1이 manifest에 있으며 설치된 공식 패키지 문서의 메뉴는 Window > Analysis > Sprite Atlas Analyzer다.

## 권장 분류와 추가 리소스 대응

아트 폴더는 파일 관리 기준이다. 아틀라스는 동시 사용·필터/압축·셰이더/재질·메모리 수명 기준으로 결정한다. 아래 묶음은 고정 최종 개수가 아닌 시작안이다.

|시작 이름|대상|확장 기준|
|---|---|---|
|CG_CombatFX_Common|현재 포탄3종, 피격·사망·전투 버프 등 공통 전투 효과|같은 텍스처 설정의 신규 일반 포탄/효과 추가. 다른 재질·블렌딩은 별도 검토|
|CG_Enemies_Common_01|동시에 나오는 일반 적의 본체·방향·보행 부위|먼저 런타임 자르기/B 호환 수정. 유닛 하나의 전체 프레임을 같은 묶음에 유지|
|CG_Towers_Common_01|기본 타워 본체·고정부·가동부|동작·발광·마스크 호환 후 적용. 타워 아이콘은 UI로 분류|
|CG_UI_Common|상시 HUD 아이콘·버튼·공통 패널|대형 카드/드문 화면은 사용성과 페이지 낭비를 보고 분리|
|CG_Boss_<Group>|희귀·대형 보스 리소스|실제 동시 등장과 로딩 정책이 분리될 때만 별도 묶음. 보스마다 무조건1개는 아님|

새 리소스는 전용 입력 폴더를 Objects for Packing에 등록하면 하위 스프라이트가 패킹 대상이 된다. 부모 Art 전체를 넣지 않는다. 현재 Resources 로딩 키와 Sprite.name은 보존하고 필요한 하위 폴더를 여러 개 등록할 수 있다. 같은 스프라이트를 서로 다른 기본 아틀라스에 중복 등록하지 않는다. 신규 유닛은 본체만 추가하고 보행/부품을 빠뜨리지 않게 전체 입력 목록을 관리한다.

페이지 수·실제 패킹 크기·빈 공간·사용 비율·텍스처 메모리를 추가 때마다 확인한다. 여러 페이지가 됐다는 이유만으로 실패는 아니지만, 같이 렌더되는 프레임이 페이지를 오가면 최적화 이점이 줄 수 있으므로 분리/재편 후보로 본다. 몬스터 수량128개는 이미지128세트를 의미하지 않는다. 메모리는 주로 고유 이미지/프레임과 패킹 크기에 좌우된다.

향후 자동화는 에디터/빌드 전 폴더·라벨 규칙으로 포함 목록을 갱신하고, 누락·중복·설정 불일치·페이지/메모리 증가를 보고하도록 한다. 빌드 전 생성은 차후 명시 빌드 수행 시의 파이프라인 제안이며 이번에 빌드를 실행하지 않는다. 초기에는 기존 로딩과 Include in Build를 사용하고, 실제 메모리 수명 분리가 필요할 때 late binding/Addressables 도입을 별도 판단한다. 아틀라스를 나누는 것만으로 Resources 참조가 해제되거나 메모리 절감이 보장되지는 않는다.

## 초기 Inspector 설정 제안

- Master/V2, Include in Build ON. 메뉴 Assets > Create > 2D > Sprite Atlas. 기존 원본 참조를 유지하고 Objects for Packing/Pack Preview로 결과 확인.
- Point, mipmaps OFF, sRGB ON을 현재 픽셀 아트 기준으로 유지. 다른 필터가 필요한 효과는 별도 묶음.
- Read/Write OFF를 기본으로 하되 실제 CPU 픽셀 접근 대상은 별도 검토. Sprite.Create만을 위해 CPU 픽셀 읽기를 켜지는 않는다.
- Allow Rotation OFF, Tight Packing OFF로 시작: 현재 사각 부위 자르기·직접 UV 접근 및 UI 호환을 우선한다. 일반적인 공간 효율 권장과 달리 이 프로젝트에는 호환 검증이 선행돼야 한다.
- Padding4 시작. 코어/타워 글로우는3~5픽셀 주변 샘플링 코드가 있어 이 값만으로 안전을 가정하지 않는다. UV 경계 제한/전용 여백을 확인한다.
- Max Texture Size2048을 시작 상한으로 제안. 4096 고정/무조건 대형화를 피하고 실제 페이지·낭비·전환 비용으로 조정한다. 계산상 무압축RGBA32의 2048²은16MiB,4096²은64MiB(각1페이지, mipmap/CPU복사 제외)다. PNG 파일 크기는 GPU 메모리 크기가 아니다.
- 최초 품질 비교는 원본 해상도/무압축으로 조건을 맞춘다. 이후 Windows 압축은 별도 픽셀 품질·메모리 비교 후보이며 자동 채택하지 않는다.

## 적용 순서와 성공 판정

1. 포탄·전투 효과만 패킹하고 동일 조건 전후 비교. 런타임 자르기 대상이 적은 작은 범위부터 시작한다.
2. 적 본체의 원본/패킹 좌표 호환과 B 키 매핑을 해결한 뒤 일반 적 아틀라스 적용. 방향·보행·부위·강조·넓은 그림자와 B fallback 발생을 확인한다.
3. 타워 동작·발광·마스크 확인 후 타워 적용. HUD는 Canvas/재질/정렬 조건을 따로 확인한다. 길 메시·마스크 배열·TMP 폰트 아틀라스는 일괄 포함하지 않는다.
4. 사용자가 지정한 적128·타워15 조건에서 같은 해상도/배속/상태/공격·포탄 수로 평균/최소/최대FPS, frame ms, CPU/Render Thread, Draw/SetPass, 텍스처 메모리를 비교한다. 길어진 임의120초를 목표로 삼지 않는다. 목표부하를 실제 유지한 구간만 비교한다.

아틀라스는 텍스처 전환/렌더 제출 비용 감소 후보다. 탄환 이동·충돌·타깃 탐색·이벤트 및 투명 오버드로를 직접 제거하지 않는다. 같은 아틀라스여도 재질·패스·정렬 때문에 여러 draw가 생길 수 있다. FPS 증가량과 GTX750Ti60FPS 달성은 현재 미측정이다.

## 읽은 공식 근거

- [Unity6.6 Sprite atlases](https://docs.unity.com/en-us/engine/6000.6/manual/unity2d/sprite/atlas/atlas-introduction): 동시 사용/압축 조건/사용 빈도별 분류와 SRP Batcher 주의.
- [Unity6.6 Create a sprite atlas](https://docs.unity.com/en-us/engine/6000.6/manual/unity2d/sprite/atlas/create-sprite-atlas): V2/폴더 등록/미리보기/중복/Analyzer.
- [Unity6.6 Inspector reference](https://docs.unity.com/en-us/engine/6000.6/manual/unity2d/sprite/atlas/sprite-atlas-reference): rotation/tight/padding/read-write/mipmap/플랫폼 설정.
- [Sprite.texture API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Sprite-texture.html): 패킹된 경우 texture는 아틀라스를 반환한다.
- [Sprite.textureRect API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Sprite-textureRect.html): tightly packed 상태에서 textureRect 사용 제한.
- 설치된 Unity 공식2D Tooling4.0.1 `Documentation~/GetStarted-sprite-atlas-analyzer.md`: Analyzer 메뉴와 보고서 제공 확인. 영상은 시청하지 않았으며 시청 근거로 인용하지 않는다.
