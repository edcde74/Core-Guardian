# 별도 보고: 삭제 후 복구할 가치가 있는 원본

아래는 **현재 게임에서 사용 중이라는 뜻이 아니다.** 미참조 판정으로 Assets에서는 제거하되, 삭제 전 커밋과 파일별 manifest에 복구 경로를 남긴다. 새 아트 승인/채택은 하지 않는다.

|우선순위|리소스|나중에 쓸 가능성이 있는 이유|처리|
|---|---|---|---|
|높음|`Assets/Art/Hud/20261013/{CardBackSource,CardBackTiersSource,HudAtlasSource}.png` 3개|HUD·증강 카드의 편집/파생 제작용 큰 원본. 현재 실행은 별도 Hud 스프라이트를 사용한다.|원본3개 삭제·현재 연결된 HUD33개 보존·Git 복구 가능|
|높음|`Assets/Art/ScreenTests/20260930/Revision2/Source/` 중 미참조6개|현재 기준 Revision2 병사·바닥·길의 생성 참고 원본. 향후 동일 계열 수정 시 비교/편집 자료로 재사용 가능. 실제 게임 픽셀 규격/승인본과는 구분.|미참조 원본만 삭제·연결된 Revision2/Sprites·프리팹 유지|
|중간|`Assets/Art/Environment/AbandonedFortressB/{NaturalCourtyard_01,CourtyardBackground_B}.png` 2개|배경 변형/다른 맵의 재료 가능성. 현재 연결된 ClearBorder 및 장식 배경과는 별개이며 재채택 승인 아님.|미참조2개 삭제·현재 배경 보존|
|낮음/별도 화풍 검토|`Assets/AS_Magic_Aura_Construction_Kit_FREE/`의 미참조 Circles/FX/Glyphs/Particles/Runes32개|추후 버프·스킬 효과 소재로 기술적 재사용 가능. 현재 픽셀 화풍에 맞는다는 판정은 하지 않았다.|미참조32개만 삭제·기존 데모 재질에 연결된10개는 보존|

`Assets/Art/RejectedAnimations/20261015/`4개와 미참조 `Resources/CoreGuardian/Animation/{Attack,Defense,Buff,Boss}/atlas.png`4개는 고우선 재사용 후보로 분류하지 않는다. 현재 연결된 보행/방향/포탑 스프라이트와 혼동하지 않는다.

Assets/docs의 과거 캡처13개는 게임 리소스가 아닌 기록 자료라, 바이트 동일한 사본을 이 기록의 `imported_evidence/`에 두고 Unity Assets에서 제거한다. 원래 경로·해시는 manifest에 보존한다.

복구는 이 폴더 README의 삭제 전 커밋에서 **필요한 원본과 `.meta`만** 꺼낸다. 사용자 현재 씬/설정 전체를 되돌리지 않는다.
