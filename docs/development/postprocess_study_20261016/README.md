# 카메라 후처리 검토·비교

2026-10-16~17 (Asia/Seoul). 사용자 요청은 ‘그래픽을 풍부하게 보이게 할 후처리와 적용 모습’이다. **제안/비교이며 게임 적용 승인이 아니다.** 담당 Notion ‘셰이더·화면 후처리 기준’을 읽고 색보정·제한적인 수정 발광/Bloom 후보와 기본 흐림 효과 제외 방향을 확인했다. Notion 변경/중복 페이지 생성 없음.

## 확인과 제안

- 시작 HEAD `0bb2be4`,Unity6000.6.2f1/URP/Linear,URP HDR지원true/렌더스케일1. 메인 카메라 HDRfalse/후처리false/AA None,Volume0개. 전투 씬active/dirty=false·Play중지/compilingfalse를 조회했다. PixelPerfectCamera 컴포넌트는 없으며 이번에 규격을 확정하거나 추가하지 않았다.
- **우선 추천: 색감·명암 정돈.** Contrast+10/Saturation+6,그림자 약간 차갑게/밝은 부분 약간 따뜻하게 하는 Split Toning을 비교한다. 새 디테일·지형·조명을 만드는 효과는 아니다. 기존 도트 팔레트와 길·숫자 판독은 실제 플레이에서 확인해야 한다.
- **조건부 추천: 약한 Bloom.** Threshold.9/Intensity.12/Scatter.35를 비교한다. 현재 일반 밝은 스프라이트만으로는 변화가 작다. 코어·포탑 수정 부분을 더 또렷하게 빛내려면 해당 부분만 HDR발광하는 개체 셰이더/마스크가 별도로 필요하며 이번에 제작하지 않았다. 발광 부위 외 길/체력바/피격 밝기까지 퍼지는지 확인해야 한다.
- 강한 분위기 비교는 Contrast18/Saturation10/Bloom.5/Vignette.22. 상시 추천안이 아니다. 가장자리 경로·배치 칸을 어둡게 하는 Vignette는 기본0/위험 순간 한정 검토로 유지한다.
- DOF/MotionBlur/ChromaticAberration/FilmGrain/LensDistortion은 이번 기본 추천에서 제외한다. 도트 경계·숫자 가독성을 우선한다. ACES를 무조건 적용하지 않으며 HDR발광을 도입한 뒤 Neutral과 비교할 후속 후보로 둔다.

## 비교 제작 범위

`CapturePostProcessing.cs`를 Assets 밖에서 메모리 실행했다. 별도 임시 additive 씬의 카메라/Volume을 사용해 원래 씬의 동일 카메라 구도를 실제 URP로 캡처한 뒤 임시 객체/프로필/씬을 제거했다. 원래 카메라 HDRfalse/후처리false·원래 씬dirtyfalse를 반환값으로 확인했다. Play/전투 주입/원본 아트 변경 없음. **정지 씬 카메라 이미지이며 런타임 HUD/적/피해 숫자/실제 전투 부하는 포함하지 않는다.**

초기 PreviewScene에서는 후처리가 유효하지 않아 regular additive 씬으로 바꾸고 실제 색·Bloom·Vignette 차이를 확인했다. 중간 SceneManager.CreateScene은 EditMode 부적합으로 실패했으며 EditorSceneManager.NewScene(Additive)로 수정했다. 초기 ARGBHalf 출력은 색공간 확인이 필요해 최종 도구를 ARGB32/sRGB 출력으로 명시했다. 10월17일 패키지 복구 후 기존 전투 씬을 다시 열고 **최종 재캡처 완료**. 현재 저장된 씬의 배치 아트가 포함되며 전투 중 적/숫자/HUD 검증은 아니다. sourceCameraPost/HDRfalse·sceneDirtyfalse 보존 확인(`capture.json`).

인터랙티브 비교는 원본/후처리 슬라이더·추천2가지 효과 선택·코어2배 확대다. Unity 출력 PNG를 픽셀 변경 없이 lossless WebP로 인코딩해577436바이트로 내장했다. 강한 분위기 PNG는 로컬 비교자료로만 보존한다. 브라우저 오류0/선택에 따른 이미지 변경/슬라이더/360px가로넘침없음을 최종 재검사했고 실제 비교 화면을 직접 열어 확인했다(`checks.json`,`comparison_checked.png`). 선택 전 비교 자료이며 영구 VolumeProfile/게임 설정은 만들지 않았다. 사용자가 별도로 요청한 에디터 오류는 [복구 기록](../package_recovery_20261017/README.md)에서 관리한다.

참고: [Unity6.6 Bloom](https://docs.unity3d.com/6000.6/Documentation/Manual/urp/post-processing-bloom.html), [Vignette](https://docs.unity3d.com/6000.6/Documentation/Manual/urp/post-processing-vignette.html), [Tonemapping](https://docs.unity3d.com/6000.6/Documentation/Manual/urp/post-processing-tonemapping.html), [담당 Notion](https://www.notion.so/3fb389dbfc68813ab657f4600eeae76f).
