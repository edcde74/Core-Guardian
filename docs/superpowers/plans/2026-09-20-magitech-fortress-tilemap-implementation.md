# Magitech Fortress Tilemap Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generate a clean top-down magitech fortress tile sheet and register its sliced tiles in a Unity Tile Palette and sample-scene test map.

**Architecture:** One imported 4x3 tile sheet is the visual source of truth. Unity slices it into sprites, uses those sprites to create Tile assets in a dedicated palette folder, then paints separate Ground, EnemyPath, and FortressProps layers below one Grid in the existing sample scene.

**Tech Stack:** Unity 6.0.6 URP 2D, Unity Tilemap package 1.0.0, generated PNG sprite sheet, Unity Sprite Editor, Tile Palette.

**Spec:** `docs/superpowers/specs/2026-09-20-magitech-fortress-tilemap-design.md`

## Global Constraints

- Use a 4x3 grid of 128x128-pixel source tiles and 64 Pixels Per Unit (PPU).
- Use a clean top-down mobile style: ivory/gray stone, navy metal, brass, restrained cyan mana light.
- Do not include pixel art, photorealistic textures, text, characters, logos, UI, enemies, towers, combat logic, or augment UI.
- Keep all new art under `Assets/Art/MagitechFortress/`; do not overwrite template assets.
- Unity object names are `Grid`, `Ground`, `EnemyPath`, and `FortressProps`.

## Review Focus

- Repeating the enemy-road tile must not show a seam that obscures the path direction.
- The cyan mana light must remain a secondary accent rather than making a dark, visually noisy floor.
- Imported sprites must slice at exactly 128x128 pixels with no overlap or empty margins.
- The palette must contain actual Tile assets, not only sprite references, so tiles can be painted.
- The scene must use separate visual layers, so a path or prop change cannot overwrite the base ground.

---

### Task 1: Generate and inspect the source tile sheet

**Files:**
- Create: `Assets/Art/MagitechFortress/Source/MagitechFortress_BaseTiles.png`

**Interfaces:**
- Consumes: the approved visual direction and strict 4x3 grid in the spec.
- Produces: one 512x384 PNG whose cells are individually usable 128x128 terrain or prop sprites.

- [ ] **Step 1: Generate the sheet**

Use the image-generation prompt below and save its returned PNG to the specified Source path.

```
Top-down orthographic 2D mobile game tile sheet, exactly 4 columns by 3 rows of equally sized square tiles, crisp visible but subtle cell separation only for sprite slicing, no text, no characters, no UI, no logos, no pixel art. Theme: maintained frontline magitech fortress city gate. Palette: pale ivory and gray stone, deep navy metal, restrained brass, subtle cyan mana glow. Cell 1 plaza floor; cell 2 road floor; cell 3 damaged plaza; cell 4 plaza corner; cell 5 road variation; cell 6 road edge; cell 7 stone wall top edge with tiny teal banner; cell 8 wall corner; cell 9 cyan energy conduit; cell 10 brass-and-cyan mana pylon; cell 11 small wall reinforcement; cell 12 clean accent floor. Keep every tile viewed directly from above, with matching scale, no cast shadows crossing tile edges, simple polished casual mobile illustration, repeating textures should tile cleanly.
```

- [ ] **Step 2: Inspect the result against the Review Focus**

Check that the image is 4 columns by 3 rows, path and floor cells differ clearly, and no text or character has been generated. If any requirement fails, regenerate once using a prompt that names the failed requirement.

- [ ] **Step 3: Confirm the source file exists**

Run: `Get-Item Assets/Art/MagitechFortress/Source/MagitechFortress_BaseTiles.png | Select-Object Name,Length`

Expected: one non-empty PNG file named `MagitechFortress_BaseTiles.png`.

### Task 2: Import and slice the sprite sheet

**Files:**
- Modify: `Assets/Art/MagitechFortress/Source/MagitechFortress_BaseTiles.png.meta`

**Interfaces:**
- Consumes: the generated 4x3 sheet from Task 1.
- Produces: 12 individually selectable Unity sprites at PPU 64.

- [ ] **Step 1: Configure the importer in Unity**

Select the PNG in Project. Set `Texture Type` to `Sprite (2D and UI)`, `Sprite Mode` to `Multiple`, `Pixels Per Unit` to `64`, `Filter Mode` to `Bilinear`, and `Compression` to `None`; click `Apply`.

