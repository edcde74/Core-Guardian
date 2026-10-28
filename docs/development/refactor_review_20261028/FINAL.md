# 코어 가디언 코드 검토 — 최종 결과 (독립 최종 검토, 3단계)

- 입력: 관점 보고서 A(데이터·계약) · B(수명·순서) · C(에셋·설정) · D(모듈 간 일관성·규칙) · E(리팩토링 지도·성능), EXTRA_EVIDENCE(PC에서 직접 읽은 값).
- 이 문서의 모든 P2 항목은 최종 검토자가 원본 줄을 직접 열어 확인했다. 반박에 성공한 항목은 내리거나 지웠고, 반박 근거 줄을 적었다.
- 실제 값(PC 확인): Domain Reload 꺼짐(Play 들어갈 때 정적 값이 초기화되지 않음), SRP Batcher 꺼짐, 스프라이트 패커 모드 5(SpriteAtlasV2). 씬이 읽는 CSV 5개는 전부 `Assets/Data/Balance/Resources/CoreGuardian/` 실제 표다(씬 `CoreGuardian_CombatPrototype_20261008.unity:18493-18497`의 GUID가 EXTRA_EVIDENCE와 일치함).
- 등급: **P0** 지금 실제 피해 · **P1** 조건이 맞으면 심각 · **P2** 실제 결함이지만 범위가 좁거나 결과를 감당할 수 있음 · **P3** 규칙·유지보수 문제이거나 지금은 드러나지 않는 위험.
- **P0와 P1은 0건이다.** 다섯 관점 어디에서도 P0·P1을 주장하지 않았고, 최종 검토에서도 새로 나오지 않았다.
- "여러 관점 일치" 칸에 관점이 2개 이상 적힌 항목은 서로 따로 찾아낸 것이라 신뢰도가 더 높다.

문서 정정 1건: `docs/development/REFACTORING_RULES.md:23`에 "네임스페이스는 지금처럼 `CoreGuardian`"이라고 적혀 있다. 실제로 `namespace CoreGuardian`은 Domain/Rules의 8개 파일만 쓰고, 나머지 64개 파일은 전역 네임스페이스다. 이 문장을 "지금 네임스페이스를 그대로 유지(전역 포함), 옮기지 않음"으로 고쳐야 한다. 옮기면 테스트의 `GetType("CoreGuardianPrototype, Assembly-CSharp")` 같은 이름 찾기가 깨진다(D-36, E §5).

---

## §1 버그·동작 문제 (게임에서 보이는 결함)

리팩토링 중에는 고치지 않는다. 사용자가 고른 것만 따로 고친다.

### P2

| ID | 등급 | 한 줄 설명 (게임 화면에서 생기는 일) | 근거 file:line | 고치는 방법 한 줄 | 고치면 바뀌는 것 | 여러 관점 일치 |
|---|---|---|---|---|---|---|
| B1 | P2 | 증강 48 "황금 거래"나 50 "유리 대포"를 고르면 코어 최대치가 줄어든다. 화면은 이것을 **코어가 맞은 것처럼** 보여 준다: 피격 효과, 코어 피해 소리, "코어 에너지 -10" 알림. | CoreGuardianPrototype.cs:150 (이전 값과 비교해서 "피해"로 추정), Session.cs:662-663 (최대치를 줄이고 현재값을 깎음), previousCore를 다시 쓰는 곳은 :121·:391뿐 | 코어 피해를 값 비교로 추정하지 말고 Session이 "피해" 신호를 직접 보낸다. 또는 증강을 고른 직후 previousCore를 맞춘다. | 증강 48·50을 고를 때 피격 연출·소리·알림이 사라진다. 실제 수치는 그대로다. | A |
| B2 | P2 | 처치 골드 증강(10 현상금, 11 저금통, 18 빚내기)을 갖고 있으면, 적이 죽을 때 떠오르는 "+3" 같은 골드 숫자가 **실제로 받은 골드와 다르다**. 상단 알림 글자는 맞는 값을 보여 줘서 둘이 어긋난다. | CoreGuardianCombatArt.cs:69 (`enemy.Spec.Gold` 기본값을 표시), Session.Effects.cs:243 (실제 지급 = 기본값 × (1+증강%) 반올림), Prototype.cs:148 (알림은 실제 차이를 표시) | 실제 지급액을 처치 정보에 실어 보내고 화면은 그 값을 쓴다. | 떠오르는 골드 숫자만 바뀐다. 실제 골드는 그대로다. | A, B |
| B3 | P2 (기획 확인 필요) | "미래형" 증강 11 저금통과 36 무피해 보상이 **카드를 고르기 전부터 센다**. 예: 26라운드 동안 코어 피해가 없다가 27라운드에 36을 고르면 즉시 +26%가 된다. 반면 9·31·32번은 고른 뒤부터 센다. | Augmentations.cs:91 (`Value(36)*CleanRounds`), :101 (`Kills/per`), Session.cs:604 (`CleanRounds++`는 카드와 상관없이 항상), Session.Effects.cs:246 (`Kills++` 항상), Augmentations.cs:77-88 (`Apply`에서 이 둘을 0으로 만들지 않음) | 카드를 고를 때 그 카드 전용 카운터를 0부터 시작한다. 또는 기획서에 "처음부터 셈"이라고 적는다. | 늦게 고른 11·36의 효과가 작아진다(밸런스 변화). | A |
| B4 | P2 | 냉기 둔화율과 레일 관통 감쇠 강화를 **상한(둔화 60%, 감쇠 1)을 넘어서도 계속 살 수 있다**. 11번째 구매부터 수정 360개를 내도 효과가 없고, 연구소 버튼 글자는 "63 → 66"처럼 상한을 넘은 값을 보여 준다. | Session.cs:333-337 (전투 값만 상한으로 자름), :363-377 (구매할 때 상한을 검사하지 않음), :348-352 (표시용 값도 자르지 않음), CoreGuardianHud.Inspector.cs:121,130; upgrades.csv 21405(+3), 21605(+0.02); effectTags.csv 31101 maxPercent 60 | 상한에 닿으면 구매 버튼을 끄고 표시도 상한으로 자른다. | 상한에 닿은 뒤에는 구매가 막힌다. | A |
| B5 | P2 | 받은 증강 목록을 고정해서 보는 "전체 / +N" 버튼이 **첫 증강 선택 화면 뒤로는 영원히 눌리지 않는다**(회색 스킨으로 보임). Tab 키로 여는 방법은 그대로 된다. | CoreGuardianHud.cs:530-531 (증강 선택 중에는 카드 말고 모든 버튼을 끔. 꺼진 버튼도 포함), CoreGuardianHud.Redesign.cs:278-281 (이 버튼만 다시 켜는 갱신이 없음), :315 (증강이 1개 이상일 때만 보임). 다른 버튼은 모두 다시 켬: Hud.cs:190, :255, Inspector.cs:57, Redesign.cs:354 | 다른 버튼처럼 `refresh.Add(()=>more.interactable=true)`를 넣는다. | 버튼을 다시 누를 수 있다. | B |

### P3 (범위가 좁거나 지금 데이터에서는 거의 안 보임)

