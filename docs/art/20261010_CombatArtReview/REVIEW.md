# 코어 가디언 1순위 전투 아트·피해 폰트 컨펌 — 2026-10-10

큰 픽셀과 단순한 형태, 수리·재가동한 석조 요새, 새 금속 보강재와 수정 장치라는 기존 게임 톤에 맞춘 선택 자료다. 승인된 캐릭터·포탑의 비교 크기(B 스프라이트 배율의 60%)와 발/받침대 접촉 중심을 유지하는 것을 후속 제작 원칙으로 삼는다.

아트는 세 방향으로 제작했으며, 각 보드에 전투 표현 9개가 있어 총 27예시를 비교할 수 있다. 아트 방향과 피해 숫자 폰트는 독립적으로 선택할 수 있다. 전체 보드 하나를 골라도 되고, 항목마다 A/B/C를 섞어 골라도 된다.

## 아트 방향 3안

| 선택 | 방향 | 화면 표현 |
|---|---|---|
| A | 절제된 수정광 | 작고 간결한 수정빛을 중심으로 전투 결과를 알린다. |
| B | 보강 금속 스파크 | 수리된 요새 설비의 단단한 금속과 짧은 스파크를 강조한다. |
| C | 각진 룬 표식 | 각진 수정·룬 형태와 표식으로 역할을 구분한다. |

### A — 절제된 수정광

![A 절제된 수정광](C:/Users/edcde/OneDrive/Desktop/CD/docs/art/20261010_CombatArtReview/Boards/A_board.png)

투명 정지 원본: [A draft](C:/Users/edcde/OneDrive/Desktop/CD/docs/art/20261010_CombatArtReview/DraftAtlases/A_draft.png)

### B — 보강 금속 스파크

![B 보강 금속 스파크](C:/Users/edcde/OneDrive/Desktop/CD/docs/art/20261010_CombatArtReview/Boards/B_board.png)

투명 정지 원본: [B draft](C:/Users/edcde/OneDrive/Desktop/CD/docs/art/20261010_CombatArtReview/DraftAtlases/B_draft.png)

### C — 각진 룬 표식

![C 각진 룬 표식](C:/Users/edcde/OneDrive/Desktop/CD/docs/art/20261010_CombatArtReview/Boards/C_board.png)

투명 정지 원본: [C draft](C:/Users/edcde/OneDrive/Desktop/CD/docs/art/20261010_CombatArtReview/DraftAtlases/C_draft.png)

## 각 보드의 셀 순서

각 보드를 왼쪽에서 오른쪽, 위에서 아래로 읽는다.

| 셀 | 표현 | 컨펌할 부분 |
|---|---|---|
| 1 | 기술병 탄 | 기술병의 짧고 간결한 발사체 형태·색. |
| 2 | 단일 포탑 탄 | 한 대상 공격이 읽히는 발사체 형태·색. |
| 3 | 범위 포탑 탄 | 서로 다른 대상에 발사하는 탄의 형태·색. 스플래시 폭발을 뜻하지 않는다. |
| 4 | 적 피격 | 맞았음을 알리는 짧은 효과의 형태와 강도. |
| 5 | 적 사망 | 처치됐음을 알리는 공통 소멸 표현. |
| 6 | 적 이동 버프 표식 | 이동 버프를 받는 적에 붙는 표식. |
| 7 | 포탑 공격력 버프 표식 | 지원 효과를 받는 공격 포탑에 붙는 표식. |
| 8 | 코어 피격 | 코어가 피해를 받았음을 알리는 표현. |
| 9 | 골드 획득 | 적 처치와 동시에 지급되는 골드 획득의 표시. 바닥에 놓고 줍는 아이템을 뜻하지 않는다. |

보드는 생성된 정지 컨셉이다. 실제 Unity Game view 캡처, 최종 스프라이트 정규화 결과, 확정된 픽셀 규격이나 애니메이션 프레임이 아니다. 투명 PNG도 임시 정지 원본이며, 컨펌 후 개별 분리·PPU·발/받침대 앵커·화면 크기를 검증한다. 애니메이션 제작에 들어가기 전 방향 수와 프레임 수를 확인한다.

보스와 새 HUD 시안은 이번 자료에 포함하지 않는다. 범위 포탑 스플래시나 바닥 골드 줍기 등 새 기능을 제안하는 자료도 아니다.

