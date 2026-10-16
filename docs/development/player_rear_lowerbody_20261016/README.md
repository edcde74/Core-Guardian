# 플레이어 상행 하체 방향 수정

2026-10-16. 사용자 실제 화면에서 뒷모습 상체에 옆모습 다리가 남아 비틀려 보인다는 피드백을 받았다. 복구 기준 `75410a4`. Unity 연결·전투 씬active/dirty=false·Play중지/컴파일false를 확인하고 진행했다.

## 원인과 적용

이전 D2는 원본 옆모습 양다리를 단순 반전했다. 원본의 다른 무릎/발 높이, 옆으로 벌어진 다리 축, 옆을 향하는 넓은 앞코가 그대로 남았다. 기존 ‘양다리 반전 일치’ 검사는 이 잘못된 형태도 통과시켰다.

원본 `player.png`의 긴 바지 다리·부츠 목·어두운 뒤꿈치/밑창 픽셀을 잘라 D2 양다리를 재조립했다. 무릎/발목 축을 x13/23으로 나란히 배치하고 양쪽 밑창을 같은 행에 둔다. 앞코의 넓은 금색 강조 대신 기존 어두운 부츠 면을 사용한다. 원본에서 복사한 색만 쓰며, 새 AI 그림이나 원본 PNG 수정은 없다. 현재 상체/팔 리소스는 바꾸지 않았다.

`CoreGuardianPlayerMotion`은 D2에만 새 고관절 기준을 적용하고, 옆으로 벌어지는 다리 회전을 ±6°에서 ±1.5°로 줄였다. 양발 반 주기 교대·들기·거리 기반 주기·정지 블렌드·실제 발사 연결은 유지한다. 다른7방향·몬스터 보행/그림자 정리·포탑·VFX·씬/프리팹·밸런스/Notion은 보존했다.

## 자료와 재생성

- [원본/이전 D2 10배](source_before10x.png), [이전/수정 하체 같은10배](before_after10x.png).
- [실제 W 이동→정지 전후 영상](actual_w_comparison.webp), [정지 후 비교](actual_stopped_comparison.png). 좌 이전/우 수정. 실제 전투 씬 InputSystem 가상 Keyboard W→정상 플레이어 Update→한 칸 이동. 직접 애니메이션 호출이나 Preview 프리팹 녹화가 아니다. 전후 세션의 타이밍은 각각 기록했으며 프레임별 완벽한 위상 동기 비교라고 주장하지 않는다.
- [BuildRearLowerBody](BuildRearLowerBody.cs)는 D2 두 다리만 제작/임포트한다. 모든 방향을 재생성할 때 순서: `player_walk_polish_20261016/BuildSourceFacings.Main` → `player_up_repair_20261016/BuildRearPlayer.Main(true)` → **이 BuildRearLowerBody.Main(true)**. 앞의 두 도구만 실행하면 이전 하체로 돌아간다. 기존 제작 결과/영상은 덮어쓰지 않는다.
- `RecordRearW.cs`는 기존 RecordW의 출력 경로를 이번 폴더로 바꾼 도구다. 시작/완료 상태를 검사하고 자체 Play만 종료했다. 입력 장치와 captureDelta를 복원했다.

## 검증과 한계

- 새 하체 방향 검사: 옆으로 튀어나오는 앞코/다리 축과 뒤꿈치 원본 대응을 확인하도록 기존 반전 검사를 교체했다. [수정 전 실패1/1](red.json) → 수정 후 전체 [EditMode102/102](editmode.json)·[PlayMode18/18](playmode.json) completed. 기존 팔꿈치·8방향·실제 발사/보행·몬스터 경로·코어VFX 회귀 포함.
- 제작기 추가색0. [실제 W 검사](capture_checks.json): 전후각36프레임·X0/Y+0.60000057, 움직인 모든 프레임 Direction2. 원본SHA256 `4af7abc22e50834381367f04c8e9add3517d5831c9b6bf29f989eb14a4ae61e3` 유지. 두 다리 외 다른 방향/Body/Arm 리소스와 원본 PNG/프리팹/씬 파일 Git 변경 없음.
- 자동 W 입력의 실제 씬 Game 카메라 렌더를 직접 열어 확인했다. ScreenSpaceOverlay HUD/사람 직접 플레이 녹화/성능 측정은 아니다. 파생 아트와 자연스러움의 최종 사용자 승인은 별도다.
- [최종 에디터](final_editor_state.json): 전투 씬dirty=false, Play/paused/컴파일false, TimeScale1/captureDelta0. 사용자 Play를 중단하지 않았다. 빌드/실행파일 실행/push 없음.
