# 가독성 A — 배경·길 질감 대비 완화

2026-10-18 (Asia/Seoul). 사용자 요청: A→B→C 순서로 검토하되 먼저 A를 진행한다. A는 배경·길만 조정하고 개체 실루엣/상시 윤곽 보강(B)은 하지 않는다. 복구 기준 `cec69ae`. 최종 체감·아트 승인은 사용자 비교 대상이다.

## 적용

- `EnvironmentContrast.shader`와 전용 재질2개를 추가했다. 기존 Sprite-Unlit의 버텍스/스프라이트 색/투명도/Point 샘플링을 유지하고 밝기만 기준점 쪽으로 압축한다. 블러/새 텍스처/AI 재생성/원본 PNG 변경 없음.
- 자연석 배경1개에는 `ReadabilityA_Courtyard.mat`(DetailContrast .62/Pivot .14/InteriorOnly1), 판석·경계석242개에는 `ReadabilityA_Route.mat`(.70/.20/InteriorOnly0)를 연결했다. 배경 텍스처 안의 외곽 폐허는 UV 가장자리 마스크로 조정량을 줄인다. 모든 바닥 픽셀을 균등한 논리 격자로 바꾸지 않는다.
- 원본② 땅굴의 MeshRenderer/재질/원본 형태·판석 연결·그림자, 유닛/포탑/코어·그림자/체력바/HUD/선택 초록2px/강화0.8초/입자 사용자값/③ 색보정26·7·-.20을 보존했다. 길/경계석은 원래 모양·위치·색 틴트 그대로이며 추가 충돌/설치/전투 규칙 변경 없음. 공용 ArtReview_Unlit 재질은 수정하지 않았다.
- `ApplyEnvironmentA.cs`를 Assets 밖에서 메모리 컴파일하고 에디터 API로 재질 생성/연결/씬 저장했다. 씬 YAML/메타를 직접 편집하지 않았다. 셰이더는 한 번의 기존 텍스처 샘플을 사용하며 성능 실측은 하지 않았다.

## 실제 비교와 확인 범위

- [전투 전후 비교판](comparison_A.png): 좌이전/우A, 상단 같은1920×1080 실제 합성 GameView를 같은1280×720로 축소, 하단 같은640×300 영역을 Nearest2배로 확대했다. 임시 줌/카메라 변경 없음. [이전](combat_before.png)/[A](combat_after.png), [Edit이전](edit_before.png)/[Edit A](edit_after.png).
- 시작에 사용자 Play가 paused라 종료하지 않고 읽기/준비를 진행했다. 사용자 ‘종료했어’ 후 ready/stopped/씬1·dirtyfalse를 재조회하고 적용했다. 이후 자체 Play만 시작해 정상 수정100으로 멀티포탑 구매/API StartCombat·자연 몬스터4마리를 확인했다. 촬영 때 TimeScale0으로 같은 세션을 잠시 고정해 A/기존 재질만 교환했다. HUD 포함, Diagnosticfalse·카메라 직렬화/Transform 동일. 재화/몹/처치/배속/피해 주입 없음. [집중 촬영 QA](combat_qa.json).
- 보호 컴포넌트563개는 대상243개 SpriteRenderer의 재질 참조만 제외하고 전후 직렬화 동일. 디스크 씬 차이도243개 재질 참조 교체486줄만인 것을 확인했다. 새 게임 코드/프로필/원본 PNG/CSV 변경 없음. [적용](applied.json), [디스크 검증](disk_verification.json), 보호 스냅샷 before/after JSON을 보존했다.
- 첫 저작 스크립트 메모리 컴파일에서 GetInstanceID 폐기 오류는 실행 전 수정했다. 첫 보호 검사는 JSON의 SpriteRenderer 루트 아래 m_Materials를 최상위에서 제외하려 해서 중단했다. 검사 경로를 수정하고 이미 생성한 재질을 재생성하지 않은 채 기존 before와 전체563개 일치를 확인한 다음 저장했다. 제품 오류가 아니라 저작/검사 도구 수정 이력이다. 최종 메모리 컴파일 diagnostics0/셰이더 오류0.
- 종료: 자체 Play 종료, 전투씬1/root7/dirtyfalse, compilingfalse/compilationFailedfalse/현재콘솔오류0·경고0, TimeScale1/captureDelta0 복원. 저장 후 배경1/길242가 A 재질을 참조함을 재조회했다.

이는 실제 API 구매/자연 출현과 같은 시점의 외형 비교다. 사람 직접 플레이/전체 자동 회귀/밸런스 측정/다수 후반 전투·성능/최종 아트 승인은 아니다. 사용자 사진의 플레이를 복구한 세션이 아니라 별도 자체 새 세션이다. 빌드/실행파일/push/Notion 변경 없음. 미추적 오디오·밸런스/BGM·다른 작업과 font_tools_vendor 접근 오류 삭제 표시는 보존한다.

다음은 A의 실제 화면에 대한 사용자 피드백이다. B·C는 아직 미적용이며 자동으로 덧붙이지 않는다.
