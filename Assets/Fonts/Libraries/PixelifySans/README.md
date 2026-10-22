# Pixelify Sans Bold — 피해 숫자용

사용자가 2026-10-10 선택한 Pixelify Sans Bold(700)이다.

`PixelifySans-Bold.ttf`는 공식 Pixelify Sans variable TTF의 wght=700을 fontTools 4.66.1로 정적으로 인스턴싱한 파일이다. 원본 출처: https://github.com/google/fonts/tree/main/ofl/pixelifysans

공식 variable 원본은 `docs/art/20261010_CombatArtReview/Fonts/PixelifySans/PixelifySans-Variable.ttf`에 보존했다. 동봉된 `OFL.txt`는 원본 그대로다. 상업 게임 번들·임베딩·배포 가능하며 저작권과 라이선스를 유지한다. 원본 라이선스의 저작권 고지에는 Reserved Font Name 지정이 없다.

생성 절차와 SHA256: `docs/development/prepare_combat_font.py`, `docs/development/combat_font_static_20261010.json`.

TMP 에디터 설치/검증 스크립트: `docs/development/InstallCombatFont.cs`.

검증을 마친 숫자용 TMP 로드 이름:

- `CoreGuardian/Fonts/PixelifySansBoldDamage`
- `CoreGuardian/Fonts/PixelifySansBoldDamageOutline`

설치·검증 결과는 `docs/development/combat_font_install_20261010.json`에 저장했다. 정적 512×512 SDF atlas, 숫자와 `+ - . ,` 및 공백 지원을 확인했고 실제 Canvas 자식 TMP에서 `13 25 128 1024`의 14문자·44vertex 메시를 생성했다. 외곽선 재질은 같은 atlas를 사용하며 width=0.12다. 폰트는 한국어를 지원하지 않으며 기존 한글 HUD를 대체하지 않는다. 게임의 실제 피해 숫자 연결과 화면 판독은 별도 적용 검증 대상이다.
