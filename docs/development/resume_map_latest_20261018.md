이 문서는 맵·환경 장식 작업의 배경 기록이다. 다음 대화의 최신 재개 지시는 `docs/development/resume_grounding_latest_20261018.md`를 따른다. 아래의 과거 작업 시점/미정 문구를 현재 우선순위로 해석하지 않는다.

코어 가디언 프로젝트를 이어서 진행해줘.
경로: C:\Users\edcde\OneDrive\Desktop\CD

먼저 다음 순서로 읽고 실제 Git HEAD와 변경 상태를 확인해.
1. AGENTS.md
2. docs/development/UNITY_HANDOVER.md
3. docs/development/LOCAL_GIT.md
4. docs/development/resume_map_latest_20261018.md
현재 사용자 지시와 최신 담당 Notion이 우선이며 과거 인수인계를 현재 상태로 가정하지 마.

■ 최신 적용·사용자 피드백
- 환경 장식 적용 커밋 1f224d8: A의 깔끔한 내부+B의 외곽 일부 혼합 환경 장식 실제 적용. 현재 HEAD라는 뜻은 아니다.
- 사용자는 적용 화면에 “굿 좋은데 이제 좀 게임이 풍부해졌네”라고 만족을 확인했다.
- 이후 사용자가 다음 작업을 접지감 다듬기 계속으로 지정했다. 최신 접지감 인수인계를 따른다. 승인된 장식을 다시 미선택/미적용으로 취급하지 마.
- 이번 후속은 문서·Notion 최신화이며 새 Unity 조회/Play/검사·게임 변경은 없었다.
- 밸런스는 사용자가 별도 시간을 정한다. 지금 측정하거나 수치를 바꾸지 마.

■ 현재 맵과 장식
- 자연석1안 NaturalCourtyard_01_ClearBorder.png의 불규칙한 돌판과 메인/외곽 연결 유지.
- 39×22칸, 실제 한 칸0.69, 카메라 위치(13.5,7.5,-10)/orthographic8.5 유지.
- 경로9점/80칸, 코어(25,8), 플레이어 시작(13,9). 회색 판석·낮은 경계석 길목1안.
- 입구는 출력 순서②의 무너진 땅굴. 사용자가 지정한 원본 형태·판석 접합부·깊은 그림자를 유지한다. 성문/다른 땅굴로 재생성하지 마.
- 입구와 새 장식은 정적 시각 요소다. 출현/경로/충돌/이동/설치 규칙이나 몬스터 페이드·가림 마스크를 변경한 것이 아니다.
- 새 장식: 내부는 희소 잡초·자갈·납작한 파편, 외곽은 성벽/모서리에 풀·석재 잔해 일부. C안의 새 황동·수정 장식은 미적용.
- 시안 전체로 배경을 덮지 않고 별도 투명 파생 리소스/정적 렌더러5개를 추가했다. 개별 풀/돌별 GameObject는 아니며 외곽4영역/내부1영역으로 나뉜다.
- 계층: CoreGuardian_MapArt_B/EnvironmentDecoration_Hybrid_Selected.
- 같은 Sorting Layer Default의 뒤→앞 순서:
  기존 자연석 바닥+외곽 배경 -20 → 추가 외곽 -10 → 낮은 프랍 -5
  → 길 판석1/경계석2 → 땅굴4 → 기존 그림자·주요 본체 높이 정렬.
- 과거45×25/0.6·초기 반복 바닥·구형 황동 길로 되돌리지 마.
- 실제 적용·구조·검증: docs/development/environment_decoration_applied_20261018/README.md
- 실제 화면: 위 폴더의 applied_edit.png / applied_combat.png / applied_installation.png.
  생성 시안 자체와 실제 Unity 화면을 구분하고 승인 원본 픽셀 동일성을 과장하지 마.

■ 보존할 시각 설정
- 카메라 색보정③ 따뜻한 고대 설비: 대비26/채도7/노출-0.20EV.
  SplitToning 그림자RGB(.46,.49,.54)/밝은색RGB(.54,.515,.48)/balance5.
  ColorAdjustments+SplitToning만, post=true/HDR=false. Bloom/블러/비네트 추가 없음.
