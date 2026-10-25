# 실행 파일 전용 버그 수정·Editor/빌드 차이 점검 (2026-10-25, Claude)

## 수정한 버그 (사용자 확인 완료)
- 증상 1: 실행 파일에서 '타워 설치'를 눌러도 설치 가능(초록)/불가(빨강) 칸 표시가 안 뜸. Editor는 정상.
  - 원인: `CoreGuardianPrototype.ChooseTower`가 칸 표시 재질로 `GraphicsSettings.currentRenderPipeline.default2DMaterial`을 사용. URP(`UniversalRenderPipelineAsset.DefaultResources.cs` `GetMaterial`)는 `#if UNITY_EDITOR`에서만 재질을 주고 Player에서는 `null`을 돌려준다 → `CoreGuardianPlacementOverlay` 생성자가 예외 → 설치 시작 처리 뒷부분(커서·Changed·HUD Refresh)이 건너뜀.
  - 수정: `PlacementOverlaySource()` 추가. Editor는 기존 `default2DMaterial` 그대로, Player는 `Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default")`로 만든 재질(전투 씬이 이 셰이더를 참조해 빌드에 포함됨 — 빌드 packedAssets에서 확인). 재질이 없으면 경고만 남기고 설치는 계속. 색·투명도·정렬 변경 없음. 종료 시 만든 재질 정리.
- 증상 2: 실행 파일에서 Esc 설치 취소가 안 됨(Editor는 됨). Esc 처리(`CoreGuardianPrototype.cs` Update의 `escapeKey` → `hud.CancelInstallation()`) 자체에는 Editor/Player 차이가 없었다. 증상 1 수정 빌드에서 사용자가 정상 동작 확인. 원인 연결은 직접 검증하지 못했다(Player.log 미확인).

## 검증
- 컴파일 오류 0. EditMode(CoreGuardian 필터) 300/300 — `CoreGuardianSpriteAtlasCompatibilityTests.ManagedAtlasesKeepFullRectPixelsAndOriginalBMaskLookup`은 첫 실행에서 `CG_Towers_Common_01.spriteatlasv2.meta` 쓰기 실패(파일 잠김 추정, OneDrive 등)로 실패 → 단독 재실행 통과. 이후 Git에서 아틀라스 파일 변경 없음 확인.
- 일반 빌드 `Builds/CoreGuardian_20261025_fix/CoreGuardian.exe` 성공(오류 0, 약 183MB, 50초). 경고 499개는 대부분 `com.unity.ai.inference`(Sentis) 셰이더, 나머지는 기존 obsolete API 3개와 Pipeline 패키지 안내 1개.
- 사용자 실행: 설치 칸 표시·Esc 취소 모두 정상.

## 전체 스크립트 점검 (읽기만, 수정 없음)
Runtime 전체(약 45만 자)와 Editor/Rendering을 읽었다. Unity에서 직접 확인한 것: Resources 폴더가 Editor 폴더 아래에 숨어 있지 않음(19개 모두 일반 경로), 오디오 catalog 28개 경로·`cancel_install` 정상·중복/잘못된 category 없음, OriginalMotion 6개·셰이더 8개·CombatB 9개 존재, SpriteAtlas 3개 모두 packables 0(패킹 차이 없음, `spritePackerMode=SpriteAtlasV2`), 마스크 아틀라스 Page 9장 데이터 정상(파일 크기 일치).

