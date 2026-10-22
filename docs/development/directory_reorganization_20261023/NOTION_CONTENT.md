# 적용 상태
기준일: <mention-date start="2026-10-23"/>
현재 Unity Assets 전체를 용도별로 정리한 **적용 완료 구조**입니다. 기존552개 파일을 모두 분류하고 전투씬1개를 제외한551개를 이동했습니다. 안내 문서1개를 추가해 현재 관리 파일은553개입니다.
이 문서는 파일 위치·분류·이름/경로 관리만 담당합니다. 게임 규칙·밸런스·아트 승인·사운드 선택의 세부 기준은 각 담당 기획서를 따릅니다.

# 전체 폴더 구조
```text
Assets/
├─ Art/
│  ├─ Characters/
│  │  ├─ Player/                  플레이어 원본·방향·동작 프레임
│  │  ├─ Enemies/                 병사·보스 원본과 보행 프레임
│  │  ├─ Towers/                  타워 원본·공격 동작
│  │  └─ Core/                    코어 스프라이트
│  ├─ Environment/
│  │  ├─ Backgrounds/             배경 이미지
│  │  ├─ Tiles/                   타일 Sprite·Tile 에셋
│  │  ├─ TilePalettes/             타일 팔레트
│  │  ├─ Meshes/Routes/            병합된 길 메시
│  │  └─ MagitechFortress/         요새 타일 원본·Tile 묶음
│  ├─ UI/                         HUD·카드·TMP UI 리소스
│  ├─ Effects/
│  │  ├─ Combat/                  포탄·전투 효과
│  │  ├─ Shadows/                 넓은 그림자
│  │  └─ MagicAura/               기존 이펙트 소재
│  ├─ Atlases/
│  │  ├─ Sprites/                 Unity SpriteAtlas
│  │  └─ MaskData/                캐릭터 마스크 배열
│  ├─ Rendering/
│  │  ├─ Materials/               재질
│  │  └─ Shaders/                 셰이더
│  └─ Editor/                     Editor용 이미지
├─ Audio/
│  ├─ Resources/CoreGuardian/Audio/   현재 선택 음원·catalog
│  └─ Libraries/                  원본 사운드 라이브러리
├─ Data/
│  ├─ Balance/                    밸런스 CSV
│  ├─ Animation/                  애니메이션 메타데이터
│  └─ Presentation/               표시·접지 관련 데이터
├─ Documentation/                 에셋 안내·출처·타사 설명
├─ Fonts/                         실행용 폰트·SDF·원본·라이선스
├─ Prefabs/
│  ├─ Gameplay/Units/             플레이어·코어·타워·병사 프리팹
│  ├─ Characters/                 적·보스 관련 프리팹
│  └─ Effects/                    이펙트 프리팹
├─ Scenes/
│  ├─ CoreGuardian_CombatPrototype_20261008.unity
│  └─ Recovery/                   기존 사용자 복구씬
├─ Scripts/
│  ├─ Runtime/
│  │  ├─ Domain/
│  │  │  ├─ Rules/                전투·라운드·증강·데이터·모델
│  │  │  ├─ Common/               공통 모델
│  │  │  ├─ Enemies/              적 기본 로직
│  │  │  └─ Towers/               타워 기본 로직
│  │  ├─ Bootstrap/               씬 연결·그리드 입력
│  │  └─ Presentation/
│  │     ├─ Audio/                오디오 재생·설정·catalog
│  │     ├─ Characters/           캐릭터 방향·보행·공격 표현
│  │     ├─ Combat/               전투 시각 효과
│  │     ├─ Rendering/            마스크·강조·그림자·발광
│  │     ├─ UI/                   HUD·증강 카드·입력 표시
│  │     └─ World/                월드 표시·설치 표시·정렬
│  ├─ Editor/                     Unity Editor 제작 도구
│  └─ Tests/
│     ├─ EditMode/
│     └─ PlayMode/
└─ Settings/
   ├─ Rendering/                  렌더 파이프라인 설정
   ├─ Input/                      입력 에셋
   ├─ Editor/                     Editor 설정 에셋
   └─ Resources/                  TMP 설정 로딩 경로
```

