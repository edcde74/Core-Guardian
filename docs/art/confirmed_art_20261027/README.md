# 확정 아트 Unity 반영 (2026-10-27)

2026-10-26 아트 시안판에서 사용자가 확정한 아트를 게임에서 바로 쓸 수 있는 투명 PNG로 다시 뽑아 기존 폴더 규칙에 맞춰 넣었다.
시안판: https://claude.ai/artifact/K1fDVrSS32V8b9Rk4Z8w6y · 정본 목록: Notion 「전투 프로토타입 추가 아트 제작 목록」 https://app.notion.com/p/3f3389dbfc6881d2a70be916ded8ddb7

## 상태
- **파일만 추가했다. 코드·씬·프리팹·CSV·registration.json은 바꾸지 않았다.** 게임에는 아직 안 나온다(개발 재개 때 연결).
- 임포트 설정은 같은 폴더의 기존 스프라이트를 복사했다(PPU 32, Point, 무압축, 발 기준 피벗 등). 효과는 PPU 32·가운데 피벗.
- 새 적 영문 key(데이터 명세 Splitter 외에는 임시): Charger=돌격병(11004) · Splitter=분열체(11005) · Healer=치유사(11006) · Armored=장갑병(11007) · Flyer=비행체(11008) · SplitChild=분열 작은 개체(11201). 새 포탑: Frost(12003) · Flame(12004) · Rail(12005).

