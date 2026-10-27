# 버티컬 슬라이스 신규 효과음 20개 (2026-10-27)

- 위치: `Assets/Audio/Resources/CoreGuardian/Audio/SelectedB/Slice/` (Resources 키 `CoreGuardian/Audio/SelectedB/Slice/<이름>`)
- 정본 선택표: Notion 「전투 사운드 설계 방향」 https://app.notion.com/p/3fb389dbfc68815f8923cb93f5b90743 '2단계 청취 선택 — 확정' (우선순위·묶음 간격은 거기).
- 원본: 프로젝트 문서 `claude/sound-synth.md` 합성 스크립트(시드 7)로 다시 만든 44.1kHz 16bit 모노 WAV. 청취판 MP3와 파형 상관 0.93~1.00 확인.
- 가져오기 설정(사용자 선택): Decompress On Load + ADPCM + Force To Mono + Preload. 피치·루프 없음.
- 아직 `catalog.json`·코드에 연결하지 않음(개발 재개 때 연결).

| 파일 | 사건 — 선택음 | 합성 원본 | SHA256(앞 12자) |
| --- | --- | --- | --- |
| `cold_pulse.wav` | S1 냉기 포탑 펄스 — 서리 숨 | `S1_1` | `48a75624d5b3` |
| `flame_shot.wav` | S2 화염 포탑 발사 — 마력 화구 | `S2_2` | `67ca380be591` |
| `rail_shot.wav` | S3 레일 포탑 발사 — 장치 격발 | `S3_1` | `9832d07a17a7` |
| `star_merge.wav` | S4 ★ 합체 — 별 승급(3차) | `S4r_1` | `e1c9e82bab70` |
| `heal_pulse.wav` | S8 치유사 회복 — 약초 숨 | `S8_2` | `91d96fc906a4` |
| `area_blast.wav` | S11 범위 폭발 — 스패너 강타 | `S11_1` | `606c9822e501` |
| `teleport.wav` | S12 순간 이동 — 마력 도약 | `S12_2` | `e02dcad973d7` |
| `chain.wav` | S16 연쇄 튀김 — 방전 사슬 | `S16_3` | `7f30bc778171` |
| `execute.wav` | S17 처형 — X 각인 | `S17_3` | `13fd0385a60c` |
| `crit.wav` | S18 치명타 — 강타 | `S18_3` | `7ef9b2908296` |
| `stun.wav` | S19 기절 — 둔탁한 멍 | `S19a_2` | `d1c11aef0473` |
| `freeze.wav` | S19 얼어붙음 — 결빙 | `S19b_1` | `b064a1ac20ff` |
| `shatter.wav` | S19 부서짐 — 얼음 파쇄 | `S19c_1` | `88210ae2a716` |
| `burn_explode.wav` | S21 화상 폭발 — 불씨 펑 | `S21_1` | `20a48954204e` |
| `reroll.wav` | S23 증강 리롤 — 카드 섞기 | `S23_1` | `984d8d3428df` |
| `shield_deploy.wav` | S24 철벽 기사 방패 전개 — 강철 방벽 | `S24_1` | `7d4b88f586f2` |
| `summon.wav` | S25 진홍의 군주 소환 — 핏빛 문 | `S25_2` | `6d3ec89f9333` |
| `hit_cold.wav` | H4 냉기 피격 — 서리 박힘 | `H4_2` | `82621dd67ba0` |
| `hit_flame.wav` | H5 화염 피격 — 화끈 타격 | `H5_3` | `1c7522e3f6f0` |
| `hit_rail.wav` | H6 레일 피격 — 관통 충격 (S15 관통도 이 소리) | `H6_1` | `243407d31286` |
