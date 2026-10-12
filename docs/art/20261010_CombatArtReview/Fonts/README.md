# 코어 가디언 피해 숫자 폰트 후보 — 2026-10-10

이 폴더는 사용자 선택을 위한 원본 폰트와 비교 표본이다. 시스템 설치, Unity 임포트, 실제 HUD 적용은 하지 않았다. 표본의 크기와 색상은 비교 조건이며 최종 렌더링 규격을 확정하지 않는다.

## 비교 이미지

- `damage_font_comparison.png`: 실제 다운로드한 TTF로 직접 렌더링했다. 1600×1190 PNG. 24px/32px, 어두운 경로와 밝은 석재 배경, 크림색 피해 숫자/금색 골드, 검은 1px 외곽선.
- 문자열: `13 25 128 1024`, `+3 GOLD`, `0 1 3 8`. 한국어를 실제 지원하는 후보만 `피해 골드`도 표시했다.
- `render_font_samples.py`: 비교 PNG와 글리프 확인 JSON을 재생성하는 스크립트. Pillow 사용. 이미지 편집이 아닌 새 타이포그래피 표본 생성이다.
- `glyph_verification.json`: 다운받은 폰트의 Unicode cmap 지원과 원본 TTF SHA256을 확인한 기록.

## 추천 순서와 구분

| 선택 | 후보 | 권장 용도와 평가 | 한국어 |
|---|---|---|---|
| A / 1순위 | Pixelify Sans Bold (700) | 큰 픽셀과 약한 둥근 모서리가 단순한 기술병·수정 설비 톤에 잘 맞는다. 숫자가 짧고 충분히 굵어 떠오르는 피해 표시용으로 우선 추천. 0/8의 내부 구멍, 1/3의 다른 윤곽이 보인다. | 미지원. 한국어 UI에는 별도 fallback 필요. |
| B / 2순위 | Neo둥근모 Regular | 한국어와 숫자를 통일하고 싶을 때 추천. 고정폭 숫자라 여러 자리 정렬이 안정적이며 0에 대각선 표시가 있어 O와 구분된다. 얇은 획이라 작은 전투 숫자보다는 32px가 선명하다. | 샘플 전체와 현대 한글 음절 11,172자 지원 확인. |
| C / 3순위 | Silkscreen Bold | 네모난 굵은 블록이 강한 레트로 느낌. 짧은 순간에 숫자를 강조하기 쉽지만 같은 nominal 크기에서 문자열 폭이 더 넓다. 3/8의 열린/닫힌 윤곽이 구분된다. 네 자리 숫자와 여러 개체 겹침은 적용 단계에서 확인 필요. | 미지원. 한국어 UI에는 별도 fallback 필요. |

이는 실제 표본을 보고 내린 디자인 판단이다. Unity Game view 적용/플레이 판독 검증 결과가 아니다.

Neo둥근모 제작자는 PC 화면에서 16픽셀 배수 크기와 가짜 bold/italic 미적용을 권한다. 그래서 24px는 다른 폰트와 동일 조건을 비교하기 위한 표본이고, 해당 후보 채택 시 32px 쪽을 우선 확인해야 한다.

공식 가이드: https://neodgm.dalgona.dev/guides.html

## 라이선스와 상업 게임 배포

세 후보 모두 다운로드한 공식 라이선스가 **SIL Open Font License 1.1**임을 확인했다. 해당 라이선스는 소프트웨어와 함께 번들·임베딩·상업 배포를 허용한다. 폰트 파일만 단독 판매하지 않으며, 폰트가 배포되는 경우 저작권 고지와 라이선스를 포함한다. 게임 코드와 게임 자체를 OFL로 공개해야 하는 것은 아니다. 수정한 폰트에는 해당 Reserved Font Name 조건을 지켜야 한다.

이 검토에서는 폰트 파일을 수정하지 않았다. Pixelify의 700은 원본 variable font가 제공하는 축 설정으로 표본만 렌더링했다. Neo둥근모는 원본 Regular, Silkscreen은 원본 Bold를 사용했다. 각각 원본 라이선스 전문을 `OFL.txt`로 함께 저장했다.

상업 게임 허용을 명시하는 OFL 공식 FAQ 1.4, 소프트웨어 공개 조건을 설명하는 1.3, 라이선스 동봉을 설명하는 1.10/1.20:
https://openfontlicense.org/ofl-faq/

## 다운로드 원본과 공식 출처

확인·다운로드 날짜: 2026-10-10 (Asia/Seoul).

### Pixelify Sans

- 공식 배포: https://github.com/google/fonts/tree/main/ofl/pixelifysans
- 원본 TTF: https://raw.githubusercontent.com/google/fonts/main/ofl/pixelifysans/PixelifySans%5Bwght%5D.ttf
- 저장 파일: `PixelifySans/PixelifySans-Variable.ttf` (원본 바이트, 파일명만 명료하게 저장).
- 라이선스: https://raw.githubusercontent.com/google/fonts/main/ofl/pixelifysans/OFL.txt
- 저장 라이선스: `PixelifySans/OFL.txt`.

### Neo둥근모

- 제작자 공식 저장소: https://github.com/neodgm/neodgm
- 공식 릴리스 v1.601: https://github.com/neodgm/neodgm/releases/tag/v1.601
- 원본 TTF: https://github.com/neodgm/neodgm/releases/download/v1.601/neodgm.ttf
- 저장 파일: `NeoDunggeunmo/NeoDunggeunmo-Regular.ttf` (원본 바이트, 파일명만 명료하게 저장).
- 공식 라이선스: https://raw.githubusercontent.com/neodgm/neodgm/main/LICENSE.txt
- 저장 라이선스: `NeoDunggeunmo/OFL.txt`.

### Silkscreen

- 공식 배포: https://github.com/google/fonts/tree/main/ofl/silkscreen
- 원본 TTF (Regular): https://raw.githubusercontent.com/google/fonts/main/ofl/silkscreen/Silkscreen-Regular.ttf
- 원본 TTF (Bold): https://raw.githubusercontent.com/google/fonts/main/ofl/silkscreen/Silkscreen-Bold.ttf
- 저장 파일: `Silkscreen/Silkscreen-Regular.ttf`, `Silkscreen/Silkscreen-Bold.ttf`.
- 공식 라이선스: https://raw.githubusercontent.com/google/fonts/main/ofl/silkscreen/OFL.txt
- 저장 라이선스: `Silkscreen/OFL.txt`.

## 검증 한계

샘플은 폰트 파일을 Pillow/FreeType으로 실제 렌더링하고 눈으로 확인한 결과다. Unity의 TMP Atlas, 캔버스 배율, 카메라 줌, 다수 피해 숫자 충돌, 안티앨리어싱 설정은 아직 검증하지 않았다. 채택 후 그 조건에 맞춰 크기·외곽선·수명·배치를 검증해야 한다. 전체 게임 한국어 UI를 이 요청에서 새로 제작하지 않았다.
