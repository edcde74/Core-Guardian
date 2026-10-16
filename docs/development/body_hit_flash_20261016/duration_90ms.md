# 몸 피격 반응 시간 소폭 증가

2026-10-16. 사용자가 현재 몸 피격 시간을 조금 늘리도록 요청했다. 복귀 시간을65ms에서90ms로 조정했다. 최대 밝기22%와 재점등 제한100ms는 유지한다. 원본 아트·씬·전투 데이터와 다른 효과는 변경하지 않는다.

수정 전 기준 HEAD는 ad99143이며, 작업 전 게임 코드 변경이 없음을 확인했다. 기존 Notion 분류 완료 인수인계 변경은 보존했다. Unity 연결ready·Play=false·컴파일false·전투 씬dirty=false 확인 후 수정했다. 기존 타이밍 검사를80ms 유지·95~100ms 복귀 기대값으로 조정했으며,65ms 구현에서5건 모두 실패한 뒤90ms로 변경했다.

90ms 적용 후 기존 피격 검사5개를 동기 실행해5/5 통과를 확인했다(`duration_90ms_focused.json`). 80ms까지 표시 유지,95~100ms 복귀,기존 property block·방향별 재질 복구와 재점등 제한을 확인했다. 직전 PlayMode 명중 연결 검사 상태에서1/1 completed도 확인했지만 원본은 따로 저장하지 않아 전체 회귀 결과로 확대하지 않는다.

비동기 status를 읽던 중 전체 EditMode92건 중 공격형 발 기준점 검사1개 실패가 관측됐다(`duration_90ms_editmode.json`). 그 사이 별도 작업으로 공격형 원본PNG·LeftLower PNG·CoreGuardianOriginalWalk.cs의 미커밋 변경이 생긴 것을 확인했다. 해당 파일은 수정·복구·커밋하지 않았다. 이번 시간 조정에 대해 전체92/92 통과를 주장하지 않으며 별도 보행 작업에서 해당 실패를 재확인해야 한다.

최종 로드값은Duration0.09/Strength0.22/RetriggerDelay0.1이며 Play=false·컴파일false·기존 전투 씬dirty=false였다(`duration_90ms_editor_state.json`). 캡처 도구의 복귀 확인 시점은120ms로 늘려 새 시간에서도 복귀 열이 정확하게 나오도록 했다. 기존 비교PNG는 재생성하지 않았다. 타격 담당 Notion의 현재 복귀값을0.09초로 갱신하고 재조회했다. 빌드·실행파일 실행·Push 없음.
