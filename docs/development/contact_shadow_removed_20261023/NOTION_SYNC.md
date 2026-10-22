# Notion 동기화 · 2026-10-23

사용자 요청으로 저장된 구현·검증 결과를 기존 담당7페이지에 부분 수정했다. 새 Unity 조작/검사/빌드/실행파일/push 없음. 인수인계 시작 HEAD7483ff0, 게임 구현0fa0f47, 성능 기록6f8cc8d.

- [대량 전투 성능·최적화 계획](https://www.notion.so/3ff389dbfc6881779abdeb5eeeb1cda4): 길/설치 메시·접촉 제거 채택, 저사양60FPS 목표와 미확정 예산, A/B 제한, 상세 검사 이력 연결.
- [플레이테스트 및 변경 이력](https://www.notion.so/3df389dbfc6881fc9f1bd004adfc5999): 세 단계 구현/검증·최저/최고/평균FPS·CPU/Draw/SetPass·원본·단일 표본 한계·복구 기록.
- [프로젝트 메인](https://www.notion.so/3df389dbfc6881bc858bce200d1bb742): 최신 구현/후속 상태 요약과 담당 문서 연결.
- [후속 제작·검증 작업 순서](https://www.notion.so/3f4389dbfc688171b84dc19b138925f4): 현재 구현과 목표 갱신, 승인 제거 유지·중복 검사 방지, 정상30R/장시간/GPU/잔류 검증 미완료 유지.
- [아트 입체감·바닥 밀착감](https://www.notion.so/3f3389dbfc6881089b13e5daf7b766a2): 동적 접촉 제거를 현재 표현 정본에 반영, 넓은 투사 그림자·발 중심 보존, 제거한 접촉 복구 지시 충돌 해소.
- [셰이더·화면 후처리 기준](https://www.notion.so/3fb389dbfc68813ab657f4600eeae76f): 접촉 생성/갱신 제거와 다른 마스크/공통 API·리소스 보존 구분.
- [전투 프로토타입 추가 아트 제작 목록](https://www.notion.so/3f3389dbfc6881d2a70be916ded8ddb7): 최신 그림자 승인 범위와 담당 정본 연결.

모든 비동기 수정 succeeded 및 재조회 확인. 메인 첫 수정은 긴 native mention 포함 검색문 no_match 경고가 있어 해당 부분은 적용되지 않았다. 그 부분만 기존 정확한 짧은 문장으로 다시 수정하고 재조회에서0fa0f47·40.35→45.00 확인했다. 성공한 다른 수정은 반복하지 않았다. 기존 child page/database URL 집합 보존 확인(메인 하위7페이지+DB1), 전체 페이지 replace/delete 없음. 날짜/mention 구문은 서버 정규화됨. 페이지 상태를 전체 완료로 변경하지 않았다.