| ID | 등급 | 한 줄 설명 | 근거 file:line | 고치는 방법 한 줄 | 고치면 바뀌는 것 | 여러 관점 일치 |
|---|---|---|---|---|---|---|
| B6 | P3 | 처형(즉사) 피해 숫자에는 치명타(노란색 !)나 약점·저항 색이 나오지 않는다. | Session.Effects.cs:184-185 (표시용 표시를 켰다가 `finally`에서 끔), :196-199 (처형 피해는 그 뒤에 들어감), CoreGuardianHud.cs:383-384 | 처형 피해도 같은 표시 범위 안에서 넣거나, 의도대로 기획에 적는다. | 처형 숫자의 색·모양이 바뀐다. | A, B, D |
| B7 | P3 | 화상 피해가 **엉뚱한(이미 철거된) 포탑에 집계될 수 있다**. 또 피해 순위판에 "Burn"·"Shatter"가 영어 그대로 나온다. | Session.Effects.cs:99-101 (새 화상일 때 출처를 지우지 않음), :261, :267, :411 (출처 없이 화상을 줌), :122; CoreGuardianHud.cs:291-298 (모르는 출처는 원래 문자열을 그대로 반환) | 새 화상일 때 출처를 비우고, 피해 순위판에 한국어 이름을 추가한다. | 피해 순위판 집계와 이름이 바뀐다. | A |
| B8 | P3 | 보스 격노 때 번쩍이는 빨간 테두리가, 같은 프레임에 그 몸을 재사용한 **새 보스에 이어서 보일 수 있다**(0.5초). | CoreGuardianPrototype.cs:480-483 (강조·강화 표시는 떼어 내지만 격노 테두리는 떼지 않음), CoreGuardianRageOutline.cs:44 (꺼진 대상만 지움) | 몸을 반납할 때 격노 테두리도 `ReleaseTarget`으로 뗀다. | 드문 경우 테두리가 이어지지 않는다. | B, D |
| B9 | P3 | HUD 숫자 일부를 전투 계산과 다르게 따로 계산한다. DPS에 공격속도·피해증가 증강이 빠지고, 지원 버프%는 반올림되며, 이동속도에 버프 오라가 빠지고, 연구소 증가량은 ★2·★3 배율을 빼고 보여 준다. | CoreGuardianHud.Redesign.cs:113-116, :135, :150-151; Augmentations.cs:92 (반올림); Prototype.cs:451; Session.cs:377, :483-484 | Session이 "보여 줄 값"을 읽기 전용으로 내주고 HUD는 그것을 쓴다. | HUD 숫자가 바뀐다. 반올림 차이는 지금 증강 값이 모두 정수라서 안 보인다. | A, D |
| B10 | P3 (기획 확인) | 증강 29 "치명타 시 관통 감쇠 없음"이 레일에만 적용되고, 기술병 관통(증강 5)에는 적용되지 않는다. 카드 글에는 레일 한정이라는 말이 없다. | Session.cs:691 vs :682, :579 | 기획을 확인한 뒤 적용 범위를 맞춘다. | 기술병 관통 피해가 바뀔 수 있다. | A |
| B11 | P3 (기획 확인) | 증강 27 "얼음 감옥"이 원래 처형이 없는 포탑 공격에도 둔화된 적에게 5% 처형을 준다. 카드 글은 "+5%p"로 읽힌다. | Session.Effects.cs:195 | 기획 확인. | 처형 빈도가 바뀔 수 있다. | A |
| B12 | P3 | 같은 줄에 선 유닛끼리의 앞뒤 그리는 순서가 **재시작(Restart)할 때마다 달라질 수 있다**. 편집기에서 Play를 다시 시작해도 마찬가지다. 번호 세는 정적 값이 초기화되지 않고, 편집 모드에서도 계속 늘어난다. | World/ArtDepthSorting.cs:6 (`[ExecuteAlways]`), :13, :26-27 | Play 시작 때 0으로 되돌린다(`SubsystemRegistration`). 장면을 다시 불러올 때도 되돌릴지는 따로 정한다. | 겹친 유닛의 앞뒤가 매번 같아진다. | B, C, D |
| B13 | P3 | 한 프레임에 소리·효과 신호가 256개를 넘으면 뒤의 것을 조용히 버린다. 전투 계산 밖(설치 등)에서 생긴 신호는 한 프레임 늦게 나간다(예: 별 합체 star_merge). | Session.Effects.cs:19, :20-27; Session.cs:322 | 버린 수를 세거나 상한을 키운다. | 아주 붐빌 때 소리·효과가 더 난다. 측정은 하지 않았다. | A, B |
| B14 | P3 (개발용) | 역할 검증(Diagnostic) 모드를 다시 시작해도 이전 판의 치명타 누적(증강 26·44)이 남는다. | Session.cs:396-398; Session.Effects.cs:471-477 | 검증 시작 때 치명타 카운터도 0으로 만든다. | 검증 모드의 첫 몇 발만 바뀐다. | B |
| B15 | P3 | 게임 시작(Start) 중 오류가 나면 화면에 이유가 안 나오고, 반쯤 준비된 상태(`Ready`가 참)로 남는다. CSV를 읽다 실패하면 HUD가 생기기 전이라 안내도 못 한다. | CoreGuardianPrototype.cs:76, :98-125 | 실패 화면을 따로 띄우거나 `Ready`를 거짓으로 둔다. | 데이터나 그림이 빠졌을 때만 바뀐다. | A, B |
| B16 | P3 (확인 필요) | 설치 가능한 칸을 판단하는 규칙이 4곳(Session, Prototype, HUD 두 곳)에 따로 있고 안내 문구도 다르다. 타일맵 길과 `routeCells`가 다른 칸이 있으면 "초록인데 거절"이 생길 수 있다. 타일 데이터와는 대조하지 못했다. | Session.cs:277, :290-293; Prototype.cs:238-242, :246-248; CoreGuardianHud.cs:343, :345 | 판단을 Session 하나로 모은다. | 문구가 하나로 바뀐다. 칸이 어긋나 있다면 그 칸의 판정도 바뀐다. | D |
| B17 | P3 (의도 확인) | 신규 포탑(냉기·화염·레일)의 바닥 그림자 기준이 섞여 있다: 바닥 수치는 지원 포탑 값, 그림자는 단일 포탑 그림. | CoreGuardianTowerGrounding.cs:18-19; CoreGuardianPhotoShadow.cs:37; Editor/Content/CoreGuardianContentAssets.cs:21 | 의도인지 확인한다. 리팩토링 중에는 절대 바꾸지 않는다. | 그림자 모양 | C |
| B18 | P3 | 개발용 적 이름표에서 분열 작은 개체가 "SplitChild"로 나온다(HUD는 "분열 작은 개체"). 같은 이름표가 Domain과 HUD에 두 벌 있다. | Session.Debug.cs:69-78 vs CoreGuardianHud.Redesign.cs:189-197; 보스 이름 Hud.cs:276 / Session.Debug.cs:76 | 이름표를 하나로 모은다. | 개발 치트 화면 글자만 바뀐다. | D |
| B19 | P3 (치트로만 도달) | 반복 가능 증강 3·7·13·15를 두 번 고르면 효과가 늘지 않는다(값을 평균 냄). 정상 플레이에서는 같은 카드가 다시 나오지 않아 치트로만 만들 수 있다. | Session.cs:681, :683, :307; Session.Effects.cs:76; Augmentations.cs:43 | 기획 확인(합산인지 평균인지). | 치트 사용 시에만 바뀐다. | A |
| B20 | P3 (사용 여부 미확인) | MagicAura 재질이 Built-in 셰이더를 쓴다. URP에서 쓰면 분홍색으로 나온다. 씬에서 쓰는지는 .mat.meta가 없어 확인하지 못했다. | Assets/Art/Effects/MagicAura/Materials/Metal Plane.mat (`fileID 46`), Aura_1·Aura_2/*.mat (`fileID 200`) | PC에서 사용처를 확인한다. | 쓰고 있다면 분홍색이 사라진다. | C |
| B21 | P3 (가능성 낮음) | 빌드에 `Universal Render Pipeline/2D/Sprite-Lit-Default` 셰이더가 빠지면 설치 위치 색 표시가 경고만 남기고 꺼진다. | CoreGuardianPrototype.cs:259, :264-276 | 빌드 포함 목록에 넣는다. | 빌드에서만 의미가 있다. | C |
| B22 | P3 (Windows 영향 없음) | SharedOutlineMask 셰이더가 4.5 기능 패스와 대체 패스를 한 SubShader에 넣었다. 4.5를 못 쓰는 기기에서는 대체 패스까지 못 쓸 수 있다. 지금 대상(Windows D3D)은 영향이 없다. | SharedOutlineMask.shader:35-76 (:46 target 4.5, :65 target 3.5); CoreGuardianSharedOutline.cs:32 | 다른 플랫폼을 낼 때 SubShader를 나눈다. | 없음(지금 대상 기준) | C |

### §1-b 편집기·빌드 문제 (게임 화면이 아니라 작업 환경에서 생김)

| ID | 등급 | 한 줄 설명 | 근거 file:line | 고치는 방법 | 바뀌는 것 | 여러 관점 일치 |
|---|---|---|---|---|---|---|
| B23 | P3 (P2에서 내림) | 빌드할 때마다 스프라이트 아틀라스 준비 코드가 돌면서 텍스처·아틀라스·Sources.asset·MaskAtlas 값을 다시 저장한다. 다른 아틀라스와 그림이 겹치거나("Duplicate sprite ownership") 마스크 원본 알파가 바뀌면 빌드를 멈춘다. 예전 `CG_Enemies.spriteatlasv2`가 남아 있어 겹칠 수 있으나, 그 내용은 사본에 없어 확인하지 못했다. | Editor/Rendering/CoreGuardianSpriteAtlasPipeline.cs:23-28, :44-52, :58, :76, :80, :97; ASSET_LIST.txt:115 | 빌드 전에 PC에서 한 번 미리보기로 돌려 확인하고, 예전 아틀라스를 정리한다. | 빌드 과정만 바뀐다. | C |
| B24 | P3 | Domain Reload가 꺼져 있어서, 편집기에서 Play를 반복하면 일부 정적 값이 이전 Play 값을 그대로 쓴다. 대상: 소리 목록(catalog.json), 그림자 등록(registration.json), 스프라이트 라이브러리(한 번 못 찾으면 계속 못 찾음), HUD 피해 아이콘(없는 이름을 영구 기억), 피격 공유 재질, SharedOutline 스위치. 그래서 Play 중에 이 파일들을 고치면 반영되지 않는다. | CoreGuardianAudioCatalog.cs:34-35; CoreGuardianPhotoShadow.cs:15, :38; CoreGuardianSpriteLibrary.cs:13-14, :19; CoreGuardianHud.cs:56, :243-246; CoreGuardianHitFlash.cs:12, :23-28; CoreGuardianSharedOutline.cs:27-28, :35-36, :82. 초기화하는 쌍둥이 예: SegmentedEnemyHealthBar.cs:49, SpriteAtlasSources.cs:17-18, ObjectEmphasis.cs:80-81, BodyMaskCache.cs:119 | `SubsystemRegistration` 초기화를 추가한다. | 편집기에서 수정 사항이 바로 반영된다. 게임 동작은 같다. | B, C, D |

---

## §2 성능 (끊김·메모리 할당)

전부 리팩토링 중에는 고치지 않는다. 고칠 때는 측정 전후를 비교한다(OPTIMIZATION_RULES). "할당"은 쓰레기(GC) 메모리를 새로 만든다는 뜻이고, 쌓이면 가끔 끊김이 생긴다.

### P2

| ID | 등급 | 한 줄 설명 | 근거 file:line | 고치는 방법 한 줄 | 고치면 바뀌는 것 | 빈도 | 여러 관점 일치 |
|---|---|---|---|---|---|---|---|
| F1 | P2 | 마우스가 UI 위에 있는지 볼 때마다 이벤트 객체를 새로 만든다. | CoreGuardianPrototype.cs:235 (`new PointerEventData`); 호출하는 곳 :167 (매 프레임), :171, :184, :189 | 객체 하나를 필드로 두고 재사용한다. | 없음 | 매 프레임 1~3회 | E |
| F2 | P2 | 외곽선 강조가 대상 목록을 `IEnumerable`로 받아 돌면서 반복자를 박싱한다(작은 객체를 새로 만듦). | CoreGuardianObjectEmphasis.cs:164, :169, :171, :180 | 매개변수를 `List<Transform>`으로 바꾼다. | 없음 | 매 프레임 | E |
| F3 | P2 | 범위 폭발 쿨다운 숫자를 매 프레임 새 문자열로 만든다. 같은 글자인지는 만든 뒤에야 비교한다. | CoreGuardianHud.Redesign.cs:364 (호출 :368 ← Hud.cs:501) | 정수가 바뀔 때만 문자열을 만든다. | 없음 | 쿨다운 중 매 프레임 | E |
| F4 | P2 | HUD를 갱신할 때마다(초당 5회) 보이지 않는 패널까지 포함해 버튼 글자 약 60개, 상단 상태 줄(**투명이라 안 보이는데도**), 보스 줄, 피해 순위 줄(줄마다 문자열 자르기·LINQ), 선택 정보 8줄(LINQ·임시 목록), 증강 줄 그림 이름, 개발 메뉴 28줄, 음량 글자, 아이콘 이름 비교 약 45회를 모두 새 문자열로 다시 만든다. | CoreGuardianHud.cs:190, :509, :512-513, :272-273, :294-296, :562-565, :149, :369-371; CoreGuardianHud.Inspector.cs:104, :121, :195-200; CoreGuardianHud.Redesign.cs:92-140, :249-250, :268-270, :313; CoreGuardianHud.Cheats.cs:17-39; Session.Debug.cs:59; Audio/CoreGuardianHud.Sound.cs:36; CoreGuardianHudSkin.cs:25-53; Session.cs:346-353 (연구소 버튼마다 새 CombatStats·Dictionary) | 보이는 패널만 갱신하고, 값이 바뀌었을 때만 문자열을 만든다. | 없음 | HUD 갱신마다(초당 5회 + 알림마다) | E (D-10, D-12가 같은 곳을 다른 이유로 지적) |
| F5 | P2 | 코어가 맞는 프레임마다 HUD 전체 갱신이 한 번 더 돈다. 알림(Notify) 하나가 갱신 전체를 부른다. | CoreGuardianPrototype.cs:150 → :418 `Changed?.Invoke()` → CoreGuardianHud.cs:115 | 알림 글자만 따로 바꾼다. | 없음 | 코어 피격·알림마다 | E |
| F6 | P2 | 피해 숫자마다 새 문자열을 만들어 TMP에 넣는다. 처치 골드는 글자 폭 계산도 한 번 더 한다. | CoreGuardianHud.cs:385, :391-394, :402, :413 | TMP `SetText(int)`를 쓴다. | 없음 | 명중마다(연쇄·화상 틱 포함) | E |
| F7 | P2 | 효과 애니메이션을 하나 띄울 때마다 `"Fx_"+key` 이름을 새로 만든다. | CoreGuardianEffectsView.cs:62 | 이름을 미리 만들어 두거나 붙이지 않는다. | 계층 창의 이름만 바뀐다 | 효과마다 | E |
| F8 | P2 | 풀에서 다시 꺼낸 투사체·적 몸에도 매번 `"Projectile_"+Id`, `"Enemy_"+종류+"_"+Id` 이름을 새로 붙인다. 새 포탑도 `"Tower_"+종류+"_"+Id`를 만든다. | CoreGuardianPrototype.cs:596-597, :555, :570, :448 | 꺼낼 때 이름을 다시 붙이지 않는다. **포탑 이름 형식은 테스트가 찾으므로("Tower_Single_1") 유지해야 한다.** | 계층 창 이름만 바뀐다 | 투사체마다 / 스폰마다 | E |
| F9 | P2 | 외곽선 강조용 오브젝트를 풀에 두지 않는다. 적이 생길 때마다 새 GameObject, SelectionOutline 컴포넌트, "SharedOutline" 자식, 이름 문자열, 배열을 만들고, 죽을 때마다 Destroy한다. | CoreGuardianObjectEmphasis.cs:186-190, :156, :212-222; CoreGuardianSharedOutline.cs:158 | 강조 오브젝트도 적 몸처럼 풀에 둔다. | 없음 | 스폰·처치마다 | E |
| F10 | P2 | 표시에만 쓰는 `LastMessage`를 적이 죽을 때마다, 코어에 들어올 때마다 새 문자열로 만든다. | Session.Effects.cs:244; Session.cs:472 | 화면이 실제로 읽을 때만 만든다. | 없음 | 처치마다 | E |
| F11 | P2 | 골드가 늘어난 프레임마다 "+N 골드 획득" 알림 문자열을 새로 만든다. | CoreGuardianPrototype.cs:148 | 바뀐 값만 저장해 두고 표시할 때 만든다. | 없음 | 처치가 있는 프레임마다 | E |

### P3

| ID | 등급 | 한 줄 설명 | 근거 file:line | 고치는 방법 한 줄 | 바뀌는 것 | 빈도 | 여러 관점 일치 |
|---|---|---|---|---|---|---|---|
| F12 | P3 | 코어를 못 찾으면 매 프레임 씬의 모든 SpriteRenderer를 찾는다(배열 할당). 지금 씬은 코어를 찾으므로 한 번에 끝난다. | CoreGuardianObjectEmphasis.cs:165-167 | 찾기를 시작할 때 한 번만 한다. | 없음 | 코어를 못 찾을 때만 매 프레임 | C, D, E |
| F13 | P3 | 성능 기록(F3/F4) 중에는 기록 도구 자체가 할당을 만들어 자기 "GC KB/frame" 수치를 더럽힌다. | CoreGuardianPerfOverlay.cs:138, :199-222, :294-310 | 기록 버퍼를 미리 잡는다. | 측정값이 정확해진다 | 기록 중 | E |
| F14 | P3 | 범위 포탑 발사음에 `"multi_"+n`을 만든다. 목소리가 모자라면 LINQ로 뺏을 목소리를 고른다. | CoreGuardianCombatAudio.cs:117, :206-208 | 키를 미리 만든다. | 없음 | 범위 발사마다 | E |
| F15 | P3 | 분열·소환 처리 때 목록을 `ToArray`로 복사하고, 플레이어가 한 칸 걸을 때마다 방향 배열을 새로 만든다(증강 22). | Session.Effects.cs:337, :400 | 재사용 목록과 `Sides`(Session.cs:310, 같은 순서)를 쓴다. | 없음 | 분열마다 / 걸음마다 | E |
| F16 | P3 | 처음 보는 스프라이트의 마스크 모양을 캐시에 넣을 때 LINQ·배열 복사를 한다(스프라이트 종류 수만큼으로 한정). | CoreGuardianSelectionOutline.cs:271-273, :301; CoreGuardianSharedOutline.cs:230 | 그대로 둬도 된다. | 없음 | 새 스프라이트마다 | E |
| F17 | P3 | 격노·강화 표시가 람다 찾기를 쓰고, 표시할 때마다 GameObject와 재질을 새로 만든다. | CoreGuardianRageOutline.cs:23, :28, :31-32; CoreGuardianUpgradeFeedback.cs:26, :31, :34-36 | 풀을 두고 for문으로 찾는다. | 없음 | 격노·강화마다 | E |
| F18 | P3 | 같은 종류의 적이 32마리를 넘게 동시에 살아 있으면 풀이 넘쳐 새로 만든다(적 모델, 적 몸 둘 다). | SessionModels.cs:15; ModelPooling.cs:148, :158; CoreGuardianPrototype.cs:41 | 상한을 데이터 최대치에 맞춘다. | 없음 | 웨이브 28-29(적 11002가 33·35마리)에서만 | E |
| F19 | P3 | 플레이어를 클릭할 때 배열 2개와 LINQ를 쓴다. | CoreGuardianPrototype.cs:198-201 | 캐시해 둔다. | 없음 | 클릭마다 | E |
| F20 | P3 | 체력바가 0개가 되면 체력바 그림을 지웠다가 나중에 다시 만든다. | SegmentedEnemyHealthBar.cs:17, :36-47 | 지우지 않는다. | 없음 | 드묾 | E |
| F21 | P3 | 증강 카드 뒤집기 연출이 불꽃 하나마다 GameObject를 만든다(최대 104개). | CoreGuardianAugmentationView.cs:34, :46, :49, :56, :64 | 풀을 둔다. | 없음 | 증강 화면에서만 | E |
| F22 | P3 | 전투 계산의 반복 비용(GC는 없음): 버프 오라 O(적×버프적), 포탑 찾기·지원 판정 O(포탑²)/프레임, 직선 투사체 O(직선×적), 레일 표적 고르기 O(적²), 증강 16 O(포탑²). 규칙(REFACTORING_RULES.md:38)이 요구하는 복잡도 주석도 없다. | Session.cs:455-466, :176-190, :57, :562-569, :815-836, :670; CoreGuardianCombatAnimation.cs:77; Prototype.cs:451 → Session.cs:254 | 지금 N(적 ≤150, 포탑 ≤32)에서는 감당할 수 있다. 리팩토링 때는 주석만 단다. | 없음 | 매 프레임(계산 단계마다) | E, D-43 |
| F23 | P3 | 매 비교·매 유닛마다 문자열을 비교한다: 표적 동점 규칙(문자열 switch), 문화권을 따르는 `StartsWith("Tower:")`, 유닛 역할 이름 비교. | CombatData.cs:430-436; Session.cs:197; CoreGuardianCombatAnimation.cs:141; CoreGuardianHud.cs:272, :294; CoreGuardianCombatAudio.cs:127; CoreGuardianUnitAnimation.cs:36, :84-85, :93; CoreGuardianOriginalMotion.cs:42, :140-171 | 불러올 때 enum이나 bool로 바꿔 둔다. | 없음 | 매 프레임 / 투사체마다 | E |
| F24 | P3 | 유닛 하나마다 LateUpdate가 3개 돈다(애니메이션·그림자·정렬. 약 185개 몸이면 약 550번 호출). 포탑 바닥 처리는 한 프레임에 두 번 돈다. | CoreGuardianUnitAnimation.cs:125, :134; CoreGuardianPhotoShadow.cs:75; World/ArtDepthSorting.cs:24 | 매니저 하나에서 한꺼번에 돌린다. | 없음 | 매 프레임 | E |
| F25 | P3 | 공유 외곽선이 스프라이트 프레임이 바뀔 때마다 배치 배치도 전체를 다시 만든다. 전투 중에는 거의 매 프레임이다. | CoreGuardianSharedOutline.cs:239-249, :275-309 | 바뀐 부분만 다시 만든다. | 없음 | 거의 매 프레임 | E |
| F26 | P3 | 상태 표시가 살아 있는 적마다 표시 7종을 매 프레임 문자열 키로 찾는다. | CoreGuardianEffectsView.cs:111-123 | 키를 미리 찾아 둔다. | 없음 | 매 프레임 | E |
| F27 | P3 | 음량을 매 프레임 PlayerPrefs에서 약 12번 읽는다. | CoreGuardianCombatAudio.cs:89 → CoreGuardianAudioCatalog.cs:28 → CoreGuardianSoundSettings.cs:22, :28; CoreGuardianMusicAudio.cs:49 | 음량을 바꿀 때만 읽는다. | 없음 | 매 프레임 | E, D-T5 |
| F28 | P3 | 바뀐 것이 없어도 매 프레임 렌더러·UI 값을 다시 쓴다: 세로 걷기 다리, 플레이어 몸, 그림자, 포탑 빛, 모든 HUD 버튼 반응, 접기 손잡이. | CoreGuardianOriginalWalk.cs:280-302; CoreGuardianPlayerMotion.cs:70-76; CoreGuardianPhotoShadow.cs:69-72; CoreGuardianTowerGlow.cs:51-53; CoreGuardianHudSkin.cs:72-89; CoreGuardianHudHandleReaction.cs:29 (최종 검토의 파일 점검에서 추가) | 값이 바뀔 때만 쓴다. | 없음 | 매 프레임 | E, 최종 검토 |
| F29 | P3 | 떠 있는 피해 숫자마다 매 프레임 화면 좌표 변환, SetActive, 색 쓰기를 한다(사라지는 연출이라 어느 정도는 피할 수 없음). | CoreGuardianHud.cs:465-480 | 큰 이득은 없다. | 없음 | 매 프레임 | E |
| F30 | P3 | 코어 피격 연출이 끝나면 빈 property block을 되돌려 넣는다. 쌍둥이인 HitFlash는 `null`을 되돌린다. 화면은 같지만 코어 스프라이트의 배칭이 달라진다. | CoreGuardianCoreFeedback.cs:30, :52 vs CoreGuardianHitFlash.cs:58, :92-93 | HitFlash처럼 처음에 블록이 없었으면 `null`을 넣는다. | 배칭만 바뀐다(드로우콜) | 코어 피격 후 계속 | C, D |
| F31 | P3 | 빠른 마스크 경로는 카메라 화면이 **정확히 1920×1080**이고 직교 크기가 8.5 또는 4.25일 때만 쓰인다. 창 크기를 바꿀 수 있어서(ProjectSettings.asset:104) 다른 크기에서는 느린 예전 경로로 조용히 바뀐다. 화면은 같다. | CoreGuardianSelectionOutline.cs:248-251; ProjectSettings.asset:44-45, :104, :116 | 측정할 때 창 크기를 고정한다. 판단 조건을 하나로 모은다. | 없음 | 창 크기에 따라 | C, D, E |

---

## §3 안 쓰는 코드 삭제 후보

규칙 7(REFACTORING_RULES.md:16)에 따라 **목록만 보고하고, 승인을 받은 것만 지운다**. 참조를 찾은 범위 표기:
- 코드 = Runtime·Editor·Tests의 .cs 전부(이름 검색, `GetType`·`GetField`·`GetMethod` 문자열 검색 포함)
- 에셋 = 사본에 있는 .unity·.prefab·.asset·.mat 전체 텍스트
- 확인 못함 = 사본에 없는 에셋 67개(ASSET_LIST에만 있음), MCP `eval`로 직접 실행하는 스크립트

| ID | 무엇 | 확인한 참조 종류 | 지워도 되는지 | 주의 |
|---|---|---|---|---|
| X1 | `CoreGuardianPrototype.Ring()` (:608-614), `dot` 스프라이트 (:61, :113, :635. Start에서 텍스처를 만들고 OnDestroy에서 지우기만 함), `IsLabCell`/`IsBenchCell` (:86-87) | 코드·에셋, 테스트 0건 | 지워도 됨 | `dot`을 지우면 Start 때 텍스처 하나를 덜 만든다. 화면은 같다. |
| X2 | HUD `PurchaseTower`→`TowerCanUpgrade`→`Price`→`Relevant` 묶음 (Hud.cs:289, :309, :301, :308) | 코드·에셋, 0건 | 지워도 됨 | `Prototype.UpgradeTower(stat)` (:343)는 테스트가 이름으로 부르므로(`GetMethod("UpgradeTower")`) **남긴다**. |
| X3 | `ReleaseInspection` (Inspector.cs:44), `InfoVisible` (Redesign.cs:47), `towerFolded` (쓰기만 함: Hud.cs:70, Inspector.cs:192), `towerAbilities` (값을 넣는 곳 없음: Abilities.cs:9), `RefreshAbilityPlate`의 `support` 분기 (호출이 항상 false: Inspector.cs:194 → Abilities.cs:75-90) | 코드·에셋, 0건 | 지워도 됨 | 없음 |
| X4 | `HudSkin.Symbol`의 도달 불가 분기: 한자 `"接기"` (:45), 중복된 `rate` (:47, 앞의 :41에서 이미 반환), `PlayerSpecs` (:50, 앞의 :38에서 이미 반환) | 코드 | 지워도 됨 | 위에서부터 반환하는 순서는 그대로 둔다. |
| X5 | `RageOutline.IsFlashing` (:23), `ArtDepthSorting.SetArtwork` (:17), `SharedOutline.SetBatched` (:28)와 `ReuseGeometry` 쓰는 곳 | 코드·에셋, 0건 | 지워도 됨 | `ReuseGeometry`를 읽는 곳(:239)은 남는다. 지우지 말고 `const`나 읽기 전용으로 바꾸는 것이 더 안전하다. |
| X6 | `UnitAnimation.Loaded` (:35), `IsEnemyBody` (:39), `FrameInfo`/`AtlasInfo` (:10-11), `OriginalWalk.Phase` (:52) | 코드·에셋, 0건 | 대체로 됨 | 주석은 "진단 도구"용이라고 하지만 그 도구가 무엇인지 적혀 있지 않다. MCP eval에서 쓰는지 사용자에게 확인한다. |
| X7 | `OriginalMotion`의 `role=="Player"` 분기 (:150-155, :209) | 코드. Player는 UnitAnimation.cs:53에서 PlayerMotion으로 먼저 빠짐 | 지워도 됨 | 테스트가 찾는 "OriginalMotion" 오브젝트는 PlayerMotion.cs:33이 만든다. 이 클래스가 만드는 것이 아니다. |
| X8 | `CoreGuardianOriginalIdle` 클래스 전체 | 코드·에셋, 0건 | 지워도 됨 | 테스트가 찾는 "OriginalIdle" 오브젝트는 PlayerMotion.cs:33이 만든다. 이 클래스만 읽던 그림 `Resources/CoreGuardian/OriginalIdle/Player/*`는 아트 파일이라 리팩토링 범위 밖이다. 따로 정리한다. |
| X9 | 포구 섬광(Muzzle) 경로: CombatAnimation.cs:156-165, 빈 루프 :111-124, `flashSprite` 불러오기 (:40의 SingleBolt) | 코드. 발사하는 쪽은 항상 Player나 포탑 6종이고 `UsesOriginalArtwork`(UnitAnimation.cs:36)가 모두 참이라 :156에서 반환함 | 지워도 됨 | 테스트가 "Muzzle_이 없다"를 검사한다(AnimationPlayModeTests.cs:239). 지워도 통과한다. 리소스 로드가 하나 줄어든다. |
| X10 | `Session.FireCellCount` (Effects.cs:395), `DebugAddProjectile` (SessionModels.cs:107), `HitPayload.ExtraLine` (ModelPooling.cs:110), `HitPayload.Skill` (쓰기만 함: Effects.cs:427, :453), `RoundsSince12` (Augmentations.cs:75), `PlayerKills`/`BossKills` (쓰기만 함), `TowerState.Stats.UpgradeSteps` (쓰기만 함: Session.cs:300, :351, :377), 항상 1인 `shots` 루프 (Session.cs:778-779) | 코드, 0건 | 대체로 됨 | `shots` 루프를 없앨 때 난수 사용 순서가 같은지 확인한다(한 번만 돌므로 같아야 함). |
| X11 | 진단 카운터: MaskPage `PageCount`/`RuntimeBytes`, BodyMaskCache `Hits`/`Generations`/`Allocations`/`Releases`/`AllocatedCount`/`AllocatedBytes`, SpriteBatchCompat `LastColorTwins`/`LastMeshes`, SharedOutline `LayoutRebuilds`/`LastMaskDraws`/`LastEntityCount`/`LastPartCount`, CombatAudio `DroppedCount`/`MultiVolleyPlayedCount`/`SessionCuePlayedCount`/`ActiveVoices` | 코드·테스트, 읽는 곳 0건 | **보류 권장** | MCP eval로 확인할 때 쓰는 값일 수 있다(확인 못함). 테스트가 읽는 `EntryCount`, `BindingCount`, `LastProducerDraws`, `LastBatchedOutputs`는 반드시 남긴다. |
| X12 | 바닥 접촉 기능 잔재: `PresentGroundContact` (SelectionOutline.cs:205, 호출 0건), `PresentGroundContactWithBand`, `PresentContact`, 접촉 자르기 (:119-136), `contactShader`, `RegisterOutput(contact=true)`, MaskOutputBatch 접촉 분기, `PhotoShadowContact.shader` | 코드(게임 코드 호출 0건, 테스트 4건), 에셋 0건(.mat 없음) | 테스트와 함께 지울 때만 | 테스트가 "승인된 제거"를 검사한다(PhotoShadowTests.cs:73). 셰이더는 Resources에 있어서 지금도 빌드에 들어간다. |
| X13 | MaskOutputBatch의 출력 묶기 본체와 `BatchedMaskOutput.shader` | 게임 실행 중에는 SelectionOutline.cs:231에서 막혀 편집 모드·테스트에서만 돈다 | **보류** | 정적 생성자의 카메라 훅(MaskOutputBatch.cs:65-68, :93)은 실행 중 마스크를 처리하는 **유일한 통로**다. 묶는 부분만 지울 수 있다. 자세한 내용은 §5 H5. |
| X14 | SelectionOutline.cs:240 `if(Application.isPlaying&&!prepared)return;` | 바로 앞 :231에서 이미 반환하므로 도달할 수 없음 | 지워도 됨 | 없음 |
| X15 | `MaskPage.Queue` 메시 버전 (:65), `MaskProducerBatch.Queue` 버전 (:57), `SelectionOutline.PresentAtOrder` (:191) | 테스트만 씀 | 테스트와 함께 | 없음 |
| X16 | 예전 프로토타입 컴포넌트: `Health`, `EnemyController`/`EnemyDefinition`/`WaypointPath`, `BeanProjectile` (테스트만 씀), `LightningTower`·`TowerTargeting` (테스트도 안 씀) | 코드·에셋(씬·프리팹 GUID 0건) | LightningTower와 TowerTargeting은 됨. 나머지는 테스트와 함께 | `TowerCombatStats`는 테스트 어셈블리의 기준점이다(RulesTests.cs:14 `typeof(TowerCombatStats).Assembly`). 지우면 컴파일이 깨진다. |
| X17 | `CombatRules.Priority`/`TowerPriorityOrder`/`CompareTargetPriority` (CombatData.cs:390-394) | 테스트만 씀 | 테스트와 함께 | 테스트가 게임이 쓰지 않는 포탑 순서를 검사하고 있다(실제 게임은 Session.cs:812). |
| X18 | `CombatData.LoadLegacy` (:137-157) | 게임 CSV에는 `key` 열이 있어서 이 경로를 타지 않음. 테스트 고정 데이터만 씀 | 테스트와 함께 | 이 경로도 실제 Resources 표를 읽는다(:155). |
| X19 | `Welcome2DScript` (Editor/ProjectWelcome) | 코드 0건 | **지우지 말 것** | 인스턴스 에셋(`Assets/Settings/Editor/Welcome2DScript.asset`)이 사본에 없다. `Packages/manifest.json:17`에 튜토리얼 패키지 `com.unity.learn.iet-framework`가 있다. D 보고서의 "튜토리얼 패키지 없음"은 틀렸다. |
| X20 | `ObjectEmphasis.EndCameraRendering` 빈 처리기 (:123, 등록 :114-115 / 해제 :235-236) | 코드 | 지워도 됨 | 등록과 해제를 함께 지운다. |
| X21 | 참조를 못 찾은 Resources 내용: BossEyePrefab, Animation `frames.json`/`joints.json`, `H3_augment_icons_sheet.png`, `stun.wav`/`teleport.wav`, SpriteLibrary의 IronKnight_Body 같은 일부 키 | 코드·에셋(catalog.json은 확인 못함) | 리팩토링 범위 밖(아트·데이터) | 그래도 빌드에 들어간다. 아트 정리 때 따로 다룬다. |
| X22 | 바꿔도 반영되지 않는 CSV 열: towers tag/attackType/requires, skills key/input/phase/android, augments category/personality/effect, effectTags unit, upgrades의 Player 행 cost/costIncrement, waves interval, stage waveReward/augmentationEveryLate/augmentGradePlatinum, enemies의 기본 health/gold/defense/defenseRef | 코드 | 데이터 작업 | 리팩토링이 아니라 데이터 정리다. CSV 열 이름은 바꾸지 않는다(규칙 2). |

남겨야 하는 것(죽은 것처럼 보이지만 살아 있음): `PrototypeGridMovement`(씬 3개의 GUID와 `player` 필드), 투명한 `status` 글자(테스트가 `Find("Status")`로 찾음), `SoundSettings.Candidate`와 CombatAudio의 `*ResourcePath` 상수(테스트가 이름으로 읽음), `SpriteBatchCompat`와 `PerfOverlay`(`RuntimeInitializeOnLoadMethod`로 시작됨), Session의 테스트용 훅(D8).

---

## §4 리팩토링 작업 목록 (동작을 그대로 유지)

공통 원칙:
- MonoBehaviour·ScriptableObject 클래스 이름, 파일 이름, `.meta` GUID, 직렬화 필드 이름은 그대로 둔다.
- `partial`로 나눌 때 새로 생기는 파일은 MonoScript가 아니므로 새 GUID여도 괜찮다.
- 테스트가 이름으로 찾는 멤버는 **같은 클래스의 다른 partial 파일로만** 옮긴다.
- 새 할당, LINQ, 캡처하는 람다를 새로 넣지 않는다.
- 각 묶음이 끝나면 EditMode와 PlayMode 테스트를 전부 돌린다.

### 선행 작업 (묶음 ①보다 먼저 결정할 것)

**P-1. Presentation·Bootstrap에 asmdef를 둘지(테스트가 리플렉션을 그만 쓰게 할지) — 위험이 중간이고, 사용자가 결정해야 한다.**
- 지금 상황: Domain만 `Fortress.Game` 어셈블리이고, 나머지는 Assembly-CSharp이다. 테스트는 Assembly-CSharp를 참조할 수 없어서 문자열로 찾는다: `GetType` 약 23~32종, 멤버 이름 약 180~208개, private 필드 포함. 그래서 이름을 바꿔도 컴파일은 되고 테스트 실행 중에야 깨진다(C-H9, D-2, E §5).
- 방법: `Assets/Scripts/Runtime/`에 asmdef 하나를 둔다. Domain은 자기 asmdef가 있으므로 빠지고, Bootstrap+Presentation만 들어간다. **둘은 서로 참조하므로(Prototype↔Hud 등) 반드시 같은 어셈블리여야 한다.** 테스트 asmdef가 이 어셈블리를 참조하게 한다.
- 씬 스크립트 GUID 영향: 씬·프리팹은 스크립트를 `.cs.meta`의 GUID로 찾는다. GUID는 바뀌지 않으므로 **연결은 끊기지 않는다**.
  - 다만 YAML의 `m_EditorClassIdentifier: Assembly-CSharp::클래스`(씬에 7줄, 프리팹 21개)는 다음에 씬·프리팹을 저장할 때 새 어셈블리 이름으로 다시 써진다. 씬·프리팹은 리팩토링 범위 밖이므로 **저장하지 않거나**, 이 차이만 따로 승인받는다.
- 동작 영향: 게임 실행 동작은 같다. `DefaultExecutionOrder`, 정적 생성자 시점, Resources, JsonUtility 모두 그대로다.
  - `SerializeReference`, `InternalsVisibleTo`, link.xml은 없다(검색 0건).
  - 같은 종류의 `RuntimeInitializeOnLoadMethod`끼리는 원래 실행 순서가 정해져 있지 않다(PerfOverlay.Install과 SpriteBatchCompat.Initialize. 둘 다 같은 새 어셈블리로 함께 옮겨짐). 그래서 위험은 낮지만 0은 아니다.
- 해야 할 일:
  - 새 asmdef에 패키지 참조를 직접 넣는다(InputSystem, TextMeshPro, UGUI, URP Runtime/Core, Tilemap, 2D). 빠지면 컴파일 오류가 나므로 조용히 깨지지는 않는다.
  - 테스트의 `"X, Assembly-CSharp"` 문자열 27곳 이상을 고쳐야 한다. 규칙 6에 따라 검사 내용은 약하게 하지 않고 `typeof`로 바꾼다.
- 판단: "실행 동작 동일"은 지킬 수 있지만 **프로젝트 구조를 바꾸는 일**이다. 다른 선택지(위험 낮음): 어셈블리는 그대로 두고, 테스트의 이름 문자열을 한 파일(예: `TestNames.cs`)로 모아 오타 위험만 줄인다. 어느 쪽이든 이 결정이 묶음 ②~⑦에서 이름을 다루는 방식을 정한다.

**P-2. 픽셀 비교 기준 만들기.**
- 지금 셰이더 픽셀 테스트는 **실제 `SelectionOutline.shader`가 아니라 복사본**(Fixtures/SelectionOutlineReference vs Candidate)을 비교한다. 복사본과 실제 파일은 1줄(이름)만 다르다(CoreGuardianSelectionOutlineShaderTests.cs:10-13, :67).
- 묶음 ③을 시작하기 전에, 바꿀 실제 셰이더 12개 각각에 대해 "바꾸기 전 / 바꾼 후" 픽셀 비교 테스트를 먼저 추가한다. 테스트를 강하게 만드는 것이므로 허용된다.
- 이 테스트는 `docs/development/outline_sampling_20261023/`를 읽고 쓴다. 이 폴더는 사본에 없어 PC에 있는지 확인해야 한다.

**P-3. 화면·성능 기준을 잡는 절차.**
- before/after 화면 비교 전에는 Unity를 다시 시작한다(ArtDepthSorting 번호가 이어지기 때문, B12).
- 창은 1920×1080으로 고정한다(F31).
- 정해 둔 시드와 정해 둔 웨이브로 비교한다.

### ① 도구 설정
- 새로 만든 `.editorconfig`가 REFACTORING_RULES.md:18-23과 맞는지 확인한다: 4칸 들여쓰기, 중괄호는 새 줄, 120자, LF, BOM 없음.
- 포매터 실행 규칙: 파일 하나씩 돌리고, 한 커밋에는 모양 변경만 넣는다.
- 줄바꿈: `Tests/EditMode/CoreGuardianRulesTests.cs` 한 줄만 CR이 있다. LF로 고친다.
- 위험:
  - 포매터가 `#if UNITY_EDITOR` 블록이나 한 줄짜리 가드를 다르게 바꿀 수 있다. 규칙 21은 한 줄 가드를 허용한다.
  - 셰이더 파일은 포매터 대상에서 뺀다(픽셀 동일은 손으로 확인).
  - Unity가 다시 쓰는 `.meta`는 건드리지 않는다.

### ② 전투 규칙 (Domain)
작업:
- `Session.cs`(857줄)를 partial로 나눈다: `Session.Board.cs`, `Session.Spatial.cs`, `Session.Towers.cs`, `Session.Attack.cs`, `Session.Projectiles.cs`, `Session.Rounds.cs`.
- `Session.Effects.cs`를 나눈다: `Session.Cues.cs`, `Session.Tags.cs`, `Session.FlyingRoute.cs`, `Session.Status.cs`, `Session.Damage.cs`, `Session.Spawns.cs`, `Session.Skills.cs`, `Session.Crit.cs`(E 2.1의 줄 범위).
- 생성자 한 줄(Session.cs:258, 약 700자)을 같은 순서의 여러 문장으로 편다.
- `CombatData.cs`를 나눈다: `CombatEnums.cs`, `CombatSpecs.cs`, `CombatStats.cs`, `CombatData.cs`(partial), `CombatData.Loading.cs`, `CombatData.Csv.cs`, `CombatRules.cs`, `TargetOrder.cs`, `TowerState.cs`. `LoadTables`(88줄)는 같은 순서로 Stage→Player→Enemies→Towers→Affinities→Upgrades→Waves로 나눈다.
- 이름 있는 상수로 바꿀 값:
  - 계산 단계 `.05f`(Session.cs:420)
  - 오차 `.00001f` 6곳(:434, :472, :516, :584, :706, :768)
  - 신호 상한 256(Effects.cs:19)
  - 풀 상한 4/32(SessionModels.cs:15)
  - `"Tower:".Length`=6(:200-203)
  - 범위 공격 최대 대상 3(:166, :763)
  - 기획 수치는 옮기지 않고 목록만 남긴다(D-46)
- 같은 것이 확실한 중복만 합친다:
  - 범위 안 적 모으기 3곳(Session.cs:38-39, :703-704, :729-730)
  - `TowerSpec`으로 `CombatStats` 만들기 2곳(:296, :350)
  - `SplitChildSpec`/`SummonSpec` 캐시 앞부분(Effects.cs:314, :327)
  - 범위 폭발·순간이동 폭발 본문(Effects.cs:419-442, :445-468). **`UseAreaBlast`는 피해 루프보다 먼저 쿨다운을 넣는다(:425)는 점과 신호 순서가 다르다.** 차이를 매개변수로 남긴다.
- 한 글자 메서드 이름(`F`, `I`, `S`, `B`)은 private이라 바꿔도 테스트에 안전하다. 다만 여러 파일에서 쓰므로 한 번에 바꾼다.

위험:
- 난수 순서: `RollCrit` 안의 `combatRandom.NextDouble()`(Effects.cs:479)는 발사·펄스·폭발 대상마다 루프 순서대로 불린다. 루프 순서를 바꾸면 결과가 달라진다.
- 콜백 뒤의 `if(disposed)return;`와 `rangeIndex.Invalidate(); supportCache.Dirty=true`(Session.cs:478, :487, :510, :580, :715-716, :793-805)는 메서드를 쪼갠 뒤에도 같은 자리에서 멈춰야 한다.
- 테스트가 이름으로 쓰는 것:
  - private 필드 `"path"`
  - private set 자동 속성 `CoreEnergy`, `Wave`, `Gold`, `Crystals`(SetValue). 자동 속성 그대로 유지한다.
  - `GetEvent("DamageApplied")`
  - `GetType("CoreGuardian.CombatRules")`
- 초기화 순서:
  - `TargetOrder.KindCount`(:402)는 `Common`(:405)보다 먼저 초기화되어야 하므로 같은 파일에 그 순서로 둔다.
  - `rangeEntryOrder`(:90)도 같은 이유로 함께 둔다.
  - partial 파일끼리의 필드 초기화 순서는 C# 규격에서 정해져 있지 않다.
- enum 순서는 풀 번호와 씬 배열 번호로 쓰인다(CombatData.cs:10 주석). enum 이름은 그림 경로가 된다.
- 예외 메시지 문자열, `LastMessage`의 한국어 문구, 신호 id(Effects.cs:12-13)는 화면이나 테스트와 맞물려 있다.

### ③ 렌더링·셰이더
작업:
- `CoreGuardianSelectionOutline.cs`(503줄, 3종류):
  - `CoreGuardianSharedMaskMaterial`(:426-450)과 `CoreGuardianSpriteReferences`(:454-503)를 각자 파일로 옮긴다. 둘 다 MonoBehaviour가 아니다.
  - 본체를 partial로 나눈다: `.Mask.cs`(PresentMask 106줄을 CollectSources / ProjectBounds / ContactCrop / ProduceMask / BuildOutputQuad로), `.Geometry.cs`, `.Output.cs`, `.Resources.cs`.
- `CoreGuardianSharedOutline.cs`: `.Mask.cs`, `.Output.cs`, `.Resources.cs`.
- `MaskOutputBatch.Flush`(66줄): CollectGroups / MergeGroups / ApplySingles.
- `ObjectEmphasis`의 쌍둥이 재질 캐시를 `.Twins.cs`로 옮긴다.
- 카메라 판단 조건 3곳(:122-129, :235-238, :248-251)에서 **똑같은 부분만** `IsNativeQaCamera`로 뽑는다. 세 곳은 조건이 일부러 다르다: artwork 검사는 QueuePage에만, isPlaying은 서로 반대. 다른 조건은 각 자리에 남긴다.
- 1920·1080·8.5·4.25·32를 상수로 만든다.
- Destroy-또는-DestroyImmediate 도우미가 10벌 있다. 하나로 합치되, **`MaskPage.cs:114`는 null 검사가 없는 버전이므로 동작이 다르다.** 그대로 두거나 따로 확인한다.
- `?.`를 `!= null`로 바꾼다: SelectionOutline.cs:94, TowerGrounding.cs:25-26, SpriteLibrary.cs:35.
- 셰이더(P-2를 끝낸 뒤에만):
  - 공통 정점 함수를 `CoreGuardianCommon.hlsl`로 옮긴다(6개 셰이더).
  - 내장 함수 이름과 겹치는 지역 변수 이름을 바꾼다: `clip`(SharedOutlineMask.shader:14), `distance`(UpgradeScan.shader:22).
  - 접촉 띠 상수 `.55`·`.21`·`half4(.035,.03,.025)`를 `#define`으로 만든다. SAMPLE와 LOAD가 다르므로 코드 본체는 합치지 않는다.
  - 다른 이름 바꾸기: `alpha`(BatchedSelectionMask.shader:21, :37), `atlasRect`(SelectionMask.shader:23, :37), 구조체 이름 `In`/`Out`.

위험:
- **`TowerGlow.shader:46`의 `3.14159`를 `PI`로 바꾸지 않는다.** 정밀도가 달라 픽셀이 바뀐다.
- 정적 생성자가 있는 정적 클래스(MaskOutputBatch, MaskPage, BodyMaskCache)는 partial로만 나눈다. 다른 클래스로 옮기면 초기화 시점이 바뀐다(`beforefieldinit`).
- `PartItem`·`Item` 구조체와 셰이더 구조체의 바이트 배치를 맞춘다(80 B / 160 B, MaskProducerBatch.cs:110의 160은 숫자 그대로).
- SharedOutlineMask 패스 번호 0/1(SharedOutline.cs:225, :270)을 유지한다.
- `_Width`는 Int 속성이고 `SetInt`로 넣는다. `SetInteger`로 "현대화"하면 깨진다.
- 전역 전용 uniform(`_CG*`)을 Properties 블록에 넣지 않는다.
- uniform을 `UnityPerMaterial`로 옮기지 않는다. SRP Batcher가 꺼진 것을 전제로 짠 C#이 있다(SpriteBlocks.cs:3-5, SpriteBatchCompat.cs:5, :44, ObjectEmphasis.cs:26-27).
- `_RendererColor`(BodyHitFlash.shader:7)는 지우지 않는다(C# 탐지 목록 SpriteBlocks.cs:13).
- `_BandWidth`의 기본값 `.06`을 지우지 않는다(C#이 넣지 않으므로 기본값이 실제 값).
- `_MainTex_TexelSize`를 직접 넣는 코드(SelectionOutline.cs:264)를 유지한다.
- `precise`와 256 서브픽셀 맞춤(BatchedSelectionMask.shader:26, :32)을 유지한다.

### ④ 캐릭터·전투 표현
작업:
- `CoreGuardianOriginalWalk.cs`: 안쪽 클래스 `VerticalLegPair`를 `CoreGuardianOriginalWalk.VerticalLegs.cs`(partial, 같은 안쪽 이름)로 옮긴다. 역할별 뼈 표(:15-19, :73-80)를 숫자 그대로 표 하나로 합친다. 깨진 들여쓰기(:72-81)를 고친다.
- `OriginalMotion`의 반동 공식 2벌(:147-148, :177-178)과 `.72f`/`1.72f`를 공통 함수로 만든다. 계산 순서가 같아야 한다.
- UnitAnimation 기본값 중복(:24-26 vs :118-119)을 상수로 만든다.
- 한 글자 이름 바꾸기: EffectsView `W`/`Set`, UnitAnimation 지역 함수 `V`/`B`.
- `CombatArt.cs:45-46`의 `??=`를 `if(x==null)x=...`로 바꾼다. 결과는 같다. Resources.Load는 없으면 진짜 null을 준다.

위험:
- HitFlash와 CoreFeedback의 블록 복원 방식을 하나로 합치지 않는다(배칭이 바뀜. F30은 성능 목록).
- `DefaultExecutionOrder` 100/200/450/500/600/600을 유지한다. 정렬 → 바닥 → 그림자 → 강조 → 외곽선 → 격노·강화 순서가 같은 프레임 결과를 정한다.
- 이름으로 찾는 것:
  - 자식 이름 `"2D Sprite"`, `"그림자"`, `"바깥 그림자"`, `"접촉 그림자"`, `"바닥 밀착선"`, `"짧은 투사 그림자"`, `"Fixed"`/`"Moving"`/`"Housing*"`
  - 스프라이트 이름 `"core"`, `"SingleBolt"`, 이름에 `Shadow`가 들어간 것
- 테스트가 쓰는 것:
  - `boots` 정적 필드, `FootOffset`, `MuzzleWorld`
  - 오브젝트 이름 "OriginalWalk", "VerticalWalk", "LeftFoot", "RightFoot", "LeftLower", "Body", "OriginalMotion", "OriginalIdle"(PlayerMotion이 만듦)
- `Point`(SelectionOutline.cs:168)와 `Lift`(OriginalWalk.cs:299) 지역 함수를 람다로 바꾸면 할당이 생긴다. 매개변수로 넘긴다.
- `SpriteAtlasSources.Reset`이 `OriginalWalk.ClearCachedParts`를 부른다(SpriteAtlasSources.cs:18).

### ⑤ HUD·UI
작업:
- `CoreGuardianHud.cs`(598줄)를 partial로 나눈다: `Hud.Factory.cs`, `Hud.FloatingNumbers.cs`, `Hud.DamageTotals.cs`, `Hud.Install.cs`, `Hud.AugmentSelection.cs`, `Hud.Formatters.cs`.
- `Redesign.cs`를 나눈다: `Hud.SelectionInfo.cs`, `Hud.AugmentStrip.cs`, `Hud.BlastButton.cs`, `Hud.DamageScrollbar.cs`.
- `CoreGuardianHoverHint`(Redesign.cs:395)와 `CoreGuardianHudReaction`(HudSkin.cs:56)을 각자 이름의 파일로 옮긴다. AddComponent로만 쓰고 씬·프리팹 참조는 없다.
- `Audio/CoreGuardianHud.Sound.cs`를 `.meta`와 함께 `UI/`로 옮긴다. MonoBehaviour 본체 파일이 아니므로 GUID와 무관하다.
- `UnityEngine.UI.Text`처럼 전체 이름을 쓰는 방식과 `using`을 하나로 통일한다.
- `?.` 바꾸기: Hud.cs:179, :313, :352; Inspector.cs:58, :79, :151, :171; Redesign.cs:32, :38, :44, :281; AugmentationView.cs:26, :64.

위험:
- **Build 호출 순서가 곧 `refresh` 실행 순서다**(Hud.cs:523, Inspector.cs:77). SetActive가 일어나는 순서도 이것으로 정해지므로 Build* 호출 순서를 바꾸지 않는다.
- `damageTotalCompare`는 `damageTotalComparer` 뒤에 같은 파일 안에 둔다(:41-43).
- 버튼 오브젝트 이름이 동작을 정한다:
  - `HudSkin.Symbol(name)`이 이름으로 아이콘을 고르고, 모르면 "Install"로 조용히 바뀐다.
  - `StartsWith("Preview")`이면 클릭 소리가 나지 않는다.
  - `Install_`, `SpecTab`, `BuyTab`도 이름으로 판단한다.
  - 코드와 테스트가 찾는 이름: "Status", "PrototypeActions", "Reload", "TestSpeed", "SoundControls", "AugmentFold/HandleArt", "Symbol_Core", "Status_Core", "Tower_"+stat, "CombatHUD", "TopStatus", "BottomControls", "TowerInstallPanel", "PlayerGrowthPanel", "PlayerSpecTab", "PlayerBuyTab", "LabKind_Single", "SelectionInfo/Title", "AugmentationSelection", "Status_Wave", "HitDamage"
- 증강 선택 중에 모든 버튼을 끄는 구조(Hud.cs:530) 때문에 새 Button을 만들면 반드시 다시 켜는 refresh를 넣어야 한다. B5가 이것을 빠뜨린 경우다. B5 자체는 고치지 않는다.
- 테스트가 쓰는 것: `damageNumbers`, `bossStatus`, `Started`, `ShowDamageNumber`, `ClearDamageNumbers`, `CancelInstallation`, `Refresh`, `ToggleLab`/`ToggleBench`, `InspectionTarget`.
- `DamageApplied` 구독 순서: CombatAudio → HUD → CombatArt → CombatAnimation → EffectsView. 이 순서 때문에 골드 숫자가 피해 숫자 위에 그려진다(Hud.cs:416 `SetAsLastSibling`).

### ⑥ 게임 시작·입력 (Bootstrap)
작업:
- `CoreGuardianPrototype.cs`(638줄)를 partial로 나눈다: `.Input.cs`, `.Placement.cs`, `.Buildings.cs`, `.Commands.cs`, `.Views.cs`, `.Art.cs`. 본체에는 필드, `Start`, `Update`를 남긴다.
- `?.`/`??` 바꾸기: Prototype.cs의 :150, :246-248, :288, :290, :293, :313(2곳), :334, :339-340, :350, :352, :362, :364, :390, :430, :481-482, :523, :528. 지금은 모든 대상이 있으므로 결과가 같다. CombatAudio는 :109-110에서 먼저 붙인다.
- 숫자를 상수로: 알림 시간 2초·3초(:148, :418), 배속 3(:413), 정렬 오프셋 +1500/1800/1900, 그림자 값.

위험:
- `Update` 순서는 Tick → SyncTestSpeed → 단계 비교 → `SynchronizeViews` → 입력이다. 중간에 끝나는 곳(:154 Esc, :163, :187, :191)도 그대로 유지한다. 단계 비교는 Tick 전에 저장한 `phase`(:141)를 쓴다.
- `Start`의 `Configure` 호출 순서(:111-117)가 이벤트 구독 순서를 정한다(REFACTORING_RULES.md:10 "이벤트 순서").
- 몸을 풀에 반납하는 순서(:475-501)를 유지한다: ReleaseEnemy → ReleaseTarget ×2 → SetActive(false) → ResetEnemyForReuse → 부모 바꾸기.
- 씬 값이 코드 기본값과 다르다:
  - `boardSize` 45×25 → 39×22
  - `coreCell` (29,9) → (25,8)
  - `routeCells`
  - PrototypeGridMovement `spriteOffset` (0,0.11,0) → 0
  - SelectionOutline `color` → 초록
  - CoreParticleSettings `sizeMax` .045 → .1
  - 필드 이름을 바꾸면 씬 값을 잃는다. 기본값을 "정리"하면 AddComponent로 만드는 테스트의 값이 바뀐다. **둘 다 하지 않는다.**
- 테스트가 쓰는 것:
  - 메서드: `enemyViews`, `towerViews`, `previousCore`, `SynchronizeViews`, `PlayerTile`, `World`, `CanInstall`, `Place`, `ChooseTower`, `CancelInstall`, `Diagnostic`, `Restart`, `CycleTestSpeed`, `Notify`, `DestroySelected`, `UpgradePlayer`, `UpgradeTower`, `StartCombat`, `TowerView`, `PlaceAtPlayerForTest`
  - 오브젝트 이름: "Tower_종류_Id", "TowerPlacementPreview", "CoreGuardianEnemyBodyPool", "Building_TurretLab", "Building_EngineerBench"
- `Application.runInBackground=true`(:97)는 프로젝트 설정(0)을 덮어쓰는 동작이므로 유지한다.
- `player.Stepped` 람다(:108)는 해제하지 않는다. 지금 설계에서는 문제가 없지만, 플레이어 오브젝트를 따로 살려 두게 바뀌면 위험해진다.

### ⑦ 편집기 도구·테스트
작업:
- `CoreGuardianSpriteAtlasPipeline.Synchronize`(56줄)를 단계별로 나눈다.
- `MaskAtlasBuilder.Build`(78줄)를 BakeGroup / ValidatePage로, `RouteMeshBuilder.CombineKeeping`(68줄)을 ValidateSources / BuildGroupMesh로 나눈다.
- `EnsureFolder` 3벌을 하나로 합친다. 편집기 전용이라 안전하다.
- 테스트: 리플렉션 이름을 한 곳으로 모은다(P-1의 결정을 따름). 실제 셰이더 픽셀 테스트를 추가한다(P-2).

위험:
- 파일 이름과 클래스 이름이 다르다: `MaskAtlasBuilder.cs` → `CoreGuardianMaskAtlasBuilder`, `RouteMeshBuilder.cs` → `CoreGuardianRouteMeshBuilder`. 테스트가 **클래스 이름**으로 찾으므로 클래스 이름은 유지한다.
- SerializedObject 문자열 `"entries"`, `"source"`, `"page"`, `"layer"`, `"dependencyHash"`, `"inputHash"`(SpriteAtlasPipeline.cs:89-103)는 직렬화 필드 이름과 맞물려 있다.
- 빌드 훅 `callbackOrder=-100`을 유지한다.
- 편집기 경로 `CoreGuardian_MapArt_B/CoreGuardian_Route01_VisualOnly`(RouteMeshBuilder.cs:12)를 유지한다.

---

## §5 리팩토링 중 절대 건드리면 안 되는 것 (위험 목록)

| # | 무엇 | file:line |
|---|---|---|
| H1 | 이름이 계약인 것: MonoBehaviour·SO 클래스와 파일 이름, `.meta` GUID, 직렬화 필드 이름(씬 값이 코드 기본값과 다름. §4 ⑥), PlayerPrefs 키(`CoreGuardian.SoundVolume`, `.ShotCandidate`, `.Audio.<분류>`) | CoreGuardianPrototype.cs:13-27; scene :18493-18500; SoundSettings.cs:8, :15 |
| H2 | 테스트가 문자열로 찾는 타입 이름 약 32종(plain class 포함: MaskPage, BodyMaskCache, MaskOutputBatch, PlacementOverlay, CoreAmbient, CoreFeedback, OriginalWalk, AudioCatalog, SoundSettings, AudioCategory, `CoreGuardian.CombatRules`, `CoreGuardian.SegmentedEnemyHealthBar` 등)과 private 멤버 이름 약 200개. REFACTORING_RULES.md:11의 목록에 이것도 넣어야 한다. | Tests 전체 (`GetType`/`GetField`/`GetMethod`) |
| H3 | Resources 경로와 enum 이름으로 만드는 경로: `Units/enemy_<kind>`, `tower_<kind>`, `Animation/Boss/<Key>Animated`, OriginalWalk·VerticalWalk·OriginalMotion·PlayerFacing·PhotoShadows·Hud·Audio 경로. enum **순서** = 씬 배열 번호 = 풀 번호. | CoreGuardianPrototype.cs:101, :517-533; CombatData.cs:10-12, :134; CombatAnimation.cs:57, :76 |
| H4 | 셰이더 이름 `CoreGuardian/*`, C#이 넣는 속성 이름(약 40개), 전역 `_CG*`, 패스 번호, 구조체 배치(80 B/160 B), UV 채널, `_Width`(Int), `_BandWidth` 기본값, `_RendererColor`, `3.14159`, `precise`. SRP Batcher 꺼짐을 전제로 한 uniform 배치. | §4 ③; SharedOutline.cs:30, :225, :270; MaskProducerBatch.cs:110; TowerGlow.shader:46; BatchedSelectionMask.shader:26, :32 |
| H5 | **MaskOutputBatch의 정적 생성자와 `Begin` 훅은 게임 실행 중 마스크 처리(`BodyMaskCache.Flush`)를 부르는 유일한 통로다.** "편집 모드 전용 묶기"라고 보고 지우면 실행 중 외곽선 마스크가 끊긴다. 정적 생성자를 시작시키는 것은 매 프레임 `Invalidate(this)` 호출이다. | MaskOutputBatch.cs:65-68, :88, :93; SelectionOutline.cs:82, :231; MaskPage.cs:108 |
| H6 | Domain Reload가 꺼진 환경에서 정적 값을 되돌리는 장치: `SubsystemRegistration` 4곳과 정적 생성자의 `playModeStateChanged` 훅. 이름을 바꾸거나 옮기거나 정적이 아니게 만들면 Play 사이에 값이 샐 수 있다. | ObjectEmphasis.cs:80-81; SpriteAtlasSources.cs:17-18; SegmentedEnemyHealthBar.cs:49; BodyMaskCache.cs:109-120; MaskPage.cs:71-80; MaskOutputBatch.cs:65-75 |
| H7 | 순서 계약: `DefaultExecutionOrder` 100/200/450/500/600/600; `Configure` 호출 순서 = `DamageApplied` 구독 순서; Build 순서 = HUD refresh 순서; Update 안의 순서; 몸 풀 반납 순서; 난수 사용 순서. | UnitAnimation.cs:6; PhotoShadow.cs:8; ObjectEmphasis.cs:8; SelectionOutline.cs:9; RageOutline.cs:6; UpgradeFeedback.cs:5; Prototype.cs:111-117, :138-193, :475-501; Hud.cs:192-239, :523; Effects.cs:479 |
| H8 | Session 안에서 콜백을 부른 뒤 다시 확인하는 것: `disposed` 검사와 캐시 무효화. 피해 이벤트 직전에 투사체 제거를 먼저 처리하는 것. | Session.cs:478, :487, :510, :517-525, :580, :715-716, :793-805 |
| H9 | 표시용 "피해 중에만 유효한" 값 `HitCritical`/`HitAffinity`: HUD가 이벤트 콜백 **안에서** 읽는다. try/finally나 이벤트 호출 위치를 옮기면 숫자 색이 조용히 바뀐다. | Session.Effects.cs:122, :184-185; Hud.cs:383-384 |
| H10 | 이름으로 찾는 GameObject와 스프라이트(프리팹 그림 계약): "2D Sprite", 그림자 이름 5종, "core", "SingleBolt", `*Shadow*`, 리그 자식 이름. 런타임에 만든 HUD 오브젝트 이름. | §4 ④·⑤ 목록 |
| H11 | 정적 클래스를 partial로 나누는 것은 괜찮다. 다른 클래스로 옮기면 `beforefieldinit` 때문에 초기화 시점이 바뀐다. partial 파일끼리의 필드 초기화 순서는 정해져 있지 않으므로, 서로 의존하는 정적 필드(`KindCount`→`Common`, `damageTotalComparer`→`damageTotalCompare`, `partOrder`→`partCompare`)는 같은 파일 안에 그 순서로 둔다. | CombatData.cs:402-405; Hud.cs:41-43; SharedOutline.cs:62-64 |
| H12 | 몸을 풀에 반납할 때 외곽선 쌍둥이 재질을 되돌리는 정리는 지금 순서상 효과가 없다. ReleaseTarget이 SetActive(false)보다 먼저 돌고, 그 뒤 HitFlash가 쌍둥이 재질을 다시 넣는다. 화면 차이는 없다. 순서를 "고치면" 화면이 바뀔 수 있으므로 손대지 않는다. | Prototype.cs:481-483; ObjectEmphasis.cs:153-158; UnitAnimation.cs:122; HitFlash.cs:58 |
| H13 | `CombatStats.Clone()`은 `UpgradeSteps`를 복사하지 않는다. 지금은 빈 Player만 복제하므로 문제가 없다. Clone을 새로 쓰는 리팩토링을 하지 않는다. | CombatData.cs:53; Session.cs:258, :397 |
| H14 | 화면 비교 함정: `ArtDepthSorting.nextLane`과 SharedOutline 스위치가 Play를 넘어 남는다. 1920×1080이 아니면 다른 마스크 경로를 탄다. | ArtDepthSorting.cs:13; SelectionOutline.cs:248-251 |

---

## §6 공통 원인 묶음 / 확인했는데 깨끗한 것 / 지금 데이터 때문에 가려진 문제

### 공통 원인 (A를 고치면 B·C도 풀린다)
- **RC1. 화면이 규칙의 결과를 "값 비교"로 추측한다**
  - 해당: B1(증강을 코어 피해로 봄), B2(골드 숫자), F5·F11(값이 늘었는지 매 프레임 비교).
  - 해결: Session이 "코어 피해 N", "지급 골드 N" 신호를 내준다. B1과 B2는 이것 하나로 함께 풀린다.
- **RC2. HUD가 규칙 계산을 따로 다시 한다**
  - 해당: B9, B16, B18, D-14(연구소 스탯 목록 2벌), F4의 일부.
  - 해결: Session이 읽기 전용 "보여 줄 값"을 제공한다. 반올림 차이 때문에 결과가 바뀔 수 있으므로 버그 수정으로 따로 진행한다.
- **RC3. Presentation이 Assembly-CSharp에 있다**
  - 해당: H2, 테스트 리플렉션, 네임스페이스 문서 오류.
  - 해결: P-1. 이름을 바꾸는 작업 전체가 이 결정에 달려 있으므로 **가장 먼저** 정한다.
- **RC4. Domain Reload 꺼짐 + 초기화 장치 부족**
  - 해당: B12, B24, H6, H14.
  - 해결: `SubsystemRegistration` 초기화를 정리한다. B12는 빌드에서 Restart해도 생기므로 별도로 판단한다.
- **RC5. 증강 선택 중 모든 버튼을 끄는 구조**
  - B5의 원인이다. 새 버튼이 생길 때마다 같은 문제가 반복될 수 있다.
- **RC6. 카메라 판단 조건 5벌**
  - 해당: F31, X12의 접촉 자르기 조건, D-4.
  - 조건이 일부러 다르므로 공통인 부분만 뽑는다.
- **수정 순서**:
  1. P-1(asmdef 결정), P-2(픽셀 기준)
  2. 묶음 ①→⑦
  3. 버그·성능은 그 뒤에 사용자가 고른 것만. RC1을 B1·B2보다 먼저 한다.

### 확인했는데 깨끗한 것 (다시 볼 필요 없음)
- 데이터 연결:
  - CSV가 요구하는 열은 모두 있다.
  - ID 참조(표적 우선순위, 약점·저항, 강화 대상, 분열·소환, defenseRef)는 모두 존재한다.
  - 증강 50장 모두 코드에 연결되어 있다.
  - stage의 Setting 키와 effectTags의 (tag, param) 16쌍이 모두 있다(A, C).
- 씬과 프리팹:
  - 끊긴 참조 0개(씬 854개 오브젝트, 5820개 참조, 프리팹 21개).
  - 스크립트 GUID는 모두 연결된다.
  - 우리 컴포넌트에 `{fileID:0}`이 없다.
  - 씬 배열 순서가 enum 순서와 같다(C).
- 경로와 이름:
  - 코드가 쓰는 Resources 경로는 모두 ASSET_LIST에 있다.
  - 셰이더 12개의 Resources 경로와 `Shader` 이름이 일치한다(A, C).
  - C#이 넣는 셰이더 속성은 모두 대상 셰이더에 있다.
  - GPU 구조체 크기가 맞는다(A, C).
  - 우리 셰이더를 쓰는 .mat은 없다. 재질은 모두 실행 중에 만든다(C, D).
- 이벤트와 수명:
  - 이벤트 구독·해제 짝이 모두 맞는다(B, D).
  - 단, D의 "Session.Dispose가 모든 이벤트를 비운다"는 **틀렸다**. `Cue`, `TowerPulsed`, `AreaBlastUsed`는 비우지 않는다(SessionModels.cs:131). 세 구독자가 스스로 해제하므로 피해는 없다(P3).
  - 핸들은 세대 번호로 검사하고, 화면 쪽은 `.Alive` 검사를 한다(A).
  - 화면 코드가 규칙 상태를 직접 바꾸는 곳은 없다(A, D).
  - Domain → Presentation 컴파일 참조는 없다(asmdef가 막음).
- 클릭 처리:
  - 모든 클릭은 동기 처리라 "처리 중" 겹침이 없다.
  - 증강 카드는 Busy와 단계로 막는다.
  - 시간 초과 자동 선택과 클릭이 겹쳐도 안전하다(B).
- 풀 재사용 시 초기화:
  - 투사체·피해 숫자·효과·섬광 풀은 필드를 다 되돌린다.
  - `EnemyState.Clear`가 `PoolKind`를 빼먹지만, 같은 종류 풀로만 돌아가고 꺼낼 때 다시 써서 무해하다(B).
- 할당이 없는 것:
  - `ModelList` 반복자, Handle 비교, 캐시된 정렬 delegate, `RemoveAll` 람다(캡처 없음), 지역 함수 `Point`/`Lift`.
  - Session.Tick 안에서 문자열을 만드는 곳은 F10뿐이다(E).
- 파일 형식: BOM 0개. 4칸 들여쓰기. 탭은 Welcome asmdef에만 있다. 쓰지 않는 `using` 0개(E).
- 최종 검토의 파일 점검(지적이 거의 없던 파일을 직접 열어 봄):
  - 깨끗함: CoreGuardianHierarchyWatch.cs, CoreGuardianSpriteBlocks.cs, CoreGuardianMaskAtlas.cs(편집기 전용 해시 검사, 빌드 훅이 맞춰 줌), CoreGuardianSoundSettings.cs(키를 미리 만듦), CoreGuardianMusicAudio.cs(F27 외 없음), PrototypeGridMovement.cs(규칙 모양을 이미 따름. 입력 → 한 칸 이동만 함), CoreGuardianContentAssets.cs(편집기 메뉴), CoreGuardianSpriteAtlasSources.cs(Cut의 `mappings.Add`는 늘 새 Sprite라 중복 예외가 없음), TowerBaseShade.shader(UnityPerMaterial을 지킴), Health/EnemyDefinition/WaypointPath(예전 코드, X16).
  - CoreGuardianHudHandleReaction.cs는 매 프레임 같은 값을 다시 쓰는 것을 F28에 추가했다.

### 지금 데이터 때문에 가려진 문제 (지금은 차이가 없고, 데이터가 바뀌면 드러남)
- CSV 기본값(`Setting(key, 기본값)`, `Tag(..., 기본값)`)이 지금 CSV 값과 모두 같다. 행 이름을 바꾸거나 지워도 아무도 모른다(CombatData.cs:91, :96-99, :161; Prototype.cs:305-307).
- 웨이브의 (웨이브, 적) 조합마다 스탯이 모두 같다(1946행 중 다른 것 0개). 그래서 "첫 행만 쓰는" 캐시가 드러나지 않는다(CombatData.cs:85; Session.Effects.cs:311, :315, :331).
- 분열체 방어력 = 공격형 방어력(모든 웨이브). 그래서 분열 작은 개체가 부모 방어력을 따르는지 자기 defenseRef를 따르는지 차이가 안 보인다(Session.Effects.cs:322 vs Session.Debug.cs:95-96).
- 회복과 소환을 둘 다 가진 적이 없다. 둘이 같은 타이머(AbilityTimer)를 쓰는 문제가 드러나지 않는다(Session.Effects.cs:356, :375).
- defenseRef 대상 행이 모두 방어력을 직접 갖고 있다. 행 순서에 따라 계산이 달라지는 문제가 드러나지 않는다(CombatData.cs:302).
- 증강 값이 모두 정수다. 그래서 `TowerBuffPercent` 반올림과 버프 표시 차이가 없다(Augmentations.cs:92; augments.csv 22001=8, 22019=20, 22036=1).
- CSV `maxTargets`=3이 코드에 고정된 3과 같다(Session.cs:166, :763). "지원은 공격 포탑 필요" 같은 규칙도 데이터 열과 코드가 같은 내용이다.
- `burnPerStack` 강화가 그룹의 하위 항목이라 연구소에서 숨겨지지 않는다. 그룹 대표로 바뀌면 숨겨진다(Inspector.cs:24, :118-124).
- 연구소 강화가 11단계에 이르기 전에는 B4가 안 보인다. 증강 10·11·18이 없으면 B2가 안 보인다. 첫 증강 화면 전에는 B5가 안 보인다.
- 증강 아이콘은 번호로 이름을 다시 만든다. 이름 규칙이 우연히 맞아서 동작한다(`icon` 열을 읽지 않음, Hud.Redesign.cs:249).
- 효과 키는 카드 **번호**로 연결된다(`effect` 열을 안 읽음, Session.cs:656; Augmentations.cs:82-109). CSV 행 번호를 바꾸면 효과가 조용히 다른 카드로 옮겨 간다.

### 최종 검토에서 바로잡은 출처
- E 보고서가 인용한 REFACTORING_RULES.md 줄 번호는 틀렸다. 실제 줄로 바꿨다: `?.` 금지는 :36(E는 :40), 빈 catch는 :35(E는 :39), 쓰기 가능한 static은 :37(E는 :41), 셰이더 픽셀 비교는 :42(E는 :52), 함수 크기 제한은 :31, 단위 이름은 :27, 120자 제한은 :20.
- D의 "manifest에 튜토리얼 패키지 없음"은 틀렸다(manifest.json:17 `com.unity.learn.iet-framework`).
- C·B의 "Domain Reload·SRP Batcher·패커 모드 확인 못함"은 EXTRA_EVIDENCE로 확정했다(꺼짐 / 꺼짐 / 5).
- "`.editorconfig` 없음" 지적(D-41, E §5)은 PC에 이미 만들어져 있어서 삭제했다.

---

cross-reconciliation merged/deleted 60; seven checks deleted 9; opposing-case deleted or downgraded 3; severity corrected 1; file inventory added 1