# 실제 파일 찾는 예시
- **게임 규칙·라운드:** `Assets/Scripts/Runtime/Domain/Rules/Session.cs`
- **전투씬 연결:** `Assets/Scripts/Runtime/Bootstrap/CoreGuardianPrototype.cs`
- **HUD:** `Assets/Scripts/Runtime/Presentation/UI/CoreGuardianHud.cs`
- **캐릭터 동작:** `Assets/Scripts/Runtime/Presentation/Characters/CoreGuardianUnitAnimation.cs`
- **포탄·전투 효과 표시:** `Assets/Scripts/Runtime/Presentation/Combat/CoreGuardianCombatArt.cs`
- **전투 오디오:** `Assets/Scripts/Runtime/Presentation/Audio/CoreGuardianCombatAudio.cs`
- **마스크·선택 강조:** `Assets/Scripts/Runtime/Presentation/Rendering/CoreGuardianSelectionOutline.cs`
- **길 메시 제작 도구:** `Assets/Scripts/Editor/Rendering/RouteMeshBuilder.cs`
- **적 원본 Sprite:** `Assets/Art/Characters/Enemies/Sprites/enemy_attack_v2.png`
- **적 보행 프레임:** `Assets/Art/Characters/Enemies/Resources/CoreGuardian/OriginalWalk/`, `VerticalWalk/`
- **타워 공격 동작:** `Assets/Art/Characters/Towers/Resources/CoreGuardian/OriginalMotion/`
- **플레이어 방향 프레임:** `Assets/Art/Characters/Player/Resources/CoreGuardian/PlayerFacing/`
- **HUD 이미지:** `Assets/Art/UI/Resources/CoreGuardian/Hud/`
- **현재 밸런스 CSV:** `Assets/Data/Balance/Resources/CoreGuardian/`
- **사운드 선택 목록:** `Assets/Audio/Resources/CoreGuardian/Audio/SelectedB/catalog.json`
- **사용자가 만든 아틀라스:** `Assets/Art/Atlases/Sprites/CG_Enemies.spriteatlasv2`. 현재 입력은 비어 있으며, 파일 정리가 아틀라스 적용 완료를 뜻하지는 않습니다.

# 이름·경로 관리 원칙
1. 신규 파일은 날짜·실험명·제작자 이름을 최상위 분류로 만들지 않고, 위 용도별 폴더에 둡니다. 원본·실행용·검사·문서는 하위 역할로 구분합니다.
2. 일반 에셋은 용도·대상·순서로 이름을 정합니다. 예: `Route_Order_01_Part_02.asset`, `Route_Material_05.mat`.
3. **Sprite.name·MonoBehaviour 클래스명·Resources key는 이름으로 참조하는 계약입니다.** Up_0/Down_0, D2_* 등은 실제 코드/검사에서 읽으므로 임의로 바꾸지 않습니다. 이름 변경이 필요하면 호출부와 검사를 같은 변경에서 갱신합니다.
4. `Resources` 폴더는 각 용도 분류 안에 있습니다. **Resources 뒤의 경로**가 로딩 키이므로, 폴더 정리 때 이 부분을 유지합니다. 현재227개 key를 보존했습니다.
5. 파일 이동·이름 변경은 Unity Editor 또는 AssetDatabase API로 진행하고 `.meta`/GUID를 함께 유지합니다. 씬 YAML을 직접 고치지 않습니다.
6. Domain의 Fortress.Game assembly, 기존 화면 표현 코드, EditMode/PlayMode assembly 경계를 유지합니다. Runtime/Editor/Tests를 섞지 않습니다.
7. 기존 전투씬은 위 경로 하나를 유지하고, Recovery는 사용자의 기존 복구 자료로 보존합니다.
8. **게임 기록·캡처는 보존**합니다. 프로젝트 제작 기록은 Assets 밖 `docs/`에 둡니다. Library·Packages·ProjectSettings 등 Unity 고정 폴더와 별도 HUDDesigner 저장소를 임의로 이동하지 않습니다.

# 검증·복구 기준
- 디렉터리 이동 완료 커밋: **babac77**
- 이동 전 복구 커밋: **c1203f1**
- GUID·meta·의존성552개, Resources key227개, 스크립트 assembly102개, 커스텀 마스크 dependencyHash92개 유지 확인.
- 기존 파일530개는 byte 동일, 코드10개는 직접 파일 경로 갱신, 길 메시/재질12개는 직렬화 이름만 변경했습니다.
- 대상 EditMode 검사: RouteMesh7 + PhotoShadow10 + PlayerFacing5 = **22/22 통과**.
- 전투씬·사용자 설정2개 SHA256 동일, 종료 scene clean. Play·빌드·실행파일 실행·push는 하지 않았습니다. 이 검증은 폴더 정리에 대한 것이며 전투 성능 통과 판정은 아닙니다.

# 로컬 상세 색인
프로젝트 루트: `C:/Users/edcde/OneDrive/Desktop/CD`
- Unity 안의 안내: `Assets/Documentation/ProjectStructure.md`
- 기존552개 전체 파일 위치: `docs/development/directory_reorganization_20261023/DIRECTORY_INDEX.md`
- 이전→새 경로 대응: `docs/development/directory_reorganization_20261023/path_map.csv`
- 상세 실행·검증: `docs/development/directory_reorganization_20261023/README.md`
과거 진단 코드·CSV·캡처는 당시 실행 증거로 남아 있으므로, 예전 도구를 재실행하기 전 이동표로 경로를 맞춥니다.

