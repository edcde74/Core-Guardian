# 마스크 공통 재질 참조

2026-10-21. 사용자: 프로젝트의 공통 리소스 참조를 먼저 적용. 범위는 기존 마스크 생성/윤곽/접촉 그림자 재질 공유이며 본체 마스크 RT 공유·HUD·Session·전체 자산 재설계는 포함하지 않는다. 기준 `b77b0df`.

설계: 3종 immutable 런타임 재질을 참조 수로 관리한다. 개체별 RT/색/두께/접촉 밴드는 renderer PropertyBlock에 분리한다. 마스크 생성용 CommandBuffer는 기존 PropertyBlock을 유지한다. 마지막 소유자 해제 시 공유 재질도 해제하며 사용자 자산 재질은 수정하지 않는다. RT/메시의 생성·표시 순서·애니메이션·그림자 수/품질/사운드는 유지한다.

검증 순서: 같은128적/15포탑 준비30/측정100×3 전후 캡처, 소유 재질 수/엔진 메모리, 기존 단순 GPU 픽셀 계약과 역할별 효과 픽셀 비교, 대상별 값 분리/공유 수명 RED→GREEN, 반복 초기화의 현재 소유 RT/공유 재질 수. 새로운 패키지/빌드/실행파일/push 없음. 공식 Unity CLI를 순차 사용한다.

전 캡처의 소유 재질은 mask274/outline274/contact129=677, 1,829,568bytes다. 전역 잔류/자산과 섞지 않고 live owner 필드의 고유 참조만 센다. 기존 사용자 설정2파일·미추적·폰트 도구 접근 표시는 보존한다.

주의: Unity 공식 [PropertyBlock 문서](https://docs.unity3d.com/kr/6000.0/Manual/DrawCallBatching-Properties.html)는 SRP Batcher 호환성 상실을 설명한다. 현재 이 3개 커스텀 셰이더는 UnityPerMaterial CBUFFER 구조를 사용하지 않으며 마스크 명령도 기존부터 PropertyBlock을 사용했다. 그래도 공유가 호출/FPS 개선이라고 가정하지 않고 같은 조건으로 확인한다. [기존 배칭 근거](https://docs.unity3d.com/jp/current/Manual/DrawCallBatching-SetUp.html). 새로운 일괄 SRP/인스턴싱 설정은 하지 않는다.

현재 결과/첫 구현 악화/최종 보완/실패·한계·종료 상태는 [결과 정본](RESULTS.md)을 따른다. 최종 소유 재질677→3/1,829,568→8,032bytes, 평균FPS28.123→30.083(+6.970%)를 관측했다. Draw Calls/SetPass 감소는 없으며 계약17/17·효과 픽셀3,456건 차이0이다. 프로젝트 전체 리소스 공유/전체 성능 통과가 아니다.