- 배경 대비 완화 가독성 A는 사용자 거부로 원복했다. 재적용 금지. 환경 장식 A안과 혼동하지 마.
- 상시 개체 강조 B② 적용: CoreGuardian_Gameplay/CoreGuardianObjectEmphasis.
  화면 두께1px, RGBA(.06,.065,.075,.90), 본체 밝기1.15, alpha threshold.1.
  두께·색·밝기는 Inspector에서 조절. 그림자/체력바/별도 발광 제외.
- 기존 초록2px 선택선과 별도이며 본체 합집합만 강조한다.
- 피격 종료 밝기 변동은 cb60b4d에서 수정했다.
  원래 속성블록이 없으면 null, 있었으면 저장 블록으로 복구한다.
  피격0.09초/최대22%/재점등 제한0.1초 유지.
- HUD 능력치A·손잡이/포탑심볼B, 강화 스캔0.8초,
  코어 입자 사용자 저장값0.025/0.1/초당4/최대12,
  포탑 발광 단일1/멀티3/지원1·조준/반동,
  보행·그림자·체력바·피해 숫자와 원본 아트를 보존한다.
- 상세: object_emphasis_B2_applied_20261018/README.md,
  hit_brightness_check_20261018/README.md,
  postprocess_map_options_20261018/applied_warm.md (모두 docs/development 아래).

■ Notion 최신화 완료
- 환경 아트: https://www.notion.so/3fc389dbfc6881e5abb7c9686d1cd893
  혼합 장식 적용·레이어·사용자 만족·집중 QA 범위 반영.
- 셰이더/후처리: https://www.notion.so/3fb389dbfc68813ab657f4600eeae76f
  현재 색보정③·상시 B②·피격 복원 후속 반영.
- 기존 전투 화면 아트 방향과 VFX 시안·선택·검증 기록도 갱신하고4문서 재조회.
- 실제 보드/경로 수치는 기존 프로토타입 레벨 디자인 담당:
  https://www.notion.so/3e9389dbfc688159ae8cec214b4e3e25
- 문서 역할을 지키고 상세 규칙은 담당 문서로 연결한다. 같은 기획서/DB를 중복 생성하지 마.
- 새 이미지 업로드는 없으며 기존 Notion 그림은 장식 적용 전 이력으로 표시했다.

■ 확인 범위와 제한
- 장식 적용 당시 원본564컴포넌트·6맵939타일·원본61리소스해시 동일, 새Collider/Behaviour0.
- 자체 Play 정상API4방향 이동/설치858(778초록·80빨강)/길 설치 거부/정상 멀티 구매/자연 몬스터4·Overlay 확인.
- 종료 당시 Playfalse/pausedfalse·전투씬1/dirtyfalse·컴파일실패false·현재오류0/경고0·TimeScale1/captureDelta0.
  이는 적용 종료 당시 상태이며 다음 변경 전 다시 조회해야 한다.
- 집중 기능 QA다. 사람 직접 플레이·전체 회귀·밸런스·다수 전투 성능 통과가 아니다.
- B② 당시 기존 CGPlay21/27의6실패는 꺼짐 수동 진단에서도 재현했다. 전체 통과로 쓰지 마.
- 새 문서/Notion 정리를 새 Unity 검사로 표현하지 마.

■ Unity·Git 보호
- Unity 변경 전 연결·열린 씬·Play·컴파일·저장 상태 재조회.
- 사용자 Play/다른 채팅 검사를 중단하거나 겹쳐 실행하지 마.
- 전투 씬 Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity 하나만 유지.
  씬 YAML 직접 수정 금지, Unity CLI/Editor API 사용. 비교/복구 씬 재생성 금지.
- 빌드·실행파일 실행·GitHub push 금지.
- AudioProduction/밸런스/BGM·다른 채팅 변경/미추적 자료 보존.
- font_tools_vendor 접근 오류의 삭제 표시를 커밋하지 마.
- 다음 작업 전 현재 HEAD·변경을 확인하고 관련 파일만 로컬 커밋한다.
