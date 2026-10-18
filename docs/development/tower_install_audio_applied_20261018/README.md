# 포탑 설치 완료음 2차② 적용

2026-10-18. 사용자 ‘2번’ 선택은 최신 설치음2차 후보 `Unequip`이다. 최초 후보②Buy/Sell과 구분한다. 시작 HEADb0b047e, 관련 게임 변경이 없어 빈 복구 커밋 없이 기준으로 사용했다. 기존 PackageManagerSettings 변경은 보존했다.

## 구현

- 원본 `Assets/Leohpaz/RPG_Essentials_Free/10_UI_Menu_SFX/071_Unequip_01.wav`를 AssetDatabase API로 `Assets/Resources/CoreGuardian/Audio/TowerInstall/02_Unequip.wav`에 복사했다. 원본/복사본 SHA256은 `8C01E051FA47D23C5DEEABD7A52F83E172595BC422109A6776BE662F0B596454` 동일.1.333333초·PCM/DecompressOnLoad/preload·무편집/비루프/고정피치1.
- Prototype.Place의 Session.TryInstall 성공/점유등록 후 한 번만 NotifyTowerInstalled를 호출한다. 단일/멀티/지원 공통 설치 성공 알림이다. 지원 전투 무음은 유지하며 지원 공격음 추가가 아니다. 설치 실패/취소/철거/강화에는 연결하지 않았다.
- 기존4음성/gain0.55/공통효과음음량을 사용한다. 풀 포화 시 코어음이 아닌 음성 하나를 교체하고 전부 코어음이면 설치음을 생략한다. 기존 공격/피격/처치/코어/보스 등장·가속과 개발도구, BGM 보존.
- 무료/상업사용 조건은 [동일 팩 공식 조회](../core_damage_audio_auditions_20261018.md)에 있다. 원본/씬/CSV/프로젝트설정/병행 그림자와 미추적자료/font_tools_vendor 접근오류 표시는 보존했다.

## 이번 검증

- 신규 검사에서 Selected tower installation audio is missing 실패를 확인 후 구현했다. 테스트 파일 수정 중 eval 통신 오류1회 뒤 editor ready/stopped·컴파일완료를 재조회했다.
- 실제 러너 **Edit3/3·Play7/7 통과**: [Edit](editmode.json), [Play](playmode.json).
- 새 검사는 실제 전투씬 컴포넌트/진단 재화 Session에서 Prototype.Place/ChooseTower/CancelInstall/DestroySelected 직접호출 기능QA다. 정상 사람 마우스 입력/반복 청취/다수전투 믹스/성능/전체회귀·밸런스 검증은 아니다.
- 지원선설치실패 무음·단일/멀티/지원 실제설치 각1재생·중복칸실패/취소/철거무음·음소거중실제설치성공/무음·풀포화 시 재생과 코어음보호·전투중설치거부무음·공통음량을 확인했다. 기존 사운드6개 집중검사도 통과했다.
- 종료 Playfalse/pausedfalse/compilingfalse·컴파일실패false·전투씬1dirtyfalse·TimeScale1/captureDelta0·음량0.0681817755(약6.8%)시작/종료동일: [최종조회](final_state.json). 현재 오류0/경고0와 누적보관로그는 구분한다: [콘솔](console.json).
- 자체 Play/임시음량 복원. 씬YAML수정/빌드/실행파일/push없음. 게임 규칙/가격/설치조건 변경 없음.

다음 구체 사운드는 미정이다. 음색선택과 연결기능QA를 최종 믹스 승인으로 확대하지 않는다.