## 연결할 때 주의
- 옆걸음 조각(OriginalWalk/<key>)은 기존 병사와 같은 Body + 다리 4조각 구조. 관절 좌표는 `Assets/Data/Animation/Resources/CoreGuardian/Animation/<key>/joints.json` (CoreGuardianOriginalWalk.cs와 같은 좌표계). 지금 관절값은 코드에 하드코딩돼 있으니 연결 때 옮길 것.
- Splitter는 SplitChild 두 명: 앞 병사 조각 + Back* 조각(뒤 병사, 어둡게, 반 박자 늦게) + Cord(허리 끈). 오프셋은 joints.json의 back.
- Flyer는 걷지 않는다: 옆 = OriginalWalk/Flyer/Hover_0~5, 앞뒤 = VerticalWalk/Flyer 4프레임(활공 오르내림). 그림자는 몸에서 떨어진 바닥 고정.
- VerticalWalk 새 프레임은 기존과 같은 96x112 캔버스에서 원본 병사 위치에 맞춰 둠(피벗은 같은 계열 원본 프레임 값 복사).
- PhotoShadows 새 그림자 6장은 registration.json에 아직 등록하지 않았다(오프셋은 연결 때 실제 화면 보고 정함).
- 기술병 범위 폭발: Characters/Player/Resources/CoreGuardian/AreaBlast/D0~D7/D*_Slam_0~5 = 캐릭터만 72x72(PlayerFacing 36x48 조각을 (18,20)에 둔 캔버스, 피벗은 발 위치). 고리·균열은 Effects/.../Skills/AreaBlast_0~2.
- 레일 포탑 발사 반동(3px)·방전, 냉기 펄스 빛, 화염 포구 불꽃은 기존 포탑처럼 코드 동작. Characters/Towers/Sprites/Motion/Rail_Fire_*는 참고 프레임.
- 얼어붙음은 공통 머티리얼 색 치환(Status/FreezeRamp.png: 밝기→얼음색 5단) + 발밑 얼음(FreezeFootIce.png). 기절 별은 체력바 기준점에 붙임.
- 치명타는 폰트 표시(노란 #FFD63C 2배 + "!"), 별도 그림 없음. 관통(D3)은 기존 피격 스파크 재사용. 레일 반사(E4)는 되돌아오는 지점의 작은 호박색 스파크 `Effects/.../Art/Towers/RailReflectSpark.png`(볼트는 RailBolt 좌우 뒤집어 재사용).
- 보스 격노 테두리는 참고용(BossRageOutline: 보스 대표 그림 기준). 실제는 기존 공통 외곽선 셰이더를 붉은색으로.

## 파일 목록

### `Assets/Art/Characters/Towers/Sprites/`
- tower_frost.png, tower_flame.png, tower_rail.png — 대표 그림(받침대+상부)

### `Assets/Art/Characters/Towers/Resources/CoreGuardian/OriginalMotion/Frost/`
- Fixed.png — 받침대: 단일 포탑 원본 그대로
- Moving.png — 상부(움직이는 부분)

### `Assets/Art/Characters/Towers/Resources/CoreGuardian/OriginalMotion/Flame/`
- Fixed.png — 받침대: 단일 포탑 원본 그대로
- Moving.png — 상부(움직이는 부분)

### `Assets/Art/Characters/Towers/Resources/CoreGuardian/OriginalMotion/Rail/`
- Fixed.png — 받침대: 단일 포탑 원본 그대로
- Moving.png — 상부(움직이는 부분)

### `Assets/Art/Characters/Towers/Resources/CoreGuardian/OriginalMotion/<포탑>/`
- Rail_Fire_0.png … Rail_Fire_6.png (7장) — 참고: 3px 반동 + 청록 방전

### `Assets/Art/UI/Resources/CoreGuardian/Hud/`
- TowerFrost.png, TowerFlame.png, TowerRail.png — 설치 목록 아이콘(48)
- DamageFrost.png, DamageFlame.png, DamageRail.png — 피해 패널 아이콘(DamageRoles 규격, 420)
- AreaBlast.png — G2 범위 폭발 아이콘(48)
- Reroll.png — G4 다시 뽑기 버튼(기존 Button 테 + 금 화살표)
- MergeLink.png — G3 두 포탑 사이 청록 연결선(가로로 늘려 씀)
- MergeStar.png — G3 합체 후 ★ 미리보기 별 하나

### `Assets/Art/Characters/Enemies/Sprites/`
- enemy_charger_v1.png, enemy_splitter_v1.png, enemy_splitchild_v1.png, enemy_healer_v1.png, enemy_armored_v1.png, enemy_flyer_v1.png — 옆모습 대표 그림

### `Assets/Art/Characters/Enemies/Resources/CoreGuardian/OriginalWalk/Charger/`
- Body.png, LeftUpper.png, LeftLower.png, RightUpper.png, RightLower.png — 옆걸음 조각

### `Assets/Data/Animation/Resources/CoreGuardian/Animation/Charger/`
- joints.json — 옆걸음 관절 좌표

### `Assets/Art/Characters/Enemies/Resources/CoreGuardian/OriginalWalk/SplitChild/`
- Body.png, LeftUpper.png, LeftLower.png, RightUpper.png, RightLower.png — 옆걸음 조각

### `Assets/Data/Animation/Resources/CoreGuardian/Animation/SplitChild/`
- joints.json — 옆걸음 관절 좌표

### `Assets/Art/Characters/Enemies/Resources/CoreGuardian/OriginalWalk/Healer/`
- Body.png, LeftUpper.png, LeftLower.png, RightUpper.png, RightLower.png — 옆걸음 조각

### `Assets/Data/Animation/Resources/CoreGuardian/Animation/Healer/`
- joints.json — 옆걸음 관절 좌표

### `Assets/Art/Characters/Enemies/Resources/CoreGuardian/OriginalWalk/Armored/`
- Body.png, LeftUpper.png, LeftLower.png, RightUpper.png, RightLower.png — 옆걸음 조각

### `Assets/Data/Animation/Resources/CoreGuardian/Animation/Armored/`
- joints.json — 옆걸음 관절 좌표

### `Assets/Art/Characters/Enemies/Resources/CoreGuardian/OriginalWalk/Splitter/`
- Body.png, LeftUpper.png, LeftLower.png, RightUpper.png, RightLower.png — 앞 병사 조각(SplitChild와 같음)
- BackBody.png, BackLeftUpper.png, BackLeftLower.png, BackRightUpper.png, BackRightLower.png — 뒤 병사 조각(어둡게)
- Cord.png — 허리 붉은 끈(앞 병사 원점 기준, 두 병사 위에 그림)

### `Assets/Data/Animation/Resources/CoreGuardian/Animation/Splitter/`
- joints.json — 옆걸음 관절 좌표

### `Assets/Art/Characters/Enemies/Resources/CoreGuardian/VerticalWalk/Charger/`
- Down_0.png … Up_3.png (8장) — 앞(Down)·뒤(Up) 걸음 4프레임

### `Assets/Art/Characters/Enemies/Resources/CoreGuardian/VerticalWalk/SplitChild/`
- Down_0.png … Up_3.png (8장) — 앞(Down)·뒤(Up) 걸음 4프레임

### `Assets/Art/Characters/Enemies/Resources/CoreGuardian/VerticalWalk/Healer/`
- Down_0.png … Up_3.png (8장) — 앞(Down)·뒤(Up) 걸음 4프레임

### `Assets/Art/Characters/Enemies/Resources/CoreGuardian/VerticalWalk/Armored/`
- Down_0.png … Up_3.png (8장) — 앞(Down)·뒤(Up) 걸음 4프레임

### `Assets/Art/Characters/Enemies/Resources/CoreGuardian/VerticalWalk/Splitter/`
- Down_0.png … Up_3.png (8장) — 앞(Down)·뒤(Up) 걸음 4프레임

### `Assets/Art/Characters/Enemies/Resources/CoreGuardian/VerticalWalk/Flyer/`
- Down_0.png … Up_3.png (8장) — 걷지 않음: 활공 오르내림 4프레임

### `Assets/Art/Characters/Enemies/Resources/CoreGuardian/OriginalWalk/Flyer/`
- Hover_0.png, Hover_1.png, Hover_2.png, Hover_3.png, Hover_4.png, Hover_5.png — 옆모습 활공 오르내림 6프레임(다리 조각 없음)

### `Assets/Art/Effects/Shadows/Resources/CoreGuardian/PhotoShadows/`
- Charger.png, Splitter.png, SplitChild.png, Healer.png, Armored.png — 넓은 그림자(registration.json 미등록)
- Flyer.png — 비행체: 몸에서 떨어진 바닥 그림자

### `Assets/Art/Characters/Player/Resources/CoreGuardian/AreaBlast/D0/`
- D0_Slam_0.png, D0_Slam_1.png, D0_Slam_2.png, D0_Slam_3.png, D0_Slam_4.png, D0_Slam_5.png — 범위 폭발 동작(캐릭터만, 72x72, PlayerFacing 36x48 조각을 (18,20)에 둔 캔버스)

### `Assets/Art/Characters/Player/Resources/CoreGuardian/AreaBlast/D1/`
- D1_Slam_0.png, D1_Slam_1.png, D1_Slam_2.png, D1_Slam_3.png, D1_Slam_4.png, D1_Slam_5.png — 범위 폭발 동작(캐릭터만, 72x72, PlayerFacing 36x48 조각을 (18,20)에 둔 캔버스)

### `Assets/Art/Characters/Player/Resources/CoreGuardian/AreaBlast/D2/`
- D2_Slam_0.png, D2_Slam_1.png, D2_Slam_2.png, D2_Slam_3.png, D2_Slam_4.png, D2_Slam_5.png — 범위 폭발 동작(캐릭터만, 72x72, PlayerFacing 36x48 조각을 (18,20)에 둔 캔버스)

### `Assets/Art/Characters/Player/Resources/CoreGuardian/AreaBlast/D3/`
- D3_Slam_0.png, D3_Slam_1.png, D3_Slam_2.png, D3_Slam_3.png, D3_Slam_4.png, D3_Slam_5.png — 범위 폭발 동작(캐릭터만, 72x72, PlayerFacing 36x48 조각을 (18,20)에 둔 캔버스)

### `Assets/Art/Characters/Player/Resources/CoreGuardian/AreaBlast/D4/`
- D4_Slam_0.png, D4_Slam_1.png, D4_Slam_2.png, D4_Slam_3.png, D4_Slam_4.png, D4_Slam_5.png — 범위 폭발 동작(캐릭터만, 72x72, PlayerFacing 36x48 조각을 (18,20)에 둔 캔버스)

### `Assets/Art/Characters/Player/Resources/CoreGuardian/AreaBlast/D5/`
- D5_Slam_0.png, D5_Slam_1.png, D5_Slam_2.png, D5_Slam_3.png, D5_Slam_4.png, D5_Slam_5.png — 범위 폭발 동작(캐릭터만, 72x72, PlayerFacing 36x48 조각을 (18,20)에 둔 캔버스)

### `Assets/Art/Characters/Player/Resources/CoreGuardian/AreaBlast/D6/`
- D6_Slam_0.png, D6_Slam_1.png, D6_Slam_2.png, D6_Slam_3.png, D6_Slam_4.png, D6_Slam_5.png — 범위 폭발 동작(캐릭터만, 72x72, PlayerFacing 36x48 조각을 (18,20)에 둔 캔버스)

### `Assets/Art/Characters/Player/Resources/CoreGuardian/AreaBlast/D7/`
- D7_Slam_0.png, D7_Slam_1.png, D7_Slam_2.png, D7_Slam_3.png, D7_Slam_4.png, D7_Slam_5.png — 범위 폭발 동작(캐릭터만, 72x72, PlayerFacing 36x48 조각을 (18,20)에 둔 캔버스)

### `Assets/Art/Effects/Combat/Resources/CoreGuardian/Art/Skills/`
- AreaBlast_0.png, AreaBlast_1.png, AreaBlast_2.png — 범위 폭발 이중 고리 + 바닥 균열(그림 크기, 게임에서 반지름 2타일로 맞춤)
- Teleport_0.png … Teleport_7.png (8장) — 순간 이동: 왼쪽 출발점에서 흩어짐 → 오른쪽 3칸 도착점에서 모임

### `Assets/Art/Effects/Combat/Resources/CoreGuardian/Art/Status/`
- Slow.png — D1 둔화 표식
- Burn_1.png — D2 화상 표식 스택 1
- Burn_2.png — D2 화상 표식 스택 2
- Burn_3.png — D2 화상 표식 스택 3
- Burn_4.png — D2 화상 표식 스택 4
- Burn_5.png — D2 화상 표식 스택 5
- Stun_0.png … Stun_11.png (12장) — D7 기절: 별 3개 + 궤도선(머리 위 체력바 기준점에 붙임)
- HealMark.png — B4 회복받는 적 표식(십자)
- FreezeFootIce.png — D7 얼어붙음 발밑 얼음(공통, 몸 폭에 맞춰 배치)
- FreezeRamp.png — D7 얼어붙음 색 치환표(밝기 → 얼음색 5단, 공통 머티리얼용)
- Execute_0.png … Execute_6.png (7장) — D5 처형: 휘어진 2연타 X(흰 칼선 → 진홍 칼자국 → 붉은 파편)
- Chain_0.png, Chain_1.png, Chain_2.png — D4 연쇄: 가는 청록 번개(참고 배치 3마리, 게임에서는 적 사이로 늘림)
- FreezeShatter_0.png, FreezeShatter_1.png, FreezeShatter_2.png, FreezeShatter_3.png, FreezeShatter_4.png — 얼음 부서짐(증강 39번): 조각만 튐

### `Assets/Art/Effects/Combat/Resources/CoreGuardian/Art/Towers/`
- Fireball.png — A2 화염탄
- RailBolt.png — A3 레일 볼트(관통·반사 공통)
- FrostPulse_0.png, FrostPulse_1.png, FrostPulse_2.png — A1 냉기 펄스 고리
- StarRank2.png — A4 ★2 받침대 아래 별 2개
- StarRank3.png — A4 ★3 별 3개
- MergeFlash_0.png, MergeFlash_1.png, MergeFlash_2.png, MergeFlash_3.png, MergeFlash_4.png, MergeFlash_5.png — A4 합체 순간 흰 섬광 + 별 떠오름

### `Assets/Art/Effects/Combat/Resources/CoreGuardian/Art/Enemies/`
- HealPulse.png — B4 회복 파동(얇은 고리)
- BossRageSpeedLines.png — B7 격노 발 뒤 붉은 속도선
- BossRageOutline.png — B7 격노 붉은 테두리(보스 대표 그림 기준 참고용, 실제는 공통 외곽선 셰이더 색으로)

### `Assets/Art/Effects/Combat/Resources/CoreGuardian/Art/Augments/`
- FireTile_0.png, FireTile_1.png, FireTile_2.png, FireTile_3.png — E1 불길 칸(증강 22번), 한 칸 32px
- BurnExplosion.png — E2 화상 폭발(증강 40번)
- StickyFloor.png — E3 끈끈이 바닥(증강 15번), 경로 첫 5칸 길이

### 포탑 동작 참고 프레임 (15:15 정리) `Assets/Art/Characters/Towers/Resources/CoreGuardian/OriginalMotion/`
- Flame/Fire_0~5 — A2 화염 발사(1px 반동 + 포구 불꽃, 그릇 불꽃 일렁임)
- Frost/Pulse_0~5 — A1 냉기 펄스(수정 하얗게 + 바닥 고리)
- Rail/Fire_0~6 — A3 레일 발사(3px 반동 + 청록 방전). 처음 넣은 `Sprites/Motion/`에서 이 위치로 옮겨져 있어서 화염·냉기도 같은 규칙으로 맞춤.
- 게임에서는 기존 포탑처럼 Fixed/Moving 두 장 + 코드 동작. 이 프레임은 동작 기준.
