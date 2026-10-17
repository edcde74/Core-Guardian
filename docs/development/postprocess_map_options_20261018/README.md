# 현재 자연석 맵 색보정 비교 3안

최신: 이후 사용자가③을 선택해 실제 저장했다. 현재 값과 검증 범위는 [선택③ 적용](applied_warm.md)을 따른다. 아래는 선택 전 비교 이력이다.

2026-10-18 (Asia/Seoul). 사용자 요청: 현재 적용 화면과 제안한 ①차분한 석조 요새/②차가운 폐허/③따뜻한 고대 설비를 나란히 비교한다. 시작 HEAD `4358fec`. 미선택 시안이며 영구 게임 적용 없음.

- [2×2 비교판](comparison_2x2.png): 좌상 현재/우상①추천/좌하②/우하③. 각각 같은 실제 전투 씬·Main Camera·1920×1080 Edit 렌더다. 비교판은 각 원본을 같은1280×720 크기로 축소해 번호/설정 글자만 붙였다. 생성형 이미지나 색을 사후 합성한 시안이 아니다. 원본은 `00_current.png`, `01_calm_stone.png`, `02_cool_ruins.png`, `03_warm_machinery.png`다.
- 기존 ColorAdjustments/SplitToning만 개별 복제한 임시 프로필로 바꿔 렌더했다. 정확한 대비/채도/노출/그림자·밝은 색조·balance는 [capture.json](capture.json)을 따른다. Bloom/HDR/비네트/톤매핑/블러·조명/국소발광/원본 아트 변경 없음. 색보정은 배경과 유닛 전체에 적용된다.
- [촬영 스크립트](CaptureMapGrades.cs)는 Play/컴파일/미저장 씬을 거부하고 finally에서 원래 프로필/weight/카메라target/RenderTexture와 Volume stack을 복원한다. 원본 카메라/Volume/프로필과 하위 컴포넌트 직렬화 일치를 확인했다. 첫 메모리 컴파일은 존재하지 않는 MarkSceneClean API로 실행 전 실패했다. SaveScene은 복원 일치 후 dirty일 때만 수행하도록 고쳤고 최종 diagnostics0/실행 성공/dirtyfalse였다. Assets 밖 코드라 게임 컴파일을 변경하지 않았다.
- 전후 SHA256 동일: 전투 씬 `665E5E58FEC304889E0331190972FA992849F459D74A0D1CC7C2737FACF8C69D`, 색보정 프로필 `FEED455BE39BA214ACCB3F5B8B55FD27F50A6EED6A050322F40B32582C7740F5`. Assets Git 변경 없음.
- 종료 조회: CD ready/Play stopped/compilingfalse/domainReloadfalse, 전투씬1/dirtyfalse/root7, compilationFailedfalse/현재 콘솔 오류0·경고0. 과거 버퍼 카운트와 구분한다. 카메라HDRfalse/posttrue 유지. 새 Play/자동검사/밸런스 측정/빌드/실행파일/push 없음.

정지 Edit 카메라 비교라 ScreenSpaceOverlay HUD/전투 몬스터/움직이는 입자는 포함하지 않는다. 실제 전투 판독·성능·최종 아트 승인이 아니며 사용자 선택 후 필요한 실제 전투 비교를 진행한다. 기존 HUD/외곽선/강화/입자 사용자값·전투 수치와 미추적 타 작업은 보존했다. Notion 변경 없음.
