# 배경 도트 A안 Unity 교체

2026-11-02. 사용자 요청: A안으로 Unity 안에 교체. 배경 아트 교체만 승인; Play·빌드·push 없음.

- 복구 기준 HEAD: `7d608ee84829fac54c302b93f8161efb2f922564`. 작업 전 씬/환경 아트 diff 없음. 기존 폰트·Sources.asset·설정·font_tools_vendor 표시·_Recovery는 별개 사용자 변경으로 보존.
- [선택 시안](approved_concept_A.png) / [적용 전](before.png) / [적용 후](after.png). 전후는 동일 Main Camera 1280×720, Edit Mode 렌더이며 Overlay HUD/전투 검증 아님.
- built-in imagegen으로 기존 배경 텍스처를 공간 배치 참조, 선택 A안을 화풍 참조로 사용해 별도 교체용 4장을 제작. 비교판 자체를 배경으로 덮어쓰지 않음. 공통 프롬프트: 기존 위치·UV·시점 유지, 따뜻한 회색 석재와 올리브 이끼, 각진 픽셀 색면, 캐릭터·글자·비교판 제외. 장식은 투명 유지, 바닥은 2×2 atlas, 땅굴은 기존 UV 영역 유지.
- 신규 이미지: `Assets/Art/Environment/Backgrounds/PixelA_{Courtyard,Decoration,Slabs,Tunnel}.png`와 meta. 원본 PNG/meta 그대로 보존. 원본 importer 설정을 AssetDatabase.CopyAsset으로 복사했으며 Point 유지.
- 신규 재질: `Assets/Art/Rendering/Materials/Environment/PixelA_Decoration.mat`, `PixelA_Tunnel.mat`. 기존 재질은 보존.
- 씬 변경: Background_Ruins_VisualOnly sprite, 장식 MeshRenderer 5개 및 땅굴 MeshRenderer 1개의 material 참조. 기존 mesh·UV·transform·route 재질·유닛·코어는 유지.
- `Stone_B_0..3.asset`의 sprite 참조를 새 atlas의 동일 이름 sprite로 교체. Tilemap RefreshAllTiles 후 Unity API로 기존 전투 씬 저장. 씬 diff의 다수 행은 858개 타일의 캐시 sprite index 변경임.
- 씬 저장 때 ExecuteAlways의 정렬값 2개가 함께 저장된 것을 발견하여 기존 디스크 값으로 Unity API를 통해 복원. `hoverMode: 0`은 기존 코드 필드의 기본값이 씬 저장으로 명시된 항목이며 코드 변경 없음.
- 검증: 적용 전후 이미지 직접 확인, 장식 이미지 1671×941 및 중앙 alpha=0, 연결 성공·씬 저장 성공. 최종 Editor ready, Play stopped, compiling=false, compilationFailed=false, consoleErrors=0 (기존 경고 7). eval 조회 한 번에서 GetInstanceID 폐기 API 진단이 발생했으나 조회 코드만 수정하여 해결; 프로젝트 스크립트 추가/변경/재컴파일 없음.
- 한계: 생성형 화풍 변환이므로 시안 A의 픽셀을 그대로 추출한 에셋이 아님. 균일한 저해상도 픽셀 격자 및 전투 가독성 검증은 미실시. 최종 아트 승인은 사용자 화면 확인 대기.
- 복구 시: 배경 sprite를 NaturalCourtyard_01_ClearBorder로, 장식/땅굴 material을 HybridDecoration_Unlit/SpawnTunnel02_Selected로, Stone_B_0..3의 sprite를 StoneSlabs_B_2x2 atlas의 동일 이름으로 Unity API에서 다시 연결. 사용자 작업 전체를 git restore/reset하지 않는다.
- 다음: 사용자 화면 피드백. 필요 시 해당 요소만 조정. 로컬 커밋·push 미실행.

## 11-02 저녁 Play 검증 (Claude, 사용자 요청)

- 결과: EditMode 327/327, PlayMode 54/54, 콘솔 오류 0. 28라운드 전투(포탑 9기·적 115)에서 상단 막대·다음 적 줄·누적 피해·설치 창·초록/빨강 설치 칸·정보창·설명창 정상. 몬스터·포탑·체력 줄 판독 가능.
- 씬 diff: 배경 sprite 1, 장식 재질 5, 땅굴 재질 1, 타일 팔레트·캐시 sprite index, `hoverMode: 0`만. 적 길 재질·경로 변경 없음(사용자 확인과 일치).
- 남은 화면 문제(사용자 판단 대기): 땅굴 아래 바닥 번짐(이전 그림보다 진함), 땅굴 안 격자 비침, 덤불·돌이 커져 설치 가능한 칸이 막힌 것처럼 보일 수 있음.

## 11-02 확정

사용자가 지금 적용된 배경을 그대로 쓰기로 확정. 땅굴 아래 번짐·땅굴 안 격자·큰 덤불과 돌도 그대로 둔다.
