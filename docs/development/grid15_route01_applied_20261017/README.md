# 화면 유지 배치① · 실제 칸15% 확대 · 길목1안 적용

2026-10-17 (Asia/Seoul). 사용자 최종 지시: 비교①로 진행하고 첨부 `exec-cc1ce9c3-fc1f-4708-8083-c11fbd0d1c6a.png`의 길목1안으로 변경. 복구 기준 `005ba5f`. 읽기 전용 검토에서 발견한 플레이어 좌표 변환 문제까지 수정하고 집중 기능 QA를 마쳤다. 밸런스/최종 아트 자연스러움 승인은 별도다.

## 현재 적용

- 전투 씬 하나에 실제 Grid.cellSize0.6→0.69, 보드45×25→39×22(858칸), 원점(.045,-.09). 기존 카메라 중심/크기8.5·메인/외곽 자연석 배경 스프라이트와 배율은 동일하다. 바닥의 불규칙한 돌 줄눈을 논리 격자에 강제로 맞추지 않았다.
- 경로9점 `(0,10)→(10,10)→(10,16)→(30,16)→(30,5)→(15,5)→(15,10)→(25,10)→(25,8)`, 총80칸. 코어(25,8), 플레이어 시작(13,9). 논리 Tilemap·코어 아트·시작점·기존 웨이포인트 마커를 같은 배치로 맞췄다. 삭제한 씬이나 비교 씬을 만들지 않았다.
- 첨부 원본을 `Assets/Art/Environment/AbandonedFortressB/Route01_FlagstoneSource.png`로 보존하고 Unity Sprite Editor API로 판석3영역·낮은 경계석1영역을 참조했다. 원본 PNG 바이트는 편집하지 않았고 새 AI 이미지도 생성하지 않았다. 이미지 전체를 배경으로 덮거나 그 안의 플레이어/코어를 중복 표시하지 않는다.
- `CoreGuardian_MapArt_B/CoreGuardian_Route01_VisualOnly`에 판석과 낮은 경계석242 SpriteRenderer를 구성했다. 기존 연속 황동선 길은 숨기고 구형 시각 Tilemap 두 개는 비웠다. 원본 리소스/타일은 보존했다. 판석·경계석은 차단 길 한 칸0.69 안쪽이며 인접 설치 칸을 덮지 않는다. 최초 생성 시안의 약1.4배 폭은 정확한 구현 배율이 아니며 이번 확정 확대는 실제 칸15%다.
- `CoreGuardianPrototype`의 직렬화된 boardSize/coreCell/routeCells를 런타임 Session에 연결한다. **현재 씬 저장값39×22/코어25,8/새 경로**와 코드의 레거시 기본값45×25/29,9를 구분한다. 실제 씬을 실행해 새 값 사용을 확인했다.
- 설치 표시도0.55→0.6325로 비례 확대했다. 코어 피해 효과는 현재 CoreCell을 참조한다. 이동/설치 차단 규칙·이동 시간·유닛 원본 크기·전투 수치/CSV는 변경하지 않았다.
- 새 원점 때문에 드러난 `PlayerTile()`의 기존 변위/점 변환 혼용은 `InverseTransformVector`로 수정했다. 보드 이동을 이중 차감하지 않으며 플레이어 사거리·투사체 논리 시작과 실제 발 중심의 일치를 보존한다.

## 실제 화면

- [적용 일반 화면](applied_edit.png)
- [실제 Overlay HUD 준비](applied_hud_ready.png)
- [실제 설치 격자](applied_hud_installation.png)
- [정상 구매 후 자연 전투·피해13](applied_hud_combat.png)

일반 화면은 Edit 카메라 렌더다. HUD 화면은 자체 Play의 실제 Game View 합성 캡처이며 화면 촬영 시에만 TimeScale0을 잠깐 사용했다. 임시 카메라 줌/수치 변경/진단 재화/강제 처치/몬스터 주입 없음. 플레이어 이동은 공개 TryStep API, 구매/시작/재시작은 실제 공개 API/HUD Button 콜백이다. 사람이 직접 키보드·마우스로 플레이한 결과는 아니다.

## 검증

- 적용 전 [보드 계약 실패](red_layout.json)→적용 후 [동일 계약 통과](green_layout.json):858바닥/80경로/코어25,8/실제 칸0.69.
- 좌표 회귀 `CoreGuardianGridCoordinatesTests`: 원점0/이동 원점 정수좌표/이동 원점 분수좌표3건. 수정 전1통과·2실패, 수정 후**3/3통과**. 실제 씬의 [보정 전 오차0.14583](red_coordinates.json)→[보정 후 약9.54e-7](green_coordinates.json).
- [실제 Play 집중 QA](runtime_qa.json): 보드 안/테두리 바깥984개 좌표의 Scene/Session 설치 마스크 일치, 경로9점/80칸,26회 정상 API 이동·한 칸0.69·시작점 복귀, 이동 중/정지 전투용 좌표 일치, 길/점유 칸 이동 차단, 경로/코어/바깥/중복 구매 차단, 정상 수정100구매/점유/타워 셀 중심 일치, 자연 몬스터 경로/뷰 위치 및 자연 타격 확인. 설치 표시는858개(초록778/빨강80), 표시 크기0.6325. 좌표 수정 후 이 QA를 재실행했다.
- [보존 검사](protected_verification.json): 카메라/Transform/URP 설정·메인/외곽 배경·이동 규칙·선택 초록2px/강화0.8초/입자 사용자max0.1 등 보호 컴포넌트 직렬화 동일, 전투5 CSV 해시 동일. 길목 아트가 논리 차단 칸 안에 들어오고 추가 Collider/런타임 행동0개. 기존 마커와 새 런타임 경로 일치.
- 종료: 자체 Play 종료, 단일 전투 씬/root7/dirtyfalse, 컴파일 실패false, TimeScale1/captureDelta0. 현재 콘솔 오류0/경고0은 종료 조회로 확인한다. 전체 Edit/Play 스위트·밸런스·사람 플레이·다수 전투/성능 검증을 했다는 뜻이 아니다.

새 배치는 경로93→80칸과 설치 영역 축소를 포함한 사용자 승인 시각/비율 변경이다. 이동·전투 수치 자체는 보존했지만 이동 거리/사거리의 월드 표현과 전체 이동 경로 길이의 체감은 달라질 수 있다. 해당 밸런스 평가는 사용자가 정할 별도 시간까지 보류한다.

## 재현과 남은 작업

[ApplyLayout.cs](ApplyLayout.cs)는 한 번만 실행하는 에디터 저작 스크립트이며 같은 이름의 길이 이미 있으면 중단한다. [RuntimeQA.cs](RuntimeQA.cs)는 자체 Play에서 정상 준비 세션으로 재시작하는 검사 도구이므로 사용자 Play 중 실행하지 않는다. [CheckLayout.cs](CheckLayout.cs), [CheckPlayerCoordinates.cs](CheckPlayerCoordinates.cs), [VerifyProtected.cs](VerifyProtected.cs)와 [수정 전 보호 스냅샷](before.json), [설정 결과](setup.json)를 보존한다.

다음은 사용자 실제 외형 피드백이다. 길목1안과 배치①는 이미 선택·적용됐으며 다시 미선택 상태로 해석하지 않는다. 기존 배경·아트·HUD와 전투 수치를 유지하면서 필요한 시각 후속만 논의한다. Notion 수정/새 페이지/DB/빌드/실행파일/GitHub push 없음.
