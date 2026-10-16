# 현재 전투 씬 시각 조합 4안

2026-10-17. 사용자 요청: 조명·후처리·셰이더·파티클을 함께 고려한 여러 시안을 실제 현재 씬으로 제시한다. 최종 PNG는 1920×1080, Main Camera의 위치·구도·원본 아트와 배치를 유지한 Unity URP 정지 렌더다. 생성형 이미지가 아니다. 최종 게임 적용·아트 승인·성능 검증이 아니다.

## 시안

| 파일 | 조합 |
| --- | --- |
| A_clear_fortress.png | 밝은 환경 조명, 약한 코어 청록 Light2D, 미세 색보정 |
| B_cool_depth.png | 차가운 환경 조명, 코어 주변 청록 Light2D, 입자·수정 발광 오버레이 없음 |
| C_crystal_energy.png | 중간 밝기 환경, 강한 코어 청록 Light2D, 원본 청록 픽셀 마스크의 HDR 추가 발광, Bloom, 사각 입자22개 |
| D_warm_machinery.png | 따뜻한 환경·상단 보조 조명, 코어 청록빛, 수정 발광·Bloom, 청록/금색 입자12개 |
| original_restored.png | 임시 비교 제거 후 실제 Game view의 원래 상태 |

정확한 렌더값은 capture.json을 따른다. 입자는 실제 ParticleSystem에 정해진 시드로 방출하고 0.01초 계산 후 고정한 시점이다. 움직임·사건 연동을 검증한 것이 아니다. 발광은 원본 코어 스프라이트에서 청록색 조건에 맞는 픽셀을 추출해 같은 위치에 추가 렌더하는 임시 셰이더다. 최종 수작업 마스크는 없으며 원본 픽셀은 수정하지 않았다. 노멀맵·새 그림자·새 전투 효과는 제작하지 않았다. HUD/전투 숫자/포탑/몬스터 다수 전투는 캡처에 포함되지 않는다.

## 임시 실행과 복원

- Assets 밖 CaptureVisualOptions.cs를 메모리 컴파일했다. 임시 additive 씬에 카메라·Light2D·Volume·발광 오버레이·ParticleSystem을 만들었다. 비교 동안 타일맵 재질과 기존 Global Light의 색/밝기를 임시 교체한 뒤 finally에서 원래 참조/값으로 복원했다. 경로 PathContrast_A의 HDR _Color(2.15,2.18,2.22)를 임시 Lit 재질에도 보존했다. 캐릭터·그림자 재질은 유지했다.
- 중간 첫 렌더는 경로 재질의 밝기 누락 때문에 폐기·덮어썼다. 새 조명의 메쉬/바운딩은 Light2D.LateUpdate의 패키지 소스를 확인해 갱신한 뒤 캡처했다. 초기 입자 초기화로 실제0개였던 출력도 폐기했다. 최종 실제 입자수 C22/D12와 유효 bounds를 확인했다. 최종 스크립트 컴파일 진단0.
- 도중 공유 Unity가 Play/paused로 바뀌어 가드가 실행을 거부했다. Play를 중단하지 않고 대기했으며, 사용자 ‘플레이모드껐어’ 후 stopped 상태를 다시 확인하고 재캡처했다.
- 종료: 전투 씬1/카메라1/Global Light1, dirty=false/Play=false/paused=false/compiling=false/compilationFailed=false, TimeScale1/captureDelta0. 원래 카메라 HDR=false/post=false, Global Light intensity1/white, Volume0/ParticleSystem0. 모든 원래 재질 참조 복원 확인. 현재 콘솔 오류0/경고0. 과거 도구 오류 이력과 구분했다.
- 저장 씬 SHA256 전후 동일: `57D5EAFBE20D99627970F8CF1EA07B95DA34FEA48B2E53AB79BC0DB6CBFE3A65`. Assets/Packages/ProjectSettings Git 변경 없음. 시작 HEAD ef87ddb 이후 다른 채팅 커밋들이 추가됐으며 보존했다.
- 새 Play·자동검사·빌드·실행파일·Notion 수정·Push 없음. 영구 Light/Volume/Material/Shader 에셋을 만들지 않았다. 원본 승인·몸 피격0.09초·코어 피해B·숫자 위치를 변경하지 않았다.

관련: [현재 씬 후처리 단독 비교](../postprocess_current_scene_20261017/README.md), [셰이더·후처리 담당 기획](https://www.notion.so/3fb389dbfc68813ab657f4600eeae76f).
