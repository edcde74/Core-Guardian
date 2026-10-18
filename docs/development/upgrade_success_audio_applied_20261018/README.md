# 강화 성공음 2차① 적용

2026-10-18. 사용자 ‘1번’은 최신 Kenney 후보① `confirmation_001.ogg` 선택이다. 거부된 최초 Attack buff와 구분한다. 시작 HEAD8b63d92, 관련 변경 없어 빈 복구 커밋 없이 기준으로 사용했다. 기존 PackageManagerSettings 변경과 병행 자료는 보존한다.

## 구현

- 공식 CC0 팩 원본 `AudioProduction/CoreGuardian_Upgrade_20261018/Kenney_Interface/Originals/Audio/confirmation_001.ogg`를 에디터API에서 복사/임포트해 `Assets/Resources/CoreGuardian/Audio/UpgradeSuccess/01_Confirmation.ogg`로 연결했다.
- 원본/복사본 SHA256 `063564703B6094D70718A3E787A55CC9141611E4ECD6B6637F8828F79B4A8C3A` 동일. PCM/DecompressOnLoad/preload 임포트,44100Hz/0.289841초/읽은샘플 peak0.898499. 원본 음색/길이/피치 편집 없음.
- Prototype.UpgradePlayer/UpgradeTower의 Session 구매 성공 분기에서 기존 성공스캔 직후 한 번 호출한다. 실패/단순조회/설치/증강카드공개에는 연결하지 않았다. 지원 포탑 강화 성공도 공통 구매 알림이며 지원 전투음 추가가 아니다.
- 기존4음성 풀/gain0.55/공통음량,포화시코어음이아닌음성교체/전부코어음이면생략. 기존 사운드와 성공스캔B0.8초·가격/능력치/구매조건을 보존했다.
- 라이선스: [팩 취득·CC0·공식출처](../../../AudioProduction/CoreGuardian_Upgrade_20261018/Kenney_Interface/README.md), 원본 `Originals/License.txt`의 개인/교육/상업 사용 허용과 출처표기 비필수 확인.

## 이번 확인

- 새 검사에서 Selected upgrade success audio is missing 실패를 확인 후 구현했다.
- 실제 러너 **Edit3/3·Play8/8 통과**: [Edit](editmode.json), [Play](playmode.json).
- 새 검사는 실제 전투씬 컴포넌트·진단 재화/설치/Gold0주입과 Prototype.UpgradePlayer/UpgradeTower 직접호출 기능QA다. 사람 직접 입력/정상 경제 진행/밸런스 실측은 아니다.
- 플레이어 성공시Gold차감/능력치증가·단일/멀티/지원구매성공당1음성/1스캔·스캔0.8초유지·잘못된항목/재화부족/포탑전투중구매실패무음·음소거에서도성공스캔유지·풀포화시코어음보호·음량반영을 확인했다. 기존 사운드7개 집중검사도 통과했다.
- 종료 Playfalse/pausedfalse/compilingfalse·컴파일실패false·전투씬1dirtyfalse·TimeScale1/captureDelta0·저장음량0.0681817755(약6.8%)시작/종료동일: [최종조회](final_state.json). 현재콘솔오류0/경고0와누적로그는구분한다: [콘솔](console.json).
- 자체Play/임시음량복원·씬YAML수정/빌드/실행파일/push없음. BGM/기존원본/병행자료·font_tools_vendor오류삭제표시보존.

음색선택·구매성공연결 기능QA 완료이며 정상다수전투믹스/반복청취/성능/전체회귀·밸런스통과가 아니다. 다음 구체 사운드는 미정이다.
