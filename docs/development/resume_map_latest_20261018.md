코어 가디언 프로젝트를 이어서 진행해줘.
경로: C:\Users\edcde\OneDrive\Desktop\CD

먼저 AGENTS.md → docs/development/UNITY_HANDOVER.md → docs/development/LOCAL_GIT.md → 이 파일을 읽고 실제 Git HEAD와 변경 상태를 확인해. 현재 사용자 지시와 최신 담당 Notion을 우선해.

■ 현재 작업 단계
- 맵 배경·메인 바닥·길목·몬스터 출현 입구까지 적용했다. 다음 구체 시각 작업은 아직 정하지 않았다. 실제 화면과 사용자 피드백을 먼저 확인하고 미선택 장식을 임의 적용하지 마.
- 밸런스는 시각 작업 이후 사용자가 별도 시간을 정한다. 지금 밸런스 측정이나 수치 변경으로 넘어가지 마.

■ 마지막 게임 커밋
- 3fce0be: 선택한 원본② 땅굴 입구·판석 연결부·그림자 적용.
- dd0b027: 배치①/실제 칸15% 확대·회색 판석 길목1안 적용.
- 문서·Notion 동기화 후속 커밋이 있을 수 있으니 실제 HEAD를 다시 확인해.

■ 보존할 현재 맵
1. 배경·바닥
- 자연석1안. 크고 불규칙한 돌판이며 논리 격자처럼 균등 반복시킬 필요가 없다.
- NaturalCourtyard_01_ClearBorder.png 한 장2D 스프라이트로 메인 바닥과 외곽 폐허를 연결한다. 초기B 반복32px 바닥으로 되돌리지 마.
- 바닥 TilemapRenderer만 숨겼고 논리 타일은 유지한다.
- 담당: docs/development/map_natural_01_applied_20261017/README.md

2. 실제 그리드·길목
- 사용자 선택 배치①: 화면 범위 유지, 39×22칸, 실제 한 칸0.69(기존0.6의115%). 과거45×25/0.6은 현재가 아니다.
- 경로9점/80칸, 코어(25,8), 플레이어 시작(13,9). 상세 좌표와 현재 씬 저장값은 담당 기록을 따른다.
- 길은 회색 판석·낮은 석재 경계석1안. 구형 연속 황동 길로 되돌리지 마.
- 설치 표시도15% 확대했다. 현재 총858개/초록778·빨강80이며 규칙 자체는 보존했다.
- 런타임 보드/경로/코어는 씬 설정과 연결했다. PlayerTile()의 InverseTransformVector 보정을 유지해 이동 원점이 전투 좌표에 두 번 차감되지 않게 한다.
- 담당: docs/development/grid15_route01_applied_20261017/README.md

3. 몬스터 출현 입구
- 실제 이미지 출력 순서의② ‘무너진 땅굴’이다. 성문이나 계단 입구가 아니다.
- 사용자가 확대 캡처로 지정한 원본의 땅굴 형태·판석 접합부·깊은 그림자를 그대로 적용했다. 다른 모양으로 다시 생성하지 마.
- 선택 원본: exec-484411e7-16c7-46ac-8279-f1a52e448ff9.png
- 씬: CoreGuardian_MapArt_B / SpawnTunnel02_Selected
- 참조: Assets/Art/Environment/AbandonedFortressB/SpawnTunnel02_Approved.png
- 입구의 지역 원본 픽셀과 가장자리 알파를 사용하는 정적 시각 장식이다. 출현 위치/웨이브/경로/충돌/설치 규칙을 변경하지 않았고, 몬스터 페이드/실시간 가림 마스크를 구현한 것도 아니다.
- 담당: docs/development/spawn_tunnel02_applied_20261018/README.md
- 번호/원본 대응: docs/development/spawn_entrance_options_20261018/README.md

■ Notion 최신화 — 2026-10-18
- 사용자 요청으로 기획서1개를 기존 ‘기획서 관리’의 아트 분류에 새로 만들고, 명시 승인받은 실제 전장 PNG1장을 첨부했다.
- 레벨디자인·환경 아트 — 안뜰·길목·출현 입구:
  https://www.notion.so/3fc389dbfc6881e5abb7c9686d1cd893
- 기존 ‘프로토타입 레벨 디자인’은 실제39×22/0.69·경로/코어/시작점으로 정정했다. 기존45×25/0.6·5×5 비교 자료는 과거 이력으로 구분했다.
  https://www.notion.so/3e9389dbfc688159ae8cec214b4e3e25