## 피해 숫자 폰트 3안

아래 비교는 생성 이미지에 글자를 그려 넣은 시안이 아니라 실제 다운로드한 TTF를 렌더링한 것이다. 24px와 32px, 어두운 배경과 밝은 배경에서 크림색 숫자/금색 골드와 검은 외곽선을 비교했다.

![실제 피해 숫자 폰트 3종 비교](C:/Users/edcde/OneDrive/Desktop/CD/docs/art/20261010_CombatArtReview/Fonts/damage_font_comparison.png)

| 선택 | 폰트 | 추천과 구분 | 한국어 |
|---|---|---|---|
| F1 | Pixelify Sans Bold 700 | 피해 숫자 우선 추천. 짧고 굵은 픽셀 형태와 약간 둥근 모서리가 기존 게임 톤에 어울린다. | 미지원. 한국어에는 별도 fallback 필요. |
| F2 | Neo둥근모 Regular | 숫자와 한국어를 같은 톤으로 통일할 때 적합. 고정폭 숫자. 제작자 권장에 따라 32px 우선. | 현대 한글 음절 11,172자와 표본 문구 지원 확인. |
| F3 | Silkscreen Bold | 네모난 굵은 블록으로 강한 레트로 느낌. 폭이 넓어 여러 자리 숫자의 겹침은 적용 후 확인 필요. | 미지원. 한국어에는 별도 fallback 필요. |

폰트 비교 PNG 내부의 A/B/C는 각각 F1/F2/F3를 뜻한다. 아트 보드 A/B/C와 묶음 선택 조건은 없다.

원본 TTF, 라이선스 전문, SHA256과 글리프 확인 기록은 [Fonts 폴더 안내](C:/Users/edcde/OneDrive/Desktop/CD/docs/art/20261010_CombatArtReview/Fonts/README.md)에 저장했다. 폰트는 시스템에 설치하지 않았고 Unity에 임포트하거나 HUD에 적용하지 않았다. Unity TMP Atlas·캔버스 배율·카메라 줌·숫자 겹침 검증은 컨펌 후 진행한다.

## 공식 출처와 상업 사용

세 후보의 원본 라이선스는 SIL Open Font License 1.1이다. 상업 게임에 번들·임베딩·배포할 수 있다. 폰트 파일 자체만 판매하지 않으며 폰트 배포 시 저작권 고지와 라이선스를 함께 유지한다. 이번 폴더에는 후보별 공식 라이선스 전문을 동봉했다.

- F1: [Google Fonts 공식 배포](https://github.com/google/fonts/tree/main/ofl/pixelifysans), [공식 라이선스](https://raw.githubusercontent.com/google/fonts/main/ofl/pixelifysans/OFL.txt).
- F2: [Neo둥근모 제작자 공식 배포](https://github.com/neodgm/neodgm/releases/tag/v1.601), [공식 라이선스](https://raw.githubusercontent.com/neodgm/neodgm/main/LICENSE.txt), [화면 표시 가이드](https://neodgm.dalgona.dev/guides.html).
- F3: [Google Fonts 공식 배포](https://github.com/google/fonts/tree/main/ofl/silkscreen), [공식 라이선스](https://raw.githubusercontent.com/google/fonts/main/ofl/silkscreen/OFL.txt).
- [OFL 공식 FAQ](https://openfontlicense.org/ofl-faq/)의 1.4에서 게임을 포함한 상업 소프트웨어 번들을 허용한다.

## 선택 전달 예

`전체 아트 A + 폰트 F1`처럼 답하거나 `1~3번 B, 4~5번 A, 6~9번 C, 폰트 F2`처럼 항목별로 골라도 된다. 현재 자료는 컨펌 후보이며 승인 완료나 게임 적용 완료 상태가 아니다.

생성 도구: 내장 imagegen. [제작 프롬프트 기록](C:/Users/edcde/OneDrive/Desktop/CD/docs/art/20261010_CombatArtReview/generation_prompts.json). 비교 보드 1536×1024, 투명 원본 1254×1254이며 3종 원본의 알파 범위 0~255를 확인했다. [이미지 파일 검증](C:/Users/edcde/OneDrive/Desktop/CD/docs/art/20261010_CombatArtReview/image_validation.json).
