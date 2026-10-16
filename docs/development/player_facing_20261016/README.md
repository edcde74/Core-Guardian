# 기술병 원본 기반 방향별 보행·조준 — 2026-10-16

**후속 사용자 피드백으로 보행/얼굴을 수정했다. 현재 제작기·동작·검증은 [출발/정지 보행 수정](../player_walk_polish_20261016/README.md)을 따른다. 이 폴더의 빌더/비교/98·18 검사는 이전 구현 이력이며 현재 리소스를 다시 만들 때 사용하지 않는다.**

사용자 지정 애니메이션 1순위. 기존 `player.png`를 분리·반전·이동·최근접 재배치하는 방식으로 제작한다. 생성형 방향 시안은 채택하지 않았고 게임에 임포트하지 않았다. 기본 방향 원본 PNG·기존 레이어·프리팹·씬·전투 수치는 보존한다. **기능 검증과 파생 외형/자연스러움의 최종 사용자 승인은 별도다.**

## 제작 범위

- 입력: `Assets/Art/ScreenTests/20260930/Sprites/player.png`, 36×48, PPU32, 발 피벗(18,0).
- 출력: `Assets/Resources/CoreGuardian/PlayerFacing/D0..D7/`, 방향별 Body/Arm/LeftLeg/RightLeg 총32개. D0동/D1북동/D2북/D3북서/D4서/D5남서(기본 원본)/D6남/D7남동.
- [제작기](BuildSourceFacings.cs)는 원본 불투명 픽셀을 복사한다. 기본 방향은 원본과 보이는 픽셀 차이0, 새 색0. 모자·머리카락·배낭은 원본 조각을 재배치하며 북향은 얼굴 대신 모자 뒤/머리카락과 등 배낭이 보인다. 새 캐릭터 그림으로 교체하지 않는다.
- [같은 6배 픽셀 비교](comparison6x.png): 원본, 동/북동/북/북서/서/남서/남/남동 순서. 이 비교는 조준 전 정지 레이어 합성이다.
- `CoreGuardianPlayerMotion`은 기술병에만 적용한다. 상체/팔/양다리를 별도 표시하며 실제 이동 거리64원본픽셀=한 보행 주기다. 기존 한 타일 이동은 반 주기, 좌우 발 교대. 이동 속도/키 입력은 변경하지 않는다.
- 이동 중 타깃이 있으면 보행은 계속하고 몸은8방향, 팔은 실제 조준 벡터를 따른다. 전체 원본 Transform이나 그림자는 회전하지 않는다.
- 실제 `ProjectileLaunched → Fire` 경로를 그대로 사용한다. 발사 프레임에 즉시 방향·팔·도구 끝 위치를 갱신하며 기존 투사체 표시 경로에 전달한다. 별도 발사 지연·공격 주기·피해 변경은 없다.
- 몬스터 X/Y 보행과 포탑 애니메이션, 별도 코어 VFX 작업은 수정하지 않는다.

## 최종 검증

- 실제 CD 프로젝트에서 전체 **EditMode98/98·PlayMode18/18 completed**. [EditMode](editmode_completed.json), [PlayMode](playmode_completed.json).
- 추가 Play 검사는 `TryStep` 정상 타일 이동→실제 위치 변위→전투 어댑터의 양발 표시, 실제 Session 발사 이벤트→표시 투사체 시작점/도구 끝 일치와 목표 방향을 확인한다. 기존 몬스터 전 경로·포탑·원본·코어 VFX 회귀도 포함한다. 전투 주기 주입 검사는 기능 QA이며 밸런스 실측이 아니다.
- [실제 Unity 움직이는 비교](eight_directions.webp), [정적 화면](unity_comparison.png): 원본/동/북동/북/북서/서/남서/남/남동 순서, 위 보행·아래 보행+Fire 표시, 같은 원본4배 배율. 확인용 표준 보행 주기를0.5배속으로 재생한다. PNG48장 입력, 동일 인접 프레임 병합 후 WebP46프레임·2.016초. 격리 PreviewRenderUtility 캡처는 실제 씬의 사람 플레이 녹화나 실제 Session 발사 녹화가 아니며, 실제 연결은 위 Play 검사로 별도 확인했다.
- 최종 현재 씬은 전투 씬active·dirty=false·Play=false·compiling=false·TimeScale1, 새 플레이어 모션 로드 확인. [최종 상태](final_editor_state.json).
- 별도 읽기 전용 코드 검토에서 중요한 정확성/회귀/성능 문제 없음. 자연스러움은 별도 검토 대상이다.

## 중단·복구 기록

- 복구 기준: 작업 시작3ed4f1e, 병행 VFX 체크포인트1e0e12e. 시작 당시 Unity ready/Play중지/컴파일false를 확인했다.
- [구현 전 실패](red.json): 추가한 플레이어 검사2/2 실패로 표시 레이어·양발 움직임·즉시 발사 방향 누락을 재현했다.
- [픽셀·임포트 감사](source_audit.json), [읽기 전용 감사 도구](audit_source.py): 원본 SHA256,32레이어 크기/Point/PPU32/mipmap없음, 추가색0, 기본 방향 보이는 픽셀 차이0, 합성8종 구분 확인. Git 비교상 원본 PNG/원본 프리팹/전투 씬 차이0.
- 런타임 연결 코드 컴파일 completed/failed=false 확인 후 전체 EditMode를 요청했으나 응답 제한시간 초과. 해당 [응답](editmode.json)은 **테스트 통과 결과가 아니다**.
- 같은 시각 별도 VFX 채팅도 공유 에디터에서 테스트와 Play를 진행 중이었다. Unity 로그에서 test tree/PostbuildCleanup/Play 중 씬 복원 오류와 동기 검사 취소가 확인됐고 Pipeline 명령 통로가 응답하지 않았다. 사용자/다른 작업의 Play 중단·에디터 재시작은 하지 않았다. `Temp/pipeline_test_status.json`의17/17은19:09 별도 VFX 결과이므로 이번 구현 통과로 재사용하지 않는다.
- 공유 에디터를 보존하고 `Temp/PlayerFacingValidation_20261016`에 검증 사본과 별도 배치 에디터를 준비했다. Assets/Packages/ProjectSettings는 복사하고 PackageCache는 원본 캐시를 읽는 junction으로 연결했다. 이후 사용자가 Play를 중지했고 새로 제공된 Unity MCP로 주 프로젝트 상태/저장 상태를 확인해 주 프로젝트 검증을 재개했다. 임시 사본 실행 결과는 위 통과 수치에 사용하지 않는다. 사용자 에디터 강제 종료 없음.
- 복구 직후 [중간97/98](intermediate_editmode.json)의1건은 새 검사에서 발사 표시 프레임을 거치지 않고 회복을 요구했기 때문이다. 실제 어댑터와 동일하게 `Present(0)`으로 발사 프레임을 확인한 뒤 회복을 검사하도록 고쳤다. 기존 첫 발사 프레임 유지 동작은 변경하지 않았다.

## 미확정

원본 조각 재배치로 만든 정면·뒷면의 외형과 보행/조준 자연스러움은 최종 사용자 검토 대상이다. 기능 검사만으로 승인 처리하지 않는다. 새 게임 빌드·게임 실행파일 실행·GitHub push 없음.
