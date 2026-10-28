# HUD 개편 B안 게임 적용 + 노션 최신화 (2026-10-28)

정본 기획: Notion [HUD 화면 및 표시](https://app.notion.com/p/3df389dbfc68811f9a87e5a5ff05a571) '전투 화면 개편안'. 아트: [docs/art/hud_redesign_20261028/](../../art/hud_redesign_20261028/README.md).

## 커밋
- `b14a22e` HUD 개편 B안 구현(강화 건물 2개, 선택 정보창, 받은 증강 줄·Tab 목록, 하단 버튼 3개·범위 폭발 아이콘, 피해 스크롤바, 약점/저항 피해 숫자).
- `582e96a` 사용자 확정 2건: 정보창을 연구소 건물·증강 줄 위/오른쪽으로 이동, 연구소 강화는 그 종류 포탑이 1기 이상 있어야 구매.
- 검증: EditMode 308/308, PlayMode 57/57, Play 화면 확인(1600×900 캡처, 에디터).

## 게임에 들어간 것
| 항목 | 내용 | 코드 |
| --- | --- | --- |
| 강화 건물 | 포탑 연구소 (2,1)~(3,2) 키 1, 기술병 작업대 (35,1)~(36,2) 키 2. 건물 칸은 설치·이동 차단. 마우스 올림·창 열림 때 `_highlight` 그림 | `CoreGuardianPrototype` SetupBuildings·RebuildOccupied, `Session.SetStructures` |
| 큰 패널 | 설치 목록 / 연구소(`TowerLabPanel`, 종류 탭 6 + 공유 강화) / 작업대(`PlayerGrowthPanel`) 중 하나만 | `CoreGuardianHud.Inspector.cs` |
| 선택 정보창 | `SelectionInfo` 왼쪽 아래(76, 200), 적·포탑·기술병. 적은 월드 클릭으로 선택. 포탑에는 파괴 버튼 | `CoreGuardianHud.Redesign.cs` |
| 받은 증강 | 왼쪽 세로 줄 최근 6개(등급 테두리 H4 + 아이콘 H3), '+N'/'전체' 버튼 = 목록 고정. Tab 누르는 동안 가운데 목록(받은 순서, ×N, 아래 설명) | 같은 파일 |
| 범위 폭발 | 하단 아이콘 버튼, 시계 방향 덮개·남은 초·Space 표기, 클릭 사용 | 같은 파일 |
| 누적 피해 | 패널 폭 332, H6 스크롤바(필요할 때만) | 같은 파일 |
| 약점/저항 숫자 | 약점 1.3배·주황, 저항 0.8배·회색(포탑 공격만) | `Session.HitAffinity`, `CoreGuardianHud.ShowDamageNumber` |
| 데이터 | stage.csv 42701~42705 (buildingLabX 2, buildingLabY 1, buildingBenchX 35, buildingBenchY 1, buildingSize 2) | `Session.Data.Setting` |
| 아트 | `Assets/Art/UI/Resources/CoreGuardian/Hud/Redesign/`(H1·H2·H4·H5·H6), `Hud/Augments/augment_01~50` — 확정본 그대로 복사 | — |

참고: 전투 중 설치 때 다른 HUD 숨김(설치 집중 표시)은 기존대로 준비 단계에서만 동작.

## 노션 최신화 (2026-10-28)
| 문서 | 바꾼 내용 |
| --- | --- |
| [HUD 화면 및 표시](https://app.notion.com/p/3df389dbfc68811f9a87e5a5ff05a571) | 개편안 절에 '게임 구현' 표시와 적용 상세(정보창 위치, 증강 줄·Tab 크기, 약점/저항 숫자, 연구소 구매 조건, stage 427xx). 남은 결정 '범위 폭발 표시' 해결, 정보창-연구소 겹침 해결. 변경 이력 추가 |
| [데이터 테이블 명세](https://app.notion.com/p/3df389dbfc68816083ced1e50bf03c20) | stage 427xx 표 추가, 한 판 설정 묶음 7 = 강화 건물, '반영 대기 데이터 변경' 전부 완료 표시 |
| [후속 제작·검증 작업 순서](https://app.notion.com/p/3f4389dbfc688171b84dc19b138925f4) | '10-28 기획 확정 — 개발 반영 대기 목록' 4·5번 완료(남은 것 6번 상태 표현·새 빌드), 변경 이력 |

## 남은 일
- HUD 문서 본문(하단 버튼 5개 등 옛 내용)을 개편안 내용으로 전면 정리.
- 상태 표현 미적용분(격노 번쩍임·치유 +·장갑 튀김·치명타 숫자).
- 새 빌드(요청 시), 빌드 실측.
