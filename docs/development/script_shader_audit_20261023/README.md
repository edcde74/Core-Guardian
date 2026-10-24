# C# / 셰이더 통합 조사

2026-10-23, HEAD `1de8ced`의 미커밋 작업 트리. 사용자 요청: 기존 C# 조사에 셰이더 조사를 더하되 분야를 나눠 보고. 이번 작업은 정적 소스/참조 조사와 이 보고서 작성뿐이다. Unity 연결·Play·Profiler·컴파일·코드/씬/설정 변경·빌드·실행파일·커밋·push 없음.

후속 승인 실험: [윤곽 샘플링 후보 실제 결과](../outline_sampling_20261023/README.md). 픽셀/논리호출수 검증은 통과했지만 일반90프레임이 악화돼 제품 원본 복구·후보 격리 보류했다. 이 정적 조사 시점과 후속 실행을 구분한다.

|분야|범위|결과 정본|
|---|---|---|
|C#|108개: runtime54 / Editor4 / tests50. 기존 조사 hash와 모두 동일해 기존 분석 재사용|[C# 보고](CSHARP_REPORT.md), [기존13후보 상세](../damage_number_prewarm_20261023/SCRIPT_AUDIT.md)|
|셰이더|관련38개: shader29/hlsl1/cginc4/Graph4. 커스텀11개+TMP27개|[셰이더 보고](SHADER_REPORT.md), [전체 파일 색인](shader_inventory.csv)|

**서로 다른 문제:** C#의 반복 탐색·할당·좌표/마스크 준비는 CPU/GC, shader의 픽셀별 주변 조회·겹침은 GPU 후보, renderer/재질/텍스처/투명 정렬의 결합은 draw 제출 문제다. 기존 마스크 CPU4.672ms를 shader GPU4.672ms로 바꾸어 쓰지 않는다.

**핵심 결과:** 활성 윤곽 셰이더는 폭1 기준 본체 밖에서 소스상 최대10회 mask 조회, 포탑 발광5회, 코어 발광9회다. 실제 피해 숫자는 Mobile SDF 단일 Pass/OUTLINE_ON/atlas1회 경로다. 보류된 BatchedMaskOutput/AttackBodyBatch와 제거된 contact를 현재 병목으로 재제시하지 않는다. 커스텀11개 자체는 전부 explicit Pass1개다.

**다음 제안:** CPU는 뷰/타깃 검증·World 변환/마스크 준비 중복, 셰이더는 윤곽 판정의 같은 결과 중복 조회를 각각 한 단위로 검증할 후보로 둔다. 셰이더 실제 GPUms/절감량은 미측정. 사용자 승인 없이 후보 수정·검사로 확대하지 않는다.

[조사 범위/기존 C#과 대조](scope.json), [정적 조사 코드](inventory.py), [38파일 hash/경로/참조](shader_inventory.json), [보호 파일 시작 hash](protected_before.json), [최종 보존/링크 검사](verification.json). 이번에 Unity 상태를 조회하지 않았으므로 이전 종료 기록을 현재 상태로 단정하지 않는다. A 보류/B 제한·원본 출력·아틀라스 보류·native batch OFF 정책 유지.
