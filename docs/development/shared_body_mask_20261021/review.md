# 독립 리뷰

Native 구현자와 별도의 읽기 전용 body_mask_final_review가47ca358→d5771b8 및 후속 diff를 검토했다. 리뷰어는 Unity/검사/Play/파일 수정·빌드·push를 하지 않았다.

- P2: fractional viewport 상단/오른쪽 경계에서 ceil 요청 크기와 실제 crop 크기가 달라 부정확한 공유를 허용했다. fractional_red_tests에서1/1 실패를 확인하고 정수 viewport gate를 추가했다. 리뷰어가 해소를 재확인했다. 최종에는 exact crop 조건도 적용했다.
- 추가 좌표 픽셀 실패 뒤 exact crop/접촉 전용 생산자/UV·밴드/수명 변경을 다시 검토했다. 정적 코드에서 새로운 refcount·epoch·불변 세대·수명 결함은 발견하지 않았다. 픽셀 동등성은 실행 검사로 별도 확인하도록 했다.
- 회전·custom projection·폭>2는 원래 증거 밖이라는 지적을 받아 identity 회전·기본 직교 투영·폭2/viewport origin0 gate를 적용했다.
- 최종51건·native1,024/fallback3,456건, 성능·수명 원본은 RESULTS가 담당한다. 리뷰는 실행 결과나 사람 아트 승인을 대체하지 않는다.
- 종료 점검의 EditMode 정리 후속 diff도 독립 검토 대상으로 전달했다. 기원/소유권 불명 native6개는 삭제하지 않았다.

마지막 Editor 정리 diff도 검토 완료: 재진입은 presentation release만 수행하고 binding/lease 반환·collection clear·commands=null로 중복 정리가 안전하며 구체적 결함을 찾지 못했다. 미확정 native6개는 전역 정리 성공으로 해석하지 않는다.
