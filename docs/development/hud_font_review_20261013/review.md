# HUD 한글 폰트 시안 · 2026-10-13

사용자 요청: 게임에 어울리는 폰트를 구해 적용 전 시안을 제시한다. 추천은 A 갈무리, 강한 제목을 선호하면 C 조합. 이는 화면 관찰에 따른 디자인 의견이며 사용자 승인 전이다.

현재 한글 HUD는 OS Malgun Gothic, 피해 숫자는 승인된 Pixelify Sans Bold다. 이번 작업은 Assets 밖의 브라우저 시안만 생성했다. Unity 폰트/씬/코드/밸런스/피해 숫자는 변경하지 않았고 시스템 폰트 설치도 하지 않았다. 회복 기준은 134be1b다.

## 비교판

- [전체 비교 PNG](font_comparison.png), [로컬 HTML](preview.html). 게임의 실제 Panel/Button/Disabled/심볼 PNG를 사용한 미적용 시안이다. 현재 전체 Game View 캡처를 수정한 이미지가 아니다.
- A [갈무리](https://github.com/quiple/galmuri): 제목 Galmuri11 Bold, 본문 Galmuri11. 픽셀 아트와 통일감을 우선. [개별 시안](option_a.png).
- B [Neo둥근모](https://github.com/neodgm/neodgm/releases/tag/v1.601): 제목·본문 NeoDunggeunmo, 빠진 화살표 →만 명시적 Galmuri fallback으로 보완. 고전 RPG 분위기. [개별 시안](option_b.png).
- C [검은고딕](https://github.com/google/fonts/tree/main/ofl/blackhansans) 제목 + Galmuri11 본문: 증강 이름/패널 제목을 묵직하게 강조. [개별 시안](option_c.png).
- 같은 설치 버튼204×96/강화 버튼488×80/증강 카드350×460, 같은 문구·글자 크기16/18/20/24/26px로 비교. 상단은 전체920px 상태창을 재현하지 않고3개 값만520px 비교 열에 표시했다. 성장 패널도2개 버튼만 표시했다.
- 카드 문구는 실제 최종 HUD 검사 JSON에 기록된18종에서 가져왔다. 보이는 예시의 골드/수정 값은 서체 판독용이며 실행 데이터 변경이 아니다.

## 확인과 한계

- 공식 TTF4개와 각 제작자 OFL-1.1 원문3개를 fonts에 보관. 공식 출처는 sources.json. 변형 없이 사용했고 라이선스 전문을 함께 유지한다.
- Headless Edge /1664×1328/배율1로4개 서체 loaded·모든 이미지 loaded·페이지 오류0 확인. 전체판과 개별3판을 직접 열어 봤다.
- 보여주는 라벨 영역 초과0,3안×18종 카드 제목/설명54조합 초과0. [검사](render_check.json).
- TTF cmap에서 시안 문구/18종 제목·설명에 쓰는 글리프 확인. Neo의 → 부재를 발견해 명시적인 Galmuri fallback을 선언했고 보완 후 요구 문자 누락0. 제목용 서체는 제목에 필요한 문자만 검사했다. 모든 한국어 문자를 검증한 것은 아니다. [글리프 검사](glyph_check.json).
- HTML에서 hover 프레임 전환과 눌림 이동을 제공한다. 실제 게임 클릭/선택/효과 동작을 구현한 시안은 아니다.
- 브라우저 렌더링은 Unity Legacy Text/TMP의 래스터 결과와 다를 수 있다. 카드의 어절 단위 줄바꿈은 시안 CSS에 적용했다. 선택 후 실제 Unity의 작은14px 라벨·줄바꿈·카드18종·축소/다른 화면비·픽셀 정렬을 다시 확인하고 필요한 글자 크기/줄간격을 맞춘다. 특히 픽셀 폰트의 정수 격자 크기는 적용 시 검증할 대상이다.
- Unity를 새로 실행/정지/테스트하지 않았다. 작업 중 읽은 상태는 ready/stopped/compiling=false/domainReload=false, 전투 씬loaded/active/dirty=false. 기존 테스트 결과를 이번 폰트 시안의 테스트 결과로 재사용하지 않았다.

## 재생성

번들 Node로 build_preview.cjs → render_preview.cjs → check_glyphs.cjs 순서 실행. 이미지/폰트는 로컬 파일로만 읽는다. 사용자 선택 후 적용하며 현재는 승인 대기 시안이다.
