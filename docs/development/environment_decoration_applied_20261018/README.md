# 승인 혼합 장식 실제 적용

2026-10-18. 사용자 ‘이대로 사진대로 적용’ 지시로 A의 절제된 내부+B의 외곽 일부를 실제 전투 씬 하나에 추가했다. 복구 기준은 변경 전 `3f04f5a`다. 이전 시안 폴더의 미적용 문구는 시안 제출 당시 이력이며 현재는 이 기록이 우선한다.

## 현재 구현

- `CoreGuardian_MapArt_B/EnvironmentDecoration_Hybrid_Selected` 아래 `OuterDecoration_OrderMinus10`(4개 정적 MeshRenderer)과 `LowProps_OrderMinus5`(1개)를 추가했다. 원래 씬 부모 외 새 루트/씬을 만들지 않았다.
- 승인 [혼합 이미지](../environment_decoration_options_20261018/D_A_interior_B_edges.png)를 built-in imagegen으로 투명 분리한 [장식 PNG](HybridDecoration_Transparent.png)를 사용한다. [분리 프롬프트](extraction_prompt.txt). 생성본 전체로 배경을 교체하거나 유닛/HUD를 구워 넣지 않았다. 장식 투명 분리는 픽셀 단위 원본 복사가 아닌 파생 작업이므로 실제 적용 캡처가 최종 외형 확인 기준이다.
- 새 리소스는 `Assets/Art/Environment/AbandonedFortressB/DecorationHybrid_20261018/`에만 생성했다. 기존 자연석 PNG·길목·선택② 땅굴 형태/판석 접합/그림자·주요 오브젝트·기존 효과는 유지했다.
- 투명 PNG1671×941, Point/비압축/밉맵 끔/NPOT 그대로/Clamp/alpha transparency. 동일 카메라 footprint30.2222×17에 투영했다. 출력 폭이 기존 시안1672보다1px 작아 전체 정규화 UV로 맞추며 카메라 이동/배율 변경은 없다.
- 신규 unlit 재질만 RGBA(.78,.82,.85,1)로 조정해 분리 이미지의 밝은 풀·돌을 현재 맵 톤에 맞췄다. 기존 색보정/재질은 바꾸지 않았다.
- 이미지 픽셀의 disjoint 5영역을 UV 메시로 나눴다. 사각 영역마다 독립 정렬이며 개별 풀/돌마다 GameObject가 있는 구조는 아니다. 내부에 기존보다 큰 장애물·충돌 판정을 추가하지 않는다.

## 레이어 순서 — 뒤에서 앞으로

모두 Sorting Layer `Default`이며 Order in Layer로 정렬한다. Physics Layer나 논리 이동 경로 변경이 아니다.

| 표현 | Order | 설명 |
|---|---:|---|
| 자연석 바닥+기존 외곽 배경 | -20 | 기존 한 장 스프라이트. 논리 바닥 Tilemap은 유지하고 바닥 렌더러는 기존처럼 숨김 |
| 추가 외곽 풀/잔해 | -10 | OuterDecoration_OrderMinus10 |
| 낮은 잡초/자갈/납작한 파편 | -5 | LowProps_OrderMinus5 |
| 몬스터 길 판석/낮은 경계석 | 1 / 2 | 기존 경로 아트242개 유지 |
| 땅굴② | 4 | 기존 정적 입구 원본 유지 |
| 그림자·코어 바닥빛 | 기존41~44 등 | 기존 표현 유지 |
| 몬스터·포탑·플레이어·코어 본체 | 기존 높이 정렬 | ArtDepthSorting/기존 조각 순서 유지 |
| 선택선/개체강조/강화·전투 VFX/HUD | 각 기존 규칙 | 변경 없음 |

사용자 제시 순서는 시각적인 뒤→앞 관계로 맞으며 현재 바닥과 기존 외곽이 한 장이라는 차이가 있다. 새 외곽/작은 장식은 별도 제어 가능하다. 앞으로 큰 프랍이 주요 오브젝트를 실제로 가려야 하는 요구가 생기면 별도 판단하며 현재는 모두 길과 본체 아래다.

