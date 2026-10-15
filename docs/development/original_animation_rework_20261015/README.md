# 원본 픽셀 기반 공격 동작 1차

2026-10-15. 사용자 지시: 원본 외형을 다시 그리거나 재해석하지 않고 그대로 움직인다.

## 적용 범위

- 복구 기준 HEAD: `5d05c04`. 수정 전 추적 파일 변경 없음. 기존 AudioProduction/·밸런스/BGM 미커밋 자료 보존.
- 원본 `Assets/Art/ScreenTests/20260930/Sprites/{player,tower_single,tower_multi,tower_support}.png`를 고정부/가동부로 분리했다. 새 색·그림·장비를 그리지 않았다. 원본 PNG·메타·프리팹·씬은 변경하지 않았다.
- 플레이어 렌치 팔 회전, 단일/다중 포탑 상부의 작은 반동, 지원 포탑 수정의 작은 상하 동작. 받침대·발·그림자는 고정한다. 정지 시 파생 레이어를 숨기고 기존 원본 SpriteRenderer를 그대로 표시한다.
- `CoreGuardianOriginalMotion`과 `OriginalMotion` Resources 레이어를 사용한다. `ProjectileLaunched`에서 동작을 시작하고 원본 렌치/포구 위치에서 기존 투사체를 표시한다. 추가 총구 효과나 새 무기는 없다. 발사 주기·피해·투사체 물리는 변경하지 않았다.
- 지원 포탑은 별도 발사체가 없다. 실제 지원 반경 안 공격 포탑의 발사에 맞춰 수정이 반응한다.

## 확인 결과

- 원본 PNG의 불투명 픽셀과 레이어 재합성 차이: 4종 모두 **0픽셀**. [검사 JSON](verification.json). 원본과 같은 PPU32·피벗·Point·무압축으로 파생 레이어를 가져왔다.
- Unity 프리뷰 씬에서 4종의 실제 공격 동작·고정부 고정·정확한 원본 복귀 검사: 최초 미구현 실패 [red](red.json) → 구현 후 4종 통과 [green](green.json).
- EditMode CoreGuardian 75/75 completed: [결과](editmode.json). PlayMode 전체 10/10 completed: [결과](playmode.json). 실제 발사 중 원본 Sprite 참조·원본 Transform 유지, cutout 활성/복귀, 거부한 총구 효과 비활성을 검사한다.
- 실제 Unity 렌더러 40프레임: [움직이는 비교](unity_original_motion.webp), [공격 순간](unity_attack.png). 각 쌍 왼쪽 원본/오른쪽 동작. 위 플레이어·단일, 아래 다중·지원. 시작/복귀 프레임 RGBA가 동일하고 공격 프레임은 다름을 확인했다. 사용자 전투 씬 대신 격리된 PreviewRenderUtility에서 촬영했다.
- [브라우저 비교](preview.html)는 같은 소스 픽셀로 동작을 느리게 볼 수 있는 보조 자료다. 실제 Unity 캡처를 우선한다.
- 종료: 전투 씬 active, Play stopped, compiling=false, dirty=false, TimeScale1. [상태](final_state.json). 새 빌드·노션 수정·GitHub push 없음.

## 미완료 / 승인 구분

이번 것은 **원본 기본 방향의 공격 동작 1차**다. 전 방향 조준/플레이어 방향 전환·보행은 미완료다. 원본 한 방향 이미지를 몸통째 돌리거나 거부된 방향별 도안으로 대체하지 않았다. 향후 방향 작업에서도 원본 외형 유지가 필수다.

기능 회귀 통과는 아트 승인·자연스러움 승인·밸런스 실측이 아니다. 지원 반경·공격 이벤트 검사는 기능 확인이며 사람 플레이 결과로 쓰지 않는다. HP27/1R1.62/2R1.53 및 노션 밸런스는 그대로다.

재현: `BuildOriginalLayers.cs` → AssetDatabase.Refresh/컴파일 → `VerifyOriginalMotion.cs` → `CaptureUnityMotion.cs`. 결과는 이 폴더에 남기며 다른 회차 결과를 덮어쓰지 않는다.
