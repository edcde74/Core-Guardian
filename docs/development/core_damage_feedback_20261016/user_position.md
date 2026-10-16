# 사용자가 조절한 코어 감소 숫자 위치 적용

2026-10-16. 사용자가 Inspector에서 임시 숫자 위치를 조절하고 ‘맞췄어’로 확정했다. 변경 전 기준 HEAD `777bd40`. CombatArt에 미커밋 변경이 없었으며 이전 위치 조절 도구/인수인계 변경은 이 작업의 기록이다.

Play 종료 전에 앵커 월드위치(18.2437515,6.85750055,0),코어 본체 bounds.max(18.54375,7.23750067)를 읽어 보존했다. [원본 값](user_number_position.json). 기존 시작위치보다 X/Y 각각-.5이며 본체 경계 기준(-.3,-.38)로 `CoreGuardianCombatArt.CoreDamaged()`에 반영했다. 고정 월드좌표 대신 본체 상대 위치를 유지한다. 폴백도 기존보다 X/Y각각-.5 조정했다. 빨간색/몬스터와 같은 TMP 폰트/정렬·수명/B안 플래시는 보존했다.

임시 앵커 제거 후 임시 숫자 없음/앵커 없음/TimeScale1 복원을 API로 확인하고 이 작업에서 시작한 Play를 종료했다. 원본 씬 저장/수정은 하지 않았다. `PositionPreview.cs`는 Assets 밖의 재사용 도구이며 런타임 배포 대상이 아니다.

재컴파일 completed/failed=false. 기존 PlayMode 위치 검사는 ‘코어 경계 밖’ 가정 대신 사용자가 고른 오른쪽·위쪽 영역을 확인하도록 조정했다. 실제 코어 도착/감소·B반응·숫자 색·동일 폰트 검사를 포함한 집중 **1/1 completed**: [이번 결과](user_position_test.json). 이번에 전체 회귀는 재실행하지 않았다. 최종 editor_status ready/Play stopped/compiling=false 및 전투 씬active·loaded·dirty=false 확인. diff --check 통과. 게임 빌드/실행파일 실행/GitHub push 없음.
