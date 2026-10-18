코어 가디언 프로젝트의 사운드 작업을 이어서 진행해줘.
경로: C:\Users\edcde\OneDrive\Desktop\CD
다음 작업은 계속 사운드다. 병행 접지감 대화의 후속과 혼동하지 마.

■ 최신 추가: 코어 피해음⑥ 적용 완료(2026-10-18)
- 사용자 ‘6번으로 진행해’로 Aura_Glyph_2 원본을 코어 에너지 감소에 연결했다. 공격음3종/지원무음 유지.
- Assets/Resources/CoreGuardian/Audio/CoreDamage/06_Aura_Glyph_2.wav. 원본 동일바이트1.981초,0.3초 연속 묶음/4음성 내 코어 알림 우선/gain0.55/공통음량.
- 현재 사용자 저장값0.0681817755(약6.8%) 시작/종료 동일. 아래13%는 이전 이력이며 기본65%/과거13%로 되돌리지 않는다.
- 새Edit3/3·Play2/2는 진단경로·적위치/수동Tick·실제Prototype.Update 집중QA다. 정상다수전투믹스/성능/전체회귀/사람플레이·밸런스 검증 아님.
- 종료Playfalse/pausedfalse/compilingfalse·전투씬1dirtyfalse·TimeScale1/captureDelta0·컴파일실패false. 상세 docs/development/core_damage_audio_applied_20261018/README.md.
- 다음 구체 사운드는 미정. 아래 ‘코어 피해음 선택대기’와 ‘이번 새 검사 없음’은 이 추가 적용 이전 기록이다.

먼저 다음 순서로 읽고 실제 Git HEAD와 변경 상태를 확인해.
1. AGENTS.md
2. docs/development/UNITY_HANDOVER.md
3. docs/development/LOCAL_GIT.md
4. docs/development/resume_audio_latest_20261018.md
5. docs/development/combat_audio_final_20261018/README.md
현재 사용자 지시와 최신 담당 기획이 우선이다. 과거 인수인계를 현재 상태로 가정하지 마.

■ 확정·실제 적용된 공격음
- 사운드 적용 커밋: e38e595 (Apply approved multi artillery B and finalize attack audio controls).
- 단일 포탑: 사용자 선택③ Aura_Spawn_1 파생.
  Assets/Resources/CoreGuardian/Audio/TowerShot/03_Aura.wav
- 멀티 포탑: 포격형② 세 포신 연속 발사의 선명도 B안. 최종 선택·전체 적용 완료.
  Assets/Resources/CoreGuardian/Audio/MultiShot/B_BrighterAttack.wav
- 플레이어 공격: 사용자 선택③ Aura_Glyph_1 파생.
  Assets/Resources/CoreGuardian/Audio/PlayerShot/03_Glyph.wav
- 지원 포탑: 사용자 지시로 무음. ‘전체 적용’을 지원 효과음 추가 승인으로 해석하지 마.
- 세 소리는 미리듣기만이 아니라 실제 Session.ProjectileLaunched에 연결돼 있다.
- 멀티 B 파일에0/85/170ms 세 포신 리듬이 있다. 같은 포탑의 타깃별3이벤트는35ms 내 한 번으로 묶고 서로 다른 멀티는 각각 재생한다. 게임 발사 수/피해/속도는 바꾸지 않았다.
- 기존4음성 풀/gain0.55/비루프/고정피치1 유지. 밀집 재생 제한은 소리만 생략한다.

■ 개발 도구와 사용자 설정
- 개발 도구에 단일③·멀티B·플레이어③ 고정 ‘듣기’와 효과음0~100% 슬라이더가 있다.
- 마지막 확인 저장 음량0.126900658(표시13%). 기본65%로 초기화하지 말고 실제 현재 저장값을 보존해.
- 기존 증강 공개 효과음도 공통 효과음 음량을 사용한다.
- 옛 후보 전환/Resources 후보는 제거했다. 과거 WAV/시안/Git과 팩 원본은 보존했다. 옛 후보 번호 저장값으로 확정음을 바꾸지 마.

