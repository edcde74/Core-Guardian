# 강화 실패음 ③ 적용

2026-10-18. 사용자 ‘3’ 선택으로 Kenney Interface Sounds의 `error_006.ogg`를 강화 구매 실패 알림에 연결했다. 시작 HEADb4ce5a6, 관련 변경 없어 빈 복구 커밋 없이 기준으로 사용했다. 기존 PackageManagerSettings 변경은 보존했다.

## 구현 범위

- 공식 CC0 원본 `AudioProduction/CoreGuardian_Upgrade_20261018/Kenney_Interface/Originals/Audio/error_006.ogg`를 에디터API로 복사/임포트해 `Assets/Resources/CoreGuardian/Audio/UpgradeFailure/03_Error006.ogg`에 연결했다.
- SHA256 원본/복사본 모두 `A97513D7FD2B12B1210C423BE943B4C1AB0E899B11DB95855E9B74836E646AE0`.0.500045초·44100Hz·PCM/DecompressOnLoad/preload·읽은샘플peak0.914246·무편집/비루프/고정피치1.
- Prototype.UpgradePlayer/UpgradeTower의 Session 구매 실패 분기에만 연결했다. 재화 부족/잘못된항목/강화불가단계/대상없음 등 기존 실패 판정을 그대로 사용한다. 성공 스캔이나 성공음은 실패 시 실행하지 않는다. 단순조회/설치실패/다른UI에는 확대하지 않았다.
- 실패음 전체는 unscaled0.2초 간격으로 묶는다. 기존4음성 풀/gain0.55/공통음량·풀포화시코어음제외교체/전부코어음이면생략. 성공음/스캔B0.8초/기존전투음/지원전투무음/구매조건/게임수치/BGM 보존.
- 라이선스는 [공식팩/동봉CC0/상업사용안내](../../../AudioProduction/CoreGuardian_Upgrade_20261018/Kenney_Interface/README.md)를 따른다.

## 새 검증

- 신규 검사에서 Selected upgrade failure audio is missing 실패 확인 후 구현했다.
- 실제 러너 **Edit3/3·Play9/9 통과**: [Edit](editmode.json), [Play](playmode.json).
- 새 검사는 전투씬 실제 컴포넌트·진단재화/Gold0/Crystals0주입과 Prototype 강화 API 직접호출 기능QA다. 실제 마우스 입력/비활성 버튼 피드백/사람청취·정상경제진행 검사는 아니다. 비활성 UI에 새 입력 이벤트를 추가하지 않았다.
- 플레이어골드부족/포탑수정부족/잘못된항목/포탑전투중불가의실패음·재화/능력치/스캔불변·성공음별도·0.2초반복제한·공통음소거/음량·풀포화시코어음보호 확인. 기존 사운드8개 집중검사도 통과했다.
- 종료Playfalse/pausedfalse/compilingfalse·컴파일실패false·전투씬1dirtyfalse·TimeScale1/captureDelta0·저장음량0.0681817755(약6.8%)시작/종료동일: [최종조회](final_state.json). 현재오류0/경고0와누적보관로그구분: [콘솔](console.json).
- 자체Play/임시음량복원·씬YAML수정/빌드/실행파일/push없음. 병행자료/원본/BGM/font_tools_vendor접근오류표시보존.

음색선택·강화실패연결 기능QA 완료. 정상다수전투믹스/반복청취/성능/전체회귀·밸런스검증은 아니다. 다음 구체 사운드는 미정이다.
