# 독립 코드 검토

2026-10-21. requesting-code-review 절차에 따라 읽기 전용 검토자를 사용했다. 기준 `b77b0df`, 대상은 SelectionOutline/PhotoShadow의 공통 재질·개체별 PropertyBlock·참조 수명과 새 공유 계약이다. Unity/Play/검사/편집은 검토자가 수행하지 않았다.

초기 구현과 최종 중간 PropertyBlock 전달 제거를 각각 검토했으며 must-fix 없음. null 재질의 기존 outline fallback, early return의 output 비활성화, 개체별 값 초기화/분리, 생존 소유자와 마지막 소유자의 해제 흐름을 정적으로 확인했다. 성능/픽셀/Editor domain reload 검증을 대신한 결과는 아니다. 실제 엔진 결과는 RESULTS.md를 따른다.
