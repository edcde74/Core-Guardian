# 현재 전투 씬 후처리 재비교

## 색보정 적용 — 2026-10-17

사용자 “선명하고 일관되게 만드는건 동의해 한번 진행해봐” 승인에 따라 실제 전투 씬에 약한 색보정을 적용했다. 수정 전 복구 기준 `c7e9b31`. 포탑 셰이더는 사용자가 이미 확인하고 만족했다고 했으므로 재승인 대상으로 돌리지 않는다.

- `Assets/Settings/CoreGuardianColorGrading.asset`: ColorAdjustments 대비+10/채도+6, SplitToning 그림자 RGB(0.47,0.49,0.53)/밝은 부분(0.53,0.51,0.47), balance0. 위 5개 파라미터만 override한다. 노출/색상 필터/전체 색상 회전은 기본값이다.
- 전투 씬에 Default 레이어의 Global Volume `Color Grading` 하나를 추가하고 Main Camera의 후처리를 켰다. 카메라 HDR=false, AA None, 기존 URP LDR 색보정 모드와 렌더스케일은 유지했다. Bloom·톤매핑·비네트·블러·그레인·색수차·렌즈 왜곡은 프로필에 넣지 않았다.
- 기존 원본 아트/조명/경로 재질/코어·포탑 국소 셰이더/피격/HUD/숫자/전투 수치와 코드·프리팹은 변경하지 않았다. 씬 YAML을 직접 편집하지 않고 [에디터 API 도구](ApplyColorGrading.cs)로 프로필 하위 에셋과 씬을 저장했다. 삭제 씬은 복원하지 않았다.
- 적용 도중 씬 저장 이후 보고서의 Unity Color 직렬화에서 순환 참조 오류가 있었다. 색 보고를 RGB 배열로 수정한 뒤 적용을 중복 실행하지 않고 `Inspect`로 저장 상태를 재조회했다. 현재 게임/콘솔 오류로 남지 않았으며 [설정 조회](applied_setup.json)에 실제 저장값을 기록했다.

### 실제 출력 확인

- [적용 전](applied_before.png) / [적용 후](applied_after.png): 현재 회색 격자·밝은 석재 경로의 같은 카메라 구도 1920×1080 정지 렌더다. 이 두 장은 Overlay HUD를 포함하지 않는다.
- 자체 Play에서 실제 카메라 Volume stack의 대비10/채도6/Bloom강도0, HDR=false/post=true를 조회했다. 실제 ScreenSpaceOverlay `CombatHUD`도 확인했다.
- [피격 순간 OFF](applied_hit_before.png) / [피격 순간 ON](applied_hit_after.png): 기존 재시작/라운드 시작/포탑 접기 HUD 콜백과 정상 수정100 지출로 단일 포탑1개를 구매·설치하고 1R 자연 발사/피해13을 확인했다. 재화·고HP 대상·발사/피해 이벤트를 주입하지 않았다. 사람 직접 입력/밸런스 측정은 아니다.
- [캡처 코드](CaptureAppliedColorQA.cs)는 첫 자연 피해 숫자가 생겼을 때 TimeScale만 잠시0으로 고정해 같은 게임 시점의 Volume weight1/0 화면을 실제 합성 GameView에서 캡처했다. 임시 카메라 줌/배속/가상 입력 장치 없이 원래1920×1080 구도를 유지했다. finally에서 weight1/TimeScale1로 복원하고 자체 Play를 종료했다. [실행 기록](applied_hit_qa.json).
- 두 화면에서 피해 숫자13·머리 위 체력바·피해 집계13과 기존 HUD 글자가 판독 가능하고, 번짐/가장자리 암전이 추가되지 않은 것을 직접 확인했다. 색보정 변화는 작다. [픽셀 차이](applied_comparison.json)의 채널 평균 절대차는 약2.44/255이며 반투명 HUD 배경은 뒤 게임 화면의 색 변화가 비칠 수 있으므로 HUD 전체가 픽셀 동일하다고 주장하지 않는다.
- [최종 상태](applied_final_status.json): 전투 씬1/dirty=false, Play/paused/compiling/compilationFailed=false, TimeScale1/captureDelta0, camera target 복원, HDR=false/post=true, Volume1/weight1/프로필2항목. 별도 console_status에서 현재 오류0/경고0 확인. 카메라 캡처 후 미저장 표시는 에디터 API 저장으로 정리하고 종료 Play 이후 재조회했다.
- 이번 검증은 색보정 설정과 실제 HUD/자연 피해 숫자 출력 확인이다. 새 자동 테스트 스위트·전체 회귀·다수 전투 성능 검사를 실행한 것이 아니며 최종 색감 선호는 사용자 화면 확인 대상이다. 빌드·실행파일·push·밸런스/Notion 변경 없음. 연결된 기존 셰이더 담당 Notion을 읽었고, 이 색보정 채택은 이번 사용자 지시가 최신 근거다.

## 아래는 적용 전 비교 이력

2026-10-17. 사용자가 이전 비교의 배치가 현재 테스트 씬과 다르다고 지적했다. 이전 배치 비교를 현재 씬의 시안으로 제시한 것을 정정한다. 현재 실제 전투 씬의 회색 격자 바닥·석재 경로·플레이어·코어를 기준으로 다시 캡처했다.

- 시작 HEAD f8f0f16. 연결 CD/Unity6000.6.2f1, 전투 씬 active/dirty=false, Play 종료·compiling=false·TimeScale1. Play를 시작하거나 종료하지 않았다.
- 원본은 실제 Game view 1920×1080 캡처(original.png). 비교 A(color.png)·B(gentle_glow.png)·강한 비교(strong.png)는 동일 메인 카메라 위치/구도/1920×1080의 임시 카메라와 URP Volume 렌더다. 임시 렌더 첫 원본 출력이 배경만 나왔으므로 비교 원본에는 실제 Game view 캡처를 사용했다. capture.json의 original 항목은 해당 실패 출력 조건이며 최종 original.png의 출처가 아니다.
- A: Contrast10/Saturation6/Split Toning. B: A+Bloom Threshold0.9/Intensity0.12/Scatter0.35. 강한 비교는 이전 도구의 비교 조건을 보존했다. 효과 규칙/이전 과정은 [기존 담당 기록](../postprocess_study_20261016/README.md).
- 직접 확인: 색보정 변화는 작다. Bloom은 코어 외 경로의 금색 테두리도 밝게 번지므로 코어만 강조하는 효과로 설명하면 안 된다. 국소 발광은 별도 미구현 후보다.
- 정지 씬이며 런타임 HUD/적/피해 숫자/성능 검증은 아니다. 새 Play/자동검사/게임 적용 없음.
- 종료 실조회: Play=false, dirty=false, scene1/camera1/Volume0, 원래 카메라 HDR=false/post=false, 현재 콘솔 오류0/경고0. 임시 객체/씬 제거 확인. 기존 효과·원본 아트·프리팹·게임 코드는 변경하지 않았다.