- 기존 ‘전투 화면 아트 방향’과 ‘06. 아트’ 목차도 새 담당 문서로 연결하고 재조회했다. 새 DB는 만들지 않았다.
- 상세: docs/development/notion_level_art_sync_20261018/README.md
- 환경 표현은 새 아트 문서, 실제 배치 수치는 기존 레벨 문서가 담당한다. HUD/전투/후처리 상세 규칙을 중복하지 말고 담당 문서로 연결해.

■ 검증 범위
- 배치 작업 당시 좌표 회귀 Edit3/3, 실제 Play의26회 API 이동0.69·984좌표 설치 마스크 대조·길/점유 차단·정상100구매·자연피해13/Overlay 화면 확인.
- 입구 작업 당시 정상 새 세션의 첫 자연 몬스터가(0.02,10)→(1.12,10)으로 기존 길을 따라 나오는 장면/뷰 좌표 확인. 원본 Transform282개·Tilemap6개·게임/피드백/카메라 직렬화 동일.
- 위 검사는 집중 기능 QA다. 사람 직접 플레이·전체 회귀·밸런스·성능·최종 자연스러움 승인으로 표현하지 마.
- 마지막 입구 작업 종료는 Play=false, 전투 씬1/dirty=false, 컴파일 실패false, 콘솔 오류0/경고0, TimeScale1/captureDelta0이었다. 이후 문서·Notion 정리에서는 새 Unity 조회/Play/검사·게임 변경을 하지 않았다. 재개 때 현재 상태를 다시 조회해.

■ 기존 구현 보존
- 능력치 HUD A안·손잡이/설치 포탑 심볼 B안·피해 HUD 유지. 제거한 ‘포탑 #번호 · 능력치’ 작은 요약 바/재개방 손잡이 재생성 금지.
- 능력치창이 열릴 때만 실제 선택 대상 하나에 초록2px 아웃라인. 접기/유효 빈 바닥 첫 클릭에 선택 해제. 플레이어 본체/발밑 반복 클릭은 열기이며 닫기 토글이 아니다.
- 강화 성공 B 청록 본체 스캔0.8초, 실제 구매 성공에만 표시. 코어 입자 현재 저장값0.025/0.1/초당4/최대12, 코드 기본max0.045로 초기화하지 마.
- 카메라는 10월18일 사용자 선택③ 따뜻한 고대 설비: 대비+26/채도+7/노출-0.20EV, 그림자RGB(.46,.49,.54)/밝은색RGB(.54,.515,.48)/balance5. ColorAdjustments+SplitToning, post=true/HDR=false. 기존35/12/-0.45는 이전 값이다. Bloom/톤매핑/블러/비네트는 추가하지 않았다. docs/development/postprocess_map_options_20261018/applied_warm.md 참고. 새 전투/HUD 검증·Notion 동기화는 하지 않았다.
- 포탑 발광 단일1/멀티3/지원1·단일 원본 포신 조준/반동, 플레이어/몬스터 보행·그림자·체력바·피해 숫자와 원본 아트/전투 수치 보존. 몬스터 보행 최신54f58ff 및 담당 기록 유지.
- 상세 담당 링크는 AGENTS.md/UNITY_HANDOVER.md와 이전 HUD 인수인계 docs/development/resume_hud_latest_20261017.md를 따른다. 이전 인수인계의 맵 미적용/45×25 문구는 현재 상태가 아니다.

■ Unity·Git 보호
- Unity 변경 전 연결·열린 씬·Play·컴파일·저장 상태 재조회. 사용자 Play/다른 채팅 검사를 중단하거나 같은 에디터에서 검사 겹치지 마.
- 전투 씬은 Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity 하나만 유지. 삭제 씬 복원/장식용 새 씬 생성 금지. 씬 YAML 직접 수정 금지, Unity CLI/에디터 API 사용.
- 빌드·실행파일 실행·GitHub push 금지. 관련 파일만 로컬 커밋하고 실제 확인 결과만 기록해.
- AudioProduction/·밸런스/BGM·다른 채팅 변경/미추적 자료 보존. font_tools_vendor 접근 오류의 삭제 표시를 실제 삭제로 단정하거나 커밋하지 마. raw 캡처/폐기 probe 폴더를 통째로 stage하지 마.
