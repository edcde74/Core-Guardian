# 같은 텍스처 마스크 메시 묶기 실험

2026-10-21. 복구 기준 `eba2b8e`. 사용자의 구현 승인으로 실험했으나 실제 부하의 호출 절감은 **0%**여서 제품 변경을 원복했다. 이후 사용자가 선택한 범위는 [텍스처 아틀라스 설계](../mask_atlas_design_20261021.md)다. 아틀라스 생성·게임 적용은 하지 않았다.

## 결과

128적/15포탑, 1920×1080, 카메라8.5, VSync1, targetFPS=-1, 배속1, 30프레임 준비 후100프레임×3회. 기기·주입 방식·Profiler 항목은 [기준 측정](../draw_calls_20261021/README.md)과 같다. Editor 주입 부하이며 정상30R·standalone 판정이 아니다.

|회차|Draw Calls 중앙 전→후|SetPass 중앙 전→후|프레임 간격 중앙 ms 전→후|FPS 최소/최대/평균 전→후|
|---|---|---|---|---|
|0|3208→3208|1608→1608|43.610→36.367|9.177/25.363/21.123 → 5.409/29.336/25.037|
|1|3208→3208|1608→1608|49.031→37.192|8.049/25.213/19.056 → 15.160/29.452/24.600|
|2|3208→3208|1608→1608|41.943→37.247|14.219/25.657/22.726 → 15.818/29.372/26.025|

Draw Calls 절감0건/0%, SetPass 절감0건/0%. FPS 평균은 높지만 호출 자체가 줄지 않았고 Editor 변동·샘플 시점의 영향을 분리하지 못했으므로 후보의 효과로 주장하지 않는다. 0회차 최소 FPS는 악화했다. 프레임 간격은 CPU 처리 시간과 동일하지 않다. 상세값·p95는 [전후 요약](comparison.json), 세부 CPU/메모리 마커는 `after/*_markers.json`과 압축 Profiler 원본을 따른다.

[실제 소스 조사](source_inventory.json): 128적, 마스크 소유자274, 파츠1304, 제출1304, 묶인 소유자0. 공격형의 Body/LeftUpper/LeftLower/RightUpper/RightLower가 서로 다른 텍스처다. 동일 텍스처 그룹을 묶는 기능 자체는 동작해도 현재 입력에는 묶을 대상이 없다. 적0 상태의 늦은 초기 조사는 제외하고 주입 후30프레임에 다시 조사한 이 파일만 근거로 사용했다.

## 구현·품질·실패

후보는 texture 참조+정확한 alpha별로 메시를 묶고 GPU에 파츠별 변환 행렬을 전달했다. CPU에서 좌표를 미리 변환하지 않았다. 다른 텍스처/alpha와 인덱스 한도 초과는 분리·원본 경로로 처리했다. [후보 C#](candidate.cs), [셰이더](candidate.shader), [테스트](candidate_tests.cs)는 **기록용이며 Assets 밖에 있다**.

- [먼저 실패한 계약 검사](red_result.json): 새 draw 진단 필드가 없어 실패했다.
- [초기4건 검사](edit_initial_failure.json): 메시 정점 감소 시 이전 삼각형 인덱스가 남아2건 실패했다. 정점 수 감소 전에 삼각형을 비우도록 고친 뒤 [4/4 통과](edit_result.json)를 확인했다. 이는 후보의 결과다.
- [픽셀 비교 도구](PixelComparison.cs)와 `pixels/*_effects_pixels.json`: 8역할×432=3456건 차이0. 양쪽 빈 출력85건을 제외하면3371건이다. 동일한 효과 출력 비교이며 보스 본체 전체 합성의 기존 불확실성을 해결한 결과가 아니다. 각 역할 RT 원복41→41은 이번 도구의 범위이며 전체 장기 누수 통과가 아니다.
- 원복 후 원본 검사 재실행은 [running](restored_tests.json)에서 완료 결과를 얻지 못했다. 자체 요청만 [취소](restored_test_cancel.json)했다. 원본3/3 재통과로 주장하지 않는다. 오래된 MCP 결과도 현재 결과로 사용하지 않았다. 연결 복구 후 공식 CLI를 사용했다.

## 원본·복원

[계측 도구](Probe.cs), [소스 조사 도구](Inventory.cs), [Profiler 압축 원본](profiler_raw.zip), [3개 항목 SHA256/크기](archive_manifest.json), [압축·검증 스크립트](archive_profiler.ps1)를 보존한다. 로컬 `.data`는 중복 커밋하지 않는다. 기존 전 측정 원본은 기준 폴더에 있다.

제품3파일은 `eba2b8e`의 원본 바이트로 되돌렸다: `CoreGuardianSelectionOutline.cs`, `SelectionMask.shader`, `CoreGuardianSelectionOutlineTests.cs`. 원복 사본도 이 폴더에 보존했다. `Assets`/`Packages` diff 없음과 [설정 해시](settings_restored.json)를 대조한다. 기존 사용자 설정3파일의 HEAD 대비 차이는 보존한다. 자동 재추가된 SENTIS 심볼만 시작 디스크 값으로 복원하며 런타임 재추가 가능성은 남는다.

[종료 상태](restored_engine.json): 기존 전투씬1개/dirty=false, Play 정지, 컴파일·업데이트·Profiler·Deep=false. 미완료 검사 상태는 위에 별도 명시했다. 빌드·실행파일 실행·push는 하지 않았다.
