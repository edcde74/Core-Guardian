# HUD 개편 아트 — 확정본 (2026-10-28, H1~H7 전부 확정, 같은 날 게임 적용 b14a22e — Assets/Art/UI/Resources/CoreGuardian/Hud/Redesign·Augments)

정본 기획: Notion 아트 제작 목록 'HUD 개편 신규 필요 목록'(H1~H7, 'H3 증강 아이콘 규칙' 표), HUD 문서 '전투 화면 개편안'.
시안판: https://claude.ai/artifact/YLPeU5hFPxjeSFYh47KqFC

## confirmed/
- H1 포탑 연구소 = C안 '포탑 조립 공방' (_highlight = 마우스 올림·선택, 청록 1px 테두리). 2×2칸, 원본과 같은 픽셀 배율(PPU 32·스케일 0.6, 1칸 ≈ 36.8px).
- H2 기술병 작업대 = A안 '작업대' 수정본(망치 제거).
- H3 증강 아이콘 50장: H3_augment_icons/augment_01~50.png (32×32 투명), index.json(이름·등급), 한눈에 보기 = H3_augment_icons_sheet.png.
  - 그리는 영역: 32×32 중 가운데 28×28, 네 모서리 판 자리 제외(scripts/safe.npy). 50장 모두 밖 픽셀 0.
  - 규칙: 주체(기술병=스패너·포탑=작은 포탑·투사체=청록 탄/볼트·적=투구·보스=큰 투구+금 띠+뿔·골드=금화·수정=수정·코어=수정+파랑 방패·증강=카드) + 효과(상태 표식 정본) + 오른쪽 위 수식어.
- H4 작은 등급 테두리 = A안 '모서리 보석' 36×36 + 테두리 안 등급 색 어두운 바탕. 아이콘은 (2,2)에 얹음.
- H5 정보창 심볼 32×32: 체력 A, 방어 A, 이동속도 A, 코어 피해 C, 초당 피해 A, 약점 B, 저항 B.
- H6 스크롤바 = A안 (트랙은 세로로 늘림, 손잡이 10×28).
- H7 범위 폭발 버튼 = 기존 AreaBlast.png 그대로 + 시계 방향 덮개는 코드.

## scripts/
Python(Pillow·numpy·scipy) 원본 스크립트. ART 경로는 클라우드 작업 공간 기준이라 다시 돌리려면 parts.py의 ART를 이 PC Assets/Art/로 바꿀 것.
- 픽셀화: quant.py(부품별 빛·덩어리 결 → 승인 원본 팔레트로 양자화), 아이콘은 iconq.py(중간 강도). 검수: qc.py + autofit.py(테두리 안 영역).
