# 전체 사운드 청취 페이지 · 2026-10-19

사용자 요청은 개별 항목 순차 제시에서 전체 항목 일괄 비교로 바뀌었다. 사용자가 **현재 적용음 + 비교 후보 2개씩, 항목당 총 3개**를 선택했다. 이 페이지의 선택은 청취 메모이며 Unity 적용 승인/실제 연결과 구분한다.

- [페이지 파일](../../../AudioProduction/CoreGuardian_AudioReview_20261019/index.html) 또는 같은 폴더 `Open_Audio_Review.cmd`로 직접 연다. 인터넷 없이 기존 로컬 리소스를 참조한다.
- 로컬 서버: 해당 폴더에서 Python으로 `serve.py --port 8879` 실행 후 `http://127.0.0.1:8879/AudioProduction/CoreGuardian_AudioReview_20261019/index.html`. 127.0.0.1 전용·읽기 전용·카탈로그 파일만 제공한다. `.local` 로그는 Git 제외. 외부 공개 배포 없음.
- 22개 항목 / 66개 청취본 / 현재 적용음 14개. 항목별 3개 연속 듣기, 현재 적용음 14개 이어 듣기, 전체 정지, 검색/필터, 라디오 선택/메모의 브라우저 저장, 결과 복사/JSON 저장.
- 기본 청취 음량15%는 페이지 전용이다. 게임 PlayerPrefs/저장 음량은 읽거나 쓰지 않는다. 마지막 게임 조회6.8%는 이전 승리음 적용 기록이며 이번 새 조회가 아니다.

## 구성과 번호

현재 적용14항목은 모두 **① 현재 적용**으로 표시한다. 이전 개별 제시 번호와 별개인 페이지 내 번호다. 단일/멀티/플레이어 공격, 몬스터 피격/처치, 코어 피해, 보스 등장/가속, 설치 성공, 강화 성공/실패, 라운드 시작/완료, 최종 승리다. 확정음을 미선택으로 되돌리지 않고 초기 비교 선택도 현재음으로 둔다.

미적용6항목은 설치 실패, 철거/취소, 패배, UI 클릭/열기/닫기 후보 각3개다. 지원 포탑 전투는 무음 유지하며 후보를 만들지 않았다.

증강은 기존 코드의 일반/희귀/전설 공식 재현3개로, 택일 후보가 아닌 현재 등급별 참고다. 게임 녹음이 아니다. BGM은 기존 제작 초안3개를 읽기 참조하며 원본/제작 문서를 수정하지 않았다. 이 두 항목을 포함해 총22항목이다.

승인 공격음의 비교본은 원본 길이82%와 끝60ms 페이드, 또는 3600Hz 1차 저역통과·gain0.94를 적용한 페이지 전용 WAV다. 원본/실제 발사 연결은 그대로다. 멀티의 초기0/85/170ms 세 포신 리듬을 포함하며 게임의35ms 타깃 이벤트 묶음은 변경하지 않았다. 나머지 후보는 기존 팩의 무편집 원본이다. 원본 음량 정렬/정상 전투 믹스 검증이나 에이전트 직접 청취 평가를 했다는 뜻은 아니다.

## 출처와 상업 사용 확인

2026-10-19 공식페이지 재조회: [Kenney Interface](https://kenney.nl/assets/interface-sounds), [Kenney Jingles](https://kenney.nl/assets/music-jingles)는 CC0이며 기존 팩의 동봉 라이선스도 보존한다. [RPG Essentials](https://assetstore-fallback.unity.com/packages/audio/sound-fx/rpg-essentials-sound-effects-free-227708)와 [Magic Aura](https://assetstore.unity.com/packages/vfx/particles/spells/free-magic-aura-construction-kit-154136)는 FREE/Standard Unity Asset Store EULA 표시다. [Unity FAQ](https://assetstore.unity.com/browse/eula-faq)의 게임 통합 상업 사용/원본 단독 재배포 제한을 따른다. 기존 취득 원본을 소유자 PC에서 참조하는 페이지로 공개 호스팅하지 않았다. 상세 제작/취득은 [공격음 기록](../magic_cannon_audio_20261018/README.md), [Kenney Interface 취득](../../../AudioProduction/CoreGuardian_Upgrade_20261018/Kenney_Interface/README.md), [Jingles 취득](../../../AudioProduction/CoreGuardian_Victory_20261019/README.md), [BGM 초안](../bgm_sketches_20261013.md)이 담당한다.

## 확인한 범위

- 시작 Git HEAD `86ad038`. 기존 PackageManagerSettings 변경·미추적 BGM/기타 자료·font_tools_vendor 접근 오류의 삭제 표시 보존.
- 카탈로그66개 경로 존재/길이/해시 확인, 생성 후 재검사에서66개 SHA256 일치. 전부 HTTP Range 응답206/파일 첫64바이트 일치66/66. 카탈로그 외 `.git/config`·씬 URL은404.
- Node `--check app.js` 통과. 브라우저에서66개 오디오 컨트롤 표시, 현재 적용 필터14/22·라운드 결과4/22·없는 검색0/22 확인. WAV 단일 포탑3개와 OGG 승리3개가 readyState4/오류 없음으로 연속 재생 종료했다. 직접 음색 평가 아님.
- 선택③ 변경 후①로 복원, 메모 입력 후 새로고침 보존/검사 메모 제거, 결과 복사 성공 표시 확인. 전체 적용음 재생의1/14 시작, 정지 후 재생0개, 정지 후 같은 항목 재시작 및3개 재생 종료 확인.
- 실제 브라우저 좁은 화면에서 세로 카드 배치 확인. [페이지 화면](preview.png).
- Unity 변경/조회/Play/컴파일/저장/자동검사 없음. Assets·씬·게임 설정·기존 음량·밸런스·원본 BGM 수정 없음. 기존 Edit3/3·Play12/12 기능QA 범위를 확대하지 않는다. 사람의 전체 청취/최종 선택·정상 다수전투 믹스·성능/전체 회귀는 남아 있다.

다음은 사용자의 페이지 선택 결과를 받는다. 번호만 받으면 항목명을 함께 확인하고 카탈로그의 정확한 경로로 대응한다. 기존 단일 항목 패배 후보 대기보다 이번 전체 청취 요청을 우선한다.
