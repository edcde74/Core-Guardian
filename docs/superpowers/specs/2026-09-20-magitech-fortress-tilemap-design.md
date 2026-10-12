# 마법공학 요새도시 타일맵 디자인 / Magitech Fortress City Tilemap Design

## 목표 / Goal

Unity 6.0 URP에서 사용할 첫 번째 2D 모바일 타워 디펜스 전장을 제작한다. 정돈된 마법공학 요새도시의 관문이 산악 생물형 마수의 침공을 막는 모습이다.

Create the first playable 2D mobile tower-defense battlefield for Unity 6.0 URP: a clean, readable magitech fortress-city gate resisting biological mountain beasts.

결과물은 생성한 기본 스프라이트 시트, Unity에서 잘라낸 타일 에셋, 등록된 Tile Palette, 그리고 기존 Sample Scene에 칠해진 테스트 맵이다.

The deliverable is a generated base sprite sheet, Unity-sliced tile assets, a registered Tile Palette, and a test map painted in the existing Sample Scene.

## 아트 방향 / Visual direction

- **장소 / Place:** 폐허가 아니라, 전쟁 중에도 보수·개조하며 버티는 최전선 요새 관문.
  **Place:** a maintained frontline fortress gate, not a ruined city.
- **아군 시설 / Defenders:** 아이보리·회색 석재, 짙은 남색 금속, 황동 장식, 절제된 청록 마력광.
  **Defenders:** pale ivory/gray stone, deep navy metal, brass trims, and restrained cyan mana light.
- **적군 대비 / Invaders:** 이후 등장할 생물형 마수는 황토색·갈색·붉은 주황 계열을 사용해 플레이 가능한 지형과 확실히 분리.
  **Invaders:** future biological enemies use ochre, brown, and red-orange, staying distinct from all playable terrain.
- **표현 방식 / Style:** 깔끔한 모바일 2D 일러스트, 단순한 형태, 부드러운 음영, 제한적인 질감, 높은 길 인식성. 지형에 픽셀아트·사실적 사진 질감·글자·캐릭터·로고·UI를 넣지 않는다.
  **Style:** polished mobile 2D illustration with simple shapes, soft shading, limited texture, and high path readability. No pixel art, photorealism, text, characters, logos, or UI baked into terrain.

## 첫 배경 타일 세트 / First environment kit

탑다운 직교 시점(Top-down orthographic)으로 만들며, Unity 기준 64 PPU(Pixels Per Unit)를 사용한다. 원본 타일 한 칸은 128×128픽셀이고, 스프라이트 시트는 정확한 4×3 칸 격자로 자른다.

Use a top-down, orthographic 64 pixels-per-unit kit. Each source tile is 128 by 128 pixels, intended to be cut on a strict 4 by 3 grid.

1. 비활성 청록 마력선이 은은하게 새겨진 아이보리 석재 광장 바닥. / Ivory stone plaza floor, lightly engraved with inactive cyan mana seams.
2. 반복해도 이어져 보이는 짙은 흑연색·황동 재질의 적 이동로. / Dark graphite-and-brass enemy road tile, visually continuous when repeated.
3. 청록 깃발과 황동 보강판이 있는 성벽 상단·가장자리. / Stone wall top/edge tile with teal banners and small brass reinforcements.
4. 약한 청록 빛을 내는 마법공학 에너지 도관 또는 배관. / Magitech energy conduit or pipe tile with low-intensity cyan glow.
5. 성벽 주변에 둘 수 있는 마력 기둥(Mana Pylon) 오브젝트. / Mana-pylon prop tile for placement along walls.
6. 생성 시 깨끗하게 분리할 수 있을 때만, 모서리와 손상된 바닥 변형. / Corner and damaged floor variations, only if cleanly separable from the generated sheet.

테스트 맵은 작은 요새 안뜰이다. 가운데에는 굽어진 적 이동로를 두고, 양옆에는 광장 바닥, 바깥에는 성벽, 그 위에는 소수의 기둥과 도관을 둔다. 적·타워·UI·전투·증강 기능은 이번 범위에 포함하지 않는다.

The test map is a compact fortress courtyard: plaza on both sides, a winding dark enemy road through the center, edge walls, and a few pylon and conduit accents. It intentionally contains no enemies, towers, UI, combat logic, or augment UI.

## Unity 등록 방식 / Unity integration

- 원본 스프라이트 / Source sprite: `Assets/Art/MagitechFortress/Source/MagitechFortress_BaseTiles.png`
- 임포트 설정 / Import: `Sprite (2D and UI)`, `Multiple`, 128×128 격자 분할, `PPU 64`, Bilinear 필터, 첫 검토를 위한 압축 해제.
- 팔레트 / Palette: `Assets/Art/MagitechFortress/TilePalettes/MagitechFortress_Base.palette`
- 타일 에셋 / Tiles: `Assets/Art/MagitechFortress/Tiles/`
- 씬 계층 / Scene hierarchy: `Grid` > `Ground`, `EnemyPath`, `FortressProps` Tilemaps in `Assets/Scenes/SampleScene.unity`.

## 완료 확인 기준 / Acceptance checks

1. 스프라이트 시트가 오류 없이 임포트되고, 각각의 타일을 선택할 수 있다.
   The sheet imports without sprite-slicing errors and produces individually selectable sprites.
2. Tile Palette 안에 사용할 수 있는 타일 에셋이 들어 있다.
   A Tile Palette contains the usable tile assets.
3. Sample Scene의 Scene 뷰에 가운데 적 이동로, 주변 광장, 요새 장식이 구분되어 보인다.
   The sample scene shows a readable central enemy path, surrounding plaza, and fortress accents in the Scene view.
4. 기존 Unity 템플릿 에셋을 삭제하거나 덮어쓰지 않는다.
   No existing Unity template assets are removed or overwritten.
