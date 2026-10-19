코어 가디언 사운드 작업을 이어서 진행해줘.
경로: C:\Users\edcde\OneDrive\Desktop\CD

먼저 AGENTS.md → docs/development/UNITY_HANDOVER.md → LOCAL_GIT.md → audio_B_applied_20261019/README.md → audio_selection_final_20261019/selection_manifest.json 순서로 읽고 실제 Git HEAD/변경 상태를 확인해.

2026-10-19 후속 사용자 지시로 B 사운드를 모두 연결하고 종류별 음량을 먼저 분리했다.
선택23·잠정2·멀티정책1의 정본은 그대로이며 멀티1·2·3발로 런타임 WAV28개다. 해시/경로는 실제 적용 폴더 runtime_mapping.json과 선택 manifest를 따라.
전체 기존키 CoreGuardian.SoundVolume에 Music/Tower/Player/MonsterCore/UI/Progress6분류 배율을 곱한다. API와 데이터 catalog는 실제 적용 README가 담당한다. 새 설정 UI는 아직 만들지 않았으며 기존 개발 도구는 전체 음량/확정음 듣기를 유지한다.
실제 전체 저장음량0.0681817755는 이번 시작/종료 조회값이며 보존했다. 분류6개는 기본1·저장키 없음으로 복구했다. 과거13%/기본65%로 초기화하지 마.

골드·플래티넘5차①, 실버2차③, 전투BGM B72①, 증강선택BGM2차③ 유지.
철거3차②·라운드시작2차③도 연결했지만 음색의 잠정 상태는 유지한다. 전체 후보 재선택/새 시안으로 돌아가지 마.
멀티는 한 공격의 실제1·2·3발 수로 대응 WAV를1회 재생하고 기존85ms 리듬을 유지한다. 타깃마다 전체3연타 중복 금지. 지원 전투는 계속 무음.
증강 공개음3등급과 음악 전환/복귀·.35초 크로스페이드/결과종료·중요음 덕킹을 연결했다. 최종 승리는 마지막 라운드 전체 승리이며 일반 완료와 별도다. 결과음이 후속 코어 피해에 잘리지 않도록 우선순위를 둔다.

최종 전체Edit149/149·사운드Play19/19(기존12+신규7) 통과. 0건 반환은 통과로 세지 않았고 실제 재컴파일 후 실행 결과가 최종이다.
기능QA는 진단/재화·코어·HP주입/강제피해/수동Tick·카드 생성 등을 포함한다. 정상 다수전투 믹스/성능·사람 직접 청취/플레이·전체Play회귀·밸런스 통과가 아니다.
종료Playfalse/pausedfalse/컴파일실패false·전투씬1dirtyfalse·TimeScale1/captureDelta0·현재오류0/경고1.

기존14개 리소스·BGM 제작자료·브라우저 선택/메모·아트/그림자/HUD·PackageManagerSettings 변경·미추적자료/font_tools_vendor 접근오류 삭제표시 보존.
이번 연결에서는 Notion을 수정하지 않았다. audio_handover_sync_20261019.md/기존사운드2페이지 동기화는 청취선택 단계 이력이다.
Unity 변경 전 연결·씬·Play·컴파일·저장 상태를 재조회하고 사용자 Play/다른 채팅 검사를 중단하거나 겹치지 마. 전투씬 하나 유지·씬 YAML 직접수정 금지.
밸런스측정/수치변경·빌드·실행파일실행·GitHub push 금지.
다음은 사용자 정상 전투 청취 피드백과 향후 설정 UI 연결이며 구체 후속 지시에 따라 진행해.
