# 현재 전투 씬 후처리 재비교

## 현재 색보정 2차 조정 — 2026-10-17

사용자가 첫 적용의 차이가 너무 적다며 “좀더 바꿔볼래? 선명하고 일관되게 하고싶은데 차이가 너무 적다”로 추가 조정을 지시했다. 복구 기준 `580b0c0`. 기존 프로필만 에디터 API로 수정했으며 씬·원본 아트·재질·국소 효과·HUD·전투 수치는 그대로다.

- 현재값: **대비+35/채도+12/노출-0.45EV**, SplitToning 그림자 RGB(0.45,0.49,0.55)/밝은 부분(0.5,0.5,0.5)/balance-10. 첫 적용의 따뜻한 밝은 색조를 중립으로 돌리고 배경 중간 밝기를 낮춰 청록/금색 및 외곽 명암 대비를 더 드러냈다. 카메라 HDR=false/post=true와 프로필2항목 유지, Bloom·흐림·톤매핑·비네트 추가 없음. 샤프닝/해상도 향상이 아니라 색·명암 조정이다.
- [튜닝 코드](TuneColorGrading.cs)는 임시 프로필과 하위 컴포넌트를 개별 복제해 현재값/강한2안을1920×1080 같은 구도로 렌더한 뒤 원래 참조를 복원했다. 첫30/-0.15와42/-0.25 비교는 차이가 작아 폐기·덮어썼다. 최종 [기존 약한 설정](revision2_previous.png) / [적용35/-0.45](revision2_clear.png) / [미적용45/-0.65](revision2_crisp.png)를 직접 비교하고 덜 어두운35/-0.45를 저장했다. [설정](revision2_setup.json). 재생성 시 최초 제작 도구 다음 `TuneColorGrading.Apply`를 실행해야 현재값이 된다.
- [실제 HUD·피해 숫자 포함 이전](revision2_hit_before.png) / [현재](revision2_hit_after.png): 정상 초기 수정100으로 단일포탑1개 설치→기존 HUD 콜백 시작→자연 피해13을 확인했다. 같은 게임 시점을 TimeScale0으로 잠시 고정하고 현재/이전 프로필을 교체한 실제 합성 GameView다. 카메라 줌·재화/피해/발사 주입 없음. [캡처 도구](CaptureAppliedColorQA.cs)의 `Main("revision2",true)`는 임시 복제 프로필을 정리하고 현재 프로필/weight1/TimeScale1을 복원했다. [실행 기록](revision2_hit_qa.json). 사람 직접 입력·밸런스 측정은 아니다.
- 직접 확인: 바닥의 밝기/따뜻한 기운이 줄고 경로 돌 명암·코어/포탑 청록·금색 대비가 강해졌다. 실제 피해13·체력바·HUD 글자가 판독 가능하다. [동일시점 비교 수치](revision2_comparison.json): 배경 표본 RGB(174,170,158)→(150,148,140), 전체 채널 평균 절대차 약15.50/255. 효과의 좋고 나쁨을 픽셀 차이만으로 승인하지 않는다.
- [실제 Play stack](revision2_runtime.json)에서도35/12/-0.45, Bloom0/HDRfalse/posttrue를 확인했다. [최종](revision2_final_status.json): 자체 Play 종료, 전투 씬1/dirtyfalse/compilingfalse/failedfalse/TimeScale1/captureDelta0/camera target 복원, 기존 프로필2항목·weight1. 별도 콘솔 실조회 현재 오류0/경고0. 게임 에셋 변경은 기존 색보정 프로필1개뿐이다.
- 자동 테스트 스위트/전체 회귀/다수 전투 성능 검사는 이번에 실행하지 않았다. 최종 색감 선호는 사용자 확인 대상이다. 빌드·실행파일·push·밸런스/Notion 변경 없음. 아래 최초 적용/비교 파일은 이력으로 보존한다.

## 첫 약한 색보정 적용 이력 — 2026-10-17

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
# Notion 동기화 — 2026-10-17 후속

사용자 요청으로 기존 [셰이더·화면 후처리 기준](https://www.notion.so/3fb389dbfc68813ab657f4600eeae76f)의 색보정 후보/후처리 꺼짐 문구를 현재 2차 적용 상태로 갱신했다. 첫 설정 대비 사용자 “이번게 낫긴하네” 피드백, 현재 값/HDR·Bloom 미적용, 실제 Overlay·자연 피해 비교의 범위와 미실시 검증을 담당 문서에 나눠 반영했다. [VFX 검증 기록](https://www.notion.so/3fb389dbfc6881f78273d798f989b1cf)·[공통 아트 기준](https://www.notion.so/3f4389dbfc688118965ecb7a1ffaaf3c)도 기존 절에 연결하고 재조회했다. 상세 동기화 근거는 [단일 포신 담당 기록](../single_tower_aim_20261017/README.md). 문서 동기화에서 새 Unity 검사·게임 변경은 없다.
