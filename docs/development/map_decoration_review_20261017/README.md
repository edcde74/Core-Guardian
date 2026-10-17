# 맵 꾸미기 사전 조사와 방향 제안

현재 상태: 이후 사용자 **B안 선택 및 게임 적용** 완료. 이 문서는 선택 전 조사/시안 이력이며, 현재 구현과 실제 화면·QA 범위는 [B안 적용 기록](../map_B_applied_20261017/README.md)을 따른다.

2026-10-17 (Asia/Seoul). 사용자 요청에 따른 읽기 조사이며 환경 테마·장식·배치는 아직 미선택이다. 게임 적용/신규 아트 생성/Play/자동검사/밸런스 측정은 하지 않았다.

## 시작 상태와 현재 화면

- AGENTS → UNITY_HANDOVER → LOCAL_GIT → resume_hud_latest_20261017 순으로 읽었다. 실제 HEAD는 `d44f519d5533c445e3bbbcd04c0c234a10d74a9e`. 사용자가 전달한 `58cfccf` 다음의 기존 보행 Notion 업로드 기록 커밋이다.
- Git 조회는 소유자 차이 때문에 명령 한정 `-c safe.directory=C:/Users/edcde/OneDrive/Desktop/CD`를 사용했다. 전역 설정 변경 없음.
- font_tools_vendor 접근 오류와 그에 따른 삭제 표시를 실제 삭제로 판단하지 않았다. 그 경로를 제외한 추적 파일 diff는 없었다. AudioProduction·밸런스/BGM·기존 포신 probe/캡처·강화 시안 raw frames 미추적 자료 보존.
- 연결 프로젝트 CD / Unity6000.6.2f1 / ready. 전투 씬 하나(active/loaded), dirty=false, Play=false/paused=false, compiling=false/compilationFailed=false, TimeScale1/captureDelta0. 종료 조회도 dirty=false/Play=false/compiling=false. 현재 Console groundTruth 오류0/경고0이며 과거 누적 버퍼 수와 구분했다.
- `test_status`는 기존 completed30/30을 반환했다. 이번에 검사를 실행한 결과가 아니다.
- [현재 화면](current_game_edit.png): 1920×1080 Game 뷰, Edit 상태의 실제 전투 맵. 카메라·시간·배치 변경이나 입력/재화 주입 없음. 런타임 HUD·몬스터 전투·코어 입자/강화 재생을 포함한 화면이 아니다.
- 저장 전투 씬 SHA256: `E9191CDAA7257B24429710F0FFD66991BA2678E075F873261841E4C3F2A94094`.

## 현재 리소스와 논리 영역

- Ground_Prototype: 45×25, cell0.6. `Assets/Art/ScreenTests/20261008_5x5/Floor_Small.asset` → `Ground_Countable.png`, 원본32×32/PPU53.333332/Point. 실제 이미지는 단순 회색 한 칸이며 최종 석재가 아니다.
- EnemyPath_Prototype/PathArt_WideStraight: `Path_Straight_Small.asset` → `Assets/Art/ScreenTests/20260930/Revision2/Sprites/path_straight_v2.png`(32×48/Point). 모서리: `Path_Corner_Small.asset` → 같은 폴더 `path_corner_v2.png`(48×48/Point). 기존 채택 A 경로의 형태·명도/황동 경계 보존.
- 현재 사용하지 않는 기존 `ground_slab_v2.png`(64×64)도 직접 보았다. 내부 균열이 있어 현재 한 칸 판독 기준과 대조할 참고 자료이며 그대로 채택하지 않는다.
- `Assets/Art/MagitechFortress/Source/MagitechFortress_BaseTiles.png` 실제 시트와 관련 Tiles/TilePalettes 존재를 확인했다. 성벽·기둥·수정·도관 형태는 있으나 부드러운 고해상도 일러스트이므로 현재 픽셀 환경 완제품으로 간주하지 않는다. 현재 바닥/경로는 이 시트를 직접 쓰지 않는다. Unity Search `p: MagitechFortress` 창을 열었다. 이번 조사에서 즉시 적용할 만한 별도의 완성 픽셀 환경 장식 세트는 확인하지 못했다.
- `CoreGuardianPrototype.Start`의 실제 전투 경로: (0,12)→(11,12)→(11,18)→(35,18)→(35,6)→(17,6)→(17,12)→(29,12)→(29,9). `Session.SetBoard(45,25,...)` 및 IsBuildable은 경로/코어/포탑 점유를 제외한다. PrototypeGridMovement는 ground/적 경로/core/점유 Tilemap을 참조한다.
- 씬의 EnemyRoute_Waypoints_Prototype 마커 위치는 위 런타임 경로와 다르다. 장식 기준 경로를 마커 이름만으로 추정하지 않으며 이번에 수정하지 않았다.
- 카메라 현재 orthographicSize8.5/position(13.5,7.5,-10)/HDRfalse. 현재 Volume 프로필은 `Assets/Settings/CoreGuardianColorGrading.asset`의 ColorAdjustments+SplitToning이다. 변경하지 않았다.

## 기획 근거와 과거 문서 충돌

