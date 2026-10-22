# Core Guardian 프로젝트 폴더 안내

|폴더|내용|
|---|---|
|Art/Characters|플레이어·적·타워의 스프라이트와 동작 프레임|
|Art/Environment|배경·타일 스프라이트·Tile 에셋·길 메시|
|Art/UI|HUD 아이콘·카드·TextMeshPro 스프라이트/스타일|
|Art/Effects|전투 효과·넓은 그림자·이펙트 텍스처|
|Art/Atlases|SpriteAtlas와 캐릭터 마스크 배열|
|Art/Rendering|셰이더·재질|
|Audio|현재 선택한 음원·사운드 catalog·원본 사운드 라이브러리|
|Data|밸런스 CSV·애니메이션 메타데이터·표시 데이터|
|Fonts|실행용 폰트·SDF·폰트 재질·원본 폰트와 라이선스|
|Prefabs|게임 유닛·적·보스 부품·이펙트 프리팹|
|Scenes|전투씬과 사용자의 기존 복구씬|
|Scripts/Runtime/Domain|Fortress.Game의 게임 규칙·모델|
|Scripts/Runtime/Bootstrap|씬 연결·그리드 입력|
|Scripts/Runtime/Presentation|UI·오디오·캐릭터 동작·렌더링·전투 효과·월드 표시|
|Scripts/Editor|Unity Editor 제작 도구|
|Scripts/Tests|기존 assembly 구성을 유지한 EditMode·PlayMode 검사|
|Settings|렌더링·입력·Editor 설정 에셋|
|Documentation|에셋 설명·타사 사용 설명·출처|

Resources 폴더는 해당 용도 분류 안에 배치한다. Resources 뒤의 경로는 로딩 키이므로 임의 변경하지 않는다. Sprite 이름과 MonoBehaviour 클래스명도 게임에서 이름으로 참조하는 계약이다.

프로젝트 기록과 캡처는 Assets 밖 docs에 보관한다. 전체 파일 위치 색인과 이전→새 경로표는 docs/development/directory_reorganization_20261023/DIRECTORY_INDEX.md 및 path_map.csv에 있다.
