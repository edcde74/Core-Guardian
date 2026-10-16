# 실제 W 상행 플레이어 재조립 — 2026-10-16

후속 사용자 피드백으로 하체의 단순 반전은 교체했다. 현재 재생성은 이 BuildRearPlayer 뒤 [BuildRearLowerBody.Main(true)](../player_rear_lowerbody_20261016/README.md)를 추가 실행해야 한다. 아래 영상/검증은 하체 후속 수정 전의 이력이다.

사용자 [실제 화면 피드백](user_feedback.png): W로 위로 갈 때 외형이 계속 무너진다. 복구 기준2986a74. 이전 기능 통과/고정 위치 비교만으로 실제 W 이동 외형을 충분히 검토했다고 보고한 것은 잘못이었다. 사용자 Play 중임을 확인해 중단하지 않고 읽기 진단/수정안만 준비했으며, 사용자가 ‘Play 껐어. 계속해’로 알린 뒤 stopped/컴파일false/전투 씬dirty=false를 다시 확인하고 적용했다.

## 실제 원인과 최소 수정

- 기존 D2 뒷모습은 배낭 마스크가 x26 이후의 맨팔·팔꿈치 픽셀까지 포함해 등 가운데로 옮겼다. 원래 오른팔은 기존 몸 위치에 남고 움직이는 팔만 반전해 팔/하체의 방향도 달랐다. 얼굴/목 일부를 복구하는 코드 때문에 뒷머리 아래 앞 얼굴 조각도 남았다.
- **D2 상행만** 원본 기술병의 몸·팔·두 다리를 같은 방향으로 반전해 재조립했다. 배낭의 실제 금속/가죽 영역과 팔 영역을 다시 나누고 배낭을 등 중앙에 배치했다. 원본 모자 윤곽·머리카락/셔츠 픽셀을 사용해 얼굴 잔여 조각을 없앴다. 양팔은 등 밖의 어깨 위치로 연결한다. 해당 반전 하체에 맞춰 D2 보행 피벗만 좌우 대응을 맞췄다.
- 새 생성 그림/색/가로 압축/원본 PNG 변경 없음. 다른7방향, 공격/실제 발사 원점, 이동 수치·키 입력·기존 보행 주기, 몬스터X/Y·다리 폭·포탑·VFX·씬·프리팹·CSV/Notion은 보존했다.
- [현재 상행 제작기](BuildRearPlayer.cs), [원본/뒷모습 같은8배 비교](source_rear8x.png), [픽셀/임포트 감사](source_audit.json): 원본 SHA동일·추가색0·기본 방향 원본 보이는 픽셀 차이0·8종 합성 구분·PPU32/Point/mipmap없음.
- 전체 방향 리소스를 다시 만들 때는 `player_walk_polish_20261016/BuildSourceFacings.cs` 실행 후 **반드시 이 BuildRearPlayer.Main(true)를 후처리**한다. 앞의 전체 빌더만 실행하면 잘못된 이전 D2가 복구되므로 주의한다. 기존 기록/영상을 덮어쓰지 않았다.

## 이번에는 실제 W 입력 화면으로 확인

1. 별도의 새 자체 Play에서 [ProbeW](ProbeW.cs)가 InputSystem 임시 Keyboard에 W 상태를 전달했다. `TryStep`나 단위 애니메이션 Move를 직접 호출하지 않고 실제 `PrototypeGridMovement.Update`의 W 입력 경로를 사용한다.
2. 이동 중 자체 세션을 잠시 멈춰 [수정 전 실제 게임 카메라](before_w_game.png)/[수정 후 실제 게임 카메라](after_w_game.png)를 캡처했다. [같은 카메라·같은 확대 배율 전후](actual_w_comparison.png). 두 캡처는 이동 중 시점이 달라 동일 보행 위상 비교로 주장하지 않는다. [전 상태](before_w_state.json)/[후 상태](after_w_state.json): 모두 실제 월드 Y 증가·Direction2·D2 Body/Arm/양다리 표시 확인.
3. [RecordW](RecordW.cs)는 실제 씬 카메라/동일 W 입력의 이동→정지36프레임을 캡처했다. [실제 W 이동 영상](actual_w.webp), [각 프레임 위치/표시](actual_w_sequence.json), [검사](actual_w_checks.json): X 이동0/Y+0.60000057(한 타일), 이동한 모든 프레임 Direction2. 60Hz 고정 캡처 간격을 임시 사용하고 복원했으며 성능 측정이 아니다. 전체1600×900 PNG를 보존하고 영상은 해당 플레이어 영역만 같은 Nearest3배로 확대했다. 게임 카메라 렌더이므로 ScreenSpaceOverlay HUD는 포함하지 않는다. 사람 키보드 플레이 녹화가 아닌 자동 W 입력의 실제 씬 검증이다.
4. 임시 입력 장치를 제거하고 이전 Keyboard/current 및 captureDelta0을 복원한 뒤 **이번에 시작한 자체 Play만 종료**했다. 사용자 Play를 임의 중단하지 않았다.

## 기능 회귀와 남은 판단

- 새 [수정 전 검사](red.json)는 원본 팔꿈치가 등 밖에 있어야 하는 위치에 없음(투명)으로 실패했다. 수정 후 전체 **[EditMode102/102](editmode.json)·[PlayMode18/18 completed](playmode.json)**. 반전 양다리/외부 팔꿈치, 실제 정상 타일 이동·기존 발사 원점·몬스터 전체 경로·HUD·코어 VFX 회귀 포함.
- 이번 실제 W 화면은 직접 열어 확인했다. 테스트 숫자가 보행 자연스러움/파생 아트의 사용자 최종 승인을 대신하지 않는다. 북동/북서 등 다른 미승인 파생 방향까지 이번 W 확인으로 승인 처리하지 않는다.
- [최종 상태](final_editor_state.json): 기존 전투 씬active/dirty=false, Play=false·paused=false·컴파일false·TimeScale1·captureDelta0. 게임 빌드/게임 실행파일 실행/GitHub push 없음. 인수인계와 로컬 커밋 갱신.