- [ ] **Step 2: Slice the sheet**

Open `Sprite Editor`, choose `Slice`, choose `Grid By Cell Size`, enter `128` for both X and Y, set `Pivot` to `Center`, click `Slice`, then `Apply`.

- [ ] **Step 3: Verify slicing**

Expand the source PNG in Project and confirm 12 child sprites appear. Select any two adjacent sprites and confirm their inspector rect is 128 by 128 pixels.

### Task 3: Create Tile assets and Tile Palette

**Files:**
- Create: `Assets/Art/MagitechFortress/TilePalettes/MagitechFortress_Base.palette`
- Create: `Assets/Art/MagitechFortress/Tiles/*.asset`

**Interfaces:**
- Consumes: the 12 sliced sprites from Task 2.
- Produces: a paintable palette with reusable Tile assets.

- [ ] **Step 1: Create the Tile Palette**

Open `Window > 2D > Tile Palette`, create a new palette named `MagitechFortress_Base`, use `Rectangular` grid, then save it to `Assets/Art/MagitechFortress/TilePalettes/`.

- [ ] **Step 2: Create Tile assets**

Drag all 12 sliced sprites into the palette. When Unity asks where to save Tile assets, choose `Assets/Art/MagitechFortress/Tiles/`.

- [ ] **Step 3: Verify paintability**

In the palette, choose one ground tile and one road tile. Confirm both show an active brush selection and that their `.asset` Tile files exist in the Tiles folder.

### Task 4: Create the scene layers and paint the test courtyard

**Files:**
- Modify: `Assets/Scenes/SampleScene.unity`

**Interfaces:**
- Consumes: the Tile Palette from Task 3.
- Produces: a three-layer painted fortress-courtyard test map.

- [ ] **Step 1: Create the grid hierarchy**

In `SampleScene`, create a `2D Object > Tilemap > Rectangular` object named `Grid` if no Grid exists. Under it create child Tilemaps named `Ground`, `EnemyPath`, and `FortressProps`.

- [ ] **Step 2: Paint the base ground**

Select `Ground`, select the ivory plaza tile, and paint a 14 by 10 rectangular courtyard.

- [ ] **Step 3: Paint a readable enemy path**

Select `EnemyPath`, select the graphite-and-brass road tile, and paint a three-tile-wide S-shaped route entering from the left edge, passing through the middle, and exiting at the right edge. Leave at least two ground tiles visible above and below the road at every point.

- [ ] **Step 4: Add fortress accents**

Select `FortressProps`. Place wall tiles along the top and bottom boundary, one mana pylon near each path bend, and two conduits parallel to a wall. Do not cover the route.

- [ ] **Step 5: Save and verify visually**

Save the scene. In Scene view, frame the entire Grid and confirm all three layers are visible: pale courtyard, dark central road, and blue/cyan fortress accents.

### Task 5: Final verification and handoff

**Files:**
- Verify: `Assets/Art/MagitechFortress/Source/MagitechFortress_BaseTiles.png`
- Verify: `Assets/Art/MagitechFortress/Tiles/`
- Verify: `Assets/Scenes/SampleScene.unity`

**Interfaces:**
- Consumes: completed Tasks 1 through 4.
- Produces: a visual review result and concise handoff notes.

- [ ] **Step 1: Run the asset inventory check**

Run: `rg --files Assets/Art/MagitechFortress Assets/Scenes/SampleScene.unity`

Expected: the source PNG, Tile Palette, Tile assets, and scene are present.

- [ ] **Step 2: Perform visual review in Unity**

Inspect Scene view at normal mobile-game zoom. Confirm clear stone/road contrast, a continuous route, and no visible 128-pixel grid seams in repeated ground or road tiles.

- [ ] **Step 3: Record results**

Report the created paths, the three Tilemap layer names, and any generator limitation that should be retouched before production use.

## Self-review

- Spec coverage: Tasks 1-4 cover every asset, import, palette, and scene requirement. Task 5 covers the stated acceptance checks.
- Placeholder scan: no TBD/TODO or undefined implementation steps remain.
- Interface consistency: every task consumes the output produced by its predecessor; the specified names and paths match the design spec.
- Review Focus coverage: source and repeated-art concerns are checked in Task 1 and Task 5; slice dimensions in Task 2; real Tile assets in Task 3; isolated layers in Task 4.
