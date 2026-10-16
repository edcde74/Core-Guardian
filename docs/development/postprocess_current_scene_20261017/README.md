# 현재 전투 씬 후처리 재비교

2026-10-17. 사용자가 이전 비교의 배치가 현재 테스트 씬과 다르다고 지적했다. 이전 배치 비교를 현재 씬의 시안으로 제시한 것을 정정한다. 현재 실제 전투 씬의 회색 격자 바닥·석재 경로·플레이어·코어를 기준으로 다시 캡처했다.

- 시작 HEAD f8f0f16. 연결 CD/Unity6000.6.2f1, 전투 씬 active/dirty=false, Play 종료·compiling=false·TimeScale1. Play를 시작하거나 종료하지 않았다.
- 원본은 실제 Game view 1920×1080 캡처(original.png). 비교 A(color.png)·B(gentle_glow.png)·강한 비교(strong.png)는 동일 메인 카메라 위치/구도/1920×1080의 임시 카메라와 URP Volume 렌더다. 임시 렌더 첫 원본 출력이 배경만 나왔으므로 비교 원본에는 실제 Game view 캡처를 사용했다. capture.json의 original 항목은 해당 실패 출력 조건이며 최종 original.png의 출처가 아니다.
- A: Contrast10/Saturation6/Split Toning. B: A+Bloom Threshold0.9/Intensity0.12/Scatter0.35. 강한 비교는 이전 도구의 비교 조건을 보존했다. 효과 규칙/이전 과정은 [기존 담당 기록](../postprocess_study_20261016/README.md).
- 직접 확인: 색보정 변화는 작다. Bloom은 코어 외 경로의 금색 테두리도 밝게 번지므로 코어만 강조하는 효과로 설명하면 안 된다. 국소 발광은 별도 미구현 후보다.
- 정지 씬이며 런타임 HUD/적/피해 숫자/성능 검증은 아니다. 새 Play/자동검사/게임 적용 없음.
- 종료 실조회: Play=false, dirty=false, scene1/camera1/Volume0, 원래 카메라 HDR=false/post=false, 현재 콘솔 오류0/경고0. 임시 객체/씬 제거 확인. 기존 효과·원본 아트·프리팹·게임 코드는 변경하지 않았다.
