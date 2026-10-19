코어 가디언 사운드 작업을 이어서 진행해줘.
경로: C:\Users\edcde\OneDrive\Desktop\CD

먼저 아래 순서로 읽고 실제 Git HEAD와 변경 상태를 확인해.
1. AGENTS.md
2. docs/development/UNITY_HANDOVER.md
3. docs/development/LOCAL_GIT.md
4. docs/development/audio_selection_final_20261019/README.md
5. docs/development/audio_selection_final_20261019/selection_manifest.json
6. docs/development/audio_handover_sync_20261019.md

최신 상태는 B 사운드 청취 선택 및 Notion/재개 문서 동기화 완료다.
청취 선택23개·잠정2개·멀티 정책1개이며 정본 경로/해시는 manifest를 따라.
골드·플래티넘은 모두 5차① ‘금빛 당첨·반짝이는 확정’으로 사용자 선택 완료.
실버2차③, 전투BGM B72①, 증강 선택BGM2차③도 유지한다.
다시 전체 후보 선택을 받거나 새 시안을 자동 제작하지 마.
철거는3차② 마력 회수 잠정 선택이며 해제 연출에 따라 재검토.
라운드 시작은2차③ 잠정 선호를 유지하며 준비→전투 전환 알림이다.
멀티는B72② 음색을 바탕으로 실제 발사 수1·2·3발 구분 방향 승인.
같은 공격의 타깃별 이벤트마다 전체3연타를 중복 재생하지 말고 기존85ms 리듬을 유지해.

이번 사용자 요청은 선택 확정 내용의 문서/Notion 갱신과 인수인계였고 게임 적용 요청은 별도다.
새 B 선택음은 아직 게임 미연결. 현재 게임의 기존14개와 지원 포탑 전투 무음을 보존해.
다음은 후속 사용자 지시에 따라 실제 연결 범위를 정하는 단계다.
증강 공개/선택 음악 전환 방향은 선택했지만 실제 전환/복귀·믹스 구현은 남아 있다.
최종 승리는 마지막 라운드 클리어로 게임 전체가 승리 종료되는 순간이며 일반 완료와 별도다.

기존 Notion ‘전투 사운드 설계 방향’과 ‘07. 사운드’를 갱신·재조회했고 새 페이지/DB/음원업로드는 없다.
이번 정리에서는 새 Unity 조회/Play/검사·게임/음량 변경을 하지 않았다.
마지막 사운드 집중 Edit3/3·Play12/12는 진단/강제피해·수동Tick 기능QA다.
정상 다수전투 믹스·성능·전체회귀·밸런스 통과가 아니다.
현재 실제 저장음량을 보존하고 기본65%/과거13%로 초기화하지 마.
약6.8%는 마지막 이전 Unity 조회값이며 이번 새 조회값이 아니다.

Unity 변경 전 연결·씬·Play·컴파일·저장 상태를 재조회해.
사용자 Play/다른 채팅 검사를 중단하거나 겹치지 마.
전투씬 하나 유지·씬 YAML 직접수정 금지.
병행 그림자/아트/HUD·BGM원본·PackageManagerSettings 변경·미추적 자료·font_tools_vendor 오류 삭제표시 보존.
밸런스측정/수치변경·빌드·실행파일실행·GitHub push 금지.

선택 원본 확인용5차 페이지: http://127.0.0.1:8886/
꺼져 있으면 AudioProduction/CoreGuardian_B_Revision5_20261019/Open_Revision5.cmd로 로컬 파일을 열 수 있다.
이전 페이지/브라우저 선택·메모는 보존하고 차수별 번호를 섞지 마.