### Editor와 실행 파일이 다르게 동작할 수 있는 곳
1. **빠른 윤곽·마스크 경로의 해상도 조건 (코드 확정, 영향 큼)** — `CoreGuardianSelectionOutline.cs` 122·235·248행. page-raster 마스크 생성(`QueuePage`)과 compact contact crop은 `camera.pixelRect == (0,0,1920,1080)`이고 ortho 8.5/4.25일 때만 켜진다. 창 모드·다른 해상도에서는 옛 경로로 조용히 돌아가 성능·윤곽이 Editor 측정과 달라진다. 2026-10-25 실측 PC는 1920×1080 창이라 해당 없음. 1280×720 최소 사양 측정 전에 정리 필요.
2. **코드로 만드는 SpriteRenderer의 기본 재질 (가능성)** — `CoreGuardianPrototype.CreateSprite`(체력바 등), `CoreGuardianCombatArt.NewRenderer`(표식·효과), `CoreGuardianCombatAnimation`(발사 섬광). 재질을 지정하지 않아 Editor는 URP Sprite-Lit-Default, Player는 다른 기본 재질(Sprites-Default)을 받을 수 있다. 실행 파일 화면으로 확인 필요.
3. **Domain Reload OFF로 Editor에만 남는 static** — 오디오 catalog `cues/byClip`, `CoreGuardianHud.damageSprites`, `CoreGuardianHudSkin.sprites`, `CoreGuardianPhotoShadow.registration`, `CoreGuardianSharedOutline.Enabled/Batched/ReuseGeometry`. 측정 도구·테스트가 바꾼 값이나 수정 전 데이터가 다음 Editor Play에 남는다. 빌드는 매번 기본값.
4. 일반 빌드에서 `Profiler.GetRuntimeMemorySizeLong` 등 프로파일러 메모리 값은 0 → 성능 표시 메모리 일부 값 무의미. PlayerPrefs(음량)는 Editor/Player 따로 저장. HUD 숫자는 현재 문화권 소수점(쉼표 지역이면 "1,5", 표시만).

### 지금은 안 터지지만 터지면 매 프레임 Update가 멈추는 구조
(멈추면 Esc·클릭·설치 미리보기가 모두 죽는다)
- `CoreGuardianUnitAnimation.Attach`(37~41행): `Configure` 도중 예외 시 반쯤 설정된 컴포넌트가 남아 이후 매 프레임 NullReference. 적은 실패마다 매 프레임 새로 Instantiate되어 쌓임.
- `CoreGuardianPrototype.Start`(94~111행): `Session` 대입 후 실패하면 `enabled=false`지만 `Ready`는 true로 남고 HUD 버튼은 계속 동작.
- `Session.CompleteRound`(453~457행): 다음 웨이브가 없으면 보상 지급 후 예외, Phase가 Combat 그대로 → 매 프레임 보상 반복 + 예외. 현재 30라운드 데이터는 정상.
- `CoreGuardianAudioCatalog`(38~45행): 읽다가 실패하면 반쯤 채운 사전이 고정 → 이후 소리 누락, `CoreGuardianMusicAudio`는 매 프레임 예외.

### 작은 버그
- 강조(×1.15 쌍둥이 재질) 중 피격 섬광 종료·그림자 생성 시 밝은 재질이 남아 계속 밝게 보일 수 있음(`ObjectEmphasis`/`HitFlash`/`PhotoShadow`).
- 설치 중 창 밖 클릭 후 복귀 시 커서가 숨은 채 남을 수 있고, 창을 다시 누르는 클릭이 설치로 처리될 수 있음.

### 확인해서 문제 없던 것
SpriteAtlas 패킹 차이 없음, 마스크 아틀라스 데이터 정상, Esc를 가로채는 코드 없음(`sendNavigationEvents=false`), Resources 누락 없음, 설치 칸 재질 셰이더 빌드 포함.

## 실행 파일 첫 전투 기록 (사용자, 2026-10-25 17:14)
- `perf_20261025_171421`: 실행 파일, 1920×1080 창, 144Hz, Ryzen 5 5600X / RTX 3070, 3배속, 5~30라운드 682초, 최대 적 60·포탑 27.
- 전투 평균 144FPS(VSync 상한), 1% low 대부분 약 137, 최악 20.8ms(11라운드 1회), 13.9ms(VSync 1회 놓침) 간헐. GPU 약 0.3ms. 메모리 284 → 314MB.
- 측정 모드(F5)는 처음 5초(준비, 적 0: 약 1700FPS, CPU 메인 0.44ms)만 켜져 있었다. 이후 VSync에 묶여 CPU 메인 값에 화면 대기가 섞임(적 0~10마리 6.7ms → 50마리 이상 3.1ms로 오히려 감소) → 전투 CPU 비용은 알 수 없음. Editor PlayerLoop와 비교하지 않는다.
- 다음 기록: F5를 한 번만 눌러 측정 모드 확인 → F4 기록 → F4 정지(F5 다시 누르지 않음).