- [프로토타입 레벨 디자인](https://www.notion.so/3e9389dbfc688159ae8cec214b4e3e25): D 안쪽 코어/45×25·0.6/경로·코어·시작점/연결 공간 기준. 비교 씬 보존 문구는 최신 사용자 씬 정리 지시에 의해 대체된 이력이다. 삭제 씬 복원 안 함.
- [전투 화면 아트 방향](https://www.notion.so/3ea389dbfc6881dca4d6fe9ca0b35058): 낡은 석조 요새를 금속 보강재·수정 장치로 수리해 재가동, 큰 픽셀·단순 형태/낮은 바닥 질감/경로 A. 현재 회색 바닥은 최종 석재가 아님. 전체 컨셉과 별개로 이번 환경 세부 테마·장식·배치는 사용자와 정해야 한다.
- [입체감·바닥 밀착감](https://www.notion.so/3f3389dbfc6881089b13e5daf7b766a2), [공통 효과 아트](https://www.notion.so/3f4389dbfc688118965ecb7a1ffaaf3c), [핵심 루프](https://www.notion.so/3df389dbfc68810d82fade870187825f)를 대조했다. 기존 유닛/설비/효과의 변경 승인이 아니다.
- `docs/superpowers/specs/2026-09-20-magitech-fortress-tilemap-design.md`의 부드러운 일러스트/비폐허/옛 SampleScene 방향은 과거 초기 시안이다. 최신 픽셀·수리한 폐요새 방향과 현재 씬 하나 유지 지시를 우선한다.
- Notion은 읽기 조회만 했다. 담당 문서의 상세 규칙은 해당 문서가 관리한다.

## 선택할 방향 — 모두 미적용

| 안 | 모습 | 장점과 주의점 |
| --- | --- | --- |
| A 수리한 요새 안뜰 (추천) | 큰 회색 석재 면, 드문 균열·금속 보수판, 보드 밖 낮은 성벽 잔부·보강 기둥 | 현재 석재/황동/청록 설비와 연결이 쉽다. 정돈된 바닥을 유지하고 외곽 실루엣으로 장소를 만든다. |
| B 오래 방치된 요새 | 마모된 석재, 소량의 이끼·모래, 보드 밖 무너진 벽·파편 | 오래된 장소 느낌이 강하다. 내부 잔해가 장애물처럼 보이지 않게 하고 식생 질감의 대비를 제한한다. |
| C 수정 동력 정비 구역 | 석재+평평한 보수판, 보드 밖 배관·정비 설비, 소수의 비발광 수정 부품 | 기술병·포탑과 연관이 선명하다. 도관이 새 이동 경로나 공격/선택 표시로 보이지 않아야 한다. |

추천 1차 범위: 바닥 기본형+낮은 대비 변형2종의 화면 시안 → 선택 후 시각 레이어 적용 → 외곽 벽/보강 요소 소수 추가. 내부 45×25칸에는 높이 있는 기둥/상자/잔해를 놓지 않는다. 내부 장식은 칸 경계를 유지하는 평면 표현으로 제한한다. 외곽 장식은 현재 카메라에 보이는 보드 밖 범위에서만 검토하며, 공간이 부족하면 생략한다. 외곽 칸을 장식용으로 빼거나 카메라를 넓히지 않는다.

사용자 선택 후 픽셀 밀도·팔레트·시점·한 칸 판독과 기존 경로 연결을 실제 카메라 크기로 비교한다. 적용 이후 설치 초록/빨강 피드백·유닛 실루엣/피해 숫자 판독을 확인할 계획이나 이번 조사에서 확인한 결과는 아니다. 경로/보드/충돌/설치·이동 규칙/카메라/전투 수치는 변경하지 않는다. 환경 발광·입자·후처리는 이번 제안에 포함하지 않는다.

## 다음 단계

사용자가 A/B/C 또는 조합을 선택하고 이번 1차 작업 범위를 정한다. 이후 구체 시안을 비교하고 선택한 리소스만 적용한다. 현재 환경 적용 완료나 최종 아트 승인 상태가 아니다.

## 사용자 영역 구분 후 생성한 3안

사용자가 첨부한 빨간 메인 그라운드/파란 전체 화면 구분을 따라, 메인 바닥을 다듬고 검은 바깥 영역은 화면 끝까지 2D 배경으로 채우는 방향으로 수정했다. 앞서 적은 외곽 소수 장식 범위는 이번 사용자 요청으로 확대됐다. 보드·카메라 변경 승인은 아니다.

- [A 수리한 요새 안뜰](concepts/A_repaired_courtyard.png): 큰 석재 면과 황동 보수판, 외곽 성벽·기둥·깃발.
- [B 방치된 요새](concepts/B_abandoned_fortress.png): 마모된 석재·작은 이끼 흔적, 외곽 무너진 벽·식생.
- [C 수정 동력 정비 구역](concepts/C_crystal_service_yard.png): 석재·평면 금속판, 외곽 배관·정비 설비.

제작: built-in image_gen의 편집 방식으로 각 안 1회. 공통 프롬프트는 현재 깨끗한 실제 화면을 편집 대상으로, 사용자 표시 이미지를 영역 설명으로 지정했다. 경로/코어/플레이어 배치와 전체 프레임 유지, 메인 그라운드와 백그라운드의 재질·팔레트·픽셀 밀도/시점 통일, 내부 평면 바닥·외부 배경의 연속성, 화면 끝까지 검은 여백 대체, 주석/UI/새 유닛/추가 경로/발광 회로 금지를 요청했다. 각 안에는 위 테마별 석재·보수판·이끼·배관 차이를 지정했다.

출력을 직접 확인했으며 생성 시안은 원래 논리 칸과 일대일로 대응하는 최종 타일 에셋이 아니다. 일부 바닥 줄눈과 외곽 장식은 실제 칸 경계와 겹칠 수 있어 선택 후 분리 제작/정렬이 필요하다. 이 이미지를 그대로 플레이 영역 위에 덮는 적용 승인이 아니다. 실제 Unity 적용/Play/새 기능 검사/픽셀 격자 일치 검증 없음. 세 안은 아직 미선택이며 게임은 변경하지 않았다.