■ 사운드 방향과 원본
- 무료만 사용한다. 실총 느낌·과한 전자음/레이저음·질감/타격감 부족은 사용자 거부 사항이다.
- 거부된 합성안·대지/화염·멀티 속성음으로 되돌리지 마. 현재 승인된 마력포/고대 장치 방향이 기준이다.
- 후보를 들려주고 사용자 선택 후 실제 적용한다. 에이전트가 직접 듣지 못한 것을 청취/음질 승인이라고 하지 마.
- 이미 임포트한 원본팩:
  Assets/Leohpaz/RPG_Essentials_Free (48음)
  Assets/AS_Magic_Aura_Construction_Kit_FREE (7음)
- 원본55 WAV 보존/팩 체크포인트1523eb7. 동봉 데모씬은 Assets 밖 보관, 전투씬 하나 유지.
- 멀티 B 제작/비교본: AudioProduction/CoreGuardian_TowerShot_20261018/multi_artillery_bright/.
  사용자 승인 B 파일과 동일 바이트로 실제 적용했다.

■ 검증 범위와 다음 작업
- 적용 당시 실제 러너 Edit3/3·Play1/1 통과. 진단보드/재화/적HP 주입·수동 Session.Tick 집중 기능 QA다.
- 단일1+멀티2대 각1음성/첫멀티3투사체·실제 플레이어 발사·지원 무음·중복 억제·음소거·슬라이더·실제 Overlay·코드 리뷰 확인.
- 적용 종료 당시 Playfalse/pausedfalse/compilingfalse·전투씬1dirtyfalse·TimeScale1/captureDelta0·컴파일실패false·현재오류0/경고0.
- 정상 다수 전투의 합산 음량/클리핑/성능·전체 회귀·사람 플레이·밸런스 통과가 아니다.
- 이번 재개 문서 정리에서는 새 Unity 조회/Play/검사를 하지 않았다. 위 상태는 적용 당시 이력이다. 다음 Unity 변경 전에 재조회해.
- 다음은 계속 사운드다. 구체 후속 효과음/우선순위는 아직 미정이다. 확정 공격음을 보존하고 남은 사운드 항목부터 확인해 이어가자.

■ 병행 작업과 보호 사항
- 병행 접지감 재개 문서는 resume_grounding_latest_20261018.md다. 사진에서 추출한 그림자 시안은 사운드 대화에서 게임 적용 승인으로 취급하지 마. 관련49fc776/eb8d513/705a505 커밋과 후속 변경을 보존해.
- 자연석 맵39×22/0.69·길목1안·원본② 무너진 땅굴·A내부+B외곽 혼합 장식 유지.
- 색보정③·개체강조B②·피격 밝기 복구·HUD A/B·코어 입자 사용자값·포탑 조준/반동/발광/접지·보행/체력바/피해 숫자 보존.
- 맵 상세는 기존 담당 문서와 resume_map_latest_20261018.md 참고. 그 문서의 과거 ‘다음 작업 미정’은 현재 사운드 작업 지시가 아니다.
- Unity 변경 전 연결·씬·Play·컴파일·저장 상태 재조회. 사용자 Play/다른 채팅 검사를 중단하거나 겹치지 마.
- 전투씬 Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity 하나만 유지. 씬 YAML 직접 수정 금지.
- 밸런스는 사용자가 별도 시간을 정한다. 지금 측정/수치 수정하지 마.
- AudioProduction/BGM/밸런스·다른 채팅 변경/미추적 자료 보존. font_tools_vendor 접근 오류의 삭제 표시를 커밋하지 마.
- 빌드·실행파일 실행·GitHub push 금지. 담당 문서 역할을 지키고 상세 규칙을 중복하지 마.