## 실제 화면

- [변경 전 Edit 카메라](before_edit.png) / [적용 후 Edit 카메라](applied_edit.png): 동일1920×1080, 카메라(13.5,7.5,-10)/orthographic8.5. Overlay HUD/전투 몬스터는 Edit 캡처에 포함되지 않는다.
- [설치 표시](applied_installation.png) / [자연 몬스터4·정상 포탑 구매 전투](applied_combat.png): 실제 ScreenSpaceOverlay 합성 화면1920×1080. 임시 줌·재화/몬스터 주입 없음.

## 검증

1. 변경 전 [preflight.json](preflight.json): 연결ready·Playfalse/pausedfalse·컴파일실패false·씬1/dirtyfalse. 샌드박스에서는 CLI 검색이 막혔으나 권한을 분리한 read-only CLI로 실제 에디터에 연결했다. Unity6000.6.2f1/URP.
2. [baseline.json](baseline.json), [verification.json](verification.json), [final_verification.json](final_verification.json): 기존564개 컴포넌트의 직렬화 동일,6 Tilemap의939개 타일 위치/참조/행렬/색 동일, 기존 의존 리소스61개SHA256 동일. 카메라/게임플레이/피드백/HUD/땅굴·길 렌더러도 포함한다. 신규 렌더러5/Collider0/MonoBehaviour0. 타일939는6개 맵의 실제 유효 타일 합계이며 논리 보드858칸과 구분한다.
3. [combat_check.json](combat_check.json): 자체 Play에서 정상API TryStep4방향 복귀, 설치 표시858/초록778·빨강80, 경로(0,10) 설치 거부, (4,8)멀티 정상 구매, 자연 몬스터4를 확인했다. 촬영 시에만 TimeScale0, finally에서1/captureDelta0 복원 후 자체 Play 종료했다.
4. [final_state.json](final_state.json): Playfalse/pausedfalse·컴파일실패false·씬1/dirtyfalse·TimeScale1/captureDelta0, 카메라/정렬 유지. 자체 Play 전후 저장 씬SHA256 `7D3332D5CB92E8FF496FC5BB605DFD51B48A3E945870C09B18B7009969C1FBCB` 동일. 현재 Editor 콘솔 오류0/경고0. 보관 로그에는 작업 전 발생한 Unity AI Account API 접근 지연 경고1건이 있으나 현재 콘솔과 구분한다.
5. 별도 읽기 검토에서 실제 Edit/전투/설치 화면 및 씬 diff의 원본 보호·정렬 문제 없음. 씬 diff는 신규 오브젝트/메시 렌더러와 기존 부모의 자식 참조 추가뿐이며 제거된 원본 블록은 없다.

집중 API/정지 화면 QA다. 사람 직접 플레이·전체 회귀·밸런스 실측·성능·최종 아트 승인은 아니다. 빌드/실행파일/push·Notion 수정/업로드 없음. 다음은 사용자의 실제 적용 화면 피드백이다.

## 재현·보존

[계획](plan.md), [상태 조회](Inspect.cs), [추가·원본 보존 검증](ApplyDecoration.cs), [신규 재질 톤 정리](FinishSetup.cs), [집중 실제 전투 확인](CombatCheck.cs). 모두 문서 폴더의 일회성 Editor 실행 코드이며 게임 런타임에 들어가지 않는다. Sprite metadata/씬 YAML 직접 편집 없음. CLI capture 명령이 Assets/docs 밑에 쓴 자체 임시 캡처1장은 제거하고 실제 결과는 본 문서 폴더로만 보존했다. 기존 Assets/docs 다른 자료는 유지했다.

미추적 AudioProduction/밸런스/BGM·다른 채팅 자료와 font_tools_vendor 접근 오류 삭제 표시는 보존한다. 이번 신규 아트·씬·관련 문서만 로컬 커밋한다.
