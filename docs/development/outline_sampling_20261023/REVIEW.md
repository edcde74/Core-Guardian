# 독립 검토와 보완

executing-plans/requesting-code-review 절차의 별도 읽기 전용 `/root/outline_review`가 승인 계획·제품 shader diff·원본 fixture·테스트/결과를 검토했다. Unity 조작/파일 수정은 하지 않았다.

- 제품 shader의 width0~8/Point·Clamp/mip없음 계약 내 correctness 결함은 찾지 못했다.
- P2: 반복 float fixture에 항상1이 있어 작은 neighbor threshold 처리가 가려지는 검증 공백을 지적했다.
- 보완: 주변을0으로 두고0/.499/.4999/.5/.5001/1 각각의 단일 값만 중앙 및 clamped corner에 놓아 폭0~8을 직접 원본과 렌더 비교했다. 최종 후보5/5통과.
- 재검토: 해당 공백 해결, candidate fixture로 테스트대상 전환과 제품 원본 복구 설명 확인, 추가 지적 없음. 재검토 자체는 테스트를 실행하지 않았으며 실제 실행은 root의 held_final_green.json이 담당한다.
- GPU 계측판 논리 호출수와 하드웨어 샘플/성능은 다르다는 한계를 유지했다. 일반90프레임 악화로 원본 복구·후보 격리 결정.
