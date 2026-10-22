# 전체 Unity 에셋 디렉터리 통일

사용자 범위: 일부 예시3분류가 아니라 Assets 전체를 용도별 큰 분류·하위 폴더로 정리하고, 이름/경로로 찾을 수 있게 한다. 현 게임 품질·데이터·사운드·A/B 제한은 유지한다. 기록/캡처는 보존한다.

분류: Art(Characters/Environment/UI/Effects/Atlases/Rendering), Audio(선택 런타임/원본 라이브러리), Data(Balance/Animation/Presentation), Documentation(에셋 설명/출처/기록), Fonts, Prefabs(Units/Enemies/Effects), Scenes(기존 전투씬/기존 복구씬), Scripts(Runtime/Domain/Presentation/Bootstrap, Editor, Tests), Settings(Rendering/Input/Editor).

Ruling: 본 작업은 사용자 명시 지시에 따른 파일 분류·경로 변경이다. 게임 기능/밸런스/아트 설계는 변경하지 않는다. 게임이 사용하는 Sprite.name·C#클래스명·Resources key는 기능 계약이라 보존한다. 임시 날짜/ScreenTests/제작자 이름을 기준으로 한 최상위 폴더는 제거하고, 일반 Route 소재/메시 이름은 용도와 번호로 정리한다. Runtime Domain asmdef의 코드 범위는 그대로 보존한다. Unity가 정한 Library/Packages/ProjectSettings 등 프로젝트 구조와 별도 HUDDesigner 저장소는 이동 대상 에셋이 아니다.

- [ ] 전체552개 GUID/hash/dependency/Resources key/스크립트 assembly를 조사하고 목적지 충돌·누락 검사.
- [ ] 이동표·진단을 로컬 checkpoint로 저장. 사용자 변경/기존 Settings는 별도 해시 기록.
- [ ] AssetDatabase.MoveAsset으로551개 파일의 GUID/meta를 보존해 이동. asmdef/Editor/테스트 경계 유지. 런타임 로딩 키227개 그대로 유지.
- [ ] 코드의 직접 AssetDatabase/File 경로·현재 담당 문서 링크를 이동표로 갱신. 과거 진단 코드/실행 원본은 당시 증거로 보존하고 이동표를 제공.
- [ ] GUID/내용/의존성/Resources 로드/assembly와 컴파일 확인. 필요한 경로 영향 EditMode 검사만 선택; Play/빌드/실행파일/push 없음.
- [ ] 불필요해진 빈 폴더만 Unity API로 제거. 독립 검토·인수인계·완료 로컬 커밋.
